using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AfterSignal
{
    public sealed class CrowdSafetyProbe:MonoBehaviour
    {
        [Serializable]sealed class Report{public bool completed;public List<string> passed=new(),errors=new();}
        readonly Report report=new();string output;float began;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-crowd-safety-probe")&&!FindAnyObjectByType<CrowdSafetyProbe>()){GameDirector.SkipTitle=true;new GameObject("Crowd safety essentials").AddComponent<CrowdSafetyProbe>();}}
        void Awake(){DontDestroyOnLoad(gameObject);LifeState.SuppressSave=CityChronicle.SuppressSave=true;RespawnNetwork.SuppressSave=true;began=Time.realtimeSinceStartup;output=Path.GetFullPath(QualitySession.Arg("-quality-output","Artifacts/CrowdPerformance/Safety"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)report.errors.Add(message+"\n"+stack);}
        void Check(bool pass,string name){(pass?report.passed:report.errors).Add(name);Debug.Log("CROWD CHECK "+pass+" / "+name);}
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));while(!GameDirector.Instance||!GameDirector.Instance.Ready||!MaritimeWorld.Instance||!MaritimeWorld.Instance.Built)yield return null;
            var game=GameDirector.Instance;game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;game.CloseDialogue();game.SetPaused(false);CityChronicle.Instance.enabled=false;
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b is RiftIncursion||b is CityGangWar||b is GangStrongholds||b is CivicTerrorEvents||b is GangMember||b is GangConvoy||b is GangCrime||b is SeaCombat||b is CityActivityDirector)b.enabled=false;
            yield return new WaitForSeconds(4);
            var overlaps=new List<string>();var live=WorldActor.All.Where(a=>a&&a.Alive&&!a.monster&&!a.helicopter).ToArray();for(int i=0;i<live.Length;i++)for(int j=i+1;j<live.Length;j++)if((live[i].transform.position-live[j].transform.position).sqrMagnitude<.45f*.45f)overlaps.Add(live[i].name+" ["+live[i].transform.parent?.name+"] "+live[i].transform.position+" / "+live[j].name+" ["+live[j].transform.parent?.name+"] "+live[j].transform.position);File.WriteAllLines(Path.Combine(output,"overlaps.txt"),overlaps);
            var passengers=live.Where(a=>a.GetComponent<TransitTraveller>()).ToArray();int stackedPassengers=0;for(int i=0;i<passengers.Length;i++)for(int j=i+1;j<passengers.Length;j++)if((passengers[i].transform.position-passengers[j].transform.position).sqrMagnitude<.45f*.45f)stackedPassengers++;
            Check(passengers.Length>5&&stackedPassengers==0,"Airport and harbour passengers spawn without shared waiting positions");
            var site=new Vector3(1000,200,-800);game.Player.Respawn(site+Vector3.right*30);var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=site-Vector3.up*.5f;floor.transform.localScale=new Vector3(70,1,70);var root=new GameObject("Placement fixture",typeof(FacilityCrowd));root.transform.position=site;var seed=root.GetComponent<FacilityCrowd>();seed.count=70;seed.radius=15;Physics.SyncTransforms();
            var people=new List<WorldActor>();var near=new List<WorldActor>();
            for(int i=0;i<70;i++)if(ExpansionWorld.TrySpawnPosition(seed,i,out var p)){var go=new GameObject("Spacing resident",typeof(SpriteRenderer),typeof(WorldActor));go.transform.position=p;PeopleArt.Attach(go,"CivilianMan");people.Add(go.GetComponent<WorldActor>());}
            int pairs=0,most=0;foreach(var a in people){ActorSpatialIndex.Nearby(a.transform.position,12,near);most=Mathf.Max(most,near.Count);foreach(var b in people)if(a!=b&&(a.transform.position-b.transform.position).sqrMagnitude<1.4f*1.4f)pairs++;}
            Debug.Log("CROWD DENSITY / accepted "+people.Count+" / maximum within 12m "+most);
            Check(people.Count>5&&people.Count<70,"Dense facility admits a bounded population instead of stacking all requested people");Check(pairs==0,"New residents preserve physical spacing");Check(most<=24,"Walking crowd remains distributed across the facility");
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=site+Vector3.up*2;blocker.transform.localScale=new Vector3(40,4,40);Physics.SyncTransforms();Check(!ExpansionWorld.TrySpawnPosition(seed,200,out _),"Blocked facilities defer creation instead of falling back to one shared point");Destroy(blocker);
            root.transform.position=site+Vector3.up*100;Physics.SyncTransforms();Check(!ExpansionWorld.TrySpawnPosition(seed,201,out _),"Unsupported spawn sites do not create falling or stacked residents");
            int accepted=0;for(int i=0;i<12;i++)if(PopulationBudget.ClaimFrame())accepted++;Check(accepted<=4,"A frame cannot claim an unbounded creation burst");
            var test=people[0];ActorSpatialIndex.Nearby(test.transform.position,2,near);Check(near.Contains(test),"Spatial query returns a nearby living actor");test.gameObject.SetActive(false);ActorSpatialIndex.Nearby(test.transform.position,2,near);Check(!near.Contains(test),"Deactivated actors immediately leave spatial queries");test.gameObject.SetActive(true);
            var far=people[1];far.transform.position=site+Vector3.forward*210;float next=0,last=0;ActorWorkBudget.Tick(far,ref next,ref last,out _);Check(!ActorWorkBudget.Tick(far,ref next,ref last,out _),"Distant routine work is throttled");Check(ActorWorkBudget.Tick(far,ref next,ref last,out _,true),"Urgent activity bypasses routine throttling");
            foreach(var person in people)if(person)Destroy(person.gameObject);yield return null;
            var police=PoliceOfficer.Create(WantedSystem.Instance,site,2,0);police.enabled=false;var gang=GangMember.Create(site+Vector3.right*9,0,0);gang.enabled=false;yield return null;
            Check(FactionCombat.NearestOpponent(police.Body,30)==gang.Body,"Spatial combat search still locates nearby gang opponents");
            Check(Physics.Raycast(site+Vector3.up*2,Vector3.down,out var support,4,1,QueryTriggerInteraction.Ignore)&&support.normal.y>.8f,"Optimization preserves floor colliders");
            float boardingDeadline=Time.time+20;while(!IntercityService.All.Any(s=>s.Boarded>0)&&Time.time<boardingDeadline)yield return null;
            Check(IntercityService.All.Any(s=>s.Boarded>0),"Spaced terminal queues still board operating transport");
            report.completed=true;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(report.errors.Count==0?0:1);
        }
        void Update(){if(Time.realtimeSinceStartup-began>200){report.errors.Add("Safety probe timeout");File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
