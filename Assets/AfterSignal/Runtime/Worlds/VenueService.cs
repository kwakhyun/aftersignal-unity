using UnityEngine;
namespace AfterSignal
{
    public sealed class VenueService:MonoBehaviour
    {
        public int index;
        public string action="desk";
        public int floor;
        public static VenueService Add(Transform parent,Vector3 local,int index,string title,string action="desk",int floor=0)
        {var go=new GameObject(title);go.transform.SetParent(parent,false);go.transform.localPosition=local;var p=go.AddComponent<InteractionPoint>();p.kind=InteractionKind.Furniture;p.title=title;p.radius=3.5f;var s=go.AddComponent<VenueService>();s.index=index;s.action=action;s.floor=floor;return s;}
        public void Use(){if(action=="sync"){FourCityCampaign.Instance?.Synchronize(floor);return;}CityLife.Instance?.VenueMenu(this);}
    }
}
