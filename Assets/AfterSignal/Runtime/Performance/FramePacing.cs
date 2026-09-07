using UnityEngine;

namespace AfterSignal
{
    public static class FramePacing
    {
        public static void Apply()
        {
            // The measured Windows DXGI flip queue stalls despite low CPU/GPU cost.
            // The Windows player uses D3D11 BitBlt with this measured 60 Hz cap.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            if (int.TryParse(QualitySession.Arg("-quality-vsync"), out int requestedSync))
                QualitySettings.vSyncCount = Mathf.Clamp(requestedSync, 0, 4);
            if (QualitySettings.vSyncCount == 0)
                Application.targetFrameRate = int.TryParse(QualitySession.Arg("-quality-fps", "60"), out int fps) ? Mathf.Clamp(fps, 15, 240) : 60;
        }
    }
}
