using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void LiftServices(MultiFloorLift lift)
        {
            LiftPage(lift,lift.CurrentFloor/5);
        }
        void LiftPage(MultiFloorLift lift,int page)
        {
            int first=page*5,last=Mathf.Min(lift.floors,first+5);
            Panel("service","승강기 · 현재 "+(lift.CurrentFloor+1)+"층",(first+1)+"–"+last+"층 / 총 "+lift.floors+"층 · 이동할 층을 선택하세요.");
            for(int i=first;i<last;i++){int floor=i;Option((i+1)+"층"+(lift.floorNames!=null&&i<lift.floorNames.Length?" · "+lift.floorNames[i]:""),()=>{Dismiss();lift.Go(floor);});}
            if(page>0)Option("◀ 아래층 목록",()=>LiftPage(lift,page-1));
            if(last<lift.floors)Option("위층 목록 ▶",()=>LiftPage(lift,page+1));
        }
        public void CustodyServices()
        {
            bool jailed=PrisonSystem.Instance&&PrisonSystem.Instance.Jailed;
            Panel("service","애프터라이트 교도소",jailed?"남은 수감 시간 "+Mathf.CeilToInt(PrisonSystem.Instance.Remaining)+"초 · 형기가 끝나면 정문으로 안내합니다.":"접견 안내 · 교정 직원이 상주하는 시설입니다.");
            if(jailed)Option("보석금 납부 / 450 C",()=>{Dismiss();PrisonSystem.Instance.Release(true);});
        }
    }
}
