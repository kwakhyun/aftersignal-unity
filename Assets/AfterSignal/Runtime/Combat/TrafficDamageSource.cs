using UnityEngine;
namespace AfterSignal
{
    // Null remains the existing player-weapon source. Traffic accidents must
    // supply a separate source so collateral damage cannot blame the player.
    public static class TrafficDamageSource
    {
        static WorldActor environment;
        public static WorldActor Environment
        {
            get
            {
                if(environment)return environment;
                var go=new GameObject("Environmental traffic damage source");
                environment=go.AddComponent<WorldActor>();
                environment.environmental=true;environment.helicopter=true;environment.enabled=false;
                return environment;
            }
        }
    }
}
