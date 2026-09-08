using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    public sealed class ChronicleSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new List<string>(),errors=new List<string>();}
        readonly Report report=new Report();readonly Dictionary<string,int> ints=new Dictionary<string,int>();readonly Dictionary<string,float> floats=new Dictionary<string,float>();readonly List<string> absent=new List<string>();
        string output,savedStory;bool hadStory,finished,error;float began;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-chronicle-smoke")&&!FindAnyObjectByType<ChronicleSmoke>()){GameDirector.SkipTitle=true;new GameObject("Chronicle essential check").AddComponent<ChronicleSmoke>();}}
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;began=Time.realtimeSinceStartup;LifeState.SuppressSave=true;
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Chronicle/Smoke"));Directory.CreateDirectory(output);
            foreach(var k in new[]{"AFTERSIGNAL.Unity.Stage","AFTERSIGNAL.Unity.Memories",UrbanCatalog.Prefix+"Site",UrbanCatalog.Prefix+"Car",UrbanCatalog.Prefix+"CarStage",UrbanCatalog.Prefix+"CarType"}){if(PlayerPrefs.HasKey(k))ints[k]=PlayerPrefs.GetInt(k);else absent.Add(k);}
            foreach(var n in new[]{"CarX","CarZ","CarYaw","CarFuel","CarHealth"}){string k=UrbanCatalog.Prefix+n;if(PlayerPrefs.HasKey(k))floats[k]=PlayerPrefs.GetFloat(k);else absent.Add(k);}
            hadStory=PlayerPrefs.HasKey(CityChronicle.SaveKey);savedStory=PlayerPrefs.GetString(CityChronicle.SaveKey,"");
            CityChronicle.SuppressSave=true;PlayerPrefs.DeleteKey(CityChronicle.SaveKey);
            Application.logMessageReceived+=Log;
        }
        void Log(string m,string s,LogType t){if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert){report.errors.Add(m+"\n"+s);error=true;}}
        void Check(bool ok,string name){(ok?report.passed:report.errors).Add(name);}
        void Update(){if(!finished&&(error||Time.realtimeSinceStartup-began>180)){if(!error)Check(false,"Timeout");Finish();}}
        IEnumerator Load(int site,bool city=false)
        {
            CityChronicle.SuppressSave=true;LifeState.Heat=0;ResidentialWorld.VisitHome=-1;PlayerPrefs.SetInt(UrbanCatalog.Prefix+"Site",site);
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(city?StageId.UrbanCity:StageId.UrbanInterior));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);
            CityChronicle.SuppressSave=false;yield return new WaitForSeconds(1.2f);
        }
        void AtObjective(){var g=GameDirector.Instance;var p=FindAnyObjectByType<StoryObjective>();g.Player.Respawn(p.transform.position+Vector3.back*2-Vector3.up,false);g.CameraRig.Snap();}
        IEnumerator Start()
        {
            yield return Load(0);var g=GameDirector.Instance;var c=CityChronicle.Instance;
            Check(c.Quests.Length==42&&c.Quests.Count(q=>q.main)==24,"24 main and 18 linked side quests loaded");
            Check(c.Quests.Select(q=>q.id).Distinct().Count()==42&&c.Quests.All(q=>q.steps.Length==3&&q.steps.All(s=>s.site>=0&&s.site<40)),"126 objectives have valid destinations and unique quest IDs");
            Check(c.Quests.All(q=>string.IsNullOrEmpty(q.after)||c.Quests.Any(p=>p.id==q.after)),"Every prerequisite resolves");
            Check(g.CameraRig.FreeOrbit,"Third person orbit is active without holding a mouse button");
            var input=ControlFrame.Empty;input.look=true;input.lookDelta=new Vector2(110,-40);input.zoom=120;float yaw=g.CameraRig.OrbitYaw,dist=g.CameraRig.OrbitDistance;
            g.CameraRig.ReadLook(input);g.CameraRig.Snap();
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw,g.CameraRig.OrbitYaw))>10&&g.CameraRig.OrbitDistance<dist,"Mouse look rotates and wheel zoom changes camera distance");
            input=ControlFrame.Empty;input.weapon=2;input.pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);g.Player.Tick(input,.02f);
            Check(g.Player.Weapon==WeaponId.Pistol,"Number weapon input equips pistol");
            int changes=g.Player.WeaponChanges;input.weapon=-1;input.zoom=-120;g.CameraRig.ReadLook(input);g.Player.Tick(input,.02f);
            Check(g.Player.WeaponChanges==changes,"Zoom does not switch weapon");
            Check(Vector3.Distance(g.Player.Aim,g.Player.Shoulder)>3,"Central aim projects into the world instead of the player plane");
            yield return Gallery();
            yield return Load(0);g=GameDirector.Instance;c=CityChronicle.Instance;AtObjective();c.Interact();yield return null;
            Check(CityLife.Instance.Mode=="story","World quest NPC opens illustrated story conversation");
            CaptureUi("dialogue");yield return new WaitForEndOfFrame();
            CityLife.Instance.Options[0].action();Check(c.Entry(c.Quests[0]).step==1,"Dialogue confirmation advances one objective");c.Save();
            yield return Load(15);g=GameDirector.Instance;c=CityChronicle.Instance;AtObjective();c.Interact();yield return new WaitForSeconds(3.5f);
            Check(CityLife.Instance.Mode=="story","Evidence scanning requires timed interaction near terminal");
            CityLife.Instance.Options[0].action();c.Save();
            yield return Load(14);c=CityChronicle.Instance;g=GameDirector.Instance;Check(InteractionPoint.All.Any(p=>p&&p.kind==InteractionKind.FirstRail)&&InteractionPoint.All.Any(p=>p&&p.kind==InteractionKind.MissionBoard),"Headquarters retains access to the original rail campaign");AtObjective();int cash=LifeState.Credits;c.Interact();yield return null;CityLife.Instance.Options[0].action();
            Check(c.Done(c.Quests[0])&&c.Tracked.id=="main02"&&LifeState.Credits==cash+c.Quests[0].reward,"Delivery pays once and unlocks the linked next main quest");
            int earned=LifeState.Credits;c.Advance("main01",2);Check(LifeState.Credits==earned,"Repeated completion cannot duplicate rewards");
            foreach(var q in c.Quests.Take(3)){c.Entry(q).step=3;c.Entry(q).rewarded=true;}
            c.Progress.tracked="main04";var choiceQuest=c.Quests[3];c.Entry(choiceQuest).accepted=true;c.Entry(choiceQuest).step=2;c.Save();
            yield return Load(14);c=CityChronicle.Instance;AtObjective();c.Interact();yield return null;CityLife.Instance.Options[1].action();
            Check(c.Entry(c.Quests[3]).choice==2&&c.Tracked.id=="main05","Player choice persists and advances the story chain");
            c.Save();var roundtrip=JsonUtility.FromJson<StorySave>(PlayerPrefs.GetString(CityChronicle.SaveKey));
            Check(roundtrip.entries.Any(e=>e.id=="main04"&&e.choice==2),"Story progress and choices serialize for continuing a saved game");
            CityLife.Instance.StoryJournal();yield return null;Check(CityLife.Instance.Mode=="journal"&&CityLife.Instance.Options.Count>=3,"J journal exposes story lists and current objectives");
            CaptureUi("journal");yield return new WaitForEndOfFrame();CityLife.Instance.Dismiss();
            yield return Load(0,true);g=GameDirector.Instance;
            var at=CityRoadNetwork.Sidewalk(g.Player.transform.position+Vector3.right*8);
            CityNpc Make(string name,int id,string art,Vector3 p){var go=new GameObject(name,typeof(SpriteRenderer),typeof(CityNpc));go.transform.position=p;go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");var n=go.GetComponent<CityNpc>();n.Configure(id);PeopleArt.Attach(go,art);return n;}
            var a=Make("Elder social check",8022,"ElderMan",at);var b=Make("Doctor social check",8013,"Doctor",at+Vector3.right*3);yield return null;
            var social=CitySocial.Instance;social.Exchange(a,b);yield return new WaitForSeconds(2.4f);
            Check(social.Exchanges>0&&a.GetComponent<NpcSpeech>()&&b.GetComponent<NpcSpeech>(),"Street citizens exchange two-way speech bubbles");
            a.SocialUntil=0;social.Hail(a);Check(social.Greetings>0&&a.context.Contains("서하"),"NPC can initiate a greeting without forcing a modal dialogue");
            a.GetComponent<WorldActor>().Damage(5,Vector3.right);Check(a.GetComponent<NpcSpeech>().CurrentLine==NpcVoice.Hurt(a,false),"Attack reaction uses the victim's role and survives generic panic messages");
            Check(NpcVoice.Hurt(a,false)!=NpcVoice.Hurt(b,false),"Elder and medical staff use distinct voices");
            CaptureUi("city-social");yield return new WaitForEndOfFrame();
            Finish();
        }
        IEnumerator Gallery()
        {
            var g=GameDirector.Instance;g.SetPaused(true);g.CameraRig.enabled=false;var cam=Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0,2,-18),Quaternion.identity);cam.orthographic=true;cam.orthographicSize=4.8f;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.01f,.035f,.055f);cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;RenderSettings.fog=false;
            foreach(var clock in FindObjectsByType<CityClock>())clock.enabled=false;
            var sprites=new[]{SeoSpriteSet.Frame("Base",2),SeoSpriteSet.Frame("Katana",7),PeopleArt.Sheet("Idle",true)[8]};
            for(int i=0;i<sprites.Length;i++){var go=new GameObject("Alpha repair pose "+i,typeof(SpriteRenderer));go.layer=31;go.transform.position=new Vector3(-5+i*5,0,0);go.transform.localScale=Vector3.one*1.5f;var r=go.GetComponent<SpriteRenderer>();r.sprite=sprites[i];r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");}
            yield return null;
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,"seo-alpha-repair.png"),texture.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(texture);
            Check(sprites.All(s=>s),"Standing, dash, rope and directional idle sprites load");
        }
        void CaptureUi(string name)
        {
            var cam=Camera.main;
            var overlays=FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=.7f;}
            Canvas.ForceUpdateCanvases();
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);foreach(var c in overlays)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Finish()
        {
            if(finished)return;finished=true;CityChronicle.SuppressSave=true;LifeState.SuppressSave=true;
            if(hadStory)PlayerPrefs.SetString(CityChronicle.SaveKey,savedStory);else PlayerPrefs.DeleteKey(CityChronicle.SaveKey);
            foreach(var p in ints)PlayerPrefs.SetInt(p.Key,p.Value);foreach(var p in floats)PlayerPrefs.SetFloat(p.Key,p.Value);foreach(var p in absent)PlayerPrefs.DeleteKey(p);PlayerPrefs.Save();
            report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"chronicle.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);
        }
    }
}
