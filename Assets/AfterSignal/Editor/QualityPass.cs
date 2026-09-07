using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        [MenuItem("AFTERSIGNAL/Quality/Apply station foundation (preserve layout)")]
        public static void ApplyQualityFoundation()
        {
            var scene=EditorSceneManager.OpenScene("Assets/AfterSignal/Scenes/01_NeonStation.unity");
            Undo.SetCurrentGroupName("AFTERSIGNAL station quality foundation");
            world=GameObject.Find("WORLD / editable architecture").transform;
            CreateMaterial("CeramicWall","#777971",.02f,.29f);CreateMaterial("WornSteel","#2a353b",.3f,.36f);CreateMaterial("RoutePaint","#376d70",.12f,.3f);CreateMaterial("WarmWindow","#bdb39c",0,.28f,.15f);
            var shadow=AssetDatabase.LoadAssetAtPath<Material>(resourceRoot+"Materials/ContactShadow.mat");if(!shadow){shadow=new Material(Shader.Find("AfterSignal/Contact Shadow"));AssetDatabase.CreateAsset(shadow,resourceRoot+"Materials/ContactShadow.mat");}
            foreach(string n in new[]{"Tile","Concrete","Metal","LightTile"}){var mat=Mat(n);Undo.RecordObject(mat,"Restrained material response");mat.SetFloat("_Smoothness",n=="Metal"?.42f:.3f);mat.SetFloat("_Metallic",n=="Metal"?.38f:.08f);EditorUtility.SetDirty(mat);}
            var objects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach(var t in objects){
                var r=t.GetComponent<Renderer>();
                if(r&&(t.name=="Ceramic wall panel"||t.name=="Tunnel rear wall"))r.sharedMaterial=Mat("CeramicWall");
                if(r&&t.name=="Turquoise route strip")r.sharedMaterial=Mat("RoutePaint");
                if(t.name=="Ceramic wall panel"){t.position=new Vector3(t.position.x,6.1f,t.position.z);t.localScale=new Vector3(t.localScale.x,9.6f,t.localScale.z);}
                if(t.name.StartsWith("Sign / 中"))continue;
                if(t.name.StartsWith("Sign / 중앙역")){t.localScale=new Vector3(6.5f,.9f,t.localScale.z);t.position=new Vector3(11,8.5f,t.position.z);}
                var text=t.GetComponent<TextMesh>();if(text){Undo.RecordObject(text,"Readable world signs");text.characterSize=text.text.StartsWith("NOA")?.048f:.066f;text.color=new Color(.76f,.81f,.77f);if(text.text.StartsWith("중앙역"))t.position=new Vector3(11,8.5f,t.position.z);}
            }
            if(!GameObject.Find("QUALITY / Station detail")){
                var detail=new GameObject("QUALITY / Station detail");Undo.RegisterCreatedObjectUndo(detail,"Station furnishings");detail.transform.SetParent(world);var saved=world;world=detail.transform;
                for(int i=0;i<12;i++){
                    float x=3+i*4.5f;Box("Wall service panel",x,7.8f,8.67f,1.1f,1.2f,.1f,"WornSteel");
                    for(int j=0;j<6;j++)Box("Vent grille",x,8.18f-j*.14f,8.59f,.83f,.035f,.045f,"Chrome");
                    Box("Conduit",x-1.7f,6.7f,8.62f,.055f,7.4f,.055f,"WornSteel");
                    Box("Route inset",x,5.95f,8.61f,4.08f,.2f,.07f,"RoutePaint");
                }
                for(int i=0;i<6;i++){
                    float x=3+i*9;Box("Warm concourse fixture",x,10.6f,2,5.9f,.12f,.3f,"WarmWindow");
                    LightAt(new Vector3(x,8.7f,2),new Color(1,.83f,.61f),3.3f,10);
                    Box("Recessed fixture trim",x,10.69f,2,6.2f,.11f,.5f,"WornSteel");
                }
                Box("Transit map backing",14,7.9f,8.56f,2.4f,2.1f,.1f,"WarmWindow");
                for(int i=0;i<3;i++){Box("Map route",14,7.3f+i*.46f,8.49f,1.9f,.025f,.025f,i%2==0?"RoutePaint":"Gold");for(int j=0;j<5;j++)Box("Map stop",13.2f+j*.39f,7.3f+i*.46f,8.46f,.055f,.075f,.02f,"DarkMetal");}
                for(int i=0;i<15;i++){float x=i*1.35f;Box("Concourse tactile edge",x,5.044f,-3.45f,1.21f,.025f,.24f,"Gold");}
                foreach(float x in new[]{5f,18f,35f,49f}){Box("Maintenance inset",x,x<20?5.033f:.033f,-2.2f,1.1f,.019f,.8f,"WornSteel");for(int k=0;k<7;k++)Box("Floor drain slot",x-.43f+k*.14f,x<20?5.045f:.045f,-2.2f,.033f,.02f,.65f,"DarkMetal");}
                world=saved;
            }
            var cam=Camera.main;cam.backgroundColor=new Color(.02f,.03f,.045f);cam.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.None;
            var profile=Object.FindAnyObjectByType<Volume>().sharedProfile;if(profile.TryGet<Bloom>(out var bloom))bloom.intensity.Override(.17f);if(profile.TryGet<ColorAdjustments>(out var grade)){grade.postExposure.Override(.22f);grade.contrast.Override(8);grade.saturation.Override(-10);}EditorUtility.SetDirty(profile);
            foreach(var n in Object.FindObjectsByType<SpriteRenderer>())if(n.transform.parent==world&&!n.GetComponent<ContactShadow>())n.gameObject.AddComponent<ContactShadow>();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void FoundationAndBuild(){ApplyQualityFoundation();BuildWindows();}
        [MenuItem("AFTERSIGNAL/Quality/Apply verified presentation assets")]
        public static void ApplyPresentation()
        {
            foreach(string n in new[]{"CyanFX","GoldFX","RedFX","GhostFX"}){
                var m=Mat(n);if(!m){m=new Material(Shader.Find("AfterSignal/Signal Effect"));AssetDatabase.CreateAsset(m,resourceRoot+"Materials/"+n+".mat");}m.shader=Shader.Find("AfterSignal/Signal Effect");m.SetColor("_BaseColor",new Color(1.15f,1.15f,1.15f,1));EditorUtility.SetDirty(m);
            }
            foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{resourceRoot+"Audio/Quality"})){
                var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.OpenScene("Assets/AfterSignal/Scenes/01_NeonStation.unity");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot+"Materials/CeramicAlbedo.png");
            var ti=(TextureImporter)AssetImporter.GetAtPath(resourceRoot+"Materials/CeramicAlbedo.png");ti.wrapMode=TextureWrapMode.Repeat;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=true;ti.maxTextureSize=1024;ti.SaveAndReimport();
            var wall=Mat("CeramicWall");wall.SetTexture("_BaseMap",texture);wall.SetTextureScale("_BaseMap",new Vector2(1,2));wall.SetColor("_BaseColor",new Color(.95f,.95f,.92f));wall.SetFloat("_Smoothness",.2f);wall.EnableKeyword("_EMISSION");wall.SetColor("_EmissionColor",new Color(.075f,.067f,.05f));EditorUtility.SetDirty(wall);
            RenderSettings.ambientSkyColor=new Color(.37f,.37f,.34f);RenderSettings.ambientEquatorColor=new Color(.26f,.28f,.29f);RenderSettings.ambientGroundColor=new Color(.14f,.17f,.19f);
            foreach(var light in Object.FindObjectsByType<Light>()){
                if(light.type==LightType.Directional){light.shadowStrength=.35f;light.intensity=.78f;light.color=new Color(.89f,.9f,.9f);}
                else if(light.transform.position.y>8){light.intensity=24;light.range=13;}
            }
            foreach(var r in Object.FindObjectsByType<MeshRenderer>())if(r.name.Contains("Ceiling")||r.name=="Sign rim"||r.name.StartsWith("Sign /"))r.shadowCastingMode=ShadowCastingMode.Off;
            foreach(var text in Object.FindObjectsByType<TextMesh>())if(text.text=="RMB"||text.text=="E / LINK")text.characterSize=.038f;
            var hero=Object.FindAnyObjectByType<PlayerMotor>();PrefabUtility.SaveAsPrefabAsset(hero.gameObject,"Assets/AfterSignal/Prefabs/Seo.prefab");
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void PresentationAndBuild(){ApplyPresentation();BuildWindows();}
    }
}
