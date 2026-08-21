using System.Collections.Generic;
using UnityEngine;

namespace GranivelCity
{
    public class GranivelGameController : MonoBehaviour
    {
        public static GranivelGameController Instance { get; private set; }

        [Header("Player")]
        public GranivelPlayerController player;
        public float playerHealth = 100f;
        public int money = 2500;

        [Header("World")]
        public Light sun;
        [Range(0f, 1f)] public float timeOfDay = 0.28f;
        public float dayLengthMinutes = 18f;

        [Header("Police")]
        [Range(0, 5)] public int wantedLevel;
        public GameObject policePrefab;
        public int maxPolice = 6;
        public float wantedCoolDownSeconds = 18f;

        readonly List<GameObject> spawnedPolice = new();
        GranivelVehicleController activeVehicle;
        float lastCrime;
        float nextPoliceSpawn;

        public int WantedLevel => wantedLevel;
        public Transform PlayerTransform => activeVehicle != null ? activeVehicle.transform : player != null ? player.transform : null;
        public float PlayerHealth => playerHealth;
        public int Money => money;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (player == null) player = FindFirstObjectByType<GranivelPlayerController>();
            if (sun == null) sun = FindFirstObjectByType<Light>();
        }

        void Update()
        {
            UpdateDayNight();
            UpdateWanted();
            UpdateCombat();
        }

        void UpdateDayNight()
        {
            float seconds = Mathf.Max(60f, dayLengthMinutes * 60f);
            timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / seconds, 1f);
            float angle = timeOfDay * 360f - 90f;
            float daylight = Mathf.Clamp01(Mathf.Sin(timeOfDay * Mathf.PI * 2f) * 0.75f + 0.35f);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(angle, 30f, 0f);
                sun.intensity = Mathf.Lerp(0.08f, 1.5f, daylight);
            }
            RenderSettings.ambientIntensity = Mathf.Lerp(0.18f, 1.05f, daylight);
            RenderSettings.fogColor = Color.Lerp(new Color(0.025f, 0.04f, 0.09f), new Color(0.58f, 0.72f, 0.82f), daylight);
        }

        void UpdateWanted()
        {
            if (wantedLevel <= 0) return;
            if (Time.time >= nextPoliceSpawn && spawnedPolice.Count < Mathf.Min(maxPolice, wantedLevel + 1))
            {
                SpawnPolice();
                nextPoliceSpawn = Time.time + Mathf.Max(2.5f, 8f - wantedLevel);
            }

            bool policeNear = false;
            var target = PlayerTransform;
            if (target != null)
            {
                for (int i = spawnedPolice.Count - 1; i >= 0; i--)
                {
                    if (spawnedPolice[i] == null) { spawnedPolice.RemoveAt(i); continue; }
                    if (Vector3.Distance(spawnedPolice[i].transform.position, target.position) < 55f) policeNear = true;
                }
            }
            if (!policeNear && Time.time - lastCrime > wantedCoolDownSeconds)
            {
                wantedLevel = Mathf.Max(0, wantedLevel - 1);
                lastCrime = Time.time;
            }
        }

        void SpawnPolice()
        {
            var target = PlayerTransform;
            if (target == null) return;
            Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(35f, 55f);
            Vector3 position = target.position + new Vector3(ring.x, 0.3f, ring.y);
            GameObject policeObject;
            if (policePrefab != null) policeObject = Instantiate(policePrefab, position, Quaternion.LookRotation(target.position - position));
            else
            {
                policeObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                policeObject.name = "Police NPC";
                policeObject.transform.position = position;
                policeObject.transform.localScale = new Vector3(0.75f, 1f, 0.75f);
                var renderer = policeObject.GetComponent<Renderer>();
                renderer.material.color = new Color(0.05f, 0.12f, 0.28f);
            }
            var agent = policeObject.GetComponent<GranivelNpcAgent>() ?? policeObject.AddComponent<GranivelNpcAgent>();
            agent.police = true;
            spawnedPolice.Add(policeObject);
        }

        void UpdateCombat()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = new(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, 120f))
            {
                var npc = hit.collider.GetComponentInParent<GranivelNpcAgent>();
                if (npc != null)
                {
                    npc.Hit(100f);
                    AddWanted(1);
                }
            }
        }

        public void AddWanted(int amount)
        {
            wantedLevel = Mathf.Clamp(wantedLevel + amount, 0, 5);
            lastCrime = Time.time;
        }

        public void ClearWanted()
        {
            wantedLevel = 0;
            lastCrime = Time.time;
        }

        public void DamagePlayer(float amount)
        {
            playerHealth = Mathf.Max(0f, playerHealth - amount);
            if (playerHealth > 0f) return;
            playerHealth = 100f;
            money = Mathf.Max(0, money - 250);
            ClearWanted();
            if (player != null)
            {
                if (player.IsDriving) player.ExitVehicle();
                player.transform.position = Vector3.up * 1.2f;
            }
        }

        public void AddMoney(int amount) => money = Mathf.Max(0, money + amount);
        public void SetActiveVehicle(GranivelVehicleController vehicle) => activeVehicle = vehicle;

        public void OnVehicleEntered(GranivelVehicleController vehicle)
        {
            if (vehicle != null && vehicle.name.ToLowerInvariant().Contains("police")) AddWanted(2);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.024f), fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(22, 18, 420, 42), $"$ {money:N0}", style);
            GUI.Label(new Rect(22, 52, 420, 42), $"HP {Mathf.CeilToInt(playerHealth)}", style);
            var stars = new string('★', wantedLevel) + new string('☆', 5 - wantedLevel);
            GUI.Label(new Rect(Screen.width - 210, 18, 200, 42), stars, style);
            if (activeVehicle != null)
                GUI.Label(new Rect(Screen.width / 2f - 100, Screen.height - 68, 260, 42), $"{Mathf.RoundToInt(activeVehicle.SpeedKph)} km/h", style);
        }
    }
}
