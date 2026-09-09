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
    public sealed class ResponseRenewalSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;GameDirector game;float began;bool finished;readonly List<GameObject> fixtures=new();
        static readonly Vector3 Site=new(1000,200,-800);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-response-renewal-smoke")&&!FindAnyObjectByType<ResponseRenewalSmoke>()){GameDirector.SkipTitle=true;new GameObject("Response renewal essential checks").AddComponent<ResponseRenewalSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;RespawnNetwork.SuppressSave=true;began=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/ResponseRenewal/Native"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(message+"\n"+stack);Save();}}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        void Check(bool yes,string label){(yes?report.passed:report.errors).Add(label);Debug.Log("RESPONSE "+yes+" / "+label);Save();}
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
        WorldActor Person(Vector3 at,float hp=100)
        {
            var go=new GameObject("Medical fixture resident",typeof(SpriteRenderer),typeof(WorldActor),typeof(BoxCollider));fixtures.Add(go);go.layer=9;go.transform.position=at;var a=go.GetComponent<WorldActor>();a.health=hp;
            var col=go.GetComponent<BoxCollider>();col.center=Vector3.up;col.size=new Vector3(.65f,2,.65f);var r=go.GetComponent<SpriteRenderer>();r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");PeopleArt.Attach(go,"CivilianWoman");return a;
        }
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.enabled=false;game.CameraRig.enabled=false;WantedSystem.Clear("");WantedSystem.Instance.enabled=false;CityChronicle.Instance.enabled=false;RiftIncursion.Instance.enabled=false;CivicTerrorEvents.Instance.enabled=false;CitySafety.Instance.enabled=false;
            foreach(var b in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(b is GangStrongholds||b is CityGangWar||b is GangConvoy||b is CityActivityDirector||b is SeaCombat||b is GangCrime||b is GangMember||b is CitySocial)b.enabled=false;
            CityChronicle.Instance.Entry(CityChronicle.Instance.Quests[0]).step=CityChronicle.Instance.Quests[0].steps.Length;CityEventGate.Reset();LifeState.Hours=13;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Response verification ground";floor.transform.position=Site-Vector3.up*.5f;floor.transform.localScale=new Vector3(200,1,160);floor.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Concrete");fixtures.Add(floor);game.Player.Respawn(Site+Vector3.right*65);Set(game.Player,"invincible",0f);yield return null;
            if(Environment.GetCommandLineArgs().Contains("-response-renewal-followup")){yield return Followup();finished=true;report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);yield break;}
            RespawnNetwork.ResetHome();Check(RespawnNetwork.Selected==-1,"Default respawn destination is Seoha's home");RespawnNetwork.Resolve();
            Check(FindObjectsByType<RespawnTerminal>(FindObjectsSortMode.None).Select(x=>x.City).Distinct().Count()==4,"Each of four cities has an interactive respawn facility");
            for(int i=0;i<4;i++){RespawnNetwork.Register(i);Check(RespawnNetwork.Selected==-1&&RespawnNetwork.Points[i]!=Vector3.zero,"Recovery centre keeps home respawn / city "+i);}RespawnNetwork.ResetHome();
            var minor=Person(Site);var critical=Person(Site+Vector3.right*4);var fatal=Person(Site+Vector3.right*8);yield return null;
            minor.Damage(28,Vector3.zero,TrafficDamageSource.Environment);critical.Damage(85,Vector3.zero,TrafficDamageSource.Environment);fatal.Damage(260,Vector3.zero,TrafficDamageSource.Environment);yield return null;
            Check(minor.Alive&&!minor.Downed&&minor.GetComponent<MedicalState>().Grade==InjuryGrade.Wounded,"Moderate damage causes a treatable wound");Check(critical.Alive&&critical.Downed,"Severe survivable damage causes incapacitation");Check(!fatal.Alive,"Overwhelming damage causes immediate death");
            var atlas=Resources.LoadAll<Sprite>("Art/Medical/InjuryLayers");Check(atlas.Length==8&&minor.GetComponent<InjuryPresentation>(),"Eight dedicated injury/treatment sprite layers preserve the person's original art");
            yield return View("TriageSprites",Site+new Vector3(3,3,-10),Site+new Vector3(3,1,0));
            Check(minor.GetComponent<MedicalState>().FirstAid()&&!minor.GetComponent<MedicalState>().NeedsRescue,"Light injury is treated on site without transport");
            var gang=GangMember.Create(Site+new Vector3(4,0,-10),0,0);fixtures.Add(gang.gameObject);gang.enabled=true;gang.GoTo(critical.transform.position);CityEventGate.Reset();CityEventGate.Begin(gang,CityEventKind.Gang);CityEventGate.Enroll(gang.Body);
            yield return new WaitForSeconds(4);Check(!critical.Alive&&gang.ShotsFired>0,"Gang gunfire can finish an incapacitated victim");Destroy(gang.gameObject);yield return null;CityEventGate.Reset();
            var host=Person(Site+Vector3.forward*40,1000);yield return null;
            Check(CityEventGate.Begin(host,CityEventKind.Monster),"Monster event reserves the encounter slot");CityEventGate.Enroll(host);
            Check(!CivicTerrorEvents.Instance.Spawn(Site)&&!GangConvoy.Dispatch(Site),"A live monster event blocks terror and gang spawners");host.health=0;yield return new WaitForSeconds(.5f);CityEventGate.Refresh();yield return new WaitForSeconds(8.6f);Check(!CityEventGate.Busy,"Encounter slot releases when the last threat is neutralized");
            CityEventGate.Begin(this,CityEventKind.Terror);Check(!RiftIncursion.Instance.Trigger(Site),"Terror event blocks monster arrival");CityEventGate.Reset();
            var cop=PoliceOfficer.Create(WantedSystem.Instance,Site+new Vector3(-25,0,22),3,2);fixtures.Add(cop.gameObject);cop.Ambient=true;
            gang=GangMember.Create(Site+new Vector3(-25,0,36),1,0);fixtures.Add(gang.gameObject);gang.enabled=false;cop.Dispatch(gang.Body);yield return new WaitForSeconds(4.5f);
            Check(cop.ShotsFired>0&&gang.Body.health<180,"Dispatched police stop to aim and damage gang members");
            Check(cop.GetComponent<DirectionalPerson>().art.StartsWith("Legacy")&&PeopleArt.Get("LegacySwat",0)&&PeopleArt.Get("Swat",0),"Legacy police/SWAT art deploys alongside cyber units");Destroy(cop.gameObject);Destroy(gang.gameObject);yield return null;CityEventGate.Reset();
            var robot=CombatRobot.Create(Site+new Vector3(30,0,0),false);fixtures.Add(robot.gameObject);game.Player.Respawn(Site+new Vector3(46,0,0));Set(game.Player,"invincible",0f);yield return null;robot.Body.Damage(12,Vector3.right*60);CivilianImpact.Launch(robot.Body,Vector3.right,30);float health=game.Player.Health;yield return new WaitForSeconds(2);
            Check(robot.Body.MaxHealth>=6000&&!robot.GetComponent<CivilianImpact>(),"Robot has vehicle-scale armor and rejects civilian launch physics");Check(robot.Shots>0&&game.Player.Health<health,"Combat robot retaliates against its attacker");robot.enabled=false;game.Player.Heal(100);game.Player.Respawn(Site+Vector3.right*65);
            yield return Vehicles();yield return Rescue();yield return HeavyAttack();
            foreach(string cue in new[]{"impact_metal","impact_flesh","impact_glass","impact_concrete","blast_pressure","tail_pistol","tail_rifle","tail_shotgun","titan_barrage"})Check(game.Audio.HasCue(cue),"Combat sound layer available / "+cue);
            game.Audio.PlayGun(GunshotKind.Rifle,game.Player.Muzzle);Check(CombatVfx.Muzzles>0&&Camera.main.GetComponent<CombatCameraImpulse>(),"Gunfire produces muzzle layers and camera recoil");
            foreach(var f in fixtures)if(f)Destroy(f);yield return null;CityEventGate.Reset();RespawnNetwork.ResetHome();game.Retry();
            float deadline=Time.realtimeSinceStartup+70;while((!GameDirector.Instance||GameDirector.Instance.stage!=StageId.Residence||!GameDirector.Instance.Ready)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(GameDirector.Instance&&GameDirector.Instance.stage==StageId.Residence&&(GameDirector.Instance.Player.transform.position-CompactHome.Spawn).sqrMagnitude<16,"Actual retry loads Seoha's home at its safe spawn");
            finished=true;report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);
        }
        IEnumerator Followup()
        {
            if(Environment.GetCommandLineArgs().Contains("-response-renewal-final")){yield return Vehicles();yield return HeavyAttack();yield break;}
            var host=Person(Site+Vector3.forward*40,1000);yield return null;
            CityEventGate.Reset();CityEventGate.Begin(host,CityEventKind.Monster);CityEventGate.Enroll(host);
            Check(!CivicTerrorEvents.Instance.Spawn(Site)&&!GangConvoy.Dispatch(Site),"Live event excludes other incident types");
            host.health=0;float until=Time.time+10;
            while(Time.time<until){CityEventGate.Refresh();yield return null;}
            Debug.Log("RESPONSE gate release / "+CityEventGate.Diagnostic);
            Check(!CityEventGate.Busy,"Encounter slot releases when the last threat is neutralized");
            CityEventGate.Reset();Destroy(host.gameObject);
            var cop=PoliceOfficer.Create(WantedSystem.Instance,Site+new Vector3(-25,0,22),3,2);fixtures.Add(cop.gameObject);cop.Ambient=true;cop.enabled=false;yield return null;
            Check(cop.GetComponent<DirectionalPerson>().art.StartsWith("Legacy")&&PeopleArt.Get("LegacySwat",0)!=PeopleArt.Get("Swat",0),"Legacy police/SWAT art deploys alongside cyber units");
            yield return Vehicles();
            var patient=Person(Site+Vector3.forward*30);yield return null;patient.Damage(85,Vector3.zero,TrafficDamageSource.Environment);
            var a=EmergencyAmbulance.Create(patient,null);yield return null;
            if(a)
            {
                a.Car.transform.position=Site+new Vector3(-9,0,30);a.enabled=false;yield return new WaitForSeconds(.3f);
                a.Car.Damage(12,a.transform.position,TrafficDamageSource.Environment);yield return null;
                Check(a.Car.occupied&&a.Car.GetComponent<VehicleCabin>().IdentityAt(0)=="Doctor","Ambulance crew stays aboard during minor collisions");
                Check(a.GetComponentsInChildren<Light>().Any(l=>l.transform.localPosition.y>3.2f),"Ambulance response lights sit above the patient compartment");
                yield return View("AmbulanceFinal",a.transform.position+new Vector3(11,5,-12),a.transform.position+Vector3.up*1.5f);
            }
        }
        IEnumerator Vehicles()
        {
            var car=UrbanSimulation.Instance.Spawn(Site+new Vector3(-40,0,-20),false,3);car.occupied=true;fixtures.Add(car.gameObject);SecurityVehicleArt.Install(car,false);yield return new WaitForSeconds(.3f);
            var model=car.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.Contains("MilitaryCarrier"));Check(model,"Dedicated security carrier model loads");
            var victim=Person(Site+new Vector3(-15,0,-20));var source=new GameObject("Empty turret fixture",typeof(WorldActor));source.transform.SetParent(car.transform,false);source.GetComponent<WorldActor>().military=true;yield return null;car.occupied=false;float hp=victim.health;
            FactionCombat.Fire(source.GetComponent<WorldActor>(),car.transform.position+Vector3.up*3,victim.Center,60,30,SignalEffects.Gold,false,car.transform);Check(Mathf.Approximately(hp,victim.health),"An empty vehicle cannot fire its mounted gun");
            car.occupied=true;car.transform.rotation=Quaternion.identity;var blocker=UrbanSimulation.Instance.Spawn(car.transform.position+Vector3.right*11,false,0);fixtures.Add(blocker.gameObject);blocker.traffic=true;yield return null;Physics.SyncTransforms();
            var steer=EmergencyTraffic.Steer(car,Vector3.right);Debug.Log($"RESPONSE bypass from={car.transform.position} blocker={blocker.transform.position} direction={steer} yield={blocker.GetComponent<TrafficYield>()!=null}");Check(Mathf.Abs(steer.z)>.05f&&blocker.GetComponent<TrafficYield>(),"Emergency driver chooses a bypass and asks traffic to yield");
            var drive=new ResponseDrive();float at=car.transform.position.x;for(int i=0;i<180;i++){drive.Drive(car,Site+new Vector3(20,0,-20),.03f,3);yield return null;}Debug.Log($"RESPONSE drive fromX={at} end={car.transform.position} speed={car.speed}");Check(car.transform.position.x>at+13,"Dispatch vehicle passes a stationary traffic obstruction");
            yield return View("ResponseVehicles",Site+new Vector3(-16,6,-33),car.transform.position+Vector3.up);Destroy(car.gameObject);Destroy(blocker.gameObject);Destroy(victim.gameObject);
        }
        IEnumerator Rescue()
        {
            var safety=CitySafety.Instance;var witness=Person(Site+new Vector3(0,0,28));var patients=new List<WorldActor>();for(int i=0;i<7;i++)patients.Add(Person(Site+new Vector3(i*3,0,30)));yield return null;
            foreach(var p in patients)p.Damage(85,Vector3.zero,TrafficDamageSource.Environment);safety.enabled=true;yield return new WaitForSeconds(6.5f);
            Check(patients.All(p=>p.GetComponent<MedicalState>().Reported)&&safety.ActiveAmbulances==5,"Witness reports queue casualties behind the five-ambulance dispatch cap");
            Check(CitySafety.PlayerReport()>0,"Player can request medical rescue");safety.enabled=false;
            foreach(var ambulance in FindObjectsByType<EmergencyAmbulance>(FindObjectsSortMode.None))Destroy(ambulance.gameObject);yield return null;
            var patient=patients[0];var a=EmergencyAmbulance.Create(patient,null);Check(a,"Ambulance assigns a live critical patient");if(!a)yield break;
            a.Car.transform.position=Site+new Vector3(-9,0,30);a.Car.transform.rotation=Quaternion.identity;yield return new WaitForSeconds(.5f);
            Check(a.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("RearDoor")),"Dedicated ambulance has animated rear patient doors");
            float deadline=Time.time+22;while(a&&a.Phase<4&&Time.time<deadline)yield return null;
            Check(a&&a.Phase>=3&&patient.GetComponent<MedicalState>().Stabilized,"Critical patient receives first aid and is loaded for hospital transport");
            yield return View("AmbulanceAndTriage",a.transform.position+new Vector3(11,5,-12),a.transform.position+Vector3.up*1.5f);
            if(a){Set(a,"hospital",a.transform.position);yield return new WaitForSeconds(1);Check(!patient.GetComponent<MedicalState>().NeedsRescue,"Hospital arrival restores critical patient's health");Destroy(a.gameObject);}foreach(var p in patients)Destroy(p.gameObject);Destroy(witness.gameObject);
        }
        IEnumerator HeavyAttack()
        {
            var near=Person(Site+new Vector3(4,0,-42));var edge=Person(Site+new Vector3(15,0,-42));yield return null;
            BlastDamage.Create(Site+new Vector3(0,0,-42),18,240,TrafficDamageSource.Environment,null,BlastPayload.ArmourPiercing);yield return new WaitForSeconds(.15f);
            Check(!near.Alive&&edge.Alive&&edge.GetComponent<MedicalState>().NeedsRescue,"Tank blast kills close targets while edge splash leaves a survivor");Destroy(near.gameObject);Destroy(edge.gameObject);
            var titan=RiftCreature.Create(Site+new Vector3(-40,0,0),1);titan.enabled=false;fixtures.Add(titan.gameObject);var target=Person(Site+new Vector3(-25,0,0));var car=UrbanSimulation.Instance.Spawn(Site+new Vector3(-24,0,6),false,0);fixtures.Add(car.gameObject);yield return new WaitForSeconds(.3f);float hp=car.health;TitanBarrage.Fire(titan);yield return new WaitForSeconds(.4f);
            Check(!target.Alive&&car.health<hp,"Radial titan barrage burns nearby people and vehicles");yield return View("TitanBarrage",Site+new Vector3(-66,18,-30),titan.AimCenter);Destroy(titan.gameObject);Destroy(target.gameObject);Destroy(car.gameObject);
        }
        IEnumerator View(string name,Vector3 eye,Vector3 target)
        {
            var cam=Camera.main;var impulse=cam.GetComponent<CombatCameraImpulse>();if(impulse)impulse.enabled=false;cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));cam.fieldOfView=60;yield return new WaitForSeconds(.25f);
            var rt=new RenderTexture(1280,720,24);rt.Create();for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});yield return null;}var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(tex);if(impulse)impulse.enabled=true;
        }
        void Update(){CityEventGate.Refresh();if(!finished&&Time.realtimeSinceStartup-began>360){finished=true;report.errors.Add("Essential check timeout");Save();Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=Log;RespawnNetwork.SuppressSave=false;}
    }
}
