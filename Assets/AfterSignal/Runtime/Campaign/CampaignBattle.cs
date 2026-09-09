using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class CampaignBattle:MonoBehaviour
    {
        public static bool Active=>current;
        static CampaignBattle current;
        public int Wave {get;private set;}
        public int Remaining {get{int count=0;foreach(var a in enemies)if(a&&a.Alive&&!a.Downed)count++;return count;}}
        public bool Completed {get;private set;}
        public string Objective {get;private set;}
        CityChronicle chronicle;StoryStep step;string quest;int expected;GameDirector game;
        bool Intro=>quest=="main01";
        public static bool TutorialActive=>current&&current.Intro&&!current.finishing&&!current.Completed;
        public static bool TutorialTarget(WorldActor actor)=>TutorialActive&&actor&&actor.gameObject.activeInHierarchy&&actor.Alive&&!actor.Downed&&!actor.environmental&&!actor.helicopter&&!actor.police&&!actor.military&&current.enemies.Contains(actor);
        public static void TutorialTargets(List<WorldActor> targets){targets.Clear();if(TutorialActive)foreach(var actor in current.enemies)if(TutorialTarget(actor))targets.Add(actor);}
        string Mode=>Intro&&step.battleMode=="sabotage"?"assault":step.battleMode;
        Image radioPortrait;
        readonly List<WorldActor> enemies=new(),devices=new();readonly List<GameObject> props=new();
        CityNpc survivor;Vector3 extraction;Text goalLabel,radio;float age,nextWave=4,held,speechUntil,failedUntil,nextPulse;bool finishing;
        public static CampaignBattle Begin(CityChronicle owner,StoryStep step)
        {
            if(current)return current;
            var c=new GameObject("MAIN / active combat operation").AddComponent<CampaignBattle>();current=c;c.chronicle=owner;c.step=step;c.quest=owner.Tracked.id;c.expected=owner.Entry(owner.Tracked).step;c.game=GameDirector.Instance;c.Build();return c;
        }
        void Build()
        {
            if(Intro)nextWave=16;
            transform.position=step.position;extraction=step.position+new Vector3(-24,0,-18);
            if(CrowdFlow.Place(extraction,expected,out var safeExtraction,20))extraction=safeExtraction;
            else if(CrowdFlow.Place(step.position,expected,out safeExtraction,25))extraction=safeExtraction;
            var canvas=new GameObject("Combat objective and radio",typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=210;
            var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);
            Text Label(string name,Vector2 min,Vector2 max,int size)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(canvas.transform,false);go.GetComponent<Image>().color=new Color(.012f,.035f,.045f,.86f);go.GetComponent<Image>().raycastTarget=false;var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
                var text=new GameObject("Text",typeof(RectTransform),typeof(Text));text.transform.SetParent(go.transform,false);var t=text.GetComponent<Text>();t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.fontSize=size;t.color=new Color(.86f,.96f,.97f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=new Vector2(16,8);t.rectTransform.offsetMax=new Vector2(-16,-8);return t;
            }
            goalLabel=Label("Live mission",new(.28f,.69f),new(.72f,.84f),20);radio=Label("Live radio",new(.23f,.19f),new(.76f,.34f),24);
            var portrait=new GameObject("Current radio speaker",typeof(RectTransform),typeof(Image));portrait.transform.SetParent(radio.transform.parent,false);radioPortrait=portrait.GetComponent<Image>();radioPortrait.preserveAspect=true;radioPortrait.raycastTarget=false;var pr=radioPortrait.rectTransform;pr.anchorMin=new Vector2(0,0);pr.anchorMax=new Vector2(0,1);pr.pivot=new Vector2(0,.5f);pr.offsetMin=new Vector2(5,4);pr.offsetMax=new Vector2(118,-4);radio.alignment=TextAnchor.MiddleLeft;radio.rectTransform.offsetMin=new Vector2(134,8);
            Say((step.person??"노아")+": "+step.line);
            // Small cover objects give each forecourt a readable combat space without sealing roads.
            for(int i=0;i<(Intro?0:5);i++)
            {
                var at=step.position+new Vector3((i-2)*8,0,i%2==0?12:-11);
                if(!CityGangWar.FindGround(at,out var floor))continue;
                var root=new GameObject("Mission mobile cover");root.transform.position=floor;props.Add(root);
                CommunityWorld.Box(root.transform,"Armoured barricade",new Vector3(0,.65f,0),new Vector3(3,1.3f,1.05f),"DarkMetal");
                CommunityWorld.Box(root.transform,"Warning trim",new Vector3(0,1.24f,0),new Vector3(2.8f,.06f,1.1f),"Metal",false);
            }
            if(Mode=="sabotage")for(int i=0;i<3;i++)
            {
                var desired=step.position+new Vector3((i-1)*9,0,6);if(!CrowdFlow.Place(desired,i,out var support,12))support=extraction+Vector3.right*(i-1)*3;
                var go=new GameObject("N-17 기억 소거 증폭기");go.transform.position=support;props.Add(go);
                var body=go.AddComponent<WorldActor>();body.gang=true;body.helicopter=true;body.health=Intro?75:180;devices.Add(body);
                var box=CommunityWorld.Box(go.transform,"Power housing",Vector3.up,new Vector3(1.5f,2,1.1f),"Metal");
                CommunityWorld.Box(go.transform,"Exposed coupler",new Vector3(0,1.4f,-.61f),new Vector3(.65f,.5f,.12f),"NeonAzure",false);
            }
            if(step.battleMode=="rescue")
            {
                var go=new GameObject("구출 대상 / 기억 실험 생존자",typeof(SpriteRenderer),typeof(CityNpc),typeof(EscortFollower));go.transform.position=CrowdFlow.Place(step.position+Vector3.right*10,expected,out var safeSurvivor,18)?safeSurvivor:extraction;
                go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");survivor=go.GetComponent<CityNpc>();survivor.fixedQuest=true;survivor.Configure(9800+expected,"구조 대상","실종 주민","저 사람들을 막아 줘요. 같이 빠져나가요!");PeopleArt.Attach(go,"CivilianWoman");props.Add(go);survivor.GetComponent<WorldActor>().health=200;
            }
            if(step.battleMode=="boss")
            {
                var at=step.position+Vector3.forward*38;if(!CrowdFlow.Place(at,expected,out var floor,30))floor=extraction;
                var boss=RiftCreature.Create(floor,Mathf.Abs(quest.GetHashCode()%3));boss.Campaign=true;boss.Body.health=5200;enemies.Add(boss.Body);Wave=1;
            }
            SignalEffects.Ring(extraction+Vector3.up*.13f,SignalEffects.Cyan,4,60);
        }
        void Say(string text){if(radio){radio.text=text;if(radioPortrait){radioPortrait.sprite=StoryPortraits.Get(text.Split(':')[0]);radioPortrait.enabled=radioPortrait.sprite;}radio.transform.parent.gameObject.SetActive(true);speechUntil=age+Mathf.Clamp(text.Length*.12f,5,12);}}
        bool SpawnWave()
        {
            int count=Intro?2:Mathf.Clamp(step.enemyCount,3,8);int spawned=0;
            for(int i=0;i<count;i++)
            {
                Vector3 at=step.position+new Vector3(Mathf.Cos(i*2.4f+Wave)*27,0,Mathf.Sin(i*2.4f+Wave)*27);
                if(quest.StartsWith("gang-main-"))at=step.position+(i<4?new Vector3((i-1.5f)*5,.1f,16):new Vector3(i%2==0?-18:18,.1f,5+i%3*5));
                if(!CrowdFlow.Place(at,i+Wave*11,out var safe,16))continue;
                var enemy=GangMember.Create(safe,i%3,i);enemy.name=i%3==0?"N-17 기억 회수대 / 돌격병":i%3==1?"N-17 기억 회수대 / 산탄병":"N-17 기억 회수대 / 소총수";enemy.CampaignUnit=true;enemy.CampaignWeapon=Intro?0:i%3==1?1:0;enemy.IntroUnit=Intro;enemy.Body.health=Intro?48:95+expected*18;enemies.Add(enemy.Body);spawned++;
                if(i==0)NpcSpeech.Say(enemy,"대상을 확보해! 증거를 남기지 마!",3,4);
            }
            if(spawned==0)return false;Wave++;Say(Wave==1?"서하: 들었어. 내가 길을 열게. 다들 내 뒤로 와.":"노아: 추가 병력이 진입한다. 엄폐하면서 양쪽을 확인해!");return true;
        }
        void Update()
        {
            if(!game||!chronicle||chronicle.Tracked?.id!=quest||chronicle.CurrentStep!=step||game.stage!=StageId.UrbanCity){Destroy(gameObject);return;}
            if(game.Blocked)return;float dt=Mathf.Min(.1f,Time.deltaTime);age+=dt;
            if(age>nextPulse&&(step.battleMode=="rescue"||step.battleMode=="escape")){nextPulse=age+4;SignalEffects.Ring(extraction+Vector3.up*.13f,SignalEffects.Cyan,4,5);}
            if(age>speechUntil&&radio)radio.transform.parent.gameObject.SetActive(false);
            if(finishing){if(age>speechUntil){Completed=true;chronicle.Advance(quest,expected);Destroy(gameObject);}return;}
            if(Vector3.Distance(game.Player.transform.position,step.position)>180){Say("노아: 작전 구역을 벗어났어. 현장으로 돌아오면 다시 진입하자.");Destroy(gameObject);return;}
            if(survivor&&!survivor.GetComponent<WorldActor>().Alive){Say("노아: 구조 대상이 쓰러졌다. 진입 지점에서 다시 시도하자.");failedUntil=age+7;survivor=null;}
            if(failedUntil>0){if(age>failedUntil)Destroy(gameObject);return;}
            int waves=step.battleMode=="boss"?1:Mathf.Max(1,step.waves);
            if(step.battleMode!="boss"&&Remaining==0&&Wave<waves&&age>=nextWave){if(SpawnWave())nextWave=age+8;else nextWave=age+3;}
            bool defeated=Wave>=waves&&Remaining==0;
            string mode=Mode;
            if(mode=="defend")
            {
                bool inside=Vector3.Distance(game.Player.transform.position,step.position)<22;
                if(inside)held+=dt;Objective=$"대피 통로 방어 · {held:0}/{step.duration:0}초\n{Wave}/{waves}차 공격 · 적 {Remaining}명";
                if(!inside)Objective+=" · 표시된 방어 구역으로 복귀";
                defeated&=held>=step.duration;
            }
            else if(mode=="sabotage")
            {int left=0;foreach(var device in devices)if(device&&device.Alive)left++;Objective=$"소거 증폭기 파괴 · 남은 장치 {left}/3\n{Wave}/{waves}차 공격 · 적 {Remaining}명";defeated&=left==0;foreach(var device in devices)if(device&&!device.Alive&&device.gameObject.activeSelf){VehicleExplosion.Create(device.transform.position+Vector3.up,1.3f);device.gameObject.SetActive(false);}}
            else if(mode=="rescue")
            {
                Objective=$"주민 구출 · 적 {Remaining}명\n생존자에게 다가간 뒤 청록색 집결점까지 천천히 호위";
                if(defeated&&survivor)
                {
                    var body=survivor.GetComponent<WorldActor>();
                    if(body.Downed){Objective+="\n부상자에게 접근해 응급 처치";if(Vector3.Distance(game.Player.transform.position,survivor.transform.position)<3){body.ResetHealth();body.health=200;NpcSpeech.Say(survivor,"고마워요. 이제 움직일 수 있어요!",4,5);}else defeated=false;}
                    if(!body.Downed)
                    survivor.GetComponent<EscortFollower>().Tick(game.Player,extraction,dt);
                    defeated=!body.Downed&&Vector3.Distance(survivor.transform.position,extraction)<4;
                }
            }
            else if(mode=="escape"){Objective=$"추격대 격파 후 청록색 탈출 지점으로 이동\n남은 적 {Remaining}명 · 탈출 {Vector3.Distance(game.Player.transform.position,extraction):0} m";defeated&=Vector3.Distance(game.Player.transform.position,extraction)<5;}
            else if(mode=="boss")Objective=$"대형 잠식체 격파 · 발광 기관이 열릴 때 집중 사격\n범위 표시에서 이탈 · 레이저 예고선은 회피";
            else Objective=$"기억 회수대 돌파 · {Wave}/{waves}차 공격\n남은 적 {Remaining}명 · 엄폐물과 측면 통로 활용";
            if(Intro&&Wave==0)Objective="전투 준비 · "+Mathf.CeilToInt(Mathf.Max(0,nextWave-age))+"초\n빨간 ▼가 적입니다 · 마우스 조준 / 좌클릭 공격 / 숫자키 무기 전환";
            goalLabel.text=chronicle.Tracked.title+"  /  "+(expected+1)+"단계\n"+Objective;
            if(defeated){finishing=true;game.Player.Heal(28);Say(step.outro??"노아: 현장 확보. 잘했어, 서하. 다음 목표로 가자.");}
        }
        void OnDestroy(){if(current==this)current=null;foreach(var a in enemies)if(a)Destroy(a.gameObject);foreach(var go in props)if(go)Destroy(go);}
    }
}
