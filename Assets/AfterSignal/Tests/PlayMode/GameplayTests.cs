using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AfterSignal.Tests
{
    public partial class GameplayTests
    {
        GameDirector game;
        int savedStage, savedMemories, savedCompleted;
        bool hadStage, hadMemories, hadCompleted;
        readonly string[] expansionKeys =
        {
            "AFTERSIGNAL.Unity.Expansion.Chapters",
            "AFTERSIGNAL.Unity.Expansion.Accepted",
            "AFTERSIGNAL.Unity.Expansion.Jobs"
        };
        readonly int[] expansionValues = new int[3];
        readonly bool[] expansionExists = new bool[3];
        readonly System.Collections.Generic.Dictionary<string, float> urbanValues = new System.Collections.Generic.Dictionary<string, float>();
        readonly System.Collections.Generic.HashSet<string> urbanExists = new System.Collections.Generic.HashSet<string>();
        [UnitySetUp]
        public IEnumerator Setup()
        {
            urbanValues.Clear();
            urbanExists.Clear();
            foreach (string s in new[]
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
                string k = UrbanCatalog.Prefix + s;
                if (PlayerPrefs.HasKey(k))
                    urbanExists.Add(k);
                urbanValues[k] = s == "Site" || s == "Car" || s == "CarStage" || s == "CarType" ? PlayerPrefs.GetInt(k) : PlayerPrefs.GetFloat(k);
            }

            for (int i = 0; i < 3; i++)
            {
                expansionExists[i] = PlayerPrefs.HasKey(expansionKeys[i]);
                expansionValues[i] = PlayerPrefs.GetInt(expansionKeys[i]);
            }

            hadStage = PlayerPrefs.HasKey("AFTERSIGNAL.Unity.Stage");
            hadMemories = PlayerPrefs.HasKey("AFTERSIGNAL.Unity.Memories");
            hadCompleted = PlayerPrefs.HasKey("AFTERSIGNAL.Unity.Completed");
            savedStage = PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Stage");
            savedMemories = PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Memories");
            savedCompleted = PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed");
            GameDirector.SkipTitle = true;
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Station));
            yield return null;
            game = GameDirector.Instance;
            game.Input.ExternalControl = true;
            game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(false);
            yield return new WaitForSeconds(.15f);
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (UrbanSimulation.Instance)
                UrbanSimulation.Instance.enabled = false;
            foreach (var kv in urbanValues)
            {
                if (!urbanExists.Contains(kv.Key))
                    PlayerPrefs.DeleteKey(kv.Key);
                else if (kv.Key.EndsWith("X") || kv.Key.EndsWith("Z") || kv.Key.EndsWith("Yaw") || kv.Key.EndsWith("Fuel") || kv.Key.EndsWith("Health"))
                    PlayerPrefs.SetFloat(kv.Key, kv.Value);
                else
                    PlayerPrefs.SetInt(kv.Key, (int)kv.Value);
            }

            for (int i = 0; i < 3; i++)
                Restore(expansionKeys[i], expansionValues[i], expansionExists[i]);
            Time.timeScale = 1;
            Restore("AFTERSIGNAL.Unity.Stage", savedStage, hadStage);
            Restore("AFTERSIGNAL.Unity.Memories", savedMemories, hadMemories);
            Restore("AFTERSIGNAL.Unity.Completed", savedCompleted, hadCompleted);
            yield return null;
        }

        void Restore(string key, int value, bool exists)
        {
            if (exists)
                PlayerPrefs.SetInt(key, value);
            else
                PlayerPrefs.DeleteKey(key);
        }

        ControlFrame Controls(Vector3 aim)
        {
            var c = ControlFrame.Empty;
            c.pointer = Camera.main.WorldToScreenPoint(aim);
            return c;
        }

        IEnumerator Drive(ControlFrame c, float seconds)
        {
            game.Input.ExternalFrame = c;
            yield return new WaitForSeconds(seconds);
            game.Input.ExternalFrame = ControlFrame.Empty;
        }

        [UnityTest]
        public IEnumerator NativeKeyboardAndMouseEventsReachTheController()
        {
            bool previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            var previousBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard);
            InputSystem.EnableDevice(mouse);
            yield return null;
            game.Input.ExternalControl = false;
            var start = game.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .8f, Screen.height * .5f), buttons = 1 });
            yield return new WaitForSeconds(.2f);
            Assert.Greater(game.Input.Read().move.x, .6f, $"keyboard W={keyboard.wKey.isPressed}, enabled={keyboard.enabled}, {game.Input.Diagnostics}");
            Assert.Greater(game.Input.Frame.move.y, .6f);
            Assert.Greater(game.Player.Attacks, 0);
            Assert.Greater(game.Player.transform.position.x, start.x);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .3f, Screen.height * .5f), buttons = 2 });
            yield return null;
            Assert.Less(game.Input.Read().move.x, 0);
            Assert.Less(game.Input.Frame.move.y, 0);
            Assert.IsTrue(game.Input.Frame.grapple);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = previousBehavior;
            Application.runInBackground = previousRunInBackground;
            game.Input.ExternalControl = true;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=previousEditor;
