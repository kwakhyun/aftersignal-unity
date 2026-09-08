using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    // Shared damage surface for citizens and the response units; campaign guards keep their own rules.
    public sealed class WorldActor : MonoBehaviour
    {
        public static readonly List<WorldActor> All = new List<WorldActor>();
        public float health = 70;
        public bool police, helicopter, protectedResident, gang;
        public bool monster, military;
        public float LastPlayerHit {get;private set;}=-100;
        bool deathHandled;
        public bool Alive => health > 0;
        public Vector3 Center => transform.position + Vector3.up * (helicopter ? 0 : 1.1f);

        float hurt;
        void OnEnable()
        {
            if (!All.Contains(this))
                All.Add(this);
        }
        void Start(){if(!helicopter&&!monster&&!GetComponent<NpcGroundSupport>())gameObject.AddComponent<NpcGroundSupport>();}

        void OnDisable()
        {
            All.Remove(this);
        }

        void LateUpdate()
        {
            if(Alive){if(!helicopter&&!GetComponent<NpcBody>())gameObject.AddComponent<NpcBody>();return;}
            if(!helicopter&&!GetComponent<CorpseBlood>())CorpseBlood.Attach(gameObject);
            if(!deathHandled){deathHandled=true;CreditDrop.From(this);}
        }

        public void ResetHealth()
        {
            GetComponent<SeveredSprite>()?.Restore();
            var impact=GetComponent<CivilianImpact>();if(impact)Destroy(impact);
            var defense=GetComponent<CivilianDefense>();if(defense)Destroy(defense);
            health = 70;
            deathHandled=false;
            hurt = 0;
        }

        public void Damage(float amount, Vector3 force, WorldActor source = null, bool blade = false)
        {
            if (!Alive || amount <= 0 || Time.time < hurt)
                return;
            hurt = Time.time + .08f;
            float before = health;
            if(!source)LastPlayerHit=Time.time;
            if(!police&&!gang&&!monster&&!military){CitySafety.Shock(transform.position);CitySafety.Alarm(transform.position,source,GetComponent<CityNpc>());}
            health = Mathf.Max(protectedResident ? 1 : 0, health - amount);
            if(!Alive)GetComponent<DirectionalPerson>()?.PreserveAppearance();
            if(!Alive&&blade&&!helicopter&&Random.value<.62f)SeveredSprite.Create(this,force);
            if(Alive&&!police&&!gang&&!helicopter&&!protectedResident&&!monster&&!military)CivilianDefense.React(this,source);
            NpcVoice.React(GetComponent<CityNpc>(),health<=0);
            // Existing weapon/vehicle callers are player attacks. Faction fire supplies its source.
            if (!source && !gang && !monster)
                WantedSystem.Report(police ? 12 : health <= 0 ? 24 : 9, transform.position);
            var game = GameDirector.Instance;
            if(!helicopter)game?.Audio.Play(Alive?"hurt":"death",Center,.34f,2);
            if(!helicopter)game?.Audio.Play(blade?"blade_hit":"urban_impact",Center,blade?.35f:.3f,3);
            game?.DamageNumber(Center + Vector3.up, Mathf.CeilToInt(amount), false);
            SignalEffects.Impact(Center, force.normalized, police ? SignalEffects.Gold : SignalEffects.Red, .5f);
            var gangster = GetComponent<GangMember>();
            if (gangster)
            {
                gangster.OnHit(force, !source);
                return;
            }
            var officer = GetComponent<PoliceOfficer>();
            if (officer)
            {
                officer.OnHit(force);
                return;
            }

            var heli = GetComponent<PoliceHelicopter>();
            if (heli)
            {
                if (!Alive)
                    heli.Crash();
                return;
            }

            var pedestrian = GetComponent<CityPedestrian>();
            if (pedestrian)
            {
                if(CivilianDefense.Active(this))return;
                pedestrian.Hit(force, health <= 0 ? 14 : 6);
                return;
            }

            GetComponent<CityNpc>()?.ReactToAttack(health <= 0, force);
            if (before > 0 && !Alive)
            {
                var hitbox = GetComponent<Collider>();
                if (hitbox)
                    hitbox.enabled = false;
            }
        }

        public void VehicleHit(Vector3 direction, float speed)
        {
            if (!Alive || Time.time < hurt)
                return;
            Damage(speed >= 12 ? 100 : speed * 4, direction * Mathf.Max(3, speed));
            if(!helicopter&&speed>3)CivilianImpact.Launch(this,direction,speed);
        }

        public static void Strike(Vector3 origin, Vector3 direction, float range, float damage, HashSet<WorldActor> struck, bool blade=false)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .01f)
                direction.Normalize();
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var actor = All[i];
                if (!actor || !actor.Alive || struck.Contains(actor))
                    continue;
                var d = actor.Center - origin;
                if (d.magnitude > range || Mathf.Abs(d.y) > 3 || direction.sqrMagnitude > .1f && Vector3.Dot(d.normalized, direction) < .05f)
                    continue;
                if (Physics.Linecast(origin, actor.Center, 1, QueryTriggerInteraction.Ignore))
                    continue;
                struck.Add(actor);
                actor.Damage(damage, d.normalized * 5, null, blade);
            }
        }
    }
}
