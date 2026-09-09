using UnityEngine;
namespace AfterSignal
{
    public static class MaritimeWeapons
    {
        public static void Fire(SeaCombat vessel,Vector3 target,bool player)
        {
            var car=vessel.Car;var direction=(target-vessel.AimCenter).normalized;
            var muzzle=vessel.AimCenter+direction*(car.HalfLength*.52f+1);var end=muzzle+direction*800;
            float damage=vessel.Faction==SeaFaction.Navy?850:vessel.Faction==SeaFaction.CoastGuard?230:300;
            var source=player?null:vessel.Body;
            if(Ballistics.Cast(muzzle,direction,800,car.transform,out var hit,true))
            {
                end=hit.point;var vehicle=hit.collider.GetComponentInParent<CityVehicle>();
                if(vehicle)vehicle.Damage(damage,end,source,false);
                else {hit.collider.GetComponentInParent<WorldActor>()?.Damage(damage,direction*8,source);hit.collider.GetComponentInParent<PlayerMotor>()?.ReceiveDamage(25,muzzle);}
                VehicleExplosion.Create(end,1.1f);
            }
            CombatVfx.Tracer(muzzle,end,SignalEffects.Gold);
            GameDirector.Instance?.Audio.PlayGun(GunshotKind.Rifle,muzzle,.85f);
        }
    }
}
