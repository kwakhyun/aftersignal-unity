using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AfterSignal.Tests
{
    public class MusicPlaybackTests
    {
        SignalMusic music;
        bool hadVolume, hadMusicVolume, hadSite, suppressed;
        float volume, musicVolume;
        int site;
        [UnitySetUp] public IEnumerator Setup()
        {
            hadVolume=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.Volume");volume=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.Volume");
            hadMusicVolume=PlayerPrefs.HasKey("AFTERSIGNAL.Unity.MusicVolume");musicVolume=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.MusicVolume");
            hadSite=PlayerPrefs.HasKey(UrbanCatalog.Prefix+"Site");site=UrbanCatalog.Current;
            suppressed=LifeState.SuppressSave;LifeState.SuppressSave=true;
            if (SignalMusic.Instance) Object.Destroy(SignalMusic.Instance.gameObject);
            yield return null;
            GameDirector.SkipTitle=true;
            yield return Load(StageId.Residence);
            music=SignalMusic.Instance;
            music.SetMusicVolume(.55f);
            GameDirector.Instance.Audio.SetVolume(.45f);
            yield return Settle(MusicCue.Home);
        }
        IEnumerator Load(StageId stage)
        {
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(stage));
            yield return null;
            var game=GameDirector.Instance;
            game.Input.ExternalControl=true;game.Input.ExternalFrame=ControlFrame.Empty;
            game.SetPaused(false);game.CloseDialogue();
        }
        IEnumerator Settle(MusicCue cue)
        {
            float until=Time.realtimeSinceStartup+12;
            while (Time.realtimeSinceStartup<until &&
                   (!music.CurrentSource.clip || music.CurrentCue!=cue || music.IsFading || !music.CurrentSource.isPlaying)) yield return null;
            Assert.AreEqual(cue,music.CurrentCue);
            Assert.IsFalse(music.IsFading);
            Assert.IsTrue(music.CurrentSource.isPlaying);
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;
            // Unload gameplay while saving is still suppressed, then restore only touched preferences.
            var empty=SceneManager.CreateScene("Music test cleanup");SceneManager.SetActiveScene(empty);
            for(int i=SceneManager.sceneCount-1;i>=0;i--){var s=SceneManager.GetSceneAt(i);if(s!=empty)yield return SceneManager.UnloadSceneAsync(s);}
            if (SignalMusic.Instance) Object.Destroy(SignalMusic.Instance.gameObject);
            yield return null;
            LifeState.SuppressSave=suppressed;
            Restore("AFTERSIGNAL.Unity.Volume",volume,hadVolume);
            Restore("AFTERSIGNAL.Unity.MusicVolume",musicVolume,hadMusicVolume);
            if(hadSite)PlayerPrefs.SetInt(UrbanCatalog.Prefix+"Site",site);else PlayerPrefs.DeleteKey(UrbanCatalog.Prefix+"Site");
            PlayerPrefs.Save();
        }
        void Restore(string key,float value,bool existed){if(existed)PlayerPrefs.SetFloat(key,value);else PlayerPrefs.DeleteKey(key);}

        [UnityTest] public IEnumerator SceneChangesKeepSharedMusicAndCrossfadeDifferentTracks()
        {
            var transport=music;var source=music.CurrentSource;float before=source.time;
            yield return Load(StageId.Clinic);
            Assert.AreSame(transport,SignalMusic.Instance);Assert.AreSame(source,music.CurrentSource);
            Assert.GreaterOrEqual(source.time,before);
            yield return Load(StageId.Haven);yield return Settle(MusicCue.Town);
            yield return Load(StageId.Station);yield return Settle(MusicCue.Tension);
            yield return Load(StageId.Lab);yield return Settle(MusicCue.Mystery);
            Assert.AreEqual(1,Object.FindObjectsByType<SignalMusic>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(2,music.GetComponents<AudioSource>().Length);
        }

        [UnityTest] public IEnumerator PauseMuteDialogueAndLoopRestartUseTheActualStreamingSources()
        {
            var game=GameDirector.Instance;
            var capture=Camera.main.gameObject.AddComponent<QualityAudioCapture>();
            string evidence=System.IO.Path.GetFullPath("Artifacts/Music");
            System.IO.Directory.CreateDirectory(evidence);
            game.SetPaused(true);yield return new WaitForSecondsRealtime(.15f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(evidence,"pause-menu.png"));
            float position=music.CurrentSource.time;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(music.CurrentSource.time,Is.EqualTo(position).Within(.03f));Assert.IsTrue(music.IsPaused);
            game.Audio.SetVolume(0);foreach(var voice in music.GetComponents<AudioSource>())Assert.AreEqual(0,voice.volume);
            game.Audio.SetVolume(.45f);game.SetPaused(false);yield return new WaitForSecondsRealtime(.2f);
            Assert.Greater(music.CurrentSource.time,position);
            float normal=music.CurrentSource.volume;
            game.ShowDialogue("음악 확인","대화 중 음량");yield return new WaitForSecondsRealtime(.5f);
            Assert.Less(music.CurrentSource.volume,normal*.6f);
            game.CloseDialogue();yield return new WaitForSecondsRealtime(.5f);
            Assert.That(music.CurrentSource.volume,Is.EqualTo(normal).Within(.01f));
            music.SetMusicVolume(0);Assert.AreEqual(0,music.CurrentSource.volume);
            Assert.AreEqual(.45f,game.Audio.Volume);music.SetMusicVolume(.55f);
            int loops=music.LoopCount;
            music.CurrentSource.time=SignalMusic.LoopEnd(music.CurrentCue,music.CurrentSource.clip)-2.2f;
            yield return new WaitForSecondsRealtime(2.8f);
            Assert.Greater(music.LoopCount,loops);Assert.IsTrue(music.CurrentSource.isPlaying);
            Assert.That(music.CurrentSource.time,Is.LessThan(5));
            capture.Save(evidence);
        }

        [UnityTest] public IEnumerator ConductorStartsOnApproachAndEndsOnDefeat()
        {
            yield return Load(StageId.Roof);yield return Settle(MusicCue.Action);
            var game=GameDirector.Instance;
            foreach(var enemy in game.Enemies)if(!enemy.boss)enemy.gameObject.SetActive(false);
            game.Player.Respawn(new Vector3(59,.1f,0));
            yield return Settle(MusicCue.Boss);
            game.Player.Respawn(game.spawn);yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(MusicCue.Boss,music.CurrentCue,"Retreat must not restart the cue");
            game.Boss.Damage(game.Boss.maxHealth,Vector3.zero,true);
            yield return Settle(MusicCue.Town);
        }
    }
}
