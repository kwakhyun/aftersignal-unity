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
                    var go = new GameObject("MAIN QUEST / street route");
                    questLine = go.AddComponent<LineRenderer>();
                    questLine.sharedMaterial = Resources.Load<Material>("Materials/CyanFX");
                    questLine.widthMultiplier = .1f;
                    questLine.numCornerVertices = 2;
                    questLine.startColor = questLine.endColor = new Color(.35f, .7f, .64f, .65f);
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
                if (questLine && hasGoal)
                {
                    roadRoute = FourCityNavigation.Route(game.Player.transform.position, mainGoal);
                    questLine.positionCount = roadRoute.Count;
                    for (int i = 0; i < roadRoute.Count; i++)
                    {
                        var p = roadRoute[i];
                        p.y += .16f;
                        questLine.SetPosition(i, p);
                    }

                    atlas?.SetRoute(roadRoute);miniAtlas?.SetRoute(roadRoute);
                }
            }

            bool show = !game.Blocked && hasGoal;
            mainRouteText.gameObject.SetActive(show);
            mainRoutePin.gameObject.SetActive(show && !(UrbanSimulation.Instance && UrbanSimulation.Instance.MapOpen));
            if (questLine)
                questLine.enabled = show && !OceanLife.Swimming && !(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current&&UrbanSimulation.Instance.Current.IsSpecial);
            if (!show)
                return;
            Vector3 delta = mainGoal - game.Player.transform.position;
            float distance = delta.magnitude;
            string heading = Mathf.Abs(delta.y) > 6 && distance < 18 ? delta.y > 0 ? "위층 ↑" : "아래층 ↓" : Mathf.Abs(delta.x) > Mathf.Abs(delta.z) ? delta.x > 0 ? "동쪽" : "서쪽" : delta.z > 0 ? "북쪽" : "남쪽";
            mainRouteText.text = mainLabel + "\n" + heading + "  ·  " + distance.ToString("0") + " m";
            Vector3 next = mainGoal;
            if (roadRoute != null)
                for (int i = 1; i < roadRoute.Count; i++)
                    if (Vector3.Distance(game.Player.transform.position, roadRoute[i]) > 7)
                    {
                        next = roadRoute[i] + Vector3.up * .8f;
                        break;
                    }

            var screen = Camera.main.WorldToScreenPoint(next);
            if (screen.z < 0)
            {
                screen.x = Screen.width - screen.x;
                screen.y = Screen.height - screen.y;
            }

            screen.x = Mathf.Clamp(screen.x, 110, Screen.width - 110);
            screen.y = Mathf.Clamp(screen.y, 190, Screen.height - 120);
            PlaceScreen(mainRoutePin.rectTransform, screen, new Vector2(100, 0));
            mainRoutePin.text = roadRoute != null ? "◆  메인 경로" : "◆  " + distance.ToString("0") + " m";
        }

    }
}
