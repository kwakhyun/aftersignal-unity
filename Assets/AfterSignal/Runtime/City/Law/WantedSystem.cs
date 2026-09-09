using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed class WantedSystem : MonoBehaviour
    {
        public static WantedSystem Instance { get; private set; }
        public static int Level => LifeState.Heat <= 0 ? 0 : LifeState.Heat < 20 ? 1 : LifeState.Heat < 45 ? 2 : LifeState.Heat < 80 ? 3 : LifeState.Heat < 130 ? 4 : 5;

        public static readonly int[] Strength =
        {
            0,
            2,
            4,
            6,
            9,
            12
        };
        public readonly List<PoliceOfficer> Officers = new List<PoliceOfficer>();
        public readonly List<PoliceCar> Cars = new List<PoliceCar>();
        public PoliceHelicopter Helicopter;
        public Vector3 LastSeen { get; private set; }
        public bool Seen { get; private set; }
        public NoaWantedSupport NoaSupport { get; } = new NoaWantedSupport();
        public float EscapeTime => 36 + Level * 10;

        public int ActiveOfficers
        {
            get
            {
                int n = 0;
                foreach (var o in Officers)
                    if (o && o.Body.Alive)
                        n++;
                return n;
            }
        }

        GameDirector game;
        float dispatchClock = 12, incidentClock, responseAge;
        int deployed, carCount;
        bool engaged;
        public void Initialize(GameDirector owner)
        {
            Instance = this;
            game = owner;
            LastSeen = game.Player.transform.position;
            engaged = Level > 0;
        }

        public static void Report(float severity, Vector3 where)
            =>CrimeObservation.Observe(severity,where);

        public static void ConfirmReport(float severity, Vector3 where)
        {
            LifeState.Load();
            int old = Level;
            LifeState.Heat = Mathf.Clamp(LifeState.Heat + severity, 0, 170);
            LifeState.HiddenSeconds = 0;
            if (Instance)
            {
                Instance.LastSeen = where;
                Instance.incidentClock = 2;
                Instance.engaged = true;
                if (old == 0)
                    {Instance.dispatchClock = Level>=3?20:12;Instance.responseAge=0;}
            }

            if (Level != old)
            {
                LifeState.Save();
                GameDirector.Instance?.Toast("수배 " + Level + "단계 · 경찰이 출동합니다.", 4);
            }
        }

        public static void Clear(string message)
        {
            CrimeObservation.Forget();
            LifeState.Heat = LifeState.HiddenSeconds = 0;
            LifeState.Save();
            if (Instance)
            {
                Instance.NoaSupport.Cancel();
                Instance.deployed = Instance.carCount = 0;
                Instance.engaged = false;
                Instance.Seen = false;
                foreach (var o in Instance.Officers)
                    if (o)
                        o.Withdraw();
                foreach (var c in Instance.Cars)
                    if (c)
                        c.Withdraw();
                if (Instance.Helicopter)
                    Instance.Helicopter.Withdraw();
                foreach(var t in FindObjectsByType<TacticalTransport>())t.Withdraw();
                var military=Instance.GetComponent<MilitaryResponse>();if(military)military.Withdraw();
                Instance.Officers.Clear();
                Instance.Cars.Clear();
                Instance.Helicopter = null;
            }

            if (!string.IsNullOrEmpty(message))
                GameDirector.Instance?.Toast(message, 5);
        }

        void Update()
        {
            if (!game || !game.Ready || game.Blocked)
                return;
            NoaSupport.Tick(Mathf.Min(.1f,Time.deltaTime));
            if(Level == 0)return;
            if(IncidentCommand.Emergency){Seen=false;return;}
            float dt = Mathf.Min(.1f, Time.deltaTime);
            incidentClock -= dt;responseAge+=dt;
            Seen = incidentClock > 0;
            foreach (var body in WorldActor.All)
                if (body && body.police && body.Alive)
                {
                    var officer = body.GetComponent<PoliceOfficer>();
                    if (officer && officer.CanSee() || body.GetComponent<StationDefender>()&&FactionCombat.Visible(body.Center,game.Player.Shoulder,55)) Seen = true;
                }
            if (Helicopter && Helicopter.Body.Alive && Helicopter.CanSee())
                Seen = true;
            if (Seen)
            {
                LastSeen = game.Player.transform.position;
                LifeState.HiddenSeconds = 0;
            }
            else
                LifeState.HiddenSeconds += dt;
            if (LifeState.HiddenSeconds >= EscapeTime)
            {
                Clear("경찰의 시야에서 벗어났습니다 · 수배 해제");
                return;
            }

            dispatchClock -= dt;
            if (dispatchClock <= 0 && deployed < Strength[Level] && (Level<3||responseAge>=20))
            {
                if(game.stage==StageId.UrbanCity&&UrbanSimulation.Instance)
                {
                    int squad=Mathf.Min(Level>=3?4:2,Strength[Level]-deployed);dispatchClock=5;
                    if(ResponseDispatch.TryOrigin(LastSeen,false,false,deployed,out var origin)){TacticalTransport.Create(this,origin,squad,Level>=3?4:Level);deployed+=squad;dispatchClock=12;}
                }
                else
                {
                dispatchClock = .9f;
                Vector3 spawn = SpawnPoint(deployed);
                Officers.Add(PoliceOfficer.Create(this, spawn, Level, deployed));
                deployed++;
                if (game.stage == StageId.UrbanCity && carCount < Mathf.Min(4, (Level + 1) / 2) && UrbanSimulation.Instance && UrbanSimulation.Instance.vehiclePrefab)
                {
                    var position = CityRoadNetwork.NearestJunction(spawn) + new Vector3(0, .04f, -4.2f);
                    Cars.Add(PoliceCar.Create(this, position));
                    carCount++;
                }
                }
            }

            if (Level >= 4 && responseAge>32 && !Helicopter && game.stage == StageId.UrbanCity && ResponseDispatch.TryOrigin(LastSeen,false,true,0,out var helipad))
                Helicopter = PoliceHelicopter.Create(this,helipad);
            bool aircraftAlive = Helicopter && Helicopter.Body.Alive;
            if (engaged && deployed >= Strength[Level] && ActiveOfficers == 0 && !aircraftAlive && !TacticalTransport.Pending && !GetComponent<MilitaryResponse>())
            {
                Clear("출동한 경찰 병력을 모두 제압했습니다 · 수배 해제");
            }
        }

        Vector3 SpawnPoint(int index)
        {
            Vector3 p = game.Player.transform.position;
            if(game.stage==StageId.UrbanCity&&ExpansionRoads.Outside(p))
                p=ExpansionRoads.Sidewalk(p+new Vector3(index%2==0?-40:40,0,25+index*3));
            else if (game.stage == StageId.UrbanCity)
                p = CityRoadNetwork.Sidewalk(new Vector3(Mathf.Clamp(p.x + (index % 2 == 0 ? -1 : 1) * (32 + index * 2), 20, 760), .08f, Mathf.Clamp(p.z + 16, -300, 300)));
            else if (CivicWorld.Interior(game.stage))
                p = game.stage == StageId.Residence ? new Vector3(34, .08f, -2) : new Vector3(5 + index % 3 * 1.4f, .08f, -7 - index / 3 * .9f);
            else
                p = new Vector3(Mathf.Clamp(p.x + (index % 2 == 0 ? -20 : 20), 3, game.stageLength - 3), .1f, Mathf.Clamp(p.z, -game.halfDepth + 2, game.halfDepth - 2));
            for (int i = 0; i < 20; i++)
            {
                var q = p + new Vector3(i % 4 * 1.3f, 0, i / 4 * 1.1f);
                if (!Physics.CheckCapsule(q + Vector3.up * .45f, q + Vector3.up * 1.6f, .36f, 1, QueryTriggerInteraction.Ignore) && Physics.Raycast(q + Vector3.up * 2, Vector3.down, out var floor, 4, 1, QueryTriggerInteraction.Ignore))
                {
                    q.y = floor.point.y + .08f;
                    return q;
                }
            }

            return game.checkpoint;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
