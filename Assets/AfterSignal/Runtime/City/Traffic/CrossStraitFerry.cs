using UnityEngine;
namespace AfterSignal
{
    public sealed class CrossStraitFerry:MonoBehaviour
    {
        CityVehicle car;int leg;float wait=40;bool returning;
        Vector3[] outbound={NeonHarbor.OldDock,new(1170,-.95f,-950),new(1080,-.95f,-1700),new(920,-.95f,-2280),NeonHarbor.NewDock};
        public bool Boarding=>wait>0;
        public int Crossings{get;private set;}
        public string Status=>Boarding?(returning?"노바 여객항":"애프터라이트 여객항")+" · 출항 "+Mathf.CeilToInt(wait)+"초":returning?"애프터라이트행 · 해협 항해 중":"노바 해협도시행 · 해협 항해 중";
        public void Initialize(CityVehicle vehicle){car=vehicle;car.occupied=true;car.traffic=false;car.GetComponent<VehicleCabin>()?.SetPassengers(12);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!car||car.Wrecked)return;
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current==car&&sim.SeatIndex==0)return;
            if(!car.occupied)return;float dt=Mathf.Min(.05f,Time.deltaTime);
            if(wait>0){car.speed=0;wait-=dt;if(wait<=0)leg=returning?outbound.Length-2:1;return;}
            var goal=outbound[leg];var d=goal-transform.position;float remaining=d.magnitude;
            car.speed=Mathf.MoveTowards(car.speed,Mathf.Min(23,Mathf.Sqrt(remaining*7)),dt*3);
            if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,Mathf.Atan2(-d.z,d.x)*Mathf.Rad2Deg,0),dt*30);
            transform.position=Vector3.MoveTowards(transform.position,goal,Mathf.Max(.4f,car.speed)*dt);
            if(remaining<.2f)
            {
                if(leg==0||leg==outbound.Length-1)
                {returning=!returning;wait=40;car.speed=0;Crossings++;car.GetComponent<VehicleCabin>()?.SetPassengers(Random.Range(7,14));if(sim&&sim.Current==car)g.Toast(returning?"노바 여객항 도착 · F 하선 / 40초 후 귀항":"애프터라이트 도착 · F 하선",5);}
                else leg+=returning?-1:1;
            }
        }
    }
}
