using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AfterSignal
{
    /// <summary>
    /// Versioned save slots for story progress. Replaces scattered PlayerPrefs keys: each slot is one JSON file in
    /// persistentDataPath/saves (saves-editor in the Editor, whose PlayerPrefs are separate too), written atomically with a
    /// .bak copy of the previous good file, and throttled so that frequent Save() calls (money, ammo, quest steps) cost at
    /// most one disk write every couple of seconds; writes made while the application quits go to disk at once.
    /// Settings (volume, graphics, bindings…) stay in PlayerPrefs because they belong to the machine, not the story.
    /// The API mirrors PlayerPrefs so call sites read the same.
    /// </summary>
    public static class SaveStore
    {
        public const int Version = 1, SlotCount = 3;
#if UNITY_EDITOR
        // The Editor and the built player share persistentDataPath, so the Editor keeps its own folder and active slot.
        const string Folder = "saves-editor", ActiveSlotKey = "AFTERSIGNAL.Unity.ActiveSlot.Editor";
#else
        const string Folder = "saves", ActiveSlotKey = "AFTERSIGNAL.Unity.ActiveSlot";
#endif
        const float FlushInterval = 2f;
        const string LegacyFlag = "AFTERSIGNAL.Unity.LegacyImported";
        // Written next to the slots once the legacy import is on disk, so the marker can never get out of step with the folder.
        const string MigratedFile = "legacy-import.done";

        [Serializable] sealed class Entry { public string key; public int type; public int i; public float f; public string s; }
        [Serializable] sealed class SlotFile { public int version; public long savedAt; public List<Entry> entries = new(); }
        public struct SlotSummary { public bool exists; public DateTime savedAt; public int stage; public float hours; public int credits; public int mainDone; }

        static readonly Dictionary<string, Entry> values = new();
        static int loadedSlot = -1;
        static bool dirty, quitting;
        static float lastFlush = -999;
        static long lastSavedTicks;
        static string directoryOverride;
        /// <summary>Changes whenever the active slot is switched or reset; static caches reload when it differs.</summary>
        public static int Generation { get { Ensure(); return generation; } }
        static int generation;

        /// <summary>Directory for slot files (tests and smokes redirect it). Pending changes are written to the old one first.</summary>
        public static string Directory { get => directoryOverride ?? Path.Combine(Application.persistentDataPath, Folder); set { Flush(); directoryOverride = value; loadedSlot = -1; values.Clear(); dirty = false; } }
        public static int ActiveSlot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotKey, 0), 0, SlotCount - 1);
            set { int slot = Mathf.Clamp(value, 0, SlotCount - 1); if (slot == ActiveSlot && loadedSlot == slot) return; Flush(); PlayerPrefs.SetInt(ActiveSlotKey, slot); PlayerPrefs.Save(); loadedSlot = -1; }
        }
        /// <summary>Set when the last load had to fall back to the backup or start empty because the file was unreadable.</summary>
        public static string LastLoadWarning { get; private set; }

        static string PathFor(int slot) => Path.Combine(Directory, $"slot{slot + 1}.json");

        static void Ensure()
        {
            if (loadedSlot == ActiveSlot) return;
            values.Clear(); dirty = false; LastLoadWarning = null; lastSavedTicks = 0;
            loadedSlot = ActiveSlot; generation++;
            string main = PathFor(loadedSlot);
            var file = ReadSlot(loadedSlot, out bool fallback);
#if !UNITY_EDITOR
            if (loadedSlot == 0 && directoryOverride == null) ImportBesideSlot1(file);
#endif
            if (fallback || file == null && File.Exists(main))
            {
                // Keep an unreadable file for inspection. A missing one (interrupted replace) also falls back to the last good copy.
                bool corrupt = File.Exists(main);
                if (corrupt) try { File.Copy(main, main + ".corrupt", true); } catch (Exception) { }
                LastLoadWarning = file == null ? "저장 파일을 읽을 수 없어 새 기록으로 시작합니다. 손상된 파일은 .corrupt로 보관했습니다." : corrupt ? "저장 파일이 손상되어 직전 백업에서 복원했습니다." : "저장 파일이 없어 직전 백업에서 복원했습니다.";
            }
            if (file != null) { foreach (var e in file.entries) if (!string.IsNullOrEmpty(e.key)) values[e.key] = e; lastSavedTicks = file.savedAt; }
            // Write the recovered copy back as the main file (only now that its values are loaded).
            if (fallback) { dirty = true; Runner.Wake(); }
            if (file == null && loadedSlot == 0 && !File.Exists(Path.Combine(Directory, MigratedFile))) ImportLegacy();
        }

        /// <summary>The main slot file, else the newer readable one of its .bak and a .tmp left by an interrupted write.</summary>
        static SlotFile ReadSlot(int slot, out bool fallback)
        {
            fallback = false;
            string main = PathFor(slot);
            if (TryRead(main, out var file)) return file;
            foreach (var path in new[] { main + ".bak", main + ".tmp" })
                if (TryRead(path, out var copy) && (file == null || copy.savedAt > file.savedAt)) file = copy;
            fallback = file != null;
            return file;
        }

        static bool TryRead(string path, out SlotFile file)
        {
            file = null;
            try
            {
                if (!File.Exists(path)) return false;
                var parsed = JsonUtility.FromJson<SlotFile>(File.ReadAllText(path));
                if (parsed == null || parsed.entries == null || parsed.version <= 0) return false;
                file = Upgrade(parsed);
                return true;
            }
            catch (Exception error) { Debug.LogWarning("AFTERSIGNAL save read failed · " + path + " · " + error.Message); return false; }
        }

