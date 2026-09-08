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
    public sealed class RegionalExpansionSmoke:MonoBehaviour
    {
        [Serializable]class Report{public bool completed;public int houses,venues,regionalPopulation;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float start;bool done;GameDirector game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-regional-smoke")&&!FindAnyObjectByType<RegionalExpansionSmoke>()){GameDirector.SkipTitle=true;new GameObject("Regional integration check").AddComponent<RegionalExpansionSmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;start=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/RegionalExpansion/Native"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(message+"\n"+trace);}
        void Check(bool yes,string label){(yes?report.passed:report.errors).Add(label);Debug.Log("REGIONAL "+yes+" / "+label);Save();}
        void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);game.CameraRig.enabled=false;game.enabled=false;game.Player.enabled=false;LifeState.Hours=15;
            WantedSystem.Clear("");WantedSystem.Instance.enabled=false;foreach(var incursion in FindObjectsByType<RiftIncursion>())incursion.enabled=false;
            yield return new WaitForSeconds(2);
            var world=FourCityWorld.Instance;var sim=UrbanSimulation.Instance;
            report.houses=RegionalSettlements.Houses;report.venues=world.Facilities.Count;report.regionalPopulation=FourCityCatalog.Venues.Skip(28).Sum(v=>v.visitors);
            Check(FourCityCatalog.Venues.Select(v=>v.id).Distinct().Count()==FourCityCatalog.Venues.Length,"Unique facility ids preserve campaign indices");
            Check(report.houses>=100&&report.houses<210,"Compact village contains densely arranged small dwellings");
            var slum=world.Find("dawn-lowlands");Check(!slum.transform.Find("Original Haven hometown")&&slum.transform.Find("Seoha small house entrance"),"Rebuilt small-house village replaces the detached hometown");
            Check(slum.GetComponentsInChildren<InteractionPoint>().Any(p=>p.destination==StageId.Residence),"Integrated hometown retains Seoha's residence entrance");
            Check(game.stageLength>=FourCityCatalog.East&&game.halfDepth>=-FourCityCatalog.South,"Movement bounds cover all regional extensions");
            foreach(var v in world.Facilities.Where(v=>v.Definition.kind>=VenueKind.Police||v.Definition.id=="nova-medical"))
            {
                if(v.Definition.kind==VenueKind.Sinkhole)continue;
                var p=v.Definition.kind==VenueKind.Slum?RegionalCatalog.HomeQuarter+new Vector3(20,0,-10):v.transform.position+new Vector3(0,.15f,-v.Definition.size.y*.43f);
                Check(Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,4,1,QueryTriggerInteraction.Ignore)&&hit.normal.y>.8f,"Ground continuity / "+v.Definition.id);
                if(v.floorCount>0)for(int f=0;f<v.floorCount;f++)
                {var floor=v.transform.position+new Vector3(0,f*v.floorHeight,-v.roomDepth*.5f+4);Check(Physics.Raycast(floor+Vector3.up*1.6f,Vector3.down,out var h,2,1,QueryTriggerInteraction.Ignore)&&h.normal.y>.8f,"Interior floor "+f+" / "+v.Definition.id);}
            }
            bool overRift=false;foreach(var road in FourCityCatalog.Roads)for(int i=1;i<road.Length;i++)if(RegionalCatalog.InRift(FourCityCatalog.Closest(RegionalCatalog.RiftCenter,road[i-1],road[i]),12))overRift=true;
            Check(!overRift,"Roads detour around the open sinkhole");
            Check(!Physics.Raycast(RegionalCatalog.RiftCenter+new Vector3(5,2,5),Vector3.down,20,1,QueryTriggerInteraction.Ignore),"Sinkhole has a physical opening without a hidden surface slab");
            Check(FourCityCatalog.Dry(RegionalCatalog.PressureAnnex+Vector3.up*2),"Public facilities share the original underwater pressure volume");
            var cars=new List<CityVehicle>();
            for(int group=0;group<3;group++)
            {
                int count=group==0?3:group==1?4:3;var type=group==0?CityVehicleType.SportsCar:group==1?CityVehicleType.Sedan:CityVehicleType.Motorcycle;
                for(int i=0;i<count;i++){var car=sim.Spawn(new Vector3(1700+i*9,.1f,900+group*14),false,(int)type);car.designVariant=i;cars.Add(car);}
            }
            yield return null;
            foreach(var c in cars)Check(c.GetComponentsInChildren<MeshRenderer>().Length>10&&Resources.Load<GameObject>("WorldAssets/"+FleetDesign.Model(c)+"/"+FleetDesign.Model(c)),"Authored fleet model loaded / "+FleetDesign.Model(c));
            game.Player.Respawn(new Vector3(1710,.1f,885));
            yield return View("supercar-lineup",new Vector3(1710,4,891),new Vector3(1708,1,901));
            yield return View("everyday-lineup",new Vector3(1713,4,906),new Vector3(1712,1,915));
            yield return View("motorcycle-lineup",new Vector3(1706,2.4f,923),new Vector3(1707,1,929));
            var test=sim.Spawn(new Vector3(1840,.1f,840),false,(int)CityVehicleType.SportsCar);yield return null;test.speed=24;
            var frame=ControlFrame.Empty;frame.move=new Vector2(.7f,1);frame.guard=true;for(int i=0;i<36;i++){test.Drive(frame,.016f);yield return null;}
            Check(test.DriftDistance>2&&test.GetComponentsInChildren<TrailRenderer>().Length==2,"Handbrake steering produces lateral drift and tyre trails");
            foreach(var c in cars)Destroy(c.gameObject);Destroy(test.gameObject);
            game.Player.Respawn(RegionalCatalog.HomeQuarter+new Vector3(20,.1f,-10));yield return new WaitForSeconds(3);
            Check(slum.ActiveVisitors==180,"Lowland population is active near Seoha's hometown");
            Check(slum.GetComponentsInChildren<NeighborBond>().Count(b=>b.Affinity>=80)>=180,"Local residents begin with strong personal affinity");
            Check(slum.GetComponent<RegionalDistrict>().PatrolCount>=8,"Lowland district runs frequent local police patrols");
            var terminal=slum.GetComponentsInChildren<VenueService>().First(p=>p.action!="lift");CityLife.Instance.VenueMenu(terminal);Check(CityLife.Instance.Options.Count>=5,"Lowland terminal exposes meals, black market, courier work and home");CityLife.Instance.Dismiss();
            yield return View("lowlands-street",RegionalCatalog.HomeQuarter+new Vector3(5,5,-22),RegionalCatalog.HomeQuarter+new Vector3(30,3,20));
            yield return View("lowlands-district",new Vector3(780,120,2600),new Vector3(1160,0,2980));
            var nova=world.Find("nova-cityhall");game.Player.Respawn(nova.Definition.Entrance);yield return new WaitForSeconds(1);
            yield return View("nova-civic",nova.transform.position+new Vector3(100,42,-130),nova.transform.position+Vector3.up*15);
            var deep=world.Find("nereid-bank");game.Player.Respawn(deep.Definition.Entrance);yield return new WaitForSeconds(1);
            yield return View("nereid-civic",deep.transform.position+new Vector3(-88,30,-108),deep.transform.position+Vector3.up*9);
            var at=RegionalCatalog.RiftCenter+new Vector3(-300,.1f,40);game.Player.Respawn(at);RegionalWorld.Instance.TriggerConflict(at);yield return new WaitForSeconds(2);
            Check(RegionalWorld.Instance.BattleUnits>=12,"Erebos encounter fields containment troops and corrupted threats");
            Check(FindObjectsByType<WorldActor>().Any(a=>a.Alive&&a.monster&&FactionCombat.NearestOpponent(a,64)),"Monster factions acquire military opponents");
            yield return View("erebos-sinkhole",RegionalCatalog.RiftCenter+new Vector3(-400,160,-330),RegionalCatalog.RiftCenter+Vector3.down*55);
            for(int i=0;i<4;i++){game.Player.Respawn(RegionalCatalog.Islands[i]+new Vector3(0,.2f,-145));yield return new WaitForSeconds(.6f);Check(FourCityCatalog.OnNewLand(RegionalCatalog.Islands[i]),"Island ground participates in ocean and traversal rules / "+i);yield return View("island-"+i,RegionalCatalog.Islands[i]+new Vector3(-200,110,-210),RegionalCatalog.Islands[i]);}
            Check(FindAnyObjectByType<RegionalFerry>(),"Island circuit has an autonomous passenger ferry");
            Finish();
        }
        IEnumerator View(string name,Vector3 eye,Vector3 target){Camera.main.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));Camera.main.fieldOfView=60;yield return new WaitForSeconds(.3f);var rt=RenderTexture.GetTemporary(1280,720,24);for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(Camera.main,new RenderPipeline.StandardRequest{destination=rt});yield return null;}var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
        void Update(){if(!done&&Time.realtimeSinceStartup-start>480){report.errors.Add("Regional check timed out");Finish();}}
        void Finish(){if(done)return;done=true;report.completed=report.errors.Count==0;Save();Application.Quit(report.completed?0:1);}
        void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
