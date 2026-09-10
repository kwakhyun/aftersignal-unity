using UnityEngine;
namespace AfterSignal
{
    // Use the building's actual platform. Units queue at the landing instead of dropping through floors.
    public sealed class GarrisonLiftRide:MonoBehaviour
    {
        static MultiFloorLift[] elevators;static float refresh;public bool Riding=>riding;GarrisonSupport soldier;MultiFloorLift lift;bool riding;Vector3 previous;CharacterController motor;NpcGroundSupport ground;
        public static bool TryRide(GarrisonSupport soldier,Vector3 exit)
        {
            var ride=soldier.GetComponent<GarrisonLiftRide>();if(ride)return true;
            MultiFloorLift best=null;float d=75*75;
            if(elevators==null||Time.time>refresh){refresh=Time.time+10;elevators=Object.FindObjectsByType<MultiFloorLift>();}
            foreach(var l in elevators){if(!l)continue;float n=Vector3.ProjectOnPlane(l.transform.position-soldier.transform.position,Vector3.up).sqrMagnitude;if(l.platform&&n<d){d=n;best=l;}}
            if(!best||best.GetComponent<LiftUnitClaim>())return false;ride=soldier.gameObject.AddComponent<GarrisonLiftRide>();ride.soldier=soldier;ride.lift=best;return true;
        }
        void LateUpdate()
        {
            if(!soldier||!soldier.Body.Alive||!lift||!lift.platform){Destroy(this);return;}var g=GameDirector.Instance;if(!g||g.Blocked)return;
            var claim=lift.GetComponent<LiftUnitClaim>();
            if(!riding)
            {
                if(claim&&claim.Rider!=this||lift.Aboard(g.Player))return;
                if(!claim)claim=lift.gameObject.AddComponent<LiftUnitClaim>();claim.Rider=this;
                int floor=Mathf.Clamp(Mathf.RoundToInt((transform.position.y-lift.transform.position.y)/lift.floorHeight),0,lift.floors-1);
                var landing=new Vector3(lift.platform.position.x,transform.position.y,lift.platform.position.z-1.1f);
                soldier.WalkTo(landing,3*Time.deltaTime);lift.Go(floor);
                if(Vector3.ProjectOnPlane(transform.position-landing,Vector3.up).magnitude<1.6f&&!lift.Moving&&Mathf.Abs(transform.position.y-lift.platform.position.y)<1.2f)
                {riding=true;motor=GetComponent<CharacterController>();if(motor)motor.enabled=false;ground=GetComponent<NpcGroundSupport>();if(ground)ground.enabled=false;previous=lift.platform.position;lift.Go(0);}
            }
            else
            {
                transform.position+=lift.platform.position-previous;previous=lift.platform.position;
                if(!lift.Moving&&lift.CurrentFloor==0){transform.position+=Vector3.back*2.6f;Destroy(this);}
            }
        }
        void OnDestroy(){if(motor)motor.enabled=true;if(ground)ground.enabled=true;if(lift){var c=lift.GetComponent<LiftUnitClaim>();if(c&&c.Rider==this)Destroy(c);}}
    }
    public sealed class LiftUnitClaim:MonoBehaviour{public GarrisonLiftRide Rider;}
}
