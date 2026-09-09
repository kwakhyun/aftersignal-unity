using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Decisions are local, deterministic and throttled. A dispatch order is not permission to fire.
    public static class TacticalJudgment
    {
        static readonly List<WorldActor> nearby=new();
        public static bool Active(WorldActor a)
        {
            if(!a||!a.gameObject.activeInHierarchy||!a.Alive)return false;
            if(a.enabled)return true;
            var mount=a.helicopter?a.GetComponentInParent<CityVehicle>():null;
            return mount&&mount.occupied&&!mount.Wrecked;
        }
        public static bool Opponent(WorldActor self,WorldActor other,bool allowDowned=false)
        {
            if(!Active(self)||self.Downed||!Active(other)||self==other||other.environmental||other.protectedResident||(!allowDowned&&other.Downed))return false;
            if(self.monster)return !other.monster;
            if(self.police||self.military)return (other.gang||other.monster||other.terrorist)&&!(self.military&&other.terrorist);
            if(self.gang||self.terrorist)return !other.gang&&!other.terrorist;
            return other.gang||other.monster||other.terrorist;
        }
        public static bool ClearShot(WorldActor self,WorldActor target,Vector3 point,bool player,float range)
        {
            if(!Active(self)||self.Downed||(!player&&!Opponent(self,target,self.gang)))return false;
            var g=GameDirector.Instance;
            if(player&&(!g||g.Player.Health<=0||(self.police||self.military)&&(WantedSystem.Level<=0||IncidentCommand.Emergency)))return false;
            var from=self.Center+Vector3.up*.24f;var delta=point-from;if(delta.sqrMagnitude>range*range)return false;
            if(!Ballistics.Cast(from,delta.normalized,delta.magnitude+.35f,self.transform,out var hit,true))return false;
            var body=hit.collider.GetComponentInParent<WorldActor>();
            if(target&&body==target)return true;
            if(target){var targetCar=target.GetComponentInParent<CityVehicle>();if(targetCar&&hit.collider.GetComponentInParent<CityVehicle>()==targetCar)return true;}
            if(player&&(hit.collider.GetComponentInParent<PlayerMotor>()||UrbanSimulation.Instance&&UrbanSimulation.Instance.Current&&hit.collider.transform.IsChildOf(UrbanSimulation.Instance.Current.transform)))return true;
            // Gangs accept civilian collateral, but do not fire through their own crew or masonry.
            return self.gang&&body&&Opponent(self,body,true);
        }
        public static WorldActor Hazard(Vector3 at,float radius)
        {
            WorldActor closest=null;float best=radius*radius;ActorSpatialIndex.Nearby(at,radius,nearby);
            foreach(var a in nearby){if(!Active(a)||a.Downed||!(a.gang||a.monster||a.terrorist))continue;float d=(a.Center-at).sqrMagnitude;if(d<best&&FactionCombat.Visible(at+Vector3.up,a.Center,radius)){best=d;closest=a;}}
            return closest;
        }
        public static Vector3 Flank(WorldActor self,Vector3 target,float distance=5)
        {
            var d=Vector3.ProjectOnPlane(target-self.transform.position,Vector3.up).normalized;
            return self.transform.position+Vector3.Cross(Vector3.up,d)*((self.GetInstanceID()&1)==0?distance:-distance);
        }
        public static void RequestProtection(Component caller,WorldActor hazard)
        {
            if(!Active(hazard))return;int count=0;
            foreach(var a in WorldActor.All){if(!Active(a)||a.Downed||!a.police||(a.Center-caller.transform.position).sqrMagnitude>180*180)continue;var officer=a.GetComponent<PoliceOfficer>();if(officer){officer.Dispatch(hazard);if(++count==3)break;}}
        }
        public static Vector3 SafeWorkPoint(Vector3 target,Vector3 origin,float radius,int slot)
        {
            Vector3 best=origin;float score=float.NegativeInfinity;
            for(int i=0;i<8;i++)
            {
                var d=Quaternion.Euler(0,i*45+slot*22,0)*Vector3.forward;var p=target+d*radius;p.y=origin.y;
                if(!PedestrianGround.Stand(p,origin.y,.85f,out p))continue;
                float rank=-(p-origin).sqrMagnitude;if(Physics.Linecast(p+Vector3.up*1.2f,target,1,QueryTriggerInteraction.Ignore))rank-=1000;
                if(rank>score){score=rank;best=p;}
            }
            return best;
        }
    }
}
