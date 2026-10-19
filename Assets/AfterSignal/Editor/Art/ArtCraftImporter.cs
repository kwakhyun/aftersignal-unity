using UnityEditor;
using UnityEngine;

namespace AfterSignal.Editor
{
    public sealed class ArtCraftImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/AfterSignal/Resources/Art/ArtCraft/UI/", System.StringComparison.Ordinal))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;
        }
    }
}
