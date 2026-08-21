using UnityEngine;

namespace GranivelCity
{
    [RequireComponent(typeof(Camera))]
    public class GranivelThirdPersonCamera : MonoBehaviour
    {
        public float distanceOnFoot = 7.5f;
        public float distanceDriving = 11.5f;
        public float heightOnFoot = 3.8f;
        public float heightDriving = 5.4f;
        public float smooth = 8f;
        public float mouseSensitivity = 2.2f;

        float yaw;
        float pitch = 14f;

        void LateUpdate()
        {
            var game = GranivelGameController.Instance;
            if (game == null || game.PlayerTransform == null) return;
            var target = game.PlayerTransform;
            bool driving = game.player != null && game.player.IsDriving;

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -5f, 55f);
            }
            else if (driving)
            {
                yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, 2.2f * Time.deltaTime);
            }
            else if (game.player != null)
            {
                yaw = Mathf.LerpAngle(yaw, game.player.transform.eulerAngles.y, 1.2f * Time.deltaTime);
            }

            float distance = driving ? distanceDriving : distanceOnFoot;
            float height = driving ? heightDriving : heightOnFoot;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * (driving ? 1.2f : 1.45f);
            Vector3 desired = focus - rotation * Vector3.forward * distance + Vector3.up * (height - 1.5f);

            if (Physics.Linecast(focus, desired, out var hit))
                desired = hit.point + hit.normal * 0.25f;

            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
