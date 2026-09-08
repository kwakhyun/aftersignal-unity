using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    // Street encounters are independent of the player's wanted-response budget.
    // Only nearby districts are simulated; cleared sites rest before respawning off camera.
    public sealed class CityGangWar : MonoBehaviour
    {
        public static CityGangWar Instance { get; private set; }
        public const int MaxEncounters = 3;
        public int ActiveEncounters { get; private set; }
        public int SiteCount => sites.Count;
        public int TotalSpawned { get; private set; }
        sealed class Site
        {
            public Vector3 center;
            public int crew;
            public GameObject root;
            public float next, age, resolved;
            public readonly List<WorldActor> units = new List<WorldActor>();
        }
        readonly List<Site> sites = new List<Site>();
        GameDirector game;
        float tick;

        public void Initialize(GameDirector owner)
        {
            Instance = this;
            game = owner;
            for (int col = 0; col < 6; col++)
                for (int row = 0; row < 4; row++)
                    if ((col + row) % 2 == 0)
                        sites.Add(new Site { center = CityRoadNetwork.Junction(col, row) + new Vector3(col == 5 ? -16 : 16, 0, 56), crew = (col + row * 2) % 3 });
            // Populate before the first city frame. Later replenishment stays off camera.
            Physics.SyncTransforms();
            if(ResidentialWorld.AtHome(LifeState.Hour))SpawnNearby(true);
        }

        void Update()
        {
            if (!game || !game.Ready || game.Blocked) return;
            tick -= Time.deltaTime;
            if (tick > 0) return;
            tick = 1;
            ActiveEncounters = 0;
            foreach (var site in sites)
            {
                if (!site.root) continue;
                site.age++;
                bool gangAlive = false, policeAlive = false, near = false;
                foreach (var unit in site.units)
                {
                    if (!unit) continue;
                    gangAlive |= unit.gang && unit.Alive;
                    policeAlive |= unit.police && unit.Alive;
                    near |= Vector3.Distance(unit.transform.position, game.Player.transform.position) < 190 || OnCamera(unit.Center);
                }
                if (!gangAlive || !policeAlive) site.resolved++;
                if (!near && (site.resolved >= 20 || site.age > 30 || Vector3.Distance(site.center, game.Player.transform.position) > 260))
                {
                    Destroy(site.root);
                    site.root = null;
                    site.units.Clear();
                    site.next = Time.time + 120;
                    continue;
                }
                ActiveEncounters++;
            }
            if(ResidentialWorld.AtHome(LifeState.Hour)||TotalSpawned==0&&LifeState.Hour>18)SpawnNearby(false);
        }

        void SpawnNearby(bool initial)
        {
            sites.Sort((a, b) => (a.center - game.Player.transform.position).sqrMagnitude.CompareTo((b.center - game.Player.transform.position).sqrMagnitude));
            foreach (var site in sites)
            {
                if (ActiveEncounters >= MaxEncounters) break;
                float distance = Vector3.Distance(site.center, game.Player.transform.position);
                if (site.root || site.next > Time.time || distance < 32 || distance > 210 || !initial && OnCamera(site.center + Vector3.up)) continue;
                if (Spawn(site, initial)) ActiveEncounters++;
                else site.next = Time.time + 15;
            }
        }

        bool Spawn(Site site, bool initial)
        {
            var positions = new Vector3[3];
            for (int i = 0; i < positions.Length; i++)
            {
                var desired = site.center + new Vector3(i % 2 == 0 ? -.7f : .7f, 0, i < 3 ? -7 - i * 3 : 12 + (i - 3) * 4);
                if (!FindGround(desired, out positions[i])) return false;
                if (!initial && OnCamera(positions[i] + Vector3.up)) return false;
            }
            site.root = new GameObject("거리 교전 / " + GangMember.CrewName(site.crew));
            site.root.transform.SetParent(transform, false);
            site.age = site.resolved = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                WorldActor body;
                if (i < 3){body = GangMember.Create(positions[i], site.crew, i).Body;body.gameObject.AddComponent<GangCrime>();}
                else
                {
                    var officer = PoliceOfficer.Create(WantedSystem.Instance, positions[i], 2, i - 3);
                    officer.Ambient = true;
                    body = officer.Body;
                }
                body.transform.SetParent(site.root.transform, true);
                site.units.Add(body);
            }
            TotalSpawned++;
            return true;
        }

        public static bool FindGround(Vector3 desired, out Vector3 position)
        {
            for (int i = 0; i < 16; i++)
            {
                var p = desired + new Vector3((i % 4 - 1) * .75f, 0, i / 4 * .9f);
                if (!Physics.Raycast(p + Vector3.up * 3, Vector3.down, out var floor, 5, 1, QueryTriggerInteraction.Ignore)
                    || floor.normal.y < .8f || Mathf.Abs(floor.point.y) > .5f) continue;
                p.y = floor.point.y + .08f;
                if (Physics.CheckCapsule(p + Vector3.up * .45f, p + Vector3.up * 1.7f, .38f, 1, QueryTriggerInteraction.Ignore)) continue;
                position = p;
                return true;
            }
            position = default;
            return false;
        }

        static bool OnCamera(Vector3 point)
        {
            if (!Camera.main) return false;
            var p = Camera.main.WorldToViewportPoint(point);
            return p.z > 0 && p.z < 180 && p.x > -.15f && p.x < 1.15f && p.y > -.15f && p.y < 1.15f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
