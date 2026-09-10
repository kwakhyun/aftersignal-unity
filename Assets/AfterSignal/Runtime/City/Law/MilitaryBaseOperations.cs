using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class MilitaryBaseOperations:MonoBehaviour
    {
        public sealed class Base
        {
            public string Name,Uniform;public Vector3 Center,Gate;public Vector2 Half;
            public WorldActor Threat;public float PlayerAlarmUntil,QuietUntil;public bool Active;
            public readonly List<GarrisonSupport> Personnel=new();public readonly List<CityVehicle> Vehicles=new();
            public bool Contains(Vector3 p,float margin=0)=>Mathf.Abs(p.x-Center.x)<Half.x+margin&&Mathf.Abs(p.z-Center.z)<Half.y+margin&&Mathf.Abs(p.y-Center.y)<85;
            public bool PlayerTarget=>Time.time<PlayerAlarmUntil&&GameDirector.Instance&&GameDirector.Instance.Player.Health>0;
            public Vector3 Target=>Threat&&Threat.Alive?Threat.Center:GameDirector.Instance?GameDirector.Instance.Player.Shoulder:Center;
        }
        public static MilitaryBaseOperations Instance{get;private set;}
        public bool Built{get;private set;}static List<Base> bases;public static List<Base> Bases
        {
            get
            {
                if(bases!=null)return bases;bases=new(){new Base{Name="루멘 방위기지",Uniform="Soldier",Center=new(420,0,840),Gate=MilitaryInstallation.Gate,Half=new(240,130)},new Base{Name="루멘 공군기지",Uniform="AirForceCrew",Center=MaritimeWorld.AirBase,Gate=MaritimeWorld.AirBase+Vector3.back*70,Half=new(102,88)},new Base{Name="해협 해군기지",Uniform="NavyCrew",Center=MaritimeWorld.NavalBase,Gate=MaritimeWorld.NavalBase+Vector3.back*44,Half=new(78,145)}};
                foreach(var v in FourCityCatalog.Venues)if(v.kind==VenueKind.Military&&!v.id.Contains("ruined"))bases.Add(new Base{Name=v.title,Uniform="Soldier",Center=v.position,Gate=v.Entrance,Half=v.size*.5f});return bases;
            }
        }
        public static Base At(Vector3 p){foreach(var b in Bases)if(b.Contains(p))return b;return null;}
        public static bool Restricted(Vector3 p)=>GameDirector.Instance&&GameDirector.Instance.stage==StageId.UrbanCity&&At(p)!=null;
        public static bool Handles(Vector3 p)=>Instance&&At(p)!=null;
        public static void ReportAttack(Vector3 at,WorldActor source)
        {
            if(!Instance)return;var b=At(at);if(b==null||source&&(source.military||source.police||source.environmental||source.terrorist))return;
            if(source)b.Threat=source;else b.PlayerAlarmUntil=Time.time+90;b.Active=true;b.QuietUntil=Time.time+15;
        }
        readonly List<WorldActor> nearby=new();float scan;
        void Awake(){Instance=this;bases=null;}
        IEnumerator Start()
        {
            while(!GameDirector.Instance.Ready||!UrbanSimulation.Instance||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            yield return GarrisonArchitecture.BuildAll(transform);Built=true;
        }
        public static void Enlist(WorldActor body,Base site,Vector3? door=null)
        {
            if(!body||!body.Alive||body.gang||body.monster||body.terrorist||body.helicopter||body.robot||body.environmental)return;
            var pedestrian=body.GetComponent<CityPedestrian>();if(pedestrian&&CityPopulation.Instance)CityPopulation.Instance.Citizens.Remove(pedestrian);
            body.military=true;body.police=false;body.health=Mathf.Max(body.health,120);
            var npc=body.GetComponent<CityNpc>();if(npc){npc.occupation=site.Uniform=="AirForceCrew"?"공군 기지 대원":site.Uniform=="NavyCrew"?"해군 기지 대원":"방위군 기지 대원";npc.context=site.Name+"에 배속된 군인. 경보 시 무장하고 주둔 장비를 운용한다.";}
            var art=body.GetComponent<RegionalUniform>()??body.gameObject.AddComponent<RegionalUniform>();art.art=site.Uniform;PeopleArt.Attach(body.gameObject,site.Uniform);
            var routine=body.GetComponent<CivicRoutine>();if(routine)routine.prisoner=false;
            GarrisonSupport.Register(body,door??GarrisonArchitecture.ExitFor(body.transform.position,site.Gate));var duty=body.GetComponent<GarrisonSupport>();duty.Base=site;duty.SetExit(door??GarrisonArchitecture.ExitFor(body.transform.position,site.Gate));if(!site.Personnel.Contains(duty))site.Personnel.Add(duty);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||g.stage!=StageId.UrbanCity||Time.time<scan)return;scan=Time.time+.75f;
            foreach(var site in Bases)
            {
                if(!LocalSimulation.Within(site.Center,650))continue;
                ActorSpatialIndex.Nearby(site.Center,Mathf.Max(site.Half.x,site.Half.y)*1.5f,nearby);
                foreach(var a in nearby)
                {
                    if(!a||!site.Contains(a.transform.position))continue;
                    if(!a.military)Enlist(a,site);else if(!a.helicopter&&!a.robot&&(!a.GetComponent<GarrisonSupport>()||a.GetComponent<GarrisonSupport>().Base!=site))Enlist(a,site);
                    if(a.Alive&&!a.Downed&&(a.monster||a.gang)&&(!site.Threat||!site.Threat.Alive||a.monster))site.Threat=a;
                }
                site.Personnel.RemoveAll(s=>!s);site.Vehicles.RemoveAll(v=>!v);
                foreach(var car in UrbanSimulation.Instance.Cars)if(car&&site.Contains(car.transform.position)&&(VehicleCrew.Role(car)=="Soldier"||VehicleCrew.Role(car)=="AirForceCrew"||VehicleCrew.Role(car)=="NavyCrew")&&!(car.GetComponent<FireEngineArt>()||car.GetComponent<PoliceCar>()||car.GetComponent<TacticalTransport>())&&!site.Vehicles.Contains(car)){site.Vehicles.Add(car);if(!car.GetComponent<RegionalParked>())car.gameObject.AddComponent<RegionalParked>();}
                bool threat=site.Threat&&site.Threat.Alive&&!site.Threat.Downed&&site.Contains(site.Threat.transform.position,90);
                if(!threat)site.Threat=null;
                if(site.Contains(g.Player.transform.position)&&WantedSystem.Level>0&&Time.time<site.PlayerAlarmUntil+15)site.PlayerAlarmUntil=Time.time+20;
                if(threat||site.PlayerTarget){site.Active=true;site.QuietUntil=Time.time+12;}
                else if(Time.time>site.QuietUntil)site.Active=false;
                if(!site.Active)continue;
                // No roster cap: every living stationed soldier is activated. Boarding is distributed by each unit.
                foreach(var soldier in site.Personnel)if(soldier)soldier.Base=site;
            }
        }
        public static CityVehicle ReserveVehicle(GarrisonSupport soldier,Base site)
        {
            CityVehicle best=null;float nearest=160*160;
            foreach(var v in site.Vehicles)
            {
                if(!v||v.Wrecked||v.occupied||v.owned||v.GetComponent<GarrisonVehicleDriver>()||v.GetComponent<VehicleClaim>()||!(v.GetComponent<MilitaryGunTruck>()||v.type==CityVehicleType.Tank||v.type==CityVehicleType.CombatHelicopter||v.type==CityVehicleType.Fighter||v.type==CityVehicleType.Bomber))continue;
                float d=(soldier.transform.position-v.transform.position).sqrMagnitude;if(d<nearest){nearest=d;best=v;}
            }
            if(best)best.gameObject.AddComponent<VehicleClaim>().Soldier=soldier;return best;
        }
        void OnDestroy(){if(Instance==this){Instance=null;bases=null;}}
    }
    public sealed class VehicleClaim:MonoBehaviour{public GarrisonSupport Soldier;void Update(){if(!Soldier||!Soldier.GetComponent<WorldActor>().Alive)Destroy(this);}}
}
