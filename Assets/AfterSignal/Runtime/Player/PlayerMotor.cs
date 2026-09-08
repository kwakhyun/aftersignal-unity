using UnityEngine;
using System.Collections.Generic;

namespace AfterSignal
{
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class PlayerMotor : MonoBehaviour
    {
        public GameTuning Tuning { get; private set; }
        public GameDirector Director { get; private set; }
        public CharacterController Controller { get; private set; }
        public RopeMotor Rope { get; private set; }

        public Vector3 Velocity;
        public bool Running { get; private set; }
        public int LastMoveDirection { get; private set; } = 1;
        public Vector3 Shoulder => transform.position + Vector3.up * 1.45f;

        public Vector3 Muzzle
        {
            get
            {
                return Shoulder + (Aim-Shoulder).normalized*.55f;
            }
        }

        public Vector3 Aim { get; private set; }
        public float Facing { get; private set; } = 1;
        public bool Grounded { get; private set; }
        public bool Guarding { get; private set; }
        public float Health { get; private set; } = 100;
        public float Energy { get; private set; } = 100;
        public float HurtTime { get; private set; }
        public float DashTime { get; private set; }
        public float AttackTime { get; private set; }
        public float AttackDuration { get; private set; }
        public WeaponId Weapon { get; private set; }
        public int Combo { get; private set; }
        public int Attacks { get; private set; }
        public int HitsTaken { get; private set; }
        public int ConfirmedHits { get; private set; }
        public float HitStopRemaining => Mathf.Max(0, freezeUntil - Time.unscaledTime);
        public float LandingTime { get; private set; }
        public bool SkillPose { get; private set; }

        readonly HashSet<CityVehicle> struckCars=new HashSet<CityVehicle>();
        readonly HashSet<EnemyBrain> struck = new HashSet<EnemyBrain>();
        readonly HashSet<BreakableGlass> shattered = new HashSet<BreakableGlass>();
        float freezeUntil, attackBuffer, stepDistance, trailClock;
        bool resolving, swingStarted;
        float attackCooldown, dashCooldown, skillCooldown, invincible, coyote, comboWindow, jumpBuffer;
        int jumps;
        PixelActor actor;
        float supportedUntil;
        Vector3 dashDirection = Vector3.right;
        public Vector3 DashDirection => dashDirection;

        public void Carry(Vector3 displacement)
        {
            if (!Controller.enabled || Velocity.y > 1 || Rope.Attached)
                return;
            Controller.Move(displacement);
            supportedUntil = Time.time + .1f;
            Grounded = true;
            Velocity.y = -2;
            coyote = .11f;
            jumps = 0;
        }

        bool Supported() => Velocity.y <= 1 && (Time.time < supportedUntil || Physics.SphereCast(transform.position + Vector3.up * .3f, .27f, Vector3.down, out var floor, .38f, 1, QueryTriggerInteraction.Ignore) && floor.normal.y > .55f);
        public void Initialize(GameDirector director)
        {
            Director = director;
            Tuning = director.tuning;
            Health = Tuning.maxHealth;
            Controller = GetComponent<CharacterController>();
            Controller.height = 2.08f;
            Controller.radius = .34f;
            Controller.center = Vector3.up * 1.06f;
            Controller.stepOffset = .36f;
            Controller.skinWidth = .035f;
            actor = GetComponent<PixelActor>();
            actor.Initialize();
            Rope = gameObject.AddComponent<RopeMotor>();
            Rope.Initialize(this);
            gameObject.layer = 8;
            Weapon = WeaponId.Katana;
            Ammo = Tuning.magazineSize;
            Equipment=gameObject.AddComponent<PlayerEquipment>();Equipment.Initialize(this);
        }

        public void Tick(ControlFrame input, float dt)
        {
            if (Health <= 0)
                return;
            if(OceanLife.Tick(this,input,dt))return;
            attackBuffer = input.attack ? Tuning.Timing(Weapon).buffer : Mathf.Max(0, attackBuffer - dt / Mathf.Max(.01f, Time.timeScale));
            if (HitStopRemaining > 0)
                return;
            UpdateArsenal(input, dt);
            LandingTime = Mathf.Max(0, LandingTime - dt);
            float previousAttack = AttackTime;
            HurtTime = Mathf.Max(0, HurtTime - dt);
            invincible = Mathf.Max(0, invincible - dt);
            DashTime = Mathf.Max(0, DashTime - dt);
            AttackTime = Mathf.Max(0, AttackTime - dt);
            attackCooldown -= dt;
            dashCooldown -= dt;
            skillCooldown -= dt;
            comboWindow -= dt;
            Energy = Mathf.Min(100, Energy + dt * 4f);
            var camera = Camera.main;
            var ray = camera.ScreenPointToRay(input.pointer);
            var viewRight = Director.CameraRig.ViewRight;
            var moveDirection = Vector3.ClampMagnitude(Director.CameraRig.MoveDirection(input.move),1);
            Running = input.move.sqrMagnitude > .04f;
            if(input.move.sqrMagnitude > .04f) LastMoveDirection = Mathf.Abs(input.move.y) > Mathf.Abs(input.move.x) ? input.move.y > 0 ? 2 : 0 : 1;
            const float gait = 1;
            Aim = Ballistics.AimPoint(ray, transform);
            if (AttackTime <= 0)
            {
                if (Mathf.Abs(input.move.x) > .15f && !input.guard)
                    Facing = Mathf.Sign(input.move.x);
                else if (input.guard && Mathf.Abs(Vector3.Dot(Aim - Shoulder, Director.CameraRig.ViewRight)) > .15f)
                    Facing = Mathf.Sign(Vector3.Dot(Aim - Shoulder, Director.CameraRig.ViewRight));
            }

            Guarding = input.guard && Grounded && !Rope.Attached && AttackTime <= 0;
            bool wasGrounded = Grounded;
            Grounded = !WallClimbing && Velocity.y <= 1 && (Controller.isGrounded || Supported());
            if (Grounded && !wasGrounded && Velocity.y < -3)
            {
                LandingTime = .13f;
                Director.Audio.Play("land", transform.position, .3f, 1);
                SignalEffects.Dust(transform.position, Vector3.up, .55f);
            }

            if (Grounded)
            {
                coyote = .11f;
                jumps = 0;
            }
            else
                coyote -= dt;
            if (input.jump)
                jumpBuffer = .12f;
            else
                jumpBuffer -= dt;
            bool wasRope = Rope.Attached;
            Rope.Tick(input, dt);
            if (jumpBuffer > 0 && !wasRope && !WallClimbing && (coyote > 0 || jumps < 2))
            {
                Velocity.y = Tuning.jumpSpeed;
                jumpBuffer = 0;
                jumps = Grounded || coyote > 0 ? 1 : Mathf.Max(1,jumps)+1;
                coyote = 0;
                supportedUntil = 0;
                Grounded = false;
                SignalEffects.Burst(transform.position + Vector3.up * .1f, SignalEffects.Cyan, 8, 2);
                Director.Audio.Play("jump", transform.position, .18f, 1);
            }

            if (input.dash && dashCooldown <= 0 && !Guarding)
            {
                DashTime = .2f;
                dashCooldown = .85f;
                invincible = .27f;
                Facing = Mathf.Abs(input.move.x) > .2f ? Mathf.Sign(input.move.x) : Facing;
                dashAttackWindow = .27f;
                CancelReload();
                dashDirection = input.move.sqrMagnitude > .04f ? moveDirection.normalized : viewRight * Facing;
                Velocity.x = dashDirection.x * Tuning.dashSpeed;
                Velocity.z = dashDirection.z * Tuning.dashSpeed;
                Velocity.y = Mathf.Max(Velocity.y, 0);
                resolving = false;
                AttackTime = 0;
                Director.Audio.Play("dash", transform.position, .3f, 2);
                SignalEffects.Dust(transform.position, -dashDirection, .6f);
            }

            if (DashTime <= 0)
            {
                if (!Rope.Attached)
                {
                    float target = moveDirection.x * Tuning.moveSpeed * gait * (Guarding ? .36f : 1) * (AttackTime > 0 && Weapon != WeaponId.Pistol ? .45f : 1);
                    Velocity.x = Mathf.MoveTowards(Velocity.x, target, (Grounded ? Tuning.acceleration : 13f) * dt);
                }

                if (!Rope.Attached)
                    Velocity.z = Mathf.MoveTowards(Velocity.z, moveDirection.z * Tuning.moveSpeed * gait * (CivicWorld.Exploration(Director.stage) ? 1 : .64f), (Grounded ? Tuning.acceleration : 13f) * dt);
                Velocity.y -= (Rope.Attached ? Tuning.ropeGravity : Tuning.gravity) * dt;
                if (Grounded && Velocity.y < 0)
                    Velocity.y = -2f;
            }

            Traverse(input,moveDirection,dt);
            bool groundedBeforeMove = Grounded;
            float landingSpeed = Velocity.y;
            Controller.Move(Velocity * dt);
            Rope.Constrain();
            Grounded = !WallClimbing && Velocity.y <= 1 && (Controller.isGrounded || Supported());
            if (Grounded && !groundedBeforeMove && landingSpeed < -3)
            {
                LandingTime = .13f;
                Director.Audio.Play("land", transform.position, .3f, 1);
                SignalEffects.Dust(transform.position, Vector3.up, .55f);
            }
            if ((Controller.collisionFlags & CollisionFlags.Above) != 0 && Velocity.y > 0)
                Velocity.y = 0;
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, 1, Director.stageLength - 1);
            position.z = Mathf.Clamp(position.z, -Director.halfDepth, Director.stage==StageId.UrbanCity?FourCityCatalog.North-2:Director.halfDepth);
            transform.position = position;
            if (position.y < (Director.stage==StageId.UrbanCity&&FourCityCatalog.Dry(position)?-125:-9))
            {
                ReceiveDamage(20, Vector3.zero, true);
                Respawn(Director.checkpoint, false);
            }

            if (resolving)
                ResolveSwing(previousAttack);
            if (attackBuffer > 0 && !WallClimbing && !Guarding && HurtTime <= 0 && attackCooldown <= 0 && !Reloading)
            {
                Attack();
                attackBuffer = 0;
            }

            if (input.skill && !WallClimbing && !Guarding && HurtTime <= 0 && !Reloading && AttackTime <= 0 && Energy >= Tuning.skillCost && skillCooldown <= 0)
                Skill();
            actor.TickHero(this, dt);
            if (Grounded && DashTime <= 0 && AttackTime <= 0)
            {
                stepDistance += new Vector2(Velocity.x, Velocity.z).magnitude * dt;
                if (stepDistance > SeoLocomotion.StrideLength * .5f)
                {
                    stepDistance -= SeoLocomotion.StrideLength * .5f;
                    Director.Audio.Play(Director.stage == StageId.Carriage || Director.stage == StageId.Roof ? "step_metal" : "step_tile", transform.position, .12f, 0);
                }
            }

            trailClock -= dt;
            if (DashTime > 0 && trailClock <= 0)
            {
                trailClock = .055f;
                SignalEffects.Afterimage(actor, .13f);
            }
        }

