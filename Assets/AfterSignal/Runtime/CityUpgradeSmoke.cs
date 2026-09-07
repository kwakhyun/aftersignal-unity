using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    // Explicit opt-in QA. Fixtures are labelled and all touched save keys are restored.
    public sealed class CityUpgradeSmoke:MonoBehaviour
    {
        [Serializable] class Report {public bool completed;public List<string> passed=new List<string>(),errors=new List<string>();public int redStops,pedestrianCrossings,ambientImpacts;public float maxLaneError;public string method="Native Release. 70s unmodified traffic simulation, followed by labelled vehicle/obstacle fixtures. Production ControlFrames drive/brake; no synthetic human-input claim.";}
        static bool initialized;static Dictionary<string,float> saved=new Dictionary<string,float>();static HashSet<string> present=new HashSet<string>();Report report=new Report();GameDirector game;UrbanSimulation sim;QualitySession metrics;string output;bool finished;float started;
        static bool FloatKey(string k)=>k.EndsWith("X")||k.EndsWith("Z")||k.EndsWith("Yaw")||k.EndsWith("Fuel")||k.EndsWith("Health");
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string t,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(t);}
        void Update(){if(started>0&&Time.realtimeSinceStartup-started>160&&!finished){Check(false,"QA timeout");Finish();}}
        IEnumerator Start()
        {
            game=GetComponent<GameDirector>();output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Urban15/Run"));Directory.CreateDirectory(output);
            if(!initialized){initialized=true;foreach(string suffix in new[]{"Site","Car","CarX","CarZ","CarYaw","CarFuel","CarStage","CarHealth","CarType"}){string k=UrbanCatalog.Prefix+suffix;if(PlayerPrefs.HasKey(k))present.Add(k);saved[k]=FloatKey(k)?PlayerPrefs.GetFloat(k):PlayerPrefs.GetInt(k);}UrbanCatalog.Reset();GameDirector.SkipTitle=true;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));yield break;}
            Application.runInBackground=true;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.SetPaused(false);started=Time.realtimeSinceStartup;yield return Hold(1,ControlFrame.Empty);sim=UrbanSimulation.Instance;metrics=gameObject.AddComponent<QualitySession>();
            Check(sim.vehiclePrefabs.Length==4,"Sedan, taxi, bus and truck prefabs");Check(FindObjectsByType<CityTrafficSignal>().Length==30,"30 coordinated intersections");Check(FindObjectsByType<CityBuildingCutaway>().Length>=80,"Every block has additional high-rise massing");Check(Vector3.Distance(Camera.main.transform.position,game.Player.transform.position)<22,"Close pedestrian camera");
            Vector3 destination;string title;Check(QuestGuidance.Resolve(game,FindObjectsByType<InteractionPoint>(),out destination,out title),"Persistent main quest target resolved");yield return Capture("01-close-city");
            float end=Time.realtimeSinceStartup+70;var crossing=new HashSet<CityPedestrian>();var stopCars=new HashSet<CityVehicle>();
            while(Time.realtimeSinceStartup<end){foreach(var c in sim.Cars)if(c&&c.traffic&&c.route!=null){float best=float.MaxValue;for(int i=0;i<c.route.Length;i++){var a=c.route[i];var d=c.route[(i+1)%c.route.Length]-a;float t=Mathf.Clamp01(Vector3.Dot(c.transform.position-a,d)/d.sqrMagnitude);best=Mathf.Min(best,Vector3.Distance(c.transform.position,a+d*t));}report.maxLaneError=Mathf.Max(report.maxLaneError,best);if(c.WaitingAtSignal&&c.speed<.2f)stopCars.Add(c);}
                foreach(var p in CityPopulation.Instance.Citizens)if(p&&p.enteredCrossing&&!p.struck)crossing.Add(p);yield return null;
            }
            report.ambientImpacts=CityPopulation.Instance.Impacts;Check(report.ambientImpacts==0,"Normal traffic does not strike protected pedestrians");report.redStops=stopCars.Count;report.pedestrianCrossings=crossing.Count;Check(report.maxLaneError<.12f,"Traffic follows lane path within 12 cm");Check(report.redStops>0,"Traffic stops before red lights");Check(report.pedestrianCrossings>0,"Citizens use protected crossing phase");yield return Capture("02-traffic-cycle");
            // Park four concrete variants on an empty forecourt for model/collision review.
            for(int i=0;i<4;i++){var c=sim.Spawn(new Vector3(75+i*14,.02f,-252),false,i);Check(c.type==(CityVehicleType)i&&c.GetComponentInChildren<Collider>()&&c.transform.Find("Sculpted chassis"),"Vehicle variant with chassis and collider "+i);}
            game.Player.Respawn(new Vector3(93,.15f,-259),false);game.CameraRig.Snap();yield return Hold(.6f,ControlFrame.Empty);yield return Capture("03-fleet");
            // A clear straight route isolates player handling from random vehicles.
            foreach(var c in sim.Cars)if(c)c.traffic=false;
            for(int i=0;i<4;i++){var v=sim.Spawn(new Vector3(235+i*18,.02f,264),false,i);game.Player.Respawn(v.transform.position+Vector3.back*4,false);game.CameraRig.Snap();yield return Hold(.4f,ControlFrame.Empty);yield return Capture("fleet-"+i);}
            var car=sim.Spawn(new Vector3(210,.02f,275),false,1);game.Player.Respawn(car.transform.position+Vector3.back*3,false);Check(sim.Enter(car),"Taxi can be entered");game.CameraRig.Snap();
            var control=ControlFrame.Empty;control.move=Vector2.up;yield return Hold(5,control);Check(car.Distance>50,"Actual throttle drives over 50 metres");Check(Vector3.Dot(Camera.main.transform.forward,car.Forward)>.8f,"Camera looks forward from behind vehicle");yield return Capture("04-chase-driving");
            control=ControlFrame.Empty;control.jump=true;yield return Hold(1.4f,control);Check(car.speed<.1f,"Brake stops vehicle");Check(car.fuel<45,"Fuel consumption remains active");Check(sim.Exit(),"Exit restores walking control");
            // Collision fixture uses the same swept vehicle physics as ordinary driving.
            car=sim.Spawn(new Vector3(420,.02f,275),false,0);game.Player.Respawn(car.transform.position+Vector3.back*3,false);sim.Enter(car);var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="QA / collision fixture";barrier.transform.position=new Vector3(446,1,275);barrier.transform.localScale=new Vector3(1,2,8);barrier.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Materials/Concrete");
            control=ControlFrame.Empty;control.move=Vector2.up;yield return Hold(3,control);Check(car.Collisions>0&&car.health<100,"Physical collision reduces vehicle health");yield return Capture("05-damaged");
            car.Damage(100,car.transform.position);yield return Hold(.15f,ControlFrame.Empty);yield return Capture("06-explosion");Check(car.Wrecked&&car.Explosions==1&&!sim.Driving,"Destruction explodes once and restores player");car.Damage(100,car.transform.position);Check(car.Explosions==1&&!sim.Enter(car),"Wreck cannot repeat explosion or be driven");yield return Hold(2,ControlFrame.Empty);Destroy(barrier);metrics.Save();Finish();
        }
        void Check(bool ok,string name){if(ok)report.passed.Add(name);else report.errors.Add(name);Debug.Log("CITY UPGRADE QA / "+ok+" / "+name);}
        IEnumerator Hold(float seconds,ControlFrame c){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end){game.Input.ExternalFrame=c;yield return null;}game.Input.ExternalFrame=ControlFrame.Empty;}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;}
        void Finish(){if(finished)return;finished=true;foreach(var kv in saved){if(!present.Contains(kv.Key))PlayerPrefs.DeleteKey(kv.Key);else if(FloatKey(kv.Key))PlayerPrefs.SetFloat(kv.Key,kv.Value);else PlayerPrefs.SetInt(kv.Key,(int)kv.Value);}PlayerPrefs.Save();report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"city-upgrade.json"),JsonUtility.ToJson(report,true));metrics?.Save();Application.Quit(report.completed?0:1);}
        void OnApplicationQuit(){if(initialized&&!finished)foreach(var kv in saved){if(!present.Contains(kv.Key))PlayerPrefs.DeleteKey(kv.Key);else if(FloatKey(kv.Key))PlayerPrefs.SetFloat(kv.Key,kv.Value);else PlayerPrefs.SetInt(kv.Key,(int)kv.Value);}PlayerPrefs.Save();}
    }
}
