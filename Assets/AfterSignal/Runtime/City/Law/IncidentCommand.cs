using UnityEngine;
namespace AfterSignal
{
    public static class IncidentCommand
    {
        static WorldActor localTitan;static float next;
        static void Refresh(){if(Time.time<next)return;next=Time.time+.25f;var g=GameDirector.Instance;localTitan=g?Monster(g.Player.transform.position,550):null;}
        public static bool Emergency{get{Refresh();return RiftIncursion.Instance&&RiftIncursion.Instance.Active||localTitan&&localTitan.Alive;}}
        public static Vector3 Position{get{Refresh();return localTitan&&localTitan.Alive?localTitan.transform.position:RiftIncursion.Instance&&RiftIncursion.Instance.Active?RiftIncursion.Instance.Position:Vector3.zero;}}
        public static WorldActor Monster(Vector3 from,float range)
        {
            WorldActor best=null;float d=range*range;
            foreach(var a in WorldActor.All)if(a&&a.monster&&a.Alive){float n=(a.Center-from).sqrMagnitude;if(n<d){d=n;best=a;}}
            return best;
        }
    }
}
