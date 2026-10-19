using UnityEngine;
namespace AfterSignal
{
    // Optional loop replacements retain each source's existing spatial and pitch controls.
    public static class GeneratedLoops
    {
        public const string ResourcePath = "Audio/Generated/Loops/";
        public static AudioClip Load(string cue, string fallbackPath = null)
        {
            var clip = Resources.Load<AudioClip>(ResourcePath + cue);
            return clip ? clip : string.IsNullOrEmpty(fallbackPath) ? null : Resources.Load<AudioClip>(fallbackPath);
        }
    }
}
