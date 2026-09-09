using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        RectTransform interactionCard,travelStatus;
        Text traversalReadout,contextKeys,basicGuide;
        bool experienceBuilt;MovingLift statusLift;float liftSearch;
        void BuildClientExperience()
        {
            if(experienceBuilt)return;experienceBuilt=true;
            interactionCard=Panel(root,"Context action",0,0,680,50,new Color(.015f,.04f,.065f,.93f)).rectTransform;
            CenterBottom(interactionCard,114,680,50);
            Panel(interactionCard,"Action accent",0,0,4,50,mint);
            prompt.transform.SetParent(interactionCard,false);Rect(prompt.rectTransform,18,9,644,32);prompt.fontSize=17;prompt.alignment=TextAnchor.MiddleCenter;
            travelStatus=Panel(root,"Traversal state",0,0,300,52,new Color(.015f,.04f,.065f,.86f)).rectTransform;
            Bottom(travelStatus,38,112,300,52);
            traversalReadout=Label(travelStatus,"",12,6,276,19,13,mint,FontStyle.Bold);
            contextKeys=Label(travelStatus,"",12,28,276,18,11,white);
            notice.fontSize=17;notice.color=white;CenterTop(notice.rectTransform,154,780,48);
        }
        void UpdateClientExperience()
        {
            BuildClientExperience();
            bool playing=!game.Blocked;
            bool driving=UrbanSimulation.Instance&&UrbanSimulation.Instance.Driving;
            if(basicGuide)basicGuide.gameObject.SetActive(playing&&!driving);
            bool bus=CityBusService.Instance&&CityBusService.Instance.Riding;
            interactionCard.gameObject.SetActive(playing&&!string.IsNullOrEmpty(prompt.text));
            CenterBottom(interactionCard,114,driving?980:680,driving?62:50);Rect(prompt.rectTransform,18,9,driving?944:644,driving?44:32);
            travelStatus.gameObject.SetActive(playing&&!driving&&!bus);
            var p=game.Player;
            if(OceanLife.Swimming){traversalReadout.text="수영 · 수심 "+Mathf.Max(0,OceanLife.Surface-p.transform.position.y).ToString("0.0")+" m";contextKeys.text="WASD 수영 · SPACE 수면으로 / CTRL 잠수 · SHIFT 빠르게";}
            else if(PrisonSystem.Instance&&PrisonSystem.Instance.Jailed){traversalReadout.text="수감 중 · 남은 시간 "+Mathf.CeilToInt(PrisonSystem.Instance.Remaining)+"초";contextKeys.text="E 교정 안내 · 보석금 / 형기 종료 후 출소";}
            else if(WantedSystem.Level>0){traversalReadout.text="경찰 수배 · "+WantedSystem.Level+"단계";contextKeys.text="경찰 근처에서 H 길게 · 투항 / 체포";}
            else if(p.WallClimbing){traversalReadout.text="외벽 등반  ·  에너지 "+Mathf.CeilToInt(p.Energy);contextKeys.text="W 위로  ·  A/D 옆으로  ·  SPACE 벽 차기  ·  S 놓기";}
            else if(p.Rope.Attached){traversalReadout.text="로프 자동 감기";contextKeys.text="W 빠르게 / S 풀기  ·  SPACE 도약  ·  우클릭 해제";}
            else if(!p.Grounded){traversalReadout.text=p.JumpsUsed<2?"공중  ·  추가 점프 가능":"공중  ·  착지 대기";contextKeys.text="SPACE 추가 점프  ·  벽을 향해 W 등반";}
            else{traversalReadout.text=p.Running?"달리는 중":"탐색";contextKeys.text="WASD 달리기  ·  SPACE 두 번 더블 점프";}
            if(Time.unscaledTime>liftSearch){statusLift=Object.FindAnyObjectByType<MovingLift>();liftSearch=Time.unscaledTime+1;}
            var lift=statusLift;
            if(lift&&lift.HasRider){traversalReadout.text=lift.Moving?"승강기 이동 중":"승강기 탑승";contextKeys.text=lift.Moving?"도착 후 내리세요":"E 층 이동";}
            if(game.Dialogue||CityLife.Instance&&CityLife.Instance.Mode.Length>0)
            {travelStatus.gameObject.SetActive(false);interactionCard.gameObject.SetActive(false);}
            if(playing&&FacilityOperation.Current){traversalReadout.text="현장 업무 · "+FacilityOperation.Current.Completed+" / 3";contextKeys.text=FacilityOperation.Current.Label+" · "+Vector3.Distance(p.transform.position,FacilityOperation.Current.Target).ToString("0")+" m / E";}
            else if(playing&&RiftIncursion.Instance&&RiftIncursion.Instance.Active&&!driving){traversalReadout.text="신호 균열 · 국방대응부 출동";contextKeys.text="잔향체 출몰 지역 · "+Vector3.Distance(p.transform.position,RiftIncursion.Instance.Position).ToString("0")+" m";}
        }
    }
}
