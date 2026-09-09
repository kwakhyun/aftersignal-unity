using UnityEngine;

namespace AfterSignal
{
    public enum CityVehicleType
    {
        Sedan,
        Taxi,
        Bus,
        Truck,
        Motorcycle,
        SportsCar,
        Boat,
        Airliner,
        CombatHelicopter,
        Fighter,
        Tank,
        Bomber
    }

    public sealed partial class CityVehicle : MonoBehaviour
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
        public bool IsAircraft => type == CityVehicleType.Airliner || type == CityVehicleType.CombatHelicopter || type == CityVehicleType.Fighter || type == CityVehicleType.Bomber;
        public bool IsWatercraft => type == CityVehicleType.Boat;
        public bool IsHeavy => type == CityVehicleType.Bus || type == CityVehicleType.Truck || type == CityVehicleType.Tank;
        public bool IsSpecial => IsAircraft || IsWatercraft;
        public float Steering { get; private set; }
        public float TopSpeed => type == CityVehicleType.Motorcycle ? 45 : type == CityVehicleType.SportsCar ? 49 : type == CityVehicleType.Tank ? 18 : type == CityVehicleType.Bus ? 21 : type == CityVehicleType.Truck ? 23 : 27;
        public float HalfLength => type==CityVehicleType.Bomber?14:IsWatercraft?WaterLength:type == CityVehicleType.Bus ? 4.6f : type == CityVehicleType.Truck ? 3.9f : type == CityVehicleType.Motorcycle ? 1.25f : type == CityVehicleType.Airliner ? 15 : type == CityVehicleType.CombatHelicopter ? 5.5f : type == CityVehicleType.Fighter ? 7.5f : type == CityVehicleType.Tank ? 4 : 2.55f;
        public float HalfWidth => type==CityVehicleType.Bomber?26:IsWatercraft?WaterWidth:type == CityVehicleType.Motorcycle ? .42f : type == CityVehicleType.Airliner ? 2 : type == CityVehicleType.Tank ? 1.9f : IsHeavy ? 1.25f : 1.05f;
        float WaterLength{get{var hull=GetComponent<MaritimeHull>();return hull?hull.Length*.5f:GetComponent<AuthoredCraft>()?235:8;}}
        float WaterWidth{get{var hull=GetComponent<MaritimeHull>();return hull?hull.Beam*.5f:GetComponent<AuthoredCraft>()?19:2.5f;}}
        readonly VehicleNavigator navigator=new();
        public bool NavigatingAroundObstacle=>navigator.Detouring;
        readonly System.Collections.Generic.List<CityVehicle> nearbyTraffic=new();

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
            InitializeDurability();
            VehicleDetails.Install(this);
            if(IsWatercraft&&!GetComponent<SeaTraffic>())gameObject.AddComponent<SeaTraffic>();
            gameObject.AddComponent<VehicleDamagePresentation>();
            gameObject.AddComponent<VehicleHorn>();
            if (IsSpecial) gameObject.AddComponent<CraftDynamics>().Initialize(this);
            else VehicleGround.Settle(this, .02f, true);
            gameObject.AddComponent<VehicleCabin>().Initialize(this);
            gameObject.AddComponent<VehicleCockpit>();
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name == "Wheel tire" || t.name == "Wheel alloy")
                {
                    wheels.Add(t);
                    var wheel=t.gameObject.AddComponent<VehicleWheel>();wheel.Initialize(this);
                }
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
            engine.clip = Resources.Load<AudioClip>("Audio/Transport/" + (GetComponent<AuthoredCraft>()?"ship":type==CityVehicleType.Bomber?"fighter":type.ToString().ToLowerInvariant()));
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
            gameObject.AddComponent<VehicleSoundscape>().Initialize(this,engine);
            ApplyDamageLook();
            ApplyCustomization();
        }

        public void Drive(ControlFrame input, float dt)
        {
            if(Tumbling)return;
            if(GetComponent<TitanGravitySnare>())return;
            if (Wrecked)
            {
                speed = 0;
                return;
            }

            if (IsSpecial) { GetComponent<CraftDynamics>()?.Drive(input, dt); return; }
            if (type == CityVehicleType.Tank) { DriveTank(input,dt); return; }
            bool braking=input.vertical>0||input.jump||input.guard;
            bool handbrakeTurn=braking&&Mathf.Abs(input.move.x)>.2f&&Mathf.Abs(speed)>7;
            Steering = Mathf.MoveTowards(Steering, input.move.x, dt * 5);
            float throttle = input.move.y, top = TopSpeed * (input.boost ? 1.32f : 1);
            float target = fuel > .001f ? (throttle >= 0 ? throttle * top : throttle * 7) : 0;
            float accel = handbrakeTurn ? 3.6f : braking ? 24 : Mathf.Abs(throttle) < .1f ? 3.8f : Mathf.Sign(target) != Mathf.Sign(speed) ? 17 : IsHeavy ? 5 : type == CityVehicleType.Motorcycle || type == CityVehicleType.SportsCar ? 14 : 8;
            if ((braking) && Mathf.Abs(speed) > 15 && brakeCooldown <= 0)
            {
                GameDirector.Instance?.Audio.Play("urban_brake", transform.position, .2f, 1);
                brakeCooldown = 2;
            }

            brakeCooldown -= dt;
            speed = Mathf.MoveTowards(speed, braking ? 0 : target, accel * (input.boost ? 1.5f : 1) * dt);
            float steer = input.move.x * Mathf.Sign(speed) * Mathf.Clamp01(Mathf.Abs(speed) / 3) * (IsHeavy ? 43 : type == CityVehicleType.Motorcycle ? 82 : 64) * (handbrakeTurn?1.5f:1) * dt;
            transform.Rotate(0, steer, 0, Space.World);
            Advance(DriftStep(input,braking,dt), dt, false);
            if(Mathf.Abs(speed)<.1f)roadVelocity=Vector3.zero;
        }

        public void TickTraffic(float dt)
        {
            if (IsSpecial || Tumbling) return;
            if(GetComponent<TrafficYield>()){speed=Mathf.MoveTowards(speed,0,dt*18);return;}
            if (!traffic || Wrecked || route == null || route.Length < 2)
                return;
            var emergency=GetComponent<VehicleEmergency>();
            bool escaping=emergency&&emergency.Escaping;
            var bus=GetComponent<CityBusLine>();
            if(bus && !escaping && bus.Prepare(dt)) return;
            Vector3 d = route[waypoint] - transform.position;
            d.y = 0;
            if (d.magnitude < .04f)
            {
                waypoint = (waypoint + 1) % route.Length;
                d = route[waypoint] - transform.position;
                d.y = 0;
            }

            Vector3 heading = LocalSimulation.Within(transform.position,420)?navigator.Direction(this,route[waypoint]):d.normalized;
            float target = d.magnitude < 8 ? (escaping?7:4) : escaping?20:9;
            if(navigator.Waiting)target=0;
            if(navigator.Detouring)target=Mathf.Min(target,5);
            if(bus && !escaping && waypoint<5) target=Mathf.Min(target,Mathf.Sqrt(Mathf.Max(0,d.magnitude-.03f)*4));
            float stop = CityRoadNetwork.StopDistance(transform.position, heading, HalfLength);
            WaitingAtSignal = stop < 22;
            target = Mathf.Min(target, Mathf.Sqrt(Mathf.Max(0, stop) * 7));
            var sim = UrbanSimulation.Instance;
            TrafficSpatialIndex.Nearby(transform.position,nearbyTraffic);
            if (sim)
                foreach (var other in nearbyTraffic)
                    if (other && other != this)
                    {
                        var gap = other.transform.position - transform.position;
                        float along = Vector3.Dot(gap, heading) - HalfLength - other.HalfLength - 2;
                        if (along > -1 && along < 22 && Mathf.Abs(Vector3.Dot(gap, new Vector3(-heading.z, 0, heading.x))) < HalfWidth + other.HalfWidth + .2f)
                            target = Mathf.Min(target, Mathf.Sqrt(Mathf.Max(0, along) * 7));
                    }

            if (CityPopulation.Instance && CityPopulation.Instance.CrossingAhead(this))
                target = 0;
            speed = Mathf.MoveTowards(speed, target, (target < speed ? 12 : escaping ? 8 : 3) * dt);
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
            RecoverContact();
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
                    if(h.distance<=.01f&&MovingOut(h.collider,delta))continue;
                    if(h.normal.y>.65f)continue;
                    if(h.distance<length+.1f && h.collider.GetComponentInParent<BreakableStreetProp>() && StructuralImpact.Hit(this,h.collider,h.point,Mathf.Abs(speed)))continue;
                    if (h.distance < allowed + .08f)
                    {
                        allowed = Mathf.Min(allowed, Mathf.Max(0, h.distance - .08f));
                        hit = h.collider;
                    }
                }

                transform.position = old + delta.normalized * allowed;
                VehicleGround.Settle(this, dt);
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

                if (owned)
                    fuel = Mathf.Max(0, fuel - moved * (IsHeavy ? .024f : .014f));
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
                        PushVehicle(other,delta,force);
                        if (!ai || force > 10)
                        {
                            CollisionDamage(force, transform.position + Forward * HalfLength,other);
                            if (other)
                                other.CollisionDamage(force, other.transform.position - Forward * other.HalfLength,this);
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

        public void Damage(float amount, Vector3 contact, WorldActor source = null, bool alertOccupants=true)
        {
            InitializeDurability();
            if (Wrecked || amount <= 0)
                return;
            RecordDamageSource(source);
            health = Mathf.Max(0, health - amount);
            var marine=GetComponent<SeaCombat>();if(marine&&marine.Body){marine.Body.health=health;CityIncidentBoard.Contribution(marine.Body,amount,source);}
            if(!Wrecked&&(alertOccupants||HealthFraction<.25f))VehicleEmergency.Hit(this,contact,false);
            ApplyDamageLook();
            SignalEffects.Burst(contact, SignalEffects.Gold, 8, 4);
            if (Wrecked && !exploded)
            {
                if(IsSpecial){if(!GetComponent<VehicleFailure>())VehicleFailure.Begin(this,source);}
                else Detonate(source);
            }
        }

        public void Detonate(WorldActor source=null)
        {
            if(exploded)return;exploded=true;Explosions++;health=0;fuel=0;traffic=false;
            source=source?source:DamageSource;
            var g=GameDirector.Instance;bool carrying=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==this;
            // Occupants take a bounded injury, never an unconditional instant death.
            if(carrying){g.Player.ProtectVehicleImpact();UrbanSimulation.Instance.EmergencyExit(this);g.Player.ReceiveDamage(35,transform.position,true);}
            VehicleEmergency.Hit(this,transform.position,true);occupied=false;speed=0;
            g?.Audio.Play("urban_explosion",transform.position,.55f,1);
            VehicleExplosion.Create(transform.position,Mathf.Clamp(HalfLength,2,15));
            BlastDamage.Create(transform.position,Mathf.Clamp(HalfLength*2.8f,7,25),145,source,this);
            WreckFragments.Shatter(gameObject,8);if(engine)engine.Stop();
            var failure=GetComponent<VehicleFailure>();if(failure)failure.enabled=false;
            Destroy(gameObject,7.8f);
        }

        void ApplyDamageLook()
        {
            if (body == null)
                return;
            float damage = 1 - HealthFraction;
            foreach (var r in body)
            {
                if (!r || !(r.name == "Sculpted chassis" || r.name == "Sedan roof" || r.name == "Cargo shell"))
                    continue;
                paint.Clear();
                Color color = customColor>=0&&customColor<colors.Length?colors[customColor]:r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                paint.SetColor("_BaseColor", Color.Lerp(color, new Color(.075f, .075f, .07f), damage * .88f));
                r.SetPropertyBlock(paint);
            }

            var hood = transform.Find("Sculpted chassis");
            if (hood)
                hood.localRotation = Quaternion.Euler(0, 0, damage * 3);
        }

        void AudioLevel()
        {
            if(GetComponent<VehicleSoundscape>())return;
            if (!engine)
                return;
            var g = GameDirector.Instance;
            engine.pitch = IsAircraft ? .7f + Mathf.Abs(speed)*.006f : IsWatercraft ? .65f + Mathf.Abs(speed)*.02f : (IsHeavy ? .6f : .75f) + Mathf.Abs(speed) * .035f;
            engine.volume = (Wrecked || fuel <= 0 ? 0 : occupied ? .035f + Mathf.Abs(speed) * .002f : .002f) * (g && g.Audio ? g.Audio.Volume*g.Audio.SfxVolume : 1);
        }

        void Update()
        {
            if(exploded)return;
            if(!GameDirector.Instance||!GameDirector.Instance.Blocked)TickCollisionDrift(Mathf.Min(.05f,Time.deltaTime));
            AudioLevel();
            var g = GameDirector.Instance;
            if (engine)
                engine.mute = g && (g.Blocked || g.Audio.Volume <= 0 || g.Audio.SfxVolume <= 0);
            if (lamps != null)
                foreach (var l in lamps)
                    l.enabled = owned && occupied && !Wrecked;
            if (HealthFraction < .12f && g && !g.Blocked && !GetComponent<VehicleDamagePresentation>())
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
