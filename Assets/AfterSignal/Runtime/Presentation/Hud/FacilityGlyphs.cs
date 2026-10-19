using System;
using UnityEngine;

namespace AfterSignal
{
    // One private, tool-authored atlas for all cities. No pixel readback or per-frame loading.
    public static class FacilityGlyphs
    {
        public const string ResourcePath = "Art/ArtCraft/UI/FacilityGlyphs";
        public const int Count = 40;
        static readonly string[] names = {
            "Garden", "Monument", "Football", "Baseball", "Basketball", "Circuit", "Cinema", "Hotel",
            "Amusement", "Museum", "Research", "Civic", "Market", "Hospital", "Archive", "Reactor",
            "Police", "FireStation", "Bank", "Restaurant", "Cafe", "School", "Library", "Military",
            "Prison", "Slum", "Island", "Laboratory", "CityHall", "Plaza", "Sinkhole",
            "Airport", "Port", "Fuel", "Garage", "Train", "Respawn", "Incident", "Waypoint", "Default"
        };
        static Sprite[] sprites;
        static int[] venueIndices;
        static bool attempted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if (sprites != null)
                foreach (var sprite in sprites)
                    if (sprite) UnityEngine.Object.Destroy(sprite);
            sprites = null;
            venueIndices = null;
            attempted = false;
        }

        static void Load()
        {
            if (attempted) return;
            attempted = true;
            var texture = Resources.Load<Texture2D>(ResourcePath);
            if (!texture || texture.width != 1024 || texture.height != 640) return;
            sprites = new Sprite[Count];
            for (int i = 0; i < Count; i++)
            {
                var rect = new Rect(i % 8 * 128, (4 - i / 8) * 128, 128, 128);
                sprites[i] = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                sprites[i].name = names[i];
            }
            var kinds = (VenueKind[])Enum.GetValues(typeof(VenueKind));
            int maximum = 0;
            foreach (var kind in kinds) maximum = Mathf.Max(maximum, (int)kind);
            venueIndices = new int[maximum + 1];
            for (int i = 0; i < venueIndices.Length; i++) venueIndices[i] = Count - 1;
            // Match by name once; adding or reordering VenueKind cannot silently change a symbol.
            foreach (var kind in kinds)
            {
                int index = Array.IndexOf(names, kind.ToString());
                venueIndices[(int)kind] = index < 0 ? Count - 1 : index;
            }
        }

        public static Sprite Get(int index)
        {
            Load();
            return sprites == null ? null : sprites[Mathf.Clamp(index, 0, Count - 1)];
        }

        public static Sprite For(VenueKind kind)
        {
            Load();
            int value = (int)kind;
            return sprites == null ? null : Get(value >= 0 && value < venueIndices.Length ? venueIndices[value] : Count - 1);
        }

        public static Sprite ForName(string name)
        {
            name ??= "";
            if (name.Contains("주유")) return Get(33);
            if (name.Contains("카센터") || name.Contains("주차") || name.Contains("정비")) return Get(34);
            if (name.Contains("중앙역") || name.Contains("터미널")) return Get(35);
            if (name.Contains("소방")) return For(VenueKind.FireStation);
            if (name.Contains("경찰")) return For(VenueKind.Police);
            if (name.Contains("병원") || name.Contains("의료") || name.Contains("진료")) return For(VenueKind.Hospital);
            if (name.Contains("공항") || name.Contains("비행단")) return Get(31);
            if (name.Contains("항구") || name.Contains("항만") || name.Contains("해군")) return Get(32);
            if (name.Contains("은행")) return For(VenueKind.Bank);
            if (name.Contains("학교")) return For(VenueKind.School);
            if (name.Contains("도서")) return For(VenueKind.Library);
            if (name.Contains("호텔")) return For(VenueKind.Hotel);
            if (name.Contains("카페")) return For(VenueKind.Cafe);
            if (name.Contains("식당") || name.Contains("술집")) return For(VenueKind.Restaurant);
            if (name.Contains("마트") || name.Contains("시장") || name.Contains("백화") || name.Contains("옷장")) return For(VenueKind.Market);
            if (name.Contains("기지") || name.Contains("군부대")) return For(VenueKind.Military);
            if (name.Contains("교도") || name.Contains("구치")) return For(VenueKind.Prison);
            if (name.Contains("연구") || name.Contains("실험")) return For(VenueKind.Research);
            if (name.Contains("시청")) return For(VenueKind.CityHall);
            if (name.Contains("집") || name.Contains("아파트") || name.Contains("저지대")) return For(VenueKind.Slum);
            if (name.Contains("공원") || name.Contains("식물원")) return For(VenueKind.Garden);
            if (name.Contains("영화") || name.Contains("시네마")) return For(VenueKind.Cinema);
            if (name.Contains("박물") || name.Contains("전시")) return For(VenueKind.Museum);
            if (name.Contains("광장")) return For(VenueKind.Plaza);
            if (name.Contains("해변") || name.Contains("섬마을")) return For(VenueKind.Island);
            return For(VenueKind.Civic);
        }
    }
}