#if !UNITY_EDITOR
        // Earlier builds kept the Editor's slots in this same folder, while on Windows the Editor and the player have separate
        // PlayerPrefs. So a slot 1 file with no import marker, no MigratedKey and this player's own legacy progress still
        // waiting may be the Editor's, or this player's own progress on a file the Editor created. Slot 1 is never touched:
        // the legacy progress goes into the first empty slot instead, so both appear in the Load list.
        const string MigratedKey = "AFTERSIGNAL.Unity.SaveMigrated";
        static void ImportBesideSlot1(SlotFile file)
        {
            if (file != null && !LifeState.SuppressSave && !PlayerPrefs.HasKey(MigratedKey) && !File.Exists(Path.Combine(Directory, MigratedFile)) && Array.Exists(LegacyKeys.Progress, PlayerPrefs.HasKey))
            {
                int target = -1;
                for (int slot = 1; slot < SlotCount && target < 0; slot++) if (ReadSlot(slot, out _) == null && !File.Exists(PathFor(slot))) target = slot;
                if (target < 0) Debug.LogWarning("AFTERSIGNAL save: earlier PlayerPrefs progress found, but slots 2 and 3 are in use; it was not imported");
                else
                {
                    var entries = LegacyEntries();
                    try
                    {
                        System.IO.Directory.CreateDirectory(Directory);
                        string path = PathFor(target);
                        File.WriteAllText(path + ".tmp", JsonUtility.ToJson(new SlotFile { version = Version, savedAt = DateTime.UtcNow.Ticks, entries = entries }));
                        File.Move(path + ".tmp", path);
                    }
                    catch (Exception error) { Debug.LogWarning("AFTERSIGNAL save: could not import earlier progress into slot " + (target + 1) + " · " + error.Message); return; }
                    WriteMigratedMarker();
                    LastLoadWarning = $"이전 버전의 진행 기록을 슬롯 {target + 1}에 가져왔습니다. 슬롯 1은 그대로 두었습니다.";
                    Debug.LogWarning($"AFTERSIGNAL save: slot 1 already existed; imported {entries.Count - 1} legacy progress keys into slot {target + 1}");
                }
            }
            if (file == null && !PlayerPrefs.HasKey(MigratedKey)) { PlayerPrefs.SetInt(MigratedKey, 1); PlayerPrefs.Save(); }
        }
