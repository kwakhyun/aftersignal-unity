using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal
{
    [DefaultExecutionOrder(3000)]
    public sealed class FourCityAtmosphere:MonoBehaviour
    {
        bool tinted;FogMode previousMode;float previousDensity;Color previousColor;CameraClearFlags previousFlags;
        void OnEnable()=>RenderPipelineManager.beginCameraRendering+=Render;
        void OnDisable(){RenderPipelineManager.beginCameraRendering-=Render;Restore();}
        void Render(ScriptableRenderContext context,Camera camera)
        {
            var game=GameDirector.Instance;if(!game||!game.Ready||camera!=Camera.main)return;
            var p=camera.transform.position;int city=FourCityCatalog.CityAt(p);bool custom=city==2||city==3&&FourCityCatalog.Dry(p);
            if(!custom){Restore();return;}
            if(!tinted){tinted=true;previousMode=RenderSettings.fogMode;previousDensity=RenderSettings.fogDensity;previousColor=RenderSettings.fogColor;previousFlags=camera.clearFlags;}
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=city==2?.0008f:.0007f;RenderSettings.fogColor=city==2?new Color(.105f,.065f,.16f):new Color(.025f,.20f,.28f);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=city==2?new Color(.037f,.021f,.07f):new Color(.018f,.10f,.17f);
        }
        void Restore(){if(!tinted)return;tinted=false;RenderSettings.fogMode=previousMode;RenderSettings.fogDensity=previousDensity;RenderSettings.fogColor=previousColor;if(Camera.main)Camera.main.clearFlags=previousFlags;}
    }
}
