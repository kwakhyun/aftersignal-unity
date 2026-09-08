using UnityEngine;
namespace AfterSignal
{
    public sealed partial class VehicleArmament
    {
        void AimTurret(float dt)
        {
            if(!turret)return;
            var local=transform.InverseTransformDirection(Aim-turret.position);
            float yaw=Mathf.Atan2(-local.z,local.x)*Mathf.Rad2Deg;
            var axis=turret.parent.InverseTransformDirection(Vector3.up);
            var target=Quaternion.AngleAxis(yaw,axis)*turretRest;
            turret.localRotation=Quaternion.RotateTowards(turret.localRotation,target,42*dt);
            Aligned=Quaternion.Angle(turret.localRotation,target)<3.5f;
            float requestedPitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            float pitch=Mathf.Clamp(requestedPitch,-9,28);Aligned&=requestedPitch>=-9&&requestedPitch<=28;
            if(elevation)
            {
                var right=Vector3.Cross(Vector3.ProjectOnPlane(Aim-turret.position,Vector3.up).normalized,Vector3.up);
                var desired=Quaternion.AngleAxis(pitch,elevation.parent.InverseTransformDirection(right))*barrelRest;
                elevation.localRotation=Quaternion.RotateTowards(elevation.localRotation,desired,24*dt);
                Aligned&=Quaternion.Angle(elevation.localRotation,desired)<3.5f;
            }
        }
        void UpdateLock(Ray ray,float dt)
        {
            searchClock-=dt;
            if(searchClock<=0)
            {
                searchClock=.15f;Transform candidate=null;float score=.997f;
                if(UrbanSimulation.Instance)foreach(var vehicle in UrbanSimulation.Instance.Cars)
                {
                    if(!vehicle||vehicle==car||vehicle.Wrecked)continue;
                    var to=vehicle.transform.position+Vector3.up*1.5f-ray.origin;float dot=Vector3.Dot(to.normalized,ray.direction);
                    if(to.magnitude<1400&&dot>score)
                    {
                        if(Ballistics.Cast(ray.origin,to.normalized,to.magnitude+.5f,transform,out var h)&&h.collider.GetComponentInParent<CityVehicle>()!=vehicle)continue;
                        candidate=vehicle.transform;score=dot;
                    }
                }
                if(candidate!=LockedTarget){LockedTarget=candidate;lockTime=0;}
            }
            lockTime=LockedTarget?lockTime+dt:0;
        }
        void Shoot(Vector3 start,Vector3 direction,float damage)
        {
            Vector3 end=start+direction*1400;
            if(Ballistics.Cast(start,direction,1400,transform,out var hit))
            {
                end=hit.point;var actor=hit.collider.GetComponentInParent<WorldActor>();if(actor)actor.Damage(damage,direction*6);
                var vehicle=hit.collider.GetComponentInParent<CityVehicle>();if(vehicle)vehicle.Damage(damage*.9f,end);
            }
            SignalEffects.Beam(start,end,SignalEffects.Gold,.055f,.08f);GameDirector.Instance.Audio.PlayGun(GunshotKind.Automatic,start,.8f);
        }
        void OnGUI()
        {
            var g=GameDirector.Instance;if(!car||!g||g.Blocked||!UrbanSimulation.Instance||UrbanSimulation.Instance.Current!=car||UrbanSimulation.Instance.SeatIndex!=0)return;
            var style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NotoSansKR"),fontSize=Mathf.RoundToInt(Screen.height/60f),normal={textColor=new Color(.65f,1,.87f)}};
            string text=car.type==CityVehicleType.Tank?$"120 mm  {Shells}  ·  {(cooldown>0?"장전 "+cooldown.ToString("0.0"):Aligned?"발사 준비":"포탑 조준 중")}\n좌클릭 주포 · 우클릭 기관총 · C 시점":$"기관포 · 미사일 {Missiles} / 예비 {reserve}\n{(reload>0?"재장전 "+reload.ToString("0.0"):LockedTarget?(lockTime>.85f?"표적 고정":"표적 추적 중"):"비유도 발사 가능")} · 우클릭 미사일 · R 장전 · C 시점";
            GUI.Label(new Rect(25,Screen.height*.62f,440,85),text,style);
        }
    }
}
