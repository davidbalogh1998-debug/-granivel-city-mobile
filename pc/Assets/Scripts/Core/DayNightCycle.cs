using UnityEngine;

namespace GranivelCity
{
    public class DayNightCycle : MonoBehaviour
    {
        [Range(30f, 900f)] public float fullDaySeconds = 360f;
        private Light sun;
        private float time01 = 0.28f;

        private void Awake() => sun = GetComponent<Light>();

        private void Update()
        {
            time01 = (time01 + Time.deltaTime / fullDaySeconds) % 1f;
            transform.rotation = Quaternion.Euler(time01 * 360f - 90f, -30f, 0f);
            float daylight = Mathf.Clamp01(Vector3.Dot(transform.forward, Vector3.down) * 1.4f + 0.1f);
            if (sun) sun.intensity = Mathf.Lerp(0.12f, 1.15f, daylight);
            RenderSettings.ambientLight = Color.Lerp(new Color(0.07f, 0.09f, 0.14f), new Color(0.55f, 0.58f, 0.65f), daylight);
        }
    }
}
