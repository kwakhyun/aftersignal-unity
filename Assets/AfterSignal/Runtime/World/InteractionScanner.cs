using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class InteractionScanner
    {
        readonly List<InteractionPoint> candidates=new(64);Vector3 sampled;float next;int registrySize=-1;
        public InteractionPoint Nearest(PlayerMotor player)
        {
            var shoulder=player.Shoulder;
            if(Time.time>next||registrySize!=InteractionPoint.All.Count||(sampled-shoulder).sqrMagnitude>12*12)
            {
                candidates.Clear();sampled=shoulder;next=Time.time+.2f;registrySize=InteractionPoint.All.Count;
                foreach(var point in InteractionPoint.All)if(point&&(point.transform.position-shoulder).sqrMagnitude<(point.radius+12)*(point.radius+12))candidates.Add(point);
            }
            InteractionPoint nearest=null;float best=float.MaxValue;
            foreach(var point in candidates)
            {
                if(!point||!point.gameObject.activeInHierarchy||point.Used&&point.kind!=InteractionKind.Noa&&point.kind!=InteractionKind.Citizen)continue;
                float distance=(point.transform.position-shoulder).sqrMagnitude;if(distance>point.radius*point.radius||distance>=best)continue;
                if(point.npc&&point.npc.TryGetComponent<WorldActor>(out var actor)&&!actor.Alive)continue;
                nearest=point;best=distance;
            }
            return nearest;
        }
    }
}
