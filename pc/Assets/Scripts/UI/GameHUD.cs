using UnityEngine;

namespace GranivelCity
{
    public class GameHUD : MonoBehaviour
    {
        private GUIStyle titleStyle, hudStyle, smallStyle, loadingStyle;
        private Texture2D panel;

        private void Awake()
        {
            panel = new Texture2D(1,1); panel.SetPixel(0,0,new Color(0.018f,0.022f,0.028f,0.78f)); panel.Apply();
        }

        private void EnsureStyles()
        {
            if(titleStyle!=null)return;
            titleStyle=new GUIStyle(GUI.skin.label){fontSize=21,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
            hudStyle=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
            smallStyle=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true,normal={textColor=new Color(0.88f,0.9f,0.93f)}};
            loadingStyle=new GUIStyle(GUI.skin.label){fontSize=22,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}};
        }

        private void OnGUI()
        {
            EnsureStyles(); var r=GameRuntime.Instance; if(r==null||r.Player==null)return;
            if(r.World==null||!r.World.WorldReady){DrawLoading(r);return;}
            DrawMiniMap(r); DrawStatus(r); DrawMission(r); DrawCrosshair(); DrawHelp(r);
        }

        private void DrawLoading(GameRuntime r)
        {
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),panel);
            string status=r.World!=null?r.World.Status:"Budapest indítása…";
            GUI.Label(new Rect(0,Screen.height*0.42f,Screen.width,40),"GRANIVEL CITY — BUDAPEST RP",loadingStyle);
            GUI.Label(new Rect(0,Screen.height*0.42f+48,Screen.width,36),status,new GUIStyle(loadingStyle){fontSize=16,fontStyle=FontStyle.Normal});
        }

        private void DrawMiniMap(GameRuntime r)
        {
            const float size=220f; Rect rect=new Rect(22,22,size,size);GUI.DrawTexture(new Rect(rect.x-4,rect.y-4,rect.width+8,rect.height+8),panel);
            if(r.MiniMap!=null&&r.MiniMap.Texture!=null)GUI.DrawTexture(rect,r.MiniMap.Texture,ScaleMode.ScaleToFit,false);
            GUI.Label(new Rect(rect.x+9,rect.y+7,rect.width-18,26),"BUDAPEST RP",titleStyle);
        }

        private void DrawStatus(GameRuntime r)
        {
            float w=370f;Rect box=new Rect(Screen.width-w-24,22,w,164);GUI.DrawTexture(box,panel);
            var health=r.Player.GetComponent<Health>();int hp=health!=null?Mathf.RoundToInt(health.Current):0;string stars=r.Wanted.Stars>0?new string('★',r.Wanted.Stars):"—";
            GUI.Label(new Rect(box.x+15,box.y+10,w-30,26),$"KÉSZPÉNZ   {r.Money:N0} Ft",hudStyle);
            GUI.Label(new Rect(box.x+15,box.y+40,w-30,26),$"BANK          {(r.RP!=null?r.RP.Bank:0):N0} Ft",smallStyle);
            GUI.Label(new Rect(box.x+15,box.y+66,w-30,26),$"ÉLET {hp}%   ÉHSÉG {(r.RP!=null?r.RP.Hunger:0):0}%   SZOMJ {(r.RP!=null?r.RP.Thirst:0):0}%",smallStyle);
            GUI.Label(new Rect(box.x+15,box.y+92,w-30,26),$"MUNKA   {(r.RP!=null?r.RP.Job:"—")}",smallStyle);
            GUI.Label(new Rect(box.x+15,box.y+118,w-30,26),$"KÖRÖZÉS   {stars}",hudStyle);
            if(r.Player.CurrentVehicle!=null)GUI.Label(new Rect(box.x+15,box.y+143,w-30,22),$"ÜZEMANYAG  {r.Player.CurrentVehicle.fuel:0}/{r.Player.CurrentVehicle.fuelCapacity:0} L",smallStyle);
        }

        private void DrawMission(GameRuntime r)
        {
            float width=Mathf.Min(760,Screen.width*0.46f);Rect box=new Rect((Screen.width-width)*0.5f,22,width,76);GUI.DrawTexture(box,panel);
            GUI.Label(new Rect(box.x+14,box.y+8,box.width-28,24),"AKTUÁLIS RP FELADAT",hudStyle);
            GUI.Label(new Rect(box.x+14,box.y+36,box.width-28,34),r.Missions.Objective??"Szabad játék",smallStyle);
        }

        private void DrawCrosshair()
        {
            float cx=Screen.width*0.5f,cy=Screen.height*0.5f;GUI.Label(new Rect(cx-8,cy-15,24,24),"+",hudStyle);
        }

        private void DrawHelp(GameRuntime r)
        {
            GUI.Label(new Rect(22,Screen.height-58,Screen.width-44,24),"WASD mozgás/vezetés  •  Egér kamera  •  Bal klikk lövés  •  Jobb klikk célzás  •  E be/kiszállás  •  Shift futás/fék  •  Space ugrás",smallStyle);
            GUI.Label(new Rect(Screen.width-350,Screen.height-34,330,20),"Map data © OpenStreetMap contributors • ODbL",new GUIStyle(smallStyle){alignment=TextAnchor.MiddleRight,fontSize=11});
        }
    }
}
