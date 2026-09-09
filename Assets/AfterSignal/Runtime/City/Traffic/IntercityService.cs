using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class IntercityService:MonoBehaviour
    {
        public static readonly List<IntercityService> All=new();
        public CityVehicle Car {get;private set;}
        public bool Aircraft {get;private set;}
        public bool Boarding=>dwell>0 && !unloading && Car && Car.occupied && !Car.Wrecked;
        public int Arrivals {get;private set;}
        public int Boarded {get;private set;}
        public int Disembarked {get;private set;}
        public int Fare=>Aircraft?240:80;
        public int Seats=>Aircraft?22:12;
        public bool AtNova {get;private set;}
        public Vector3 ExitPoint=>Aircraft?(AtNova?new Vector3(1868,.2f,-3996):new Vector3(1408,.2f,794)):AtNova?new Vector3(927.4f,.1f,-2390):new Vector3(1250,.1f,-664);
        public Vector3 BoardPoint=>Car.GetComponent<TransportAccess>()?Car.GetComponent<TransportAccess>().Dock:VehicleSeats.Door(Car);
        public string Status=>Car&&Car.Wrecked?"운항 중단":(AtNova?"애프터라이트행":"노바행")+" · "+(Boarding?"탑승 "+Mathf.CeilToInt(dwell)+"초 · "+Manifest.Count+" / "+Seats+"명":phase)+" · "+Fare+" C";
        public readonly List<TransitTraveller> Manifest=new();
        readonly List<TransitTraveller> travellers=new();
        Vector3[] path;int leg;float dwell=50,nextUnload;string phase="운항 중";bool exchange,unloading;TransitTraveller approaching;
        public void Initialize(CityVehicle car,bool aircraft,bool nova=false)
        {
            Car=car;Aircraft=aircraft;AtNova=nova;car.occupied=true;car.traffic=false;All.Add(this);
            StartCoroutine(Setup());
        }
        IEnumerator Setup()
        {
            yield return null;Car.GetComponent<VehicleCabin>()?.SetPassengers(0);
            int pool=Aircraft?28:18;var created=new bool[pool];int remaining=pool;bool firstPass=true;
            while(remaining>0&&Car&&!Car.Wrecked)
            {
            for(int i=0;i<pool;i++)
            {
                if(created[i])continue;
                while(!PopulationBudget.ClaimFrame())yield return null;
                if(!TryTerminalPosition(i%2==0,i,out var spawn))continue;
                var go=new GameObject("Traveller / "+(Aircraft?"LA 207":"BW 12")+" / "+i,typeof(SpriteRenderer),typeof(CityNpc),typeof(TransitTraveller));
                go.transform.position=spawn;
                var t=go.GetComponent<TransitTraveller>();t.Art=FacilityPeople.Key((Aircraft?32:24)+i%16);t.Service=this;t.Serial=i;t.AtNova=i%2==0;
                go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");go.GetComponent<SpriteRenderer>().sprite=PeopleArt.Get(t.Art,0);
                go.GetComponent<CityNpc>().Configure(18000+(Aircraft?200:0)+(AtNova?100:0)+i,Aircraft?"도시 간 항공 승객":"해협 통근 승객",null,"애프터라이트와 노바를 오가는 주민. 목적지 터미널에서 내려 일상을 보낸다.");PeopleArt.Attach(go,t.Art);
                t.Target=spawn;t.WantsBoard=Boarding&&t.AtNova==AtNova;t.Cooldown=Time.time+4+i%5;travellers.Add(t);created[i]=true;remaining--;
            }
            if(firstPass){firstPass=false;if(Boarding)Exchange();}
            if(remaining>0)yield return new WaitForSeconds(3);
            }
        }
        public Vector3 Terminal(bool nova)=>Aircraft?(nova?new Vector3(1855,.2f,-3975):new Vector3(1394,.2f,780)):nova?new Vector3(927.4f,.1f,-2404):new Vector3(1250,.1f,-672);
        public bool TryTerminalPosition(bool nova,int serial,out Vector3 position)
        {
            var terminal=Terminal(nova);
            for(int attempt=0;attempt<8;attempt++)
            {
                int slot=serial+attempt*7+(AtNova?17:0);float angle=slot*2.399963f;float radius=3+Mathf.Sqrt(slot%43)*2.8f;
                var candidate=terminal+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                if(PopulationBudget.Room(candidate)&&CrowdFlow.Place(candidate,slot,out position,3))return true;
            }
            position=default;return false;
        }
        public bool ApproachTurn(TransitTraveller traveller)
        {
            if(!approaching||!approaching.Healthy||approaching.Inside||!approaching.WantsBoard||approaching.AtNova!=AtNova)approaching=traveller;
            return approaching==traveller;
        }
        public bool BuyTicket()
        {
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;
            if(!Boarding||exchange&&dwell<6||!sim||!g||sim.Current)return false;
            if(Manifest.Count>=Seats){g.Toast("만석입니다 · 다음 교통편을 이용하세요");return false;}
            if(LifeState.Credits<Fare){g.Toast("교통비가 부족합니다 · "+Fare+" C");return false;}
            if(!sim.Enter(Car,VehicleSeats.Count(Car)-1))return false;
            LifeState.Spend(Fare);g.Toast((Aircraft?"항공권":"승선권")+" 결제 완료 · "+Fare+" C / F 이동 중 탈출");return true;
        }
        void Exchange()
        {
            exchange=true;unloading=true;Unload();
            foreach(var t in travellers)if(t&&!t.Inside&&t.AtNova==AtNova&&t.Healthy){t.WantsBoard=true;t.Cooldown=Mathf.Max(t.Cooldown,Time.time+4+t.Serial%5);}
        }
        void Unload()
        {
            nextUnload=Time.time+.5f;
            var cabin=Car.GetComponent<VehicleCabin>();
            for(int i=Manifest.Count-1;i>=0;i--)
            {
                var t=Manifest[i];if(!t){Manifest.RemoveAt(i);continue;}
                if(!TryTerminalPosition(AtNova,t.Serial,out var destination)||!CrowdFlow.Place(ExitPoint,t.Serial,out var exit,18))continue;
                t.Inside=false;t.AtNova=AtNova;t.transform.position=exit;t.Target=destination;t.Cooldown=Time.time+26;t.gameObject.SetActive(true);Manifest.RemoveAt(i);Disembarked++;
            }
            cabin?.SetManifest(Manifest.ConvertAll(x=>x.Art));unloading=Manifest.Count>0;
        }
        public void Accept(TransitTraveller t)
        {
            int capacity=Seats-(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==Car?1:0);
            if(!Boarding||dwell<3||t.Inside||!t.Healthy||Manifest.Count>=capacity)return;
            Manifest.Add(t);t.Inside=true;t.WantsBoard=false;t.gameObject.SetActive(false);
            if(approaching==t)approaching=null;
            Car.GetComponent<VehicleCabin>()?.SetManifest(Manifest.ConvertAll(x=>x.Art));Boarded++;
        }
        void Depart()
        {
            exchange=false;approaching=null;foreach(var t in travellers)if(t){t.WantsBoard=false;if(!t.Inside)t.Target=t.transform.position;}
            if(Aircraft)
            {
                path=AtNova?new[]{new Vector3(1910,.2f,-3990),new Vector3(1940,.2f,-4070),new Vector3(1660,12,-4070),new Vector3(1300,105,-4070),new Vector3(1080,320,-3750),new Vector3(1080,320,-1700),new Vector3(950,320,400),new Vector3(1060,150,910),new Vector3(1315,10,910),new Vector3(1600,.2f,910),new Vector3(1500,.2f,840),new Vector3(1360,.2f,800),new Vector3(1400,.2f,800)}:
                    new[]{new Vector3(1360,.2f,845),new Vector3(1360,.2f,910),new Vector3(1660,12,910),new Vector3(2030,130,910),new Vector3(2110,320,500),new Vector3(2140,320,-1700),new Vector3(2140,320,-3750),new Vector3(2120,140,-4070),new Vector3(1940,10,-4070),new Vector3(1650,.2f,-4070),new Vector3(1740,.2f,-3990),new Vector3(1860,.2f,-3990)};
            }
            else path=AtNova?new[]{new Vector3(920,OceanLife.Surface,-2280),new Vector3(1080,OceanLife.Surface,-1700),new Vector3(1170,OceanLife.Surface,-950),new Vector3(1272.5f,OceanLife.Surface,-690),NeonHarbor.OldDock}:
                new[]{new Vector3(1170,OceanLife.Surface,-950),new Vector3(1080,OceanLife.Surface,-1700),new Vector3(920,OceanLife.Surface,-2280),NeonHarbor.NewDock};
            leg=0;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Car)return;
            if(Car.Wrecked){ReleaseAfterCrash();return;}
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current==Car&&sim.SeatIndex==0||!Car.occupied)return;
            float dt=Mathf.Min(.05f,Time.deltaTime);
            if(dwell>0){Car.speed=0;if(unloading&&Time.time>=nextUnload)Unload();dwell=unloading?Mathf.Max(2,dwell-dt):dwell-dt;if(dwell<=0)Depart();return;}
            if(path==null)return;var delta=path[leg]-transform.position;float distance=delta.magnitude;
            float target=Aircraft?(path[leg].y>100?95:path[leg].y>1?45:11):23;
            phase=Aircraft?(leg<2?"지상 활주":leg<4?"이륙 / 상승":leg<7?"순항":leg<10?"접근 / 착륙":"도착 게이트 이동"):"해협 항해 중";
            Car.speed=Mathf.MoveTowards(Car.speed,Mathf.Min(target,Mathf.Sqrt(distance*(Aircraft?15:7))),dt*(Aircraft?9:3));
            var horizontal=Vector3.ProjectOnPlane(delta,Vector3.up);if(horizontal.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,Mathf.Atan2(-horizontal.z,horizontal.x)*Mathf.Rad2Deg,0),dt*(Aircraft?32:30));
            var step=Vector3.ClampMagnitude(delta,Mathf.Max(.5f,Car.speed)*dt);
            if(Aircraft&&StructuralImpact.CheckCraft(Car,step))return;
            transform.position+=step;Car.fuel=Mathf.Max(0,Car.fuel-step.magnitude*.0003f);
            if(distance<.25f){leg++;if(leg>=path.Length){AtNova=!AtNova;Arrivals++;dwell=55;Car.speed=0;Car.fuel=45;Exchange();if(sim&&sim.Current==Car)g.Toast((AtNova?"노바":"애프터라이트")+" 도착 · F 하차 / 55초 후 재출발",6);}}
        }
        public void ReleaseAfterCrash(){foreach(var t in Manifest)if(t){t.Inside=false;t.WantsBoard=false;t.gameObject.SetActive(true);t.transform.position=transform.position+Random.insideUnitSphere*3;t.GetComponent<WorldActor>()?.Damage(999,Vector3.up*4,Car?Car.DamageSource:TrafficDamageSource.Environment);}Manifest.Clear();}
        void OnDestroy(){All.Remove(this);foreach(var t in travellers)if(t)Destroy(t.gameObject);}
    }
    public sealed class TransitTraveller:MonoBehaviour
    {
        public IntercityService Service;public string Art;public int Serial;public bool AtNova,Inside,WantsBoard;public Vector3 Target;public float Cooldown;
        public bool Healthy=>!GetComponent<WorldActor>()||GetComponent<WorldActor>().Alive;
        float nextVisibility,tick,lastTick;SpriteRenderer sprite;readonly PursuitPath path=new();float stuck;Vector3 previous;PedestrianSteering steering;
        public bool PlaceAtTerminal(){if(!Service.TryTerminalPosition(AtNova,Serial,out var p))return false;transform.position=Target=p;ActorSpatialIndex.Changed();return true;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Service||Inside||!Healthy)return;
            var npc=GetComponent<CityNpc>();if(npc&&npc.Fleeing)return;
            if(Time.time>nextVisibility){nextVisibility=Time.time+.5f;if(!sprite)sprite=GetComponent<SpriteRenderer>();if(sprite)sprite.enabled=(transform.position-g.Player.transform.position).sqrMagnitude<250*250;}
            if(!ActorWorkBudget.Tick(this,ref tick,ref lastTick,out var dt))return;
            bool boarding=WantsBoard&&Service.Boarding&&Service.AtNova==AtNova&&Time.time>Cooldown&&Service.ApproachTurn(this);
            if(boarding)Target=Service.BoardPoint;
            var d=Target-transform.position;if(d.sqrMagnitude>1.4f*1.4f){var dir=path.Direction(transform.position,Target);if(!steering)steering=PedestrianSteering.For(this);steering.Move(transform.position+dir*2,dt*2.1f);}
            stuck=(transform.position-previous).sqrMagnitude<.0001f?stuck+dt:0;previous=transform.position;
            if(boarding&&(d.sqrMagnitude<2.25f||stuck>5&&(Service.Terminal(AtNova)-transform.position).sqrMagnitude<35*35))Service.Accept(this);
        }
    }
}
