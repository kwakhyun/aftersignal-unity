using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AfterSignal
{
    public sealed class NpcConversation : MonoBehaviour
    {
        [Serializable]
        sealed class RequestBody
        {
            public string name, occupation, personality, context, place;
            public NpcLine[] messages;
            public int day, wanted, site;
            public float hour;
            public bool allowQuest;
        }

        [Serializable]
        public sealed class Reply
        {
            public string reply, status;
            public HiddenErrand quest;
        }

        UnityWebRequest active;
        Coroutine pending;
        public void Cancel()
        {
            if (active != null)
                active.Abort();
            if (pending != null)
                StopCoroutine(pending);
            pending = null;
            active?.Dispose();
            active = null;
        }

        public void Request(CityNpc npc, NpcLine[] history, Action<Reply> done)
        {
            Cancel();
            if(history!=null&&history.Length>0&&FacilityGuide.TryAnswer(npc,history[history.Length-1].content,out var guide))
            {done(new Reply{reply=guide,status="시설 이용 안내"});return;}
            pending = StartCoroutine(Send(npc, history, done));
        }

        IEnumerator Send(CityNpc npc, NpcLine[] history, Action<Reply> done)
        {
            var game = GameDirector.Instance;
            var data = new RequestBody
            {
                name = npc.displayName,
                occupation = npc.occupation,
                personality = npc.personality,
                context = npc.context+"\n"+FacilityGuide.Knowledge(npc),
                place = CivicWorld.Title(game.stage),
                messages = history,
                day = LifeState.Day,
                hour = LifeState.Hour,
                wanted = WantedSystem.Level,
                site = game.stage == StageId.UrbanInterior ? UrbanCatalog.Current : -1,
                allowQuest = (LifeState.Errand == null || LifeState.Errand.completed) && CivicWorld.Exploration(game.stage)
            };
            string endpoint = Environment.GetEnvironmentVariable("AFTERSIGNAL_NPC_URL");
            if (string.IsNullOrWhiteSpace(endpoint))
                endpoint = "http://127.0.0.1:8766/dialogue";
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && !uri.IsLoopback))
            {
                done(new Reply { reply = "통신 연결을 확인해 주세요.", status = "대화 서버 주소는 HTTPS 또는 로컬 주소여야 합니다." });
                yield break;
            }

            using (var request = new UnityWebRequest(endpoint, "POST"))
            {
                active = request;
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(data)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 35;
                yield return request.SendWebRequest();
                Reply reply = null;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        reply = JsonUtility.FromJson<Reply>(request.downloadHandler.text);
                    }
                    catch
                    {
                    }
                }

                if (reply == null || string.IsNullOrWhiteSpace(reply.reply))
                    reply = new Reply
                    {
                        reply = "지금은 통신 연결이 닿지 않네요. 잠시 후 다시 이야기해요.",
                        status = "AI 대화 연결 실패 · 로컬 대화 서버와 API 키 설정을 확인하세요."
                    };
                reply.reply = reply.reply.Length > 1300 ? reply.reply.Substring(0, 1300) : reply.reply;
                active = null;
                pending = null;
                done(reply);
            }
        }

        void OnDestroy()
        {
            Cancel();
        }
    }
}
