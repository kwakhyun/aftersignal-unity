using UnityEngine;

namespace AfterSignal
{
    public sealed class EnemyBrain : MonoBehaviour
    {
        public string kind = "blade";
        public float maxHealth = 72;
        public bool boss;
        public float activateAt;
        public float Health { get; private set; }
        public bool Alive => Health > 0;
        public bool Active { get; private set; }
        public float Telegraph { get; private set; }
        public float Facing { get; private set; } = -1;
        public float Exposed { get; private set; }

        GameDirector director;
        PixelActor actor;
        CharacterController controller;
        Vector3 knockback;
        float cooldown = 1.6f, hurt, clock, gravity, leapTime;
        bool fired;
        float freezeUntil, deathTime, impactLean;
        float followThrough, windupDuration;
        Vector3 lockedAim;
        EnemyWeaponRig weaponRig;
        public void React(float stagger, float stop, Vector3 direction)
        {
            if (boss)
                return;
            hurt = Mathf.Max(hurt, stagger);
            freezeUntil = Mathf.Max(freezeUntil, Time.unscaledTime + stop);
            impactLean = -Mathf.Sign(direction.x) * 13;
            Telegraph = 0;
            cooldown = Mathf.Max(cooldown, .38f);
        }

        public void Initialize(GameDirector owner)
        {
            director = owner;
            Health = maxHealth;
            actor = GetComponent<PixelActor>();
            actor.Initialize();
            controller = GetComponent<CharacterController>();
            gameObject.layer = 9;
            Active = false;
            if (!boss)
            {
                weaponRig = gameObject.AddComponent<EnemyWeaponRig>();
                weaponRig.Initialize(kind);
            }
        }

