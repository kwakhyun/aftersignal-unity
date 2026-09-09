using UnityEngine;
namespace AfterSignal
{
    // Transform-driven crowds need the same floor support as the player controller.
    [DefaultExecutionOrder(1050)]
    public sealed class NpcGroundSupport:MonoBehaviour
    {
        static readonly RaycastHit[] hits=new RaycastHit[48];
        WorldActor actor;CharacterController controller;Vector3 safe;bool supported;float next;
        void Start(){actor=GetComponent<WorldActor>();controller=GetComponent<CharacterController>();safe=transform.position;}
        public static bool Floor(Vector3 point,float ceiling,float distance,out float height)
        {
            height=float.NegativeInfinity;
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,ceiling,point.z),Vector3.down,hits,distance,1,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var h=hits[i];var c=h.collider;
                if(h.normal.y<.65f||!c||c.attachedRigidbody||c.GetComponentInParent<WorldActor>()||c.GetComponentInParent<CityVehicle>())continue;
                height=Mathf.Max(height,h.point.y);
            }
            return !float.IsNegativeInfinity(height);
        }
        void LateUpdate()
        {
            if(!actor||controller||actor.helicopter||actor.monster||CivilianImpact.Active(this))return;
            var art=GetComponent<DirectionalPerson>();if(art&&(art.Lying||art.Sitting))return;
            var ped=GetComponent<CityPedestrian>();if(ped&&ped.struck&&ped.velocity.sqrMagnitude>.1f)return;
            if(Time.time<next)return;float distance=ActorWorkBudget.DistanceSquared(this);next=Time.time+(distance>160*160?.55f:distance>70*70?.24f:.09f);
            var p=transform.position;
            // A short upward allowance cannot select the next storey or a roof overhead.
            float origin=p.y+.65f;
            if(supported&&Mathf.Abs(p.y-safe.y)<3)origin=Mathf.Max(origin,safe.y+.45f);
            if(Floor(p,origin,3.4f,out var y)&&y<=p.y+.62f)
            {
                p.y=y+(actor.Alive?.035f:.065f);transform.position=p;safe=p;supported=true;
            }
            else if(supported&&p.y<safe.y-1.5f&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(safe.x,safe.z))<6)
                transform.position=safe;
        }
    }
}
