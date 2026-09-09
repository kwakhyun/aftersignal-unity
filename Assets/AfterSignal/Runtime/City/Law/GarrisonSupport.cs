using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class GarrisonSupport:MonoBehaviour
    {
        WorldActor body,target;Vector3 home,exit;float scan,fire,quiet;bool engaged,returning,leftRoom;
        readonly List<WorldActor> nearby=new();readonly List<Behaviour> suspended=new();readonly PursuitPath path=new();
        public int Shots {get;private set;}public bool Supporting=>engaged;public WorldActor Target=>target;
        public static void Register(WorldActor body,Vector3 gate)
        {if(!body||!body.military||body.GetComponent<GarrisonSupport>())return;var duty=body.gameObject.AddComponent<GarrisonSupport>();duty.body=body;duty.home=body.transform.position;duty.exit=gate;}
        void PauseRoutine()
        {
            foreach(var b in GetComponents<Behaviour>())if(b.enabled&&(b is CityNpc||b is VenueActor||b is CivicRoutine||b is FacilityCitizen||b is RegionalGuard||b is PoliceOfficer)){suspended.Add(b);b.enabled=false;}
        }
        void ResumeRoutine(){foreach(var b in suspended)if(b)b.enabled=true;suspended.Clear();engaged=returning=false;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!body||!body.Alive||body.Downed||CivilianImpact.Active(this))return;
            if(!LocalSimulation.Combat(transform.position)){if(engaged)ResumeRoutine();target=null;return;}
            if(Time.time>=scan)
            {
                scan=Time.time+.8f;target=null;float best=280*280;
                ActorSpatialIndex.Nearby(transform.position,280,nearby);
                foreach(var other in nearby)
                {
                    // Army garrisons do not join the police-only terrorist response.
                    if(!other||!other.Alive||other.Downed||other.terrorist||!TacticalJudgment.Opponent(body,other))continue;
                    if((other.transform.position-exit).sqrMagnitude>280*280)continue;
                    float d=(other.Center-body.Center).sqrMagnitude*(other.monster?.35f:1);if(d<best){best=d;target=other;}
                }
            }
            if(target)
            {
                if(!engaged){PauseRoutine();engaged=true;leftRoom=(home-exit).sqrMagnitude<25;NpcSpeech.Say(body,"기지 인근 교전 확인! 외곽 방어선 지원한다!",4,5);}
                returning=false;quiet=Time.time+10;var aim=target.Center;
                bool clear=TacticalJudgment.ClearShot(body,target,aim,false,180);
                Vector3 goal=aim;
                // Leave the staffed room through its real entrance before pursuing outside.
                if(!leftRoom){if((transform.position-exit).sqrMagnitude<16)leftRoom=true;else goal=exit;}
                if(!clear||(aim-body.Center).sqrMagnitude>55*55)Move(goal,3.9f*Time.deltaTime);
                GetComponent<DirectionalPerson>()?.Face(aim,.7f);
                if(clear&&Time.time>=fire){fire=Time.time+.36f;Shots++;GetComponent<PixelActor>()?.Pose(3,Mathf.Sign((aim-body.Center).x));FactionCombat.Fire(body,body.Center+Vector3.up*.25f,aim,180,target.monster?28:16,SignalEffects.Gold,false);g.Audio.PlayGun(GunshotKind.Rifle,body.Center,.7f);}
            }
            else if(engaged&&Time.time>=quiet)
            {
                if(!returning){returning=true;NpcSpeech.Say(body,"주변 위협 해소. 부상자를 확인하고 기지로 복귀한다.",4,3);}
                var goal=(transform.position-exit).sqrMagnitude>25&&(transform.position-home).sqrMagnitude>900?exit:home;
                if((transform.position-home).sqrMagnitude<4)ResumeRoutine();else Move(goal,2.3f*Time.deltaTime);
            }
        }
        void Move(Vector3 goal,float distance)
        {
            var d=path.Direction(transform.position,goal);var cc=GetComponent<CharacterController>();
            if(cc&&cc.enabled)cc.Move(d*distance+Vector3.down*.12f);else PedestrianSteering.For(this).Move(transform.position+d*4,distance);
            GetComponent<DirectionalPerson>()?.Face(goal,.5f);GetComponent<PixelActor>()?.Pose(1+(int)(Time.time*7)%2,Mathf.Sign(d.x));
        }
        void OnDisable(){if(engaged)ResumeRoutine();}
    }
}
