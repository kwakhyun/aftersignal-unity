using UnityEngine;
namespace AfterSignal
{
    public sealed partial class VehicleArmament : MonoBehaviour
    {
        float cooldown, secondaryCooldown, reload, lockTime, searchClock;
        CityVehicle car; Transform turret,elevation,muzzle; Quaternion turretRest,barrelRest;
        int wing,reserve=24;
        public int Shells{get;private set;}=32;
        public int Missiles{get;private set;}=8;
        public int Shots{get;private set;}
        public int MissilesFired{get;private set;}
        public Transform LockedTarget{get;private set;}
        public Vector3 Aim{get;private set;}
        public float ReloadRemaining=>Mathf.Max(0,cooldown);
        public bool Aligned{get;private set;}=true;
        public bool ArticulatedTurret=>turret&&elevation&&muzzle;
        void Start()=>ResolveRig();
        void ResolveRig()
        {
            car=GetComponent<CityVehicle>();
            foreach(var t in GetComponentsInChildren<Transform>())
            {if(t.name=="Turret assembly")turret=t;if(t.name=="Cannon elevation")elevation=t;if(t.name=="Muzzle reference")muzzle=t;}
            if(turret)turretRest=turret.localRotation;if(elevation)barrelRest=elevation.localRotation;
        }
        public void Resupply(){Shells=32;Missiles=8;reserve=24;}
        public void Tick(ControlFrame input,float dt)
        {
            if(!car)car=GetComponent<CityVehicle>();if(!car||car.Wrecked||!Camera.main)return;
            cooldown-=dt;secondaryCooldown-=dt;
            var ray=Camera.main.ViewportPointToRay(new Vector3(.5f,.5f));Aim=Ballistics.AimPoint(ray,transform);
            bool tank=car.type==CityVehicleType.Tank;
            // VehicleDetails creates the FBX hierarchy in Start; do not depend on component Start order.
            if(tank&&!turret)ResolveRig();
            if(tank)AimTurret(dt);else UpdateLock(ray,dt);
            if(reload>0){reload-=dt;if(reload<=0){int n=Mathf.Min(8-Missiles,reserve);Missiles+=n;reserve-=n;}}
            if(input.reload&&!tank&&reload<=0&&Missiles<8&&reserve>0)reload=4.5f;
            if(input.attack&&cooldown<=0&&(!tank||Shells>0&&Aligned))
            {
                cooldown=tank?2.6f:.105f;Shots++;
                var start=tank&&muzzle?muzzle.position:transform.position+Vector3.up*1.5f+car.Forward*(tank?6.5f:3.9f);
                var dir=(Aim-start).normalized;
                if(tank){Shells--;VehicleMissile.Launch(this,start,Aim,null,true);GameDirector.Instance.Audio.Play("cannon",start,.48f,3);GameDirector.Instance.CameraRig.Impact(-dir,.16f);}
                else Shoot(start,dir,23);
                WantedSystem.Report(tank?14:2,start);
            }
            if(input.secondaryFire&&secondaryCooldown<=0)
            {
                if(tank){secondaryCooldown=.095f;var start=transform.position+Vector3.up*2.8f+(Aim-transform.position).normalized*1.5f;Shoot(start,(Aim-start).normalized,15);}
                else if(Missiles>0&&reload<=0)
                {secondaryCooldown=.75f;Missiles--;MissilesFired++;wing=1-wing;var start=transform.TransformPoint(new Vector3(.9f,1.12f,wing==0?-2.35f:2.35f));VehicleMissile.Launch(this,start,Aim,lockTime>.85f?LockedTarget:null,false);GameDirector.Instance.Audio.Play("turbine",start,.32f,3);WantedSystem.Report(14,start);}
            }
        }
    }
}
