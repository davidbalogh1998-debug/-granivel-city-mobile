using UnityEngine;

namespace GranivelCity
{
    public class GranivelNpcAgent : MonoBehaviour
    {
        public float walkSpeed = 1.5f;
        public float fleeSpeed = 4.2f;
        public float roamRadius = 32f;
        public bool police;
        public float policeSpeed = 5.5f;

        Vector3 origin;
        Vector3 target;
        float retargetAt;
        bool alive = true;

        void Start()
        {
            origin = transform.position;
            PickTarget();
        }

        void Update()
        {
            if (!alive) return;
            var game = GranivelGameController.Instance;
            Vector3 destination = target;
            float speed = walkSpeed;

            if (game != null && game.PlayerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, game.PlayerTransform.position);
                if (police && game.WantedLevel > 0)
                {
                    destination = game.PlayerTransform.position;
                    speed = policeSpeed + game.WantedLevel * 0.4f;
                    if (distance < 1.8f) game.DamagePlayer(15f * Time.deltaTime);
                }
                else if (game.WantedLevel > 0 && distance < 18f)
                {
                    destination = transform.position + (transform.position - game.PlayerTransform.position).normalized * 25f;
                    speed = fleeSpeed;
                }
            }

            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1.2f || Time.time > retargetAt)
            {
                PickTarget();
                return;
            }

            direction.Normalize();
            if (!Physics.Raycast(transform.position + Vector3.up * 0.6f, direction, 0.8f))
            {
                transform.position += direction * speed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
            }
            else PickTarget();
        }

        void PickTarget()
        {
            Vector2 circle = Random.insideUnitCircle * roamRadius;
            target = origin + new Vector3(circle.x, 0f, circle.y);
            retargetAt = Time.time + Random.Range(6f, 16f);
        }

        public void Hit(float damage)
        {
            if (!alive) return;
            alive = false;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);
            GranivelGameController.Instance?.AddWanted(police ? 2 : 1);
            GranivelGameController.Instance?.AddMoney(police ? 0 : 50);
        }
    }
}
