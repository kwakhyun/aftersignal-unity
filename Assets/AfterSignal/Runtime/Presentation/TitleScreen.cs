using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    // A separate canvas keeps the title independent from world HUD and scene layout.
    public sealed class TitleScreen : MonoBehaviour
    {
        public static TitleScreen Instance { get; private set; }
        public bool SettingsOpen => settings && settings.activeSelf;
        public bool ConfirmationOpen => confirmation && confirmation.activeSelf;
        public bool Visible => screen && screen.activeSelf;
        public Button ContinueButton { get; private set; }
        public Button NewGameButton { get; private set; }
        public bool ArtworkLoaded => artwork && artwork.texture;
        GameDirector game;
        GameObject screen, menu, settings, confirmation, loading;
        CanvasGroup entrance;
        RawImage artwork;
        RectTransform artRect, progressFill;
        Text saveInfo, loadingText, motionText, effectsText, displayText;
        Button settingsButton;
        Font font;
        float age;
        bool shown;
        static readonly Color Paper = new Color(.9f,.95f,.94f), Mint = new Color(.63f,.94f,.85f), Muted = new Color(.51f,.65f,.69f);
        public void Initialize(GameDirector owner)
        {
            game=owner; Instance=this;
            if(game.Title&&PlayerPrefs.HasKey("AFTERSIGNAL.Unity.Fullscreen"))
                Screen.fullScreenMode=PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Fullscreen")==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
        }
        void Update()
        {
            if (!game || !game.Ready) return;
            if (game.Title && !screen) Build();
            if (!screen) return;
            if (game.Title != shown)
            {
                shown=game.Title;screen.SetActive(shown);
                if (shown) { age=0;Refresh();ClosePanels();SelectDefault(); }
            }
            if (!shown) return;
            age+=Time.unscaledDeltaTime;
            entrance.alpha=Mathf.SmoothStep(0,1,age/.65f);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(SettingsOpen)RefreshSettings();
            bool busy=game.Transition;
            loading.SetActive(busy);menu.SetActive(!busy);
            if (busy)
            {
                settings.SetActive(false);confirmation.SetActive(false);
                float p=game.LoadProgress;
                progressFill.anchorMax=new Vector2(p,1);
                loadingText.text=$"도시와 연결 중   {p*100:0}%";
                return;
            }
            if (Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { if (SettingsOpen||ConfirmationOpen) { ClosePanels();SelectDefault(); } }
        }
        void Build()
        {
            font=Resources.Load<Font>("Fonts/NotoSansKR");
            screen=new GameObject("TITLE / AFTERSIGNAL",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
            var canvas=screen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=screen.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            entrance=screen.GetComponent<CanvasGroup>();entrance.alpha=0;
            var canvasRoot=screen.transform;
            var surround=Box(canvasRoot,0,0,1600,900,new Color(.006f,.018f,.028f));Stretch(surround.rectTransform);
            // Keep the embedded logo, cast and live menu together at every display aspect ratio.
            var frame=new GameObject("Title artwork frame",typeof(RectTransform));frame.transform.SetParent(canvasRoot,false);
            var frameRect=frame.GetComponent<RectTransform>();frameRect.anchorMin=frameRect.anchorMax=frameRect.pivot=new Vector2(.5f,.5f);frameRect.sizeDelta=new Vector2(1600,900);
            var root=frame.transform;
            var art=new GameObject("Afterlight / title key art",typeof(RectTransform),typeof(RawImage));art.transform.SetParent(root,false);
            artwork=art.GetComponent<RawImage>();artwork.texture=Resources.Load<Texture2D>("Art/Title/AfterlightTitle");
            artwork.raycastTarget=false;artRect=art.GetComponent<RectTransform>();
            Stretch(artRect);
            var shade=new GameObject("Menu readability gradient",typeof(RectTransform),typeof(TitleShade));shade.transform.SetParent(root,false);Stretch(shade.GetComponent<RectTransform>());
            shade.GetComponent<RectTransform>().anchorMax=new Vector2(1,.57f);
            var body=new GameObject("Title composition",typeof(RectTransform));body.transform.SetParent(root,false);
            var bodyRect=body.GetComponent<RectTransform>();bodyRect.anchorMin=new Vector2(0,.5f);bodyRect.anchorMax=new Vector2(0,.5f);bodyRect.pivot=new Vector2(0,.5f);bodyRect.sizeDelta=new Vector2(670,900);
            // English branding is part of the commissioned artwork; do not draw a second logo.
            Label(body.transform,"애 프 터 시 그 널",86,405,460,30,21,Paper);
            Label(body.transform,"끊어진 신호 너머, 다시 이어지는 도시.",86,448,470,26,16,Muted);
            menu=new GameObject("Main menu",typeof(RectTransform));menu.transform.SetParent(body.transform,false);Place(menu.GetComponent<RectTransform>(),84,520,460,268);
            ContinueButton=MenuButton(menu.transform,"Continue","이어하기","CONTINUE",0,()=>StartGame(true));
            NewGameButton=MenuButton(menu.transform,"New game","새 게임","NEW GAME",67,RequestNewGame);
            settingsButton=MenuButton(menu.transform,"Settings","설정","SETTINGS",134,OpenSettings);
            MenuButton(menu.transform,"Exit","게임 종료","EXIT",201,Quit);
            saveInfo=Label(body.transform,"",86,814,550,48,13,Muted);
            var footer=Label(root,"© AFTERSIGNAL STUDIO   /   "+Application.version,0,0,390,25,11,Muted);
            var f=footer.rectTransform;f.anchorMin=f.anchorMax=f.pivot=new Vector2(1,0);f.anchoredPosition=new Vector2(-38,24);
            var hint=Label(root,"↑ ↓  메뉴 선택     ENTER  확인     ESC  뒤로",0,0,450,24,12,Muted);
            var hr=hint.rectTransform;hr.anchorMin=hr.anchorMax=hr.pivot=new Vector2(0,0);hr.anchoredPosition=new Vector2(84,22);
            BuildSettings(root);BuildConfirmation(root);
            loading=Box(root,0,0,1600,900,new Color(.015f,.035f,.055f,.78f)).gameObject;loading.name="Connecting";Stretch(loading.GetComponent<RectTransform>());loading.GetComponent<Image>().raycastTarget=true;
            var loadCard=CenterPanel(loading.transform,580,160);
            Label(loadCard,"AFTERSIGNAL",0,8,580,55,34,Paper,FontStyle.Bold).alignment=TextAnchor.MiddleCenter;
            loadingText=Label(loadCard,"도시와 연결 중",0,85,580,32,18,Mint);loadingText.alignment=TextAnchor.MiddleCenter;
            var track=Box(loadCard,40,142,500,3,new Color(.25f,.36f,.38f));
            var fill=Box(track.transform,0,0,500,3,Mint);progressFill=fill.rectTransform;Stretch(progressFill);
            screen.SetActive(false);
        }
        public void Refresh()
        {
            if (!screen) return;
            bool saved=GameDirector.HasSavedGame;
            ContinueButton.interactable=saved;
            ContinueButton.GetComponent<CanvasGroup>().alpha=saved?1:.36f;
            int stage=PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Stage",0);
            string place=CivicWorld.Exploration((StageId)stage)?CivicWorld.Title((StageId)stage):CampaignCatalog.Title((StageId)stage);
            float hours=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.Life.Hours",8);
            saveInfo.text=saved?$"DAY {Mathf.FloorToInt(hours/24)+1:00}   ·   {place}\n마지막으로 저장된 구역 입구에서 이어집니다.":"서하의 집에서 이야기를 시작하세요.";
        }
        void SelectDefault(){if(EventSystem.current)EventSystem.current.SetSelectedGameObject((GameDirector.HasSavedGame?ContinueButton:NewGameButton).gameObject);}
        void ClosePanels(){PlayerPrefs.Save();settings.SetActive(false);confirmation.SetActive(false);menu.GetComponent<CanvasGroup>().interactable=true;menu.GetComponent<CanvasGroup>().blocksRaycasts=true;}
        void BlockMenu(){var g=menu.GetComponent<CanvasGroup>();g.interactable=false;g.blocksRaycasts=false;}
        public void RequestNewGame()
        {
            if(game.Transition)return;
            if (!GameDirector.HasSavedGame) { StartGame(false);return; }
            confirmation.SetActive(true);BlockMenu();
            EventSystem.current?.SetSelectedGameObject(confirmation.transform.Find("Card/Cancel").gameObject);
        }
        public void ConfirmNewGame(){StartGame(false);}
        public void CancelNewGame(){ClosePanels();SelectDefault();}
        public void StartGame(bool resume)
        {
            if (game.Transition || resume&&!GameDirector.HasSavedGame) return;
            ClosePanels();game.Audio.Play("ui_confirm",game.Player.Shoulder,.2f,1);game.Begin(resume);
        }
        public void OpenSettings()
        {
            settings.SetActive(true);confirmation.SetActive(false);BlockMenu();RefreshSettings();
            EventSystem.current?.SetSelectedGameObject(settings.GetComponentInChildren<Slider>().gameObject);
        }
        void BuildSettings(Transform root)
        {
            settings=Box(root,0,0,1600,900,new Color(.01f,.025f,.04f,.86f)).gameObject;settings.name="Title settings";Stretch(settings.GetComponent<RectTransform>());settings.GetComponent<Image>().raycastTarget=true;
            var card=CenterPanel(settings.transform,670,720);
            Box(card,0,0,670,720,new Color(.025f,.055f,.075f,.98f));
            Label(card,"SETTINGS  /  설정",42,34,580,52,32,Paper,FontStyle.Bold);
            Label(card,"소리와 화면 설정은 게임에도 함께 적용됩니다.",42,94,580,30,16,Muted);
            SettingSlider(card,"전체 음량",154,game.Audio.Volume,v=>game.Audio.SetVolume(v));
            SettingSlider(card,"효과음",238,game.Audio.SfxVolume,v=>game.Audio.SetSfxVolume(v));
            SettingSlider(card,"배경음악",322,SignalMusic.Instance.MusicLevel,v=>SignalMusic.Instance.SetMusicVolume(v));
            var m=PlainButton(card,"Motion","",42,416,286,52,()=>{PresentationSettings.Motion=PresentationSettings.Motion>0?0:.65f;PresentationSettings.Save();RefreshSettings();});motionText=m.GetComponentInChildren<Text>();
            var e=PlainButton(card,"Effects","",342,416,286,52,()=>{PresentationSettings.Post=!PresentationSettings.Post;PresentationSettings.Save();RefreshSettings();});effectsText=e.GetComponentInChildren<Text>();
            var d=PlainButton(card,"Display","",42,484,586,52,()=>{bool fullscreen=!Screen.fullScreen;Screen.fullScreenMode=fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Fullscreen",fullscreen?1:0);RefreshSettings();});displayText=d.GetComponentInChildren<Text>();
            Label(card,"ESC로 돌아갈 수 있습니다. 변경 사항은 자동 저장됩니다.",42,558,590,30,14,Muted);
            PlainButton(card,"Back","돌아가기",42,626,586,52,()=>{PlayerPrefs.Save();ClosePanels();SelectDefault();});
        }
        void SettingSlider(Transform parent,string title,float y,float value,UnityEngine.Events.UnityAction<float> changed)
        {
            var label=Label(parent,title,42,y,350,30,18,Paper);
            var number=Label(parent,$"{value*100:0}%",536,y,92,30,16,Mint);number.alignment=TextAnchor.MiddleRight;
            var go=new GameObject(title,typeof(RectTransform),typeof(Slider));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),42,y+39,586,22);
            Box(go.transform,0,7,586,5,new Color(.12f,.23f,.27f));
            var area=new GameObject("Fill area",typeof(RectTransform));area.transform.SetParent(go.transform,false);Stretch(area.GetComponent<RectTransform>());area.GetComponent<RectTransform>().offsetMin=new Vector2(8,7);area.GetComponent<RectTransform>().offsetMax=new Vector2(-8,-10);
            var fill=Box(area.transform,0,0,570,5,Mint);Stretch(fill.rectTransform);
            var handles=new GameObject("Handle area",typeof(RectTransform));handles.transform.SetParent(go.transform,false);Stretch(handles.GetComponent<RectTransform>());handles.GetComponent<RectTransform>().offsetMin=new Vector2(8,0);handles.GetComponent<RectTransform>().offsetMax=new Vector2(-8,0);
            var handle=Box(handles.transform,0,0,14,22,Paper);handle.raycastTarget=true;
            var slider=go.GetComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.value=value;
            slider.onValueChanged.AddListener(v=>{number.text=$"{v*100:0}%";changed(v);});
        }
        void RefreshSettings()
        {
            motionText.text=PresentationSettings.Motion>0?"화면 흔들림  켜짐":"화면 흔들림  꺼짐";
            effectsText.text=PresentationSettings.Post?"후처리 효과  켜짐":"후처리 효과  꺼짐";
            displayText.text=Screen.fullScreen?"화면 모드  전체 화면":"화면 모드  창 모드";
        }
        void BuildConfirmation(Transform root)
        {
            confirmation=Box(root,0,0,1600,900,new Color(.01f,.025f,.04f,.9f)).gameObject;confirmation.name="Confirm new game";Stretch(confirmation.GetComponent<RectTransform>());confirmation.GetComponent<Image>().raycastTarget=true;
            var card=CenterPanel(confirmation.transform,630,365);card.name="Card";
            Box(card,0,0,630,365,new Color(.03f,.065f,.085f));
            Label(card,"새로운 신호를 시작할까요?",36,35,558,48,28,Paper,FontStyle.Bold);
            Label(card,"현재 저장된 이야기와 재화가 초기화됩니다.\n이어가려면 취소한 뒤 이어하기를 선택하세요.",36,112,558,92,18,Muted);
            PlainButton(card,"Cancel","취소",36,270,268,55,CancelNewGame);
            PlainButton(card,"Confirm","새 게임 시작",324,270,270,55,ConfirmNewGame);
        }
        Button MenuButton(Transform p,string name,string label,string sub,float y,UnityEngine.Events.UnityAction action)
        {
            if(!p.GetComponent<CanvasGroup>())p.gameObject.AddComponent<CanvasGroup>();
            var b=PlainButton(p,name,label,0,y,460,59,action);
            b.gameObject.AddComponent<CanvasGroup>();
            b.GetComponent<Image>().color=new Color(.16f,.32f,.34f,.5f);
            Label(b.transform,sub,292,21,148,21,11,Muted).alignment=TextAnchor.MiddleRight;
            Box(b.transform,0,58,460,1,new Color(.4f,.64f,.62f,.35f));
            return b;
        }
        Button PlainButton(Transform p,string name,string label,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var img=Box(p,x,y,w,h,new Color(.12f,.26f,.29f,.9f));img.gameObject.name=name;img.raycastTarget=true;
            var button=img.gameObject.AddComponent<Button>();var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,1,.92f);colors.selectedColor=new Color(.68f,.96f,.88f);colors.pressedColor=new Color(.43f,.72f,.7f);colors.disabledColor=new Color(.4f,.5f,.5f);button.colors=colors;button.onClick.AddListener(action);
            Label(img.transform,label,22,0,w-44,h,21,Paper).alignment=TextAnchor.MiddleLeft;
            img.gameObject.AddComponent<TitleButtonFocus>();
            return button;
        }
        public void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        RectTransform CenterPanel(Transform parent,float w,float h){var go=new GameObject("Card",typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);return r;}
        Image Box(Transform p,float x,float y,float w,float h,Color color){var go=new GameObject("Panel",typeof(RectTransform),typeof(Image));go.transform.SetParent(p,false);var img=go.GetComponent<Image>();img.color=color;img.raycastTarget=false;Place(img.rectTransform,x,y,w,h);return img;}
        Text Label(Transform p,string text,float x,float y,float w,float h,int size,Color color,FontStyle style=FontStyle.Normal){var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(p,false);var t=go.GetComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.fontStyle=style;t.color=color;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Truncate;Place(t.rectTransform,x,y,w,h);return t;}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        void OnDestroy(){if(screen)Destroy(screen);if(Instance==this)Instance=null;}
    }
    public sealed class TitleButtonFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler, IPointerExitHandler
    {
        Text label;bool selected,hover;float shift;
        void Awake(){label=GetComponentInChildren<Text>();}
        void Update(){shift=Mathf.MoveTowards(shift,selected||hover?7:0,Time.unscaledDeltaTime*60);if(label)label.rectTransform.anchoredPosition=new Vector2(22+shift,0);}
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;}
        public void OnSelect(BaseEventData e){selected=true;}
        public void OnDeselect(BaseEventData e){selected=false;}
    }
    public sealed class TitleShade : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            float[] stops={0,.34f,.57f,1};float[] alpha={.38f,.24f,0,0};
            for(int i=0;i<4;i++){var c=new Color(.008f,.025f,.045f,alpha[i]);vh.AddVert(new Vector3(r.xMin+r.width*stops[i],r.yMin),c,Vector2.zero);c.a=0;vh.AddVert(new Vector3(r.xMin+r.width*stops[i],r.yMax),c,Vector2.zero);}
            for(int i=0;i<3;i++){int k=i*2;vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k+2,k+1,k+3);}
        }
        protected override void Awake(){base.Awake();raycastTarget=false;}
    }
}
