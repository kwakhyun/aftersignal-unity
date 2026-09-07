using UnityEngine;

namespace AfterSignal
{
    public enum CityVehicleType
    {
        Sedan,
        Taxi,
        Bus,
        Truck
    }

    public sealed class CityVehicle : MonoBehaviour
    {
        public CityVehicleType type;
        public float fuel = 45, health = 100;
        public const float Capacity = 45;
        public bool occupied, traffic, owned;
        public float speed;
        public Vector3[] route;
        public int waypoint;
        public float Distance { get; private set; }
        public int Collisions { get; private set; }
        public int Explosions { get; private set; }
        public bool Wrecked => health <= 0;
        public bool WaitingAtSignal { get; private set; }
        public Vector3 Forward => transform.right;
        public float HalfLength => type == CityVehicleType.Bus ? 4.6f : type == CityVehicleType.Truck ? 3.9f : 2.55f;
        public float HalfWidth => (int)type >= 2 ? 1.25f : 1.05f;

        readonly System.Collections.Generic.List<Transform> wheels = new System.Collections.Generic.List<Transform>();
        Light[] lamps;
        readonly RaycastHit[] hits = new RaycastHit[32];
        AudioSource engine;
        float impactCooldown, smokeClock, brakeCooldown;
        Renderer[] body;
        MaterialPropertyBlock paint;
        bool exploded;
        void Start()
        {
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name == "Wheel tire" || t.name == "Wheel alloy")
                    wheels.Add(t);
            body = GetComponentsInChildren<Renderer>();
            paint = new MaterialPropertyBlock();
            lamps = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Vehicle headlight");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(HalfLength + .1f, .72f, i == 0 ? -.65f : .65f);
                go.transform.localRotation = Quaternion.LookRotation(new Vector3(1, -.1f, 0));
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 28;
                l.spotAngle = 52;
                l.intensity = 7;
                l.color = new Color(.8f, .9f, 1);
                lamps[i] = l;
            }

            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = Resources.Load<AudioClip>("Audio/urban_engine_" + type.ToString().ToLowerInvariant());
            if (!engine.clip)
                engine.clip = Resources.Load<AudioClip>("Audio/urban_engine");
            engine.loop = true;
            engine.spatialBlend = 1;
            engine.minDistance = 3;
            engine.maxDistance = 32;
            engine.rolloffMode = AudioRolloffMode.Linear;
            engine.priority = owned ? 50 : 180;
            engine.volume = 0;
            if (engine.clip)
                engine.Play();
            ApplyDamageLook();
        }

        public void Drive(ControlFrame input, float dt)
        {
            if (Wrecked)
            {
                speed = 0;
                return;
            }

            float throttle = input.move.y, top = type == CityVehicleType.Bus ? 21 : type == CityVehicleType.Truck ? 23 : 27;
            float target = fuel > .001f ? (throttle >= 0 ? throttle * top : throttle * 7) : 0;
            float accel = input.jump || input.guard ? 24 : Mathf.Abs(throttle) < .1f ? 3.8f : Mathf.Sign(target) != Mathf.Sign(speed) ? 17 : (int)type >= 2 ? 5 : 8;
            if ((input.jump || input.guard) && Mathf.Abs(speed) > 15 && brakeCooldown <= 0)
            {
                GameDirector.Instance?.Audio.Play("urban_brake", transform.position, .2f, 1);
                brakeCooldown = 2;
            }

            brakeCooldown -= dt;
            speed = Mathf.MoveTowards(speed, input.jump || input.guard ? 0 : target, accel * dt);
            float steer = input.move.x * Mathf.Sign(speed) * Mathf.Clamp01(Mathf.Abs(speed) / 3) * ((int)type >= 2 ? 43 : 64) * dt;
            transform.Rotate(0, steer, 0, Space.World);
            Advance(Forward * speed * dt, dt, false);
        }

        public void TickTraffic(float dt)
        {
            if (!traffic || Wrecked || route == null || route.Length < 2)
                return;
            Vector3 d = route[waypoint] - transform.position;
            d.y = 0;
            if (d.magnitude < .04f)
            {
                waypoint = (waypoint + 1) % route.Length;
                d = route[waypoint] - transform.position;
                d.y = 0;
            }

            Vector3 heading = d.normalized;
            float target = d.magnitude < 8 ? 4 : 9;
            float stop = CityRoadNetwork.StopDistance(transform.position, heading, HalfLength);
            WaitingAtSignal = stop < 22;
            target = Mathf.Min(target, Mathf.Sqrt(Mathf.Max(0, stop) * 7));
            var sim = UrbanSimulation.Instance;
            if (sim)
                foreach (var other in sim.Cars)
                    if (other && other != this)
                    {
                        var gap = other.transform.position - transform.position;
                        float along = Vector3.Dot(gap, heading) - HalfLength - other.HalfLength - 2;
                        if (along > -1 && along < 22 && Mathf.Abs(Vector3.Dot(gap, new Vector3(-heading.z, 0, heading.x))) < HalfWidth + other.HalfWidth + .2f)
                            target = Mathf.Min(target, Mathf.Sqrt(Mathf.Max(0, along) * 7));
                    }

            if (CityPopulation.Instance && CityPopulation.Instance.CrossingAhead(this))
                target = 0;
            speed = Mathf.MoveTowards(speed, target, (target < speed ? 12 : 3) * dt);
            float distance = Mathf.Min(d.magnitude, speed * dt, Mathf.Max(0, stop));
            if (distance > 0)
            {
                transform.rotation = Quaternion.Euler(0, Mathf.Atan2(-heading.z, heading.x) * Mathf.Rad2Deg, 0);
                Advance(heading * distance, dt, true);
            }

            AudioLevel();
        }

        void Advance(Vector3 delta, float dt, bool ai)
        {
            Vector3 old = transform.position;
            float length = delta.magnitude;
            impactCooldown -= dt;
            if (length > 0)
            {
                int n = Physics.BoxCastNonAlloc(old + Vector3.up * .9f, new Vector3(HalfLength - .08f, .5f, HalfWidth - .08f), delta.normalized, hits, transform.rotation, length + .12f, 1, QueryTriggerInteraction.Ignore);
                float allowed = length;
                Collider hit = null;
                for (int i = 0; i < n; i++)
                {
                    var h = hits[i];
                    if (!h.collider || h.collider.transform.IsChildOf(transform))
                        continue;
                    if (h.distance < allowed + .08f)
                    {
                        allowed = Mathf.Min(allowed, Mathf.Max(0, h.distance - .08f));
                        hit = h.collider;
                    }
                }

                transform.position = old + delta.normalized * allowed;
                var g = GameDirector.Instance;
                if (g)
                {
                    var p = transform.position;
                    p.x = Mathf.Clamp(p.x, 4, g.stageLength - 4);
                    p.z = Mathf.Clamp(p.z, -g.halfDepth + 4, g.halfDepth - 4);
                    transform.position = p;
                }

                float moved = Vector3.Distance(old, transform.position);
                Distance += moved;
                foreach (var wheel in wheels)
                    wheel.Rotate(0, moved * Mathf.Sign(speed) * 130, 0, Space.Self);
                if (owned)
                    fuel = Mathf.Max(0, fuel - moved * ((int)type >= 2 ? .024f : .014f));
                CityPopulation.Instance?.VehicleSweep(this, old, transform.position, speed);
                if (hit)
                {
                    if (Mathf.Abs(speed) > 4 && impactCooldown <= 0)
                    {
                        Collisions++;
                        impactCooldown = .6f;
                        float force = Mathf.Abs(speed);
                        var other = hit.GetComponentInParent<CityVehicle>();
                        if (other)
                            force = (Forward * speed - other.Forward * other.speed).magnitude;
                        if (!ai || force > 10)
                        {
                            Damage(Mathf.Max(3, (force - 3) * (force - 3) * .22f), transform.position + Forward * HalfLength);
                            if (other)
                                other.Damage(force * 1.4f, other.transform.position - Forward * other.HalfLength);
                        }

                        g?.Audio.Play("urban_crash", transform.position, .32f, 1);
                        if (owned)
                            g?.CameraRig.Kick(.09f);
                    }

                    speed = 0;
                }
            }

            AudioLevel();
        }

        public void Damage(float amount, Vector3 contact)
        {
            if (Wrecked || amount <= 0)
                return;
            health = Mathf.Max(0, health - amount);
            ApplyDamageLook();
            SignalEffects.Burst(contact, SignalEffects.Gold, 8, 4);
            if (Wrecked && !exploded)
            {
                exploded = true;
                Explosions++;
                speed = 0;
                fuel = 0;
                traffic = false;
                var g = GameDirector.Instance;
                g?.Audio.Play("urban_explosion", transform.position, .55f, 1);
                VehicleExplosion.Create(transform.position, HalfLength);
                if (owned)
                {
                    g?.CameraRig.Kick(.15f);
                    g?.Toast("차량 파손 · 안전한 곳으로 탈출했습니다");
                    UrbanSimulation.Instance?.EmergencyExit(this);
                }

                occupied = false;
            }
        }

        void ApplyDamageLook()
        {
            if (body == null)
                return;
            float damage = 1 - health / 100;
            foreach (var r in body)
            {
                if (!r || !(r.name == "Sculpted chassis" || r.name == "Sedan roof" || r.name == "Cargo shell"))
                    continue;
                paint.Clear();
                Color color = r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                paint.SetColor("_BaseColor", Color.Lerp(color, new Color(.075f, .075f, .07f), damage * .88f));
                r.SetPropertyBlock(paint);
            }

            var hood = transform.Find("Sculpted chassis");
            if (hood)
                hood.localRotation = Quaternion.Euler(0, 0, damage * 3);
        }

        void AudioLevel()
        {
            if (!engine)
                return;
            var g = GameDirector.Instance;
            engine.pitch = ((int)type >= 2 ? .6f : .75f) + Mathf.Abs(speed) * .035f;
            engine.volume = (Wrecked || fuel <= 0 ? 0 : occupied ? .035f + Mathf.Abs(speed) * .002f : .002f) * (g && g.Audio ? g.Audio.Volume : 1);
        }

        void Update()
        {
            var g = GameDirector.Instance;
            if (engine)
                engine.mute = g && (g.Blocked || g.Audio.Volume <= 0);
            if (lamps != null)
                foreach (var l in lamps)
                    l.enabled = owned && occupied && !Wrecked;
            if (health < 40 && g && !g.Blocked)
            {
                smokeClock -= Time.deltaTime;
                if (smokeClock <= 0)
                {
                    smokeClock = 1.2f;
                    VehicleExplosion.Smoke(transform.position + Forward * HalfLength * .65f + Vector3.up, health <= 0 ? 2 : 1);
                }
            }
        }
    }
}
