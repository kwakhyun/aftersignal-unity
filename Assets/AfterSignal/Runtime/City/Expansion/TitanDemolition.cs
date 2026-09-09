using UnityEngine;
namespace AfterSignal
{
    public static class TitanDemolition
    {
        static float next;static readonly Collider[] hits=new Collider[128];
        public static void Strike(Vector3 at,float radius,WorldActor source)
        {
            if(Time.time<next)return;next=Time.time+6;int count=Physics.OverlapSphereNonAlloc(at,radius,hits,1,QueryTriggerInteraction.Ignore),collapsed=0;
            for(int i=0;i<count&&collapsed<2;i++)
            {
                var c=hits[i];if(!c||c.GetComponentInParent<CityVehicle>()||c.bounds.size.y<9||c.bounds.size.x>150||c.bounds.size.z>150||c.GetComponentInParent<VenueRuntime>())continue;
                var b=c.GetComponentInParent<CollapsibleBuilding>();if(!b){b=c.gameObject.AddComponent<CollapsibleBuilding>();b.worldBounds=c.bounds;b.cutBatches=true;}
                if(b.Collapsed)continue;b.Collapse(c.ClosestPoint(at),(c.bounds.center-at).normalized,source);collapsed++;
            }
        }
    }
}
