using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class TacticalTransport:MonoBehaviour
    {
        static readonly List<TacticalTransport> all=new();
        public static bool Pending=>all.Exists(t=>t&&t.remaining>0&&t.car&&!t.car.Wrecked);
        public int Deployed{get;private set;}
        WorldActor incidentTarget;
        public void AssignIncident(WorldActor target){incidentTarget=target;incidentAssignment=true;}
        readonly ResponseDrive route=new();
        CityVehicle car;WantedSystem owner;int remaining,rank=4;float elapsed,release,leave;bool withdrawing,patrol,incidentAssignment;
        // Wanted-response vehicles that have dropped their whole squad leave before the next wave, so parked vans do not pile up near LastSeen.
        public static void WithdrawUnloaded(){foreach(var t in all.ToArray())if(t&&t.remaining<=0&&!t.incidentAssignment&&!t.withdrawing)t.Withdraw();}
        public static TacticalTransport Create(WantedSystem system,Vector3 at,int count,int rank=4)
        {
            CityVehicle vehicle;
            if(rank<3){var p=PoliceCar.Create(system,at);p.enabled=false;system.Cars.Add(p);vehicle=p.Vehicle;}
            else vehicle=UrbanSimulation.Instance.Spawn(at,false,3);
            vehicle.name=rank<3?"경찰서 출동 순찰차":"특수대응팀 장갑차";
            var t=vehicle.gameObject.AddComponent<TacticalTransport>();t.car=vehicle;t.owner=system;t.remaining=count;
            t.rank=rank;t.patrol=rank<3;vehicle.InitializeDurability();vehicle.health=vehicle.MaxHealth;vehicle.occupied=true;return t;
        }
        void OnEnable()=>all.Add(this);
        void OnDisable()=>all.Remove(this);
        IEnumerator Start()
        {
            yield return null;
            if(patrol){car.GetComponent<VehicleCabin>()?.SetPassengers(remaining);yield break;}
            SecurityVehicleArt.Install(car,false);
            car.GetComponent<VehicleCabin>()?.SetPassengers(remaining);
            yield break;

        }
        void Update()
        {
            var game=GameDirector.Instance;if(!car||!game||game.Blocked)return;
            if(car.Wrecked){remaining=0;return;}
            // Leave once off screen (or after 30 s) so a van withdrawn mid-fight does not vanish in view; a van Seoha took is kept.
            if(withdrawing){if(car.owned){enabled=false;return;}leave+=Time.deltaTime;if(leave>30||leave>1&&ResponseDispatch.Hidden(transform.position,4))Destroy(gameObject);return;}
            if(UrbanSimulation.Instance.Current==car){remaining=0;return;}
            elapsed+=Time.deltaTime;
            if(incidentAssignment&&(!incidentTarget||!incidentTarget.Alive||incidentTarget.Downed)&&!IncidentCommand.Emergency){remaining=0;car.speed=0;if(elapsed>100&&ResponseDispatch.Hidden(transform.position))Destroy(gameObject);return;}
            var goal=IncidentCommand.Emergency?IncidentCommand.Position:incidentTarget&&incidentTarget.Alive?incidentTarget.transform.position:owner.LastSeen;
            var delta=goal-transform.position;delta.y=0;
            var getaway=incidentTarget?incidentTarget.GetComponentInParent<GangGetaway>():null;
            if(!IncidentCommand.Emergency&&getaway&&remaining>0&&Mathf.Abs(getaway.GetComponent<CityVehicle>().speed)>3){route.Drive(car,goal,Mathf.Min(.05f,Time.deltaTime),8);return;}
            if(delta.magnitude>24&&remaining>0)
            {route.Drive(car,goal,Mathf.Min(.05f,Time.deltaTime),24);return;}
            car.speed=0;GetComponent<SecurityVehicleArt>()?.OpenRear();
            if(remaining<=0||elapsed<2)return;
            release-=Time.deltaTime;if(release>0)return;release=.65f;
            Vector3 door=transform.position-car.Forward*(car.HalfLength+1.7f)+transform.forward*((Deployed%2==0?1:-1)*.65f);
            if(!CityGangWar.FindGround(door,out var ground))return;
            var officer=PoliceOfficer.Create(owner,ground,rank,Deployed);
            if(incidentTarget){officer.Ambient=true;officer.Dispatch(incidentTarget);CitySafety.Instance?.Patrol.Add(officer);}else owner.Officers.Add(officer);
            if(Deployed==0&&rank>=4)CombatRobot.Create(ground+transform.forward*2,false,!incidentAssignment);
            NpcSpeech.Say(officer,NpcDialogueBank.Line(officer.GetComponent<CityNpc>(),"deployment"),3);
            remaining--;Deployed++;car.GetComponent<VehicleCabin>()?.SetPassengers(remaining);
        }
        public void Withdraw(){if(incidentTarget||IncidentCommand.Emergency)return;remaining=0;withdrawing=true;var lights=GetComponent<ResponseLightbar>();if(lights)lights.enabled=false;}
    }
}
