using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    // Optional authored replacements stay separate from the shipped fallback recordings.
    public sealed class GeneratedSoundBank
    {
        public const string ResourcePath = "Audio/Generated/SFX";
        sealed class Cue
        {
            public AudioClip single;
            public readonly AudioClip[] variants = new AudioClip[3];
        }
        readonly Dictionary<string, Cue> cues = new Dictionary<string, Cue>();

        public GeneratedSoundBank(IEnumerable<AudioClip> clips = null)
        {
            if (clips == null) return;
            foreach (var clip in clips)
            {
                if (!clip || string.IsNullOrEmpty(clip.name)) continue;
                string name = clip.name;
                int slot = -1;
                if (name.Length > 2 && name[name.Length - 2] == '_' &&
                    name[name.Length - 1] >= '0' && name[name.Length - 1] <= '2')
                {
                    slot = name[name.Length - 1] - '0';
                    name = name.Substring(0, name.Length - 2);
                }
                if (!cues.TryGetValue(name, out var cue)) cues[name] = cue = new Cue();
                if (slot < 0) cue.single = clip;
                else cue.variants[slot] = clip;
            }
        }

        public bool Contains(string cue) => cues.ContainsKey(cue);

        public AudioClip Resolve(string cue, int sequence, IDictionary<string, AudioClip> legacy)
        {
            sequence &= int.MaxValue;
            if (cues.TryGetValue(cue, out var replacement))
            {
                // A single replacement replaces the entire cue, including existing variants.
                if (replacement.single) return replacement.single;
                int count = 0;
                foreach (var clip in replacement.variants) if (clip) count++;
                if (count > 0)
                {
                    int index = sequence % count;
                    foreach (var clip in replacement.variants)
                        if (clip && index-- == 0) return clip;
                }
            }
            // Preserve the original selection order whenever this cue has no generated replacement.
            if (legacy != null)
            {
                for (int i = 0; i < 3; i++)
                    if (legacy.TryGetValue(cue + "_" + ((sequence % 3 + i) % 3), out var clip)) return clip;
                if (legacy.TryGetValue(cue, out var single)) return single;
            }
            return null;
        }
    }
}
