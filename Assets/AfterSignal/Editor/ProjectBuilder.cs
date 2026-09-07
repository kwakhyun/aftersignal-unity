using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        const string resourceRoot="Assets/AfterSignal/Resources/";
        [MenuItem("AFTERSIGNAL/Build authored scenes and settings")]
        public static void CreateProject()
        {
            Directory.CreateDirectory("Assets/AfterSignal/Scenes");Directory.CreateDirectory(resourceRoot+"Materials");Directory.CreateDirectory("Assets/AfterSignal/Prefabs");Directory.CreateDirectory("Artifacts");
            PlayerSettings.companyName="AfterSignal Studio";PlayerSettings.productName="AFTERSIGNAL Night Line";PlayerSettings.bundleVersion="1.5.0";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null)input.intValue=2;settings.ApplyModifiedPropertiesWithoutUndo();
            var urp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");urp.msaaSampleCount=4;urp.shadowDistance=65;urp.renderScale=1;urp.supportsHDR=true;
            GraphicsSettings.defaultRenderPipeline=urp;QualitySettings.renderPipeline=urp;QualitySettings.vSyncCount=1;QualitySettings.shadows=UnityEngine.ShadowQuality.All;
            var quality=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);var levels=quality.FindProperty("m_QualitySettings");for(int i=0;i<levels.arraySize;i++){var prop=levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline");if(prop!=null)prop.objectReferenceValue=urp;}quality.ApplyModifiedPropertiesWithoutUndo();
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layers.GetArrayElementAtIndex(8).stringValue="Player";layers.GetArrayElementAtIndex(9).stringValue="Enemy";layers.GetArrayElementAtIndex(10).stringValue="Grapple";tags.ApplyModifiedPropertiesWithoutUndo();
            Physics.IgnoreLayerCollision(8,9,true);Physics.IgnoreLayerCollision(9,9,true);
            CreateMaterials();CreateControls();
            var tuning=AssetDatabase.LoadAssetAtPath<GameTuning>(resourceRoot+"GameTuning.asset");if(!tuning){tuning=ScriptableObject.CreateInstance<GameTuning>();AssetDatabase.CreateAsset(tuning,resourceRoot+"GameTuning.asset");}
            foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{resourceRoot+"Art"}))AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(path),ImportAssetOptions.ForceUpdate);
            DistrictMaterials();InteriorMaterials();CityMaterials();UrbanMaterials();for(int i=0;i<CampaignRules.Scenes.Length;i++)BuildScene((StageId)i,tuning);
            var buildScenes=new EditorBuildSettingsScene[CampaignRules.Scenes.Length];for(int i=0;i<buildScenes.Length;i++)buildScenes[i]=new EditorBuildSettingsScene("Assets/AfterSignal/Scenes/"+CampaignRules.Scenes[i]+".unity",true);EditorBuildSettings.scenes=buildScenes;
            EditorSceneManager.OpenScene(buildScenes[0].path);AssetDatabase.SaveAssets();ApplyQualityFoundation();ApplyPresentation();
            CityMaterials();
            for(int i=0;i<23;i++){var scene=EditorSceneManager.OpenScene(buildScenes[i].path);world=GameObject.Find("WORLD / editable architecture").transform;var game=UnityEngine.Object.FindAnyObjectByType<GameDirector>();InstallBackdrop((StageId)i,game.stageLength);NeonArchitecture((StageId)i,game.stageLength);if(i>=3)PolishDistrict((StageId)i);if(i==3)InstallUrbanHaven();ReplaceSignImages();EditorSceneManager.SaveScene(scene);}
            OptimizeAllScenes();EditorSceneManager.OpenScene(buildScenes[0].path);Debug.Log("AFTERSIGNAL: 25 URP scenes, residence, civic interiors and optimized geometry configured.");
        }
        [MenuItem("AFTERSIGNAL/Build Windows player")]
        public static void BuildWindows()
        {
            BuildWindowsPlayer(BuildOptions.Development);
        }
        [MenuItem("AFTERSIGNAL/Build Windows release player")]
        public static void BuildRelease(){BuildWindowsPlayer(BuildOptions.None);}
        static void BuildWindowsPlayer(BuildOptions mode)
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});PlayerSettings.useFlipModelSwapchain=false;
            if(EditorBuildSettings.scenes.Length<4)CreateProject();Directory.CreateDirectory("Builds/Windows");Directory.CreateDirectory("Artifacts");PlayerSettings.bundleVersion="1.6.0";AssetDatabase.SaveAssets();
            var options=new BuildPlayerOptions {scenes=Array.ConvertAll(EditorBuildSettings.scenes,s=>s.path),locationPathName="Builds/Windows/AFTERSIGNAL.exe",target=BuildTarget.StandaloneWindows64,options=mode|BuildOptions.CleanBuildCache};
            var report=BuildPipeline.BuildPlayer(options);File.WriteAllText("Artifacts/build-result.json",JsonUtility.ToJson(new BuildResult {result=report.summary.result.ToString(),bytes=report.summary.totalSize,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        }
        public static void RebuildAndBuild(){CreateProject();BuildWindows();}
        [Serializable] class BuildResult{public string result;public ulong bytes;public int errors,warnings;}
        static void CreateControls()
        {
            var asset=ScriptableObject.CreateInstance<InputActionAsset>();var map=asset.AddActionMap("Gameplay");var move=map.AddAction("Move",InputActionType.Value,expectedControlLayout:"Vector2");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");map.AddAction("Point",InputActionType.PassThrough,"<Pointer>/position",expectedControlLayout:"Vector2");
            string[] names={"Attack","Grapple","Jump","Interact","Dash","Skill","Guard","Pause","Katana","Greatsword","Pistol"};string[] keys={"<Mouse>/leftButton","<Mouse>/rightButton","<Keyboard>/space","<Keyboard>/e","<Keyboard>/leftShift","<Keyboard>/q","<Keyboard>/leftCtrl","<Keyboard>/escape","<Keyboard>/1","<Keyboard>/2","<Keyboard>/3"};
            for(int i=0;i<names.Length;i++)map.AddAction(names[i],InputActionType.Button,keys[i]);File.WriteAllText(resourceRoot+"Controls.inputactions",asset.ToJson());UnityEngine.Object.DestroyImmediate(asset);AssetDatabase.ImportAsset(resourceRoot+"Controls.inputactions");
        }
        static Material CreateMaterial(string name,string hex,float metallic=0,float smoothness=.5f,float emission=0)
        {
            string path=resourceRoot+"Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            ColorUtility.TryParseHtmlString(hex,out var color);material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);if(emission>0){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*emission);}return material;
        }
        static void CreateMaterials()
        {
            CreateMaterial("Concrete","#25343e",.1f,.32f);CreateMaterial("Tile","#506874",.24f,.63f);CreateMaterial("LightTile","#a5b0a9",.05f,.58f);CreateMaterial("Metal","#253d4e",.78f,.76f);CreateMaterial("DarkMetal","#0d1b29",.64f,.66f);CreateMaterial("Chrome","#839b9b",.9f,.84f);CreateMaterial("Gold","#c39655",.65f,.67f);CreateMaterial("Seat","#426a70",.15f,.36f);CreateMaterial("Rubber","#081217",.08f,.25f);CreateMaterial("Cyan","#2cdbd3",.25f,.6f,2.2f);CreateMaterial("Amber","#efae5e",.1f,.5f,1.8f);CreateMaterial("Pink","#dd518e",.1f,.5f,1.6f);CreateMaterial("Window","#607b87",.6f,.92f);CreateMaterial("Building","#101d31",.45f,.5f);CreateMaterial("Leaf","#386562",0,.36f);
            foreach(var c in new[]{("CyanFX","#47e6ee"),("GoldFX","#ffc16b"),("RedFX","#ff4567")}){var mat=CreateMaterial(c.Item1,c.Item2,0,.4f,3);mat.shader=Shader.Find("Universal Render Pipeline/Unlit");ColorUtility.TryParseHtmlString(c.Item2,out var color);mat.SetColor("_BaseColor",color*2.2f);mat.SetFloat("_Cull",0);}
            var glass=CreateMaterial("Glass","#54919b",.35f,.92f);glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetColor("_BaseColor",new Color(.32f,.7f,.78f,.24f));glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;glass.SetFloat("_Cull",0);
            string actorPath=resourceRoot+"Materials/PixelActor.mat";if(!AssetDatabase.LoadAssetAtPath<Material>(actorPath))AssetDatabase.CreateAsset(new Material(Shader.Find("AfterSignal/Pixel Actor URP")),actorPath);
            var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);var random=new System.Random(24);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float value=.77f+(float)random.NextDouble()*.2f;if(x%64==0||y%32==0)value*=.55f;tex.SetPixel(x,y,new Color(value,value,value));}tex.Apply();File.WriteAllBytes(resourceRoot+"Materials/SurfaceGrain.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(resourceRoot+"Materials/SurfaceGrain.png");var grain=AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot+"Materials/SurfaceGrain.png");foreach(string n in new[]{"Tile","Concrete","LightTile"}){var mat=Mat(n);mat.SetTexture("_BaseMap",grain);mat.SetTextureScale("_BaseMap",new Vector2(4,2));}
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(resourceRoot+"Materials/"+name+".mat");
    }
}
