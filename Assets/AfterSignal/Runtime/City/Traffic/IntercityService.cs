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
        public bool Boarding=>dwell>0 && Car && Car.occupied && !Car.Wrecked;
        public int Arrivals {get;private set;}
        public int Boarded {get;private set;}
        public int Disembarked {get;private set;}
        public int Fare=>Aircraft?240:80;
        public int Seats=>Aircraft?22:12;
        public bool AtNova {get;private set;}
        public Vector3 ExitPoint=>Aircraft?(AtNova?new Vector3(1868,.2f,-3996):new Vector3(1408,.2f,794)):AtNova?new Vector3(927.4f,.1f,-2390):new Vector3(1250,.1f,-664);
        public Vector3 BoardPoint=>VehicleSeats.Door(Car);
        public string Status=>Car&&Car.Wrecked?"운항 중단":(AtNova?"애프터라이트행":"노바행")+" · "+(Boarding?"탑승 "+Mathf.CeilToInt(dwell)+"초 · "+Manifest.Count+" / "+Seats+"명":phase)+" · "+Fare+" C";
        public readonly List<TransitTraveller> Manifest=new();
        readonly List<TransitTraveller> travellers=new();
        Vector3[] path;int leg;float dwell=50;string phase="운항 중";bool exchange;
        public void Initialize(CityVehicle car,bool aircraft,bool nova=false)
        {
            Car=car;Aircraft=aircraft;AtNova=nova;car.occupied=true;car.traffic=false;All.Add(this);
            StartCoroutine(Setup());
        }
        IEnumerator Setup()
        {
            yield return null;Car.GetComponent<VehicleCabin>()?.SetPassengers(0);
            int pool=Aircraft?28:18;
            for(int i=0;i<pool;i++)
            {
                var go=new GameObject("Traveller / "+(Aircraft?"LA 207":"BW 12")+" / "+i,typeof(SpriteRenderer),typeof(CityNpc),typeof(TransitTraveller));
                var t=go.GetComponent<TransitTraveller>();t.Art=FacilityPeople.Key((Aircraft?32:24)+i%16);t.Service=this;t.Serial=i;t.AtNova=i%2==0;
                go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");go.GetComponent<SpriteRenderer>().sprite=PeopleArt.Get(t.Art,0);
                go.GetComponent<CityNpc>().Configure(18000+(Aircraft?200:0)+(AtNova?100:0)+i,Aircraft?"도시 간 항공 승객":"해협 통근 승객",null,"애프터라이트와 노바를 오가는 주민. 목적지 터미널에서 내려 일상을 보낸다.");PeopleArt.Attach(go,t.Art);
                t.PlaceAtTerminal();travellers.Add(t);
            }
            Exchange();
        }
        public Vector3 Terminal(bool nova)=>Aircraft?(nova?new Vector3(1855,.2f,-3975):new Vector3(1394,.2f,780)):nova?new Vector3(927.4f,.1f,-2404):new Vector3(1250,.1f,-672);
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
            exchange=true;
            var cabin=Car.GetComponent<VehicleCabin>();
            foreach(var t in Manifest)if(t){t.Inside=false;t.AtNova=AtNova;t.gameObject.SetActive(true);t.transform.position=ExitPoint+Vector3.right*(Disembarked%3*.55f);t.Target=Terminal(AtNova)+new Vector3((t.Serial%5-2)*1.2f,0,t.Serial%3);t.Cooldown=Time.time+26;Disembarked++;}
            Manifest.Clear();cabin?.SetPassengers(0);
            foreach(var t in travellers)if(t&&!t.Inside&&t.AtNova==AtNova&&t.Healthy){t.WantsBoard=true;t.Cooldown=Mathf.Max(t.Cooldown,Time.time+4+t.Serial%5);}
        }
        public void Accept(TransitTraveller t)
        {
            int capacity=Seats-(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==Car?1:0);
            if(!Boarding||dwell<3||t.Inside||!t.Healthy||Manifest.Count>=capacity)return;
            Manifest.Add(t);t.Inside=true;t.WantsBoard=false;t.gameObject.SetActive(false);
            Car.GetComponent<VehicleCabin>()?.SetManifest(Manifest.ConvertAll(x=>x.Art));Boarded++;
        }
        void Depart()
        {
            exchange=false;foreach(var t in travellers)if(t)t.WantsBoard=false;
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
            if(dwell>0){Car.speed=0;dwell-=dt;if(dwell<=0)Depart();return;}
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
        public void ReleaseAfterCrash(){foreach(var t in Manifest)if(t){t.Inside=false;t.WantsBoard=false;t.gameObject.SetActive(true);t.transform.position=transform.position+Random.insideUnitSphere*3;t.GetComponent<WorldActor>()?.Damage(999,Vector3.up*4);}Manifest.Clear();}
        void OnDestroy(){All.Remove(this);foreach(var t in travellers)if(t)Destroy(t.gameObject);}
    }
    public sealed class TransitTraveller:MonoBehaviour
    {
        public IntercityService Service;public string Art;public int Serial;public bool AtNova,Inside,WantsBoard;public Vector3 Target;public float Cooldown;
        public bool Healthy=>!GetComponent<WorldActor>()||GetComponent<WorldActor>().Alive;
        float nextVisibility;SpriteRenderer sprite;
        public void PlaceAtTerminal(){transform.position=Service.Terminal(AtNova)+new Vector3((Serial%5-2)*1.3f,0,Serial%4*1.2f);Target=transform.position;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Service||Inside||!Healthy)return;
            var npc=GetComponent<CityNpc>();if(npc&&npc.Fleeing)return;
            if(Time.time>nextVisibility){nextVisibility=Time.time+.5f;if(!sprite)sprite=GetComponent<SpriteRenderer>();if(sprite)sprite.enabled=(transform.position-g.Player.transform.position).sqrMagnitude<250*250;}
            bool boarding=WantsBoard&&Service.Boarding&&Service.AtNova==AtNova&&Time.time>Cooldown;
            if(boarding)Target=Service.BoardPoint;
            var d=Target-transform.position;if(d.sqrMagnitude>.09f){transform.position+=Vector3.ClampMagnitude(d,Time.deltaTime*2.1f);}
            if(boarding&&d.sqrMagnitude<.36f)Service.Accept(this);
        }
    }
}
