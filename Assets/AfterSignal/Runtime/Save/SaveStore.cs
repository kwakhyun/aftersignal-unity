using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AfterSignal
{
    /// <summary>
    /// Versioned save slots for story progress. Replaces scattered PlayerPrefs keys: each slot is one JSON file in
    /// persistentDataPath/saves, written atomically with a .bak copy of the previous good file, and throttled so that
    /// frequent Save() calls (money, ammo, quest steps) cost at most one disk write every couple of seconds.
    /// Settings (volume, graphics, bindings…) stay in PlayerPrefs because they belong to the machine, not the story.
    /// The API mirrors PlayerPrefs so call sites read the same.
    /// </summary>
    public static class SaveStore
    {
        public const int Version = 1, SlotCount = 3;
        const string ActiveSlotKey = "AFTERSIGNAL.Unity.ActiveSlot", MigratedKey = "AFTERSIGNAL.Unity.SaveMigrated";
        const float FlushInterval = 2f;

        [Serializable] sealed class Entry { public string key; public int type; public int i; public float f; public string s; }
        [Serializable] sealed class SlotFile { public int version; public long savedAt; public List<Entry> entries = new(); }
        public struct SlotSummary { public bool exists; public DateTime savedAt; public int stage; public float hours; public int credits; public int mainDone; }

        static readonly Dictionary<string, Entry> values = new();
        static int loadedSlot = -1;
        static bool dirty;
        static float lastFlush = -999;
        static string directoryOverride;

        /// <summary>Directory for slot files (tests may redirect it).</summary>
        public static string Directory { get => directoryOverride ?? Path.Combine(Application.persistentDataPath, "saves"); set { directoryOverride = value; loadedSlot = -1; values.Clear(); } }
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
            values.Clear(); dirty = false; LastLoadWarning = null;
            loadedSlot = ActiveSlot;
            if (!TryRead(PathFor(loadedSlot), out var file) && File.Exists(PathFor(loadedSlot)))
            {
                // Keep the unreadable file for inspection and fall back to the last good copy.
                try { File.Copy(PathFor(loadedSlot), PathFor(loadedSlot) + ".corrupt", true); } catch (Exception) { }
                if (TryRead(PathFor(loadedSlot) + ".bak", out file)) LastLoadWarning = "저장 파일이 손상되어 직전 백업에서 복원했습니다.";
                else LastLoadWarning = "저장 파일을 읽을 수 없어 새 기록으로 시작합니다. 손상된 파일은 .corrupt로 보관했습니다.";
                if (file != null) dirty = true;
            }
            if (file != null) foreach (var e in file.entries) if (!string.IsNullOrEmpty(e.key)) values[e.key] = e;
            if (file == null && loadedSlot == 0 && PlayerPrefs.GetInt(MigratedKey, 0) == 0) ImportLegacy();
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

        /// <summary>Hook for future format changes: migrate older slot files step by step.</summary>
        static SlotFile Upgrade(SlotFile file) { file.version = Version; return file; }

        /// <summary>One-time import of the pre-slot PlayerPrefs progress into slot 1 (legacy keys are left untouched).</summary>
        static void ImportLegacy()
        {
            PlayerPrefs.SetInt(MigratedKey, 1);
            int imported = 0;
            foreach (var key in LegacyKeys.Progress)
            {
                if (!PlayerPrefs.HasKey(key)) continue;
                // PlayerPrefs has no type query: reading a key as the wrong type returns the fallback sentinel.
                const string none = "\u0001";
                string text = PlayerPrefs.GetString(key, none);
                if (text != none) Put(key, 2, 0, 0, text);
                else if (PlayerPrefs.GetInt(key, int.MinValue) != int.MinValue) Put(key, 0, PlayerPrefs.GetInt(key, 0), 0, null);
                else Put(key, 1, 0, PlayerPrefs.GetFloat(key, 0), null);
                imported++;
            }
            if (imported > 0) { dirty = true; Flush(); Debug.Log($"AFTERSIGNAL: imported {imported} legacy progress keys into save slot 1"); }
        }

        static void Put(string key, int type, int i, float f, string s) { values[key] = new Entry { key = key, type = type, i = i, f = f, s = s }; dirty = true; Runner.Wake(); }

        public static bool HasKey(string key) { Ensure(); return values.ContainsKey(key); }
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

        /// <summary>Writes the active slot now if anything changed. Skipped while diagnostics suppress saving.</summary>
        public static void Flush()
        {
            if (!dirty || loadedSlot < 0) return;
            if (LifeState.SuppressSave) { dirty = false; return; }
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                var file = new SlotFile { version = Version, savedAt = DateTime.UtcNow.Ticks, entries = new List<Entry>(values.Values) };
                string path = PathFor(loadedSlot), temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(file));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                dirty = false; lastFlush = Time.realtimeSinceStartup;
            }
            catch (Exception error) { Debug.LogError("AFTERSIGNAL save write failed · " + error.Message); }
        }

        /// <summary>Clears the active slot completely (New Game). Every progress key goes, including ones added later.</summary>
        public static void ResetActiveSlot()
        {
            Ensure();
            values.Clear(); dirty = true; Flush();
        }

        public static bool SlotExists(int slot) => File.Exists(PathFor(slot)) || File.Exists(PathFor(slot) + ".bak");

        public static SlotSummary Summary(int slot)
        {
            var summary = new SlotSummary();
            if (slot == loadedSlot && values.Count > 0) { summary.exists = values.ContainsKey(LegacyKeys.Stage); summary.savedAt = DateTime.Now; }
            else if (TryRead(PathFor(slot), out var file) || TryRead(PathFor(slot) + ".bak", out file))
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
            if (slot == loadedSlot) { values.Clear(); dirty = false; loadedSlot = -1; }
        }

        /// <summary>Hidden runner that performs throttled flushes and a final flush on quit / focus loss.</summary>
        sealed class Runner : MonoBehaviour
        {
            static Runner instance;
            public static void Wake()
            {
                if (instance || !Application.isPlaying) return;
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
