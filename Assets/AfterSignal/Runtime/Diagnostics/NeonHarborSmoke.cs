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
    public sealed class NeonHarborSmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public int population,frames,buildings;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began;bool done,hadJail;float jail;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-neon-harbor-smoke")&&!FindAnyObjectByType<NeonHarborSmoke>()){GameDirector.SkipTitle=true;new GameObject("Nova essential verification").AddComponent<NeonHarborSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/NeonHarbor"));Directory.CreateDirectory(output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){if(report.errors.Count<16)report.errors.Add(text+"\n"+stack);}}
        void Check(bool ok,string text){(ok?report.passed:report.errors).Add(text);Debug.Log("NOVA CHECK "+ok+" / "+text);}
        void Update(){if(!done&&Time.realtimeSinceStartup-began>290){Check(false,"Essential verification timed out");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);LifeState.Hours=15;yield return new WaitForSeconds(3);
            var incident=FindAnyObjectByType<RiftIncursion>();if(incident)incident.enabled=false;
            if(Environment.GetCommandLineArgs().Contains("-neon-visual-only")){yield return VisualOnly();Finish();yield break;}
            report.population=ExpansionWorld.Instance.Population;report.buildings=FindAnyObjectByType<NeonHarbor>().GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Nova building"));
            Check(report.buildings>400&&report.population>1400,"Dense second city and additional citizens instantiate");
            Check(Enumerable.Range(24,24).All(role=>Enumerable.Range(0,4).All(d=>FacilityPeople.Get(FacilityPeople.Key(role),d))),"All 24 new NPC archetypes load four directional sprites");
            Check(NeonHarbor.Land.All(r=>Physics.Raycast(new Vector3(r.center.x,3,r.center.y),Vector3.down,6,1))&&OceanLife.Contains(new Vector3(1060,-2,-2850)),"Four islands have solid ground and central canal remains water");
            Check(NeonHarbor.Roads.All(r=>r.All(p=>p.x>0&&p.z>NeonHarbor.South))&&ExpansionRoads.Roads.Count==28,"New roads and bridges participate in the city road network");
            var ferry=FindObjectsByType<IntercityService>().FirstOrDefault(s=>!s.Aircraft);
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Essential isolated vehicle deck";deck.transform.position=new Vector3(1100,199.5f,-1600);deck.transform.localScale=new Vector3(300,1,160);deck.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("WorldAssets/Generated/Asphalt");
            bool audio=true,views=true;
            for(int i=0;i<11;i++)
            {
                var c=sim.Spawn(new Vector3(1100,200.05f,-1600),false,i);yield return null;yield return null;
                audio&=c.GetComponent<VehicleSoundscape>()&&c.GetComponent<VehicleSoundscape>().Cue!="missing";
                g.Player.Respawn(c.transform.position+Vector3.back*4);sim.Enter(c);g.CameraRig.ReadLook(ControlFrame.Empty);
                if(!g.CameraRig.FirstPersonVehicle)g.CameraRig.ToggleVehicleView();yield return new WaitForSeconds(.25f);g.CameraRig.Snap();
                bool matched=g.CameraRig.FirstPersonVehicle&&Vector3.Distance(Camera.main.transform.position,g.CameraRig.CockpitPosition(c))<.2f;views&=matched;
                if(!matched)Debug.Log("NOVA VIEW MISMATCH "+c.type+" "+Camera.main.transform.position+" expected "+g.CameraRig.CockpitPosition(c));
                if(i==0||i==2||i==6||i==7||i==8||i==10)Capture("cockpit-"+c.type);
                sim.EmergencyExit(c);Destroy(c.gameObject);yield return null;
            }
            Check(audio,"All 11 drivable vehicle types use imported engine recordings");Check(views,"All drivable types enter a vehicle-specific first-person camera");
            var tank=sim.Spawn(new Vector3(1080,200.05f,-1600),false,10);yield return null;yield return null;g.Player.Respawn(tank.transform.position+Vector3.back*5);sim.Enter(tank);
            float initial=tank.transform.eulerAngles.y;var pivot=ControlFrame.Empty;pivot.move=Vector2.right;g.Input.ExternalFrame=pivot;yield return new WaitForSeconds(1);g.Input.ExternalFrame=ControlFrame.Empty;
            Check(Mathf.Abs(Mathf.DeltaAngle(initial,tank.transform.eulerAngles.y))>15&&Mathf.Abs(tank.speed)<.1f,"Tank pivots its tracks while stationary");
            g.CameraRig.enabled=false;Camera.main.transform.position=tank.transform.position+Vector3.up*3;
            var victim=sim.Spawn(new Vector3(1180,200.05f,-1600),false,3);yield return null;yield return null;
            Camera.main.transform.LookAt(victim.transform.position+Vector3.up*1.15f);yield return new WaitForSeconds(2.8f);
            var fire=ControlFrame.Empty;fire.attack=true;g.Input.ExternalFrame=fire;yield return new WaitForSeconds(.2f);g.Input.ExternalFrame=ControlFrame.Empty;yield return new WaitForSeconds(.7f);
            Check(tank.GetComponent<VehicleArmament>().ArticulatedTurret&&tank.GetComponent<VehicleArmament>().Shells==31&&victim.health<100,"Independent articulated turret aims and a cannon projectile damages a distant vehicle");
            Camera.main.transform.position=tank.transform.position+new Vector3(10,5,-11);Camera.main.transform.LookAt(tank.transform.position+Vector3.up*1.5f);Capture("tank-articulated");
            sim.EmergencyExit(tank);Destroy(tank.gameObject);Destroy(victim.gameObject);WantedSystem.Clear("verification");
            var heli=sim.Spawn(new Vector3(1100,212,-1600),false,8);victim=sim.Spawn(new Vector3(1220,200.05f,-1600),false,3);yield return null;yield return null;sim.Enter(heli);
            Camera.main.transform.position=heli.transform.position+new Vector3(3.4f,2.35f,-.38f);Camera.main.transform.LookAt(victim.transform.position+Vector3.up);
            yield return new WaitForSeconds(1.2f);fire=ControlFrame.Empty;fire.secondaryFire=true;g.Input.ExternalFrame=fire;yield return new WaitForSeconds(.12f);g.Input.ExternalFrame=ControlFrame.Empty;yield return new WaitForSeconds(2.5f);
            Check(heli.GetComponent<VehicleArmament>().Missiles==7&&VehicleMissile.Detonations>0&&victim.health<100,"Helicopter missile consumes ammunition, flies and explodes on target");
            Camera.main.transform.position=heli.transform.position+new Vector3(12,6,-14);Camera.main.transform.LookAt(heli.transform.position+Vector3.up*1.5f);Capture("helicopter-rotors");
            sim.EmergencyExit(heli);Destroy(heli.gameObject);Destroy(victim.gameObject);Destroy(deck);WantedSystem.Clear("verification");
            g.CameraRig.enabled=true;var boat=sim.Spawn(new Vector3(2120,OceanLife.Surface,-2090),false,6);boat.transform.rotation=Quaternion.Euler(0,90,0);yield return null;yield return null;sim.Enter(boat);
            var drive=ControlFrame.Empty;drive.move=Vector2.up;drive.boost=true;
            for(int i=0;i<650;i++)boat.Drive(drive,.02f);
            Check(boat.transform.position.z< -2280&&boat.transform.position.y<1,"Piloted boat crosses the old world boundary without rising or clamping");sim.EmergencyExit(boat);Destroy(boat.gameObject);
            g.Player.Respawn(new Vector3(980,-5,-2180));var dive=ControlFrame.Empty;dive.vertical=-1;g.Input.ExternalFrame=dive;yield return new WaitForSeconds(1.5f);g.Input.ExternalFrame=ControlFrame.Empty;
            var waterSprite=g.Player.GetComponent<PixelActor>().Visual.GetComponent<SpriteRenderer>().sprite;
            Check(OceanLife.Swimming&&g.Player.transform.position.y< -8&&waterSprite.name.StartsWith("Tread"),"Diving has swimming physics, breathing and a dedicated water animation");
            Check(waterSprite.texture.GetPixel((int)waterSprite.rect.x+2,(int)waterSprite.rect.y+2).a<.05f,"Swimming texture preserves the generated alpha mask");
            g.Player.Respawn(new Vector3(927.4f,.2f,-2400));yield return new WaitForSeconds(.3f);Check(!OceanLife.Swimming&&g.Player.Controller.height>1.4f,"Returning to land restores the standing collision capsule");
            Check(CityChronicle.Instance.Quests.Count(q=>q.id.StartsWith("nova_side"))==4&&CityChronicle.Instance.Quests.Any(q=>q.id=="main30")&&CityChronicle.Instance.Quests.Where(q=>q.steps.Any(s=>s.world)).SelectMany(q=>q.steps).Count(s=>s.world)==30,"Six main chapters and four side quests connect 30 real world objectives");
            LifeState.Hours=21;g.CameraRig.enabled=false;
            yield return View("nova-skyline",new Vector3(920,.2f,-2460),new Vector3(820,120,-2220),new Vector3(1100,40,-3300));
            yield return View("nova-market",new Vector3(610,.2f,-2850),new Vector3(608,3.2f,-2857),new Vector3(630,3,-2800));
            yield return View("nova-canal",new Vector3(1010,.2f,-2780),new Vector3(1050,16,-2670),new Vector3(1065,6,-3110));
            yield return View("nova-interior",new Vector3(1345,.2f,-2800),new Vector3(1345,2,-2800),new Vector3(1368,2,-2778));
            LifeState.Hours=15;yield return View("underwater-archive",new Vector3(980,-25,-2180),new Vector3(992,-22,-2204),new Vector3(980,-24,-2180));
            g.Player.Respawn(new Vector3(927.4f,.2f,-2390));g.CameraRig.enabled=true;g.CameraRig.Snap();
            while(ferry&&ferry.Arrivals==0&&Time.realtimeSinceStartup-began<265)yield return new WaitForSeconds(1);
            Check(ferry&&ferry.Arrivals>0&&NeonHarbor.Region(ferry.transform.position),"NPC ferry completes an actual ocean crossing and docks at Nova");
            Finish();
        }
        IEnumerator View(string name,Vector3 player,Vector3 eye,Vector3 at){GameDirector.Instance.Player.Respawn(player);Camera.main.transform.position=eye;Camera.main.transform.LookAt(at);yield return new WaitForSeconds(1);Capture(name);}
        IEnumerator VisualOnly()
        {
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;
            var heli=sim.Spawn(new Vector3(1280,.15f,-2780),false,8);yield return null;yield return null;g.Player.Respawn(heli.transform.position+Vector3.back*5);sim.Enter(heli);g.CameraRig.ReadLook(ControlFrame.Empty);g.CameraRig.ToggleVehicleView();yield return new WaitForSeconds(.5f);g.CameraRig.Snap();Capture("helicopter-instruments");
            var station=heli.transform.Find("Driver instrument station");Check(station&&station.gameObject.activeSelf,"Cockpit instruments are rendered in the driver view");sim.EmergencyExit(heli);Destroy(heli.gameObject);
            g.Player.Respawn(new Vector3(980,-25,-2180));g.CameraRig.enabled=false;
            Camera.main.transform.position=new Vector3(986,-24,-2190);Camera.main.transform.LookAt(new Vector3(980,-25,-2180));yield return new WaitForSeconds(1);Capture("swimming-underwater");
            Check(OceanLife.Submerged&&Camera.main.clearFlags==CameraClearFlags.SolidColor&&RenderSettings.fogMode==FogMode.ExponentialSquared,"Underwater camera applies its fog and clear colour at render time");
            Debug.Log("NOVA WATER CAMERA "+Camera.main.transform.position+" contains="+OceanLife.Contains(Camera.main.transform.position)+" flags="+Camera.main.clearFlags+" fog="+RenderSettings.fogMode+" ocean="+FindAnyObjectByType<OceanLife>().enabled);
        }
        void Capture(string name)
        {
            var cam=Camera.main;var rt=RenderTexture.GetTemporary(1600,900,24);var previous=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(texture);
        }
        void Finish(){if(done)return;done=true;Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"neon.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);}
    }
}
