using System.IO;
using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class StorySpriteImporter:AssetPostprocessor
    {
        bool Match=>assetPath.Contains("/Art/StorySprites/");
        public override uint GetVersion()=>1;
        static bool Matte(Color32 p)=>p.a<20||p.g>75&&p.g>Mathf.Max(p.r,p.b)*1.35f&&p.g-Mathf.Max(p.r,p.b)>24;
        static void Clean(Texture2D tex){var p=tex.GetPixels32();for(int i=0;i<p.Length;i++)if(Matte(p[i]))p[i]=new Color32(0,0,0,0);SpriteSilhouetteFinish.Apply(p,tex.width,tex.height);tex.SetPixels32(p);tex.Apply();}
        void OnPreprocessTexture()
        {
            if(!Match)return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Multiple;t.mipmapEnabled=false;t.filterMode=FilterMode.Point;t.alphaIsTransparency=true;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=4096;t.npotScale=TextureImporterNPOTScale.None;
            var platform=t.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.RGBA32;t.SetPlatformTextureSettings(platform);
            var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(assetPath));Clean(tex);var pixels=tex.GetPixels32();var xs=Cuts(tex,pixels,4,true);var ys=Cuts(tex,pixels,8,false);var frames=new SpriteMetaData[32];float height=0;
            for(int i=0;i<32;i++)
            {
                int x=xs[i%4],y=ys[7-i/4],w=xs[i%4+1]-x,h=ys[8-i/4]-y,left=w,right=0,bottom=h,top=0;
                for(int yy=0;yy<h;yy++)for(int xx=0;xx<w;xx++)if(pixels[(y+yy)*tex.width+x+xx].a>30){left=Mathf.Min(left,xx);right=Mathf.Max(right,xx);bottom=Mathf.Min(bottom,yy);top=Mathf.Max(top,yy);}
                float center=0,count=0;for(int yy=bottom+(top-bottom)*55/100;yy<bottom+(top-bottom)*85/100;yy++)for(int xx=left;xx<=right;xx++)if(pixels[(y+yy)*tex.width+x+xx].a>30){center+=xx;count++;}
                if(i%4==0)height+=Mathf.Max(1,top-bottom)/8f;
                frames[i]=new SpriteMetaData{name=Path.GetFileNameWithoutExtension(assetPath)+"-"+i.ToString("00"),rect=new Rect(x,y,w,h),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(count>0?center/count/w:.5f,Mathf.Max(1,bottom)/(float)h)};
            }
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spritePixelsPerUnit=height/2.06f;settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;t.SetTextureSettings(settings);
#pragma warning disable CS0618
            t.spritesheet=frames;
#pragma warning restore CS0618
            Object.DestroyImmediate(tex);
        }
        static int[] Cuts(Texture2D tex,Color32[] pixels,int count,bool horizontal)
        {
            int length=horizontal?tex.width:tex.height,other=horizontal?tex.height:tex.width;var cuts=new int[count+1];cuts[count]=length;
            for(int i=1;i<count;i++){int expected=length*i/count,best=expected;float score=float.MaxValue;for(int n=expected-length/count/5;n<=expected+length/count/5;n++){int ink=0;for(int q=0;q<other;q++)if(pixels[horizontal?q*tex.width+n:n*tex.width+q].a>30)ink++;float rank=ink*1000+Mathf.Abs(n-expected);if(rank<score){score=rank;best=n;}}cuts[i]=best;}return cuts;
        }
        void OnPostprocessTexture(Texture2D tex){if(Match)Clean(tex);}
    }
}
