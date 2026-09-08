using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class TrafficJusticeSmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float start,jail;int site;bool hadJail,done,protect;GameDirector game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-traffic-justice-smoke")&&!FindAnyObjectByType<TrafficJusticeSmoke>()){GameDirector.SkipTitle=true;new GameObject("Traffic and justice verification").AddComponent<TrafficJusticeSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");site=UrbanCatalog.Current;PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");start=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/TrafficJustice/Final"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(message+"\n"+trace);}
        void Check(bool ok,string title){(ok?report.passed:report.errors).Add(title);Debug.Log("TRAFFIC JUSTICE "+ok+" / "+title);Save();}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        IEnumerator Load(StageId stage,int venue=0)
        {
            Time.timeScale=1;PlayerPrefs.SetInt(UrbanCatalog.Prefix+"Site",venue);ResidentialWorld.VisitHome=-1;CivicWorld.ClearArrival();
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(stage));while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            game=GameDirector.Instance;game.CloseDialogue();game.SetPaused(false);game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CameraRig.enabled=false;game.enabled=false;
            WantedSystem.Clear("");WantedSystem.Instance.enabled=false;
            if(stage==StageId.UrbanCity){while(!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;foreach(var x in FindObjectsByType<RiftIncursion>())x.enabled=false;}
            yield return new WaitForSeconds(2.2f);
        }
        IEnumerator Start()
        {
            UnityEngine.Random.InitState(91237);yield return Load(StageId.UrbanCity);
            var sim=UrbanSimulation.Instance;var at=new Vector3(1730,.15f,780);game.Player.Respawn(at+Vector3.back*8);game.Player.enabled=false;
            var car=sim.Spawn(at,false,0);yield return null;
            float initial=car.health;car.CollisionDamage(7,at);Check(car.health>initial*.99f&&!car.Wrecked,"Light collision causes less than one percent body damage");
            car.CollisionDamage(48,at);Check(car.health>initial*.9f&&!car.Wrecked,"One severe road crash cannot explode a healthy car");
            for(int i=0;i<5;i++)car.CollisionDamage(48,at);yield return new WaitForSeconds(.4f);
            Check(!car.Wrecked&&car.GetComponent<VehicleDamagePresentation>().Stage==2,"Repeated strong collisions produce smoke before destruction");
            car.Damage(car.health-car.MaxHealth*.2f,at,null,false);yield return new WaitForSeconds(.4f);
            Check(car.GetComponent<VehicleDamagePresentation>().Stage==3&&car.GetComponentsInChildren<ParticleSystem>().Any(p=>p.name=="Engine fire"),"Low hull health enables persistent engine fire");
            Camera.main.transform.SetPositionAndRotation(at+new Vector3(6,3,-7),Quaternion.LookRotation(at+Vector3.up-(at+new Vector3(6,3,-7))));yield return Capture("damaged-car");car.Repair();
            Check(car.health==car.MaxHealth,"Repair restores the type-specific hull capacity");
            sim.Enter(car);float hp=game.Player.Health;car.CollisionDamage(45,at);Check(game.Player.Health>=hp-12&&game.Player.Health>0,"A belted driver receives a bounded severe-collision injury");
            protect=true;var dummy=Actor(at+Vector3.right*25);dummy.health=500;
            Camera.main.transform.position=at+new Vector3(-7,3,-2);Camera.main.transform.LookAt(dummy.Center);
            game.Player.TickMountedCombat(new ControlFrame{weapon=2},.02f);
            int shots=game.Player.ShotsFired;for(int i=0;i<30;i++)game.Player.TickMountedCombat(new ControlFrame{weapon=-1,attack=true},.02f);
            Check(game.Player.ShotsFired>shots&&dummy.health<500,"Mounted pistol consumes ammunition and hits a target beyond the own vehicle");
            game.CameraRig.ToggleVehicleView();Camera.main.transform.position=game.CameraRig.CockpitPosition(car);Camera.main.transform.rotation=Quaternion.LookRotation(car.Forward);yield return new WaitForSeconds(.2f);yield return Capture("cockpit");
            Check(car.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="AfterSignal/Structural Glass"&&r.sharedMaterial.GetColor("_BaseColor").a<.04f),"First-person vehicle glass has a transparent bounded material");
            Check(car.GetComponent<VehicleHorn>().Honk()&&car.GetComponent<VehicleHorn>().Honks==1,"Vehicle horn produces a playback event");
            sim.Exit();WantedSystem.Clear("");Destroy(dummy.gameObject);
            var bike=sim.Spawn(at+Vector3.back*12,false,4);yield return null;sim.Enter(bike);shots=game.Player.ShotsFired;game.Player.TickMountedCombat(new ControlFrame{weapon=-1,attack=true},.5f);Check(game.Player.ShotsFired>shots,"Motorcycle rider can fire a pistol");sim.Exit();
            var plane=sim.Spawn(at+new Vector3(80,100,0),false,7);yield return null;Check(plane.MaxHealth>=initial*15,"Airliner has substantially greater durability than a car");float y=plane.transform.position.y;plane.speed=60;plane.Damage(plane.MaxHealth*2,plane.transform.position);yield return new WaitForSeconds(1.2f);
            Check(plane&&plane.GetComponent<VehicleFailure>()&&!plane.GetComponent<VehicleFailure>().Sinking&&plane.transform.position.y<y&&plane.Explosions==0,"Disabled flying airliner falls before impact detonation");
            var boat=sim.Spawn(new Vector3(1730,OceanLife.Surface,-1200),false,6);yield return null;y=boat.transform.position.y;boat.Damage(boat.MaxHealth*2,boat.transform.position);yield return new WaitForSeconds(2);
            Check(boat&&boat.GetComponent<VehicleFailure>().Sinking&&boat.transform.position.y<y&&boat.Explosions==0,"Disabled boat progressively sinks instead of immediately shattering");
            var cargo=sim.Cars.FirstOrDefault(c=>c&&c.GetComponent<AuthoredCraft>());Check(cargo&&cargo.MaxHealth>=50000,"Container ship receives fifty-thousand hull capacity");
            Check(sim.Cars.Count(c=>c&&c.name=="군부대 대기 전차")>=8,"Military installation contains an expanded parked tank fleet");
            var patrolHeli=PoliceHelicopter.Create(WantedSystem.Instance,at+Vector3.up*90);patrolHeli.Body.health=800;yield return new WaitForSeconds(.2f);
            Check(patrolHeli.GetComponentsInChildren<ParticleSystem>().Count(p=>p.name=="Engine smoke"||p.name=="Engine fire")==2,"Damaged police helicopter emits staged smoke and fire before crashing");Destroy(patrolHeli.gameObject);
            var isolated=new Vector3(7000,600,7000);var witnessFloor=GameObject.CreatePrimitive(PrimitiveType.Cube);witnessFloor.name="Temporary witness fixture floor";witnessFloor.transform.position=isolated-Vector3.up*.4f;witnessFloor.transform.localScale=new Vector3(40,.8f,40);game.Player.Respawn(isolated);game.Player.Controller.enabled=false;
            WantedSystem.Clear("");CrimeObservation.Observe(20,isolated);yield return new WaitForSeconds(.1f);Check(WantedSystem.Level==0&&CrimeObservation.Instance.PendingCalls==0,"Unwitnessed crime does not create a wanted level");
            var witness=Actor(isolated+Vector3.right*5);CrimeObservation.Observe(12,isolated);Check(WantedSystem.Level==0&&CrimeObservation.Instance.PendingCalls>0,"Civilian witness starts a delayed report");witness.health=0;yield return new WaitForSeconds(5.2f);Check(WantedSystem.Level==0,"Incapacitated witness cannot complete a pending report");Destroy(witness.gameObject);
            witness=Actor(isolated+Vector3.right*5);CrimeObservation.Observe(12,isolated);yield return new WaitForSeconds(5.2f);Check(WantedSystem.Level>0,"Living witness completes the call and creates a wanted level");WantedSystem.Clear("");witness.police=true;CrimeObservation.Observe(12,isolated);Check(WantedSystem.Level>0,"Direct police observation reports the crime immediately");Destroy(witness.gameObject);WantedSystem.Clear("");
            var accident=sim.Spawn(isolated+Vector3.forward*10,false,0);yield return null;accident.health=1;
            var bystander=Actor(accident.transform.position+Vector3.right*4);bystander.health=1000;bystander.police=true;
            accident.CollisionDamage(48,accident.transform.position);yield return new WaitForSeconds(.2f);
            Check(accident.Explosions==1&&bystander.health<1000&&WantedSystem.Level==0&&CrimeObservation.Instance.PendingCalls==0,"NPC collision explosion causes collateral injury without blaming the player");Destroy(bystander.gameObject);
            game.Player.Respawn(at+new Vector3(20,0,30));WantedSystem.ConfirmReport(85,game.Player.transform.position);
            var tactical=TacticalTransport.Create(WantedSystem.Instance,at,4);yield return new WaitForSeconds(22);
            Check(tactical&&tactical.Deployed==4,"Four tactical officers deploy sequentially from an armoured vehicle rear");
            Check(tactical&&tactical.GetComponent<ResponseLightbar>(),"Tactical response uses dedicated armour and red-blue lights");
            Camera.main.transform.position=at+new Vector3(-11,5,-11);Camera.main.transform.LookAt(at+Vector3.up);yield return Capture("tactical-deployment");WantedSystem.Clear("");
            var police=PoliceCar.Create(WantedSystem.Instance,at+Vector3.right*12);yield return null;Check(police.GetComponentsInChildren<TextMesh>().Length==0&&police.GetComponent<ResponseLightbar>(),"Patrol car removes floating text and uses red-blue lightbar");police.Withdraw();
            var officerWitness=Actor(game.Player.transform.position+Vector3.right*4);officerWitness.police=true;for(int i=0;i<8;i++)CrimeObservation.Observe(24,game.Player.transform.position,null,true);yield return new WaitForSeconds(14);
            var response=game.GetComponent<MilitaryResponse>();Check(response&&response.VehicleCount==5,"Observed mass casualties dispatch trucks, tank, combat helicopter and fighter");WantedSystem.Clear("");Destroy(officerWitness.gameObject);
            game.Player.Respawn(at+Vector3.forward*50);WantedSystem.ConfirmReport(12,game.Player.transform.position);
            var arrestor=PoliceOfficer.Create(WantedSystem.Instance,game.Player.transform.position+Vector3.right*1.1f,1,0);yield return new WaitForSeconds(3.5f);
            Check(PrisonSystem.Instance&&PrisonSystem.Instance.Jailed,"Close police restraint completes a live arrest and transfers the player to prison");
            if(PrisonSystem.Instance&&PrisonSystem.Instance.Jailed)PrisonSystem.Instance.Release(false);Destroy(arrestor.gameObject);WantedSystem.Clear("");
            yield return Load(StageId.UrbanInterior,3);var vault=CashLocations.Instance.Counter;Check(vault&&vault.Bank&&vault.GetComponent<InteractionPoint>(),"Bank creates a physical interactive cash vault");
            if(vault){game.Player.Respawn(vault.transform.position+Vector3.back*3);int before=LifeState.Credits;vault.StartRobbery();yield return new WaitForSeconds(12.4f);Check(vault.LastLoot>=8000&&LifeState.Credits>=before+8000&&vault.Empty,"Bank vault robbery awards cash once and empties the vault");Camera.main.transform.position=vault.transform.position+new Vector3(4,3,-7);Camera.main.transform.LookAt(vault.transform.position+Vector3.up);yield return Capture("bank-vault");}
            yield return Load(StageId.Clinic);Check(CashLocations.Instance.Counter&&!CashLocations.Instance.Counter.Bank,"Hospital provides an interactive cash counter");
            yield return Load(StageId.UrbanInterior,1);FacilitySecurity.Alert(game.Player.transform.position);Check(FindObjectsByType<StationDefender>().Length>1,"Police station staff are registered for armed defence");
            yield return Load(StageId.Residence);var safe=FindObjectsByType<CashContainer>().FirstOrDefault(c=>c.Home);Check(safe,"Residence creates Seoha's personal safe");
            int cash=LifeState.Credits,stored=LifeState.HomeCash;bool deposited=LifeState.StoreHomeCash(500),withdrawn=LifeState.TakeHomeCash(500);Check(deposited&&withdrawn&&LifeState.Credits==cash&&LifeState.HomeCash==stored,"Home safe deposit and withdrawal conserve total money");
            WantedSystem.ConfirmReport(25,game.Player.transform.position);game.Player.ReceiveDamage(1000,game.Player.transform.position,true);Check(game.Dead&&!(PrisonSystem.Instance&&PrisonSystem.Instance.Jailed)&&!PrisonSystem.Capture(game),"Death while wanted does not count as capture or enter prison");
            Finish();
        }
        WorldActor Actor(Vector3 at){var go=new GameObject("Verification witness",typeof(WorldActor),typeof(BoxCollider));go.layer=9;go.transform.position=at;var col=go.GetComponent<BoxCollider>();col.center=Vector3.up;col.size=new Vector3(.65f,2,.65f);return go.GetComponent<WorldActor>();}
        IEnumerator Capture(string name)
        {
            var rt=RenderTexture.GetTemporary(1280,720,24);for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(Camera.main,new RenderPipeline.StandardRequest{destination=rt});yield return null;}
            var prior=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Destroy(texture);
        }
        void Update(){if(protect&&game&&game.Player)game.Player.ProtectVehicleImpact();if(!done&&Time.realtimeSinceStartup-start>480){report.errors.Add("Native verification timeout");Finish();}}
        void Finish(){if(done)return;done=true;Time.timeScale=1;report.completed=report.errors.Count==0;Save();Restore();Application.Quit(report.completed?0:1);}
        void Restore(){PlayerPrefs.SetInt(UrbanCatalog.Prefix+"Site",site);if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");}
        void OnDestroy(){Application.logMessageReceived-=Log;Restore();}
    }
}
