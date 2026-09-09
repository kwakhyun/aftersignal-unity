using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityVehicle { public bool Tumbling { get; internal set; } }

    // Short, bounded flight uses sweeps rather than an uncontrolled dynamic rigidbody.
    [DefaultExecutionOrder(1050)]
    public sealed class MotorcycleImpact : MonoBehaviour
    {
        CityVehicle bike; Vector3 velocity,origin; Quaternion upright; float age;
        readonly RaycastHit[] hits=new RaycastHit[16];
        public static bool TryLaunch(CityVehicle car,CityVehicle other,float closingSpeed,Vector3 contact)
        {
            if(!car||!other||car.type!=CityVehicleType.Motorcycle||car.Tumbling||car.Wrecked||closingSpeed<14)return false;
            var impact=car.GetComponent<MotorcycleImpact>()??car.gameObject.AddComponent<MotorcycleImpact>();
            impact.bike=car;impact.age=0;impact.origin=car.transform.position;impact.upright=Quaternion.Euler(0,car.transform.eulerAngles.y,0);
            Vector3 away=Vector3.ProjectOnPlane(car.transform.position-other.transform.position,Vector3.up).normalized;
            Vector3 heading=Vector3.ProjectOnPlane(car.Forward*car.speed+other.Forward*other.speed*.4f,Vector3.up).normalized;
            var direction=(away+heading*.6f).normalized;if(direction.sqrMagnitude<.1f)direction=-car.Forward;
            impact.velocity=direction*Mathf.Clamp(closingSpeed*.35f,5,10)+Vector3.up*Mathf.Clamp(closingSpeed*.17f,3.5f,7);
            var sim=UrbanSimulation.Instance;var game=GameDirector.Instance;
            if(sim&&sim.Current==car)
            {
                // Capture the rider before the vehicle movement loop loses ownership.
                sim.BailOut();game.Player.ProtectVehicleImpact();
                game.Player.Velocity=direction*Mathf.Clamp(closingSpeed*.32f,5,10)+Vector3.up*6;
                game.CameraRig.Kick(.14f);game.Toast("강한 충돌 · 바이크에서 튕겨 나왔습니다",3);
            }
            else if(car.occupied)
            {
                var rider=car.GetComponent<VehicleCabin>()?.EjectDriver();
                if(rider){rider.transform.position=car.transform.position+away*2+Vector3.up*.8f;CivilianImpact.Blast(rider,direction,8);rider.Damage(12,direction,TrafficDamageSource.Environment);}
            }
            car.occupied=car.traffic=false;car.speed=0;car.Tumbling=true;car.transform.position+=Vector3.up*.25f;
            var unattended=car.GetComponent<UnattendedVehicle>();if(unattended)Destroy(unattended);
            return true;
        }
        void Update()
        {
            if(!bike||!bike.Tumbling)return;
            var g=GameDirector.Instance;if(g&&g.Blocked)return;
            float dt=Mathf.Min(Time.deltaTime,.04f);age+=dt;velocity.y-=19.6f*dt;
            if(Vector3.ProjectOnPlane(transform.position-origin,Vector3.up).sqrMagnitude>14*14)velocity.x=velocity.z=0;
            var step=velocity*dt;float allowed=step.magnitude;Vector3 normal=Vector3.zero;
            int n=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.6f,.42f,step.normalized,hits,allowed+.04f,1,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++){var h=hits[i];if(!h.collider||h.collider.transform.IsChildOf(transform)||h.distance<.001f||h.normal.y>.55f&&velocity.y>=0)continue;if(h.distance<allowed){allowed=Mathf.Max(0,h.distance-.03f);normal=h.normal;}}
            transform.position+=step.normalized*allowed;
            transform.rotation=upright*Quaternion.Euler(age*160,0,Mathf.Sin(age*5)*55);
            if(normal.y>.5f&&velocity.y<0){Finish();return;}
            if(normal.sqrMagnitude>.1f){velocity=Vector3.Reflect(velocity,normal)*.25f;velocity.y=Mathf.Min(velocity.y,2);}
            if(velocity.y<0&&VehicleGround.Sample(bike,transform.position,0,.3f,out var floor)){transform.position=new Vector3(transform.position.x,floor.point.y+.04f,transform.position.z);Finish();}
            else if(age>4)Finish();
        }
        void Finish(){bike.Tumbling=false;bike.speed=0;transform.rotation=upright;VehicleGround.Settle(bike,.02f);Destroy(this);}
    }
}
