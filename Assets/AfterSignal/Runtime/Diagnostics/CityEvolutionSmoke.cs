using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class CityEvolutionSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new List<string>(),errors=new List<string>();}
        readonly Report report=new Report();string output;bool finished,exception;
        readonly Dictionary<string,int> ints=new Dictionary<string,int>();
        readonly Dictionary<string,float> floats=new Dictionary<string,float>();
        readonly List<string> absent=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-city-evolution-smoke")&&!FindAnyObjectByType<CityEvolutionSmoke>()){GameDirector.SkipTitle=true;new GameObject("City evolution integration").AddComponent<CityEvolutionSmoke>();}}
        void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;LifeState.SuppressSave=true;
            output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CityEvolution/Smoke"));Directory.CreateDirectory(output);
            foreach(var k in new[]{"AFTERSIGNAL.Unity.Stage","AFTERSIGNAL.Unity.Memories",UrbanCatalog.Prefix+"Site",UrbanCatalog.Prefix+"Car",UrbanCatalog.Prefix+"CarStage",UrbanCatalog.Prefix+"CarType",UrbanCatalog.Prefix+"CarColor",UrbanCatalog.Prefix+"CarWheels",UrbanCatalog.Prefix+"CarSpoiler"}){if(PlayerPrefs.HasKey(k))ints[k]=PlayerPrefs.GetInt(k);else absent.Add(k);}
            foreach(var name in new[]{"CarX","CarZ","CarYaw","CarFuel","CarHealth"}){string k=UrbanCatalog.Prefix+name;if(PlayerPrefs.HasKey(k))floats[k]=PlayerPrefs.GetFloat(k);else absent.Add(k);}
            Application.logMessageReceived+=Log;
        }
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert){report.errors.Add(m+"\n"+s);exception=true;}}
        void Check(bool ok,string name){(ok?report.passed:report.errors).Add(name);}
        void Update(){if(!finished&&exception){Finish();return;}if(!finished&&Time.realtimeSinceStartup>260){Check(false,"Integration timeout");Finish();}}
        IEnumerator Load(StageId stage,int site=0)
        {
            LifeState.Heat=0;ResidentialWorld.VisitHome=-1;PlayerPrefs.SetInt(UrbanCatalog.Prefix+"Site",site);
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(stage));
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            var g=GameDirector.Instance;g.Input.ExternalControl=true;g.Input.ExternalFrame=ControlFrame.Empty;g.CloseDialogue();g.SetPaused(false);
            yield return new WaitForSeconds(.6f);
        }
        IEnumerator Start()
        {
            LifeState.Load();LifeState.Hours=10;
            yield return Load(StageId.UrbanCity);
            string[] directions={"Front","FrontRight","Right","BackRight","Back","BackLeft","Left","FrontLeft","Idle"};
            foreach(var d in directions)Check(PeopleArt.Sheet(d,true).Length==16,"Seoha "+d+" normalized frames");
            string[] roles={"Doctor","Nurse","PatientMan","PatientWoman","TeacherMan","TeacherWoman","StudentBoy","StudentGirl","OfficeMan","OfficeWoman","Worker","ElderMan","ElderWoman","CivilianMan","CivilianWoman","Bartender","Police","Swat","GangCrimson","GangViolet","GangChrome","PoliceShotgun"};
            foreach(var role in roles)Check(PeopleArt.Sheet(role).Length==16,role+" four facing directions");
            var game=GameDirector.Instance;
            game.enabled=false;game.Player.Respawn(new Vector3(100,.1f,-296),false);
            var control=ControlFrame.Empty;control.pointer=new Vector2(800,450);
            var seen=new HashSet<int>();var motion=game.Player.GetComponent<SeoLocomotion>();
            for(int d=0;d<8;d++)
            {
                float angle=d*45*Mathf.Deg2Rad;control.move=new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));control.run=false;
                for(int n=0;n<25;n++)game.Player.Tick(control,.02f);
                seen.Add(motion.Sector);
            }
            Check(seen.Count==8,"Locomotion selects all eight headings");
            float phase=motion.Cycle;control.move=Vector2.zero;
            for(int n=0;n<40;n++)game.Player.Tick(control,.02f);
            float stopped=motion.Cycle;for(int n=0;n<30;n++)game.Player.Tick(control,.02f);
            Check(Mathf.Abs(stopped-motion.Cycle)<.001f,"Stationary feet stop the gait clock");
            game.enabled=true;
            yield return Gallery(directions,roles);
            yield return Load(StageId.School);
            game=GameDirector.Instance;
            Check(game.stageLength>=180&&CommunityWorld.Instance.RoomCenters.Count==10,"Expanded school includes ten teaching and service rooms");
            Check(CommunityWorld.Instance.People.Count(p=>p.student)>=24,"School has distinct male and female students");
            LifeState.Hours=10;foreach(var p in CommunityWorld.Instance.People)p.Replan();
            Check(CommunityWorld.Instance.People.Any(p=>p.Activity==CivicActivity.Lesson),"Students attend scheduled classes");
            Overview("school",new Vector3(91,75,-90),new Vector3(91,0,0));
            yield return Load(StageId.Clinic);
            Check(CommunityWorld.Instance.People.Count(p=>p.patient)==8,"Hospital includes eight patient routines");
            var patient=CommunityWorld.Instance.People.First(p=>p.patient);
            LifeState.Hours=23;patient.Replan();patient.transform.position=patient.rest;yield return null;
            Check(patient.Activity==CivicActivity.Rest,"Patients rest in assigned beds at night");
            LifeState.Hours=12.5f;patient.Replan();Check(patient.Activity==CivicActivity.Meal,"Patients move to meals at lunchtime");
            LifeState.Hours=10;
            Overview("hospital",new Vector3(73,26,-38),new Vector3(73,0,16));
            yield return Load(StageId.Headquarters);
            Check(CommunityWorld.Instance.People.Count>=12,"Headquarters has autonomous staff");
            yield return Load(StageId.Residence);
            Check(FindObjectsByType<NeighborEntrance>().Length==12,"Apartment has twelve neighbor entrances on four floors");
            Check(Physics.Raycast(new Vector3(25,22.5f,-6),Vector3.down,out var upper,1,1)&&upper.point.y>=21.9f,"Seoha apartment retains a solid upper floor");
            for(int site=0;site<16;site++)
            {
                yield return Load(StageId.UrbanInterior,site);
                game=GameDirector.Instance;
                int supported=0;
                foreach(var p in new[]{new Vector3(5,.5f,0),new Vector3(game.stageLength-3,.5f,0),new Vector3(8,.5f,-game.halfDepth+2),new Vector3(8,.5f,game.halfDepth-2)})
                    if(Physics.Raycast(p,Vector3.down,out var hit,1,1,QueryTriggerInteraction.Ignore)&&hit.normal.y>.8f)supported++;
                game.enabled=false;game.Player.Respawn(new Vector3(7,.1f,0),false);control=ControlFrame.Empty;control.move=Vector2.right;
                for(int n=0;n<60;n++)game.Player.Tick(control,.02f);game.enabled=true;
                Check(supported==4&&game.Player.transform.position.y>=-.1f,"Facility "+site+" continuous collision floor");
                if(site==1){Check(CommunityWorld.Instance.People.Count(p=>p.prisoner)==3,"Police holding cells contain three prisoners");Overview("custody",new Vector3(59,15,-17),new Vector3(61,1,17));}
            }
            yield return Load(StageId.UrbanInterior,38);
            LifeState.Earn(5000);PlayerPrefs.SetInt(UrbanCatalog.Prefix+"Car",1);PlayerPrefs.SetFloat(UrbanCatalog.Prefix+"CarHealth",13);
            var life=CityLife.Instance;life.Garage();int cash=LifeState.Credits;life.Options[0].action();
            Check(PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarHealth")==100&&LifeState.Credits==cash-150,"Garage repairs the saved owned vehicle and charges once");
            life.Options[2].action();Check(PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarWheels")==1,"Wheel customization persists");life.Dismiss();
            Overview("garage",new Vector3(37,25,-39),new Vector3(37,0,8));
            yield return Load(StageId.UrbanInterior,39);
            Check(FindObjectsByType<CivicRoutine>().Any(p=>p.GetComponent<DirectionalPerson>().art=="Bartender"),"Neon bar includes bartenders and patrons");
            Overview("bar",new Vector3(37,16,-27),new Vector3(37,1,10));
            yield return Load(StageId.UrbanInterior,3);
            cash=LifeState.Credits;UrbanCrime.Instance.StartHeist();Check(WantedSystem.Level>0,"Player robbery alerts police");
            WantedSystem.Clear("");yield return new WaitForSeconds(10.5f);
            Check(LifeState.Credits==cash+1600,"Bank robbery awards cash only after the countdown");
            LifeState.Hours=23;yield return Load(StageId.UrbanCity);yield return new WaitForSeconds(1.2f);game=GameDirector.Instance;
            Check(FindObjectsByType<GangCrime>().Length>0,"Night-time gangs receive autonomous crime plans");
            var resident=FindObjectsByType<CityNpc>().Where(n=>!n.fixedQuest&&n.GetComponent<WorldActor>().Alive).OrderByDescending(n=>FindObjectsByType<PoliceOfficer>().Select(o=>Vector3.Distance(o.transform.position,n.transform.position)).DefaultIfEmpty(999).Min()).First();
            var gangster=GangMember.Create(resident.transform.position+Vector3.right*2,0,0);gangster.Body.health=500;var crime=gangster.gameObject.AddComponent<GangCrime>();
            yield return null;crime.Plan(0);int reports=CitySafety.Instance.Reports;yield return new WaitForSeconds(.8f);
            Check(crime.Loot>0&&(CitySafety.Instance.Reports>reports||resident.Fleeing),"Gang intimidation makes citizens flee and call police");
            yield return new WaitForSeconds(3.5f);
            Check(CitySafety.Instance.Dispatches>0,"Police respond to a witnessed gang crime");
            var victim=FindObjectsByType<CityPedestrian>().First(p=>p.GetComponent<CityNpc>()&&!p.dead);
            victim.GetComponent<WorldActor>().health=25;
            if(!victim.GetComponent<MedicalPending>())victim.gameObject.AddComponent<MedicalPending>();
            var ambulance=EmergencyAmbulance.Create(victim.GetComponent<WorldActor>(),()=>{});
            yield return null;
            ambulance.transform.position=new Vector3(victim.transform.position.x,.02f,CityRoadNetwork.NearestJunction(victim.transform.position).z+5);
            float until=Time.time+28;while(ambulance&&ambulance.Phase<3&&Time.time<until)yield return null;
            Check(ambulance&&ambulance.Phase==3,"Medical team reaches and loads the injured citizen");
            if(ambulance&&ambulance.Phase==3){ambulance.transform.position=UrbanCatalog.Door(4)+Vector3.back*10;yield return null;Check(victim.GetComponent<WorldActor>().health>=70,"Ambulance delivers citizen to hospital treatment");}
            Finish();
        }
        IEnumerator Gallery(string[] directions,string[] roles)
        {
            var game=GameDirector.Instance;game.SetPaused(true);game.CameraRig.enabled=false;
            var cam=Camera.main;cam.transform.SetPositionAndRotation(new Vector3(0,0,-22),Quaternion.identity);cam.orthographic=true;cam.orthographicSize=7.2f;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.075f,.1f,.13f);cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;RenderSettings.fog=false;
            foreach(var clock in FindObjectsByType<CityClock>())clock.enabled=false;
            foreach(var canvas in FindObjectsByType<Canvas>())canvas.enabled=false;
            var objects=new List<GameObject>();
            for(int row=0;row<4;row++)for(int d=0;d<8;d++)
            {
                var go=new GameObject("Motion "+d,typeof(SpriteRenderer));go.layer=31;go.transform.position=new Vector3(-11+d*3.1f,4.3f-row*3.05f,0);var r=go.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Sheet(directions[d],true)[new[]{0,3,8,11}[row]];r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");objects.Add(go);
            }
            yield return null;Capture("seo-eight-directions");
            foreach(var go in objects)Destroy(go);objects.Clear();
            for(int row=0;row<5;row++)for(int d=0;d<4;d++)
            {
                var go=new GameObject("Person "+row,typeof(SpriteRenderer));go.layer=31;go.transform.position=new Vector3(-7+d*4.5f,4.5f-row*2.8f,0);var r=go.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get(roles[row],d);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");objects.Add(go);
            }
            yield return null;Capture("npc-four-directions");
            foreach(var go in objects)Destroy(go);
        }
        void Overview(string name,Vector3 at,Vector3 target)
        {
            var game=GameDirector.Instance;game.SetPaused(true);game.CameraRig.enabled=false;var cam=Camera.main;cam.transform.SetPositionAndRotation(at,Quaternion.LookRotation(target-at));cam.fieldOfView=58;Capture(name);game.SetPaused(false);game.CameraRig.enabled=true;game.CameraRig.Snap();
        }
        void Capture(string name)
        {
            var cam=Camera.main;
            var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;
            var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);
        }
        void Finish()
        {
            finished=true;report.completed=report.errors.Count==0;
            foreach(var item in ints)PlayerPrefs.SetInt(item.Key,item.Value);foreach(var item in floats)PlayerPrefs.SetFloat(item.Key,item.Value);foreach(var key in absent)PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(output,"city-evolution.json"),JsonUtility.ToJson(report,true));Application.Quit(report.completed?0:1);
        }
    }
}
