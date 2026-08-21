using UnityEngine;

namespace GranivelCity
{
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;
        public bool isPlayer;
        public bool isPolice;
        public float Current { get; private set; }
        public float Normalized => maxHealth <= 0f ? 0f : Current / maxHealth;

        private void Awake() => Current = maxHealth;
        private void Start() => Current = maxHealth;

        public void TakeDamage(float amount, GameObject source = null)
        {
            if (Current <= 0f) return;
            Current = Mathf.Max(0f, Current - Mathf.Abs(amount));

            if (isPlayer && GameRuntime.Instance != null && GameRuntime.Instance.Wanted.Stars > 0)
                GameRuntime.Instance.Wanted.RegisterPoliceContact();

            if (Current <= 0f) Die();
        }

        public void Heal(float amount) => Current = Mathf.Min(maxHealth, Current + Mathf.Abs(amount));

        private void Die()
        {
            if (isPlayer)
            {
                Current = maxHealth;
                GameRuntime.Instance?.RespawnPlayer();
                return;
            }

            if (isPolice) GameRuntime.Instance?.Wanted.AddHeat(12f);
            Destroy(gameObject);
        }
    }
}
