using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    public sealed class WorldExpansionSmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public int population,voiceLines;public List<string> passed=new List<string>(),errors=new List<string>();}
        readonly Report result=new Report();readonly Dictionary<string,int> prefs=new Dictionary<string,int>();readonly List<string> absent=new List<string>();
        string folder,story;bool hadStory,finished;float began;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-world-expansion-smoke")&&!FindAnyObjectByType<WorldExpansionSmoke>()){GameDirector.SkipTitle=true;new GameObject("Coastal expansion verification").AddComponent<WorldExpansionSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;began=Time.realtimeSinceStartup;folder=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/WorldExpansion/Smoke"));Directory.CreateDirectory(folder);foreach(var k in new[]{"AFTERSIGNAL.Unity.Stage","AFTERSIGNAL.Unity.Memories",UrbanCatalog.Prefix+"Site",UrbanCatalog.Prefix+"Car",UrbanCatalog.Prefix+"CarStage",UrbanCatalog.Prefix+"CarType"}){if(PlayerPrefs.HasKey(k))prefs[k]=PlayerPrefs.GetInt(k);else absent.Add(k);}hadStory=PlayerPrefs.HasKey(CityChronicle.SaveKey);story=PlayerPrefs.GetString(CityChronicle.SaveKey,"");Application.logMessageReceived+=Log;}
        void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert){if(result.errors.Count<12)result.errors.Add(message+"\n"+trace);}}
        void Check(bool condition,string name){(condition?result.passed:result.errors).Add(name);}
        void Update(){if(!finished&&Time.realtimeSinceStartup-began>240){Check(false,"Native verification timed out");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);LifeState.Hours=15.5f;
            yield return new WaitForSeconds(2);
            if(Environment.GetCommandLineArgs().Contains("-expansion-views-only"))
            {
                g.CameraRig.enabled=false;LifeState.Hours=17.5f;
                yield return View("airport",new Vector3(1730,.3f,532),new Vector3(1830,76,402),new Vector3(1730,15,650));
                yield return View("airport-hall",new Vector3(1698,.3f,568),new Vector3(1682,3.2f,557),new Vector3(1715,2,586));
                yield return View("harbour",new Vector3(1730,.3f,-476),new Vector3(1800,94,-786),new Vector3(1700,20,-430));
                yield return View("coast",new Vector3(570,.3f,-545),new Vector3(700,66,-760),new Vector3(775,10,-420));
                LifeState.Hours=21;
                yield return View("night-market",new Vector3(984,.3f,-225),new Vector3(988,3.2f,-225),new Vector3(1000,2.8f,-177));
                result.population=ExpansionWorld.Instance.Population;result.voiceLines=NpcDialogueBank.Count;
                Check(true,"Five district views captured after lighting and population settle");Finish();yield break;
            }
            result.population=ExpansionWorld.Instance?ExpansionWorld.Instance.Population:0;result.voiceLines=NpcDialogueBank.Count;
            Check(result.population>=344,"Facility populations and roles instantiated");Check(FindObjectsByType<ExpansionService>().Select(service=>service.facility).Distinct().Count()==ExpansionWorld.Names.Length,"All facility interaction components survive serialization");Check(result.voiceLines>=280,"Expanded authored speech bank loaded");
            Check(Enumerable.Range(0,24).All(n=>Enumerable.Range(0,4).All(d=>FacilityPeople.Get(FacilityPeople.Key(n),d))),"All 24 facility characters resolve four directions");
            Check(FacilityPeople.Sheets.All(name=>{var t=Resources.Load<Texture2D>("Art/FacilityCitizens/"+name);if(!t)return false;var pixels=t.GetPixels32();int clear=pixels.Count(c=>c.a<20);return clear>pixels.Length*.4f&&clear<pixels.Length*.95f;}),"Imported facility sheets preserve real alpha and intact silhouettes");
            Check(!Resources.Load<GameObject>("Characters/Seo3D/SeoModel")&&g.Player.GetComponent<PixelActor>(),"Seo uses sprites; withdrawn 3D model absent");
            Check(g.stageLength==2200&&g.halfDepth>=1000,"Expanded world movement bounds");
            foreach(var p in ExpansionWorld.Places){var route=ExpansionRoads.Navigation(new Vector3(740,0,0),p);Check(route.Count>4&&(route[route.Count-1]-p).sqrMagnitude<1,"Road route: "+p);}
            Check(g.Audio.HasCue("reload_out")&&g.Audio.HasCue("reload_in")&&g.Audio.HasCue("reload_slide"),"Recorded reload phases available");
            var origin=new Vector3(1120,155,80);var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary test deck";floor.transform.position=origin+new Vector3(240,-.55f,0);floor.transform.localScale=new Vector3(650,1,120);floor.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Materials/UrbanRoad");
            g.Player.Respawn(origin+Vector3.up*.2f);g.CameraRig.enabled=false;var cam=Camera.main;cam.transform.position=origin+new Vector3(-4,2,-4);cam.transform.LookAt(origin+Vector3.right*450+Vector3.up);
            var target=Person(origin+Vector3.right*450,6003);yield return null;Physics.SyncTransforms();
            Ray ray=cam.ViewportPointToRay(new Vector3(.5f,.5f));Vector3 aim=Ballistics.AimPoint(ray,g.Player.transform);Check(Vector3.Distance(aim,target.Center)<1.5f,"Crosshair acquires a citizen at 450 metres");
            typeof(PlayerMotor).GetProperty("Aim").SetValue(g.Player,aim);typeof(PlayerMotor).GetMethod("FirePistol",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g.Player,new object[]{18f,true});Check(target.health<70,"Pistol hits the crosshair target at 450 metres");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=origin+new Vector3(230,2,0);wall.transform.localScale=new Vector3(2,8,35);Physics.SyncTransforms();Check(Ballistics.Cast(g.Player.Muzzle,(target.Center-g.Player.Muzzle).normalized,800,g.Player.transform,out var obstruction)&&obstruction.collider.gameObject==wall,"Nearby solid cover blocks the muzzle ray");Destroy(wall);Destroy(target.gameObject);WantedSystem.Clear("verification");
            var pedestrian=Person(origin+new Vector3(18,0,0),6100);Vector3 start=pedestrian.transform.position;pedestrian.VehicleHit(Vector3.right,27);yield return new WaitForSeconds(.78f);Check(pedestrian.transform.position.x-start.x>24&&pedestrian.transform.position.y>start.y+2,"Fast vehicle impact launches a citizen far and upward");Destroy(pedestrian.gameObject);
            var defender=Person(origin+new Vector3(10,0,18),6203);PeopleArt.Attach(defender.gameObject,"Worker");defender.Damage(3,Vector3.right);Check(CivilianDefense.Active(defender),"Selected adult citizen retaliates against the aggressor");Destroy(defender.gameObject);WantedSystem.Clear("verification");
            var blade=Person(origin+new Vector3(10,0,22),6210);blade.health=0;SeveredSprite.Create(blade,Vector3.right*5);yield return null;Check(blade.GetComponent<SeveredSprite>()&&FindObjectsByType<DetachedLimb>().Length>0,"Blade death separates a matching body region");blade.ResetHealth();yield return null;Check(!blade.GetComponent<SeveredSprite>(),"Pooled actor restores intact artwork");Destroy(blade.gameObject);
            var sim=UrbanSimulation.Instance;var car=sim.Spawn(origin+new Vector3(18,0,-18),false,0);car.occupied=true;yield return new WaitForSeconds(.2f);car.GetComponent<VehicleCabin>().SetPassengers(2);Check(car.GetComponent<VehicleDetails>()&&car.transform.Find("Detailed vehicle coachwork"),"Detailed sedan mesh installed with existing cabin");
            cam.transform.position=car.transform.position+new Vector3(7,3.2f,-7);cam.transform.LookAt(car.transform.position+Vector3.up);Capture("detailed-sedan");
            int pieces=WreckFragments.Spawned;car.Damage(300,car.transform.position);yield return new WaitForSeconds(.28f);Check(WreckFragments.Spawned>pieces+4&&car.GetComponent<VehicleCabin>().PassengerCount==0,"Vehicle explosion ejects occupants and fractures coachwork");Capture("vehicle-explosion");
            yield return new WaitForSeconds(8);Check(!car,"Wrecked vehicle and fragments expire");WantedSystem.Clear("verification");
            var heli=PoliceHelicopter.Create(FindAnyObjectByType<WantedSystem>(),origin+new Vector3(40,38,5));int blasts=ExplosionPresentation.Detonations;heli.Body.Damage(1000,Vector3.right);yield return new WaitForSeconds(.7f);Check(heli&&heli.transform.position.y<origin.y+38,"Destroyed helicopter falls under gravity");cam.transform.position=origin+new Vector3(64,20,-28);cam.transform.LookAt(heli.transform.position);Capture("helicopter-falling");yield return new WaitForSeconds(5);Check(!heli&&ExplosionPresentation.Detonations>blasts,"Helicopter detonates on terrain impact");Destroy(floor);WantedSystem.Clear("verification");
            LifeState.Hours=16.6f;
            yield return View("airport",new Vector3(1730,.3f,532),new Vector3(1830,76,402),new Vector3(1730,15,650));
            yield return View("airport-hall",new Vector3(1698,.3f,568),new Vector3(1682,3.2f,557),new Vector3(1715,2,586));
            Check(FindObjectsByType<FacilityCitizen>().Length>=60,"Airport citizens are active near the player");
            yield return View("harbour",new Vector3(1730,.3f,-476),new Vector3(1800,94,-786),new Vector3(1700,20,-430));
            yield return View("coast",new Vector3(570,.3f,-545),new Vector3(700,66,-760),new Vector3(775,10,-420));
            LifeState.Hours=21;
            yield return View("night-market",new Vector3(984,.3f,-225),new Vector3(978,7,-230),new Vector3(995,3,-177));
            Check(ExpansionWorld.Places.All(p=>Physics.Raycast(p+Vector3.up*4,Vector3.down,10,1,QueryTriggerInteraction.Ignore)),"Every new destination has solid walkable ground");
            Finish();
        }
        WorldActor Person(Vector3 p,int seed){var o=new GameObject("Verification citizen",typeof(SpriteRenderer),typeof(CityNpc));o.transform.position=p;var sr=o.GetComponent<SpriteRenderer>();sr.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");sr.sprite=PeopleArt.Get("CivilianMan",0);o.GetComponent<CityNpc>().Configure(seed);return o.GetComponent<WorldActor>();}
        IEnumerator View(string name,Vector3 player,Vector3 camera,Vector3 look){var g=GameDirector.Instance;g.Player.Respawn(player);g.CameraRig.enabled=false;Camera.main.transform.position=camera;Camera.main.transform.LookAt(look);yield return new WaitForSeconds(1.3f);Capture(name);}
        void Capture(string name){var cam=Camera.main;var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
        void Finish(){if(finished)return;finished=true;Application.logMessageReceived-=Log;if(hadStory)PlayerPrefs.SetString(CityChronicle.SaveKey,story);else PlayerPrefs.DeleteKey(CityChronicle.SaveKey);foreach(var p in prefs)PlayerPrefs.SetInt(p.Key,p.Value);foreach(var k in absent)PlayerPrefs.DeleteKey(k);PlayerPrefs.Save();result.completed=result.errors.Count==0;File.WriteAllText(Path.Combine(folder,"world-expansion.json"),JsonUtility.ToJson(result,true));Application.Quit(result.completed?0:1);}
    }
}
