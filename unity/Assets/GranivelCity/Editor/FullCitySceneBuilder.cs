#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GranivelCity.Editor
{
    public static class FullCitySceneBuilder
    {
        const string ThirdParty = "Assets/ThirdParty/CC0";
        const string ScenePath = "Assets/Scenes/GranivelCity.unity";
        static readonly string[] ShopNames = { "NOVA MARKET", "CITY CAFÉ", "GRANIVEL MOTORS", "24/7 SHOP", "URBAN GYM", "PIZZA POINT", "TECH HUB", "FRESH MART", "BARBER 21", "CENTRAL PHARMACY", "RIVER HOTEL", "METRO FASHION" };

        sealed class Library
        {
            public readonly List<GameObject> buildings = new();
            public readonly List<GameObject> vehicles = new();
            public readonly List<GameObject> people = new();
            public readonly List<GameObject> nature = new();
            public readonly List<GameObject> lamps = new();
            public readonly List<GameObject> signs = new();
            public readonly List<GameObject> props = new();
        }

        [MenuItem("Granivel City/Build Full City Scene")]
        public static void BuildFullCity()
        {
            EnsureFolders();
            var library = ScanLibrary();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UnityEngine.Random.InitState(260821);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0016f;
            RenderSettings.fogColor = new Color(0.58f, 0.71f, 0.80f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.64f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.34f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.14f, 0.11f);

            var mats = CreateMaterials();
            BuildGroundAndRoads(mats);
            BuildRiverAndBridges(mats);
            BuildBuildings(library, mats);
            BuildStreetFurniture(library, mats);
            BuildNature(library, mats);
            BuildVehicles(library, mats);
            var player = BuildCharacters(library, mats);
            BuildLightingAndSystems(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"Granivel City generated. Assets scanned — buildings {library.buildings.Count}, vehicles {library.vehicles.Count}, people {library.people.Count}, nature {library.nature.Count}, lamps {library.lamps.Count}, signs {library.signs.Count}.");
        }

        [MenuItem("Granivel City/Rescan CC0 Asset Library")]
        public static void Rescan() => ScanLibrary(true);

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/GranivelCity/Generated"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/GranivelCity")) AssetDatabase.CreateFolder("Assets", "GranivelCity");
                AssetDatabase.CreateFolder("Assets/GranivelCity", "Generated");
            }
        }

        static Library ScanLibrary(bool log = false)
        {
            var lib = new Library();
            if (!AssetDatabase.IsValidFolder(ThirdParty))
            {
                Debug.LogWarning($"{ThirdParty} is missing. The builder will create a complete fallback city with generated geometry. Run the Full City asset workflow first for the CC0 model library.");
                return lib;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { ThirdParty }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;
                string n = (asset.name + " " + path).ToLowerInvariant();
                if (Has(n, "building", "house", "office", "shop", "store", "hotel", "factory", "industrial", "commercial", "apartment")) lib.buildings.Add(asset);
                if (!Has(n, "debris", "wheel", "door", "bumper") && Has(n, "car", "sedan", "suv", "taxi", "truck", "van", "ambulance", "police", "hatchback", "vehicle")) lib.vehicles.Add(asset);
                if (Has(n, "character", "person", "human", "civilian", "man", "woman", "male", "female")) lib.people.Add(asset);
                if (Has(n, "tree", "bush", "plant", "grass", "palm", "shrub")) lib.nature.Add(asset);
                if (Has(n, "lamp", "streetlight", "lightpost", "light_post")) lib.lamps.Add(asset);
                if (Has(n, "sign", "traffic", "stop", "speed")) lib.signs.Add(asset);
                if (Has(n, "bench", "trash", "bin", "hydrant", "bollard", "barrier", "mailbox", "cone")) lib.props.Add(asset);
            }
            if (log) Debug.Log($"CC0 scan: {lib.buildings.Count} buildings, {lib.vehicles.Count} vehicles, {lib.people.Count} people, {lib.nature.Count} nature, {lib.lamps.Count} lamps, {lib.signs.Count} signs, {lib.props.Count} props.");
            return lib;
        }

        static bool Has(string value, params string[] terms) => terms.Any(value.Contains);

        static Dictionary<string, Material> CreateMaterials()
        {
            return new Dictionary<string, Material>
            {
                ["grass"] = Material("Grass", new Color(0.20f,0.38f,0.16f), .95f),
                ["asphalt"] = Material("Asphalt", new Color(0.095f,0.10f,0.11f), .92f),
                ["concrete"] = Material("Concrete", new Color(0.48f,0.49f,0.47f), .88f),
                ["line"] = Material("RoadLine", new Color(0.92f,0.84f,0.55f), .65f),
                ["water"] = Material("RiverWater", new Color(0.04f,0.32f,0.54f), .20f, .20f),
                ["metal"] = Material("StreetMetal", new Color(0.11f,0.13f,0.15f), .38f, .72f),
                ["glass"] = Material("Glass", new Color(0.30f,0.54f,0.67f), .14f, .20f),
                ["wood"] = Material("Wood", new Color(0.30f,0.19f,0.12f), .90f)
            };
        }

        static Material Material(string name, Color color, float smoothness, float metallic = .02f)
        {
            string path = $"Assets/GranivelCity/Generated/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name, color = color };
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f - smoothness);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f - smoothness);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        static void BuildGroundAndRoads(Dictionary<string, Material> m)
        {
            var root = new GameObject("WORLD — Roads & Sidewalks");
            CreateCube("Ground", new Vector3(0,-.35f,0), new Vector3(540,.6f,540), m["grass"], root.transform);
            const float block = 62f, road = 18f;
            for (int i = -3; i <= 3; i++)
            {
                float p = i * block;
                CreateCube($"Road_NS_{i}", new Vector3(p,.02f,0), new Vector3(road,.16f,540), m["asphalt"], root.transform);
                CreateCube($"Road_EW_{i}", new Vector3(0,.025f,p), new Vector3(540,.16f,road), m["asphalt"], root.transform);
                for (int k = -6; k <= 6; k++)
                {
                    CreateCube("Lane", new Vector3(p,.115f,k*34f), new Vector3(.13f,.025f,4.5f), m["line"], root.transform);
                    CreateCube("Lane", new Vector3(k*34f,.12f,p), new Vector3(4.5f,.025f,.13f), m["line"], root.transform);
                }
            }
            for (int x = -3; x < 3; x++) for (int z = -3; z < 3; z++)
            {
                Vector3 center = new((x+.5f)*block,.16f,(z+.5f)*block);
                CreateCube("SidewalkBlock", center, new Vector3(block-road+4f,.28f,block-road+4f), m["concrete"], root.transform);
            }
        }

        static void BuildRiverAndBridges(Dictionary<string, Material> m)
        {
            var root = new GameObject("WORLD — River & Bridges");
            const float riverX = 224f;
            CreateCube("River", new Vector3(riverX,-.05f,0), new Vector3(44,.12f,540), m["water"], root.transform);
            foreach (float z in new[] {-124f, 0f, 124f})
            {
                CreateCube("BridgeRoad", new Vector3(riverX,.13f,z), new Vector3(48,.42f,19f), m["asphalt"], root.transform);
                CreateCube("BridgeWalk", new Vector3(riverX,.38f,z-11f), new Vector3(48,.55f,3f), m["concrete"], root.transform);
                CreateCube("BridgeWalk", new Vector3(riverX,.38f,z+11f), new Vector3(48,.55f,3f), m["concrete"], root.transform);
                for (int i=-5;i<=5;i++)
                {
                    CreateCube("Railing", new Vector3(riverX+i*4.4f,1.05f,z-12.5f), new Vector3(.12f,1.6f,.12f), m["metal"], root.transform);
                    CreateCube("Railing", new Vector3(riverX+i*4.4f,1.05f,z+12.5f), new Vector3(.12f,1.6f,.12f), m["metal"], root.transform);
                }
            }
        }

        static void BuildBuildings(Library lib, Dictionary<string, Material> m)
        {
            var root = new GameObject("CITY — Buildings & Shops");
            int index = 0;
            for (int x=-3;x<3;x++) for (int z=-3;z<3;z++)
            {
                Vector3 center = new((x+.5f)*62f,.32f,(z+.5f)*62f);
                if (Mathf.Abs(center.x-224f)<38f) continue;
                int count = 1 + Mathf.Abs((x*7+z*3)%3);
                for (int b=0;b<count;b++)
                {
                    float angle = count == 1 ? 0 : b/(float)count*Mathf.PI*2f;
                    Vector3 pos = center + new Vector3(Mathf.Cos(angle)*10f,0,Mathf.Sin(angle)*10f);
                    GameObject go;
                    if (lib.buildings.Count > 0)
                    {
                        go = InstantiateAsset(lib.buildings[(index*13+b*5)%lib.buildings.Count], pos, Quaternion.Euler(0, UnityEngine.Random.Range(0,4)*90f,0), root.transform);
                        FitToMaxDimension(go, count == 1 ? 34f : 17f);
                    }
                    else
                    {
                        float h = 14f + (index%6)*5f;
                        go = CreateCube("Fallback Building", pos+Vector3.up*h/2f, new Vector3(count==1?31f:14f,h,count==1?30f:14f), Material("Facade"+(index%6), Color.HSVToRGB((index*.11f)%1f,.18f,.72f),.82f), root.transform);
                        AddWindows(go, m["glass"]);
                    }
                    if (index % 3 == 0) AddShopSign(go, ShopNames[index%ShopNames.Length]);
                    index++;
                }
            }
        }

        static void AddWindows(GameObject building, Material glass)
        {
            var b = WorldBounds(building);
            for (float y=b.min.y+3f;y<b.max.y-2f;y+=4f) for(float x=b.min.x+2f;x<b.max.x-1f;x+=3.5f)
                CreateCube("Window", new Vector3(x,y,b.max.z+.03f), new Vector3(1.8f,1.9f,.08f), glass, building.transform.parent);
        }

        static void AddShopSign(GameObject building, string text)
        {
            var bounds = WorldBounds(building);
            var sign = new GameObject("Shop Sign — "+text);
            sign.transform.position = new Vector3(bounds.center.x, Mathf.Max(bounds.min.y+3.2f,1.8f), bounds.max.z+.25f);
            sign.transform.SetParent(building.transform.parent);
            var mesh = sign.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = .06f;
            mesh.color = Color.white;
            var back = CreateCube("SignBack", sign.transform.position+Vector3.forward*.08f, new Vector3(Mathf.Clamp(text.Length*.38f,4f,9f),1.5f,.16f), Material("ShopSign",new Color(.025f,.055f,.095f),.35f,.12f), building.transform.parent);
            back.transform.SetSiblingIndex(Mathf.Max(0,sign.transform.GetSiblingIndex()-1));
        }

        static void BuildStreetFurniture(Library lib, Dictionary<string, Material> m)
        {
            var root = new GameObject("CITY — Lamps, Signs & Street Props");
            int index=0;
            for(int r=-3;r<=3;r++) for(int k=-3;k<=3;k++)
            {
                float p=r*62f, q=k*62f+22f;
                PlaceStreetAsset(lib.lamps, new Vector3(p+11f,.25f,q), root.transform, 6f, () => FallbackLamp(new Vector3(p+11f,.25f,q),m,root.transform));
                PlaceStreetAsset(lib.signs, new Vector3(p+12f,.25f,q+6f), root.transform, 3f, () => FallbackSign(new Vector3(p+12f,.25f,q+6f),m,root.transform,30+(Mathf.Abs(r+k)%3)*10));
                if(lib.props.Count>0 && index%2==0) PlaceStreetAsset(lib.props,new Vector3(q,.25f,p-11f),root.transform,2f,null);
                index++;
            }
        }

        static void PlaceStreetAsset(List<GameObject> assets, Vector3 pos, Transform root, float targetHeight, Action fallback)
        {
            if(assets.Count==0){fallback?.Invoke();return;}
            var go=InstantiateAsset(assets[UnityEngine.Random.Range(0,assets.Count)],pos,Quaternion.Euler(0,UnityEngine.Random.Range(0,4)*90,0),root);
            FitToHeight(go,targetHeight);
        }

        static void FallbackLamp(Vector3 p, Dictionary<string,Material> m, Transform root)
        {
            CreateCylinder("Street Lamp",p+Vector3.up*3f,new Vector3(.16f,3f,.16f),m["metal"],root);
            CreateCube("Lamp Arm",p+new Vector3(.9f,5.75f,0),new Vector3(1.8f,.12f,.12f),m["metal"],root);
            var lightGo=GameObject.CreatePrimitive(PrimitiveType.Sphere);lightGo.name="Lamp Bulb";lightGo.transform.SetParent(root);lightGo.transform.position=p+new Vector3(1.75f,5.62f,0);lightGo.transform.localScale=Vector3.one*.28f;
            var light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.range=13f;light.intensity=2.2f;light.color=new Color(1f,.78f,.48f);light.shadows=LightShadows.None;
        }

        static void FallbackSign(Vector3 p, Dictionary<string,Material> m, Transform root, int speed)
        {
            CreateCylinder("Traffic Sign Pole",p+Vector3.up*1.35f,new Vector3(.08f,1.35f,.08f),m["metal"],root);
            var go=new GameObject("Speed Sign "+speed);go.transform.SetParent(root);go.transform.position=p+Vector3.up*2.45f;var tm=go.AddComponent<TextMesh>();tm.text=speed.ToString();tm.fontSize=80;tm.characterSize=.018f;tm.anchor=TextAnchor.MiddleCenter;tm.color=Color.white;
        }

        static void BuildNature(Library lib, Dictionary<string, Material> m)
        {
            var root=new GameObject("CITY — Vegetation");
            for(int i=0;i<110;i++)
            {
                Vector3 p=new(UnityEngine.Random.Range(-245f,245f),.2f,UnityEngine.Random.Range(-245f,245f));
                if(NearRoad(p,15f)||Mathf.Abs(p.x-224f)<28f)continue;
                if(lib.nature.Count>0){var go=InstantiateAsset(lib.nature[UnityEngine.Random.Range(0,lib.nature.Count)],p,Quaternion.Euler(0,UnityEngine.Random.Range(0,360),0),root.transform);FitToHeight(go,UnityEngine.Random.Range(3.5f,7f));}
                else {CreateCylinder("Tree Trunk",p+Vector3.up*1.5f,new Vector3(.3f,1.5f,.3f),m["wood"],root.transform);var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.name="Tree Crown";crown.transform.SetParent(root.transform);crown.transform.position=p+Vector3.up*4f;crown.transform.localScale=new Vector3(3.2f,3.7f,3.2f);crown.GetComponent<Renderer>().sharedMaterial=m["grass"];}
            }
        }

        static void BuildVehicles(Library lib, Dictionary<string, Material> m)
        {
            var root=new GameObject("TRAFFIC — Vehicles");
            for(int i=0;i<34;i++)
            {
                bool vertical=i%2==0;int lane=(i%7)-3;float along=-220f+(i*29f)%440f;Vector3 p=vertical?new Vector3(lane*62f-3f,.65f,along):new Vector3(along,.65f,lane*62f+3f);Quaternion r=Quaternion.Euler(0,vertical?0:90,0);
                GameObject go;
                if(lib.vehicles.Count>0){go=InstantiateAsset(lib.vehicles[(i*7)%lib.vehicles.Count],p,r,root.transform);FitToMaxDimension(go,5.2f);}
                else{go=CreateCube("Fallback Car",p,new Vector3(2.1f,1.35f,4.7f),Material("Vehicle"+(i%6),Color.HSVToRGB(i/6f,.65f,.6f),.25f,.5f),root.transform);go.transform.rotation=r;}
                EnsurePhysics(go);
                var vehicle=go.GetComponent<GranivelVehicleController>()??go.AddComponent<GranivelVehicleController>();vehicle.trafficVehicle=i>5;vehicle.trafficSpeed=8f+(i%5);
            }
        }

        static GranivelPlayerController BuildCharacters(Library lib, Dictionary<string, Material> m)
        {
            var root=new GameObject("PEOPLE — Player & NPCs");
            GameObject playerGo;
            if(lib.people.Count>0){playerGo=InstantiateAsset(lib.people[0],new Vector3(-15f,1f,18f),Quaternion.identity,root.transform);FitToHeight(playerGo,1.8f);}
            else{playerGo=GameObject.CreatePrimitive(PrimitiveType.Capsule);playerGo.name="Player";playerGo.transform.SetParent(root.transform);playerGo.transform.position=new Vector3(-15f,1f,18f);playerGo.GetComponent<Renderer>().sharedMaterial=Material("Player",new Color(.04f,.18f,.34f),.82f);UnityEngine.Object.DestroyImmediate(playerGo.GetComponent<Collider>());}
            var cc=playerGo.GetComponent<CharacterController>()??playerGo.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.36f;cc.center=new Vector3(0,.9f,0);
            var player=playerGo.GetComponent<GranivelPlayerController>()??playerGo.AddComponent<GranivelPlayerController>();
            for(int i=0;i<52;i++)
            {
                Vector3 p=RandomSidewalkPoint(i);GameObject npc;
                if(lib.people.Count>0){npc=InstantiateAsset(lib.people[(i*5+1)%lib.people.Count],p,Quaternion.Euler(0,UnityEngine.Random.Range(0,360),0),root.transform);FitToHeight(npc,1.75f+UnityEngine.Random.Range(-.08f,.12f));}
                else{npc=GameObject.CreatePrimitive(PrimitiveType.Capsule);npc.name="Civilian NPC";npc.transform.SetParent(root.transform);npc.transform.position=p+Vector3.up;var rr=npc.GetComponent<Renderer>();rr.sharedMaterial=Material("NPC"+(i%8),Color.HSVToRGB((i*.13f)%1f,.45f,.62f),.85f);}
                if(npc.GetComponent<Collider>()==null)npc.AddComponent<CapsuleCollider>();
                npc.GetComponent<GranivelNpcAgent>()??npc.AddComponent<GranivelNpcAgent>();
            }
            return player;
        }

        static void BuildLightingAndSystems(GranivelPlayerController player)
        {
            var sunGo=new GameObject("Sun");var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.35f;sun.color=new Color(1f,.91f,.76f);sun.shadows=LightShadows.Soft;sunGo.transform.rotation=Quaternion.Euler(46f,-28f,0);
            var systems=new GameObject("Granivel Game Systems");var game=systems.AddComponent<GranivelGameController>();game.player=player;game.sun=sun;
            var cameraGo=new GameObject("Main Camera");cameraGo.tag="MainCamera";var cam=cameraGo.AddComponent<Camera>();cam.fieldOfView=62f;cam.nearClipPlane=.08f;cam.farClipPlane=1100f;cameraGo.AddComponent<AudioListener>();cameraGo.AddComponent<GranivelThirdPersonCamera>();cameraGo.transform.position=player.transform.position+new Vector3(0,5,-8);
        }

        static GameObject InstantiateAsset(GameObject asset, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject go;
            try{go=(GameObject)PrefabUtility.InstantiatePrefab(asset);}
            catch{go=UnityEngine.Object.Instantiate(asset);}
            go.name=asset.name;go.transform.SetParent(parent);go.transform.position=position;go.transform.rotation=rotation;go.SetActive(true);return go;
        }

        static void EnsurePhysics(GameObject go)
        {
            if(go.GetComponentInChildren<Collider>()==null){var box=go.AddComponent<BoxCollider>();var b=WorldBounds(go);box.center=go.transform.InverseTransformPoint(b.center);Vector3 size=go.transform.InverseTransformVector(b.size);box.size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));}
            var rb=go.GetComponent<Rigidbody>()??go.AddComponent<Rigidbody>();rb.mass=1200f;rb.interpolation=RigidbodyInterpolation.Interpolate;
        }

        static void FitToHeight(GameObject go,float height){var b=WorldBounds(go);if(b.size.y>.001f)go.transform.localScale*=height/b.size.y;Ground(go);}
        static void FitToMaxDimension(GameObject go,float size){var b=WorldBounds(go);float max=Mathf.Max(b.size.x,b.size.y,b.size.z);if(max>.001f)go.transform.localScale*=size/max;Ground(go);}
        static void Ground(GameObject go){var b=WorldBounds(go);go.transform.position+=Vector3.up*(.25f-b.min.y);}
        static Bounds WorldBounds(GameObject go){var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return new Bounds(go.transform.position,Vector3.one);Bounds b=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);return b;}
        static bool NearRoad(Vector3 p,float distance){for(int i=-3;i<=3;i++)if(Mathf.Abs(p.x-i*62f)<distance||Mathf.Abs(p.z-i*62f)<distance)return true;return false;}
        static Vector3 RandomSidewalkPoint(int i){int road=(i%7)-3;float along=-215f+(i*37f)%430f;return i%2==0?new Vector3(road*62f+UnityEngine.Random.Range(10f,13f),.3f,along):new Vector3(along,.3f,road*62f+UnityEngine.Random.Range(10f,13f));}

        static GameObject CreateCube(string name,Vector3 pos,Vector3 scale,Material material,Transform parent){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
        static GameObject CreateCylinder(string name,Vector3 pos,Vector3 halfScale,Material material,Transform parent){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent);go.transform.position=pos;go.transform.localScale=halfScale;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
    }
}
#endif