#endif

        /// <summary>Hook for future format changes: migrate older slot files step by step.</summary>
        static SlotFile Upgrade(SlotFile file) { file.version = Version; return file; }

        /// <summary>
        /// One-time import of the pre-slot PlayerPrefs progress into slot 1 (legacy keys are left untouched). It only counts as
        /// done once slot 1 is on disk: while diagnostics suppress saving, or if the write fails, the next launch imports again.
        /// </summary>
        static void ImportLegacy()
        {
            var entries = LegacyEntries();
            if (entries.Count == 0) return;
            // Straight into the slot, not through Put: while quitting every Put flushes (and a suppressed flush clears dirty),
            // so the Flush below would report "nothing to write" although slot 1 was never written.
            foreach (var e in entries) values[e.key] = e;
            // dirty is set here, so true means this call wrote slot 1; a failed write is retried by the runner, not marked done.
            dirty = true;
            if (!Flush()) { Save(); return; }
            WriteMigratedMarker();
            Debug.Log($"AFTERSIGNAL: imported {entries.Count - 1} legacy progress keys into save slot 1");
        }

        /// <summary>The pre-slot PlayerPrefs progress as slot entries, plus the LegacyImported flag; empty when there is none.</summary>
        static List<Entry> LegacyEntries()
        {
            var entries = new List<Entry>();
            foreach (var key in LegacyKeys.Progress)
            {
                if (!PlayerPrefs.HasKey(key)) continue;
                // PlayerPrefs has no type query: reading a key as the wrong type returns the fallback sentinel.
                const string none = "\u0001";
                string text = PlayerPrefs.GetString(key, none);
                entries.Add(text != none ? new Entry { key = key, type = 2, s = text }
                    : PlayerPrefs.GetInt(key, int.MinValue) != int.MinValue ? new Entry { key = key, type = 0, i = PlayerPrefs.GetInt(key, 0) }
                    : new Entry { key = key, type = 1, f = PlayerPrefs.GetFloat(key, 0) });
            }
            if (entries.Count > 0) entries.Add(new Entry { key = LegacyFlag, type = 0, i = 1 });
            return entries;
        }

        static void WriteMigratedMarker()
        {
            try { File.WriteAllText(Path.Combine(Directory, MigratedFile), DateTime.UtcNow.ToString("o")); }
            catch (Exception error) { Debug.LogWarning("AFTERSIGNAL save migration marker failed · " + error.Message); }
        }

        static void Put(string key, int type, int i, float f, string s) { values[key] = new Entry { key = key, type = type, i = i, f = f, s = s }; dirty = true; Runner.Wake(); }

        public static bool HasKey(string key) { Ensure(); return values.ContainsKey(key); }
        /// <summary>
        /// For keys that cannot be listed up front (e.g. one per neighbour): read the slot value, or — only in a slot that
        /// was imported from legacy PlayerPrefs and never reset — the old PlayerPrefs value.
        /// </summary>
        public static int GetIntWithLegacy(string key, int fallback) { Ensure(); if (values.ContainsKey(key)) return GetInt(key, fallback); return values.ContainsKey(LegacyFlag) ? PlayerPrefs.GetInt(key, fallback) : fallback; }
        public static void DeleteKey(string key) { Ensure(); if (values.Remove(key)) { dirty = true; Runner.Wake(); } }
        public static int GetInt(string key, int fallback = 0) { Ensure(); return values.TryGetValue(key, out var e) ? e.type == 0 ? e.i : e.type == 1 ? (int)e.f : fallback : fallback; }
        public static float GetFloat(string key, float fallback = 0) { Ensure(); return values.TryGetValue(key, out var e) ? e.type == 1 ? e.f : e.type == 0 ? e.i : fallback : fallback; }
        public static string GetString(string key, string fallback = "") { Ensure(); return values.TryGetValue(key, out var e) && e.type == 2 ? e.s ?? "" : fallback; }
        public static void SetInt(string key, int value) { Ensure(); if (values.TryGetValue(key, out var e) && e.type == 0 && e.i == value) return; Put(key, 0, value, 0, null); }
        public static void SetFloat(string key, float value) { Ensure(); if (values.TryGetValue(key, out var e) && e.type == 1 && e.f == value) return; Put(key, 1, 0, value, null); }
        public static void SetString(string key, string value) { Ensure(); if (values.TryGetValue(key, out var e) && e.type == 2 && e.s == value) return; Put(key, 2, 0, 0, value ?? ""); }
        /// <summary>Removes every key with the prefix (bounded cleanup of per-location/per-day entries).</summary>
        public static int DeletePrefix(string prefix) { Ensure(); var doomed = new List<string>(); foreach (var k in values.Keys) if (k.StartsWith(prefix, StringComparison.Ordinal)) doomed.Add(k); foreach (var k in doomed) values.Remove(k); if (doomed.Count > 0) { dirty = true; Runner.Wake(); } return doomed.Count; }

        /// <summary>Requests a write; actual disk writes are throttled (see <see cref="Flush"/>).</summary>
        public static void Save() { if (dirty) Runner.Wake(); }

        /// <summary>
        /// Writes the active slot now if anything changed. Skipped while diagnostics suppress saving.
        /// Returns false when no slot is loaded or pending changes were suppressed or could not be written.
        /// </summary>
        public static bool Flush()
        {
            if (loadedSlot < 0) return false;
            if (!dirty) return true;
            if (LifeState.SuppressSave) { dirty = false; return false; }
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                var file = new SlotFile { version = Version, savedAt = DateTime.UtcNow.Ticks, entries = new List<Entry>(values.Values) };
                string path = PathFor(loadedSlot), temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(file));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                dirty = false; lastFlush = Time.realtimeSinceStartup; lastSavedTicks = file.savedAt;
                return true;
            }
            catch (Exception error) { Debug.LogError("AFTERSIGNAL save write failed · " + error.Message); return false; }
        }

        /// <summary>Clears the active slot completely (New Game). Every progress key goes, including ones added later.</summary>
        public static void ResetActiveSlot()
        {
            Ensure();
            values.Clear(); dirty = true; Flush(); generation++;
        }

        public static bool SlotExists(int slot) => slot == loadedSlot && values.Count > 0 || ReadSlot(slot, out _) != null;

        public static SlotSummary Summary(int slot)
        {
            var summary = new SlotSummary();
            // The loaded slot reports its last write; only changes still waiting for the throttled write read as "now".
            if (slot == loadedSlot && values.Count > 0) { summary.exists = values.ContainsKey(LegacyKeys.Stage); summary.savedAt = dirty || lastSavedTicks <= 0 ? DateTime.Now : new DateTime(lastSavedTicks, DateTimeKind.Utc).ToLocalTime(); }
            else if (ReadSlot(slot, out _) is SlotFile file)
            {
                var map = new Dictionary<string, Entry>(); foreach (var e in file.entries) if (e.key != null) map[e.key] = e;
                summary.exists = map.ContainsKey(LegacyKeys.Stage); summary.savedAt = new DateTime(file.savedAt, DateTimeKind.Utc).ToLocalTime();
                summary.stage = map.TryGetValue(LegacyKeys.Stage, out var st) ? st.i : 0;
                summary.hours = map.TryGetValue(LegacyKeys.Hours, out var h) ? h.f : 8;
                summary.credits = map.TryGetValue(LegacyKeys.Credits, out var c) ? c.i : 0;
                return summary;
            }
            else return summary;
            summary.stage = GetInt(LegacyKeys.Stage); summary.hours = GetFloat(LegacyKeys.Hours, 8); summary.credits = GetInt(LegacyKeys.Credits);
            return summary;
        }

        public static void DeleteSlot(int slot)
        {
            foreach (var suffix in new[] { "", ".bak", ".tmp" }) try { File.Delete(PathFor(slot) + suffix); } catch (Exception) { }
            if (slot == loadedSlot) { values.Clear(); dirty = false; loadedSlot = -1; lastSavedTicks = 0; }
        }

        // Play sessions without a domain reload start from the real folder again. OnApplicationQuit runs in no fixed order and
        // OnDestroy saves come after it, so once the application quits every write goes to disk at once.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { quitting = dirty = false; directoryOverride = null; loadedSlot = -1; values.Clear(); Application.quitting -= OnQuitting; Application.quitting += OnQuitting; }
        static void OnQuitting() { quitting = true; Flush(); }

        /// <summary>Hidden runner that performs throttled flushes and a final flush on quit / focus loss.</summary>
        sealed class Runner : MonoBehaviour
        {
            static Runner instance;
            public static void Wake()
            {
                if (!Application.isPlaying) return;
                if (quitting) { Flush(); return; }
                if (instance) return;
                var go = new GameObject("AFTERSIGNAL save writer") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go); instance = go.AddComponent<Runner>();
            }
            void Update() { if (dirty && Time.realtimeSinceStartup - lastFlush >= FlushInterval) Flush(); }
            void OnApplicationPause(bool paused) { if (paused) Flush(); }
            void OnApplicationFocus(bool focus) { if (!focus) Flush(); }
            void OnApplicationQuit() => Flush();
        }
    }
}
