using UnityEngine;

namespace AfterSignal
{
    public sealed class PassengerRoute : MonoBehaviour
    {
        CityVehicle car;Vector3[] stops;int leg;float wait=45;
        public int Departures {get;private set;}
        public bool Boarding => leg==0 && wait>0;
        public string Status => Boarding ? "승객 탑승 중 · 출발 "+Mathf.CeilToInt(wait)+"초" : car.IsAircraft ? "루멘 에어 · 해안 순환 비행" : "블루워터 · 연안 순환 운항";
        public void Initialize(CityVehicle vehicle,bool aircraft)
        {
            car=vehicle;car.occupied=true;car.traffic=false;
            car.GetComponent<VehicleCabin>()?.SetPassengers(aircraft?20:12);
            if(car.GetComponent<AuthoredCraft>())
            {stops=new[]{car.transform.position,new Vector3(1800,0,-960),new Vector3(1480,0,-1350),new Vector3(800,0,-1400),new Vector3(1400,0,-1700),new Vector3(1830,0,-1450),new Vector3(1730,0,-615)};wait=70;return;}
            stops=aircraft?new[]{new Vector3(1400,.15f,800),new Vector3(1338,.15f,845),new Vector3(1360,.15f,910),new Vector3(1580,8,910),new Vector3(1870,105,910),new Vector3(2130,180,350),new Vector3(1800,180,-900),new Vector3(600,180,-900),new Vector3(1120,110,1050),new Vector3(1320,12,910),new Vector3(1550,.15f,910),new Vector3(1500,.15f,830),new Vector3(1400,.15f,800)}:
                new[]{new Vector3(1272.5f,OceanLife.Surface,-708),new Vector3(1200,OceanLife.Surface,-900),new Vector3(700,OceanLife.Surface,-920),new Vector3(420,OceanLife.Surface,-850),new Vector3(780,OceanLife.Surface,-1090),new Vector3(1250,OceanLife.Surface,-990),new Vector3(1272.5f,OceanLife.Surface,-708)};
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!car||car.Wrecked||stops==null)return;
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current==car&&sim.SeatIndex==0)return;
            if(!car.occupied)return;
            float dt=Mathf.Min(.05f,Time.deltaTime);
            if(wait>0){car.speed=0;wait-=dt;if(wait<=0){leg=1;Departures++;}return;}
            var goal=stops[leg];var delta=goal-transform.position;
            float target=car.GetComponent<AuthoredCraft>()?8:car.IsAircraft?goal.y>20?65:leg==3?34:leg==10?32:9:11;
            float distance=delta.magnitude;
            car.speed=Mathf.MoveTowards(car.speed,Mathf.Min(target,Mathf.Sqrt(distance*10)),dt*8);
            var heading=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(heading.sqrMagnitude>1)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,Mathf.Atan2(-heading.z,heading.x)*Mathf.Rad2Deg,0),dt*(car.GetComponent<AuthoredCraft>()?3:38));
            transform.position=Vector3.MoveTowards(transform.position,goal,Mathf.Max(2,car.speed)*dt);
            if(distance<1.2f)
            {
                leg++;
                if(leg>=stops.Length){leg=0;wait=45;car.speed=0;car.GetComponent<VehicleCabin>()?.SetPassengers(car.IsAircraft?16+Random.Range(0,7):8+Random.Range(0,6));}
            }
        }
    }
}
