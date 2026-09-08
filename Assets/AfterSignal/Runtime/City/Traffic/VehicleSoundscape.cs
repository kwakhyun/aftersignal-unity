using UnityEngine;
namespace AfterSignal
{
    public sealed class VehicleSoundscape:MonoBehaviour
    {
        CityVehicle car;AudioSource engine,load;AudioLowPassFilter low;
        float rpm,throttle,previousSpeed;bool wasRunning;
        public string Cue=>engine&&engine.clip?engine.clip.name:"missing";
        public void Initialize(CityVehicle owner,AudioSource source)
        {
            car=owner;engine=source;engine.dopplerLevel=.25f;engine.minDistance=5;engine.maxDistance=car.IsAircraft?230:car.IsWatercraft?95:65;
            low=engine.gameObject.AddComponent<AudioLowPassFilter>();
            var go=new GameObject("Engine load and drivetrain");go.transform.SetParent(transform,false);load=go.AddComponent<AudioSource>();
            load.clip=Resources.Load<AudioClip>("Audio/Transport/"+(car.IsAircraft?"turbine":car.type==CityVehicleType.Tank?"tank":car.IsWatercraft?"ship":car.IsHeavy?"truck":car.type==CityVehicleType.Motorcycle?"motorcycle":"sportscar"));
            load.loop=true;load.spatialBlend=1;load.minDistance=4;load.maxDistance=engine.maxDistance;load.dopplerLevel=.2f;load.volume=0;if(load.clip)load.Play();
        }
        public void SetThrottle(float value){throttle=Mathf.Abs(value);}
        void LateUpdate()
        {
            var g=GameDirector.Instance;if(!car||!g||!engine)return;
            bool active=!car.Wrecked&&car.fuel>0&&(car.occupied||car.traffic);
            bool current=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car;
            bool inside=current&&g.CameraRig.FirstPersonVehicle;
            float master=g.Blocked?0:g.Audio.Volume*g.Audio.SfxVolume;
            float motion=Mathf.Clamp01(Mathf.Abs(car.speed)/(car.IsAircraft?90:car.IsWatercraft?19:car.TopSpeed));
            float acceleration=Mathf.Clamp01(Mathf.Abs(car.speed-previousSpeed)/Mathf.Max(.001f,Time.deltaTime)/8);previousSpeed=car.speed;
            if(active&&!wasRunning&&current&&!car.IsSpecial&&car.type!=CityVehicleType.Tank)g.Audio.Play(car.type==CityVehicleType.Motorcycle?"bike_start":"ignition",transform.position,.24f,1);
            wasRunning=active;
            float gear=car.IsHeavy?motion*3:motion*5;
            float target=car.IsAircraft?.88f+motion*.35f:car.IsWatercraft?.72f+motion*.48f:car.type==CityVehicleType.Tank?.8f+motion*.35f:.78f+(gear-Mathf.Floor(gear))*.32f+motion*.48f;
            rpm=Mathf.MoveTowards(rpm,active?target:0,Time.deltaTime*(car.IsAircraft?.7f:2.2f));
            engine.pitch=Mathf.Max(.1f,rpm);engine.volume=(active?(current?.14f:.055f)+motion*(current?.11f:.08f):0)*master;
            // A freighter's chase camera can be hundreds of metres from its centre. Retain
            // an onboard mix for the active craft while other traffic remains fully spatial.
            engine.priority=current?35:185;engine.spatialBlend=current?(inside?.15f:.4f):1;low.cutoffFrequency=inside?1800:16000;
            load.spatialBlend=current?.55f:1;
            load.pitch=Mathf.Max(.2f,rpm*1.05f);load.volume=(active?motion*.055f+Mathf.Max(throttle,acceleration)*.035f:0)*master;
            load.mute=engine.mute=g.Blocked;throttle=Mathf.MoveTowards(throttle,0,Time.deltaTime*3);
        }
    }
}
