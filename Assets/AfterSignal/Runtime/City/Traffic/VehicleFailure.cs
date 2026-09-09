using UnityEngine;
namespace AfterSignal
{
    // One terminal state owns movement until impact/sinking. Route controllers
    // are disabled first so an NPC transport cannot snap back to its itinerary.
    public sealed class VehicleFailure : MonoBehaviour
    {
        public bool Sinking {get;private set;}
        public float Progress {get;private set;}
        CityVehicle car;Vector3 velocity;float elapsed,originY;WorldActor source;
        readonly RaycastHit[] hits=new RaycastHit[32];
        public static void Begin(CityVehicle vehicle,WorldActor attacker)
        {
            if(vehicle.GetComponent<VehicleFailure>())return;
            var f=vehicle.gameObject.AddComponent<VehicleFailure>();f.car=vehicle;f.source=attacker;
            f.Sinking=vehicle.IsWatercraft;f.originY=vehicle.transform.position.y;
            f.velocity=vehicle.Forward*Mathf.Max(12,Mathf.Abs(vehicle.speed))+Vector3.down*3;
            vehicle.traffic=false;vehicle.fuel=0;
            foreach(var controller in vehicle.GetComponents<MonoBehaviour>())
                if(controller is PassengerRoute || controller is IntercityService || controller is CityTaxiService || controller is MilitaryVehicleAI || controller is SeaCombat)controller.enabled=false;
            GameDirector.Instance?.Toast(f.Sinking?"선체 침수 · F로 즉시 탈출하세요":"항공기 동력 상실 · 추락 중 · F로 탈출하세요",5);
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!car||!game||game.Blocked)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);elapsed+=dt;
            if(Sinking)
            {
                float duration=car.GetComponent<AuthoredCraft>()?65:32;Progress=Mathf.Clamp01(elapsed/duration);
                var p=transform.position;p+=car.Forward*car.speed*dt;car.speed=Mathf.MoveTowards(car.speed,0,dt*2);
                p.y=originY-Mathf.SmoothStep(0,1,Progress)*(car.GetComponent<AuthoredCraft>()?100:20);transform.position=p;
                transform.Rotate(0,0,dt*(car.GetComponent<AuthoredCraft>()?.3f:.75f),Space.Self);
                if(elapsed>6&&car.occupied)VehicleEmergency.Hit(car,transform.position,false);
                if(Progress>=1){UrbanSimulation.Instance?.EmergencyExit(car);car.GetComponent<IntercityService>()?.ReleaseAfterCrash();Destroy(gameObject);}
                return;
            }
            velocity+=Vector3.down*9.81f*dt;var delta=velocity*dt;
            int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*2,1.5f,delta.normalized,hits,delta.magnitude+.5f,1,QueryTriggerInteraction.Ignore);
            bool impact=false;
            for(int i=0;i<count;i++)if(hits[i].collider&&!hits[i].transform.IsChildOf(transform)){impact=true;break;}
            transform.position+=delta;transform.Rotate(0,dt*6,dt*9,Space.Self);Progress=Mathf.Clamp01(elapsed/20);
            if(impact||transform.position.y<OceanLife.Surface&&OceanLife.Contains(transform.position)||elapsed>25)car.Detonate(source);
        }
    }
}
