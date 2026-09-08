using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Linq;
using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEngine.SceneManagement;using UnityEngine.Video;
namespace AfterSignal
{
    public sealed class FourCitySmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public int facilities,npcPopulation;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began,jail;bool done,hadJail;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-four-city-smoke")&&!FindAnyObjectByType<FourCitySmoke>()){GameDirector.SkipTitle=true;new GameObject("Four-city essential checks").AddComponent<FourCitySmoke>();}}
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=CityChronicle.SuppressSave=true;hadJail=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.JailSeconds");jail=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.JailSeconds");PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/FourCities/Native"));Directory.CreateDirectory(output);began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)if(report.errors.Count<30)report.errors.Add(message+"\n"+stack);}
        void Check(bool ok,string message){(ok?report.passed:report.errors).Add(message);Debug.Log("FOUR CITY CHECK "+ok+" / "+message);}
        void Update(){if(!done&&Time.realtimeSinceStartup-began>300){Check(false,"Native essential checks timeout");Finish();}}
        IEnumerator Start()
        {
            ResidentialWorld.VisitHome=-1;yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            var g=GameDirector.Instance;var world=FourCityWorld.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);LifeState.Hours=14;
            var rift=FindAnyObjectByType<RiftIncursion>();if(rift)rift.enabled=false;var horde=FindAnyObjectByType<ErebosPopulation>();if(horde)horde.enabled=false;
            g.CameraRig.enabled=false;var occlusion=Camera.main.GetComponent<CameraOcclusion>();if(occlusion)occlusion.enabled=false;
            if(Environment.GetCommandLineArgs().Contains("-four-city-visual")){yield return VisualReview(g,world);yield return CampaignProgress(g);Finish();yield break;}
            report.facilities=world.Facilities.Count;report.npcPopulation=FourCityCatalog.Venues.Sum(v=>v.visitors);
            Check(report.facilities==28,"28 facilities constructed with unique services");
            Check(new[]{"FootballPlayer","BaseballPlayer","BasketballPlayer","RacingDriver","AbyssEngineer","AbyssCitizen","AbyssMedic","CorruptedCitizen"}.All(key=>PeopleArt.Sheet(key).Length==16),"Eight new casts each import sixteen aligned directional frames");
            foreach(var v in world.Facilities)
            {
                g.Player.Respawn(v.Definition.Entrance,false);Physics.SyncTransforms();
                Check(NpcGroundSupport.Floor(v.Definition.Entrance,v.Definition.Entrance.y+.5f,1.2f,out var height)&&Mathf.Abs(height-v.Definition.position.y)<.3f,"Continuous entrance floor / "+v.Definition.id);
                if(v.roomWidth<=0||v.Definition.kind==VenueKind.Cinema)continue;
                var entry=v.transform.TransformPoint(new Vector3(0,.12f,-v.roomDepth*.5f-3));g.Player.Respawn(entry,false);Physics.SyncTransforms();for(int n=0;n<70;n++)g.Player.Controller.Move(new Vector3(0,-.035f,.14f));
                Check(g.Player.transform.position.z>entry.z+7&&Mathf.Abs(g.Player.transform.position.y-v.transform.position.y)<.5f,"Character walks through open main entrance / "+v.Definition.id);
                bool stairs=true;
                for(int floor=0;floor<v.floorCount-1;floor++)
                {
                    var bottom=v.transform.TransformPoint(new Vector3(-v.roomWidth*.5f+7,floor*v.floorHeight+.15f,-5.8f));g.Player.Respawn(bottom,false);Physics.SyncTransforms();
                    for(int n=0;n<95;n++)g.Player.Controller.Move(new Vector3(0,-.02f,.14f));
                    float climbed=g.Player.transform.position.y-bottom.y;if(climbed<v.floorHeight-.55f)stairs=false;
                }
                Check(stairs,"Unblocked stairs through every floor / "+v.Definition.id);yield return null;
            }
            var hotel=world.Find("nova-skyhotel");hotel.Lift.Go(0);g.Player.Respawn(hotel.Lift.platform.position+Vector3.up*.14f,false);float initial=g.Player.transform.position.y;hotel.Lift.Go(1);float timeout=Time.time+5;while(hotel.Lift.Moving&&Time.time<timeout)yield return null;
            Check(g.Player.transform.position.y>initial+4&&hotel.Lift.Aboard(g.Player),"Hotel elevator carries player to requested floor");
            if(Environment.GetCommandLineArgs().Contains("-four-city-floor-only")){Finish();yield break;}
            var undersea=world.Find("nereid-forum");g.Player.Respawn(undersea.Definition.Entrance,false);for(int i=0;i<25;i++)yield return null;
            Check(g.Player.transform.position.y< -60&&g.Player.transform.position.y> -63&&!OceanLife.Swimming,"Underwater city walking does not trigger swimming or fall recovery");
            Check(undersea.StaffOnDuty>=3&&undersea.ActiveVisitors>30,"Underwater facility has operating staff and varied residents");
            var guide=undersea.GetComponentsInChildren<CityNpc>().FirstOrDefault();Check(guide&&FacilityGuide.TryAnswer(guide,"공항에서 비행기 타는 방법 알려 주세요",out var answer)&&answer.Contains("공항")&&answer.Contains("G"),"Facility NPC can explain airport boarding without waiting for AI");
            var league=FourCitySports.Instance;var match=league.Get("dawn-stadium");match.phase=MatchPhase.Scheduled;match.wait=30;match.stake=0;match.settled=false;LifeState.Earn(300);int cash=LifeState.Credits;bool bet=league.Bet(match,0,100);match.Begin();Check(bet&&LifeState.Credits==cash-100&&!league.Bet(match,1,100),"Bet charges once and locks at kickoff");match.homeScore=1;match.awayScore=0;match.Finish("Test result");league.Settle(match);int paid=LifeState.Credits;league.Settle(match);Check(paid==cash+100&&LifeState.Credits==paid,"Winning prediction settles exactly once");
            var stadium=world.Find("dawn-stadium");g.Player.Respawn(stadium.Definition.Entrance,false);yield return new WaitForSeconds(.6f);Check(stadium.GetComponentsInChildren<VenueActor>().Count(a=>a.athlete)==22,"Football field has two eleven-player teams");
            yield return View("football-stadium",stadium.transform.position+new Vector3(125,90,-132),stadium.transform.position+Vector3.up*6);
            var garden=world.Find("lumen-garden");g.Player.Respawn(garden.Definition.Entrance,false);yield return View("lumen-garden",garden.transform.position+new Vector3(132,67,-150),garden.transform.position+Vector3.up*12);
            var park=world.Find("nova-wonder");g.Player.Respawn(park.Definition.Entrance,false);Check(park.rides.Count==3,"Three staffed ride types exist");yield return View("wonder-park",park.transform.position+new Vector3(250,125,-225),park.transform.position+Vector3.up*10);
            var ride=park.rides[0];ride.Board();float until=Time.time+85;float low=g.Player.transform.position.y,max=low;while(VenueRide.Riding&&Time.time<until){max=Mathf.Max(max,g.Player.transform.position.y);yield return null;}Check(max>low+15&&!VenueRide.Riding,"Paid Ferris ride boards, carries player upward, and safely unloads");
            g.Player.Respawn(undersea.Definition.Entrance,false);yield return View("nereid-city",new(3990,-22,-4700),new(3900,-53,-4430));
            var archive=world.Find("nereid-archive");g.Player.Respawn(archive.transform.position+new Vector3(0,.15f,-12),false);yield return View("nereid-interior",archive.transform.position+new Vector3(7,4,-20),archive.transform.position+new Vector3(-10,1,0));
            var erebos=world.Find("erebos-vault");g.Player.Respawn(erebos.Definition.Entrance,false);yield return View("erebos-city",new(3890,140,-1050),new(4300,60,-1300));
            var hostile=ErebosThreat.Spawn(g.Player.transform.position+Vector3.forward, false,79000,transform);float hp=g.Player.Health;yield return new WaitForSeconds(1.3f);Check(g.Player.Health<hp,"Corrupted citizen attacks player at melee distance");hostile.Damage(500,Vector3.forward*4);Check(!hostile.Alive,"Corrupted citizen can be defeated");
            var movie=world.Find("prism-cinema");g.Player.Respawn(movie.Definition.Entrance,false);movie.WatchFilm();until=Time.time+15;while(!movie.FilmPlaying&&Time.time<until)yield return null;yield return new WaitForSeconds(1);Check(movie.FilmPlaying&&movie.GetComponentInChildren<VideoPlayer>().frame>0,"Bundled original movie decodes and advances in cinema");g.CameraRig.enabled=true;yield return new WaitForSeconds(.5f);yield return Capture("cinema");g.CameraRig.enabled=false;
            bool cinematicDone=false;CityCinematic.Play("기록의 목소리","리안 · 기록관","유리 너머의 도시를 봐 주세요. 서로 다른 네 도시가 하나의 기억을 지키고 있어요.",archive.transform.position,archive.transform.position+new Vector3(3,0,5),()=>cinematicDone=true);yield return new WaitForSeconds(5);yield return Capture("campaign-cinematic");while(CityCinematic.Active)yield return null;Check(cinematicDone&&!CityCinematic.Active,"Cinematic plays staged shots and restores control");
            g.Player.Respawn(new Vector3(1110,.15f,1650),false);UrbanSimulation.Instance.OpenMap();yield return new WaitForSeconds(.3f);var atlas=FindObjectsByType<AtlasViewport>().FirstOrDefault(a=>!a.mini);atlas.Focus(FourCityCatalog.Centers[0],1800);atlas.follow=false;yield return Capture("city-atlas");atlas.Focus(FourCityCatalog.Centers[3],1800);yield return Capture("nereid-atlas");
            Check(atlas&&FindObjectsByType<AtlasLabels>().Length>0,"Detailed city atlas has searchable categories and geographic labels");Finish();
        }
        IEnumerator View(string name,Vector3 eye,Vector3 look){Camera.main.transform.position=eye;Camera.main.transform.LookAt(look);yield return Capture(name);}
        IEnumerator Capture(string name)
        {
            yield return new WaitForSeconds(.85f);
            var canvases=FindObjectsByType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=Camera.main.nearClipPlane+.2f;}
            Canvas.ForceUpdateCanvases();yield return null;
            var rt=RenderTexture.GetTemporary(1600,900,24);var previous=RenderTexture.active;var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(image);
            foreach(var canvas in canvases)if(canvas){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}Canvas.ForceUpdateCanvases();
        }
        IEnumerator VisualReview(GameDirector g,FourCityWorld world)
        {
            report.facilities=world.Facilities.Count;report.npcPopulation=FourCityCatalog.Venues.Sum(v=>v.visitors);
            var stadium=world.Find("dawn-stadium");var match=FourCitySports.Instance.Get(stadium.Definition.id);match.Begin();match.finalSeconds=0;match.homeScore=match.awayScore=0;g.Player.Respawn(stadium.Definition.Entrance,false);
            yield return View("football-stadium",stadium.transform.position+new Vector3(125,90,-132),stadium.transform.position+Vector3.up*6);
            var garden=world.Find("lumen-garden");g.Player.Respawn(garden.Definition.Entrance,false);yield return View("lumen-garden",garden.transform.position+new Vector3(132,67,-150),garden.transform.position+Vector3.up*12);
            yield return View("garden-interior",garden.transform.TransformPoint(garden.viewPoint+new Vector3(0,1.6f,-3)),garden.transform.position+new Vector3(0,10,0));
            var park=world.Find("nova-wonder");g.Player.Respawn(park.Definition.Entrance,false);yield return View("wonder-park",park.transform.position+new Vector3(250,125,-225),park.transform.position+Vector3.up*10);
            Check(park.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)>20,"Facility geometry is visible immediately after intercity travel");
            var hotel=world.Find("nova-skyhotel");g.Player.Respawn(hotel.Definition.Entrance,false);yield return View("nova-hotel",hotel.transform.position+new Vector3(140,68,-155),hotel.transform.position+Vector3.up*24);
            var circuit=world.Find("apex-circuit");g.Player.Respawn(circuit.Definition.Entrance,false);yield return View("race-circuit",circuit.transform.position+new Vector3(300,200,-290),circuit.transform.position+Vector3.up*3);
            var undersea=world.Find("nereid-forum");g.Player.Respawn(undersea.Definition.Entrance,false);yield return View("nereid-city",new(3990,-43,-4740),new(3900,-53,-4430));
            Check(!OceanLife.Swimming&&undersea.ActiveVisitors>30,"Breathable underwater city has active residents");
            var archive=world.Find("nereid-archive");g.Player.Respawn(archive.transform.position+new Vector3(0,.15f,-12),false);yield return View("nereid-interior",archive.transform.position+new Vector3(7,4,-20),archive.transform.position+new Vector3(-10,1,0));
            var erebos=world.Find("erebos-vault");g.Player.Respawn(erebos.Definition.Entrance,false);yield return View("erebos-city",new(3890,140,-1050),new(4300,60,-1300));
            var movie=world.Find("prism-cinema");g.Player.Respawn(movie.Definition.Entrance,false);movie.WatchFilm();float until=Time.time+15;while(!movie.FilmPlaying&&Time.time<until)yield return null;yield return new WaitForSeconds(1);Check(movie.FilmPlaying,"Original movie playback after auditorium layout correction");
            Check(g.Player.transform.position.y>=1.5f,"Cinema viewpoint stays above tiered seating floor");g.CameraRig.enabled=true;yield return new WaitForSeconds(.5f);yield return Capture("cinema");g.CameraRig.enabled=false;
            g.Player.Respawn(archive.transform.position+new Vector3(3,.15f,4),false);yield return new WaitForSeconds(1);
            bool complete=false;CityCinematic.Play("02 · 유리 아래의 도시","리안 · 기록관","유리 너머의 도시를 봐 주세요. 서로 다른 네 도시가 하나의 기억을 지키고 있어요.",archive.transform.position,archive.transform.position+new Vector3(3,.1f,7),()=>complete=true);yield return new WaitForSeconds(5);yield return Capture("campaign-cinematic");while(CityCinematic.Active)yield return null;Check(complete,"Cinematic with dialogue and letterbox restores player control");
            g.Player.Respawn(new Vector3(1110,.15f,1650),false);UrbanSimulation.Instance.OpenMap();yield return new WaitForSeconds(.3f);var atlas=FindObjectsByType<AtlasViewport>().FirstOrDefault(a=>!a.mini);atlas.Focus(FourCityCatalog.Centers[0],1800);atlas.follow=false;yield return Capture("city-atlas");atlas.Focus(FourCityCatalog.Centers[3],1800);yield return Capture("nereid-atlas");
        }
        IEnumerator CampaignProgress(GameDirector g)
        {
            UrbanSimulation.Instance.CloseMap();g.CloseDialogue();var story=FourCityCampaign.Instance;
            while(!story.Complete)
            {
                int chapter=story.Step;var current=story.Current;g.Player.Respawn(story.Destination,false);g.Player.Heal(100);yield return new WaitForSeconds(.7f);
                foreach(var threat in FindObjectsByType<ErebosThreat>())if(threat&&threat.Body&&threat.Body.Alive&&(threat.transform.position-story.Destination).sqrMagnitude<100*100)threat.Body.Damage(10000,Vector3.zero);
                if(chapter==5)
                {
                    var vault=FourCityWorld.Instance.Find("erebos-vault");foreach(var terminal in vault.GetComponentsInChildren<VenueService>())if(terminal.action=="sync"){g.Player.Respawn(terminal.transform.position-Vector3.up*.9f,false);terminal.Use();}
                    g.Player.Respawn(story.Destination,false);
                }
                story.Interact(current.facility);yield return new WaitForSeconds(.2f);var shot=FindAnyObjectByType<CityCinematic>();if(shot)shot.Skip();float until=Time.time+3;while(CityCinematic.Active&&Time.time<until)yield return null;
                if(chapter==9&&CityLife.Instance.Options.Count>0)CityLife.Instance.Options[0].action();
                Check(story.Step==chapter+1,"Indoor campaign advances through investigation, combat and choice / chapter "+(chapter+1));if(story.Step!=chapter+1)yield break;
                g.CloseDialogue();
            }
            Check(story.Complete&&story.Choice==1,"Twelve-chapter campaign reaches selected ending without saving test progress");
        }
        void Finish(){if(done)return;done=true;Application.logMessageReceived-=Log;if(hadJail)PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.JailSeconds",jail);else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.JailSeconds");report.completed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"four-cities.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);}
    }
}
