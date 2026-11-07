using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        GameObject lifePanel;
        Text lifeTitle, lifeBody, lifeStatus, lifeQuest;
        InputField lifeInput;
        Button lifeSend, lifeClose;
        Image sleepShade;
        readonly Button[] lifeOptions = new Button[7];
        readonly Image[] lifeItemImages = new Image[7];
        readonly Text[] lifeItemActions = new Text[7];
        int lifeRevision = -1;
        void BuildLifeHud()
        {
            lifeStatus = Legible(Label(root, "", 0, 0, 440, 32, 17, white, FontStyle.Bold));
            Right(lifeStatus.rectTransform, 32, 312, 440, 32);
            lifeStatus.alignment = TextAnchor.MiddleRight;
            lifeQuest = Legible(Label(root, "", 0, 0, 440, 88, 16, mint));
            Right(lifeQuest.rectTransform, 32, 354, 440, 88);
            lifeQuest.alignment = TextAnchor.UpperRight;
            lifePanel = Panel(root, "City life", 0, 0, 1040, 690, new Color(.025f, .06f, .085f, 1)).gameObject;
            Center(lifePanel.GetComponent<RectTransform>(), 1040, 690);
            lifeTitle = Label(lifePanel.transform, "", 36, 26, 820, 44, 29, white, FontStyle.Bold);
            lifeBody = Label(lifePanel.transform, "", 36, 91, 968, 320, 20, white);
            lifeBody.verticalOverflow = VerticalWrapMode.Truncate;
            lifeClose = MakeButton(lifePanel.transform, "닫기  ESC", 870, 28, 132, 38, () => CityLife.Instance.Dismiss());
            for (int i = 0; i < lifeOptions.Length; i++)
            {
                int selected = i;
                lifeOptions[i] = MakeButton(lifePanel.transform, "", 36 + (i % 2) * 492, 420 + (i / 2) * 52, 474, 44, () =>
                {
                    var life = CityLife.Instance;
                    // Like E, the first press on a story line that is still typing only completes it.
                    if (life && selected < life.Options.Count && !(life.Mode == "story" && RevealDialogue()))
                        life.Options[selected].action();
                });
                lifeOptions[i].GetComponentInChildren<Text>().fontSize = 16;
                var icon = new GameObject("Shop item image", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(lifeOptions[i].transform, false);
                lifeItemImages[i] = icon.GetComponent<Image>();
                lifeItemImages[i].preserveAspect = true;
                lifeItemImages[i].raycastTarget = false;
                PlaceLifeElement(lifeItemImages[i].rectTransform, 16, 23, 200, 200);
                icon.SetActive(false);
                lifeItemActions[i] = Label(lifeOptions[i].transform, "", 232, 170, 222, 28, 16, mint);
                lifeItemActions[i].gameObject.SetActive(false);
            }

            var field = Panel(lifePanel.transform, "Reply", 36, 616, 790, 45, new Color(.07f, .13f, .17f));
            field.raycastTarget = true;
            lifeInput = field.gameObject.AddComponent<InputField>();
            lifeInput.textComponent = Label(field.transform, "", 13, 9, 765, 28, 18, white);
            lifeInput.placeholder = Label(field.transform, "서하의 말을 입력하세요…", 13, 9, 765, 28, 18, muted);
            lifeInput.characterLimit = 350;
            lifeInput.lineType = InputField.LineType.SingleLine;
            lifeSend = MakeButton(lifePanel.transform, "말하기  ↵", 846, 616, 156, 45, LifeSendClicked);
            lifePanel.SetActive(false);
            sleepShade = Panel(root, "Sleep fade", 0, 0, 1600, 900, Color.clear);
            Stretch(sleepShade.rectTransform);
            sleepShade.gameObject.SetActive(false);
            BuildConversationPresentation();
        }

        /// <summary>The send button doubles as "cancel reply" while an AI answer is pending; Enter only ever sends.</summary>
        void LifeSendClicked()
        {
            var life = CityLife.Instance;
            if (life && life.Sending)
            {
                // A mouse click leaves this button selected, so Enter arrives here as a UI submit; it must not drop the pending reply.
                var keyboard = Keyboard.current;
                if (life.CanCancelReply && !(keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))) life.CancelReply();
                return;
            }
            SendLifeText();
        }

        void SendLifeText()
        {
            var life = CityLife.Instance;
            if (!life || life.Sending || string.IsNullOrWhiteSpace(lifeInput.text))
                return;
            string text = lifeInput.text;
            lifeInput.text = "";
            life.Send(text);
        }

        void UpdateLifeHud()
        {
            var life = CityLife.Instance;
            if (!life)
                return;
            if (!lifeStatus)
                BuildLifeHud();
            bool display = !game.Title && !game.Dead;
            lifeStatus.gameObject.SetActive(display);
            lifeQuest.gameObject.SetActive(display && !game.Dialogue);
            string time = $"{Mathf.FloorToInt(LifeState.Hour):00}:{Mathf.FloorToInt(LifeState.Hour % 1 * 60):00}";
            int level = WantedSystem.Level;
            lifeStatus.text = $"DAY {LifeState.Day:00}   {time}   ·   {LifeState.Credits:N0} C" + (level > 0 ? "   " + new string ('★', level) + new string ('☆', 5 - level) : "");
            lifeStatus.color = level > 0 ? SignalEffects.Red : white;
            var errand = LifeState.Errand;
            lifeQuest.text = level > 0 ? (WantedSystem.Instance && WantedSystem.Instance.Seen ? "추격 중 · 시야를 끊고 숨으세요" : "수색 중 · " + Mathf.CeilToInt(Mathf.Max(0, WantedSystem.Instance.EscapeTime - LifeState.HiddenSeconds)) + "초 후 수배 해제") : "";
            if (errand != null && errand.accepted && !errand.completed)
            {
                lifeQuest.text += "\n숨은 의뢰 · " + errand.title + " / " + UrbanCatalog.Name(errand.site) + (errand.kind == "rooftop" ? " 옥상" : "");
                if (game.stage == StageId.UrbanCity)
                {
                    var p = errand.kind == "rooftop" ? CityRooftop.Destination(errand.site) : UrbanCatalog.Door(errand.site);
                    lifeQuest.text += " · " + Vector3.Distance(game.Player.transform.position, p).ToString("0") + " m";
                }
            }

            lifeQuest.text = KeyHints.Apply(lifeQuest.text);
            civicMap.SetActive(!life.Panorama && !game.Title);
            area.transform.parent.gameObject.SetActive(!life.Panorama);
            weapon.transform.parent.gameObject.SetActive(!life.Panorama && !(UrbanSimulation.Instance && UrbanSimulation.Instance.Driving));
            if (life.Panorama)
            {
                lifeStatus.gameObject.SetActive(false);
                lifeQuest.text = KeyHints.Apply("도시 전망 · ← → 회전 / ↑ ↓ 높이 / P 돌아가기");
                CenterBottom(lifeQuest.rectTransform, 65, 900, 30);
                lifeQuest.alignment = TextAnchor.MiddleCenter;
                mainRouteText.gameObject.SetActive(false);
                mainRoutePin.gameObject.SetActive(false);
                if (questLine)
                    questLine.enabled = false;
                routeRibbon?.Show(false);if(navigationCard)navigationCard.SetActive(false);
                prompt.text = "";
            }
            else
            {
                Right(lifeQuest.rectTransform, 32, 354, 440, 88);
                lifeQuest.alignment = TextAnchor.UpperRight;
            }

            bool open = life.Mode.Length > 0 && game.Dialogue;
            lifePanel.SetActive(open);
            if (open)
            {
                dialogueBox.SetActive(false);
                life.TextFocused = lifeInput.isFocused;
                ButtonText(lifeClose, KeyHints.Apply("닫기  ESC"));
                bool changed = lifeRevision != life.Revision;
                if (changed)
                {
                    lifeRevision = life.Revision;
                    storyReveal = default; // every story panel is a new line, even when its text repeats
                    lifeTitle.text = life.Heading;
                    lifeBody.text = life.Body;
                    bool illustrated = life.Options.Exists(option => ShopItemArt.Get(option.image));
                    PlaceLifeElement(lifeBody.rectTransform, 36, 91, 968, illustrated ? 78 : 320);
                    int cardIndex = 0, plainIndex = 0;
                    for (int i = 0; i < lifeOptions.Length; i++)
                    {
                        lifeOptions[i].gameObject.SetActive(i < life.Options.Count);
                        if (i >= life.Options.Count) continue;
                        var option = life.Options[i];
                        var sprite = ShopItemArt.Get(option.image);
                        var label = lifeOptions[i].GetComponentInChildren<Text>();
                        lifeItemImages[i].gameObject.SetActive(sprite);
                        lifeItemImages[i].sprite = sprite;
                        lifeItemActions[i].gameObject.SetActive(sprite);
                        if (sprite)
                        {
                            PlaceLifeElement(lifeOptions[i].GetComponent<RectTransform>(), 36 + cardIndex % 2 * 492, 190 + cardIndex / 2 * 260, 474, 246);
                            PlaceLifeElement(label.rectTransform, 232, 35, 222, 125);
                            label.alignment = TextAnchor.MiddleLeft;
                            label.fontSize = 20;
                            label.text = option.label.Replace(" / ", "\n");
                            lifeItemActions[i].text = life.Mode == "wardrobe" ? "선택" : "구매";
                            cardIndex++;
                        }
                        else
                        {
                            PlaceLifeElement(lifeOptions[i].GetComponent<RectTransform>(), 36 + plainIndex % 2 * 492, (illustrated ? 500 : 420) + plainIndex / 2 * 52, 474, 44);
                            PlaceLifeElement(label.rectTransform, 10, 0, 454, 44);
                            label.alignment = TextAnchor.MiddleCenter;
                            label.fontSize = 16;
                            label.text = option.label;
                            plainIndex++;
                        }
                    }
                }

                ConversationLayout(life);
                if (life.Mode == "story") lifeBody.text = Reveal(life.Body, ref storyReveal);
                bool talk = life.Mode == "talk";
                lifeInput.gameObject.SetActive(talk);
                lifeSend.gameObject.SetActive(talk);
                lifeSend.interactable = !life.Sending || life.CanCancelReply;
                lifeInput.interactable = !life.Sending;
                if (talk && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                    SendLifeText();
                // Talk mode keeps keyboard focus free for the reply field unless a gamepad is in use.
                SelectLifeOption(changed && (!talk || KeyHints.UsingGamepad));
                lifeBack = !lifeInput.isFocused && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            }
            else
            {
                life.TextFocused = false;
                storyReveal = default;
            }
            sleepShade.gameObject.SetActive(life.SleepFade > 0);
            sleepShade.color = new Color(.005f, .012f, .025f, life.SleepFade);
        }

        bool lifeBack;
        /// <summary>Gamepad B closes the life panel after every Update has run. B is also Dash: closing during Update could unblock GameDirector later in the same frame and turn the press into a dash.</summary>
        void LateUpdate()
        {
            if (!lifeBack)
                return;
            lifeBack = false;
            var life = CityLife.Instance;
            if (life && life.Mode.Length > 0 && game.Dialogue)
                life.Dismiss();
        }

        /// <summary>Gives gamepad and keyboard navigation a starting point: the first option, unless the selection is already a live control in this panel.</summary>
        void SelectLifeOption(bool force)
        {
            var events = EventSystem.current;
            if (!events)
                return;
            var current = events.currentSelectedGameObject;
            if (current && current.activeInHierarchy && current.transform.IsChildOf(lifePanel.transform))
                return;
            var pad = Gamepad.current;
            if (!force && !(pad != null && (pad.leftStick.ReadValue().sqrMagnitude > .25f || pad.dpad.ReadValue().sqrMagnitude > .25f)))
                return;
            foreach (var option in lifeOptions)
                if (option.gameObject.activeInHierarchy && option.interactable)
                {
                    events.SetSelectedGameObject(option.gameObject);
                    return;
                }
        }

        static void PlaceLifeElement(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
