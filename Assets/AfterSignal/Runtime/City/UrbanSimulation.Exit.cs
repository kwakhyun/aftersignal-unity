using UnityEngine;
namespace AfterSignal
{
    public sealed partial class UrbanSimulation
    {
        public int Bailouts {get;private set;}
        public bool BailOut()
        {
            if(!Current||!game)return false;
            var car=Current;var momentum=car.Forward*car.speed;bool driver=SeatIndex==0;
            // The aircraft fuselage door is safer than the wing tip. Never snap to a distant ground sample.
            Vector3 side=car.transform.forward*(car.HalfWidth+2.3f);
            Vector3 point=car.transform.position+side+Vector3.up*(car.IsAircraft?2.5f:car.IsWatercraft?3:1);
            if(car.GetComponent<AuthoredCraft>())point=VehicleSeats.Door(car)+car.transform.forward*3+Vector3.up*2;
            if(Physics.CheckCapsule(point+Vector3.up*.4f,point+Vector3.up*1.7f,.36f,1,QueryTriggerInteraction.Ignore))point=car.transform.position+Vector3.up*(car.IsAircraft?7:car.IsWatercraft?6:4.5f);
            Current=null;SeatIndex=0;Refueling=false;
            if(driver){car.occupied=false;car.gameObject.AddComponent<UnattendedVehicle>().Initialize(car);}
            game.Player.Respawn(point,false);game.Player.Velocity=Vector3.ClampMagnitude(momentum,58)+side.normalized*4+Vector3.up*4;
            if(playerRenderers!=null)for(int i=0;i<playerRenderers.Length;i++)if(playerRenderers[i])playerRenderers[i].enabled=rendererStates[i];
            if(contact)contact.enabled=true;
            Bailouts++;game.Audio.Play("jump",point,.2f,1);game.Toast(car.IsAircraft?"항공기 탈출 · 로프로 착지 지점을 확보하세요":"이동 중 탈출 · 관성에 주의하세요");SaveCar();return true;
        }
    }
    public sealed class UnattendedVehicle:MonoBehaviour
    {
        CityVehicle car;
        public void Initialize(CityVehicle c){car=c;c.GetComponent<CraftDynamics>()?.CutThrottle();}
        void Update()
        {
            if(!car||car.Wrecked||car.occupied){Destroy(this);return;}
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            var input=ControlFrame.Empty;if(car.IsAircraft)input.vertical=-1;
            car.Drive(input,Mathf.Min(.05f,Time.deltaTime));
            if(!car.IsAircraft&&Mathf.Abs(car.speed)<.01f)Destroy(this);
        }
    }
}
