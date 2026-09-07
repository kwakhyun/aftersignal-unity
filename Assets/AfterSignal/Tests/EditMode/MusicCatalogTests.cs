using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AfterSignal.Tests
{
    public class MusicCatalogTests
    {
        [Test] public void AllMastersAreStereoStreamingAssetsAndLoopsFitTheirDuration()
        {
            foreach (MusicCue cue in System.Enum.GetValues(typeof(MusicCue)))
            {
                var clip = Resources.Load<AudioClip>(MusicCatalog.Path(cue));
                Assert.NotNull(clip, cue.ToString());
                Assert.AreEqual(2, clip.channels);
                Assert.Greater(clip.length, 40);
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType);
                Assert.AreEqual(AudioCompressionFormat.Vorbis, importer.defaultSampleSettings.compressionFormat);
                Assert.IsFalse(importer.defaultSampleSettings.preloadAudioData);
                Assert.That(SignalMusic.LoopEnd(cue, clip), Is.LessThanOrEqualTo(clip.length));
                Assert.Greater(SignalMusic.LoopEnd(cue, clip) - SignalMusic.LoopStart(cue), 40);
            }
        }

        [Test] public void EveryStageAndFacilityHasTheIntendedMood()
        {
            var expected = new[] {
                MusicCue.Tension, MusicCue.Action, MusicCue.Action, MusicCue.Town,
                MusicCue.Tension, MusicCue.Tension, MusicCue.Tension, MusicCue.Action,
                MusicCue.Mystery, MusicCue.Mystery, MusicCue.Action, MusicCue.Action,
                MusicCue.Action, MusicCue.Action, MusicCue.Tension, MusicCue.Mystery,
                MusicCue.Mystery, MusicCue.Home, MusicCue.Home, MusicCue.Town,
                MusicCue.Home, MusicCue.Town, MusicCue.Action, MusicCue.Town, MusicCue.Home
            };
            for (int i = 0; i < CampaignRules.Scenes.Length; i++)
                Assert.AreEqual(expected[i], MusicCatalog.Select((StageId)i, 0, false, false), ((StageId)i).ToString());
            for (int site = 0; site < UrbanCatalog.SiteCount; site++)
            {
                int kind = UrbanCatalog.Kind(site);
                var mood = kind == 0 || kind == 4 || kind == 9 || kind == 15 ? MusicCue.Home : MusicCue.Town;
                Assert.AreEqual(mood, MusicCatalog.Select(StageId.UrbanInterior, site, false, false), UrbanCatalog.Name(site));
            }
            Assert.AreEqual(MusicCue.Boss, MusicCatalog.Select(StageId.Roof, 0, true, false));
            Assert.AreEqual(MusicCue.Town, MusicCatalog.Select(StageId.Roof, 0, true, true));
            Assert.AreEqual(MusicCue.Town, MusicCatalog.Select(StageId.Haven, 0, true, false));
        }
    }
}
