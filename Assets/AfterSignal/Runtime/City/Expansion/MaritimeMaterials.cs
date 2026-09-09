using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class MaritimeMaterials
    {
        static readonly Dictionary<string,Material> cache=new();
        public static void Apply(GameObject root,SeaFaction faction)
        {
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)
                {
                    string name=mats[i]?mats[i].name:"SecurityArmor",key=faction+name;
                    if(!cache.TryGetValue(key,out var m))
                    {
                        bool glass=name.Contains("Glass");m=new Material(glass?Resources.Load<Shader>("Shaders/StructuralGlass"):Shader.Find("Universal Render Pipeline/Lit"));m.name=key;
                        Color color=glass?new Color(.16f,.4f,.53f,.26f):name.Contains("Ceramic")?new Color(.78f,.85f,.86f):name.Contains("Steel")?new Color(.39f,.49f,.54f):name.Contains("Cyan")?new Color(.08f,.7f,.84f):name.Contains("Red")?new Color(.7f,.13f,.08f):name.Contains("Rubber")?new Color(.055f,.063f,.07f):faction==SeaFaction.Pirates?new Color(.25f,.10f,.14f):faction==SeaFaction.Navy?new Color(.31f,.40f,.46f):new Color(.65f,.75f,.79f);
                        m.SetColor("_BaseColor",color);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",.2f);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.38f);m.enableInstancing=true;cache[key]=m;
                    }
                    mats[i]=m;
                }r.sharedMaterials=mats;
            }
        }
    }
}
