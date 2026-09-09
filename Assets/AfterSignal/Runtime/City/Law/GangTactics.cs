using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class GangTactics
    {
        readonly GangMember gang;readonly int weapon;readonly List<WorldActor> neighbors=new();CityVehicle escape;float scan;bool fleeing;
        public bool Heavy=>weapon<2;
        public GangTactics(GangMember member,int index){gang=member;weapon=Mathf.Abs(index)%7;if(weapon==0)member.gameObject.AddComponent<GangLauncherArt>();}
        public WorldActor Civilian()
        {
            ActorSpatialIndex.Nearby(gang.transform.position,40,neighbors);WorldActor best=null;float distance=40*40;
            foreach(var a in neighbors){if(!a||!a.Alive||a.police||a.military||a.gang||a.terrorist||a.monster||a.helicopter||a.environmental||a.protectedResident)continue;float d=(a.Center-gang.Body.Center).sqrMagnitude;if(d<distance&&FactionCombat.Visible(gang.Body.Center,a.Center,40)){distance=d;best=a;}}return best;
        }
        public bool SpecialAttack(Vector3 from,Vector3 to)
        {
            if(!Heavy||(to-from).magnitude<10)return false;GangWarhead.Launch(gang.Body,from,to,weapon==1);NpcSpeech.Say(gang,weapon==0?"로켓 쏜다! 비켜!":"폭탄 받아라!",3,4);return true;
        }
        public bool TickEscape(CharacterController motor,float dt)
        {
            if(gang.Body.Downed)return false;
            if(!fleeing&&gang.Body.health<gang.Body.MaxHealth*.32f)fleeing=true;if(!fleeing)return false;
            if((!escape||escape.Wrecked||escape.owned)&&Time.time>scan)
            {
                scan=Time.time+4;escape=null;float nearest=80*80;if(UrbanSimulation.Instance)foreach(var car in UrbanSimulation.Instance.Cars){if(!car||car.Wrecked||car.owned||car.occupied||car.IsSpecial||car.type==CityVehicleType.Tank||car.GetComponent<TacticalTransport>()||car.GetComponent<EmergencyAmbulance>()||car.GetComponent<FireEngine>())continue;float d=(car.transform.position-gang.transform.position).sqrMagnitude;if(d<nearest){nearest=d;escape=car;}}
                if(escape){gang.GoTo(escape.transform.position);NpcSpeech.Say(gang,"안 되겠다! 차로 빠져!",4,6);}
            }
            if(!escape)return false;var dlt=escape.transform.position-gang.transform.position;dlt.y=0;
            if(dlt.magnitude>4){motor.Move((dlt.normalized*4+Vector3.down*2)*dt);return true;}
            GangGetaway.Begin(escape,gang);return true;
        }
    }
    public sealed class GangWarhead:MonoBehaviour
    {
        WorldActor owner;Vector3 velocity;bool grenade;float fuse;CityVehicle direct;
        public static void Launch(WorldActor who,Vector3 from,Vector3 to,bool bomb)
        {
            var go=new GameObject(bomb?"Gang thrown bomb":"Gang RPG projectile");go.transform.position=from;var p=go.AddComponent<GangWarhead>();p.owner=who;p.grenade=bomb;p.fuse=bomb?2.6f:5;p.velocity=bomb?(to-from)/2+Vector3.up*13:(to-from).normalized*65;
            WorldGeometry.Part(go.transform,"Warhead",Vector3.zero,bomb?Vector3.one*.2f:new Vector3(.18f,.18f,.8f),"DarkMetal",bomb?PrimitiveType.Sphere:PrimitiveType.Capsule);
        }
        void Update()
        {
            if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;float dt=Mathf.Min(.04f,Time.deltaTime);fuse-=dt;if(grenade)velocity+=Vector3.down*13*dt;var delta=velocity*dt;
            if(Ballistics.Cast(transform.position,delta.normalized,delta.magnitude+.15f,owner?owner.transform:null,out var hit)){transform.position=hit.point+hit.normal*.15f;if(grenade)velocity=Vector3.Reflect(velocity,hit.normal)*.35f;else{direct=hit.collider.GetComponentInParent<CityVehicle>();Explode();return;}}else transform.position+=delta;
            if(!grenade){transform.rotation=Quaternion.LookRotation(velocity);SignalEffects.Beam(transform.position,transform.position-delta,SignalEffects.Gold,.05f,.18f);}if(fuse<=0)Explode();
        }
        void Explode(){VehicleExplosion.Create(transform.position,grenade?2.5f:4);BlastDamage.Create(transform.position,grenade?8:11,grenade?85:150,owner?owner:TrafficDamageSource.Environment,null,grenade?BlastPayload.Conventional:BlastPayload.Missile,direct);GameDirector.Instance?.Audio.Play("urban_explosion",transform.position,.5f,3);Destroy(gameObject);}
    }
    public sealed class GangGetaway:MonoBehaviour
    {
        public WorldActor Driver{get;private set;}CityVehicle car;Vector3 goal;float next,age;readonly ResponseDrive drive=new();readonly List<TacticalTransport> pursuers=new();
        public static void Begin(CityVehicle car,GangMember gang)
        {
            var run=car.gameObject.AddComponent<GangGetaway>();run.car=car;run.Driver=gang.Body;car.occupied=true;car.traffic=false;gang.enabled=false;foreach(var c in gang.GetComponents<Collider>())c.enabled=false;foreach(var r in gang.GetComponentsInChildren<Renderer>())r.enabled=false;
            var crime=gang.GetComponent<GangCrime>();if(crime)crime.enabled=false;gang.transform.SetParent(car.transform);gang.transform.localPosition=Vector3.up;car.GetComponent<VehicleCabin>()?.SetCrew("GangCrimson",0);CityEventGate.Enroll(car);SecurityResponse.Request(gang.Body,false);
            var ground=gang.GetComponent<NpcGroundSupport>();if(ground)ground.enabled=false;
            if(ResponseDispatch.TryOrigin(car.transform.position,false,false,0,out var origin)){var police=TacticalTransport.Create(WantedSystem.Instance,origin,2,2);police.AssignIncident(gang.Body);run.pursuers.Add(police);}
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.06f,Time.deltaTime);age+=dt;
            if(!Driver||!Driver.Alive||car.Wrecked||car.owned){if(Driver){Driver.transform.SetParent(null);Driver.transform.position=transform.position-car.Forward*5;foreach(var r in Driver.GetComponentsInChildren<Renderer>())r.enabled=true;foreach(var c in Driver.GetComponents<Collider>())c.enabled=true;var gang=Driver.GetComponent<GangMember>();if(gang)gang.enabled=true;var ground=Driver.GetComponent<NpcGroundSupport>();if(ground)ground.enabled=true;}Destroy(this);return;}
            if(Time.time>next){next=Time.time+8;goal=CityRoadNetwork.Sidewalk(transform.position+car.Forward*300+Vector3.forward*80);}drive.Drive(car,goal,dt,8);
            foreach(var p in pursuers)if(p&&p.GetComponent<CityVehicle>()){var chase=p.GetComponent<CityVehicle>();if((chase.transform.position-transform.position).sqrMagnitude>25*25)p.AssignIncident(Driver);}
            if(age>120&&(g.Player.transform.position-transform.position).sqrMagnitude>600*600)Destroy(gameObject);
        }
    }
    public sealed class GangLauncherArt:MonoBehaviour
    {
        Transform tube;GangMember member;
        void Start(){member=GetComponent<GangMember>();tube=new GameObject("Gang shoulder rocket launcher").transform;tube.SetParent(transform,false);var barrel=WorldGeometry.Part(tube,"Launcher tube",Vector3.zero,new Vector3(.22f,.75f,.22f),"DarkMetal",PrimitiveType.Cylinder);barrel.transform.localRotation=Quaternion.Euler(90,0,0);WorldGeometry.Part(tube,"Rocket cone",new(0,0,.72f),new Vector3(.3f,.3f,.45f),"Metal",PrimitiveType.Capsule);}
        void LateUpdate(){if(!tube||!member)return;tube.gameObject.SetActive(!member.CampaignUnit&&member.Body.Alive&&!GetComponentInParent<GangGetaway>());var target=member.Target?member.Target.Center:GameDirector.Instance.Player.Shoulder;var d=target-member.Body.Center;if(d.sqrMagnitude<.1f)return;tube.position=member.Body.Center+Vector3.up*.4f+d.normalized*.25f;tube.rotation=Quaternion.LookRotation(d);}
    }
}

