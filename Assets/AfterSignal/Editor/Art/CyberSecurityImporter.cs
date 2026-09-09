using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    // Keep original generated art intact; normalize its matte and foot pivots at import.
    public sealed class CyberSecurityImporter:AssetPostprocessor
    {
        bool Sheet=>(assetPath.Contains("/Art/CyberSecurity/")||assetPath.Contains("/Art/CivicForces/"))&&assetPath.EndsWith(".png");
        public static void BuildLivingHarborRelease(){PrepareForces();ProjectBuilder.BuildRelease();}
        public static void PrepareForces()
        {
            foreach(string path in Directory.GetFiles("Assets/AfterSignal/Resources/Art/CivicForces","*.png"))
            {
                string original=Path.Combine("Documentation/LivingHarbor/SourceArt",Path.GetFileName(path));var input=new Texture2D(2,2);input.LoadImage(File.ReadAllBytes(original));var rgba=new Texture2D(input.width,input.height,TextureFormat.RGBA32,false);rgba.SetPixels32(Matte(input));rgba.Apply();File.WriteAllBytes(path,rgba.EncodeToPNG());UnityEngine.Object.DestroyImmediate(input);UnityEngine.Object.DestroyImmediate(rgba);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
        }
        public override uint GetVersion()=>5;
        public override int GetPostprocessOrder()=>100;
        public static void BuildCyberConflictRelease()
        {
            string source="Documentation/CyberConflict/SourceArt";Directory.CreateDirectory(source);
            foreach(string path in Directory.GetFiles("Assets/AfterSignal/Resources/Art/CyberSecurity","*.png"))
            {
                string original=Path.Combine(source,Path.GetFileName(path));if(!File.Exists(original))File.Copy(path,original);
                var input=new Texture2D(2,2);input.LoadImage(File.ReadAllBytes(original));
                var rgba=new Texture2D(input.width,input.height,TextureFormat.RGBA32,false);rgba.SetPixels32(Matte(input));rgba.Apply();
                File.WriteAllBytes(path,rgba.EncodeToPNG());UnityEngine.Object.DestroyImmediate(input);UnityEngine.Object.DestroyImmediate(rgba);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();ProjectBuilder.BuildRelease();
        }
        static Color32[] Matte(Texture2D texture)
        {
            var p=texture.GetPixels32();int w=texture.width,h=texture.height;var queue=new Queue<int>();var seen=new bool[p.Length];
            bool Background(Color32 c)=>c.a<10||Mathf.Min(c.r,Mathf.Min(c.g,c.b))>153&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<25;
            void Add(int i){if(i<0||i>=p.Length||seen[i]||!Background(p[i]))return;seen[i]=true;queue.Enqueue(i);}
            for(int row=0;row<=4;row++){int y=Mathf.Min(h-1,row*h/4);for(int x=0;x<w;x++)Add(y*w+x);}
            for(int col=0;col<=4;col++){int x=Mathf.Min(w-1,col*w/4);for(int y=0;y<h;y++)Add(y*w+x);}
            while(queue.Count>0){int i=queue.Dequeue();p[i].a=0;int x=i%w,y=i/w;if(x>0)Add(i-1);if(x<w-1)Add(i+1);if(y>0)Add(i-w);if(y<h-1)Add(i+w);}return p;
        }
        void OnPreprocessTexture()
        {
            if(!Sheet)return;var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(assetPath));var p=Matte(t);SpriteSilhouetteFinish.Apply(p,t.width,t.height);var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;
            var sprites=new SpriteMetaData[16];float largest=0;string name=Path.GetFileNameWithoutExtension(assetPath);
            for(int i=0;i<16;i++)
            {
                int x0=i%4*t.width/4,x1=(i%4+1)*t.width/4,y0=(3-i/4)*t.height/4,y1=(4-i/4)*t.height/4;
                int loX=x1,hiX=x0,loY=y1,hiY=y0;for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(p[y*t.width+x].a>100){loX=Mathf.Min(x,loX);hiX=Mathf.Max(x,hiX);loY=Mathf.Min(y,loY);hiY=Mathf.Max(y,hiY);}
                largest=Mathf.Max(largest,hiY-loY+1);var r=Rect.MinMaxRect(Mathf.Max(x0,loX-2),Mathf.Max(y0,loY-2),Mathf.Min(x1,hiX+3),Mathf.Min(y1,hiY+3));
                float feet=0;int n=0;for(int y=loY;y<Mathf.Min(loY+14,hiY);y++)for(int x=loX;x<=hiX;x++)if(p[y*t.width+x].a>100){feet+=x;n++;}
                sprites[i]=new SpriteMetaData{name=name+"-"+i.ToString("00"),rect=r,alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(((n>0?feet/n:(loX+hiX)*.5f)-r.x)/r.width,(loY-r.y)/r.height)};
            }
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spritePixelsPerUnit=largest/2.12f;settings.spriteGenerateFallbackPhysicsShape=false;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet=sprites;
#pragma warning restore CS0618
            UnityEngine.Object.DestroyImmediate(t);
        }
        void OnPostprocessTexture(Texture2D t){if(Sheet){var p=Matte(t);SpriteSilhouetteFinish.Apply(p,t.width,t.height);t.SetPixels32(p);t.Apply(false,false);}}
    }
}
