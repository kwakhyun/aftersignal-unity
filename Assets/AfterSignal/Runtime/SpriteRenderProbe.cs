using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace AfterSignal
{
    public static class SpriteRenderProbe
    {
        public static bool Passed;public static float MirrorError;public static int VisiblePixels;
        public static IEnumerator Run(string folder)
        {
            var go=new GameObject("QA sprite render",typeof(SpriteRenderer));go.layer=31;var sprite=go.GetComponent<SpriteRenderer>();sprite.sprite=Array.Find(Resources.LoadAll<Sprite>("Art/Hero"),s=>s.name=="seo-00");sprite.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
            var camGo=new GameObject("QA mirror camera",typeof(Camera));var cam=camGo.GetComponent<Camera>();camGo.AddComponent<UniversalAdditionalCameraData>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=1.6f;cam.transform.position=new Vector3(0,1.3f,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<31;cam.nearClipPlane=.1f;cam.farClipPlane=20;
            var rt=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);rt.Create();Color32[][] frames=new Color32[2][];
            for(int i=0;i<2;i++){
                sprite.flipX=i==1;yield return new WaitForEndOfFrame();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(128,128,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=old;frames[i]=image.GetPixels32();File.WriteAllBytes(Path.Combine(folder,i==0?"gpu-facing-right.png":"gpu-facing-left.png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            }
            int difference=0,unflipped=0;VisiblePixels=0;for(int y=0;y<128;y++)for(int x=0;x<128;x++){var a=frames[0][y*128+x];var b=frames[1][y*128+127-x];var raw=frames[1][y*128+x];if(a.a>20)VisiblePixels++;difference+=Mathf.Abs(a.a-b.a);unflipped+=Mathf.Abs(a.a-raw.a);}
            MirrorError=difference/(128f*128*255);Passed=VisiblePixels>150&&MirrorError<.008f&&unflipped>255*80;
            File.WriteAllText(Path.Combine(folder,"gpu-mirror.json"),"{\"passed\":"+Passed.ToString().ToLowerInvariant()+",\"visiblePixels\":"+VisiblePixels+",\"mirrorError\":"+MirrorError.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(camGo);UnityEngine.Object.Destroy(go);
        }
    }
}
