using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class FutureCitySmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public int registeredResidents,residentObjects,activeResidents,buildingProfiles;public float meanFrameMs;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began;bool done,hadJail;float jail;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-future-city-smoke")&&!FindAnyObjectByType<FutureCitySmoke>()){GameDirector.SkipTitle=true;new GameObject("Future city essential checks").AddComponent<FutureCitySmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/FutureCity"));Directory.CreateDirectory(output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)if(report.errors.Count<20)report.errors.Add(text+"\n"+stack);}
        void Check(bool ok,string message){(ok?report.passed:report.errors).Add(message);Debug.Log("FUTURE CHECK "+ok+" / "+message);}
        void Update(){if(!done&&Time.realtimeSinceStartup-began>390){Check(false,"Essential verification timeout");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);LifeState.Hours=16;
            var incident=FindAnyObjectByType<RiftIncursion>();if(incident)incident.enabled=false;
            yield return new WaitForSeconds(4);
            report.registeredResidents=ExpansionWorld.Instance.Population;report.residentObjects=ExpansionWorld.Instance.ResidentObjects;report.activeResidents=ExpansionWorld.Instance.ActiveResidents;
            Check(report.registeredResidents>2000&&report.residentObjects<700,"District residents stream near the player instead of instantiating the entire population");
            var open=ControlFrame.Empty;open.map=true;sim.BeforeInput(ref open,.02f);yield return null;yield return null;
            var atlas=FindObjectsByType<AtlasViewport>().FirstOrDefault(a=>!a.mini);
            Check(atlas&&atlas.Span<1000&&Vector2.Distance(atlas.Center,new Vector2(g.Player.transform.position.x,g.Player.transform.position.z))<2,"Map opens zoomed around the current player position");
            if(atlas)
            {
                var anchor=atlas.rectTransform.rect.center+new Vector2(150,80);var world=atlas.Unproject(anchor);atlas.Zoom(.5f,anchor);
                Check(Vector3.Distance(world,atlas.Unproject(anchor))<.01f,"Map zoom preserves the world location under the pointer");
                var old=atlas.Center;var e=new PointerEventData(EventSystem.current){position=new Vector2(800,450),delta=new Vector2(80,40)};atlas.OnBeginDrag(e);atlas.OnDrag(e);Check(Vector2.Distance(old,atlas.Center)>5,"Dragging explores the map without moving the player");atlas.Recenter();
            }
            yield return CaptureUi("local-map");sim.CloseMap();yield return null;
            if(Environment.GetCommandLineArgs().Contains("-future-presentation-only"))
            {
                g.CameraRig.enabled=false;
                yield return View("canal-water",new(980,.2f,-2500),new(1040,5,-2650),new(1065,2,-3000));
                Check(Camera.main.GetComponent<UniversalAdditionalCameraData>().requiresColorTexture&&Shader.Find("AfterSignal/CoastalWater").isSupported,"Revised ocean shader compiles and receives the opaque scene texture");Finish();yield break;
            }
            var profiles=FindObjectsByType<CollapsibleBuilding>();report.buildingProfiles=profiles.Length;
            Check(profiles.Length>500&&profiles.All(b=>b.GetComponentsInChildren<MeshCollider>().Length>0),"Authored future buildings use independent collision and distance LOD meshes");
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Isolated essential verification deck";deck.transform.position=new Vector3(2040,199.5f,-1800);deck.transform.localScale=new Vector3(220,1,170);deck.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("WorldAssets/Generated/Asphalt");
            bool escaped=true,models=true;
            for(int i=0;i<11;i++)
            {
                var car=sim.Spawn(new Vector3(2040,200.1f,-1800),false,i);yield return null;yield return null;
                g.Player.Respawn(car.transform.position+Vector3.back*7);sim.Enter(car);car.speed=20;escaped&=sim.Exit()&&!sim.Current&&g.Player.Controller.enabled&&g.Player.Velocity.magnitude>15;
                if(i<6)models&=car.transform.Find("Detailed vehicle coachwork");Destroy(car.gameObject);yield return null;
            }
            Check(escaped,"All eleven transport types allow moving bailouts and preserve momentum");Check(models,"Sedan, taxi, bus, truck, motorcycle and sports car load their future coachwork");
            g.Player.Respawn(new Vector3(2040,200.1f,-1860));
            var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pole.name="Impact verification lamp";pole.transform.position=new Vector3(2030,204,-1800);pole.transform.localScale=new Vector3(.4f,4,.4f);var prop=pole.AddComponent<BreakableStreetProp>();prop.breakEnergy=120000;
            var bike=sim.Spawn(new Vector3(2025,200.1f,-1800),false,4);yield return null;yield return null;float health=bike.health;
            bike.speed=6;var drive=ControlFrame.Empty;drive.move=Vector2.up;for(int i=0;i<25;i++)bike.Drive(drive,.02f);
            Check(!prop.broken&&bike.health<health,"A light vehicle collides with a street fixture and takes damage without destroying it");Destroy(bike.gameObject);yield return null;
            var tank=sim.Spawn(new Vector3(2018,200.1f,-1800),false,10);yield return null;yield return null;tank.speed=12;for(int i=0;i<45;i++)tank.Drive(drive,.02f);
            Check(prop.broken&&tank.health>60,"Tank mass destroys the same fixture and retains most of its armour");Destroy(tank.gameObject);Destroy(pole);yield return null;
            var tower=GameObject.CreatePrimitive(PrimitiveType.Cube);tower.name="Aircraft impact structural target";tower.transform.position=new Vector3(2025,220,-1800);tower.transform.localScale=new Vector3(24,40,24);tower.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("WorldAssets/Generated/FutureSilver");var collapse=tower.AddComponent<CollapsibleBuilding>();
            var jet=sim.Spawn(new Vector3(2090,217,-1800),false,9);yield return null;yield return null;jet.transform.rotation=Quaternion.Euler(0,180,0);jet.speed=110;
            for(int i=0;i<90&&!jet.Wrecked;i++)jet.Drive(drive,.02f);
            Check(jet.Wrecked&&collapse.Collapsed,"A high-speed aircraft impact destroys the aircraft and starts building collapse");
            g.CameraRig.enabled=false;Camera.main.transform.position=new Vector3(2070,231,-1855);Camera.main.transform.LookAt(tower.transform.position);yield return new WaitForSeconds(.4f);Capture("building-collapse");
            Destroy(deck);yield return new WaitForSeconds(5);Destroy(tower);
            var ferry=IntercityService.All.FirstOrDefault(s=>!s.Aircraft);var plane=IntercityService.All.FirstOrDefault(s=>s.Aircraft);
            Check(IntercityService.All.Count(s=>s.Aircraft)==2&&ferry,"Both airports and the seaports have operating intercity services");
            yield return View("future-skyline",new(920,.2f,-2460),new(820,100,-2220),new(1100,42,-3200));
            yield return View("canal-water",new(980,.2f,-2500),new(1040,5,-2650),new(1065,2,-3000));
            yield return View("core-architecture",new(420,.2f,-140),new(430,62,-225),new(390,17,20));
            yield return View("boarding-gate",new(1394,.2f,780),new(1374,6,765),new(1408,2,800));
            // Measure a short representative window; no assertion against machine-specific FPS.
            float start=Time.realtimeSinceStartup;for(int i=0;i<90;i++)yield return null;report.meanFrameMs=(Time.realtimeSinceStartup-start)/90*1000;
            while(Time.realtimeSinceStartup-began<355&&(!(ferry&&ferry.Arrivals>0)||!IntercityService.All.Any(s=>s.Aircraft&&s.Arrivals>0)))yield return new WaitForSeconds(1);
            foreach(var service in IntercityService.All)Debug.Log("FUTURE TRANSIT "+service.Aircraft+" at "+service.transform.position+" / "+service.Status+" / arrivals "+service.Arrivals+" boarded "+service.Boarded+" unloaded "+service.Disembarked);
            Check(ferry&&ferry.Arrivals>0&&ferry.Boarded>0&&ferry.Disembarked>0,"Ferry passengers board, cross the sea and disembark at the destination");
            Check(IntercityService.All.Any(s=>s.Aircraft&&s.Arrivals>0&&s.Boarded>0&&s.Disembarked>0),"An airliner taxis, takes off, crosses between cities, lands and unloads its citizens");
            var docked=IntercityService.All.FirstOrDefault(s=>s.Boarding&&s.Manifest.Count<s.Seats);
            if(docked){int cash=LifeState.Credits;LifeState.Earn(1000);cash=LifeState.Credits;g.Player.Respawn(docked.ExitPoint);bool paid=docked.BuyTicket();Check(paid&&sim.Current==docked.Car&&sim.SeatIndex>0&&LifeState.Credits==cash-docked.Fare,"Player buys a ticket, occupies a reserved passenger seat and pays the fare");if(sim.Current)sim.Exit();}
            else Check(false,"No arrival was available for the player ticket check");
            Check(Camera.main.GetComponent<UniversalAdditionalCameraData>().requiresColorTexture&&Shader.Find("AfterSignal/CoastalWater").isSupported,"Depth-aware refractive ocean shader is supported and receives the opaque scene texture");
            Finish();
        }
        IEnumerator View(string name,Vector3 player,Vector3 eye,Vector3 target){GameDirector.Instance.Player.Respawn(player);Camera.main.transform.position=eye;Camera.main.transform.LookAt(target);yield return new WaitForSeconds(1);Capture(name);}
        IEnumerator CaptureUi(string name)
        {
            var canvases=FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=Camera.main;c.planeDistance=1;}
            Canvas.ForceUpdateCanvases();yield return null;Capture(name);foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Capture(string name)
        {
            var rt=RenderTexture.GetTemporary(1600,900,24);var previous=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(image);
        }
        void Finish(){if(done)return;done=true;Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"future.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);}
    }
}
