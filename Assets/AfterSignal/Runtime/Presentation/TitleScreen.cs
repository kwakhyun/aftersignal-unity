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
        public bool SettingsOpen => SettingsPanel.IsOpen;
        public bool ConfirmationOpen => confirmation && confirmation.activeSelf;
        public bool SlotsOpen => slots && slots.activeSelf;
        public bool Visible => screen && screen.activeSelf;
        public Button ContinueButton { get; private set; }
        public Button NewGameButton { get; private set; }
        public Button LoadButton { get; private set; }
        public bool ArtworkLoaded => artwork && artwork.texture;
        GameDirector game;
        GameObject screen, menu, confirmation, loading, slots;
        Text slotsTitle, confirmTitle;readonly Button[] slotButtons=new Button[SaveStore.SlotCount];bool slotsForNewGame;int pendingSlot=-1;
        CanvasGroup entrance;
        RawImage artwork;
        RectTransform artRect, progressFill;
        Text saveInfo, loadingText;
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
            bool busy=game.Transition;
            loading.SetActive(busy);menu.SetActive(!busy);
            if (busy)
            {
                if(SettingsOpen)SettingsPanel.Instance.Close();confirmation.SetActive(false);
                float p=game.LoadProgress;
                progressFill.anchorMax=new Vector2(p,1);
                loadingText.text=$"도시와 연결 중   {p*100:0}%";
                return;
            }
            bool back=Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame||Gamepad.current!=null&&Gamepad.current.buttonEast.wasPressedThisFrame;
            if (back&&ConfirmationOpen&&SettingsPanel.ClosedFrame!=Time.frameCount) { CancelNewGame(); }
            else if (back&&SlotsOpen&&SettingsPanel.ClosedFrame!=Time.frameCount) { ClosePanels();SelectDefault(); }
            if (!SettingsOpen&&!ConfirmationOpen&&!SlotsOpen&&EventSystem.current&&!EventSystem.current.currentSelectedGameObject&&Gamepad.current!=null&&Gamepad.current.leftStick.ReadValue().sqrMagnitude>.25f) SelectDefault();
        }
        void Build()
        {
            font=Resources.Load<Font>("Fonts/NotoSansKR");
            screen=new GameObject("TITLE / AFTERSIGNAL",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
            var canvas=screen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=screen.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=PresentationSettings.CanvasReference;scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
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
            menu=new GameObject("Main menu",typeof(RectTransform));menu.transform.SetParent(body.transform,false);Place(menu.GetComponent<RectTransform>(),84,496,460,308);
            ContinueButton=MenuButton(menu.transform,"Continue","이어하기","CONTINUE",0,()=>StartGame(true));
            LoadButton=MenuButton(menu.transform,"Load","불러오기","LOAD",62,()=>OpenSlots(false));
            NewGameButton=MenuButton(menu.transform,"New game","새 게임","NEW GAME",124,RequestNewGame);
            settingsButton=MenuButton(menu.transform,"Settings","설정","SETTINGS",186,OpenSettings);
            MenuButton(menu.transform,"Exit","게임 종료","EXIT",248,Quit);
            saveInfo=Label(body.transform,"",86,814,550,48,13,Muted);
            var footer=Label(root,"© AFTERSIGNAL STUDIO   /   "+Application.version,0,0,390,25,12,Muted);
            var f=footer.rectTransform;f.anchorMin=f.anchorMax=f.pivot=new Vector2(1,0);f.anchoredPosition=new Vector2(-38,24);
            var audioCredit=Resources.Load<TextAsset>("Audio/Generated/attribution");
            if(audioCredit)
            {
                var credit=Label(root,audioCredit.text.Trim(),0,0,470,25,12,Muted);
                var cr=credit.rectTransform;cr.anchorMin=cr.anchorMax=cr.pivot=new Vector2(1,0);cr.anchoredPosition=new Vector2(-38,52);
            }
            var hint=Label(root,"↑ ↓  메뉴 선택     ENTER · [A]  확인     ESC · [B]  뒤로",0,0,520,24,13,Muted);
            var hr=hint.rectTransform;hr.anchorMin=hr.anchorMax=hr.pivot=new Vector2(0,0);hr.anchoredPosition=new Vector2(84,22);
            BuildSlots(root);BuildConfirmation(root);
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
            bool any=false;for(int i=0;i<SaveStore.SlotCount;i++)any|=SaveStore.Summary(i).exists;
            LoadButton.interactable=any;LoadButton.GetComponent<CanvasGroup>().alpha=any?1:.36f;
            var summary=SaveStore.Summary(SaveStore.ActiveSlot);
            saveInfo.text=saved?$"슬롯 {SaveStore.ActiveSlot+1}   ·   {Describe(summary)}\n마지막으로 저장된 구역 입구에서 이어집니다."+(SaveStore.LastLoadWarning!=null?"\n"+SaveStore.LastLoadWarning:""):"서하의 집에서 이야기를 시작하세요.";
        }
        void SelectDefault(){if(EventSystem.current)EventSystem.current.SetSelectedGameObject((GameDirector.HasSavedGame?ContinueButton:NewGameButton).gameObject);}
        void ClosePanels(){PlayerPrefs.Save();if(SettingsOpen)SettingsPanel.Instance.Close();confirmation.SetActive(false);if(slots)slots.SetActive(false);menu.GetComponent<CanvasGroup>().interactable=true;menu.GetComponent<CanvasGroup>().blocksRaycasts=true;}
        void BlockMenu(){var g=menu.GetComponent<CanvasGroup>();g.interactable=false;g.blocksRaycasts=false;}
        public void RequestNewGame()
        {
            if(game.Transition)return;
            bool any=false;for(int i=0;i<SaveStore.SlotCount;i++)any|=SaveStore.Summary(i).exists;
            if (!any) { StartGame(false);return; }
            OpenSlots(true);
        }
        /// <summary>Slot list for loading (occupied slots only) or for a new game (any slot; occupied asks first).</summary>
        public void OpenSlots(bool newGame)
        {
            if(game.Transition)return;
            slotsForNewGame=newGame;confirmation.SetActive(false);slots.SetActive(true);BlockMenu();
            slotsTitle.text=newGame?"새 이야기를 시작할 슬롯을 고르세요":"불러올 기록을 고르세요";
            for(int i=0;i<slotButtons.Length;i++)
            {
                var s=SaveStore.Summary(i);var label=slotButtons[i].GetComponentInChildren<Text>();
                label.text=$"슬롯 {i+1}   ·   "+(s.exists?Describe(s)+$"\n마지막 저장  {s.savedAt:yyyy-MM-dd HH:mm}"+(i==SaveStore.ActiveSlot?"   ·   최근 사용":""):"비어 있음");
                slotButtons[i].interactable=newGame||s.exists;slotButtons[i].GetComponent<CanvasGroup>().alpha=slotButtons[i].interactable?1:.4f;
            }
            int first=System.Array.FindIndex(slotButtons,b=>b.interactable);
            EventSystem.current?.SetSelectedGameObject(slotButtons[Mathf.Max(0,first)].gameObject);
        }
        public void SelectSlot(int slot)
        {
            if(slotsForNewGame&&SaveStore.Summary(slot).exists)
            {
                pendingSlot=slot;slots.SetActive(false);confirmation.SetActive(true);
                confirmTitle.text=$"슬롯 {slot+1}에 새 이야기를 시작할까요?";
                EventSystem.current?.SetSelectedGameObject(confirmation.transform.Find("Card/Cancel").gameObject);
                return;
            }
            SaveStore.ActiveSlot=slot;StartGame(!slotsForNewGame);
        }
        public void ConfirmNewGame(){if(pendingSlot>=0)SaveStore.ActiveSlot=pendingSlot;pendingSlot=-1;StartGame(false);}
        public void CancelNewGame(){pendingSlot=-1;ClosePanels();SelectDefault();}
        static string Describe(SaveStore.SlotSummary s)
        {
            var stage=(StageId)s.stage;string place=CivicWorld.Exploration(stage)?CivicWorld.Title(stage):CampaignCatalog.Title(stage);
            return $"DAY {Mathf.FloorToInt(s.hours/24)+1:00}   ·   {place}   ·   {s.credits:N0} C";
        }
        void BuildSlots(Transform root)
        {
            slots=Box(root,0,0,1600,900,new Color(.01f,.025f,.04f,.9f)).gameObject;slots.name="Save slots";Stretch(slots.GetComponent<RectTransform>());slots.GetComponent<Image>().raycastTarget=true;
            var card=CenterPanel(slots.transform,700,520);Box(card,0,0,700,520,new Color(.03f,.065f,.085f));
            slotsTitle=Label(card,"",36,30,628,44,26,Paper,FontStyle.Bold);
            for(int i=0;i<slotButtons.Length;i++){int slot=i;slotButtons[i]=PlainButton(card,"Slot "+(i+1),"",36,96+i*112,628,96,()=>SelectSlot(slot));slotButtons[i].gameObject.AddComponent<CanvasGroup>();var t=slotButtons[i].GetComponentInChildren<Text>();t.fontSize=18;}
            PlainButton(card,"Back","돌아가기",36,438,628,52,()=>{ClosePanels();SelectDefault();});
            slots.SetActive(false);
        }
        public void StartGame(bool resume)
        {
            if (game.Transition || resume&&!GameDirector.HasSavedGame) return;
            ClosePanels();game.Audio.Play("ui_confirm",game.Player.Shoulder,.2f,1);game.Begin(resume);
        }
        public void OpenSettings()
        {
            confirmation.SetActive(false);BlockMenu();
            SettingsPanel.Open(game,()=>{ClosePanels();SelectDefault();});
        }
        void BuildConfirmation(Transform root)
        {
            confirmation=Box(root,0,0,1600,900,new Color(.01f,.025f,.04f,.9f)).gameObject;confirmation.name="Confirm new game";Stretch(confirmation.GetComponent<RectTransform>());confirmation.GetComponent<Image>().raycastTarget=true;
            var card=CenterPanel(confirmation.transform,630,365);card.name="Card";
            Box(card,0,0,630,365,new Color(.03f,.065f,.085f));
            confirmTitle=Label(card,"새로운 신호를 시작할까요?",36,35,558,48,26,Paper,FontStyle.Bold);
            Label(card,"이 슬롯에 저장된 이야기와 재화가 모두 지워집니다.\n다른 슬롯의 기록은 그대로 남습니다.",36,112,558,92,18,Muted);
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
