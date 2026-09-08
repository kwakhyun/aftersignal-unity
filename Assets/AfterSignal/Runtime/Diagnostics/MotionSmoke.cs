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
    public sealed class MotionSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new List<string>(),errors=new List<string>();}
        readonly Report report=new Report();readonly Dictionary<string,int> ints=new Dictionary<string,int>();readonly Dictionary<string,float> floats=new Dictionary<string,float>();readonly List<string> absent=new List<string>();
        string output,savedStory;bool hadStory,finished,error;float began;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-motion-smoke")&&!FindAnyObjectByType<MotionSmoke>()){GameDirector.SkipTitle=true;new GameObject("Vehicle motion check").AddComponent<MotionSmoke>();}}
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;began=Time.realtimeSinceStartup;LifeState.SuppressSave=true;CityChronicle.SuppressSave=true;
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Motion/Smoke"));Directory.CreateDirectory(output);
            foreach(var k in new[]{"AFTERSIGNAL.Unity.Stage","AFTERSIGNAL.Unity.Memories",UrbanCatalog.Prefix+"Site",UrbanCatalog.Prefix+"Car",UrbanCatalog.Prefix+"CarStage",UrbanCatalog.Prefix+"CarType"}){if(PlayerPrefs.HasKey(k))ints[k]=PlayerPrefs.GetInt(k);else absent.Add(k);}
            foreach(var n in new[]{"CarX","CarZ","CarYaw","CarFuel","CarHealth"}){string k=UrbanCatalog.Prefix+n;if(PlayerPrefs.HasKey(k))floats[k]=PlayerPrefs.GetFloat(k);else absent.Add(k);}
            hadStory=PlayerPrefs.HasKey(CityChronicle.SaveKey);savedStory=PlayerPrefs.GetString(CityChronicle.SaveKey,"");
            Application.logMessageReceived+=Log;
        }
        void Log(string m,string s,LogType t){if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert){report.errors.Add(m+"\n"+s);error=true;}}
        void Check(bool ok,string name){(ok?report.passed:report.errors).Add(name);}
        void Update(){if(!finished&&(error||Time.realtimeSinceStartup-began>180)){if(!error)Check(false,"Timeout");Finish();}}
        IEnumerator Load(StageId stage)
        {
            LifeState.Heat=0;ResidentialWorld.VisitHome=-1;
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(stage));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);
            yield return new WaitForSeconds(1.2f);
        }
        ControlFrame Input(Vector2 move,bool jump=false){var f=ControlFrame.Empty;f.pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);f.move=move;f.jump=jump;return f;}
        IEnumerator Start()
        {
            yield return Load(StageId.UrbanCity);var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;
            var dirs=new[]{"Front","FrontRight","Right","BackRight","Back"};
            Check(dirs.All(d=>SeoRefined.Sheet("Sprint"+d).Length==8),"Five canonical run views have eight phases");
            Check(dirs.All(d=>SeoRefined.Sheet("Action"+d).Length==8),"All forty action poses load");
            Check(Enumerable.Range(0,8).All(d=>Enumerable.Range(0,8).All(f=>SeoRefined.Run(d,f))),"All eight movement directions resolve");
            Check(new[]{"CabinSeo","CabinCitizens","CabinCommunity","CabinServices","CabinSpecialists"}.All(n=>VehiclePortraits.Sheet(n).Length==16),"Eighty cabin sprites load");
            Check(PeopleArt.Citizens.All(n=>Enumerable.Range(0,4).All(d=>VehiclePortraits.Passenger(n,d).rect.height<PeopleArt.Get(n,d).rect.height*.7f)),"All citizen views are cropped to seated torsos");
            var origin=new Vector3(400,120,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=origin+Vector3.down*.5f;floor.transform.localScale=new Vector3(90,1,90);floor.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Materials/UrbanRoad");
            g.Player.Respawn(origin+Vector3.up*.1f);g.CameraRig.Snap();yield return new WaitForSeconds(.3f);
            g.Input.ExternalFrame=Input(Vector2.right);var seen=new HashSet<int>();float until=Time.time+.7f;
            while(Time.time<until){var m=g.Player.GetComponent<SeoLocomotion>();if(m&&m.State=="Run")seen.Add(m.FrameIndex);yield return null;}
            Check(seen.Count>=6,"Running advances six or more phases");
            g.Input.ExternalFrame=Input(Vector2.zero,true);yield return null;g.Input.ExternalFrame=Input(Vector2.zero);yield return new WaitForSeconds(.12f);
            var motion=g.Player.GetComponent<SeoLocomotion>();
            Check(motion.State=="Rise"&&motion.FrameIndex==0,"Jump selects ascent");
            until=Time.time+2;while(Time.time<until&&motion.State!="Fall")yield return null;
            Check(motion.State=="Fall"&&motion.FrameIndex==1,"Descent selects falling pose");
            until=Time.time+2;while(Time.time<until&&motion.State!="Land")yield return null;
            Check(motion.State=="Land"&&motion.FrameIndex==2,"Contact selects landing absorption");
            yield return new WaitForSeconds(.3f);g.Player.ReceiveDamage(1,g.Player.transform.position+Vector3.right,true);yield return new WaitForSeconds(.04f);
            Check(motion.State=="Hurt"&&motion.FrameIndex==3,"Damage selects flinch");
            g.Player.Respawn(origin+Vector3.up*.1f);yield return new WaitForSeconds(.3f);
            var dash=Input(Vector2.right);dash.dash=true;g.Input.ExternalFrame=dash;yield return null;g.Input.ExternalFrame=Input(Vector2.zero);yield return new WaitForSeconds(.03f);
            Check(motion.State=="Dash"&&motion.FrameIndex==4,"Dash selects lunge");
            g.Player.Respawn(origin+Vector3.up*.1f);g.Input.ExternalFrame=Input(Vector2.zero);
            var cars=new List<CityVehicle>();
            for(int type=0;type<4;type++){var car=sim.Spawn(origin+Vector3.forward*(type*9),false,type);car.owned=true;car.occupied=true;cars.Add(car);}
            yield return null;
            foreach(var car in cars)car.GetComponent<VehicleCabin>().SetPassengers(car.type==CityVehicleType.Bus?9:1);
            yield return null;
            Check(cars.All(c=>{var r=c.transform.Find("Visible driver").GetComponent<SpriteRenderer>();float h=r.sprite.bounds.size.y*r.transform.localScale.y;return r.enabled&&h>=.7f&&h<=1.08f&&r.sprite.name.StartsWith("Cabin");}),"All vehicle types have correctly sized seated drivers");
            var bus=cars[2];
            Check(bus.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name=="Visible passenger"&&r.enabled).Select(r=>r.sprite.name).Distinct().Count()>=7,"Bus has seven or more distinct passenger appearances");
            Check(cars[3].transform.Find("Visible driver").localPosition.x>2.5f,"Truck driver sits in front cab");
            Check(sim.Enter(cars[0]),"Seo enters revised cabin");
            g.SetPaused(true);g.CameraRig.enabled=false;var cam=Camera.main;
            foreach(var clock in FindObjectsByType<CityClock>())clock.enabled=false;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;RenderSettings.fog=false;
            cam.fieldOfView=36;cam.transform.position=origin+new Vector3(6,2.25f,-.25f);cam.transform.LookAt(origin+new Vector3(.2f,1.02f,0));
            yield return null;yield return null;
            var driver=cars[0].transform.Find("Visible driver").GetComponent<SpriteRenderer>();
            Check(driver.sprite.name.StartsWith("CabinSeo"),"Seo uses steering wheel upper body");Capture("seo-driver",false);
            sim.Exit();g.Player.Respawn(origin+Vector3.left*25);cars[0].occupied=true;yield return null;Capture("npc-driver",false);
            cam.transform.position=bus.transform.position+new Vector3(7,3.2f,-11);cam.transform.LookAt(bus.transform.position+Vector3.up*1.9f);yield return null;Capture("bus-passengers",false);
            cars[0].Damage(200,origin);yield return null;Check(!driver.enabled,"Wrecked cabin clears driver visual");
            yield return Gallery();Finish();
        }
        IEnumerator Gallery()
        {
            var g=GameDirector.Instance;g.SetPaused(true);g.CameraRig.enabled=false;var cam=Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0,3.5f,-18),Quaternion.identity);cam.orthographic=true;cam.orthographicSize=4.65f;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.01f,.035f,.055f);cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;RenderSettings.fog=false;
            for(int mode=0;mode<2;mode++)
            {
                var pieces=new List<GameObject>();
                for(int row=0;row<3;row++)for(int i=0;i<8;i++)
                {
                    var go=new GameObject("Refined pose",typeof(SpriteRenderer));go.layer=31;go.transform.position=new Vector3(-7+i*2,5.6f-row*2.7f,0);var r=go.GetComponent<SpriteRenderer>();
                    int direction=new[]{0,2,4}[row];bool flip=false;
                    r.sprite=mode==0?SeoRefined.Run(direction,i):SeoRefined.Pose(direction,(SeoMotionPose)i,out flip);
                    r.flipX=flip;r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");pieces.Add(go);
                }
                yield return null;Capture(mode==0?"run-sequences":"action-poses",false);
                foreach(var go in pieces)Destroy(go);yield return null;
            }
        }
        void Capture(string name,bool ui)
        {
            var cam=Camera.main;var overlays=ui?FindObjectsByType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray():new Canvas[0];
            foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=.7f;}Canvas.ForceUpdateCanvases();
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);foreach(var c in overlays)c.renderMode=RenderMode.ScreenSpaceOverlay;
        }
        void Finish()
        {
            if(finished)return;finished=true;CityChronicle.SuppressSave=true;LifeState.SuppressSave=true;
            if(hadStory)PlayerPrefs.SetString(CityChronicle.SaveKey,savedStory);else PlayerPrefs.DeleteKey(CityChronicle.SaveKey);
            foreach(var p in ints)PlayerPrefs.SetInt(p.Key,p.Value);foreach(var p in floats)PlayerPrefs.SetFloat(p.Key,p.Value);foreach(var p in absent)PlayerPrefs.DeleteKey(p);PlayerPrefs.Save();
            report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"motion.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);
        }
    }
}
