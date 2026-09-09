using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class FourCityCampaign:MonoBehaviour
    {
        public sealed class Chapter
        {public string title,speaker,line,objective;public int facility,floor,enemies;public Chapter(string t,int v,int f,int e,string s,string l,string o){title=t;facility=v;floor=f;enemies=e;speaker=s;line=l;objective=o;}}
        public static readonly Chapter[] Chapters={
            new("01 · 전시되지 않은 기억",9,0,0,"아린 · 기억 큐레이터","도시는 복구됐지만 열일곱 분의 원본은 전시할 수 없었어요. 바다 아래에서 똑같은 신호가 다시 도착했습니다. 이 조각의 발신지는 에레보스예요.","박물관 내부 비공개 기록 단말 조사"),
            new("02 · 유리 아래의 증언",27,1,0,"리안 · 네레이드 기록관","네레이드의 호흡 돔은 피난 시설로 만들어졌어요. 지상의 기억이 잠식에 휩쓸릴 때, 연구진은 사람들의 증언을 이곳으로 옮겼죠. 이 증언에는 당신 이름도 남아 있습니다.","블루 아카이브 2층에서 원본 증언 확보"),
            new("03 · 격리선 너머",15,0,0,"유건 · 격리대장","안쪽의 사람처럼 보이는 것은 잔향입니다. 감염된 기억을 반복하죠. 무조건 없애기 전에 방송국의 중계기를 꺼 주세요. 구조 신호를 따라 들어간 대원들이 돌아오지 않았습니다.","에레보스 격리 관문에서 진입 허가 수령"),
            new("04 · 반복되는 마지막 방송",16,1,6,"노아 · 무전","같은 구조 요청이 열일곱 분마다 반복되고 있어. 생방송이 아니야. 송출실 안의 잠식체를 제압하고 백업 콘솔을 열어 줘.","방송국 2층 잠식체 제압 후 송출 기록 회수"),
            new("05 · 병실의 이름들",17,1,8,"서하","이 병실 번호… 삭제된 시민 명부와 같아. 환자들이 먼저 감염된 게 아니었어. 실험을 감추려고 진료 기록부터 지운 거야.","기억병원 2층 기록실 조사 및 위협 제거"),
            new("06 · 떠 있는 무게",18,0,10,"노아 · 무전","중력장이 기억 보관 장치를 중심으로 뒤집히고 있어. 바깥 잔해는 그대로 두고 성당 내부의 세 동기화 코어를 차례로 조사해.","기록성당 내부 방어체 제압 · 코어 동기화"),
            new("07 · 심해의 설계자",22,2,0,"이솔 · 심해 책임연구원","잠식은 외부에서 온 괴물이 아닙니다. 강제로 합쳐진 시민의 기억이 자아를 만들었어요. 완전히 지우면 잃어버린 사람들의 증언도 사라집니다. 원본을 분리해 주세요.","해저 연구원 3층에서 분리 프로토콜 확보"),
            new("08 · 돌려받을 도시",16,2,9,"서하","방송을 다시 켤 거야. 구조 요청을 반복하는 대신, 사람들의 이름을 돌려줄 수 있게. 이번에는 기록을 숨기지 않겠어.","방송국 3층에서 분리 신호 재송출"),
            new("09 · 영점의 문지기",19,0,12,"유건 · 무전","코어 앞에서 대형 반응이 움직인다. 화력을 집중하되 시설의 비상 통로를 확보해. 민간 구조대가 뒤에서 대기 중이다.","공명로 내부 잠식체와 문지기 격파"),
            new("10 · 기억이 선택한 목소리",19,2,8,"합성된 목소리","우리는 버려진 기억이다. 도시가 잊으라고 했던 이름들이다. 우리를 끄면 조용해지겠지. 하지만 조용한 것이 회복인가? 서하, 네가 결정해.","공명로 3층에 도달하여 기억 처리 방침 선택"),
            new("11 · 네 도시의 증인",23,1,0,"리안 · 시민 포럼","지상과 심해가 공동으로 기록을 관리하기로 했어요. 에레보스에는 아직 잔향이 남았지만, 누가 살았는지 이야기할 수 있게 됐습니다. 네 도시 모두가 증인이에요.","심해 시민 포럼 2층에 결과 전달"),
            new("12 · 돌아온 열일곱 분",9,1,0,"서하","비어 있던 전시실이 채워졌다. 내가 기억하지 못하는 시간에도 누군가는 살아가고 있었어. 끝난 사건이 아니라, 이제 함께 기억해야 할 이야기야.","기억의 파도 박물관 2층에서 복원 전시 공개")
        };
        public static FourCityCampaign Instance{get;private set;}
        public int Step{get;private set;}
        public int Choice{get;private set;}
        public int Kills{get;private set;}
        public bool Complete=>Step>=Chapters.Length;
        public Chapter Current=>Complete?null:Chapters[Step];
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
            if(core<0||core>2)return;var g=GameDirector.Instance;if(Step!=5){g.Toast("기록성당 조사 단계에서 동기화할 수 있습니다.");return;}
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
            if(Step==5&&synchronized!=7){game.Toast("기록성당의 SYNC 1·2·3 코어를 E로 동기화한 뒤 중앙 단말을 조사하세요.",7);return;}
            var chapter=Current;
            string line=chapter.line;if(Step==10)line=Choice==1?"네 도시의 시민 원본을 분리해 공동 보존했습니다. 누구나 자신의 기록을 열람하고 잘못된 기록의 정정을 요청할 수 있어요. 에레보스 복구 위원회에도 주민 대표가 참여합니다.":"기억의 소유권을 시민에게 돌려주었습니다. 동의한 기록만 보호 저장하고, 공개를 원하지 않는 증언은 봉인했습니다. 이제 누구도 다른 사람의 기억을 마음대로 이용할 수 없어요.";
            CityCinematic.Play(chapter.title,chapter.speaker,line,FourCityCatalog.Venues[facility].position,Destination,()=>
            {
                if(Step==9)CityLife.Instance.StoryDialogue(chapter.title,chapter.speaker,"시민 원본을 분리하여 공동 보존하거나, 각 당사자의 동의를 받아 보호 저장할 수 있습니다.",true,choice=>{Choice=choice;Advance();});
                else Advance();
            });
        }
        void SpawnEncounter()
        {
            encounterStep=Step;encounter.Clear();var c=Current;var v=FourCityWorld.Instance.Find(FourCityCatalog.Venues[c.facility].id);
            for(int i=0;i<c.enemies;i++){var local=new Vector3((i%2==0?-1:1)*(8+i%3*3),c.floor*v.floorHeight+.12f,-18+(i/2)*6);var actor=ErebosThreat.Spawn(v.transform.TransformPoint(local),i==0&&Step>=8,70000+Step*30+i,v.transform);encounter.Add(actor);}
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
