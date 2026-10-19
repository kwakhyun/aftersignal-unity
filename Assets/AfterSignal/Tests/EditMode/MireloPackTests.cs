using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AfterSignal.Tests
{
    public class MireloPackTests
    {
        [Serializable] class Plan { public Request[] requests; }
        [Serializable] class Request { public string cue, kind; public string[] planned_files; }

        [Test] public void InstalledPackMatchesEveryPlannedFileAndImportsReadablePcm()
        {
            if (Resources.LoadAll<AudioClip>(GeneratedSoundBank.ResourcePath).Length == 0)
                Assert.Ignore("Optional private Mirelo pack is not installed.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText("Documentation/Audio/mirelo-sfx-plan.json"));
            int checkedClips = 0, loops = 0;
            foreach (var request in plan.requests)
            foreach (var file in request.planned_files)
            {
                bool loop = request.kind == "loop";
                string path = "Assets/AfterSignal/Resources/Audio/Generated/" + (loop ? "Loops/" : "SFX/") + file;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.IsNotNull(clip, path);
                Assert.AreEqual(48000, clip.frequency, file); Assert.AreEqual(1, clip.channels, file);
                var settings = ((AudioImporter)AssetImporter.GetAtPath(path)).defaultSampleSettings;
                Assert.AreEqual(AudioCompressionFormat.PCM, settings.compressionFormat, file);
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, settings.loadType, file);
                clip.LoadAudioData();
                var data = new float[clip.samples]; Assert.IsTrue(clip.GetData(data, 0), file);
                float peak = 0; bool finite = true;
                foreach (float sample in data)
                {
                    finite &= !float.IsNaN(sample) && !float.IsInfinity(sample);
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }
                Assert.IsTrue(finite, file);
                Assert.That(peak, Is.InRange(.001f, .801f), file);
                Assert.That(clip.length, Is.InRange(.02f, loop ? 20 : 5), file);
                if (loop)
                {
                    Assert.Less(Mathf.Abs(data[0] - data[data.Length - 1]), .00013f, file + " seam");
                    Assert.AreSame(clip, GeneratedLoops.Load(request.cue));
                    loops++;
                }
                checkedClips++;
            }
            Assert.AreEqual(123, checkedClips); Assert.AreEqual(19, loops);
            Assert.AreEqual(104, Resources.LoadAll<AudioClip>(GeneratedSoundBank.ResourcePath).Length);
            Assert.AreEqual(19, Resources.LoadAll<AudioClip>(GeneratedLoops.ResourcePath).Length);
        }

        [Test] public void MissingOptionalLoopUsesTheOriginalResource()
        {
            var original = Resources.Load<AudioClip>("Audio/Transport/sedan");
            Assert.IsNotNull(original);
            Assert.AreSame(original, GeneratedLoops.Load("absent_test_loop", "Audio/Transport/sedan"));
            Assert.IsNull(GeneratedLoops.Load("absent_test_loop"));
        }
    }
}
