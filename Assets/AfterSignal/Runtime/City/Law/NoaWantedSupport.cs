using UnityEngine;

namespace AfterSignal
{
    // Local story support: the countdown never blocks movement or calls a remote dialogue service.
    public sealed class NoaWantedSupport
    {
        public const float ClearDelay = 4f;
        const float ConfirmationDuration = 5f;
        static readonly string[] opening =
        {
            "서하, 네 이름이 수배망에 떴어. 잠깐만, 내가 지울게.",
            "또 사고 친 거야? 일단 뛰어. 기록은 내가 정리할게.",
            "서하, 뒤는 신경 쓰지 마. 지금 추적 신호 끊고 있어.",
            "네 얼굴이 너무 잘 찍혔는데? 괜찮아. 없는 기록으로 만들면 돼.",
            "경찰망에 들어왔어. 서하, 몇 초만 버텨 줘.",
            "이번에도 내가 먼저 찾았네. 수배 목록에서 널 빼 줄게.",
            "진정해, 서하. 사이렌 소리보다 내 손이 더 빠르니까.",
            "서하, 통신 켜 놔. 네 추적 기록을 다른 곳으로 돌리는 중이야.",
            "잠깐만. 도시가 널 찾는 동안 난 도시의 기록을 고치면 되지.",
            "이름, 얼굴, 위치… 다 잡혔네. 좋아, 한꺼번에 지워 줄게.",
            "서하, 무사하지? 대답은 나중에. 우선 수배부터 풀자.",
            "네 편이 여기 있는 거 잊었어? 조금만 기다려. 거의 다 됐어."
        };
        static readonly string[] closing =
        {
            "됐어. 수배 기록 삭제 끝. 이제 편하게 움직여.",
            "정리했어. 빚은 나중에 커피 한 잔으로 받아 둘게.",
            "추적 끊겼어. 이제 경찰은 널 찾지 못해.",
            "끝났어. 오늘 네 사진은 본 적 없는 걸로 해 두자.",
            "수배 해제 확인. 서하, 무사해서 다행이야.",
            "목록에서 사라졌어. 누가 도와줬는지는 우리만 아는 거야.",
            "봐, 벌써 끝났잖아. 다음엔 조금만 조용히 다녀.",
            "신호 정리 완료. 그쪽으로 가던 병력도 철수할 거야.",
            "도시는 이제 널 잊었어. 나만 기억하면 됐지.",
            "깨끗해졌어. 적어도 수배망 안에서는 모범 시민이네.",
            "이제 괜찮아. 다친 데 있으면 먼저 치료부터 받아.",
            "해제됐어. 계속 가, 서하. 뒤에서 보고 있을게."
        };

        static int lastLine = -1;
        int line;
        float remaining;
        public bool Visible => remaining > 0;
        public bool Clearing { get; private set; }
        public float Remaining => Mathf.Max(0,remaining);
        public string Line => Clearing ? opening[line] : closing[line];

        public void Tick(float deltaTime)
        {
            if(WantedSystem.Level > 0 && !Clearing)
            {
                int choice = Random.Range(0,opening.Length - (lastLine >= 0 ? 1 : 0));
                if(lastLine >= 0 && choice >= lastLine)choice++;
                lastLine = line = choice;
                remaining = ClearDelay;
                Clearing = true;
            }
            if(!Visible)return;
            remaining = Mathf.Max(0,remaining - Mathf.Max(0,deltaTime));
            if(Clearing && remaining <= 0)
            {
                // Use the existing release path so pending reports and pursuit units clear together.
                WantedSystem.Clear("");
                Clearing = false;
                remaining = ConfirmationDuration;
            }
        }

        public void Cancel(){Clearing=false;remaining=0;}
    }
}
