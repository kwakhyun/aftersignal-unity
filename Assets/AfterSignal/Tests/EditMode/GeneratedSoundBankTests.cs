using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AfterSignal.Tests
{
    public class GeneratedSoundBankTests
    {
        readonly List<AudioClip> clips = new List<AudioClip>();
        AudioClip Clip(string name)
        {
            var clip = AudioClip.Create(name, 128, 1, 48000, false);
            clips.Add(clip);
            return clip;
        }
        [TearDown] public void Cleanup()
        {
            foreach (var clip in clips) Object.DestroyImmediate(clip);
            clips.Clear();
        }

        [Test] public void SingleGeneratedClipReplacesEveryLegacyVariation()
        {
            var legacy = new Dictionary<string, AudioClip>();
            for (int i = 0; i < 3; i++) legacy["blade_hit_" + i] = Clip("blade_hit_" + i);
            var replacement = Clip("blade_hit");
            var bank = new GeneratedSoundBank(new[] { Clip("blade_hit_2"), replacement });
            for (int i = 0; i < 6; i++) Assert.AreSame(replacement, bank.Resolve("blade_hit", i, legacy));
        }

        [Test] public void SparseGeneratedVariationsCycleWithoutReplayingMissingSlots()
        {
            var first = Clip("step_tile_0");
            var second = Clip("step_tile_2");
            var legacy = new Dictionary<string, AudioClip> { ["step_tile"] = Clip("step_tile") };
            var bank = new GeneratedSoundBank(new[] { second, first });
            Assert.IsTrue(bank.Contains("step_tile"));
            for (int i = 0; i < 6; i++) Assert.AreSame(i % 2 == 0 ? first : second, bank.Resolve("step_tile", i, legacy));
        }

        [Test] public void AbsentGeneratedCueKeepsLegacySelectionAndMissingCueBehavior()
        {
            var first = Clip("rope_attach_0");
            var second = Clip("rope_attach_1");
            var single = Clip("ui_cancel");
            var legacy = new Dictionary<string, AudioClip>
            {
                ["rope_attach_0"] = first, ["rope_attach_1"] = second, ["ui_cancel"] = single
            };
            foreach (var bank in new[] { new GeneratedSoundBank(), new GeneratedSoundBank(new[] { Clip("blade_hit") }) })
            {
                Assert.IsFalse(bank.Contains("rope_attach"));
                Assert.AreSame(first, bank.Resolve("rope_attach", 0, legacy));
                Assert.AreSame(second, bank.Resolve("rope_attach", 1, legacy));
                Assert.AreSame(first, bank.Resolve("rope_attach", 2, legacy));
                Assert.AreSame(single, bank.Resolve("ui_cancel", 2, legacy));
                Assert.IsNull(bank.Resolve("not_installed", 0, legacy));
            }
        }

        [Test] public void OneOrThreeGeneratedVariationsResolveWithoutLegacyRecordings()
        {
            var only = Clip("dash_1");
            var zero = Clip("guard_0");
            var one = Clip("guard_1");
            var two = Clip("guard_2");
            var bank = new GeneratedSoundBank(new[] { two, only, one, zero });
            var ordered = new[] { zero, one, two };
            for (int i = 0; i < 6; i++)
            {
                Assert.AreSame(only, bank.Resolve("dash", i, null));
                Assert.AreSame(ordered[i % 3], bank.Resolve("guard", i, null));
            }
        }
    }
}
