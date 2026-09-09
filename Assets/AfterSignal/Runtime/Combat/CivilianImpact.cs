using UnityEngine;

namespace AfterSignal
{
    [DefaultExecutionOrder(1100)]
    public sealed class CivilianImpact : MonoBehaviour
    {
        Vector3 velocity;float age,spin,settled;WorldActor actor;
        public bool Flying {get;private set;}
        public static bool Active(Component who){var c=who.GetComponent<CivilianImpact>();return c&&c.Flying;}
        public static void Launch(WorldActor target,Vector3 direction,float speed)
        {
            if(!target||target.robot||target.monster||target.helicopter)return;
            var c=target.GetComponent<CivilianImpact>()??target.gameObject.AddComponent<CivilianImpact>();
            c.actor=target;c.age=c.settled=0;c.Flying=true;
            direction.y=0;direction.Normalize();
            c.velocity=direction*Mathf.Clamp(speed*1.32f,3,43)+Vector3.up*Mathf.Clamp(speed*.43f,2,13);
            c.spin=Random.Range(-210f,210f);
        }
        void Update()
        {
            if(!Flying||GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            float dt=Mathf.Min(.04f,Time.deltaTime);age+=dt;velocity.y-=19.6f*dt;
            Vector3 from=transform.position,delta=velocity*dt;
            if(Physics.SphereCast(from+Vector3.up*.5f,.22f,delta.normalized,out var hit,delta.magnitude,1,QueryTriggerInteraction.Ignore))
            {
                transform.position=hit.point+hit.normal*.24f-Vector3.up*.5f;
                if(hit.normal.y>.5f)
                {
                    transform.position=new Vector3(transform.position.x,hit.point.y+.13f,transform.position.z);
                    velocity=new Vector3(velocity.x*.72f,Mathf.Abs(velocity.y)*.17f,velocity.z*.72f);
                    settled+=dt;
                    if(velocity.magnitude<2||settled>.8f)Flying=false;
                }
                else velocity=Vector3.Reflect(velocity,hit.normal)*.3f;
            }
            else transform.position+=delta;
            if(age>8)Flying=false;
            if(!Flying)
            {
                var ped=GetComponent<CityPedestrian>();if(ped){ped.velocity=Vector3.zero;ped.age=0;ped.dead=!actor.Alive;ped.struck=!actor.Alive;}
                if(actor.Alive)Destroy(this);
            }
        }
        void LateUpdate()
        {
            if(!Flying)return;
            var cam=Camera.main;
            transform.rotation=Quaternion.Euler(cam?cam.transform.eulerAngles.x:0,cam?cam.transform.eulerAngles.y:0,age*spin);
        }
    }
}
