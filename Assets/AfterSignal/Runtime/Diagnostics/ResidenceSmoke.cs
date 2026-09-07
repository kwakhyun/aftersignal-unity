using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    // Opt-in native QA drives the same ControlFrame as real input. It restores the user's saves.
    public sealed class ResidenceSmoke : MonoBehaviour
    {
        [Serializable]
        class Check
        {
            public string stage, detail;
            public bool passed;
            public float health, seconds;
            public int kills, ropes;
        }

        [Serializable]
        class Report
        {
            public bool completed;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
            public string fixtures = "Optional Breach unlock only; traversal and damage use ordinary controls.";
        }

        static Report report = new Report();
        static readonly Dictionary<string, int> saved = new Dictionary<string, int>();
        static readonly HashSet<string> existed = new HashSet<string>();
        static bool initialized;
        static int phase;
        GameDirector game;
        QualitySession metrics;
        string root, output;
        bool failed;
        void Awake()
        {
            Application.logMessageReceived += Log;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }

        void Log(string t, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                report.errors.Add(t);
                failed = true;
                Write();
            }
        }

        IEnumerator Start()
        {
            game = GetComponent<GameDirector>();
            root = Path.GetFullPath(QualitySession.Arg("-quality-output", Path.Combine(Application.dataPath, "../../../Artifacts/Residence/Run")));
            Directory.CreateDirectory(root);
            if (!initialized)
            {
                initialized = true;
                foreach (string name in new[]
                {
                    "Stage",
                    "Memories",
                    "Completed",
                    "Expansion.Chapters",
                    "Expansion.Accepted",
                    "Expansion.Jobs"
                }

                )
                {
                    string key = "AFTERSIGNAL.Unity." + name;
                    saved[key] = PlayerPrefs.GetInt(key);
                    if (PlayerPrefs.HasKey(key))
                        existed.Add(key);
                }

                GameDirector.SkipTitle = true;
                PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Completed", 0);
                var stage = Enum.TryParse(QualitySession.Arg("-residence-only"), out StageId fixture) ? fixture : StageId.Residence;
                yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(stage));
                yield break;
            }

            output = Path.Combine(root, game.stage + "-" + phase);
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            FramePacing.Apply();
            game.Input.ExternalControl = true;
            game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(false);
            metrics = gameObject.AddComponent<QualitySession>();
            QualitySession.Output = output;
            yield return Hold(.6f, ControlFrame.Empty);
            if (game.Dialogue)
            {
                yield return Hold(.6f, ControlFrame.Empty);
                game.CloseDialogue();
            }

            yield return Capture("entry");
            if (game.stage == StageId.Residence)
                yield return Home();
            else if (game.stage == StageId.Haven)
                yield return Town();
            else if (CivicWorld.Interior(game.stage))
                yield return Interior();
            else if (game.stage == StageId.Breach)
                yield return Facade();
            else
            {
                Record(true, "Main quest reached the original rail campaign");
                Finish();
            }

            if (failed)
                Finish();
        }

        IEnumerator Home()
        {
            yield return SpriteRenderProbe.Run(output);
            Record(SpriteRenderProbe.Passed, "GPU sprite shader produces actual mirrored silhouettes");
            Record(game.Player.transform.position.y > 21, "New campaign starts in Seoha's upper-floor room");
            yield return Go(6, 1.4f);
            yield return Use(InteractionKind.Furniture);
            yield return Capture("bed-dialogue");
            game.CloseDialogue();
            yield return Go(14, 0);
            yield return Use(InteractionKind.Furniture);
            game.CloseDialogue();
            yield return Go(23, 1.5f);
            yield return Use(InteractionKind.Furniture);
            game.CloseDialogue();
            yield return Go(24, 0);
            yield return Arsenal();
            yield return Go(26, 0);
            yield return Use(InteractionKind.HomeDoor);
            yield return Hold(.65f, ControlFrame.Empty);
            yield return Go(35, 0);
            yield return Capture("hall");
            yield return Go(42, 0);
            yield return Use(InteractionKind.Lift);
            yield return Hold(5.1f, ControlFrame.Empty);
            Record(game.Player.transform.position.y < .5f, "Elevator carries rider from residence to ground lobby");
            yield return Capture("lobby");
            yield return Go(55, 0);
            yield return Go(58, 2);
            for (int i = 0; i < 5 && !failed; i++)
            {
                bool right = i % 2 == 0;
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stair-camera-review") >= 0)
                {
                    yield return Go(69, right ? 2 : 8);
                    yield return Hold(.4f, ControlFrame.Empty);
                    yield return Capture("stair-middle-" + i);
                }

                yield return Go(right ? 80 : 58, right ? 2 : 8);
                Record(game.Player.transform.position.y > 4.4f * (i + 1) - .5f, "Emergency stair flight " + (i + 1));
                if (i < 4)
                    yield return Go(right ? 80 : 58, right ? 8 : 2);
            }

            yield return Capture("stairs-top");
            for (int i = 4; i >= 0 && !failed; i--)
            {
                bool right = i % 2 == 0;
                yield return Go(right ? 58 : 80, right ? 2 : 8);
                if (i > 0)
                    yield return Go(right ? 58 : 80, right ? 8 : 2);
            }

            yield return Go(55, 0);
            yield return Go(34, -3);
            Record(!failed, "Furniture, hinged door, elevator and five reversible stair flights");
            metrics.Save();
            if (Single())
            {
                Finish();
                yield break;
            }

            phase = 1;
            yield return Use(InteractionKind.ReturnTown);
        }

        IEnumerator Arsenal()
        {
            for (int w = 0; w < 3 && !failed; w++)
            {
                var c = Aim(game.Player.Shoulder + Vector3.left * 8);
                c.move = Vector2.left;
                c.weapon = w;
                yield return Hold(.25f, c);
                yield return Capture("left-walk-" + w);
                Record(game.Player.Facing < 0, "Leftward locomotion / weapon " + w);
                c.weapon = -1;
                c.move = Vector2.up;
                yield return Hold(.35f, c);
                yield return Capture("back-walk-" + w);
                c.move = Vector2.down;
                yield return Hold(.45f, c);
                yield return Capture("front-walk-" + w);
                c.move = Vector2.zero;
                c.pointer = Camera.main.WorldToScreenPoint(game.Player.Shoulder + Vector3.right * 10);
                c.attack = true;
                yield return Hold(w == 1 ? 2.2f : 1.45f, c);
                yield return Hold(.9f, ControlFrame.Empty);
                c.attack = true;
                c.dash = true;
                c.move = Vector2.left;
                game.Input.ExternalFrame = c;
                yield return null;
                c.dash = false;
                c.attack = false;
                c.move = Vector2.zero;
                yield return Hold(.9f, c);
                c.skill = true;
                game.Input.ExternalFrame = c;
                yield return null;
                c.skill = false;
                yield return Hold(1, c);
                yield return Capture("weapon-" + w);
                yield return Hold(3.3f, ControlFrame.Empty);
                if (w == 2)
                {
                    int rounds = game.Player.Ammo;
                    c.reload = true;
                    game.Input.ExternalFrame = c;
                    yield return null;
                    c.reload = false;
                    yield return Hold(.5f, c);
                    yield return Capture("reload");
                    yield return Hold(.8f, c);
                    Record(game.Player.Ammo == game.tuning.magazineSize && game.Player.Reloads > 0, "Pistol consumed rounds and completed magazine reload (before " + rounds + ")");
                }
            }

            Record(game.Player.DashAttacks >= 3 && game.Player.SkillsUsed >= 3, "Three weapon dash attacks and three distinct special skills");
        }

        IEnumerator Town()
        {
            if (phase >= 5)
            {
                Record((PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Expansion.Chapters") & (1 << 5)) != 0, "Breach completion saved and returned to town");
                metrics.Save();
                Finish();
                yield break;
            }

            if (Single())
            {
                yield return Go(42, 0);
                yield return Go(42, 18);
                yield return Go(94, 18);
                yield return Capture("cross-street");
                yield return Go(138, 18);
                yield return Go(204, 18);
                yield return Go(204, -12);
                yield return Capture("east-avenue");
                Record(!failed, "Town routes across horizontal and depth axes");
                metrics.Save();
                Finish();
                yield break;
            }

            if (phase == 1)
            {
                yield return Go(42, 14);
                yield return Go(42, 19);
                yield return Go(64, 19);
                yield return Capture("school-street");
                phase = 2;
                yield return Use(InteractionKind.FacilityTravel);
            }
            else if (phase == 2)
            {
                yield return Go(94, 19);
                yield return Go(113, 19);
                yield return Capture("clinic-street");
                phase = 3;
                yield return Use(InteractionKind.FacilityTravel);
            }
            else
            {
                yield return Go(138, 19);
                yield return Go(178, 19);
                yield return Capture("headquarters-street");
                Record(game.Player.transform.position.z > 18, "Town reaches real depth street and three civic buildings");
                phase = 4;
                yield return Use(InteractionKind.FacilityTravel);
            }
        }

        IEnumerator Interior()
        {
            if (game.stage == StageId.School)
            {
                yield return Go(14, 1);
                yield return Use(InteractionKind.Citizen);
                yield return Capture("teacher");
                game.CloseDialogue();
                yield return Go(18, -6);
                yield return Go(48, -6);
                yield return Capture("classroom");
                yield return Go(16, -6);
            }
            else if (game.stage == StageId.Clinic)
            {
                yield return Go(13, 1);
                yield return Use(InteractionKind.Furniture);
                game.CloseDialogue();
                yield return Go(35, -2);
                yield return Capture("treatment");
                yield return Go(12, -2);
            }
            else
            {
                yield return Go(15, 1);
                yield return Use(InteractionKind.Citizen);
                game.CloseDialogue();
                yield return Go(46, -4);
                yield return Use(InteractionKind.BreachMission);
                Record(game.Dialogue && !game.Transition, "Facade mission stays locked before the original rail clear");
                yield return Capture("mission-locked");
                game.CloseDialogue();
                Record(UnityEngine.Object.FindObjectsByType<InteractionPoint>().Length >= 5, "Headquarters has commander, rail and restoration missions");
                metrics.Save();
                if (Single())
                {
                    Finish();
                    yield break;
                }

                PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Completed", 1);
                phase = 5;
                yield return Use(InteractionKind.BreachMission);
                yield break;
            }

            yield return Go(3, -4);
            Record(!failed, "Facility interior, fitting citizen interaction and return door");
            metrics.Save();
            if (Single())
            {
                Finish();
                yield break;
            }

            yield return Use(InteractionKind.ReturnTown);
        }

        IEnumerator Facade()
        {
            yield return Go(21, 0);
            float[] ax =
            {
                27,
                45,
                62,
                80
            }, lx =
            {
                33,
                50,
                68,
                84
            };
            for (int i = 0; i < 4 && !failed; i++)
            {
                yield return Swing(ax[i], lx[i], 6 * (i + 1));
                yield return Capture("climb-" + i);
            }

            Record(game.Player.transform.position.y > 23 && game.Player.Rope.AttachCount >= 4, "Four rope ascents reach the upper-floor exterior");
            yield return Go(92, 0, true);
            yield return Capture("window-breach");
            yield return Go(128, 0, true);
            yield return Clear();
            yield return Go(124, 2);
            yield return Use(InteractionKind.Memory);
            game.CloseDialogue();
            yield return Go(133, -1.2f);
            yield return Use(InteractionKind.DistrictRelay);
            yield return Hold(.6f, ControlFrame.Empty);
            yield return Go(139, 0, true);
            yield return Capture("records-secured");
            Record(game.BrokenGlass && game.Cleared && game.Power, "Break upper window, clear 14 guards, preserve record and unlock exit");
            metrics.Save();
            if (Single())
            {
                Finish();
                yield break;
            }

            yield return Use(InteractionKind.DistrictExit);
        }

        ControlFrame Aim(Vector3 point, bool attack = false)
        {
            var c = ControlFrame.Empty;
            c.pointer = Camera.main.WorldToScreenPoint(point);
            c.attack = attack;
            return c;
        }

        EnemyBrain Target()
        {
            EnemyBrain best = null;
            float distance = 999;
            foreach (var e in game.Enemies)
                if (e && e.Alive && Mathf.Abs(e.transform.position.y - game.Player.transform.position.y) < 3)
                {
                    float d = Vector3.Distance(e.transform.position, game.Player.transform.position);
                    if (d < distance)
                    {
                        distance = d;
                        best = e;
                    }
                }

            return best;
        }

        IEnumerator Go(float x, float z, bool fight = false)
        {
            if (failed)
                yield break;
            float end = Time.realtimeSinceStartup + 30;
            while (!failed && Time.realtimeSinceStartup < end)
            {
                var pos = game.Player.transform.position;
                if (Mathf.Abs(pos.x - x) < .3f && Mathf.Abs(pos.z - z) < .22f)
                    break;
                if (game.Dead)
                {
                    Fail("Player died during traversal");
                    break;
                }

                var e = Target();
                bool near = fight && e && Vector3.Distance(e.transform.position, pos) < 3.8f;
                bool glass = false;
                if (fight)
                    foreach (var g in game.Glass)
                        if (g && !g.Broken && Mathf.Abs(g.transform.position.x - pos.x) < 3.8f)
                            glass = true;
                var c = Aim(near ? e.transform.position + Vector3.up * 1.2f : new Vector3(x, pos.y + 1.3f, z), near || glass);
                c.weapon = 1;
                c.move = new Vector2(Mathf.Abs(x - pos.x) > .16f ? Mathf.Sign(x - pos.x) : 0, Mathf.Abs(z - pos.z) > .14f ? Mathf.Sign(z - pos.z) : 0);
                c.skill = near && game.Player.Energy > 35 && game.Player.SkillCooldown <= 0;
                game.Input.ExternalFrame = c;
                yield return null;
            }

            game.Input.ExternalFrame = ControlFrame.Empty;
            if (Mathf.Abs(game.Player.transform.position.x - x) > 1 || Mathf.Abs(game.Player.transform.position.z - z) > .7f)
                Fail("Movement timeout toward " + x + ", " + z);
        }

        IEnumerator Swing(float x, float landing, float height)
        {
            var a = GrappleAnchor.All.Find(v => Mathf.Abs(v.transform.position.x - x) < .1f);
            if (!a)
            {
                Fail("Missing anchor " + x);
                yield break;
            }

            float end = Time.realtimeSinceStartup + 4;
            bool attached = false;
            while (Time.realtimeSinceStartup < end)
            {
                var c = Aim(a.transform.position);
                c.grapple = true;
                c.move = Vector2.up;
                game.Input.ExternalFrame = c;
                attached |= game.Player.Rope.Attached;
                if (attached && game.Player.transform.position.y > height + .8f)
                    break;
                yield return null;
            }

            if (!attached)
            {
                Fail("Rope could not attach at " + x);
                yield break;
            }

            var release = Aim(a.transform.position);
            release.grapple = true;
            release.jump = true;
            release.move = Vector2.right;
            game.Input.ExternalFrame = release;
            yield return null;
            release.jump = release.grapple = false;
            yield return Hold(.15f, release);
            yield return Go(landing, 0, true);
            yield return Hold(.3f, ControlFrame.Empty);
            if (game.Player.transform.position.y < height - .5f)
                Fail("Missed upper landing " + height);
        }

        IEnumerator Clear()
        {
            float end = Time.realtimeSinceStartup + 45;
            while (!failed && !game.Cleared && Time.realtimeSinceStartup < end)
            {
                var e = Target();
                if (!e)
                {
                    Fail("Guards outside reachable combat lane");
                    break;
                }

                var d = e.transform.position - game.Player.transform.position;
                var c = Aim(e.transform.position + Vector3.up, true);
                c.weapon = 1;
                c.move = new Vector2(Mathf.Abs(d.x) > 2 ? Mathf.Sign(d.x) : 0, Mathf.Abs(d.z) > .6f ? Mathf.Sign(d.z) : 0);
                c.skill = game.Player.Energy > 35 && d.magnitude < 4;
                game.Input.ExternalFrame = c;
                if (game.Dead)
                    Fail("Player died in combat");
                yield return null;
            }

            game.Input.ExternalFrame = ControlFrame.Empty;
            if (!game.Cleared)
                Fail("Remaining guards");
        }

        IEnumerator Hold(float seconds, ControlFrame c)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!failed && Time.realtimeSinceStartup < end)
            {
                game.Input.ExternalFrame = c;
                yield return null;
            }

            game.Input.ExternalFrame = ControlFrame.Empty;
        }

        IEnumerator Use(InteractionKind kind)
        {
            if (failed)
                yield break;
            yield return Hold(.25f, ControlFrame.Empty);
            if (!game.Nearby || game.Nearby.kind != kind)
            {
                Fail("Expected " + kind + ", got " + (game.Nearby ? game.Nearby.kind.ToString() : "none"));
                yield break;
            }

            var c = ControlFrame.Empty;
            c.interact = true;
            game.Input.ExternalFrame = c;
            yield return null;
            yield return Hold(.3f, ControlFrame.Empty);
        }

        IEnumerator Capture(string name)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-quality-no-captures") >= 0)
                yield break;
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }

        bool Single() => !string.IsNullOrEmpty(QualitySession.Arg("-residence-only"));
        void Fail(string detail)
        {
            if (failed)
                return;
            failed = true;
            report.errors.Add(game.stage + ": " + detail + " at " + game.Player.transform.position);
            Write();
        }

        void Record(bool pass, string detail)
        {
            report.checks.Add(new Check { stage = game.stage.ToString(), detail = detail, passed = pass && !failed, health = game.Player.Health, kills = game.Kills, ropes = game.Player.Rope.AttachCount, seconds = game.Elapsed });
            if (!pass)
                Fail(detail);
            Write();
        }

        void Write()
        {
            if (!string.IsNullOrEmpty(root))
                File.WriteAllText(Path.Combine(root, "residence.json"), JsonUtility.ToJson(report, true));
        }

        void Restore()
        {
            foreach (var p in saved)
            {
                if (existed.Contains(p.Key))
                    PlayerPrefs.SetInt(p.Key, p.Value);
                else
                    PlayerPrefs.DeleteKey(p.Key);
            }

            PlayerPrefs.Save();
        }

        void OnApplicationQuit()
        {
            Restore();
        }

        void Finish()
        {
            if (metrics)
                metrics.Save();
            report.completed = !failed && report.errors.Count == 0;
            Write();
            Restore();
            Application.Quit(report.completed ? 0 : 1);
        }
    }
}
