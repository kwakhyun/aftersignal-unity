using UnityEngine;
namespace AfterSignal
{
    public static class FourCityAtlasSelection
    {
        public static CityVenue Venue;
        public static bool Campaign;
        public static int Revision;
        public static void Select(CityVenue venue,bool campaign=false){Venue=venue;Campaign=campaign;Revision++;ExpansionWorld.Selected=-1;}
        public static Vector3 Goal=>Campaign&&FourCityCampaign.Instance&&!FourCityCampaign.Instance.Complete?FourCityCampaign.Instance.Destination:Venue?.Entrance??Vector3.zero;
        public static string Label=>Campaign&&FourCityCampaign.Instance&&!FourCityCampaign.Instance.Complete?FourCityCampaign.Instance.Current.title:Venue?.title??"";
        public static void Clear(){Venue=null;Campaign=false;Revision++;}
        public static Color Color(VenueKind kind)=>kind is VenueKind.Football or VenueKind.Baseball or VenueKind.Basketball or VenueKind.Circuit?new(.98f,.61f,.27f):kind is VenueKind.Archive or VenueKind.Reactor?new(.77f,.5f,1):kind==VenueKind.Hospital?new(.55f,.94f,.64f):new(.27f,.83f,.92f);
        public static string Icon(VenueKind kind)=>kind switch{VenueKind.Football=>"축",VenueKind.Baseball=>"야",VenueKind.Basketball=>"농",VenueKind.Circuit=>"R",VenueKind.Cinema=>"영",VenueKind.Hotel=>"H",VenueKind.Amusement=>"P",VenueKind.Garden=>"숲",VenueKind.Monument=>"탑",VenueKind.Museum=>"전",VenueKind.Hospital=>"+",VenueKind.Research=>"연",VenueKind.Market=>"상",VenueKind.Archive=>"기",_=>"안"};
    }
}
