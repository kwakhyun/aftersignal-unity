using UnityEngine;

namespace AfterSignal
{
    public enum PoliceWeapon
    {
        Pistol,
        Shotgun,
        Rifle
    }

    public sealed class PoliceOfficer : MonoBehaviour
    {
        public WorldActor Body { get; private set; }
        public PoliceWeapon Weapon { get; private set; }
        public bool Ambient { get; set; }
        public int ShotsFired { get; private set; }
        public WorldActor GangTarget { get; private set; }

        WantedSystem system;
        GameDirector game;
        CharacterController motor;
        PixelActor actor;
        SpriteRenderer silhouette;
        Bounds weaponBounds;
        readonly PursuitPath path = new PursuitPath();
        float clock, cooldown, aimTime, recoil, gravity, death, hurt, search, facing = 1;
        int burst;
        float restraint;
        Vector3 shotTarget, knockback, patrolHome;
        bool retreat, playerTarget;
        WorldActor dispatchTarget;
        public void Dispatch(WorldActor target){dispatchTarget=target;search=0;patrolHome=target.transform.position;}
        public static PoliceOfficer Create(WantedSystem owner, Vector3 at, int level, int index)
        {
            var go = new GameObject("도시 경찰", typeof(CharacterController), typeof(PixelActor), typeof(WorldActor), typeof(PoliceOfficer));
            go.transform.position = at;
            go.layer = 9;
            var p = go.GetComponent<PoliceOfficer>();
            p.system = owner;
            p.patrolHome = at;
            p.game = GameDirector.Instance;
            p.Body = go.GetComponent<WorldActor>();
            p.Body.police = true;
            p.Body.health = 65 + level * 18;
            p.Weapon = level >= 3 && (index % 2 == 0 || level >= 4) ? PoliceWeapon.Rifle : level >= 2 && index % 2 == 1 ? PoliceWeapon.Shotgun : PoliceWeapon.Pistol;
            go.name = p.Weapon == PoliceWeapon.Rifle ? "특수대응팀 / 소총" : p.Weapon == PoliceWeapon.Shotgun ? "도시 경찰 / 샷건" : "도시 경찰 / 권총";
            p.motor = go.GetComponent<CharacterController>();
            p.motor.height = 2.1f;
            p.motor.center = Vector3.up * 1.05f;
            p.motor.radius = .34f;
            p.motor.stepOffset = .35f;
            p.actor = go.GetComponent<PixelActor>();
            p.actor.art = PoliceSpriteCatalog.Art(p.Weapon);
            p.actor.Initialize();
            p.silhouette = p.actor.Visual.GetComponent<SpriteRenderer>();
            var aimSprite = System.Array.Find(Resources.LoadAll<Sprite>("Art/" + p.actor.art), s => s.name.EndsWith("-03"));
            p.weaponBounds = aimSprite.bounds;
            p.cooldown = 1.5f + index * .12f;
            PeopleArt.Attach(go,p.Weapon==PoliceWeapon.Rifle?"Swat":p.Weapon==PoliceWeapon.Shotgun?"PoliceShotgun":"Police");
            return p;
        }

        public bool CanSee()
        {
            if (!game || !Body.Alive)
                return false;
            Vector3 start = transform.position + Vector3.up * 1.65f, end = game.Player.Shoulder;
            if (Vector3.Distance(start, end) > 42)
                return false;
            if (!Physics.Linecast(start, end, out var hit, 1, QueryTriggerInteraction.Ignore))
                return true;
            return UrbanSimulation.Instance && UrbanSimulation.Instance.Current && hit.collider.transform.IsChildOf(UrbanSimulation.Instance.Current.transform);
        }

