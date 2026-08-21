using System.Collections.Generic;
using UnityEngine;

namespace GranivelCity
{
    public class NPCController : MonoBehaviour
    {
        private static readonly List<NPCController> All = new();
        private Vector3 target; private Vector3 home; private float targetTimer; private float panicTimer; private float speed;
        private void OnEnable() { if (!All.Contains(this)) All.Add(this); home = transform.position; PickTarget(); }
        private void OnDisable() => All.Remove(this);
        private void Update()
        {
            targetTimer -= Time.deltaTime; panicTimer -= Time.deltaTime;
            if (targetTimer <= 0f || Vector3.Distance(transform.position, target) < 1.2f) PickTarget();
            Vector3 dir = Vector3.ProjectOnPlane(target - transform.position, Vector3.up); if (dir.sqrMagnitude < 0.1f) return; dir.Normalize();
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, 1.2f)) { targetTimer = 0f; return; }
            transform.position += dir * speed * (panicTimer > 0f ? 2.1f : 1f) * Time.deltaTime; transform.forward = Vector3.Slerp(transform.forward, dir, 8f * Time.deltaTime);
        }
        private void PickTarget()
        {
            targetTimer = Random.Range(3f, 8f); speed = Random.Range(1.1f, 1.9f); Vector2 circle = Random.insideUnitCircle * Random.Range(5f, 24f);
            target = home + new Vector3(circle.x, 0f, circle.y);
        }
        public void PanicFrom(Vector3 source) { panicTimer = Random.Range(3f, 6f); Vector3 away = Vector3.ProjectOnPlane(transform.position - source, Vector3.up).normalized; target = transform.position + away * 25f; targetTimer = panicTimer; }
        public static void AlertNearby(Vector3 source, float radius) { for (int i = All.Count - 1; i >= 0; i--) if (All[i] != null && Vector3.Distance(All[i].transform.position, source) <= radius) All[i].PanicFrom(source); }
    }
}
