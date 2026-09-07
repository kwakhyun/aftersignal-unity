using UnityEngine;
using UnityEditor;
namespace AfterSignal.Editor
{
    public sealed class ArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/Art/"))return;
            var importer=(TextureImporter)assetImporter;
            if(assetPath.Contains("/Urban/")){importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;importer.wrapMode=assetPath.Contains("Surface-")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.alphaIsTransparency=false;importer.anisoLevel=4;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;return;}
            if(assetPath.Contains("/Environment/")){importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Compressed;return;}
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spriteMeshType=SpriteMeshType.FullRect;
            bool hero=assetPath.Contains("/Hero/")||assetPath.Contains("/NPC/Civic/"),npc=assetPath.Contains("/NPC/");
            bool expanded=assetPath.Contains("/Actions/")||assetPath.Contains("/Depth/")||assetPath.Contains("/NPC/Civic/");
            settings.spritePivot=expanded?new Vector2(.5f,16/288f):hero?new Vector2(.5f,12/224f):new Vector2(.5f,npc?.065f:12/224f);settings.spritePixelsPerUnit=hero?49f:npc?38f:53f;importer.SetTextureSettings(settings);
        }
    }
}
