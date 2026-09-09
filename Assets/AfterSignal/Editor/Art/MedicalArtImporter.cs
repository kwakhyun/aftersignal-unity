using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class MedicalArtImporter:AssetPostprocessor
    {
        const string PathName="Assets/AfterSignal/Resources/Art/Medical/InjuryLayers.png";
        public static void BuildResponseRelease()
        {
            var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes("Documentation/ResponseRenewal/SourceArt/InjuryLayers.png"));
            var pixels=RemoveMatte(t);var rgba=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false);rgba.SetPixels32(pixels);rgba.Apply();File.WriteAllBytes(PathName,rgba.EncodeToPNG());
            Object.DestroyImmediate(t);Object.DestroyImmediate(rgba);AssetDatabase.ImportAsset(PathName,ImportAssetOptions.ForceUpdate);AssetDatabase.Refresh();ProjectBuilder.BuildRelease();
        }
        static Color32[] RemoveMatte(Texture2D t)
        {
            var p=t.GetPixels32();int w=t.width,h=t.height;var seen=new bool[p.Length];var queue=new Queue<int>();
            void Add(int i){if(i<0||i>=p.Length||seen[i])return;var c=p[i];if(c.a>10&&(Mathf.Min(c.r,Mathf.Min(c.g,c.b))<155||Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))>24))return;seen[i]=true;queue.Enqueue(i);}
            for(int col=0;col<=4;col++){int x=Mathf.Min(w-1,col*w/4);for(int y=0;y<h;y++)Add(y*w+x);}
            for(int row=0;row<=2;row++){int y=Mathf.Min(h-1,row*h/2);for(int x=0;x<w;x++)Add(y*w+x);}
            while(queue.Count>0){int i=queue.Dequeue();p[i].a=0;int x=i%w,y=i/w;if(x>0)Add(i-1);if(x<w-1)Add(i+1);if(y>0)Add(i-w);if(y<h-1)Add(i+w);}return p;
        }
        public override int GetPostprocessOrder()=>150;
        void OnPreprocessTexture()
        {
            if(assetPath!=PathName)return;var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(assetPath));var pixels=RemoveMatte(t);var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;
            var slices=new SpriteMetaData[8];
            for(int i=0;i<8;i++)
            {
                int x0=i%4*t.width/4,x1=(i%4+1)*t.width/4,y0=(1-i/4)*t.height/2,y1=(2-i/4)*t.height/2,loX=x1,hiX=x0,loY=y1,hiY=y0;
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*t.width+x].a>100){loX=Mathf.Min(loX,x);hiX=Mathf.Max(hiX,x);loY=Mathf.Min(loY,y);hiY=Mathf.Max(hiY,y);}
                slices[i]=new SpriteMetaData{name="InjuryLayers-"+i.ToString("00"),rect=Rect.MinMaxRect(loX,loY,hiX+1,hiY+1),alignment=0,pivot=Vector2.one*.5f};
            }
            importer.spritePixelsPerUnit=200;
#pragma warning disable CS0618
            importer.spritesheet=slices;
#pragma warning restore CS0618
            Object.DestroyImmediate(t);
        }
    }
}