        public void Attack()
        {
            if(Equipment&&Equipment.Extended){Equipment.Attack();return;}
            if (Reloading)
                return;
            if (Weapon == WeaponId.Pistol && Ammo <= 0)
            {
                BeginReload();
                return;
            }

            if (Mathf.Abs(Vector3.Dot(Aim - Shoulder, Director.CameraRig.ViewRight)) > .15f)
                Facing = Mathf.Sign(Vector3.Dot(Aim - Shoulder, Director.CameraRig.ViewRight));
            Combo = comboWindow > 0 ? (Combo + 1) % 3 : 0;
            comboWindow = 1.05f;
            Attacks++;
            Action = dashAttackWindow > 0 ? WeaponAction.Dash : WeaponAction.Combo;
            dashAttackWindow = 0;
            if (Action == WeaponAction.Dash)
                DashAttacks++;
            currentTiming = Action == WeaponAction.Dash ? (Weapon == WeaponId.Katana ? Tuning.katanaDash : Weapon == WeaponId.Greatsword ? Tuning.heavyDash : Tuning.pistolDash) : Tuning.ComboTiming(Weapon, Combo);
            SkillPose = false;
            AttackDuration = currentTiming.duration;
            shotsInAction = 0;
            AttackTime = AttackDuration;
            attackCooldown = AttackDuration;
            struck.Clear();
            shattered.Clear();
            worldStruck.Clear();struckCars.Clear();
            resolving = true;
            swingStarted = false;
            if (Weapon == WeaponId.Pistol && currentTiming.contact <= 0)
                ResolveSwing(AttackDuration);
        }

