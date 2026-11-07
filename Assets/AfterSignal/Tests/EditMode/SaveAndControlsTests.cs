using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterSignal.Tests
{
    /// <summary>Save slots, armory save migration, input-action coverage and key-hint rewriting.</summary>
    public sealed class SaveAndControlsTests
    {
        const int TestSlot = 2; // slot 1 (index 0) would import the developer's legacy PlayerPrefs
        string directory;
        int previousSlot;

        [SetUp]
        public void SetUp()
        {
            previousSlot = SaveStore.ActiveSlot;
            directory = Path.Combine(Path.GetTempPath(), "aftersignal-save-tests-" + System.Guid.NewGuid().ToString("N"));
            SaveStore.Directory = directory;
            SaveStore.ActiveSlot = TestSlot;
            LifeState.SuppressSave = false;
        }

        [TearDown]
        public void TearDown()
        {
            SaveStore.Directory = null;
            SaveStore.ActiveSlot = previousSlot;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        static void Reload() => SaveStore.Directory = SaveStore.Directory;

        [Test]
        public void SlotValuesSurviveAReload()
        {
            SaveStore.SetInt("AFTERSIGNAL.Unity.Stage", (int)StageId.Residence);
            SaveStore.SetFloat("AFTERSIGNAL.Unity.Life.Hours", 31.5f);
            SaveStore.SetString("AFTERSIGNAL.Unity.Test", "서하");
            SaveStore.Flush();
            Reload();
            Assert.AreEqual((int)StageId.Residence, SaveStore.GetInt("AFTERSIGNAL.Unity.Stage"));
            Assert.AreEqual(31.5f, SaveStore.GetFloat("AFTERSIGNAL.Unity.Life.Hours"));
            Assert.AreEqual("서하", SaveStore.GetString("AFTERSIGNAL.Unity.Test"));
            Assert.IsTrue(SaveStore.Summary(TestSlot).exists);
        }

        [Test]
        public void CorruptSlotFallsBackToTheBackup()
        {
            SaveStore.SetInt("AFTERSIGNAL.Unity.Life.Credits", 1200); SaveStore.Flush();
            SaveStore.SetInt("AFTERSIGNAL.Unity.Life.Credits", 3400); SaveStore.Flush(); // previous file becomes .bak
            File.WriteAllText(Path.Combine(directory, $"slot{TestSlot + 1}.json"), "{ not json");
            Reload();
            Assert.AreEqual(1200, SaveStore.GetInt("AFTERSIGNAL.Unity.Life.Credits"));
            Assert.IsNotNull(SaveStore.LastLoadWarning);
            Assert.IsTrue(File.Exists(Path.Combine(directory, $"slot{TestSlot + 1}.json.corrupt")));
        }

        [Test]
        public void NewGameResetRemovesEveryProgressKey()
        {
            SaveStore.SetInt("AFTERSIGNAL.Unity.FourCities.Chapter", 12);
            SaveStore.SetString("AFTERSIGNAL.Unity.Sports.League", "{}");
            SaveStore.ResetActiveSlot();
            Reload();
            Assert.IsFalse(SaveStore.HasKey("AFTERSIGNAL.Unity.FourCities.Chapter"));
            Assert.IsFalse(SaveStore.HasKey("AFTERSIGNAL.Unity.Sports.League"));
        }

        [Test]
        public void SuppressedSavesNeverReachDisk()
        {
            LifeState.SuppressSave = true;
            try { SaveStore.SetInt("AFTERSIGNAL.Unity.Stage", 3); SaveStore.Flush(); }
            finally { LifeState.SuppressSave = false; }
            Assert.IsFalse(File.Exists(Path.Combine(directory, $"slot{TestSlot + 1}.json")));
        }

        [Test]
        public void ArmoryPurchasesSurviveItemListChanges()
        {
            // An older save with fewer items: shorter arrays must not wipe the purchase of item 3 (slot 0).
            string legacy = "{\"owned\":15,\"equipped\":[3,1,2,-1,-1,-1,-1],\"rounds\":[0,0,0,0,0],\"reserve\":[0]}";
            var migrated = JsonUtility.FromJson<MigratedState>(ArmoryInventory.MigrateJson(legacy));
            Assert.AreEqual(ArmoryInventory.Items.Length, migrated.rounds.Length);
            Assert.AreEqual(ArmoryInventory.Items.Length, migrated.reserve.Length);
            Assert.AreNotEqual(0, migrated.owned & (1 << 3));
            Assert.AreEqual(3, migrated.equipped[0]);
        }
        [System.Serializable] sealed class MigratedState { public int owned; public int[] equipped, rounds, reserve; }

        [Test]
        public void EveryControlIsAnActionWithAKeyboardBinding()
        {
            var asset = Resources.Load<InputActionAsset>("Controls");
            Assert.NotNull(asset);
            foreach (var name in new[] { "Move", "Point", "Attack", "Grapple", "Jump", "Interact", "Dash", "Skill", "Guard", "Pause", "Katana", "Greatsword", "Pistol", "Weapon4", "Weapon5", "Weapon6", "Weapon7", "Look", "LookStick", "Zoom", "Boost", "Ascend", "Descend", "VehicleView", "Horn", "SummonBike", "SecondaryFire", "Passenger", "Surrender", "CameraReset", "Journal", "Reload", "Map", "Exit", "WeaponNext", "RescueReport", "Panorama" })
            {
                var action = asset.FindAction(name);
                Assert.NotNull(action, name);
                if (name == "Point" || name == "LookStick" || name == "WeaponNext") continue;
                bool keyboard = false;
                foreach (var binding in action.bindings) keyboard |= binding.groups != null && binding.groups.Contains(KeyHints.KeyboardGroup);
                Assert.IsTrue(keyboard, name + " has no keyboard/mouse binding to rebind");
            }
            foreach (var name in new[] { "Move", "Attack", "Grapple", "Jump", "Interact", "Dash", "Skill", "Guard", "Pause", "Surrender", "CameraReset", "Journal", "Reload", "Map", "Exit", "WeaponNext" })
            {
                bool pad = false;
                foreach (var binding in asset.FindAction(name).bindings) pad |= binding.groups != null && binding.groups.Contains(KeyHints.GamepadGroup);
                Assert.IsTrue(pad, name + " has no gamepad binding");
            }
        }

        [Test]
        public void EveryActionAndBindingHasAUniqueId()
        {
            var asset = Resources.Load<InputActionAsset>("Controls");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var map in asset.actionMaps)
            {
                foreach (var action in map.actions) Assert.IsTrue(ids.Add(action.id.ToString()), "Duplicate id on action " + action.name);
                foreach (var binding in map.bindings) Assert.IsTrue(ids.Add(binding.id.ToString()), "Duplicate id on binding " + binding.action + " " + binding.path);
            }
        }

        static int KeyboardIndex(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
                if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite && action.bindings[i].groups != null && action.bindings[i].groups.Contains(KeyHints.KeyboardGroup)) return i;
            return -1;
        }

        [Test]
        public void RebindingARowMovesBothActionsAndReportsSameKeyConflicts()
        {
            var asset = Object.Instantiate(Resources.Load<InputActionAsset>("Controls"));
            try
            {
                var jump = asset.FindAction("Jump"); var ascend = asset.FindAction("Ascend"); var interact = asset.FindAction("Interact");
                jump.ApplyBindingOverride(KeyboardIndex(jump), "<Keyboard>/k");
                SettingsPanel.SyncPaired(asset, "Jump");
                Assert.AreEqual("<Keyboard>/k", ascend.bindings[KeyboardIndex(ascend)].effectivePath, "'점프 · 상승' also moves Ascend");
                Assert.IsNull(SettingsPanel.Conflict(asset, "Jump"), "The paired action is not a conflict");
                interact.ApplyBindingOverride(KeyboardIndex(interact), "<Keyboard>/k");
                Assert.AreEqual("점프 · 상승", SettingsPanel.Conflict(asset, "Interact"));
                interact.ApplyBindingOverride(KeyboardIndex(interact), "<Keyboard>/w");
                Assert.AreEqual("이동", SettingsPanel.Conflict(asset, "Interact"));
                jump.ApplyBindingOverride(KeyboardIndex(jump), "<Keyboard>/space");
                SettingsPanel.SyncPaired(asset, "Jump");
                Assert.AreEqual("<Keyboard>/space", ascend.bindings[KeyboardIndex(ascend)].effectivePath);
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [Test]
        public void WeaponCycleWalksEveryOwnedArmorySlot()
        {
            // Fresh slot: only the katana, greatsword and pistol (slots 1–3) are owned.
            Assert.AreEqual(1, PlayerEquipment.NextOwnedSlot(0, 1));
            Assert.AreEqual(0, PlayerEquipment.NextOwnedSlot(2, 1), "Empty slots 4–7 are skipped");
            Assert.AreEqual(2, PlayerEquipment.NextOwnedSlot(0, -1));
            ArmoryInventory.Issue(12); // S12 shotgun → slot 7
            Assert.AreEqual(6, PlayerEquipment.NextOwnedSlot(2, 1));
            Assert.AreEqual(0, PlayerEquipment.NextOwnedSlot(6, 1));
            Assert.AreEqual(6, PlayerEquipment.NextOwnedSlot(0, -1));
        }

        [Test]
        public void KeyHintsFollowRebindsButLeavePricesAlone()
        {
            var asset = Object.Instantiate(Resources.Load<InputActionAsset>("Controls"));
            try
            {
                KeyHints.Attach(asset);
                Assert.AreEqual("E 상호작용 · 120 C", KeyHints.Apply("E 상호작용 · 120 C", false), "Default bindings keep authored text");
                var interact = asset.FindAction("Interact");
                int index = -1;
                for (int i = 0; i < interact.bindings.Count; i++) if (interact.bindings[i].groups.Contains(KeyHints.KeyboardGroup)) { index = i; break; }
                interact.ApplyBindingOverride(index, "<Keyboard>/k");
                KeyHints.Refresh();
                Assert.AreEqual("K 상호작용 · 120 C", KeyHints.Apply("E 상호작용 · 120 C", false));
                Assert.AreEqual("CORE EXPOSED", KeyHints.Apply("CORE EXPOSED", false));
                Assert.AreEqual("1–7 무기 · K 상호작용", KeyHints.Apply("1–7 무기 · E 상호작용", false), "Default weapon keys keep the range");
                var katana = asset.FindAction("Katana");
                for (int i = 0; i < katana.bindings.Count; i++) if (katana.bindings[i].groups.Contains(KeyHints.KeyboardGroup)) { katana.ApplyBindingOverride(i, "<Keyboard>/z"); break; }
                KeyHints.Refresh();
                Assert.AreEqual("Z/2/3/4/5/6/7 무기", KeyHints.Apply("1–7 무기", false), "A rebound weapon key shows in the range");
            }
            finally { KeyHints.Detach(asset); Object.DestroyImmediate(asset); }
        }

        [Test]
        public void GamepadHintsShowPadButtonsAndNeverKeyboardKeys()
        {
            var asset = Object.Instantiate(Resources.Load<InputActionAsset>("Controls"));
            try
            {
                KeyHints.Attach(asset);
                Assert.AreEqual("[X] 상호작용 · 120 C", KeyHints.Apply("E 상호작용 · 120 C", true));
                Assert.AreEqual("대화 계속  [X]", KeyHints.Apply("대화 계속  [ E ]", true), "No doubled brackets");
                Assert.AreEqual("[Y] 길게 투항 · [RS 누름] 시점 초기화", KeyHints.Apply("H 길게 투항 · HOME 시점 초기화", true));
                Assert.AreEqual("[Y] 돌아가기", KeyHints.Apply("P 돌아가기", true), "The rooftop view is left with Exit on a pad");
                Assert.AreEqual("— 구조 신고", KeyHints.Apply("F6 구조 신고", true), "No pad binding: no keyboard key either");
                Assert.AreEqual("[→] 무기 · [RT] 발사", KeyHints.Apply("1–7 무기 · LMB 발사", true), "Pads cycle weapons with WeaponNext");
                Assert.AreEqual("대화 계속  [ E ]", KeyHints.Apply("대화 계속  [ E ]", false));
            }
            finally { KeyHints.Detach(asset); Object.DestroyImmediate(asset); }
        }
    }
}
