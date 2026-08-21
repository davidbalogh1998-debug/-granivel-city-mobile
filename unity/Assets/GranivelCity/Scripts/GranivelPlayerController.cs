using UnityEngine;

namespace GranivelCity
{
    [RequireComponent(typeof(CharacterController))]
    public class GranivelPlayerController : MonoBehaviour
    {
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float gravity = -24f;
        public float interactRadius = 4.5f;
        public Transform cameraTarget;

        CharacterController controller;
        Vector2 mobileMove;
        float verticalVelocity;
        GranivelVehicleController driving;
        Renderer[] renderers;

        public bool IsDriving => driving != null;
        public GranivelVehicleController DrivingVehicle => driving;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            renderers = GetComponentsInChildren<Renderer>(true);
            if (cameraTarget == null)
            {
                var target = new GameObject("CameraTarget").transform;
                target.SetParent(transform);
                target.localPosition = new Vector3(0f, 1.45f, 0f);
                cameraTarget = target;
            }
        }

        void Update()
        {
            if (driving != null)
            {
                transform.position = driving.transform.position;
                if (Input.GetKeyDown(KeyCode.E)) ExitVehicle();
                return;
            }

            var keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var move = mobileMove.sqrMagnitude > 0.01f ? mobileMove : keyboard;
            move = Vector2.ClampMagnitude(move, 1f);

            var cam = Camera.main;
            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 worldMove = forward * move.y + right * move.x;

            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = sprint ? sprintSpeed : walkSpeed;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((worldMove * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
            if (worldMove.sqrMagnitude > 0.04f)
            {
                var rotation = Quaternion.LookRotation(worldMove, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 12f * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.E)) Interact();
        }

        public void SetMobileMove(Vector2 value) => mobileMove = Vector2.ClampMagnitude(value, 1f);

        public void Interact()
        {
            if (driving != null) { ExitVehicle(); return; }
            GranivelVehicleController best = null;
            float bestDistance = interactRadius;
            foreach (var collider in Physics.OverlapSphere(transform.position, interactRadius))
            {
                var vehicle = collider.GetComponentInParent<GranivelVehicleController>();
                if (vehicle == null || vehicle.HasDriver) continue;
                float distance = Vector3.Distance(transform.position, vehicle.transform.position);
                if (distance < bestDistance) { best = vehicle; bestDistance = distance; }
            }
            if (best != null) EnterVehicle(best);
        }

        public void EnterVehicle(GranivelVehicleController vehicle)
        {
            if (vehicle == null || vehicle.HasDriver) return;
            driving = vehicle;
            vehicle.SetDriver(this);
            controller.enabled = false;
            foreach (var r in renderers) r.enabled = false;
            GranivelGameController.Instance?.OnVehicleEntered(vehicle);
        }

        public void ExitVehicle()
        {
            if (driving == null) return;
            var vehicle = driving;
            driving = null;
            vehicle.ClearDriver();
            transform.position = vehicle.transform.position + vehicle.transform.right * 2.4f + Vector3.up * 0.3f;
            controller.enabled = true;
            foreach (var r in renderers) r.enabled = true;
        }
    }
}
