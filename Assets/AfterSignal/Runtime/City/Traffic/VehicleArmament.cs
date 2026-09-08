using UnityEngine;
namespace AfterSignal
{
    public sealed class VehicleArmament : MonoBehaviour
    {
        float cooldown;
        public void Tick(ControlFrame input,float dt)
        {
            cooldown-=dt;if(!input.attack||cooldown>0)return;
            var car=GetComponent<CityVehicle>();bool cannon=car.type==CityVehicleType.Tank;
            cooldown=cannon?2.2f:.13f;
            var g=GameDirector.Instance;Vector3 start=transform.position+Vector3.up*(cannon?2.5f:1.8f)+car.Forward*(car.HalfLength+.3f);
            var ray=Camera.main.ViewportPointToRay(new Vector3(.5f,.5f));var aim=Ballistics.AimPoint(ray,transform);var dir=(aim-start).normalized;
            Vector3 end=start+dir*1400;
            if(Ballistics.Cast(start,dir,1400,transform,out var hit))
            {
                end=hit.point;
                var body=hit.collider.GetComponentInParent<WorldActor>();if(body)body.Damage(cannon?160:24,dir*8);
                var target=hit.collider.GetComponentInParent<CityVehicle>();if(target)target.Damage(cannon?125:20,end);
            }
            SignalEffects.Beam(start,end,SignalEffects.Gold,cannon?.16f:.055f,.1f);
            if(cannon)VehicleExplosion.Create(end,2.5f);
            g.Audio.PlayGun(cannon?GunshotKind.Shotgun:GunshotKind.Automatic,start);
            WantedSystem.Report(cannon?14:2,start);
        }
    }
}
