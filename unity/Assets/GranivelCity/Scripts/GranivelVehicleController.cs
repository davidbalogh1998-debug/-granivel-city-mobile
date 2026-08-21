using UnityEngine;

namespace GranivelCity
{
    [RequireComponent(typeof(Rigidbody))]
    public class GranivelVehicleController : MonoBehaviour
    {
        public float acceleration = 22f;
        public float reverseAcceleration = 12f;
        public float maxSpeed = 32f;
        public float steering = 70f;
        public float lateralGrip = 4f;
        public float idleDrag = 0.6f;
        public bool trafficVehicle;
        public float trafficSpeed = 10f;

        Rigidbody body;
        GranivelPlayerController driver;
        float trafficDirection = 1f;

        public bool HasDriver => driver != null;
        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = Mathf.Max(900f, body.mass);
            body.centerOfMass = new Vector3(0f, -0.45f, 0f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        void FixedUpdate()
        {
            if (driver != null) DrivePlayer();
            else if (trafficVehicle) DriveTraffic();
            else body.linearDamping = idleDrag;
        }

        void DrivePlayer()
        {
            float throttle = Input.GetAxisRaw("Vertical");
            float steer = Input.GetAxisRaw("Horizontal");
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            float engine = throttle >= 0f ? acceleration : reverseAcceleration;
            if (Mathf.Abs(forwardSpeed) < maxSpeed || Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed))
                body.AddForce(transform.forward * throttle * engine, ForceMode.Acceleration);

            float speedFactor = Mathf.Clamp01(body.linearVelocity.magnitude / 3f);
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, steer * steering * speedFactor * Time.fixedDeltaTime, 0f));

            Vector3 lateral = Vector3.Project(body.linearVelocity, transform.right);
            body.AddForce(-lateral * lateralGrip, ForceMode.Acceleration);
            body.linearDamping = Input.GetKey(KeyCode.Space) ? 3.5f : 0.08f;
            if (body.linearVelocity.magnitude > maxSpeed)
                body.linearVelocity = body.linearVelocity.normalized * maxSpeed;
        }

        void DriveTraffic()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.8f, transform.forward, 7f))
                trafficDirection *= -1f;
            float target = trafficSpeed * trafficDirection;
            float current = Vector3.Dot(body.linearVelocity, transform.forward);
            body.AddForce(transform.forward * Mathf.Clamp(target - current, -5f, 5f), ForceMode.Acceleration);
            body.linearDamping = 0.15f;
        }

        public void SetDriver(GranivelPlayerController value)
        {
            driver = value;
            trafficVehicle = false;
            GranivelGameController.Instance?.SetActiveVehicle(this);
        }

        public void ClearDriver()
        {
            driver = null;
            GranivelGameController.Instance?.SetActiveVehicle(null);
        }
    }
}