#endif
        }

        [UnityTest]
        public IEnumerator MovementUsesBothHorizontalAndDepthAxesAndStops()
        {
            var start = game.Player.transform.position;
            var c = Controls(start + Vector3.right * 8);
            c.move = new Vector2(1, 1);
            yield return Drive(c, .35f);
            Assert.Greater(game.Player.transform.position.x, start.x + 1);
            Assert.Greater(game.Player.transform.position.z, start.z + .5f);
            yield return new WaitForSeconds(.4f);
            Assert.Less(Mathf.Abs(game.Player.Velocity.x), .05f);
            Assert.Less(Mathf.Abs(game.Player.Velocity.z), .05f);
        }

        [UnityTest]
        public IEnumerator JumpDashAndPausePreserveGameState()
        {
            var start = game.Player.transform.position;
            var c = Controls(start + Vector3.right * 8);
            c.jump = true;
            yield return Drive(c, .03f);
            yield return new WaitForSeconds(.18f);
            Assert.Greater(game.Player.transform.position.y, start.y + 1);
            c.jump = false;
            c.dash = true;
            yield return Drive(c, .03f);
            Assert.Greater(game.Player.DashTime, 0);
            game.SetPaused(true);
            var paused = game.Player.transform.position;
            float elapsed = game.Elapsed;
            yield return new WaitForSecondsRealtime(.12f);
            Assert.AreEqual(paused, game.Player.transform.position);
            Assert.AreEqual(elapsed, game.Elapsed);
            game.SetPaused(false);
        }

        [UnityTest]
        public IEnumerator GrappleSelectsMouseTargetReelsAndReleasesMomentum()
        {
            var anchor = GrappleAnchor.All.Find(a => Mathf.Abs(a.transform.position.x - 12) < .1f);
            var c = Controls(anchor.transform.position);
            c.grapple = true;
            c.move = Vector2.up;
            yield return Drive(c, .08f);
            Assert.IsTrue(game.Player.Rope.Attached);
            float length = game.Player.Rope.Length;
            yield return Drive(c, .2f);
            Assert.Less(game.Player.Rope.Length, length);
            c.grapple = false;
            c.move = Vector2.right;
            yield return Drive(c, .05f);
            Assert.IsFalse(game.Player.Rope.Attached);
            Assert.Greater(game.Player.Velocity.magnitude, 1);
        }

        [UnityTest]
        public IEnumerator GlassBlocksTravelUntilDamaged()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Carriage));
            yield return null;
            game = GameDirector.Instance;
            var glass = game.Glass[0];
            Assert.IsTrue(glass.GetComponent<Collider>().enabled);
            glass.Hit(36);
            Assert.IsFalse(glass.Broken);
            glass.Hit(36);
            Assert.IsTrue(glass.Broken);
            Assert.IsFalse(glass.GetComponent<Collider>().enabled);
            Assert.IsTrue(game.BrokenGlass);
        }

        [UnityTest]
        public IEnumerator CarriageCeilingStopsJumpingThroughTheTrain()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Carriage));
            yield return null;
            game = GameDirector.Instance;
            game.Input.ExternalControl = true;
            game.SetPaused(false);
            yield return new WaitForSeconds(.15f);
            var c = Controls(game.Player.Shoulder + Vector3.right * 5);
            c.jump = true;
            game.Input.ExternalFrame = c;
            yield return null;
            game.Input.ExternalFrame = ControlFrame.Empty;
            float highest = 0;
            for (float t = 0; t < .65f; t += Time.deltaTime)
            {
                highest = Mathf.Max(highest, game.Player.transform.position.y);
                yield return null;
            }

            Assert.That(highest, Is.GreaterThan(.7f));
            Assert.That(highest, Is.LessThan(1.96f));
        }

        [UnityTest]
        public IEnumerator MeleeAndPistolDamageActualEnemyColliders()
        {
            var e = game.Enemies[0];
            e.GetComponent<CharacterController>().enabled = false;
            e.transform.position = new Vector3(7, 5, .1f);
            e.GetComponent<CharacterController>().enabled = true;
            var c = Controls(e.transform.position + Vector3.up * 1.3f);
            c.weapon = 2;
            c.attack = true;
            yield return Drive(c, .08f);
            Assert.Less(e.Health, e.maxHealth);
            c.weapon = 1;
            c.attack = false;
            yield return Drive(c, .3f);
            c.attack = true;
            yield return Drive(c, .35f);
            Assert.IsFalse(e.Alive);
            Assert.That(game.Player.Muzzle.x - game.Player.transform.position.x, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator DirectionalGuardReducesIncomingDamage()
        {
            var c = Controls(game.Player.Shoulder + Vector3.right * 8);
            c.guard = true;
            yield return Drive(c, .1f);
            float before = game.Player.Health;
            game.Player.ReceiveDamage(12, game.Player.transform.position + Vector3.right * 3, true);
            Assert.That(before - game.Player.Health, Is.EqualTo(1.8f).Within(.01f));
            yield return Drive(ControlFrame.Empty, .1f);
            before = game.Player.Health;
            game.Player.ReceiveDamage(12, game.Player.transform.position + Vector3.right * 3, true);
            Assert.That(before - game.Player.Health, Is.EqualTo(12).Within(.01f));
        }

        [UnityTest]
        public IEnumerator BossNeedsTwoCapacitorsAndThreeCoreStrikes()
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Roof));
            yield return null;
            game = GameDirector.Instance;
            game.Input.ExternalControl = true;
            game.Player.Respawn(new Vector3(59, .1f, 0));
            yield return new WaitForSeconds(.1f);
            float hp = game.Boss.Health;
            game.Boss.Damage(100, Vector3.zero);
            Assert.That(hp - game.Boss.Health, Is.LessThan(5));
            for (int cycle = 0; cycle < 3; cycle++)
            {
                game.Player.Respawn(new Vector3(59, .1f, 0));
                game.Player.Rope.Attach(GrappleAnchor.All.Find(a => a.capacitor == 0));
                game.Player.Rope.Release();
                Assert.AreEqual(0, game.ExposeTimer);
                game.Player.Rope.Attach(GrappleAnchor.All.Find(a => a.capacitor == 1));
                game.Player.Rope.Release();
                Assert.Greater(game.ExposeTimer, 0);
                game.TryCoreStrike(game.Player, 3.4f);
                yield return new WaitForSeconds(1.02f);
            }

            Assert.IsFalse(game.Boss.Alive);
            Assert.AreEqual(3, game.CoreStrikes);
        }

        IEnumerator LoadDistrict(StageId stage)
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(stage));
            yield return null;
            game = GameDirector.Instance;
            game.Input.ExternalControl = true;
            game.SetPaused(false);
            game.CloseDialogue();
            yield return new WaitForSeconds(.2f);
        }

        [UnityTest]
        public IEnumerator LiftCarriesRiderUpAndDownAndIgnoresMidTravelToggle()
        {
            yield return LoadDistrict(StageId.Lab);
            var lift = Object.FindAnyObjectByType<MovingLift>();
            game.Player.Respawn(new Vector3(26, .1f, 0));
            yield return new WaitForSeconds(.2f);
            lift.Use(game);
            yield return new WaitForSeconds(.3f);
            Assert.IsTrue(lift.Moving);
            lift.Use(game);
            yield return new WaitForSeconds(2.6f);
            Assert.That(lift.Height, Is.EqualTo(6).Within(.03f));
            Assert.That(game.Player.transform.position.y, Is.InRange(5.9f, 6.3f));
            lift.Use(game);
            yield return new WaitForSeconds(2.9f);
            Assert.That(game.Player.transform.position.y, Is.InRange(-.1f, .3f));
        }

        [UnityTest]
        public IEnumerator EscalatorCarriesAnIdleRiderWithoutChangingSimulationSpeed()
        {
            yield return LoadDistrict(StageId.Arcade);
            game.Player.Respawn(new Vector3(35, 1.7f, 0));
            yield return new WaitForSeconds(.3f);
            float start = game.Player.transform.position.x;
            yield return new WaitForSeconds(.8f);
            Assert.Greater(game.Player.transform.position.x, start + .5f);
            Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator TownEscalatorJoinsItsUpperFloorWithoutAHiddenStep()
        {
            yield return LoadDistrict(StageId.Haven);
            game.Player.Respawn(new Vector3(9, .1f, 4));
            yield return new WaitForSeconds(.2f);
            var c = Controls(new Vector3(36, 6, 4));
            c.move = Vector2.right;
            float end = Time.time + 5;
            while (Time.time < end && game.Player.transform.position.x < 31)
            {
                game.Input.ExternalFrame = c;
                yield return null;
            }

            game.Input.ExternalFrame = ControlFrame.Empty;
            Assert.Greater(game.Player.transform.position.x, 30);
            Assert.That(game.Player.transform.position.y, Is.InRange(4.9f, 5.3f));
        }

        [UnityTest]
        public IEnumerator NewChapterGateNeedsQuestAndSideRewardsCannotDuplicate()
        {
            yield return LoadDistrict(StageId.Haven);
            PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Completed", 1);
            PlayerPrefs.DeleteKey(expansionKeys[0]);
            PlayerPrefs.DeleteKey(expansionKeys[1]);
            CampaignCatalog.Jobs = 0;
            var points = Object.FindObjectsByType<InteractionPoint>();
            System.Array.Find(points, p => p.kind == InteractionKind.MissionBoard).Interact(game);
            Assert.IsFalse(game.Transition);
            System.Array.Find(points, p => p.kind == InteractionKind.QuestGiver).Interact(game);
            Assert.AreEqual(2, PlayerPrefs.GetInt(expansionKeys[1]));
            game.CloseDialogue();
            var min = System.Array.Find(points, p => p.kind == InteractionKind.MinJob);
            CampaignCatalog.Jobs = 2;
            int memory = game.Memories;
            min.Interact(game);
            min.Interact(game);
            Assert.AreEqual(memory + 1, game.Memories);
            CampaignCatalog.Complete(2);
            Assert.AreEqual(3, CampaignCatalog.NextChapter);
            CampaignCatalog.Complete(3);
            Assert.AreEqual(4, CampaignCatalog.NextChapter);
            CampaignCatalog.Complete(4);
            Assert.AreEqual(0, CampaignCatalog.NextChapter);
        }

        [UnityTest]
        public IEnumerator BakedGeometryRetainsFloorAndDestructibleCollision()
        {
            yield return LoadDistrict(StageId.Archive);
            var manifest = Object.FindAnyObjectByType<SceneBatchManifest>();
            Assert.IsNotNull(manifest);
            Assert.Greater(manifest.sources.Length, 100);
            Assert.Less(manifest.generated.Length, manifest.sources.Length / 3);
            Assert.IsTrue(Physics.Raycast(new Vector3(58, 12, 0), Vector3.down, out var hit, 8, 1 << 0));
            Assert.That(hit.point.y, Is.EqualTo(8).Within(.1f));
            var glass = game.Glass[0];
            Assert.IsTrue(glass.GetComponent<Renderer>().enabled);
            Assert.IsTrue(glass.GetComponent<Collider>().enabled);
            glass.Hit(100);
            Assert.IsFalse(glass.GetComponent<Collider>().enabled);
        }

        EnemyBrain NearbyTarget(float x = 2)
        {
            var e = game.Enemies[0];
            var cc = e.GetComponent<CharacterController>();
            cc.enabled = false;
            e.transform.position = game.Player.transform.position + Vector3.right * x;
            cc.enabled = true;
            e.activateAt = 999;
            return e;
        }

        [UnityTest]
        public IEnumerator AttackWindupContactAndRecoveryDoNotDoubleHit()
        {
            var e = NearbyTarget();
            var c = Controls(e.transform.position + Vector3.up);
            c.attack = true;
            yield return Drive(c, .02f);
            c.attack = false;
            float hp = e.Health;
            Assert.AreEqual(e.maxHealth, hp, "Anticipation has no hitbox");
            yield return Drive(c, .18f);
            Assert.AreEqual(e.maxHealth - 36, e.Health);
            Assert.AreEqual(1, game.Player.ConfirmedHits);
            yield return Drive(c, .23f);
            Assert.AreEqual(e.maxHealth - 36, e.Health, "Active window cannot damage twice");
        }

        [UnityTest]
        public IEnumerator WhiffNeverFreezesOrReportsAHit()
        {
            var c = Controls(game.Player.Shoulder + Vector3.right * 5);
            c.attack = true;
            yield return Drive(c, .02f);
            yield return new WaitForSeconds(.36f);
            Assert.AreEqual(0, game.Player.ConfirmedHits);
            Assert.AreEqual(0, game.Player.HitStopRemaining);
            Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator HitstopPauseAndSlowMotionDoNotOverwriteTimeScale()
        {
            var e = NearbyTarget();
            var c = Controls(e.transform.position + Vector3.up);
            c.attack = true;
            Time.timeScale = .5f;
            yield return Drive(c, .16f);
            Assert.AreEqual(.5f, Time.timeScale);
            Assert.Less(e.Health, e.maxHealth);
            game.SetPaused(true);
            var pos = game.Player.transform.position;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(pos, game.Player.transform.position);
            game.SetPaused(false);
            Assert.AreEqual(.5f, Time.timeScale, "Pause must restore the previous speed");
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator TakingDamageCancelsAnUnresolvedWindup()
        {
            var e = NearbyTarget();
            var c = Controls(e.transform.position + Vector3.up);
            c.attack = true;
            yield return Drive(c, .02f);
            game.Player.ReceiveDamage(12, e.transform.position, true);
            yield return Drive(ControlFrame.Empty, .4f);
            Assert.AreEqual(e.maxHealth, e.Health);
        }

        [UnityTest]
        public IEnumerator AudioAndEffectBudgetsRemainBounded()
        {
            for (int i = 0; i < 100; i++)
            {
                game.Audio.Play("blade_hit", game.Player.Shoulder, .4f, 3);
                SignalEffects.Impact(game.Player.Shoulder, Vector3.right, SignalEffects.Cyan, 1);
            }

            Assert.LessOrEqual(game.Audio.ActiveVoices, 14);
            Assert.LessOrEqual(EffectBudget.Active, 64);
            yield return new WaitForSeconds(1.3f);
            Assert.AreEqual(0, EffectBudget.Active);
        }

        [UnityTest]
        public IEnumerator ContactSurvivesFifteenAndOneTwentyHzSimulation()
        {
            game.enabled = false;
            var e = NearbyTarget();
            float previous = e.Health;
            foreach (int rate in new[]
            {
                15,
                120
            }

            )
            {
                var c = Controls(e.transform.position + Vector3.up);
                c.attack = true;
                game.Player.Tick(c, 1f / rate);
                c.attack = false;
                for (int i = 0; i < rate / 2; i++)
                {
                    game.Player.Tick(c, 1f / rate);
                    yield return new WaitForSecondsRealtime(1f / rate);
                }

                Assert.That(previous - e.Health, Is.EqualTo(Mathf.Min(36, previous)).Within(.01f), "Exactly one hit when a render step crosses contact at " + rate + " Hz");
                previous = e.Health;
            // The second strike is also a deliberate kill; no extra collider or skipped pose can duplicate it.
            }

            Assert.AreEqual(2, game.Player.ConfirmedHits);
            game.enabled = true;
        }

        [UnityTest]
        public IEnumerator OneSwingHitsTwoEnemiesButNotTheirExtraColliders()
        {
            var first = NearbyTarget();
            var extra = first.gameObject.AddComponent<BoxCollider>();
            extra.center = Vector3.up;
            extra.size = new Vector3(.6f, 1.8f, .6f);
            var second = game.Enemies[1];
            var cc = second.GetComponent<CharacterController>();
            cc.enabled = false;
            second.transform.position = first.transform.position + Vector3.forward * .45f;
            cc.enabled = true;
            second.activateAt = 999;
            float a = first.Health, b = second.Health;
            var c = Controls(first.transform.position + Vector3.up);
            c.attack = true;
            yield return Drive(c, .02f);
            yield return Drive(ControlFrame.Empty, .38f);
            Assert.AreEqual(a - 36, first.Health);
            Assert.AreEqual(b - 36, second.Health);
            Assert.AreEqual(2, game.Player.ConfirmedHits);
        }
    }
}
