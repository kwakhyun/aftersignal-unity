using UnityEngine;
namespace AfterSignal
{
    public sealed partial class PlayerMotor
    {
        float mountedShot,vehicleProtection;
        public void ProtectVehicleImpact()=>vehicleProtection=Time.time+2;
        public void TickVehicleStatus(float dt){invincible=Mathf.Max(0,invincible-dt);HurtTime=Mathf.Max(0,HurtTime-dt);}
        public void TickMountedCombat(ControlFrame input,float dt)
        {
            AttackTime=Mathf.Max(0,AttackTime-dt);mountedShot=Mathf.Max(0,mountedShot-dt);
            if(!Camera.main)return;
            Aim=Ballistics.AimPoint(Camera.main.ViewportPointToRay(new Vector3(.5f,.5f)),UrbanSimulation.Instance.Current.transform);
            UpdateArsenal(input,dt);
            if(!input.attack||Health<=0)return;
            if(Equipment&&Equipment.Extended){if(Equipment.Slot==3||Equipment.Slot==6)Equipment.Attack();return;}
            if(Weapon!=WeaponId.Pistol){Equipment.Select(2);return;}
            if(Reloading||mountedShot>0)return;
            if(Ammo<=0){BeginReload();return;}
            FirePistol(Tuning.pistolDamage,false);mountedShot=.28f;
        }
    }
}
