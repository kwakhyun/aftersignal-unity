using UnityEngine;

namespace AfterSignal
{
    public static class QuestGuidance
    {
        public static bool Resolve(GameDirector game, InteractionPoint[] points, out Vector3 target, out string label)
        {
            target = game.Player.transform.position;
            label = game.Objective;
            InteractionKind kind = InteractionKind.MissionBoard;
            bool first = PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed", 0) == 0;
            int chapter = CampaignCatalog.NextChapter;
            bool needNoa = !first && chapter > 0 && PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Expansion.Accepted", 0) != chapter;
            if (game.stage == StageId.UrbanCity)
            {
                if (needNoa)
                {
                    target = new Vector3(30, 1.2f, -299);
                    label = "메인 의뢰 · 주거지의 노아에게";
                }
                else
                {
                    target = UrbanCatalog.Door(14) + Vector3.up;
                    label = first ? "메인 의뢰 · 본부에서 중앙역 조사" : chapter > 0 ? "메인 의뢰 · 본부 작전 단말" : "메인 노선 복구 완료 · 본부";
                }

                return true;
            }

            if (game.stage == StageId.Residence)
            {
                if (game.Player.transform.position.y > 18)
                {
                    foreach (var p in points)
                        if (p && p.kind == InteractionKind.HomeDoor && p.door && !p.door.Open && game.Player.transform.position.x < 29)
                        {
                            target = p.transform.position;
                            label = "메인 경로 · 현관문 열기";
                            return true;
                        }

                    kind = InteractionKind.Lift;
                    label = "메인 경로 · 승강기로 1층 이동";
                }
                else
                {
                    kind = InteractionKind.ReturnTown;
                    label = "메인 경로 · 마을로 외출";
                }
            }
            else if (game.stage == StageId.Haven)
            {
                kind = first ? InteractionKind.FacilityTravel : needNoa ? InteractionKind.QuestGiver : chapter > 0 ? InteractionKind.MissionBoard : InteractionKind.HarborTravel;
                label = first ? "메인 의뢰 · 북쪽 신호복원본부" : needNoa ? "메인 의뢰 · 노아와 대화" : chapter > 0 ? "메인 의뢰 · 다음 노선 출발" : "메인 노선 완료 · 주민 의뢰";
            }
            else if (game.stage == StageId.UrbanInterior)
            {
                kind = needNoa || UrbanCatalog.Kind(UrbanCatalog.Current) != 14 && UrbanCatalog.Kind(UrbanCatalog.Current) != 10 ? InteractionKind.UrbanExit : first ? InteractionKind.FirstRail : InteractionKind.MissionBoard;
                label = kind == InteractionKind.UrbanExit ? "메인 경로 · 출입문으로 돌아가기" : first ? "메인 의뢰 · 중앙역 조사 시작" : "메인 의뢰 · 작전 단말";
            }
            else if (game.stage == StageId.School || game.stage == StageId.Clinic)
            {
                kind = InteractionKind.ReturnTown;
                label = "메인 경로 · 마을로 돌아가기";
            }
            else if (game.stage == StageId.Headquarters)
            {
                kind = needNoa ? InteractionKind.ReturnTown : first ? InteractionKind.FirstRail : InteractionKind.MissionBoard;
                label = needNoa ? "메인 의뢰 · 마을의 노아와 대화" : first ? "메인 의뢰 · 중앙역 조사 시작" : "메인 의뢰 · 도시 복원 작전";
            }
            else if (game.stage == StageId.Harbor)
            {
                kind = InteractionKind.HarborTravel;
                label = "메인 경로 · 마을로 돌아가기";
            }
            else if (game.stage == StageId.Station)
                kind = !game.Power ? InteractionKind.Power : InteractionKind.Board;
            else if (game.stage == StageId.Carriage)
                kind = !game.Release ? InteractionKind.Release : InteractionKind.Hatch;
            else if (game.stage == StageId.Roof)
            {
                kind = InteractionKind.Core;
                if (game.Boss && game.Boss.Alive)
                {
                    if (game.ExposeTimer <= 0)
                        foreach (var a in GrappleAnchor.All)
                            if (a && a.capacitor >= 0 && !a.Charged)
                            {
                                target = a.transform.position;
                                label = "메인 목표 · 축전기 로프 과부하";
                                return true;
                            }

                    target = game.Boss.transform.position + Vector3.up;
                    label = "메인 목표 · 노출된 코어 공격";
                    return true;
                }
            }
            else if (CampaignCatalog.Get(game.stage) != null)
                kind = !game.Power ? InteractionKind.DistrictRelay : InteractionKind.DistrictExit;
            if (!CivicWorld.Exploration(game.stage) && game.stage != StageId.Roof && game.Power && !game.Cleared)
            {
                float best = float.MaxValue;
                foreach (var e in game.Enemies)
                    if (e && e.Alive)
                    {
                        float d = Vector3.Distance(e.transform.position, target);
                        if (d < best)
                        {
                            best = d;
                            target = e.transform.position + Vector3.up;
                        }
                    }

                label = "메인 목표 · 남은 경비병 제압";
                return best < float.MaxValue;
            }

            float distance = float.MaxValue;
            bool found = false;
            foreach (var p in points)
                if (p && p.gameObject.activeInHierarchy && p.kind == kind)
                {
                    if (game.stage == StageId.Haven && first && p.destination != StageId.Headquarters)
                        continue;
                    float d = Vector3.Distance(p.transform.position, game.Player.transform.position);
                    if (d < distance)
                    {
                        distance = d;
                        target = p.transform.position;
                        found = true;
                    }
                }

            return found;
        }
    }
}
