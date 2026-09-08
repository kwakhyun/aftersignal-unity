using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class CompactCitySmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();GameDirector game;string output;float started;bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-compact-city-smoke")&&!FindAnyObjectByType<CompactCitySmoke>()){GameDirector.SkipTitle=true;new GameObject("Compact city essential check").AddComponent<CompactCitySmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;started=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CompactCities/Native"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){report.errors.Add(message+"\n"+stack);Save();}}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        void Check(bool yes,string label){(yes?report.passed:report.errors).Add(label);Debug.Log("COMPACT "+yes+" / "+label);Save();}
        void Setup(){game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.enabled=false;game.CameraRig.enabled=false;LifeState.Hours=15;WantedSystem.Clear("");WantedSystem.Instance.enabled=false;CityChronicle.Instance.enabled=false;if(RiftIncursion.Instance)RiftIncursion.Instance.enabled=false;}
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            Setup();yield return new WaitForSeconds(2);
            Check(FourCityCatalog.Land.Length==1&&FourCityCatalog.North==1100,"Afterlight and Nova outlying land strips removed");
            foreach(var v in FourCityCatalog.Venues.Where(v=>CompactCityLayout.Previous.ContainsKey(v.id)))
            {
                bool bounds=v.city==0?v.position.x<2200&&v.position.z<1100:v.city==1?NeonHarbor.OnIsland(v.position):FourCityCatalog.Dry(v.position+Vector3.up);
                Check(bounds,"Facility lies inside original city / "+v.id);
                var entrance=v.kind==VenueKind.Slum?CompactHome.Exit:v.Entrance;
                Check(CityGangWar.FindGround(entrance,out _),"Walkable facility entrance / "+v.id);
            }
            Check(RegionalSettlements.Houses>=100&&RegionalSettlements.Houses<210,"Compact varied village dwelling count / "+RegionalSettlements.Houses);
            Check(!FindObjectsByType<Transform>().Any(t=>t.name=="Original Haven hometown"||t.name=="Nereid civic pressure annex"),"Old duplicated village and underwater annex absent");
            foreach(var at in new[]{new Vector3(1110,.1f,220),new Vector3(1260,.1f,210),new Vector3(1440,.1f,240)})Check(CityGangWar.FindGround(at,out _),"Existing ground retained around relocated blocks / "+at);
            var sim=UrbanSimulation.Instance;game.Player.Respawn(CompactHome.Exit);yield return new WaitForSeconds(3);
            var homeCar=sim.Cars.FirstOrDefault(c=>c&&c.name=="새벽 골목 / 이웃 주차 차량");Check(homeCar&&sim.Enter(homeCar),"Home-area parked car can be boarded");if(sim.Current)sim.Exit();
            game.Player.Respawn(CompactHome.Exit);Check(sim.SummonBike()&&sim.PersonalBike&&sim.PersonalBike.owned,"Personal motorcycle delivery at home");
            if(sim.PersonalBike){game.Player.Respawn(VehicleSeats.Door(sim.PersonalBike));Check(sim.Enter(sim.PersonalBike)&&WantedSystem.Level==0,"Personal motorcycle boarding does not create wanted level");sim.Exit();}
            sim.OpenMap();game.enabled=true;game.Input.ExternalFrame=new ControlFrame{pause=true,weapon=-1};yield return null;game.Input.ExternalFrame=ControlFrame.Empty;game.enabled=false;
            Check(!sim.MapOpen&&!game.Paused,"ESC closes map without opening pause menu");
            yield return View("DawnAlley",CompactHome.Exit,new(RegionalCatalog.HomeQuarter.x+24,9,RegionalCatalog.HomeQuarter.z-33),RegionalCatalog.HomeQuarter+new Vector3(0,2.5f,0));
            yield return View("AfterlightInfill",new(1145,.2f,226),new(1460,390,-270),new(1050,0,290));
            yield return Population("NovaInfill",new(935,.2f,-3250),new(1200,360,-3090),new(1120,0,-3550));
            yield return Population("NereidInfill",new(3980,-61.8f,-4390),new(3920,-25,-4340),new(4140,-60,-4650));
            yield return Population("SmartIsland",RegionalCatalog.Islands[0]+new Vector3(0,.15f,-80),RegionalCatalog.Islands[0]+new Vector3(-100,68,-160),RegionalCatalog.Islands[0]+new Vector3(0,3,0));
            Check(CityChronicle.Instance.Quests.First(q=>q.id=="main01").steps[0].position.z<1100,"Existing main campaign target follows compact hometown");
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Residence));while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;Setup();yield return new WaitForSeconds(2);
            Check(game.Player.transform.position.y<2&&game.stageLength==17,"Home uses a ground-floor small-house interior");
            Check(FindObjectsByType<InteractionPoint>().Any(p=>p.kind==InteractionKind.Sleep)&&FindObjectsByType<InteractionPoint>().Any(p=>p.kind==InteractionKind.Wardrobe)&&FindObjectsByType<CashContainer>().Any(c=>c.Home),"Small house retains bed wardrobe and savings safe");
            yield return View("SeohaHome",CompactHome.Spawn,new(8,3.2f,-5.5f),new(8,1,3));Finish();
        }
        IEnumerator Population(string name,Vector3 at,Vector3 eye,Vector3 target)
        {
            game.Player.Respawn(at);float until=Time.time+12;while(Time.time<until){var frame=ControlFrame.Empty;UrbanSimulation.Instance.BeforeInput(ref frame,.05f);yield return new WaitForSeconds(.05f);}
            int people=WorldActor.All.Count(a=>a&&a.Alive&&a.gameObject.activeInHierarchy&&!a.monster&&Vector3.Distance(a.transform.position,at)<230);
            int cars=UrbanSimulation.Instance.Cars.Count(c=>c&&c.traffic&&Vector3.Distance(c.transform.position,at)<420);
            Check(people>=10,name+" nearby residents / "+people);Check(cars>=4,name+" local moving traffic / "+cars);
            yield return View(name,at,eye,target);
        }
        IEnumerator View(string name,Vector3 player,Vector3 eye,Vector3 target)
        {
            game.Player.Respawn(player);var camera=Camera.main;camera.transform.position=eye;camera.transform.LookAt(target);camera.fieldOfView=60;
            yield return new WaitForSeconds(.7f);var rt=new RenderTexture(1280,720,24);rt.Create();
            for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});yield return null;}
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;rt.Release();Destroy(rt);Destroy(texture);
        }
        void Finish(){finished=true;report.completed=true;Save();Application.Quit(report.errors.Count==0?0:1);}
        void Update(){if(!finished&&Time.realtimeSinceStartup-started>280){report.errors.Add("Essential check timed out");Finish();}}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
