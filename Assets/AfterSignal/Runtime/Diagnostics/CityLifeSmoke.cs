using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    // Explicit, short integration check; never active in a normal game session.
    public sealed class CityLifeSmoke : MonoBehaviour
    {
        [Serializable]
        sealed class Result
        {
            public bool completed;
            public List<string> passed = new List<string>(), errors = new List<string>();
        }

        sealed class Saved
        {
            public string key, text;
            public float value;
            public int type;
            public bool present;
        }

        static readonly List<Saved> saved = new List<Saved>();
        static readonly Result result = new Result();
        static bool initialized, finished;
        static int phase;
        static float started;
        GameDirector game;
        string output;
        void Awake()
        {
            Application.runInBackground = true;
            Application.logMessageReceived += Log;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }

        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                result.errors.Add(message);
        }

        void Update()
        {
            if (initialized && !finished && Time.realtimeSinceStartup - started > 160)
            {
                Check(false, "integration timeout");
                Finish();
            }
        }

        IEnumerator Start()
        {
            game = GetComponent<GameDirector>();
            output = Path.GetFullPath(QualitySession.Arg("-quality-output", "Artifacts/CityLife/Smoke"));
            Directory.CreateDirectory(output);
            if (!initialized)
            {
                initialized = true;
                started = Time.realtimeSinceStartup;
                Snapshot();
                LifeState.Heat = LifeState.HiddenSeconds = 0;
                GameDirector.SkipTitle = true;
                yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
                yield break;
            }

            Application.runInBackground = true;
            game.Input.ExternalControl = true;
            game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(false);
            if (game.Dialogue)
                game.CloseDialogue();
            yield return Hold(.45f, ControlFrame.Empty);
            if (phase == 0)
            {
                Check(FindObjectsByType<CityRooftop>(FindObjectsSortMode.None).Length >= 80, "80 connected city rooftops");
                Check(GrappleAnchor.All.Count(a => a && a.cityAnchor) > 300, "Street and facade rope network");
                var player = game.Player;
                player.Respawn(new Vector3(66, .15f, -295));
                yield return Hold(.15f, ControlFrame.Empty);
                float z = player.transform.position.z;
                var control = ControlFrame.Empty;
                control.move = Vector2.up;
                control.dash = true;
                yield return Hold(.15f, control);
                Check(player.transform.position.z > z + 1.7f, "Forward dash moves through depth");
                yield return Hold(.85f, ControlFrame.Empty);
                z = player.transform.position.z;
                control.move = Vector2.down;
                yield return Hold(.15f, control);
                Check(player.transform.position.z < z - 1.7f, "Backward dash moves through depth");
                var roof = FindObjectsByType<CityRooftop>(FindObjectsSortMode.None).First(r => r.site == 0);
                var route = roof.GetComponentsInChildren<GrappleAnchor>().OrderBy(a => a.transform.position.y).ToArray();
                player.Respawn(new Vector3(route[0].transform.position.x, .15f, route[0].transform.position.z - 1));
                game.CameraRig.Snap();
                foreach (var anchor in route)
                {
                    control = ControlFrame.Empty;
                    control.grapple = true;
                    control.move = Vector2.up;
                    control.pointer = Camera.main.WorldToScreenPoint(anchor.transform.position);
                    player.Rope.Attach(anchor);
                    float until = Time.realtimeSinceStartup + 4.5f;
                    while (player.Rope.Attached && Time.realtimeSinceStartup < until)
                    {
                        game.Input.ExternalFrame = control;
                        yield return null;
                    }

                    game.Input.ExternalFrame = ControlFrame.Empty;
                    yield return Hold(.1f, ControlFrame.Empty);
                    if (Vector3.Distance(player.transform.position, anchor.landing) > 1.6f)
                    {
                        Check(false, "Physical rope climb at " + anchor.landing);
                        break;
                    }
                }

                Check(player.transform.position.y > 30, "Ground-to-rooftop traversal through actual rope constraints");
                yield return Hold(.25f, ControlFrame.Empty);
                yield return Capture("roof-day");
                var highest = FindObjectsByType<CityRooftop>(FindObjectsSortMode.None).OrderByDescending(r => r.landing.y).First();
                player.Respawn(highest.landing);
                yield return Hold(.2f, ControlFrame.Empty);
                LifeState.Hours = 21;
                CityLife.Instance.PanoramaYaw = 160;
                var view = highest.GetComponentInChildren<InteractionPoint>();
                if (view)
                    view.Interact(game);
                game.CameraRig.Snap();
                yield return Hold(.35f, ControlFrame.Empty);
                yield return Capture("roof-night");
                var citizen = CityPopulation.Instance.Citizens.First(c => !c.dead);
                Check(citizen.GetComponent<CityNpc>() && citizen.GetComponent<CityNpc>().point, "Ambient citizens have conversation interactions");
                citizen.GetComponent<WorldActor>().Damage(12, Vector3.right * 5);
                Check(WantedSystem.Level > 0, "Citizen attack starts wanted response");
                WantedSystem.Report(140, player.transform.position);
                yield return Hold(12.8f, ControlFrame.Empty);
                var wanted = WantedSystem.Instance;
                Check(wanted.ActiveOfficers == 12 && wanted.Cars.Count >= 3 && wanted.Helicopter, "Level five dispatches officers, response cars and helicopter");
                Check(wanted.Officers.Any(o => o.Weapon == PoliceWeapon.Rifle), "Special response rifles equipped");
                foreach (var officer in wanted.Officers.ToArray())
                    if (officer && officer.Body.Alive)
                        officer.Body.Damage(1000, Vector3.right);
                if (wanted.Helicopter)
                    wanted.Helicopter.Body.Damage(1000, Vector3.right);
                yield return Hold(.2f, ControlFrame.Empty);
                Check(WantedSystem.Level == 0, "Defeating dispatched force clears wanted status");
                phase = 1;
                UrbanCatalog.Enter(game, 9);
                yield break;
            }

            if (phase == 1)
            {
                var start = game.Player.transform.position;
                yield return Hold(.5f, ControlFrame.Empty);
                Check(game.Player.Grounded && Mathf.Abs(game.Player.transform.position.y - start.y) < .4f, "Interior arrival stays on solid floor");
                Check(FindObjectsByType<CityNpc>(FindObjectsSortMode.None).Length >= 6, "Restaurant contains staff and multiple patrons");
                Check(!FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t => t.name.StartsWith("IMAGE SIGN /")), "Interior retail signs removed");
                game.Player.ReceiveDamage(45, Vector3.zero, true);
                float health = game.Player.Health;
                int money = LifeState.Credits;
                CityLife.Instance.Services();
                CityLife.Instance.Options[0].action();
                Check(LifeState.Credits == money - 90 && game.Player.Health > health, "Restaurant purchase spends credits and heals");
                yield return Capture("restaurant-service");
                CityLife.Instance.Dismiss();
                phase = 2;
                game.Travel(StageId.Residence);
                yield break;
            }

            if (phase == 2)
            {
                CityLife.Instance.Wardrobe();
                int before = LifeState.Outfit;
                CityLife.Instance.Options[before == 0 ? 1 : 0].action();
                Check(LifeState.Outfit != before, "Wardrobe changes persistent outfit");
                CityLife.Instance.Dismiss();
                float hour = LifeState.Hours;
                CityLife.Instance.SleepMenu();
                CityLife.Instance.Options[0].action();
                yield return Hold(1.9f, ControlFrame.Empty);
                Check(LifeState.Hours >= hour + 6 && !game.Dialogue, "Bed sleep advances time and returns control");
                var lift = FindAnyObjectByType<MovingLift>();
                game.Player.Respawn(lift.platform.position + Vector3.up * .08f);
                game.CameraRig.Snap();
                yield return Hold(.2f, ControlFrame.Empty);
                lift.Use(game);
                yield return Hold(.5f, ControlFrame.Empty);
                Check(game.Player.Grounded && Mathf.Abs(game.Player.transform.position.y - lift.Height) < .2f, "Lift rider keeps grounded animation and follows platform");
                Finish();
            }
        }

        IEnumerator Hold(float duration, ControlFrame frame)
        {
            float end = Time.realtimeSinceStartup + duration;
            while (Time.realtimeSinceStartup < end)
            {
                game.Input.ExternalFrame = frame;
                yield return null;
            }

            game.Input.ExternalFrame = ControlFrame.Empty;
        }

        IEnumerator Capture(string name)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            var camera = Camera.main;
            var canvas = FindAnyObjectByType<Canvas>();
            var mode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float distance = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            target.Create();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
            Destroy(pixels);
            target.Release();
            Destroy(target);
            canvas.renderMode = mode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = distance;
            Canvas.ForceUpdateCanvases();
        }

        void Check(bool ok, string message)
        {
            (ok ? result.passed : result.errors).Add(message);
            Debug.Log("CITY LIFE / " + ok + " / " + message);
        }

        static void Snapshot()
        {
            foreach (string suffix in new[]
            {
                "Credits",
                "Savings",
                "Outfit",
                "Outfits",
                "Hours",
                "Heat",
                "Hidden",
                "Errand"
            }

            )
                Keep("AFTERSIGNAL.Unity.Life." + suffix, suffix == "Errand" ? 2 : suffix == "Hours" || suffix == "Heat" || suffix == "Hidden" ? 1 : 0);
            foreach (string suffix in new[]
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
                Keep(UrbanCatalog.Prefix + suffix, suffix == "Site" || suffix == "Car" || suffix == "CarStage" || suffix == "CarType" ? 0 : 1);
            Keep("AFTERSIGNAL.Unity.Stage", 0);
            Keep("AFTERSIGNAL.Unity.Memories", 0);
        }

        static void Keep(string key, int type)
        {
            saved.Add(new Saved { key = key, type = type, present = PlayerPrefs.HasKey(key), text = type == 2 ? PlayerPrefs.GetString(key) : "", value = type == 1 ? PlayerPrefs.GetFloat(key) : type == 0 ? PlayerPrefs.GetInt(key) : 0 });
        }

        static void Restore()
        {
            LifeState.SuppressSave = true;
            foreach (var s in saved)
            {
                if (!s.present)
                    PlayerPrefs.DeleteKey(s.key);
                else if (s.type == 2)
                    PlayerPrefs.SetString(s.key, s.text);
                else if (s.type == 1)
                    PlayerPrefs.SetFloat(s.key, s.value);
                else
                    PlayerPrefs.SetInt(s.key, (int)s.value);
            }

            PlayerPrefs.Save();
        }

        void Finish()
        {
            if (finished)
                return;
            finished = true;
            Restore();
            result.completed = result.errors.Count == 0;
            File.WriteAllText(Path.Combine(output, "life-smoke.json"), JsonUtility.ToJson(result, true));
            Application.Quit(result.completed ? 0 : 1);
        }

        void OnApplicationQuit()
        {
            if (initialized)
                Restore();
        }
    }
}
