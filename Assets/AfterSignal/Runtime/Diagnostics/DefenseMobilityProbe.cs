using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    // Opt-in native regression check; never writes the user's saved game.
    public sealed class DefenseMobilityProbe:MonoBehaviour
    {
        [Serializable] sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();GameDirector game;float began;bool finished;
        const string Output="Artifacts/DefenseMobility/Native";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(!Environment.GetCommandLineArgs().Contains("-defense-mobility-probe")||FindAnyObjectByType<DefenseMobilityProbe>())return;GameDirector.SkipTitle=true;new GameObject("Defense and mobility essentials").AddComponent<DefenseMobilityProbe>();}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;Directory.CreateDirectory(Output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string line,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(line+"\n"+stack);Save();}}
        void Check(bool ok,string text){(ok?report.passed:report.errors).Add(text);Debug.Log("DEFENSE MOBILITY "+ok+" / "+text);Save();}
        void Save()=>File.WriteAllText(Path.Combine(Output,"result.json"),JsonUtility.ToJson(report,true));
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        GameObject Box(string name,Vector3 at,Vector3 size){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Concrete");return go;}
        WorldActor Citizen(string role,Vector3 at)
        {
            var go=new GameObject("Probe / "+role,typeof(SpriteRenderer),typeof(CityNpc));go.transform.position=at;var npc=go.GetComponent<CityNpc>();npc.Configure(91917,role,"안전 검증 시민");npc.enabled=false;PeopleArt.Attach(go,role);return go.GetComponent<WorldActor>();
        }
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);Set(game,"<Title>k__BackingField",false);Set(game,"<Fade>k__BackingField",0f);game.enabled=false;game.CameraRig.enabled=false;
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is CityChronicle||b is CitySafety||b is CityFireService||b is WantedSystem||b is CityGangWar||b is GangStrongholds||b is CitySocial||b is CityActivityDirector||b is GangCrime||b is GangMember||b is PoliceOfficer||b is ArmyResponder||b is RegionalGuard||b is SeaCombat||b is RiftIncursion||b is CivicTerrorEvents||b is FourCityCampaign||b is SecurityResponse)b.enabled=false;
            foreach(var battle in FindObjectsByType<CampaignBattle>())Destroy(battle.gameObject);WantedSystem.Clear("");CityEventGate.Reset();
            var sim=UrbanSimulation.Instance;sim.enabled=false;var site=new Vector3(1000,200,-800);var floor=Box("Mobility diagnostic deck",site-Vector3.up*.5f,new Vector3(220,1,180));game.Player.Respawn(site+Vector3.back*55);yield return null;
            var camera=Camera.main;Set(game.CameraRig,"orbitYaw",0f);Set(game.CameraRig,"orbitPitch",15f);game.CameraRig.Snap();game.CameraRig.enabled=true;
            var look=ControlFrame.Empty;look.look=true;look.lookDelta=new Vector2(260,30);game.CameraRig.ReadLook(look);yield return null;
            var focus=(Vector3)typeof(CameraRig).GetProperty("Focus",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.CameraRig);
            Check(Vector3.Angle(camera.transform.forward,focus-camera.transform.position)<.2f,"Normal orbit aims at the smoothed focus instead of snapping the camera rotation ahead of its position");
            yield return new WaitForSeconds(1);Check(!game.CameraRig.WallCorrection&&Mathf.Abs(camera.fieldOfView-45)<.5f&&camera.nearClipPlane==.15f,"Normal field of view and clipping are restored");
            Set(game.Player,"<WallClimbing>k__BackingField",true);Set(game.Player,"<WallNormal>k__BackingField",Vector3.back);game.CameraRig.Snap();yield return null;
            Check(game.CameraRig.WallCorrection&&camera.nearClipPlane==.08f,"Extra camera correction is enabled only while attached to a wall");Set(game.Player,"<WallClimbing>k__BackingField",false);game.CameraRig.enabled=false;
            game.Die();yield return null;var choices=(UnityEngine.UI.Button[])Get(game.Hud,"respawnChoices");
            Check(choices.Length==4&&choices.All(b=>b.gameObject.activeInHierarchy)&&Time.timeScale==0,"Death pauses the game and presents four city selection buttons");choices[3].onClick.Invoke();
            Check(((UnityEngine.UI.Button)Get(game.Hud,"primary")).GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("네레이드"),"Selecting a death-menu city changes the explicit respawn destination");yield return null;Capture("respawn-selection");Set(game,"<Dead>k__BackingField",false);Time.timeScale=1;game.Player.Heal(100);yield return null;
            Check(!FindObjectsByType<RectTransform>().Any(t=>t.name=="Weapon shortcuts"),"Gameplay has no list of unequipped weapon shortcuts");
            RespawnNetwork.Resolve();for(int i=1;i<4;i++)Check(FourCityCatalog.CityAt(RespawnNetwork.Points[i])==i,"Respawn facility is in city "+i);
            Check(FindObjectsByType<MilitaryWeaponRack>().Length>=39&&FindObjectsByType<GarrisonSupport>().Length>=20,"Three staffed military bases have physical arsenals and existing garrison responders");
            for(int i=0;i<ArmoryInventory.Items.Length;i++)ArmoryInventory.Issue(i);
            Check(Enumerable.Range(0,13).All(ArmoryInventory.Owns)&&ArmoryInventory.Rounds(10)>=6&&ArmoryInventory.Rounds(11)==1&&ArmoryInventory.Reserve(8)>=90,"All thirteen military arsenal weapons can be issued with usable ammunition");
            MilitaryArmory.Build(transform,site+new Vector3(-75,0,-55));var rack=FindObjectsByType<MilitaryWeaponRack>().First(r=>r.Item==9);rack.Use();
            Check(game.Player.Equipment.Slot==3&&ArmoryInventory.Equipped(3)==9,"Using a weapon rack equips that weapon in the real player inventory");
            var obstacle=Box("Vehicle detour obstacle",site+Vector3.up*4,new Vector3(14,8,28));
            var car=sim.Spawn(site+Vector3.left*40,false,0);car.route=new[]{site+Vector3.right*40,site+Vector3.left*40};car.traffic=true;car.waypoint=0;car.occupied=true;
            var blocker=Box("Response detour obstacle",site+new Vector3(0,4,49),new Vector3(14,8,22));var truck=sim.Spawn(site+new Vector3(-40,0,49),false,(int)CityVehicleType.Truck);truck.occupied=true;
            yield return null;Physics.SyncTransforms();var navigator=new VehicleNavigator();bool found=navigator.Find(car,site+Vector3.right*40);
            Check(found&&navigator.Path.Any(p=>Mathf.Abs(p.z-site.z)>14+car.HalfWidth),"Vehicle A-star finds a supported route around the full building footprint");
            var response=new ResponseDrive();float deadline=Time.time+38;bool civilianArrived=false,responderArrived=false;float civilSide=0;
            while(Time.time<deadline&&(!civilianArrived||!responderArrived))
            {
                float dt=Mathf.Min(.05f,Time.deltaTime);
                if(!civilianArrived){car.TickTraffic(dt);civilSide=Mathf.Max(civilSide,Mathf.Abs(car.transform.position.z-site.z));civilianArrived=(car.transform.position-(site+Vector3.right*40)).sqrMagnitude<25;if(civilianArrived){car.traffic=false;car.speed=0;}}
                if(!responderArrived){response.Drive(truck,site+new Vector3(40,0,49),dt,4);responderArrived=(truck.transform.position-(site+new Vector3(40,0,49))).sqrMagnitude<36;}
                yield return null;
            }
            Check(civilianArrived&&civilSide>16,"Civilian traffic physically drives around a blocked street and reaches its waypoint / "+car.transform.position);
            Check(responderArrived,"An emergency truck drives around a wall and reaches the incident / "+truck.transform.position);
            camera.transform.SetPositionAndRotation(site+new Vector3(10,62,-77),Quaternion.LookRotation(site+new Vector3(0,0,20)-(site+new Vector3(10,62,-77))));Capture("vehicle-detours");Destroy(car.gameObject);Destroy(truck.gameObject);Destroy(obstacle);Destroy(blocker);
            var defender=Citizen("Soldier",site+Vector3.left*20);defender.military=true;GarrisonSupport.Register(defender,defender.transform.position);
            var hostile=Citizen("CivilianMan",site+Vector3.right*35);hostile.gang=true;hostile.health=5000;yield return null;Physics.SyncTransforms();var support=defender.GetComponent<GarrisonSupport>();float targetHealth=hostile.health;
            yield return new WaitForSeconds(3);Check(support.Supporting&&support.Shots>0&&hostile.health<targetHealth,"Existing garrison personnel identify a nearby hostile and fire supporting shots");hostile.health=0;
            yield return new WaitForSeconds(12);Check(!support.Supporting,"Garrison support ends after the threat is gone and the soldier returns to duty");Destroy(hostile.gameObject);Destroy(defender.gameObject);
            var water=new Vector3(1500,OceanLife.Surface,-1650);game.Player.Respawn(water+Vector3.up*20);var boatA=sim.Spawn(water,false,6);var boatB=sim.Spawn(water,false,6);yield return null;yield return null;
            Check(!SeaTraffic.Overlap(boatA,boatB,out _,0),"Initially overlapping civilian ships separate according to their complete hull sizes");
            Destroy(boatA.gameObject);Destroy(boatB.gameObject);yield return null;boatA=sim.Spawn(water-Vector3.right*45,false,6);boatB=sim.Spawn(water,false,6);yield return null;yield return null;
            boatA.transform.position=water+Vector3.right*45;yield return null;
            Check(boatA.transform.position.x<boatB.transform.position.x&&!SeaTraffic.Overlap(boatA,boatB,out _,0),"Swept ship contact prevents fast vessels crossing through another hull");
            boatA.transform.position=water-Vector3.right*22;boatA.speed=5;yield return null;var steering=SeaTraffic.Steer(boatA,Vector3.right);Check(Mathf.Abs(steering.z)>.1f,"All registered ship types share predictive passing avoidance");Destroy(boatA.gameObject);Destroy(boatB.gameObject);
            game.Player.Respawn(site+Vector3.back*55);yield return null;
            var bomber=sim.Spawn(site+new Vector3(0,65,0),false,(int)CityVehicleType.Bomber);bomber.occupied=true;yield return null;
            var bay=bomber.GetComponent<BomberBay>();Check(bomber.IsAircraft&&bay&&bomber.HalfWidth==26&&bomber.GetComponents<Collider>().Length>=2&&VehicleSeats.Count(bomber)==2,"Dedicated broad-wing bomber has flight controls, two cockpit seats and physical airframe collision");
            Check(sim.Cars.Count(c=>c&&c.type==CityVehicleType.Bomber&&c.GetComponent<RegionalParked>())>=2,"Two usable bombers are parked at the air-force base");
            int detonations=FallingBomb.Detonations;Check(sim.Enter(bomber),"Player can enter the bomber cockpit");var bombInput=ControlFrame.Empty;bombInput.secondaryFire=true;sim.Tick(bombInput,.02f);yield return new WaitForSeconds(2);
            camera.transform.SetPositionAndRotation(site+new Vector3(45,95,-75),Quaternion.LookRotation(bomber.transform.position-(site+new Vector3(45,95,-75))));Capture("bomber-release");
            yield return new WaitForSeconds(7);Check(bay.Released==24&&bay.Bombs==72&&FallingBomb.Detonations>=detonations+20,"Bomber salvo releases 24 physical gravity bombs, consumes ammunition and detonates at ground impact");sim.EmergencyExit(bomber);Destroy(bomber.gameObject);game.Player.Respawn(site+Vector3.back*55);yield return null;
            var aiBomber=sim.Spawn(site+new Vector3(-89,25,0),false,(int)CityVehicleType.Bomber);aiBomber.occupied=true;yield return null;aiBomber.speed=40;Set(aiBomber.GetComponent<CraftDynamics>(),"throttle",.7f);aiBomber.GetComponent<BomberBay>().FlyRun(site,.03f);
            Check(aiBomber.GetComponent<BomberBay>().Dropping,"Autonomous bomber computes a ballistic release point and begins its bombing run");Destroy(aiBomber.gameObject);
            var normal=MilitaryResponse.Deployment(true,false);var expanded=MilitaryResponse.Deployment(true,false,true);
            Check(expanded.Count(t=>t==CityVehicleType.Truck)>normal.Count(t=>t==CityVehicleType.Truck)&&expanded.Count(t=>t==CityVehicleType.Tank)==7&&expanded.Count(t=>t==CityVehicleType.CombatHelicopter)==5&&expanded.Count(t=>t==CityVehicleType.Fighter)==3&&expanded.Count(t=>t==CityVehicleType.Bomber)==2,"Erebos dispatch has more infantry carriers, seven tanks, five helicopters, three fighters and two bombers");
            var riftSite=new Vector3(3850,200,-780);Box("Rift spawn diagnostic deck",riftSite-Vector3.up*.5f,new Vector3(240,1,240));game.Player.Respawn(riftSite+Vector3.back*40);yield return null;Physics.SyncTransforms();CityEventGate.Reset();
            var chronicle=CityChronicle.Instance;chronicle.Entry(chronicle.Quests[0]).step=chronicle.Quests[0].steps.Length;var incursion=RiftIncursion.Instance;bool triggered=incursion.Trigger(riftSite);foreach(var creature in FindObjectsByType<RiftCreature>())creature.enabled=false;
            Check(triggered&&incursion.RiftCity&&incursion.CreatureCount>=4,"A single Erebos incident creates a group of at least four separated monsters / triggered="+triggered+", count="+incursion.CreatureCount);typeof(RiftIncursion).GetMethod("End",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(incursion,null);
            // Follow the actual death-menu callback through a complete city reload.
            game.Die();yield return null;choices=(UnityEngine.UI.Button[])Get(game.Hud,"respawnChoices");choices[3].onClick.Invoke();((UnityEngine.UI.Button)Get(game.Hud,"primary")).onClick.Invoke();
            var previous=game;while(GameDirector.Instance==previous||!GameDirector.Instance||!GameDirector.Instance.Ready||RespawnNetwork.Arriving)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.enabled=false;game.CameraRig.enabled=false;
            Check(game.stage==StageId.UrbanCity&&FourCityCatalog.CityAt(game.Player.transform.position)==3&&game.Player.Health==100&&!game.Dead,"Death-menu Nereid selection actually reloads the city and respawns a living player there");
            Check(NpcGroundSupport.Floor(game.Player.transform.position,game.Player.transform.position.y+1,3,out _),"Selected underwater respawn is supported by the facility ground");Capture("nereid-respawn");Finish();
        }
        void Capture(string name)
        {
            var camera=Camera.main;var overlays=FindObjectsByType<Canvas>().Where(c=>c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=.7f;}Canvas.ForceUpdateCanvases();
            var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGBA32,false);RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Destroy(texture);foreach(var c in overlays)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>310){report.errors.Add("Defense mobility probe timed out");Finish();}}
        void Finish(){if(finished)return;finished=true;report.completed=report.errors.Count==0;Save();Application.Quit(report.completed?0:1);}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
