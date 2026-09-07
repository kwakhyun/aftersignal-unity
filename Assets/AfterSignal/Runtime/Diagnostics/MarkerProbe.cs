using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

namespace AfterSignal
{
    public sealed class MarkerProbe : MonoBehaviour
    {
        sealed class Entry
        {
            public string name;
            public ProfilerRecorder recorder;
            public long sum, max;
        }

        readonly List<Entry> entries = new List<Entry>();
        int frames;
        float start;
        void Start()
        {
            start = Time.realtimeSinceStartup;
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(handle);
                if (d.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds)
                    continue;
                var r = ProfilerRecorder.StartNew(d.Category, d.Name, 1);
                if (r.Valid)
                    entries.Add(new Entry { name = d.Name, recorder = r });
            }
        }

        void LateUpdate()
        {
            if (Time.realtimeSinceStartup - start < 2)
                return;
            frames++;
            foreach (var e in entries)
            {
                long v = e.recorder.LastValue;
                e.sum += v;
                e.max = Math.Max(e.max, v);
            }
        }

        public void Save()
        {
            File.WriteAllLines(Path.Combine(QualitySession.Output, "markers.csv"), new[] { "marker,average_ms,max_ms" }.Concat(entries.OrderByDescending(e => e.sum).Select(e => "\"" + e.name.Replace("\"", "\"\"") + "\"," + (e.sum / Math.Max(1.0, frames) * .000001).ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (e.max * .000001).ToString(System.Globalization.CultureInfo.InvariantCulture))));
        }

        void OnDestroy()
        {
            foreach (var e in entries)
                e.recorder.Dispose();
        }
    }
}
