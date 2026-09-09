using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityVehicle
    {
        readonly Collider[] overlaps=new Collider[24];
        Collider chassisCollider;Vector3 collisionDrift;
        float Mass=>type==CityVehicleType.Tank?8:IsHeavy?3.5f:type==CityVehicleType.Motorcycle?.35f:1;
        void RecoverContact()
        {
            if(!chassisCollider)
            {
                float largest=0;
                foreach(var c in GetComponentsInChildren<Collider>())
                {
                    if(!c.enabled||c.isTrigger||c.GetComponentInParent<CityVehicle>()!=this||!(c is BoxCollider||c is CapsuleCollider||c is MeshCollider mesh&&mesh.convex))continue;
                    var size=c.bounds.size;float volume=size.x*size.y*size.z;if(volume>largest){largest=volume;chassisCollider=c;}
                }
            }
            if(!chassisCollider)return;
            int count=Physics.OverlapBoxNonAlloc(transform.position+Vector3.up*.9f,new Vector3(HalfLength+.6f,.48f,HalfWidth+.6f),overlaps,transform.rotation,1,QueryTriggerInteraction.Ignore);
            for(int pass=0;pass<3;pass++)
            {
                bool moved=false;
                for(int i=0;i<count;i++)
                {
                    var c=overlaps[i];if(!c||c.transform.IsChildOf(transform))continue;
                    if(!Physics.ComputePenetration(chassisCollider,chassisCollider.transform.position,chassisCollider.transform.rotation,c,c.transform.position,c.transform.rotation,out var away,out var depth))continue;
                    if(Mathf.Abs(away.y)>.55f||depth<.001f)continue;
                    away.y=0;transform.position+=away.normalized*Mathf.Min(depth+.025f,.6f);moved=true;
                }
                if(!moved)break;
                // ComputePenetration consumes the explicit current poses. A world-wide physics
                // synchronization for every car/contact/pass caused repeated broadphase rebuilds.
            }
        }
        bool MovingOut(Collider obstacle,Vector3 delta)
        {
            if(chassisCollider&&Physics.ComputePenetration(chassisCollider,chassisCollider.transform.position,chassisCollider.transform.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out var away,out _)&&Vector3.Dot(delta,away)>.0001f)return true;
            // Sweep clearance is slightly larger than the chassis. At a touching contact there
            // may be no physical penetration, but reversing still increases the separation.
            var center=transform.position+Vector3.up*.9f;
            var closest=obstacle.bounds.ClosestPoint(center);
            return Vector3.Dot(delta,center-closest)>.00001f;
        }
        void PushVehicle(CityVehicle other,Vector3 direction,float force)
        {
            if(!other||other.IsSpecial||other==this)return;
            direction.y=0;other.collisionDrift+=direction.normalized*Mathf.Min(force*Mass/(Mass+other.Mass)*.45f,9);
        }
        void TickCollisionDrift(float dt)
        {
            if(Tumbling){collisionDrift=Vector3.zero;return;}
            if(IsSpecial||collisionDrift.sqrMagnitude<.002f)return;
            RecoverContact();
            var step=collisionDrift*dt;
            int count=Physics.BoxCastNonAlloc(transform.position+Vector3.up*.9f,new Vector3(HalfLength-.12f,.42f,HalfWidth-.12f),step.normalized,hits,transform.rotation,step.magnitude,1,QueryTriggerInteraction.Ignore);
            float allowed=step.magnitude;
            for(int i=0;i<count;i++){var hit=hits[i];if(hit.collider.transform.IsChildOf(transform)||hit.normal.y>.65f||hit.distance<=.01f&&MovingOut(hit.collider,step))continue;allowed=Mathf.Min(allowed,Mathf.Max(0,hit.distance-.03f));}
            transform.position+=step.normalized*allowed;
            collisionDrift=Vector3.MoveTowards(collisionDrift,Vector3.zero,dt*9);VehicleGround.Settle(this,dt);
        }
    }
}
