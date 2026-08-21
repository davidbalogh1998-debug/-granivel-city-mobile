using UnityEngine;

namespace GranivelCity
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 5.2f;
        public float height = 1.85f;
        public float aimDistance = 3.15f;
        private float yaw;
        private float pitch = 12f;

        private void LateUpdate()
        {
            var runtime = GameRuntime.Instance; if (runtime == null) return;
            target = runtime.ActiveTarget; if (target == null) return;
            Vector2 look = runtime.PCInput != null ? runtime.PCInput.LookDelta : Vector2.zero;
            yaw += look.x; pitch = Mathf.Clamp(pitch + look.y, -22f, 62f);
            bool vehicle = runtime.Player != null && runtime.Player.CurrentVehicle != null;
            bool aiming = runtime.PCInput != null && runtime.PCInput.AimHeld && !vehicle;
            float currentDistance = vehicle ? 8.4f : aiming ? aimDistance : distance;
            float currentHeight = vehicle ? 2.7f : height;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * currentHeight;
            if (aiming) pivot += rotation * Vector3.right * 0.65f;
            Vector3 wanted = pivot - rotation * Vector3.forward * currentDistance;
            if (Physics.Linecast(pivot, wanted, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) wanted = hit.point + hit.normal * 0.25f;
            transform.position = Vector3.Lerp(transform.position, wanted, 16f * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation((pivot + rotation * Vector3.forward * (aiming ? 12f : 0f) - transform.position).normalized, Vector3.up);
        }
    }
}
