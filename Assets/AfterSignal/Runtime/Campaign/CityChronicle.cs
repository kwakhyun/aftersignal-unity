using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AfterSignal
{
    // Optional data: choiceA/choiceB label a choice step's options, portrait overrides the witness art,
    // formation picks a battle spawn layout, enemyHealth/timeLimit override the scaled defaults (0 = auto).
    [Serializable] public sealed class StoryStep { public int site; public string kind,person,label,line,battleMode,outro,choiceA,choiceB,portrait,formation; public int waves=1,enemyCount=4,enemyHealth;public float duration=40,timeLimit;public bool world;public Vector3 position; }
    // tutorial marks the opening operation, finale marks the quest whose last step reveals the ending (endingA/endingB).
    [Serializable] public sealed class StoryQuest { public string id,title,arc,after,summary,endingA,endingB; public bool main,tutorial,finale;public int unlockAt,reward;public StoryStep[] steps; }
    [Serializable] public sealed class StoryCatalog { public StoryQuest[] quests; }
    [Serializable] public sealed class StoryEntry { public string id; public int step,choice;public bool accepted,rewarded; }
    [Serializable] public sealed class StorySave { public string tracked="main01";public List<StoryEntry> entries=new List<StoryEntry>(); }
    public sealed class CityChronicle:MonoBehaviour
    {
        public const string SaveKey="AFTERSIGNAL.Unity.Chronicle.V1";
        // Battle steps start only on foot, without a wanted level, inside this radius of the operation point.
        public const float BattleStartRadius=34;
        public const string DefaultChoiceA="기록 공개 · 공동 감독",DefaultChoiceB="당사자 동의 · 보호 우선";
        public static CityChronicle Instance{get;private set;}
        public static bool IntroProtected=>Instance&&Instance.Quests!=null&&!Instance.Done(Instance.Opening);
        public static bool SuppressSave;
        public StoryQuest[] Quests{get;private set;}
        public StorySave Progress{get;private set;}
        public StoryQuest Opening{get;private set;}
        public StoryQuest Finale{get;private set;}
        public int CompletedMain=>Quests.Count(q=>q.main&&Done(q));
        public StoryQuest Tracked=>Quests.FirstOrDefault(q=>q.id==Progress.tracked);
        public StoryStep CurrentStep=>Tracked!=null&&!Done(Tracked)?Tracked.steps[Entry(Tracked).step]:null;
        public int SocialMilestone=>CompletedMain;
        GameDirector game;GameObject marker;string markerKey;float next,scan,hintAt;Vector3 scanOrigin;bool scanning,combatStarted,grounded;
        readonly List<GangMember> guards=new List<GangMember>();
        readonly HashSet<string> mainLine=new HashSet<string>();readonly Dictionary<string,int> ordinals=new Dictionary<string,int>();
        public StoryEntry Entry(StoryQuest q){var e=Progress.entries.FirstOrDefault(p=>p.id==q.id);if(e==null){e=new StoryEntry{id=q.id};Progress.entries.Add(e);}return e;}
        public bool Done(StoryQuest q)=>Entry(q).step>=q.steps.Length;
        public bool Available(StoryQuest q)=>CompletedBefore(q)&&(!q.main?CompletedMain>=q.unlockAt:true);
        bool CompletedBefore(StoryQuest q)=>string.IsNullOrEmpty(q.after)||Quests.Any(p=>p.id==q.after&&Done(p));
        StoryQuest Find(string id)=>string.IsNullOrEmpty(id)?null:Quests.FirstOrDefault(p=>p.id==id);
        /// <summary>Quests on the `after` chain that leads to the finale; other main quests are side arcs.</summary>
        public bool OnMainLine(StoryQuest q)=>q!=null&&mainLine.Contains(q.id);
        /// <summary>Depth of a quest along its `after` chain (opening = 0); drives combat scaling.</summary>
        public int Ordinal(StoryQuest q)
        {
            if(q==null)return 0;if(ordinals.TryGetValue(q.id,out int depth))return depth;
            depth=0;var seen=new HashSet<string>{q.id};for(var p=Find(q.after);p!=null&&seen.Add(p.id);p=Find(p.after))depth++;
            ordinals[q.id]=depth;return depth;
        }
        static string Or(string value,string fallback)=>string.IsNullOrEmpty(value)?fallback:value;
        public static string ChoiceLabel(StoryStep step,int choice)=>choice==1?Or(step?.choiceA,DefaultChoiceA):choice==2?Or(step?.choiceB,DefaultChoiceB):"";
        /// <summary>Decisions made on quests that currently contain a choice step (stale legacy values are ignored).</summary>
        public int Votes(int side)=>Quests.Count(q=>q.steps.Any(s=>s.kind=="choice")&&Entry(q).choice==side);
        public string Ending(StoryQuest q)
        {
            string text=Votes(1)>Votes(2)?Or(q?.endingA,"공개 기록 결말: 시민 의회가 원본과 책임 보고서를 공개하고 복원 본부를 공동 감독한다."):Or(q?.endingB,"동의의 기록 결말: 피해자별 동의에 따라 기록을 돌려주고 독립 돌봄 위원회가 본부를 감독한다.");
            return text+"\n완료한 주민 의뢰 "+Quests.Count(p=>!p.main&&Done(p))+"건이 후속 돌봄망에 반영됩니다.";
        }
        void Awake()
        {
            Instance=this;game=GetComponent<GameDirector>();Quests=JsonUtility.FromJson<StoryCatalog>(Resources.Load<TextAsset>("Story/CityChronicle").text).quests;
            foreach(var quest in Quests)foreach(var step in quest.steps)if(step.world)step.position=CompactCityLayout.Migrate(step.position);
            Opening=Quests.FirstOrDefault(q=>q.tutorial)??Quests[0];Finale=Quests.FirstOrDefault(q=>q.finale);
            for(var p=Finale;p!=null&&mainLine.Add(p.id);)p=Find(p.after);
            try{Progress=JsonUtility.FromJson<StorySave>(SaveStore.GetString(SaveKey,""));}catch{Progress=null;}
            if(Progress==null)Progress=new StorySave();if(Progress.entries==null)Progress.entries=new List<StoryEntry>();
            foreach(var q in Quests){var e=Entry(q);e.step=Mathf.Clamp(e.step,0,q.steps.Length);}
            Entry(Opening).accepted=true;
            if(Tracked==null)Progress.tracked=Opening.id;
            if(Tracked!=null&&Done(Tracked)&&Tracked.main){var continuation=Continuation(Tracked);if(continuation!=null){Entry(continuation).accepted=true;Progress.tracked=continuation.id;}}
        }
        /// <summary>The quest to track after `done`: the main line continues first; a finished side arc returns to it.</summary>
        StoryQuest Continuation(StoryQuest done)
        {
            var follow=Quests.Where(p=>p.main&&p.after==done.id&&!Done(p)).ToArray();
            if(mainLine.Count==0)return follow.FirstOrDefault();
            var line=follow.FirstOrDefault(OnMainLine);if(line!=null||OnMainLine(done))return line;
            return follow.FirstOrDefault()??Quests.Where(p=>OnMainLine(p)&&!Done(p)&&Available(p)).OrderBy(Ordinal).FirstOrDefault();
        }
        public void Track(StoryQuest q){if(!Available(q)||Done(q))return;Entry(q).accepted=true;Progress.tracked=q.id;Save();Refresh();}
        public void ResetProgress(){Progress=new StorySave{tracked=Opening.id};Entry(Opening).accepted=true;Save();Refresh();}
        public void Save(){if(!SuppressSave){SaveStore.SetString(SaveKey,JsonUtility.ToJson(Progress));SaveStore.Save();}}
        public void Refresh(){if(marker)Destroy(marker);marker=null;markerKey=null;scanning=false;combatStarted=false;foreach(var g in guards)if(g)Destroy(g.gameObject);guards.Clear();}
        bool AtSite(StoryStep step)
        {
            if(step.world)return game.stage==StageId.UrbanCity;
            if(game.stage==StageId.UrbanInterior)return ResidentialWorld.VisitHome<0&&UrbanCatalog.Current==step.site;
            return game.stage==StageId.Residence&&step.site==0||game.stage==StageId.School&&step.site==5||game.stage==StageId.Clinic&&step.site==4||game.stage==StageId.Headquarters&&step.site==14;
        }
        Vector3 LocalTarget()=>CurrentStep!=null&&CurrentStep.world?CurrentStep.position:game.stage==StageId.Residence?new Vector3(8,.1f,-3):new Vector3(18,.1f,-3);
        void Hint(string text){if(Time.time<hintAt)return;hintAt=Time.time+6;game.Toast(text,4);}
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
                if(CampaignBattle.Active||Vector3.Distance(game.Player.transform.position,step.position)>=BattleStartRadius)return;
                if(WantedSystem.Level>0){Hint("작전 지점 도착 · 경찰 추적 중에는 작전을 시작할 수 없습니다. 수배를 먼저 해제하세요.");return;}
                if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Driving){Hint("작전 지점 도착 · 차량에서 내려 도보로 진입하면 작전이 시작됩니다.");return;}
                CampaignBattle.Begin(this,step);
                return;
            }
            string key=Tracked.id+":"+Entry(Tracked).step;
            if(markerKey!=key){Refresh();Spawn(step);markerKey=key;}
            // World markers spawn before their district is near; settle them onto the walkable ground once it is.
            if(marker&&!grounded&&step.world&&(marker.transform.position-game.Player.transform.position).sqrMagnitude<90*90){grounded=true;if(CityGangWar.FindGround(step.position,out var floor))marker.transform.position=floor;}
        }
        void Spawn(StoryStep step)
        {
            marker=new GameObject("STORY / "+Tracked.title);marker.transform.SetParent(transform,false);marker.transform.position=LocalTarget();grounded=false;
            InteractionPoint point;
            if(step.kind=="meet"||step.kind=="deliver"||step.kind=="choice")
            {
                var npc=marker.AddComponent<CityNpc>();marker.AddComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
                npc.fixedQuest=true;npc.Configure(7000+Array.IndexOf(Quests,Tracked),"사건 관계자",Or(step.person,"기록 담당자"),step.line);point=npc.point;
                PeopleArt.Attach(marker,WitnessArt(step));
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
        // Data `portrait` (a PeopleArt key) wins; otherwise named cast members use their dedicated story sprite.
        static string WitnessArt(StoryStep step)=>!string.IsNullOrEmpty(step.portrait)?step.portrait:StorySprites.Key(step.person)??"OfficeWoman";
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
            if(q.finale&&expected==q.steps.Length-1)line+="\n\n"+Ending(q);
            CityLife.Instance.StoryDialogue(q.title,Or(step.person,"서하 · 조사 기록"),line,step.kind=="choice",choice=>Advance(q.id,expected,choice),ChoiceLabel(step,1),ChoiceLabel(step,2));
        }
        public void Advance(string id,int expected,int choice=0)
        {
            var q=Find(id);if(q==null)return;var e=Entry(q);
            if(e.step!=expected||e.step>=q.steps.Length||!e.accepted)return;
            var open=Quests.Where(p=>!Done(p)&&Available(p)).ToList();
            string notice=choice!=0?"결정 기록 · "+ChoiceLabel(q.steps[expected],choice):null;
            if(choice!=0)e.choice=choice;e.step++;
            if(Done(q))
            {
                if(!e.rewarded){e.rewarded=true;LifeState.Earn(q.reward);notice=(notice!=null?notice+"\n":"")+"의뢰 완료 · "+q.title+" / "+q.reward+" C";}
                var follow=q.main?Continuation(q):null;
                if(follow!=null){Entry(follow).accepted=true;Progress.tracked=follow.id;}
                // Every other quest this completion unlocked (side arcs of the main story, resident requests) is announced, not auto-tracked.
                var opened=Quests.Where(p=>p!=follow&&!Done(p)&&Available(p)&&!open.Contains(p)).OrderByDescending(p=>p.main).ToList();
                if(opened.Count>0)notice=(notice!=null?notice+"\n":"")+"새 의뢰 개방 · "+string.Join(" / ",opened.Take(2).Select(p=>(p.main?"메인 ":"주민 ")+p.title))+(opened.Count>2?" 외 "+(opened.Count-2)+"건":"")+" · J 일지에서 추적";
                if(q.finale&&q.steps[q.steps.Length-1].kind=="battle"&&CityLife.Instance)CityLife.Instance.StoryDialogue(q.title,"서하 · 조사 기록",Ending(q),false,_=>{});
            }
            if(notice!=null)game.Toast(notice,notice.Contains("새 의뢰")?8:6);
            Save();Refresh();
        }
        public bool Guide(out Vector3 target,out string label)
        {
            target=Vector3.zero;label="";var step=CurrentStep;if(step==null)return false;
            label=(Tracked.main?"메인":"서브")+" · "+Tracked.title+" · "+step.label+" [J]";
            if(game.stage==StageId.UrbanCity){target=step.world?step.position+Vector3.up:step.site==0?CompactHome.Exit+Vector3.up:UrbanCatalog.Door(step.site)+Vector3.up;if(step.world&&NeonHarbor.Region(target)!=NeonHarbor.Region(game.Player.transform.position))label+=" · 여객선/항공기로 해협 횡단";return true;}
            if(AtSite(step)){target=marker?marker.transform.position+Vector3.up:LocalTarget()+Vector3.up;return true;}
            if(CivicWorld.Interior(game.stage))
            {
                var exit=InteractionPoint.All.Where(p=>p&&p.gameObject.activeInHierarchy&&(p.kind==InteractionKind.UrbanExit||p.kind==InteractionKind.ReturnTown||p.kind==InteractionKind.FacilityTravel&&(p.destination==StageId.UrbanCity||p.destination==StageId.Haven))).OrderBy(p=>(p.transform.position-game.Player.transform.position).sqrMagnitude).FirstOrDefault();
                if(exit){target=exit.transform.position;label="의뢰 경로 · 밖으로 이동 → "+(step.world?step.label:step.site==0?"서하의 집":UrbanCatalog.Name(step.site));return true;}
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
