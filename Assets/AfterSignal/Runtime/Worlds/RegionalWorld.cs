using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class RegionalWorld:MonoBehaviour
    {
        public static RegionalWorld Instance{get;private set;}
        public int Battles{get;private set;}
        public int BattleUnits=>units.Count;
        readonly List<WorldActor> units=new();float nextBattle;
        void Awake(){Instance=this;nextBattle=Time.time+10;}
        IEnumerator Start()
        {
            while(!FourCityWorld.Instance.Built)yield return null;
            var homeCar=UrbanSimulation.Instance.Spawn(RegionalCatalog.HomeQuarter+new Vector3(16,.1f,-25),false,(int)CityVehicleType.Sedan);homeCar.gameObject.AddComponent<RegionalParked>();homeCar.name="새벽 골목 / 이웃 주차 차량";
            var homeBike=UrbanSimulation.Instance.Spawn(RegionalCatalog.HomeQuarter+new Vector3(10,.1f,-18),false,(int)CityVehicleType.Motorcycle);homeBike.owned=true;homeBike.gameObject.AddComponent<RegionalParked>();homeBike.name="서하의 집 앞 루멘 바이크";
            foreach(var venue in FourCityWorld.Instance.Facilities)
            {
                var v=venue.Definition;
                if(v.kind==VenueKind.Bank||v.kind==VenueKind.Cafe||v.kind==VenueKind.Restaurant||v.id=="nova-medical")
                {
                    var cash=CashLocations.Make(venue.transform.TransformPoint(new Vector3(venue.roomWidth*.23f,.1f,venue.roomDepth*.3f)),false,v.kind==VenueKind.Bank);cash.transform.SetParent(venue.transform,true);cash.locationId=v.id;
                }
                if(v.kind==VenueKind.Military&&v.city==1)
                {
                    for(int i=0;i<6;i++)Park(venue,new(-120+i%3*36,.15f,90-i/3*33),CityVehicleType.Tank);
                    Park(venue,new(-65,.15f,94),CityVehicleType.Fighter);Park(venue,new(45,.15f,94),CityVehicleType.CombatHelicopter);
                }
                if(v.kind==VenueKind.Police)for(int i=0;i<3;i++){var p=venue.transform.TransformPoint(new(-v.size.x*.38f+i*7,.1f,-v.size.y*.37f));var car=PoliceCar.Create(WantedSystem.Instance,p);car.enabled=false;car.Vehicle.occupied=false;foreach(var audio in car.GetComponentsInChildren<AudioSource>())audio.Stop();car.gameObject.AddComponent<RegionalParked>();}
                if(v.kind==VenueKind.Island)Park(venue,new(13,OceanLife.Surface,-212),CityVehicleType.Boat);
                yield return null;
            }
            var ferry=UrbanSimulation.Instance.Spawn(new Vector3(1272,OceanLife.Surface,-725),false,(int)CityVehicleType.Boat);ferry.gameObject.AddComponent<RegionalFerry>();
        }
        void Park(VenueRuntime venue,Vector3 at,CityVehicleType type)
        {var car=UrbanSimulation.Instance.Spawn(venue.transform.TransformPoint(at),false,(int)type);car.gameObject.AddComponent<RegionalParked>();}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked)return;
            if(Time.time<nextBattle)return;nextBattle=Time.time+8;
            var p=g.Player.transform.position;if(FourCityCatalog.CityAt(p)!=2||p.y>6||RegionalCatalog.InRift(p,30))return;
            units.RemoveAll(u=>!u);int alive=0;foreach(var u in units)if(u&&u.Alive)alive++;
            if(alive>4)return;
            foreach(var u in units)if(u)Destroy(u.gameObject);units.Clear();
            TriggerConflict(p+Vector3.forward*60);nextBattle=Time.time+42;
        }
        public void TriggerConflict(Vector3 desired)
        {
            if(!RiftIncursion.Instance||RiftIncursion.Instance.Active||!ResponseDispatch.TryOrigin(desired,true,false,Battles%4,out desired))return;
            Vector3 at=desired;float best=float.MaxValue;
            foreach(var road in FourCityCatalog.Roads)for(int i=1;i<road.Length;i++)
            {var p=FourCityCatalog.Closest(desired,road[i-1],road[i]);if(FourCityCatalog.CityAt(p)!=2||RegionalCatalog.InRift(p,50))continue;float d=(desired-p).sqrMagnitude;if(d<best){best=d;at=p;}}
            for(int i=0;i<5;i++)
            {
                var p=at+new Vector3((i%3-1)*3,.1f,5+i/3*3);if(!CityGangWar.FindGround(p,out var ground))continue;
                units.Add(ErebosThreat.Spawn(ground,false,84000+Battles*100+i,transform));
            }
            RiftIncursion.Instance.Trigger(at);
            Battles++;GameDirector.Instance?.Toast("에레보스 격리선 교전 · 특수부대와 잠식체 충돌",5);
        }
        void OnDestroy(){RegionalErrand.Reset();if(Instance==this)Instance=null;}
    }
    public sealed class RegionalParked:MonoBehaviour{}
    public sealed class RegionalDistrict:MonoBehaviour
    {
        VenueRuntime venue;readonly List<WorldActor> patrol=new(),gangs=new();float next,gangAt;bool bound;
        public int PatrolCount=>patrol.Count;
        public void Initialize(VenueRuntime v){venue=v;gangAt=Time.time+12;}
        IEnumerator Start(){yield return new WaitForSeconds(1);CityLife.Instance?.BindResidents();foreach(var npc in venue.GetComponentsInChildren<CityNpc>())NeighborBond.Attach(npc);bound=true;}
        void Update()
        {
            var g=GameDirector.Instance;if(!bound||!g||g.Blocked||Time.time<next)return;next=Time.time+2;
            bool near=RegionalCatalog.Slum(g.Player.transform.position);
            if(near&&patrol.Count==0)for(int i=0;i<5;i++)
            {
                var p=RegionalCatalog.HomeQuarter+new Vector3(-36+i*15,.1f,-20);if(CityGangWar.FindGround(p,out var safe)){var officer=PoliceOfficer.Create(WantedSystem.Instance,safe,1+i%2,i);officer.Ambient=true;officer.name="새벽 저지대 집중 순찰";patrol.Add(officer.Body);CitySafety.Instance?.Patrol.Add(officer);}
            }
            foreach(var p in patrol)if(p)p.gameObject.SetActive(near||WantedSystem.Level>0);
            gangs.RemoveAll(x=>!x||!x.Alive);
            if(near&&(LifeState.Hour>=19||LifeState.Hour<6)&&Time.time>gangAt&&gangs.Count<6)
            {gangAt=Time.time+75;for(int i=0;i<3;i++){var p=g.Player.transform.position+new Vector3(32+i*3,0,24);if(CityGangWar.FindGround(p,out var at)){var gangster=GangMember.Create(at,1,i);gangster.gameObject.AddComponent<GangCrime>();gangs.Add(gangster.Body);}}}
        }
    }
    public sealed class NeighborBond:MonoBehaviour
    {
        CityNpc npc;float next;string background;
        public int Affinity{get;private set;}
        string Key=>"AFTERSIGNAL.Neighbor."+npc.identity;
        public static void Attach(CityNpc person)
        {
            if(!person||person.GetComponent<NeighborBond>())return;
            var bond=person.gameObject.AddComponent<NeighborBond>();bond.npc=person;bond.background=person.context;bond.Affinity=PlayerPrefs.GetInt(bond.Key,80);bond.Apply();bond.next=Time.time+12+person.variation;
        }
        void Apply()=>npc.context=background+" 서하는 새벽 저지대에서 함께 자란 이웃이다. 친밀도 "+Affinity+"/100. "+(Affinity>=60?"서하에게 격의 없이 안부를 묻고 어릴 적 함께 지낸 기억을 나눈다.":"폭력 때문에 신뢰가 흔들려 경계한다.");
        public void Greet(){Affinity=Mathf.Min(100,Affinity+1);Save();}
        public void Offended(){Affinity=Mathf.Max(0,Affinity-35);Save();}
        void Save(){Apply();if(!LifeState.SuppressSave){PlayerPrefs.SetInt(Key,Affinity);PlayerPrefs.Save();}}
        void Update(){var g=GameDirector.Instance;if(!g||g.Blocked||Time.time<next||!GetComponent<WorldActor>().Alive)return;if((g.Player.transform.position-transform.position).sqrMagnitude<11*11){next=Time.time+40;NpcSpeech.Say(npc,Affinity>=60?new[]{"서하야, 밥은 챙겨 먹었니?","우리 골목에서 보니까 반갑다. 집에 들렀어?","어릴 때 여기서 같이 놀았잖아. 아직 기억나?","필요한 거 있으면 말해. 우린 이웃이잖아."}[npc.variation%4]:"오늘은 조용히 지나가 줘.",4);}}
    }
}
