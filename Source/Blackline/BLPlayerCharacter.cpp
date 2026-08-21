#include "BLPlayerCharacter.h"
#include "BLPlayerController.h"
#include "BLWantedComponent.h"
#include "BLSimpleVehiclePawn.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "UObject/ConstructorHelpers.h"
#include "Engine/World.h"
#include "EngineUtils.h"
#include "Kismet/GameplayStatics.h"
#include "GameFramework/DamageType.h"

ABLPlayerCharacter::ABLPlayerCharacter()
{
    PrimaryActorTick.bCanEverTick = true;
    bUseControllerRotationYaw = false;
    GetCharacterMovement()->bOrientRotationToMovement = true;
    GetCharacterMovement()->RotationRate = FRotator(0.f, 540.f, 0.f);
    GetCharacterMovement()->MaxWalkSpeed = 440.f;

    CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
    CameraBoom->SetupAttachment(RootComponent);
    CameraBoom->TargetArmLength = 430.f;
    CameraBoom->SocketOffset = FVector(0.f, 45.f, 85.f);
    CameraBoom->bUsePawnControlRotation = true;
    CameraBoom->bEnableCameraLag = true;
    CameraBoom->CameraLagSpeed = 9.f;

    FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
    FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
    FollowCamera->bUsePawnControlRotation = false;
    FollowCamera->FieldOfView = 76.f;

    BuildFallbackVisual();
}

void ABLPlayerCharacter::BuildFallbackVisual()
{
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Sphere(TEXT("/Engine/BasicShapes/Sphere.Sphere"));
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Cube(TEXT("/Engine/BasicShapes/Cube.Cube"));
    if (!Sphere.Succeeded() || !Cube.Succeeded()) return;

    // Fallback only. Replace with a MetaHuman/skeletal hero in production.
    UStaticMeshComponent* Torso = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("FallbackTorso"));
    Torso->SetupAttachment(RootComponent);
    Torso->SetStaticMesh(Cube.Object);
    Torso->SetRelativeLocation(FVector(0,0,10));
    Torso->SetRelativeScale3D(FVector(.28f,.18f,.48f));
    Torso->SetCollisionEnabled(ECollisionEnabled::NoCollision);

    UStaticMeshComponent* Head = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("FallbackHead"));
    Head->SetupAttachment(RootComponent);
    Head->SetStaticMesh(Sphere.Object);
    Head->SetRelativeLocation(FVector(0,0,78));
    Head->SetRelativeScale3D(FVector(.18f));
    Head->SetCollisionEnabled(ECollisionEnabled::NoCollision);
}

void ABLPlayerCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
    Super::SetupPlayerInputComponent(PlayerInputComponent);
    PlayerInputComponent->BindAxis(TEXT("MoveForward"), this, &ABLPlayerCharacter::MoveForward);
    PlayerInputComponent->BindAxis(TEXT("MoveRight"), this, &ABLPlayerCharacter::MoveRight);
    PlayerInputComponent->BindAxis(TEXT("Turn"), this, &ABLPlayerCharacter::Turn);
    PlayerInputComponent->BindAxis(TEXT("LookUp"), this, &ABLPlayerCharacter::LookUp);
    PlayerInputComponent->BindAction(TEXT("Interact"), IE_Pressed, this, &ABLPlayerCharacter::Interact);
    PlayerInputComponent->BindAction(TEXT("Fire"), IE_Pressed, this, &ABLPlayerCharacter::FireWeapon);
    PlayerInputComponent->BindAction(TEXT("Sprint"), IE_Pressed, this, &ABLPlayerCharacter::StartSprint);
    PlayerInputComponent->BindAction(TEXT("Sprint"), IE_Released, this, &ABLPlayerCharacter::StopSprint);
}

void ABLPlayerCharacter::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);
    if (!MobileMove.IsNearlyZero())
    {
        MoveForward(-MobileMove.Y);
        MoveRight(MobileMove.X);
    }
    if (!MobileLook.IsNearlyZero())
    {
        AddControllerYawInput(MobileLook.X * 0.065f);
        AddControllerPitchInput(MobileLook.Y * 0.05f);
    }
    UpdateMovementSpeed();
}

void ABLPlayerCharacter::MoveForward(float Value)
{
    if (!Controller || FMath::IsNearlyZero(Value)) return;
    const FRotator YawRot(0.f, Controller->GetControlRotation().Yaw, 0.f);
    AddMovementInput(FRotationMatrix(YawRot).GetUnitAxis(EAxis::X), Value);
}

void ABLPlayerCharacter::MoveRight(float Value)
{
    if (!Controller || FMath::IsNearlyZero(Value)) return;
    const FRotator YawRot(0.f, Controller->GetControlRotation().Yaw, 0.f);
    AddMovementInput(FRotationMatrix(YawRot).GetUnitAxis(EAxis::Y), Value);
}

void ABLPlayerCharacter::Turn(float Value) { AddControllerYawInput(Value); }
void ABLPlayerCharacter::LookUp(float Value) { AddControllerPitchInput(Value); }
void ABLPlayerCharacter::StartSprint() { bMobileSprint = true; }
void ABLPlayerCharacter::StopSprint() { bMobileSprint = false; }
void ABLPlayerCharacter::UpdateMovementSpeed() { GetCharacterMovement()->MaxWalkSpeed = bMobileSprint ? 720.f : 440.f; }

void ABLPlayerCharacter::Interact()
{
    ABLSimpleVehiclePawn* Best = nullptr;
    float BestDist = 330.f;
    for (TActorIterator<ABLSimpleVehiclePawn> It(GetWorld()); It; ++It)
    {
        const float D = FVector::Dist(GetActorLocation(), It->GetActorLocation());
        if (D < BestDist) { BestDist = D; Best = *It; }
    }
    if (!Best) return;

    if (ABLPlayerController* PC = Cast<ABLPlayerController>(GetController()))
    {
        SetActorHiddenInGame(true);
        SetActorEnableCollision(false);
        Best->SetDriverCharacter(this);
        PC->Possess(Best);
        PC->GetWantedComponent()->AddHeat(8.f);
    }
}

void ABLPlayerCharacter::FireWeapon()
{
    if (!FollowCamera) return;
    const FVector Start = FollowCamera->GetComponentLocation();
    const FVector End = Start + FollowCamera->GetForwardVector() * 8000.f;
    FHitResult Hit;
    FCollisionQueryParams Params(SCENE_QUERY_STAT(BLFire), true, this);
    GetWorld()->LineTraceSingleByChannel(Hit, Start, End, ECC_Visibility, Params);
    if (ABLPlayerController* PC = Cast<ABLPlayerController>(GetController()))
    {
        PC->GetWantedComponent()->AddHeat(24.f);
    }
    if (Hit.GetActor())
    {
        UGameplayStatics::ApplyPointDamage(Hit.GetActor(), 40.f, FollowCamera->GetForwardVector(), Hit, GetController(), this, UDamageType::StaticClass());
    }
}
