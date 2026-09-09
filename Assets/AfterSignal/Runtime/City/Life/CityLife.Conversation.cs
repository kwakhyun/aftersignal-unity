using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public float SendingStarted { get; private set; }

        public void Talk(CityNpc npc)
        {
            npc.GetComponent<NeighborBond>()?.Greet();
            Speaker = npc;
            history.Clear();
            Panel("talk", npc.displayName, "대화를 시작합니다…");
            Sending = false;
            RequestReply(null);
        }

        public void Send(string text)
        {
            if (Sending || !Speaker || string.IsNullOrWhiteSpace(text))
                return;
            text = text.Trim();
            if (text.Length > 350)
                text = text.Substring(0, 350);
            RequestReply(text);
        }

        void RequestReply(string text)
        {
            bool opening=string.IsNullOrEmpty(text);
            Sending = true;
            SendingStarted = Time.unscaledTime;
            Body = opening ? "주변을 살피며 말을 고르는 중…" : "서하: " + text + "\n\n…";
            Revision++;
            if(!opening)history.Add(new NpcLine { role = "user", content = text });
            while (history.Count > 8)
                history.RemoveAt(0);
            var speaker = Speaker;
            conversation.Request(speaker, history.ToArray(), result =>
            {
                if (Mode != "talk" || Speaker != speaker)
                    return;
                Sending = false;
                Body = (opening ? "" : "서하: " + text + "\n\n") + result.reply;
                history.Add(new NpcLine { role = "assistant", content = result.reply });
                Options.Clear();
                if (!string.IsNullOrEmpty(result.status))
                    Body += "\n\n" + result.status;
                if (result.quest != null && (LifeState.Errand == null || LifeState.Errand.completed) && result.quest.site >= 0 && result.quest.site < UrbanCatalog.SiteCount)
                {
                    offer = result.quest;
                    offer.giver = speaker.displayName;
                    offer.reward = Mathf.Clamp(offer.reward, 120, 350);
                    offer.title = Trim(offer.title, 64);
                    offer.description = Trim(offer.description, 180);
                    if (offer.kind != "rooftop" && offer.kind != "visit")
                        offer.kind = "delivery";
                    Body += "\n\n숨은 의뢰: " + offer.title + "\n" + UrbanCatalog.Name(offer.site) + " · 보수 " + offer.reward + " C";
                    Option("숨은 의뢰 수락", () =>
                    {
                        offer.accepted = true;
                        LifeState.Errand = offer;
                        LifeState.Save();
                        game.Toast("의뢰 수락 · " + offer.title, 5);
                        InstallErrandMarker();
                        Dismiss();
                    });
                }

                Revision++;
            });
        }

        static string Trim(string s, int max) => string.IsNullOrEmpty(s) ? "도시의 작은 부탁" : s.Length > max ? s.Substring(0, max) : s;
    }
}
