using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    [DefaultExecutionOrder(2500)]
    public sealed class FidelityPresentation : MonoBehaviour
    {
        public static int Preset { get; private set; }=1;
        public static string PresetName => Preset==0?"성능":Preset==1?"고품질":"최고 품질";
        GameDirector game; Volume volume; VolumeProfile profile; ColorAdjustments grade; Bloom bloom;
        ReflectionProbe probe; int capture=-1; float nextProbe,lastHour=-99; Vector3 lastProbe;
        public static void Cycle(){Preset=(Preset+1)%3;PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Graphics",Preset);PlayerPrefs.Save();ApplyQuality();}
        public static void ApplyQuality()
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(!pipeline)return;
            pipeline.msaaSampleCount=Preset==0?2:4;pipeline.renderScale=Preset==0?.85f:1;pipeline.shadowDistance=Preset==0?80:Preset==1?140:210;
            pipeline.mainLightShadowmapResolution=Preset==0?2048:4096;pipeline.shadowCascadeCount=4;
            if(Camera.main){var data=Camera.main.GetUniversalAdditionalCameraData();data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;}
        }
        void Start()
        {
            game=GameDirector.Instance;Preset=Mathf.Clamp(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Graphics",1),0,2);ApplyQuality();
            volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=25;profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            grade=profile.Add<ColorAdjustments>(true);grade.contrast.Override(14);grade.saturation.Override(-3);
            bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.25f);bloom.intensity.Override(.28f);bloom.scatter.Override(.65f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.11f);vignette.smoothness.Override(.6f);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var go=new GameObject("Local city reflection");go.transform.SetParent(transform,false);probe=go.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.size=new Vector3(120,70,120);probe.boxProjection=true;probe.blendDistance=16;probe.nearClipPlane=.4f;probe.farClipPlane=110;probe.hdr=true;probe.cullingMask=1;probe.shadowDistance=45;probe.intensity=.75f;
        }
        void LateUpdate()
        {
            if(!game||!game.Ready||!Camera.main)return;
            bool outdoor=game.stage==StageId.UrbanCity||game.stage==StageId.Haven;
            float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.32f,Mathf.Sin((LifeState.Hour-6)/24*Mathf.PI*2)));
            grade.postExposure.Override(outdoor?Mathf.Lerp(.45f,.05f,day):.18f);bloom.intensity.Override(Mathf.Lerp(.36f,.2f,day));
            // A damp maritime city; roughness varies by material and position, never a mirrored sheet.
            Shader.SetGlobalFloat("_CityWetness",outdoor?Mathf.Lerp(.7f,.18f,day):0);
            if(!probe||Preset==0){if(probe)probe.enabled=false;return;}probe.enabled=true;
            var p=Camera.main.transform.position;bool moved=(p-lastProbe).sqrMagnitude>28*28;bool hourChanged=Mathf.Abs(LifeState.Hour-lastHour)>.6f;
            if(Time.unscaledTime>nextProbe&&(capture<0||probe.IsFinishedRendering(capture))&&(moved||hourChanged))
            {
                lastProbe=p;lastHour=LifeState.Hour;probe.transform.position=p+Vector3.up;probe.resolution=Preset==2?256:128;capture=probe.RenderProbe();nextProbe=Time.unscaledTime+8;
            }
        }
        void OnDestroy(){if(profile)Destroy(profile);Shader.SetGlobalFloat("_CityWetness",0);}
    }
}
