using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    public sealed partial class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }
        public static bool SkipTitle;
        public StageId stage;
        public GameTuning tuning;
        public Vector3 spawn,checkpoint;
        public float stageLength=58,halfDepth=3;
        public PlayerMotor Player { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public SignalAudio Audio { get; private set; }
        public SignalHud Hud { get; private set; }
        public readonly List<EnemyBrain> Enemies=new List<EnemyBrain>();
        public readonly List<BreakableGlass> Glass=new List<BreakableGlass>();
        public EnemyBrain Boss { get; private set; }
        public InteractionPoint Nearby { get; private set; }
        public bool Power,Release,BrokenGlass;
        public bool Title { get; private set; }
        public bool Paused { get; private set; }
        public bool Dead { get; private set; }
        public bool Transition { get; private set; }
        public bool Dialogue => !string.IsNullOrEmpty(DialogueText);
        public bool Blocked => Title||Paused||Dead||Transition||Dialogue||CityCinematic.Active;
        public bool Cleared => LivingGuards==0;
        public int LivingGuards { get {int n=0;foreach(var e in Enemies)if(e&&e.Alive&&!e.boss)n++;return n;} }
        public int Kills { get; private set; }
        public int Memories;
        public float Arrival { get; private set; }
        public float TravelDistance { get; private set; }
        public float Speed { get; private set; }
        public string Notice { get; private set; }
        public string DialogueTitle { get; private set; }
        public string DialogueText { get; private set; }
        public float NoticeTimer { get; private set; }
        public float Elapsed { get; private set; }
        public int Capacitors { get; private set; }
        public float ExposeTimer { get; private set; }
        public float WaveWarning { get; private set; }
        public float SafeX { get; private set; }=52;
        public float Fade { get; private set; }
        public int CoreStrikes { get; private set; }
        public bool Ready { get; private set; }
        float waveClock=4.5f,coreCooldown,inputSuppress,prePauseScale=1;
        int waveIndex;
        InteractionPoint[] interactions;
        readonly InteractionScanner interactionScanner=new();
        void Awake(){Instance=this;Time.timeScale=1;FramePacing.Apply();Physics.IgnoreLayerCollision(8,9,false);Physics.IgnoreLayerCollision(9,9,true);}
        void Start()
        {
            ExpansionWorld.Install(this);
            if(!tuning)tuning=Resources.Load<GameTuning>("GameTuning");
            spawn=CivicWorld.Spawn(stage,spawn);spawn=CivicWorld.SafeSpawn(stage,spawn);checkpoint=spawn;
            Player=FindAnyObjectByType<PlayerMotor>();Player.Initialize(this);Player.Respawn(spawn);
            Input=gameObject.AddComponent<PlayerInputReader>();
            CameraRig=Camera.main.GetComponent<CameraRig>();CameraRig.director=this;CameraRig.Snap();
            Audio=gameObject.AddComponent<SignalAudio>();Audio.Initialize();
            PresentationSettings.Load();
            gameObject.AddComponent<FidelityPresentation>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-quality-effects-off")>=0){PresentationSettings.Effects=0;PresentationSettings.Motion=0;}
            foreach(var enemy in FindObjectsByType<EnemyBrain>()){Enemies.Add(enemy);enemy.Initialize(this);if(enemy.boss)Boss=enemy;}
            Glass.AddRange(FindObjectsByType<BreakableGlass>());
            gameObject.AddComponent<CityLife>().Initialize(this);
            gameObject.AddComponent<CityChronicle>();gameObject.AddComponent<CitySocial>();
            interactions=FindObjectsByType<InteractionPoint>();
            Hud=gameObject.AddComponent<SignalHud>();Hud.Initialize(this);
            bool pacingProbe=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-quality-probe")>=0;
            Title=!SkipTitle&&!pacingProbe;SkipTitle=true;
            if(Title)Time.timeScale=0;
            gameObject.AddComponent<TitleScreen>().Initialize(this);
            Speed=stage==StageId.Carriage?22:stage==StageId.Roof?34:0;
            Memories=PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Memories",0);
            if(stage==StageId.Haven)Toast(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed",0)>0?"애프터라이트 · 노아와 이웃들의 소식을 확인하세요":"애프터라이트 · 북쪽 가로의 본부로 향하세요",6);
            var district=CampaignCatalog.Get(stage);if(district!=null){ShowDialogue(district.title,district.brief);gameObject.AddComponent<DistrictCheckpoint>();}
            gameObject.AddComponent<SceneLightBudget>();
            Ready=true;
            if(CivicWorld.Interior(stage)){gameObject.AddComponent<InteriorRefinement>();if(!GetComponent<PrisonSystem>())gameObject.AddComponent<PrisonSystem>();}
            CityLife.Instance.InstallErrandMarker();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-life-smoke")>=0)gameObject.AddComponent<CityLifeSmoke>();
            if(pacingProbe)gameObject.AddComponent<PacingProbe>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-quality-slice")>=0||System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-quality-campaign")>=0)gameObject.AddComponent<QualitySession>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-aftersignal-smoke")>=0)gameObject.AddComponent<RuntimeSmoke>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-expansion-smoke")>=0)gameObject.AddComponent<ExpansionSmoke>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-city-upgrade-smoke")>=0)gameObject.AddComponent<CityUpgradeSmoke>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-urban-manual")>=0)gameObject.AddComponent<UrbanManualProbe>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-urban-smoke")>=0)gameObject.AddComponent<UrbanSmoke>();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-residence-smoke")>=0)gameObject.AddComponent<ResidenceSmoke>();
        }
        void Update()
        {
            if(!Ready)return;
            var control=Input.Read();
            if(control.journal&&(!Blocked||CityLife.Instance.Mode=="journal")){if(CityLife.Instance.Mode=="journal")CityLife.Instance.Dismiss();else CityLife.Instance.StoryJournal();return;}
            if(control.pause&&!Title&&!Dead&&!Transition&&!CityCinematic.Active){if(CityLife.Instance&&CityLife.Instance.Mode=="sleeping")return;if(Dialogue)CloseDialogue();else SetPaused(!Paused);}
            if(Dialogue&&control.interact&&!(CityLife.Instance&&CityLife.Instance.Mode.Length>0)){CloseDialogue();return;}
            if(Blocked){Audio.SetPaused(Paused||Dead);return;}
            float dt=Mathf.Min(Time.deltaTime,.1f);Audio.SetPaused(false);inputSuppress-=dt;
            if(inputSuppress>0){control.attack=control.grapple=control.interact=false;}
            Elapsed+=dt;NoticeTimer=Mathf.Max(0,NoticeTimer-dt);coreCooldown-=dt;
            VenueRide.BeforeInput(ref control);
            if(UrbanSimulation.Instance&&UrbanSimulation.Instance.enabled)UrbanSimulation.Instance.BeforeInput(ref control,dt);
            CityLife.Instance?.BeforeInput(ref control,dt);
            PrisonSystem.BeforeInput(ref control,dt);
            CameraRig.ReadLook(Input.Frame);
            float remaining=dt;
            while(remaining>.00001f){float step=Mathf.Min(.02f,remaining);if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Driving)UrbanSimulation.Instance.Tick(control,step);else if(!VenueRide.Riding&&!(CityBusService.Instance&&CityBusService.Instance.Riding))Player.Tick(control,step);foreach(var enemy in Enemies)if(enemy&&enemy.gameObject.activeSelf)enemy.Tick(step);control.jump=control.dash=control.skill=control.reload=false;control.weaponCycle=0;control.weapon=-1;remaining-=step;}
            if(stage==StageId.Station&&CampaignRules.CanBoard(Power,Cleared))Arrival=Mathf.MoveTowards(Arrival,1,dt/4f);
            if(stage==StageId.Carriage||stage==StageId.Roof){Speed=Mathf.MoveTowards(Speed,stage==StageId.Roof?34:27,dt*2);TravelDistance+=Speed*dt;}
            UpdateBoss(dt);
            Nearby=interactionScanner.Nearest(Player);
            if(control.interact&&Nearby&&!(UrbanSimulation.Instance&&UrbanSimulation.Instance.Driving))Nearby.Interact(this);
        }
        public string Objective
        {
            get {
                if(stage==StageId.UrbanCity)return "M 도시 지도 · E 차량 탑승 / 건물 출입 · 주유소에서 연료 보충";
                if(stage==StageId.UrbanInterior&&ResidentialWorld.VisitHome>=0)return ResidentialWorld.VisitTitle+" · E / ESC";
                if(stage==StageId.UrbanInterior)return UrbanCatalog.Name(UrbanCatalog.Current)+" · 직원과 대화 / 출입문으로 돌아가기";
                if(stage==StageId.Residence)return Player.transform.position.y>18?"내 방을 둘러보고 현관문 열기 · E / 승강기 또는 비상계단으로 1층":"1층 출입구에서 E · 애프터라이트로 외출";
                if(stage==StageId.School)return "학교 도서실과 교실 탐색 · 교사와 대화 / 출입구 E";
                if(stage==StageId.Clinic)return "접수와 진료실 · 의료진에게 치료받기 / 출입구 E";
                if(stage==StageId.Headquarters)return "작전 단말 · 중앙역 조사 / 외벽 기록 회수 작전";
                if(stage==StageId.Haven&&PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed",0)==0)return "북쪽 본부의 작전 단말에서 중앙역 조사 시작 · W로 북쪽 가로 이동";
                if(stage==StageId.Station)return !Power?"상층 단말에서 승강장 전력 복구":!Cleared?$"승강장 경비병 제압 · {LivingGuards}명":Arrival<1?"유령 열차 진입 중 · 승차 지점으로 이동":"열린 문 앞에서 E · 열차에 탑승";
                if(stage==StageId.Carriage)return !BrokenGlass?"경비병을 뚫고 유리 격벽 파괴":!Release?"객실 동쪽의 수동 잠금 장치 작동":!Cleared?$"남은 경비병 제압 · {LivingGuards}명":"사다리 앞에서 E · 열차 지붕으로";
                if(stage==StageId.Roof)return Boss&&Boss.Alive?(Player.transform.position.x<46?"로프로 객차 사이를 건너 컨덕터에게 접근":ExposeTimer>0?"코어 노출 · 가까이서 왼쪽 클릭으로 검격":"양쪽 축전기를 오른쪽 클릭 · 로프로 과부하"):"동쪽의 기억 코어 회수 · E";
                var district=CampaignCatalog.Get(stage);if(district!=null)return !Power?"중계기 복구 · 상층 통로와 앵커를 따라 이동":!Cleared?$"구역 경비병 제압 · {LivingGuards}명":district.glass&&!BrokenGlass?"상층 유리 격벽을 파괴하세요":"출구 앞에서 E · "+(district.Finale?"애프터라이트로 귀환":"다음 구역으로 이동");
                return CampaignCatalog.NextChapter>0?"노아의 메인 의뢰 · 광장 아래 노선 단말":"도시 노선 복구 완료 · 주민 의뢰와 선착장 탐색";
            }
        }
        public void SetPaused(bool value){if(value==Paused)return;if(value){prePauseScale=Time.timeScale;Time.timeScale=0;}else Time.timeScale=prePauseScale;Paused=value;inputSuppress=.16f;Player.Rope.Release();}
        public void ShowDialogue(string title,string text){DialogueTitle=title;DialogueText=text;Player.Rope.Release();}
        public void CloseDialogue(){CityLife.Instance?.Close();DialogueText=null;inputSuppress=.18f;if(Audio)Audio.Play("ui_cancel",Player.Shoulder,.12f,1);}
        public void Toast(string text,float duration=3.2f){Notice=text;NoticeTimer=duration;}
        public void DamageNumber(Vector3 position,int amount,bool critical){if(Hud)Hud.AddDamage(position,amount,critical);}
        public void EnemyDied(EnemyBrain enemy){Kills++;LifeState.Earn(enemy.boss?350:25);Player.Heal(enemy.boss?30:3);if(enemy.boss){Toast("컨덕터 정지 · 기억 코어를 회수하세요",6);ExposeTimer=WaveWarning=0;}else if(Cleared)Toast("구역 확보 · 다음 목표로 이동하세요");}
        public void GlassBroken(){BrokenGlass=true;Toast("유리 격벽 파괴 · 다음 객실로 진입하세요");}
        public void Die(){if(PrisonSystem.Capture(this))return;WantedSystem.Clear("");Dead=true;Player.Rope.Release();Time.timeScale=0;}
        public void Retry(){Time.timeScale=1;SkipTitle=true;SceneManager.LoadScene(CampaignRules.Scene(stage));}
        public void Restart(){Time.timeScale=1;SkipTitle=false;PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Stage",0);PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Memories",0);ResetExpansion();SceneManager.LoadScene(CampaignRules.Scene(StageId.Station));}
        static void ResetExpansion(){if(CityChronicle.Instance)CityChronicle.Instance.ResetProgress();else PlayerPrefs.DeleteKey(CityChronicle.SaveKey);foreach(string key in new[]{"Chapters","Accepted","Jobs"})PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.Expansion."+key);}
        public void Travel(StageId next){if(Transition)return;StartCoroutine(TravelRoutine(next));}
        IEnumerator TravelRoutine(StageId next)
        {
            Transition=true;Player.Rope.Release();UrbanSimulation.Instance?.SaveCar();LifeState.Save();
            for(float t=0;t<.8f;t+=Time.unscaledDeltaTime){Fade=t/.8f;yield return null;}
            PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Stage",(int)next);PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Memories",Memories);PlayerPrefs.Save();
            SceneManager.LoadScene(CampaignRules.Scene(next));
        }
        public void OnAnchor(GrappleAnchor anchor)
        {
            if(anchor.capacitor<0||!Boss||!Boss.Alive||ExposeTimer>0)return;
            int mask=1<<anchor.capacitor;if((Capacitors&mask)!=0)return;
            Capacitors|=mask;anchor.Charged=true;Toast("축전기 과부하 · "+(Capacitors==3?"코어 노출! 검으로 공격하세요":"다른 축전기에 로프를 연결하세요"));
            if(Capacitors==3){ExposeTimer=9;Boss.SetExposed(9);WaveWarning=0;CameraRig.Kick(.2f);SignalEffects.Ring(Boss.transform.position+Vector3.up*2,SignalEffects.Gold,3,.7f);}
        }
        void ResetCapacitors(){Capacitors=0;foreach(var a in GrappleAnchor.All)if(a)a.Charged=false;}
        public void TryCoreStrike(PlayerMotor player,float range)
        {
            if(!Boss||!Boss.Alive||ExposeTimer<=0||coreCooldown>0)return;
            var d=Boss.transform.position-player.transform.position;
            if(Mathf.Abs(d.x)>range+1||Mathf.Abs(d.z)>1.8f||Mathf.Abs(d.y)>3)return;
            Boss.Damage(tuning.bossCoreDamage,Vector3.zero,true);CoreStrikes++;coreCooldown=1;
            ExposeTimer=0;ResetCapacitors();Boss.SetExposed(0);waveClock=3.5f;CameraRig.Kick(.28f);
            Toast(Boss.Alive?"코어 직격! · 축전기가 재가동됩니다":"컨덕터 무력화 · 기억 코어 회수",4);
        }
        void UpdateBoss(float dt)
        {
            if(!Boss||!Boss.Alive||!Boss.Active)return;
            if(ExposeTimer>0){ExposeTimer-=dt;if(ExposeTimer<=0){ResetCapacitors();waveClock=2.5f;}return;}
            if(WaveWarning>0){
                WaveWarning-=dt;Boss.SetTelegraph(WaveWarning);
                if(WaveWarning<=0){
                    SignalEffects.Beam(new Vector3(46,.35f,0),new Vector3(73,.35f,0),SignalEffects.Red,.45f,.4f);
                    if(Player.transform.position.x>45&&Player.transform.position.y<1.6f&&Mathf.Abs(Player.transform.position.x-SafeX)>2.6f)Player.ReceiveDamage(22,Boss.transform.position);
                    waveClock=4.4f;
                }
            }else{
                waveClock-=dt;if(waveClock<=0){WaveWarning=1.45f;waveIndex++;SafeX=waveIndex%2==0?52:68;Toast("레일 충격파 · 점프 / 로프 또는 초록 안전 구역",1.45f);}
            }
        }
        void OnApplicationFocus(bool focus){if(!focus&&Ready&&!Blocked&&!(Input&&Input.ExternalControl))SetPaused(true);}
        void OnDestroy(){if(Instance==this)Instance=null;Time.timeScale=1;}
    }
}
