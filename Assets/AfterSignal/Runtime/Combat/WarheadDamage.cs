using UnityEngine;
namespace AfterSignal
{
    public enum BlastPayload { Conventional, Missile, ArmourPiercing }
    public static class WarheadDamage
    {
        // Durability still protects against traffic and small arms. Shaped charges defeat it.
        public static float Against(CityVehicle car, BlastPayload payload, float baseDamage)
        {
            if(payload==BlastPayload.Conventional)return baseDamage;
            float fraction=car.GetComponent<AuthoredCraft>()?.28f:car.type==CityVehicleType.Tank?.39f:
                car.type==CityVehicleType.Boat?.55f:car.IsAircraft?.72f:car.GetComponent<TacticalTransport>()?.65f:1.35f;
            if(payload==BlastPayload.ArmourPiercing)fraction*=.8f;
            return Mathf.Max(baseDamage,car.MaxHealth*fraction);
        }
        public static Vector3 HullPoint(CityVehicle car,Vector3 origin)
        {
            var collider=car.GetComponent<Collider>();
            return collider?collider.ClosestPoint(origin):car.transform.position+Vector3.up;
        }
    }
}
