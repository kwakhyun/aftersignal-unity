using System.Collections;using System.Collections.Generic;using UnityEngine;
namespace AfterSignal
{
    public sealed class CityTaxiService:MonoBehaviour
    {
        public static readonly List<CityTaxiService> All=new();
        public bool Air;public int Serial;public CityVehicle Car{get;private set;}
        Vector3[] route;int leg;float dwell;Transform[] fans;float clock;
        public int Arrivals{get;private set;}
        public static readonly Vector3[] WaterStops={new(1200,-.95f,-722),new(910,-.95f,-2380),new(1063,-.95f,-3000),new(1063,-.95f,-4080)};
        public static readonly Vector3[] AirStops={new(1440,.2f,975),new(1900,.2f,-3880)};
        public void Initialize(CityVehicle car,bool air,int serial)
        {
            Car=car;Air=air;Serial=serial;car.occupied=true;car.traffic=false;All.Add(this);
            if(air)
            {
                float lane=serial%4*22;
                route=new[]{AirStops[0],AirStops[0]+Vector3.up*(340+lane),new Vector3(450,340+lane,400),new Vector3(650,340+lane,-1400),new Vector3(680,340+lane,-3350),AirStops[1]+Vector3.up*(340+lane),AirStops[1],new Vector3(2130,340+lane,-3880),new Vector3(2140,340+lane,-1400),new Vector3(2100,340+lane,600)};
            }
            else
            {
                float lane=serial%4*6;
                route=serial%2==0?new[]{WaterStops[0],new Vector3(1160+lane,-.95f,-1000),new Vector3(650+lane,-.95f,-1600),new Vector3(800+lane,-.95f,-2280),WaterStops[1],new Vector3(1300+lane,-.95f,-2120),new Vector3(1680+lane,-.95f,-1200)}:
                    new[]{WaterStops[1],new Vector3(1060+lane,-.95f,-2350),WaterStops[2]+Vector3.right*lane,WaterStops[3]+Vector3.right*lane,new Vector3(1080-lane,-.95f,-4200),new Vector3(1080-lane,-.95f,-3000),new Vector3(1080-lane,-.95f,-2350)};
            }
            int edge=serial%route.Length;float fraction=(serial*.6180339f)%1;car.transform.position=Vector3.Lerp(route[edge],route[(edge+1)%route.Length],fraction);leg=(edge+1)%route.Length;
            if(serial==0){car.transform.position=route[0];leg=1;dwell=24;}
            StartCoroutine(Finish());
        }
        IEnumerator Finish()
        {
            yield return null;
            var armament=Car.GetComponent<VehicleArmament>();if(armament)Destroy(armament);
            var hull=Car.GetComponent<BoxCollider>();if(hull&&Air){hull.center=new Vector3(0,1.3f,0);hull.size=new Vector3(9.5f,2.6f,8.4f);}
            Car.GetComponent<VehicleCabin>()?.SetPassengers(Air?1+Serial%2:3+Serial%8);
            var list=new List<Transform>();foreach(var t in Car.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Electric fan rotor"))list.Add(t);fans=list.ToArray();
            var point=gameObject.AddComponent<InteractionPoint>();point.title=(Air?"스카이라인 무인택시":"블루웨이 수상택시")+" 탑승 · "+(Air?120:35)+" C";point.radius=Air?7:12;point.kind=InteractionKind.Furniture;
        }
        public void Board()
        {
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;if(!g||!sim||Car.Wrecked)return;
            if(Car.speed>2){g.Toast("정류장에 정차하면 탑승할 수 있습니다.");return;}
            int fare=Air?120:35;if(LifeState.Credits<fare){g.Toast("교통비가 부족합니다.");return;}
            if(sim.Enter(Car,VehicleSeats.Count(Car)-1)){LifeState.Spend(fare);g.Toast((Air?"공중 무인택시":"수상택시")+" 탑승 · F 이동 중 하차 / C 실내 시점",5);}
        }
        bool Berth(int at)=>Air?at==0||at==6:at==0||at==4||Serial%2==1&&(at==2||at==3);
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Car||Car.Wrecked||!Car.occupied)return;
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current==Car&&sim.SeatIndex==0)return;
            float dt=Mathf.Min(.05f,Time.deltaTime);clock+=dt;
            if(fans!=null)foreach(var fan in fans)if(fan)fan.Rotate(transform.up,dt*1600,Space.World);
            if(dwell>0){Car.speed=0;dwell-=dt;return;}
            var delta=route[leg]-transform.position;
            if(Berth(leg)&&delta.magnitude<65)
                foreach(var taxi in All)
                    if(taxi&&taxi!=this&&taxi.Air==Air&&!taxi.Car.Wrecked&&Vector3.Distance(taxi.transform.position,route[leg])<(Air?25:22))
                    {Car.speed=Mathf.MoveTowards(Car.speed,0,dt*18);return;}
            float max=Air?Mathf.Abs(delta.y)>20?28:68:21;
            Car.speed=Mathf.MoveTowards(Car.speed,Mathf.Min(max,Mathf.Sqrt(delta.magnitude*8)),dt*(Air?12:4));
            var horizontal=Car.IsWatercraft?SeaTraffic.Steer(Car,delta):Vector3.ProjectOnPlane(delta,Vector3.up);if(horizontal.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,Mathf.Atan2(-horizontal.z,horizontal.x)*Mathf.Rad2Deg,0),dt*48);
            var step=Vector3.ClampMagnitude(delta,Mathf.Max(.5f,Car.speed)*dt);
            if(!Air&&Physics.Raycast(transform.position+Vector3.up*1.6f,step.normalized,out var obstruction,step.magnitude+2,1,QueryTriggerInteraction.Ignore)&&!obstruction.collider.transform.IsChildOf(transform)&&!obstruction.collider.GetComponentInParent<CityVehicle>()){Car.speed=0;return;}
            transform.position=SeaTraffic.Move(Car,transform.position+step);
            if(delta.magnitude<.3f)
            {
                Arrivals++;bool stop=Berth(leg);
                if(stop){dwell=10+Serial%5;Car.GetComponent<VehicleCabin>()?.SetPassengers(Air?1+Arrivals%2:2+(Arrivals+Serial)%10);if(sim&&sim.Current==Car)g.Toast("택시 정류장 도착 · F 하차",4);}
                leg=(leg+1)%route.Length;
            }
            Car.GetComponent<VehicleSoundscape>()?.SetThrottle(Mathf.Clamp01(Car.speed/(Air?68:21)));
        }
        void OnDestroy(){All.Remove(this);}
    }
}
