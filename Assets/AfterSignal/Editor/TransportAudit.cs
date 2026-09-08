using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public static class TransportAudit
    {
        public static void Inspect()
        {
            foreach(var name in new[]{"DetailedSedan","SportsCar","Airliner","Fighter","Tank"})
            {
                var asset=Resources.Load<GameObject>("WorldAssets/"+name+"/"+name);if(!asset)continue;
                var instance=Object.Instantiate(asset);
                foreach(var r in instance.GetComponentsInChildren<MeshRenderer>())if(r.name.StartsWith("Headlamp")||r.name.StartsWith("Taillamp"))Debug.Log("TRANSPORT AXES "+name+" / "+r.name+" / "+r.bounds.center+" root rotation "+instance.transform.eulerAngles);
                Object.DestroyImmediate(instance);
            }
        }
    }
}
