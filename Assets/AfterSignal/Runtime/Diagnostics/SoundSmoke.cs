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
    public sealed class SoundSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new List<string>(),errors=new List<string>();public List<string> levels=new List<string>();}
        readonly Report report=new Report();
        readonly Dictionary<string,float> saved=new Dictionary<string,float>();readonly List<string> absent=new List<string>();
        bool finished;string output;float began;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-sound-smoke")&&!FindAnyObjectByType<SoundSmoke>()){GameDirector.SkipTitle=true;new GameObject("SFX and corpse essential check").AddComponent<SoundSmoke>();}}
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=true;CityChronicle.SuppressSave=true;began=Time.realtimeSinceStartup;
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Sound/Final"));Directory.CreateDirectory(output);
            foreach(var k in new[]{"Volume","SfxVolume","MusicVolume"}){string key="AFTERSIGNAL.Unity."+k;if(PlayerPrefs.HasKey(key))saved[key]=PlayerPrefs.GetFloat(key);else absent.Add(key);}
            Application.logMessageReceived+=Log;
        }
        void Log(string m,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(m+"\n"+stack);}
        void Check(bool ok,string title){(ok?report.passed:report.errors).Add(title);}
        void Update(){if(!finished&&(report.errors.Count>0||Time.realtimeSinceStartup-began>110)){if(report.errors.Count==0)Check(false,"Timed out");Finish();}}
        ControlFrame Frame(Vector2 move,bool jump=false,bool attack=false,int weapon=-1){var f=ControlFrame.Empty;f.move=move;f.jump=jump;f.attack=attack;f.weapon=weapon;f.pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);return f;}
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;g.Input.ExternalControl=true;g.CloseDialogue();g.SetPaused(false);
            g.Input.ExternalFrame=ControlFrame.Empty;
            var a=g.Audio;a.SetVolume(.6f);a.SetSfxVolume(.9f);SignalMusic.Instance.SetMusicVolume(0);
            var origin=new Vector3(400,120,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Sound check / elevated solid floor";floor.transform.position=origin+Vector3.down*.5f;floor.transform.localScale=new Vector3(50,1,40);
            floor.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Materials/LightTile");
            g.Player.Respawn(origin+Vector3.up*.1f);g.CameraRig.Snap();yield return new WaitForSeconds(.3f);
            Check(FindObjectsByType<AudioListener>().Count(l=>l.enabled)==1&&AudioListener.volume>0&&!AudioListener.pause,"One enabled listener with audible global output");
            var core=new[]{"blade_swing","heavy_swing","blade_hit","heavy_hit","jump","land","step_tile","step_metal","hurt","guard","dash","rope_attach","rope_release","reload_in","reload_out","reload_slide"};
            Check(core.All(a.HasCue),"All required movement combat and rope cues load");
            Check(Resources.LoadAll<AudioClip>("Audio/Firearms").Length==18,"All eighteen real firearm variations load");
            foreach(var cue in core)
            {
                int before=a.Count(cue);a.Play(cue,g.Player.Shoulder,.3f,2);
                float peak=0,until=Time.unscaledTime+.23f;
                while(Time.unscaledTime<until){peak=Mathf.Max(peak,a.OutputPeak());yield return null;}
                report.levels.Add(cue+" peak="+peak.ToString("F5"));
                Check(a.Count(cue)>before&&peak>.0001f,"Audible output: "+cue);
                yield return new WaitForSeconds(.12f);
            }
            foreach(GunshotKind kind in Enum.GetValues(typeof(GunshotKind)))
            {
                a.PlayGun(kind,g.Player.Shoulder);
                string clip=a.LastClip;float peak=0,until=Time.unscaledTime+.4f;
                while(Time.unscaledTime<until){peak=Mathf.Max(peak,a.OutputPeak());yield return null;}
                report.levels.Add(kind+" "+clip+" peak="+peak.ToString("F5"));
                Check(clip.StartsWith("gun_")&&peak>.0001f,"Real recorded firearm output: "+kind);
                yield return new WaitForSeconds(1.2f);
            }
            a.SetSfxVolume(0);int muted=a.Played;a.Play("jump",g.Player.Shoulder);Check(a.Played==muted,"SFX mute is independent of master");a.SetSfxVolume(.9f);
            a.SetPaused(true);int paused=a.Played;a.Play("jump",g.Player.Shoulder);Check(a.Played==paused,"Paused effects do not trigger");a.SetPaused(false);
            int footsteps=a.Count("step_tile");g.Input.ExternalFrame=Frame(Vector2.right);yield return new WaitForSeconds(1.2f);g.Input.ExternalFrame=ControlFrame.Empty;
            Check(a.Count("step_tile")>footsteps,"Actual running emits footfalls");
            int jumps=a.Count("jump");g.Input.ExternalFrame=Frame(Vector2.zero,true);yield return null;g.Input.ExternalFrame=ControlFrame.Empty;yield return new WaitForSeconds(.2f);
            g.Input.ExternalFrame=Frame(Vector2.zero,true);yield return null;g.Input.ExternalFrame=ControlFrame.Empty;yield return new WaitForSeconds(.2f);
            Check(a.Count("jump")>=jumps+2,"Ground and double jump both emit sound");
            yield return new WaitForSeconds(1.1f);
            int attacks=a.Count("blade_swing");g.Input.ExternalFrame=Frame(Vector2.zero,false,true,0);yield return new WaitForSeconds(.8f);g.Input.ExternalFrame=ControlFrame.Empty;
            Check(a.Count("blade_swing")>attacks,"Actual katana attack emits a swing");
            int hurt=a.Count("hurt");g.Player.ReceiveDamage(4,g.Player.transform.position+Vector3.right);yield return null;Check(a.Count("hurt")>hurt,"Player damage emits impact feedback");
            var anchor=new GameObject("SFX probe rope anchor").AddComponent<GrappleAnchor>();anchor.transform.position=g.Player.Shoulder+Vector3.up*5;
            int attach=a.Count("rope_attach"),release=a.Count("rope_release");g.Player.Rope.Attach(anchor);yield return new WaitForSeconds(.12f);g.Player.Rope.Release();
            Check(a.Count("rope_attach")>attach&&a.Count("rope_release")>release,"Actual rope attachment and release emit sound");Destroy(anchor.gameObject);
            g.Input.ExternalFrame=ControlFrame.Empty;
            var cam=Camera.main;g.CameraRig.enabled=false;cam.transform.SetPositionAndRotation(origin+new Vector3(0,6,-7),Quaternion.LookRotation(new Vector3(0,-6,7)));cam.farClipPlane=35;
            g.Player.Respawn(origin+Vector3.left*12+Vector3.up*.1f);
            WorldActor Person(string name,Vector3 at,string art)
            {
                var go=new GameObject(name,typeof(SpriteRenderer),typeof(WorldActor));go.layer=9;go.transform.position=at;
                go.transform.rotation=Quaternion.Euler(cam.transform.eulerAngles.x,cam.transform.eulerAngles.y,86);
                var r=go.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get(art,0);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
                return go.GetComponent<WorldActor>();
            }
            var source=new GameObject("SFX probe source").AddComponent<WorldActor>();source.police=true;
            var body=Person("Citizen corpse",origin+new Vector3(-1,.05f,0),"CivilianMan");body.Damage(90,Vector3.right,source);
            yield return new WaitForSeconds(.8f);
            var blood=body.GetComponent<CorpseBlood>();
            Check(blood&&blood.HasPool,"Dead citizen creates a visible floor blood pool");
            Check(blood&&Mathf.Abs(blood.FloorPoint.y-origin.y)<.04f,"Blood projects onto raised floors instead of world zero");
            Capture("corpse-blood");
            var mark=blood;body.ResetHealth();yield return null;yield return null;
            Check(!mark,"Reviving or recycling a citizen removes its blood");
            body.health=0;yield return new WaitForSeconds(.2f);mark=body.GetComponent<CorpseBlood>();body.gameObject.SetActive(false);yield return null;
            Check(!mark.HasPool&&!CorpseBlood.All.Contains(mark),"Hiding a corpse removes its pool immediately");
            body.gameObject.SetActive(true);yield return new WaitForSeconds(.2f);mark=body.GetComponent<CorpseBlood>();Destroy(body.gameObject);yield return null;
            Check(!mark,"Destroying a corpse cleans its blood component");
            Check(a.MissingCues==0,"No missing effect cues encountered");
            Finish();
        }
        void Capture(string name)
        {
            var cam=Camera.main;var ui=FindObjectsByType<Canvas>();foreach(var c in ui)c.enabled=false;Canvas.ForceUpdateCanvases();
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);foreach(var c in ui)c.enabled=true;
        }
        void Finish()
        {
            if(finished)return;finished=true;LifeState.SuppressSave=true;CityChronicle.SuppressSave=true;
            foreach(var item in saved)PlayerPrefs.SetFloat(item.Key,item.Value);foreach(var key in absent)PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"sound.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
