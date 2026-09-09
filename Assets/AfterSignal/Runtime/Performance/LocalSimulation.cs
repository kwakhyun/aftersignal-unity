using UnityEngine;

namespace AfterSignal
{
    // Distances affect incidental simulation, never terrain, collision or authored mission state.
    public static class LocalSimulation
    {
        public const float EncounterRadius=240, CombatRadius=420, RetireRadius=550;
        static int frame=-1;static Vector3 player;static bool available;
        public static bool Within(Vector3 point,float radius)
        {
            if(frame!=Time.frameCount){frame=Time.frameCount;var g=GameDirector.Instance;available=g&&g.Player;player=available?g.Player.transform.position:Vector3.zero;}
            return available&&(point-player).sqrMagnitude<=radius*radius;
        }
        public static bool Combat(Vector3 point)=>Within(point,CombatRadius);
        public static bool CanStart(Vector3 point)=>Within(point,EncounterRadius);
        public static bool PlayerAboard(CityVehicle vehicle)=>vehicle&&UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==vehicle;
    }
}
