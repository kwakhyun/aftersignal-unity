using UnityEngine;

namespace AfterSignal
{
    public sealed class PoliceHelicopter : MonoBehaviour
    {
        public WorldActor Body { get; private set; }

        WantedSystem system;
        Transform rotor, tailRotor;
        Light searchlight;
        float clock, fireClock = 3, warning, fall;
        Vector3 aim;
        bool retreat;
        Vector3 crashVelocity;
        float smokeAt;
        bool crashing;
        ParticleSystem damageSmoke,damageFire;
        public static PoliceHelicopter Create(WantedSystem owner, Vector3 at)
        {
            var go = new GameObject("POLICE / aerial response", typeof(WorldActor), typeof(PoliceHelicopter));
            go.transform.position = at;
            go.layer = 9;
            var h = go.GetComponent<PoliceHelicopter>();
            h.system = owner;
            h.Body = go.GetComponent<WorldActor>();
            h.Body.police = h.Body.helicopter = true;
            h.Body.health = 4000;
            var c = go.AddComponent<SphereCollider>();
            c.radius = 2.5f;
            c.isTrigger = true;
            WorldGeometry.Part(go.transform, "Armoured fuselage", Vector3.zero, new Vector3(3.4f, 2.3f, 6.2f), "DarkMetal", PrimitiveType.Sphere);
            WorldGeometry.Part(go.transform, "Cockpit glazing", new Vector3(0, .35f, 2), new Vector3(2.8f, 1.55f, 2.4f), "DistrictWindow", PrimitiveType.Sphere);
            WorldGeometry.Part(go.transform, "Tail boom", new Vector3(0, .4f, -4.8f), new Vector3(.5f, .6f, 5.5f), "DarkMetal");
            WorldGeometry.Part(go.transform, "Tail fin", new Vector3(0, 1.2f, -7.4f), new Vector3(.18f, 2.2f, 1.4f), "DistrictBlue");
            foreach (float side in new[]
            {
                -1f,
                1f
            }

            )
            {
                WorldGeometry.Part(go.transform, "Landing skid", new Vector3(side * 1.8f, -1.55f, 0), new Vector3(.16f, .16f, 6.5f), "Chrome");
                WorldGeometry.Part(go.transform, "Skid strut", new Vector3(side * 1.55f, -1, 1), new Vector3(.14f, 1, .14f), "Chrome");
                WorldGeometry.Part(go.transform, "Skid strut", new Vector3(side * 1.55f, -1, -1.5f), new Vector3(.14f, 1, .14f), "Chrome");
                WorldGeometry.Part(go.transform, "Door blue stripe", new Vector3(side * 1.6f, .1f, 0), new Vector3(.08f, .4f, 3), "DistrictBlue");
            }

            h.rotor = new GameObject("Main rotor").transform;
            h.rotor.SetParent(go.transform, false);
            h.rotor.localPosition = Vector3.up * 1.65f;
            WorldGeometry.Part(h.rotor, "Rotor blade A", Vector3.zero, new Vector3(12, .08f, .32f), "Chrome");
            WorldGeometry.Part(h.rotor, "Rotor blade B", Vector3.zero, new Vector3(.32f, .08f, 12), "Chrome");
            h.tailRotor = new GameObject("Tail rotor").transform;
            h.tailRotor.SetParent(go.transform, false);
            h.tailRotor.localPosition = new Vector3(.4f, .8f, -7.1f);
            WorldGeometry.Part(h.tailRotor, "Tail blades", Vector3.zero, new Vector3(.08f, 2.5f, .2f), "Chrome");
            var spot = new GameObject("Searchlight");
            spot.transform.SetParent(go.transform, false);
            spot.transform.localPosition = new Vector3(0, -1, 1.8f);
            h.searchlight = spot.AddComponent<Light>();
            h.searchlight.type = LightType.Spot;
            h.searchlight.range = 160;
            h.searchlight.spotAngle = 32;
            h.searchlight.intensity = 40;
            h.searchlight.shadows = UnityEngine.LightShadows.None;
            WorldGeometry.Part(go.transform, "Nose gun", new Vector3(0, -.75f, 3.3f), new Vector3(.18f, .2f, 1.5f), "Metal");
            return h;
        }

        public bool CanSee()
        {
            var g = GameDirector.Instance;
            if(!g||!Body.Alive||Vector3.Distance(transform.position,g.Player.Shoulder)>150)return false;
            if(!Physics.Linecast(transform.position,g.Player.Shoulder,out var hit,1,QueryTriggerInteraction.Ignore))return true;
            return UrbanSimulation.Instance&&UrbanSimulation.Instance.Current&&hit.transform.IsChildOf(UrbanSimulation.Instance.Current.transform);
        }

