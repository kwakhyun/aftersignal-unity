using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterSignal
{
    // Opt-in development instrumentation. Never runs in an ordinary game session.
    public sealed class QualitySession : MonoBehaviour
    {
        public static string Output;
        readonly List<float> frames=new List<float>(18000);
        readonly List<long> allocations=new List<long>(18000);
        ProfilerRecorder gc,batches,memory;
        long maxMemory=-1,maxBatches=-1; float started;QualityAudioCapture audioCapture;QualityVideoCapture videoCapture;bool lightweight;
        readonly FrameTiming[] timing=new FrameTiming[1];
        readonly List<double> gpu=new List<double>(18000);
        public static string Arg(string name,string fallback="") {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        void Awake()
        {
            Output=Path.GetFullPath(Arg("-quality-output",Path.Combine(Application.dataPath,"../../../Artifacts/Quality/Run")));Directory.CreateDirectory(Output);
            lightweight=Array.IndexOf(Environment.GetCommandLineArgs(),"-quality-lightweight")>=0;
            if(!lightweight){
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);
            memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);
            started=Time.realtimeSinceStartup;
            gameObject.AddComponent<FrameDiagnostics>();
            }started=Time.realtimeSinceStartup;
            string recordStages=Arg("-quality-record-stages");bool record=string.IsNullOrEmpty(recordStages)||Array.IndexOf(recordStages.Split(','),GameDirector.Instance.stage.ToString())>=0;
            if(record&&!string.IsNullOrEmpty(Arg("-quality-ffmpeg"))){audioCapture=Camera.main.gameObject.AddComponent<QualityAudioCapture>();videoCapture=gameObject.AddComponent<QualityVideoCapture>();}
        }
        void LateUpdate()
        {
            if(Time.realtimeSinceStartup-started<2)return;
            frames.Add(Time.unscaledDeltaTime*1000);allocations.Add(gc.Valid?gc.LastValue:-1);
            maxMemory=Math.Max(maxMemory,memory.Valid?memory.LastValue:-1);if(batches.Valid&&batches.LastValue>0)maxBatches=Math.Max(maxBatches,batches.LastValue);
            if(!lightweight){FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);}
        }
        public void Save()
        {
            if(GetComponent<FrameDiagnostics>())GetComponent<FrameDiagnostics>().Save(Output);
            if(videoCapture)videoCapture.Finish();if(audioCapture)audioCapture.Save(Output);
            if(frames.Count==0)return;var sorted=frames.ToArray();Array.Sort(sorted);double sum=0,alloc=0;int slow=0;foreach(float f in frames){sum+=f;if(f>33.34f)slow++;}foreach(long a in allocations)alloc+=Math.Max(0,a);
            var g=GameDirector.Instance;
            File.WriteAllText(Path.Combine(Output,"metrics.json"),JsonUtility.ToJson(new Metrics {device=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,width=Screen.width,height=Screen.height,frames=frames.Count,meanMs=sum/frames.Count,p95Ms=sorted[(int)(sorted.Length*.95f)],p99Ms=sorted[(int)(sorted.Length*.99f)],over33ms=slow,meanGcBytes=Debug.isDebugBuild&&gc.Valid?alloc/frames.Count:-1,peakMemoryBytes=maxMemory,maxBatches=maxBatches,gpuMeanMs=gpu.Count>0?Mean(gpu):-1,health=g.Player.Health,kills=g.Kills,attacks=g.Player.Attacks,build=Debug.isDebugBuild?"Development":"Release"},true));
        }
        static double Mean(List<double> values){double n=0;foreach(double v in values)n+=v;return n/values.Count;}
        void OnDestroy(){gc.Dispose();batches.Dispose();memory.Dispose();}
        [Serializable] class Metrics {public string device,cpu,api,unity,build;public int width,height,frames,over33ms,kills,attacks;public int vsync=QualitySettings.vSyncCount,targetFps=Application.targetFrameRate;public double displayHz=Screen.currentResolution.refreshRateRatio.value;public double meanMs,p95Ms,p99Ms,meanGcBytes,gpuMeanMs;public long peakMemoryBytes,maxBatches;public float health;}
    }
}