        void ResolveSwing(float previousAttack)
        {
            var timing = ActiveTiming;
            float elapsed = AttackDuration - AttackTime, previous = AttackDuration - previousAttack;
            if (Action == WeaponAction.Skill)
            {
                ResolveSkill(elapsed);
                return;
            }

            if (elapsed < timing.contact)
                return;
            if (previous > timing.activeEnd)
            {
                resolving = false;
                return;
            }

            float damage = CampaignRules.Damage(Weapon, Combo, Tuning) * (Equipment?Equipment.Item.multiplier:1) * (Action == WeaponAction.Dash ? Weapon == WeaponId.Greatsword ? 1.3f : 1.15f : 1);
            if (Weapon == WeaponId.Pistol)
            {
                int count = Action == WeaponAction.Dash ? 3 : 1;
                while (shotsInAction < count && elapsed >= timing.contact + shotsInAction * .075f)
                {
                    if (Ammo <= 0)
                        break;
                    actor.TickHero(this, 0);
                    FirePistol(damage, false);
                    shotsInAction++;
                }

                if (shotsInAction >= count || Ammo <= 0)
                    resolving = false;
            }
            else
            {
                float range = (Weapon == WeaponId.Greatsword ? 3.4f : 2.6f) + (Action == WeaponAction.Dash ? .8f : 0);
                if (!swingStarted)
                {
                    swingStarted = true;
                    FacadeGlass.Strike(Shoulder,AttackHeading,range,damage);
                    SignalEffects.Slash(Shoulder, AttackHeading, range, Weapon == WeaponId.Greatsword ? SignalEffects.Gold : SignalEffects.Cyan, Combo);
                    Director.Audio.Play(Weapon == WeaponId.Greatsword ? "heavy_swing" : "blade_swing", Shoulder, .22f, 1);
                }

                foreach (var enemy in Director.Enemies)
                    if (enemy && enemy.Alive && !struck.Contains(enemy))
                    {
                        Vector3 d = enemy.transform.position - transform.position;
                        var strikeRight = AttackHeading;
                        float along = Vector3.Dot(d, strikeRight);
                        if (Mathf.Abs(Vector3.Dot(d, Vector3.Cross(strikeRight, Vector3.up))) < 1.35f && Mathf.Abs(d.y) < 2.9f && along > -.55f && along < range && !Physics.Linecast(Shoulder,enemy.transform.position+Vector3.up,1,QueryTriggerInteraction.Ignore))
                        {
                            struck.Add(enemy);
                            enemy.Damage(damage, strikeRight * (Weapon == WeaponId.Greatsword ? 5.5f : 3.5f));
                            OnContact(enemy, enemy.transform.position + new Vector3(-Facing * .22f, 1.22f, -.08f), Vector3.right * Facing);
                        }
                    }

                foreach (var glass in BreakableGlass.All)
                    if (glass && !glass.Broken && !shattered.Contains(glass) && Vector3.Distance(Shoulder, glass.transform.position) < range + 1.1f)
                    {
                        shattered.Add(glass);
                        glass.Hit(damage);
                    }

                WorldActor.Strike(Shoulder, Action == WeaponAction.Dash ? dashDirection : (Aim - Shoulder).normalized, range, damage, worldStruck, true);
                StrikeVehicles(range,damage);
                Director.TryCoreStrike(this, range);
            }

            if (elapsed >= timing.activeEnd)
                resolving = false;
        }

