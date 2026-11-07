using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class FourCityCampaign:MonoBehaviour
    {
        // Chapters live in Resources/Story/FourCityCampaign.json. sync = the SYNC 1·2·3 core chapter, elite = the first threat is a lead unit,
        // decision = the campaign's choice (prompt, choiceA/choiceB), resultA/resultB = the line that replaces `line` after that choice.
        [Serializable] public sealed class Chapter
        {public string id,title,speaker,line,objective,prompt,choiceA,choiceB,resultA,resultB;public int facility,floor,enemies;public bool sync,elite,decision,finale;}
        [Serializable] sealed class ChapterCatalog {public Chapter[] chapters;}
        static Chapter[] chapters;
        public static Chapter[] Chapters=>chapters??=LoadChapters();
        static Chapter[] LoadChapters()
        {
            var asset=Resources.Load<TextAsset>("Story/FourCityCampaign");
            var list=asset?JsonUtility.FromJson<ChapterCatalog>(asset.text)?.chapters:null;
            if(list==null){Debug.LogError("FourCityCampaign: Resources/Story/FourCityCampaign.json is missing or invalid");list=Array.Empty<Chapter>();}
            return list;
        }
        static string Or(string value,string fallback)=>string.IsNullOrEmpty(value)?fallback:value;
        public static FourCityCampaign Instance{get;private set;}
        public int Step{get;private set;}
        public int Choice{get;private set;}
        public int Kills{get;private set;}
        public bool Complete=>Step>=Chapters.Length;
        public Chapter Current=>Complete?null:Chapters[Step];
        public string ChoiceLabel{get{var c=Array.Find(Chapters,x=>x.decision);return Choice==1?Or(c?.choiceA,"기록 공개와 공동 감독"):Or(c?.choiceB,"당사자 동의와 보호");}}
        public Vector3 Destination{get;private set;}
        public int Remaining{get{int count=0;foreach(var e in encounter)if(e&&e.Alive)count++;return count;}}
        readonly List<WorldActor> encounter=new();readonly List<VenueService> objectives=new();
        int encounterStep=-1,synchronized;float next;
        public static bool Contains(int index){foreach(var c in Chapters)if(c.facility==index)return true;return false;}
        void Awake(){Instance=this;Step=Mathf.Clamp(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.FourCities.Chapter",0),0,Chapters.Length);Choice=PlayerPrefs.GetInt("AFTERSIGNAL.Unity.FourCities.Choice",0);}
        public void BuildObjectives()
        {
            var witnesses=new HashSet<string>();
            for(int i=0;i<Chapters.Length;i++)
            {var chapter=Chapters[i];var v=FourCityWorld.Instance.Find(FourCityCatalog.Venues[chapter.facility].id);if(!v)continue;var at=new Vector3(3,chapter.floor*v.floorHeight+.1f,7);
                var key=StorySprites.Key(chapter.speaker);
                if(key!=null&&!chapter.speaker.Contains("무전")&&witnesses.Add(chapter.facility+":"+key))
                {
                    var witness=new GameObject("Campaign witness / "+chapter.speaker,typeof(SpriteRenderer),typeof(CityNpc));witness.transform.SetParent(v.transform,false);witness.transform.localPosition=new Vector3(-3,chapter.floor*v.floorHeight+.05f,5);
                    witness.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");var npc=witness.GetComponent<CityNpc>();npc.fixedQuest=true;npc.Configure(97500+i,"기록 협력자",chapter.speaker,"이 시설에서 도시 기억 복원 작전을 돕는 관계자.");npc.point.title=chapter.speaker;npc.point.dialogue="반가워요, 서하. 이 층의 기록 단말에서 현재 작전 자료를 확인할 수 있어요.\n"+chapter.objective;PeopleArt.Attach(witness,key);
                }
                var g=new CityGeometry(v.transform);g.Box("Campaign archive console",at+Vector3.up*.7f,new(2,1.4f,1.2f),"FutureCarbon",true);g.Box("Archive holographic panel",at+new Vector3(0,1.5f,0),new(1.7f,1.1f,.05f),"NeonCyan");g.Sign("기록 조사  E",at+new Vector3(0,2.5f,-.7f),.16f);g.Finish();
                var objective=VenueService.Add(v.transform,at+new Vector3(0,1,-2),chapter.facility,chapter.objective,"campaign");objectives.Add(objective);}
            var vault=FourCityWorld.Instance.Find("erebos-vault");
            for(int n=0;n<3;n++){var p=SyncPoint(n);var g=new CityGeometry(vault.transform);g.Cylinder(p,1.1f,.7f,"FutureCarbon",24);g.Cylinder(p+Vector3.up*.7f,.45f,2,"NovaNeonViolet",16);g.Ring(p+Vector3.up*2.1f,1,1,.06f,"NeonCyan",32);g.Sign("SYNC "+(n+1)+" / E",p+new Vector3(0,3,-1),.16f);g.Finish();VenueService.Add(vault.transform,p+new Vector3(0,1,-2),18,"동기화 코어 "+(n+1),"sync",n);}
            SetDestination();gameObject.AddComponent<ErebosPopulation>();
        }
        static Vector3 SyncPoint(int n)=>n==0?new(-9,.1f,-10):n==1?new(9,.1f,-10):new(0,.1f,20);
        public void Synchronize(int core)
        {
            if(core<0||core>2)return;var g=GameDirector.Instance;if(Current==null||!Current.sync){g.Toast("기록성당 조사 단계에서 동기화할 수 있습니다.");return;}
            var vault=FourCityWorld.Instance.Find("erebos-vault");if(Vector3.Distance(g.Player.transform.position,vault.transform.TransformPoint(SyncPoint(core)))>5)return;
            if(Remaining>0){g.Toast("방어체를 먼저 제압하세요.");return;}synchronized|=1<<core;g.Toast("코어 "+(core+1)+" 동기화 완료"+(synchronized==7?" · 중앙 기록 단말을 조사하세요.":" · 나머지 코어를 조사하세요."));
        }
        void SetDestination()
        {if(Complete)return;var c=Current;var venue=FourCityWorld.Instance.Find(FourCityCatalog.Venues[c.facility].id);Destination=venue.transform.TransformPoint(new Vector3(3,c.floor*venue.floorHeight+.1f,5));}
        public void Interact(int facility)
        {
            var game=GameDirector.Instance;if(Complete){game.ShowDialogue("네 도시의 기록","복원 전시가 공개되었습니다. 도시 곳곳의 잔향과 시민 이야기는 계속됩니다.");return;}
            if(facility!=Current.facility){game.ShowDialogue("연계 기록",Current.title+"\n먼저 "+FourCityCatalog.Venues[Current.facility].title+"에서 다음 기록을 확인하세요.\n"+Current.objective);return;}
            if(Vector3.Distance(game.Player.transform.position,Destination)>7){game.ShowDialogue(Current.title,Current.objective+"\n\n지정된 층의 기록 단말을 조사하세요. 입구와 승강기 안내를 이용할 수 있습니다.");return;}
            if(Current.enemies>0&&encounterStep!=Step)SpawnEncounter();
            if(Remaining>0){game.Toast("기록 단말 접근 차단 · 남은 위협 "+Remaining+"명",5);return;}
            if(Current.sync&&synchronized!=7){game.Toast("기록성당의 SYNC 1·2·3 코어를 E로 동기화한 뒤 중앙 단말을 조사하세요.",7);return;}
            var chapter=Current;
            string line=chapter.line;if(!string.IsNullOrEmpty(chapter.resultA)||!string.IsNullOrEmpty(chapter.resultB))line=Choice==1?Or(chapter.resultA,chapter.line):Or(chapter.resultB,chapter.line);
            CityCinematic.Play(chapter.title,chapter.speaker,line,FourCityCatalog.Venues[facility].position,Destination,()=>
            {
                if(chapter.decision)CityLife.Instance.StoryDialogue(chapter.title,chapter.speaker,chapter.prompt,true,choice=>{Choice=choice;Advance();},chapter.choiceA,chapter.choiceB);
                else Advance();
            });
        }
        void SpawnEncounter()
        {
            encounterStep=Step;encounter.Clear();var c=Current;var v=FourCityWorld.Instance.Find(FourCityCatalog.Venues[c.facility].id);
            for(int i=0;i<c.enemies;i++){var local=new Vector3((i%2==0?-1:1)*(8+i%3*3),c.floor*v.floorHeight+.12f,-18+(i/2)*6);var actor=ErebosThreat.Spawn(v.transform.TransformPoint(local),i==0&&c.elite,70000+Step*30+i,v.transform);encounter.Add(actor);}
        }
        void Advance()
        {
            int completed=Step;Step++;if(!LifeState.SuppressSave){PlayerPrefs.SetInt("AFTERSIGNAL.Unity.FourCities.Chapter",Step);PlayerPrefs.SetInt("AFTERSIGNAL.Unity.FourCities.Choice",Choice);PlayerPrefs.Save();}
            LifeState.Earn(250+completed*45);GameDirector.Instance.Toast("기록 복원 · "+(250+completed*45)+" C\n"+(Complete?"네 도시의 증언을 모두 확보했습니다.":Current.title),7);SetDestination();
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||!game.Ready||Complete||!FourCityWorld.Instance||!FourCityWorld.Instance.Built||Time.time<next)return;next=Time.time+.5f;
            if(Current.enemies>0&&encounterStep!=Step&&Vector3.Distance(game.Player.transform.position,Destination)<38)SpawnEncounter();
        }
        public void Journal()
        {CityLife.Instance.FourCityJournal();}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
