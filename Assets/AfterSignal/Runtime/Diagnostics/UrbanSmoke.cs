using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    public sealed class UrbanSmoke : MonoBehaviour
    {
        [Serializable]
        class Report
        {
            public bool completed;
            public List<string> passed = new List<string>(), errors = new List<string>();
            public string fixtures = "Vehicle/pedestrian collision and low-fuel fixtures are explicitly repositioned. Movement, steering, entry, theft, fueling, and scene return use production methods/control frames.";
        }

        static Report report = new Report();
        static bool initialized;
        static int phase, site;
        static readonly Dictionary<string, float> saves = new Dictionary<string, float>();
        static readonly HashSet<string> present = new HashSet<string>();
        GameDirector game;
        UrbanSimulation sim;
        QualitySession metrics;
        string output;
        bool failed;
        float watchdog;
        void Update()
        {
            watchdog += Time.unscaledDeltaTime;
            if ((failed || watchdog > 95) && metrics)
            {
                if (watchdog > 95)
                    Check(false, "Stage QA timed out");
                Finish();
            }
        }

        void Awake()
        {
            Application.logMessageReceived += Log;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }

        void Log(string t, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                failed = true;
                report.errors.Add(t);
            }
        }

        IEnumerator Start()
        {
            game = GetComponent<GameDirector>();
            output = Path.GetFullPath(QualitySession.Arg("-quality-output", "Artifacts/Urban/Run"));
            Directory.CreateDirectory(output);
            if (!initialized)
            {
                initialized = true;
                foreach (var suffix in new[]
                {
                    "Site",
                    "Car",
                    "CarX",
                    "CarZ",
                    "CarYaw",
                    "CarFuel",
                    "CarStage",
                    "CarHealth",
                    "CarType"
                }

                )
                {
                    string k = UrbanCatalog.Prefix + suffix;
                    if (PlayerPrefs.HasKey(k))
                        present.Add(k);
                    saves[k] = suffix == "Site" || suffix == "Car" || suffix == "CarStage" || suffix == "CarType" ? PlayerPrefs.GetInt(k) : PlayerPrefs.GetFloat(k);
                }

                foreach (string s in new[]
                {
                    "Stage",
                    "Memories"
                }

                )
                {
                    string k = "AFTERSIGNAL.Unity." + s;
                    if (PlayerPrefs.HasKey(k))
                        present.Add(k);
                    saves[k] = PlayerPrefs.GetInt(k);
                }

                UrbanCatalog.Reset();
                GameDirector.SkipTitle = true;
                yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
                yield break;
            }

            Application.runInBackground = true;
            game.Input.ExternalControl = true;
            game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(false);
            QualitySession.Output = Path.Combine(output, game.stage + "-" + phase);
            Directory.CreateDirectory(QualitySession.Output);
            metrics = gameObject.AddComponent<QualitySession>();
            QualitySession.Output = Path.Combine(output, game.stage + "-" + phase);
            Directory.CreateDirectory(QualitySession.Output);
            yield return Hold(.8f, ControlFrame.Empty);
            if (game.Dialogue)
                game.CloseDialogue();
            sim = UrbanSimulation.Instance;
            if (game.stage == StageId.UrbanCity && phase == 0)
            {
                Check(FindObjectsByType<InteractionPoint>().Length >= 42, "40 site entrances plus city return and guide");
                Check(sim && sim.Cars.Count >= 10, "Bounded traffic population spawned");
                yield return Capture("city-entry");
                var map = ControlFrame.Empty;
                map.map = true;
                yield return Tap(map);
                yield return Hold(.15f, ControlFrame.Empty);
                yield return Capture("city-map");
                yield return Tap(map);
                var car = sim.Cars.Find(c => !c.traffic && !c.owned);
                game.Player.Respawn(car.transform.position + Vector3.back * 3, false);
                var use = ControlFrame.Empty;
                use.interact = true;
                yield return Tap(use);
                Check(sim.Driving, "E enters parked car");
                float start = car.Distance;
                var c = ControlFrame.Empty;
                c.move = Vector2.up;
                yield return Hold(6, c);
                Check(car.Distance > start + 50 && car.fuel < 45, "Acceleration and fuel consumed by actual distance");
                yield return Capture("driving");
                float yaw = car.transform.eulerAngles.y;
                c.move = new Vector2(1, .35f);
                yield return Hold(1.3f, c);
                Check(Mathf.Abs(Mathf.DeltaAngle(yaw, car.transform.eulerAngles.y)) > 20, "WASD steering changes heading");
                c = ControlFrame.Empty;
                c.jump = true;
                yield return Hold(1.8f, c);
                Check(Mathf.Abs(car.speed) < .2f, "Space brake stops vehicle");
                c = ControlFrame.Empty;
                c.move = Vector2.down;
                yield return Hold(.9f, c);
                Check(car.speed < -.5f, "Reverse gear");
                c.move = Vector2.zero;
                c.jump = true;
                yield return Hold(1, c);
                yield return Tap(use);
                Check(!sim.Driving && game.Player.Controller.enabled, "E exits with player collider restored");
                // Deterministic slow traffic fixture to test theft independently of random arrival timing.
                var other = sim.Spawn(new Vector3(150, .02f, -283), true);
                other.traffic = false;
                other.speed = 0;
                game.Player.Respawn(other.transform.position + Vector3.back * 3, false);
                yield return Tap(use);
                Check(sim.Current == other && sim.Hijacks > 0, "Occupied NPC vehicle can be taken; driver ejected");
                var citizen = CityPopulation.Instance.Citizens[0];
                citizen.ResetAt(new Vector3(166, .06f, -283), new Vector3(166, .06f, -283), citizen.poses);
                c = ControlFrame.Empty;
                c.move = Vector2.up;
                yield return Hold(3, c);
                Check(CityPopulation.Instance.Impacts > 0 && CityPopulation.Instance.Fatalities > 0, "High-speed vehicle impact launches and kills ambient citizen");
                yield return Capture("impact");
                other.speed = 0;
                other.transform.position = UrbanCatalog.Pump(11);
                other.transform.rotation = Quaternion.identity;
                other.fuel = 1;
                yield return Hold(.2f, ControlFrame.Empty);
                yield return Tap(use);
                yield return Hold(4.6f, ControlFrame.Empty);
                Check(other.fuel > 44, "Low-fuel fixture refilled at physical station");
                yield return Capture("fuel-station");
                c = ControlFrame.Empty;
                c.exit = true;
                yield return Tap(c);
                Check(!sim.Driving, "F exits at fuel pump");
                sim.SaveCar();
                metrics.Save();
                phase = 1;
                site = 0;
                game.Player.Respawn(UrbanCatalog.Door(site), false);
                yield return Hold(.25f, ControlFrame.Empty);
                yield return Tap(use);
                yield break;
            }

            if (game.stage == StageId.UrbanInterior)
            {
                int kind = UrbanCatalog.Kind(UrbanCatalog.Current);
                var theme = FindAnyObjectByType<UrbanInterior>();
                int active = 0;
                foreach (var t in theme.themes)
                    if (t.activeSelf)
                        active++;
                Check(active == 1 && theme.themes[kind].activeSelf, "Correct interior theme " + kind + " / " + UrbanCatalog.Name(UrbanCatalog.Current));
                var p = Array.Find(FindObjectsByType<InteractionPoint>(), i => i.kind == InteractionKind.UrbanService);
                Check(p != null, "Facility staff/service " + kind);
                if (p)
                {
                    game.Player.Respawn(p.transform.position - Vector3.up + Vector3.back * 1.8f, false);
                    yield return Hold(.2f, ControlFrame.Empty);
                    var u = ControlFrame.Empty;
                    u.interact = true;
                    yield return Tap(u);
                    Check(game.Dialogue, "Facility dialogue " + kind);
                    yield return Capture("dialogue-" + kind);
                    game.CloseDialogue();
                    game.Player.Respawn(new Vector3(33, .15f, -7), false);
                    game.CameraRig.Snap();
                    yield return Hold(.25f, ControlFrame.Empty);
                    yield return Capture("interior-" + kind);
                }

                game.Player.Respawn(new Vector3(5, .15f, -10), false);
                yield return Hold(.3f, ControlFrame.Empty);
                metrics.Save();
                phase++;
                var use = ControlFrame.Empty;
                use.interact = true;
                yield return Tap(use);
                yield break;
            }

            if (game.stage == StageId.UrbanCity && phase > 0)
            {
                Check(Vector3.Distance(game.Player.transform.position, UrbanCatalog.Door(site)) < 4, "Return to same city entrance " + site);
                Check(sim.Owned && sim.Owned.fuel > 44, "Parked vehicle and fuel persist across interior " + site);
                site++;
                if (site < 16)
                {
                    phase++;
                    game.Player.Respawn(UrbanCatalog.Door(site), false);
                    yield return Hold(.25f, ControlFrame.Empty);
                    var use = ControlFrame.Empty;
                    use.interact = true;
                    yield return Tap(use);
                    yield break;
                }

                Finish();
            }

            if (failed)
                Finish();
        }

        void Check(bool ok, string text)
        {
            if (ok)
                report.passed.Add(text);
            else
            {
                report.errors.Add(text);
                failed = true;
            }

            Debug.Log("URBAN QA / " + ok + " / " + text);
        }

        IEnumerator Hold(float time, ControlFrame c)
        {
            float end = Time.realtimeSinceStartup + time;
            while (Time.realtimeSinceStartup < end)
            {
                game.Input.ExternalFrame = c;
                yield return null;
            }

            game.Input.ExternalFrame = ControlFrame.Empty;
        }

        IEnumerator Tap(ControlFrame c)
        {
            game.Input.ExternalFrame = c;
            yield return null;
            game.Input.ExternalFrame = ControlFrame.Empty;
            yield return null;
        }

        IEnumerator Capture(string name)
        {
            if (!string.IsNullOrEmpty(QualitySession.Arg("-quality-no-captures")))
                yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
            yield return null;
        }

        void Finish()
        {
            foreach (var kv in saves)
            {
                if (!present.Contains(kv.Key))
                    PlayerPrefs.DeleteKey(kv.Key);
                else if (kv.Key.EndsWith("X") || kv.Key.EndsWith("Z") || kv.Key.EndsWith("Yaw") || kv.Key.EndsWith("Fuel") || kv.Key.EndsWith("Health"))
                    PlayerPrefs.SetFloat(kv.Key, kv.Value);
                else
                    PlayerPrefs.SetInt(kv.Key, (int)kv.Value);
            }

            PlayerPrefs.Save();
            report.completed = !failed && report.errors.Count == 0;
            File.WriteAllText(Path.Combine(output, "urban.json"), JsonUtility.ToJson(report, true));
            metrics?.Save();
            Application.Quit(report.completed ? 0 : 1);
        }
    }
}
