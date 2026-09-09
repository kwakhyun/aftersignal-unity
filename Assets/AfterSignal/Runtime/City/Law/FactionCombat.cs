using UnityEngine;

namespace AfterSignal
{
    // One projectile rule for both factions: scenery and bystanders block fire;
    // every first-hit civilian can take collateral damage from either faction.
    public static class FactionCombat
    {
        static readonly RaycastHit[] hits = new RaycastHit[64];
        static readonly System.Collections.Generic.List<WorldActor> neighbors=new();
        const int Mask = (1 << 0) | (1 << 8) | (1 << 9);

        public static bool Visible(Vector3 from, Vector3 to, float distance = 44)
        {
            return (to - from).sqrMagnitude <= distance * distance
                && !Physics.Linecast(from, to, 1, QueryTriggerInteraction.Ignore);
        }

        public static WorldActor NearestOpponent(WorldActor self, float range)
        {
            if(!TacticalJudgment.Active(self)||self.Downed)return null;
            if(self.police||self.military){var titan=IncidentCommand.Monster(self.Center,Mathf.Max(range,350));if(TacticalJudgment.Opponent(self,titan))return titan;}
            WorldActor result = null;
            float closest = range * range;
            ActorSpatialIndex.Nearby(self.transform.position,range+4,neighbors);
            foreach (var other in neighbors)
            {
                if (!TacticalJudgment.Opponent(self,other)||other.helicopter||self.gang&&!other.police&&!other.military&&!other.monster) continue;
                float distance = (other.Center - self.Center).sqrMagnitude;
                if (distance >= closest || !Visible(self.Center, other.Center, range)) continue;
                closest = distance;
                result = other;
            }
            return result;
        }

        public static WorldActor WoundedVictim(WorldActor self,float range)
        {
            WorldActor best=null;float nearest=range*range;ActorSpatialIndex.Nearby(self.transform.position,range+4,neighbors);foreach(var a in neighbors){if(!a||a==self||!a.Alive||!a.Downed||a.gang||a.monster||a.robot||a.environmental)continue;float d=(a.Center-self.Center).sqrMagnitude;if(d<nearest&&Visible(self.Center,a.Center,range)){nearest=d;best=a;}}return best;
        }

        public static void Fire(WorldActor source, Vector3 muzzle, Vector3 target, float range, float damage, Color color, bool hostilePlayer, Transform firingMount=null)
        {
            if(!TacticalJudgment.Active(source)||source.Downed||!LocalSimulation.Combat(muzzle))return;
            var mounted=firingMount?firingMount.GetComponentInParent<CityVehicle>():source.GetComponentInParent<CityVehicle>();
            if(mounted&&!mounted.occupied&&!(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==mounted))return;
            var direction = (target - muzzle).normalized;
            var end = muzzle + direction * range;
            if (!firingMount && Physics.Linecast(source.Center, muzzle, out var wall, 1, QueryTriggerInteraction.Ignore))
            {
                return;
            }
            int count = Physics.RaycastNonAlloc(muzzle, direction, hits, range, Mask, QueryTriggerInteraction.Collide);
            var contacts = hits;
            if (count == hits.Length)
            {
                contacts = Physics.RaycastAll(muzzle, direction, range, Mask, QueryTriggerInteraction.Collide);
                count = contacts.Length;
            }
            int nearest = -1;
            float distance = range;
            for (int i = 0; i < count; i++)
            {
                if (Ballistics.Ignore(contacts[i],source.transform,firingMount) || contacts[i].distance >= distance) continue;
                nearest = i;
                distance = contacts[i].distance;
            }
            if (nearest >= 0)
            {
                var hit = contacts[nearest];
                end = hit.point;CombatVfx.Hit(hit,direction);
                var victim = hit.collider.GetComponentInParent<WorldActor>();
                if (victim && victim.Alive && (source.monster ? !victim.monster : source.police||source.military ? !victim.police&&!victim.military : !victim.gang))
                    victim.Damage(damage, direction * 2, source);
                var car=hit.collider.GetComponentInParent<CityVehicle>();
                if(car)car.Damage(damage*.65f,hit.point,source);
                if (hostilePlayer)
                    hit.collider.GetComponentInParent<PlayerMotor>()?.ReceiveDamage(Mathf.RoundToInt(damage), source.transform.position);
            }
            CombatVfx.Tracer(muzzle,end,color);
        }
    }
}
