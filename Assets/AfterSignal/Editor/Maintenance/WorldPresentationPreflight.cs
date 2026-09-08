using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AfterSignal.Editor
{
    // A successful compile cannot detect a saved prefab with its visual world removed.
    public sealed class WorldPresentationPreflight : IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report)=>Validate();
        [MenuItem("AFTERSIGNAL/Quality/Check world presentation dependencies")]
        public static void Validate()
        {
            foreach(string name in new[]{"AfterlightExpansion","MobilityDistricts","CivicRenewal","NeonHarbor","TransitFacilities"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AfterSignal/Resources/WorldAssets/"+name+".prefab");
                if(!prefab)throw new BuildFailedException("Missing world prefab: "+name);
                var batch=prefab.transform.Find("Batched district geometry");
                int count=batch?batch.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh):0;
                if(count<10)throw new BuildFailedException(name+" lost its baked presentation ("+count+" batches); refusing to ship invisible terrain and facilities.");
                if(prefab.GetComponentsInChildren<MeshFilter>(true).Any(f=>!f.sharedMesh))throw new BuildFailedException(name+" has a missing mesh dependency.");
                Debug.Log("WORLD PREFLIGHT "+name+" / "+count+" valid presentation batches");
            }
        }
    }
}
