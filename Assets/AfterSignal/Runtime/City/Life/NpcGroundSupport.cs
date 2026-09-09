using UnityEngine;
namespace AfterSignal
{
    // Transform-driven crowds need the same floor support as the player controller.
    [DefaultExecutionOrder(1050)]
    public sealed class NpcGroundSupport:MonoBehaviour
    {
        static readonly RaycastHit[] hits=new RaycastHit[48];
        WorldActor actor;CharacterController controller;Vector3 safe,lastChecked;bool supported;float next,refresh,stationaryCheck;
        DirectionalPerson art;CityPedestrian ped;MedicalPending pending;CityVehicle mount;
        public int Corrections {get;private set;}
        void Start(){actor=GetComponent<WorldActor>();controller=GetComponent<CharacterController>();safe=transform.position;next=Time.time+(Mathf.Abs(GetInstanceID())%13)*.007f;}
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
            if(!actor||actor.helicopter||actor.monster||actor.environmental||Time.time<next)return;
            float distance=ActorWorkBudget.DistanceSquared(this);next=Time.time+(distance>160*160?.75f:distance>70*70?.32f:.12f);
            if(Time.time>=refresh){refresh=Time.time+.7f;art=GetComponent<DirectionalPerson>();ped=GetComponent<CityPedestrian>();pending=GetComponent<MedicalPending>();mount=GetComponentInParent<CityVehicle>();}
            if(mount||CivilianImpact.Active(this)||pending&&pending.carried||art&&(art.Lying||art.Sitting)||ped&&ped.struck&&ped.velocity.sqrMagnitude>.1f)return;
            var p=transform.position;
            if(supported&&(p-lastChecked).sqrMagnitude<.0004f&&Time.time<stationaryCheck)return;
            // Spread corrective work across frames. Close falls still recover immediately.
            if(distance>35*35&&!AmbientWorkBudget.ClaimGround())return;
            lastChecked=p;stationaryCheck=Time.time+1.2f;
            // Respect explicit teleports, elevators and floor transitions; validate their landing.
            float horizontal=Vector2.Distance(new(p.x,p.z),new(safe.x,safe.z));
            bool relocated=horizontal>18||Mathf.Abs(p.y-safe.y)>4&&p.y>safe.y;
            if(supported&&!relocated&&!controller&&horizontal>.015f&&actor.Alive)
            {
                if(!PedestrianGround.Step(safe,p,out var step,.85f))
                {
                    var x=new Vector3(p.x,safe.y,safe.z);var z=new Vector3(safe.x,safe.y,p.z);
                    if(PedestrianGround.Step(safe,x,out step,.85f)||PedestrianGround.Step(safe,z,out step,.85f))p=step;
                    else p=safe;
                    Place(p);Corrections++;
                }
            }
            // A short upward allowance cannot select the next storey or a roof overhead.
            float origin=p.y+.65f;
            if(supported&&Mathf.Abs(p.y-safe.y)<3)origin=Mathf.Max(origin,safe.y+.45f);
            if(Floor(p,origin,3.4f,out var y)&&y<=p.y+.62f)
            {
                var grounded=new Vector3(p.x,y+(actor.Alive?.035f:.065f),p.z);
                bool clear=!actor.Alive||!Physics.CheckCapsule(grounded+Vector3.up*.39f,grounded+Vector3.up*1.65f,.27f,1,QueryTriggerInteraction.Ignore);
                if(clear){if(!controller||p.y<y-.05f)Place(grounded);safe=grounded;supported=true;}
                else if(supported&&!relocated){Place(safe);Corrections++;}
                else
                {
                    // Authored spawns embedded in a wall recover to the nearest supported free point.
                    int phase=Mathf.FloorToInt(Time.time*4)%4;
                    for(int i=0;i<8;i++){var q=grounded+Quaternion.Euler(0,i*45,0)*Vector3.forward*(.65f+phase*.8f);if(!PedestrianGround.Stand(q,grounded.y,.6f,out q))continue;Place(q);safe=q;supported=true;Corrections++;break;}
                }
            }
            else if(supported&&!relocated&&p.y<safe.y-1.2f){Place(safe);Corrections++;}
        }
        void Place(Vector3 p){bool enabled=controller&&controller.enabled;if(enabled)controller.enabled=false;transform.position=p;if(enabled)controller.enabled=true;}
        void OnTransformParentChanged(){refresh=0;supported=false;}
    }
}
