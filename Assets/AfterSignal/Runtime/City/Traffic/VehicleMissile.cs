using UnityEngine;
namespace AfterSignal
{
    public sealed class VehicleMissile:MonoBehaviour
    {
        VehicleArmament owner;Transform target;Vector3 aim,velocity;float age;bool shell;
        public static int Detonations{get;private set;}
        public static VehicleMissile Launch(VehicleArmament owner,Vector3 start,Vector3 aim,Transform target,bool shell)
        {
            var p=new GameObject(shell?"120 mm shell":"RAVEN guided missile").AddComponent<VehicleMissile>();p.owner=owner;p.target=target;p.aim=aim;p.shell=shell;p.transform.position=start;
            p.velocity=(aim-start).normalized*(shell?340:38);
            WorldGeometry.Part(p.transform,"Warhead",Vector3.zero,new Vector3(.18f,.18f,shell?.55f:1.45f),"Chrome",PrimitiveType.Sphere);
            if(!shell)for(int i=0;i<4;i++){var fin=WorldGeometry.Part(p.transform,"Stabilizer fin",new Vector3(0,0,-.4f),new Vector3(.6f,.025f,.34f),"DarkMetal");fin.transform.localRotation=Quaternion.Euler(0,0,i*45);}
            return p;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.05f,Time.deltaTime);age+=dt;
            if(target)aim=target.position+Vector3.up*1.5f;
            if(!shell){var desired=(aim-transform.position).normalized;velocity=Vector3.RotateTowards(velocity.normalized,desired,dt*1.1f,0)*Mathf.Min(145,38+age*65);}
            var step=velocity*dt;
            if(Ballistics.Cast(transform.position,step.normalized,step.magnitude+.12f,owner?owner.transform:null,out var hit)){transform.position=hit.point;Detonate();return;}
            transform.position+=step;transform.rotation=Quaternion.LookRotation(velocity);
            SignalEffects.Beam(transform.position,transform.position-step,shell?SignalEffects.Gold:new Color(.8f,.88f,.88f,.6f),shell?.09f:.13f,shell?.15f:1.2f);
            if((aim-transform.position).sqrMagnitude<4||age>12)Detonate();
        }
        void Detonate()
        {
            Detonations++;VehicleExplosion.Create(transform.position,shell?3:4);BlastDamage.Create(transform.position,shell?10:14,shell?240:270);
            GameDirector.Instance?.Audio.Play("urban_explosion",transform.position,.5f,3);Destroy(gameObject);
        }
    }
}
