using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    public sealed class FidelityBenchmark : MonoBehaviour
    {
        [Serializable] public class Sample { public string view; public float meanMs,p95Ms; public long memoryBytes; public int renderers,visibleRenderers; }
        [Serializable] public class Report { public string device,unity; public int width,height; public bool completed; public List<Sample> samples=new(); public List<string> passed=new(),errors=new(); }
        readonly Report report=new(); string output; bool done,hadJail; float jail,began;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Environment.GetCommandLineArgs().Contains("-fidelity-benchmark")&&!FindAnyObjectByType<FidelityBenchmark>())
            {GameDirector.SkipTitle=true;new GameObject("Fidelity visual and performance benchmark").AddComponent<FidelityBenchmark>();}
        }
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;began=Time.realtimeSinceStartup;
            LifeState.SuppressSave=CityChronicle.SuppressSave=true;
            hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Fidelity"));Directory.CreateDirectory(output);
            report.device=SystemInfo.graphicsDeviceName;report.unity=Application.unityVersion;report.width=1600;report.height=900;Application.logMessageReceived+=Log;
        }
        void Log(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&report.errors.Count<24)report.errors.Add(message+"\n"+stack);}
        void Update(){if(!done&&Time.realtimeSinceStartup-began>360){report.errors.Add("Benchmark timeout");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;UnityEngine.Random.InitState(71239);
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            var game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.CameraRig.enabled=false;
            foreach(var item in FindObjectsByType<RiftIncursion>())item.enabled=false;foreach(var item in FindObjectsByType<ErebosPopulation>())item.enabled=false;
            yield return new WaitForSeconds(2);
            EssentialChecks();
            yield return View("01-core-day",new(420,.2f,-140),new(430,5,-175),new(390,11,20),14);
            yield return View("02-core-night",new(420,.2f,-140),new(430,5,-175),new(390,11,20),22);
            yield return View("03-stadium",FourCityCatalog.Venues[2].Entrance,new(1295,90,1308),new(1170,6,1440),14);
            yield return View("04-nova-street",new(700,.2f,-4460),new(706,4,-4454),new(540,11,-4560),18.4f);
            yield return View("05-nereid",FourCityCatalog.Venues[23].Entrance,new(3990,-43,-4740),new(3900,-53,-4430),14);
            var archive=FourCityWorld.Instance.Find("nereid-archive");
            yield return View("06-interior",archive.transform.position+new Vector3(0,.15f,-12),archive.transform.position+new Vector3(7,4,-20),archive.transform.position+new Vector3(-10,1,0),14);
            Check(FindAnyObjectByType<VenuePracticalLights>().ActiveLights>0,"Practical lights activate on the occupied interior floor");
            var garden=FourCityWorld.Instance.Find("lumen-garden");
            yield return View("07-garden",garden.Definition.Entrance,garden.transform.position+new Vector3(0,2.4f,-32),garden.transform.position+new Vector3(12,6,8),14);
            game.Player.Respawn(new Vector3(420,.2f,-140),false);yield return new WaitForSeconds(3);
            var street=FindObjectsByType<StreetAmenity>().OrderBy(p=>(p.transform.position-game.Player.transform.position).sqrMagnitude).FirstOrDefault();
            Check(street,"Streaming creates usable street amenities near the player");
            if(street)
            {
                var at=street.transform.position;var q=street.transform.rotation;
                yield return View("08-street-detail",at+q*new Vector3(-4,0,-4),at+q*new Vector3(0,2,-7),at+Vector3.up*1.2f,19.5f);
            }
            game.SetPaused(true);yield return null;yield return CaptureHud("09-graphics-settings");game.SetPaused(false);
            Finish();
        }
        void Check(bool ok,string message){(ok?report.passed:report.errors).Add(message);Debug.Log("FIDELITY CHECK "+ok+" / "+message);}
        void EssentialChecks()
        {
            var root=new GameObject("Temporary navigation regression fixture");root.transform.position=new(8000,500,8000);
            var floor=new GameObject("Floor").AddComponent<BoxCollider>();floor.transform.SetParent(root.transform,false);floor.center=new(0,-.2f,0);floor.size=new(40,.4f,40);
            var wall=new GameObject("Obstacle").AddComponent<BoxCollider>();wall.transform.SetParent(root.transform,false);wall.center=new(0,1.2f,0);wall.size=new(3,2.4f,1);
            var walker=new GameObject("Test pedestrian").AddComponent<PedestrianSteering>();walker.transform.SetParent(root.transform,false);walker.transform.localPosition=new(0,.035f,-8);Physics.SyncTransforms();
            var target=root.transform.TransformPoint(new Vector3(0,.035f,8));bool crossedWall=false;
            for(int i=0;i<1000;i++){walker.Move(target,.04f);var p=walker.transform.localPosition;if(Mathf.Abs(p.x)<1.49f&&Mathf.Abs(p.z)<.49f)crossedWall=true;}
            Check(!crossedWall&&Vector3.Distance(walker.transform.position,target)<.2f,"Pedestrian reaches a destination around a solid obstruction without crossing it / remaining "+Vector3.Distance(walker.transform.position,target).ToString("F2")+"m");
            walker.transform.localPosition=new(8,.035f,18);target=root.transform.TransformPoint(new Vector3(8,.035f,28));
            for(int i=0;i<400;i++)walker.Move(target,.04f);
            Check(walker.transform.localPosition.z<20.1f&&walker.transform.localPosition.y>.02f,"Pedestrian stops at an unsupported ledge");
            var other=new GameObject("Trigger-body neighbour").AddComponent<CapsuleCollider>();other.gameObject.layer=9;other.isTrigger=true;other.center=Vector3.up*.8f;other.height=1.6f;other.radius=.24f;other.transform.SetParent(root.transform,false);other.transform.localPosition=new(.4f,0,0);
            var second=new GameObject("Neighbour avoidance walker").AddComponent<PedestrianSteering>();second.transform.SetParent(root.transform,false);second.transform.localPosition=new(0,.035f,0);wall.enabled=false;Physics.SyncTransforms();
            for(int i=0;i<15;i++)second.Move(root.transform.TransformPoint(new Vector3(0,.035f,8)),.04f);
            Check(second.transform.localPosition.x<-.05f,"Trigger-based citizen bodies contribute to local separation");
            var game=GameDirector.Instance;game.Player.Respawn(root.transform.position+new Vector3(10,.15f,-10),false);
            var first=new GameObject("Nearest interaction fixture").AddComponent<InteractionPoint>();first.transform.SetParent(root.transform,false);first.transform.position=game.Player.Shoulder+Vector3.forward;first.radius=3;
            var far=new GameObject("Remote interaction fixture").AddComponent<InteractionPoint>();far.transform.SetParent(root.transform,false);far.transform.position=first.transform.position+Vector3.right*60;far.radius=3;
            var scanner=new InteractionScanner();Check(scanner.Nearest(game.Player)==first,"Interaction cache selects the nearest usable point");first.gameObject.SetActive(false);Check(scanner.Nearest(game.Player)==null,"Disabled interactions leave the cache immediately");
            game.Player.Respawn(far.transform.position-Vector3.up*1.5f,false);Check(scanner.Nearest(game.Player)==far,"Interaction cache refreshes after long-distance travel");
            var botanical=Resources.Load<GameObject>("WorldAssets/Botanical/BotanicalTree");
            var lods=botanical?botanical.GetComponent<LODGroup>().GetLODs():Array.Empty<LOD>();
            Check(lods.Length==3&&IndexCount(lods[0])>IndexCount(lods[1])&&IndexCount(lods[1])>IndexCount(lods[2]),"Botanical asset has three strictly decreasing mesh LODs");
            var facade=FindObjectsByType<FacadeGlass>().FirstOrDefault(f=>f.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="AfterSignal/Architectural Glass"));
            if(facade)
            {
                var pane=facade.GetComponentsInChildren<MeshRenderer>().First(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="AfterSignal/Architectural Glass");
                var hit=pane.bounds.center;int before=facade.BrokenWindows;facade.Hit(hit,40);var block=new MaterialPropertyBlock();pane.GetPropertyBlock(block);
                Check(facade.BrokenWindows==before+1&&facade.OpenAt(hit)&&block.GetInt("_BreakCount")==before+1,"Physical facade materials retain breakable windows and their opening mask");
            }
            else Check(false,"An architectural facade with breakable windows is present");
            var bollard=StreetKit.Place("SmartBollard",root.transform,new Vector3(10,0,10),Quaternion.identity,false);
            var bounds=bollard.GetComponentInChildren<MeshRenderer>().bounds;
            Check(bounds.size.y>.7f&&bounds.size.y>Mathf.Max(bounds.size.x,bounds.size.z)*2,"FBX street models retain their upright axis and metre scale");
            Destroy(root);
        }
        static ulong IndexCount(LOD lod){ulong count=0;foreach(var renderer in lod.renderers){var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;for(int s=0;s<mesh.subMeshCount;s++)count+=mesh.GetIndexCount(s);}return count;}
        IEnumerator CaptureHud(string name)
        {
            var canvases=FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=.5f;}Canvas.ForceUpdateCanvases();yield return null;
            var rt=RenderTexture.GetTemporary(1600,900,24);var old=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(texture);
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}Canvas.ForceUpdateCanvases();
        }
        IEnumerator View(string name,Vector3 player,Vector3 eye,Vector3 target,float hour)
        {
            var game=GameDirector.Instance;LifeState.Hours=hour;game.Player.Respawn(player,false);Camera.main.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));Camera.main.fieldOfView=60;
            yield return new WaitForSeconds(2.5f);
            // Hidden Windows players skip ordinary presentation. Explicitly render each measured
            // frame, including a GPU readback fence, so an idle window is never reported as FPS.
            var benchmarkTarget=RenderTexture.GetTemporary(1600,900,24);
            var fence=new Texture2D(1,1,TextureFormat.RGB24,false);var frames=new float[120];
            for(int n=0;n<frames.Length;n++)
            {
                float start=Time.realtimeSinceStartup;
                RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=benchmarkTarget});
                var old=RenderTexture.active;RenderTexture.active=benchmarkTarget;fence.ReadPixels(new Rect(0,0,1,1),0,0);RenderTexture.active=old;
                yield return null;frames[n]=(Time.realtimeSinceStartup-start)*1000;
            }
            RenderTexture.ReleaseTemporary(benchmarkTarget);Destroy(fence);
            var sorted=frames.OrderBy(f=>f).ToArray();var renderers=FindObjectsByType<MeshRenderer>();
            report.samples.Add(new Sample{view=name,meanMs=frames.Average(),p95Ms=sorted[(int)(sorted.Length*.95f)],memoryBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),renderers=renderers.Length,visibleRenderers=renderers.Count(r=>r.enabled&&r.isVisible)});
            var rt=RenderTexture.GetTemporary(1600,900,24);var previous=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(texture);
            File.WriteAllText(Path.Combine(output,"benchmark.json"),JsonUtility.ToJson(report,true));Debug.Log("FIDELITY VIEW "+name+" "+frames.Average().ToString("F2")+" ms");
        }
        void Finish(){if(done)return;done=true;report.completed=true;File.WriteAllText(Path.Combine(output,"benchmark.json"),JsonUtility.ToJson(report,true));Application.Quit(report.errors.Count==0?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");}
    }
}
