using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace AfterSignal.Editor
{
    public static class WorldRegionalBuilder
    {
        public static void Prepare()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/AfterSignal/Scenes/04_Afterlight.unity",OpenSceneMode.Single);
                GameObject world=null;foreach(var root in scene.GetRootGameObjects())if(root.name.StartsWith("WORLD"))world=root;
                if(!world)throw new System.InvalidOperationException("Original Haven world was not found");
                var copy=Object.Instantiate(world);copy.name="Haven hometown quarter";
                foreach(var s in copy.GetComponentsInChildren<MonoBehaviour>(true))if(s is GameDirector||s is CameraRig||s is SignalHud||s is UrbanSimulation||s is FourCityWorld)Object.DestroyImmediate(s);
                PrefabUtility.SaveAsPrefabAsset(copy,"Assets/AfterSignal/Resources/WorldAssets/HavenQuarter.prefab");Object.DestroyImmediate(copy);
                Debug.Log("REGIONAL: preserved original Haven architecture and interactions in HavenQuarter");
                AssetDatabase.SaveAssets();
            }
            finally{if(System.Array.Exists(setup,s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void PrepareAndBuild(){Prepare();ProjectBuilder.BuildRelease();}
    }
}
