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

            for (int i = 0; i < (game.stage == StageId.Haven ? 18 : ResidentialWorld.AtHome(LifeState.Hour) ? 16 : 40); i++)
            {Spawn(false);if(i%3==2)yield return null;}
        }

        Vector3 Position(bool hidden)
        {
            var center = game.Player.transform.position;
            if(game.stage==StageId.UrbanCity&&ExpansionRoads.Outside(center))
            {var p=LocalCityRoutes.Sidewalk(center+new Vector3(Random.Range(-100,100),0,Random.Range(-100,100)));return CrowdFlow.Place(p,Random.Range(0,10000),out var safe,22)?safe:new Vector3(float.NaN,0,0);}
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

            return new Vector3(float.NaN,0,0);
        }

        CityPedestrian Spawn(bool hidden)
        {
            if(!PopulationBudget.ClaimFrame())return null;
            var p = Position(hidden);
            if(float.IsNaN(p.x))return null;
            if(!CityGangWar.FindGround(p,out p))return null;
            if(!PopulationBudget.Room(p))return null;
            Citizens.RemoveAll(person=>!person);
            CityPedestrian c = Citizens.Find(person => !person.gameObject.activeSelf);
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

            c.ResetAt(p, p, sprites[(Citizens.IndexOf(c) / 3) % 4]);
            c.speed = Random.Range(1.8f, 2f);
            SetDestination(c);
            return c;
        }

        void SetDestination(CityPedestrian c)
        {
            if(game.stage==StageId.UrbanCity&&ExpansionRoads.Outside(c.transform.position)){c.WalkTo(LocalCityRoutes.Sidewalk(c.transform.position,Random.value>.5f?4:-4),false);return;}
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
                if (c&&c.gameObject.activeSelf && !c.GetComponent<WorldActor>().Downed)
                {
                    c.Tick(dt);
                    if ((c.dead && c.age > 12 && !c.GetComponent<MedicalPending>()) || !c.GetComponent<MedicalPending>() && Vector3.Distance(c.transform.position, game.Player.transform.position) > 250)
                        c.gameObject.SetActive(false);
                    else if (!c.struck && Vector3.Distance(c.transform.position, c.target) < 1.4f)
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
                if ((game.stage!=StageId.UrbanCity||FourCityCatalog.CityAt(game.Player.transform.position)!=2)&&count < (game.stage == StageId.Haven ? 18 : ResidentialWorld.AtHome(LifeState.Hour) ? 16 : 40))
                    Spawn(true);
            }
        }

        public void Eject(Vector3 p)
        {
            var c = Spawn(false);
            if(!c)return;
            if (c)
            {
                var safe = game.stage == StageId.UrbanCity ? CityRoadNetwork.Sidewalk(p) : p + Vector3.forward * 3;
                c.ResetAt(p, safe, sprites[2]);
            }
        }

        readonly Dictionary<int,float> impactCooldown=new();
        readonly List<WorldActor> sweepActors=new();
        public void VehicleSweep(CityVehicle car, Vector3 a, Vector3 b, float speed)
        {
            if(Mathf.Abs(speed)<.8f)return;
            Vector3 line=b-a;float denom=line.sqrMagnitude;
            bool player=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car;
            ActorSpatialIndex.Nearby((a+b)*.5f,car.HalfLength+car.HalfWidth+line.magnitude*.5f+3,sweepActors);
            foreach(var actor in sweepActors)
            {
                if(!actor||!actor.Alive||actor.helicopter||actor.environmental||actor.transform.IsChildOf(car.transform)||actor.GetComponent<MedicalPending>() is MedicalPending pending&&pending.carried)continue;
                var stolen=car.GetComponent<StolenVehicle>();if(stolen&&stolen.Driver&&stolen.Driver.Body==actor)continue;
                int id=actor.GetInstanceID();if(impactCooldown.TryGetValue(id,out float next)&&Time.time<next)continue;
                var pos=actor.transform.position;float t=denom>.00001f?Mathf.Clamp01(Vector3.Dot(pos-a,line)/denom):0;
                var local=Quaternion.Inverse(car.transform.rotation)*(pos-(a+line*t));
                if(Mathf.Abs(local.x)>car.HalfLength+.3f||Mathf.Abs(local.z)>car.HalfWidth+.35f||Mathf.Abs(local.y)>1.8f)continue;
                impactCooldown[id]=Time.time+.8f;
                actor.VehicleHit(car.Forward*Mathf.Sign(speed),Mathf.Abs(speed),player?null:TrafficDamageSource.Environment);
                Impacts++;if(!actor.Alive)Fatalities++;
                game?.Audio.Play("urban_impact",pos,.22f,1);
            }
            if(impactCooldown.Count>2048)impactCooldown.Clear();
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
