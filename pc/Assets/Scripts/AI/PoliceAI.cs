using UnityEngine;

namespace GranivelCity
{
    public class PoliceAI : MonoBehaviour
    {
        public float moveSpeed = 4.8f;
        private float attackCooldown;

        private void Update()
        {
            var runtime = GameRuntime.Instance;
            if (runtime == null || runtime.Wanted.Stars <= 0 || runtime.Player == null)
            {
                Destroy(gameObject);
                return;
            }

            Transform target = runtime.ActiveTarget;
            Vector3 dir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
            float dist = dir.magnitude;
            if (dist > 0.1f)
            {
                dir /= dist;
                if (!Physics.Raycast(transform.position + Vector3.up * 0.6f, dir, 1.1f))
                    transform.position += dir * moveSpeed * Time.deltaTime;
                transform.forward = Vector3.Slerp(transform.forward, dir, 10f * Time.deltaTime);
            }

            attackCooldown -= Time.deltaTime;
            if (dist < 2.2f && attackCooldown <= 0f)
            {
                attackCooldown = 0.8f;
                runtime.Player.GetComponent<Health>()?.TakeDamage(8f, gameObject);
                runtime.Wanted.RegisterPoliceContact();
            }
        }
    }
}
