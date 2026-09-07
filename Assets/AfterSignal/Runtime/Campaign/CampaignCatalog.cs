using UnityEngine;

namespace AfterSignal
{
    public enum DistrictLayout
    {
        Street,
        Escalator,
        Canal,
        Lift,
        Exterior,
        Facade
    }

    public sealed class DistrictSpec
    {
        public readonly StageId id;
        public readonly string title, theme, brief, record;
        public readonly int chapter, enemies;
        public readonly float length;
        public readonly DistrictLayout layout;
        public readonly bool glass;
        public DistrictSpec(StageId id, string title, string theme, int chapter, DistrictLayout layout, int enemies, float length, bool glass, string brief, string record)
        {
            this.id = id;
            this.title = title;
            this.theme = theme;
            this.chapter = chapter;
            this.layout = layout;
            this.enemies = enemies;
            this.length = length;
            this.glass = glass;
            this.brief = brief;
            this.record = record;
        }

        public float EndHeight => layout == DistrictLayout.Facade ? 24 : layout == DistrictLayout.Escalator ? 5 : layout == DistrictLayout.Lift ? 6 : layout == DistrictLayout.Exterior ? 8 : 0;
        public bool Finale => id == StageId.Tower || id == StageId.Crown || id == StageId.Origin || id == StageId.Breach;
        public StageId Next => Finale || id == StageId.Harbor ? StageId.Haven : (StageId)((int)id + 1);
    }

