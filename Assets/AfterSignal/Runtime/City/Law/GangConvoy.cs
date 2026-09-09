using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class GangConvoy:MonoBehaviour
    {
        public static int Active{get;private set;}public int Released{get;private set;}
        CityVehicle car;WorldActor victim;Vector3 objective;float age,scan,shot,release;bool reported;WorldActor gunner;readonly ResponseDrive drive=new();readonly List<GangMember> crew=new();
        public static GangConvoy Dispatch(Vector3 near)
        {
            if(CityEventGate.Busy||CampaignBattle.Active)return null;
            if(!ResponseDispatch.TryOrigin(near,false,false,Active+8,out var at))return null;
            var c=UrbanSimulation.Instance.Spawn(at,false,0);c.name="혈선 연합 / 무장 습격 차량";c.occupied=true;c.health=c.MaxHealth;
            var convoy=c.gameObject.AddComponent<GangConvoy>();convoy.car=c;convoy.objective=near;CityEventGate.Begin(convoy,CityEventKind.Gang);CityEventGate.Enroll(c);
            var go=new GameObject("Convoy roof gunner");go.transform.SetParent(c.transform,false);convoy.gunner=go.AddComponent<WorldActor>();convoy.gunner.gang=true;convoy.gunner.helicopter=true;convoy.gunner.enabled=false;
            SecurityVehicleArt.Install(c,true);return convoy;
        }
        void OnEnable()=>Active++;
        void OnDisable()=>Active--;
        public WorldActor EjectDriver(){Released=Mathf.Max(Released,1);return VehicleOccupant.Create(car,"GangCrimson",0,false,GameDirector.Instance.Player.transform.position);}
        public void PrepareEvacuation(){Released=4;if(gunner)gunner.health=0;enabled=false;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!car)return;float dt=Mathf.Min(.05f,Time.deltaTime);age+=dt;scan-=dt;shot-=dt;release-=dt;
            if(age<1)car.GetComponent<VehicleCabin>()?.SetCrew("GangCrimson",3);
            if(car.Wrecked){PrepareEvacuation();return;}
            if(car.owned||UrbanSimulation.Instance.Current==car){while(Released<4)Disembark();if(gunner)gunner.health=0;enabled=false;return;}
            if(scan<=0)
            {
                scan=1;victim=FactionCombat.NearestOpponent(gunner,65);
                if(!victim)foreach(var a in WorldActor.All)if(a&&a.Alive&&!a.Downed&&!a.gang&&!a.monster&&!a.helicopter&&!a.environmental&&Vector3.Distance(a.transform.position,transform.position)<55){victim=a;break;}
                if(victim)objective=victim.transform.position;
            }
            if(Released==0)drive.Drive(car,objective,dt,20);else car.speed=0;
            bool close=Vector3.ProjectOnPlane(objective-transform.position,Vector3.up).magnitude<28;
            if(close&&Released<4&&release<=0){Disembark();release=.75f;}
            if(car.occupied&&Released<4&&gunner&&gunner.Alive&&victim&&shot<=0&&FactionCombat.Visible(transform.position+Vector3.up*2.4f,victim.Center,65))
            {
                shot=.38f;FactionCombat.Fire(gunner,transform.position+Vector3.up*2.4f,victim.Center,70,12,SignalEffects.Red,false,transform);g.Audio.PlayGun(GunshotKind.Rifle,transform.position,.5f);
                if(!reported){reported=true;SecurityResponse.Request(gunner,false);CitySafety.Shock(victim.transform.position);}
            }
            if(age>180&&(g.Player.transform.position-transform.position).sqrMagnitude>450*450){foreach(var m in crew)if(m&&!m.Body.Downed)Destroy(m.gameObject);Destroy(gameObject);}
        }
        void Disembark()
        {
            Vector3 p=transform.position-car.Forward*(car.HalfLength+2)+transform.forward*(Released%2==0?-1.2f:1.2f);Released++;
            if(CityGangWar.FindGround(p,out var at)){var m=GangMember.Create(at,0,Released);m.gameObject.AddComponent<GangCrime>();crew.Add(m);CityEventGate.Enroll(m.Body);SecurityResponse.Request(m.Body,false);NpcSpeech.Say(m,"장비 챙겨! 거리부터 장악한다!",3);}
            car.GetComponent<VehicleCabin>()?.SetPassengers(Mathf.Max(0,4-Released));if(Released>=4){car.occupied=false;if(gunner)gunner.health=0;}
        }
    }
}
