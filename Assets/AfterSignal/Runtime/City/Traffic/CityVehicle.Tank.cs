using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityVehicle
    {
        void DriveTank(ControlFrame input,float dt)
        {
            Steering=Mathf.MoveTowards(Steering,input.move.x,dt*3.2f);
            GetComponent<VehicleSoundscape>()?.SetThrottle(Mathf.Max(Mathf.Abs(input.move.y),Mathf.Abs(input.move.x)*.5f));
            float target=fuel>0?input.move.y*(input.move.y<0?8:input.boost?23:18):0;
            speed=Mathf.MoveTowards(speed,input.vertical>0?0:target,dt*(input.vertical>0?22:Mathf.Sign(target)!=Mathf.Sign(speed)?14:input.boost?7:5));
            if(fuel>0&&Mathf.Abs(Steering)>.01f)
            {
                var rotation=Quaternion.AngleAxis(Steering*Mathf.Lerp(36,22,Mathf.Abs(speed)/23)*dt,Vector3.up)*transform.rotation;
                bool blocked=false;
                foreach(var col in Physics.OverlapBox(transform.position+Vector3.up*.95f,new Vector3(HalfLength-.15f,.48f,HalfWidth-.1f),rotation,1,QueryTriggerInteraction.Ignore))
                    if(!col.transform.IsChildOf(transform)){blocked=true;break;}
                if(!blocked){transform.rotation=rotation;fuel=Mathf.Max(0,fuel-Mathf.Abs(Steering)*dt*.004f);}
            }
            Advance(Forward*speed*dt,dt,false);
            GetComponent<VehicleArmament>()?.Tick(input,dt);
        }
    }
}
