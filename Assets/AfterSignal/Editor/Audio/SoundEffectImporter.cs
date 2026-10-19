using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class SoundEffectImporter:AssetPostprocessor
    {
        public override uint GetVersion()=>3;
        void OnPreprocessAudio()
        {
            bool speech=assetPath.Contains("/Audio/Generated/Voice/");
            bool loop=assetPath.Contains("/Audio/Generated/Loops/");
            if(!speech&&!loop&&!assetPath.Contains("/Audio/Generated/SFX/")&&!assetPath.Contains("/Audio/Quality/")&&!assetPath.Contains("/Audio/Firearms/"))return;
            var importer=(AudioImporter)assetImporter;var settings=importer.defaultSampleSettings;
            settings.loadType=speech?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=speech?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
            if(speech)settings.quality=.85f;
            settings.preloadAudioData=true;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=settings;importer.loadInBackground=false;
        }
    }
}
