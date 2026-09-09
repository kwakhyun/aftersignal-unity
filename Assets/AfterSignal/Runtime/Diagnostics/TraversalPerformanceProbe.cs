using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    // Opt-in integration checks. Never changes the player's saved progress.
    public sealed class TraversalPerformanceProbe:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();readonly Vector3 site=new(1000,200,-800);GameDirector game;float started;bool done;
        const string Output="Artifacts/TraversalPerformance/functional-result.json";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-traversal-performance-probe")&&!FindAnyObjectByType<TraversalPerformanceProbe>()){GameDirector.SkipTitle=true;new GameObject("Traversal essentials").AddComponent<TraversalPerformanceProbe>();}}
        void Awake(){DontDestroyOnLoad(gameObject);LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;Application.runInBackground=true;started=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;Directory.CreateDirectory(Path.GetDirectoryName(Output));}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(message+"\n"+stack);Save();}}
        void Save()=>File.WriteAllText(Output,JsonUtility.ToJson(report,true));
        void Check(bool value,string message){(value?report.passed:report.errors).Add(message);Debug.Log("TRAVERSAL "+value+" / "+message);Save();}
        static void Set(object o,string key,object value)=>o.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        GameObject Box(string name,Vector3 point,Vector3 size){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=point;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Concrete");return go;}
        Vector2 Aim(Vector3 p){var cam=Camera.main;cam.transform.SetPositionAndRotation(game.Player.Shoulder,Quaternion.LookRotation(p-game.Player.Shoulder));return new Vector2(Screen.width*.5f,Screen.height*.5f);}
        IEnumerator Start()
        {
            UnityEngine.Random.InitState(91723);yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.enabled=false;game.CameraRig.enabled=false;
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is CityChronicle||b is CitySafety||b is CityFireService||b is WantedSystem||b is CityGangWar||b is GangStrongholds||b is CitySocial||b is CityActivityDirector||b is GangCrime||b is GangMember||b is PoliceOfficer||b is ArmyResponder||b is RegionalGuard||b is SeaCombat||b is RiftIncursion||b is CivicTerrorEvents||b is FourCityCampaign)b.enabled=false;
            foreach(var battle in FindObjectsByType<CampaignBattle>())Destroy(battle.gameObject);yield return null;
            Box("Diagnostic floor",site-Vector3.up*.5f,new(180,1,140));game.Player.Respawn(site+Vector3.back*8);yield return null;
            var chronicle=CityChronicle.Instance;chronicle.ResetProgress();var step=chronicle.CurrentStep;step.position=site;var intro=CampaignBattle.Begin(chronicle,step);Set(intro,"age",17f);yield return new WaitForSeconds(.5f);
            var guards=FindObjectsByType<GangMember>().Where(m=>m.IntroUnit).ToArray();foreach(var guard in guards)guard.enabled=false;
            Check(guards.Length==2&&guards.All(m=>ThreatOverlay.Hostile(m.Body)),"Only two actual tutorial combatants receive enemy markers");
            Check(!GameObject.Find("Mission mobile cover")&&!GameObject.Find("N-17 기억 소거 증폭기"),"Tutorial creates no barricades or enemy-labelled devices outside home");
            var police=PoliceOfficer.Create(WantedSystem.Instance,site+Vector3.left*30,1,0);police.enabled=false;LifeState.Heat=65;
            var ambient=GangMember.Create(site+Vector3.left*35,0,0);ambient.enabled=false;
            Check(WantedSystem.Level>0&&!ThreatOverlay.Hostile(police.Body)&&!ThreatOverlay.Hostile(ambient.Body),"Pursuing police and unrelated gang members never receive tutorial markers");
            Destroy(intro.gameObject);yield return null;Check(!CampaignBattle.TutorialActive&&!ThreatOverlay.Hostile(ambient.Body),"Tutorial overlay closes when the tutorial operation ends");Destroy(police.gameObject);Destroy(ambient.gameObject);LifeState.Heat=0;
            chronicle.Entry(chronicle.Quests[0]).step=1;step=chronicle.CurrentStep;step.position=site;var second=CampaignBattle.Begin(chronicle,step);yield return null;
            Check(step.battleMode=="assault"&&!GameObject.Find("N-17 기억 소거 증폭기"),"Second tutorial step remains completable without removed devices");Destroy(second.gameObject);yield return null;
            game.Player.Respawn(site);game.Player.Heal(100);var rope=game.Player.Rope;
            var wall=Box("Ordinary building wall",site+new Vector3(32,15,0),new(2,30,18));Physics.SyncTransforms();var pointer=Aim(site+new Vector3(31,23,0));var candidate=rope.Select(pointer);
            Check(candidate&&candidate.Surface&&candidate.SurfaceCollider==wall.GetComponent<Collider>(),"Crosshair selects an ordinary building wall without a pre-placed anchor");
            rope.Attach(candidate);Check(rope.Attached&&rope.Target.Surface,"Rope attaches to the selected masonry surface");
            var input=ControlFrame.Empty;input.pointer=pointer;input.grapple=true;input.move=Vector2.up;
            float startHeight=game.Player.transform.position.y;for(int i=0;i<80;i++){game.Player.Tick(input,.02f);yield return null;}
            Check(game.Player.transform.position.y>startHeight+3,"Player reels upward and flies along the surface rope");
            var beforeRelease=game.Player.Velocity;rope.Release();Check(!rope.Attached&&(game.Player.Velocity-beforeRelease).sqrMagnitude<.01f,"Releasing the rope preserves flight momentum");
            game.Player.Respawn(site);var car=UrbanSimulation.Instance.Spawn(site+new Vector3(0,.05f,24),false,0);yield return new WaitForSeconds(.3f);Physics.SyncTransforms();pointer=Aim(car.transform.position+Vector3.up*.9f);candidate=rope.Select(pointer);
            Check(candidate&&candidate.SurfaceCollider&&candidate.SurfaceCollider.GetComponentInParent<CityVehicle>()==car,"Crosshair selects a vehicle hull");rope.Attach(candidate);Vector3 first=rope.Target?rope.Target.transform.position:Vector3.zero;car.transform.position+=Vector3.right*4;rope.Target?.FollowSurface();
            Check(rope.Attached&&(rope.Target.transform.position-first-Vector3.right*4).sqrMagnitude<.01f,"Rope endpoint follows the moving vehicle's local attachment point");
            car.gameObject.SetActive(false);input.grapple=true;rope.Tick(input,.02f);Check(!rope.Attached,"Rope releases when the attached vehicle disappears");Destroy(car.gameObject);
            pointer=Aim(site+Vector3.up*180);Check(!rope.Select(pointer),"Aiming into empty sky cannot attach to a phantom anchor");
            var fixedGo=new GameObject("Existing campaign anchor");fixedGo.transform.position=site+new Vector3(5,8,0);var fixedAnchor=fixedGo.AddComponent<GrappleAnchor>();rope.Attach(fixedAnchor);Check(rope.Attached&&rope.Target==fixedAnchor,"Existing authored campaign anchors still attach");rope.Release();Destroy(fixedGo);
            var walker=new GameObject("Ground regression",typeof(SpriteRenderer),typeof(WorldActor));walker.transform.position=site+Vector3.left*12;yield return new WaitForSeconds(.4f);walker.transform.position-=Vector3.up*2;yield return new WaitForSeconds(.7f);Check(walker.transform.position.y>site.y-.1f,"Budgeted NPC support still recovers below-floor actors");Destroy(walker);
            var contactCar=UrbanSimulation.Instance.Spawn(site+new Vector3(-20,.03f,-20),false,0);yield return new WaitForSeconds(.3f);var obstacle=Box("Contact regression wall",contactCar.transform.position+Vector3.forward*1.1f+Vector3.up,new(10,2,1));Physics.SyncTransforms();typeof(CityVehicle).GetMethod("RecoverContact",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(contactCar,null);
            var collider=(Collider)typeof(CityVehicle).GetField("chassisCollider",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(contactCar);
            bool overlap=collider&&Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,obstacle.GetComponent<Collider>(),obstacle.transform.position,obstacle.transform.rotation,out _,out _);
            float depth=0;if(overlap)Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,obstacle.GetComponent<Collider>(),obstacle.transform.position,obstacle.transform.rotation,out _,out depth);
            Check(collider&&(!overlap||depth<.06f),"Car contact recovery resolves overlap without repeated world synchronizations");Destroy(contactCar.gameObject);Destroy(obstacle);
            bool family=false;for(int f=0;f<30&&!family;f++){yield return null;family=PopulationBudget.ClaimFrame(3);}Check(family,"Atomic three-person families still fit the staggered spawn budget");
            Check(FidelityPresentation.Preset==0&&!(FindObjectsByType<ReflectionProbe>().Any(p=>p.name=="Local city reflection"&&p.enabled)),"Performance default disables recurring local six-face reflection captures");
            report.completed=true;done=true;Save();Application.Quit(report.errors.Count==0?0:1);
        }
        void Update(){if(!done&&Time.realtimeSinceStartup-started>240){done=true;report.errors.Add("Probe timeout");Save();Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
