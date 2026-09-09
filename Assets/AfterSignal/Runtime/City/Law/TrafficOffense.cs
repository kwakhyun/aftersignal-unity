using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // A traffic suspect is a vehicle, not Seoha's wanted record or an armed gang actor.
    public sealed class TrafficOffense:MonoBehaviour
    {
        public static readonly List<TrafficOffense> Active=new();
        public CityVehicle Suspect {get;private set;}
        public bool Reported {get;private set;}public bool Resolved {get;private set;}
        public int Pursuers=>patrols.Count;
        readonly List<PoliceCar> patrols=new();float reportAt,nearTime,age;Vector3 accident;
        public static void Report(CityVehicle car,WorldActor victim,float speed)
        {
            var sim=UrbanSimulation.Instance;
            if(!car||!victim||!car.occupied||car.IsSpecial||speed<2||sim&&sim.Current==car||!LocalSimulation.Combat(car.transform.position))return;
            if(car.GetComponent<PoliceCar>()||car.GetComponent<TacticalTransport>()||car.GetComponent<MilitaryVehicleAI>()||car.GetComponent<FireEngine>()||car.GetComponent<EmergencyAmbulance>())return;
            var offense=car.GetComponent<TrafficOffense>();if(offense)return;
            if(Active.Count>=2)return;
            offense=car.gameObject.AddComponent<TrafficOffense>();offense.Suspect=car;offense.accident=victim.transform.position;offense.reportAt=Time.time+3;
            NpcSpeech.Say(victim,"차량이 사람을 쳤어요! 경찰과 구급대를 보내 주세요!",5,8);
            victim.GetComponent<MedicalState>()?.Report();
        }
        void OnEnable()=>Active.Add(this);
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Suspect)return;age+=Time.deltaTime;
            if(Resolved)return;
            if(!LocalSimulation.Within(transform.position,550)||age>150||Suspect.Wrecked||UrbanSimulation.Instance.Current==Suspect){Finish(false);return;}
            // Major disaster responders retain monster priority; resume traffic enforcement afterwards.
            if(IncidentCommand.Emergency){nearTime=0;return;}
            if(!Reported&&Time.time>=reportAt)
            {
                for(int i=0;i<2;i++)if(ResponseDispatch.TryOrigin(accident,false,false,i,out var at))
                {var police=PoliceCar.Create(WantedSystem.Instance,at);police.AssignTraffic(this);patrols.Add(police);}
                Reported=patrols.Count>0;reportAt=Time.time+5;
                if(Reported)g.ToastNear("보행자 사고 신고 접수 · 경찰이 가해 차량을 추격합니다",accident,110,4);
            }
            bool close=patrols.Exists(p=>p&&p.Vehicle&&!p.Vehicle.Wrecked&&(p.transform.position-transform.position).sqrMagnitude<24*24);
            if(close){nearTime+=Time.deltaTime;Suspect.traffic=false;var taxi=Suspect.GetComponent<CityTaxiService>();if(taxi)taxi.enabled=false;Suspect.speed=Mathf.MoveTowards(Suspect.speed,0,Time.deltaTime*9);}
            else nearTime=0;
            if(nearTime>4&&Mathf.Abs(Suspect.speed)<.8f)Finish(true);
        }
        void Finish(bool stopped)
        {
            if(Resolved)return;Resolved=true;
            if(stopped)
            {
                Suspect.traffic=false;Suspect.speed=0;
                var cabin=Suspect.GetComponent<VehicleCabin>();int seat=0;
                if(cabin)foreach(var role in cabin.ReleaseOccupants(false))
                {
                    var occupant=VehicleOccupant.Create(Suspect,role,seat++,false,transform.position);
                    if(seat==1){NpcSpeech.Say(occupant,"멈췄습니다… 제가 사고를 냈어요. 조사에 응하겠습니다.",6);var survivor=occupant.GetComponent<VehicleSurvivor>();if(survivor)Destroy(survivor);}
                }
                Suspect.occupied=false;
                GameDirector.Instance.ToastNear("경찰이 사고 차량을 정차시켰습니다 · 운전자 조사 및 부상자 구조",transform.position,100,5);
            }
            foreach(var p in patrols)if(p)p.Withdraw();Active.Remove(this);Destroy(this,stopped?20:0);
        }
        void OnDestroy(){Active.Remove(this);foreach(var p in patrols)if(p)p.Withdraw();}
    }
}
