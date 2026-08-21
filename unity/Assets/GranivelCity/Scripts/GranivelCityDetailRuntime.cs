using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GranivelCity
{
    /// <summary>
    /// Runtime detail pass for generated city scenes. It adds the small-scale objects that
    /// make a road network read as a real city: traffic lights, zebra crossings, bus stops,
    /// benches, bins, hydrants, street-name plates and parking meters. Runs automatically
    /// when GranivelCity.unity is loaded, so generated scenes stay reproducible.
    /// </summary>
    public static class GranivelCityDetailRuntime
    {
        static Material roadWhite, roadYellow, metal, glass, bench, signBlue, hydrant;
        static readonly List<TrafficSignal> signals = new();
        static bool installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (installed) return;
            installed = true;
            SceneManager.sceneLoaded += (_, __) => { if (Object.FindFirstObjectByType<GranivelGameController>() != null) Build(); };
            if (Object.FindFirstObjectByType<GranivelGameController>() != null) Build();
        }

        static void Build()
        {
            if (GameObject.Find("CITY — Runtime Detail Pass") != null) return;
            InitMaterials();
            var root = new GameObject("CITY — Runtime Detail Pass");

            // 7x7 road intersections. Alternate signal phase between axes.
            for (int x = -3; x <= 3; x++)
            for (int z = -3; z <= 3; z++)
            {
                Vector3 center = new(x * 62f, .13f, z * 62f);
                BuildCrosswalk(center, root.transform);
                BuildTrafficLight(center + new Vector3(9.6f, 0, 9.6f), 180f, (x + z) % 2 == 0, root.transform);
                BuildTrafficLight(center + new Vector3(-9.6f, 0, -9.6f), 0f, (x + z) % 2 == 0, root.transform);
                if ((x + z) % 2 == 0)
                    BuildStreetName(center + new Vector3(10.5f, 0, -10.5f), $"GRANIVEL {Mathf.Abs(x) + 1}. UTCA", root.transform);
            }

            // Bus stops, seating and bins along the main corridors.
            for (int i = -3; i <= 3; i++)
            {
                float p = i * 62f;
                BuildBusStop(new Vector3(p + 12.2f, .2f, 31f), 0f, root.transform);
                BuildBench(new Vector3(p - 12.5f, .2f, -31f), 180f, root.transform);
                BuildBin(new Vector3(p - 10.2f, .2f, -31f), root.transform);
                BuildHydrant(new Vector3(31f, .2f, p + 11.8f), root.transform);
                BuildParkingMeter(new Vector3(-31f, .2f, p - 11.8f), root.transform);
            }

            var driver = root.AddComponent<TrafficSignalClock>();
            driver.signals = signals;
        }

        static void InitMaterials()
        {
            roadWhite = Make("RuntimeRoadWhite", new Color(.92f, .92f, .88f), 0f, .8f);
            roadYellow = Make("RuntimeRoadYellow", new Color(.92f, .68f, .08f), 0f, .75f);
            metal = Make("RuntimeStreetMetal", new Color(.11f, .13f, .15f), .7f, .42f);
            glass = Make("RuntimeGlass", new Color(.25f, .50f, .62f, .45f), .15f, .15f, true);
            bench = Make("RuntimeBench", new Color(.32f, .17f, .07f), 0f, .82f);
            signBlue = Make("RuntimeSignBlue", new Color(.03f, .19f, .42f), .15f, .42f);
            hydrant = Make("RuntimeHydrant", new Color(.72f, .07f, .05f), .55f, .35f);
        }

        static Material Make(string name, Color color, float metallic, float roughness, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = name, color = color };
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 1f - roughness);
            if (transparent)
            {
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);
                m.renderQueue = 3000;
            }
            return m;
        }

        static void BuildCrosswalk(Vector3 center, Transform root)
        {
            for (int i = -4; i <= 4; i++)
            {
                float o = i * 1.35f;
                Box("Crosswalk", center + new Vector3(o, .07f, 7.2f), new Vector3(.68f, .035f, 3.4f), roadWhite, root);
                Box("Crosswalk", center + new Vector3(7.2f, .071f, o), new Vector3(3.4f, .035f, .68f), roadWhite, root);
            }
            Box("Stop Line", center + new Vector3(0, .072f, 9.2f), new Vector3(8.1f, .035f, .22f), roadWhite, root);
            Box("Stop Line", center + new Vector3(9.2f, .073f, 0), new Vector3(.22f, .035f, 8.1f), roadWhite, root);
        }

        static void BuildTrafficLight(Vector3 p, float yaw, bool phaseA, Transform root)
        {
            var go = new GameObject("Traffic Light");
            go.transform.SetParent(root);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            Cylinder("Pole", new Vector3(0, 2.45f, 0), new Vector3(.11f, 2.45f, .11f), metal, go.transform);
            Box("Arm", new Vector3(1.65f, 4.75f, 0), new Vector3(3.2f, .13f, .13f), metal, go.transform);
            Box("Signal Head", new Vector3(3.05f, 4.42f, 0), new Vector3(.58f, 1.55f, .52f), metal, go.transform);
            var red = LampSphere(new Vector3(3.05f, 4.88f, -.28f), Color.red, go.transform);
            var amber = LampSphere(new Vector3(3.05f, 4.42f, -.28f), new Color(1f, .55f, .02f), go.transform);
            var green = LampSphere(new Vector3(3.05f, 3.96f, -.28f), new Color(.05f, .85f, .17f), go.transform);
            signals.Add(new TrafficSignal { red = red, amber = amber, green = green, phaseA = phaseA });
        }

        static Renderer LampSphere(Vector3 local, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Signal Lamp";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.one * .31f;
            Object.Destroy(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.material = Make("Signal " + color, color * .22f, .05f, .25f);
            return renderer;
        }

        static void BuildStreetName(Vector3 p, string label, Transform root)
        {
            var pole = new GameObject("Street Name — " + label);
            pole.transform.SetParent(root);
            pole.transform.position = p;
            Cylinder("Pole", new Vector3(0, 1.65f, 0), new Vector3(.055f, 1.65f, .055f), metal, pole.transform);
            Box("Plate", new Vector3(0, 3.1f, 0), new Vector3(3.3f, .52f, .08f), signBlue, pole.transform);
            var text = new GameObject("Text");
            text.transform.SetParent(pole.transform, false);
            text.transform.localPosition = new Vector3(0, 3.1f, -.051f);
            text.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            var tm = text.AddComponent<TextMesh>();
            tm.text = label;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = .022f;
            tm.color = Color.white;
        }

        static void BuildBusStop(Vector3 p, float yaw, Transform root)
        {
            var stop = new GameObject("Bus Stop");
            stop.transform.SetParent(root);
            stop.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            Box("Roof", new Vector3(0, 2.45f, 0), new Vector3(4.6f, .15f, 1.75f), metal, stop.transform);
            Box("Glass Back", new Vector3(0, 1.25f, .78f), new Vector3(4.4f, 2.35f, .08f), glass, stop.transform);
            Cylinder("Post L", new Vector3(-2.05f, 1.25f, .72f), new Vector3(.07f, 1.25f, .07f), metal, stop.transform);
            Cylinder("Post R", new Vector3(2.05f, 1.25f, .72f), new Vector3(.07f, 1.25f, .07f), metal, stop.transform);
            Box("Seat", new Vector3(0, .62f, .35f), new Vector3(2.7f, .16f, .62f), bench, stop.transform);
            Box("BUS Plate", new Vector3(2.22f, 2.35f, 0), new Vector3(.55f, .65f, .06f), signBlue, stop.transform);
        }

        static void BuildBench(Vector3 p, float yaw, Transform root)
        {
            var b = new GameObject("Bench");
            b.transform.SetParent(root);
            b.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            Box("Seat", new Vector3(0,.58f,0), new Vector3(2.2f,.13f,.58f), bench,b.transform);
            Box("Back", new Vector3(0,1.05f,.25f), new Vector3(2.2f,.8f,.12f),bench,b.transform);
            Cylinder("Leg",new Vector3(-.78f,.3f,0),new Vector3(.08f,.3f,.08f),metal,b.transform);
            Cylinder("Leg",new Vector3(.78f,.3f,0),new Vector3(.08f,.3f,.08f),metal,b.transform);
        }

        static void BuildBin(Vector3 p, Transform root)
        {
            var g = new GameObject("Street Bin"); g.transform.SetParent(root); g.transform.position=p;
            Cylinder("Bin",new Vector3(0,.52f,0),new Vector3(.34f,.52f,.34f),metal,g.transform);
        }

        static void BuildHydrant(Vector3 p, Transform root)
        {
            var g=new GameObject("Fire Hydrant");g.transform.SetParent(root);g.transform.position=p;
            Cylinder("Body",new Vector3(0,.45f,0),new Vector3(.25f,.45f,.25f),hydrant,g.transform);
            Cylinder("Cap",new Vector3(0,.92f,0),new Vector3(.31f,.08f,.31f),hydrant,g.transform);
        }

        static void BuildParkingMeter(Vector3 p, Transform root)
        {
            var g=new GameObject("Parking Meter");g.transform.SetParent(root);g.transform.position=p;
            Cylinder("Pole",new Vector3(0,.8f,0),new Vector3(.045f,.8f,.045f),metal,g.transform);
            Box("Meter",new Vector3(0,1.55f,0),new Vector3(.34f,.48f,.22f),metal,g.transform);
        }

        static GameObject Box(string name, Vector3 local, Vector3 scale, Material material, Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }

        static GameObject Cylinder(string name, Vector3 local, Vector3 scale, Material material, Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }

        public sealed class TrafficSignal
        {
            public Renderer red, amber, green;
            public bool phaseA;
        }

        sealed class TrafficSignalClock : MonoBehaviour
        {
            public List<TrafficSignal> signals;
            float cycle;

            void Update()
            {
                cycle = Mathf.Repeat(Time.time, 24f);
                foreach (var signal in signals)
                {
                    bool green = signal.phaseA ? cycle < 10f : cycle >= 12f && cycle < 22f;
                    bool amber = signal.phaseA ? cycle >= 10f && cycle < 12f : cycle >= 22f;
                    Set(signal.green, green);
                    Set(signal.amber, amber);
                    Set(signal.red, !green && !amber);
                }
            }

            static void Set(Renderer renderer, bool on)
            {
                if (renderer == null) return;
                Color baseColor = renderer.material.color;
                Color glow = on ? Color.Lerp(baseColor, Color.white, .20f) * 4f : baseColor * .18f;
                if (renderer.material.HasProperty("_EmissionColor"))
                {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", glow);
                }
                renderer.material.color = on ? Color.Lerp(baseColor, Color.white, .08f) : baseColor;
            }
        }
    }
}
