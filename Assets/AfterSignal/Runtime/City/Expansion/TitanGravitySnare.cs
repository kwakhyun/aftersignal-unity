using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // A bounded, interruptible attack owns aircraft movement; empty craft cannot be selected.
    public sealed class TitanGravitySnare:MonoBehaviour
    {
        public const float Range=260,Duration=4.5f;
        public float Progress=>Mathf.Clamp01(age/Duration);
        public CityVehicle Vehicle=>car;
        RiftCreature titan;CityVehicle car;float age;Vector3 direction;
        readonly List<MonoBehaviour> suspended=new();readonly RaycastHit[] hits=new RaycastHit[24];
        readonly LineRenderer[] coils=new LineRenderer[3];bool terminal;
        public static bool Eligible(CityVehicle v,RiftCreature source)=>v&&source&&source.Body.Alive&&v.IsAircraft&&!v.Wrecked&&(v.occupied||LocalSimulation.PlayerAboard(v))&&!v.GetComponent<TitanGravitySnare>()&&v.transform.position.y>source.transform.position.y+10&&(v.transform.position-source.AimCenter).sqrMagnitude<Range*Range;
        public static TitanGravitySnare Begin(RiftCreature source,CityVehicle vehicle)
        {
            if(!Eligible(vehicle,source))return null;
            var s=vehicle.gameObject.AddComponent<TitanGravitySnare>();s.car=vehicle;s.titan=source;
            s.direction=Vector3.ProjectOnPlane(vehicle.transform.position-source.transform.position,Vector3.up).normalized;
            if(s.direction.sqrMagnitude<.1f)s.direction=Vector3.forward;
            foreach(var b in vehicle.GetComponents<MonoBehaviour>())
                if(b.enabled&&(b is MilitaryVehicleAI||b is IntercityService||b is PassengerRoute||b is CityTaxiService||b is SeaCombat)){s.suspended.Add(b);b.enabled=false;}
            for(int i=0;i<s.coils.Length;i++){s.coils[i]=Firefighter.Line(s.transform,"Gravitational vortex",.055f,new Color(.55f,.22f,.92f,.8f));s.coils[i].positionCount=32;}
            GameDirector.Instance?.ToastNear("잠식체 중력 포획 · 항공기 강제 추락!",vehicle.transform.position,100,4);
            if(LocalSimulation.PlayerAboard(vehicle))GameDirector.Instance?.Toast("중력 포획! F로 항공기에서 탈출하세요",4);
            return s;
        }
        void LateUpdate()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            if(!car||car.Wrecked){terminal=true;Destroy(this);return;}
            if(!titan||!titan.Body.Alive||!LocalSimulation.Combat(titan.transform.position)){Destroy(this);return;}
            float dt=Mathf.Min(Time.deltaTime,.06f);age+=dt;car.speed=0;
            Vector3 destination=titan.transform.position+direction*(12+car.HalfLength)+Vector3.up*7;
            var from=transform.position;var delta=Vector3.MoveTowards(from,destination,Mathf.Lerp(24,120,Progress)*dt)-from;
            int n=Physics.SphereCastNonAlloc(from+Vector3.up,1.5f,delta.normalized,hits,delta.magnitude,1,QueryTriggerInteraction.Ignore);bool impact=false;
            for(int i=0;i<n;i++)if(hits[i].collider&&!hits[i].transform.IsChildOf(transform)&&!hits[i].transform.IsChildOf(titan.transform)){delta=delta.normalized*Mathf.Max(0,hits[i].distance-.1f);impact=true;break;}
            transform.position+=delta;
            var facing=Quaternion.LookRotation(Vector3.Cross(Vector3.up,-direction),Vector3.up)*Quaternion.Euler(0,0,-12);
            transform.rotation=Quaternion.Slerp(transform.rotation,facing,dt*2);
            for(int k=0;k<coils.Length;k++)for(int i=0;i<32;i++)
            {float t=i/31f,a=t*Mathf.PI*6+age*5+k*Mathf.PI*2/3;var p=Vector3.Lerp(titan.AimCenter,transform.position+Vector3.up,t);float r=Mathf.Sin(t*Mathf.PI)*(2+car.HalfLength*.2f);coils[k].SetPosition(i,p+(Vector3.up*Mathf.Sin(a)+Vector3.Cross(direction,Vector3.up)*Mathf.Cos(a))*r);}
            var sim=UrbanSimulation.Instance;
            if(sim&&sim.Current==car)g.Player.transform.position=car.transform.TransformPoint(VehicleSeats.Local(car,sim.SeatIndex));
            if(impact||age>=Duration||(transform.position-destination).sqrMagnitude<9)
            {
                terminal=true;car.Damage(car.MaxHealth*2,transform.position,titan.Body,false);
                TitanImpact.Create(transform.position,6);g.Audio.Play("urban_explosion",transform.position,.45f,3);Destroy(this);
            }
        }
        void OnDestroy()
        {
            foreach(var line in coils)if(line)Destroy(line.gameObject);
            if(!terminal&&car&&!car.Wrecked)foreach(var b in suspended)if(b)b.enabled=true;
        }
    }
}
