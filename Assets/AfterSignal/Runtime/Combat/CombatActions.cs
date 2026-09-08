using UnityEngine;
using System.Collections.Generic;

namespace AfterSignal
{
    public enum WeaponAction
    {
        Combo,
        Dash,
        Skill
    }

    public sealed partial class PlayerMotor
    {
        public WeaponAction Action { get; private set; }
        public AttackTiming ActiveTiming => currentTiming ?? Tuning.Timing(Weapon);
        public int Ammo { get; private set; }
        public int Reloads { get; private set; }
        public int DashAttacks { get; private set; }
        public int SkillsUsed { get; private set; }
        public int ShotsFired { get; private set; }
        public int WeaponChanges { get; private set; }
        public bool Reloading => reloadRemaining > 0 || Equipment&&Equipment.Reloading;
        public float ReloadProgress => Equipment&&Equipment.Extended?Equipment.ReloadProgress:Reloading ? 1 - reloadRemaining / Tuning.reloadDuration : 0;
        public float SkillCooldown => Mathf.Max(0, skillCooldown);
        public string SkillName => Weapon == WeaponId.Katana ? "월광참" : Weapon == WeaponId.Greatsword ? "지각 붕괴" : "전술 연사";

        AttackTiming currentTiming;
        float reloadRemaining, dashAttackWindow;
        int pendingWeapon = -1, reloadPhase, shotsInAction;
        readonly HashSet<WorldActor> worldStruck = new HashSet<WorldActor>();
        void UpdateArsenal(ControlFrame input, float dt)
        {
            Equipment?.Tick(input,dt);
            if(Equipment&&Equipment.Extended){pendingWeapon=-1;return;}
            dashAttackWindow = Mathf.Max(0, dashAttackWindow - dt);
            if (input.weapon >= 0)
                pendingWeapon = Mathf.Clamp(input.weapon, 0, 2);
            if (input.weaponCycle != 0)
                pendingWeapon = ((pendingWeapon >= 0 ? pendingWeapon : (int)Weapon) + (input.weaponCycle > 0 ? 1 : 2)) % 3;
            if (pendingWeapon >= 0 && AttackTime <= 0)
            {
                if ((int)Weapon != pendingWeapon)
                {
                    CancelReload();
                    Weapon = (WeaponId)pendingWeapon;
                    Combo = 0;
                    comboWindow = 0;
                    WeaponChanges++;
                    Director.Audio.Play("weapon_switch", Shoulder, .22f, 1);
                }

                pendingWeapon = -1;
            }

            if (input.reload && Weapon == WeaponId.Pistol && AttackTime <= 0 && HurtTime <= 0)
                BeginReload();
            if (!Reloading)
                return;
            reloadRemaining = Mathf.Max(0, reloadRemaining - dt);
            if (reloadPhase == 0 && ReloadProgress > .54f)
            {
                reloadPhase = 1;
                Director.Audio.Play("reload_in", Shoulder, .3f, 2);
            }

            if (reloadRemaining <= 0)
            {
                Ammo = Tuning.magazineSize;
                Reloads++;
                Director.Audio.Play("reload_slide", Shoulder, .32f, 2);
                Director.Toast("장전 완료 · " + Ammo + "발", 1.2f);
            }
        }

        public void BeginReload()
        {
            if(Equipment&&Equipment.Extended){Equipment.BeginReload();return;}
            if (Weapon != WeaponId.Pistol || Reloading || Ammo >= Tuning.magazineSize || AttackTime > 0 || HurtTime > 0)
                return;
            reloadRemaining = Tuning.reloadDuration;
            reloadPhase = 0;
            Guarding = false;
            Director.Audio.Play("reload_out", Shoulder, .26f, 2);
        }

        void CancelReload()
        {
            reloadRemaining = 0;
            reloadPhase = 0;
        }

