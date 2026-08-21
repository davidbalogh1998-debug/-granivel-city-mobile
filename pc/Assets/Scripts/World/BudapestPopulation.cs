using System.Collections.Generic;
using UnityEngine;

namespace GranivelCity
{
    public class BudapestPopulation : MonoBehaviour
    {
        public int desiredCars = 14;
        public int desiredCitizens = 34;
        private readonly List<GameObject> cars = new();
        private readonly List<GameObject> citizens = new();
        private float refreshTimer;

        private void Update()
        {
            var r = GameRuntime.Instance;
            if (r == null || r.World == null || !r.World.WorldReady || r.Player == null) return;
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 4f;
            cars.RemoveAll(x => x == null); citizens.RemoveAll(x => x == null);
            CullFar(cars, r.ActiveTarget.position, 900f); CullFar(citizens, r.ActiveTarget.position, 500f);
            while (cars.Count < desiredCars) TrySpawnCar(r);
            while (citizens.Count < desiredCitizens) TrySpawnCitizen(r);
        }

        private void TrySpawnCar(GameRuntime r)
        {
            if (!r.World.TryGetRoadSpawnNear(r.ActiveTarget.position, Random.Range(80f, 500f), out Vector3 pos, out Quaternion rot)) return;
            pos += rot * Vector3.right * (Random.value > 0.5f ? 1.7f : -1.7f);
            if (Physics.CheckBox(pos + Vector3.up * 0.6f, new Vector3(1.1f, 0.7f, 2.4f), rot)) return;
            var car = CreateCar(pos, rot, Random.value > 0.45f);
            car.transform.SetParent(transform); cars.Add(car);
        }

        private void TrySpawnCitizen(GameRuntime r)
        {
            if (!r.World.TryGetRoadSpawnNear(r.ActiveTarget.position, Random.Range(40f, 350f), out Vector3 pos, out Quaternion rot)) return;
            pos += rot * Vector3.right * (Random.value > 0.5f ? 4.5f : -4.5f); pos.y = r.World.SampleHeightAtWorld(pos) + 0.95f;
            if (Physics.CheckSphere(pos, 0.45f)) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = "Budapest Citizen"; go.transform.position = pos; go.transform.localScale = new Vector3(0.68f,0.92f,0.68f);
            go.GetComponent<Renderer>().material = BudapestMaterials.Building("citizen_" + Random.Range(0, 18));
            var h = go.AddComponent<Health>(); h.maxHealth = 70f; go.AddComponent<NPCController>(); go.transform.SetParent(transform); citizens.Add(go);
        }

        private static GameObject CreateCar(Vector3 position, Quaternion rotation, bool traffic)
        {
            var root = new GameObject("Budapest Vehicle"); root.transform.position = position; root.transform.rotation = rotation;
            var rb = root.AddComponent<Rigidbody>(); rb.mass = 1350f;
            var box = root.AddComponent<BoxCollider>(); box.size = new Vector3(1.9f,1.2f,4.45f); box.center = new Vector3(0f,0.45f,0f);
            Color color = Color.HSVToRGB(Random.value, Random.Range(0.12f,0.62f), Random.Range(0.3f,0.88f));
            var bodyMat = RuntimeMaterials.Get("Vehicle_"+Random.Range(0,24), color, 0.55f, 0.72f);
            ChildBox(root.transform,"Body",new Vector3(0f,0.42f,0f),new Vector3(1.9f,0.72f,4.35f),bodyMat);
            ChildBox(root.transform,"Cabin",new Vector3(0f,1.03f,-0.15f),new Vector3(1.58f,0.72f,2.15f),BudapestMaterials.Glass);
            ChildBox(root.transform,"FrontBumper",new Vector3(0f,0.3f,2.22f),new Vector3(1.82f,0.28f,0.16f),bodyMat);
            ChildBox(root.transform,"RearBumper",new Vector3(0f,0.3f,-2.22f),new Vector3(1.82f,0.28f,0.16f),bodyMat);
            Vector3[] wheels={new(-0.98f,0.05f,1.45f),new(0.98f,0.05f,1.45f),new(-0.98f,0.05f,-1.45f),new(0.98f,0.05f,-1.45f)};
            foreach(var wp in wheels){var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.name="Wheel";w.transform.SetParent(root.transform);w.transform.localPosition=wp;w.transform.localRotation=Quaternion.Euler(0,0,90);w.transform.localScale=new Vector3(0.38f,0.15f,0.38f);var c=w.GetComponent<Collider>();if(c)Destroy(c);w.GetComponent<Renderer>().material=RuntimeMaterials.Get("Tire",new Color(0.025f,0.025f,0.028f));}
            root.AddComponent<VehicleController>(); if(traffic)root.AddComponent<TrafficCarAI>(); return root;
        }

        private static void ChildBox(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.identity;go.transform.localScale=scale;var c=go.GetComponent<Collider>();if(c)Destroy(c);go.GetComponent<Renderer>().material=mat;
        }

        private static void CullFar(List<GameObject> list, Vector3 center, float distance)
        {
            float d2=distance*distance; for(int i=list.Count-1;i>=0;i--){if(list[i]==null){list.RemoveAt(i);continue;}if((list[i].transform.position-center).sqrMagnitude>d2){Object.Destroy(list[i]);list.RemoveAt(i);}}
        }
    }
}
