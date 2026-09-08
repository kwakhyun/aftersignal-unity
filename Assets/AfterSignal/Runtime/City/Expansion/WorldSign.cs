using UnityEngine;
namespace AfterSignal
{
    public sealed class WorldSign:MonoBehaviour
    {
        static Material material;TextMesh text;
        void OnEnable(){if(!Application.isPlaying)return;text=GetComponent<TextMesh>();Font.textureRebuilt+=Refresh;Apply();}
        void Refresh(Font font){if(text&&font==text.font)Apply();}
        void Apply(){if(!text||!text.font)return;if(!material)material=new Material(Shader.Find("AfterSignal/WorldLettering"));material.mainTexture=text.font.material.mainTexture;GetComponent<MeshRenderer>().sharedMaterial=material;}
        void OnDisable(){Font.textureRebuilt-=Refresh;}
    }
}
