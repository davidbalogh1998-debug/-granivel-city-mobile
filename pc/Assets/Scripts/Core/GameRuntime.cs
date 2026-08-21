using UnityEngine;

namespace GranivelCity
{
    public class GameRuntime : MonoBehaviour
    {
        public static GameRuntime Instance { get; private set; }
        public PlayerController Player { get; private set; }
        public WantedSystem Wanted { get; private set; }
        public MissionSystem Missions { get; private set; }
        public PCInput PCInput { get; private set; }
        public ThirdPersonCamera MainCameraRig { get; private set; }
        public MiniMapCamera MiniMap { get; private set; }
        public BudapestWorldStreamer World { get; private set; }
        public RPPlayerState RP { get; private set; }
        public int Money { get; private set; }

        private float saveTimer;
        private readonly Vector3 spawnPoint = new(0f, 30f, 0f); // Deák Ferenc tér geo origin; corrected after terrain arrives.

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 144;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            Money = PlayerPrefs.GetInt("GC_Money", 50000);

            var inputObj = new GameObject("PC Input"); inputObj.transform.SetParent(transform);
            PCInput = inputObj.AddComponent<PCInput>();

            var wantedObj = new GameObject("Wanted System"); wantedObj.transform.SetParent(transform);
            Wanted = wantedObj.AddComponent<WantedSystem>();

            var missionObj = new GameObject("Mission System"); missionObj.transform.SetParent(transform);
            Missions = missionObj.AddComponent<MissionSystem>();

            var worldObj = new GameObject("Budapest 1:1 World"); worldObj.transform.SetParent(transform);
            World = worldObj.AddComponent<BudapestWorldStreamer>();

            CreatePlayer();
            CreateLighting();
            CreateCameras();

            var hudObj = new GameObject("HUD"); hudObj.transform.SetParent(transform); hudObj.AddComponent<GameHUD>();
            World.Begin(Player.transform);
            Missions.Begin();
            CreateAmbientPopulation();
        }

        private void CreatePlayer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player"; go.transform.position = spawnPoint;
            var capsule = go.GetComponent<CapsuleCollider>(); if (capsule) Destroy(capsule);
            var cc = go.AddComponent<CharacterController>(); cc.height = 2f; cc.radius = 0.42f; cc.center = Vector3.zero;
            var renderer = go.GetComponent<Renderer>(); if (renderer) renderer.material = BudapestMaterials.Building("player-clothes");
            var health = go.AddComponent<Health>(); health.maxHealth = 100f; health.isPlayer = true;
            Player = go.AddComponent<PlayerController>();
            go.AddComponent<WeaponSystem>();
            RP = go.AddComponent<RPPlayerState>();
        }

        private void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.44f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.58f,0.63f,0.68f); RenderSettings.fogDensity = 0.00065f;
            var sunObj = new GameObject("Budapest Sun"); sunObj.transform.SetParent(transform); sunObj.transform.rotation = Quaternion.Euler(42f, -25f, 0f);
            var light = sunObj.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.25f; light.shadows = LightShadows.Soft;
            sunObj.AddComponent<DayNightCycle>();
        }

        private void CreateCameras()
        {
            var cameraObj = new GameObject("Main Camera"); cameraObj.tag = "MainCamera";
            var cam = cameraObj.AddComponent<Camera>(); cam.fieldOfView = 70f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 4000f;
            cameraObj.AddComponent<AudioListener>(); MainCameraRig = cameraObj.AddComponent<ThirdPersonCamera>(); MainCameraRig.target = Player.transform;

            var miniObj = new GameObject("MiniMap Camera");
            var miniCam = miniObj.AddComponent<Camera>(); miniCam.orthographic = true; miniCam.orthographicSize = 90f; miniCam.clearFlags = CameraClearFlags.SolidColor;
            miniCam.backgroundColor = new Color(0.04f, 0.045f, 0.05f); miniCam.depth = -2f;
            MiniMap = miniObj.AddComponent<MiniMapCamera>(); MiniMap.target = Player.transform;
        }

        private void CreateAmbientPopulation()
        {
            var go = new GameObject("Budapest Population"); go.transform.SetParent(transform);
            go.AddComponent<BudapestPopulation>();
        }

        private void Update()
        {
            saveTimer += Time.deltaTime;
            if (saveTimer >= 15f) { saveTimer = 0f; Save(); }
        }

        public void AddMoney(int amount) { Money = Mathf.Max(0, Money + amount); Save(); }
        public void RespawnPlayer()
        {
            if (Player == null) return;
            Player.ExitVehicle(true);
            float y = World != null ? World.SampleHeightAtWorld(Vector3.zero) + 1.25f : 20f;
            Player.Teleport(new Vector3(0f, y, 0f)); AddMoney(-5000); Wanted.ClearWanted();
        }
        public Transform ActiveTarget => Player != null && Player.CurrentVehicle != null ? Player.CurrentVehicle.transform : Player != null ? Player.transform : transform;
        public void Save()
        {
            PlayerPrefs.SetInt("GC_Money", Money); PlayerPrefs.SetInt("GC_Mission", Missions != null ? Missions.Stage : 0); PlayerPrefs.Save(); RP?.Save();
        }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();
    }
}
