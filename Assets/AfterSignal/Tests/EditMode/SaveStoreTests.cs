using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace AfterSignal.Tests
{
    /// <summary>Slot recovery, slot details, legacy import bookkeeping and per-slot life state, all without a scene.</summary>
    public sealed class SaveStoreTests
    {
        const int TestSlot = 2, OtherSlot = 1; // slot 1 (index 0) would import the developer's legacy PlayerPrefs
        string directory;
        int previousSlot;

        [SetUp]
        public void SetUp()
        {
            previousSlot = SaveStore.ActiveSlot;
            directory = Path.Combine(Path.GetTempPath(), "aftersignal-savestore-tests-" + Guid.NewGuid().ToString("N"));
            SaveStore.Directory = directory;
            SaveStore.ActiveSlot = TestSlot;
            LifeState.SuppressSave = false;
        }

        [TearDown]
        public void TearDown()
        {
            LifeState.SuppressSave = false;
            SaveStore.Directory = null;
            SaveStore.ActiveSlot = previousSlot;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        static void Reload() => SaveStore.Directory = SaveStore.Directory;
        string SlotPath(int slot) => Path.Combine(directory, $"slot{slot + 1}.json");

        [Test]
        public void SwitchingTheFolderWritesPendingChangesFirst()
        {
            SaveStore.SetInt(LegacyKeys.Credits, 777);
            Reload();
            Assert.AreEqual(777, SaveStore.GetInt(LegacyKeys.Credits));
        }

        [Test]
        public void MissingSlotFileFallsBackToTheBackup()
        {
            SaveStore.SetInt(LegacyKeys.Stage, (int)StageId.Residence);
            SaveStore.SetInt(LegacyKeys.Credits, 1200); SaveStore.Flush();
            SaveStore.SetInt(LegacyKeys.Credits, 3400); SaveStore.Flush(); // previous file becomes .bak
            File.Delete(SlotPath(TestSlot)); // a replace interrupted after its rename leaves only the .bak
            Reload();
            Assert.IsTrue(SaveStore.HasKey(LegacyKeys.Stage), "the backup is loaded, so Continue stays available");
            Assert.AreEqual(1200, SaveStore.GetInt(LegacyKeys.Credits));
            Assert.IsNotNull(SaveStore.LastLoadWarning);
            Assert.IsFalse(File.Exists(SlotPath(TestSlot) + ".corrupt"), "a missing file is not kept as corrupt");
            Assert.IsTrue(SaveStore.Flush());
            Assert.IsTrue(File.Exists(SlotPath(TestSlot)), "the restored copy is written back as the main file");
        }

        [Test]
        public void InterruptedReplacePrefersTheNewerTemporaryFile()
        {
            SaveStore.SetInt(LegacyKeys.Credits, 3400); SaveStore.Flush();
            Thread.Sleep(40); // distinct save times
            SaveStore.SetInt(LegacyKeys.Credits, 5600); SaveStore.Flush();
            // The old main file was already renamed to .bak, but the new .tmp never moved into place.
            File.Move(SlotPath(TestSlot), SlotPath(TestSlot) + ".tmp");
            Reload();
            Assert.AreEqual(5600, SaveStore.GetInt(LegacyKeys.Credits));
        }

        [Test]
        public void LoadedSlotShowsWhenItWasLastSaved()
        {
            SaveStore.SetInt(LegacyKeys.Stage, (int)StageId.Residence);
            Assert.IsTrue(SaveStore.Flush());
            SaveStore.ActiveSlot = OtherSlot;
            var onDisk = SaveStore.Summary(TestSlot).savedAt; // not loaded: read from the file
            Thread.Sleep(40);
            SaveStore.ActiveSlot = TestSlot;
            Assert.IsTrue(SaveStore.HasKey(LegacyKeys.Stage));
            Assert.AreEqual(onDisk, SaveStore.Summary(TestSlot).savedAt, "the loaded slot reports its file time, not the current time");
            SaveStore.SetInt(LegacyKeys.Credits, 10);
            Assert.That((DateTime.Now - SaveStore.Summary(TestSlot).savedAt).TotalSeconds, Is.LessThan(5), "changes not yet written read as now");
        }

        [Test]
        public void SuppressedLegacyImportIsRetriedAndOnlyThenMarkedDone()
        {
            SaveStore.ActiveSlot = 0; // slot 1 imports legacy PlayerPrefs progress, here into the temporary folder
            bool injected = !PlayerPrefs.HasKey(LegacyKeys.Stage);
            if (injected) PlayerPrefs.SetInt(LegacyKeys.Stage, (int)StageId.Residence);
            try
            {
                LifeState.SuppressSave = true;
                try { Assert.IsTrue(SaveStore.HasKey(LegacyKeys.Stage), "legacy progress is readable while saving is suppressed"); }
                finally { LifeState.SuppressSave = false; }
                Assert.IsFalse(File.Exists(SlotPath(0)));
                Reload();
                Assert.IsTrue(SaveStore.HasKey(LegacyKeys.Stage), "a suppressed import does not count as done");
                Assert.IsTrue(File.Exists(SlotPath(0)));
                SaveStore.DeleteSlot(0);
                Reload();
                Assert.IsFalse(SaveStore.HasKey(LegacyKeys.Stage), "once slot 1 was written the import is not repeated");
            }
            finally { if (injected) PlayerPrefs.DeleteKey(LegacyKeys.Stage); }
        }

        [Test]
        public void NewGameKeepsTheCampaignSerialIncreasing()
        {
            SaveStore.SetInt("AFTERSIGNAL.Unity.Life.CampaignSerial", 4);
            int serial = LifeState.CampaignSerial;
            SaveStore.ResetActiveSlot();
            LifeState.Reset(serial);
            Assert.AreEqual(5, LifeState.CampaignSerial);
        }

        [Test]
        public void LoadingAnotherSlotDropsTheErrandAndSessionState()
        {
            SaveStore.SetString("AFTERSIGNAL.Unity.Life.Errand", JsonUtility.ToJson(new HiddenErrand { title = "옥상 배달", kind = "rooftop", accepted = true }));
            LifeState.Load();
            Assert.IsNotNull(LifeState.Errand);
            LifeState.LastWake = LifeState.Hours;
            SaveStore.ActiveSlot = OtherSlot; // an empty slot without an errand
            LifeState.Load();
            Assert.IsNull(LifeState.Errand);
            Assert.AreEqual(-999f, LifeState.LastWake);
        }
    }
}
