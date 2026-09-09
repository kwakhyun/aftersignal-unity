using UnityEngine;
namespace AfterSignal
{
    public static class FactionVoice
    {
        static int variation;
        static readonly string[][] gang={
            new[]{"조직 구역이다. 눈 깔고 지나가.","장비 점검해. 다음 건을 준비한다.","순찰차 보이면 무전해.","몫은 임무 끝나고 나눈다.","혈선의 이름으로 거리를 장악한다."},
            new[]{"엄폐해! 저놈부터 처리한다!","측면으로 돌아! 도망갈 틈 주지 마!","쓰러진 놈도 확인해. 방심하지 마!","탄창 바꾼다. 화력 유지해!","저항하면 끝이다. 밀어붙여!"},
            new[]{"젠장, 맞았어! 엄호해!","피가 나잖아… 가만 안 둔다!","크윽! 놈을 놓치지 마!","다리를 맞았다! 조직에 연락해!","나 아직 안 끝났어…!"},
            new[]{"괴물이다! 중화기로 쏴!","저 빛 피하라고! 장비 챙겨!","거신부터 쓰러뜨려!","건물 뒤로! 광선이 온다!"}};
        static readonly string[][] police={
            new[]{"순찰 중 이상 없습니다.","주변을 살피겠습니다. 안전하게 이동하세요.","불편한 점은 가까운 치안센터에 신고해 주세요.","관제실, 보행 구역 확인 완료.","상호 엄호 간격 유지."},
            new[]{"무장 용의자 확인! 시민부터 대피시켜!","경찰이다! 무기를 내려놓아라!","측면 확보! 용의자에게 집중 사격!","재장전! 엄호 부탁한다!","특수대응팀, 진입한다!"},
            new[]{"대원 부상! 엄호와 구급 지원 요청!","방탄복 피격! 계속 대응한다!","총상이다. 현장 의료 지원 바란다!","대원 쓰러졌다! 안전 통로 확보해!"},
            new[]{"거대 잠식체 확인! 시민 접근을 통제한다!","광선 경보! 견고한 엄폐물 뒤로!","괴물 우선 제압! 지원 화력 요청!","붕괴 위험! 안전선을 넓혀!"}};
        static readonly string[][] military={
            new[]{"경계 근무 이상 무.","장비와 통신 상태 확인한다.","지정 구역 유지. 민간인 통행 확인.","지휘소, 전 대원 대기 완료.","차량 간격과 사계 유지."},
            new[]{"목표 식별! 분대 단위로 교전 개시!","지원 화력! 아군 위치 확인하고 발사!","엄호 사격 유지! 측면을 확보한다!","탄약 보급 요청! 진지를 유지해!","장갑부대와 보조를 맞춰 전진!"},
            new[]{"전투 부상자 발생! 의무병 지원 요청!","피격! 전투 지속 가능!","대원 중상! 후송 통로 확보!","분대원 쓰러졌다! 엄호 사격!"},
            new[]{"거대 잠식체 조준! 중화기 집중 운용!","대공 광선! 항공대 회피 기동!","폭격 예정 구역에서 이탈!","장갑부대 전개! 발광 기관을 노려!"}};
        public static string Filter(Component who,string requested,int priority)
        {
            var body=who.GetComponent<WorldActor>();if(!body||body.robot||body.monster||body.terrorist||!(body.gang||body.police||body.military))return requested;
            int mode=requested.Contains("광선")||requested.Contains("괴물")||requested.Contains("잠식")?3:requested.Contains("부상")||requested.Contains("출혈")||requested.Contains("맞았")||requested.Contains("총상")||priority>=10?2:requested.Contains("사격")||requested.Contains("용의")||requested.Contains("공격")||requested.Contains("엄호")||requested.Contains("진입")||requested.Contains("돈")||requested.Contains("처리")||priority>=5?1:0;
            var bank=body.gang?gang:body.military?military:police;return bank[mode][(variation++&int.MaxValue)%bank[mode].Length];
        }
    }
}
