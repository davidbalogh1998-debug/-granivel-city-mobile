using UnityEngine;

namespace GranivelCity
{
    public class RPPlayerState : MonoBehaviour
    {
        public int Bank { get; private set; }
        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public string Job { get; private set; }
        public int JobGrade { get; private set; }

        private float saveTimer;

        private void Awake()
        {
            Bank = PlayerPrefs.GetInt("GC_RP_Bank", 250000);
            Hunger = PlayerPrefs.GetFloat("GC_RP_Hunger", 100f);
            Thirst = PlayerPrefs.GetFloat("GC_RP_Thirst", 100f);
            Job = PlayerPrefs.GetString("GC_RP_Job", "Munkanélküli");
            JobGrade = PlayerPrefs.GetInt("GC_RP_JobGrade", 0);
        }

        private void Update()
        {
            Hunger = Mathf.Max(0f, Hunger - Time.deltaTime * 0.018f);
            Thirst = Mathf.Max(0f, Thirst - Time.deltaTime * 0.026f);
            saveTimer += Time.deltaTime;
            if (saveTimer > 30f) { saveTimer = 0f; Save(); }
        }

        public void SetJob(string job, int grade = 0) { Job = job; JobGrade = grade; Save(); }
        public bool Withdraw(int amount) { if (amount <= 0 || Bank < amount) return false; Bank -= amount; GameRuntime.Instance?.AddMoney(amount); Save(); return true; }
        public void Deposit(int amount) { if (amount <= 0 || GameRuntime.Instance == null || GameRuntime.Instance.Money < amount) return; GameRuntime.Instance.AddMoney(-amount); Bank += amount; Save(); }
        public void Eat(float value) => Hunger = Mathf.Clamp(Hunger + value, 0f, 100f);
        public void Drink(float value) => Thirst = Mathf.Clamp(Thirst + value, 0f, 100f);

        public void Save()
        {
            PlayerPrefs.SetInt("GC_RP_Bank", Bank); PlayerPrefs.SetFloat("GC_RP_Hunger", Hunger); PlayerPrefs.SetFloat("GC_RP_Thirst", Thirst);
            PlayerPrefs.SetString("GC_RP_Job", Job); PlayerPrefs.SetInt("GC_RP_JobGrade", JobGrade); PlayerPrefs.Save();
        }
    }
}
