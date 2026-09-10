using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class MilitaryResponse:MonoBehaviour
    {
        readonly List<CityVehicle> vehicles=new();readonly List<WorldActor> soldiers=new();
        public RiftIncursion Incident {get;private set;}
        public int VehicleCount=>vehicles.Count;
        public int SoldierCount=>soldiers.Count;
        public Vector3 Target=>IncidentCommand.Emergency?IncidentCommand.Position:Incident?Incident.Position:WantedSystem.Instance.LastSeen;
        public bool Active=>!withdrawn&&(Incident?Incident.Active:WantedSystem.Level>0);
        bool withdrawn,fullyDeployed;
        public static CityVehicleType[] Deployment(bool incident,bool underwater,bool riftCity=false)
        {
            var kinds=incident?new[]{CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Tank,CityVehicleType.Fighter,CityVehicleType.CombatHelicopter,CityVehicleType.Tank}:new[]{CityVehicleType.Truck,CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Fighter};
            if(incident&&riftCity)kinds=new[]{CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Bomber,CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.Fighter,CityVehicleType.CombatHelicopter,CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Bomber,CityVehicleType.Tank,CityVehicleType.Fighter,CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Tank,CityVehicleType.Fighter,CityVehicleType.CombatHelicopter,CityVehicleType.Tank};
            if(underwater)for(int i=0;i<kinds.Length;i++)if(kinds[i]==CityVehicleType.Bomber||kinds[i]==CityVehicleType.Fighter||kinds[i]==CityVehicleType.CombatHelicopter)kinds[i]=CityVehicleType.Tank;
            return kinds;
        }
        public static MilitaryResponse ForIncident(RiftIncursion incident)
        {if(incident&&MilitaryBaseOperations.Handles(incident.Position))return null;var r=new GameObject("국방 출동 지휘 / 잠식체").AddComponent<MilitaryResponse>();r.Incident=incident;return r;}
        IEnumerator Start()
        {
            while(Active&&MilitaryBaseOperations.Handles(Target))yield return new WaitForSeconds(2);
            if(!Active)yield break;
            GameDirector.Instance.ToastNear(Incident?"방위기지에 긴급 지원 요청 · 중장비 출동 준비":"민간인 대규모 희생 확인 · 방위기지 출동 준비",Target,180,6);
            float wait=Incident?Incident.RiftCity?30:44:55;
            while(wait>0&&Active){if(!GameDirector.Instance.Blocked)wait-=Time.deltaTime;yield return null;}
            var kinds=Deployment(Incident,Target.y< -30,Incident&&Incident.RiftCity);
            for(int i=0;i<kinds.Length&&Active;i++)
            {
                bool aircraft=kinds[i]==CityVehicleType.CombatHelicopter||kinds[i]==CityVehicleType.Fighter||kinds[i]==CityVehicleType.Bomber;Vector3 at;
                while(Active&&!ResponseDispatch.TryOrigin(Target,true,aircraft,i,out _))yield return new WaitForSeconds(3);
                if(!Active||!ResponseDispatch.TryOrigin(Target,true,aircraft,i,out at))yield break;
                var car=UrbanSimulation.Instance.Spawn(at,false,(int)kinds[i]);car.name="방위기지 출동 / "+VehicleSeats.Title(kinds[i]);car.occupied=true;car.InitializeDurability();car.health=car.MaxHealth;
                car.transform.rotation=Quaternion.Euler(0,Vector3.SignedAngle(Vector3.right,Vector3.ProjectOnPlane(Target-at,Vector3.up),Vector3.up),0);
                car.gameObject.AddComponent<MilitaryVehicleAI>().Initialize(car,this);vehicles.Add(car);
                yield return new WaitForSeconds(Incident&&Incident.RiftCity?7:i<2?7:14);
            }
            fullyDeployed=true;
        }
        public void Deploy(Vector3 at,int index)
        {
            if(!Active||MilitaryBaseOperations.Handles(Target)||!CityGangWar.FindGround(at,out var safe))return;
            WorldActor body;
            if(Incident)body=ArmyResponder.Create(safe,index,Incident).Body;
            else{var s=PoliceOfficer.Create(WantedSystem.Instance,safe,4,index);s.name="군 긴급대응 소총수";s.Body.military=true;PeopleArt.Attach(s.gameObject,"Soldier");WantedSystem.Instance.Officers.Add(s);body=s.Body;}
            if(index==0)CombatRobot.Create(safe+Vector3.right*3,true);
            soldiers.Add(body);NpcSpeech.Say(body,NpcDialogueBank.Line(null,"deployment"),4,5);
        }
        public void Withdraw()
        {
            if(withdrawn)return;withdrawn=true;StopAllCoroutines();
            foreach(var s in soldiers)if(s&&!s.Downed){var officer=s.GetComponent<PoliceOfficer>();if(officer)officer.Withdraw();else Destroy(s.gameObject,18);}
            foreach(var v in vehicles)if(v){var ai=v.GetComponent<MilitaryVehicleAI>();if(ai)Destroy(ai);if(!v.owned)Destroy(v.gameObject,18);}
            if(Incident)Destroy(gameObject);else Destroy(this);
        }
        void Update(){if(!withdrawn&&!Active)Withdraw();else if(fullyDeployed&&!vehicles.Exists(v=>v&&!v.Wrecked)&&!soldiers.Exists(s=>s&&s.Alive))Withdraw();}
    }
}
