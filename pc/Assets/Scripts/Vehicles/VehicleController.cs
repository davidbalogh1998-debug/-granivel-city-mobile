using UnityEngine;

namespace GranivelCity
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        public float acceleration = 22f;
        public float reverseAcceleration = 13f;
        public float maxSpeed = 42f;
        public float steering = 72f;
        public float brakePower = 4.5f;
        public float fuel = 65f;
        public float fuelCapacity = 65f;
        public PlayerController Driver { get; private set; }
        public bool CanEnter => Driver == null;
        public bool IsPlayerDriven => Driver != null;

        private Rigidbody rb;
        private TrafficCarAI traffic;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>(); rb.mass = 1350f; rb.linearDamping = 0.22f; rb.angularDamping = 2.8f; rb.centerOfMass = new Vector3(0f, -0.45f, 0f);
            traffic = GetComponent<TrafficCarAI>(); fuel = Random.Range(fuelCapacity * 0.35f, fuelCapacity);
        }

        private void FixedUpdate()
        {
            if (Driver == null) return;
            var input = GameRuntime.Instance?.PCInput; if (input == null) return;
            float throttle = fuel > 0.01f ? input.Move.y : 0f; float steer = input.Move.x;
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward); float accel = throttle >= 0f ? acceleration : reverseAcceleration;
            if (Mathf.Abs(forwardSpeed) < maxSpeed || Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed)) rb.AddForce(transform.forward * throttle * accel, ForceMode.Acceleration);
            float steerScale = Mathf.Lerp(0.28f, 1f, Mathf.Clamp01(rb.linearVelocity.magnitude / 6f));
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steer * steering * steerScale * Time.fixedDeltaTime, 0f));
            Vector3 local = transform.InverseTransformDirection(rb.linearVelocity); local.x *= 0.89f; rb.linearVelocity = transform.TransformDirection(local);
            if (input.SprintHeld) rb.linearVelocity *= Mathf.Clamp01(1f - brakePower * Time.fixedDeltaTime);
            fuel = Mathf.Max(0f, fuel - Mathf.Abs(throttle) * Time.fixedDeltaTime * 0.018f);
        }

        public void SetDriver(PlayerController player) { Driver = player; if (traffic != null) traffic.enabled = false; }
        public void ClearDriver() { Driver = null; if (traffic != null) traffic.enabled = true; }
    }
}
