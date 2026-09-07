using System;
using System.Collections;
using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void Wardrobe()
        {
            Panel("wardrobe", "서하의 옷장", "현재 의상: " + ActorWardrobe.Names[LifeState.Outfit] + "\n의상은 모든 무기와 이동 모션에 적용됩니다.");
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                if ((LifeState.Outfits & (1 << i)) != 0)
                    Option(ActorWardrobe.Names[i] + (i == LifeState.Outfit ? " · 착용 중" : " · 착용"), () =>
                    {
                        LifeState.Wear(index);
                        Wardrobe();
                    });
            }
        }

        public void SleepMenu()
        {
            Panel("sleep", "서하의 침대", "잠을 자면 체력과 에너지가 회복되고 도시의 시간이 흐릅니다.");
            Option("6시간 취침", () => Rest(6, 0));
            Option("8시간 취침", () => Rest(8, 0));
            Option("다음 아침 7시까지", () => Rest(Mathf.Repeat(7 - LifeState.Hour + 23.999f, 24) + .001f, 0));
        }

        void Rest(float hours, int cost)
        {
            if (WantedSystem.Level > 0)
            {
                Body = "추격 중에는 잠들 수 없습니다. 먼저 안전한 곳을 찾으세요.";
                Revision++;
                return;
            }

            if (!LifeState.Spend(cost))
            {
                Body = "숙박료가 부족합니다.";
                Revision++;
                return;
            }

            StartCoroutine(SleepRoutine(hours));
        }

        IEnumerator SleepRoutine(float hours)
        {
            Panel("sleeping", "취침 중", "도시의 불빛이 천천히 지나갑니다.");
            Vector3 before = game.Player.transform.position;
            var actor = game.Player.GetComponent<PixelActor>();
            var bed = Array.Find(FindObjectsByType<Transform>(FindObjectsSortMode.None), t => t.name == "Mattress");
            if (bed)
            {
                game.Player.Controller.enabled = false;
                game.Player.transform.position = bed.position + new Vector3(1.2f, .2f, 0);
                actor.Pose(0, 1, 0, 88);
                game.CameraRig.Snap();
            }

            for (float t = 0; t < .8f; t += Time.unscaledDeltaTime)
            {
                SleepFade = t / .8f;
                yield return null;
            }

            SleepFade = 1;
            LifeState.Hours += Mathf.Clamp(hours, .1f, 24);
            game.Player.Heal(100);
            game.Player.RestoreEnergy(100);
            LifeState.Save();
            yield return null;
            for (float t = 0; t < .8f; t += Time.unscaledDeltaTime)
            {
                SleepFade = 1 - t / .8f;
                yield return null;
            }

            SleepFade = 0;
            game.Player.Respawn(before, false);
            Mode = "sleep";
            Dismiss();
            game.Toast("기상 · " + Mathf.FloorToInt(LifeState.Hour).ToString("00") + ":" + Mathf.FloorToInt((LifeState.Hour % 1) * 60).ToString("00") + " · 체력과 에너지가 회복되었습니다.", 5);
        }
    }
}
