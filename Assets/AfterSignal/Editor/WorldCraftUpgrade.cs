using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        static void UpgradeExistingCraft()
        {
            string path=Output+"AfterlightExpansion.prefab";
            var instance=PrefabUtility.LoadPrefabContents(path);
            var ship=instance.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.StartsWith("MV AFTERGLOW"));
            if(!ship){PrefabUtility.UnloadPrefabContents(instance);return;}
            var batches=instance.transform.Find("Batched district geometry");if(batches)Object.DestroyImmediate(batches.gameObject);
            foreach(var r in instance.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
            // Preserve the actual detailed cargo ship, while removing it from static world batches.
            var cargo=Object.Instantiate(ship.gameObject);cargo.transform.position=Vector3.zero;
            var car=cargo.AddComponent<CityVehicle>();car.type=CityVehicleType.Boat;cargo.AddComponent<AuthoredCraft>();
            PrefabUtility.SaveAsPrefabAsset(cargo,Output+"ContainerShip.prefab");Object.DestroyImmediate(cargo);Object.DestroyImmediate(ship.gameObject);
            foreach(var plane in instance.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Lumen Air / regional jet")).ToArray())
            {
                var marker=new GameObject("Boardable aircraft parking",typeof(CraftSpawn));marker.transform.SetParent(plane.parent,false);marker.transform.position=plane.position;marker.transform.rotation=Quaternion.Euler(0,90,0);Object.DestroyImmediate(plane.gameObject);
            }
            root=instance.transform;meshId=6000;CombinePresentation();SaveGeneratedMeshes();PrefabUtility.SaveAsPrefabAsset(instance,path);PrefabUtility.UnloadPrefabContents(instance);
            Debug.Log("MOBILITY CRAFT: original four airport stands now spawn drivable aircraft; detailed cargo vessel is steerable.");
        }
    }
}
