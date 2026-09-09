using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class ThreatOverlay:MonoBehaviour
    {
        readonly List<WorldActor> nearby=new();float next;GUIStyle label;GameDirector game;
        public static bool Hostile(WorldActor a)=>CampaignBattle.TutorialTarget(a);
        void Awake(){game=GetComponent<GameDirector>();}
        void Update(){if(!game||!game.Ready||Time.time<next)return;next=Time.time+.15f;CampaignBattle.TutorialTargets(nearby);}
        void OnGUI()
        {
            if(!game||!game.Ready||game.Blocked||!CampaignBattle.TutorialActive||!Camera.main)return;
            label??=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,font=Resources.Load<Font>("Fonts/NotoSansKR"),fontSize=Mathf.RoundToInt(15*Screen.height/900f),fontStyle=FontStyle.Bold};
            int drawn=0;foreach(var a in nearby)
            {
                if(!Hostile(a)||drawn>=8||(a.Center-game.Player.Shoulder).sqrMagnitude>110*110)continue;Draw(a.Center+Vector3.up*1.05f,"적 · 회수대",a.transform);drawn++;
            }
        }
        void Draw(Vector3 world,string text,Transform target)
        {
            var camera=Camera.main;var p=camera.WorldToScreenPoint(world);if(p.z<0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)return;
            if(Physics.Linecast(camera.transform.position,world,out var hit,1,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(target))return;
            var rect=new Rect(p.x-74,Screen.height-p.y-23,148,25);var old=GUI.color;GUI.color=new Color(.015f,.02f,.028f,.85f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=new Color(1,.32f,.26f);GUI.Label(rect,"▼ "+text,label);GUI.color=old;
        }
    }
}
