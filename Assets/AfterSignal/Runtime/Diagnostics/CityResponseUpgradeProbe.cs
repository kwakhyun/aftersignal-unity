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
    // Isolated native essentials. No save writes and no changes to the user's running player.
    public sealed class CityResponseUpgradeProbe:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began;bool finished;GameDirector game;
        static readonly Vector3 Site=new(1000,200,-800);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-city-response-upgrade-probe")&&!FindAnyObjectByType<CityResponseUpgradeProbe>()){GameDirector.SkipTitle=true;new GameObject("City response essentials").AddComponent<CityResponseUpgradeProbe>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;began=Time.realtimeSinceStartup;output=Path.GetFullPath("Artifacts/CityResponseUpgrade/Native");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(text+"\n"+stack);Save();}}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        void Check(bool ok,string text){(ok?report.passed:report.errors).Add(text);Debug.Log("CITY RESPONSE "+ok+" / "+text);Save();}
        static void Set(object o,string name,object v)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
        WorldActor Person(Vector3 at,bool walker=false)
        {
            var go=new GameObject("Response fixture resident",typeof(SpriteRenderer),typeof(WorldActor),typeof(BoxCollider));go.layer=9;go.transform.position=at;var b=go.GetComponent<WorldActor>();b.health=100;var hit=go.GetComponent<BoxCollider>();hit.center=Vector3.up;hit.size=new(.6f,2,.6f);go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");PeopleArt.Attach(go,"CivilianWoman");if(walker)go.AddComponent<CityPedestrian>().ResetAt(at,at+Vector3.right*20,PeopleArt.Sheet("CivilianWoman"));return b;
        }
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.enabled=false;game.CameraRig.enabled=false;WantedSystem.Clear("");CityChronicle.Instance.enabled=false;CitySafety.Instance.enabled=false;CityFireService.Instance.enabled=false;WantedSystem.Instance.enabled=false;
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is CityGangWar||b is GangStrongholds||b is GangConvoy||b is CitySocial||b is CityActivityDirector||b is GangCrime||b is GangMember||b is PoliceOfficer||b is RegionalGuard||b is SeaCombat||b is RiftIncursion||b is CivicTerrorEvents)b.enabled=false;
            foreach(var battle in FindObjectsByType<CampaignBattle>())Destroy(battle.gameObject);yield return null;
            Check(Resources.LoadAll<Sprite>("Art/StoryCast/CoreCast").Length==8&&Resources.LoadAll<Sprite>("Art/StoryCast/WorldCast").Length==12,"Twenty separate story portraits are imported");
            Check(new[]{"서하","노아","민재","다은","수연","지훈","라온","윤","해진","다미","리안","나리","수호","미루","이솔","유건","유라","세린","도윤"}.All(n=>StoryPortraits.Get(n)),"Story speaker names resolve to their own portraits");
            Check(FireCrewArt.Frames.Length==16&&PeopleArt.Get("Firefighter",2),"Fire crew has both sexes, hose poses and four directions");
            var cars=UrbanSimulation.Instance.Cars;
            Check(cars.Count(c=>c&&c.IsAircraft&&c.transform.position.x<550&&c.transform.position.z>700&&c.transform.position.z<1000)>=8,"Original Lumen base retains four fighters and four combat helicopters");
            Check(cars.Count(c=>c&&c.IsAircraft&&(c.transform.position-MaritimeWorld.AirBase).sqrMagnitude<150*150)>=5,"New airfield has its own five aircraft");
            Check(GameObject.Find("해군 · 해협 통합기지")&&GameObject.Find("공군 · 루멘 항공작전기지"),"Expanded naval and air command architecture exists");
            Check(FindObjectsByType<RegionalUniform>().Count(u=>u.art=="NavyCrew"||u.art=="AirForceCrew")>=24,"Military bases have dedicated naval and air personnel");
            yield return View("airfield",MaritimeWorld.AirBase+new Vector3(100,95,-130),MaritimeWorld.AirBase+new Vector3(0,0,20));
            yield return View("naval-base",MaritimeWorld.NavalBase+new Vector3(135,110,-200),MaritimeWorld.NavalBase+new Vector3(0,0,-30));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Response essential fixture floor";floor.transform.position=Site-Vector3.up*.5f;floor.transform.localScale=new(220,1,170);floor.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Concrete");game.Player.Respawn(Site+Vector3.right*60);LifeState.Hours=12;yield return null;
            game.Toast("local notice");game.ToastNear("distant notice",game.Player.transform.position+Vector3.right*500);Check(game.Notice=="local notice","Distant incident does not replace a local notice");game.ToastNear("nearby notice",game.Player.transform.position+Vector3.right*30);Check(game.Notice=="nearby notice","Nearby incident still displays its notice");
            Set(game,"<Fade>k__BackingField",0f);game.ShowDialogue("노아","서하, 천천히 움직여. 빨간 표시가 있는 회수대가 적이야. 주민들은 우리가 지킬게.");yield return null;
            var canvases=FindObjectsByType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=.3f+canvas.sortingOrder*.0001f;}
            yield return View("story-dialogue",Site+new Vector3(5,4,-14),Site+Vector3.up);Check(FindObjectsByType<UnityEngine.UI.Image>().Any(i=>i.isActiveAndEnabled&&i.sprite==StoryPortraits.Bust("노아")),"Dialogue displays Noa's portrait on its live UI image");foreach(var canvas in canvases)canvas.renderMode=RenderMode.ScreenSpaceOverlay;game.CloseDialogue();
            if(Environment.GetCommandLineArgs().Contains("-response-presentation-only")){report.completed=true;finished=true;Save();Application.Quit(report.errors.Count==0?0:1);yield break;}
            var chronicle=CityChronicle.Instance;chronicle.ResetProgress();var step=chronicle.CurrentStep;var original=step.position;step.position=Site;game.Player.Respawn(Site+Vector3.back*8);var intro=CampaignBattle.Begin(chronicle,step);
            yield return new WaitForSeconds(2);Check(intro.Wave==0&&CityChronicle.IntroProtected,"First campaign offers preparation time and blocks random incidents");
            yield return new WaitForSeconds(15);var guards=FindObjectsByType<GangMember>().Where(m=>m.IntroUnit).ToArray();Check(guards.Length==2&&guards.All(m=>m.Body.health<=48&&m.CampaignWeapon==0),"Opening wave contains two low-health rifle enemies");Check(guards.All(m=>ThreatOverlay.Hostile(m.Body)),"Campaign enemies are eligible for visible hostile markers");Destroy(intro.gameObject);step.position=original;chronicle.Entry(chronicle.Quests[0]).step=chronicle.Quests[0].steps.Length;game.Player.Heal(100);game.Player.Respawn(Site+Vector3.right*75);yield return null;
            var minor=Person(Site+new Vector3(-30,0,-25),true);var critical=Person(Site+new Vector3(-15,0,-25));yield return null;minor.Damage(28,Vector3.zero,TrafficDamageSource.Environment);critical.Damage(85,Vector3.zero,TrafficDamageSource.Environment);minor.gameObject.AddComponent<MedicalPending>();float before=minor.transform.position.x;
            for(int i=0;i<60;i++){minor.GetComponent<CityPedestrian>().Tick(.03f);yield return null;}
            Check(minor.Alive&&!minor.Downed&&minor.transform.position.x>before+.5f,"Wounded civilian walks while waiting for assigned ambulance");Check(critical.Alive&&critical.Downed,"Only critically injured civilian is incapacitated");
            var patients=new List<WorldActor>{critical};for(int i=0;i<6;i++)patients.Add(Person(Site+new Vector3(-10+i*4,0,-25)));yield return null;foreach(var p in patients){if(!p.Downed)p.Damage(85,Vector3.zero,TrafficDamageSource.Environment);p.GetComponent<MedicalState>().Report();}
            CitySafety.Instance.enabled=true;yield return new WaitForSeconds(6.5f);Check(CitySafety.Instance.ActiveAmbulances==5&&patients.Any(p=>!p.GetComponent<MedicalPending>()),"Seven casualties are queued behind a five-ambulance cap");CitySafety.Instance.enabled=false;
            foreach(var a in FindObjectsByType<EmergencyAmbulance>())Destroy(a.gameObject);yield return null;
            var ambulance=EmergencyAmbulance.Create(critical,null);ambulance.Car.transform.position=critical.transform.position-Vector3.right*16;Set(ambulance,"hospital",Site+new Vector3(-45,0,-25));
            float limit=Time.time+25;while(ambulance&&ambulance.Phase<3&&Time.time<limit)yield return null;
            Check(ambulance&&ambulance.Phase==3&&ambulance.Stretcher.Find("Patient lying on stretcher"),"Critical patient has an actual horizontal sprite on stretcher");
            if(ambulance&&ambulance.Stretcher){ambulance.enabled=false;yield return View("stretcher",ambulance.Stretcher.position+ambulance.Stretcher.right*5+Vector3.up*5,ambulance.Stretcher.position+Vector3.up*.8f);}
            Destroy(ambulance.gameObject);foreach(var p in patients)Destroy(p.gameObject);yield return null;
            var aid=EmergencyAmbulance.Create(minor,null);aid.Car.transform.position=minor.transform.position-Vector3.right*16;Set(aid,"hospital",aid.Car.transform.position+Vector3.back*4);limit=Time.time+28;bool treated=false,returning=false;while(aid&&Time.time<limit){treated|=!minor.GetComponent<MedicalState>().NeedsRescue;returning|=aid.Phase==6||aid.Phase==5;yield return null;}Check(treated&&returning&&!aid,"On-site first aid is followed by hospital return before releasing ambulance");if(aid)Destroy(aid.gameObject);Destroy(minor.gameObject);yield return null;
            var fires=new List<BurningObject>();for(int i=0;i<7;i++){var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.name="Ignitable fixture "+i;prop.transform.position=Site+new Vector3(-38+i*12,2,27);prop.transform.localScale=new(4,4,4);prop.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Metal");fires.Add(CityFireService.Ignite(prop,prop.transform.position+Vector3.up*2,1));}
            for(int i=0;i<5;i++){var e=FireEngine.Create(fires[i],Site+new Vector3(-38+i*12,0,5),Site+new Vector3(-38+i*12,0,-15));fires[i].Assigned=e;CityFireService.Instance.Engines.Add(e);}
            CityFireService.Instance.enabled=true;yield return new WaitForSeconds(2.5f);Check(CityFireService.Instance.Engines.Count<=5&&fires.Count(f=>f&&!f.Assigned)>=2,"Fire service limits five engines and queues remaining fires");Check(FindObjectsByType<Firefighter>().Length==10,"Five engines deploy ten dedicated firefighters");
            yield return View("fire-response",Site+new Vector3(-23,13,-17),Site+new Vector3(-20,1,16));yield return new WaitForSeconds(12);Check(CityFireService.Instance.Extinguished>0,"Hoses and truck cannon extinguish active fires");
            var gang=GangMember.Create(Site+new Vector3(-35,0,-35),0,0);gang.enabled=false;var target=Person(gang.transform.position+Vector3.right*17);yield return null;Check(gang.Tactics.Civilian()==target,"Gang tactics can target nearby unarmed civilians");Check(gang.Tactics.SpecialAttack(gang.Body.Center,target.Center),"Gang RPG attack launches its dedicated projectile");
            var escape=UrbanSimulation.Instance.Spawn(Site+new Vector3(-35,0,-30),false,0);GangGetaway.Begin(escape,gang);yield return null;Check(escape.GetComponent<GangGetaway>()&&gang.transform.parent==escape.transform&&escape.occupied,"Cornered gang member can occupy a getaway car");
            report.completed=true;finished=true;Save();Application.Quit(report.errors.Count==0?0:1);
        }
        IEnumerator View(string name,Vector3 eye,Vector3 focus)
        {
            var cam=Camera.main;var impulse=cam.GetComponent<CombatCameraImpulse>();if(impulse)impulse.enabled=false;cam.transform.position=eye;cam.transform.LookAt(focus);cam.fieldOfView=60;yield return new WaitForSeconds(.25f);var rt=new RenderTexture(1280,720,24);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});yield return null;var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(tex);
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>240){finished=true;report.errors.Add("Response probe timed out");Save();Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
