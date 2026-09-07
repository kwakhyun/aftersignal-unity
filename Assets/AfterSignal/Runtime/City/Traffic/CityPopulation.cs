using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed class CityPopulation : MonoBehaviour
    {
        public static CityPopulation Instance { get; private set; }

        public readonly List<CityPedestrian> Citizens = new List<CityPedestrian>();
        public int Impacts { get; private set; }
        public int Fatalities { get; private set; }

        readonly Sprite[][] sprites = new Sprite[4][];
        GameDirector game;
        Material material;
        float spawnClock;
        void Awake()
        {
            Instance = this;
        }

        System.Collections.IEnumerator Start()
        {
            game = GameDirector.Instance;
            while (!game.Ready)
                yield return null;
            material = Resources.Load<Material>("Materials/PixelActor");
            string[] roles =
            {
                "teacher",
                "medic",
                "concierge",
                "commander"
            };
            for (int i = 0; i < 4; i++)
            {
                sprites[i] = Resources.LoadAll<Sprite>("Art/NPC/Civic/" + roles[i]);
                System.Array.Sort(sprites[i], (a, b) => string.CompareOrdinal(a.name, b.name));
            }

            for (int i = 0; i < (game.stage == StageId.Haven ? 18 : 60); i++)
                Spawn(false);
        }

        Vector3 Position(bool hidden)
        {
            var center = game.Player.transform.position;
            for (int tries = 0; tries < 30; tries++)
            {
                Vector3 p;
                if (game.stage == StageId.Haven)
                    p = new Vector3(Random.Range(5, 222), .06f, Random.value > .5f ? 10 : -8);
                else
                {
                    int col = Mathf.Clamp(Mathf.RoundToInt((center.x - 40) / 140) + Random.Range(-1, 2), 0, 5), row = Mathf.Clamp(Mathf.RoundToInt((center.z + 280) / 140) + Random.Range(-1, 2), 0, 4);
                    bool along = Random.value > .5f;
                    p = along ? new Vector3(Mathf.Clamp(center.x + Random.Range(-140, 140), 22, 758), .06f, -280 + row * 140 + (Random.value > .5f ? 16 : -16)) : new Vector3(40 + col * 140 + (Random.value > .5f ? 16 : -16), .06f, Mathf.Clamp(center.z + Random.Range(-140, 140), -298, 298));
                }

                if (game.stage == StageId.UrbanCity)
                {
                    var junction = CityRoadNetwork.NearestJunction(p);
                    if (Mathf.Abs(p.x - junction.x) < 16 && Mathf.Abs(p.z - junction.z) < 16)
                        p = CityRoadNetwork.Sidewalk(p);
                }

                bool crowded = Vector3.Distance(p, game.Player.transform.position) < 3;
                foreach (var citizen in Citizens)
                    if (citizen && citizen.gameObject.activeSelf && Vector3.Distance(p, citizen.transform.position) < 1.5f)
                    {
                        crowded = true;
                        break;
                    }

                if (crowded)
                    continue;
                if (Physics.CheckCapsule(p + Vector3.up * .5f, p + Vector3.up * 1.6f, .4f, 1, QueryTriggerInteraction.Ignore))
                    continue;
                var v = Camera.main.WorldToViewportPoint(p);
                if (hidden && v.z > 0 && v.x > -.1f && v.x < 1.1f && v.y > -.1f && v.y < 1.1f)
                    continue;
                return p;
            }

            return new Vector3(24, .06f, -300);
        }

        CityPedestrian Spawn(bool hidden)
        {
            CityPedestrian c = Citizens.Find(p => !p.gameObject.activeSelf);
            if (!c)
            {
                if (Citizens.Count >= 72)
                    return null;
                var go = new GameObject("Citizen / ambient", typeof(SpriteRenderer), typeof(CityPedestrian), typeof(ContactShadow));
                go.transform.SetParent(transform);
                go.GetComponent<SpriteRenderer>().sharedMaterial = material;
                c = go.GetComponent<CityPedestrian>();
                Citizens.Add(c);
                go.AddComponent<CityNpc>().Configure(1000 + Citizens.Count);
            }

            var p = Position(hidden);
            c.ResetAt(p, p, sprites[(Citizens.IndexOf(c) / 3) % 4]);
            c.speed = Random.Range(1.8f, 2f);
            SetDestination(c);
            return c;
        }

        void SetDestination(CityPedestrian c)
        {
            if (game.stage == StageId.Haven)
            {
                c.WalkTo(new Vector3(Mathf.Clamp(c.transform.position.x + Random.Range(-24, 24), 5, 222), .06f, c.transform.position.z), false);
                return;
            }

            var pos = c.transform.position;
            var j = CityRoadNetwork.NearestJunction(pos);
            if (Mathf.Abs(pos.x - j.x) > 16.4f || Mathf.Abs(pos.z - j.z) > 16.4f)
            {
                c.WalkTo(CityRoadNetwork.Sidewalk(pos), false);
                return;
            }

            bool crossing;
            var target = CityRoadNetwork.NextWalk(c.transform.position, Random.Range(0, 4), out crossing);
            c.WalkTo(target, crossing);
        }

        void Update()
        {
            if (!game || game.Blocked)
                return;
            float dt = Mathf.Min(Time.deltaTime, .08f);
            foreach (var c in Citizens)
                if (c.gameObject.activeSelf)
                {
                    c.Tick(dt);
                    if ((c.dead && c.age > 12) || Vector3.Distance(c.transform.position, game.Player.transform.position) > 250)
                        c.gameObject.SetActive(false);
                    else if (!c.struck && Vector3.Distance(c.transform.position, c.target) < .3f)
                        SetDestination(c);
                }

            spawnClock -= dt;
            if (spawnClock <= 0)
            {
                spawnClock = .35f;
                int count = 0;
                foreach (var c in Citizens)
                    if (c.gameObject.activeSelf)
                        count++;
                if (count < (game.stage == StageId.Haven ? 18 : 60))
                    Spawn(true);
            }
        }

        public void Eject(Vector3 p)
        {
            var c = Spawn(false);
            if (c)
            {
                var safe = game.stage == StageId.UrbanCity ? CityRoadNetwork.Sidewalk(p) : p + Vector3.forward * 3;
                c.ResetAt(p, safe, sprites[2]);
            }
        }

        public void VehicleSweep(CityVehicle car, Vector3 a, Vector3 b, float speed)
        {
            if (Mathf.Abs(speed) < 2)
                return;
            Vector3 line = b - a;
            float denom = line.sqrMagnitude;
            bool player = UrbanSimulation.Instance && UrbanSimulation.Instance.Current == car;
            foreach (var c in Citizens)
                if (c.gameObject.activeSelf && !c.struck && !c.dead)
                {
                    var pos = c.transform.position;
                    float t = denom > .00001f ? Mathf.Clamp01(Vector3.Dot(pos - a, line) / denom) : 0;
                    var local = Quaternion.Inverse(car.transform.rotation) * (pos - (a + line * t));
                    if (Mathf.Abs(local.x) < car.HalfLength + .25f && Mathf.Abs(local.z) < car.HalfWidth + .3f && Mathf.Abs(local.y) < 1.7f)
                    {
                        if (player)
                            c.GetComponent<WorldActor>()?.VehicleHit(car.Forward * Mathf.Sign(speed), Mathf.Abs(speed));
                        else
                            c.Hit(car.Forward * Mathf.Sign(speed), Mathf.Abs(speed));
                        Impacts++;
                        if (c.dead)
                            Fatalities++;
                        game.Audio.Play("urban_impact", pos, .22f, 1);
                    }
                }

            if (player)
                foreach (var actor in WorldActor.All)
                    if (actor && actor.Alive && !actor.GetComponent<CityPedestrian>())
                    {
                        var pos = actor.transform.position;
                        float t = denom > .00001f ? Mathf.Clamp01(Vector3.Dot(pos - a, line) / denom) : 0;
                        var local = Quaternion.Inverse(car.transform.rotation) * (pos - (a + line * t));
                        if (Mathf.Abs(local.x) < car.HalfLength + .3f && Mathf.Abs(local.z) < car.HalfWidth + .4f && Mathf.Abs(local.y) < 1.7f)
                            actor.VehicleHit(car.Forward * Mathf.Sign(speed), Mathf.Abs(speed));
                    }
        }

        public bool CrossingAhead(CityVehicle car)
        {
            foreach (var c in Citizens)
                if (c.gameObject.activeSelf && !c.struck)
                {
                    var d = c.transform.position - car.transform.position;
                    float forward = Vector3.Dot(d, car.Forward);
                    if (forward > car.HalfLength && forward < car.HalfLength + 14 && Mathf.Abs(Vector3.Dot(d, car.transform.forward)) < 1.5f)
                        return true;
                }

            return false;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
