using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using Unity.Profiling;
using UnityEngine;

namespace AfterSignal
{
    public sealed class FrameDiagnostics : MonoBehaviour
    {
        readonly string[] names =
        {
            "CPU Main Thread Frame Time",
            "CPU Render Thread Frame Time",
            "CPU Total Frame Time",
            "PlayerLoop",
            "WaitForTargetFPS",
            "Gfx.WaitForPresentOnGfxThread",
            "Gfx.WaitForGfxCommandsFromMainThread"
        };
        ProfilerRecorder[] recorders;
        readonly List<float[]> rows = new List<float[]>(18000);
        float start;
        void Awake()
        {
            start = Time.realtimeSinceStartup;
            recorders = new ProfilerRecorder[names.Length];
            for (int i = 0; i < names.Length; i++)
                recorders[i] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, names[i]);
        }

        void LateUpdate()
        {
            if (Time.realtimeSinceStartup - start < 2)
                return;
            var row = new float[names.Length + 3];
            row[0] = Time.unscaledDeltaTime * 1000;
            row[1] = Application.isFocused ? 1 : 0;
            row[2] = Time.realtimeSinceStartup;
            for (int i = 0; i < names.Length; i++)
                row[i + 3] = recorders[i].Valid ? recorders[i].LastValue * .000001f : -1;
            rows.Add(row);
        }

        public void Save(string path)
        {
            var s = new StringBuilder("frame_ms,focused,elapsed," + string.Join(",", names) + "\n");
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0)
                        s.Append(',');
                    s.Append(row[i].ToString("F4", CultureInfo.InvariantCulture));
                }

                s.Append('\n');
            }

            File.WriteAllText(Path.Combine(path, "frames.csv"), s.ToString());
        }

        void OnDestroy()
        {
            if (recorders != null)
                foreach (var recorder in recorders)
                    recorder.Dispose();
        }
    }
}
