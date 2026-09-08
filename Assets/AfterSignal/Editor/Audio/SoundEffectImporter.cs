using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class SoundEffectImporter:AssetPostprocessor
    {
        public override uint GetVersion()=>1;
        void OnPreprocessAudio()
        {
            if(!assetPath.Contains("/Audio/Quality/")&&!assetPath.Contains("/Audio/Firearms/"))return;
            var importer=(AudioImporter)assetImporter;var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.preloadAudioData=true;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=settings;importer.loadInBackground=false;
        }
    }
}
