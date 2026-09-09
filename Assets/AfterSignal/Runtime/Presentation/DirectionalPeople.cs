using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class PeopleArt
    {
        public static readonly string[] Citizens={"CivilianMan","CivilianWoman","OfficeMan","OfficeWoman","Worker","ElderMan","ElderWoman","TeacherMan","TeacherWoman","Doctor","Nurse","Bartender"};
        static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        static Camera camera;static int cameraFrame=-1;
        public static Camera ViewCamera{get{if(cameraFrame!=Time.frameCount||!camera){cameraFrame=Time.frameCount;camera=Camera.main;}return camera;}}
        public static Sprite[] Sheet(string key,bool seo=false)
        {
            bool legacy=key.StartsWith("Legacy");if(legacy)key=key.Substring(6);
            if(!seo&&!legacy&&(key=="Police"||key=="PoliceShotgun"||key=="Swat"))key="CyberPolice";
            if(!seo&&key.StartsWith("Gang"))key="CyberGang";
            string path=(seo?"SeoMotion/":key is "CoastGuard" or "NavyCrew" or "AirForceCrew" or "SeaRaider" or "NullCell"?"CivicForces/":key.StartsWith("Cyber")?"CyberSecurity/":"NpcDirections/")+key;
            if(!cache.TryGetValue(path,out var frames)){frames=Resources.LoadAll<Sprite>("Art/"+path);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));cache[path]=frames;}
            return frames;
        }
        public static Sprite Get(string key,int facing,int phase=0){if(key.StartsWith("Facility"))key=FacilityPeople.UniformArt(key);var s=Sheet(key);return s.Length>=16?s[Mathf.Clamp(facing,0,3)*4+Mathf.Clamp(phase,0,3)]:null;}
        public static int Direction(Vector3 world)
        {
            var camera=ViewCamera;var right=camera?Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized:Vector3.right;
            float x=Vector3.Dot(world,right),z=Vector3.Dot(world,Vector3.Cross(right,Vector3.up));
            return Mathf.Abs(x)>Mathf.Abs(z)?x>0?1:3:z>0?2:0;
        }
        public static DirectionalPerson Attach(GameObject go,string key)
        {
            var c=go.GetComponent<DirectionalPerson>();if(!c)c=go.AddComponent<DirectionalPerson>();c.art=key;return c;
        }
        public static string Role(CityNpc npc)
        {
            var g=GameDirector.Instance;int n=npc.variation;
            if(g&& (g.stage==StageId.Clinic||g.stage==StageId.UrbanInterior&&UrbanCatalog.Kind(UrbanCatalog.Current)==4))return new[]{"Doctor","Nurse","PatientMan","PatientWoman"}[n%4];
            if(g&&(g.stage==StageId.School||g.stage==StageId.UrbanInterior&&UrbanCatalog.Kind(UrbanCatalog.Current)==5))return new[]{"TeacherMan","TeacherWoman","StudentBoy","StudentGirl","OfficeWoman"}[n%5];
            if(g&&g.stage==StageId.Headquarters)return n%2==0?"OfficeMan":"OfficeWoman";
            return Citizens[n%Citizens.Length];
        }
    }
    [DefaultExecutionOrder(900)]
    public sealed class DirectionalPerson:MonoBehaviour
    {
        public string art="CivilianMan";
        public Vector3 Look;
        public float LookUntil;
        public bool Sitting,Lying;
        SpriteRenderer visual;Vector3 previous;float phase;int facing;Sprite lastLivingSprite;bool lastLivingFlip;
        float nextVisual,refresh;WorldActor body;CivicRoutine routine;RegionalUniform regionalUniform;CityNpc npc;PoliceOfficer officer;GangMember gang;TerrorSuspect suspect;CityPedestrian walker;
        static readonly int[] WalkCycle={1,0,2,0};
        void Start(){visual=GetComponent<SpriteRenderer>();if(!visual)visual=GetComponentInChildren<SpriteRenderer>();previous=transform.position;}
        public void Face(Vector3 point,float seconds=2){Look=point-transform.position;LookUntil=Time.time+seconds;}
        public void PreserveAppearance(){if(visual&&lastLivingSprite){visual.sprite=lastLivingSprite;visual.flipX=lastLivingFlip;}}
        void LateUpdate()
        {
            if(!visual)return;
            if(Time.time<nextVisual)return;nextVisual=Time.time+ActorWorkBudget.VisualInterval(this);
            if(Time.time>=refresh){refresh=Time.time+1;body=GetComponent<WorldActor>();routine=GetComponent<CivicRoutine>();regionalUniform=GetComponent<RegionalUniform>();npc=GetComponent<CityNpc>();officer=GetComponent<PoliceOfficer>();gang=GetComponent<GangMember>();suspect=GetComponent<TerrorSuspect>();walker=GetComponent<CityPedestrian>();}
            Vector3 movement=transform.position-previous;previous=transform.position;movement.y=0;
            if(body&&!body.Alive)
            {
                // Keep the exact atlas region captured in life (also used by the dismemberment mask).
                if(lastLivingSprite){visual.sprite=lastLivingSprite;visual.flipX=lastLivingFlip;}
                if(!CivilianImpact.Active(this)&&Camera.main)
                    visual.transform.rotation=Quaternion.Euler(90,Camera.main.transform.eulerAngles.y,85);
                return;
            }
            if(regionalUniform)art=regionalUniform.art;
            else if(routine&&routine.prisoner)art="Prisoner";
            else if(body&&body.military&&art!="NavyCrew"&&art!="AirForceCrew")art="Soldier";
            else if(art=="Prisoner")art="Worker";
            var game=GameDirector.Instance;if(!game)return;
            bool talking=CityLife.Instance&&CityLife.Instance.Speaker==npc&&game.Dialogue;
            bool attack=false;Vector3 direction=movement;
            if(officer&&officer.GangTarget){direction=officer.GangTarget.transform.position-transform.position;attack=true;}
            else if(officer&&WantedSystem.Level>0&&!IncidentCommand.Emergency){direction=game.Player.transform.position-transform.position;attack=true;}
            if(gang&&gang.Target){direction=gang.Target.transform.position-transform.position;attack=true;}
            if(gang&&gang.AttackingPlayer){direction=game.Player.transform.position-transform.position;attack=true;}
            if(suspect&&suspect.Firing){direction=suspect.AimTarget-transform.position;attack=true;}
            if(talking){direction=game.Player.transform.position-transform.position;LookUntil=Time.time+1;Look=direction;}
            if(Time.time<LookUntil)direction=Look;
            if(direction.sqrMagnitude>.0001f)facing=PeopleArt.Direction(direction);
            if(!game.Blocked)phase+=movement.magnitude/1.65f;
            int frame=attack&&movement.magnitude<.01f||talking||Time.time<LookUntil?3:movement.magnitude>.002f?WalkCycle[(int)(phase*4)%4]:0;
            var sprite=PeopleArt.Get(art,facing,frame);
            if(sprite){visual.sprite=sprite;visual.flipX=false;visual.color=Color.white;lastLivingSprite=sprite;lastLivingFlip=false;}
            bool down=walker&&walker.struck;
            if(Camera.main)visual.transform.rotation=Quaternion.Euler(Camera.main.transform.eulerAngles.x,Camera.main.transform.eulerAngles.y,Lying||down?85:0);
            if(Sitting&&!Lying)visual.transform.localScale=new Vector3(1,.78f,1);
            else visual.transform.localScale=Vector3.one;
        }
    }
}
