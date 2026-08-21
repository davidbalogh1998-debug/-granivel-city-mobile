using System.Collections.Generic;
using UnityEngine;

namespace GranivelCity
{
    public class WantedSystem : MonoBehaviour
    {
        public float Heat { get; private set; }
        public int Stars => Heat <= 0.1f ? 0 : Mathf.Clamp(Mathf.CeilToInt(Heat / 20f), 1, 5);
        private float lastCrimeTime = -999f, lastPoliceContact = -999f, spawnTimer;
        private readonly List<GameObject> police = new();
        private void Update()
        {
            if (Heat > 0f && Time.time-lastCrimeTime>12f && Time.time-lastPoliceContact>7f) Heat=Mathf.Max(0f,Heat-Time.deltaTime*1.5f);
            spawnTimer -= Time.deltaTime;
            if (Stars > 0 && spawnTimer <= 0f) { spawnTimer=4f; CleanupPolice(); int desired=Mathf.Min(12,Stars*2); while(police.Count<desired)SpawnOfficer(); }
            else if (Stars==0){ CleanupPolice(); for(int i=police.Count-1;i>=0;i--)if(police[i]!=null)Destroy(police[i]); police.Clear(); }
        }
        public void AddHeat(float amount){Heat=Mathf.Clamp(Heat+Mathf.Abs(amount),0f,100f);lastCrimeTime=Time.time;}
        public void RegisterPoliceContact()=>lastPoliceContact=Time.time;
        public void ClearWanted(){Heat=0f;for(int i=police.Count-1;i>=0;i--)if(police[i]!=null)Destroy(police[i]);police.Clear();}
        private void CleanupPolice()=>police.RemoveAll(x=>x==null);
        private void SpawnOfficer()
        {
            var r=GameRuntime.Instance;if(r==null||r.Player==null)return;Vector2 ring=Random.insideUnitCircle.normalized*Random.Range(28f,48f);Vector3 pos=r.ActiveTarget.position+new Vector3(ring.x,0f,ring.y);
            if(r.World!=null)pos.y=r.World.SampleHeightAtWorld(pos)+1f;else pos.y=r.ActiveTarget.position.y;
            var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="BRFK Police Officer";go.transform.position=pos;var renderer=go.GetComponent<Renderer>();if(renderer)renderer.material=RuntimeMaterials.Get("Police",new Color(0.035f,0.08f,0.18f));
            var health=go.AddComponent<Health>();health.maxHealth=90f;health.isPolice=true;go.AddComponent<PoliceAI>();police.Add(go);
        }
    }
}
