using System.IO;
using UnityEngine;
using UnityEditor;
namespace AfterSignal.Editor
{
    public sealed class SwimmingSpriteImporter:AssetPostprocessor
    {
        bool Match=>assetPath.Contains("/Art/SeoSwimming/");public override uint GetVersion()=>2;
        static bool Blank(Color32 c)=>c.a<16||Mathf.Min(c.r,Mathf.Min(c.g,c.b))>220&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<20;
        void OnPreprocessTexture()
        {
            if(!Match)return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Multiple;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;t.isReadable=true;t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;
            // RGB source sheets acquire their alpha mask below, so the imported texture must retain it.
            var platform=t.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.RGBA32;t.SetPlatformTextureSettings(platform);
            var temp=new Texture2D(2,2);temp.LoadImage(File.ReadAllBytes(assetPath));int w=temp.width/4,h=temp.height/2;var pixels=temp.GetPixels32();var frames=new SpriteMetaData[8];float extent=0;
            for(int i=0;i<8;i++)
            {
                int ox=i%4*w,oy=(1-i/4)*h;int minX=w,minY=h,maxX=0,maxY=0;
                for(int y=2;y<h-2;y++)for(int x=2;x<w-2;x++)if(!Blank(pixels[(oy+y)*temp.width+ox+x])){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                extent+=Mathf.Max(maxX-minX,maxY-minY)/8f;
                frames[i]=new SpriteMetaData{name=Path.GetFileNameWithoutExtension(assetPath)+"-"+i.ToString("00"),rect=new Rect(ox,oy,w,h),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2((minX+maxX)*.5f/w,(minY+maxY)*.5f/h)};
            }
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spritePixelsPerUnit=extent/(assetPath.Contains("Tread")?1.65f:2.5f);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;t.SetTextureSettings(settings);
#pragma warning disable CS0618
            t.spritesheet=frames;
#pragma warning restore CS0618
            Object.DestroyImmediate(temp);
        }
        void OnPostprocessTexture(Texture2D texture)
        {
            if(!Match)return;var p=texture.GetPixels32();int w=texture.width,h=texture.height;var seen=new bool[p.Length];var queue=new int[p.Length];int begin=0,end=0;
            // Flood from every cell perimeter, preserving all enclosed hair/clothing pixels.
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(x%(w/4)==0||y%(h/2)==0||x==w-1||y==h-1){int n=y*w+x;if(!seen[n]&&Blank(p[n])){seen[n]=true;queue[end++]=n;}}
            while(begin<end){int k=queue[begin++],x=k%w,y=k/w;p[k].a=0;for(int d=0;d<4;d++){int xx=x+(d==0?-1:d==1?1:0),yy=y+(d==2?-1:d==3?1:0);if(xx<0||xx>=w||yy<0||yy>=h)continue;int n=yy*w+xx;if(!seen[n]&&Blank(p[n])){seen[n]=true;queue[end++]=n;}}}
            texture.SetPixels32(p);texture.Apply(false,false);
        }
    }
}
