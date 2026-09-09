using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class StoryCastImporter:AssetPostprocessor
    {
        bool Match=>assetPath.Contains("/Art/StoryCast/")||assetPath.Contains("/Art/ResponseCrew/");bool Crew=>assetPath.Contains("/ResponseCrew/");
        public override uint GetVersion()=>2;
        void OnPreprocessTexture()
        {
            if(!Match)return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Multiple;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.isReadable=true;t.filterMode=Crew?FilterMode.Point:FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;
            var platform=t.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.RGBA32;t.SetPlatformTextureSettings(platform);var temp=new Texture2D(2,2);temp.LoadImage(System.IO.File.ReadAllBytes(assetPath));int rows=Crew?4:assetPath.Contains("CoreCast")?2:3,w=temp.width/4,h=temp.height/rows;var frames=new SpriteMetaData[rows*4];
            for(int i=0;i<frames.Length;i++)frames[i]=new SpriteMetaData{name=System.IO.Path.GetFileNameWithoutExtension(assetPath)+"-"+i.ToString("00"),rect=new Rect(i%4*w+3,(rows-1-i/4)*h+3,w-6,h-6),alignment=(int)SpriteAlignment.Custom,pivot=Crew?new Vector2(.5f,.045f):new Vector2(.5f,0)};
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spritePixelsPerUnit=Crew?h/2.3f:100;settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;t.SetTextureSettings(settings);
#pragma warning disable CS0618
            t.spritesheet=frames;
#pragma warning restore CS0618
            Object.DestroyImmediate(temp);
        }
        void OnPostprocessTexture(Texture2D image){if(Match&&Crew)SpriteMatte.Apply(image);else if(Match)PortraitAlpha.Apply(image,4,assetPath.Contains("CoreCast")?2:3);}
    }
}
