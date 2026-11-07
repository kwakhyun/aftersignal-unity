using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AfterSignal
{
    /// <summary>
    /// One settings screen shared by the title menu and the pause menu: sound, display, presentation, camera,
    /// interface, difficulty and key bindings. Built in code like the rest of the HUD, on its own canvas above both.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        public static SettingsPanel Instance { get; private set; }
        public static bool IsOpen => Instance && Instance.root && Instance.root.activeSelf;
        /// <summary>Frame on which the panel consumed ESC/back, so menus underneath don't react to the same press.</summary>
        public static int ClosedFrame { get; private set; } = -1;

        static readonly Color Ink = new Color(.025f, .055f, .075f, .98f), Row = new Color(.06f, .12f, .15f, .9f), Button = new Color(.14f, .28f, .3f, .95f);
        static readonly Color Paper = new Color(.9f, .95f, .94f), Mint = new Color(.63f, .94f, .85f), Muted = new Color(.51f, .65f, .69f);
        // Rebindable actions in display order (action name, Korean label).
        static readonly (string action, string label)[] Rebindable =
        {
            ("Interact", "상호작용 · 탑승 · 대화"), ("Exit", "하차 · 탈출"), ("Passenger", "승객으로 탑승"), ("Jump", "점프 · 상승"), ("Dash", "대시 · 가속"), ("Guard", "방어 · 하강"),
            ("Skill", "기술"), ("Reload", "장전"), ("Map", "지도"), ("Journal", "의뢰 일지"), ("VehicleView", "차량 시점"), ("Horn", "경적"), ("SummonBike", "바이크 호출"),
            ("Surrender", "투항 (길게)"), ("CameraReset", "시점 초기화"), ("RescueReport", "구조 신고"), ("Attack", "공격"), ("Grapple", "로프 · 보조 무장"),
        };

        GameDirector game;
        GameObject root, generalPage, keysPage;
        RectTransform card, generalContent, keysContent;
        ScrollRect generalScroll, keysScroll;
        CanvasScaler scaler;
        Font font;
        Action onClose;
        Text pageTitle, rebindStatus;
        readonly List<Action> refreshers = new();
        InputActionRebindingExtensions.RebindingOperation rebinding;

        public static SettingsPanel Ensure(GameDirector owner)
        {
            if (Instance) return Instance;
            var panel = owner.gameObject.AddComponent<SettingsPanel>();
            panel.game = owner;
            Instance = panel;
            return panel;
        }

        public static void Open(GameDirector owner, Action closed = null)
        {
            var panel = Ensure(owner);
            if (!panel.root) panel.Build();
            panel.onClose = closed;
            panel.root.SetActive(true);
            panel.ShowPage(false);
        }

        public void Close()
        {
            if (!root || !root.activeSelf) return;
            CancelRebind();
            PlayerPrefs.Save();
            root.SetActive(false);
            ClosedFrame = Time.frameCount;
            var callback = onClose; onClose = null;
            callback?.Invoke();
        }

        void OnDestroy() { CancelRebind(); if (Instance == this) Instance = null; if (root) Destroy(root); }

        void Update()
        {
            if (!IsOpen) return;
            scaler.referenceResolution = PresentationSettings.CanvasReference;
            var size = ((RectTransform)root.transform).rect.size;
            card.localScale = Vector3.one * Mathf.Min(1, (size.y - 30) / 820f, (size.x - 30) / 800f);
            if (rebinding != null) return;
            bool back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (back) { if (keysPage.activeSelf) ShowPage(false); else Close(); return; }
            KeepSelectionVisible(generalPage.activeSelf ? generalScroll : keysScroll);
        }

        void Build()
        {
            font = Resources.Load<Font>("Fonts/NotoSansKR");
            root = new GameObject("SETTINGS / AFTERSIGNAL", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
            scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; scaler.referenceResolution = PresentationSettings.CanvasReference;
            var shade = Box(root.transform, 0, 0, 10, 10, new Color(.01f, .02f, .035f, .82f)); Stretch(shade.rectTransform); shade.raycastTarget = true;
            card = Box(root.transform, 0, 0, 800, 820, Ink).rectTransform; card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.anchoredPosition = Vector2.zero; card.sizeDelta = new Vector2(800, 820);
            pageTitle = Label(card, "SETTINGS  /  설정", 40, 26, 520, 46, 30, Paper, FontStyle.Bold);
            Label(card, "변경 사항은 바로 적용되고 자동 저장됩니다.  ESC · [B] 뒤로", 40, 74, 720, 24, 14, Muted);
            MakeButton(card, "일반", 40, 108, 170, 40, () => ShowPage(false), 16);
            MakeButton(card, "키 설정", 220, 108, 170, 40, () => ShowPage(true), 16);
            MakeButton(card, "닫기", 590, 26, 170, 44, Close, 17);
            generalPage = Page("General", out generalContent, out generalScroll);
            keysPage = Page("Key bindings", out keysContent, out keysScroll);
            BuildGeneral();
            BuildKeys();
            root.SetActive(false);
        }

        GameObject Page(string name, out RectTransform content, out ScrollRect scroll)
        {
            var view = Box(card, 40, 162, 720, 630, new Color(0, 0, 0, .18f)); view.name = name; view.raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            var holder = new GameObject("Content", typeof(RectTransform)); holder.transform.SetParent(view.transform, false);
            content = holder.GetComponent<RectTransform>(); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 0);
            scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.viewport = view.rectTransform; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            return view.gameObject;
        }

        float y;
        void Section(RectTransform content, string title) { y += 10; Label(content, title, 14, y, 680, 30, 17, Mint, FontStyle.Bold); y += 36; }
        void Finish(RectTransform content) { content.sizeDelta = new Vector2(0, y + 16); }

        void SliderRow(RectTransform content, string title, float min, float max, Func<float> get, Action<float> set, Func<float, string> format)
        {
            Box(content, 6, y, 708, 50, Row);
            Label(content, title, 22, y + 11, 280, 28, 17, Paper);
            var value = Label(content, "", 600, y + 11, 100, 28, 16, Mint); value.alignment = TextAnchor.MiddleRight;
            var go = new GameObject(title, typeof(RectTransform), typeof(Slider)); go.transform.SetParent(content, false);
            Place(go.GetComponent<RectTransform>(), 300, y + 14, 290, 22);
            var track = Box(go.transform, 0, 8, 290, 6, new Color(.12f, .23f, .27f)); track.raycastTarget = true;
            var area = new GameObject("Fill area", typeof(RectTransform)); area.transform.SetParent(go.transform, false); var ar = area.GetComponent<RectTransform>(); ar.anchorMin = new Vector2(0, .5f); ar.anchorMax = new Vector2(1, .5f); ar.sizeDelta = new Vector2(-16, 6); ar.anchoredPosition = Vector2.zero;
            var fill = Box(area.transform, 0, 0, 10, 6, Mint); fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1); fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = new GameObject("Handle area", typeof(RectTransform)); handleArea.transform.SetParent(go.transform, false); var hr = handleArea.GetComponent<RectTransform>(); hr.anchorMin = Vector2.zero; hr.anchorMax = Vector2.one; hr.offsetMin = new Vector2(8, 0); hr.offsetMax = new Vector2(-8, 0);
            var handle = Box(handleArea.transform, 0, 0, 16, 22, Paper); handle.rectTransform.anchorMin = new Vector2(0, 0); handle.rectTransform.anchorMax = new Vector2(0, 1); handle.rectTransform.sizeDelta = new Vector2(16, 0); handle.raycastTarget = true;
            var slider = go.GetComponent<Slider>(); slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.minValue = min; slider.maxValue = max;
            var colors = slider.colors; colors.highlightedColor = new Color(.7f, 1, .9f); colors.selectedColor = new Color(.7f, 1, .9f); slider.colors = colors;
            bool syncing = false;
            slider.onValueChanged.AddListener(v => { if (syncing) return; set(v); value.text = format(v); });
            refreshers.Add(() => { syncing = true; slider.value = get(); value.text = format(slider.value); syncing = false; });
            y += 56;
        }

        void CycleRow(RectTransform content, string title, Func<string> current, Action next, string note = null)
        {
            Box(content, 6, y, 708, 50, Row);
            Label(content, title, 22, y + 11, 280, 28, 17, Paper);
            var button = MakeButton(content, "", 300, y + 6, 400, 38, () => { next(); RefreshAll(); }, 16);
            var text = button.GetComponentInChildren<Text>(); text.alignment = TextAnchor.MiddleCenter;
            refreshers.Add(() => text.text = "◀   " + current() + "   ▶");
            y += 56;
            if (!string.IsNullOrEmpty(note)) { Label(content, note, 22, y - 4, 680, 22, 13, Muted); y += 22; }
        }

        void BuildGeneral()
        {
            y = 8; var c = generalContent;
            Section(c, "게임");
            CycleRow(c, "난이도", () => DifficultySettings.Names[Mathf.Clamp(DifficultySettings.Difficulty, 0, DifficultySettings.Names.Length - 1)], () => DifficultySettings.Difficulty = (DifficultySettings.Difficulty + 1) % DifficultySettings.Names.Length, "적·경찰의 체력과 피해, 수배 상승 속도, 보상과 벌금에 적용됩니다.");
            Section(c, "소리");
            SliderRow(c, "전체 음량", 0, 1, () => game.Audio.Volume, v => game.Audio.SetVolume(v), Percent);
            SliderRow(c, "효과음", 0, 1, () => game.Audio.SfxVolume, v => game.Audio.SetSfxVolume(v), Percent);
            SliderRow(c, "배경음악", 0, 1, () => SignalMusic.Instance ? SignalMusic.Instance.MusicLevel : SignalMusic.DefaultLevel, v => { if (SignalMusic.Instance) SignalMusic.Instance.SetMusicVolume(v); }, Percent);
            Section(c, "화면");
            CycleRow(c, "화면 모드", () => Screen.fullScreen ? "전체 화면" : "창 모드", () => { bool full = !Screen.fullScreen; Screen.fullScreenMode = full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Fullscreen", full ? 1 : 0); });
            CycleRow(c, "해상도", () => $"{Screen.width} × {Screen.height}", NextResolution);
            CycleRow(c, "그래픽 품질", () => FidelityPresentation.PresetName, FidelityPresentation.Cycle);
            CycleRow(c, "프레임 제한", () => PresentationSettings.VSync ? "수직 동기화 사용" : PresentationSettings.FrameLimitNames[PresentationSettings.FrameLimit], () => { PresentationSettings.FrameLimit = (PresentationSettings.FrameLimit + 1) % PresentationSettings.FrameLimits.Length; PresentationSettings.Save(); });
            CycleRow(c, "수직 동기화", () => PresentationSettings.VSync ? "켜짐" : "꺼짐", () => { PresentationSettings.VSync = !PresentationSettings.VSync; PresentationSettings.Save(); }, "끊김이 보이면 60 FPS 제한과 수직 동기화 꺼짐을 권장합니다.");
            Section(c, "연출");
            CycleRow(c, "화면 흔들림", () => PresentationSettings.Motion > 0 ? "켜짐" : "꺼짐", () => { PresentationSettings.Motion = PresentationSettings.Motion > 0 ? 0 : .65f; PresentationSettings.Save(); });
            CycleRow(c, "전투 효과", () => PresentationSettings.Effects > 0 ? "켜짐" : "꺼짐", () => { PresentationSettings.Effects = PresentationSettings.Effects > 0 ? 0 : .7f; PresentationSettings.Save(); });
            CycleRow(c, "후처리 (블룸·색보정)", () => PresentationSettings.Post ? "켜짐" : "꺼짐", () => { PresentationSettings.Post = !PresentationSettings.Post; PresentationSettings.Save(); });
            Section(c, "카메라");
            SliderRow(c, "시점 감도", .2f, 3, () => PresentationSettings.MouseSensitivity, v => { PresentationSettings.MouseSensitivity = Mathf.Round(v * 20) / 20; PresentationSettings.Save(); }, v => $"{v:0.00}×");
            CycleRow(c, "상하 반전", () => PresentationSettings.InvertY ? "켜짐" : "꺼짐", () => { PresentationSettings.InvertY = !PresentationSettings.InvertY; PresentationSettings.Save(); });
            Section(c, "인터페이스");
            CycleRow(c, "UI 크기", () => PresentationSettings.UiScaleNames[PresentationSettings.UiScaleIndex], () => { PresentationSettings.UiScale = PresentationSettings.UiScales[(PresentationSettings.UiScaleIndex + 1) % PresentationSettings.UiScales.Length]; PresentationSettings.Save(); });
            CycleRow(c, "조작 안내", () => PresentationSettings.ControlHintNames[PresentationSettings.ControlHints], () => { PresentationSettings.ControlHints = (PresentationSettings.ControlHints + 1) % 3; PresentationSettings.Save(); });
            CycleRow(c, "대사 표시 속도", () => PresentationSettings.TextSpeedNames[PresentationSettings.TextSpeed], () => { PresentationSettings.TextSpeed = (PresentationSettings.TextSpeed + 1) % 3; PresentationSettings.Save(); });
            CycleRow(c, "대사 자동 진행", () => PresentationSettings.AutoAdvance ? "켜짐" : "꺼짐", () => { PresentationSettings.AutoAdvance = !PresentationSettings.AutoAdvance; PresentationSettings.Save(); });
            Finish(c);
        }

        void BuildKeys()
        {
            y = 8; var c = keysContent;
            rebindStatus = Label(c, "", 14, y, 690, 44, 15, Mint); y += 46;
            Label(c, "키보드 · 마우스", 300, y, 200, 24, 14, Muted); Label(c, "게임패드", 560, y, 150, 24, 14, Muted); y += 28;
            foreach (var (action, label) in Rebindable)
            {
                string actionName = action;
                Box(c, 6, y, 708, 46, Row);
                Label(c, label, 22, y + 9, 270, 28, 16, Paper);
                var button = MakeButton(c, "", 300, y + 5, 240, 36, () => StartRebind(actionName), 16);
                var text = button.GetComponentInChildren<Text>(); text.alignment = TextAnchor.MiddleCenter;
                var pad = Label(c, "", 560, y + 9, 150, 28, 16, Mint);
                refreshers.Add(() => { text.text = KeyHints.For(actionName); pad.text = KeyHints.For(actionName, true); if (string.IsNullOrEmpty(pad.text)) pad.text = "—"; });
                y += 52;
            }
            y += 6;
            MakeButton(c, "기본 키로 복원", 6, y, 708, 44, ResetBindings, 17); y += 54;
            Label(c, "이동(WASD/왼쪽 스틱)과 시점(마우스/오른쪽 스틱)은 고정입니다. 지도 클릭은 마우스로 사용합니다.", 14, y, 690, 44, 13, Muted); y += 48;
            Finish(c);
        }

        void ShowPage(bool keys)
        {
            if (!root) return;
            CancelRebind();
            generalPage.SetActive(!keys); keysPage.SetActive(keys);
            pageTitle.text = keys ? "SETTINGS  /  키 설정" : "SETTINGS  /  설정";
            if (rebindStatus) rebindStatus.text = "바꿀 동작을 선택한 뒤 새 키를 누르세요. 다른 동작과 겹치는 키는 두 동작에 모두 쓰입니다.";
            RefreshAll();
            var first = (keys ? keysContent : generalContent).GetComponentInChildren<Selectable>();
            if (EventSystem.current && first) EventSystem.current.SetSelectedGameObject(first.gameObject);
            (keys ? keysScroll : generalScroll).verticalNormalizedPosition = 1;
        }

        void RefreshAll() { foreach (var r in refreshers) r(); }

        void StartRebind(string actionName)
        {
            var input = game ? game.Input : null;
            var action = input && input.Actions ? input.Actions.FindAction(actionName) : null;
            if (action == null) { rebindStatus.text = "게임이 시작된 뒤에 키를 바꿀 수 있습니다."; return; }
            int index = -1;
            for (int i = 0; i < action.bindings.Count; i++)
                if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite && action.bindings[i].groups != null && action.bindings[i].groups.Contains(KeyHints.KeyboardGroup)) { index = i; break; }
            if (index < 0) return;
            CancelRebind();
            rebindStatus.text = "'" + Rebindable.First(r => r.action == actionName).label + "'에 쓸 키를 누르세요 · ESC 취소";
            action.Disable();
            rebinding = action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath("<Keyboard>").WithControlsHavingToMatchPath("<Mouse>")
                .WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta").WithControlsExcluding("<Mouse>/scroll").WithControlsExcluding("<Mouse>/press")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(.1f)
                .OnComplete(op => { FinishRebind(op, true); })
                .OnCancel(op => { FinishRebind(op, false); })
                .Start();
        }

        void FinishRebind(InputActionRebindingExtensions.RebindingOperation op, bool applied)
        {
            var action = op.action;
            op.Dispose(); if (rebinding == op) rebinding = null;
            action?.Enable();
            if (applied && game && game.Input) game.Input.SaveBindings();
            rebindStatus.text = applied ? "저장했습니다. 화면의 키 안내도 새 키로 표시됩니다." : "변경을 취소했습니다.";
            ClosedFrame = Time.frameCount;
            RefreshAll();
        }

        void CancelRebind() { if (rebinding != null) { var op = rebinding; rebinding = null; op.Cancel(); } }

        void ResetBindings()
        {
            CancelRebind();
            if (game && game.Input && game.Input.Actions) { game.Input.Actions.RemoveAllBindingOverrides(); game.Input.SaveBindings(); }
            else PlayerPrefs.DeleteKey(PlayerInputReader.BindingsKey);
            rebindStatus.text = "모든 키를 기본값으로 되돌렸습니다.";
            RefreshAll();
        }

        static void NextResolution()
        {
            var sizes = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Where(s => s.x >= 1024 && s.y >= 576).Distinct().OrderBy(s => s.x * s.y).ToList();
            if (sizes.Count == 0) return;
            int current = sizes.FindIndex(s => s.x == Screen.width && s.y == Screen.height);
            var next = sizes[(current + 1) % sizes.Count];
            Screen.SetResolution(next.x, next.y, Screen.fullScreenMode);
        }

        static string Percent(float v) => v < .005f ? "꺼짐" : $"{v * 100:0}%";

        void KeepSelectionVisible(ScrollRect scroll)
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (!selected || !selected.transform.IsChildOf(scroll.content)) return;
            var item = (RectTransform)selected.transform; var view = scroll.viewport;
            float top = -item.anchoredPosition.y, bottom = top + item.rect.height;
            float offset = scroll.content.anchoredPosition.y, height = view.rect.height;
            if (top - 40 < offset) offset = Mathf.Max(0, top - 40);
            else if (bottom + 40 > offset + height) offset = bottom + 40 - height;
            scroll.content.anchoredPosition = new Vector2(scroll.content.anchoredPosition.x, Mathf.Min(offset, Mathf.Max(0, scroll.content.rect.height - height)));
        }

        Image Box(Transform parent, float x, float yPos, float w, float h, Color color)
        {
            var go = new GameObject("Box", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; Place(image.rectTransform, x, yPos, w, h); return image;
        }
        Text Label(Transform parent, string text, float x, float yPos, float w, float h, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>(); t.font = font; t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = false;
            Place(t.rectTransform, x, yPos, w, h); return t;
        }
        UnityEngine.UI.Button MakeButton(Transform parent, string title, float x, float yPos, float w, float h, Action click, int size)
        {
            var image = Box(parent, x, yPos, w, h, Button); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); var colors = button.colors; colors.highlightedColor = new Color(.7f, 1, .9f); colors.selectedColor = new Color(.62f, .95f, .85f); colors.pressedColor = new Color(.45f, .75f, .68f); button.colors = colors;
            button.onClick.AddListener(() => click());
            var label = Label(image.transform, title, 12, 0, w - 24, h, size, Paper); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        static void Place(RectTransform r, float x, float yPos, float w, float h) { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -yPos); r.sizeDelta = new Vector2(w, h); }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    }
}
