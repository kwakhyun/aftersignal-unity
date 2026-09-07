using UnityEngine;
namespace AfterSignal
{
    public sealed class UrbanInterior:MonoBehaviour
    {
        public GameObject[] themes;
        void Awake(){int kind=UrbanCatalog.Kind(UrbanCatalog.Current);for(int i=0;i<themes.Length;i++)if(themes[i])themes[i].SetActive(i==kind);}
    }
}
