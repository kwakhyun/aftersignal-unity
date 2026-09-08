using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    public sealed class MobilitySmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public int population,parkingLots,traffic,lifts;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began;bool finished;float jail;bool hadJail;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-mobility-smoke")&&!FindAnyObjectByType<MobilitySmoke>()){GameDirector.SkipTitle=true;new GameObject("Mobility verification").AddComponent<MobilitySmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/Mobility/Smoke"));Directory.CreateDirectory(output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){if(report.errors.Count<18)report.errors.Add(text+"\n"+stack);}}
        void Check(bool condition,string label){(condition?report.passed:report.errors).Add(label);Debug.Log("MOBILITY CHECK "+condition+" / "+label);}
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>230){Check(false,"Mobility verification timeout");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);LifeState.Hours=15;
            yield return new WaitForSeconds(3);
            report.population=ExpansionWorld.Instance.Population;report.parkingLots=FindObjectsByType<ParkingLot>().Length;report.lifts=FindObjectsByType<MultiFloorLift>().Length;report.traffic=sim.Cars.Count(c=>c&&c.traffic);
            Check(report.parkingLots>=25&&report.lifts>=12,"Populated walk-in districts, parking lots and multistorey lifts installed");
            Check(Enumerable.Range(4,7).All(t=>sim.Cars.Any(c=>c&&(int)c.type==t)),"All seven new transport types spawn in the world");
            Check(ExpansionRoads.Roads.Count>=17,"Old-city exits and new districts share continuous road data");
            if(!Environment.GetCommandLineArgs().Contains("-mobility-presentation"))
            {
            var car=sim.Spawn(new Vector3(780,.2f,140),false,0);yield return null;Physics.SyncTransforms();
            var frame=ControlFrame.Empty;frame.move=Vector2.up;
            float start=car.transform.position.x,maxY=car.transform.position.y;
            for(int i=0;i<500;i++){car.Drive(frame,.02f);maxY=Mathf.Max(maxY,car.transform.position.y);}
            Check(maxY<1&&car.transform.position.x>start+15,"Five hundred road-driving steps never climb the vehicle's own roof");
            Check(car.GetComponentsInChildren<VehicleWheel>().All(w=>Mathf.Abs(Vector3.Dot(w.WorldAxle,car.transform.forward))>.99f),"Wheel spin axes stay parallel to the actual vehicle axles");
            var lamp=car.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.name=="Headlamp");var tail=car.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.name=="Taillamp");
            Check(lamp&&tail&&car.transform.InverseTransformPoint(lamp.bounds.center).x>0&&car.transform.InverseTransformPoint(tail.bounds.center).x<0,"Visible headlights and taillights align with driving forward and reverse");
            car.speed=0;g.Player.Respawn(car.transform.position+Vector3.forward*3);Check(sim.Enter(car,2)&&sim.SeatIndex==2,"Rear-seat entry is separate from the driver seat");
            var old=car.transform.position;sim.Tick(frame,.1f);Check((car.transform.position-old).sqrMagnitude<.01f,"Passenger input cannot drive the car");Check(sim.Exit()&&g.Player.Controller.enabled,"Rear-seat exit restores character collision");
            Destroy(car.gameObject);
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.transform.position=new Vector3(1450,179.5f,50);deck.transform.localScale=new Vector3(900,1,80);
            var sedan=sim.Spawn(new Vector3(1100,180.03f,50),false,0);var sport=sim.Spawn(new Vector3(1100,180.03f,65),false,5);yield return null;Physics.SyncTransforms();
            frame.boost=true;for(int i=0;i<180;i++){sedan.Drive(frame,.02f);sport.Drive(frame,.02f);}
            Check(sport.speed>sedan.speed+15&&sport.speed>49,"Sports car and held Shift deliver a higher real driving speed");Destroy(sedan.gameObject);Destroy(sport.gameObject);Destroy(deck);
            var jet=sim.Cars.First(c=>c&&c.type==CityVehicleType.Fighter);jet.transform.position=new Vector3(330,2,900);Physics.SyncTransforms();frame.vertical=1;
            for(int i=0;i<200;i++)jet.Drive(frame,.02f);
            Check(jet.transform.position.y>12&&jet.speed>30,"Fighter accelerates and climbs from the runway");
            var heli=sim.Cars.First(c=>c&&c.type==CityVehicleType.CombatHelicopter);float altitude=heli.transform.position.y;var hover=ControlFrame.Empty;hover.vertical=1;
            for(int i=0;i<100;i++)heli.Drive(hover,.02f);Check(heli.transform.position.y>altitude+12,"Combat helicopter takes off vertically");
            var boat=sim.Cars.First(c=>c&&c.type==CityVehicleType.Boat&&!c.GetComponent<PassengerRoute>());boat.transform.position=new Vector3(1192.5f,OceanLife.Surface,-738);boat.transform.rotation=Quaternion.Euler(0,90,0);old=boat.transform.position;
            frame.vertical=0;for(int i=0;i<150;i++)boat.Drive(frame,.02f);Check(Vector3.Distance(boat.transform.position,old)>12&&Mathf.Abs(boat.transform.position.y-OceanLife.Surface)<.2f,"Boat propulsion stays on water and follows the helm");
            foreach(var route in FindObjectsByType<PassengerRoute>())typeof(PassengerRoute).GetField("wait",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(route,.01f);
            yield return new WaitForSeconds(2);
            Check(FindObjectsByType<PassengerRoute>().All(r=>r.Departures>0&&r.GetComponent<VehicleCabin>().PassengerCount>=8),"NPC pilot and captain depart carrying varied passengers");
            var lift=FindObjectsByType<MultiFloorLift>().First();g.Player.Respawn(lift.platform.position+Vector3.up*.06f);lift.Go(1);yield return new WaitForSeconds(2.6f);
            Check(Mathf.Abs(g.Player.transform.position.y-lift.platform.position.y)<.6f&&lift.platform.localPosition.y>4,"Elevator carries Seo through an open upper-floor shaft");
            g.Player.Respawn(new Vector3(900,-5,-770));var dive=ControlFrame.Empty;dive.vertical=-1;g.Input.ExternalFrame=dive;yield return new WaitForSeconds(1.3f);g.Input.ExternalFrame=ControlFrame.Empty;
            Check(OceanLife.Swimming&&g.Player.transform.position.y< -7&&g.Player.Health>0,"Diving moves below water without the old fall-respawn rule");
            Check(FindObjectsByType<MarineAnimal>().Length>0&&Physics.Raycast(g.Player.transform.position,Vector3.down,100,1),"Marine animals and solid seabed are present below the surface");
            WantedSystem.Report(30,g.Player.transform.position);Check(PrisonSystem.Capture(g)&&PrisonSystem.Instance.Jailed&&Vector3.Distance(g.Player.transform.position,PrisonSystem.Cell)<1,"Police capture transfers Seo into the actual prison cell");
            PrisonSystem.Instance.Release(false);Check(!PrisonSystem.Instance.Jailed&&Vector3.Distance(g.Player.transform.position,PrisonSystem.ReleasePoint)<1,"Sentence release returns Seo to the prison entrance");
            }
            g.CameraRig.enabled=false;LifeState.Hours=16;
            yield return View("military-base",new Vector3(420,.2f,735),new Vector3(650,135,655),new Vector3(420,5,835));
            yield return View("prison",PrisonSystem.ReleasePoint,new Vector3(1290,88,689),new Vector3(1180,7,825));
            yield return View("passenger-harbour",new Vector3(1250,.3f,-677),new Vector3(1140,50,-777),new Vector3(1240,2,-668));
            yield return View("aircraft",new Vector3(1480,.3f,793),new Vector3(1445,14,769),new Vector3(1480,3,800));
            var civilian=sim.Spawn(new Vector3(1260,.2f,510),false,5);yield return null;
            yield return View("sports-car",civilian.transform.position,new Vector3(1268,3,503),civilian.transform.position+Vector3.up);
            var first=FindObjectsByType<MultiFloorLift>().First(l=>l.transform.position.x<700);var building=first.transform.parent;
            yield return View("multistorey-interior",building.position+new Vector3(-8,.3f,-8),building.position+new Vector3(-9,3.5f,-11),building.position+new Vector3(4,1.4f,1));
            yield return View("underwater",new Vector3(900,-7,-770),new Vector3(900,-6,-780),new Vector3(890,-8,-762));
            Finish();
        }
        IEnumerator View(string file,Vector3 player,Vector3 eye,Vector3 target){GameDirector.Instance.Player.Respawn(player);Camera.main.transform.position=eye;Camera.main.transform.LookAt(target);yield return new WaitForSeconds(1.3f);Capture(file);}
        void Capture(string file)
        {var cam=Camera.main;var rt=RenderTexture.GetTemporary(1600,900,24);var prior=RenderTexture.active;var t=new Texture2D(1600,900,TextureFormat.RGBA32,false);var renders=GameDirector.Instance.Player.GetComponentsInChildren<Renderer>();var states=renders.Select(r=>r.enabled).ToArray();foreach(var r in renders)r.enabled=false;RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});for(int i=0;i<renders.Length;i++)renders[i].enabled=states[i];RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes(Path.Combine(output,file+".png"),t.EncodeToPNG());RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Destroy(t);}
        void Finish(){if(finished)return;finished=true;Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"mobility.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);}
    }
}
