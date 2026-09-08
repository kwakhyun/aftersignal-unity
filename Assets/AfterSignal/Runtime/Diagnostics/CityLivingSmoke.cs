using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class CityLivingSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new List<string>(),errors=new List<string>();}
        readonly Report report=new Report();
        string output;
        bool finished;
        readonly string[] keys={"AFTERSIGNAL.Unity.Stage","AFTERSIGNAL.Unity.Memories"};
        readonly Dictionary<string,int> saved=new Dictionary<string,int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(!Environment.GetCommandLineArgs().Contains("-city-living-smoke")||FindAnyObjectByType<CityLivingSmoke>())return;
            GameDirector.SkipTitle=true;new GameObject("City living integration").AddComponent<CityLivingSmoke>();
        }
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=true;
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CityLiving/Smoke"));Directory.CreateDirectory(output);
            foreach(var key in keys)if(PlayerPrefs.HasKey(key))saved[key]=PlayerPrefs.GetInt(key);
            Application.logMessageReceived+=Log;
        }
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(message+"\n"+stack);}
        void Update(){if(!finished&&Time.realtimeSinceStartup>110){Check(false,"Timed out");Finish();}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Check(bool value,string label){(value?report.passed:report.errors).Add(label);}
        IEnumerator Ready()
        {
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;
            if(g.Dialogue)g.CloseDialogue();g.SetPaused(false);yield return null;
        }
        IEnumerator Start()
        {
            if(Environment.GetCommandLineArgs().Contains("-home-review-only"))
            {
                LifeState.Hours=22;ResidentialWorld.VisitHome=2;ResidentialWorld.VisitResident=19;
                yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanInterior));
                yield return Ready();yield return new WaitForSecondsRealtime(1);
                Capture(Camera.main,"resident-home");Finish();yield break;
            }
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Haven));yield return Ready();
            var game=GameDirector.Instance;
            game.Player.Respawn(new Vector3(214,.1f,-7),false);
            float deadline=Time.realtimeSinceStartup+10;
            while(GameDirector.Instance&&GameDirector.Instance.stage!=StageId.UrbanCity&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return Ready();game=GameDirector.Instance;
            Check(game.stage==StageId.UrbanCity,"Avenue proximity automatically enters the city");
            string[] sheets={"Base","MoveFront","MoveBack","MoveLeft","MoveRight","Context","Katana","Greatsword","Pistol","Rope"};
            foreach(var sheet in sheets)Check(Resources.LoadAll<Sprite>("Art/SeoIllustrated/"+sheet).Length==8,sheet+": eight replacement frames");
            Check(Resources.Load<Sprite>("Art/Portraits/Seo"),"Conversation illustration loads");
            yield return new WaitForSeconds(2);
            Check(CityBusService.Instance.Lines.Count==5,"Five bus lines serve all 25 existing stops");
            var line=CityBusService.Instance.Lines[0];line.Car.transform.position=line.Car.route[0];line.Prepare(.02f);
            game.Player.Respawn(line.BoardPoint,false);
            LifeState.Earn(500);
            int before=LifeState.Credits;
            Check(CityBusService.Instance.Board(line)&&LifeState.Credits==before-CityBusService.Fare,"Bus boarding charges the fare");
            yield return new WaitForSeconds(.4f);
            Check(!game.Player.Controller.enabled&&CityBusService.Instance.Riding==line.Car,"Passenger mode disables driving and ground controller");
            Check(line.Car.GetComponent<VehicleCabin>().PassengerCount>0,"Bus has visible passengers");
            Capture(Camera.main,"bus-ride");
            CityBusService.Instance.Leave();
            Check(game.Player.Controller.enabled&&!CityBusService.Instance.Riding,"Safe bus disembark restores player control");
            Check(FindObjectsByType<ResidentialCitizen>().Length==24&&FindObjectsByType<ResidentialDoor>().Length==5,"Residents and visitable houses are installed");
            game.enabled=false;
            game.Player.Respawn(new Vector3(300,100,0));
            var move=ControlFrame.Empty;move.move=Vector2.right;move.pointer=new Vector2(800,450);
            for(int i=0;i<50;i++)game.Player.Tick(move,.02f);
            float walk=game.Player.Velocity.x;move.run=true;
            for(int i=0;i<50;i++)game.Player.Tick(move,.02f);
            Check(game.Player.Running&&game.Player.Velocity.x>walk*1.7f,"Running has a separate faster gait");
            game.enabled=true;
            var life=CityLife.Instance;
            life.BeginShift(true);yield return new WaitForSecondsRealtime(.25f);
            life.Options[0].action();
            life.PrepareOrder(LifeState.Day%3);
            yield return new WaitForSecondsRealtime(.77f);
            Capture(Camera.main,"cafe-minigame");
            life.FinishPreparation();before=LifeState.Credits;life.ServeTable(0);
            Check(LifeState.Credits>=before+40,"Correct preparation and serving earns wages");
            life.Options[life.Options.Count-1].action();
            typeof(CityLife).GetMethod("Panel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(life,new object[]{"talk","보라 · 대학생","서하: 오늘 하루는 어땠어요?\n\n도서관에서 공부하다가 카페에 들렀어요. 저녁에는 북문아파트로 돌아가 쉴 생각이에요."});
            typeof(CityLife).GetProperty("Sending").SetValue(life,true);
            typeof(CityLife).GetProperty("SendingStarted").SetValue(life,Time.unscaledTime);
            yield return new WaitForSecondsRealtime(.3f);
            Capture(Camera.main,"dialogue-loading");
            life.Dismiss();
            LifeState.Hours=22;
            ResidentialWorld.EnterHome(2,19);
            yield return new WaitForSecondsRealtime(2);yield return Ready();
            Check(ResidentialWorld.VisitHome==2&&GameObject.Find("Furnished resident home"),"Resident home opens with furnished 3D rooms");
            yield return new WaitForSecondsRealtime(.5f);
            Check(FindObjectsByType<ResidentialCitizen>().Any(c=>c.identity==19&&c.GetComponent<SpriteRenderer>().enabled),"Resident is at home at night");
            Capture(Camera.main,"resident-home");
            LifeState.Hours=12;yield return new WaitForSecondsRealtime(1.1f);
            Check(FindObjectsByType<ResidentialCitizen>().All(c=>!c.GetComponent<SpriteRenderer>().enabled),"Resident leaves home during working hours");
            Check(ResidentialWorld.ExitHome(),"Home exit returns to the correct street entrance");
            yield return new WaitForSecondsRealtime(2);yield return Ready();
            yield return Gallery();
            Finish();
        }
        IEnumerator Gallery()
        {
            var game=GameDirector.Instance;game.SetPaused(true);game.CameraRig.enabled=false;
            var cam=Camera.main;cam.transform.SetPositionAndRotation(new Vector3(0,0,-20),Quaternion.identity);
            cam.orthographic=true;cam.orthographicSize=8;cam.cullingMask=1<<31;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.12f,.15f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            RenderSettings.fog=false;
            foreach(var clock in FindObjectsByType<CityClock>())clock.enabled=false;
            foreach(var c in FindObjectsByType<Canvas>())c.enabled=false;
            var actors=new List<SpriteRenderer>();
            string[] names={"Base","MoveFront","MoveBack","MoveLeft","MoveRight","Context","Katana","Greatsword","Pistol","Rope"};
            for(int n=0;n<names.Length;n++)
            {
                for(int f=0;f<8;f++)
                {
                    var go=new GameObject(names[n]+f,typeof(SpriteRenderer));go.layer=31;
                    go.transform.position=new Vector3(-12+f*3.4f,5.3f-(n%5)*3.1f,0);
                    var r=go.GetComponent<SpriteRenderer>();r.sprite=SeoSpriteSet.Frame(names[n],f);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");actors.Add(r);
                }
                if(n==4||n==9){yield return null;Capture(cam,n==4?"seo-movement":"seo-actions");foreach(var actor in actors)actor.enabled=false;}
            }
        }
        void Capture(Camera cam,string name)
        {
            var canvases=FindObjectsByType<Canvas>();var changed=new List<Canvas>();
            foreach(var c in canvases)if(c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;changed.Add(c);}
            Canvas.ForceUpdateCanvases();
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;
            var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);
            foreach(var c in changed)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Finish()
        {
            finished=true;report.completed=report.errors.Count==0;
            foreach(var key in keys)if(saved.TryGetValue(key,out var value))PlayerPrefs.SetInt(key,value);else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(output,"city-living.json"),JsonUtility.ToJson(report,true));
            Application.Quit(report.completed?0:1);
        }
    }
}
