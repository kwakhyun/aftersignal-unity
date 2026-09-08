using UnityEngine;
namespace AfterSignal
{
    public sealed class RegionalFerry:MonoBehaviour
    {
        CityVehicle car;int stop;float dwell=25;Vector3[] route;
        public int StopsVisited{get;private set;}
        public string Destination=>stop<route.Length?"해협 마을 순환선":"";
        void Start(){car=GetComponent<CityVehicle>();car.occupied=true;car.traffic=false;car.name="해협 섬마을 순환 여객선";route=new[]{new Vector3(1272,OceanLife.Surface,-725),new Vector3(390,OceanLife.Surface,-1545),new Vector3(2350,OceanLife.Surface,-2065),new Vector3(3500,OceanLife.Surface,-2965),new Vector3(5050,OceanLife.Surface,-3115),new Vector3(2250,OceanLife.Surface,-2200),new Vector3(1080,OceanLife.Surface,-2200),new Vector3(920,OceanLife.Surface,-2379),new Vector3(1080,OceanLife.Surface,-2200)};car.GetComponent<VehicleCabin>()?.SetPassengers(10);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!car||car.Wrecked)return;
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current==car&&sim.SeatIndex==0)return;
            if(dwell>0){dwell-=Time.deltaTime;car.speed=0;return;}
            var delta=route[stop]-transform.position;delta.y=0;float dt=Mathf.Min(.05f,Time.deltaTime);car.speed=Mathf.MoveTowards(car.speed,Mathf.Min(24,Mathf.Sqrt(delta.magnitude*5)),dt*4);
            if(delta.sqrMagnitude>.1f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,Mathf.Atan2(-delta.z,delta.x)*Mathf.Rad2Deg,0),dt*35);
            transform.position=Vector3.MoveTowards(transform.position,route[stop],Mathf.Max(.6f,car.speed)*dt);
            if(delta.sqrMagnitude<1){StopsVisited++;stop=(stop+1)%route.Length;dwell=stop==6||stop==7||stop==0?2:30;car.GetComponent<VehicleCabin>()?.SetPassengers(Random.Range(6,13));if(sim&&sim.Current==car)g.Toast("섬마을 여객선 정차 · F 하선 / 계속 탑승하면 다음 기항지로 이동",5);}
        }
    }
}
