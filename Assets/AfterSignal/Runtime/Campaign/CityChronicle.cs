using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AfterSignal
{
    [Serializable] public sealed class StoryStep { public int site; public string kind,person,label,line,battleMode,outro; public int waves=1,enemyCount=4;public float duration=40;public bool world;public Vector3 position; }
    [Serializable] public sealed class StoryQuest { public string id,title,arc,after,summary; public bool main;public int unlockAt,reward;public StoryStep[] steps; }
    [Serializable] public sealed class StoryCatalog { public StoryQuest[] quests; }
    [Serializable] public sealed class StoryEntry { public string id; public int step,choice;public bool accepted,rewarded; }
    [Serializable] public sealed class StorySave { public string tracked="main01";public List<StoryEntry> entries=new List<StoryEntry>(); }
    public sealed class CityChronicle:MonoBehaviour
    {
        public const string SaveKey="AFTERSIGNAL.Unity.Chronicle.V1";
        public static CityChronicle Instance{get;private set;}
        public static bool SuppressSave;
        public StoryQuest[] Quests{get;private set;}
        public StorySave Progress{get;private set;}
        public int CompletedMain=>Quests.Count(q=>q.main&&Done(q));
        public StoryQuest Tracked=>Quests.FirstOrDefault(q=>q.id==Progress.tracked);
        public StoryStep CurrentStep=>Tracked!=null&&!Done(Tracked)?Tracked.steps[Entry(Tracked).step]:null;
        public int SocialMilestone=>CompletedMain;
        GameDirector game;GameObject marker;string markerKey;float next,scan;Vector3 scanOrigin;bool scanning,combatStarted;
        readonly List<GangMember> guards=new List<GangMember>();
        public StoryEntry Entry(StoryQuest q){var e=Progress.entries.FirstOrDefault(p=>p.id==q.id);if(e==null){e=new StoryEntry{id=q.id};Progress.entries.Add(e);}return e;}
        public bool Done(StoryQuest q)=>Entry(q).step>=q.steps.Length;
        public bool Available(StoryQuest q)=>CompletedBefore(q)&&(!q.main?CompletedMain>=q.unlockAt:true);
        bool CompletedBefore(StoryQuest q)=>string.IsNullOrEmpty(q.after)||Quests.Any(p=>p.id==q.after&&Done(p));
        void Awake()
        {
            Instance=this;game=GetComponent<GameDirector>();Quests=JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("Story/CityChronicle").text).quests;
            foreach(var quest in Quests)foreach(var step in quest.steps)if(step.world)step.position=CompactCityLayout.Migrate(step.position);
            try{Progress=JsonUtility.FromJson<StorySave>(PlayerPrefs.GetString(SaveKey,""));}catch{Progress=null;}
            if(Progress==null)Progress=new StorySave();if(Progress.entries==null)Progress.entries=new List<StoryEntry>();
            foreach(var q in Quests){var e=Entry(q);e.step=Mathf.Clamp(e.step,0,q.steps.Length);}
            Entry(Quests[0]).accepted=true;
            if(Tracked==null)Progress.tracked=Quests[0].id;
            if(Tracked!=null&&Done(Tracked)&&Tracked.main){var continuation=Quests.FirstOrDefault(q=>q.main&&q.after==Tracked.id&&!Done(q));if(continuation!=null){Entry(continuation).accepted=true;Progress.tracked=continuation.id;}}
        }
        public void Track(StoryQuest q){if(!Available(q)||Done(q))return;Entry(q).accepted=true;Progress.tracked=q.id;Save();Refresh();}
        public void ResetProgress(){Progress=new StorySave();Entry(Quests[0]).accepted=true;Save();Refresh();}
        public void Save(){if(!SuppressSave){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(Progress));PlayerPrefs.Save();}}
        public void Refresh(){if(marker)Destroy(marker);marker=null;markerKey=null;scanning=false;combatStarted=false;foreach(var g in guards)if(g)Destroy(g.gameObject);guards.Clear();}
        bool AtSite(StoryStep step)
        {
            if(step.world)return game.stage==StageId.UrbanCity;
            if(game.stage==StageId.UrbanInterior)return ResidentialWorld.VisitHome<0&&UrbanCatalog.Current==step.site;
            return game.stage==StageId.Residence&&step.site==0||game.stage==StageId.School&&step.site==5||game.stage==StageId.Clinic&&step.site==4||game.stage==StageId.Headquarters&&step.site==14;
        }
        Vector3 LocalTarget()=>CurrentStep!=null&&CurrentStep.world?CurrentStep.position:game.stage==StageId.Residence?new Vector3(8,.1f,-3):new Vector3(18,.1f,-3);
        void Update()
        {
            if(!game||!game.Ready)return;
            if(game.Blocked)return;
            if(scanning)
            {
                if(Vector3.Distance(game.Player.transform.position,scanOrigin)>3||!marker){scanning=false;game.Toast("조사를 중단했습니다. 단말 근처에서 다시 시작하세요.");return;}
                scan-=Time.deltaTime;game.Toast("기록 복원 중 · "+Mathf.Max(0,scan).ToString("0.0")+"초",.2f);
                if(scan<=0){scanning=false;ReadCurrent();}
            }
            if(combatStarted&&guards.Count>0&&guards.All(g=>!g||!g.Body.Alive||g.Body.Downed)){combatStarted=false;ReadCurrent();}
            if(Time.time<next)return;next=Time.time+.4f;
            var step=CurrentStep;if(step==null||!AtSite(step)){if(marker)Refresh();return;}
            if(step.kind=="battle")
            {
                if(marker){Destroy(marker);marker=null;markerKey=null;}
                if(!CampaignBattle.Active&&Vector3.Distance(game.Player.transform.position,step.position)<70)CampaignBattle.Begin(this,step);
                return;
            }
            string key=Tracked.id+":"+Entry(Tracked).step;
            if(markerKey!=key){Refresh();Spawn(step);markerKey=key;}
        }
        void Spawn(StoryStep step)
        {
            marker=new GameObject("STORY / "+Tracked.title);marker.transform.SetParent(transform,false);marker.transform.position=LocalTarget();
            InteractionPoint point;
            if(step.kind=="meet"||step.kind=="deliver"||step.kind=="choice")
            {
                var npc=marker.AddComponent<CityNpc>();marker.AddComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
                npc.fixedQuest=true;npc.Configure(7000+Array.IndexOf(Quests,Tracked),"사건 관계자",step.person??"기록 담당자",step.line);point=npc.point;
                PeopleArt.Attach(marker,WitnessArt(step.person));
            }
            else
            {
                CommunityWorld.Box(marker.transform,"Evidence terminal",new Vector3(0,.6f,0),new Vector3(1.1f,1.2f,.6f),"Metal");
                CommunityWorld.Box(marker.transform,"Evidence display",new Vector3(0,1.3f,-.12f),new Vector3(.95f,.55f,.15f),"NeonAzure",false);
                var go=new GameObject("Investigate",typeof(InteractionPoint));go.transform.SetParent(marker.transform,false);go.transform.localPosition=Vector3.up*1.2f;point=go.GetComponent<InteractionPoint>();
            }
            point.kind=InteractionKind.Story;point.radius=3;point.title=step.label;
            var hook=point.gameObject.AddComponent<StoryObjective>();hook.owner=this;
            Beacon(marker.transform);
        }
        static string WitnessArt(string name)
        {
            if(string.IsNullOrEmpty(name))return "OfficeWoman";
            if(name.Contains("유라"))return FacilityPeople.Key(26);
            if(name.Contains("리안"))return FacilityPeople.Key(28);
            if(name.Contains("해린"))return FacilityPeople.Key(42);
            if(name.Contains("도윤"))return FacilityPeople.Key(27);
            if(name.Contains("세린"))return FacilityPeople.Key(32);
            if(name.Contains("수호"))return FacilityPeople.Key(47);
            if(name.Contains("나리"))return FacilityPeople.Key(44);
            if(name.Contains("미루"))return FacilityPeople.Key(25);
            if(name.Contains("한결"))return "Doctor";
            if(name.Contains("다은")||name.Contains("유진"))return "Nurse";
            if(name.Contains("민재")||name.Contains("태오"))return "Worker";
            if(name.Contains("수연"))return "TeacherWoman";
            if(name.Contains("지훈"))return "Police";
            if(name.Contains("라온"))return "Bartender";
            if(name.Contains("은서")||name.Contains("하린"))return "CivilianWoman";
            if(name.Contains("기록관"))return "OfficeMan";
            return "OfficeWoman";
        }
        static void Beacon(Transform parent)
        {
            var go=new GameObject("Story objective diamond",typeof(TextMesh),typeof(StoryBeacon));go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.up*2.7f;
            var t=go.GetComponent<TextMesh>();t.text="◆";t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.GetComponent<Renderer>().sharedMaterial=t.font.material;t.fontSize=48;t.characterSize=.08f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(1,.82f,.35f);
        }
        public void Interact()
        {
            if(game.Blocked||CurrentStep==null||!AtSite(CurrentStep)||!marker||Vector3.Distance(game.Player.transform.position,marker.transform.position)>4)return;
            if(CurrentStep.kind=="scan"){scanning=true;scan=3;scanOrigin=game.Player.transform.position;return;}
            if(CurrentStep.kind=="combat")
            {
                if(combatStarted)return;combatStarted=true;
                for(int i=0;i<3;i++){var at=CurrentStep.world?LocalTarget()+new Vector3(-5+i*5,0,6):new Vector3(26+i*3,.1f,3);var g=GangMember.Create(at,i,0);guards.Add(g);g.OnHit(Vector3.zero,true);NpcSpeech.Say(g,"그 기록은 두고 가!");}
                game.Toast("증거 방어 · 갱단 3명을 제압하세요",5);return;
            }
            ReadCurrent();
        }
        void ReadCurrent()
        {
            var q=Tracked;var step=CurrentStep;if(q==null||step==null)return;int expected=Entry(q).step;
            string line=step.line;
            if(q.id=="main24"&&expected==2)
            {
                int publicVotes=Progress.entries.Count(e=>e.choice==1),protectedVotes=Progress.entries.Count(e=>e.choice==2);
                line+="\n\n"+(publicVotes>protectedVotes?"공개 기록 결말: 시민 의회가 원본과 책임 보고서를 공개하고 복원 본부를 공동 감독한다.":"동의의 기록 결말: 피해자별 동의에 따라 기록을 돌려주고 독립 돌봄 위원회가 본부를 감독한다.");
                line+="\n완료한 주민 의뢰 "+Quests.Count(p=>!p.main&&Done(p))+"건이 후속 돌봄망에 반영됩니다.";
            }
            CityLife.Instance.StoryDialogue(q.title,step.person??"서하 · 조사 기록",line,step.kind=="choice",choice=>Advance(q.id,expected,choice));
        }
        public void Advance(string id,int expected,int choice=0)
        {
            var q=Quests.FirstOrDefault(p=>p.id==id);if(q==null)return;var e=Entry(q);
            if(e.step!=expected||e.step>=q.steps.Length||!e.accepted)return;
            if(choice!=0)e.choice=choice;e.step++;
            if(Done(q)&&!e.rewarded){e.rewarded=true;LifeState.Earn(q.reward);game.Toast("의뢰 완료 · "+q.title+" / "+q.reward+" C",6);}
            if(Done(q)&&q.main)
            {
                var follow=Quests.FirstOrDefault(p=>p.main&&p.after==q.id);
                if(follow!=null){Entry(follow).accepted=true;Progress.tracked=follow.id;}
            }
            Save();Refresh();
        }
        public bool Guide(out Vector3 target,out string label)
        {
            target=Vector3.zero;label="";var step=CurrentStep;if(step==null)return false;
            label=(Tracked.main?"메인":"서브")+" · "+Tracked.title+" · "+step.label+" [J]";
            if(game.stage==StageId.UrbanCity){target=step.world?step.position+Vector3.up:UrbanCatalog.Door(step.site)+Vector3.up;if(step.world&&NeonHarbor.Region(target)!=NeonHarbor.Region(game.Player.transform.position))label+=" · 여객선/항공기로 해협 횡단";return true;}
            if(AtSite(step)){target=LocalTarget()+Vector3.up;return true;}
            if(CivicWorld.Interior(game.stage))
            {
                var exit=InteractionPoint.All.FirstOrDefault(p=>p&&(p.kind==InteractionKind.UrbanExit||p.kind==InteractionKind.FacilityTravel||p.kind==InteractionKind.ReturnTown));
                if(exit){target=exit.transform.position;label="의뢰 경로 · 시내로 이동 → "+UrbanCatalog.Name(step.site);return true;}
            }
            return false;
        }
        void OnDestroy(){Save();if(Instance==this)Instance=null;}
    }
    public sealed class StoryBeacon:MonoBehaviour
    {
        void LateUpdate(){if(Camera.main)transform.rotation=Camera.main.transform.rotation;var r=GetComponent<Renderer>();if(r)r.enabled=GameDirector.Instance&&!GameDirector.Instance.Blocked;}
    }
    public sealed class StoryObjective:MonoBehaviour { public CityChronicle owner; }
}
