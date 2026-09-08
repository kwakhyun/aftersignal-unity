using UnityEngine;
namespace AfterSignal
{
    public sealed class CombatProjectile:MonoBehaviour
    {
        Vector3 velocity;PlayerMotor owner;float fuse,damage;bool grenade,done;CityVehicle direct;
        public static CombatProjectile Launch(PlayerMotor owner,bool grenade,float damage)
        {
            var go=new GameObject(grenade?"Thrown fragmentation grenade":"LANCER rocket");go.transform.position=owner.Muzzle;
            var p=go.AddComponent<CombatProjectile>();p.owner=owner;p.grenade=grenade;p.damage=damage;p.fuse=grenade?2.4f:9;
            var direction=(owner.Aim-owner.Muzzle).normalized;p.velocity=direction*(grenade?17:100)+(grenade?Vector3.up*5:Vector3.zero);
            WorldGeometry.Part(go.transform,"Warhead",Vector3.zero,grenade?new Vector3(.17f,.24f,.17f):new Vector3(.15f,.15f,.65f),"DarkMetal",grenade?PrimitiveType.Sphere:PrimitiveType.Capsule);
            WorldGeometry.Part(go.transform,"Arming ring",Vector3.zero,new Vector3(.19f,.06f,.19f),"CyanFX");
            return p;
        }
        void Update()
        {
            if(done||!owner||owner.Director.Blocked)return;float dt=Mathf.Min(.05f,Time.deltaTime);fuse-=dt;
            if(grenade)velocity+=Vector3.down*14*dt;
            var step=velocity*dt;
            if(Ballistics.Cast(transform.position,step.normalized,step.magnitude+.12f,owner.transform,out var hit))
            {
                transform.position=hit.point+hit.normal*.13f;
                if(!grenade){direct=hit.collider.GetComponentInParent<CityVehicle>();Detonate();return;}
                velocity=Vector3.Reflect(velocity,hit.normal)*.42f;if(velocity.magnitude<1.1f)velocity=Vector3.zero;
            }
            else transform.position+=step;
            if(velocity.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(velocity);
            if(!grenade)SignalEffects.Beam(transform.position,transform.position-step,SignalEffects.Gold,.055f,.12f);
            if(fuse<=0)Detonate();
        }
        public void Detonate()
        {
            if(done)return;done=true;VehicleExplosion.Create(transform.position,grenade?2.2f:3.2f);BlastDamage.Create(transform.position,grenade?7:10,damage,null,null,grenade?BlastPayload.Conventional:BlastPayload.Missile,direct);
            owner?.Director.Audio.Play("urban_explosion",transform.position,.48f,3);Destroy(gameObject);
        }
    }
}
