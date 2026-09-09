using UnityEngine;

namespace AfterSignal
{
    public sealed class GangMember : MonoBehaviour
    {
        public WorldActor Body { get; private set; }
        public int ShotsFired { get; private set; }
        public bool AttackingPlayer=>playerTarget;
        public bool CampaignUnit;
        public int CampaignWeapon;
        public WorldActor Target { get; private set; }
        public void GoTo(Vector3 point){home=point;}
        GameDirector game;
        CharacterController motor;
        PixelActor actor;
        SpriteRenderer visual;
        Bounds weaponBounds;
        readonly PursuitPath path = new PursuitPath();
        Vector3 home, shotTarget, knockback;
        float cooldown, search, aim, recoil, hurt, death, gravity, clock, provoked, facing = 1;
        bool playerTarget;

        public static GangMember Create(Vector3 at, int crew, int index)
        {
            var go = new GameObject("갱단 / " + CrewName(crew), typeof(CharacterController), typeof(PixelActor), typeof(WorldActor), typeof(GangMember));
            go.layer = 9;
            go.transform.position = at;
            var member = go.GetComponent<GangMember>();
            member.game = GameDirector.Instance;
            member.home = at;
            member.Body = go.GetComponent<WorldActor>();
            member.Body.gang = true;
            member.Body.health = 180 + index%5 * 22;
            member.motor = go.GetComponent<CharacterController>();
            member.motor.height = 2.1f;
            member.motor.center = Vector3.up * 1.05f;
            member.motor.radius = .34f;
            member.motor.stepOffset = .35f;
            member.actor = go.GetComponent<PixelActor>();
            member.actor.art = GangSpriteCatalog.Art(crew);
            member.actor.Initialize();
            member.visual = member.actor.Visual.GetComponent<SpriteRenderer>();
            var aimSprite = System.Array.Find(Resources.LoadAll<Sprite>("Art/" + member.actor.art), s => s.name.EndsWith("-03"));
            member.weaponBounds = aimSprite ? aimSprite.bounds : member.visual.sprite.bounds;
            member.cooldown = 1.1f + index * .25f;
            PeopleArt.Attach(go,new[]{"GangCrimson","GangViolet","GangChrome"}[crew%3]);
            NpcPersona.Ensure(member.Body,"GangCrimson",CrewName(crew)+" 무장 조직원");
            return member;
        }

        public static string CrewName(int crew) => crew % 3 == 0 ? "혈선 연합" : crew % 3 == 1 ? "유령 회로" : "크롬 신디케이트";

        void Update()
        {
            if (!game || game.Blocked) return;
            float dt = Mathf.Min(.07f, Time.deltaTime);
            clock += dt;
            if (!Body.Alive)
            {
                death += dt;
                actor.Pose(death < .18f ? 6 : 7, facing, 0, 0);
                visual.color = Color.white;
                if (death > 12) Destroy(gameObject);
                return;
            }
            if(!motor.enabled)return;
            cooldown -= dt;
            search -= dt;
            hurt = Mathf.Max(0, hurt - dt);
            recoil = Mathf.Max(0, recoil - dt);
            provoked = Mathf.Max(0, provoked - dt);
            if (search <= 0 || Target && !Target.Alive)
            {
                search = .4f;
                bool eventAllowed=CampaignUnit||CityEventGate.Enrolled(Body)||!CityEventGate.Busy;
                var next = eventAllowed?FactionCombat.NearestOpponent(Body, 65):null;
                if(!next&&eventAllowed&&!CampaignUnit)next=FactionCombat.WoundedVictim(Body,30);
                if(next&&!CampaignUnit&&!CityEventGate.JoinGang(Body))next=null;
                bool attackPlayer = CampaignUnit || provoked > 0 && FactionCombat.Visible(Body.Center, game.Player.Shoulder, 32)
                    && (!next || (game.Player.Shoulder - Body.Center).sqrMagnitude < (next.Center - Body.Center).sqrMagnitude);
                if (next != Target || playerTarget != attackPlayer) aim = 0;
                Target = next;
                playerTarget = attackPlayer;
            }
            bool hasTarget = playerTarget || Target && Target.Alive;
            var destination = hasTarget ? playerTarget ? game.Player.transform.position : Target.transform.position : home;
            var delta = destination - transform.position;
            float screenDirection = Vector3.Dot(delta, Camera.main ? Camera.main.transform.right : Vector3.right);
            if (Mathf.Abs(screenDirection) > .1f) facing = Mathf.Sign(screenDirection);
            int frame = 0;
            if (hasTarget && hurt <= 0)
            {
                Vector3 center = playerTarget ? game.Player.Shoulder : Target.Center;
                bool visible = FactionCombat.Visible(Body.Center, center, 48);
                if (aim > 0)
                {
                    aim -= dt;
                    frame = 3;
                    if (aim <= 0 && visible)
                    {
                        actor.Pose(3, facing);
                        Vector3 muzzle = Body.Center+Vector3.up*.35f+(shotTarget-Body.Center).normalized*.65f;
                        FactionCombat.Fire(Body, muzzle, center, 65, Target&&Target.Downed?Mathf.Max(45,Target.health+5):CampaignUnit?CampaignWeapon==1?15:8:10, SignalEffects.Red, playerTarget);
                        game.Audio.PlayGun(CampaignUnit?CampaignWeapon==1?GunshotKind.Shotgun:GunshotKind.Rifle:GunshotKind.Rifle, muzzle);
                        ShotsFired++;
                        recoil = .2f;
                        cooldown = CampaignUnit?CampaignWeapon==1?1.5f:.48f:.5f;
                    }
                }
                else if (visible && delta.magnitude < 23 && cooldown <= 0)
                {
                    aim = .34f;
                    shotTarget = center;
                    frame = 3;
                }
                else if (!visible || delta.magnitude > 16)
                {
                    motor.Move(path.Direction(transform.position, destination) * dt * 3.2f);
                    frame = 1 + (int)(clock * 8) % 2;
                }
            }
            else if (!hasTarget && delta.magnitude > 1 && hurt <= 0)
            {
                motor.Move(path.Direction(transform.position, home) * dt * 1.6f);
                frame = 1 + (int)(clock * 5) % 2;
            }
            gravity = motor.isGrounded ? -2 : gravity - dt * 28;
            motor.Move((Vector3.up * gravity + knockback) * dt);
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, dt * 15);
            actor.Pose(hurt > 0 ? 5 : recoil > 0 ? 4 : frame, facing, 0, recoil > 0 ? -facing * 4 : 0);
            visual.color = hurt > 0 ? new Color(1, .65f, .65f) : Color.white;
        }

        public void OnHit(Vector3 force, bool byPlayer)
        {
            hurt = .16f;
            aim = 0;
            knockback = force;
            if (byPlayer) { provoked = 25; search = 0; }
            if (!Body.Alive) { motor.enabled = false; game.Audio.Play("death", transform.position, .15f, 2); }
        }
    }
}
