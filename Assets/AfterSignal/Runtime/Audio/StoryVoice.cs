using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    // Optional, pre-generated dialogue. Text remains the authority when a take is absent.
    [DisallowMultipleComponent]
    public sealed class StoryVoice : MonoBehaviour
    {
        public const string ResourceRoot = "Audio/Generated/Voice/";
        public const string CatalogPath = ResourceRoot + "catalog";
        [Serializable] sealed class Catalog { public Line[] lines; }
        [Serializable] sealed class Line { public string speaker, text, clip; }
        readonly Dictionary<(string speaker, string text), string> lines = new();
        readonly Dictionary<string, AudioClip> clips = new(StringComparer.Ordinal);
        static StoryVoice active;
        GameDirector owner;
        AudioSource source;
        bool paused;
        public AudioSource CurrentSource => source;
        public static bool IsSpeaking => active && active.isActiveAndEnabled && active.source &&
            active.source.isPlaying && !active.source.mute && active.source.volume > 0 &&
            !active.ShouldPause && active.MasterVolume > 0;
        bool ShouldPause => owner && (owner.Paused || owner.Dead || owner.Audio && owner.Audio.IsPaused);
        float MasterVolume => owner && owner.Audio ? owner.Audio.Volume : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetActive() { active = null; }

        public static StoryVoice Create(GameDirector owner, TextAsset catalog)
        {
            if (!owner || !catalog) return null;
            Catalog data;
            try { data = JsonUtility.FromJson<Catalog>(catalog.text); }
            catch (ArgumentException) { Debug.LogWarning("Ignored invalid optional story voice catalog."); return null; }
            if (data?.lines == null) return null;
            var valid = new Dictionary<(string speaker, string text), string>();
            foreach (var line in data.lines)
            {
                if (line == null || string.IsNullOrEmpty(line.speaker) || string.IsNullOrEmpty(line.text) || !ValidClipName(line.clip)) continue;
                var key = (line.speaker, line.text);
                // Ambiguous takes must not select a voice based on catalog ordering.
                if (valid.ContainsKey(key)) valid[key] = null;
                else valid.Add(key, line.clip);
            }
            bool hasLine = false;
            foreach (var clip in valid.Values) if (clip != null) { hasLine = true; break; }
            if (!hasLine) return null;
            var result = owner.GetComponent<StoryVoice>();
            if (result) return result;
            result = owner.gameObject.AddComponent<StoryVoice>();
            result.owner = owner;
            foreach (var line in valid) if (line.Value != null) result.lines.Add(line.Key, line.Value);
            return result;
        }

        static bool ValidClipName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            // Only file stems within this catalog's Resources folder are accepted.
            foreach (char c in value)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '-')) return false;
            return true;
        }

        public bool PlayLine(string speaker, string text)
        {
            StopSpeech();
            if (!owner || !owner.Audio || !isActiveAndEnabled || !lines.TryGetValue((speaker, text), out var name)) return false;
            if (!clips.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>(ResourceRoot + name);
                clips.Add(name, clip);
            }
            if (!clip) return false;
            if (active && active != this) active.StopSpeech();
            active = this;
            if (!source)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0;
                source.dopplerLevel = 0;
                source.priority = 16;
                source.pitch = 1;
            }
            source.clip = clip;
            source.volume = MasterVolume;
            StartCoroutine(PlayWhenReady(clip));
            return true;
        }

        IEnumerator PlayWhenReady(AudioClip clip)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            while (clip.loadState == AudioDataLoadState.Loading || ShouldPause) yield return null;
            if (!owner || !owner.Audio || clip.loadState != AudioDataLoadState.Loaded) { StopSpeech(); yield break; }
            source.volume = MasterVolume;
            source.Play();
        }

        public void StopSpeech()
        {
            StopAllCoroutines();
            if (source) { source.Stop(); source.clip = null; }
            paused = false;
            if (active == this) active = null;
        }

        void Update()
        {
            if (!source || !source.clip) return;
            if (!owner || !owner.Audio) { StopSpeech(); return; }
            source.volume = MasterVolume;
            bool nextPause = ShouldPause;
            if (paused != nextPause)
            {
                paused = nextPause;
                if (paused) source.Pause(); else source.UnPause();
            }
        }
        void OnDisable() { StopSpeech(); }
        void OnDestroy() { StopSpeech(); if (source) Destroy(source); }
    }
}
