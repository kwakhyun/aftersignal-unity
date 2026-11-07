using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    /// <summary>
    /// Player-facing presentation, display, camera and HUD preferences. All values live in PlayerPrefs under
    /// AFTERSIGNAL.Unity.* (settings are per machine, not per save slot) and are shared by the title and pause menus.
    /// </summary>
    public static class PresentationSettings
    {
        public static float Motion=1,Effects=1;
        public static bool Post=true;
        // HUD and camera
        public static float UiScale=1,MouseSensitivity=1;
        public static bool InvertY,AutoAdvance;
        public static int ControlHints=1,TextSpeed=1,FrameLimit=1;
        public static bool VSync;
        public static readonly float[] UiScales={.9f,1f,1.15f};
        public static readonly string[] UiScaleNames={"작게","보통","크게"};
        public static readonly string[] ControlHintNames={"항상 표시","처음 10분","숨김"};
        public static readonly string[] TextSpeedNames={"즉시","빠르게","보통"};
        public static readonly int[] FrameLimits={30,60,120,144,0};
        public static readonly string[] FrameLimitNames={"30 FPS","60 FPS","120 FPS","144 FPS","제한 없음"};
        const string Prefix="AFTERSIGNAL.Unity.";
        static bool loaded;
        public static void Load()
        {
            Motion=PlayerPrefs.GetFloat(Prefix+"Motion",.65f);Effects=PlayerPrefs.GetFloat(Prefix+"Effects",.7f);Post=PlayerPrefs.GetInt(Prefix+"Post",1)!=0;
            UiScale=Mathf.Clamp(PlayerPrefs.GetFloat(Prefix+"UiScale",1),.85f,1.2f);MouseSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(Prefix+"MouseSensitivity",1),.2f,3);
            InvertY=PlayerPrefs.GetInt(Prefix+"InvertY",0)!=0;AutoAdvance=PlayerPrefs.GetInt(Prefix+"AutoAdvance",0)!=0;
            ControlHints=Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"ControlHints",1),0,2);TextSpeed=Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"TextSpeed",1),0,2);
            FrameLimit=Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"FrameLimit",1),0,FrameLimits.Length-1);VSync=PlayerPrefs.GetInt(Prefix+"VSync",0)!=0;
            loaded=true;Apply();
        }
        public static void EnsureLoaded(){if(!loaded)Load();}
        public static void Apply(){if(Camera.main)Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=Post;FramePacing.Apply();}
        public static void Save()
        {
            PlayerPrefs.SetFloat(Prefix+"Motion",Motion);PlayerPrefs.SetFloat(Prefix+"Effects",Effects);PlayerPrefs.SetInt(Prefix+"Post",Post?1:0);
            PlayerPrefs.SetFloat(Prefix+"UiScale",UiScale);PlayerPrefs.SetFloat(Prefix+"MouseSensitivity",MouseSensitivity);
            PlayerPrefs.SetInt(Prefix+"InvertY",InvertY?1:0);PlayerPrefs.SetInt(Prefix+"AutoAdvance",AutoAdvance?1:0);
            PlayerPrefs.SetInt(Prefix+"ControlHints",ControlHints);PlayerPrefs.SetInt(Prefix+"TextSpeed",TextSpeed);
            PlayerPrefs.SetInt(Prefix+"FrameLimit",FrameLimit);PlayerPrefs.SetInt(Prefix+"VSync",VSync?1:0);
            PlayerPrefs.Save();Apply();
        }
        /// <summary>Basic control hints are shown always, only during the first ten minutes of play, or never.</summary>
        public static bool ShowBasicHints=>ControlHints==0||ControlHints==1&&HintSeconds<600;
        public static float HintSeconds{get=>PlayerPrefs.GetFloat(Prefix+"HintSeconds",0);set=>PlayerPrefs.SetFloat(Prefix+"HintSeconds",value);}
        /// <summary>Characters per second for story text; 0 means instant.</summary>
        public static float CharactersPerSecond=>TextSpeed==0?0:TextSpeed==1?90:45;
        public static int UiScaleIndex{get{int best=1;for(int i=0;i<UiScales.Length;i++)if(Mathf.Abs(UiScales[i]-UiScale)<Mathf.Abs(UiScales[best]-UiScale))best=i;return best;}}
        /// <summary>Reference resolution for every gameplay/title canvas; a smaller reference makes the HUD larger.</summary>
        public static Vector2 CanvasReference=>new Vector2(1600,900)/Mathf.Max(.5f,UiScale);
    }
}