        void OnContact(EnemyBrain enemy, Vector3 position, Vector3 direction)
        {
            ConfirmedHits++;
            bool armored = enemy.boss && Director.ExposeTimer <= 0;
            bool heavy = Weapon == WeaponId.Greatsword;
            SignalEffects.Impact(position, direction, armored ? SignalEffects.Gold : SignalEffects.Cyan, heavy ? 1.1f : .7f);
            Director.Audio.Play(armored ? "guard" : heavy ? "heavy_hit" : "blade_hit", position, heavy ? .5f : .36f, 3);
            if (armored)
                return;
            float stop = ActiveTiming.hitStop + (!enemy.Alive ? .018f : 0);
            freezeUntil = Mathf.Max(freezeUntil, Time.unscaledTime + stop);
            enemy.React(ActiveTiming.stagger, stop, direction);
            Director.CameraRig.Impact(direction, heavy ? .1f : .045f);
        }

        public void Skill()
        {
            if(Equipment&&Equipment.Extended){Equipment.Attack();return;}
            if (Energy < Tuning.skillCost || Reloading)
                return;
            if (Mathf.Abs(Vector3.Dot(Aim - Shoulder, Director.CameraRig.ViewRight)) > .15f)
                Facing = Mathf.Sign(Vector3.Dot(Aim - Shoulder, Director.CameraRig.ViewRight));
            Energy -= Tuning.skillCost;
            skillCooldown = 4f;
            SkillsUsed++;
            Action = WeaponAction.Skill;
            currentTiming = Weapon == WeaponId.Katana ? Tuning.katanaSkill : Weapon == WeaponId.Greatsword ? Tuning.heavySkill : Tuning.pistolSkill;
            AttackDuration = currentTiming.duration;
            AttackTime = attackCooldown = AttackDuration;
            SkillPose = true;
            resolving = true;
            swingStarted = false;
            shotsInAction = 0;
            struck.Clear();
            shattered.Clear();
            worldStruck.Clear();struckCars.Clear();
        }