        public void Tick(float dt)
        {
            if (!Alive)
            {
                deathTime += dt;
                actor.Pose(deathTime < .13f ? 6 : 7, Facing, 0, Mathf.Lerp(impactLean, Mathf.Sign(impactLean) * 35, Mathf.Clamp01(deathTime / .32f)));
                if (deathTime > .38f)
                    gameObject.SetActive(false);
                return;
            }

            if (Time.unscaledTime < freezeUntil)
                return;
            clock += dt;
            hurt = Mathf.Max(0, hurt - dt);
            Exposed = Mathf.Max(0, Exposed - dt);
            followThrough = Mathf.Max(0, followThrough - dt);
            var player = director.Player;
            Vector3 delta = player.transform.position - transform.position;
            Active = player.transform.position.x >= activateAt;
            if (delta.sqrMagnitude > .1f)
                Facing = Mathf.Sign(delta.x);
            int frame = 0;
            if (Active && !boss)
            {
                cooldown -= dt;
                if (Telegraph > 0)
                {
                    Telegraph -= dt;
                    frame = 3;
                    if (Telegraph <= 0 && !fired)
                    {
                        fired = true;
                        if(kind!="gunner")director.Audio.Play(kind=="shield"?"heavy_swing":"blade_swing",transform.position+Vector3.up,.3f,1);
                        cooldown = kind == "gunner" ? 1.7f : 1.15f;
                        followThrough = kind == "gunner" ? .34f : .42f;
                        if (kind == "gunner")
                        {
                            Vector3 start = transform.position + new Vector3(Facing * .6f, 1.25f, 0);
                            Vector3 end = lockedAim;
                            director.Audio.PlayGun(GunshotKind.Rifle, start);
                            SignalEffects.Beam(start, end, SignalEffects.Red, .035f, .2f);
                            var shot = (end - start).normalized;
                            if (Physics.Raycast(start, shot, out var contact, 16, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore) && contact.collider.GetComponentInParent<PlayerMotor>())
                                player.ReceiveDamage(10, transform.position);
                        }
                        else if (Mathf.Abs(delta.x) < 2.3f && Mathf.Abs(delta.z) < 1.2f && Mathf.Abs(delta.y) < 2.6f)
                        {
                            SignalEffects.Slash(transform.position + Vector3.up, Facing, 1.5f, SignalEffects.Red, 0);
                            player.ReceiveDamage(12, transform.position);
                        }
                    }
                }
                else if (followThrough > 0)
                {
                    frame = 4;
                    if (kind != "gunner" && followThrough > .27f)
                        controller.Move(Vector3.right * Facing * 1.5f * dt);
                }
                else if (leapTime > 0)
                {
                    leapTime -= dt;
                    controller.Move(Vector3.right * Facing * 5.2f * dt);
                    frame = 4;
                }
                else if (hurt <= 0)
                {
                    float desired = kind == "gunner" ? 8 : 1.65f;
                    if (Mathf.Abs(delta.x) > desired || Mathf.Abs(delta.z) > .7f)
                    {
                        Vector3 move = new Vector3(Mathf.Abs(delta.x) > desired ? Facing * 2.5f : 0, 0, Mathf.Clamp(delta.z, -1, 1) * 1.8f);
                        if (controller.isGrounded && Mathf.Abs(move.x) > .1f && !Physics.Raycast(transform.position + Vector3.up + Vector3.right * Facing * .75f, Vector3.down, 2.1f, 1, QueryTriggerInteraction.Ignore))
                        {
                            gravity = 11;
                            leapTime = .82f;
                        }

                        controller.Move(move * dt);
                        frame = 1 + (int)(clock * 7) % 2;
                    }
                    else if (cooldown <= 0)
                    {
                        Telegraph = windupDuration = kind == "gunner" ? .8f : kind == "stalker" ? .48f : kind == "shield" ? .9f : .65f;
                        lockedAim = player.Shoulder;
                        fired = false;
                    }
                }
            }

            if (!boss)
            {
                gravity = controller.isGrounded && leapTime <= 0 ? -2 : gravity - 30 * dt;
                controller.Move((knockback + Vector3.up * gravity) * dt);
                knockback = Vector3.MoveTowards(knockback, Vector3.zero, 22 * dt);
                var pos = transform.position;
                pos.z = Mathf.Clamp(pos.z, -director.halfDepth, director.halfDepth);
                transform.position = pos;
                if (pos.y < -8)
                {
                    Damage(Health, Vector3.zero);
                    return;
                }
            }

            if (hurt > 0)
                frame = 5;
            else if (boss)
                frame = Exposed > 0 ? 5 : Telegraph > 0 ? 3 : (int)(clock * 3) % 2;
            impactLean = Mathf.MoveTowards(impactLean, 0, 55 * dt);
            actor.Pose(frame, Facing, hurt > 0 ? .28f : 0, hurt > 0 ? impactLean : 0);
            if (weaponRig)
            {
                float phase = Telegraph > 0 ? Mathf.Lerp(.1f, .5f, 1 - Telegraph / Mathf.Max(.01f, windupDuration)) : followThrough > 0 ? Mathf.Lerp(.5f, 1, 1 - followThrough / (kind == "gunner" ? .34f : .42f)) : 0;
                weaponRig.Present(lockedAim - transform.position - Vector3.up * 1.3f, phase, kind == "gunner");
            }
        }

        public void SetTelegraph(float seconds)
        {
            Telegraph = seconds;
        }

        public void SetExposed(float seconds)
        {
            Exposed = seconds;
            Telegraph = 0;
        }

        public void Damage(float amount, Vector3 force, bool core = false)
        {
            if (!Alive)
                return;
            if (boss && !core)
            {
                amount *= .035f;
                director.Toast("장갑이 공격을 흡수합니다 · 양쪽 축전기에 로프를 연결하세요", 1.8f);
            }

            Health = Mathf.Max(0, Health - amount);
            if(Health>0)director.Audio.Play("hurt",transform.position,.25f,2);
            hurt = .2f;
            knockback = force;
            if (core)
                SignalEffects.Impact(transform.position + Vector3.up * 1.8f, Vector3.right, SignalEffects.Gold, 1.6f);
            director.DamageNumber(transform.position + Vector3.up * 2.5f, Mathf.CeilToInt(amount), core);
            if (Health <= 0)
            {
                CorpseBlood.Attach(gameObject);
                director.EnemyDied(this);
                controller.enabled = false;
                deathTime = 0;
                impactLean = -Mathf.Sign(force.x) * 13;
                actor.Pose(6, Facing);
                director.Audio.Play("death", transform.position, .27f, 2);
                SignalEffects.Dust(transform.position, Vector3.up, .7f);
            }
        }
    }
}
