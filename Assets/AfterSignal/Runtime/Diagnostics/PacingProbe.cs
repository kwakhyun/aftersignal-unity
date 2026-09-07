using System.Collections;
using UnityEngine;

namespace AfterSignal
{
    // Short stationary diagnostic, separate from combat/playthrough evidence.
    public sealed class PacingProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            var g = GetComponent<GameDirector>();
            g.Input.ExternalControl = true;
            g.SetPaused(false);
            var stats = gameObject.AddComponent<QualitySession>();
            MarkerProbe markers = null;
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-quality-markers") >= 0)
                markers = gameObject.AddComponent<MarkerProbe>();
            float until = Time.realtimeSinceStartup + 15;
            while (Time.realtimeSinceStartup < until)
            {
                if (g.Paused)
                    g.SetPaused(false);
                yield return null;
            }

            stats.Save();
            if (markers)
                markers.Save();
            Application.Quit(0);
        }
    }
}