        void FirePistol(float damage, bool charged)
        {
            if (!charged)
            {
                if (Ammo <= 0)
                    return;
                Ammo--;
            }

            ShotsFired++;
            Vector3 muzzle = Muzzle;
            Vector3 direction = (Aim - muzzle).normalized;
            if (direction.sqrMagnitude < .1f)
                direction = Vector3.right * Facing;
            var end = muzzle + direction * Ballistics.Range;
            if (Ballistics.Cast(muzzle, direction, Ballistics.Range, transform, out var hit))
            {
                end = hit.point;
                hit.collider.GetComponentInParent<FacadeGlass>()?.Hit(hit.point,damage);
                var enemy = hit.collider.GetComponentInParent<EnemyBrain>();
                if (enemy && enemy.Alive)
                {
                    enemy.Damage(damage, direction * 3);
                    OnContact(enemy, hit.point, direction);
                }

                var glass = hit.collider.GetComponentInParent<BreakableGlass>();
                if (glass)
                    glass.Hit(damage);
                var citizen = hit.collider.GetComponentInParent<WorldActor>();
                if (citizen)
                    citizen.Damage(damage, direction * 4);
                var car = hit.collider.GetComponentInParent<CityVehicle>();
                if (car)
                {
                    bool occupied=car.occupied||car.GetComponent<VehicleCabin>()&&car.GetComponent<VehicleCabin>().PassengerCount>0;
                    car.Damage(damage, hit.point);
                    if(occupied&&!car.GetComponent<PoliceCar>())WantedSystem.Report(7,car.transform.position);
                    if (car.GetComponent<PoliceCar>())
                        WantedSystem.Report(12, car.transform.position);
                }
            }

            var color = charged ? SignalEffects.Cyan : SignalEffects.Gold;
            SignalEffects.Beam(muzzle, end, color, charged ? .055f : .028f, .085f);
            SignalEffects.Impact(muzzle, direction, color, charged ? .52f : .32f);
            Director.Audio.PlayGun(GunshotKind.Pistol, muzzle, charged ? 1.12f : 1);
        }

        void ResolveSkill(float elapsed)
        {
            if (elapsed < ActiveTiming.contact)
                return;
            if (Weapon == WeaponId.Pistol)
            {
                while (shotsInAction < 5 && elapsed >= ActiveTiming.contact + shotsInAction * .08f)
                {
                    actor.TickHero(this, 0);
                    FirePistol(Tuning.pistolDamage * .65f, true);
                    shotsInAction++;
                }

                if (shotsInAction >= 5)
                    resolving = false;
                return;
            }

            if (swingStarted)
                return;
            swingStarted = true;
            resolving = false;
            Director.Audio.Play(Weapon == WeaponId.Katana ? "katana_wave" : "heavy_slam", Shoulder, .48f, 3);
            if (Weapon == WeaponId.Katana)
            {
                var go = new GameObject("Mooncut / travelling blade");
                go.transform.position = Shoulder;
                go.AddComponent<TravellingCut>().Initialize(Director, AttackHeading, Tuning.skillDamage);
                SignalEffects.Slash(Shoulder, AttackHeading, 3, SignalEffects.Cyan, 2);
            }
            else
            {
                var center = transform.position + AttackHeading * 1.6f;
                SignalEffects.Ring(center + Vector3.up * .15f, SignalEffects.Gold, 6.2f, .45f);
                SignalEffects.Dust(center, Vector3.up, 1.5f);
                Director.CameraRig.Impact(Vector3.down, .13f);
                foreach (var enemy in Director.Enemies)
                    if (enemy && enemy.Alive)
                    {
                        var d = enemy.transform.position - center;
                        if (new Vector2(d.x, d.z).magnitude < 6.2f && Mathf.Abs(d.y) < 3)
                        {
                            enemy.Damage(Tuning.skillDamage, new Vector3(Mathf.Sign(d.x) * 6, 0, 0));
                            OnContact(enemy, enemy.transform.position + Vector3.up, d.normalized);
                        }
                    }

                foreach (var glass in BreakableGlass.All)
                    if (glass && !glass.Broken && Vector3.Distance(center, glass.transform.position) < 6.7f)
                        glass.Hit(Tuning.skillDamage);
                WorldActor.Strike(center + Vector3.up, Vector3.zero, 6.2f, Tuning.skillDamage, worldStruck);
                StrikeVehicles(6.2f,Tuning.skillDamage);
            }
        }
    }
}
