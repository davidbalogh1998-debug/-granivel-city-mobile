using UnityEngine;

namespace GranivelCity
{
    [RequireComponent(typeof(Camera))]
    public class MiniMapCamera : MonoBehaviour
    {
        public Transform target;
        public RenderTexture Texture { get; private set; }

        private void Awake()
        {
            Texture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
            {
                name = "GranivelCity_Minimap"
            };
            GetComponent<Camera>().targetTexture = Texture;
        }

        private void LateUpdate()
        {
            if (GameRuntime.Instance != null) target = GameRuntime.Instance.ActiveTarget;
            if (target == null) return;
            transform.position = target.position + Vector3.up * 45f;
            transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
        }

        private void OnDestroy()
        {
            if (Texture != null) Texture.Release();
        }
    }
}