        public void ReceiveDamage(float amount, Vector3 source, bool ignoreInvulnerability = false)
        {
            if (Health <= 0 || (!ignoreInvulnerability && invincible > 0))
                return;
            bool blocked = Guarding && Vector3.Dot(source - transform.position, Director.CameraRig.ViewRight) * Facing >= 0;
            if (blocked)
            {
                amount *= .15f;
                Energy = Mathf.Max(0, Energy - 8);
                SignalEffects.Ring(Shoulder + Vector3.right * Facing * .55f, SignalEffects.Gold, 1.2f, .2f);
            }
            else
            {
                HurtTime = .28f;
                Rope.Release();
                resolving = false;
                AttackTime = 0;
                CancelReload();
                Director.CameraRig.Impact((transform.position - source).normalized, .09f);
                Velocity.x = (transform.position.x >= source.x ? 1 : -1) * 4.5f;
            }

            Health = Mathf.Max(0, Health - amount);
            invincible = .65f;
            HitsTaken++;
            Director.Audio.Play(blocked ? "guard" : "hurt", Shoulder, .42f, 3);
            if (Health <= 0)
                Director.Die();
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(Tuning.maxHealth, Health + amount);
            Energy = Mathf.Min(100, Energy + 12);
        }

        public void RestoreEnergy(float amount)
        {
            Energy = Mathf.Min(100, Energy + amount);
        }

        public void Respawn(Vector3 point, bool fullHeal = true)
        {
            Rope.Release();
            Controller.enabled = false;
            transform.position = point;
            Controller.enabled = true;
            Velocity = Vector3.zero;
            Grounded = false;
            WallClimbing=false;wallRelease=0;jumps=0;jumpBuffer=0;coyote=0;
            supportedUntil = 0;
            DashTime = 0;
            if (fullHeal)
            {
                Health = Tuning.maxHealth;
                Ammo = Tuning.magazineSize;
            }

            invincible = 1.5f;
            HurtTime = 0;
            AttackTime = 0;
            resolving = false;
            freezeUntil = attackBuffer = 0;
            CancelReload();
            pendingWeapon = -1;
            dashAttackWindow = 0;
        }
    }
}
