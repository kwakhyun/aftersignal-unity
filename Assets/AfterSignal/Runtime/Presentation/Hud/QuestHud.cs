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
                Legible(mainRouteText);
                mainRoutePin = Label(root, "◆", 0, 0, 200, 44, 18, new Color(1, .84f, .45f));
                mainRoutePin.alignment = TextAnchor.MiddleCenter;
                Legible(mainRoutePin);
                questPoints = FindObjectsByType<InteractionPoint>();
                if (game.stage == StageId.UrbanCity)
                {
                    var go = new GameObject("Street navigation / directional ribbon");go.transform.SetParent(transform,false);routeRibbon=go.AddComponent<RouteRibbon>();
                    var panel=Panel(root,"Navigation guidance",0,0,320,116,new Color(.025f,.08f,.105f,.94f));navigationCard=panel.gameObject;
                    // Anchored from the top under lifeQuest (354..442): bottom-anchored, larger UI scales slid it under the wanted / errand lines.
                    var r=panel.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-28,-452);
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
                    navigation.Update(game.Player.transform.position,mainGoal,vehicle?vehicle.Forward:WalkingForward());roadRoute=navigation.Route;
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
                // Each number once: turn distance on the pin, remaining distance and time here.
                navigationStats.text=navigation.Arrived?KeyHints.Apply("입구에서 E로 상호작용"):$"남은 {navigation.Remaining:0} m  ·  약 {Mathf.Max(1,Mathf.CeilToInt(navigation.Remaining/pace/60))}분";}
            if (!show)
                return;
            Vector3 delta = mainGoal - game.Player.transform.position;
            // One distance everywhere: the road distance when a route exists, otherwise the straight line.
            float distance = roadRoute != null && navigation.Connected && !navigation.Arrived ? navigation.Remaining : delta.magnitude;
            string heading = Mathf.Abs(delta.y) > 6 && delta.magnitude < 18 ? delta.y > 0 ? "위층 ↑" : "아래층 ↓" : RelativeDirection(delta);
            // The label is often the objective text, which carries default keys. With the navigation card up, its remaining distance is the only one shown.
            mainRouteText.text = KeyHints.Apply(mainLabel) + "\n" + heading + (navigationCard && navigationCard.activeSelf ? "" : "  ·  " + distance.ToString("0") + " m");
            Vector3 next = mainGoal;
            if (roadRoute != null && !navigation.Arrived)next=navigation.Next+Vector3.up*.8f;

            var cam = Camera.main;
            var screen = cam.WorldToScreenPoint(next);
            bool behind = screen.z < 0;
            float floor = PinFloor();
            if (behind)
            {
                // A mirrored projection can land mid-screen. Park the pin on the border instead, toward the target's side, with "behind" reading as down.
                var local = cam.transform.InverseTransformPoint(next);
                var dir = new Vector2(local.x, -Mathf.Abs(local.z));
                dir = dir.sqrMagnitude > .0001f ? dir.normalized : Vector2.down;
                var centre = new Vector2(Screen.width * .5f, Screen.height * .5f);
                float tx = dir.x > 0 ? (Screen.width - 110 - centre.x) / dir.x : dir.x < 0 ? (110 - centre.x) / dir.x : float.MaxValue;
                float ty = (floor - centre.y) / dir.y;
                var edge = centre + dir * Mathf.Max(0, Mathf.Min(tx, ty));
                screen.x = edge.x;
                screen.y = edge.y;
            }

            screen.x = Mathf.Clamp(screen.x, 110, Screen.width - 110);
            screen.y = Mathf.Clamp(screen.y, floor, Screen.height - 120);
            PlaceScreen(mainRoutePin.rectTransform, screen, new Vector2(100, 0));
            string pin = roadRoute != null ? navigation.Arrived?"◆  도착":navigation.Arrow+"  "+navigation.TurnDistance.ToString("0")+" m" : "◆  " + distance.ToString("0") + " m";
            mainRoutePin.text = behind ? RelativeDirection(next - game.Player.transform.position) + "  " + pin : pin;
        }

        /// <summary>Lowest screen y in pixels for the route pin, whose label hangs 44 units below that point: clear of the bottom-centre prompts, which are drawn over it.</summary>
        float PinFloor()
        {
            float top = 0;
            void Over(Component c, bool shown) { if (c && shown) { var r = (RectTransform)c.transform; top = Mathf.Max(top, r.anchoredPosition.y + r.sizeDelta.y); } }
            Over(anchorHint, anchorHint && anchorHint.text.Length > 0);
            Over(drivingInfo, drivingInfo && drivingInfo.gameObject.activeSelf && drivingInfo.text.Length > 0);
            Over(interactionCard, interactionCard && interactionCard.gameObject.activeSelf);
            Over(bossPanel ? bossPanel.transform : null, bossPanel && bossPanel.activeSelf);
            return Mathf.Max(190, (top + 50) * canvas.scaleFactor);
        }

        /// <summary>On foot the route instruction follows where Seo is actually moving, not where the camera happens to look.</summary>
        Vector3 WalkingForward()
        {
            var v = game.Player.Velocity; v.y = 0;
            return v.sqrMagnitude > 1f ? v.normalized : game.CameraRig.LookForward;
        }

        /// <summary>Direction to a target relative to the current view (앞/뒤/좌/우), which reads faster than compass words.</summary>
        string RelativeDirection(Vector3 delta)
        {
            delta.y = 0;
            if (delta.sqrMagnitude < .01f) return "현재 위치";
            float angle = Vector3.SignedAngle(game.CameraRig.LookForward, delta, Vector3.up);
            return Mathf.Abs(angle) <= 35 ? "앞쪽 ↑" : Mathf.Abs(angle) >= 145 ? "뒤쪽 ↓" : angle > 0 ? (angle < 80 ? "오른쪽 앞 ↗" : angle > 110 ? "오른쪽 뒤 ↘" : "오른쪽 →") : (angle > -80 ? "왼쪽 앞 ↖" : angle < -110 ? "왼쪽 뒤 ↙" : "왼쪽 ←");
        }

        void StartNavigation(){navigationMuted=false;navigation.Reset();nextQuest=0;}
        void ClearNavigation(){navigationMuted=true;customMapGoal=false;selectedSite=-1;ExpansionWorld.Selected=-1;FourCityAtlasSelection.Clear();navigation.Reset();roadRoute=null;atlas?.SetRoute(null);miniAtlas?.SetRoute(null);routeRibbon?.Show(false);nextQuest=0;game.Toast("길 안내를 해제했습니다. 지도에서 새 목적지를 선택하세요.");}

    }
}
