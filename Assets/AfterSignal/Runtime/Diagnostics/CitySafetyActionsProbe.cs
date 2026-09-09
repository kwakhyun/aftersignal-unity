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
    public sealed class CitySafetyActionsProbe:MonoBehaviour
    {
        [Serializable] sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();GameDirector game;float began;bool finished;
        const string Output="Artifacts/CitySafetyActions/Native";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(!Environment.GetCommandLineArgs().Contains("-city-safety-actions-probe")||FindAnyObjectByType<CitySafetyActionsProbe>())return;GameDirector.SkipTitle=true;new GameObject("City safety actions essentials").AddComponent<CitySafetyActionsProbe>();}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;Directory.CreateDirectory(Output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string line,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(line+"\n"+stack);Save();}}
        void Check(bool ok,string text){(ok?report.passed:report.errors).Add(text);Debug.Log("CITY SAFETY "+ok+" / "+text);Save();}
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
            foreach(var battle in FindObjectsByType<CampaignBattle>())Destroy(battle.gameObject);
            WantedSystem.Clear("");CityEventGate.Reset();game.Player.Respawn(new Vector3(300,1,0));yield return null;
            Set(game.Player,"<Health>k__BackingField",50f);Set(game.Player,"recoveryWait",8f);game.Player.TickRecovery(7);
            Check(game.Player.Health==50,"Natural recovery waits for eight seconds without damage");game.Player.TickRecovery(1);game.Player.TickRecovery(1);
            Check(game.Player.Health>50&&game.Player.Health<=55.01f,"Recovery restores 2.5 HP per second and does not refill instantly");
            Set(game.Player,"invincible",0f);game.Player.ReceiveDamage(5,game.Player.transform.position-Vector3.right);float hp=game.Player.Health;game.Player.TickRecovery(2);Check(game.Player.Health==hp,"Taking damage resets the recovery delay");
            game.SetPaused(true);game.Player.TickRecovery(40);Check(game.Player.Health==hp,"Paused time does not heal the player");game.SetPaused(false);game.Player.Heal(100);
            var sim=UrbanSimulation.Instance;var car=sim.Spawn(new Vector3(300,.2f,0),false,0);car.occupied=true;car.speed=7;
            var victim=Citizen("CivilianMan",car.transform.position+car.Forward*2);yield return null;
            CityPopulation.Instance.VehicleSweep(car,car.transform.position,car.transform.position+car.Forward*2,7);
            var offense=car.GetComponent<TrafficOffense>();Check(offense&&victim.health<70&&WantedSystem.Level==0,"An NPC vehicle pedestrian collision creates its own offense without blaming Seoha");
            yield return new WaitForSeconds(5);
            Check(offense&&offense.Reported&&offense.Pursuers>0,"Reported NPC traffic accident dispatches pursuing police cars");
            if(offense)
            {
                car.traffic=false;car.speed=0;var cops=(List<PoliceCar>)Get(offense,"patrols");
                foreach(var cop in cops)if(cop){cop.enabled=false;cop.transform.position=car.transform.position-Vector3.forward*15;cop.Vehicle.speed=0;}
                yield return new WaitForSeconds(4.5f);Check(offense.Resolved&&!car.occupied&&!car.traffic,"Police intercept and stop the suspect vehicle and its driver gets out");
                foreach(var cop in cops)if(cop)Destroy(cop.gameObject);
            }
            Destroy(victim.gameObject);Destroy(car.gameObject);
            var site=new Vector3(1000,200,-800);var floor=Box("Diagnostic floor",site-Vector3.up*.5f,new Vector3(190,1,160));game.Player.Respawn(site+Vector3.back*40);yield return null;
            var tower=new GameObject("Facade regression tower");tower.transform.position=site+Vector3.right*55;var geo=new CityGeometry(tower.transform);DistrictTower.Build(geo,30,24,48,1);geo.Finish();Physics.SyncTransforms();
            var front=tower.transform.position+new Vector3(0,1,-20);
            Check(Physics.Raycast(front,Vector3.forward,out var wall,15,1)&&wall.collider.transform.IsChildOf(tower.transform)&&wall.distance<9,"Facade collider reaches the visible exterior rather than a narrow internal core");
            Check(!PedestrianGround.Step(tower.transform.position+new Vector3(0,0,-14),tower.transform.position+new Vector3(0,0,-10),out _,.85f),"NPC path steps cannot pass through the building facade");
            game.Player.Respawn(tower.transform.position+new Vector3(0,.1f,-15));game.Player.Controller.Move(Vector3.forward*10);
            Check(game.Player.transform.position.z<tower.transform.position.z-12,"Player controller is blocked by the actual facade");
            Check(!Physics.Linecast(tower.transform.position+new Vector3(0,13,-20),tower.transform.position+new Vector3(0,13,20),1),"Separate upper towers retain their open central gap");Destroy(tower);game.Player.Respawn(site+Vector3.back*40);yield return null;
            float shore=OceanLife.Shore(600)-3.5f;var shoreWater=new Vector3(600,-5,shore-.25f);game.Player.Respawn(shoreWater);yield return null;
            var stopped=ShoreAccess.Constrain(game.Player,Vector3.forward*1.5f);Check(Mathf.Abs(stopped.z)<.001f,"Diving cannot move underneath a land column");
            game.Player.Respawn(new Vector3(600,OceanLife.Surface-.15f,shore-.1f));var coast=Box("Shore landing fixture",new Vector3(600,-.15f,shore+3),new Vector3(8,.3f,8));Physics.SyncTransforms();ShoreAccess.Constrain(game.Player,Vector3.forward*.8f);
            Check(game.Player.transform.position.y>OceanLife.Surface+.5f,"A surface swimmer climbs onto a supported clear shoreline");
            game.Player.Respawn(new Vector3(600,-4,shore+2));Check(ShoreAccess.Recover(game.Player)&&game.Player.transform.position.y>=0,"Existing under-land positions recover onto safe ground");Destroy(coast);
            var deep=FourCityCatalog.Centers[3]+Vector3.up*2;game.Player.Respawn(deep);yield return null;
            Check(FourCityCatalog.Dry(deep)&&ShoreAccess.Constrain(game.Player,Vector3.right)==Vector3.right,"Authored underwater-city dry space remains accessible");
            game.Player.Respawn(site+Vector3.back*40);yield return null;
            var soldier=Citizen("Soldier",site+Vector3.left*25);soldier.military=true;var start=soldier.transform.position;CivilianImpact.Blast(soldier,Vector3.right,40);yield return new WaitForSeconds(3);
            Check(Vector3.ProjectOnPlane(soldier.transform.position-start,Vector3.up).magnitude<9,"Military blast knockback stays within a short bounded distance");Destroy(soldier.gameObject);
            var manifest=MilitaryResponse.Deployment(true,false);Check(manifest.Count(k=>k==CityVehicleType.Tank)==4&&manifest.Count(k=>k==CityVehicleType.CombatHelicopter)==3,"Monster response deploys four tanks and three combat helicopters in staggered waves");
            var titan=RiftCreature.Create(site,0);titan.enabled=false;var heli=sim.Spawn(site+new Vector3(65,60,0),false,(int)CityVehicleType.CombatHelicopter);heli.occupied=true;
            var jet=sim.Spawn(site+new Vector3(-60,75,0),false,(int)CityVehicleType.Fighter);jet.occupied=false;
            Check(!TitanGravitySnare.Eligible(jet,titan),"Gravity attack ignores empty aircraft");jet.occupied=true;Check(TitanGravitySnare.Eligible(jet,titan),"Gravity attack accepts occupied fighter jets as well as helicopters");
            var groundAttacker=Citizen("Soldier",site+Vector3.back*20);groundAttacker.military=true;titan.Attacked(groundAttacker,400);
            Set(titan,"gravityAt",0f);Set(titan,"barrageAt",99f);Set(titan,"laserAt",99f);Set(titan,"cooldown",99f);titan.enabled=true;
            yield return new WaitForSeconds(.4f);Check(titan.PriorityTarget==groundAttacker&&titan.AttackWarning.Contains("중력"),"Ground attackers retain aggro while the monster automatically warns of an aircraft gravity capture");
            float initial=Vector3.Distance(heli.transform.position,titan.AimCenter);yield return new WaitForSeconds(3.5f);var snare=heli?heli.GetComponent<TitanGravitySnare>():null;
            Check(snare&&Vector3.Distance(heli.transform.position,titan.AimCenter)<initial-15,"Gravity skill visibly draws an occupied helicopter toward the monster");
            Camera.main.transform.SetPositionAndRotation(site+new Vector3(5,28,-85),Quaternion.LookRotation(site+new Vector3(30,18,0)-(site+new Vector3(5,28,-85))));Capture("gravity-capture");
            yield return new WaitForSeconds(4);Check(!heli||heli.Wrecked&&heli.GetComponent<VehicleFailure>(),"Captured aircraft lose power and enter the existing physical crash sequence");
            var release=TitanGravitySnare.Begin(titan,jet);titan.Body.health=0;yield return null;yield return null;Check(!jet.GetComponent<TitanGravitySnare>()&&!jet.Wrecked,"Destroying the monster cancels a pending gravity capture without killing the aircraft");Destroy(jet.gameObject);if(heli)Destroy(heli.gameObject);Destroy(titan.gameObject);Destroy(groundAttacker.gameObject);yield return null;
            var target=Box("Fire rescue fixture",site+Vector3.up*3,new Vector3(6,6,6));var fire=CityFireService.Ignite(target,site+Vector3.up,1);
            var patient=Citizen("CivilianWoman",site+new Vector3(-7,.1f,-2));patient.Damage(60,Vector3.zero,TrafficDamageSource.Environment);var injury=patient.GetComponent<MedicalState>();
            var engine=FireEngine.Create(fire,site+new Vector3(-15,0,-15),site+new Vector3(-40,0,-40));fire.Assigned=engine;yield return new WaitForSeconds(1);
            var rescuers=FindObjectsByType<Firefighter>().Where(f=>(f.transform.position-site).sqrMagnitude<100*100).ToArray();
            Check(engine.Phase==1&&rescuers.Length==2&&injury&&injury.Incapacitated,"Arriving fire engine deploys hose and rescue specialists beside a critical casualty");
            float until=Time.time+32;bool rescued=false,carried=false;
            while(Time.time<until)
            {
                var pending=patient?patient.GetComponent<MedicalPending>():null;carried|=pending&&pending.carried;
                rescued=rescuers.Any(r=>r&&r.GetComponent<FireRescue>().Rescued>0);
                if(carried&& !File.Exists(Path.Combine(Output,"fire-rescue.png"))){Camera.main.transform.SetPositionAndRotation(site+new Vector3(-35,12,-35),Quaternion.LookRotation(site-(site+new Vector3(-35,12,-35))));Capture("fire-rescue");}
                if(rescued&&!fire)break;yield return null;
            }
            Check(carried&&rescued&&patient&&patient.Alive&&(patient.transform.position-site).magnitude>20&&injury.Stabilized&&injury.Reported,"Firefighters carry a critical citizen out of danger, stabilize and report for ambulance handover");
            Check(!fire&&CityFireService.EngineLimit==5,"Autonomous hose and roof cannon suppress the fire while the five-engine limit remains");
            if(engine)Destroy(engine.gameObject);Destroy(target);if(patient)Destroy(patient.gameObject);
            foreach(var name in new[]{"서하","노아","해진"})
            {
                var portrait=StoryPortraits.Get(name);var pixels=portrait.texture.GetPixels32();int transparent=pixels.Count(p=>p.a<10);int dark=pixels.Count(p=>p.a>250&&p.r<70&&p.g<80&&p.b<100);
                Check(transparent>pixels.Length/8&&dark>pixels.Length/100,name+" portrait has actual transparent background and opaque dark clothing");
            }
            game.ShowDialogue("서하","다친 사람은 소방대가 안전한 곳으로 옮겼어.\n경찰과 구급대도 현장에 도착했대.");yield return new WaitForSeconds(.2f);Capture("transparent-dialogue");game.CloseDialogue();Finish();
        }
        void Capture(string name)
        {
            var camera=Camera.main;var overlays=FindObjectsByType<Canvas>().Where(c=>c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=.7f;}Canvas.ForceUpdateCanvases();
            var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGBA32,false);RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Destroy(texture);foreach(var c in overlays)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>190){report.errors.Add("City safety probe timed out");Finish();}}
        void Finish(){if(finished)return;finished=true;report.completed=report.errors.Count==0;Save();Application.Quit(report.completed?0:1);}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