    public static class CampaignCatalog
    {
        public static readonly DistrictSpec[] Districts =
        {
            new DistrictSpec(StageId.Market, "빗속의 거리", "market", 2, DistrictLayout.Street, 8, 78, false, "노아: 열차의 기록은 청계 시장에서 시작됐어. 비상 중계기를 찾아 상가로 진입해.", "우산 수선공의 장부. 기억세를 내지 못한 주민들의 이름이 줄지어 있다."),
            new DistrictSpec(StageId.Arcade, "잠들지 않는 상가", "atrium", 2, DistrictLayout.Escalator, 9, 84, true, "서하: 상가는 봉쇄됐지만 에스컬레이터의 예비 전원은 살아 있어. 상층 유리를 뚫자.", "보안 기록. 상가 관리인은 주민들이 숨을 수 있도록 마지막 셔터를 열어 두었다."),
            new DistrictSpec(StageId.Canal, "물 아래의 도시", "water", 2, DistrictLayout.Canal, 8, 88, false, "윤: 다리가 끊어졌어. 배수관의 로프 앵커를 이용해서 두 수로를 건너.", "잠긴 우체국의 엽서. 물속에서도 받는 사람의 이름은 지워지지 않았다."),
            new DistrictSpec(StageId.Transit, "끊어진 노선", "freight", 2, DistrictLayout.Street, 12, 94, false, "민: 버려진 화물선이 연구소까지 연결돼. 경비망을 끊으면 통로가 열릴 거야.", "화물 명세. 치료 장비라고 적힌 상자 안에는 주민의 기억 코어가 들어 있었다."),
            new DistrictSpec(StageId.Lab, "유리 속의 잔향", "lab", 2, DistrictLayout.Lift, 10, 86, true, "노아: 연구동 승강기로 올라가. 기록을 먼저 보존해야 거짓 치료를 증명할 수 있어.", "임상 파일. 서하의 증상은 병이 아니라 강제로 지운 기억이 돌아오는 반응이었다."),
            new DistrictSpec(StageId.Archive, "수직 기록동", "archive", 2, DistrictLayout.Exterior, 10, 90, true, "서하: 승강기 위 외벽의 앵커가 보여. 건물 사이를 건너 고층 기록실로 들어간다.", "삭제 승인서. 지우라는 명령 옆에 노아가 남긴 거절 서명이 있다."),
            new DistrictSpec(StageId.Tower, "마지막 중계탑", "tower", 2, DistrictLayout.Lift, 12, 100, true, "노아: 중계탑을 다시 연결하면 시장의 기억이 돌아와. 기록을 갖고 애프터라이트로 돌아와 줘.", "복원된 방송. 청계의 주민들은 서로의 이름을 다시 부르기 시작했다."),
            new DistrictSpec(StageId.Skyline, "전선 위의 하늘", "sky", 3, DistrictLayout.Exterior, 10, 96, true, "윤: 복구 신호를 가로채는 송신기가 옥상에 있어. 외벽 앵커를 따라 올라가자.", "정비사의 도면. 송신기의 전력은 폐쇄된 주조 공장에서 공급된다."),
            new DistrictSpec(StageId.Foundry, "잠들지 않는 용광로", "foundry", 3, DistrictLayout.Escalator, 14, 108, false, "다미: 공장의 상층 제어반으로 가. 생산 라인을 끊어야 주민들에게 전기가 돌아와.", "노동 일지. 공장 직원들은 자신들의 이름을 사물함 안쪽에 새겨 기억했다."),
            new DistrictSpec(StageId.Crown, "왕관의 균열", "crown", 3, DistrictLayout.Lift, 14, 102, true, "서하: 송신망의 최상층이다. 마지막 제어권을 주민들에게 돌려준다.", "해방된 배전도. 기업의 왕관에서 흐르던 전력이 거리의 집으로 돌아간다."),
            new DistrictSpec(StageId.Aqueduct, "도시의 맥박", "water", 4, DistrictLayout.Canal, 12, 100, false, "해진: 옛 수로에서 오래된 신호가 들려. 사라진 주민들의 첫 기록일지도 몰라.", "수문 각인. 도시는 처음부터 주민들이 함께 만든 기억 저장소였다."),
            new DistrictSpec(StageId.Observatory, "유리 천문대", "observatory", 4, DistrictLayout.Exterior, 12, 104, true, "윤: 천문대의 고층 렌즈를 지나야 기원 서버에 닿아. 외벽의 앵커를 놓치지 마.", "별자리 지도. 하늘의 좌표가 아니라 이 도시에 살았던 사람들의 관계를 그렸다."),
            new DistrictSpec(StageId.Origin, "기억의 기원", "origin", 4, DistrictLayout.Lift, 16, 112, true, "노아: 이제 기억을 소유하는 주인을 없애자. 네가 돌아올 곳은 여기야.", "기원 프로토콜 해제. 누구도 타인의 기억을 소유할 수 없다. 도시가 스스로의 이야기를 이어 간다."),
            new DistrictSpec(StageId.Harbor, "새벽의 선착장", "harbor", 0, DistrictLayout.Street, 0, 76, false, "해진: 강을 따라가면 오래된 보급 상자가 있어. 민에게 가져다주면 도움이 될 거야.", "강가의 라디오. 기억이 돌아온 도시에서 처음으로 생방송이 흘러나온다.")
        };
        public static readonly DistrictSpec FacadeMission = new DistrictSpec(StageId.Breach, "스물네 번째 창", "archive", 5, DistrictLayout.Facade, 14, 142, true, "지안: 외벽 정비 앵커를 따라 24층으로 올라가. 주황색 창을 깨면 내부 기록실에 진입할 수 있어.", "피난 기록. 서하가 살던 주거동의 주민들이 실종된 것이 아니었다. 누군가 그들을 비상 대피시켰다.");
        public static DistrictSpec Get(StageId id)
        {
            if (id == StageId.Breach)
                return FacadeMission;
            foreach (var s in Districts)
                if (s.id == id)
                    return s;
            return null;
        }

        public static string Title(StageId id) => Get(id)?.title ?? (CivicWorld.Interior(id) ? CivicWorld.Title(id) : id == StageId.UrbanCity ? "애프터라이트 시내" : id == StageId.Haven ? "애프터라이트" : id == StageId.Station ? "중앙역" : id == StageId.Carriage ? "밤의 객실" : "도시 위의 열차");
        public static int NextChapter
        {
            get
            {
                int bits = PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Expansion.Chapters", 0);
                return (bits & 4) == 0 ? 2 : (bits & 8) == 0 ? 3 : (bits & 16) == 0 ? 4 : 0;
            }
        }

        public static StageId ChapterStart(int chapter) => chapter == 2 ? StageId.Market : chapter == 3 ? StageId.Skyline : StageId.Aqueduct;
        public static void Complete(int chapter)
        {
            string key = "AFTERSIGNAL.Unity.Expansion.Chapters";
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) | (1 << chapter));
            PlayerPrefs.Save();
        }

        public static int Jobs
        {
            get => PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Expansion.Jobs", 0);
            set
            {
                PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Expansion.Jobs", value);
                PlayerPrefs.Save();
            }
        }
    }
}
