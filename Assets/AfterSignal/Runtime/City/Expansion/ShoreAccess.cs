using UnityEngine;
namespace AfterSignal
{
    // Land decks are thin collision surfaces; water-to-land movement needs a solid coast below them.
    public static class ShoreAccess
    {
        static bool LandColumn(Vector3 at)=>!OceanLife.Contains(new Vector3(at.x,OceanLife.Surface,at.z));
        static bool Landing(Vector3 at,out Vector3 safe)
        {
            safe=at;
            if(!LandColumn(at)||!NpcGroundSupport.Floor(at,OceanLife.Surface+4,9,out float y)||y<OceanLife.Surface-.2f)return false;
            safe.y=y+.08f;
            return !Physics.CheckCapsule(safe+Vector3.up*.4f,safe+Vector3.up*1.65f,.32f,1,QueryTriggerInteraction.Ignore);
        }
        public static Vector3 Constrain(PlayerMotor player,Vector3 delta)
        {
            var start=player.transform.position;var next=start+delta;
            // Explicit deep-city pressure chambers retain their authored underwater entrance.
            if(next.y< -40&&FourCityCatalog.Dry(next))return delta;
            bool blocked=false;
            for(int i=1;i<=3;i++)if(LandColumn(Vector3.Lerp(start,next,i/3f))){blocked=true;break;}
            if(!blocked)return delta;
            var horizontal=Vector3.ProjectOnPlane(delta,Vector3.up).normalized;
            if(start.y>OceanLife.Surface-1.3f&&Landing(next+horizontal*.85f,out var safe))
            {player.Respawn(safe,false);player.Director.CameraRig.Snap();return Vector3.zero;}
            player.Velocity=new Vector3(0,player.Velocity.y,0);
            return Vector3.up*delta.y;
        }
        public static bool Recover(PlayerMotor player)
        {
            var p=player.transform.position;
            if(p.y>=OceanLife.Surface-.1f||!LandColumn(p)||p.y< -40&&FourCityCatalog.Dry(p))return false;
            if(Landing(p,out var safe)){player.Respawn(safe,false);player.Director.CameraRig.Snap();return true;}
            for(int ring=1;ring<=8;ring++)for(int i=0;i<12;i++)
            {
                var q=p+Quaternion.Euler(0,i*30,0)*Vector3.forward*(ring*4);
                if(!OceanLife.Contains(q))continue;q.y=Mathf.Max(q.y,OceanLife.Bed(q.x,q.z)+1);player.Respawn(q,false);player.Director.CameraRig.Snap();return true;
            }
            return false;
        }
    }
}
