using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AfterSignal.Editor
{
    public static partial class FidelityBuild
    {
        const string Textures="Assets/AfterSignal/Resources/Materials/Scanned/";
        [MenuItem("AFTERSIGNAL/Quality/Apply physical city materials and rendering")]
        public static void Apply()
        {
            foreach(string path in Directory.GetFiles(Textures,"*.jpg"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)continue;
                importer.textureType=path.Contains("_normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=path.Contains("_albedo");importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            var shader=Shader.Find("AfterSignal/Urban Surface");var glass=Shader.Find("AfterSignal/Architectural Glass");
            int count=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/AfterSignal/Resources/Materials","Assets/AfterSignal/Resources/WorldAssets/Generated"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)continue;string n=m.name.ToLowerInvariant();string scan=null;float density=.25f,wet=0,metal=0;Color color=Color.white;
                if(n=="asphalt"||n=="novaasphalt"||n=="urbanroad"||n=="roadsurface"){scan="asphalt_04";density=.25f;wet=1;color=new Color(.34f,.38f,.40f);}
                else if(n=="pavement"||n=="novapavement"||n=="terminalfloor"||n=="tile"||n=="lighttile"){scan="concrete_tiles_02";density=.55f;wet=.6f;color=new Color(.72f,.78f,.78f);}
                else if(n=="novaconcrete"||n=="concrete"||n=="futureceramic"||n=="ceramicwall"){scan="concrete_wall_006";density=.3f;color=n=="futureceramic"?new Color(.84f,.85f,.8f):new Color(.68f,.71f,.70f);}
                else if(n=="futurecarbon"||n=="novasteel"||n=="novasilver"||n=="novafacade"){scan="blue_metal_plate";density=.3f;metal=.55f;color=n=="futurecarbon"?new Color(.29f,.38f,.43f):new Color(.56f,.62f,.65f);}
                else if(n.StartsWith("civicfacade")){scan="concrete_wall_006";density=.3f;color=m.GetColor("_BaseColor");}
                if(scan!=null)
                {
                    m.shader=shader;m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+scan+"_albedo.jpg"));m.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+scan+"_normal.jpg"));m.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+scan+"_arm.jpg"));m.SetColor("_BaseColor",color);m.SetFloat("_Density",density);m.SetFloat("_UsePbrMaps",1);m.SetFloat("_NormalStrength",.48f);m.SetFloat("_Metallic",metal);m.SetFloat("_RainResponse",wet);m.enableInstancing=true;EditorUtility.SetDirty(m);count++;
                }
                else if(n=="novawindow"||n=="futureglass"||n=="window"||n=="officewindow"||n=="districtwindow")
                {m.shader=glass;m.SetColor("_BaseColor",new Color(.16f,.24f,.28f));m.SetColor("_EmissionColor",new Color(1,.68f,.32f));m.SetFloat("_Smoothness",.84f);m.SetVector("_WindowScale",new Vector4(2.8f,4,0,0));m.enableInstancing=true;EditorUtility.SetDirty(m);count++;}
            }
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");pipeline.msaaSampleCount=4;pipeline.shadowDistance=140;pipeline.shadowCascadeCount=4;pipeline.cascade4Split=new Vector3(.10f,.27f,.55f);pipeline.mainLightShadowmapResolution=4096;pipeline.shadowDepthBias=.65f;pipeline.shadowNormalBias=.35f;pipeline.supportsHDR=true;pipeline.renderScale=1;EditorUtility.SetDirty(pipeline);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            foreach(var feature in renderer.rendererFeatures)
            {if(feature.GetType().Name!="ScreenSpaceAmbientOcclusion")continue;var so=new SerializedObject(feature);so.FindProperty("m_Settings.Source").enumValueIndex=0;so.FindProperty("m_Settings.AfterOpaque").boolValue=true;so.FindProperty("m_Settings.Intensity").floatValue=.65f;so.FindProperty("m_Settings.Radius").floatValue=.55f;so.FindProperty("m_Settings.DirectLightingStrength").floatValue=.08f;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(feature);}
            BuildBotanicalPrefab();
            AssetDatabase.SaveAssets();Debug.Log("FIDELITY physical materials updated: "+count);
        }
        public static void ApplyAndBuild(){Apply();ProjectBuilder.BuildRelease();}
    }
}