        void Update()
        {
            if (!game || game.Blocked)
                return;
            float dt = Mathf.Min(.07f, Time.deltaTime);
            clock += dt;
            if (!Body.Alive)
            {
                death += dt;
                // The dedicated defeat frame is drawn prone; rotating it again would stand it upright.
                actor.Pose(death < .18f ? 6 : 7, facing);
                if (death > 12)
                    Destroy(gameObject);
                return;
            }

            if (retreat)
            {
                transform.position += Vector3.right * dt * 2;
                if (clock > 4)
                    Destroy(gameObject);
                return;
            }

            hurt = Mathf.Max(0, hurt - dt);
            cooldown -= dt;
            recoil = Mathf.Max(0, recoil - dt);
            search -= dt;
            if (search <= 0 || GangTarget && !GangTarget.Alive || playerTarget && WantedSystem.Level == 0)
            {
                search = .4f;
                var next = dispatchTarget&&dispatchTarget.Alive?dispatchTarget:FactionCombat.NearestOpponent(Body, 44);
                bool pursuePlayer = WantedSystem.Level > 0 && (!next || CanSee()
                    && (game.Player.Shoulder - Body.Center).sqrMagnitude < (next.Center - Body.Center).sqrMagnitude);
                if (next != GangTarget || pursuePlayer != playerTarget) { aimTime = 0; burst = 0; }
                GangTarget = next;
                playerTarget = pursuePlayer;
            }
            if (!playerTarget && (!GangTarget || !GangTarget.Alive))
            {
                aimTime = 0;
                burst = 0;
                int idleFrame = 0;
                Vector3 patrol = patrolHome + (Ambient ? Vector3.forward * Mathf.Sin(clock * .12f) * 5 : Vector3.zero);
                if (Ambient && hurt <= 0 && Vector3.Distance(transform.position, patrol) > .8f)
                {
                    motor.Move(path.Direction(transform.position, patrol) * dt * 1.5f);
                    idleFrame = 1 + (int)(clock * 5) % 2;
                }
                Ground(dt);
                actor.Pose(hurt > 0 ? 5 : idleFrame, facing, hurt > 0 ? .35f : 0);
                return;
            }
            Vector3 targetPosition = playerTarget ? game.Player.transform.position : GangTarget.transform.position;
            Vector3 targetCenter = playerTarget ? game.Player.Shoulder : GangTarget.Center;
            Vector3 delta = targetPosition - transform.position;
            facing = Vector3.Dot(delta, Camera.main ? Camera.main.transform.right : Vector3.right) >= 0 ? 1 : -1;
            bool visible = playerTarget ? CanSee() : FactionCombat.Visible(Body.Center, targetCenter);
            bool close=playerTarget&&visible&&delta.magnitude<2.2f&&hurt<=0&&!(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current)&&game.Player.Health>0;
            if(close)
            {
                if(restraint==0)NpcSpeech.Say(this,"움직이지 마! 무기를 내려놔!",2);
                restraint+=dt;aimTime=0;burst=0;cooldown=.5f;
                if(restraint>(game.Player.Health<45?1.3f:2.5f)){restraint=0;PrisonSystem.Capture(game);return;}
                Ground(dt);actor.Pose(3,facing);return;
            }
            restraint=0;
            float range = Weapon == PoliceWeapon.Shotgun ? 12 : Weapon == PoliceWeapon.Rifle ? 29 : 21;
            int frame = 0;
            if (aimTime > 0)
            {
                aimTime -= dt;
                frame = 3;
                if (aimTime <= 0)
                {
                    burst = Weapon == PoliceWeapon.Rifle ? 3 : 1;
                    cooldown = 0;
                }
            }
            else if (burst > 0 && cooldown <= 0)
            {
                if (visible) Fire();
                burst--;
                recoil = .18f;
                cooldown = burst > 0 ? .15f : Weapon == PoliceWeapon.Shotgun ? 2.3f : Weapon == PoliceWeapon.Rifle ? 1.7f : 1.35f;
            }
            else if (visible && delta.magnitude < range && cooldown <= 0 && hurt <= 0)
            {
                aimTime = Weapon == PoliceWeapon.Shotgun ? .85f : .65f;
                shotTarget = targetCenter;
                frame = 3;
            }
            else if ((!visible || delta.magnitude > range * .78f || playerTarget&&game.Player.Health<45&&delta.magnitude>1.8f) && hurt <= 0)
            {
                Vector3 target = playerTarget && !visible && system ? system.LastSeen : targetPosition;
                if (game.stage == StageId.UrbanCity && target.y > transform.position.y + 6)
                    target = CityRoadNetwork.Sidewalk(new Vector3(target.x, 0, target.z));
                var direction = path.Direction(transform.position, target);
                motor.Move(direction * dt * (Weapon == PoliceWeapon.Rifle ? 4.1f : 3.4f));
                frame = 1 + (int)(clock * 8) % 2;
            }

            Ground(dt);
            actor.Pose(hurt > 0 ? 5 : recoil > 0 ? 4 : frame, facing, hurt > 0 ? .35f : 0, recoil > 0 ? -facing * 5 : 0);
        }

        void Ground(float dt)
        {
            gravity = motor.isGrounded ? -2 : gravity - dt * 28;
            motor.Move((Vector3.up * gravity + knockback) * dt);
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, dt * 15);
        }

        void Fire()
        {
            // Weapons and hands are authored together in the sprite, avoiding a second floating gun.
            // Keep burst tracers at the aiming muzzle even while the recoil/flash frame is displayed.
            float muzzleHeight = Weapon == PoliceWeapon.Rifle ? .78f : .81f;
            Vector3 start = Body.Center+Vector3.up*.35f+(shotTarget-Body.Center).normalized*.7f;
            var direction = (shotTarget - start).normalized;
            int pellets = Weapon == PoliceWeapon.Shotgun ? 5 : 1;
            float distance = Weapon == PoliceWeapon.Shotgun ? 15 : 36;
            for (int i = 0; i < pellets; i++)
            {
                Vector3 aim = Quaternion.Euler(0, (i - (pellets - 1) * .5f) * 2.6f, 0) * direction;
                FactionCombat.Fire(Body, start, start + aim * distance, distance,
                    Weapon == PoliceWeapon.Shotgun ? 18 : Weapon == PoliceWeapon.Rifle ? 9 : 11,
                    SignalEffects.Gold, playerTarget && WantedSystem.Level > 0);
            }

            game.Audio.PlayGun(Weapon == PoliceWeapon.Shotgun ? GunshotKind.Shotgun : Weapon == PoliceWeapon.Rifle ? GunshotKind.Rifle : GunshotKind.PolicePistol, start);
            ShotsFired++;
        }

        public void OnHit(Vector3 force)
        {
            restraint=0;
            hurt = .22f;
            aimTime = 0;
            burst = 0;
            cooldown = Mathf.Max(.45f, cooldown);
            knockback = force;
            if (!Body.Alive)
            {
                motor.enabled = false;
                game.Audio.Play("death", transform.position, .18f, 2);
            }
        }

        public void Withdraw()
        {
            retreat = true;
            clock = 0;
            var hit = GetComponent<Collider>();
            if (hit)
                hit.enabled = false;
        }
    }
}
