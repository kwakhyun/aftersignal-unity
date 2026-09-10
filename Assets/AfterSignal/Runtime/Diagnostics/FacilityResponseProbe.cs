using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal
{
    public sealed class FacilityResponseProbe:MonoBehaviour
    {
        [Serializable] sealed class Report {public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();GameDirector game;float began;bool finished;HttpListener listener;
        static bool BaseProbe => Environment.GetCommandLineArgs().Contains("-base-mobilization-probe");
        static bool CrewWitness => Environment.GetCommandLineArgs().Contains("-crew-witness-probe");
        static bool DrivingPhoto => Environment.GetCommandLineArgs().Contains("-readme-driving-photo");
        static string Output => BaseProbe ? "Artifacts/BaseMobilization/Native" : CrewWitness ? "Artifacts/CrewWitness/Native" : DrivingPhoto ? "Artifacts/ReadmeDriving" : "Artifacts/FacilityResponse/Native";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if((!BaseProbe&&!CrewWitness&&!DrivingPhoto&&!Environment.GetCommandLineArgs().Contains("-facility-response-probe"))||FindAnyObjectByType<FacilityResponseProbe>())return;GameDirector.SkipTitle=true;new GameObject("Facility response essentials").AddComponent<FacilityResponseProbe>();}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;Directory.CreateDirectory(Output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string line,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.completed=false;report.errors.Add(line+"\n"+stack);Save();}}
        void Check(bool ok,string text){(ok?report.passed:report.errors).Add(text);Debug.Log("FACILITY RESPONSE "+ok+" / "+text);Save();}
        void Save()=>File.WriteAllText(Path.Combine(Output,"result.json"),JsonUtility.ToJson(report,true));
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);Set(game,"<Title>k__BackingField",false);Set(game,"<Fade>k__BackingField",0f);game.enabled=false;game.CameraRig.enabled=false;
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is PrisonSystem||b is CityChronicle||b is CitySafety||b is WantedSystem||b is CityGangWar||b is GangStrongholds||b is GangCrime||b is GangMember||b is PoliceOfficer||b is ArmyResponder||b is RegionalGuard||b is SeaCombat||b is RiftIncursion||b is CivicTerrorEvents||b is FourCityCampaign||b is SecurityResponse)b.enabled=false;
            foreach(var battle in FindObjectsByType<CampaignBattle>())Destroy(battle.gameObject);if(PrisonSystem.Instance)Set(PrisonSystem.Instance,"remaining",0f);WantedSystem.Clear("");CityEventGate.Reset();Set(game,"<Nearby>k__BackingField",null);Set(game,"<NoticeTimer>k__BackingField",0f);
            if(BaseProbe){yield return BaseChecks();Finish();yield break;}
            if(CrewWitness){yield return CrewWitnessChecks();Finish();yield break;}
            if(DrivingPhoto){yield return DrivingShowcase();yield return BattleShowcase();Finish();yield break;}
            var sim=UrbanSimulation.Instance;sim.enabled=false;
            foreach(int site in new[]{2,1,4})
            {
                var door=UrbanCatalog.Door(site);game.Player.Respawn(door+Vector3.back*22);yield return new WaitForSeconds(3);
                var fleet=FindObjectsByType<FacilityParked>().Where(p=>(p.transform.position-door).sqrMagnitude<60*60&&p.kind==FacilityParking.SiteKind(site)).ToArray();
                Check(fleet.Length>=1&&fleet.All(p=>FacilityParking.Matches(p.GetComponent<CityVehicle>(),p.kind)),UrbanCatalog.Name(site)+" has its dedicated, unoccupied parked fleet / "+fleet.Length);
                if(site==2){game.enabled=true;yield return new WaitForSeconds(1);game.enabled=false;Set(game,"<NoticeTimer>k__BackingField",0f);CameraAt(door+new Vector3(-31,18,-39),door+Vector3.up*3);Capture("fire-station");}
            }
            Check(FacilityParking.Reserved(UrbanCatalog.Center(2)+new Vector3(-12,.02f,-44))==ServiceParkingKind.Fire,"Ambient random parking is excluded from the fire station forecourt");
            var fireSite=UrbanCatalog.Door(2);game.Player.Respawn(fireSite+Vector3.back*20);yield return new WaitForSeconds(3);
            var target=sim.Spawn(fireSite+new Vector3(0,0,-26),false,0);yield return null;
            var fire=CityFireService.Ignite(target.gameObject,target.transform.position+Vector3.up,1);
            yield return new WaitForSeconds(3);var engine=fire?fire.Assigned:null;
            Check(engine&&engine.Car.occupied&&engine.GetComponent<FireEngineArt>()&&VehicleFleet.Persistent(engine.Car),"A real local fire dispatches a visible dedicated engine that survives distant-traffic culling / fire="+(bool)fire+", engine="+(bool)engine+", service="+CityFireService.Instance.enabled);
            if(engine){yield return new WaitForSeconds(5);Check(engine.Phase==1&&FindObjectsByType<Firefighter>().Any(f=>(f.transform.position-fireSite).sqrMagnitude<100*100),"Dispatched engine arrives and deploys its hose and rescue crew");}
            if(fire)fire.Suppress(1000);if(engine)Destroy(engine.gameObject);if(target)Destroy(target.gameObject);
            // Isolate the strong impact from ambient traffic; only test scene owns this deck.
            var siteAt=new Vector3(1000,200,-800);var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Motorcycle verification deck";deck.transform.position=siteAt-Vector3.up*.5f;deck.transform.localScale=new Vector3(100,1,100);Physics.SyncTransforms();
            game.Player.Respawn(siteAt+Vector3.back*10);var bike=sim.Spawn(siteAt,false,4);var car=sim.Spawn(siteAt+Vector3.right*4.9f,false,0);yield return null;Physics.SyncTransforms();
            Check(sim.Enter(bike),"Player enters the motorcycle before the collision");bike.speed=35;var input=ControlFrame.Empty;input.move=Vector2.up;sim.Tick(input,.1f);yield return new WaitForSeconds(.35f);
            Check(bike.Tumbling&&!sim.Current&&bike.transform.position.y>siteAt.y+.4f&&game.Player.Controller.enabled,"Strong motorcycle/car collision ejects the player and launches the bike / tumbling="+bike.Tumbling+", riding="+(bool)sim.Current+", bikeY="+bike.transform.position.y+", controller="+game.Player.Controller.enabled);
            Check(game.Player.Velocity.y>0&&game.Player.Health>0,"Ejected rider receives bounded launch velocity and survives the impact");
            yield return new WaitForSeconds(4);Check(!bike.Tumbling&&Vector3.ProjectOnPlane(bike.transform.position-siteAt,Vector3.up).magnitude<16&&bike.transform.up.y>.99f,"Motorcycle lands upright within a moderate distance and remains mountable");
            Destroy(bike.gameObject);Destroy(car.gameObject);Destroy(deck);game.Player.Respawn(UrbanCatalog.Door(4)+Vector3.back*20);
            var a=CityRoadNetwork.Junction(1,1);var roadEnd=CityRoadNetwork.Junction(2,1);var from=Vector3.Lerp(a,roadEnd,.3f);var to=Vector3.Lerp(a,roadEnd,.7f);var route=FourCityNavigation.Route(from,to,out bool connected);float distance=0;for(int i=1;i<route.Count;i++)distance+=Vector3.Distance(route[i-1],route[i]);
            Check(connected&&distance<Vector3.Distance(from,to)+3,"Navigation attaches mid-street objectives to road edges without a junction backtrack");
            var nav=new NavigationGuide();nav.Update(a,roadEnd,Vector3.right);nav.Update(Vector3.Lerp(a,roadEnd,.8f),roadEnd,Vector3.right);Check(Vector3.Dot(nav.Next-Vector3.Lerp(a,roadEnd,.8f),roadEnd-a)>=0,"The active navigation point stays ahead after route progress");
            var chronicle=CityChronicle.Instance;var step=new StoryStep{world=true,position=game.Player.transform.position+Vector3.forward*12,kind="battle",battleMode="escape",waves=1,enemyCount=1,label="탈출"};
            var battleTest=CampaignBattle.Begin(chronicle,step);battleTest.enabled=false;Set(battleTest,"<Wave>k__BackingField",1);var exit=(Vector3)typeof(CampaignBattle).GetField("extraction",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battleTest);
            Check(CampaignBattle.Guide(game.Player.transform.position,out var goal,out _)&&(goal-exit).sqrMagnitude<.1f,"Main combat navigation switches from battle origin to the active extraction point");Destroy(battleTest.gameObject);
            // Exercise the real HTTP/UI path with an offline deterministic NPC response.
            listener=new HttpListener();listener.Prefixes.Add("http://127.0.0.1:18766/");listener.Start();string payload=null;
            var server=Task.Run(()=>{var request=listener.GetContext();using(var reader=new StreamReader(request.Request.InputStream))payload=reader.ReadToEnd();var bytes=Encoding.UTF8.GetBytes("{\"reply\":\"서하, 오늘도 야간 근무야? 난 병동으로 돌아가려던 참이야.\",\"quest\":null}");request.Response.ContentType="application/json";request.Response.OutputStream.Write(bytes,0,bytes.Length);request.Response.Close();});
            string endpoint=Environment.GetEnvironmentVariable("AFTERSIGNAL_NPC_URL");Environment.SetEnvironmentVariable("AFTERSIGNAL_NPC_URL","http://127.0.0.1:18766/dialogue");
            var person=new GameObject("Dialogue verification nurse",typeof(SpriteRenderer),typeof(CityNpc));person.transform.position=game.Player.transform.position+Vector3.right*2;var npc=person.GetComponent<CityNpc>();npc.Configure(93321,"간호사","윤 · 간호사");PeopleArt.Attach(person,"Nurse");yield return null;
            CityLife.Instance.Talk(npc);Check(!CityLife.Instance.Body.Contains("서하:"),"Opening conversation does not invent a spoken line for Seoha");float until=Time.realtimeSinceStartup+8;while(CityLife.Instance.Sending&&Time.realtimeSinceStartup<until)yield return null;
            Check(payload!=null&&payload.Contains("\"opening\":true")&&payload.Contains("\"messages\":[]")&&CityLife.Instance.Body.StartsWith("서하, 오늘도"),"The NPC-first HTTP request has no fabricated user history and displays the reply");
            listener.Stop();listener.Close();listener=null;Environment.SetEnvironmentVariable("AFTERSIGNAL_NPC_URL",endpoint);CityLife.Instance.Dismiss();Destroy(person);
            // Actual world and HUD captures, separate from the artificial collision fixture.
            yield return Shot("afterlight-streets",new Vector3(475,.2f,-90),new Vector3(-25,19,-34),new Vector3(8,6,12));
            var garden=FourCityCatalog.Venues.First(v=>v.city==1&&v.kind==VenueKind.Amusement);yield return Shot("nova-city",garden.Entrance+Vector3.back*18,new Vector3(-80,70,-110),new Vector3(10,15,65));
            var hospital=FourCityCatalog.Venues.First(v=>v.city==3&&v.kind==VenueKind.Hospital);yield return Shot("nereid-city",hospital.Entrance+Vector3.back*18,new Vector3(-55,45,-85),new Vector3(15,12,45));
            Finish();
        }
        // Reproducible photo session on the actual city road, without changing normal population settings.
        IEnumerator DrivingShowcase()
        {
            var sim=UrbanSimulation.Instance;var at=CityRoadNetwork.Junction(1,1)+new Vector3(55,.12f,-5);
            game.Player.Respawn(at+Vector3.back*4);LifeState.Hours=22;game.enabled=true;
            yield return new WaitForSeconds(5);Physics.SyncTransforms();
            var car=sim.Spawn(at,false,0);car.owned=true;
            yield return null;Check(sim.Enter(car),"Seoha is seated at the wheel in the city showcase");
            var photoCars=new List<CityVehicle>();
            for(int i=0;i<10;i++)
            {
                var p=at+new Vector3(-15-(i/2)*13,0,i%2==0?0:10);
                var traffic=sim.Spawn(p,true,new[]{0,1,5,3,0}[i%5]);
                traffic.transform.rotation=Quaternion.Euler(0,i%2==0?0:180,0);
                traffic.route=new[]{p,p+(i%2==0?Vector3.right:Vector3.left)*100};traffic.waypoint=1;traffic.speed=3;photoCars.Add(traffic);
            }
            for(int i=0;i<28;i++)
            {
                var p=at+new Vector3(10-(i/2)*5+(i%3)*.8f,0,(i%2==0?-12:22)+(i%3-1)*1.1f);
                if(!CityGangWar.FindGround(p,out p))continue;
                var person=new GameObject("Showcase sidewalk citizen",typeof(SpriteRenderer),typeof(CityNpc),typeof(CityPedestrian),typeof(ContactShadow));
                person.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
                person.GetComponent<CityNpc>().Configure(24000+i);var walk=person.GetComponent<CityPedestrian>();
                walk.ResetAt(p,p+Vector3.right*6,null);walk.WalkTo(p+Vector3.right*6,false);
            }
            Physics.SyncTransforms();var input=ControlFrame.Empty;input.move=Vector2.up;game.Input.ExternalFrame=input;
            yield return new WaitForSeconds(.6f);game.Input.ExternalFrame=ControlFrame.Empty;
            Set(game,"<NoticeTimer>k__BackingField",0f);Set(game,"<Nearby>k__BackingField",null);
            CameraAt(car.transform.position+new Vector3(15,7,-19),car.transform.position+new Vector3(-14,1,0));
            yield return null;Capture("city-driving");
            CameraAt(car.transform.position+new Vector3(11,4,-12),car.transform.position+new Vector3(-7,1,0));
            yield return null;Capture("city-driving-close");
            Check(sim.Current==car&&photoCars.All(c=>c&&c.occupied),"Showcase contains Seoha driving and ten occupied civilian vehicles");
        }
        IEnumerator BattleShowcase()
        {
            var sim=UrbanSimulation.Instance;sim.Exit();
            var at=CityRoadNetwork.Junction(3,2);game.Player.Respawn(at+new Vector3(45,.2f,0));LifeState.Hours=18.4f;
            yield return new WaitForSeconds(3);Physics.SyncTransforms();
            var titan=RiftCreature.Create(at,0);var soldiers=new List<ArmyResponder>();var officers=new List<PoliceOfficer>();
            for(int i=0;i<6;i++)
            {
                var p=at+new Vector3(20+i%3*4,.1f,-7+i/3*4);
                var officer=PoliceOfficer.Create(WantedSystem.Instance,p,4,i);officer.Ambient=true;officers.Add(officer);
                var soldier=ArmyResponder.Create(at+new Vector3(19+i%3*4,.1f,3+i/3*4),i,null);soldiers.Add(soldier);
            }
            titan.Attacked(soldiers[0].Body,100);Physics.SyncTransforms();
            CameraAt(at+new Vector3(43,8,-9),at+new Vector3(5,4,0));
            for(int i=0;i<12;i++)
            {
                yield return new WaitForSeconds(.5f);Set(game,"<NoticeTimer>k__BackingField",0f);
                CameraAt(at+new Vector3(43,8,-9),at+new Vector3(5,4,0));
                Capture("monster-battle-"+i.ToString("00"));
            }
            Check(soldiers.Any(a=>a&&a.Shots>0)&&officers.Any(a=>a&&a.ShotsFired>0),"Police and defense soldiers actually fire at the live rift creature during capture");
        }
        WorldActor WitnessPerson(Vector3 at,int id)
        {
            var go=new GameObject("Witness essentials / "+id,typeof(SpriteRenderer),typeof(CityNpc));go.transform.position=at;
            go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");go.GetComponent<CityNpc>().Configure(id);return go.GetComponent<WorldActor>();
        }
        IEnumerator BaseChecks()
        {
            while(!MilitaryBaseOperations.Instance||!MilitaryBaseOperations.Instance.Built)yield return null;
            var sim=UrbanSimulation.Instance;sim.enabled=false;var street=CityRoadNetwork.Junction(1,1);game.Player.Respawn(street);Physics.SyncTransforms();
            var a=WitnessPerson(street+new Vector3(3,.1f,2),6101);var b=WitnessPerson(street+new Vector3(4.4f,.1f,2),6102);a.health=b.health=70;
            a.GetComponent<CityNpc>().occupation=b.GetComponent<CityNpc>().occupation="회사원";PeopleArt.Attach(a.gameObject,"OfficeMan");PeopleArt.Attach(b.gameObject,"OfficeMan");yield return null;
            Check(StreetDispute.Begin(a.GetComponent<CityNpc>(),b.GetComponent<CityNpc>()),"Healthy adult citizens can start a local argument");
            yield return new WaitForSeconds(8);var fight=FindAnyObjectByType<StreetDispute>();
            Check(fight&&fight.Punches>1&&a.Alive&&b.Alive&&a.health<70&&b.health<70,"Argument escalates into alternating nonlethal melee / punches="+(fight?fight.Punches:0));
            var patrol=PoliceOfficer.Create(WantedSystem.Instance,street+new Vector3(3,.1f,7),2,71);patrol.enabled=false;Physics.SyncTransforms();yield return new WaitForSeconds(1);
            Check(!FindAnyObjectByType<StreetDispute>()&&a.Alive&&b.Alive,"Nearby police stop the civilian fight before a death");Destroy(a.gameObject);Destroy(b.gameObject);Destroy(patrol.gameObject);
            var site=MilitaryBaseOperations.Bases[0];game.Player.Respawn(new Vector3(510,.15f,765));LifeState.Hours=13;yield return new WaitForSeconds(5);Physics.SyncTransforms();
            var civilian=WitnessPerson(new Vector3(514,.15f,765),6110);PeopleArt.Attach(civilian.gameObject,"CivilianMan");yield return new WaitForSeconds(1.5f);
            Check(civilian.military&&civilian.GetComponent<RegionalUniform>()?.art=="Soldier"&&civilian.GetComponent<GarrisonSupport>()?.Base==site,"Residual civilian inside a military installation becomes a uniformed registered soldier");
            Check(site.Personnel.Count>=40&&site.Vehicles.Count>=20,"Base has existing indoor personnel and a parked weapons fleet / personnel="+site.Personnel.Count+", vehicles="+site.Vehicles.Count);
            var truck=FindObjectsByType<MilitaryGunTruck>().First();Check(FindObjectsByType<MilitaryGunTruck>().Length>=10&&truck.Built&&truck.GetComponent<VehicleCabin>().IdentityAt(0)=="Soldier","Ten original armed 6x6 vehicles have actual weapon mounts and military cabin identities");
            Check(truck.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)<65,"Gun truck hull detail is batched by material while wheels and turret stay articulated");
            CameraAt(truck.transform.position+new Vector3(13,7,-12),truck.transform.position+Vector3.up*1.7f);yield return null;Capture("gun-truck");
            CameraAt(new Vector3(580,34,957),new Vector3(580,1,915));yield return null;Capture("base-facilities");
            Check(!ResponseDispatch.TryOrigin(site.Center,true,false,0,out _)&&!ResponseDispatch.TryOrigin(site.Center,true,true,0,out _),"Ground and air reinforcement generators defer to the existing garrison inside its base");
            var ids=new HashSet<int>(site.Vehicles.Where(v=>v).Select(v=>v.GetInstanceID()));
            var enemy=new GameObject("Probe base intruder",typeof(CapsuleCollider),typeof(WorldActor));enemy.transform.position=new Vector3(465,.1f,824);var body=enemy.GetComponent<WorldActor>();body.monster=true;body.health=1000000;enemy.layer=9;enemy.GetComponent<CapsuleCollider>().height=3;enemy.GetComponent<CapsuleCollider>().center=Vector3.up*1.5f;
            MilitaryBaseOperations.ReportAttack(site.Center,body);Physics.SyncTransforms();
            for(int sample=0;sample<5;sample++){yield return new WaitForSeconds(sample==0?.5f:5.4f);File.WriteAllLines(Path.Combine(Output,"roster-"+sample+".txt"),new[]{"siteActive="+site.Active+" target="+(site.Threat?site.Threat.name:"none")+" intruder="+body.transform.position+" health="+body.health+" down="+body.Downed+" blocked="+game.Blocked+" player="+game.Player.transform.position}.Concat(site.Personnel.Where(s=>s).Select(s=>s.name+" pos="+s.transform.position+" enabled="+s.enabled+" active="+s.gameObject.activeInHierarchy+" sameSite="+(s.Base==site)+" state="+s.DutyState+" health="+s.Body.health+" target="+(s.Target?s.Target.name:"none"))));}
            var drivers=FindObjectsByType<GarrisonVehicleDriver>().Where(d=>d&&d.Car&&ids.Contains(d.Car.GetInstanceID())).ToArray();
            Check(site.Active&&site.Personnel.Count(s=>s&&s.Supporting)>20,"Base alarm mobilizes the stationed roster beyond ordinary dispatch limits / active="+site.Personnel.Count(s=>s&&s.Supporting));
            Check(drivers.Any(d=>d.Car.type==CityVehicleType.Tank)&&drivers.Any(d=>d.Car.type==CityVehicleType.CombatHelicopter)&&drivers.Any(d=>d.Car.GetComponent<MilitaryGunTruck>()),"Real soldiers board pre-existing tanks, helicopters and gun trucks / types="+string.Join(",",drivers.Select(d=>d.Car.type)));
            Check(drivers.All(d=>d.Soldier&&d.Soldier.GetComponent<GarrisonPassenger>()&&d.Soldier.transform.IsChildOf(d.Car.transform))&&drivers.Any(d=>d.Shots>0),"Mounted soldiers remain the same actor and use their vehicle weapons / shots="+drivers.Sum(d=>d.Shots));
            Check(site.Personnel.Where(s=>s&&s.name.Contains("근무 군인")).Any(s=>s.transform.position.z<888||s.GetComponent<GarrisonPassenger>()),"Armed indoor personnel leave the real facility entrances to join base defense");
            CameraAt(new Vector3(465,26,745),new Vector3(468,2,817));yield return null;Capture("base-defense");
            var mounted=drivers.FirstOrDefault(d=>d.Car.GetComponent<MilitaryGunTruck>());if(mounted){var car=mounted.Car;var original=mounted.Soldier.Body;var result=car.GetComponent<VehicleCabin>().EjectDriver();Check(result==original&&!original.transform.IsChildOf(car.transform),"Vehicle takeover ejects the same stationed soldier without creating a substitute");yield return null;car.occupied=false;sim.Enter(car);yield return null;var gun=car.GetComponent<MilitaryGunTruck>();int shots=gun.Shots;gun.TickPlayer(new ControlFrame{attack=true},.2f);Check(sim.Current==car&&gun.Shots>shots,"Seoha can take over the new gun truck and fire its mounted weapon");sim.Exit();}
            Destroy(enemy);site.Threat=null;site.PlayerAlarmUntil=0;site.Active=false;
        }
        IEnumerator CrewWitnessChecks()
        {
            var sim=UrbanSimulation.Instance;var at=CityRoadNetwork.Junction(1,1);game.Player.Respawn(at);Physics.SyncTransforms();
            var vehicles=new List<CityVehicle>();
            foreach(var kind in new[]{CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Fighter,CityVehicleType.Bomber})
            {
                var car=sim.Spawn(at+new Vector3(0,0,40),false,(int)kind);vehicles.Add(car);car.occupied=true;yield return new WaitForSeconds(.2f);
                var cabin=car.GetComponent<VehicleCabin>();cabin.SetPassengers(2);string role=kind==CityVehicleType.Tank?"Soldier":"AirForceCrew";
                Check(cabin.IdentityAt(0)==role&&cabin.IdentityAt(1)==role&&cabin.ReleaseOccupants(false).All(x=>x==role),kind+" uses military crew in cabin, boarding and evacuation");
                Destroy(car.gameObject);yield return null;
            }
            var truck=sim.Spawn(at+Vector3.right*25,false,(int)CityVehicleType.Truck);yield return new WaitForSeconds(.2f);
            truck.gameObject.AddComponent<MilitaryVehicleAI>().Initialize(truck,null);truck.occupied=true;
            var truckCabin=truck.GetComponent<VehicleCabin>();truckCabin.SetPassengers(3);
            Check(truckCabin.IdentityAt(0)=="Soldier"&&truckCabin.IdentityAt(2)=="Soldier","Military component added after cabin initialization overrides cached civilian identities");
            var survivor=VehicleOccupant.Create(truck,truckCabin.IdentityAt(0),0,false,at);Check(survivor.military&&survivor.GetComponent<ArmyResponder>(),"Military driver exits as a functioning soldier");Destroy(survivor.gameObject);Destroy(truck.gameObject);yield return null;
            var fireCar=sim.Spawn(at+Vector3.right*28,false,(int)CityVehicleType.Truck);yield return new WaitForSeconds(.2f);fireCar.gameObject.AddComponent<FireEngineArt>();fireCar.occupied=true;
            yield return new WaitForSeconds(.3f);var fireCabin=fireCar.GetComponent<VehicleCabin>();fireCabin.SetPassengers(0);fireCabin.SetPassengers(2);
            Check(fireCabin.IdentityAt(0)=="Firefighter"&&fireCabin.ReleaseOccupants(false).All(x=>x=="Firefighter"),"Fire crew survives reboarding and evacuation without civilian replacement");
            var engine=fireCar.gameObject.AddComponent<FireEngine>();Set(engine,"<Car>k__BackingField",fireCar);engine.enabled=false;
            var firefighter=Firefighter.Create(engine,null,at+new Vector3(20,0,7),0);
            yield return new WaitForSeconds(.4f);
            Check(firefighter&&!firefighter.GetComponent<DirectionalPerson>().enabled&&FireCrewArt.Frames.Contains(firefighter.GetComponent<SpriteRenderer>().sprite),"Dedicated firefighter sprite remains after multiple LateUpdate frames");
            Destroy(firefighter.gameObject);Destroy(fireCar.gameObject);yield return null;
            // Isolate the visibility and cooldown regression from ongoing ambient incidents.
            at=new Vector3(1000,200,-800);var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.transform.position=at-Vector3.up*.5f;deck.transform.localScale=new Vector3(120,1,100);Physics.SyncTransforms();game.Player.Respawn(at);yield return null;
            var victim=WitnessPerson(at,26000);var witness=WitnessPerson(at+Vector3.right*5,26001);var far=WitnessPerson(at+Vector3.right*70,26002);var hidden=WitnessPerson(at+Vector3.forward*9,26003);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=at+new Vector3(0,2,5);wall.transform.localScale=new Vector3(8,5,1);Physics.SyncTransforms();ActorSpatialIndex.Changed();
            var source=new GameObject("Environmental test source").AddComponent<WorldActor>();source.environmental=true;source.transform.position=at;
            victim.Damage(15,Vector3.zero,source);yield return null;
            var speech=witness.GetComponent<NpcSpeech>();string injuryLine=speech?speech.CurrentLine:"";
            Check(speech&&!string.IsNullOrEmpty(injuryLine),"Damage event makes a nearby witness react to the injury");
            Check(!far.GetComponent<NpcSpeech>()&&!hidden.GetComponent<NpcSpeech>(),"Distant and wall-occluded people do not react to the victim");
            Check(!NpcSpeech.Witness(witness,false),"Witness cooldown suppresses repeated injury chatter");
            yield return new WaitForSeconds(.12f);victim.Damage(150,Vector3.zero,source);yield return null;yield return null;
            Check(!victim.Alive&&speech.CurrentLine!=injuryLine,"Death event upgrades an earlier injury reaction immediately");
            string first=NpcDialogueBank.Line(witness.GetComponent<CityNpc>(),"witness_death"),second=NpcDialogueBank.Line(witness.GetComponent<CityNpc>(),"witness_death");Check(first!=second,"Consecutive witness lines avoid immediate repetition");
            foreach(var body in new[]{victim,witness,far,hidden,source})Destroy(body.gameObject);Destroy(wall);Destroy(deck);
        }
        IEnumerator Shot(string name,Vector3 at,Vector3 offset,Vector3 focus)
        {game.Player.Respawn(at);game.enabled=true;yield return new WaitForSeconds(4);game.enabled=false;Set(game,"<NoticeTimer>k__BackingField",0f);CameraAt(at+offset,at+focus);yield return null;Capture(name);}
        void CameraAt(Vector3 at,Vector3 target){Camera.main.transform.SetPositionAndRotation(at,Quaternion.LookRotation(target-at));Camera.main.fieldOfView=55;}
        void Capture(string name)
        {
            var camera=Camera.main;var overlays=FindObjectsByType<Canvas>().Where(c=>c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=.7f;}Canvas.ForceUpdateCanvases();
            var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGBA32,false);RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Destroy(texture);foreach(var c in overlays)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>260){report.errors.Add("Facility response probe timed out");Finish();}}
        void Finish(){if(finished)return;finished=true;report.completed=report.errors.Count==0;Save();Application.Quit(report.completed?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Log;listener?.Close();}
    }
}
