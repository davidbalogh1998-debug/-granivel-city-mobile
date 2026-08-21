#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GranivelCity.Editor
{
    public static class FullCitySceneBuilder
    {
        const string ThirdParty = "Assets/ThirdParty/CC0";
        const string ScenePath = "Assets/Scenes/GranivelCity.unity";
        static readonly string[] Shops = { "NOVA MARKET", "CITY CAFÉ", "GRANIVEL MOTORS", "24/7 SHOP", "URBAN GYM", "PIZZA POINT", "TECH HUB", "FRESH MART", "BARBER 21", "CENTRAL PHARMACY", "RIVER HOTEL", "METRO FASHION" };

        class Library
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
        public static void Build()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/GranivelCity/Generated");
            var lib = Scan();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Random.InitState(260821);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .0016f;
            RenderSettings.fogColor = new Color(.58f,.71f,.80f);

            var grass = Mat("Grass", new Color(.20f,.38f,.16f));
            var asphalt = Mat("Asphalt", new Color(.095f,.10f,.11f));
            var concrete = Mat("Concrete", new Color(.48f,.49f,.47f));
            var metal = Mat("StreetMetal", new Color(.11f,.13f,.15f), .65f);
            var water = Mat("RiverWater", new Color(.04f,.32f,.54f), .2f);

            BuildRoads(grass, asphalt, concrete, water);
            BuildBuildings(lib, concrete);
            BuildStreetProps(lib, metal);
            BuildNature(lib, grass);
            BuildVehicles(lib);
            var player = BuildPeople(lib);
            BuildSystems(player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Granivel City scene generated: {lib.buildings.Count} building assets, {lib.vehicles.Count} vehicles, {lib.people.Count} people, {lib.nature.Count} nature assets.");
        }

        static Library Scan()
        {
            var lib = new Library();
            if (!AssetDatabase.IsValidFolder(ThirdParty)) return lib;
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { ThirdParty }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;
                string n = (asset.name + " " + path).ToLowerInvariant();
                if (Any(n,"building","house","office","shop","store","hotel","factory","industrial","commercial","apartment")) lib.buildings.Add(asset);
                if (!Any(n,"debris","wheel","bumper") && Any(n,"car","sedan","suv","taxi","truck","van","ambulance","police","hatchback","vehicle")) lib.vehicles.Add(asset);
                if (Any(n,"character","person","human","civilian","male","female","woman","man")) lib.people.Add(asset);
                if (Any(n,"tree","bush","plant","palm","shrub")) lib.nature.Add(asset);
                if (Any(n,"lamp","streetlight","lightpost")) lib.lamps.Add(asset);
                if (Any(n,"sign","traffic","stop","speed")) lib.signs.Add(asset);
                if (Any(n,"bench","trash","bin","hydrant","bollard","barrier","mailbox","cone")) lib.props.Add(asset);
            }
            return lib;
        }

        static bool Any(string value, params string[] terms) => terms.Any(value.Contains);

        static void BuildRoads(Material grass, Material asphalt, Material concrete, Material water)
        {
            var root = new GameObject("WORLD");
            Cube("Ground", new Vector3(0,-.35f,0), new Vector3(540,.6f,540), grass, root.transform);
            for (int i=-3;i<=3;i++)
            {
                float p=i*62f;
                Cube("Road NS",new Vector3(p,.02f,0),new Vector3(18,.16f,540),asphalt,root.transform);
                Cube("Road EW",new Vector3(0,.02f,p),new Vector3(540,.16f,18),asphalt,root.transform);
                for(int k=-6;k<=6;k++)
                {
                    Cube("Lane",new Vector3(p,.115f,k*34f),new Vector3(.12f,.02f,4.5f),concrete,root.transform);
                    Cube("Lane",new Vector3(k*34f,.115f,p),new Vector3(4.5f,.02f,.12f),concrete,root.transform);
                }
            }
            for(int x=-3;x<3;x++) for(int z=-3;z<3;z++)
                Cube("Sidewalk",new Vector3((x+.5f)*62f,.16f,(z+.5f)*62f),new Vector3(48,.28f,48),concrete,root.transform);

            const float riverX=224f;
            Cube("River",new Vector3(riverX,-.03f,0),new Vector3(44,.1f,540),water,root.transform);
            foreach(float z in new[]{-124f,0f,124f})
            {
                Cube("Bridge",new Vector3(riverX,.20f,z),new Vector3(48,.45f,20),asphalt,root.transform);
                Cube("Bridge Walk",new Vector3(riverX,.38f,z-11.5f),new Vector3(48,.5f,3),concrete,root.transform);
                Cube("Bridge Walk",new Vector3(riverX,.38f,z+11.5f),new Vector3(48,.5f,3),concrete,root.transform);
            }
        }

        static void BuildBuildings(Library lib, Material fallback)
        {
            var root=new GameObject("CITY — Buildings & Shops");
            int index=0;
            for(int x=-3;x<3;x++) for(int z=-3;z<3;z++)
            {
                Vector3 center=new((x+.5f)*62f,.32f,(z+.5f)*62f);
                int count=1+Mathf.Abs((x*7+z*3)%3);
                for(int b=0;b<count;b++)
                {
                    float angle=count==1?0:b/(float)count*Mathf.PI*2f;
                    Vector3 pos=center+new Vector3(Mathf.Cos(angle)*10f,0,Mathf.Sin(angle)*10f);
                    GameObject go;
                    if(lib.buildings.Count>0)
                    {
                        go=Spawn(lib.buildings[(index*11+b*5)%lib.buildings.Count],pos,Quaternion.Euler(0,Random.Range(0,4)*90,0),root.transform);
                        FitMax(go,count==1?34f:17f);
                    }
                    else
                    {
                        float h=14+(index%6)*5;
                        go=Cube("Building",pos+Vector3.up*h/2,new Vector3(count==1?31:14,h,count==1?30:14),Mat("Facade"+(index%7),Color.HSVToRGB((index*.11f)%1f,.18f,.72f)),root.transform);
                    }
                    if(index%3==0) ShopSign(go,Shops[index%Shops.Length]);
                    index++;
                }
            }
        }

        static void ShopSign(GameObject building,string label)
        {
            Bounds b=BoundsOf(building);
            var back=Cube("Shop Sign",new Vector3(b.center.x,Mathf.Max(3f,b.min.y+3f),b.max.z+.16f),new Vector3(Mathf.Clamp(label.Length*.35f,4,9),1.35f,.15f),Mat("ShopSign",new Color(.02f,.05f,.09f)),building.transform.parent);
            var text=new GameObject(label);text.transform.SetParent(back.transform.parent);text.transform.position=back.transform.position+Vector3.forward*.1f;
            var mesh=text.AddComponent<TextMesh>();mesh.text=label;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.fontSize=64;mesh.characterSize=.055f;mesh.color=Color.white;
        }

        static void BuildStreetProps(Library lib, Material metal)
        {
            var root=new GameObject("CITY — Lamps, Signs & Props");int q=0;
            for(int r=-3;r<=3;r++) for(int k=-3;k<=3;k++)
            {
                float p=r*62f,s=k*62f+22f;
                if(lib.lamps.Count>0){var lamp=Spawn(lib.lamps[q%lib.lamps.Count],new Vector3(p+11,.2f,s),Quaternion.identity,root.transform);FitHeight(lamp,6f);}else Lamp(new Vector3(p+11,0,s),metal,root.transform);
                if(lib.signs.Count>0){var sign=Spawn(lib.signs[q%lib.signs.Count],new Vector3(p+12,.2f,s+6),Quaternion.identity,root.transform);FitHeight(sign,2.8f);}else Sign(new Vector3(p+12,0,s+6),30+(q%3)*10,metal,root.transform);
                if(lib.props.Count>0 && q%2==0){var prop=Spawn(lib.props[q%lib.props.Count],new Vector3(s,.2f,p-11),Quaternion.Euler(0,q%4*90,0),root.transform);FitHeight(prop,1.3f);}
                q++;
            }
        }

        static void Lamp(Vector3 p,Material metal,Transform root)
        {
            var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pole.name="Street Lamp";pole.transform.SetParent(root);pole.transform.position=p+Vector3.up*3;pole.transform.localScale=new Vector3(.12f,3,.12f);pole.GetComponent<Renderer>().sharedMaterial=metal;
            var lightGo=new GameObject("Street Light");lightGo.transform.SetParent(root);lightGo.transform.position=p+new Vector3(.8f,5.7f,0);var light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.range=13;light.intensity=2;light.color=new Color(1,.78f,.48f);light.shadows=LightShadows.None;
        }

        static void Sign(Vector3 p,int speed,Material metal,Transform root)
        {
            var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pole.name="Sign Pole";pole.transform.SetParent(root);pole.transform.position=p+Vector3.up*1.4f;pole.transform.localScale=new Vector3(.07f,1.4f,.07f);pole.GetComponent<Renderer>().sharedMaterial=metal;
            var go=new GameObject("Speed "+speed);go.transform.SetParent(root);go.transform.position=p+Vector3.up*2.5f;var tm=go.AddComponent<TextMesh>();tm.text=speed.ToString();tm.anchor=TextAnchor.MiddleCenter;tm.fontSize=80;tm.characterSize=.018f;tm.color=Color.white;
        }

        static void BuildNature(Library lib,Material fallback)
        {
            var root=new GameObject("CITY — Vegetation");
            for(int i=0;i<120;i++)
            {
                Vector3 p=new(Random.Range(-245f,245f),.25f,Random.Range(-245f,245f));
                if(NearRoad(p,14)||Mathf.Abs(p.x-224)<28)continue;
                if(lib.nature.Count>0){var go=Spawn(lib.nature[i%lib.nature.Count],p,Quaternion.Euler(0,Random.Range(0,360),0),root.transform);FitHeight(go,Random.Range(3.5f,7f));}
                else{var tree=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tree.name="Tree";tree.transform.SetParent(root.transform);tree.transform.position=p+Vector3.up*2;tree.transform.localScale=new Vector3(.3f,2,.3f);tree.GetComponent<Renderer>().sharedMaterial=fallback;}
            }
        }

        static void BuildVehicles(Library lib)
        {
            var root=new GameObject("TRAFFIC — Vehicles");
            for(int i=0;i<36;i++)
            {
                bool vertical=i%2==0;int lane=(i%7)-3;float along=-215+(i*29)%430;Vector3 p=vertical?new Vector3(lane*62-3,.6f,along):new Vector3(along,.6f,lane*62+3);Quaternion rot=Quaternion.Euler(0,vertical?0:90,0);
                GameObject go=lib.vehicles.Count>0?Spawn(lib.vehicles[(i*7)%lib.vehicles.Count],p,rot,root.transform):Cube("Car",p,new Vector3(2.1f,1.3f,4.8f),Mat("Car"+(i%8),Color.HSVToRGB((i*.13f)%1f,.65f,.6f)),root.transform);
                if(lib.vehicles.Count>0)FitMax(go,5.2f);go.transform.rotation=rot;
                EnsureVehiclePhysics(go);var vehicle=go.GetComponent<GranivelVehicleController>();if(vehicle==null)vehicle=go.AddComponent<GranivelVehicleController>();vehicle.trafficVehicle=i>6;vehicle.trafficSpeed=8+i%5;
            }
        }

        static GranivelPlayerController BuildPeople(Library lib)
        {
            var root=new GameObject("PEOPLE");GameObject pg;
            if(lib.people.Count>0){pg=Spawn(lib.people[0],new Vector3(-15,1,18),Quaternion.identity,root.transform);FitHeight(pg,1.8f);}else{pg=GameObject.CreatePrimitive(PrimitiveType.Capsule);pg.name="Player";pg.transform.SetParent(root.transform);pg.transform.position=new Vector3(-15,1,18);Object.DestroyImmediate(pg.GetComponent<Collider>());}
            var cc=pg.GetComponent<CharacterController>();if(cc==null)cc=pg.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.36f;cc.center=new Vector3(0,.9f,0);var player=pg.GetComponent<GranivelPlayerController>();if(player==null)player=pg.AddComponent<GranivelPlayerController>();
            for(int i=0;i<54;i++)
            {
                Vector3 p=SidewalkPoint(i);GameObject npc=lib.people.Count>0?Spawn(lib.people[(i*5+1)%lib.people.Count],p,Quaternion.Euler(0,Random.Range(0,360),0),root.transform):GameObject.CreatePrimitive(PrimitiveType.Capsule);
                if(lib.people.Count>0)FitHeight(npc,Random.Range(1.65f,1.9f));else{npc.name="Civilian";npc.transform.SetParent(root.transform);npc.transform.position=p+Vector3.up;}
                if(npc.GetComponentInChildren<Collider>()==null)npc.AddComponent<CapsuleCollider>();if(npc.GetComponent<GranivelNpcAgent>()==null)npc.AddComponent<GranivelNpcAgent>();
            }
            return player;
        }

        static void BuildSystems(GranivelPlayerController player)
        {
            var sunGo=new GameObject("Sun");var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.35f;sun.color=new Color(1,.91f,.76f);sun.shadows=LightShadows.Soft;sunGo.transform.rotation=Quaternion.Euler(46,-28,0);
            var sys=new GameObject("Granivel Game Systems");var game=sys.AddComponent<GranivelGameController>();game.player=player;game.sun=sun;
            var camGo=new GameObject("Main Camera");camGo.tag="MainCamera";var cam=camGo.AddComponent<Camera>();cam.fieldOfView=62;cam.nearClipPlane=.08f;cam.farClipPlane=1100;camGo.AddComponent<AudioListener>();camGo.AddComponent<GranivelThirdPersonCamera>();camGo.transform.position=player.transform.position+new Vector3(0,5,-8);
        }

        static void EnsureVehiclePhysics(GameObject go)
        {
            if(go.GetComponentInChildren<Collider>()==null){var box=go.AddComponent<BoxCollider>();Bounds b=BoundsOf(go);box.center=go.transform.InverseTransformPoint(b.center);box.size=new Vector3(Mathf.Max(.8f,b.size.x),Mathf.Max(.6f,b.size.y),Mathf.Max(1.5f,b.size.z));}
            var rb=go.GetComponent<Rigidbody>();if(rb==null)rb=go.AddComponent<Rigidbody>();rb.mass=1200;rb.interpolation=RigidbodyInterpolation.Interpolate;
        }

        static GameObject Spawn(GameObject asset,Vector3 p,Quaternion r,Transform parent){GameObject go;try{go=(GameObject)PrefabUtility.InstantiatePrefab(asset);}catch{go=Object.Instantiate(asset);}go.name=asset.name;go.transform.SetParent(parent);go.transform.SetPositionAndRotation(p,r);go.SetActive(true);return go;}
        static void FitHeight(GameObject go,float h){var b=BoundsOf(go);if(b.size.y>.001f)go.transform.localScale*=h/b.size.y;Ground(go);}
        static void FitMax(GameObject go,float s){var b=BoundsOf(go);float max=Mathf.Max(b.size.x,b.size.y,b.size.z);if(max>.001f)go.transform.localScale*=s/max;Ground(go);}
        static void Ground(GameObject go){var b=BoundsOf(go);go.transform.position+=Vector3.up*(.25f-b.min.y);}
        static Bounds BoundsOf(GameObject go){var r=go.GetComponentsInChildren<Renderer>();if(r.Length==0)return new Bounds(go.transform.position,Vector3.one);var b=r[0].bounds;for(int i=1;i<r.Length;i++)b.Encapsulate(r[i].bounds);return b;}
        static bool NearRoad(Vector3 p,float d){for(int i=-3;i<=3;i++)if(Mathf.Abs(p.x-i*62)<d||Mathf.Abs(p.z-i*62)<d)return true;return false;}
        static Vector3 SidewalkPoint(int i){int road=(i%7)-3;float along=-210+(i*37)%420;return i%2==0?new Vector3(road*62+11,.3f,along):new Vector3(along,.3f,road*62+11);}
        static GameObject Cube(string n,Vector3 p,Vector3 s,Material m,Transform parent){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(parent);go.transform.position=p;go.transform.localScale=s;go.GetComponent<Renderer>().sharedMaterial=m;return go;}

        static Material Mat(string name,Color color,float metallic=.02f)
        {
            string path="Assets/GranivelCity/Generated/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;var shader=Shader.Find("Standard");if(shader==null)shader=Shader.Find("Universal Render Pipeline/Lit");m=new Material(shader);m.name=name;m.color=color;if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);AssetDatabase.CreateAsset(m,path);return m;
        }

        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;string[] parts=path.Split('/');string current=parts[0];for(int i=1;i<parts.Length;i++){string next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}
        }
    }
}
#endif
