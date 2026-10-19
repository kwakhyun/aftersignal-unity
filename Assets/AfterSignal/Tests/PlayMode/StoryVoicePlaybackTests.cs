using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AfterSignal.Tests
{
    public class StoryVoicePlaybackTests
    {
        const string Text = "기억은 사라지지 않았어. 함께 찾아보자.";
        const string Catalog = "{\"lines\":[{\"speaker\":\"서하\",\"text\":\"" + Text + "\",\"clip\":\"voice_test_01\"}," +
            "{\"speaker\":\"노아\",\"text\":\"다음 기록\",\"clip\":\"voice_test_02\"}," +
            "{\"speaker\":\"서하\",\"text\":\"누락된 녹음\",\"clip\":\"voice_missing_test_take\"}]}";
        const BindingFlags PrivateField = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<Object> assets = new();
        GameDirector game;
        CityLife life;
        StoryVoice voice;
        bool suppressed, chronicleSuppressed, hadVolume, hadSfxVolume, hadMusicVolume, hadSite, skipTitle;
        float volume, sfxVolume, musicVolume;
        int site;

        [UnitySetUp] public IEnumerator Setup()
        {
            suppressed = LifeState.SuppressSave; LifeState.SuppressSave = true;
            chronicleSuppressed = CityChronicle.SuppressSave; CityChronicle.SuppressSave = true;
            hadVolume = PlayerPrefs.HasKey("AFTERSIGNAL.Unity.Volume");
            volume = PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.Volume");
            hadSfxVolume = PlayerPrefs.HasKey("AFTERSIGNAL.Unity.SfxVolume");
            sfxVolume = PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.SfxVolume");
            hadMusicVolume = PlayerPrefs.HasKey("AFTERSIGNAL.Unity.MusicVolume");
            musicVolume = PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.MusicVolume");
            hadSite = PlayerPrefs.HasKey(UrbanCatalog.Prefix + "Site"); site = UrbanCatalog.Current;
            skipTitle = GameDirector.SkipTitle; GameDirector.SkipTitle = true;
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.Residence));
            yield return null;
            game = GameDirector.Instance;
            game.Input.ExternalControl = true; game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(false); game.CloseDialogue(); game.Audio.SetVolume(.45f);
            life = CityLife.Instance;
        }

        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale = 1;
            var empty = SceneManager.CreateScene("Story voice test cleanup");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty) yield return SceneManager.UnloadSceneAsync(scene);
            }
            if (SignalMusic.Instance) Object.Destroy(SignalMusic.Instance.gameObject);
            foreach (var asset in assets) if (asset) Object.Destroy(asset);
            assets.Clear();
            yield return null;
            LifeState.SuppressSave = suppressed; CityChronicle.SuppressSave = chronicleSuppressed; GameDirector.SkipTitle = skipTitle;
            if (hadVolume) PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.Volume", volume); else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.Volume");
            if (hadSfxVolume) PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.SfxVolume", sfxVolume); else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.SfxVolume");
            if (hadMusicVolume) PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.MusicVolume", musicVolume); else PlayerPrefs.DeleteKey("AFTERSIGNAL.Unity.MusicVolume");
            if (hadSite) PlayerPrefs.SetInt(UrbanCatalog.Prefix + "Site", site); else PlayerPrefs.DeleteKey(UrbanCatalog.Prefix + "Site");
            PlayerPrefs.Save();
        }

        TextAsset Json(string text)
        {
            var asset = new TextAsset(text); assets.Add(asset); return asset;
        }
        void BindVoice(float seconds = 3)
        {
            voice = StoryVoice.Create(game, Json(Catalog));
            Assert.IsNotNull(voice);
            // Memory-only fixtures avoid distributing test audio as game Resources.
            var cache = (Dictionary<string, AudioClip>)typeof(StoryVoice).GetField("clips", PrivateField).GetValue(voice);
            foreach (string name in new[] { "voice_test_01", "voice_test_02" })
            {
                var clip = AudioClip.Create(name, Mathf.CeilToInt(24000 * seconds), 1, 24000, false);
                var samples = new float[clip.samples];
                for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 2 * Mathf.PI * 220 / 24000) * .02f;
                clip.SetData(samples, 0); assets.Add(clip); cache.Add(name, clip);
            }
            typeof(CityLife).GetField("storyVoice", PrivateField).SetValue(life, voice);
            typeof(CityLife).GetField("voiceCatalogChecked", PrivateField).SetValue(life, true);
        }

        [Test] public void MissingEmptyOrAmbiguousCatalogDoesNotAllocatePlayback()
        {
            int sources = game.GetComponents<AudioSource>().Length;
            Assert.IsNull(StoryVoice.Create(game, null));
            Assert.IsNull(StoryVoice.Create(game, Json("{\"lines\":[]}")));
            Assert.IsNull(StoryVoice.Create(game, Json("{\"lines\":[{\"speaker\":\"서하\",\"text\":\"기록\",\"clip\":\"../Quality/ui_confirm\"}]}")));
            Assert.IsNull(StoryVoice.Create(game, Json("{\"lines\":[{\"speaker\":\"서하\",\"text\":\"기록\",\"clip\":\"first\"},{\"speaker\":\"서하\",\"text\":\"기록\",\"clip\":\"second\"}]}")));
            Assert.IsNull(game.GetComponent<StoryVoice>());
            Assert.AreEqual(sources, game.GetComponents<AudioSource>().Length);
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            Assert.AreEqual(Text, life.Body);
            Assert.AreEqual(1, life.Options.Count);
        }

        [UnityTest] public IEnumerator ExactTextAndSpeakerSelectOneSourceAndUnvoicedLinesStopTheOldTake()
        {
            int sourceCount = game.GetComponents<AudioSource>().Length;
            BindVoice();
            Assert.IsNull(voice.CurrentSource, "A valid catalog alone must not allocate a voice.");
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            yield return null;
            var source = voice.CurrentSource;
            Assert.IsTrue(StoryVoice.IsSpeaking);
            Assert.AreEqual("voice_test_01", source.clip.name);
            Assert.AreEqual(1, source.pitch); Assert.AreEqual(0, source.spatialBlend); Assert.Less(source.priority, 32);
            Assert.IsFalse(voice.PlayLine("노아", Text), "Speaker identity must match exactly.");
            Assert.IsFalse(source.isPlaying);
            Assert.IsFalse(voice.PlayLine("서하", Text + " "), "An edited subtitle must not reuse an outdated recording.");
            Assert.IsTrue(voice.PlayLine("노아", "다음 기록"));
            yield return null;
            Assert.AreSame(source, voice.CurrentSource);
            Assert.AreEqual("voice_test_02", source.clip.name);
            life.StoryDialogue("기록", "서하", "누락된 녹음", false, _ => { });
            Assert.IsFalse(source.isPlaying); Assert.IsFalse(StoryVoice.IsSpeaking);
            Assert.AreEqual("누락된 녹음", life.Body); Assert.AreEqual(1, life.Options.Count);
            Assert.AreEqual(sourceCount + 1, game.GetComponents<AudioSource>().Length);
        }

        [UnityTest] public IEnumerator PauseResumeAndMasterMuteUseTheDedicatedVoice()
        {
            BindVoice();
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            yield return new WaitForSecondsRealtime(.1f);
            var source = voice.CurrentSource;
            game.SetPaused(true);
            yield return new WaitForSecondsRealtime(.1f);
            float position = source.time;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(source.time, Is.EqualTo(position).Within(.03f));
            Assert.IsFalse(StoryVoice.IsSpeaking);
            game.SetPaused(false);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.Greater(source.time, position); Assert.IsTrue(StoryVoice.IsSpeaking);
            game.Audio.SetVolume(0);
            yield return null;
            Assert.AreEqual(0, source.volume); Assert.IsFalse(StoryVoice.IsSpeaking);
            game.Audio.SetVolume(.45f);
            yield return null;
            Assert.AreEqual(.45f, source.volume); Assert.IsTrue(StoryVoice.IsSpeaking);
            game.SetPaused(true);
            yield return null;
            life.StoryDialogue("기록", "노아", "다음 기록", false, _ => { });
            Assert.IsFalse(source.isPlaying);
            life.Close();
            game.SetPaused(false);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsFalse(source.isPlaying, "Closing while paused must cancel a pending take before resume.");
            Assert.IsNull(source.clip);
        }

        [UnityTest] public IEnumerator SpeakingAddsMusicDuckAndNaturalEndRestoresTheOpenDialogueMix()
        {
            var music = SignalMusic.Instance;
            music.SetMusicVolume(.55f);
            float deadline = Time.realtimeSinceStartup + 12;
            while (Time.realtimeSinceStartup < deadline &&
                   (music.CurrentCue != MusicCue.Home || !music.CurrentSource.clip || music.IsFading || !music.CurrentSource.isPlaying))
                yield return null;
            Assert.AreEqual(MusicCue.Home, music.CurrentCue);
            Assert.IsFalse(music.IsFading); Assert.IsTrue(music.CurrentSource.isPlaying);
            var musicSource = music.CurrentSource;
            musicSource.time = SignalMusic.LoopStart(music.CurrentCue);
            game.ShowDialogue("음성 믹스 확인", "자막은 녹음이 끝난 뒤에도 열려 있습니다.");
            yield return new WaitForSecondsRealtime(.5f);
            float dialogueVolume = musicSource.volume;
            Assert.Greater(dialogueVolume, 0);

            BindVoice(1.5f);
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            yield return new WaitForSecondsRealtime(.35f);
            Assert.IsTrue(StoryVoice.IsSpeaking);
            Assert.AreSame(musicSource, music.CurrentSource);
            Assert.That(musicSource.volume, Is.EqualTo(dialogueVolume * (.28f / .45f)).Within(.003f));

            deadline = Time.realtimeSinceStartup + 4;
            while (voice.CurrentSource.isPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(voice.CurrentSource.isPlaying, "The take must finish naturally.");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(StoryVoice.IsSpeaking); Assert.AreEqual("story", life.Mode);
            Assert.IsTrue(game.Dialogue); Assert.AreSame(musicSource, music.CurrentSource);
            Assert.That(musicSource.volume, Is.EqualTo(dialogueVolume).Within(.003f),
                "The open dialogue retains its normal mix after the additional voice duck ends.");
        }

        [UnityTest] public IEnumerator ListenerReceivesVoiceSamplesAndMasterMuteSilencesThem()
        {
            // Isolate the synthetic voice in the game's listener; no microphone or OS capture.
            game.Audio.SetSfxVolume(0);
            SignalMusic.Instance.SetMusicVolume(0);
            yield return new WaitForSecondsRealtime(.25f);
            BindVoice(3);
            string evidence = Path.GetFullPath("Artifacts/AudioProduction/LicenseRecovery/VoiceOutput");
            var capture = Camera.main.gameObject.AddComponent<QualityAudioCapture>();
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            var source = voice.CurrentSource;
            var output = new float[512];
            float deadline = Time.realtimeSinceStartup + 1.5f, peak = 0;
            while (peak <= .0001f && Time.realtimeSinceStartup < deadline)
            {
                source.GetOutputData(output, 0);
                foreach (float sample in output) peak = Mathf.Max(peak, Mathf.Abs(sample));
                yield return null;
            }
            Assert.Greater(peak, .0001f, "The dedicated voice source must reach the audio DSP.");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.IsTrue(StoryVoice.IsSpeaking);
            var playing = SaveListenerPcm(capture, Path.Combine(evidence, "playing"));
            Assert.Greater(playing.rms, .0001, "A playing voice must produce real listener PCM samples.");
            Object.Destroy(capture);
            yield return null;

            game.Audio.SetVolume(0);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsFalse(StoryVoice.IsSpeaking);
            capture = Camera.main.gameObject.AddComponent<QualityAudioCapture>();
            yield return new WaitForSecondsRealtime(.35f);
            var muted = SaveListenerPcm(capture, Path.Combine(evidence, "muted"));
            Assert.IsTrue(source.isPlaying, "The muted capture must occur before the fixture ends naturally.");
            Assert.Less(muted.peak, .0001, "Master mute must silence the actual listener output.");
            Object.Destroy(capture);
        }

        static (double rms, double peak) SaveListenerPcm(QualityAudioCapture capture, string folder)
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "game-audio.wav");
            // Remove only prior test evidence so a missing capture cannot pass using an old WAV.
            if (File.Exists(path)) File.Delete(path);
            string unavailable = Path.Combine(folder, "audio-capture-unavailable.txt");
            if (File.Exists(unavailable)) File.Delete(unavailable);
            capture.Save(folder);
            Assert.IsTrue(File.Exists(path), "The audio listener did not supply PCM samples.");
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4)));
                reader.BaseStream.Position = 22;
                int channels = reader.ReadInt16();
                int rate = reader.ReadInt32();
                reader.BaseStream.Position = 40;
                int sampleCount = reader.ReadInt32() / 2;
                Assert.GreaterOrEqual(channels, 1);
                Assert.Greater(rate, 0);
                Assert.Greater(sampleCount / (double)(rate * channels), .15, "Capture must include a meaningful playback interval.");
                Assert.AreEqual(44L + sampleCount * 2L, reader.BaseStream.Length);
                double squared = 0, peak = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    double sample = reader.ReadInt16() / 32768.0;
                    squared += sample * sample;
                    peak = System.Math.Max(peak, System.Math.Abs(sample));
                }
                return (System.Math.Sqrt(squared / sampleCount), peak);
            }
        }

        [UnityTest] public IEnumerator CloseDisableDestructionAndNaturalEndReleaseTheVoice()
        {
            BindVoice(.3f);
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(StoryVoice.IsSpeaking, "Ducking must end with the take while the subtitle remains open.");
            Assert.AreEqual("story", life.Mode);
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            yield return null;
            life.Dismiss();
            Assert.IsFalse(voice.CurrentSource.isPlaying); Assert.IsNull(voice.CurrentSource.clip);
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            yield return null;
            life.enabled = false;
            Assert.IsFalse(StoryVoice.IsSpeaking); Assert.IsFalse(voice.CurrentSource.isPlaying);
            life.enabled = true;
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            yield return null;
            voice.enabled = false;
            Assert.IsFalse(voice.CurrentSource.isPlaying);
            voice.enabled = true;
            life.StoryDialogue("기록", "서하", Text, false, _ => { });
            var source = voice.CurrentSource;
            Object.Destroy(voice);
            yield return null;
            yield return null;
            Assert.IsFalse(StoryVoice.IsSpeaking); Assert.IsFalse(source);
        }
    }
}
