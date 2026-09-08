using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace AfterSignal.Editor
{
    public static class CargoPresentationBake
    {
        public static void Refine()
        {
            const string path="Assets/AfterSignal/Resources/WorldAssets/ContainerShip.prefab";
            var source=PrefabUtility.LoadPrefabContents(path);
            if(source.transform.Find("Cargo material batch 0")){PrefabUtility.UnloadPrefabContents(source);return;}
            var root=new GameObject("MV AFTERGLOW / controllable cargo ship");
            root.AddComponent<CityVehicle>().type=CityVehicleType.Boat;root.AddComponent<AuthoredCraft>();
            var groups=new Dictionary<string,List<MeshFilter>>();var paints=new Dictionary<string,Material>();
            foreach(var f in source.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!f.sharedMesh)continue;
                string key=r.name=="Navigation bridge"?"Bridge glazing":r.sharedMaterial.name;
                if(!groups.ContainsKey(key)){groups[key]=new List<MeshFilter>();paints[key]=r.sharedMaterial;}groups[key].Add(f);
            }
            int serial=0;
            foreach(var group in groups)
            {
                var mesh=new Mesh{name="Moving cargo ship / "+group.Key,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Value.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=source.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);mesh.RecalculateBounds();
                string asset="Assets/AfterSignal/Resources/WorldAssets/Generated/CargoBatch"+serial+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
                if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,asset);
                var o=new GameObject(group.Key=="Bridge glazing"?"Navigation bridge":"Cargo material batch "+serial,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(root.transform,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=paints[group.Key];serial++;
            }
            void Collision(Vector3 at,Vector3 size){var c=root.AddComponent<BoxCollider>();c.center=at;c.size=size;}
            Collision(new Vector3(0,-1,0),new Vector3(460,12,37));Collision(new Vector3(185,15,0),new Vector3(32,20,30));Collision(new Vector3(185,26,0),new Vector3(42,4,34));
            PrefabUtility.UnloadPrefabContents(source);PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
            Debug.Log("CARGO OPTIMIZED: "+serial+" material batches and three hull colliders.");
        }
        public static void RefineAndRelease(){Refine();ProjectBuilder.BuildRelease();}
    }
}
