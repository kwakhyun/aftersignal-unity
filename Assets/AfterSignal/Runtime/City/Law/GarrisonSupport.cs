using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class GarrisonSupport:MonoBehaviour
    {
        public MilitaryBaseOperations.Base Base;public WorldActor Body=>body;CityVehicle reserved;float vehicleRetry;
        static int pathFrame=-1,paths;float steerUntil;Vector3 steering,lastGoal;
        WorldActor body,target;Vector3 home,exit;float scan,fire,quiet;bool engaged,returning,leftRoom;
        readonly List<WorldActor> nearby=new();readonly List<Behaviour> suspended=new();readonly PursuitPath path=new();
        public string DutyState{get;private set;}="대기";public int Shots {get;private set;}public bool Supporting=>engaged;public WorldActor Target=>target;
        public static void Register(WorldActor body,Vector3 gate)
        {if(!body||!body.military||body.GetComponent<GarrisonSupport>())return;var duty=body.gameObject.AddComponent<GarrisonSupport>();duty.body=body;duty.home=body.transform.position;duty.exit=gate;}
        void PauseRoutine()
        {
            foreach(var b in GetComponents<Behaviour>())if(b.enabled&&(b is CityNpc||b is VenueActor||b is CivicRoutine||b is FacilityCitizen||b is RegionalGuard||b is PoliceOfficer||b is ArmyResponder)){suspended.Add(b);b.enabled=false;}
        }
        void ResumeRoutine(){foreach(var b in suspended)if(b)b.enabled=true;suspended.Clear();engaged=returning=false;}
        void Update()
        {
            var g=GameDirector.Instance;DutyState=!g||g.Blocked?"게임 대기":!body?"병사 없음":!body.Alive?"사망":body.Downed?"중상":CivilianImpact.Active(this)?"넘어짐":"경계";if(!g||g.Blocked||!body||!body.Alive||body.Downed||CivilianImpact.Active(this))return;
            if(!(Base!=null&&Base.Active)&&!LocalSimulation.Combat(transform.position)){if(engaged)ResumeRoutine();target=null;return;}
            if(GetComponent<GarrisonPassenger>()){DutyState="탑승";return;}
            bool baseAlarm=Base!=null&&Base.Active;bool playerTarget=baseAlarm&&Base.PlayerTarget&&!(Base.Threat&&Base.Threat.Alive);
            if(baseAlarm)target=Base.Threat;
            if(!baseAlarm&&Time.time>=scan)
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
            if(target||playerTarget)
            {
                DutyState="출동 / "+(leftRoom?"외부":"실내");
                if(!engaged){PauseRoutine();engaged=true;leftRoom=Mathf.Abs(home.y-exit.y)<2&&(Vector3.Distance(home,exit)<3||GarrisonArchitecture.Outdoors(home));NpcSpeech.Say(body,"기지 인근 교전 확인! 외곽 방어선 지원한다!",4,5);}
                returning=false;quiet=Time.time+10;var aim=playerTarget?g.Player.Shoulder:target.Center;
                if(!leftRoom&&transform.position.y>exit.y+2.5f&&GarrisonLiftRide.TryRide(this,exit))return;
                if(baseAlarm&&leftRoom)
                {
                    if(!reserved&&Time.time>vehicleRetry){vehicleRetry=Time.time+2;reserved=MilitaryBaseOperations.ReserveVehicle(this,Base);}
                    if(reserved)
                    {
                        if(reserved.Wrecked||reserved.owned||reserved.occupied){ReleaseReservation();}
                        else{DutyState="장비 접근 / "+reserved.type;var door=VehicleSeats.Door(reserved);if(Vector3.Distance(transform.position,door)<3.2f){GarrisonVehicleDriver.Board(reserved,this,Base);reserved=null;return;}Move(door,4.5f*Time.deltaTime);return;}
                    }
                }
                bool clear=playerTarget?FactionCombat.Visible(body.Center,aim,180):TacticalJudgment.ClearShot(body,target,aim,false,180);
                Vector3 goal=aim;
                // Leave the staffed room through its real entrance before pursuing outside.
                if(!leftRoom){if((transform.position-exit).sqrMagnitude<16)leftRoom=true;else goal=exit;}
                if(!leftRoom||!clear||(aim-body.Center).sqrMagnitude>55*55)Move(goal,3.9f*Time.deltaTime);
                if(clear&&(aim-body.Center).sqrMagnitude<=55*55&&leftRoom)GetComponent<DirectionalPerson>()?.Face(aim,.7f);
                if(leftRoom&&clear&&Time.time>=fire){fire=Time.time+.36f;Shots++;GetComponent<PixelActor>()?.Pose(3,Mathf.Sign((aim-body.Center).x));FactionCombat.Fire(body,body.Center+Vector3.up*.25f,aim,180,target&&target.monster?28:16,SignalEffects.Gold,playerTarget);g.Audio.PlayGun(GunshotKind.Rifle,body.Center,.7f);}
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
            if(pathFrame!=Time.frameCount){pathFrame=Time.frameCount;paths=2;}
            if((goal-lastGoal).sqrMagnitude>16){steerUntil=0;steering=Vector3.zero;}
            if(Time.time>=steerUntil&&paths>0){paths--;steering=path.Direction(transform.position,goal);lastGoal=goal;steerUntil=Time.time+.3f;}
            var d=steering;var cc=GetComponent<CharacterController>();
            if(cc&&cc.enabled)cc.Move(d*distance+Vector3.down*.12f);else PedestrianSteering.For(this).Move(transform.position+d*4,distance);
            GetComponent<PixelActor>()?.Pose(1+(int)(Time.time*7)%2,Mathf.Sign(d.x));
        }
        public void SetExit(Vector3 point){exit=point;}
        public void WalkTo(Vector3 goal,float distance)=>Move(goal,distance);
        void ReleaseReservation(){if(reserved){var claim=reserved.GetComponent<VehicleClaim>();if(claim&&claim.Soldier==this)Destroy(claim);}reserved=null;}
        void OnDestroy()=>ReleaseReservation();
        void OnDisable(){ReleaseReservation();if(engaged)ResumeRoutine();}
    }
}
