using UnityEngine;

namespace AfterSignal
{
    public sealed class PoliceCar : MonoBehaviour
    {
        public CityVehicle Vehicle { get; private set; }

        WantedSystem system;
        Renderer red, blue;
        AudioSource siren;
        float clock, pathClock;
        Vector3 waypoint;
        bool retreat;
        public static PoliceCar Create(WantedSystem owner, Vector3 point)
        {
            var sim = UrbanSimulation.Instance;
            var car = sim.Spawn(point, false, 0);
            car.name = "POLICE / response vehicle";
            car.occupied = true;
            car.health = 160;
            car.fuel = 45;
            foreach (var r in car.GetComponentsInChildren<MeshRenderer>())
                if (r.name == "Sculpted chassis" || r.name == "Sedan roof")
                    r.sharedMaterial = Resources.Load<Material>("Materials/DistrictIvory");
            var p = car.gameObject.AddComponent<PoliceCar>();
            p.Vehicle = car;
            PoliceVehicleArt.Apply(car);
            p.system = owner;
            WorldGeometry.Part(car.transform, "Police blue stripe", new Vector3(0, 1.05f, -1.06f), new Vector3(4.5f, .35f, .05f), "DistrictBlue");
            WorldGeometry.Part(car.transform, "Police blue stripe", new Vector3(0, 1.05f, 1.06f), new Vector3(4.5f, .35f, .05f), "DistrictBlue");
            p.red = WorldGeometry.Part(car.transform, "Siren red", new Vector3(0, 2.2f, -.45f), new Vector3(.65f, .22f, .5f), "RedFX").GetComponent<Renderer>();
            p.blue = WorldGeometry.Part(car.transform, "Siren blue", new Vector3(0, 2.2f, .45f), new Vector3(.65f, .22f, .5f), "CyanFX").GetComponent<Renderer>();
            p.siren = car.gameObject.AddComponent<AudioSource>();
            p.siren.clip = SirenClip();
            p.siren.spatialBlend = 1;
            p.siren.minDistance = 5;
            p.siren.maxDistance = 90;
            p.siren.rolloffMode = AudioRolloffMode.Linear;
            p.siren.loop = true;
            p.siren.volume = .13f;
            p.siren.Play();
            return p;
        }

        static AudioClip clip;
        static AudioClip SirenClip()
        {
            if (clip)
                return clip;
            const int rate = 22050;
            var samples = new float[rate * 2];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                phase += (760 + 330 * Mathf.Sin(t * Mathf.PI)) * Mathf.PI * 2 / rate;
                samples[i] = Mathf.Sin(phase) * .4f;
            }

            clip = AudioClip.Create("Two tone city siren", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void Update()
        {
            var g = GameDirector.Instance;
            if (!g || !Vehicle)
                return;
            if (siren)
            {
                siren.mute = g.Blocked || Vehicle.Wrecked || retreat;
                siren.volume=.25f*g.Audio.Volume*g.Audio.SfxVolume;
            }
            if (g.Blocked)
                return;
            float dt = Mathf.Min(.07f, Time.deltaTime);
            clock += dt;
            if (red)
                red.enabled = !Vehicle.Wrecked && !retreat && Mathf.Repeat(clock, .5f) < .25f;
            if (blue)
                blue.enabled = !Vehicle.Wrecked && !retreat && Mathf.Repeat(clock, .5f) >= .25f;
            if (retreat)
            {
                if (clock > 8)
                    Destroy(gameObject);
                return;
            }

            if (Vehicle.Wrecked)
                return;
            if (UrbanSimulation.Instance && UrbanSimulation.Instance.Current == Vehicle)
            {
                if (siren)
                    siren.mute = true;
                return;
            }

            pathClock -= dt;
            var position = transform.position;
            Vector3 destination = system.LastSeen;
            float distance = Vector3.Distance(position, destination);
            if (pathClock <= 0 || Vector3.Distance(position, waypoint) < 5)
            {
                pathClock = 2;
                var junction = CityRoadNetwork.NearestJunction(position);
                var goal = CityRoadNetwork.NearestJunction(destination);
                if (Vector3.Distance(new Vector3(position.x, 0, position.z), junction) > 12)
                    waypoint = junction;
                else if (Mathf.Abs(goal.x - junction.x) > 10)
                    waypoint = junction + Vector3.right * Mathf.Sign(goal.x - junction.x) * 140;
                else if (Mathf.Abs(goal.z - junction.z) > 10)
                    waypoint = junction + Vector3.forward * Mathf.Sign(goal.z - junction.z) * 140;
                else
                    waypoint = goal;
                waypoint.y = position.y;
            }

            var delta = waypoint - position;
            delta.y = 0;
            float angle = Vector3.SignedAngle(Vehicle.Forward, delta, Vector3.up);
            var control = ControlFrame.Empty;
            control.move = new Vector2(Mathf.Clamp(angle / 28, -1, 1), distance < 18 ? 0 : Mathf.Abs(angle) > 70 ? .17f : .55f);
            control.guard = distance < 18;
            Vehicle.Drive(control, dt);
        }

        public void Withdraw()
        {
            if (Vehicle.owned)
            {
                if (siren)
                    siren.Stop();
                if (red)
                    red.enabled = false;
                if (blue)
                    blue.enabled = false;
                Destroy(this);
                return;
            }

            retreat = true;
            clock = 0;
            Vehicle.occupied = false;
            Vehicle.speed = 0;
        }
    }

    public static class WorldGeometry
    {
        public static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale, string material, PrimitiveType shape = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Materials/" + material);
            return go;
        }
    }
}
