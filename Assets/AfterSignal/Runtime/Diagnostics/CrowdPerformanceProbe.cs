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
    // Explicit opt-in, identical before/after views with a GPU fence in headless mode.
    public sealed class CrowdPerformanceProbe:MonoBehaviour
    {
        [Serializable]sealed class Sample{public string view;public float meanMs,p95Ms;public double mainThreadAllocatedBytes;public int actors,within40m,closePairs;}
        [Serializable]sealed class Report{public bool completed;public string device;public List<Sample> samples=new();public List<string> errors=new();}
        readonly Report report=new();string output;float started;GameDirector game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-crowd-performance-probe")&&!FindAnyObjectByType<CrowdPerformanceProbe>()){GameDirector.SkipTitle=true;new GameObject("Crowd performance probe").AddComponent<CrowdPerformanceProbe>();}}
        void Awake(){DontDestroyOnLoad(gameObject);LifeState.SuppressSave=CityChronicle.SuppressSave=true;RespawnNetwork.SuppressSave=true;Application.runInBackground=true;started=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CrowdPerformance/Native"));Directory.CreateDirectory(output);report.device=SystemInfo.graphicsDeviceName;Application.logMessageReceived+=Log;}
        void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)report.errors.Add(message+"\n"+trace);}
        IEnumerator Start()
        {
            UnityEngine.Random.InitState(91723);yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.CameraRig.enabled=false;CityChronicle.Instance.enabled=false;LifeState.Hours=13;WantedSystem.Clear("");
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is RiftIncursion||b is CityGangWar||b is GangStrongholds||b is CivicTerrorEvents||b is SeaCombat||b is GangCrime||b is GangConvoy||b is GangMember)b.enabled=false;
            yield return View("city",new Vector3(420,.05f,-140));
            var venue=FindObjectsByType<VenueRuntime>().First(v=>v.Definition.city==0&&v.Definition.kind==VenueKind.Football);
            yield return View("stadium",venue.Definition.Entrance+Vector3.back*8);
            report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);
        }
        IEnumerator View(string name,Vector3 point)
        {
            if(CityGangWar.FindGround(point,out var safe))point=safe;game.Player.Respawn(point,false);Camera.main.transform.SetPositionAndRotation(point+new Vector3(12,8,-18),Quaternion.LookRotation(new Vector3(-12,-6,28)));Camera.main.fieldOfView=60;
            yield return new WaitForSeconds(7);var frames=new List<float>();double alloc=0;long prior=GC.GetAllocatedBytesForCurrentThread();
            var rt=new RenderTexture(1600,900,24);rt.Create();var fence=new Texture2D(1,1,TextureFormat.RGB24,false);
            for(int i=0;i<150;i++)
            {
                float before=Time.realtimeSinceStartup;yield return null;RenderPipeline.SubmitRenderRequest(Camera.main,new RenderPipeline.StandardRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;fence.ReadPixels(new Rect(0,0,1,1),0,0);RenderTexture.active=old;
                if(i>=20){frames.Add((Time.realtimeSinceStartup-before)*1000);long now=GC.GetAllocatedBytesForCurrentThread();alloc+=Math.Max(0,now-prior);prior=now;}else prior=GC.GetAllocatedBytesForCurrentThread();
            }
            var persons=WorldActor.All.Where(a=>a&&a.Alive&&!a.monster&&!a.helicopter).ToArray();int pairs=0;
            for(int i=0;i<persons.Length;i++)for(int j=i+1;j<persons.Length;j++)if((persons[i].transform.position-persons[j].transform.position).sqrMagnitude<.45f*.45f)pairs++;
            var sorted=frames.OrderBy(x=>x).ToArray();report.samples.Add(new Sample{view=name,meanMs=frames.Average(),p95Ms=sorted[(int)(sorted.Length*.95f)],mainThreadAllocatedBytes=alloc/frames.Count,actors=persons.Length,within40m=persons.Count(a=>(a.transform.position-point).sqrMagnitude<1600),closePairs=pairs});
            var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());RenderTexture.active=prev;Destroy(tex);Destroy(fence);rt.Release();Destroy(rt);Save();
        }
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        void Update(){if(Time.realtimeSinceStartup-started>300){report.errors.Add("Probe timeout");Save();Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
