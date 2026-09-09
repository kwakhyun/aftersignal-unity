using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Fire and dispatch have independent hard budgets; one truck owns one fire until return.
    public sealed class CityFireService:MonoBehaviour
    {
        public static CityFireService Instance {get;private set;}
        public const int EngineLimit=5,FireLimit=24;
        public readonly List<BurningObject> Fires=new();
        public readonly List<FireEngine> Engines=new();
        public int Extinguished {get;set;}
        float next;static readonly Collider[] contacts=new Collider[96];
        void Awake()=>Instance=this;
        public static BurningObject Ignite(GameObject target,Vector3 at,float intensity=1)
        {
            if(!Instance||!target||target.GetComponentInParent<WorldActor>()&&!target.GetComponentInParent<CityVehicle>()||target.GetComponentInParent<FireEngine>())return null;
            var existing=target.GetComponent<BurningObject>();if(existing){existing.Heat=Mathf.Min(120,existing.Heat+intensity*10);return existing;}
            Instance.Fires.RemoveAll(f=>!f);if(Instance.Fires.Count>=FireLimit)return null;
            var quenched=target.GetComponent<ExtinguishedObject>();if(quenched&&Time.time<quenched.Until)return null;
            var f=target.AddComponent<BurningObject>();f.Initialize(at,intensity);Instance.Fires.Add(f);
            GameDirector.Instance?.ToastNear("화재 발생 · 119 소방대 출동 요청",at,110,4);return f;
        }
        public static void IgniteBlast(Vector3 at,float radius,float damage)
        {
            if(!Instance||damage<70)return;int n=Physics.OverlapSphereNonAlloc(at,Mathf.Min(radius,22),contacts,1,QueryTriggerInteraction.Ignore),count=0;
            for(int i=0;i<n&&count<3;i++)
            {
                var c=contacts[i];if(!c||c.GetComponentInParent<WorldActor>()&&!c.GetComponentInParent<CityVehicle>())continue;
                var car=c.GetComponentInParent<CityVehicle>();if(car){if(!car.IsWatercraft&&!car.GetComponent<FireEngine>()&&Ignite(car.gameObject,car.transform.position+Vector3.up,1))count++;continue;}
                var size=c.bounds.size;if(size.y<2.5f||size.x>180||size.z>180)continue;
                if(Ignite(c.gameObject,c.ClosestPoint(at)+Vector3.up*.3f,Mathf.Clamp(size.y/8,1,3)))count++;
            }
        }
        public static Vector3 Station(Vector3 target)
        {
            Vector3 at=UrbanCatalog.Door(1);float best=float.MaxValue;
            foreach(var v in FourCityCatalog.Venues)if(v.kind==VenueKind.FireStation&&v.city!=2){float d=(v.Entrance-target).sqrMagnitude;if(d<best){best=d;at=v.Entrance;}}
            return at;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||Time.time<next||!UrbanSimulation.Instance)return;next=Time.time+2;
            Fires.RemoveAll(f=>!f);Engines.RemoveAll(e=>!e);if(Engines.Count>=EngineLimit)return;
            foreach(var fire in Fires)
            {
                if(!fire||fire.Assigned||fire.Heat<=0||(fire.Position-g.Player.transform.position).sqrMagnitude>650*650)continue;
                var station=Station(fire.Position);Vector3 origin=station+Vector3.back*15+Vector3.right*(Engines.Count*7);
                if(!CityGangWar.FindGround(origin,out origin)&&!ResponseDispatch.TryOrigin(fire.Position,false,false,Engines.Count,out origin))continue;
                var engine=FireEngine.Create(fire,origin,station);fire.Assigned=engine;Engines.Add(engine);break;
            }
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class ExtinguishedObject:MonoBehaviour {public float Until;}
    public sealed class BurningObject:MonoBehaviour
    {
        public float Heat;public FireEngine Assigned;public Vector3 Position=>transform.TransformPoint(localPoint);
        Vector3 localPoint;ParticleSystem flame,smoke;float tick,age,scale;CityVehicle car;
        public void Initialize(Vector3 at,float intensity)
        {
            localPoint=transform.InverseTransformPoint(at);Heat=80;scale=Mathf.Clamp(intensity,1,3);car=GetComponent<CityVehicle>();
            flame=VehicleDamagePresentation.Emitter(transform,"Active fire",localPoint,scale,true);
            smoke=VehicleDamagePresentation.Emitter(transform,"Fire column",localPoint+Vector3.up,scale*1.3f,false);
        }
        public void Suppress(float water){Heat=Mathf.Max(0,Heat-water);if(Heat<=0)Quench();}
        void Quench()
        {
            var mark=GetComponent<ExtinguishedObject>();if(!mark)mark=gameObject.AddComponent<ExtinguishedObject>();mark.Until=Time.time+35;
            if(CityFireService.Instance)CityFireService.Instance.Extinguished++;
            Destroy(this);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;age+=Time.deltaTime;if(age>240){Suppress(1000);return;}
            if(Time.time<tick)return;tick=Time.time+1;bool near=(Position-g.Player.transform.position).sqrMagnitude<650*650;
            foreach(var p in new[]{flame,smoke})if(p){var e=p.emission;e.enabled=near;e.rateOverTime=p==flame?Mathf.Lerp(2,20,Heat/100):Mathf.Lerp(3,18,Heat/100);}
            if(!near)return;
            if(car&&!car.Wrecked)car.Damage(car.MaxHealth*.003f,Position,TrafficDamageSource.Environment,false);
            foreach(var a in WorldActor.All.ToArray())
            {
                if(!a||!a.Alive||a.environmental||a.GetComponent<Firefighter>()||(a.Center-Position).sqrMagnitude>16*scale)continue;
                float d=Vector3.Distance(a.Center,Position);if(d<2.5f*scale)a.Damage(6,Vector3.zero,TrafficDamageSource.Environment);
                var npc=a.GetComponent<CityNpc>();if(npc&&!a.police&&!a.military&&!a.gang)npc.Panic(Position,12);
            }
            if((g.Player.Shoulder-Position).sqrMagnitude<6)g.Player.ReceiveDamage(5,Position);
        }
        void OnDestroy(){if(flame)Destroy(flame.gameObject);if(smoke)Destroy(smoke.gameObject);}
    }
    public sealed class FireEngine:MonoBehaviour
    {
        public CityVehicle Car {get;private set;}public BurningObject Fire {get;private set;}public int Phase {get;private set;}
        readonly ResponseDrive drive=new();Vector3 station;Firefighter[] crew;float age,water=1000;LineRenderer cannon;
        public static FireEngine Create(BurningObject fire,Vector3 origin,Vector3 station)
        {
            var car=UrbanSimulation.Instance.Spawn(origin,false,(int)CityVehicleType.Truck);car.occupied=true;car.traffic=false;car.name="119 / 펌프·방수 소방차";
            car.gameObject.AddComponent<FireEngineArt>();car.gameObject.AddComponent<ResponseLightbar>();
            var e=car.gameObject.AddComponent<FireEngine>();e.Car=car;e.Fire=fire;e.station=station;return e;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(Time.deltaTime,.06f);age+=dt;
            if(!Car||Car.Wrecked||age>420){Finish();return;}
            if(Phase<2&&(!Fire||Fire.Heat<=0||water<=0)){Phase=2;RetireCrew();}
            if(Phase==0){drive.Drive(Car,Fire.Position,dt,22);if(Vector3.ProjectOnPlane(Fire.Position-transform.position,Vector3.up).magnitude<26){Car.speed=0;Phase=1;Deploy();}}
            else if(Phase==1)
            {
                var nozzle=transform.position+Vector3.up*3.6f;
                bool clear=BlastDamage.Exposed(nozzle,Fire.Position,Fire.transform,Car);
                if(clear){water-=dt*4;Fire.Suppress(dt*5);Firefighter.Jet(cannon,nozzle,Fire.Position);}
                else if(cannon)cannon.enabled=false;
            }
            else {drive.Drive(Car,station,dt,18);if(Vector3.ProjectOnPlane(transform.position-station,Vector3.up).magnitude<22)Finish();}
        }
        void Deploy()
        {
            crew=new Firefighter[2];for(int i=0;i<2;i++)crew[i]=Firefighter.Create(this,Fire,transform.position-Car.Forward*7+transform.forward*(i*2-1),i);
            cannon=Firefighter.Line(transform,"Roof water cannon",.17f,new Color(.52f,.82f,1,.6f));
            NpcSpeech.Say(crew[0],"소방대 도착! 시민들은 물러나세요. 방수 개시!",5);
        }
        void RetireCrew(){if(cannon)cannon.enabled=false;if(crew!=null)foreach(var c in crew)if(c)c.Returning=true;}
        void Finish(){if(Fire&&Fire.Assigned==this)Fire.Assigned=null;if(crew!=null)foreach(var c in crew)if(c)Destroy(c.gameObject);Destroy(gameObject);}
        void OnDestroy(){if(Fire&&Fire.Assigned==this)Fire.Assigned=null;if(crew!=null)foreach(var c in crew)if(c)Destroy(c.gameObject);}
    }
    public sealed class Firefighter:MonoBehaviour
    {
        public bool Returning;FireEngine truck;BurningObject fire;SpriteRenderer visual;WorldActor body;int variant;LineRenderer hose,jet;readonly PursuitPath path=new();
        float assess,radio;Vector3 workPoint;WorldActor hazard;public string Decision {get;private set;}="방수 위치 확보";
        public static Firefighter Create(FireEngine truck,BurningObject fire,Vector3 at,int variant)
        {
            var go=new GameObject(variant==0?"소방대원 / 방수 담당":"소방대원 / 구조 담당",typeof(SpriteRenderer),typeof(CityNpc));go.transform.position=at;
            var npc=go.GetComponent<CityNpc>();npc.Configure(94000+variant,"119 소방대원",null,"화재 현장에서 시민을 대피시키고 호스로 불을 끄는 전문 소방관이다.");
            var f=go.AddComponent<Firefighter>();f.truck=truck;f.fire=fire;f.variant=variant;f.body=go.GetComponent<WorldActor>();
            f.visual=go.GetComponent<SpriteRenderer>();f.visual.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
            f.hose=Line(go.transform,"Connected fire hose",.07f,new Color(.8f,.22f,.035f));f.jet=Line(go.transform,"Pressurised water",.1f,new Color(.62f,.88f,1,.6f));return f;
        }
        public static LineRenderer Line(Transform parent,string name,float width,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var r=go.AddComponent<LineRenderer>();r.sharedMaterial=Resources.Load<Material>("Materials/VehicleSmoke");r.startColor=r.endColor=color;r.startWidth=width;r.endWidth=width*.65f;r.positionCount=9;r.useWorldSpace=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return r;
        }
        public static void Jet(LineRenderer line,Vector3 from,Vector3 to)
        {
            if(!line)return;line.enabled=true;for(int i=0;i<9;i++){float t=i/8f;line.SetPosition(i,Vector3.Lerp(from,to,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.1f));}
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!truck)return;if(!body.Alive||body.Downed){jet.enabled=hose.enabled=false;return;}
            if(!fire)Returning=true;
            if(Time.time>assess){assess=Time.time+1.2f;hazard=TacticalJudgment.Hazard(transform.position,23);if(fire)workPoint=TacticalJudgment.SafeWorkPoint(fire.Position,transform.position,9+variant*2,variant);}
            bool unsafeArea=TacticalJudgment.Active(hazard);
            Vector3 goal=Returning||unsafeArea?truck.transform.position:workPoint;Vector3 d=goal-transform.position;d.y=0;
            bool spraying=!Returning&&!unsafeArea&&fire&&Vector3.Distance(transform.position,fire.Position)<14&&BlastDamage.Exposed(transform.position+Vector3.up*1.35f,fire.Position,fire.transform,truck.Car);
            Decision=Returning?"소방차 복귀":unsafeArea?"엄호 요청 / 안전 위치 이동":spraying?"화점 방수":"장애물을 우회해 방수 위치 확보";
            if(unsafeArea&&Time.time>radio){radio=Time.time+15;NpcSpeech.Say(this,"소방대 사격 위험! 경찰 엄호가 필요합니다!",4,5);TacticalJudgment.RequestProtection(this,hazard);}
            if(!spraying)PedestrianSteering.For(this).Move(transform.position+path.Direction(transform.position,goal)*3,Time.deltaTime*3);
            if(Returning&&d.magnitude<8){Destroy(gameObject);return;}
            int facing=PeopleArt.Direction(spraying?fire.Position-transform.position:d);var sprites=FireCrewArt.Frames;int index=variant*8+(spraying?4:0)+facing;if(sprites.Length>index)visual.sprite=sprites[index];
            if(Camera.main)visual.transform.rotation=Quaternion.Euler(Camera.main.transform.eulerAngles.x,Camera.main.transform.eulerAngles.y,0);
            var nozzle=transform.position+Vector3.up*1.35f;
            for(int i=0;i<9;i++){float t=i/8f;var p=Vector3.Lerp(truck.transform.position+Vector3.up*.8f,nozzle,t);p.y=Mathf.Lerp(p.y,transform.position.y+.1f,Mathf.Sin(t*Mathf.PI));hose.SetPosition(i,p);}
            bool clear=spraying&&BlastDamage.Exposed(nozzle,fire.Position,fire.transform,truck.Car);jet.enabled=clear;
            if(clear){Jet(jet,nozzle,fire.Position);fire.Suppress(Time.deltaTime*4);}
        }
    }
    public static class FireCrewArt
    {
        static Sprite[] frames;public static Sprite[] Frames {get{if(frames==null){frames=Resources.LoadAll<Sprite>("Art/ResponseCrew/FireCrew");System.Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));}return frames;}}
    }
}

