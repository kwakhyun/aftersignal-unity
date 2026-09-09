using UnityEngine;
namespace AfterSignal
{
    public sealed class MilitaryVehicleAI:MonoBehaviour
    {
        CityVehicle car;MilitaryResponse response;WorldActor gunner;float clock,shot,missile,disembark;int deployed;readonly ResponseDrive route=new();
        public void Initialize(CityVehicle c,MilitaryResponse r)
        {
            car=c;response=r;var source=new GameObject("Military vehicle gunner");source.transform.SetParent(transform,false);gunner=source.AddComponent<WorldActor>();gunner.military=true;gunner.helicopter=true;gunner.enabled=false;
        }
        void Start(){if(car.type==CityVehicleType.Truck)SecurityVehicleArt.Install(car,false);car.GetComponent<VehicleCabin>()?.SetPassengers(car.type==CityVehicleType.Truck?6:0);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!car||car.Wrecked||!response||!response.Active)return;
            if(!car.occupied)return;
            if(UrbanSimulation.Instance.Current==car){enabled=false;return;}
            float dt=Mathf.Min(.05f,Time.deltaTime);clock+=dt;shot-=dt;missile-=dt;disembark-=dt;
            var target=response.Target;WorldActor monster=null;
            if(response.Incident||IncidentCommand.Emergency)foreach(var actor in WorldActor.All)if(actor&&actor.monster&&actor.Alive&&(!monster||(actor.Center-transform.position).sqrMagnitude<(monster.Center-transform.position).sqrMagnitude))monster=actor;
            if(monster)target=monster.transform.position;
            var delta=target-transform.position;
            if(car.type==CityVehicleType.Bomber){var bay=car.GetComponent<BomberBay>();if(bay)bay.FlyRun(target,dt);return;}
            if(car.IsAircraft)
            {
                var orbit=target+new Vector3(Mathf.Cos(clock*.13f)*120,car.type==CityVehicleType.Fighter?140:55,Mathf.Sin(clock*.13f)*120);
                var d=orbit-transform.position;var input=ControlFrame.Empty;input.move=new Vector2(Mathf.Clamp(Vector3.SignedAngle(car.Forward,Vector3.ProjectOnPlane(d,Vector3.up),Vector3.up)/28,-1,1),car.type==CityVehicleType.Fighter?.25f:.7f);input.vertical=Mathf.Clamp(d.y/15,-1,1);car.Drive(input,dt);
            }
            else route.Drive(car,target,dt,car.type==CityVehicleType.Truck?35:60);
            // Arrival routes remain active; distant response vehicles do not deploy troops or fire.
            if(!LocalSimulation.Combat(transform.position))return;
            if(car.type==CityVehicleType.Truck)
            {
                if(Vector3.ProjectOnPlane(delta,Vector3.up).magnitude<40&&deployed<6&&disembark<=0)
                {car.speed=0;GetComponent<SecurityVehicleArt>()?.OpenRear();response.Deploy(transform.position-car.Forward*(car.HalfLength+2)+transform.forward*(deployed%2==0?-1:1),deployed);deployed++;disembark=.8f;car.GetComponent<VehicleCabin>()?.SetPassengers(6-deployed);}return;
            }
            if((response.Incident||IncidentCommand.Emergency)&&!monster)return;
            Vector3 aim=monster?monster.Center:UrbanSimulation.Instance.Current?UrbanSimulation.Instance.Current.transform.position+Vector3.up:g.Player.Shoulder;
            var mount=transform.position+Vector3.up*(car.type==CityVehicleType.Tank?3.2f:-1)+car.Forward*(car.HalfLength+.8f);
            var armament=car.GetComponent<VehicleArmament>();if(car.type==CityVehicleType.Tank&&armament)mount=armament.AimForAI(aim,dt);
            if((aim-mount).sqrMagnitude>620*620||!BlastDamage.Exposed(mount,aim,monster?monster.transform:g.Player.transform,car))return;
            if(missile<=0&&(car.type!=CityVehicleType.Tank||!armament||armament.Aligned))
            {
                missile=car.type==CityVehicleType.Tank?7:11;
                VehicleMissile.LaunchFaction(car,gunner,mount,aim,monster?monster.transform:UrbanSimulation.Instance.Current?UrbanSimulation.Instance.Current.transform:null,car.type==CityVehicleType.Tank);
                if(response.Incident)response.Incident.MilitaryShots++;g.Audio.Play("cannon",mount,.3f,2);
            }
            if(shot<=0&&car.type!=CityVehicleType.Tank)
            {shot=.32f;FactionCombat.Fire(gunner,mount,aim,620,monster?45:16,SignalEffects.Gold,!response.Incident&&!IncidentCommand.Emergency,transform);g.Audio.PlayGun(GunshotKind.Automatic,mount,.8f);}
        }
    }
}
