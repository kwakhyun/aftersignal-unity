using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class HarborAccess:MonoBehaviour
    {
        public static HarborAccess Instance{get;private set;}float next;bool queued,air,nova;Vector3 waitingAt;
        public bool Waiting=>queued;
        void Awake(){Instance=this;}
        public void Queue(bool aircraft,bool atNova){air=aircraft;nova=atNova;queued=true;waitingAt=GameDirector.Instance.Player.transform.position;GameDirector.Instance.Toast("다음 교통편을 기다립니다 · 이곳에서 대기하면 자동 탑승 · 멀리 이동하면 취소",7);}
        public void Cancel(){queued=false;}
        void Update()
        {
            var sim=UrbanSimulation.Instance;var g=GameDirector.Instance;if(!sim||!g||!g.Ready||g.Blocked||Time.time<next)return;next=Time.time+.7f;
            foreach(var car in sim.Cars)if(car&&car.IsSpecial&&!car.GetComponent<TransportAccess>())car.gameObject.AddComponent<TransportAccess>();
            if(queued)
            {
                if(sim.Current||(g.Player.transform.position-waitingAt).sqrMagnitude>70*70){queued=false;return;}
                foreach(var line in IntercityService.All)if(line&&line.Aircraft==air&&line.AtNova==nova&&line.Boarding){queued=false;line.BuyTicket();break;}
            }
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class TransportAccess:MonoBehaviour
    {
        CityVehicle car;Transform dockPoint,helmPoint;public Vector3 Dock{get;private set;}
        void Start(){car=GetComponent<CityVehicle>();dockPoint=Marker("탑승 계단 · E 조종석 / G 승객석");helmPoint=Marker(car.IsWatercraft?"선장석 탑승":"조종석 탑승");}
        Transform Marker(string title){var go=new GameObject(title,typeof(InteractionPoint),typeof(TransportBoarding));go.transform.SetParent(transform,false);var p=go.GetComponent<InteractionPoint>();p.kind=InteractionKind.Furniture;p.title=title;p.radius=5.5f;go.GetComponent<TransportBoarding>().Car=car;return go.transform;}
        void Update()
        {
            if(!car||!dockPoint)return;var authored=car.GetComponent<AuthoredCraft>();
            Dock=authored?authored.BoardingPoint:car.transform.TransformPoint(car.IsAircraft?new Vector3(car.HalfLength-2,0,-car.HalfWidth-3):new Vector3(car.HalfLength*.4f,.2f,-car.HalfWidth-2));
            if(NpcGroundSupport.Floor(Dock,car.transform.position.y+1.5f,4,out var y))Dock=new Vector3(Dock.x,y,Dock.z);
            dockPoint.position=Dock+Vector3.up;helmPoint.position=car.transform.TransformPoint(VehicleSeats.Local(car,0))+Vector3.up;
            bool usable=!car.Wrecked&&Mathf.Abs(car.speed)<12;dockPoint.gameObject.SetActive(usable);helmPoint.gameObject.SetActive(usable);
        }
        public static float Distance(CityVehicle car,Vector3 from){var access=car.GetComponent<TransportAccess>();return access?Mathf.Min(Vector3.Distance(from,access.Dock),Vector3.Distance(from,car.transform.TransformPoint(VehicleSeats.Local(car,0)))):Vector3.Distance(from,VehicleSeats.Door(car));}
    }
    public sealed class TransportBoarding:MonoBehaviour
    {
        public CityVehicle Car;public void Use(){if(Car)UrbanSimulation.Instance?.Enter(Car,0);}
    }
}
