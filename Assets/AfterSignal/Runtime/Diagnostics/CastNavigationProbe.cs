using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AfterSignal
{
    // Opt-in native checks and visual evidence; does not write player progress.
    public sealed class CastNavigationProbe:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began;bool finished;GameDirector game;static readonly Vector3 Site=new(1000,200,-800);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-cast-navigation-probe")&&!FindAnyObjectByType<CastNavigationProbe>()){GameDirector.SkipTitle=true;new GameObject("Cast navigation essentials").AddComponent<CastNavigationProbe>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;began=Time.realtimeSinceStartup;output=Path.GetFullPath("Artifacts/CastNavigation/Native");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception){report.errors.Add(text+"\n"+stack);Save();}}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        void Check(bool ok,string description){(ok?report.passed:report.errors).Add(description);Debug.Log("CAST NAV "+ok+" / "+description);Save();}
        static void Set(object o,string key,object value)=>o.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        WorldActor Person(Vector3 at,string art="CivilianWoman")
        {
            var go=new GameObject("Native body fixture",typeof(SpriteRenderer),typeof(WorldActor),typeof(BoxCollider));go.layer=9;go.transform.position=at;var a=go.GetComponent<WorldActor>();a.health=100;var c=go.GetComponent<BoxCollider>();c.center=Vector3.up;c.size=new(.65f,2,.65f);go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");PeopleArt.Attach(go,art);return a;
        }
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.enabled=false;game.CameraRig.enabled=false;WantedSystem.Clear("");
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is CityChronicle||b is CitySafety||b is CityFireService||b is WantedSystem||b is CityGangWar||b is GangStrongholds||b is GangConvoy||b is CitySocial||b is CityActivityDirector||b is GangCrime||b is GangMember||b is PoliceOfficer||b is ArmyResponder||b is RegionalGuard||b is SeaCombat||b is RiftIncursion||b is CivicTerrorEvents||b is FourCityCampaign)b.enabled=false;
            foreach(var battle in FindObjectsByType<CampaignBattle>())Destroy(battle.gameObject);yield return null;
            Check(StorySprites.Names.All(n=>PeopleArt.Sheet(StorySprites.Key(n)).Length==16),"All 22 named characters have 16 imported frames (four directions)");
            Check(StorySprites.Names.Select(n=>PeopleArt.Get(StorySprites.Key(n),0)).Distinct().Count()==22,"Every named story character resolves to an independent sprite region");
            Check(StoryPortraits.Get("서하")&&Resources.Load<Texture2D>("Art/Title/AfterlightTitle"),"New Seoha portrait and ensemble title resolve through shipping resource paths");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Cast fixture floor";floor.transform.position=Site-Vector3.up*.5f;floor.transform.localScale=new(180,1,130);floor.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("Concrete");game.Player.Respawn(Site+Vector3.right*65);LifeState.Hours=12;yield return null;
            if(Environment.GetCommandLineArgs().Contains("-cast-navigation-ui")){yield return UiReview();report.completed=true;finished=true;Save();Application.Quit(report.errors.Count==0?0:1);yield break;}
            var walker=Person(Site+Vector3.left*20);yield return new WaitForSeconds(.4f);var safe=walker.transform.position;var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=safe+new Vector3(2,1.5f,0);wall.transform.localScale=new(1,3,5);Physics.SyncTransforms();walker.transform.position=wall.transform.position-Vector3.up*1.5f;yield return new WaitForSeconds(.4f);
            Check(walker.transform.position.x<wall.transform.position.x-.7f,"Transform-driven NPC is stopped before entering masonry");walker.transform.position=safe-Vector3.up*2;yield return new WaitForSeconds(.4f);Check(walker.transform.position.y>Site.y-.1f,"NPC that falls below support is recovered to a valid floor");Destroy(walker.gameObject);Destroy(wall);yield return null;
            var officer=PoliceOfficer.Create(WantedSystem.Instance,Site+Vector3.left*20,1,0);var hostile=Person(Site+Vector3.left*4);hostile.gang=true;hostile.health=500;yield return new WaitForSeconds(.5f);officer.Dispatch(hostile);yield return new WaitForSeconds(3.5f);Check(officer.ShotsFired>0,"Police identify and fire on a live visible hostile");hostile.health=0;int shots=officer.ShotsFired;yield return new WaitForSeconds(2);Check(officer.ShotsFired==shots&&!officer.GangTarget,"Police cancel burst and dispatch target immediately after threat dies");
            var inactive=Person(Site+Vector3.left*3);inactive.gang=true;inactive.gameObject.SetActive(false);officer.Dispatch(inactive);yield return new WaitForSeconds(.6f);Check(!officer.GangTarget,"Inactive hostile cannot become a dispatch target");Destroy(inactive.gameObject);Destroy(hostile.gameObject);Destroy(officer.gameObject);yield return null;
            var casualty=Person(Site);yield return null;casualty.Damage(85,Vector3.zero,TrafficDamageSource.Environment);yield return new WaitForSeconds(.3f);Physics.SyncTransforms();
            bool proneHit=Ballistics.Cast(Site+new Vector3(0,1,-7),(casualty.Center-(Site+new Vector3(0,1,-7))).normalized,12,null,out var hit)&&hit.collider.GetComponentInParent<WorldActor>()==casualty;
            Check(casualty.Downed&&proneHit,"Prone critical patient is hit by player ballistics with rotated billboard");yield return View("prone-hitbox",Site+new Vector3(3,4,-5),Site,true);
            var shooter=Person(Site+Vector3.back*8);shooter.gang=true;yield return new WaitForSeconds(.15f);FactionCombat.Fire(shooter,shooter.Center,casualty.Center,15,80,SignalEffects.Red,false);Check(!casualty.Alive,"Faction fire reaches and can kill a critically wounded prone target");Destroy(casualty.gameObject);Destroy(shooter.gameObject);yield return null;
            var patient=Person(Site);yield return null;patient.Damage(35,Vector3.zero,TrafficDamageSource.Environment);patient.gameObject.AddComponent<MedicalPending>();var ambulance=EmergencyAmbulance.Create(patient,null);ambulance.Car.transform.position=Site-Vector3.right*16;Set(ambulance,"hospital",Site-Vector3.right*20);yield return new WaitForSeconds(.5f);
            var threat=Person(Site+Vector3.forward*8);threat.gang=true;yield return new WaitForSeconds(1.6f);Check(ambulance.Decision.Contains("엄호")&&ambulance.Phase<=2,"Paramedics detect an active nearby shooter and request protection before treatment");threat.health=0;yield return new WaitForSeconds(3.5f);Check(!ambulance.Decision.Contains("위험"),"Rescue work resumes after the nearby threat is neutralized");if(ambulance)Destroy(ambulance.gameObject);Destroy(patient.gameObject);Destroy(threat.gameObject);yield return null;
            var gallery=new List<GameObject>();for(int n=0;n<22;n++){var a=Person(Site+new Vector3((n%11-5)*2.4f,0,n/11*5),"Story/"+n);a.GetComponent<DirectionalPerson>().Face(a.transform.position+Vector3.back*10,60);gallery.Add(a.gameObject);}yield return View("story-cast",Site+new Vector3(0,10,-25),Site+new Vector3(0,1,2),false);foreach(var a in gallery)Destroy(a);yield return null;
            Set(game,"<Fade>k__BackingField",0f);game.ShowDialogue("서하","노아, 주민들은 안전한 곳으로 이동했어. 지금부터 신호를 따라갈게.");yield return View("seo-dialogue",Site+new Vector3(0,4,-12),Site,true);game.CloseDialogue();
            var start=CityRoadNetwork.Junction(1,1);var goal=CityRoadNetwork.Junction(4,3);var nav=new NavigationGuide();nav.Update(start,goal,Vector3.forward);Check(nav.Connected&&nav.Route.Count>2&&nav.Remaining>200,"Navigation resolves the city road graph and remaining route distance");int previous=nav.Recalculations;nav.Update(goal,goal,Vector3.forward);Check(nav.Arrived,"Destination arrival is detected");Check(NavigationGuide.Bearing(Vector3.right).Contains("090"),"Compass bearing follows east as 090 degrees");
            game.Player.Respawn(start+Vector3.up*.1f);yield return new WaitForSeconds(.7f);var map=FindObjectsByType<AtlasViewport>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(a=>!a.mini);map.Waypoint?.Invoke(goal);yield return new WaitForSeconds(.8f);yield return View("navigation",start+new Vector3(-5,5,-10),start+Vector3.forward*10,true);
            UrbanSimulation.Instance.OpenMap();yield return new WaitForSeconds(.3f);yield return View("city-atlas",start+new Vector3(0,6,-14),start,true);Check(FindObjectsByType<Text>().Any(t=>t.isActiveAndEnabled&&t.text.Contains("길 안내 해제")),"Atlas exposes destination controls and navigation cancellation");UrbanSimulation.Instance.CloseMap();map.ClearWaypoint?.Invoke();
            Set(game,"<Title>k__BackingField",true);yield return new WaitForSeconds(1);yield return View("ensemble-title",start+new Vector3(0,6,-14),start,true);Set(game,"<Title>k__BackingField",false);
            report.completed=true;finished=true;Save();Application.Quit(report.errors.Count==0?0:1);
        }
        IEnumerator UiReview()
        {
            Set(game,"<Fade>k__BackingField",0f);var start=CityRoadNetwork.Junction(1,1);var goal=CityRoadNetwork.Junction(4,3);game.Player.Respawn(start+Vector3.up*.1f);yield return new WaitForSeconds(.6f);var map=FindObjectsByType<AtlasViewport>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(a=>!a.mini);map.Waypoint?.Invoke(goal);game.Toast("");yield return new WaitForSeconds(.6f);
            var card=GameObject.Find("Navigation guidance");Check(card&&card.GetComponent<RectTransform>().anchoredPosition.y>=330,"Navigation card is above weapon selection UI");
            yield return View("navigation",start+new Vector3(-8,5,-6),start+Vector3.right*18,true);UrbanSimulation.Instance.OpenMap();yield return new WaitForSeconds(.3f);yield return View("city-atlas",start+new Vector3(-8,5,-6),start+Vector3.right*18,true);UrbanSimulation.Instance.CloseMap();
            game.ShowDialogue("서하","노아, 주민들은 안전한 곳으로 이동했어. 지금부터 신호를 따라갈게.");yield return View("seo-dialogue",start+new Vector3(-8,5,-6),start+Vector3.right*18,true);game.CloseDialogue();
            Set(game,"<Title>k__BackingField",true);yield return new WaitForSeconds(1);yield return View("ensemble-title",start+new Vector3(-8,5,-6),start+Vector3.right*18,true);Set(game,"<Title>k__BackingField",false);
        }
        IEnumerator View(string name,Vector3 eye,Vector3 focus,bool ui)
        {
            var cam=Camera.main;var impulse=cam.GetComponent<CombatCameraImpulse>();if(impulse)impulse.enabled=false;cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(focus-eye));cam.fieldOfView=60;yield return new WaitForSeconds(.25f);
            var canvases=FindObjectsByType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();foreach(var c in canvases){if(ui){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=.3f+c.sortingOrder*.0001f;}else c.enabled=false;}
            var rt=new RenderTexture(1440,810,24);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});yield return null;var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1440,810,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,810),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=prior;rt.Release();Destroy(rt);Destroy(image);foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.enabled=true;}
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>240){finished=true;report.errors.Add("Probe timeout");Save();Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