        void Update()
        {
            var g = GameDirector.Instance;
            if (!g || g.Blocked)
                return;
            float dt = Mathf.Min(.07f, Time.deltaTime);
            clock += dt;
            if(Body.health<2000&&!damageSmoke)damageSmoke=VehicleDamagePresentation.Emitter(transform,"Engine smoke",Vector3.up,2,false);
            if(Body.health<1000&&!damageFire)damageFire=VehicleDamagePresentation.Emitter(transform,"Engine fire",Vector3.up,1.5f,true);
            rotor.Rotate(0, (Body.Alive ? 1250 : 220) * dt, 0);
            tailRotor.Rotate(1400 * dt, 0, 0);
            if (!Body.Alive)
            {
                fall += dt;
                crashVelocity += Vector3.down*9.81f*dt;
                var delta=crashVelocity*dt;
                bool impact=Physics.SphereCast(transform.position,1.5f,delta.normalized,out var crashHit,delta.magnitude,1,QueryTriggerInteraction.Ignore);
                transform.position=impact?crashHit.point+crashHit.normal*1.5f:transform.position+delta;
                transform.Rotate(12*dt,45*dt,(35+fall*12)*dt);
                if(fall>smokeAt){smokeAt=fall+.14f;VehicleExplosion.Smoke(transform.position,3);SignalEffects.Burst(transform.position,SignalEffects.Gold,3,4);}
                if (impact || transform.position.y < -15)
                {
                    g.Audio.Play("urban_explosion",transform.position,.9f,3);
                    VehicleExplosion.Create(transform.position, 4);
                    WreckFragments.Shatter(gameObject,12);
                    Destroy(gameObject);
                }

                return;
            }

            if (retreat)
            {
                transform.position += new Vector3(0, 12, 20) * dt;
                if (clock > 6)
                    Destroy(gameObject);
                return;
            }

            var priority=IncidentCommand.Monster(transform.position,650);
            if(IncidentCommand.Emergency&&!priority){warning=0;return;}
            Vector3 player = priority?priority.Center:g.Player.transform.position;
            Vector3 focus=IncidentCommand.Emergency?IncidentCommand.Position:system.LastSeen;
            Vector3 target = focus + new Vector3(Mathf.Cos(clock * .16f) * 28, Mathf.Max(28, player.y - system.LastSeen.y + 22), Mathf.Sin(clock * .16f) * 28);
            if (Physics.Linecast(transform.position, target, out var obstruction, 1, QueryTriggerInteraction.Ignore))
                target.y = Mathf.Max(target.y, obstruction.collider.bounds.max.y + 8);
            transform.position = Vector3.MoveTowards(transform.position, target, 18 * dt);
            var direction = player - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > .1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 2);
            var tracked=priority?priority.Center:UrbanSimulation.Instance&&UrbanSimulation.Instance.Current?UrbanSimulation.Instance.Current.transform.position+Vector3.up:g.Player.Shoulder;
            searchlight.transform.rotation=Quaternion.Slerp(searchlight.transform.rotation,Quaternion.LookRotation(tracked-searchlight.transform.position),1-Mathf.Exp(-dt*7));
            fireClock -= dt;
            if (warning > 0)
            {
                warning -= dt;
                if (warning <= 0)
                {
                    Fire(g);
                    fireClock = WantedSystem.Level >= 5 ? 1.5f : 2.3f;
                }
            }
            else if (fireClock <= 0 && (priority?FactionCombat.Visible(transform.position,priority.Center,200):CanSee()))
            {
                aim = priority?priority.Center:g.Player.Shoulder;
                warning = .7f;
            }
        }

        void Fire(GameDirector g)
        {
            Vector3 start = transform.position + transform.forward * 3 - Vector3.up;
            Vector3 direction = (aim - start).normalized;
            var end = start + direction * 95;
            if (Physics.Raycast(start, direction, out var hit, 95, (1 << 0) | (1 << 8) | (1 << 9), QueryTriggerInteraction.Collide))
            {
                end = hit.point;
                if (!IncidentCommand.Emergency && hit.collider.GetComponentInParent<PlayerMotor>())
                    g.Player.ReceiveDamage(16, transform.position);
                var car = hit.collider.GetComponentInParent<CityVehicle>();
                var civilian=hit.collider.GetComponentInParent<WorldActor>();
                if(civilian&&!civilian.police)civilian.Damage(16,direction*3,Body);
                if (car)
                    car.Damage(12, hit.point,Body);
            }

            for (int i = 0; i < 3; i++)
                CombatVfx.Tracer(start + transform.right * (i - 1) * .12f,end,SignalEffects.Gold);
            g.Audio.PlayGun(GunshotKind.Automatic, start, 1.1f);
        }

        public void Crash()
        {
            if(crashing)return;crashing=true;fall=0;
            crashVelocity=transform.forward*12+Vector3.down*2;
            var c = GetComponent<Collider>();
            if (c)
                c.enabled = false;
            searchlight.enabled = false;
            GameDirector.Instance?.Audio.Play("urban_explosion", transform.position, .3f, 2);
        }

        public void Withdraw()
        {
            retreat = true;
            clock = 0;
            searchlight.enabled = false;
        }
    }
}
