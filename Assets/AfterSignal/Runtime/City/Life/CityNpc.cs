using UnityEngine;

namespace AfterSignal
{
    public sealed class CityNpc : MonoBehaviour
    {
        public string identity, displayName, occupation, personality, context;
        public int variation;
        public int CashOnHand { get; private set; } = 180;
        public int TakeCash(int amount){int taken=Mathf.Min(CashOnHand,Mathf.Max(0,amount));CashOnHand-=taken;return taken;}
        public InteractionPoint point;
        public bool fixedQuest;
        public float SocialUntil;
        public bool Fleeing=>Time.time<fleeUntil;
        SpriteRenderer sprite;
        WorldActor body;
        float fleeUntil, downTime;
        Vector3 fleeDirection;
        void Start()
        {
            sprite = GetComponent<SpriteRenderer>();
            body = GetComponent<WorldActor>();
        }

        static readonly string[] Names =
        {
            "나린",
            "도윤",
            "해솔",
            "이안",
            "유진",
            "서진",
            "소은",
            "가람",
            "지안",
            "태오",
            "유나",
            "시온",
            "하린",
            "수현",
            "민재",
            "예림",
            "윤슬",
            "서율",
            "진우",
            "다온",
            "연우",
            "보라",
            "은재",
            "채린"
        };
        static readonly string[] Jobs =
        {
            "회사원",
            "배달원",
            "기술자",
            "음악가",
            "여행객",
            "상인",
            "퇴근 중인 간호사",
            "도서관 사서",
            "사진가",
            "대학생",
            "정원사",
            "택시 기사"
        };
        static readonly string[] Temperaments =
        {
            "차분하고 세심하다. 도시의 사소한 변화를 잘 기억한다.",
            "쾌활하고 수다스럽다. 주민들의 일상을 궁금해한다.",
            "무뚝뚝하지만 도움이 필요한 이웃에게 친절하다.",
            "상상력이 풍부하고 도시의 신호를 음악에 비유한다.",
            "현실적이고 꼼꼼하다. 위험한 상황에서는 신중하다.",
            "호기심이 많고 오래된 기억과 도시의 역사를 좋아한다."
        };
        public void Configure(int seed, string role = null, string name = null, string background = null)
        {
            variation = Mathf.Abs(seed) % 24;
            CashOnHand=80+variation*13;
            identity = string.IsNullOrEmpty(identity) ? "resident-" + seed : identity;
            occupation = role ?? Jobs[variation % Jobs.Length];
            displayName = name ?? Names[variation] + " · " + occupation;
            personality = Temperaments[variation % Temperaments.Length];
            context = background ?? "기억을 잃었던 도시 애프터라이트에서 생활하는 시민. 서하는 신호 복원에 참여하는 주민이다.";
            sprite = GetComponent<SpriteRenderer>();
            body = GetComponent<WorldActor>();
            if (!body)
                body = gameObject.AddComponent<WorldActor>();
            body.protectedResident = fixedQuest;
            var hit = GetComponent<CapsuleCollider>();
            if (!hit)
                hit = gameObject.AddComponent<CapsuleCollider>();
            hit.isTrigger = true;
            hit.radius = .38f;
            hit.height = 2.1f;
            hit.center = Vector3.up * 1.05f;
            gameObject.layer = 9;
            if (!point)
            {
                var go = new GameObject("Talk / " + displayName);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.up * 1.2f;
                point = go.AddComponent<InteractionPoint>();
                point.kind = InteractionKind.Citizen;
            }

            point.npc = this;
            if (!fixedQuest)
                point.title = displayName + " · 대화";
            point.radius = 3.1f;
            PeopleArt.Attach(gameObject,PeopleArt.Role(this));
            var look = GetComponent<ActorWardrobe>();
            if (!look)
                look = gameObject.AddComponent<ActorWardrobe>();
            look.citizen = true;
            look.variant = variation;
        }

        public void Talk(GameDirector game)
        {
            if (body && !body.Alive)
            {
                game.Toast("응답이 없습니다.");
                return;
            }

            GetComponent<DirectionalPerson>()?.Face(game.Player.transform.position,6);
            CityLife.Instance?.Talk(this);
        }

        public void Panic(Vector3 danger,float duration)
        {
            if(GetComponent<RescueMedic>())return;
            if(CivilianDefense.Active(this))return;
            if(body&&(body.police||body.military||body.gang||body.monster||body.terrorist||body.Downed))return;
            if(fixedQuest||body&&!body.Alive)return;
            GetComponent<FamilyMember>()?.Group?.Panic(danger,duration);
            fleeUntil=Time.time+duration;fleeDirection=(transform.position-danger).normalized;
            if(fleeDirection.sqrMagnitude<.01f)fleeDirection=Vector3.right;
            var ped=GetComponent<CityPedestrian>();
            if(ped){ped.speed=3.7f;ped.WalkTo(transform.position+fleeDirection*13,false);}
        }
        public void ReactToAttack(bool down, Vector3 force)
        {
            if(body&&(body.police||body.military||body.gang||body.terrorist||body.Downed))return;
            if(!down&&CivilianDefense.Active(this))return;
            fleeUntil = Time.time + (down ? 3600 : 7);
            fleeDirection = new Vector3(force.x, 0, force.z).normalized;
            downTime = down ? 12 : 0;
            var walker = GetComponent<ResidentWalker>();
            if (walker)
            {
                walker.walking = false;
                if (down)
                    walker.enabled = false;
            }
        }

        void Update()
        {
            if(CivilianImpact.Active(this)||CivilianDefense.Active(this))return;
            var g = GameDirector.Instance;
            if (!g || g.Blocked || !sprite || Time.time >= fleeUntil)
                return;
            if (downTime > 0)
            {
                sprite.transform.rotation = Quaternion.Euler(15, 0, 85);
                return;
            }

            if (fixedQuest || GetComponent<CityPedestrian>() || GetComponent<FamilyMember>())
                return;
            var step = fleeDirection * Time.deltaTime * 2.8f;
            if (!Physics.Raycast(transform.position + Vector3.up, step.normalized, step.magnitude + .6f, 1, QueryTriggerInteraction.Ignore) && Physics.Raycast(transform.position + step + Vector3.up, Vector3.down, 1.5f, 1, QueryTriggerInteraction.Ignore))
                transform.position += step;
        }
    }
}
