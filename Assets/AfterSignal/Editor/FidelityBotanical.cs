using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterSignal.Editor
{
    public static partial class FidelityBuild
    {
        static void BuildBotanicalPrefab()
        {
            const string folder="Assets/AfterSignal/Resources/WorldAssets/Botanical/";
            if(!Directory.Exists(folder))return;
            foreach(var path in Directory.GetFiles(folder))
            {
                if(AssetImporter.GetAtPath(path) is not TextureImporter importer)continue;
                importer.textureType=path.Contains("_normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=path.Contains("_albedo");importer.mipmapEnabled=true;importer.anisoLevel=4;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            var materials=new Material[3];string[] parts={"branch","leaves","trunk"};
            for(int i=0;i<3;i++)
            {
                string part=parts[i],path=folder+part+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
                m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+part+"_albedo.jpg"));m.SetColor("_BaseColor",Color.white);
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+part+"_normal.jpg"));m.SetFloat("_BumpScale",.65f);m.EnableKeyword("_NORMALMAP");
                var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+part+"_mask.png");m.SetTexture("_MetallicGlossMap",mask);m.SetTexture("_OcclusionMap",mask);m.SetFloat("_Smoothness",.65f);m.SetFloat("_OcclusionStrength",.65f);m.EnableKeyword("_METALLICSPECGLOSSMAP");m.EnableKeyword("_OCCLUSIONMAP");m.SetFloat("_Cull",i==1?0:2);m.enableInstancing=true;EditorUtility.SetDirty(m);materials[i]=m;
            }
            var root=new GameObject("BotanicalTree");var lods=new LOD[3];float[] transitions={.10f,.035f,.012f};
            try
            {
                for(int i=0;i<3;i++)
                {
                    var asset=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"BotanicalTree_LOD"+i+".fbx");
                    if(!asset)throw new System.InvalidOperationException("Missing botanical mesh LOD "+i);
                    var model=Object.Instantiate(asset,root.transform);model.name="LOD "+i;model.transform.localPosition=Vector3.zero;
                    var renderers=model.GetComponentsInChildren<Renderer>();
                    foreach(var renderer in renderers)
                    {
                        var slots=renderer.sharedMaterials;
                        for(int s=0;s<slots.Length;s++){string name=slots[s]?slots[s].name:"trunk";slots[s]=materials[name.Contains("leaves")?1:name.Contains("branches")?0:2];}
                        renderer.sharedMaterials=slots;renderer.shadowCastingMode=i==2?ShadowCastingMode.Off:ShadowCastingMode.On;
                    }
                    lods[i]=new LOD(transitions[i],renderers){fadeTransitionWidth=.15f};
                }
                var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root,folder+"BotanicalTree.prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
