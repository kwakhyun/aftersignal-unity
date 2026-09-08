using System;using System.IO;using UnityEditor;using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class FacilitySpriteImporter:AssetPostprocessor
    {
        bool Match=>assetPath.Contains("/Art/FacilityCitizens/");
        public override uint GetVersion()=>3;
        static bool Background(Color32 p)=>p.a<20||Mathf.Min(p.r,Mathf.Min(p.g,p.b))>223&&Mathf.Max(p.r,Mathf.Max(p.g,p.b))-Mathf.Min(p.r,Mathf.Min(p.g,p.b))<17;
        void OnPreprocessTexture()
        {
            if(!Match)return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Multiple;t.mipmapEnabled=false;t.filterMode=FilterMode.Point;t.textureCompression=TextureImporterCompression.Uncompressed;t.alphaIsTransparency=true;t.isReadable=true;t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;
            var platform=t.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.RGBA32;t.SetPlatformTextureSettings(platform);
            var temp=new Texture2D(2,2);temp.LoadImage(File.ReadAllBytes(assetPath));var p=temp.GetPixels32();int w=temp.width/4,h=temp.height/4;var sprites=new SpriteMetaData[16];float height=0;
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
            {
                int lo=h,hi=0;float center=0;int samples=0;int ox=col*w,oy=(3-row)*h;
                for(int y=3;y<h-3;y++)for(int x=3;x<w-3;x++)if(!Background(p[(oy+y)*temp.width+ox+x])){lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);if(y>h*.45f&&y<h*.8f){center+=x;samples++;}}
                height+=Mathf.Max(1,hi-lo)/16f;sprites[row*4+col]=new SpriteMetaData{name=Path.GetFileNameWithoutExtension(assetPath)+"-"+(row*4+col).ToString("00"),rect=new Rect(ox,oy,w,h),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(samples>0?center/samples/w:.5f,Mathf.Max(2,lo)/(float)h)};
            }
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spritePixelsPerUnit=height/2.02f;settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;t.SetTextureSettings(settings);
#pragma warning disable CS0618
            t.spritesheet=sprites;
#pragma warning restore CS0618
            UnityEngine.Object.DestroyImmediate(temp);
        }
        void OnPostprocessTexture(Texture2D texture)
        {
            if(!Match)return;var p=texture.GetPixels32();var seen=new bool[p.Length];var q=new int[p.Length];int a=0,b=0,w=texture.width,h=texture.height;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if((x==0||y==0||x==w-1||y==h-1)&&Background(p[y*w+x])){seen[y*w+x]=true;q[b++]=y*w+x;}
            while(a<b){int k=q[a++],x=k%w,y=k/w;p[k].a=0;for(int d=0;d<4;d++){int nx=x+(d==0?-1:d==1?1:0),ny=y+(d==2?-1:d==3?1:0);if(nx<0||nx>=w||ny<0||ny>=h)continue;int n=ny*w+nx;if(seen[n]||!Background(p[n]))continue;seen[n]=true;q[b++]=n;}}
            texture.SetPixels32(p);texture.Apply(false,false);
        }
    }
}
