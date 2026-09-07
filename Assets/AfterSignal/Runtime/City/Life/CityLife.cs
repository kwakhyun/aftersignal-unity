using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterSignal
{
    public sealed partial class CityLife : MonoBehaviour
    {
        public static CityLife Instance { get; private set; }
        public string Mode { get; private set; } = "";
        public string Heading { get; private set; } = "";
        public string Body { get; private set; } = "";
        public CityNpc Speaker { get; private set; }
        public bool Sending { get; private set; }

        public bool TextFocused;
        public float SleepFade { get; private set; }
        public int Revision { get; private set; }
        public bool Panorama { get; private set; }

        public float PanoramaYaw = -15, PanoramaPitch = 14;
        public readonly List<LifeOption> Options = new List<LifeOption>();
        readonly List<NpcLine> history = new List<NpcLine>();
        GameDirector game;
        NpcConversation conversation;
        HiddenErrand offer;
        float saveClock;
        public void Initialize(GameDirector owner)
        {
            Instance = this;
            game = owner;
            LifeState.Load();
            conversation = gameObject.AddComponent<NpcConversation>();
            game.Player.gameObject.AddComponent<ActorWardrobe>();
            gameObject.AddComponent<WantedSystem>().Initialize(game);
            gameObject.AddComponent<CityClock>().Initialize(game);
            Camera.main.gameObject.AddComponent<CameraOcclusion>();
            BindResidents();
        }

        void BindResidents()
        {
            var points = FindObjectsByType<InteractionPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var visuals = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var p in points)
            {
                bool quest = p.kind == InteractionKind.Noa || p.kind == InteractionKind.QuestGiver || p.kind == InteractionKind.MinJob || p.kind == InteractionKind.YunJob;
                if (!quest && p.kind != InteractionKind.Citizen && p.kind != InteractionKind.UrbanService && p.kind != InteractionKind.Furniture && p.kind != InteractionKind.Rest)
                    continue;
                SpriteRenderer nearest = null;
                float distance = 2.3f;
                foreach (var v in visuals)
                {
                    if (v.GetComponentInParent<PixelActor>() || v.GetComponent<CityNpc>())
                        continue;
                    float d = Vector3.Distance(v.transform.position, p.transform.position - Vector3.up * 1.2f);
                    if (d < distance)
                    {
                        distance = d;
                        nearest = v;
                    }
                }

                if (!nearest)
                    continue;
                var npc = nearest.gameObject.AddComponent<CityNpc>();
                npc.point = p;
                npc.fixedQuest = quest;
                var walker = nearest.GetComponent<ResidentWalker>();
                string role = p.title.Contains(" · ") ? p.title.Substring(p.title.LastIndexOf(" · ", StringComparison.Ordinal) + 3) : walker ? Role(walker.role) : "도시 주민";
                if (game.stage == StageId.UrbanInterior && UrbanCatalog.IsHotel(UrbanCatalog.Current))
                {
                    role = role == "주민" || role == "방문객" ? "호텔 투숙객" : role == "관리사" || role == "입주 상담사" ? "호텔 프런트 직원" : role;
                }

                npc.identity = "authored-" + (int)game.stage + "-" + (game.stage == StageId.UrbanInterior ? UrbanCatalog.Current : 0) + "-" + p.title;
                npc.Configure(Mathf.Abs(p.title.GetHashCode() % 10000), role, p.title, game.stage == StageId.UrbanInterior && UrbanCatalog.IsHotel(UrbanCatalog.Current) ? "애프터뷰 호텔의 " + role + ". 옥상에서 보는 도시 야경과 따뜻한 숙박 서비스를 이야기한다." : p.dialogue);
            }

            foreach (var walker in FindObjectsByType<ResidentWalker>(FindObjectsSortMode.None))
                if (!walker.GetComponent<CityNpc>())
                    walker.gameObject.AddComponent<CityNpc>().Configure(walker.GetInstanceID(), Role(walker.role), walker.name);
        }

        static string Role(string role) => role == "medic" ? "의료진" : role == "teacher" ? "교사" : role == "commander" ? "안전 담당자" : "시설 직원";
        public bool Interact(InteractionPoint point)
        {
            if (point.kind == InteractionKind.Wardrobe || game.stage == StageId.Residence && point.title.Contains("옷장"))
            {
                Wardrobe();
                return true;
            }

            if (point.kind == InteractionKind.Sleep || game.stage == StageId.Residence && point.title.Contains("침대"))
            {
                if (game.stage == StageId.UrbanInterior)
                    Services();
                else
                    SleepMenu();
                return true;
            }

            if (point.kind == InteractionKind.Viewpoint)
            {
                Panorama = true;
                game.Toast("옥상 전망 · ← → 시점 회전 / ↑ ↓ 높이 / V 돌아가기", 5);
                return true;
            }

            if (point.kind == InteractionKind.HiddenErrand)
            {
                CompleteErrand();
                return true;
            }

            if (point.kind == InteractionKind.UrbanService || point.kind == InteractionKind.LifeService)
            {
                Services();
                return true;
            }

            if (point.npc && !point.npc.fixedQuest)
            {
                point.npc.Talk(game);
                return true;
            }

            return false;
        }

        public void BeforeInput(ref ControlFrame control, float dt)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame && game.stage == StageId.UrbanCity && game.Player.Grounded && game.Player.transform.position.y > 10)
                Panorama = !Panorama;
            if (!Panorama)
                return;
            if (keyboard != null)
            {
                PanoramaYaw += (keyboard.rightArrowKey.isPressed ? 1 : 0) * dt * 36 - (keyboard.leftArrowKey.isPressed ? 1 : 0) * dt * 36;
                PanoramaPitch = Mathf.Clamp(PanoramaPitch + (keyboard.downArrowKey.isPressed ? 1 : 0) * dt * 20 - (keyboard.upArrowKey.isPressed ? 1 : 0) * dt * 20, -12, 38);
            }

            control = ControlFrame.Empty;
        }

        void Update()
        {
            if (!game || !game.Ready)
                return;
            if (!game.Blocked)
            {
                saveClock -= Time.deltaTime;
                if (saveClock <= 0)
                {
                    saveClock = 20;
                    LifeState.Save();
                }
            }

            if (!game.Dialogue && Mode.Length > 0)
                Close();
        }

        void Panel(string mode, string heading, string body)
        {
            Mode = mode;
            Heading = heading;
            Body = body;
            Options.Clear();
            Revision++;
            game.ShowDialogue(heading, body.Length > 0 ? body : " ");
        }

        void Option(string label, Action action)
        {
            Options.Add(new LifeOption { label = label, action = action });
        }

        public void Close()
        {
            conversation?.Cancel();
            Mode = "";
            Speaker = null;
            Sending = false;
            TextFocused = false;
            offer = null;
            Options.Clear();
            history.Clear();
            Revision++;
        }

        public void Dismiss()
        {
            if (Mode == "sleeping")
                return;
            Close();
            game.CloseDialogue();
        }

        void OnApplicationQuit()
        {
            LifeState.Save();
        }

        void OnDestroy()
        {
            LifeState.Save();
            if (Instance == this)
                Instance = null;
        }
    }
}
