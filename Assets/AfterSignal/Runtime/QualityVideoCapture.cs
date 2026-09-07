using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    // Opt-in capture of the actual URP camera. Presentation only; never changes simulation coordinates.
    public sealed class QualityVideoCapture:MonoBehaviour
    {
        Process encoder;Stream input;RenderTexture target;Texture2D pixels;float next;const float Interval=1f/30;bool finished;
        IEnumerator Start()
        {
            string ffmpeg=QualitySession.Arg("-quality-ffmpeg");if(string.IsNullOrEmpty(ffmpeg))yield break;
            target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            var start=new ProcessStartInfo(ffmpeg,"-y -f rawvideo -pix_fmt rgb24 -s 1600x900 -r 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset ultrafast -crf 20 -pix_fmt yuv420p \""+Path.Combine(QualitySession.Output,"gameplay-unedited.mp4")+"\""){UseShellExecute=false,RedirectStandardInput=true,RedirectStandardError=true,CreateNoWindow=true};
            encoder=Process.Start(start);encoder.ErrorDataReceived+=(s,e)=>{};encoder.BeginErrorReadLine();input=encoder.StandardInput.BaseStream;next=Time.realtimeSinceStartup;
            while(!finished){yield return new WaitForEndOfFrame();if(finished)yield break;if(Time.realtimeSinceStartup+.001f<next)continue;
                var camera=Camera.main;var canvas=FindAnyObjectByType<Canvas>();if(!camera||!canvas)continue;
                var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();RenderTexture.active=old;
                canvas.renderMode=mode;canvas.worldCamera=oldCamera;Canvas.ForceUpdateCanvases();
                byte[] data=pixels.GetRawTextureData<byte>().ToArray();float now=Time.realtimeSinceStartup;int repeats=Mathf.Clamp(Mathf.FloorToInt((now-next)/Interval)+1,1,8);for(int i=0;i<repeats;i++)input.Write(data,0,data.Length);next+=repeats*Interval;
            }
        }
        public void Finish(){if(finished)return;finished=true;if(input!=null){input.Close();input=null;}if(encoder!=null){if(!encoder.WaitForExit(15000))encoder.Kill();encoder.Dispose();encoder=null;}if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);}
        void OnDestroy(){Finish();}
    }
}
