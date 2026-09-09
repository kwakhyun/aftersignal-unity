using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class PortraitImporter:AssetPostprocessor
    {
        bool Portrait=>assetPath.Contains("/Resources/Art/Portraits/");
        public override uint GetVersion()=>4;
        void OnPreprocessTexture()
        {
            if(!Portrait)return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.isReadable=true;
            var platform=importer.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.RGBA32;importer.SetPlatformTextureSettings(platform);
        }
        void OnPostprocessTexture(Texture2D image)
        {
            if(!Portrait)return;
            if(assetPath.EndsWith("/SeoDialogue.png"))PortraitAlpha.Apply(image,1,1);else SpriteMatte.Apply(image);
        }
    }
}
