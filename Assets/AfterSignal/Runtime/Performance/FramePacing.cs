using UnityEngine;

namespace AfterSignal
{
    public static class FramePacing
    {
        public static void Apply()
        {
            PresentationSettings.EnsureLoaded();
            // The measured Windows DXGI flip queue stalls despite low CPU/GPU cost.
            // The Windows player uses D3D11 BitBlt with this measured 60 Hz cap.
            // Players can opt into VSync or another cap in the settings; command-line quality flags still win.
            QualitySettings.vSyncCount = PresentationSettings.VSync ? 1 : 0;
            int limit = PresentationSettings.FrameLimits[Mathf.Clamp(PresentationSettings.FrameLimit, 0, PresentationSettings.FrameLimits.Length - 1)];
            Application.targetFrameRate = PresentationSettings.VSync ? -1 : limit > 0 ? limit : -1;
            // A slow frame must not trigger a long physics catch-up spiral on the next frame.
            Time.maximumDeltaTime = .08f;
            if (int.TryParse(QualitySession.Arg("-quality-vsync"), out int requestedSync))
                QualitySettings.vSyncCount = Mathf.Clamp(requestedSync, 0, 4);
            if (QualitySettings.vSyncCount == 0)
            {
                if (int.TryParse(QualitySession.Arg("-quality-fps"), out int fps))
                    Application.targetFrameRate = Mathf.Clamp(fps, 15, 240);
            }
        }
    }
}
