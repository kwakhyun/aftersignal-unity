using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        GameObject cityMapOverlay;
        RectTransform cityMapPin, cityMiniPin;
        Text drivingInfo, cityDestination;
        int selectedSite = -1;
        float nextCityText;
        void BuildUrbanMap(Transform parent)
        {
            Label(parent, "애프터라이트   /   M 전체 지도", 12, 8, 296, 20, 12, mint, FontStyle.Bold);
            for (int i = 0; i < 6; i++)
                Panel(parent, "Avenue", 24 + i * 51, 31, 3, 117, new Color(.22f, .4f, .46f));
            for (int i = 0; i < 5; i++)
                Panel(parent, "Cross avenue", 20, 38 + i * 26, 272, 3, new Color(.22f, .4f, .46f));
            for (int i = 0; i < UrbanCatalog.SiteCount; i++)
            {
                var p = UrbanCatalog.Center(i);
                int kind = UrbanCatalog.Kind(i);
                Panel(parent, "Facility", 20 + p.x / 790 * 272, 145 - (p.z + 330) / 660 * 116, 3, 3, kind == 11 ? new Color(1, .61f, .26f) : new Color(.51f, .65f, .7f));
            }

            miniRoute = AddMapRoute(parent, false);
            cityMiniPin = Label(parent, "◆", 0, 0, 14, 16, 13, new Color(1, .84f, .36f)).rectTransform;
        }

        void EnsureCityHud()
        {
            if (drivingInfo)
                return;
            drivingInfo = Label(root, "", 0, 0, 720, 90, 21, white, FontStyle.Bold);
            CenterBottom(drivingInfo.rectTransform, 45, 720, 90);
            drivingInfo.alignment = TextAnchor.MiddleCenter;
            if (game.stage != StageId.UrbanCity)
                return;
            cityMapOverlay = Panel(root, "City atlas", 0, 0, 1450, 760, new Color(.025f, .055f, .075f, .98f)).gameObject;
            Center(cityMapOverlay.GetComponent<RectTransform>(), 1450, 760);
            Label(cityMapOverlay.transform, "AFTERLIGHT  /  도시 안내", 36, 24, 1000, 45, 29, white, FontStyle.Bold);
            Label(cityMapOverlay.transform, "금색: 메인 경로 · 시설 선택: 방향과 거리      M 닫기", 36, 76, 1200, 25, 16, mint);
            for (int i = 0; i < 6; i++)
                Panel(cityMapOverlay.transform, "North avenue", 48 + (40 + i * 140) / 790f * 790, 127, 12, 557, new Color(.2f, .34f, .4f));
            for (int i = 0; i < 5; i++)
                Panel(cityMapOverlay.transform, "Cross avenue", 55, 676 - (-280 + i * 140 + 330) / 660f * 550, 780, 12, new Color(.2f, .34f, .4f));
            for (int i = 0; i < UrbanCatalog.SiteCount; i++)
            {
                int id = i;
                var p = UrbanCatalog.Center(i);
                var button = MakeButton(cityMapOverlay.transform, (i + 1).ToString("00"), 55 + p.x, 675 - (p.z + 330) / 660 * 550, 32, 27, () => selectedSite = id);
                button.GetComponentInChildren<Text>().fontSize = 12;
                Rect(button.GetComponentInChildren<Text>().rectTransform, 1, 4, 30, 20);
                button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
                var list = MakeButton(cityMapOverlay.transform, $"{i + 1:00}  {UrbanCatalog.Name(i)}", 872 + (i / 20) * 275, 125 + (i % 20) * 27, 264, 24, () => selectedSite = id);
                list.GetComponentInChildren<Text>().fontSize = 12;
            }

            largeRoute = AddMapRoute(cityMapOverlay.transform, true);
            cityMapPin = Label(cityMapOverlay.transform, "◆", 0, 0, 25, 26, 23, new Color(1, .83f, .28f), FontStyle.Bold).rectTransform;
            cityDestination = Label(cityMapOverlay.transform, "시설을 선택하세요", 45, 708, 1250, 28, 18, mint);
            cityMapOverlay.SetActive(false);
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
            if (Time.unscaledTime >= nextCityText)
            {
                nextCityText = Time.unscaledTime + .1f;
                drivingInfo.text = sim.Current ? $"{Mathf.Abs(sim.Current.speed) * 3.6f:000} km/h    ·    연료 {sim.Current.fuel:0.0} L    ·    차체 {sim.Current.health:0}%\n" + (sim.Current.fuel < 3 ? "연료 부족 · 가까운 주유소로 이동하세요" : "W/S 가속·후진   A/D 조향   SPACE 제동   E 하차") : selectedSite >= 0 ? $"{UrbanCatalog.Name(selectedSite)}  ·  {Vector3.Distance(game.Player.transform.position, UrbanCatalog.Door(selectedSite)):0} m" : "";
            }

            arsenalPanel.SetActive(!sim.Driving);
            weapon.transform.parent.gameObject.SetActive(!sim.Driving);
            drivingInfo.gameObject.SetActive(active);
            if (cityMiniPin)
            {
                var p = game.Player.transform.position;
                cityMiniPin.anchoredPosition = new Vector2(16 + p.x / 790 * 272, -(145 - (p.z + 330) / 660 * 116));
            }

            if (cityMapOverlay)
            {
                cityMapOverlay.SetActive(sim.MapOpen && active);
                if (sim.MapOpen && active)
                {
                    Cursor.visible = true;
                    cursor.gameObject.SetActive(false);
                    var p = game.Player.transform.position;
                    cityMapPin.anchoredPosition = new Vector2(52 + p.x, -(676 - (p.z + 330) / 660 * 550));
                    cityDestination.text = selectedSite < 0 ? "번호 또는 시설 이름을 선택하면 이동 방향과 거리를 표시합니다." : UrbanCatalog.Name(selectedSite) + "  ·  " + UrbanCatalog.Descriptions[UrbanCatalog.Kind(selectedSite)];
                }
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
