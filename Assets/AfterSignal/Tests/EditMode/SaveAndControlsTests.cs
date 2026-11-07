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
            foreach (var name in new[] { "Move", "Point", "Attack", "Grapple", "Jump", "Interact", "Dash", "Skill", "Guard", "Pause", "Katana", "Greatsword", "Pistol", "Weapon4", "Weapon5", "Weapon6", "Weapon7", "Look", "LookStick", "Zoom", "Boost", "Ascend", "Descend", "VehicleView", "Horn", "SummonBike", "SecondaryFire", "Passenger", "Surrender", "CameraReset", "Journal", "Reload", "Map", "Exit", "WeaponNext", "RescueReport" })
            {
                var action = asset.FindAction(name);
                Assert.NotNull(action, name);
                if (name == "Point" || name == "LookStick" || name == "WeaponNext") continue;
                bool keyboard = false;
                foreach (var binding in action.bindings) keyboard |= binding.groups != null && binding.groups.Contains(KeyHints.KeyboardGroup);
                Assert.IsTrue(keyboard, name + " has no keyboard/mouse binding to rebind");
            }
        }

        [Test]
        public void KeyHintsFollowRebindsButLeavePricesAlone()
        {
            var asset = Object.Instantiate(Resources.Load<InputActionAsset>("Controls"));
            try
            {
                KeyHints.Attach(asset);
                Assert.AreEqual("E 상호작용 · 120 C", KeyHints.Apply("E 상호작용 · 120 C"), "Default bindings keep authored text");
                var interact = asset.FindAction("Interact");
                int index = -1;
                for (int i = 0; i < interact.bindings.Count; i++) if (interact.bindings[i].groups.Contains(KeyHints.KeyboardGroup)) { index = i; break; }
                interact.ApplyBindingOverride(index, "<Keyboard>/k");
                KeyHints.Refresh();
                Assert.AreEqual("K 상호작용 · 120 C", KeyHints.Apply("E 상호작용 · 120 C"));
                Assert.AreEqual("CORE EXPOSED", KeyHints.Apply("CORE EXPOSED"));
            }
            finally { KeyHints.Detach(asset); Object.DestroyImmediate(asset); }
        }
    }
}
