using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CameraRig
    {
        bool cockpit;
        CityVehicle viewedVehicle;
        float lastVehicleYaw;
        public bool FirstPersonVehicle => cockpit && Driving;
        public void ToggleVehicleView()
        {
            if(!Driving)return;
            cockpit=!cockpit;velocity=Vector3.zero;impulse=Vector3.zero;
            orbitPitch=cockpit?0:15;
            orbitYaw=Quaternion.LookRotation(UrbanSimulation.Instance.Current.Forward).eulerAngles.y;
            Snap();director.Toast(cockpit?"조종석 1인칭 · C 외부 시점 / 마우스 둘러보기":"탈것 3인칭 · C 조종석 시점");
        }
        void ReadVehicleView(ControlFrame input)
        {
            var car=Driving?UrbanSimulation.Instance.Current:null;
            if(car!=viewedVehicle)
            {
                viewedVehicle=car;velocity=Vector3.zero;
                if(car){lastVehicleYaw=car.transform.eulerAngles.y;orbitYaw=Quaternion.LookRotation(car.Forward).eulerAngles.y;orbitPitch=cockpit?0:15;}
            }
            if(car)
            {
                float yaw=car.transform.eulerAngles.y;
                if(cockpit&&car.type!=CityVehicleType.Tank)orbitYaw+=Mathf.DeltaAngle(lastVehicleYaw,yaw);
                lastVehicleYaw=yaw;
                if(input.vehicleView)ToggleVehicleView();
            }
        }
        Vector3 VehicleFocus()
        {
            var car=UrbanSimulation.Instance.Current;
            bool armed=car.GetComponent<VehicleArmament>();
            float height=armed?(car.type==CityVehicleType.Tank?4.5f:6.4f):car.IsAircraft?4.5f:car.IsWatercraft?4:2;
            return car.transform.position+Vector3.up*height+Quaternion.Euler(0,orbitYaw,0)*Vector3.right*(armed?2.7f:1.25f);
        }
        public Vector3 CockpitPosition(CityVehicle car)
        {
            int seat=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car?UrbanSimulation.Instance.SeatIndex:0;
            var eye=VehicleSeats.Local(car,seat)+Vector3.up*(car.IsSpecial?.82f:.56f)+Vector3.right*.15f;
            if(car.type==CityVehicleType.Tank)eye=new Vector3(.1f,3.04f,0);
            if(car.type==CityVehicleType.CombatHelicopter&&seat==0)eye=new Vector3(2.60f,2.24f,-.48f);
            return car.transform.TransformPoint(eye);
        }
        void SetCockpit()
        {
            var car=UrbanSimulation.Instance.Current;
            basePosition=CockpitPosition(car);transform.position=basePosition;
            transform.rotation=Quaternion.Euler(orbitPitch,orbitYaw,0);
            var cam=GetComponent<Camera>();cam.nearClipPlane=.04f;
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,car.type==CityVehicleType.Tank?52:72+Mathf.Clamp(Mathf.Abs(car.speed)*.08f,0,8),1-Mathf.Exp(-Time.deltaTime*8));
        }
    }
}
