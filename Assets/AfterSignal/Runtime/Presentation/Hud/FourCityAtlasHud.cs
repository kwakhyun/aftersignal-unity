using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        sealed class AtlasRow{public Button button;public string search;public int city,category;public CityVenue venue;}
        readonly Button[] atlasCityButtons=new Button[4],atlasCategoryButtons=new Button[7];
        readonly List<AtlasRow> atlasRows=new();InputField atlasSearch;RectTransform atlasContent;Text atlasDetail,atlasResults;int atlasCity=-1,atlasCategory;float nextAtlasInfo;ScrollRect atlasScroll;
        void BuildAtlasExplorer()
        {
            for(int i=0;i<4;i++){int city=i;atlasCityButtons[i]=MakeButton(cityMapOverlay.transform,FourCityCatalog.CityNames[i],30+i*251,76,241,34,()=>{atlasCity=city;atlas.follow=false;atlas.Focus(FourCityCatalog.Centers[city],city==2?2300:1800);FilterAtlas();});}
            var field=Panel(cityMapOverlay.transform,"Search city facilities",1060,118,350,40,new Color(.09f,.17f,.21f));field.raycastTarget=true;atlasSearch=field.gameObject.AddComponent<InputField>();atlasSearch.textComponent=Label(field.transform,"",12,8,326,26,17,white);atlasSearch.placeholder=Label(field.transform,"시설·지역 검색 (예: 축구, 공항)",12,8,326,26,15,muted);atlasSearch.onValueChanged.AddListener(_=>FilterAtlas());
            string[] categories={"전체","스포츠","문화","숙박","교통","기록","생활"};
            for(int i=0;i<7;i++){int selected=i;atlasCategoryButtons[i]=MakeButton(cityMapOverlay.transform,categories[i],1060+i%4*88,168+i/4*34,82,29,()=>{atlasCategory=selected;atlas.category=selected;atlas.filter=selected==4?2:0;FilterAtlas();});}
            var allCities=MakeButton(cityMapOverlay.transform,"전 도시",1324,202,86,29,()=>{atlasCity=-1;FilterAtlas();});
            foreach(var button in atlasCategoryButtons)button.GetComponentInChildren<Text>().fontSize=14;allCities.GetComponentInChildren<Text>().fontSize=14;
            atlasResults=Label(cityMapOverlay.transform,"",1062,237,350,26,14,mint);
            var view=Panel(cityMapOverlay.transform,"Search results",1056,270,360,312,new Color(.04f,.09f,.12f));view.raycastTarget=true;view.gameObject.AddComponent<RectMask2D>();atlasContent=new GameObject("Facility list",typeof(RectTransform)).GetComponent<RectTransform>();atlasContent.SetParent(view.transform,false);
            atlasScroll=view.gameObject.AddComponent<ScrollRect>();atlasScroll.viewport=view.rectTransform;atlasScroll.content=atlasContent;atlasScroll.horizontal=false;atlasScroll.scrollSensitivity=38;atlasScroll.movementType=ScrollRect.MovementType.Clamped;
            foreach(var venue in FourCityCatalog.Venues)
            {var v=venue;var b=MakeButton(atlasContent,FourCityAtlasSelection.Icon(v.kind)+"  "+v.title,6,0,333,43,()=>SelectAtlasVenue(v));b.GetComponentInChildren<Text>().fontSize=15;atlasRows.Add(new AtlasRow{button=b,search=v.title+" "+v.district+" "+v.description,city=v.city,category=v.Sport?1:v.kind==VenueKind.Hotel?3:FourCityCampaign.Contains(System.Array.IndexOf(FourCityCatalog.Venues,v))?5:v.kind is VenueKind.Garden or VenueKind.Monument or VenueKind.Cinema or VenueKind.Museum or VenueKind.Amusement?2:6,venue=v});}
            for(int i=0;i<ExpansionWorld.Names.Length+UrbanCatalog.SiteCount;i++)
            {bool expansion=i<ExpansionWorld.Names.Length;int id=expansion?i:i-ExpansionWorld.Names.Length;string name=expansion?ExpansionWorld.Names[id]:UrbanCatalog.Name(id);var p=expansion?ExpansionWorld.Places[id]:UrbanCatalog.Center(id);var b=MakeButton(atlasContent,"·  "+name,6,0,333,43,()=>atlas.Select(id,expansion));b.GetComponentInChildren<Text>().fontSize=15;atlasRows.Add(new AtlasRow{button=b,search=name,city=FourCityCatalog.CityAt(p),category=name.Contains("공항")||name.Contains("항만")||name.Contains("터미널")||name.Contains("역")?4:name.Contains("호텔")?3:6});}
            atlasDetail=Label(cityMapOverlay.transform,"현재 위치 중심의 지도입니다.\n도시 탭으로 이동하고 검색·분류로 시설을 찾으세요.\n선택한 시설에는 이름과 길 안내가 표시됩니다.",1060,597,348,106,16,white);
            MakeButton(cityMapOverlay.transform,"내 위치 중심",1060,708,165,35,()=>{atlas.Recenter();atlasCity=-1;FilterAtlas();});
            MakeButton(cityMapOverlay.transform,"네 도시 캠페인",1235,708,175,35,()=>{var c=FourCityCampaign.Instance;if(c&&!c.Complete){SelectAtlasVenue(FourCityCatalog.Venues[c.Current.facility]);FourCityAtlasSelection.Select(FourCityCatalog.Venues[c.Current.facility],true);}});
            FilterAtlas();
        }
        void FilterAtlas()
        {
            if(!atlasContent)return;for(int i=0;i<4;i++)atlasCityButtons[i].GetComponent<Image>().color=atlasCity==i?new Color(.16f,.5f,.5f):new Color(.16f,.29f,.3f);for(int i=0;i<7;i++)atlasCategoryButtons[i].GetComponent<Image>().color=atlasCategory==i?new Color(.16f,.5f,.5f):new Color(.16f,.29f,.3f);string query=atlasSearch.text.Trim();int visible=0;
            foreach(var row in atlasRows){bool active=(atlasCity<0||row.city==atlasCity)&&(atlasCategory==0||row.category==atlasCategory||atlasCategory==5&&row.venue!=null&&FourCityCampaign.Contains(System.Array.IndexOf(FourCityCatalog.Venues,row.venue)))&&(query.Length==0||row.search.IndexOf(query,System.StringComparison.OrdinalIgnoreCase)>=0);row.button.gameObject.SetActive(active);if(active){Rect(row.button.GetComponent<RectTransform>(),6,visible++*46,333,43);}}
            Rect(atlasContent,0,0,345,Mathf.Max(312,visible*46));atlasScroll.verticalNormalizedPosition=1;atlasResults.text=(atlasCity<0?"모든 도시":FourCityCatalog.CityNames[atlasCity])+" · "+visible+"개 시설"+(visible==0?" / 검색어를 바꿔 보세요":"");
        }
        void SelectAtlasVenue(CityVenue v)
        {FourCityAtlasSelection.Select(v);selectedSite=-1;customMapGoal=false;atlas.follow=false;atlas.Focus(v.position,Mathf.Clamp(Mathf.Max(v.size.x,v.size.y)*2.8f,480,1300));UpdateAtlasExplorer();}
        void UpdateAtlasExplorer()
        {
            if(!atlasDetail||Time.unscaledTime<nextAtlasInfo)return;nextAtlasInfo=Time.unscaledTime+.5f;
            var v=FourCityAtlasSelection.Venue;if(v==null)return;string extra=v.Sport&&FourCitySports.Instance?FourCitySports.Instance.Get(v.id).Status+" · "+FourCitySports.Instance.Get(v.id).homeScore+" : "+FourCitySports.Instance.Get(v.id).awayScore:v.city==3?"수중 돔 내부 · 호흡 가능 / 기밀 도로 연결":v.city==2?"잠식 지역 · 진입 전 장비 확인":"정문 도보 진입 · E 안내 / 이용";
            atlasDetail.text=v.title+"\n"+extra+"\n"+v.description;
        }
    }
}
