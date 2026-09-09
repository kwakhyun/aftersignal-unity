using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    // Opt-in shipping-renderer check; never runs during ordinary play.
    public sealed class SpriteQualityProbe:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();readonly List<GameObject> actors=new();
        string output;float began;Camera reviewCamera;RenderTexture target;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-sprite-quality-probe"))new GameObject("Sprite quality essentials").AddComponent<SpriteQualityProbe>();}
        void Awake(){DontDestroyOnLoad(gameObject);began=Time.realtimeSinceStartup;LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;output=Path.GetFullPath("Artifacts/SpriteQuality/Native");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(text+"\n"+stack);}
        void Check(bool pass,string text){(pass?report.passed:report.errors).Add(text);}
        IEnumerator Start()
        {
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var keys=new[]{"CivilianMan","CivilianWoman","Doctor","Nurse"};
            foreach(string key in keys)
            {
                var sheet=PeopleArt.Sheet(key);bool valid=sheet.Length==24;
                for(int direction=0;direction<4;direction++)for(int phase=0;phase<4;phase++)valid&=PeopleArt.Walk(key,direction,phase)!=null&&PeopleArt.Get(key,direction,phase)!=null;
                Check(valid,key+": all 24 frames and four-direction walk/talk lookups loaded");
            }
            Check(new[]{"Worker","StudentBoy","CyberPolice","CyberGang","Soldier","CoastGuard"}.All(k=>PeopleArt.Sheet(k).Length>=16),"Existing civilian/security/military lookups remain available");
            var cameraObject=new GameObject("Sprite review camera");reviewCamera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<UniversalAdditionalCameraData>();reviewCamera.enabled=false;reviewCamera.orthographic=true;reviewCamera.orthographicSize=5.6f;reviewCamera.transform.position=new Vector3(0,0,-20);reviewCamera.clearFlags=CameraClearFlags.SolidColor;reviewCamera.backgroundColor=new Color(.04f,.075f,.1f,1);reviewCamera.cullingMask=1<<31;reviewCamera.nearClipPlane=.1f;reviewCamera.farClipPlane=30;
            target=new RenderTexture(1600,1200,24,RenderTextureFormat.ARGB32);target.Create();
            for(int phase=0;phase<4;phase++)
            {
                ClearActors();for(int row=0;row<4;row++)for(int facing=0;facing<4;facing++)Add(PeopleArt.Walk(keys[row],facing,phase),facing,row);
                yield return null;Capture("npc-walk-"+phase+".png");
            }
            ClearActors();string[] seo={"SeoRefined/SprintRight","SeoRefined/ActionFrontRight","SeoSwimming/SwimFront","SeoSwimming/SwimRight"};
            for(int row=0;row<4;row++){var frames=Resources.LoadAll<Sprite>("Art/"+seo[row]).OrderBy(s=>s.name).ToArray();Check(frames.Length==8,seo[row]+": eight action frames loaded");for(int col=0;col<4&&col<frames.Length;col++)Add(frames[col],col,row);}
            yield return null;Capture("seo-actions.png");report.completed=true;Finish();
        }
        void Add(Sprite sprite,int col,int row){if(!sprite)return;var go=new GameObject("review actor",typeof(SpriteRenderer));go.layer=31;var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");go.transform.position=new Vector3((col-1.5f)*3.1f,3.7f-row*2.6f-sprite.bounds.center.y,0);actors.Add(go);}
        void ClearActors(){foreach(var actor in actors)Destroy(actor);actors.Clear();}
        void Capture(string file)
        {
            RenderPipeline.SubmitRenderRequest(reviewCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();RenderTexture.active=previous;var pixels=texture.GetPixels32();int colored=pixels.Count(p=>Mathf.Max(p.r,Mathf.Max(p.g,p.b))>100);Check(colored>10000,file+": character pixels render through the shipping material");File.WriteAllBytes(Path.Combine(output,file),texture.EncodeToPNG());Destroy(texture);
        }
        void Update(){if(Time.realtimeSinceStartup-began>120){report.errors.Add("Sprite quality probe timeout");Finish();}}
        void Finish(){File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(report.errors.Count==0?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Log;if(target){target.Release();Destroy(target);}}
    }
}
