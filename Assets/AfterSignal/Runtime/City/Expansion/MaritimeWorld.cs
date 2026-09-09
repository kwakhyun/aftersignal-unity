using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public enum SeaFaction{CoastGuard,Navy,Pirates}
    public sealed class MaritimeWorld:MonoBehaviour
    {
        public static MaritimeWorld Instance{get;private set;}
        public static readonly Vector3 NavalBase=new(2050,.1f,-548),AirBase=new(1655,.1f,832),CoastBase=new(1170,.1f,-606);
        public readonly List<SeaCombat> Fleet=new();public bool Built{get;private set;}float next;
        void Awake(){Instance=this;}
        IEnumerator Start()
        {
            while(!FourCityWorld.Instance||!FourCityWorld.Instance.Built||!UrbanSimulation.Instance)yield return null;yield return new WaitForSeconds(3);
            Base(NavalBase,"해군 · 해협 방위사령부",true);Base(AirBase,"공군 · 루멘 비행단",false);Base(CoastBase,"해양경찰 · 구조 경비대",true,true);
            // The original Lumen air detachment stays in place; the new airfield has its own fleet.
            for(int i=0;i<3;i++)ParkAircraft(AirBase+new Vector3(-28+i*44,.15f,46),CityVehicleType.Fighter);
            for(int i=0;i<2;i++)ParkAircraft(AirBase+new Vector3(12+i*52,.15f,-22),CityVehicleType.CombatHelicopter);
            for(int i=0;i<2;i++)ParkAircraft(AirBase+new Vector3(-31+i*82,.2f,-45),CityVehicleType.Bomber);
            for(int i=0;i<2;i++){var ship=UrbanSimulation.Instance.Spawn(new Vector3(NavalBase.x+31+i*49,OceanLife.Surface,NavalBase.z-95),false,(int)CityVehicleType.Boat);ship.transform.rotation=Quaternion.Euler(0,90,0);ship.gameObject.AddComponent<MaritimeHull>().Faction=i==0?SeaFaction.Navy:SeaFaction.CoastGuard;ship.gameObject.AddComponent<RegionalParked>();ship.name="해군 기지 정박 / "+(i==0?"방위 함정":"구조 지원함");}
            for(int i=0;i<4;i++)Spawn(new Vector3(1980+i*65,OceanLife.Surface,-780-i*40),i%2==0?SeaFaction.Navy:SeaFaction.CoastGuard);
            for(int i=0;i<3;i++)Spawn(new Vector3(1860+i*60,OceanLife.Surface,-1070-i*45),SeaFaction.Pirates);
            HarborParcelRepair.Apply();Built=true;next=Time.time+60;
        }
        void Base(Vector3 at,string title,bool dock,bool coast=false)
        {
            if(!coast){DefenseBaseArchitecture.Build(transform,at,dock);DutyCrew(at,dock);MilitaryArmory.Build(transform,at+new Vector3(dock?-39:-72,0,-36));return;}
            var root=new GameObject(title).transform;root.SetParent(transform,false);root.position=at;var b=new CityGeometry(root);float width=coast?28:72,depth=coast?30:56;
            b.Box("Service platform",new(0,-.22f,0),new(width,.4f,depth),"Metal",true);
            b.Box("Operations block",new(-width*.24f,3,6),new(width*.35f,6,depth*.55f),"DarkMetal",true);
            b.Box("Operations windows",new(-width*.24f,3.7f,-depth*.18f),new(width*.3f,1.8f,.12f),"Glass");b.Sign(title,new(0,5,-depth*.48f),coast?.3f:.48f);
            b.Box("Command roof",new(-width*.24f,6.15f,6),new(width*.38f,.3f,depth*.58f),"Metal");
            for(int i=0;i<4;i++)b.Box("Security bollard",new(width*.08f+i*width*.1f,1,-depth*.47f),new(.3f,2,.3f),"Metal",true);
            if(dock){b.Box("Service pier",new(0,-.15f,-depth*.5f-40),new(coast?10:18,.5f,80),"Concrete",true);for(int i=0;i<8;i++)b.Box("Pier side light",new((i%2==0?-1:1)*(coast?4:8),.3f,-depth*.5f-i/2*20),new(.3f,.6f,.3f),"NeonCyan");}
            else{b.Box("Hangar back",new(16,8,15),new(30,16,.4f),"Metal",true);b.Box("Hangar roof",new(16,16,0),new(31,.5f,31),"Metal",true);for(int s=-1;s<=1;s+=2)b.Box("Hangar side",new(16+s*15,8,0),new(.5f,16,30),"Metal",true);}
            b.Finish();
            for(int i=0;i<(coast?4:8);i++)
            {
                var p=at+new Vector3((i%4-1.5f)*4,0,-8-i/4*4);if(!CrowdFlow.Place(p,i,out p,5))continue;
                var guard=PoliceOfficer.Create(WantedSystem.Instance,p,coast?2:4,i);guard.Ambient=true;guard.Body.military=!coast;guard.Body.police=coast;string art=coast?"CoastGuard":dock?"NavyCrew":"AirForceCrew";
                PeopleArt.Attach(guard.gameObject,art);guard.gameObject.AddComponent<RegionalUniform>().art=art;var npc=guard.GetComponent<CityNpc>();npc.occupation=coast?"해양경찰":dock?"해군 경계병":"공군 기지 경계병";guard.name=npc.occupation;
            }
        }
        void ParkAircraft(Vector3 at,CityVehicleType kind)
        {var car=UrbanSimulation.Instance.Spawn(at,false,(int)kind);car.gameObject.AddComponent<RegionalParked>();car.name="공군 비행단 / 대기 "+VehicleSeats.Title(kind);}
        void DutyCrew(Vector3 at,bool navy)
        {
            string art=navy?"NavyCrew":"AirForceCrew";float x=navy?-45:-60;
            for(int i=0;i<16;i++)
            {
                var p=i<6?at+new Vector3(x-12+i%5*6,.1f,i<5?22:5):at+new Vector3(-22+(i-6)%5*22,.1f,(i-6)/5*18-22);
                if(!CrowdFlow.Place(p,i,out p,4))continue;var guard=PoliceOfficer.Create(WantedSystem.Instance,p,4,i);guard.Ambient=true;guard.Body.military=true;guard.Body.police=false;GarrisonSupport.Register(guard.Body,at+new Vector3(0,0,navy?-44:-70));
                PeopleArt.Attach(guard.gameObject,art);guard.gameObject.AddComponent<RegionalUniform>().art=art;
                var npc=guard.GetComponent<CityNpc>();string role=(navy?"해군 ":"공군 ")+(i<5?"관제 대원":i%3==0?"정비 대원":i%3==1?(navy?"함정 승조원":"조종사"):"기지 경계병");npc.Configure(96000+(navy?0:100)+i,role,null,"군사 통제 구역에서 근무한다. 정비, 관제, 경계 임무를 담당하며 민간인은 위병소로 안내한다.");guard.name=role;
            }
        }
        public SeaCombat Spawn(Vector3 at,SeaFaction faction){var c=UrbanSimulation.Instance.Spawn(at,false,(int)CityVehicleType.Boat);var sea=c.gameObject.AddComponent<SeaCombat>();sea.Initialize(c,faction);Fleet.Add(sea);return sea;}
        void Update()
        {
            if(!Built||Time.time<next||!GameDirector.Instance||GameDirector.Instance.Blocked)return;next=Time.time+50;Fleet.RemoveAll(x=>!x);var p=GameDirector.Instance.Player.transform.position;if(!OceanLife.Contains(p)||p.y< -10)return;
            int nearby=0;foreach(var s in Fleet)if(s&&(s.transform.position-p).sqrMagnitude<600*600)nearby++;
            if(nearby<5&&Fleet.Count<18){var at=p+Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward*Random.Range(280,450);at.y=OceanLife.Surface;if(OceanLife.Contains(at))Spawn(at,nearby%3==0?SeaFaction.Pirates:nearby%2==0?SeaFaction.Navy:SeaFaction.CoastGuard);}
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class MaritimeHull:MonoBehaviour
    {
        public SeaFaction Faction;public float Length=>Faction==SeaFaction.Navy?72:Faction==SeaFaction.CoastGuard?28:18;public float Beam=>Faction==SeaFaction.Navy?12:Faction==SeaFaction.CoastGuard?6.4f:4.5f;
        public Vector3 Helm=>new(-Length*.08f,Faction==SeaFaction.Navy?8.3f:Faction==SeaFaction.CoastGuard?4.5f:2.9f,-.6f);
        Transform radar;Quaternion radarRest;CityVehicle car;
        IEnumerator Start()
        {
            yield return null;car=GetComponent<CityVehicle>();var prefab=Resources.Load<GameObject>("Maritime/"+(Faction==SeaFaction.Navy?"NavalDestroyer":Faction==SeaFaction.CoastGuard?"CoastGuardCutter":"PirateInterceptor"));if(!prefab)yield break;
            foreach(var r in GetComponentsInChildren<MeshRenderer>())r.enabled=false;foreach(var c in GetComponentsInChildren<Collider>())Destroy(c);
            var model=Instantiate(prefab,transform);MaritimeMaterials.Apply(model,Faction);var hull=gameObject.AddComponent<BoxCollider>();hull.center=Vector3.up*(Faction==SeaFaction.Navy?3:1.6f);hull.size=new Vector3(Length,Faction==SeaFaction.Navy?7:4,Beam);
            foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name=="RotatingRadar"){radar=t;radarRest=t.localRotation;}
            car.GetComponent<VehicleCabin>()?.SetCrew(Faction==SeaFaction.Navy?"NavyCrew":Faction==SeaFaction.CoastGuard?"CoastGuard":"SeaRaider",Faction==SeaFaction.Navy?8:4);
        }
        void LateUpdate(){if(radar&&LocalSimulation.Within(transform.position,650))radar.localRotation=radarRest*Quaternion.Euler(0,0,Time.time*36);}
    }
    public sealed class SeaCombat:MonoBehaviour
    {
        public CityVehicle Car{get;private set;}public WorldActor Body{get;private set;}public SeaFaction Faction;public SeaCombat Target{get;private set;}public int Shots{get;private set;}
        CityVehicle civilianTarget;Vector3 home;float scan,fire,healthMirror,age,alert;public Vector3 AimCenter=>transform.position+Vector3.up*(Faction==SeaFaction.Navy?5:2);
        public void Initialize(CityVehicle car,SeaFaction faction)
        {
            Car=car;Faction=faction;home=transform.position;Car.traffic=false;Car.occupied=true;gameObject.AddComponent<MaritimeHull>().Faction=faction;Car.health=Car.MaxHealth;Car.durabilityVersion=1;healthMirror=Car.health;
            Body=gameObject.AddComponent<WorldActor>();Body.health=Car.health;Body.helicopter=true;Body.police=faction==SeaFaction.CoastGuard;Body.military=faction==SeaFaction.Navy;Body.gang=faction==SeaFaction.Pirates;
            name=faction==SeaFaction.Navy?"해군 함정 / 해협 방위함":faction==SeaFaction.CoastGuard?"해양경찰 경비함":"해적 갱단 / 해협 약탈선";
        }
        public void ManualFire(){if(Time.time<fire)return;fire=Time.time+(Faction==SeaFaction.Navy?2.6f:1.3f);Shots++;var cam=Camera.main;MaritimeWeapons.Fire(this,Ballistics.AimPoint(cam.ViewportPointToRay(new Vector3(.5f,.5f)),Car.transform),true);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Car||!Body)return;float dt=Mathf.Min(Time.deltaTime,.05f);age+=dt;
            if(Body.health<healthMirror&&Car.health>=healthMirror)Car.Damage(healthMirror-Body.health,AimCenter,Body.LastPlayerHit>Time.time-1?null:TrafficDamageSource.Environment,false);
            Body.health=Car.health;healthMirror=Car.health;if(Car.Wrecked){CityIncidentBoard.Neutralized(Body);return;}var sim=UrbanSimulation.Instance;if(sim.Current==Car&&sim.SeatIndex==0)return;
            if(!LocalSimulation.Combat(transform.position))
            {
                Target=null;civilianTarget=null;Car.speed=0;scan=Time.time+.5f;CityEventGate.Cancel(Body);return;
            }
            if(Time.time>scan){scan=Time.time+1;Target=null;float d=350*350;foreach(var other in MaritimeWorld.Instance.Fleet)if(other&&other!=this&&!other.Car.Wrecked&&LocalSimulation.Combat(other.transform.position)&&((Faction==SeaFaction.Pirates)!=(other.Faction==SeaFaction.Pirates))){float n=(other.AimCenter-AimCenter).sqrMagnitude;if(n<d){d=n;Target=other;}}}
            if(Faction==SeaFaction.Pirates&&!Target&&Time.time>scan-.1f){civilianTarget=null;float closest=400*400;foreach(var c in sim.Cars)if(c&&c!=Car&&!c.Wrecked&&c.IsWatercraft&&!c.GetComponent<SeaCombat>()){float d=(c.transform.position-transform.position).sqrMagnitude;if(d<closest){closest=d;civilianTarget=c;}}}
            if(civilianTarget&&!LocalSimulation.Combat(civilianTarget.transform.position))civilianTarget=null;
            var pirate=Faction==SeaFaction.Pirates?this:Target&&Target.Faction==SeaFaction.Pirates?Target:null;
            if((Target||civilianTarget)&&(!pirate||!CityEventGate.JoinGang(pirate.Body))){Target=null;civilianTarget=null;}
            Vector3 destination=Target?Target.transform.position:civilianTarget?civilianTarget.transform.position:home+new Vector3(Mathf.Sin(age*.015f+GetInstanceID())*230,0,Mathf.Cos(age*.015f+GetInstanceID())*180);var delta=destination-transform.position;delta.y=0;
            float desired=Target?delta.magnitude>130?Faction==SeaFaction.Pirates?19:14:7:12;var heading=Target&&delta.magnitude<160?Quaternion.Euler(0,75,0)*delta.normalized:delta.normalized;
            foreach(var other in MaritimeWorld.Instance.Fleet)if(other&&other!=this){var away=transform.position-other.transform.position;away.y=0;float separation=Car.HalfLength+other.Car.HalfLength+12;if(away.magnitude<separation)heading+=away.normalized*(separation-away.magnitude)/separation*2;}
            heading=SeaTraffic.Steer(Car,heading);Car.speed=Mathf.MoveTowards(Car.speed,desired,dt*2);var next=transform.position+heading*Car.speed*dt;next.y=OceanLife.Surface;
            if(OceanLife.Contains(next)){transform.position=next;if(heading.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,Mathf.Atan2(-heading.z,heading.x)*Mathf.Rad2Deg,0),dt*(Faction==SeaFaction.Navy?12:30));}else home=transform.position-heading*70;
            if((Target||civilianTarget)&&Time.time>fire&&delta.magnitude<350)
            {
                fire=Time.time+(Faction==SeaFaction.Navy?2.6f:1.3f);Shots++;MaritimeWeapons.Fire(this,Target?Target.AimCenter:civilianTarget.transform.position+Vector3.up*2,false);
                if(Time.time>alert){alert=Time.time+25;NpcSpeech.Say(this,Faction==SeaFaction.Pirates?"해적선: 경비함이 온다! 항로를 빼앗아!":Faction==SeaFaction.Navy?"해군: 민간 선박을 보호한다. 해적선을 제압하라!":"해양경찰: 즉시 정선하라! 민간인은 안전 해역으로 이동하세요!",5,5);}
            }
            if(age>300&&(transform.position-g.Player.transform.position).sqrMagnitude>1400*1400)Destroy(gameObject);
        }
        void OnDestroy(){if(Body)CityEventGate.Cancel(Body);}
    }
}
