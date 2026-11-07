using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AfterSignal
{
    public sealed class NpcConversation : MonoBehaviour
    {
        // Tools/NpcGateway.py: 18 s provider deadline, 1 request/s, 2 concurrent. The client waits a little longer and spaces its requests to match.
        public const int ClientTimeout = 22, MaxRetries = 2;
        public const float MinInterval = 1.1f;

        [Serializable]
        sealed class RequestBody
        {
            public string name, occupation, personality, context, place;
            public NpcLine[] messages;
            public string[] sites;
            public int day, wanted, site;
            public float hour;
            public bool allowQuest, opening;
        }

        [Serializable]
        public sealed class Reply
        {
            public string reply, status, error;
            public HiddenErrand quest;
            public float retry_after;
            public int provider_status;
            // failed: a local stand-in line that must never enter the conversation history. charged: the request may have reached the paid provider.
            [NonSerialized] public bool failed, charged;
        }

        static float lastDispatch = -10;
        static string[] siteNames;
        UnityWebRequest active;
        Coroutine pending;
        bool charged;
        public bool Pending => pending != null;

        // Stops the pending reply without calling back. Returns true when the aborted request may already have reached the provider.
        public bool Cancel()
        {
            bool sent = charged;
            charged = false;
            if (pending != null)
                StopCoroutine(pending);
            pending = null;
            if (active != null)
            {
                active.Abort();
                active.Dispose();
            }
            active = null;
            return sent;
        }

        public void Request(CityNpc npc, NpcLine[] history, Action<Reply> done)
        {
            Cancel();
            if(history!=null&&history.Length>0&&FacilityGuide.TryAnswer(npc,history[history.Length-1].content,out var guide))
            {done(new Reply{reply=NpcText.Clean(guide,1300),status="시설 이용 안내"});return;}
            pending = StartCoroutine(Send(npc, history, done));
        }

        public static bool Valid(HiddenErrand q) => q != null && !string.IsNullOrWhiteSpace(q.title) && (q.kind == "delivery" || q.kind == "visit" || q.kind == "rooftop") && q.site >= 0 && q.site < UrbanCatalog.SiteCount;
        static string Bound(string s, int max) => string.IsNullOrEmpty(s) ? "" : s.Length > max ? s.Substring(0, max) : s;
        static string[] Sites()
        {
            if (siteNames == null)
            {
                siteNames = new string[UrbanCatalog.SiteCount];
                for (int i = 0; i < siteNames.Length; i++)
                    siteNames[i] = UrbanCatalog.Name(i);
            }
            return siteNames;
        }

        IEnumerator Send(CityNpc npc, NpcLine[] history, Action<Reply> done)
        {
            var game = GameDirector.Instance;
            var data = new RequestBody
            {
                name = npc.displayName,
                occupation = npc.occupation,
                personality = Bound(npc.personality, 900),
                context = Bound(NpcOpening.Context(npc)+"\n"+npc.context+"\n"+FacilityGuide.Knowledge(npc), 900),
                place = NpcOpening.Place(npc),
                opening = history==null||history.Length==0,
                messages = history,
                sites = Sites(),
                day = LifeState.Day,
                hour = LifeState.Hour,
                wanted = WantedSystem.Level,
                site = game.stage == StageId.UrbanInterior ? UrbanCatalog.Current : -1,
                allowQuest = (LifeState.Errand == null || LifeState.Errand.completed) && CivicWorld.Exploration(game.stage)
            };
            string endpoint = Environment.GetEnvironmentVariable("AFTERSIGNAL_NPC_URL");
            if (string.IsNullOrWhiteSpace(endpoint))
                endpoint = "http://127.0.0.1:8766/dialogue";
            Reply result = null;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && !uri.IsLoopback))
                result = Failure(npc, "offline", "대화 서버 주소는 HTTPS 또는 로컬 주소여야 합니다.", false);
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            for (int attempt = 0; result == null; attempt++)
            {
                // Match the gateway's 1 request/s limit instead of provoking 429s.
                float wait = MinInterval - (Time.realtimeSinceStartup - lastDispatch);
                if (wait > 0)
                    yield return new WaitForSecondsRealtime(wait);
                lastDispatch = Time.realtimeSinceStartup;
                charged = true;
                active = new UnityWebRequest(endpoint, "POST") { uploadHandler = new UploadHandlerRaw(body), downloadHandler = new DownloadHandlerBuffer(), timeout = ClientTimeout };
                active.SetRequestHeader("Content-Type", "application/json");
                yield return active.SendWebRequest();
                var outcome = active.result;
                long code = active.responseCode;
                string text = active.downloadHandler.text;
                active.Dispose();
                active = null;
                var parsed = Parse(text);
                if (outcome == UnityWebRequest.Result.Success)
                {
                    result = parsed != null && NpcText.Clean(parsed.reply, 1300).Length > 0 ? parsed : Failure(npc, "signal", "AI 응답 형식이 올바르지 않습니다.", true);
                    result.charged = true;
                }
                else if (code == 429 && attempt < MaxRetries)
                {
                    // Rejected before any provider call: retrying costs nothing.
                    charged = false;
                    yield return new WaitForSecondsRealtime(Mathf.Clamp(parsed != null && parsed.retry_after > 0 ? parsed.retry_after : .6f * (attempt + 1), .3f, 3));
                }
                else
                    result = Failed(npc, outcome, code, parsed, Time.realtimeSinceStartup - lastDispatch);
            }

            result.reply = NpcText.Clean(result.reply, 1300);
            result.status = NpcText.Clean(result.status, 200);
            if (result.failed || !Valid(result.quest))
                result.quest = null;
            else
            {
                result.quest.title = NpcText.Clean(result.quest.title, 64);
                result.quest.description = NpcText.Clean(result.quest.description, 180);
            }
            pending = null;
            charged = false;
            done(result);
        }

        static Reply Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            try
            {
                return JsonUtility.FromJson<Reply>(text);
            }
            catch
            {
                return null;
            }
        }

        static Reply Failure(CityNpc npc, string reason, string status, bool charged) => new Reply { reply = NpcOpening.Reply(npc, reason), status = status, failed = true, charged = charged };

        // Error bodies from Tools/NpcGateway.py: 429 {error,retry_after} · 503 {error,reply,status} · 502 {error,provider_status?} · 504 {error} · 4xx {error}.
        static Reply Failed(CityNpc npc, UnityWebRequest.Result outcome, long code, Reply parsed, float elapsed)
        {
            if (code == 503 && parsed != null && !string.IsNullOrWhiteSpace(parsed.reply))
                return new Reply { reply = parsed.reply, status = "AI 대화 연결 안 됨" + (string.IsNullOrWhiteSpace(parsed.status) ? "" : " · " + parsed.status), failed = true };
            if (code == 503)
                return Failure(npc, "offline", "AI 대화 서버를 지금 사용할 수 없습니다.", false);
            if (code == 429)
                return Failure(npc, "busy", "대화 요청이 몰려 답을 미뤘습니다 · 잠시 후 다시 말해 주세요.", false);
            if (code == 504)
                return Failure(npc, "signal", "AI 응답이 늦어 이번 답은 건너뜁니다 · 다시 말해 주세요.", true);
            if (code == 502 || outcome == UnityWebRequest.Result.DataProcessingError)
                return Failure(npc, "signal", "AI 응답을 받지 못했습니다" + (parsed != null && parsed.provider_status > 0 ? " (제공자 " + parsed.provider_status + ")" : "") + " · 잠시 후 다시 말해 주세요.", true);
            if (code >= 400)
                return Failure(npc, "signal", "대화 서버가 요청을 거절했습니다 (" + code + ") · 게이트웨이 버전을 확인하세요.", false);
            if (elapsed >= ClientTimeout - 1)
                return Failure(npc, "signal", "AI 응답이 " + ClientTimeout + "초 안에 오지 않았습니다 · 다시 말해 주세요.", true);
            return Failure(npc, "offline", "로컬 대화 서버에 연결하지 못했습니다 · Tools/NpcGateway.py 실행을 확인하세요.", false);
        }

        void OnDestroy()
        {
            Cancel();
        }
    }
}
