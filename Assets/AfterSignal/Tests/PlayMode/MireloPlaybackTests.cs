using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AfterSignal.Tests
{
    public class MireloPlaybackTests
    {
        const string Evidence = "Artifacts/AudioProduction/Mirelo/Playback";
        static readonly string[] PreferenceKeys = { "AFTERSIGNAL.Unity.Volume", "AFTERSIGNAL.Unity.SfxVolume", "AFTERSIGNAL.Unity.MusicVolume" };
        readonly bool[] had = new bool[3]; readonly float[] values = new float[3];
        bool initialized, save, chronicleSave, skip, hadSite;
        int site; GameDirector game;
        [Serializable] class Measurement { public string clip; public float peak; }
        [Serializable] class Report { public List<Measurement> clips = new(); }

        [UnitySetUp] public IEnumerator Setup()
        {
            if (Resources.LoadAll<AudioClip>(GeneratedSoundBank.ResourcePath).Length == 0)
                Assert.Ignore("Optional private Mirelo pack is not installed.");
            initialized = true;
            save = LifeState.SuppressSave; LifeState.SuppressSave = true;
            chronicleSave = CityChronicle.SuppressSave; CityChronicle.SuppressSave = true;
            skip = GameDirector.SkipTitle; GameDirector.SkipTitle = true;
            hadSite = PlayerPrefs.HasKey(UrbanCatalog.Prefix + "Site"); site = UrbanCatalog.Current;
            for (int i = 0; i < 3; i++) { had[i] = PlayerPrefs.HasKey(PreferenceKeys[i]); values[i] = PlayerPrefs.GetFloat(PreferenceKeys[i]); }
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Residence));
            yield return null;
            game = GameDirector.Instance;
            game.Input.ExternalControl = true; game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(false); game.CloseDialogue();
            game.Audio.SetVolume(.45f); game.Audio.SetSfxVolume(.9f); SignalMusic.Instance.SetMusicVolume(0);
            StopSources();
            yield return new WaitForSecondsRealtime(.2f);
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if (!initialized) yield break;
            Time.timeScale = 1;
            var empty = SceneManager.CreateScene("Mirelo playback cleanup"); SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty) yield return SceneManager.UnloadSceneAsync(scene);
            }
            if (SignalMusic.Instance) Object.Destroy(SignalMusic.Instance.gameObject);
            yield return null;
            LifeState.SuppressSave = save; CityChronicle.SuppressSave = chronicleSave; GameDirector.SkipTitle = skip;
            for (int i = 0; i < 3; i++) { if (had[i]) PlayerPrefs.SetFloat(PreferenceKeys[i], values[i]); else PlayerPrefs.DeleteKey(PreferenceKeys[i]); }
            if (hadSite) PlayerPrefs.SetInt(UrbanCatalog.Prefix + "Site", site); else PlayerPrefs.DeleteKey(UrbanCatalog.Prefix + "Site");
            PlayerPrefs.Save(); initialized = false;
        }
        void StopSources() { foreach (var source in game.GetComponents<AudioSource>()) source.Stop(); }
        static float Peak(AudioSource source)
        {
            var data = new float[512]; source.GetOutputData(data, 0); float peak = 0;
            foreach (float sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
            return peak;
        }
        static void SaveCapture(QualityAudioCapture capture, string name, bool expectSound)
        {
            string folder = Path.GetFullPath(Path.Combine(Evidence, name)); Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "game-audio.wav");
            if (File.Exists(path)) File.Delete(path);
            capture.Save(folder); Assert.IsTrue(File.Exists(path), "Listener PCM was not captured.");
            using var reader = new BinaryReader(File.OpenRead(path));
            reader.BaseStream.Position = 40; int bytes = reader.ReadInt32();
            Assert.Greater(bytes, 4096); double peak = 0;
            for (int i = 0; i < bytes / 2; i++) peak = Math.Max(peak, Math.Abs(reader.ReadInt16() / 32768.0));
            if (expectSound) Assert.Greater(peak, .0001, name); else Assert.Less(peak, .0001, name);
            Object.Destroy(capture);
        }

        [UnityTest] public IEnumerator EveryInstalledOneShotVariantReachesGameAudioDsp()
        {
            var clips = Resources.LoadAll<AudioClip>(GeneratedSoundBank.ResourcePath);
            Assert.AreEqual(104, clips.Length);
            var report = new Report();
            var capture = Camera.main.gameObject.AddComponent<QualityAudioCapture>();
            foreach (string cue in clips.Select(c => c.name.Substring(0, c.name.Length - 2)).Distinct().OrderBy(s => s))
            {
                var seen = new HashSet<string>();
                for (int variant = 0; variant < 2; variant++)
                {
                    StopSources(); yield return new WaitForSecondsRealtime(.07f);
                    game.Audio.Play(cue, game.Player.Shoulder, .3f, 3);
                    var source = game.GetComponents<AudioSource>().FirstOrDefault(s => s.isPlaying && s.clip && s.clip.name == game.Audio.LastClip);
                    Assert.IsNotNull(source, cue);
                    Assert.AreSame(Resources.Load<AudioClip>(GeneratedSoundBank.ResourcePath + "/" + source.clip.name), source.clip, cue);
                    seen.Add(source.clip.name);
                    float peak = 0, deadline = Time.realtimeSinceStartup + source.clip.length + .3f;
                    while (peak <= .00001f && Time.realtimeSinceStartup < deadline)
                    {
                        peak = Mathf.Max(peak, Peak(source)); yield return null;
                    }
                    Assert.Greater(peak, .00001f, source.clip.name + " did not reach DSP");
                    report.clips.Add(new Measurement { clip = source.clip.name, peak = peak });
                    yield return new WaitForSecondsRealtime(.12f);
                }
                CollectionAssert.AreEquivalent(new[] { cue + "_0", cue + "_1" }, seen);
            }
            Assert.AreEqual(0, game.Audio.MissingCues);
            SaveCapture(capture, "all-sfx", true);
            File.WriteAllText(Path.Combine(Evidence, "sfx-dsp.json"), JsonUtility.ToJson(report, true));
        }

        [UnityTest] public IEnumerator EveryLoopWrapsWithAudibleOutputAndAmbientUsesReplacement()
        {
            var ambient = game.GetComponents<AudioSource>().Single(s => s.loop && s.clip && s.clip.name == "ambient_town");
            Assert.AreSame(GeneratedLoops.Load("ambient_town"), ambient.clip);
            var clips = Resources.LoadAll<AudioClip>(GeneratedLoops.ResourcePath);
            Assert.AreEqual(19, clips.Length);
            var loopObject = new GameObject("Mirelo loop playback");
            loopObject.transform.SetParent(Camera.main.transform, false);
            var source = loopObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = true; source.volume = .2f; source.spatialBlend = 0;
            var report = new Report(); var capture = Camera.main.gameObject.AddComponent<QualityAudioCapture>();
            foreach (var clip in clips.OrderBy(c => c.name))
            {
                source.Stop(); source.clip = GeneratedLoops.Load(clip.name); source.Play(); source.time = clip.length - .15f;
                bool wrapped = false; float peak = 0, deadline = Time.realtimeSinceStartup + .8f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    if (source.time < .5f) wrapped = true;
                    if (wrapped) peak = Mathf.Max(peak, Peak(source));
                    yield return null;
                }
                Assert.IsTrue(source.isPlaying, clip.name); Assert.IsTrue(wrapped, clip.name + " did not wrap");
                Assert.Greater(peak, .00001f, clip.name + " silent after wrap");
                report.clips.Add(new Measurement { clip = clip.name, peak = peak });
            }
            source.Stop(); Object.Destroy(loopObject);
            SaveCapture(capture, "loop-boundaries", true);
            File.WriteAllText(Path.Combine(Evidence, "loops-dsp.json"), JsonUtility.ToJson(report, true));
        }

        [UnityTest] public IEnumerator GeneratedSfxRespectPauseVolumeAndLegacyPistolAliases()
        {
            game.Audio.Play("pistol_overdrive", game.Player.Shoulder, .3f, 3);
            StringAssert.StartsWith("gun_pistol_", game.Audio.LastClip);
            StopSources(); yield return new WaitForSecondsRealtime(.1f);
            game.Audio.Play("cannon", game.Player.Shoulder, .3f, 3);
            var source = game.GetComponents<AudioSource>().First(s => s.isPlaying && s.clip && s.clip.name == game.Audio.LastClip);
            yield return new WaitForSecondsRealtime(.1f);
            game.SetPaused(true); yield return new WaitForSecondsRealtime(.1f);
            float position = source.time; yield return new WaitForSecondsRealtime(.15f);
            Assert.That(source.time, Is.EqualTo(position).Within(.03f));
            int played = game.Audio.Played; game.Audio.Play("blade_hit", game.Player.Shoulder);
            Assert.AreEqual(played, game.Audio.Played);
            game.SetPaused(false); yield return new WaitForSecondsRealtime(.1f);
            Assert.Greater(source.time, position);
            game.Audio.SetSfxVolume(0); yield return new WaitForSecondsRealtime(.2f);
            var capture = Camera.main.gameObject.AddComponent<QualityAudioCapture>();
            game.Audio.Play("blade_hit", game.Player.Shoulder); Assert.AreEqual(played, game.Audio.Played);
            yield return new WaitForSecondsRealtime(.3f); SaveCapture(capture, "sfx-muted", false);
            game.Audio.SetSfxVolume(.9f); game.Audio.SetVolume(0);
            yield return new WaitForSecondsRealtime(.2f);
            capture = Camera.main.gameObject.AddComponent<QualityAudioCapture>();
            game.Audio.Play("blade_hit", game.Player.Shoulder); Assert.AreEqual(played, game.Audio.Played);
            yield return new WaitForSecondsRealtime(.3f); SaveCapture(capture, "master-muted", false);
        }
    }
}
