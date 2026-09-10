using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // This retains the real enlisted actor, rather than synthesizing a separate vehicle gunner.
    public sealed class GarrisonPassenger:MonoBehaviour{}
    public sealed class GarrisonVehicleDriver:MonoBehaviour
    {
        public CityVehicle Car{get;private set;}public GarrisonSupport Soldier{get;private set;}public int Shots{get;private set;}
        MilitaryBaseOperations.Base site;Vector3 home;Quaternion homeRotation;float clock,shot,missile;readonly ResponseDrive route=new();
        bool quitting;float safetyAt;bool ordnanceClear;readonly List<WorldActor> safetyNeighbors=new();
        readonly List<Renderer> renderers=new();readonly List<Collider> colliders=new();readonly List<Behaviour> routines=new();Transform parent;
        public static bool Board(CityVehicle car,GarrisonSupport duty,MilitaryBaseOperations.Base site)
        {
            if(!car||!duty||!duty.Body||!duty.Body.Alive||car.Wrecked||car.occupied||car.owned||car.GetComponent<GarrisonVehicleDriver>())return false;
            var claim=car.GetComponent<VehicleClaim>();if(claim&&claim.Soldier!=duty)return false;
            var d=car.gameObject.AddComponent<GarrisonVehicleDriver>();d.Car=car;d.Soldier=duty;d.site=site;d.home=car.transform.position;d.homeRotation=car.transform.rotation;d.parent=duty.transform.parent;
            duty.gameObject.AddComponent<GarrisonPassenger>();foreach(var r in duty.GetComponentsInChildren<Renderer>())if(r.enabled){d.renderers.Add(r);r.enabled=false;}
            foreach(var c in duty.GetComponentsInChildren<Collider>())if(c.enabled){d.colliders.Add(c);c.enabled=false;}
            foreach(var b in duty.GetComponents<Behaviour>())if(b.enabled&&(b is NpcBody||b is NpcGroundSupport||b is CityNpc||b is DirectionalPerson||b is RegionalUniform||b is PixelActor||b is PoliceOfficer||b is ArmyResponder||b is VenueActor||b is CivicRoutine||b is FacilityCitizen)){d.routines.Add(b);b.enabled=false;}
            duty.transform.SetParent(car.transform,true);duty.transform.localPosition=VehicleSeats.Local(car,0);car.occupied=true;car.traffic=false;car.GetComponent<VehicleCabin>()?.SetCrew(site.Uniform,0);
            if(claim)Destroy(claim);NpcSpeech.Say(duty,"장비 탑승 완료. 기지 방어에 합류한다!",3,5);return true;
        }
        void LateUpdate()
        {if(!Soldier)return;foreach(var r in renderers)if(r)r.enabled=false;Soldier.transform.localPosition=VehicleSeats.Local(Car,0);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Car)return;
            if(!Soldier||!Soldier.Body.Alive||Car.Wrecked){Dismount(Car.Wrecked);return;}
            if(UrbanSimulation.Instance.Current==Car||Car.owned){Dismount(false);return;}
            float dt=Mathf.Min(Time.deltaTime,.05f);clock+=dt;shot-=dt;missile-=dt;
            bool combat=site.Active&&(site.Threat&&site.Threat.Alive||site.PlayerTarget);var target=combat?site.Target:home;
            if(Car.IsAircraft)
            {
                var orbit=target+new Vector3(Mathf.Cos(clock*.12f+GetInstanceID())*95,Car.type==CityVehicleType.Fighter||Car.type==CityVehicleType.Bomber?110:42,Mathf.Sin(clock*.12f+GetInstanceID())*95);
                float range=Vector3.ProjectOnPlane(home-transform.position,Vector3.up).magnitude;
                if(!combat)orbit=range>12?home+Vector3.up*35:home;
                if(combat&&Car.type==CityVehicleType.CombatHelicopter&&transform.position.y<home.y+25)orbit=transform.position+Vector3.up*35;
                var d=orbit-transform.position;var input=ControlFrame.Empty;
                input.move=new Vector2(Mathf.Clamp(Vector3.SignedAngle(Car.Forward,Vector3.ProjectOnPlane(d,Vector3.up),Vector3.up)/28,-1,1),Vector3.ProjectOnPlane(d,Vector3.up).magnitude<10?0:Car.type==CityVehicleType.Fighter?.28f:.6f);input.vertical=Mathf.Clamp(d.y/10,-1,1);Car.Drive(input,dt);
                if(!combat&&range<13&&Mathf.Abs(transform.position.y-home.y)<2.5f){Car.speed=0;Dismount(false);return;}
            }
            else {route.Drive(Car,target,dt,combat?Car.type==CityVehicleType.Tank?32:25:3);if(!combat&&Vector3.ProjectOnPlane(transform.position-home,Vector3.up).sqrMagnitude<20){Car.speed=0;Dismount(false);return;}}
            if(!combat||!LocalSimulation.Combat(transform.position))return;
            var actor=site.Threat;bool hostilePlayer=!actor&&site.PlayerTarget;var aim=target;
            var gun=Car.GetComponent<MilitaryGunTruck>();gun?.AimAt(aim,dt);
            var armament=Car.GetComponent<VehicleArmament>();var muzzle=gun?gun.Muzzle:Car.type==CityVehicleType.Tank&&armament?armament.AimForAI(aim,dt):transform.position+Vector3.up*1.6f+Car.Forward*(Car.HalfLength+.8f);
            if((aim-muzzle).sqrMagnitude>620*620||!BlastDamage.Exposed(muzzle,aim,actor?actor.transform:g.Player.transform,Car))return;
            if(!gun&&Car.type!=CityVehicleType.Bomber&&missile<=0&&OrdnanceSafe(aim)&&(Car.type!=CityVehicleType.Tank||!armament||armament.Aligned))
            {missile=Car.type==CityVehicleType.Tank?7:10;VehicleMissile.LaunchFaction(Car,Soldier.Body,muzzle,aim,actor?actor.transform:g.Player.transform,Car.type==CityVehicleType.Tank);Shots++;g.Audio.Play("cannon",muzzle,.3f,2);}
            if(Car.type==CityVehicleType.Bomber){var bay=Car.GetComponent<BomberBay>();if(bay)bay.FlyRun(aim,dt);return;}
            if(shot<=0){shot=gun?.15f:.3f;FactionCombat.Fire(Soldier.Body,muzzle,aim,620,actor&&actor.monster?45:18,SignalEffects.Gold,hostilePlayer,transform);Shots++;g.Audio.PlayGun(GunshotKind.Automatic,muzzle,.7f);}
        }
        public WorldActor Dismount(bool destroyed)
        {
            var duty=Soldier;if(!Car||!GameDirector.Instance||!UrbanSimulation.Instance){Soldier=null;return null;}if(!duty){Destroy(this);return null;}Soldier=null;var body=duty.Body;
            duty.transform.SetParent(parent,true);var desired=VehicleSeats.Door(Car);if(CityGangWar.FindGround(desired,out var ground))desired=ground;duty.transform.position=desired;
            foreach(var c in colliders)if(c)c.enabled=true;foreach(var b in routines)if(b)b.enabled=true;foreach(var r in renderers)if(r)r.enabled=true;
            var marker=duty.GetComponent<GarrisonPassenger>();if(marker)Destroy(marker);
            if(UrbanSimulation.Instance.Current!=Car){Car.occupied=false;Car.speed=0;}
            if(destroyed&&body.Alive)body.Damage(10000,Vector3.up*2,body);Destroy(this);return body;
        }
        bool OrdnanceSafe(Vector3 aim)
        {
            if(Time.time<safetyAt)return ordnanceClear;safetyAt=Time.time+.35f;ordnanceClear=true;ActorSpatialIndex.Nearby(aim,17,safetyNeighbors);
            foreach(var a in safetyNeighbors)if(a&&a.Alive&&(a.military||a.police)&&a!=Soldier.Body&&(a.Center-aim).sqrMagnitude<17*17){ordnanceClear=false;break;}return ordnanceClear;
        }
        void OnApplicationQuit()=>quitting=true;
        void OnDestroy(){if(!quitting&&gameObject.scene.isLoaded&&GameDirector.Instance&&UrbanSimulation.Instance&&Car&&Soldier)Dismount(false);}
    }
}
