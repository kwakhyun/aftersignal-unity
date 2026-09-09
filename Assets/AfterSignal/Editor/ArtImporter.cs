using UnityEngine;
using UnityEditor;
namespace AfterSignal.Editor
{
    public sealed class ArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(assetPath.Contains("/StoryCast/")||assetPath.Contains("/ResponseCrew/"))return;
            if(assetPath.Contains("/Art/Medical/")||assetPath.Contains("/CivicForces/")||assetPath.Contains("/CyberSecurity/")||assetPath.Contains("/SeoMotion/")||assetPath.Contains("/NpcDirections/")||assetPath.Contains("/NpcPolished/")||assetPath.Contains("/SeoSwimming/")||assetPath.Contains("/FacilityCitizens/"))return;
            if(!assetPath.Contains("/Resources/Art/"))return;
            if(assetPath.Contains("/LawEnforcement/")||assetPath.Contains("/Gangs/")||assetPath.Contains("/SeoIllustrated/")||assetPath.Contains("/Portraits/"))return;
            var importer=(TextureImporter)assetImporter;
            if(assetPath.Contains("/Title/"))
            {
                importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Bilinear;
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=false;
                importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.isReadable=false;return;
            }
            if(assetPath.Contains("/ShopItems/"))
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=512;
                var iconSettings=new TextureImporterSettings();importer.ReadTextureSettings(iconSettings);
                iconSettings.spriteAlignment=(int)SpriteAlignment.Center;iconSettings.spritePivot=new Vector2(.5f,.5f);iconSettings.spriteMeshType=SpriteMeshType.FullRect;
                importer.SetTextureSettings(iconSettings);return;
            }
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
