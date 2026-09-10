using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Damage/death events query nearby actors only; no per-NPC polling or world-wide speech scans.
    public static class WitnessReactions
    {
        static readonly List<WorldActor> nearby=new();static int frame=-1,spoken;
        public static int Notify(WorldActor victim,bool death)
        {
            if(!victim||victim.monster||victim.robot||victim.helicopter||victim.environmental||!LocalSimulation.Combat(victim.transform.position))return 0;
            if(frame!=Time.frameCount){frame=Time.frameCount;spoken=0;}if(spoken>=6)return 0;
            ActorSpatialIndex.Nearby(victim.transform.position,32,nearby);
            var point=victim.Center;nearby.Sort((a,b)=>(a.Center-point).sqrMagnitude.CompareTo((b.Center-point).sqrMagnitude));
            int count=0;
            foreach(var witness in nearby)
            {
                if(!witness||witness==victim||!witness.Alive||witness.Downed||witness.monster||witness.robot||witness.helicopter||witness.environmental)continue;
                if(!FactionCombat.Visible(witness.Center,point,34))continue;
                if(!NpcSpeech.Witness(witness,death))continue;
                count++;spoken++;if(count>=3||spoken>=6)break;
            }
            return count;
        }
    }
}
