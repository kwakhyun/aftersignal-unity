using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    public static class PresentationSettings
    {
        public static float Motion=1,Effects=1;
        public static bool Post=true;
        public static void Load(){Motion=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.Motion",.65f);Effects=PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.Effects",.7f);Post=PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Post",1)!=0;Apply();}
        public static void Apply(){if(Camera.main)Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=Post;}
        public static void Save(){PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.Motion",Motion);PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.Effects",Effects);PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Post",Post?1:0);Apply();}
    }
}
