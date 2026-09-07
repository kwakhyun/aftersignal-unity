using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        Text arsenalStatus, skillStatus, coreTarget;
        Text[] capacitorLabels = new Text[2];
        Image reloadBar;
        RectTransform mapPlayer;
        GameObject civicMap, arsenalPanel;
        float nextArsenalText;
        void BuildCivicHud(RectTransform objectivePanel)
        {
            var panel = Panel(root, "Arsenal", 30, 181, 316, 72, ink).rectTransform;
            arsenalPanel = panel.gameObject;
            Panel(panel, "Arsenal edge", 0, 0, 3, 72, new Color(.8f, .58f, .3f));
            arsenalStatus = Label(panel, "", 18, 9, 283, 24, 15, white, FontStyle.Bold);
            skillStatus = Label(panel, "", 18, 37, 282, 23, 12, muted);
            reloadBar = Panel(panel, "Reload progress", 18, 64, 0, 3, mint);
            coreTarget = Label(root, "", 0, 0, 210, 65, 21, new Color(1, .79f, .42f), FontStyle.Bold);
            coreTarget.alignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 2; i++)
            {
                capacitorLabels[i] = Label(root, "", 0, 0, 188, 67, 16, mint, FontStyle.Bold);
                capacitorLabels[i].alignment = TextAnchor.MiddleCenter;
            }

            civicMap = Panel(root, "District map", 0, 0, 320, 167, ink).gameObject;
            Right(civicMap.GetComponent<RectTransform>(), 30, 28, 320, 167);
            civicMap.transform.localScale = Vector3.one * .88f;
            if (game.stage == StageId.UrbanCity)
            {
                BuildUrbanMap(civicMap.transform);
                return;
            }

            if (game.stage != StageId.Haven)
            {
                BuildRouteMinimap(civicMap.transform);
                return;
            }

            Label(civicMap.transform, "AFTERLIGHT   /   N ↑", 14, 8, 290, 20, 12, mint, FontStyle.Bold);
            Panel(civicMap.transform, "Main avenue", 14, 113, 292, 10, new Color(.31f, .32f, .29f));
            Panel(civicMap.transform, "North avenue", 14, 73, 292, 7, new Color(.28f, .29f, .27f));
            foreach (float x in new[]
            {
                42f,
                94f,
                138f,
                204f
            }

            )
                Panel(civicMap.transform, "Cross street", 14 + x / 234 * 292, 57, 6, 82, new Color(.28f, .29f, .27f));
            string[] names =
            {
                "집",
                "학교",
                "병원",
                "본부"
            };
            float[] px =
            {
                16,
                64,
                113,
                178
            };
            for (int i = 0; i < 4; i++)
            {
                float x = 14 + px[i] / 234 * 292;
                Panel(civicMap.transform, names[i], x - 6, 48, 12, 10, new Color(.78f, .57f, .32f));
                Label(civicMap.transform, names[i], x - 16, 29, 48, 18, 11, white);
            }

            mapPlayer = Label(civicMap.transform, "◆", 0, 0, 18, 20, 16, mint, FontStyle.Bold).rectTransform;
            Label(civicMap.transform, "주거 · 교육 · 의료 · 임무", 14, 143, 290, 18, 11, muted);
        }

        void UpdateCivicHud()
        {
            var p = game.Player;
            arsenalPanel.SetActive(true);
            if (Time.unscaledTime >= nextArsenalText)
            {
                nextArsenalText = Time.unscaledTime + .08f;
                arsenalStatus.text = p.Weapon == WeaponId.Pistol ? (p.Reloading ? $"장전 중  {p.ReloadProgress * 100:0}%" : $"{p.Ammo:00} / {game.tuning.magazineSize:00}     R  장전") : p.AttackTime > 0 ? $"{(p.Action == WeaponAction.Dash ? "대시 공격" : p.Action == WeaponAction.Skill ? p.SkillName : "연속 검격  " + (p.Combo + 1))}" : "SHIFT + LMB  대시 공격";
                skillStatus.text = $"Q  {p.SkillName}   ·   " + (p.SkillCooldown > 0 ? $"{p.SkillCooldown:0.0}s" : p.Energy < game.tuning.skillCost ? "에너지 부족" : "준비 완료");
                reloadBar.rectTransform.sizeDelta = new Vector2(p.Reloading ? 280 * p.ReloadProgress : 0, 3);
            }

            if (mapPlayer)
            {
                Vector3 pos = p.transform.position;
                mapPlayer.anchoredPosition = game.stage == StageId.Haven ? new Vector2(14 + Mathf.Clamp01(pos.x / 234) * 292, -(116 - Mathf.Clamp(pos.z, -15, 35) * 1.55f)) : new Vector2(14 + Mathf.Clamp01(pos.x / game.stageLength) * 292, -(133 - Mathf.Clamp(pos.y, 0, 36) * 2.6f));
            }

            bool boss = game.Boss && game.Boss.Alive && game.Boss.Active && !game.Blocked;
            coreTarget.gameObject.SetActive(boss && game.ExposeTimer > 0);
            if (boss && game.ExposeTimer > 0)
            {
                float dist = Mathf.Abs(p.transform.position.x - game.Boss.transform.position.x);
                coreTarget.text = "[  CORE  ]\n" + (dist > 3.5f ? "접근 → 검으로 공격" : "LMB  검격  ·  지금!");
                PlaceWorld(coreTarget.rectTransform, game.Boss.transform.position + Vector3.up * 2.3f, new Vector2(105, 32));
            }

            for (int i = 0; i < 2; i++)
            {
                capacitorLabels[i].gameObject.SetActive(boss && game.ExposeTimer <= 0);
                if (!boss || game.ExposeTimer > 0)
                    continue;
                foreach (var a in GrappleAnchor.All)
                    if (a && a.capacitor == i)
                    {
                        bool charged = (game.Capacitors & (1 << i)) != 0;
                        capacitorLabels[i].text = charged ? $"✓  {i + 1:00} 완료" : $"[  {i + 1:00}  ]\nRMB  로프 연결";
                        capacitorLabels[i].color = charged ? muted : mint;
                        PlaceWorld(capacitorLabels[i].rectTransform, a.transform.position, new Vector2(94, 64));
                        break;
                    }
            }
        }
    }
}
