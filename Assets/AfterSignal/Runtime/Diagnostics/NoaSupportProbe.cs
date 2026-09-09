using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace AfterSignal
{
    // Opt-in check of the real countdown and shipping dialogue UI. No API calls or progress writes.
    public sealed class NoaSupportProbe : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool completed;
            public float secondsUntilClear;
            public List<string> passed=new(),errors=new();
        }
        readonly Report report=new();
        string output;
        float began;
        bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(!Environment.GetCommandLineArgs().Contains("-noa-support-probe")||FindAnyObjectByType<NoaSupportProbe>())return;
            GameDirector.SkipTitle=true;
            new GameObject("Noa support essentials").AddComponent<NoaSupportProbe>();
        }
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            LifeState.SuppressSave=CityChronicle.SuppressSave=RespawnNetwork.SuppressSave=true;
            output=Path.GetFullPath("Artifacts/NoaSupport/Native");Directory.CreateDirectory(output);
            began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;
        }
        void Log(string text,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception)report.errors.Add(text+"\n"+stack);}
        void Check(bool value,string description)=>(value?report.passed:report.errors).Add(description);
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        IEnumerator Start()
        {
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!WantedSystem.Instance||!CityLife.Instance)yield return null;
            var game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;
            Set(game,"<Title>k__BackingField",false);Set(game,"<Fade>k__BackingField",0f);game.CloseDialogue();game.SetPaused(false);
            WantedSystem.Clear("");yield return new WaitForSeconds(.5f);
            var support=WantedSystem.Instance.NoaSupport;
            WantedSystem.ConfirmReport(8,game.Player.transform.position);
            float start=Time.realtimeSinceStartup;
            yield return new WaitForSeconds(.15f);
            string first=support.Line;
            Check(WantedSystem.Level==1&&support.Clearing&&support.Visible&&!game.Blocked,"Wanted status starts Noa radio without blocking player control");
            Capture("noa-removing");
            game.SetPaused(true);float beforePause=support.Remaining;
            yield return new WaitForSecondsRealtime(.35f);
            Check(Mathf.Abs(beforePause-support.Remaining)<.01f,"Pause preserves the deletion countdown");game.SetPaused(false);
            WantedSystem.ConfirmReport(160,game.Player.transform.position);
            yield return null;
            Check(WantedSystem.Level==5&&support.Remaining<=beforePause,"Escalation to five stars does not restart Noa's countdown");
            var witness=WorldActor.All.FirstOrDefault(a=>a&&a.Alive&&!a.police&&!a.gang&&!a.monster);
            if(witness)CrimeObservation.Queue(witness,9,game.Player.transform.position);
            while(WantedSystem.Level>0&&Time.realtimeSinceStartup-start<8)yield return null;
            report.secondsUntilClear=Time.realtimeSinceStartup-start-.35f;
            Check(WantedSystem.Level==0&&LifeState.Heat==0&&report.secondsUntilClear<6,"Noa clears even five-star pursuit within about four active seconds");
            Check(!CrimeObservation.Instance||CrimeObservation.Instance.PendingCalls==0,"Pending witness reports are removed with pursuit records");
            Check(support.Visible&&!support.Clearing,"Noa confirms completion after the pursuit ends");
            yield return null;Capture("noa-cleared");
            WantedSystem.ConfirmReport(22,game.Player.transform.position);yield return new WaitForSeconds(.1f);
            Check(support.Clearing&&support.Line!=first,"A later pursuit gets a different random line");
            WantedSystem.Clear("");yield return null;
            Check(!support.Visible&&!support.Clearing,"External clear/death/arrest cancels the pending support message");
            var seo=StoryPortraits.Bust("서하");var noa=StoryPortraits.Bust("노아");
            Check(seo&&noa&&seo.texture==StoryPortraits.Get("서하").texture&&seo==StoryPortraits.Bust("서하"),"Enlarged busts retain source detail and reuse the cached sprite");
            game.ShowDialogue("서하","노아, 들려? 얼굴까지 선명하게 보이네.\n신호는 안정적이야. 이쪽 사람들은 모두 무사해.");
            yield return new WaitForSeconds(.2f);Capture("seo-dialogue");game.CloseDialogue();
            CityLife.Instance.StoryDialogue("끊어진 신호", "노아", "서하, 네 신호 잡았어. 수배 기록은 내가 지웠으니까 잠깐 숨 돌려.\n다음 구역에도 사람이 남아 있어. 준비되면 같이 길을 찾자.",false,_=>{});
            yield return new WaitForSeconds(.2f);
            Check(FindObjectsByType<Image>().Any(i=>i.isActiveAndEnabled&&i.sprite==noa&&i.rectTransform.rect.height>=400),"Story dialogue shows Noa in a large upper-body portrait");
            Capture("noa-story");game.CloseDialogue();
            typeof(CityLife).GetMethod("Panel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(CityLife.Instance,new object[]{"talk","민재 · 기술자","서하: 오늘은 좀 어때요?\n\n괜찮아. 네 덕분에 골목도 조용해졌어. 여기 앉아서 잠깐 쉬었다 가."});
            yield return new WaitForSeconds(.2f);Capture("conversation");
            Check(FindObjectsByType<Image>().Any(i=>i.isActiveAndEnabled&&i.sprite==seo&&i.rectTransform.rect.width>=390),"Ordinary conversation enlarges Seoha while keeping the input and send button visible");
            Finish();
        }
        void Capture(string name)
        {
            var camera=Camera.main;
            var overlays=FindObjectsByType<Canvas>().Where(c=>c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=.7f;}
            Canvas.ForceUpdateCanvases();
            var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
            var prior=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            var cameraData=camera.GetUniversalAdditionalCameraData();bool post=cameraData.renderPostProcessing;cameraData.renderPostProcessing=false;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            cameraData.renderPostProcessing=post;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);Destroy(image);
            foreach(var c in overlays)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>100){report.errors.Add("Noa support probe timed out");Finish();}}
        void Finish()
        {
            if(finished)return;finished=true;report.completed=report.errors.Count==0;
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);
        }
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
