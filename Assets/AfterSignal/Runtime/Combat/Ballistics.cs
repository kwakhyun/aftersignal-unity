using UnityEngine;

namespace AfterSignal
{
    public static class Ballistics
    {
        public const float Range = 1400f;
        // Camera acquisition and barrel obstruction use the same filter. Doorway/AI trigger
        // volumes must not absorb rounds; real damage hitboxes deliberately remain triggers.
        static readonly RaycastHit[] buffer=new RaycastHit[96];
        public static bool Ignore(RaycastHit hit,Transform owner,Transform mount=null)
        {
            var c=hit.collider;if(!c||owner&&c.transform.IsChildOf(owner)||mount&&c.transform.IsChildOf(mount))return true;
            if(owner&&owner.GetComponent<PlayerMotor>()&&UrbanSimulation.Instance&&UrbanSimulation.Instance.Current&&c.transform.IsChildOf(UrbanSimulation.Instance.Current.transform))return true;
            var facade=c.GetComponentInParent<FacadeGlass>();if(facade&&facade.OpenAt(hit.point))return true;
            var actor=c.GetComponentInParent<WorldActor>();
            // Prone damage volumes belong to the actual body, unlike conversation/AI triggers.
            if(actor&&!actor.Alive&&!c.GetComponent<ProneHitVolume>())return true;
            return c.isTrigger&&!c.GetComponent<ProneHitVolume>()&&(!actor||c.GetComponent<InteractionPoint>())&&!c.GetComponentInParent<EnemyBrain>()&&!c.GetComponentInParent<CityVehicle>()&&!c.GetComponentInParent<BreakableGlass>();
        }
        public static bool Cast(Vector3 origin, Vector3 direction, float range, Transform owner, out RaycastHit closest, bool includePlayer=false)
        {
            closest=default; float distance=range+1;
            int count=Physics.RaycastNonAlloc(origin,direction,buffer,range,(1<<0)|(1<<9)|(includePlayer?1<<8:0),QueryTriggerInteraction.Collide);
            var contacts=buffer;if(count==buffer.Length){contacts=Physics.RaycastAll(origin,direction,range,(1<<0)|(1<<9)|(includePlayer?1<<8:0),QueryTriggerInteraction.Collide);count=contacts.Length;}
            for(int i=0;i<count;i++)
            {
                var hit=contacts[i];if(Ignore(hit,owner))continue;
                if(hit.distance>=distance)continue;
                distance=hit.distance;closest=hit;
            }
            return distance<=range;
        }
        public static Vector3 AimPoint(Ray ray,Transform owner)
            =>Cast(ray.origin,ray.direction,Range,owner,out var hit)?hit.point:ray.GetPoint(Range);
    }
}
