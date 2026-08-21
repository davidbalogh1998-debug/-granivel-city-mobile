using UnityEngine;

namespace GranivelCity
{
    [RequireComponent(typeof(Rigidbody))]
    public class TrafficCarAI : MonoBehaviour
    {
        public float speed = 7f;
        private Rigidbody rb;
        private float turnTimer;
        private float turnDirection;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            turnTimer = Random.Range(2f, 6f);
        }

        private void FixedUpdate()
        {
            if (GetComponent<VehicleController>()?.IsPlayerDriven == true) return;

            turnTimer -= Time.fixedDeltaTime;
            if (turnTimer <= 0f)
            {
                turnTimer = Random.Range(3f, 7f);
                turnDirection = Random.Range(-0.35f, 0.35f);
            }

            Vector3 origin = transform.position + Vector3.up * 0.45f;
            if (Physics.Raycast(origin, transform.forward, out _, 5f)) turnDirection = Random.value > 0.5f ? 1f : -1f;

            rb.AddForce(transform.forward * speed, ForceMode.Acceleration);
            float max = 9f;
            if (rb.linearVelocity.magnitude > max) rb.linearVelocity = rb.linearVelocity.normalized * max;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnDirection * 28f * Time.fixedDeltaTime, 0f));
            Vector3 local = transform.InverseTransformDirection(rb.linearVelocity);
            local.x *= 0.8f;
            rb.linearVelocity = transform.TransformDirection(local);
        }
    }
}
