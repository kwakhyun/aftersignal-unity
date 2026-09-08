using UnityEngine;
namespace AfterSignal
{
    public static class VehicleDurability
    {
        public static float Capacity(CityVehicle car)
        {
            if(car.GetComponent<AuthoredCraft>())return 50000;
            if(car.GetComponent<TacticalTransport>()&&!car.GetComponent<PoliceCar>())return 6500;
            return car.type switch {
                CityVehicleType.Motorcycle=>600, CityVehicleType.SportsCar=>1000,
                CityVehicleType.Bus=>2200, CityVehicleType.Truck=>2800,
                CityVehicleType.Boat=>7000, CityVehicleType.Airliner=>18000,
                CityVehicleType.CombatHelicopter=>6000, CityVehicleType.Fighter=>10000,
                CityVehicleType.Tank=>24000, _=>car.GetComponent<PoliceCar>()?1400:1000
            };
        }
        // A bumper contact is a small repair bill; even a severe road impact takes
        // several repeated hits to destroy a maintained vehicle.
        public static float CollisionDamage(float closingSpeed,float capacity)
            => Mathf.Min(capacity*.085f,Mathf.Max(0,closingSpeed-2)*Mathf.Max(1,closingSpeed-6)*.13f);
        public static float OccupantDamage(float closingSpeed,bool bike)
            => Mathf.Clamp((closingSpeed-6)*(bike?.65f:.24f),0,bike?24:12);
    }

    public sealed partial class CityVehicle
    {
        public int durabilityVersion;
        bool playerDamage;WorldActor lastAttacker;float collisionSpeechAt;
        public WorldActor DamageSource=>playerDamage?null:lastAttacker?lastAttacker:TrafficDamageSource.Environment;
        internal void RecordDamageSource(WorldActor source){playerDamage=!source;lastAttacker=source;}
        public float MaxHealth=>VehicleDurability.Capacity(this);
        public float HealthFraction=>Mathf.Clamp01(health/MaxHealth);
        public void InitializeDurability()
        {
            if(durabilityVersion>0)return;
            health=Mathf.Clamp01(health/100)*MaxHealth;durabilityVersion=1;
        }
        public void CollisionDamage(float closingSpeed,Vector3 point,CityVehicle other=null)
        {
            InitializeDurability();
            if(closingSpeed>3&&occupied&&Time.time>collisionSpeechAt){collisionSpeechAt=Time.time+7;NpcSpeech.Say(this,NpcDialogueBank.Line(null,"collision"),4,4);}
            var sim=UrbanSimulation.Instance;var game=GameDirector.Instance;
            bool player=sim&&sim.Current&&(sim.Current==this||sim.Current==other);
            Damage(VehicleDurability.CollisionDamage(closingSpeed,MaxHealth),point,player?null:TrafficDamageSource.Environment,false);
            if(sim&&sim.Current==this&&game)
                game.Player.ReceiveDamage(VehicleDurability.OccupantDamage(closingSpeed,type==CityVehicleType.Motorcycle),point);
            GetComponent<VehicleCabin>()?.InjureOccupants(closingSpeed);
        }
    }
}
