using UnityEngine;
namespace AfterSignal
{
    // Shared local avoidance for transform-driven walkers. It preserves authored routes and
    // floor selection; it is not a substitute for a full multi-floor navigation mesh.
    public sealed class PedestrianSteering:MonoBehaviour
    {
        readonly Collider[] neighbors=new Collider[12];
        float sideUntil,nextNeighbors;Vector3 separation,avoidanceTarget;int side;bool avoiding;
        public Vector3 Move(Vector3 target,float distance)
        {
            var at=transform.position;Vector3 delta=target-at;delta.y=0;if(delta.sqrMagnitude<.0025f||distance<=0)return Vector3.zero;
            float length=delta.magnitude;Vector3 desired=delta/length;distance=Mathf.Min(distance,length);float clearance=distance+.6f;
            // Commit to a short corner waypoint. Re-aiming directly at the destination
            // every step caused alternating turns against the same wall.
            if(avoiding)
            {
                var onward=avoidanceTarget-at;onward.y=0;
                if(onward.sqrMagnitude<.06f||onward.sqrMagnitude>16)avoiding=false;
                else desired=onward.normalized;
            }
            if(Time.time>nextNeighbors)
            {
                nextNeighbors=Time.time+.18f;separation=Vector3.zero;
                int count=Physics.OverlapSphereNonAlloc(at+Vector3.up*.8f,.85f,neighbors,1<<9,QueryTriggerInteraction.Collide);
                for(int i=0;i<count;i++){var c=neighbors[i];if(!c||c.transform==transform||c.transform.IsChildOf(transform))continue;Vector3 away=at-c.transform.position;away.y=0;float d=away.magnitude;if(d>.03f&&d<.8f)separation+=away/d*(.8f-d)*.7f;}
            }
            Vector3 direction=(desired+separation).normalized;
            if(!Clear(at,direction,clearance))
            {
                if(Time.time>sideUntil){side=GetInstanceID()%2==0?1:-1;sideUntil=Time.time+1.4f;}
                bool found=false;
                for(int n=0;n<6;n++)
                {
                    float angle=(n/2+1)*45*(n%2==0?side:-side);var candidate=Quaternion.Euler(0,angle,0)*desired;
                    if(!Clear(at,candidate,2.2f))continue;direction=candidate;side=angle>0?1:-1;avoidanceTarget=at+direction*2.1f;avoiding=true;found=true;break;
                }
                if(!found)return Vector3.zero;
            }
            // Avoid snapping across corners or taking shortcuts across stair wells.
            Vector3 step=direction*distance,point=at+step;
            if(!PedestrianGround.Step(at,point,out var supported,.85f))return Vector3.zero;
            point=supported;transform.position=point;return point-at;
        }
        bool Clear(Vector3 at,Vector3 direction,float distance)
        {
            return PedestrianGround.Step(at,at+direction*distance,out _,.85f);
        }
        public static PedestrianSteering For(Component actor){var steering=actor.GetComponent<PedestrianSteering>();return steering?steering:actor.gameObject.AddComponent<PedestrianSteering>();}
    }
}
