using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void LiftServices(MultiFloorLift lift)
        {
            Panel("service","승강기","이동할 층을 선택하세요.");
            for(int i=0;i<lift.floors;i++){int floor=i;Option((i+1)+"층",()=>{Dismiss();lift.Go(floor);});}
        }
        public void CustodyServices()
        {
            bool jailed=PrisonSystem.Instance&&PrisonSystem.Instance.Jailed;
            Panel("service","애프터라이트 교도소",jailed?"남은 수감 시간 "+Mathf.CeilToInt(PrisonSystem.Instance.Remaining)+"초 · 형기가 끝나면 정문으로 안내합니다.":"접견 안내 · 교정 직원이 상주하는 시설입니다.");
            if(jailed)Option("보석금 납부 / 450 C",()=>{Dismiss();PrisonSystem.Instance.Release(true);});
        }
    }
}
