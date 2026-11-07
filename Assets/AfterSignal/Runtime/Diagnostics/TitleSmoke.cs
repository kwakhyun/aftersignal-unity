using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace AfterSignal
{
    // Opt-in start-flow check; it saves into a fresh scratch folder next to its report, so real save slots are never touched.
    public sealed class TitleSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool completed;public List<string> passed=new List<string>(),errors=new List<string>(); }
        readonly Report report=new Report();
        string output;bool finished;float began;
        bool ArtworkOnly => Environment.GetCommandLineArgs().Contains("-title-artwork-only");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-title-smoke")&&!FindAnyObjectByType<TitleSmoke>())new GameObject("Title essential check").AddComponent<TitleSmoke>();}
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;began=Time.realtimeSinceStartup;
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Title/Final"));Directory.CreateDirectory(output);
            if(ArtworkOnly){LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;Application.logMessageReceived+=Log;return;}
            // New Game, Continue and the pause-menu save all really write (and flush on quit), so they go to a scratch folder.
            // The reset leaves an empty slot even when slot 1 would import the machine's legacy PlayerPrefs progress.
            string saves=Path.Combine(output,"saves");if(Directory.Exists(saves))Directory.Delete(saves,true);
            SaveStore.Directory=saves;SaveStore.ResetActiveSlot();
            Application.logMessageReceived+=Log;
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(message+"\n"+trace);}
        void Check(bool condition,string label){(condition?report.passed:report.errors).Add(label);}
        void Update(){if(!finished&&(report.errors.Count>0||Time.realtimeSinceStartup-began>100)){if(report.errors.Count==0)Check(false,"Timed out");Finish();}}
        IEnumerator Ready()
        {
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            GameDirector.Instance.Input.ExternalControl=true;GameDirector.Instance.Input.ExternalFrame=ControlFrame.Empty;
        }
        IEnumerator Start()
        {
            yield return Ready();yield return new WaitForSecondsRealtime(.9f);
            var g=GameDirector.Instance;var title=TitleScreen.Instance;
            Check(g.Title&&title.Visible&&Time.timeScale==0&&!g.CameraRig.CanLook,"Launch presents a paused title with free cursor");
            Check(title.ArtworkLoaded,"Dedicated illustration loads at runtime");
            Check(FindObjectsByType<Canvas>().Where(c=>c.name.StartsWith("HUD")).All(c=>!c.enabled),"World HUD is hidden behind title");
            if(ArtworkOnly){Capture("title-composition");Finish();yield break;}
            SaveStore.DeleteKey("AFTERSIGNAL.Unity.Stage");title.Refresh();yield return null;
            Check(!title.ContinueButton.interactable,"Continue is disabled without a save");
            Capture("title-new");
            SaveStore.SetInt("AFTERSIGNAL.Unity.Stage",(int)StageId.Residence);title.Refresh();
            title.OpenSettings();yield return null;
            Check(title.SettingsOpen&&Time.timeScale==0,"Settings opens without unpausing the world");Capture("title-settings");
            title.CancelNewGame();title.NewGameButton.onClick.Invoke();yield return null;if(title.SlotsOpen){title.SelectSlot(SaveStore.ActiveSlot);yield return null;}
            Check(title.ConfirmationOpen,"New game asks before replacing existing progress");Capture("title-confirm");
            title.CancelNewGame();
            Check(!title.ConfirmationOpen&&SaveStore.GetInt("AFTERSIGNAL.Unity.Stage")== (int)StageId.Residence,"Cancelling new game preserves the saved checkpoint");
            Capture("title-continue");
            title.ContinueButton.onClick.Invoke();yield return null;
            Check(g.Transition&&g.Title,"Continue shows loading state and blocks repeat activation");
            while(GameDirector.Instance==g)yield return null;yield return Ready();g=GameDirector.Instance;yield return null;
            Check(g.stage==StageId.Residence&&!g.Title&&Time.timeScale==1,"Continue loads saved residence into gameplay");
            g.SetPaused(true);g.ReturnToTitle();yield return new WaitForSecondsRealtime(.8f);
            Check(g.Title&&TitleScreen.Instance.Visible&&GameDirector.HasSavedGame&&Time.timeScale==0,"Pause return saves progress and opens title");
            TitleScreen.Instance.NewGameButton.onClick.Invoke();yield return null;if(TitleScreen.Instance.SlotsOpen){TitleScreen.Instance.SelectSlot(SaveStore.ActiveSlot);yield return null;}TitleScreen.Instance.ConfirmNewGame();
            while(GameDirector.Instance==g)yield return null;yield return Ready();g=GameDirector.Instance;yield return null;
            Check(g.stage==StageId.Residence&&!g.Title&&LifeState.Credits==1500&&LifeState.Day==1,"Confirmed new game starts at home with fresh life state");
            Check(CityChronicle.Instance.CompletedMain==0,"Confirmed new game resets story progress");
            Finish();
        }
        void Capture(string name)
        {
            var cam=Camera.main;var overlays=FindObjectsByType<Canvas>().Where(c=>c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=.7f;}Canvas.ForceUpdateCanvases();
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);foreach(var c in overlays)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Finish()
        {
            if(finished)return;finished=true;report.completed=report.errors.Count==0;
            File.WriteAllText(Path.Combine(output,"title.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
