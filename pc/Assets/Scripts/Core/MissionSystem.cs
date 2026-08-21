using UnityEngine;

namespace GranivelCity
{
    public class MissionSystem : MonoBehaviour
    {
        public int Stage { get; private set; }
        public string Objective { get; private set; }
        public Vector3 MarkerPosition { get; private set; }
        public bool HasMarker { get; private set; }
        private GameObject marker; private bool enteredVehicle; private float configureDelay;

        public void Begin(){Stage=Mathf.Clamp(PlayerPrefs.GetInt("GC_Mission",0),0,4);configureDelay=0.5f;}
        private void Update()
        {
            var r=GameRuntime.Instance;if(r==null||r.Player==null)return;
            if(configureDelay>0f){configureDelay-=Time.deltaTime;if(configureDelay<=0f)ConfigureStage();return;}
            if(r.World==null||!r.World.WorldReady)return;
            if(Stage==0&&Vector3.Distance(r.ActiveTarget.position,MarkerPosition)<6f){r.AddMoney(15000);Stage=1;ConfigureStage();}
            else if(Stage==1&&enteredVehicle){Stage=2;ConfigureStage();}
            else if(Stage==2&&r.Player.CurrentVehicle!=null&&Vector3.Distance(r.ActiveTarget.position,MarkerPosition)<8f){r.AddMoney(30000);r.Wanted.AddHeat(45f);Stage=3;ConfigureStage();}
            else if(Stage==3&&r.Wanted.Stars==0){r.AddMoney(50000);Stage=4;ConfigureStage();}
            if(marker!=null){marker.transform.Rotate(0f,45f*Time.deltaTime,0f,Space.World);float p=1f+Mathf.Sin(Time.time*3f)*0.08f;marker.transform.localScale=new Vector3(4.5f*p,0.15f,4.5f*p);}
        }
        public void NotifyVehicleEntered()=>enteredVehicle=true;
        private void ConfigureStage()
        {
            enteredVehicle=false;if(marker!=null)Destroy(marker);marker=null;HasMarker=false;
            switch(Stage)
            {
                case 0: Objective="Menj a Vörösmarty térre az első RP megbízásért."; SetMarkerGeo(47.49690,19.05035,new Color(0.95f,0.70f,0.08f));break;
                case 1: Objective="Szerezz egy járművet. [E]";break;
                case 2: Objective="Vidd a járművet a Nyugati pályaudvarhoz.";SetMarkerGeo(47.51090,19.05675,new Color(0.92f,0.12f,0.10f));break;
                case 3: Objective="Köröznek. Rázd le a BRFK egységeit.";break;
                default: Objective="Budapest RP szabad játék — fedezd fel a várost.";break;
            }
            GameRuntime.Instance?.Save();
        }
        private void SetMarkerGeo(double lat,double lon,Color color)
        {
            Vector3 p=GeoProjection.GeoToWorld(lat,lon);p.y=(GameRuntime.Instance?.World?.SampleHeightAtWorld(p)??8f)+0.3f;MarkerPosition=p;HasMarker=true;
            marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Mission Marker";marker.transform.position=p;marker.transform.localScale=new Vector3(4.5f,0.15f,4.5f);var col=marker.GetComponent<Collider>();if(col)Destroy(col);marker.GetComponent<Renderer>().material=RuntimeMaterials.Get("Mission"+Stage,color,0f,0.65f);
        }
    }
}
