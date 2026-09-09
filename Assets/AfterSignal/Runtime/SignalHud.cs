using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AfterSignal
{
    public sealed partial class SignalHud : MonoBehaviour
    {
        readonly Color ink=new Color(.035f,.07f,.095f,.94f),mint=new Color(.63f,.92f,.83f),muted=new Color(.48f,.64f,.66f),white=new Color(.88f,.94f,.92f);
        Canvas canvas;
        public void CinematicVisibility(bool visible){if(canvas)canvas.enabled=visible;}
        RectTransform root;
        Font font;
        GameDirector game;
        Text health,objective,area,weapon,notice,prompt,rail,bossText,anchorHint,modalTitle,modalBody,dialogueName,dialogueText;
        Image hpBar,energyBar,bossBar,fade;
        GameObject modal,dialogueBox,bossPanel;
        RectTransform cursor,candidate,safeMarker;
        Button primary,secondary,third,musicButton,sfxButton,homeButton,bikeButton,dialogueContinue;
        Button motionButton,effectButton,postButton,graphicsButton;GameObject settingsRow;
        Image hpTrail;float displayedHealth=100,nextText;
        static readonly string[] weaponNames={"01  ·  KATANA / 연속 검격","02  ·  GREATSWORD / 중검","03  ·  PISTOL / 조준 사격"};
        static readonly string[] stageNames={"01 / CENTRAL STATION","02 / THE NIGHT CARRIAGE","03 / ABOVE THE CITY","HUB / AFTERLIGHT"};
        Text menuFooter;string modalMode;int respawnCity;readonly Button[] respawnChoices=new Button[4];
        readonly List<(Text text,Vector3 world,float born)> numbers=new List<(Text,Vector3,float)>();
        readonly Dictionary<EnemyBrain,Text> warnings=new Dictionary<EnemyBrain,Text>();
        public void Initialize(GameDirector owner)
        {
            game=owner;font=Resources.Load<Font>("Fonts/NotoSansKR");
            var go=new GameObject("HUD · AFTERSIGNAL",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root=go.GetComponent<RectTransform>();
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            BuildReferenceHud();
            modal=Panel(root,"Modal shade",0,0,1600,900,new Color(.025f,.045f,.07f,.82f)).gameObject;Stretch(modal.GetComponent<RectTransform>());
            var card=Panel(modal.transform,"Menu",0,0,610,750,ink).rectTransform;Center(card,610,750);
            Label(card,"AFTERSIGNAL  /  NIGHT LINE",40,30,530,24,13,mint,FontStyle.Bold);
            Panel(card,"Accent",40,74,64,3,mint);
            modalTitle=Label(card,"",40,96,530,115,44,white,FontStyle.Bold);
            modalBody=Label(card,"",40,223,530,102,18,muted);
            for(int i=0;i<4;i++){int city=i;respawnChoices[i]=MakeButton(card,"",40+(i%2)*272,250+(i/2)*77,258,67,()=>{respawnCity=city;ConfigureModal("dead");});respawnChoices[i].name="Respawn city "+i;respawnChoices[i].GetComponentInChildren<Text>().fontSize=17;respawnChoices[i].gameObject.SetActive(false);}
            primary=MakeButton(card,"",40,350,530,58,()=>Primary());
            secondary=MakeButton(card,"",40,423,530,48,()=>Secondary());
            third=MakeButton(card,"",40,486,190,44,()=>Third());
            homeButton=MakeButton(card,"집으로 복귀",240,486,155,44,CashLocations.ReturnHome);
            bikeButton=MakeButton(card,"V · 바이크 호출",405,486,165,44,()=>{game.SetPaused(false);UrbanSimulation.RequestBike();});
            settingsRow=Panel(card,"Presentation options",40,541,530,42,Color.clear).gameObject;
            motionButton=MakeButton(settingsRow.transform,"",0,0,171,38,()=>{PresentationSettings.Motion=PresentationSettings.Motion>0?0:.65f;PresentationSettings.Save();ConfigureModal("pause");});
            effectButton=MakeButton(settingsRow.transform,"",180,0,171,38,()=>{PresentationSettings.Effects=PresentationSettings.Effects>0?0:.7f;PresentationSettings.Save();ConfigureModal("pause");});
            postButton=MakeButton(settingsRow.transform,"",360,0,170,38,()=>{PresentationSettings.Post=!PresentationSettings.Post;PresentationSettings.Save();ConfigureModal("pause");});
            foreach(var button in new[]{motionButton,effectButton,postButton})button.GetComponentInChildren<Text>().fontSize=14;
            musicButton=MakeButton(card,"",40,590,530,38,()=>{
                var music=SignalMusic.Instance;if(!music)return;
                float level=music.MusicLevel;
                music.SetMusicVolume(level<.01f?.3f:level<.31f?.55f:level<.56f?.8f:level<.81f?1:0);
                ConfigureModal("pause");
            });
            musicButton.GetComponentInChildren<Text>().fontSize=16;
            sfxButton=MakeButton(card,"",40,638,530,38,()=>{
                float level=game.Audio.SfxVolume;
                game.Audio.SetSfxVolume(level<.01f?.3f:level<.31f?.6f:level<.61f?.9f:level<.91f?1:0);
                PlayerPrefs.Save();ConfigureModal("pause");
            });
            sfxButton.GetComponentInChildren<Text>().fontSize=16;
            graphicsButton=MakeButton(card,"",40,683,530,32,()=>{FidelityPresentation.Cycle();ConfigureModal("pause");});graphicsButton.GetComponentInChildren<Text>().fontSize=15;
            menuFooter=Label(card,"ESC  메뉴 닫기     ·     배경음악과 화면 연출 조절",40,723,530,20,12,muted);
            dialogueBox=Panel(root,"Dialogue",0,0,1020,230,ink).gameObject;CenterBottom(dialogueBox.GetComponent<RectTransform>(),90,1020,230);
            dialogueName=Label(dialogueBox.transform,"",30,23,920,28,14,mint,FontStyle.Bold);
            dialogueText=Label(dialogueBox.transform,"",30,67,952,95,23,white);
            dialogueContinue=MakeButton(dialogueBox.transform,"대화 계속  [ E ]",754,177,230,35,()=>game.CloseDialogue());
            fade=Panel(root,"Transition",0,0,1600,900,Color.black);Stretch(fade.rectTransform);fade.raycastTarget=false;
            foreach(var e in game.Enemies){var txt=Label(root,"",0,0,160,42,17,SignalEffects.Red,FontStyle.Bold);txt.alignment=TextAnchor.MiddleCenter;warnings.Add(e,txt);}
        }
        void Update()
        {
            if(!game||!game.Ready)return;canvas.enabled=!game.Title&&!CityCinematic.Active&&!VenueRuntime.ViewingCinema;if(game.Title||CityCinematic.Active||VenueRuntime.ViewingCinema)return;UpdateCivicHud();
            displayedHealth=Mathf.MoveTowards(displayedHealth,game.Player.Health,Time.unscaledDeltaTime*30);hpTrail.rectTransform.sizeDelta=new Vector2(278*displayedHealth/100,9);
            hpBar.rectTransform.sizeDelta=new Vector2(278*game.Player.Health/100,9);energyBar.rectTransform.sizeDelta=new Vector2(174*game.Player.Energy/100,3);
            if(Time.unscaledTime>=nextText){nextText=Time.unscaledTime+.08f;UpdateReferenceHud();health.text=$"{Mathf.CeilToInt(game.Player.Health)} / 100";weapon.text=game.Player.EquippedName;
            area.text=(int)game.stage<4?stageNames[(int)game.stage]:CampaignCatalog.Title(game.stage);objective.text=game.Objective;if(game.stage==StageId.UrbanCity)area.text=FourCityCatalog.CityNames[FourCityCatalog.CityAt(game.Player.transform.position)];
            rail.text=game.Speed>0?$"NIGHT LINE    {game.Speed*3.6f:0} KM/H    ·    {game.LivingGuards} HOSTILES":$"ARCHIVE  {game.Memories:00}     /     {game.LivingGuards} HOSTILES";
            if(game.stage==StageId.Haven){int jobs=((CampaignCatalog.Jobs&4)!=0?1:0)+((CampaignCatalog.Jobs&16)!=0?1:0);rail.text=$"주민 의뢰  {jobs} / 2     ·     기록  {game.Memories:00}";}
            var district=CampaignCatalog.Get(game.stage);if(district!=null&&district.chapter>0){int first=district.chapter==5?22:district.chapter==2?4:district.chapter==3?11:14,count=district.chapter==5?1:district.chapter==2?7:3;area.text=$"CH {district.chapter:00}  ·  {(int)game.stage-first+1}/{count}  /  {district.title}";}
            }
            notice.text=game.NoticeTimer>0&&game.NoticeInRange&&!game.Blocked?game.Notice:"";
            notice.rectTransform.anchoredPosition=new Vector2(0,CampaignBattle.Active?-96:-200);
            prompt.text=!game.Blocked&&game.Nearby?$"[ E ]   {game.Nearby.title}":"";
            anchorHint.text=game.Blocked?"":game.Player.Rope.Attached?"로프 자동 감기 · W 빠르게 / S 풀기 · SPACE 도약 · 우클릭 해제":game.Player.Rope.Candidate?$"우클릭 · 갈고리 연결  {Vector3.Distance(game.Player.Shoulder,game.Player.Rope.Candidate.transform.position):0}m"+(game.Player.Rope.AimAssisted?" · 조준 보조":""):"";
            Cursor.visible=!game.CameraRig.CanLook;cursor.gameObject.SetActive(game.CameraRig.CanLook);
            PlaceScreen(cursor,new Vector2(Screen.width*.5f,Screen.height*.5f),new Vector2(15,15));
            var anchor=game.Player.Rope.Candidate;candidate.gameObject.SetActive(anchor&&!game.Blocked);
            if(anchor)PlaceWorld(candidate,anchor.transform.position,new Vector2(30,32));
            bool showBoss=game.Boss&&game.Boss.Alive&&game.Boss.Active;
            bossPanel.SetActive(showBoss&&!game.Blocked);
            if(showBoss){bossBar.rectTransform.sizeDelta=new Vector2(668*game.Boss.Health/game.Boss.maxHealth,6);
                bossText.text=game.ExposeTimer>0?$"CORE EXPOSED  ·  {game.ExposeTimer:0.0}s   /   접근 후 검 공격":game.WaveWarning>0?$"충격파  {game.WaveWarning:0.0}s   ·   점프 / 로프 / SAFE":"CONDUCTOR   ·   축전기  "+((game.Capacitors&1)>0?"●":"○")+"  "+((game.Capacitors&2)>0?"●":"○");}
            safeMarker.gameObject.SetActive(game.WaveWarning>0&&!game.Blocked);if(game.WaveWarning>0)PlaceWorld(safeMarker,new Vector3(game.SafeX,1.1f,0),new Vector2(65,36));
            foreach(var pair in warnings){bool visible=pair.Key&&pair.Key.Alive&&pair.Key.Telegraph>0&&!pair.Key.boss&&!game.Blocked;pair.Value.gameObject.SetActive(visible);if(visible){pair.Value.text=pair.Key.kind=="gunner"?"!  SHOT":"!  STRIKE";PlaceWorld(pair.Value.rectTransform,pair.Key.transform.position+Vector3.up*2.8f,new Vector2(80,32));}}
            for(int i=numbers.Count-1;i>=0;i--){var n=numbers[i];float age=Time.time-n.born;if(age>.85f){Destroy(n.text.gameObject);numbers.RemoveAt(i);}else{PlaceWorld(n.text.rectTransform,n.world+Vector3.up*age,new Vector2(40,20));}}
            string mode=game.Dead?"dead":game.Paused?"pause":"";
            modal.SetActive(mode!="");if(mode!=modalMode){if(mode=="dead")respawnCity=0;modalMode=mode;ConfigureModal(mode);}
            dialogueBox.SetActive(game.Dialogue);if(game.Dialogue){dialogueName.text=game.DialogueTitle;dialogueText.text=game.DialogueText;if(storyPortrait){storyPortrait.sprite=StoryPortraits.Bust(game.DialogueTitle);storyPortrait.gameObject.SetActive(storyPortrait.sprite);}}
            UpdateUrbanHud();UpdateQuestHud();UpdateLifeHud();UpdateClientExperience();
            UpdateNoaRadio();
            fade.gameObject.SetActive(game.Transition);fade.color=new Color(0,0,0,game.Fade);
        }
        void ConfigureModal(string mode)
        {
            if(mode=="")return;menuFooter.gameObject.SetActive(mode=="pause");
            musicButton.gameObject.SetActive(mode=="pause");
            homeButton.gameObject.SetActive(mode=="pause");bikeButton.gameObject.SetActive(mode=="pause");
            sfxButton.gameObject.SetActive(mode=="pause");
            graphicsButton.gameObject.SetActive(mode=="pause");ButtonText(graphicsButton,"그래픽: "+FidelityPresentation.PresetName+"  ·  클릭하여 변경");
            ButtonText(sfxButton,game.Audio.SfxVolume<.01f?"효과음: 꺼짐  ·  클릭하여 켜기":$"효과음: {game.Audio.SfxVolume*100:0}%  ·  클릭하여 조절");
            float musicLevel=SignalMusic.Instance?SignalMusic.Instance.MusicLevel:SignalMusic.DefaultLevel;
            ButtonText(musicButton,musicLevel<.01f?"배경음악: 꺼짐  ·  클릭하여 변경":$"배경음악: {musicLevel*100:0}%  ·  클릭하여 변경");
            settingsRow.SetActive(mode=="pause");ButtonText(motionButton,PresentationSettings.Motion>0?"화면 충격: 켜짐":"화면 충격: 꺼짐");ButtonText(effectButton,PresentationSettings.Effects>0?"전투 효과: 켜짐":"전투 효과: 꺼짐");ButtonText(postButton,PresentationSettings.Post?"후처리: 켜짐":"후처리: 꺼짐");
            for(int i=0;i<4;i++){respawnChoices[i].gameObject.SetActive(mode=="dead");ButtonText(respawnChoices[i],(i==respawnCity?"● ":"○ ")+FourCityCatalog.CityNames[i]+"\n"+(i==0?"서하의 집":i==1?"응급의료센터":i==2?"전진기지 의무실":"생명지원센터"));respawnChoices[i].GetComponent<Image>().color=i==respawnCity?new Color(.13f,.43f,.42f):new Color(.08f,.16f,.19f);}
            Rect(primary.GetComponent<RectTransform>(),40,mode=="dead"?424:350,530,58);Rect(secondary.GetComponent<RectTransform>(),40,mode=="dead"?500:423,530,48);Rect(third.GetComponent<RectTransform>(),40,mode=="dead"?566:486,190,44);
            Rect(modalBody.rectTransform,40,mode=="dead"?207:223,530,mode=="dead"?40:102);
            if(mode=="dead"){
                modalTitle.text="SIGNAL LOST";modalBody.text="다시 시작할 도시를 선택하세요.";ButtonText(primary,FourCityCatalog.CityNames[respawnCity]+"에서 다시 시작   →");ButtonText(secondary,"타이틀로 돌아가기");ButtonText(third,"게임 종료");secondary.interactable=true;
            }else{
                modalTitle.text="잠시 멈춘 밤";modalBody.text="WASD 달리기 · 마우스 왼쪽 공격 / 오른쪽 로프\n휠 확대·축소 / 1–3 무기 · R 장전 · Q 기술 · CTRL 방어\nSPACE 두 번 더블 점프 · 벽으로 W 등반 · SHIFT 대시\n마우스 시점 · J 사건 일지 · F6 구조 신고 · HOME 시점 초기화";ButtonText(primary,"계속하기   →");ButtonText(secondary,game.Audio.Volume>.01f?"소리 끄기":"소리 켜기");ButtonText(third,"저장하고 타이틀로");secondary.interactable=true;
            }
        }
        void Primary(){if(game.Title)game.Begin();else if(game.Dead)game.Retry(respawnCity);else game.SetPaused(false);}
        void Secondary(){if(game.Title)game.Begin(true);else if(game.Dead)game.ReturnToTitle();else{game.Audio.SetVolume(game.Audio.Volume>.01f?0:.45f);ConfigureModal("pause");}}
        void Third(){if(game.Paused){game.ReturnToTitle();}else Application.Quit();}
        public void AddDamage(Vector3 position,int damage,bool critical)
        {
            if(numbers.Count>=24){Destroy(numbers[0].text.gameObject);numbers.RemoveAt(0);}
            var t=Label(root,damage.ToString(),0,0,120,48,critical?34:23,critical?SignalEffects.Gold:white,FontStyle.Bold);numbers.Add((t,position,Time.time));
        }
        void PlaceScreen(RectTransform rect,Vector2 screen,Vector2 offset){RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=point+new Vector2(-offset.x,offset.y);}
        void PlaceWorld(RectTransform rect,Vector3 position,Vector2 offset){var p=Camera.main.WorldToScreenPoint(position);PlaceScreen(rect,p,offset);}
        Image Panel(Transform parent,string name,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var img=go.GetComponent<Image>();img.color=color;img.raycastTarget=false;Rect(img.rectTransform,x,y,w,h);return img;}
        Text Label(Transform parent,string content,float x,float y,float w,float h,int size,Color color,FontStyle style=FontStyle.Normal){var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.font=font;text.text=content;text.fontSize=size;text.fontStyle=style;text.color=color;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.lineSpacing=.9f;Rect(text.rectTransform,x,y,w,h);return text;}
        Button MakeButton(Transform parent,string title,float x,float y,float w,float h,UnityEngine.Events.UnityAction click){var panel=Panel(parent,"Button",x,y,w,h,new Color(.16f,.29f,.3f));panel.raycastTarget=true;var button=panel.gameObject.AddComponent<Button>();var colors=button.colors;colors.highlightedColor=new Color(.66f,.95f,.86f);colors.pressedColor=new Color(.45f,.75f,.68f);button.colors=colors;button.onClick.AddListener(click);var label=Label(panel.transform,title,16,7,w-32,h-12,18,white);label.alignment=TextAnchor.MiddleLeft;return button;}
        void ButtonText(Button button,string text){button.GetComponentInChildren<Text>().text=text;}
        static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Right(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Bottom(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
        static void Center(RectTransform r,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(w,h);}
        static void CenterTop(RectTransform r,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(w,h);}
        static void CenterBottom(RectTransform r,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,y);r.sizeDelta=new Vector2(w,h);}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
