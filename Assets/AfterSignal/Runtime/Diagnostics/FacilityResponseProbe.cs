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
        const string Output="Artifacts/FacilityResponse/Native";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(!Environment.GetCommandLineArgs().Contains("-facility-response-probe")||FindAnyObjectByType<FacilityResponseProbe>())return;GameDirector.SkipTitle=true;new GameObject("Facility response essentials").AddComponent<FacilityResponseProbe>();}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;Directory.CreateDirectory(Output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string line,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(line+"\n"+stack);Save();}}
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
