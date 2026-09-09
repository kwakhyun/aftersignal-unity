using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        Text mainRouteText, mainRoutePin;
        InteractionPoint[] questPoints;
        LineRenderer questLine;
        RouteRibbon routeRibbon;readonly NavigationGuide navigation=new();
        GameObject navigationCard;Text navigationTurn,navigationStats,navigationBearing;bool navigationMuted;
        float nextQuest;
        Vector3 mainGoal;
        string mainLabel;
        bool hasGoal;
        List<Vector3> roadRoute;
        bool customMapGoal; Vector3 mapGoal;
        void UpdateQuestHud()
        {
            if (!mainRouteText)
            {
                mainRouteText = Label(root, "", 0, 0, 560, 54, 16, new Color(1, .84f, .5f));
                CenterTop(mainRouteText.rectTransform, 30, 560, 54);
                mainRouteText.alignment = TextAnchor.MiddleCenter;
                mainRoutePin = Label(root, "◆", 0, 0, 200, 44, 18, new Color(1, .84f, .45f));
                mainRoutePin.alignment = TextAnchor.MiddleCenter;
                questPoints = FindObjectsByType<InteractionPoint>();
                if (game.stage == StageId.UrbanCity)
                {
                    var go = new GameObject("Street navigation / directional ribbon");go.transform.SetParent(transform,false);routeRibbon=go.AddComponent<RouteRibbon>();
                    var panel=Panel(root,"Navigation guidance",0,0,320,116,new Color(.025f,.08f,.105f,.94f));navigationCard=panel.gameObject;
                    var r=panel.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-28,330);
                    Panel(panel.transform,"Guidance accent",0,0,3,116,new Color(.35f,.87f,.77f));
                    navigationBearing=Label(panel.transform,"",18,10,288,20,12,mint);
                    navigationTurn=Label(panel.transform,"",18,34,288,37,20,white,FontStyle.Bold);
                    navigationStats=Label(panel.transform,"",18,82,288,22,13,muted);
                }
            }

            if (Time.unscaledTime >= nextQuest)
            {
                nextQuest = Time.unscaledTime + .5f;
                hasGoal = QuestGuidance.Resolve(game, questPoints, out mainGoal, out mainLabel);
                if(game.stage==StageId.UrbanCity&&ExpansionWorld.Selected>=0){hasGoal=true;mainGoal=ExpansionWorld.Places[ExpansionWorld.Selected];mainLabel=ExpansionWorld.Names[ExpansionWorld.Selected];}
                if(game.stage==StageId.UrbanCity&&selectedSite>=0){hasGoal=true;mainGoal=UrbanCatalog.Door(selectedSite);mainLabel=UrbanCatalog.Name(selectedSite);}
                if(game.stage==StageId.UrbanCity&&customMapGoal){hasGoal=true;mainGoal=mapGoal;mainLabel="지도 경유지";}
                if(game.stage==StageId.UrbanCity&&FourCityAtlasSelection.Venue!=null){hasGoal=true;mainGoal=FourCityAtlasSelection.Goal;mainLabel=FourCityAtlasSelection.Label;}
                if (routeRibbon && hasGoal&&!navigationMuted)
                {
                    var vehicle=UrbanSimulation.Instance?UrbanSimulation.Instance.Current:null;
                    navigation.Update(game.Player.transform.position,mainGoal,vehicle?vehicle.Forward:game.CameraRig.LookForward);roadRoute=navigation.Route;
                    routeRibbon.Draw(roadRoute,game.Player.transform.position,navigation.Connected&&!navigation.Arrived);
                    atlas?.SetRoute(roadRoute);miniAtlas?.SetRoute(roadRoute);
                }
                else{roadRoute=null;atlas?.SetRoute(null);miniAtlas?.SetRoute(null);}
            }

            bool show = !game.Blocked && hasGoal;
            mainRouteText.gameObject.SetActive(show);
            mainRoutePin.gameObject.SetActive(show && !(UrbanSimulation.Instance && UrbanSimulation.Instance.MapOpen));
            if (questLine)
                questLine.enabled = show && !OceanLife.Swimming && !(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current&&UrbanSimulation.Instance.Current.IsSpecial);
            bool guidance=show&&!navigationMuted&&!(UrbanSimulation.Instance&&UrbanSimulation.Instance.MapOpen);
            routeRibbon?.Show(guidance&&!navigation.Arrived&&navigation.Connected&&!OceanLife.Swimming&&!(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current&&UrbanSimulation.Instance.Current.IsSpecial));
            if(navigationCard){navigationCard.SetActive(guidance);navigationBearing.text=NavigationGuide.Bearing(game.CameraRig.LookForward)+"   /   NAVIGATION";
                navigationTurn.text=navigation.Arrow+"  "+(navigation.Arrived?"목적지 도착":navigation.Instruction);
                float pace=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current?Mathf.Max(5,Mathf.Abs(UrbanSimulation.Instance.Current.speed)):4.8f;
                navigationStats.text=navigation.Arrived?"입구에서 E로 상호작용":$"{navigation.TurnDistance:0} m 앞  ·  남은 {navigation.Remaining:0} m  ·  약 {Mathf.Max(1,Mathf.CeilToInt(navigation.Remaining/pace/60))}분";}
            if (!show)
                return;
            Vector3 delta = mainGoal - game.Player.transform.position;
            float distance = delta.magnitude;
            string heading = Mathf.Abs(delta.y) > 6 && distance < 18 ? delta.y > 0 ? "위층 ↑" : "아래층 ↓" : Mathf.Abs(delta.x) > Mathf.Abs(delta.z) ? delta.x > 0 ? "동쪽" : "서쪽" : delta.z > 0 ? "북쪽" : "남쪽";
            mainRouteText.text = mainLabel + "\n" + heading + "  ·  " + distance.ToString("0") + " m";
            Vector3 next = mainGoal;
            if (roadRoute != null && !navigation.Arrived)next=navigation.Next+Vector3.up*.8f;

            var screen = Camera.main.WorldToScreenPoint(next);
            if (screen.z < 0)
            {
                screen.x = Screen.width - screen.x;
                screen.y = Screen.height - screen.y;
            }

            screen.x = Mathf.Clamp(screen.x, 110, Screen.width - 110);
            screen.y = Mathf.Clamp(screen.y, 190, Screen.height - 120);
            PlaceScreen(mainRoutePin.rectTransform, screen, new Vector2(100, 0));
            mainRoutePin.text = roadRoute != null ? navigation.Arrived?"◆  도착":navigation.Arrow+"  "+navigation.TurnDistance.ToString("0")+" m" : "◆  " + distance.ToString("0") + " m";
        }

        void StartNavigation(){navigationMuted=false;navigation.Reset();nextQuest=0;}
        void ClearNavigation(){navigationMuted=true;customMapGoal=false;selectedSite=-1;ExpansionWorld.Selected=-1;FourCityAtlasSelection.Clear();navigation.Reset();roadRoute=null;atlas?.SetRoute(null);miniAtlas?.SetRoute(null);routeRibbon?.Show(false);nextQuest=0;game.Toast("길 안내를 해제했습니다. 지도에서 새 목적지를 선택하세요.");}

    }
}
