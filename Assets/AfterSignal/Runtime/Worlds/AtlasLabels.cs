using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed class AtlasLabels : MonoBehaviour
    {
        const int MaxLabels = 64;
        sealed class Entry
        {
            public RectTransform root;
            public Text text;
            public Image icon;
        }
        AtlasViewport map;
        readonly List<Entry> labels = new();
        readonly List<Rect> occupied = new();
        Font font;
        float next;
        int count;

        public void Initialize(AtlasViewport viewport)
        {
            map = viewport;
            font = Resources.Load<Font>("Fonts/NotoSansKR");
            if (!GetComponent<RectMask2D>()) gameObject.AddComponent<RectMask2D>();
        }

        Entry Label()
        {
            if (count >= labels.Count)
            {
                var root = new GameObject("Facility map label", typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(transform, false);
                root.gameObject.layer = gameObject.layer;
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
                var textObject = new GameObject("Name", typeof(RectTransform), typeof(Text), typeof(Outline));
                textObject.transform.SetParent(root, false);
                textObject.layer = gameObject.layer;
                var text = textObject.GetComponent<Text>();
                text.font = font;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
                text.rectTransform.anchoredPosition = new Vector2(0, -16);
                var outline = textObject.GetComponent<Outline>();
                outline.effectColor = new Color(.015f, .045f, .06f, .95f);
                outline.effectDistance = new Vector2(1, -1);
                var iconObject = new GameObject("Facility symbol", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(root, false);
                iconObject.layer = gameObject.layer;
                var icon = iconObject.GetComponent<Image>();
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.rectTransform.anchoredPosition = new Vector2(0, 16);
                labels.Add(new Entry { root = root, text = text, icon = icon });
            }
            return labels[count++];
        }

        void Add(Vector3 position, string title, Color color, int size = 13, bool priority = false, Sprite symbol = null)
        {
            if (count >= MaxLabels) return;
            var xy = map.Project(position);
            var bounds = map.rectTransform.rect;
            if (!bounds.Contains(xy) || xy.y > bounds.yMax - 18 || xy.y < bounds.yMin + 40) return;
            float width = Mathf.Min(245, title.Length * size * .73f + 12);
            var box = new Rect(xy.x - width / 2, xy.y - 48, width, 66);
            foreach (var used in occupied)
                if (!priority && box.Overlaps(used)) return;
            occupied.Add(box);
            var entry = Label();
            entry.root.anchoredPosition = xy - bounds.center + Vector2.down * 16;
            entry.root.sizeDelta = new Vector2(width, 64);
            if (entry.text.text != title) entry.text.text = title;
            entry.text.color = color;
            entry.text.fontSize = size;
            entry.text.rectTransform.sizeDelta = new Vector2(width, 28);
            entry.icon.sprite = symbol;
            entry.icon.enabled = symbol;
            entry.icon.rectTransform.sizeDelta = Vector2.one * (priority ? 34 : 28);
        }

        void Update()
        {
            if (!map || map.mini || Time.unscaledTime < next) return;
            next = Time.unscaledTime + .12f;
            RefreshLabels();
        }

        public void RefreshLabels()
        {
            if (!map || map.mini) return;
            count = 0;
            occupied.Clear();
            if (map.Span < 2600)
                for (int i = 0; i < 4; i++)
                    if (RespawnNetwork.Points[i] != Vector3.zero) Add(RespawnNetwork.Points[i], RespawnNetwork.Names[i], new Color(.45f, 1, .75f), 14, true, FacilityGlyphs.Get(36));
            foreach (var incident in CityIncidentBoard.Active)
                Add(incident.position, incident.title, incident.color, 14, true, FacilityGlyphs.Get(37));
            if (FourCityAtlasSelection.Venue != null)
            {
                var venue = FourCityAtlasSelection.Venue;
                Add(venue.position, venue.title, new Color(1, .88f, .45f), 16, true, FacilityGlyphs.For(venue.kind));
            }
            if (map.Span > 2600)
            {
                for (int i = 0; i < 4; i++)
                    Add(FourCityCatalog.Centers[i], FourCityCatalog.CityNames[i], new Color(.7f, .88f, .94f), 21, true);
            }
            else
            {
                if (map.category is 0 or 6)
                {
                    Add(MaritimeWorld.NavalBase, "해군 해협 방위사령부", new Color(.4f,.7f,1), 13, false, FacilityGlyphs.For(VenueKind.Military));
                    Add(MaritimeWorld.AirBase, "공군 루멘 비행단", new Color(.6f,.82f,1), 13, false, FacilityGlyphs.Get(31));
                    Add(MaritimeWorld.CoastBase, "해양경찰 구조 경비대", new Color(1,.7f,.35f), 13, false, FacilityGlyphs.For(VenueKind.Police));
                }
                // Facilities respect the same category on the map and in the search list.
                foreach (var venue in FourCityCatalog.Venues)
                    if (map.Visible(venue) && venue != FourCityAtlasSelection.Venue)
                        Add(venue.position, venue.title, FourCityAtlasSelection.Color(venue.kind), 14, false, FacilityGlyphs.For(venue.kind));
                if (map.category is 0 or 3 or 4 or 6)
                {
                    for (int i = 0; i < ExpansionWorld.Places.Length; i++)
                        if (map.VisibleFacility(ExpansionWorld.Names[i]))
                            Add(ExpansionWorld.Places[i], ExpansionWorld.Names[i], new Color(.65f, .79f, .82f), 13, false, FacilityGlyphs.ForName(ExpansionWorld.Names[i]));
                    if (map.Span < 800)
                        for (int i = 0; i < UrbanCatalog.SiteCount; i++)
                            if (map.VisibleFacility(UrbanCatalog.Name(i)))
                                Add(UrbanCatalog.Center(i), UrbanCatalog.Name(i), new Color(.55f, .71f, .72f), 12, false, FacilityGlyphs.ForName(UrbanCatalog.Name(i)));
                }
            }
            for (int i = 0; i < labels.Count; i++)
                if (labels[i].root.gameObject.activeSelf != (i < count))
                    labels[i].root.gameObject.SetActive(i < count);
        }
    }
}
