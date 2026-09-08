using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        GameObject cityMapOverlay;
        Text drivingInfo, cityDestination;
        int selectedSite = -1;
        float nextCityText;
        AtlasViewport atlas, miniAtlas;
        bool wasMapOpen;
        Text atlasScale;
        void BuildUrbanMap(Transform parent)
        {
            Label(parent, "AFTERLIGHT / N ↑    M 지도 탐색",12,8,296,20,12,mint,FontStyle.Bold);
            miniAtlas=CreateAtlas(parent,20,32,272,116,true);
        }
        AtlasViewport CreateAtlas(Transform parent,float x,float y,float w,float h,bool mini)
        {
            var go=new GameObject("Local street atlas",typeof(RectTransform),typeof(AtlasViewport));go.transform.SetParent(parent,false);
            var map=go.GetComponent<AtlasViewport>();Rect(map.rectTransform,x,y,w,h);map.mini=mini;map.raycastTarget=!mini;map.Focus(game.Player.transform.position,mini?360:820);
            map.Select=(id,expanded)=>{selectedSite=expanded?-1:id;ExpansionWorld.Selected=expanded?id:-1;map.follow=false;map.Focus(expanded?ExpansionWorld.Places[id]:UrbanCatalog.Center(id));customMapGoal=false;};
            map.Waypoint=p=>{mapGoal=p;customMapGoal=true;selectedSite=-1;ExpansionWorld.Selected=-1;game.Toast("지도 경유지를 지정했습니다");};return map;
        }
        void EnsureCityHud()
        {
            if(drivingInfo)return;
            drivingInfo=Label(root,"",0,0,900,90,19,white,FontStyle.Bold);CenterBottom(drivingInfo.rectTransform,45,900,90);drivingInfo.alignment=TextAnchor.MiddleCenter;
            if(game.stage!=StageId.UrbanCity)return;
            cityMapOverlay=Panel(root,"City atlas",0,0,1450,820,new Color(.025f,.055f,.075f,.98f)).gameObject;Center(cityMapOverlay.GetComponent<RectTransform>(),1450,820);
            var mapCanvas=cityMapOverlay.AddComponent<Canvas>();mapCanvas.overrideSorting=true;mapCanvas.sortingOrder=100;
            cityMapOverlay.AddComponent<GraphicRaycaster>();
            Label(cityMapOverlay.transform,"AFTERLIGHT / NETWORK ATLAS",30,22,1080,42,29,white,FontStyle.Bold);
            Label(cityMapOverlay.transform,"휠 확대·축소   ·   드래그 탐색   ·   시설 선택 / 빈 곳 클릭: 경유지",30,72,1110,27,16,mint);
            MakeButton(cityMapOverlay.transform,"닫기 M",1290,28,126,36,()=>UrbanSimulation.Instance?.CloseMap());
            atlas=CreateAtlas(cityMapOverlay.transform,30,120,1000,630,false);
            MakeButton(cityMapOverlay.transform,"+",40,132,40,40,()=>atlas.Zoom(.7f,atlas.rectTransform.rect.center));
            MakeButton(cityMapOverlay.transform,"−",40,178,40,40,()=>atlas.Zoom(1.4f,atlas.rectTransform.rect.center));
            MakeButton(cityMapOverlay.transform,"내 위치",886,132,130,40,()=>atlas.Recenter());
            atlasScale=Label(cityMapOverlay.transform,"",45,710,950,28,15,mint);
            Label(cityMapOverlay.transform,"목적지",1060,120,330,30,22,white,FontStyle.Bold);
            MakeButton(cityMapOverlay.transform,"메인 의뢰",1060,160,350,34,()=>{ExpansionWorld.Selected=-1;selectedSite=-1;customMapGoal=false;atlas.Recenter();});
            MakeButton(cityMapOverlay.transform,"전체",1060,202,110,30,()=>atlas.filter=0);
            MakeButton(cityMapOverlay.transform,"시설",1180,202,110,30,()=>atlas.filter=1);
            MakeButton(cityMapOverlay.transform,"교통",1300,202,110,30,()=>atlas.filter=2);
            var view=Panel(cityMapOverlay.transform,"Destination scroll",1056,244,360,496,new Color(.035f,.09f,.115f));view.raycastTarget=true;view.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("Destinations",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(view.transform,false);
            int count=ExpansionWorld.Names.Length+UrbanCatalog.SiteCount;Rect(content,0,0,342,count*34);
            var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.viewport=view.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=32;scroll.movementType=ScrollRect.MovementType.Clamped;
            for(int i=0;i<count;i++)
            {
                int id=i;bool expanded=i<ExpansionWorld.Names.Length;if(!expanded)id-=ExpansionWorld.Names.Length;
                int site=id;bool region=expanded;
                string name=expanded?ExpansionWorld.Names[id]:UrbanCatalog.Name(id);
                var b=MakeButton(content,name,6,i*34,330,31,()=>atlas.Select(site,region));b.GetComponentInChildren<Text>().fontSize=15;
            }
            cityDestination=Label(cityMapOverlay.transform,"",32,773,1370,28,17,mint);cityMapOverlay.SetActive(false);
        }

        void UpdateUrbanHud()
        {
            var sim = UrbanSimulation.Instance;
            if (!sim)
                return;
            EnsureCityHud();
            bool active = !game.Blocked;
            if (sim.Prompt.Length > 0 && active)
                prompt.text = sim.Prompt;
            if(CityBusService.Instance && CityBusService.Instance.Prompt.Length>0 && active) prompt.text=CityBusService.Instance.Prompt;
            if (Time.unscaledTime >= nextCityText)
            {
                nextCityText = Time.unscaledTime + .1f;
                drivingInfo.text = sim.Current ? $"{Mathf.Abs(sim.Current.speed) * 3.6f:000} km/h    ·    연료 {sim.Current.fuel:0.0} L    ·    차체 {sim.Current.health:0}%\n" + (sim.Current.fuel < 3 ? "연료 부족 · 가까운 주유소로 이동하세요" : VehicleSeats.Name(sim.Current,sim.SeatIndex)+" · "+(sim.SeatIndex==0?VehicleSeats.Controls(sim.Current):"승객 탑승 · F 하차")) : selectedSite >= 0 ? $"{UrbanCatalog.Name(selectedSite)}  ·  {Vector3.Distance(game.Player.transform.position, UrbanCatalog.Door(selectedSite)):0} m" : "";
            }

            arsenalPanel.SetActive(!sim.Driving);
            weapon.transform.parent.gameObject.SetActive(!sim.Driving);
            drivingInfo.gameObject.SetActive(active);
            if(cityMapOverlay)
            {
                bool open=sim.MapOpen&&active;cityMapOverlay.SetActive(open);
                if(open)
                {
                    if(!wasMapOpen)atlas.Recenter();Cursor.visible=true;cursor.gameObject.SetActive(false);
                    atlasScale.text="N ↑     표시 폭 "+atlas.Span.ToString("0")+" m     ◆ 서하    ■ 시설    ■ 교통편";
                    cityDestination.text=customMapGoal?"경유지 · "+Vector3.Distance(game.Player.transform.position,mapGoal).ToString("0")+" m":ExpansionWorld.Selected>=0?ExpansionWorld.Names[ExpansionWorld.Selected]+" · "+Vector3.Distance(game.Player.transform.position,ExpansionWorld.Places[ExpansionWorld.Selected]).ToString("0")+" m":selectedSite>=0?UrbanCatalog.Name(selectedSite)+" · "+UrbanCatalog.Descriptions[UrbanCatalog.Kind(selectedSite)]:"시설이나 지점을 선택하면 길 안내를 시작합니다.";
                }
                wasMapOpen=open;
            }

            if (selectedSite >= 0 && active && !sim.MapOpen)
            {
                var d = UrbanCatalog.Door(selectedSite) - game.Player.transform.position;
                string dir = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? d.x > 0 ? "동쪽 →" : "← 서쪽" : d.z > 0 ? "북쪽 ↑" : "남쪽 ↓";
                objective.text = $"{UrbanCatalog.Name(selectedSite)} · {dir} {d.magnitude:0} m   /   M 목적지 변경";
            }
        }
    }
}
