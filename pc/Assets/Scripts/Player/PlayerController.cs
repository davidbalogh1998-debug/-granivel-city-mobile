using UnityEngine;

namespace GranivelCity
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 5.4f;
        public float sprintSpeed = 8.8f;
        public float jumpHeight = 1.3f;
        public float gravity = -24f;
        public VehicleController CurrentVehicle { get; private set; }

        private CharacterController controller;
        private Renderer bodyRenderer;
        private float verticalVelocity;

        private void Awake() { controller = GetComponent<CharacterController>(); bodyRenderer = GetComponent<Renderer>(); }

        private void Update()
        {
            var runtime = GameRuntime.Instance;
            var input = runtime?.PCInput;
            if (input == null || runtime.World == null || !runtime.World.WorldReady) return;

            if (CurrentVehicle != null)
            {
                if (input.InteractPressed) ExitVehicle(false);
                return;
            }

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Vector3 move = forward * input.Move.y + right * input.Move.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = input.SprintHeld ? sprintSpeed : walkSpeed;
            if (move.sqrMagnitude > 0.02f) transform.forward = Vector3.Slerp(transform.forward, move.normalized, 12f * Time.deltaTime);

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (input.JumpPressed) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
            if (input.InteractPressed) TryEnterNearestVehicle();
        }

        private void TryEnterNearestVehicle()
        {
            var vehicles = Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            VehicleController nearest = null; float best = 4.2f;
            foreach (var vehicle in vehicles)
            {
                if (!vehicle.CanEnter) continue;
                float d = Vector3.Distance(transform.position, vehicle.transform.position);
                if (d < best) { best = d; nearest = vehicle; }
            }
            if (nearest != null) EnterVehicle(nearest);
        }

        public void EnterVehicle(VehicleController vehicle)
        {
            if (vehicle == null || CurrentVehicle != null || !vehicle.CanEnter) return;
            CurrentVehicle = vehicle; vehicle.SetDriver(this); controller.enabled = false; if (bodyRenderer) bodyRenderer.enabled = false;
            transform.SetParent(vehicle.transform); transform.localPosition = new Vector3(-0.35f, 0.55f, 0f); transform.localRotation = Quaternion.identity;
            GameRuntime.Instance?.Missions.NotifyVehicleEntered();
        }

        public void ExitVehicle(bool force)
        {
            if (CurrentVehicle == null) return;
            var vehicle = CurrentVehicle; CurrentVehicle = null; transform.SetParent(null);
            transform.position = vehicle.transform.position + vehicle.transform.right * -2.1f + Vector3.up * 0.5f;
            if (bodyRenderer) bodyRenderer.enabled = true; controller.enabled = true; vehicle.ClearDriver();
        }

        public void Teleport(Vector3 position)
        {
            bool enabledBefore = controller.enabled; controller.enabled = false; transform.position = position; verticalVelocity = 0f; controller.enabled = enabledBefore;
        }
    }
}
