using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        GameObject lifePanel;
        Text lifeTitle, lifeBody, lifeStatus, lifeQuest;
        InputField lifeInput;
        Button lifeSend;
        Image sleepShade;
        readonly Button[] lifeOptions = new Button[7];
        int lifeRevision = -1;
        void BuildLifeHud()
        {
            lifeStatus = Label(root, "", 0, 0, 440, 32, 17, white, FontStyle.Bold);
            Right(lifeStatus.rectTransform, 32, 312, 440, 32);
            lifeStatus.alignment = TextAnchor.MiddleRight;
            lifeQuest = Label(root, "", 0, 0, 440, 88, 16, mint);
            Right(lifeQuest.rectTransform, 32, 354, 440, 88);
            lifeQuest.alignment = TextAnchor.UpperRight;
            lifePanel = Panel(root, "City life", 0, 0, 1040, 690, new Color(.025f, .06f, .085f, .99f)).gameObject;
            Center(lifePanel.GetComponent<RectTransform>(), 1040, 690);
            lifeTitle = Label(lifePanel.transform, "", 36, 26, 820, 44, 29, white, FontStyle.Bold);
            lifeBody = Label(lifePanel.transform, "", 36, 91, 968, 320, 20, white);
            lifeBody.verticalOverflow = VerticalWrapMode.Truncate;
            MakeButton(lifePanel.transform, "닫기  ESC", 870, 28, 132, 38, () => CityLife.Instance.Dismiss());
            for (int i = 0; i < lifeOptions.Length; i++)
            {
                int selected = i;
                lifeOptions[i] = MakeButton(lifePanel.transform, "", 36 + (i % 2) * 492, 420 + (i / 2) * 52, 474, 44, () =>
                {
                    var life = CityLife.Instance;
                    if (life && selected < life.Options.Count)
                        life.Options[selected].action();
                });
                lifeOptions[i].GetComponentInChildren<Text>().fontSize = 16;
            }

            var field = Panel(lifePanel.transform, "Reply", 36, 616, 790, 45, new Color(.07f, .13f, .17f));
            field.raycastTarget = true;
            lifeInput = field.gameObject.AddComponent<InputField>();
            lifeInput.textComponent = Label(field.transform, "", 13, 9, 765, 28, 18, white);
            lifeInput.placeholder = Label(field.transform, "서하의 말을 입력하세요…", 13, 9, 765, 28, 18, muted);
            lifeInput.characterLimit = 350;
            lifeInput.lineType = InputField.LineType.SingleLine;
            lifeSend = MakeButton(lifePanel.transform, "말하기  ↵", 846, 616, 156, 45, SendLifeText);
            lifePanel.SetActive(false);
            sleepShade = Panel(root, "Sleep fade", 0, 0, 1600, 900, Color.clear);
            Stretch(sleepShade.rectTransform);
            sleepShade.gameObject.SetActive(false);
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

            civicMap.SetActive(!life.Panorama && !game.Title);
            area.transform.parent.gameObject.SetActive(!life.Panorama);
            weapon.transform.parent.gameObject.SetActive(!life.Panorama && !(UrbanSimulation.Instance && UrbanSimulation.Instance.Driving));
            if (life.Panorama)
            {
                lifeStatus.gameObject.SetActive(false);
                lifeQuest.text = "도시 전망 · ← → 회전 / ↑ ↓ 높이 / V 돌아가기";
                CenterBottom(lifeQuest.rectTransform, 65, 900, 30);
                lifeQuest.alignment = TextAnchor.MiddleCenter;
                mainRouteText.gameObject.SetActive(false);
                mainRoutePin.gameObject.SetActive(false);
                if (questLine)
                    questLine.enabled = false;
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
                if (lifeRevision != life.Revision)
                {
                    lifeRevision = life.Revision;
                    lifeTitle.text = life.Heading;
                    lifeBody.text = life.Body;
                    for (int i = 0; i < lifeOptions.Length; i++)
                    {
                        lifeOptions[i].gameObject.SetActive(i < life.Options.Count);
                        if (i < life.Options.Count)
                            ButtonText(lifeOptions[i], life.Options[i].label);
                    }
                }

                bool talk = life.Mode == "talk";
                lifeInput.gameObject.SetActive(talk);
                lifeSend.gameObject.SetActive(talk);
                lifeSend.interactable = !life.Sending;
                lifeInput.interactable = !life.Sending;
                if (talk && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                    SendLifeText();
            }
            else
                life.TextFocused = false;
            sleepShade.gameObject.SetActive(life.SleepFade > 0);
            sleepShade.color = new Color(.005f, .012f, .025f, life.SleepFade);
        }
    }
}
