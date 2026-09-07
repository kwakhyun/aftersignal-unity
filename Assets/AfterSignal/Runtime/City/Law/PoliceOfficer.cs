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

        WantedSystem system;
        GameDirector game;
        CharacterController motor;
        PixelActor actor;
        EnemyWeaponRig rig;
        readonly PursuitPath path = new PursuitPath();
        float clock, cooldown, aimTime, recoil, gravity, death, hurt;
        int burst;
        Vector3 shotTarget, knockback;
        bool retreat;
        public static PoliceOfficer Create(WantedSystem owner, Vector3 at, int level, int index)
        {
            var go = new GameObject(level >= 3 ? "특수대응팀 / rifle" : "도시 경찰", typeof(CharacterController), typeof(PixelActor), typeof(WorldActor), typeof(PoliceOfficer));
            go.transform.position = at;
            go.layer = 9;
            var p = go.GetComponent<PoliceOfficer>();
            p.system = owner;
            p.game = GameDirector.Instance;
            p.Body = go.GetComponent<WorldActor>();
            p.Body.police = true;
            p.Body.health = 65 + level * 18;
            p.Weapon = level >= 3 && (index % 2 == 0 || level >= 4) ? PoliceWeapon.Rifle : level >= 2 && index % 2 == 1 ? PoliceWeapon.Shotgun : PoliceWeapon.Pistol;
            p.motor = go.GetComponent<CharacterController>();
            p.motor.height = 2.1f;
            p.motor.center = Vector3.up * 1.05f;
            p.motor.radius = .34f;
            p.motor.stepOffset = .35f;
            p.actor = go.GetComponent<PixelActor>();
            p.actor.art = "Enemies/gunner";
            p.actor.Initialize();
            p.rig = go.AddComponent<EnemyWeaponRig>();
            p.rig.Initialize(p.Weapon.ToString().ToLowerInvariant());
            p.cooldown = 1.5f + index * .12f;
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
                actor.Pose(death < .15f ? 6 : 7, 1, 0, Mathf.Min(80, death * 190));
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
            Vector3 delta = game.Player.transform.position - transform.position;
            float facing = delta.x >= 0 ? 1 : -1;
            bool visible = CanSee();
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
                Fire();
                burst--;
                recoil = .18f;
                cooldown = burst > 0 ? .15f : Weapon == PoliceWeapon.Shotgun ? 2.3f : Weapon == PoliceWeapon.Rifle ? 1.7f : 1.35f;
            }
            else if (visible && delta.magnitude < range && cooldown <= 0 && hurt <= 0)
            {
                aimTime = Weapon == PoliceWeapon.Shotgun ? .85f : .65f;
                shotTarget = game.Player.Shoulder;
                frame = 3;
            }
            else if ((!visible || delta.magnitude > range * .78f) && hurt <= 0)
            {
                Vector3 target = visible ? game.Player.transform.position : system.LastSeen;
                if (game.stage == StageId.UrbanCity && target.y > transform.position.y + 6)
                    target = CityRoadNetwork.Sidewalk(new Vector3(target.x, 0, target.z));
                var direction = path.Direction(transform.position, target);
                motor.Move(direction * dt * (Weapon == PoliceWeapon.Rifle ? 4.1f : 3.4f));
                frame = 1 + (int)(clock * 8) % 2;
            }

            gravity = motor.isGrounded ? -2 : gravity - dt * 28;
            motor.Move((Vector3.up * gravity + knockback) * dt);
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, dt * 15);
            actor.Pose(hurt > 0 ? 5 : recoil > 0 ? 4 : frame, facing, hurt > 0 ? .35f : 0, recoil > 0 ? -facing * 5 : 0);
            rig.Present(shotTarget - transform.position, aimTime > 0 ? .35f : recoil > 0 ? .65f : 0, Weapon != PoliceWeapon.Pistol);
        }

        void Fire()
        {
            Vector3 start = transform.position + Vector3.up * 1.35f;
            var direction = (shotTarget - start).normalized;
            int pellets = Weapon == PoliceWeapon.Shotgun ? 5 : 1;
            float distance = Weapon == PoliceWeapon.Shotgun ? 15 : 36;
            for (int i = 0; i < pellets; i++)
            {
                Vector3 aim = Quaternion.Euler(0, (i - (pellets - 1) * .5f) * 2.6f, 0) * direction;
                Vector3 end = start + aim * distance;
                if (Physics.Raycast(start, aim, out var hit, distance, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    var player = hit.collider.GetComponentInParent<PlayerMotor>();
                    if (player)
                        player.ReceiveDamage(Weapon == PoliceWeapon.Shotgun ? 18 : Weapon == PoliceWeapon.Rifle ? 9 : 11, transform.position);
                    var car = hit.collider.GetComponentInParent<CityVehicle>();
                    if (car && UrbanSimulation.Instance && car == UrbanSimulation.Instance.Current)
                        car.Damage(Weapon == PoliceWeapon.Shotgun ? 3 : 5, hit.point);
                }

                SignalEffects.Beam(start, end, SignalEffects.Gold, .025f, .07f);
            }

            game.Audio.Play("pistol", start, Weapon == PoliceWeapon.Shotgun ? .3f : .18f, 2);
        }

        public void OnHit(Vector3 force)
        {
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
