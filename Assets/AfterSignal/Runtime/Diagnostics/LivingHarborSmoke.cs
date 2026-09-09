using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class LivingHarborSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began;bool finished;GameDirector game;GameObject platform;
        static readonly Vector3 Sandbox=new(-450,80,-900);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-living-harbor-smoke")&&!FindAnyObjectByType<LivingHarborSmoke>()){GameDirector.SkipTitle=true;new GameObject("Living harbor essential checks").AddComponent<LivingHarborSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;began=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/LivingHarbor/Native"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(message+"\n"+stack);Save();}}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        void Check(bool yes,string label){(yes?report.passed:report.errors).Add(label);Debug.Log("HARBOR "+yes+" / "+label);Save();}
        static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
        GameObject Box(string name,Vector3 at,Vector3 size){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Concrete");return go;}
        WorldActor Person(Vector3 at){var go=new GameObject("Spacing check resident",typeof(SpriteRenderer),typeof(CityNpc));go.transform.position=at;go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");go.GetComponent<CityNpc>().Configure(Mathf.Abs(go.GetInstanceID()),"회사원",null,"");PeopleArt.Attach(go,"CivilianMan");return go.GetComponent<WorldActor>();}
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            Debug.Log("HARBOR relocation count: "+HarborParcelRepair.Relocated);
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.enabled=false;game.CameraRig.enabled=false;LifeState.Hours=14;WantedSystem.Clear("");WantedSystem.Instance.enabled=false;CityChronicle.Instance.enabled=false;RiftIncursion.Instance.enabled=false;
            CivicTerrorEvents.Instance.enabled=false;if(GangStrongholds.Instance)GangStrongholds.Instance.enabled=false;
            foreach(var d in FindObjectsByType<CityActivityDirector>(FindObjectsSortMode.None))d.enabled=false;
            if(Environment.GetCommandLineArgs().Contains("-living-harbor-final-check")){yield return LastCheck();finished=true;report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);yield break;}
            if(Environment.GetCommandLineArgs().Contains("-living-harbor-followup")){yield return Followup();finished=true;report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);yield break;}
            foreach(string key in new[]{"CoastGuard","NavyCrew","AirForceCrew","SeaRaider","NullCell"})
            {var frames=Resources.LoadAll<Sprite>("Art/CivicForces/"+key);Check(frames.Length==16&&frames.Max(s=>s.bounds.size.y)<2.3f,"16 directional poses at human scale / "+key);}
            platform=Box("Essential traversal fixture",Sandbox-Vector3.up*.5f,new Vector3(120,1,120));game.Player.Respawn(Sandbox+Vector3.right*20);yield return null;
            var crowd=new List<WorldActor>();for(int i=0;i<12;i++)crowd.Add(Person(Sandbox));yield return new WaitForSeconds(6);
            float nearest=float.MaxValue;for(int i=0;i<crowd.Count;i++)for(int j=i+1;j<crowd.Count;j++)nearest=Mathf.Min(nearest,Vector3.Distance(crowd[i].transform.position,crowd[j].transform.position));
            Check(nearest>.72f,"Twelve overlapping residents separate: minimum "+nearest.ToString("0.00")+" m");foreach(var a in crowd)Destroy(a.gameObject);yield return null;
            var curb=Box("0.8 metre curb",Sandbox+new Vector3(4,.4f,0),new Vector3(8,.8f,8));Physics.SyncTransforms();var follower=Person(Sandbox+Vector3.left*4);var escort=follower.gameObject.AddComponent<EscortFollower>();game.Player.Respawn(Sandbox+new Vector3(6,.84f,0));
            float until=Time.time+12;while(Time.time<until&&Vector3.Distance(follower.transform.position,game.Player.transform.position)>2.2f){escort.Tick(game.Player,Sandbox+Vector3.forward*40,.04f);yield return null;}
            Check(follower.transform.position.x>Sandbox.x+1&&follower.transform.position.y>80.7f,"Rescue civilian steps onto high curb and continues following / "+follower.transform.position);Destroy(follower.gameObject);Destroy(curb);yield return null;
            var family=FamilyGroup.Create(Sandbox,51,true);family.enabled=false;var couple=FamilyGroup.Create(Sandbox+Vector3.forward*8,53,false);couple.enabled=false;yield return null;
            Check(family.Members.Count==3&&couple.Members.Count==2&&NpcPersona.Age(family.Members[2].GetComponent<CityNpc>())<14,"Family and couple roles include age-appropriate child");
            var child=family.Members[2];var parent=family.Members[0];parent.Damage(999,Vector3.zero,TrafficDamageSource.Environment);yield return null;
            Check(parent.GetComponent<FamilyMember>().DeathAnnounced&&NpcDialogueBank.Line(child.GetComponent<CityNpc>(),"companion_death").Length>10,"Companion death notification and child-specific response");
            var worker=couple.Members[0].GetComponent<CityNpc>();LifeState.Hours=7;string morning=NpcDialogueBank.Line(worker,"ambient");LifeState.Hours=18;string evening=NpcDialogueBank.Line(worker,"ambient");Check(morning!=evening,"Time-dependent dialogue changes between commute and return");
            yield return View("FamilyResidents",Sandbox+new Vector3(12,4,-7),Sandbox+new Vector3(0,1,5));Destroy(family.gameObject);Destroy(couple.gameObject);LifeState.Hours=14;
            yield return Missions();
            var building=Box("Reversible building fixture",Sandbox+new Vector3(20,10,0),new Vector3(9,20,9));var collapse=building.AddComponent<CollapsibleBuilding>();collapse.worldBounds=building.GetComponent<Renderer>().bounds;
            collapse.Collapse(building.transform.position,Vector3.right,TrafficDamageSource.Environment);yield return new WaitForSeconds(4.8f);Check(collapse.Collapsed&&!building.GetComponent<Collider>().enabled,"Building collapse disables damaged solid");collapse.Restore();yield return null;Check(!collapse.Collapsed&&building.GetComponent<Collider>().enabled&&building.GetComponent<Renderer>().enabled,"Building restoration recovers rendering and collision");
            var original=building.GetComponent<MeshFilter>().sharedMesh;int triangles=original.triangles.Length;int cut=DistrictGeometryCut.Remove(collapse.worldBounds);Check(building.GetComponent<MeshFilter>().sharedMesh.triangles.Length<triangles,"Combined mesh destruction cuts geometry");DistrictGeometryCut.Restore(cut);Check(building.GetComponent<MeshFilter>().sharedMesh.triangles.Length==triangles,"Combined geometry is recoverable");Destroy(building);
            var hostile=TerrorSuspect.Create(Sandbox+Vector3.forward*10,0);hostile.enabled=false;var officer=PoliceOfficer.Create(WantedSystem.Instance,Sandbox+Vector3.forward*15,2,0);officer.enabled=false;var soldier=ArmyResponder.Create(Sandbox+Vector3.forward*17,1,null);soldier.enabled=false;Physics.SyncTransforms();yield return new WaitForSeconds(1.1f);
            Check(CityIncidentBoard.Active.Any(x=>x.threat==hostile.Body),"Live terrorist incident is visible to map");Check(!hostile.Body.gang&&hostile.Body.terrorist&&FactionCombat.NearestOpponent(officer.Body,20)==hostile.Body&&FactionCombat.NearestOpponent(soldier.Body,20)!=hostile.Body,"Independent terrorist group is a police target and excluded from army targeting");
            int cash=LifeState.Credits;hostile.Body.Damage(999,Vector3.zero);CityIncidentBoard.Neutralized(hostile.Body);CityIncidentBoard.Neutralized(hostile.Body);Check(LifeState.Credits==cash+160,"Assistance reward pays exactly once");foreach(var go in new[]{hostile.gameObject,officer.gameObject,soldier.gameObject})Destroy(go);
            yield return Sports();
            yield return Transport();
            var navy=MaritimeWorld.Instance.Spawn(new Vector3(2100,OceanLife.Surface,-1120),SeaFaction.Navy);var pirate=MaritimeWorld.Instance.Spawn(new Vector3(2250,OceanLife.Surface,-1120),SeaFaction.Pirates);navy.enabled=pirate.enabled=false;
            navy.Car.health=navy.Car.MaxHealth;pirate.Car.health=pirate.Car.MaxHealth;navy.transform.SetPositionAndRotation(new Vector3(2100,OceanLife.Surface,-1120),Quaternion.identity);pirate.transform.SetPositionAndRotation(new Vector3(2250,OceanLife.Surface,-1120),Quaternion.identity);game.Player.Respawn(navy.transform.position+Vector3.up*20);
            yield return new WaitForSeconds(1);Physics.SyncTransforms();float health=pirate.Car.health;MaritimeWeapons.Fire(navy,pirate.AimCenter,false);yield return null;
            Check(pirate.Car.health<health,"Naval cannon hits pirate vessel through shared ballistic collision");
            Check(MaritimeWorld.Instance.Fleet.Where(s=>s).Select(s=>s.Faction).Distinct().Count()==3&&navy.GetComponentsInChildren<MeshRenderer>().Length>15,"Coast guard, navy and pirates have authored vessels");
            yield return View("SeaPatrol",navy.transform.position+new Vector3(-62,38,-66),navy.transform.position+Vector3.right*35);
            Check(FindObjectsByType<RegionalUniform>(FindObjectsSortMode.None).Select(x=>x.art).Distinct().Intersect(new[]{"CoastGuard","NavyCrew","AirForceCrew"}).Count()==3,"Separate marine and air bases are staffed with new uniforms");
            Debug.Log("HARBOR relocation count: "+HarborParcelRepair.Relocated);yield return View("HarborLayout",new Vector3(1750,150,-800),new Vector3(1710,0,-470));
            finished=true;report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);
        }
        IEnumerator LastCheck()
        {
            platform=Box("Combat verification ground",Sandbox-Vector3.up*.5f,new Vector3(120,1,120));game.Player.Respawn(Sandbox+Vector3.right*18);game.Player.Heal(100);Set(game.Player,"invincible",0f);Physics.SyncTransforms();
            var suspect=TerrorSuspect.Create(Sandbox,0);suspect.Body.Damage(1,Vector3.zero);float health=game.Player.Health;yield return new WaitForSeconds(4.5f);Check(game.Player.Health<health,"Terrorist retaliatory gunfire hits the player after spawn protection expires");Destroy(suspect.gameObject);
            var fleet=MaritimeWorld.Instance;foreach(var ship in fleet.Fleet)if(ship)ship.enabled=false;
            var navy=fleet.Spawn(new Vector3(2110,OceanLife.Surface,-1120),SeaFaction.Navy);var pirate=fleet.Spawn(new Vector3(2250,OceanLife.Surface,-1110),SeaFaction.Pirates);var coast=fleet.Spawn(new Vector3(2110,OceanLife.Surface,-1180),SeaFaction.CoastGuard);navy.enabled=pirate.enabled=coast.enabled=false;game.Player.Respawn(navy.transform.position+Vector3.up*25);yield return new WaitForSeconds(1.1f);Physics.SyncTransforms();
            float hp=pirate.Car.health;MaritimeWeapons.Fire(navy,pirate.AimCenter,false);yield return null;Check(pirate.Car.health<hp,"Re-exported naval hull retains live ballistic hit detection");
            yield return View("SeaPatrol",navy.transform.position+new Vector3(58,31,-64),navy.transform.position+Vector3.up*3);
            yield return View("CoastGuardCutter",coast.transform.position+new Vector3(24,13,-27),coast.transform.position+Vector3.up*2);
            yield return View("PirateInterceptor",pirate.transform.position+new Vector3(19,10,-21),pirate.transform.position+Vector3.up);
        }
        IEnumerator Followup()
        {
            platform=Box("Traversal followup",Sandbox-Vector3.up*.5f,new Vector3(120,1,120));
            var curb=Box("0.8 metre curb",Sandbox+new Vector3(4,.4f,0),new Vector3(8,.8f,8));Physics.SyncTransforms();
            var follower=Person(Sandbox+Vector3.left*4);var escort=follower.gameObject.AddComponent<EscortFollower>();game.Player.Respawn(Sandbox+new Vector3(6,.84f,0));
            float until=Time.time+12;while(Time.time<until&&Vector3.Distance(follower.transform.position,game.Player.transform.position)>2.2f){escort.Tick(game.Player,Sandbox+Vector3.forward*40,.04f);yield return null;}
            Check(follower.transform.position.x>Sandbox.x+1&&follower.transform.position.y>80.7f,"Rescue civilian clears 0.8 m curb / "+follower.transform.position);yield return View("CurbRescue",Sandbox+new Vector3(-8,5,-9),follower.transform.position+Vector3.up);Destroy(follower.gameObject);Destroy(curb);yield return null;
            int groundTriangles=platform.GetComponent<MeshFilter>().sharedMesh.triangles.Length;int undergroundCut=DistrictGeometryCut.Remove(new Bounds(Sandbox+Vector3.up*9,new Vector3(120,22,120)));Check(platform.GetComponent<MeshFilter>().sharedMesh.triangles.Length==groundTriangles,"Subsurface building foundation bounds preserve shared ground geometry");DistrictGeometryCut.Restore(undergroundCut);
            var group=FamilyGroup.Create(Sandbox,57,true);yield return null;Vector3 before=group.Members[0].transform.position;foreach(var a in group.Members)a.GetComponent<CityNpc>().Panic(Sandbox-Vector3.right*10,15);yield return new WaitForSeconds(2);
            Check(Vector3.Distance(before,group.Members[0].transform.position)>2,"Family evacuates together without competing panic movement");Destroy(group.gameObject);
            var suspect=TerrorSuspect.Create(Sandbox+new Vector3(-40,0,30),0);suspect.enabled=false;int reports=CitySafety.Instance.Reports;CitySafety.Alarm(suspect.transform.position,suspect.Body);Check(CitySafety.Instance.Reports==reports,"Terrorist cannot act as its own witness");
            var witness=Person(suspect.transform.position+Vector3.right*5);CitySafety.Alarm(suspect.transform.position,suspect.Body,witness.GetComponent<CityNpc>());Check(CitySafety.Instance.Reports==reports+1,"Civilian witness report initiates police response");witness.transform.position+=Vector3.forward*8;Physics.SyncTransforms();game.Player.Respawn(suspect.transform.position+Vector3.right*18);game.Player.Heal(100);float playerHealth=game.Player.Health;Set(game.Player,"invincible",0f);suspect.Body.Damage(1,Vector3.zero);suspect.enabled=true;yield return new WaitForSeconds(4.2f);Check(game.Player.Health<playerHealth,"Terrorist retaliatory gunfire can hit the player");Destroy(suspect.gameObject);Destroy(witness.gameObject);yield return null;
            var gang=GangMember.Create(Sandbox+Vector3.forward*15,0,2);gang.enabled=false;Set(gang,"playerTarget",true);var titan=RiftCreature.Create(Sandbox+Vector3.forward*35,0);titan.enabled=false;yield return new WaitForSeconds(1.2f);
            Check(CityIncidentBoard.Active.Any(x=>x.threat==titan.Body)&&CityIncidentBoard.Active.Any(x=>x.threat==gang.Body),"Monster and gang threat locations reach map incident feed");int cash=LifeState.Credits;
            gang.Body.Damage(9999,Vector3.zero);titan.Body.Damage(1000000,Vector3.zero);yield return null;Check(LifeState.Credits==cash+740,"Monster and gang assistance grant their distinct rewards");Destroy(gang.gameObject);Destroy(titan.gameObject);
            var fleet=MaritimeWorld.Instance;var navy=fleet.Spawn(new Vector3(2100,OceanLife.Surface,-1120),SeaFaction.Navy);var pirate=fleet.Spawn(new Vector3(2250,OceanLife.Surface,-1120),SeaFaction.Pirates);var coast=fleet.Spawn(new Vector3(2090,OceanLife.Surface,-1210),SeaFaction.CoastGuard);navy.enabled=pirate.enabled=coast.enabled=false;game.Player.Respawn(navy.transform.position+Vector3.up*20);yield return new WaitForSeconds(1.1f);Physics.SyncTransforms();
            float hp=pirate.Car.health;MaritimeWeapons.Fire(navy,pirate.AimCenter,false);yield return null;Check(pirate.Car.health<hp,"Naval cannon collides with and damages pirate vessel");
            Check(new[]{navy,pirate,coast}.All(s=>s.GetComponentsInChildren<Transform>().Any(t=>t.name=="DeckTurret")),"Three dedicated naval hulls have articulated deck turrets");
            var crew=VehicleOccupant.Create(coast.Car,"CoastGuard",0,false,coast.transform.position);yield return null;Check(crew.police&&crew.GetComponent<RegionalUniform>().art=="CoastGuard","Disembarked coast guard retains faction and new uniform");Destroy(crew.gameObject);
            coast.Car.Damage(coast.Car.MaxHealth*.8f,coast.AimCenter,TrafficDamageSource.Environment);Check(coast.Car.occupied&&coast.GetComponent<VehicleEmergency>().State==VehicleEmergency.Reaction.Calm,"Damaged armed vessels retain their crew during engagement");
            yield return View("SeaPatrol",navy.transform.position+new Vector3(-62,38,-66),navy.transform.position+Vector3.right*35);
            var roles=FindObjectsByType<RegionalUniform>(FindObjectsSortMode.None).Select(x=>x.art).Distinct().ToArray();Check(roles.Intersect(new[]{"CoastGuard","NavyCrew","AirForceCrew"}).Count()==3,"Separate coast guard, navy and air force personnel / "+string.Join(",",roles));
            var planes=UrbanSimulation.Instance.Cars.Count(c=>c&&c.IsAircraft&&(c.transform.position-MaritimeWorld.AirBase).sqrMagnitude<130*130);Check(planes>=4,"Aircraft relocated from army compound to air base / "+planes);
            Debug.Log("HARBOR relocation count: "+HarborParcelRepair.Relocated);Check(HarborParcelRepair.Relocated>0,"Misplaced harbor high-rises relocated / "+HarborParcelRepair.Relocated);yield return View("HarborLayout",new Vector3(1750,150,-800),new Vector3(1710,0,-470));
        }
        IEnumerator Missions()
        {
            var chronicle=CityChronicle.Instance;var quest=chronicle.Quests.First(q=>q.main);chronicle.Progress.tracked=quest.id;
            foreach(string mode in new[]{"assault","defend","sabotage","rescue","escape","boss"})
            {
                chronicle.Entry(quest).step=0;chronicle.Progress.tracked=quest.id;quest.steps=new[]{new StoryStep{kind="battle",battleMode=mode,position=Sandbox,waves=1,enemyCount=3,duration=1,line="진입",outro="현장 확보"}};
                game.Player.Respawn(Sandbox);var battle=CampaignBattle.Begin(chronicle,quest.steps[0]);yield return new WaitForSeconds(4.3f);
                Check(battle&&battle.Remaining>0,"Main mission spawns reachable targets / "+mode);
                if(!battle)continue;foreach(var enemy in Field<List<WorldActor>>(battle,"enemies"))if(enemy){enemy.health=0;if(enemy.Titan)enemy.Titan.enabled=false;}
                foreach(var device in Field<List<WorldActor>>(battle,"devices"))if(device)device.health=0;
                Vector3 exit=Field<Vector3>(battle,"extraction");if(mode=="escape")game.Player.Respawn(exit);
                if(mode=="rescue")
                {
                    var citizen=Field<CityNpc>(battle,"survivor");game.Player.Respawn(citizen.transform.position+Vector3.right*2);yield return new WaitForSeconds(.3f);
                    float timeout=Time.time+15;while(battle&&Time.time<timeout&&Vector3.Distance(citizen.transform.position,exit)>3){var p=Vector3.MoveTowards(game.Player.transform.position,exit,Time.deltaTime*3);game.Player.Respawn(p,false);yield return null;}
                }
                float deadline=Time.time+9;while(battle&&Time.time<deadline)yield return null;
                Check(chronicle.Entry(quest).step==1,"Main mission advances after real completion / "+mode);if(battle)Destroy(battle.gameObject);yield return null;
            }
        }
        IEnumerator Sports()
        {
            foreach(var kind in new[]{VenueKind.Football,VenueKind.Baseball,VenueKind.Basketball,VenueKind.Circuit})
            {
                var venue=FindObjectsByType<VenueRuntime>(FindObjectsSortMode.None).First(v=>v.Definition.city==0&&v.Definition.kind==kind);var match=FourCitySports.Instance.Get(venue.Definition.id);match.Begin();game.Player.Respawn(venue.Definition.Entrance);yield return new WaitForSeconds(1.2f);
                Check(venue.ActiveVisitors>=140,"Expanded nonduplicated crowd / "+kind+" / "+venue.ActiveVisitors);
                var safety=venue.GetComponent<VenueSafety>();game.Player.Respawn(venue.transform.TransformPoint(kind==VenueKind.Circuit?FourCityArchitecture.Track(.1f):Vector3.zero));yield return new WaitForSeconds(.5f);float clock=match.clock;yield return new WaitForSeconds(.3f);Check(safety.Suspended&&Mathf.Approximately(match.clock,clock),"Field intrusion pauses match clock / "+kind);
                game.Player.Respawn(venue.Definition.Entrance);yield return new WaitForSeconds(.5f);Check(!safety.Suspended,"Play resumes after pitch clearance / "+kind);
                var witness=venue.GetComponentsInChildren<VenueActor>().First(x=>!x.athlete);witness.GetComponent<WorldActor>().Damage(25,Vector3.zero,TrafficDamageSource.Environment);yield return new WaitForSeconds(.1f);Check(safety.Emergency,"Spectators, players and staff share emergency response / "+kind);
                if(kind==VenueKind.Football)yield return View("StadiumCrowd",venue.transform.position+new Vector3(58,23,-46),venue.transform.position+new Vector3(5,1,15));
                Set(safety,"resume",Time.time-1);
            }
        }
        IEnumerator Transport()
        {
            var sim=UrbanSimulation.Instance;var air=IntercityService.All.First(x=>x.Aircraft);air.Car.speed=0;air.enabled=false;Set(air,"dwell",50f);game.Player.Respawn(air.Terminal(air.AtNova));int cash=LifeState.Credits;if(cash<500)LifeState.Earn(500);cash=LifeState.Credits;
            Check(air.BuyTicket()&&sim.Current==air.Car&&sim.SeatIndex>0&&LifeState.Credits==cash-air.Fare,"Terminal passenger boarding deducts one airfare");Check(!air.BuyTicket()&&LifeState.Credits==cash-air.Fare,"Ticket cannot be charged twice while seated");sim.Exit();air.Car.occupied=false;Check(sim.Enter(air.Car,0)&&sim.SeatIndex==0,"Airport aircraft cockpit is accessible");sim.Exit();
            var cargo=sim.Cars.FirstOrDefault(c=>c&&c.GetComponent<AuthoredCraft>()&&c.IsWatercraft&&!c.GetComponent<SeaCombat>());Check(cargo,"Cargo vessel exists");if(cargo){var route=cargo.GetComponent<PassengerRoute>();if(route)route.enabled=false;cargo.speed=0;cargo.occupied=false;yield return new WaitForSeconds(.8f);var access=cargo.GetComponent<TransportAccess>();game.Player.Respawn(access.Dock);Check(TransportAccess.Distance(cargo,game.Player.transform.position)<5.3f&&sim.Enter(cargo,0),"Cargo access point leads to helm");sim.Exit();}
        }
        IEnumerator View(string name,Vector3 eye,Vector3 target)
        {
            var cam=Camera.main;cam.transform.position=eye;cam.transform.LookAt(target);cam.fieldOfView=60;yield return new WaitForSeconds(.25f);var rt=new RenderTexture(1280,720,24);rt.Create();for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});yield return null;}var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(tex);
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>360){report.errors.Add("Native check timeout");finished=true;Save();Application.Quit(1);}}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
