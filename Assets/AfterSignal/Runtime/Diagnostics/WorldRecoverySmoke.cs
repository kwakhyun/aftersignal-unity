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
    // Opt-in native captures: never changes an ordinary session or writes a game save.
    public sealed class WorldRecoverySmoke : MonoBehaviour
    {
        string output; readonly List<string> log=new(); bool done,hadJail; float jail,start;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-world-recovery-smoke")&&!FindAnyObjectByType<WorldRecoverySmoke>()){GameDirector.SkipTitle=true;new GameObject("World recovery capture").AddComponent<WorldRecoverySmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;start=Time.realtimeSinceStartup;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/WorldRecovery"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)log.Add("ERROR "+m+"\n"+s);}
        void Update(){if(!done&&Time.realtimeSinceStartup-start>300){log.Add("ERROR timeout");Finish();}}
        IEnumerator Start()
        {
            UnityEngine.Random.InitState(71239);ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            var g=GameDirector.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CameraRig.enabled=false;g.CloseDialogue();g.SetPaused(false);
            foreach(var r in FindObjectsByType<RiftIncursion>())r.enabled=false;foreach(var r in FindObjectsByType<ErebosPopulation>())r.enabled=false;
            Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            LifeState.Hours=10.13f;yield return new WaitForSeconds(3);
            CheckSurface("Coastal connector",new(1120,0,70));
            CheckSurface("Afterlight airport apron",new(1730,0,780));
            yield return View("01-expansion-ground",new(1120,.2f,70),new(1140,4,80),new(1220,6,180));
            yield return View("02-airport",new(1510,160,440),new(1410,210,325),new(1730,0,525));
            if(Environment.GetCommandLineArgs().Contains("-world-diagnose"))
            {
                var fade=FindAnyObjectByType<CameraOcclusion>();fade.enabled=false;yield return Capture("02a-no-fade");
                var lods=FindObjectsByType<LODGroup>();foreach(var lod in lods)lod.ForceLOD(0);yield return Capture("02b-force-lod");
                var pairs=new Dictionary<Material,Shader>();
                foreach(var r in FindObjectsByType<MeshRenderer>())foreach(var m in r.sharedMaterials)if(m&&m.shader.name=="AfterSignal/Urban Surface"&&!pairs.ContainsKey(m)){pairs[m]=m.shader;m.shader=Shader.Find("Universal Render Pipeline/Lit");}
                yield return Capture("02c-standard-material");foreach(var p in pairs)p.Key.shader=p.Value;foreach(var lod in lods)lod.ForceLOD(-1);fade.enabled=true;
            }
            yield return View("03-garden-aerial",new(990,140,620),new(970,190,570),new(860,0,630));
            var garden=FourCityWorld.Instance.Find("lumen-garden");var at=garden.transform.position;
            yield return View("04-botanical-aerial",at+new Vector3(0,100,-30),at+new Vector3(0,145,-80),at);
            yield return View("04-close-canopy",at+new Vector3(0,65,0),at+new Vector3(5,85,-20),at);
            if(Environment.GetCommandLineArgs().Contains("-world-diagnose"))
            {
                Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=false;yield return Capture("04a-no-post");Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                var volume=GameDirector.Instance.GetComponents<Volume>().First(v=>v.priority==25);volume.sharedProfile.TryGet<Bloom>(out var bloom);bloom.clamp.Override(65472);yield return Capture("04b-unbounded-bloom");bloom.clamp.Override(6);yield return Capture("04c-bounded-bloom");
            }
            yield return View("05-nova",new(1100,.2f,-3000),new(1090,7,-2990),new(1250,12,-3110));
            yield return View("06-nereid-above",new(3900,70,-4300),new(3900,130,-4140),new(3900,-62,-4480));
            yield return View("07-nereid-street",new(3900,-61.8f,-4530),new(3910,-57,-4570),new(3900,-49,-4380));
            yield return View("07-nereid-garden",new(3640,-61.8f,-4180),new(3640,-49,-4200),new(3640,-44,-4050));
            LifeState.Hours=17.45f;
            yield return View("07b-erebos-coast",new(3400,35,-2170),new(3370,40,-2250),new(3670,10,-1600));
            yield return View("08-airport-return",new(1750,.2f,400),new(1640,12,340),new(1730,8,525));
            CheckSurface("Airport after return from Nereid",new(1730,0,780));
            LifeState.Hours=22.3f;yield return View("09-garden-night",at+new Vector3(0,.2f,-132),at+new Vector3(0,5,-140),at+Vector3.up*18);
            Finish();
        }
        void CheckSurface(string title,Vector3 at)
        {
            Physics.SyncTransforms();bool solid=Physics.Raycast(at+Vector3.up*2,Vector3.down,out var hit,5,1,QueryTriggerInteraction.Ignore)&&hit.normal.y>.6f;
            bool drawn=FindObjectsByType<MeshRenderer>().Any(r=>r.enabled&&!r.forceRenderingOff&&r.bounds.size.y<8&&r.bounds.min.y<at.y+.4f&&r.bounds.max.y>at.y-.4f&&r.bounds.min.x<=at.x&&r.bounds.max.x>=at.x&&r.bounds.min.z<=at.z&&r.bounds.max.z>=at.z&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh.vertexCount>0);
            log.Add((solid&&drawn?"PASS ":"ERROR ")+title+" / physical floor="+solid+" / visible surface="+drawn);
        }
        IEnumerator View(string name,Vector3 player,Vector3 eye,Vector3 target)
        {
            GameDirector.Instance.Player.Respawn(player,false);GameDirector.Instance.Player.enabled=false;Camera.main.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));Camera.main.fieldOfView=60;yield return new WaitForSeconds(3);
            log.Add("VIEW "+name+" player="+player+" camera="+eye);
            var filters=FindObjectsByType<MeshFilter>();log.Add("Missing meshes="+filters.Count(f=>!f.sharedMesh));
            foreach(var f in filters){var r=f.GetComponent<MeshRenderer>();if(!r||!f.sharedMesh||r.bounds.SqrDistance(target)>240*240)continue;if(r.name.StartsWith("Batch ")||r.bounds.size.x>100||r.bounds.size.z>100)log.Add(r.name+" / enabled="+r.enabled+" / bounds="+r.bounds+" / "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+" ["+m.shader.name+"]":"NULL")));}
            yield return Capture(name);
        }
        IEnumerator Capture(string name)
        {
            yield return null;var rt=RenderTexture.GetTemporary(1440,810,24);var previous=RenderTexture.active;var texture=new Texture2D(1440,810,TextureFormat.RGB24,false);
            // StandardRequest updates the volume stack. SingleCameraRequest bypasses
            // UpdateVolumeFramework, so it cannot validate in-game bloom or exposure.
            for(int warm=0;warm<20;warm++){RenderPipeline.SubmitRenderRequest(Camera.main,new RenderPipeline.StandardRequest{destination=rt});yield return null;}
            RenderPipeline.SubmitRenderRequest(Camera.main,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1440,810),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            var pixels=texture.GetPixels32();log.Add(name+" white pixels="+pixels.Count(c=>c.r>248&&c.g>248&&c.b>248)+" / "+pixels.Length);
            if(!name.Contains("no-post")&&!name.Contains("unbounded"))log.Add((pixels.Count(c=>c.r>248&&c.g>248&&c.b>248)<pixels.Length*.10f?"PASS ":"ERROR ")+name+" / excessive whiteout");
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(texture);File.WriteAllLines(Path.Combine(output,"audit.txt"),log);Debug.Log("WORLD RECOVERY "+name);
        }
        void Finish(){if(done)return;done=true;File.WriteAllLines(Path.Combine(output,"audit.txt"),log);Application.Quit(log.Any(x=>x.StartsWith("ERROR"))?1:0);}
        void OnDestroy(){Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");}
    }
}
