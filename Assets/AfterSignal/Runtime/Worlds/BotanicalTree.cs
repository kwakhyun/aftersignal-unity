using UnityEngine;
namespace AfterSignal
{
    public static class BotanicalTree
    {
        static GameObject prefab;static bool loaded;
        public static bool Place(Transform parent,Vector3 position,float scale,int seed)
        {
            if(!loaded){loaded=true;prefab=Resources.Load<GameObject>("WorldAssets/Botanical/BotanicalTree");}
            if(!prefab)return false;
            var tree=Object.Instantiate(prefab,parent);tree.name="Botanical canopy / LOD";tree.transform.localPosition=position;
            tree.transform.localRotation=Quaternion.Euler(0,seed*137.508f,0);tree.transform.localScale=Vector3.one*scale*1.25f;
            var trunk=tree.AddComponent<CapsuleCollider>();trunk.center=new(0,1.5f,0);trunk.radius=.14f;trunk.height=3;
            tree.AddComponent<BreakableStreetProp>().breakEnergy=95000;
            return true;
        }
    }
}
