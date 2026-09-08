using UnityEngine;

namespace AfterSignal
{
    public static class Ballistics
    {
        public const float Range = 1400f;
        // Camera acquisition and barrel obstruction use the same filter. Doorway/AI trigger
        // volumes must not absorb rounds; real damage hitboxes deliberately remain triggers.
        public static bool Cast(Vector3 origin, Vector3 direction, float range, Transform owner, out RaycastHit closest, bool includePlayer=false)
        {
            closest=default; float distance=range+1;
            foreach(var hit in Physics.RaycastAll(origin,direction,range,(1<<0)|(1<<9)|(includePlayer?1<<8:0),QueryTriggerInteraction.Collide))
            {
                var c=hit.collider;
                if(owner && c.transform.IsChildOf(owner))continue;
                if(owner&&owner.GetComponent<PlayerMotor>()&&UrbanSimulation.Instance&&UrbanSimulation.Instance.Current&&c.transform.IsChildOf(UrbanSimulation.Instance.Current.transform))continue;
                var facade=c.GetComponentInParent<FacadeGlass>();if(facade&&facade.OpenAt(hit.point))continue;
                var actor=c.GetComponentInParent<WorldActor>();
                if(actor&&!actor.Alive)continue;
                if(c.isTrigger&&!actor&&!c.GetComponentInParent<EnemyBrain>()&&!c.GetComponentInParent<CityVehicle>()&&!c.GetComponentInParent<BreakableGlass>())continue;
                if(hit.distance>=distance)continue;
                distance=hit.distance;closest=hit;
            }
            return distance<=range;
        }
        public static Vector3 AimPoint(Ray ray,Transform owner)
            =>Cast(ray.origin,ray.direction,Range,owner,out var hit)?hit.point:ray.GetPoint(Range);
    }
}
