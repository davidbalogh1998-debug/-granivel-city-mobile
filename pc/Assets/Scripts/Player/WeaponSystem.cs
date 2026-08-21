using UnityEngine;

namespace GranivelCity
{
    public class WeaponSystem : MonoBehaviour
    {
        public float damage = 34f;
        public float fireRate = 5f;
        public float range = 75f;
        private float nextShot;

        private void Update()
        {
            var runtime = GameRuntime.Instance;
            if (runtime == null || runtime.Player != GetComponent<PlayerController>()) return;
            if (runtime.Player.CurrentVehicle != null) return;
            if (!runtime.PCInput.ShootHeld || Time.time < nextShot) return;

            nextShot = Time.time + 1f / fireRate;
            Fire();
        }

        private void Fire()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 end = ray.origin + ray.direction * range;

            if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null && !health.isPlayer)
                {
                    health.TakeDamage(damage, gameObject);
                    GameRuntime.Instance.Wanted.AddHeat(health.isPolice ? 18f : 10f);
                }
                else
                {
                    GameRuntime.Instance.Wanted.AddHeat(2f);
                }
            }
            else GameRuntime.Instance.Wanted.AddHeat(1f);

            SpawnTracer(cam.transform.position + cam.transform.forward * 0.7f, end);
            NPCController.AlertNearby(transform.position, 18f);
        }

        private void SpawnTracer(Vector3 start, Vector3 end)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Bullet Tracer";
            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);
            Vector3 delta = end - start;
            go.transform.position = start + delta * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(delta.normalized);
            go.transform.localScale = new Vector3(0.025f, 0.025f, Mathf.Min(delta.magnitude, 16f));
            var r = go.GetComponent<Renderer>();
            if (r) r.material = RuntimeMaterials.Get("Tracer", new Color(1f, 0.78f, 0.2f));
            Destroy(go, 0.06f);
        }
    }
}
