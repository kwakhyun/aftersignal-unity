using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class StreetKit
    {
        static readonly Dictionary<string,GameObject> prefabs=new();
        public static GameObject Place(string key,Transform parent,Vector3 position,Quaternion rotation,bool solid=true)
        {
            if(!prefabs.TryGetValue(key,out var prefab)){prefab=Resources.Load<GameObject>("WorldAssets/StreetKit/"+key);prefabs[key]=prefab;}
            if(!prefab)return null;var root=new GameObject(key);root.transform.SetParent(parent,false);root.transform.localPosition=position;root.transform.localRotation=rotation;
            // Preserve the imported FBX basis rotation: clearing it laid Z-up source models flat.
            var model=Object.Instantiate(prefab,root.transform);model.transform.localPosition=Vector3.zero;
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                var mats=renderer.sharedMaterials;
                for(int n=0;n<mats.Length;n++)
                {
                    string m=mats[n]?mats[n].name:"Graphite";
                    string material=m.Contains("Alloy")?"FutureSilver":m.Contains("Ceramic")?"FutureCeramic":m.Contains("Copper")?"FutureCopper":m.Contains("Glass")?"Glass":m.Contains("Wood")?"Trunk":m.Contains("Leaf")?"CanopyLeaf":m.Contains("Soil")?"GardenSoil":m.Contains("Cyan")?"NeonCyan":m.Contains("Amber")?"NeonWarm":"FutureCarbon";
                    mats[n]=CityGeometry.Material(material);
                }
                renderer.sharedMaterials=mats;renderer.receiveShadows=true;
            }
            if(solid)
            {
                var box=root.AddComponent<BoxCollider>();
                if(key=="TransitShelter"){box.center=new(0,1.4f,1.1f);box.size=new(5,2.7f,.12f);}
                else if(key=="PromenadeBench"){box.center=new(0,.4f,0);box.size=new(2.2f,.8f,.65f);}
                else if(key=="CoastalPalm"){box.center=new(0,.3f,0);box.size=new(2.3f,.6f,2.3f);}
                else if(key=="ClimateUnit"){box.center=new(0,.65f,0);box.size=new(1.7f,1.3f,.85f);}
                else{box.center=new(0,key=="CivicKiosk"?1.3f:.6f,0);box.size=new(key=="CivicKiosk"?.9f:.5f,key=="CivicKiosk"?2.6f:1.2f,.5f);}
                if(key!="TransitShelter"&&key!="CoastalPalm")root.AddComponent<BreakableStreetProp>().breakEnergy=65000;
            }
            if(key=="PromenadeBench"){var point=root.AddComponent<InteractionPoint>();point.kind=InteractionKind.Furniture;point.title="벤치에서 휴식";point.radius=2.8f;root.AddComponent<UsableProp>().use=PropUse.Bench;}
            return root;
        }
    }
}
