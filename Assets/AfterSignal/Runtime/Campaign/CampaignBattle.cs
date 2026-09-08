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
        public int Remaining=>enemies.FindAll(a=>a&&a.Alive).Count;
        public bool Completed {get;private set;}
        public string Objective {get;private set;}
        CityChronicle chronicle;StoryStep step;string quest;int expected;GameDirector game;
        readonly List<WorldActor> enemies=new(),devices=new();readonly List<GameObject> props=new();
        readonly PursuitPath escortPath=new();
        CityNpc survivor;Vector3 extraction;Text goalLabel,radio;float age,nextWave=4,held,speechUntil,failedUntil,nextPulse;bool finishing;
        public static CampaignBattle Begin(CityChronicle owner,StoryStep step)
        {
            if(current)return current;
            var c=new GameObject("MAIN / active combat operation").AddComponent<CampaignBattle>();current=c;c.chronicle=owner;c.step=step;c.quest=owner.Tracked.id;c.expected=owner.Entry(owner.Tracked).step;c.game=GameDirector.Instance;c.Build();return c;
        }
        void Build()
        {
            transform.position=step.position;extraction=step.position+new Vector3(-24,0,-18);
            var canvas=new GameObject("Combat objective and radio",typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=210;
            var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);
            Text Label(string name,Vector2 min,Vector2 max,int size)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(canvas.transform,false);go.GetComponent<Image>().color=new Color(.012f,.035f,.045f,.86f);go.GetComponent<Image>().raycastTarget=false;var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
                var text=new GameObject("Text",typeof(RectTransform),typeof(Text));text.transform.SetParent(go.transform,false);var t=text.GetComponent<Text>();t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.fontSize=size;t.color=new Color(.86f,.96f,.97f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=new Vector2(16,8);t.rectTransform.offsetMax=new Vector2(-16,-8);return t;
            }
            goalLabel=Label("Live mission",new(.28f,.69f),new(.72f,.84f),20);radio=Label("Live radio",new(.23f,.19f),new(.76f,.34f),24);
            Say((step.person??"노아")+": "+step.line);
            // Small cover objects give each forecourt a readable combat space without sealing roads.
            for(int i=0;i<5;i++)
            {
                var at=step.position+new Vector3((i-2)*8,0,i%2==0?12:-11);
                if(!CityGangWar.FindGround(at,out var floor))continue;
                var root=new GameObject("Mission mobile cover");root.transform.position=floor;props.Add(root);
                CommunityWorld.Box(root.transform,"Armoured barricade",new Vector3(0,.65f,0),new Vector3(3,1.3f,1.05f),"DarkMetal");
                CommunityWorld.Box(root.transform,"Warning trim",new Vector3(0,1.24f,0),new Vector3(2.8f,.06f,1.1f),"Metal",false);
            }
            if(step.battleMode=="sabotage")for(int i=0;i<3;i++)
            {
                var go=new GameObject("N-17 기억 소거 증폭기");go.transform.position=step.position+new Vector3((i-1)*9,0,6);props.Add(go);
                var body=go.AddComponent<WorldActor>();body.gang=true;body.helicopter=true;body.health=180;devices.Add(body);
                var box=CommunityWorld.Box(go.transform,"Power housing",Vector3.up,new Vector3(1.5f,2,1.1f),"Metal");
                CommunityWorld.Box(go.transform,"Exposed coupler",new Vector3(0,1.4f,-.61f),new Vector3(.65f,.5f,.12f),"NeonAzure",false);
            }
            if(step.battleMode=="rescue")
            {
                var go=new GameObject("구출 대상 / 기억 실험 생존자",typeof(SpriteRenderer),typeof(CityNpc));go.transform.position=step.position+Vector3.right*10;survivor=go.GetComponent<CityNpc>();survivor.fixedQuest=true;survivor.Configure(9800+expected,"구조 대상","실종 주민","저 사람들을 막아 줘요. 같이 빠져나가요!");PeopleArt.Attach(go,"CivilianWoman");props.Add(go);survivor.GetComponent<WorldActor>().health=200;
            }
            if(step.battleMode=="boss")
            {
                var at=step.position+Vector3.forward*38;if(CityGangWar.FindGround(at,out var floor)){var boss=RiftCreature.Create(floor,Mathf.Abs(quest.GetHashCode())%3);boss.Campaign=true;boss.Body.health=5200;enemies.Add(boss.Body);Wave=1;}
            }
            SignalEffects.Ring(extraction+Vector3.up*.13f,SignalEffects.Cyan,4,60);
        }
        void Say(string text){if(radio){radio.text=text;radio.transform.parent.gameObject.SetActive(true);speechUntil=age+Mathf.Clamp(text.Length*.12f,5,12);}}
        bool SpawnWave()
        {
            int count=Mathf.Clamp(step.enemyCount,3,8);int spawned=0;
            for(int i=0;i<count;i++)
            {
                Vector3 at=step.position+new Vector3(Mathf.Cos(i*2.4f+Wave)*27,0,Mathf.Sin(i*2.4f+Wave)*27);
                if(!CityGangWar.FindGround(at,out var safe)||Physics.CheckCapsule(safe+Vector3.up*.5f,safe+Vector3.up*1.5f,.5f,1,QueryTriggerInteraction.Ignore))continue;
                var enemy=GangMember.Create(safe,i%3,i);enemy.name=i%3==0?"N-17 기억 회수대 / 돌격병":i%3==1?"N-17 기억 회수대 / 산탄병":"N-17 기억 회수대 / 소총수";enemy.CampaignUnit=true;enemy.CampaignWeapon=i%3==1?1:0;enemy.Body.health=95+expected*18;enemies.Add(enemy.Body);spawned++;
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
            int waves=Mathf.Max(1,step.waves);
            if(step.battleMode!="boss"&&Remaining==0&&Wave<waves&&age>=nextWave){if(SpawnWave())nextWave=age+8;else nextWave=age+3;}
            bool defeated=Wave>=waves&&Remaining==0;
            string mode=step.battleMode;
            if(mode=="defend")
            {
                bool inside=Vector3.Distance(game.Player.transform.position,step.position)<22;
                if(inside)held+=dt;Objective=$"대피 통로 방어 · {held:0}/{step.duration:0}초\n{Wave}/{waves}차 공격 · 적 {Remaining}명";
                if(!inside)Objective+=" · 표시된 방어 구역으로 복귀";
                defeated&=held>=step.duration;
            }
            else if(mode=="sabotage")
            {int left=devices.FindAll(a=>a&&a.Alive).Count;Objective=$"소거 증폭기 파괴 · 남은 장치 {left}/3\n{Wave}/{waves}차 공격 · 적 {Remaining}명";defeated&=left==0;foreach(var device in devices)if(device&&!device.Alive&&device.gameObject.activeSelf){VehicleExplosion.Create(device.transform.position+Vector3.up,1.3f);device.gameObject.SetActive(false);}}
            else if(mode=="rescue")
            {
                Objective=$"주민 구출 · 적 {Remaining}명\n생존자에게 다가간 뒤 청록색 집결점까지 천천히 호위";
                if(defeated&&survivor)
                {
                    var p=survivor.transform.position;float distance=Vector3.Distance(p,game.Player.transform.position);
                    if(distance<16){var goal=game.Player.transform.position;if(Vector3.Distance(goal,extraction)<5)goal=extraction;var next=p+escortPath.Direction(p,goal)*Mathf.Min(dt*3.6f,Vector3.Distance(p,goal));if(CityGangWar.FindGround(next,out var ground)&&(ground-next).sqrMagnitude<.4f)survivor.transform.position=ground;survivor.GetComponent<DirectionalPerson>()?.Face(goal,.3f);}
                    defeated=Vector3.Distance(survivor.transform.position,extraction)<4;
                }
            }
            else if(mode=="escape"){Objective=$"추격대 격파 후 청록색 탈출 지점으로 이동\n남은 적 {Remaining}명 · 탈출 {Vector3.Distance(game.Player.transform.position,extraction):0} m";defeated&=Vector3.Distance(game.Player.transform.position,extraction)<5;}
            else if(mode=="boss")Objective=$"대형 잠식체 격파 · 발광 기관이 열릴 때 집중 사격\n범위 표시에서 이탈 · 레이저 예고선은 회피";
            else Objective=$"기억 회수대 돌파 · {Wave}/{waves}차 공격\n남은 적 {Remaining}명 · 엄폐물과 측면 통로 활용";
            goalLabel.text=chronicle.Tracked.title+"  /  "+(expected+1)+"단계\n"+Objective;
            if(defeated){finishing=true;game.Player.Heal(28);Say(step.outro??"노아: 현장 확보. 잘했어, 서하. 다음 목표로 가자.");}
        }
        void OnDestroy(){if(current==this)current=null;foreach(var a in enemies)if(a)Destroy(a.gameObject);foreach(var go in props)if(go)Destroy(go);}
    }
}
