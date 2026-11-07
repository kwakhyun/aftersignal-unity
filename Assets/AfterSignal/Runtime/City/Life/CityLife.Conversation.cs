using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityLife
    {
        // Paid AI replies per game run and per conversation. The NPC's first line is always local and free.
        public const int SessionReplyBudget = 60, ConversationReplyBudget = 20;
        const int HistoryTurns = 8;
        static int sessionReplies;
        int conversationReplies;
        string bodyBeforeSending;
        public float SendingStarted { get; private set; }
        public int RepliesLeft => Mathf.Max(0, Mathf.Min(SessionReplyBudget - sessionReplies, ConversationReplyBudget - conversationReplies));
        public bool CanCancelReply => Sending && Mode == "talk";

        public void Talk(CityNpc npc)
        {
            npc.GetComponent<NeighborBond>()?.Greet();
            conversation.Cancel();
            Speaker = npc;
            history.Clear();
            conversationReplies = 0;
            Sending = false;
            // The NPC speaks first from the local banks; Seoha never greets automatically and no request is sent yet.
            string first = NpcText.Clean(NpcOpening.Line(npc), 400);
            if (first.Length == 0)
                first = "...";
            Panel("talk", npc.displayName, first);
            history.Add(new NpcLine { role = "assistant", content = first });
        }

        public void Send(string text)
        {
            if (Sending || !Speaker)
                return;
            text = NpcText.Clean(text, 350);
            if (text.Length > 0)
                RequestReply(text);
        }

        // UI hook (e.g. a cancel button while Sending): aborts the pending AI reply, adds nothing to history and re-enables input.
        public void CancelReply()
        {
            if (!Sending)
                return;
            if (!conversation.Cancel())
                Refund();
            Sending = false;
            Body = bodyBeforeSending ?? "";
            Revision++;
            game.Toast("답변 기다리기를 취소했습니다.", 2);
        }

        void Refund()
        {
            sessionReplies = Mathf.Max(0, sessionReplies - 1);
            conversationReplies = Mathf.Max(0, conversationReplies - 1);
        }

        void RequestReply(string text)
        {
            var speaker = Speaker;
            string said = "서하: " + text + "\n\n";
            if (RepliesLeft <= 0)
            {
                Options.Clear();
                Body = said + NpcText.Clean(NpcOpening.Reply(speaker, "budget"), 600) + "\n\n" + (sessionReplies >= SessionReplyBudget
                    ? "이번 실행의 AI 대화 한도(" + SessionReplyBudget + "회)를 모두 써서 로컬 대사로 이어갑니다. 게임을 다시 시작하면 초기화됩니다."
                    : "이 대화의 AI 답변 한도(" + ConversationReplyBudget + "회)에 도달했습니다. 다시 말을 걸면 새 대화로 시작합니다.");
                Revision++;
                return;
            }

            bodyBeforeSending = Body;
            Sending = true;
            SendingStarted = Time.unscaledTime;
            Body = said + "…";
            Revision++;
            sessionReplies++;
            conversationReplies++;
            var line = new NpcLine { role = "user", content = text };
            var turns = new List<NpcLine>(history) { line };
            while (turns.Count > HistoryTurns)
                turns.RemoveAt(0);
            conversation.Request(speaker, turns.ToArray(), result =>
            {
                if (!result.charged)
                    Refund();
                if (Mode != "talk" || Speaker != speaker)
                    return;
                Sending = false;
                string reply = NpcText.Clean(result.reply, 1300);
                if (reply.Length == 0)
                    reply = "...";
                Body = said + reply;
                // Failures and local stand-ins never enter history, so they cannot pollute later context.
                if (!result.failed)
                {
                    history.Add(line);
                    history.Add(new NpcLine { role = "assistant", content = reply.Length > 600 ? reply.Substring(0, 600) : reply });
                    while (history.Count > HistoryTurns)
                        history.RemoveAt(0);
                }
                Options.Clear();
                string status = NpcText.Clean(result.status, 200);
                if (status.Length > 0)
                    Body += "\n\n" + status;
                if (result.charged && !result.failed && RepliesLeft <= 3)
                    Body += "\n\nAI 대화 남은 횟수 " + RepliesLeft + "회";
                if (NpcConversation.Valid(result.quest) && (LifeState.Errand == null || LifeState.Errand.completed))
                {
                    offer = result.quest;
                    offer.giver = speaker.displayName;
                    offer.reward = Mathf.Clamp(offer.reward, 120, 350);
                    offer.title = Trim(NpcText.Clean(offer.title, 64), 64);
                    offer.description = Trim(NpcText.Clean(offer.description, 180), 180);
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
