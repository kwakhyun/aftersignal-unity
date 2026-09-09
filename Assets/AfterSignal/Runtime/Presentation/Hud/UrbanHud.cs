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
        Text atlasScale,miniAtlasTitle;
        void BuildUrbanMap(Transform parent)
        {
            miniAtlasTitle=Label(parent, "AFTERLIGHT / N ↑    M 지도 탐색",12,8,296,20,12,mint,FontStyle.Bold);
            miniAtlas=CreateAtlas(parent,20,32,272,116,true);
            Label(parent,"N ↑",265,127,32,20,12,mint,FontStyle.Bold);
        }
        AtlasViewport CreateAtlas(Transform parent,float x,float y,float w,float h,bool mini)
        {
            var go=new GameObject("Local street atlas",typeof(RectTransform),typeof(AtlasViewport));go.transform.SetParent(parent,false);
            var map=go.GetComponent<AtlasViewport>();Rect(map.rectTransform,x,y,w,h);map.mini=mini;map.raycastTarget=!mini;map.Focus(game.Player.transform.position,mini?360:820);
            map.Select=(id,expanded)=>{FourCityAtlasSelection.Clear();selectedSite=expanded?-1:id;ExpansionWorld.Selected=expanded?id:-1;map.follow=false;map.Focus(expanded?ExpansionWorld.Places[id]:UrbanCatalog.Center(id));customMapGoal=false;StartNavigation();};
            map.SelectVenue=v=>{SelectAtlasVenue(v);};if(!mini)map.gameObject.AddComponent<AtlasLabels>().Initialize(map);
            map.ClearWaypoint=ClearNavigation;
            map.Waypoint=p=>{FourCityAtlasSelection.Clear();p.y=FourCityCatalog.Centers[FourCityCatalog.CityAt(p)].y;if(NpcGroundSupport.Floor(p,p.y+2,4,out float floor))p.y=floor+.05f;mapGoal=p;customMapGoal=true;selectedSite=-1;ExpansionWorld.Selected=-1;StartNavigation();game.Toast("목적지 지정 · 이동하면 방향과 다음 회전을 안내합니다");};return map;
        }
        void EnsureCityHud()
        {
            if(drivingInfo)return;
            drivingInfo=Label(root,"",0,0,900,90,19,white,FontStyle.Bold);CenterBottom(drivingInfo.rectTransform,45,900,90);drivingInfo.alignment=TextAnchor.MiddleCenter;
            if(game.stage!=StageId.UrbanCity)return;
            cityMapOverlay=Panel(root,"City atlas",0,0,1450,820,new Color(.025f,.055f,.075f,.98f)).gameObject;Center(cityMapOverlay.GetComponent<RectTransform>(),1450,820);
            var mapCanvas=cityMapOverlay.AddComponent<Canvas>();mapCanvas.overrideSorting=true;mapCanvas.sortingOrder=100;
            cityMapOverlay.AddComponent<GraphicRaycaster>();
            Label(cityMapOverlay.transform,"CITY ATLAS / 네 도시를 탐색하세요",30,22,1080,42,29,white,FontStyle.Bold);
            Label(cityMapOverlay.transform,"휠 확대·축소 · 드래그 탐색 · 클릭 목적지 지정 · 우클릭 길 안내 해제",30,752,1030,20,14,mint);
            MakeButton(cityMapOverlay.transform,"닫기 ESC",1290,28,126,36,()=>UrbanSimulation.Instance?.CloseMap());
            atlas=CreateAtlas(cityMapOverlay.transform,30,120,1000,630,false);
            MakeButton(cityMapOverlay.transform,"+",40,132,40,40,()=>atlas.Zoom(.7f,atlas.rectTransform.rect.center));
            MakeButton(cityMapOverlay.transform,"−",40,178,40,40,()=>atlas.Zoom(1.4f,atlas.rectTransform.rect.center));
            MakeButton(cityMapOverlay.transform,"내 위치",886,132,130,40,()=>atlas.Recenter());
            MakeButton(cityMapOverlay.transform,"목적지 보기",732,132,144,40,()=>{if(hasGoal){atlas.follow=false;atlas.Focus(mainGoal,650);}});
            MakeButton(cityMapOverlay.transform,"길 안내 해제",886,180,130,34,ClearNavigation);
            atlasScale=Label(cityMapOverlay.transform,"",45,710,950,28,15,mint);
            BuildAtlasExplorer();
            cityDestination=Label(cityMapOverlay.transform,"",32,773,1370,28,17,mint);cityMapOverlay.SetActive(false);
        }

        void UpdateUrbanHud()
        {
            if(miniAtlasTitle&&game&&game.Ready)miniAtlasTitle.text=new[]{"애프터라이트","노바 시티","에레보스","네레이드"}[FourCityCatalog.CityAt(game.Player.transform.position)]+" / "+NavigationGuide.Bearing(game.CameraRig.LookForward);
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
                drivingInfo.text = sim.Current ? $"{Mathf.Abs(sim.Current.speed) * 3.6f:000} km/h    ·    연료 {sim.Current.fuel:0.0} L    ·    차체 {sim.Current.HealthFraction*100:0}%" + (sim.Current.fuel < 3 ? "\n연료 부족 · 가까운 주유소로 이동하세요" : "") : selectedSite >= 0 ? $"{UrbanCatalog.Name(selectedSite)}  ·  {Vector3.Distance(game.Player.transform.position, UrbanCatalog.Door(selectedSite)):0} m" : "";
            }

            arsenalPanel.SetActive(!sim.Driving);
            weapon.transform.parent.gameObject.SetActive(!sim.Driving);
            drivingInfo.gameObject.SetActive(active);
            if(cityMapOverlay)
            {
                bool open=sim.MapOpen&&active;cityMapOverlay.SetActive(open);
                if(open)
                {
                    if(!wasMapOpen){atlas.Recenter();if(FourCityAtlasSelection.Venue!=null)atlas.Focus(FourCityAtlasSelection.Venue.position,700);}UpdateAtlasExplorer();Cursor.visible=true;cursor.gameObject.SetActive(false);
                    atlasScale.text="N ↑ 북쪽 고정  ·  시선 "+NavigationGuide.Bearing(game.CameraRig.LookForward)+"    |    표시 폭 "+atlas.Span.ToString("0")+" m    ◆ 목적지  ▲ 현재 방향";
                    cityDestination.text=FourCityAtlasSelection.Venue!=null?FourCityAtlasSelection.Label+" · "+Vector3.Distance(game.Player.transform.position,FourCityAtlasSelection.Goal).ToString("0")+" m · "+(FourCityAtlasSelection.Venue.city==3?"기밀 해저 도로 이용":"도로 안내 중"):customMapGoal?"경유지 · "+Vector3.Distance(game.Player.transform.position,mapGoal).ToString("0")+" m":ExpansionWorld.Selected>=0?ExpansionWorld.Names[ExpansionWorld.Selected]+" · "+Vector3.Distance(game.Player.transform.position,ExpansionWorld.Places[ExpansionWorld.Selected]).ToString("0")+" m":selectedSite>=0?UrbanCatalog.Name(selectedSite)+" · "+UrbanCatalog.Descriptions[UrbanCatalog.Kind(selectedSite)]:"시설이나 지점을 선택하면 길 안내를 시작합니다.";
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
