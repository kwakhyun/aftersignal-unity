using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;
using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class CityContinuitySmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public int highrises,waterTaxis,airTaxis;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began,jail;bool done,hadJail;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-city-continuity-smoke")&&!FindAnyObjectByType<CityContinuitySmoke>()){GameDirector.SkipTitle=true;new GameObject("Continuity essential checks").AddComponent<CityContinuitySmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CityContinuity"));Directory.CreateDirectory(output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)if(report.errors.Count<20)report.errors.Add(message+"\n"+stack);}
        void Check(bool ok,string message){(ok?report.passed:report.errors).Add(message);Debug.Log("CONTINUITY CHECK "+ok+" / "+message);}
        void Update(){if(!done&&Time.realtimeSinceStartup-began>190){Check(false,"Essential checks timeout");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);LifeState.Hours=14;
            var incident=FindAnyObjectByType<RiftIncursion>();if(incident)incident.enabled=false;
            for(int i=0;i<90;i++)yield return null;
            g.CameraRig.enabled=false;
            var occlusion=Camera.main.GetComponent<CameraOcclusion>();if(occlusion)occlusion.enabled=false;
            report.highrises=HighriseBuilding.All.Count;report.airTaxis=CityTaxiService.All.Count(t=>t.Air);report.waterTaxis=CityTaxiService.All.Count(t=>!t.Air);
            Check(report.highrises>400&&HighriseBuilding.All.All(b=>b.Floors>=10&&b.Floors<=30),"Every generated high-rise registers a 10–30 floor entrance");
            Check(report.airTaxis==24&&report.waterTaxis==32,"24 autonomous air taxis and 32 water taxis are operating");
            Check(CityTaxiService.All.All(t=>t.Car.transform.Find("Detailed vehicle coachwork")&&!t.Car.GetComponent<VehicleArmament>()),"Taxi fleets use their dedicated coachwork and have no weapons");
            var moving=CityTaxiService.All.Where(t=>t.Serial==3).ToArray();var before=moving.Select(t=>t.transform.position).ToArray();yield return new WaitForSeconds(1);
            Check(moving.Select((t,i)=>Vector3.Distance(t.transform.position,before[i])>.1f).All(x=>x),"Water and air taxi routes advance independently");
            var ride=CityTaxiService.All.First(t=>t.Air&&t.Serial==1);ride.enabled=false;ride.transform.position=new Vector3(2050,200,-1760);ride.Car.speed=0;
            g.Player.Respawn(VehicleSeats.Door(ride.Car));LifeState.Earn(200);int credits=LifeState.Credits;var board=ControlFrame.Empty;board.interact=true;sim.BeforeInput(ref board,.02f);
            Check(sim.Current==ride.Car&&sim.SeatIndex>0&&LifeState.Credits==credits-120,"The E boarding input charges the taxi fare and uses a passenger seat");if(sim.Current)sim.Exit();ride.enabled=true;
            Vector3 center=new(2040,200,-1800);var deck=Cube("Verification support deck",center-Vector3.up*.5f,new(100,1,80));
            g.Player.Respawn(center+Vector3.back*20);Physics.SyncTransforms();
            var npc=new GameObject("Ground and identity citizen",typeof(SpriteRenderer),typeof(CityNpc));npc.transform.position=center;
            npc.GetComponent<CityNpc>().Configure(3,"정비 직원");PeopleArt.Attach(npc,"Worker");var sr=npc.GetComponent<SpriteRenderer>();sr.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
            yield return new WaitForSeconds(.3f);npc.transform.position-=Vector3.up*.4f;yield return new WaitForSeconds(.3f);
            Check(Mathf.Abs(npc.transform.position.y-200.035f)<.08f,"A citizen pushed beneath a raised floor recovers onto that floor");
            var identity=sr.sprite;npc.GetComponent<WorldActor>().health=0;sr.sprite=PeopleArt.Get("CivilianMan",0);yield return null;yield return null;
            Check(identity&&sr.sprite==identity,"Death preserves the same character atlas region instead of replacing the citizen");Destroy(npc);
            Check(PeopleArt.Sheet("Worker").Length==16&&PeopleArt.Sheet("Soldier").Length==16&&PeopleArt.Get("Soldier",0).texture!=PeopleArt.Get("Swat",0).texture,"Dedicated worker and soldier sheets import all 16 poses with separate textures");
            string[] styles={"Soldier","Worker","Doctor","OfficeWoman"};
            for(int i=0;i<styles.Length;i++){var person=new GameObject("Review "+styles[i],typeof(SpriteRenderer));person.transform.position=center+new Vector3(i*2-3,0,0);var r=person.GetComponent<SpriteRenderer>();r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");r.sprite=PeopleArt.Get(styles[i],0);person.transform.rotation=Quaternion.Euler(0,180,0);}
            yield return View("npc-style",center+new Vector3(0,3,-9),center+Vector3.up);
            var wall=Cube("Collision escape target",center+new Vector3(3,1.5f,12),new(1,3,10));
            var car=sim.Spawn(center+new Vector3(.4f,.06f,12),false,0);yield return null;yield return null;car.traffic=false;
            var reverse=ControlFrame.Empty;reverse.move=Vector2.down;float start=car.transform.position.x;for(int i=0;i<65;i++)car.Drive(reverse,.02f);
            Debug.Log("CONTINUITY DRIVE start "+start+" end "+car.transform.position+" speed "+car.speed);Check(car.transform.position.x<start-2,"A vehicle touching a wall can reverse out without remaining stuck");Destroy(car.gameObject);Destroy(wall);Destroy(deck);
            var stairs=FindObjectsByType<Collider>().Where(c=>c.name=="Continuous stair collision ramp").OrderBy(c=>Vector3.Distance(c.transform.position,NeonHarbor.Sites[2])).First();
            var bounds=stairs.bounds;g.Player.Respawn(new Vector3(bounds.center.x,bounds.min.y+.2f,bounds.min.z-.7f));yield return null;
            float stairStart=g.Player.transform.position.y;float stairTimeout=Time.realtimeSinceStartup+7;
            while(g.Player.transform.position.z<bounds.max.z+.8f&&Time.realtimeSinceStartup<stairTimeout){g.Player.Controller.Move(Vector3.forward*Time.deltaTime*6);yield return null;}
            Check(g.Player.transform.position.y>stairStart+3.5f,"The character controller climbs a complete interior stair flight without hitting a blocked step");
            var tower=HighriseBuilding.All.OrderByDescending(b=>b.Floors).First();tower.Enter();yield return null;yield return null;
            var room=HighriseInterior.Active;Check(room&&room.Lift.floors==tower.Floors&&room.GetComponentsInChildren<CityNpc>().Length>4,"A high-rise opens a populated, furnished interior with the full floor count");
            var pane=room.GetComponentsInChildren<BreakableGlass>().First();pane.Hit(100);Check(pane.Broken&&!pane.GetComponent<Collider>().enabled&&!pane.GetComponent<Renderer>().enabled,"Physical window destruction removes both the pane collision and visible glass");
            yield return View("highrise-office",g.Player.transform.position+new Vector3(7,2.2f,1),room.transform.position+Vector3.up*1.2f);
            var lift=room.Lift;g.Player.Respawn(lift.platform.position+Vector3.up*.08f,false);yield return null;lift.speed=100;lift.Go(tower.Floors-1);
            float timeout=Time.realtimeSinceStartup+12;while(lift.Moving&&Time.realtimeSinceStartup<timeout)yield return null;
            yield return new WaitForSeconds(.7f);
            Debug.Log("CONTINUITY LIFT cabin "+lift.platform.position+" player "+g.Player.transform.position+" aboard "+lift.Aboard(g.Player));Check(!lift.Moving&&lift.CurrentFloor==tower.Floors-1&&Mathf.Abs(g.Player.transform.position.y-lift.platform.position.y)<.7f,"The lift physically carries Seo from the lobby to the highest floor");
            Check(room.LoadedFloors<=4,"High-rise detail streams at most four floors while travelling");
            yield return View("highrise-top",lift.platform.position+new Vector3(-10,3,-8),room.transform.position+Vector3.up*(lift.platform.localPosition.y+1));room.Leave();yield return null;
            var facade=tower.GetComponent<FacadeGlass>();var hit=tower.Bounds.center+Vector3.back*tower.Bounds.extents.z;facade.Hit(hit,100);Check(facade.BrokenWindows>0&&facade.OpenAt(hit),"Tower facade windows retain a shattered opening for ballistic passage");
            Check(Vector3.Distance(NeonHarbor.Sites[2],new Vector3(1475,.05f,-2713))<.1f,"Marine research entrance uses the relocated footprint clear of the Nova road");
            g.Player.Respawn(NeonHarbor.Sites[2]);yield return View("marine-research-road",new(1475,17,-2780),new(1475,6,-2685));
            var air=CityTaxiService.All.First(t=>t.Air&&t.Serial==0);g.Player.Respawn(air.transform.position+Vector3.back*10);yield return View("air-taxi",air.transform.position+new Vector3(11,6,-13),air.transform.position+Vector3.up);
            var water=CityTaxiService.All.First(t=>!t.Air&&t.Serial==0);g.Player.Respawn(water.transform.position+Vector3.up*2);yield return View("water-taxi",water.transform.position+new Vector3(13,5,-13),water.transform.position+Vector3.up);
            Check(FindObjectsByType<MeshRenderer>().Any(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="AfterSignal/CoastalWater"&&r.bounds.Contains(new Vector3(1200,OceanLife.Surface,-1300))),"The original coast has a continuous rendered ocean surface under its water taxi routes");
            Finish();
        }
        GameObject Cube(string title,Vector3 at,Vector3 size){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=title;go.transform.position=at;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("WorldAssets/Generated/Asphalt");return go;}
        IEnumerator View(string name,Vector3 eye,Vector3 target){Camera.main.transform.position=eye;Camera.main.transform.LookAt(target);yield return new WaitForSeconds(.6f);var rt=RenderTexture.GetTemporary(1600,900,24);var prev=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
        void Finish(){if(done)return;done=true;Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"continuity.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);}
    }
}
