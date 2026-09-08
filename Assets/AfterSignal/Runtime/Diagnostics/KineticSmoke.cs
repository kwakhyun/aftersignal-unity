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
    public sealed class KineticSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new List<string>(),errors=new List<string>();}
        readonly Report report=new Report();readonly Dictionary<string,int> ints=new Dictionary<string,int>();readonly Dictionary<string,float> floats=new Dictionary<string,float>();readonly List<string> absent=new List<string>();
        string output,savedStory;bool hadStory,finished,error;float began;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-kinetic-smoke")&&!FindAnyObjectByType<KineticSmoke>()){GameDirector.SkipTitle=true;new GameObject("Kinetic essential check").AddComponent<KineticSmoke>();}}
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;began=Time.realtimeSinceStartup;LifeState.SuppressSave=true;CityChronicle.SuppressSave=true;
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Kinetic/Smoke"));Directory.CreateDirectory(output);
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
            yield return Load(StageId.UrbanCity);var g=GameDirector.Instance;
            Check(SeoKinetic.Directions.All(d=>SeoKinetic.Sheet("Run"+d).Length==8),"Eight directions load eight run phases each");
            Check(SeoKinetic.Directions.All(d=>SeoKinetic.Sheet("Combat"+d).Length==12),"Three weapons have four attack phases in each of eight directions");
            Check(SeoKinetic.Sheet("Idle").Length==8&&SeoKinetic.Sheet("Traversal").Length==16,"Standing, jump and climbing atlases are available");
            var origin=new Vector3(400,120,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="QA isolated floor";floor.transform.position=origin+Vector3.down*.5f;floor.transform.localScale=new Vector3(90,1,90);
            g.Player.Respawn(origin+Vector3.up*.1f);g.CameraRig.Snap();yield return new WaitForSeconds(.3f);
            g.Input.ExternalFrame=Input(Vector2.right);yield return new WaitForSeconds(.55f);Check(g.Player.Running&&new Vector2(g.Player.Velocity.x,g.Player.Velocity.z).magnitude>5,"A single movement press uses running speed");
            g.Input.ExternalFrame=Input(Vector2.zero,true);yield return null;g.Input.ExternalFrame=Input(Vector2.zero);yield return new WaitForSeconds(.18f);
            Check(g.Player.JumpsUsed==1&&!g.Player.Grounded,"First jump leaves the ground");
            g.Input.ExternalFrame=Input(Vector2.zero,true);yield return null;g.Input.ExternalFrame=Input(Vector2.zero);yield return new WaitForSeconds(.1f);
            Check(g.Player.JumpsUsed==2&&g.Player.Velocity.y>3,"Second airborne press adds a second jump");
            float before=g.Player.Velocity.y;g.Input.ExternalFrame=Input(Vector2.zero,true);yield return null;g.Input.ExternalFrame=Input(Vector2.zero);
            Check(g.Player.JumpsUsed==2&&g.Player.Velocity.y<=before+.2f,"Third airborne jump is rejected");
            g.Player.Respawn(origin+Vector3.up*.1f);yield return new WaitForSeconds(.25f);
            var forward=g.CameraRig.MoveDirection(Vector2.up).normalized;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA climbable facade";wall.transform.position=origin+forward*2.3f+Vector3.up*3.5f;wall.transform.rotation=Quaternion.LookRotation(forward);wall.transform.localScale=new Vector3(8,7,1.5f);
            g.Input.ExternalFrame=Input(Vector2.up,true);yield return null;g.Input.ExternalFrame=Input(Vector2.up);float attachDeadline=Time.time+1.5f;while(!g.Player.WallClimbing&&Time.time<attachDeadline)yield return null;
            Check(g.Player.WallClimbing,"Jumping against a facade grabs the wall");
            Debug.Log("Climb attachment: "+g.Player.transform.position+" velocity="+g.Player.Velocity+" energy="+g.Player.Energy);
            float y=g.Player.transform.position.y;yield return new WaitForSeconds(.4f);Check(g.Player.transform.position.y>y+.7f,"Holding forward climbs the wall");
            g.Input.ExternalFrame=Input(Vector2.up,true);yield return null;g.Input.ExternalFrame=Input(Vector2.zero);yield return new WaitForSeconds(.1f);
            Check(!g.Player.WallClimbing&&g.Player.Velocity.y>0,"Jump releases the wall with an upward kick");
            g.Player.Respawn(origin+Vector3.up*.1f);yield return new WaitForSeconds(.2f);
            g.Input.ExternalFrame=Input(Vector2.up,true);yield return null;g.Input.ExternalFrame=Input(Vector2.up);float roofDeadline=Time.time+3.5f;while(Time.time<roofDeadline&&g.Player.transform.position.y<origin.y+6.8f)yield return null;
            Check(g.Player.transform.position.y>=origin.y+6.8f,"Climbing reaches the roof and mantles the ledge");
            g.Input.ExternalFrame=Input(Vector2.zero);Destroy(wall);
            var sim=UrbanSimulation.Instance;
            var parked=sim.Spawn(origin+Vector3.right*18,false,0);parked.occupied=true;yield return null;parked.GetComponent<VehicleCabin>().SetPassengers(2);
            parked.Damage(5,origin);yield return null;
            var response=parked.GetComponent<VehicleEmergency>();
            Check(response&&response.Ejected==3&&!parked.occupied&&!parked.traffic,"Stopped occupied car evacuates driver and every passenger");
            Check(FindObjectsByType<VehicleSurvivor>().Count(p=>p.GetComponent<WorldActor>().Alive)>=3,"Evacuated passengers exist as living civilians");
            var fleeing=sim.Spawn(origin+Vector3.left*20,false,0);fleeing.occupied=true;yield return null;
            fleeing.route=new[]{fleeing.transform.position+Vector3.right*50,fleeing.transform.position+Vector3.right*75};fleeing.traffic=true;fleeing.waypoint=0;fleeing.speed=8;
            fleeing.Damage(5,origin);Check(fleeing.GetComponent<VehicleEmergency>().Escaping,"Moving occupied car chooses acceleration to escape");
            fleeing.TickTraffic(.1f);Check(fleeing.speed>8,"Escape reaction increases acceleration");
            var bus=sim.Spawn(origin+Vector3.back*22,false,2);bus.occupied=true;yield return null;bus.GetComponent<VehicleCabin>().SetPassengers(4);
            bus.Damage(200,origin);yield return null;var busEmergency=bus.GetComponent<VehicleEmergency>();
            Check(bus.Wrecked&&busEmergency.Ejected==5&&bus.GetComponent<VehicleCabin>().PassengerCount==0,"Bus explosion releases all five occupants and empties cabin");
            Check(FindObjectsByType<VehicleSurvivor>().Count(p=>!p.GetComponent<WorldActor>().Alive)==5,"All exploded bus occupants emerge incapacitated");
            bus.Damage(200,origin);Check(busEmergency.Ejected==5,"Repeated damage cannot duplicate exploded occupants");
            WorldActor Person(string name,Vector3 at,bool police,bool gang)
            {
                var go=new GameObject(name,typeof(SpriteRenderer));go.transform.position=at;var n=go.AddComponent<CityNpc>();n.Configure(name.GetHashCode()&int.MaxValue);
                var actor=go.GetComponent<WorldActor>();actor.police=police;actor.gang=gang;return actor;
            }
            var source=Person("QA officer",origin+Vector3.forward*22,true,false);var victim=Person("QA civilian",origin+Vector3.forward*27,false,false);
            Physics.SyncTransforms();FactionCombat.Fire(source,source.Center+Vector3.forward*.5f,victim.Center,10,9,Color.cyan,false);
            Check(victim.health<70,"Police bullet damages a civilian trigger collider");yield return new WaitForSeconds(.12f);
            source.police=false;source.gang=true;float hp=victim.health;FactionCombat.Fire(source,source.Center+Vector3.forward*.5f,victim.Center,10,9,Color.red,false);
            Check(victim.health<hp,"Gang bullet also damages a civilian");
            var policeCar=PoliceCar.Create(WantedSystem.Instance,origin+Vector3.forward*35);yield return null;
            Check(policeCar.GetComponentInChildren<TextMesh>()&&policeCar.transform.Find("Dedicated police bodywork"),"Police response vehicle has dedicated bodywork and readable markings");
            yield return Load(StageId.Residence);g=GameDirector.Instance;
            var lift=FindAnyObjectByType<MovingLift>();g.Player.Respawn(new Vector3(42,22.08f,0));g.CameraRig.Snap();yield return new WaitForSeconds(.3f);
            lift.Use(g);yield return new WaitForSeconds(5.1f);
            Check(lift.Height<.05f&&g.Player.transform.position.y<.65f,"Apartment elevator carries Seo down through the open shaft");
            lift.Use(g);yield return new WaitForSeconds(5.1f);
            Check(lift.Height>21.9f&&Mathf.Abs(g.Player.transform.position.y-22)<.65f,"Apartment elevator returns the rider to the sixth floor");
            Capture("elevator-hud",true);
            yield return Gallery();
            Finish();
        }
        IEnumerator Gallery()
        {
            var g=GameDirector.Instance;g.SetPaused(true);g.CameraRig.enabled=false;var cam=Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0,3.5f,-18),Quaternion.identity);cam.orthographic=true;cam.orthographicSize=4.65f;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.01f,.035f,.055f);cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;RenderSettings.fog=false;
            foreach(var clock in FindObjectsByType<CityClock>())clock.enabled=false;
            var pieces=new List<GameObject>();
            for(int row=0;row<3;row++)for(int i=0;i<8;i++)
            {
                var go=new GameObject("Directional sprite "+row+" "+i,typeof(SpriteRenderer));go.layer=31;go.transform.position=new Vector3(-7+i*2,5.6f-row*2.7f,0);var r=go.GetComponent<SpriteRenderer>();
                r.flipX=row==0?i==2:row==1?SeoKinetic.RunFlip(i):i==7;
                r.sprite=row==0?SeoKinetic.Frame("Idle",i):row==1?SeoKinetic.Frame("Run"+SeoKinetic.Directions[i],2):SeoKinetic.Frame("Combat"+SeoKinetic.Directions[i],10);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");pieces.Add(go);
            }
            yield return null;Capture("directional-gallery",false);
            foreach(var go in pieces)Destroy(go);
            yield return null;
            for(int row=0;row<3;row++)for(int i=0;i<8;i++)
            {
                var go=new GameObject("Run sequence",typeof(SpriteRenderer));go.layer=31;go.transform.position=new Vector3(-7+i*2,5.6f-row*2.7f,0);var r=go.GetComponent<SpriteRenderer>();
                r.sprite=SeoKinetic.Frame("Run"+new[]{"Front","Right","BackRight"}[row],i);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
            }
            yield return null;Capture("run-sequences",false);
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
            report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"kinetic.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);
        }
    }
}