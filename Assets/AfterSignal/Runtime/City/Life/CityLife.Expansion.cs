using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void ExpansionServices(int facility)
        {
            if(facility==13){FacilityServices(FacilityFunction.Registry,"도시기억관리청");return;}
            if(facility==10){FacilityServices(FacilityFunction.Military,"루멘 방위기지");return;}
            if(facility==11){CustodyServices();return;}
            facility=Mathf.Clamp(facility,0,ExpansionWorld.Names.Length-1);
            Panel("service",ExpansionWorld.Names[facility],"잔액 "+LifeState.Credits.ToString("N0")+" C · 지역 시설 안내");
            int site=facility;
            Option("이 장소를 지도 목적지로 지정",()=>{ExpansionWorld.Selected=site;Dismiss();game.Toast("목적지: "+ExpansionWorld.Names[site]);});
            if(facility==0){Option("해변 매점 · 음료 / 25 C",()=>Purchase(25,()=>game.Player.RestoreEnergy(55)),"EnergyDrink");Option("해변 정화 활동 / 1시간 · 130 C",()=>Work("coast-cleanup",130,1));}
            if(facility==1||facility==6){Option("화물 검수 아르바이트 / 2시간 · 320 C",()=>Work("port-inspection",320,2));Option("구내식당 / 50 C",()=>Purchase(50,()=>game.Player.Heal(60)),"WarmMeal");}
            if(facility==2){Option("수하물 분류 근무 / 2시간 · 280 C",()=>Work("airport-baggage",280,2));Option("공항 카페 / 45 C",()=>Purchase(45,()=>game.Player.RestoreEnergy(85)),"Coffee");Option("시내 급행 셔틀 / 80 C",()=>LocalRide(new Vector3(880,.15f,105),80));}
            if(facility==3){Option("야시장 국수 / 55 C",()=>Purchase(55,()=>game.Player.Heal(70)),"WarmMeal");Option("야간 매대 정리 / 1시간 · 160 C",()=>Work("night-market",160,1));}
            if(facility==4){Option("공항 급행 셔틀 / 80 C",()=>LocalRide(new Vector3(1730,.2f,527),80));Option("해변 순환버스 / 40 C",()=>LocalRide(new Vector3(570,.2f,-545),40));Option("항만 통근 셔틀 / 55 C",()=>LocalRide(new Vector3(1730,.2f,-475),55));}
            if(facility==5)Option("전망공원에서 휴식 / 1시간",()=>Rest(1,0));
            if(facility==7){Option("오션뷰 숙박 / 8시간 · 280 C",()=>Rest(8,280));Option("호텔 조식 / 80 C",()=>Purchase(80,()=>game.Player.Heal(90)),"SpecialMeal");}
            if(facility==8){Option("응급 치료 / 80 C",()=>Purchase(80,()=>game.Player.Heal(100)));Option("의료물자 정리 / 1시간 · 140 C",()=>Work("port-medical",140,1));}
            if(facility==10){Option("정비 격납고 안내",()=>{Dismiss();game.Toast("기지 안 H 패드에 전투헬기, 활주로에 전투기, 차고 앞에 전차가 있습니다.",6);});}
            if(facility==12){Option("여객선 승선 안내",()=>{Dismiss();game.Toast("부두 끝에서 E 선장석 / G 승객석 · NPC 여객선은 45초 정차합니다.",7);});}
            if(facility==9)Option("전력 설비 점검 보조 / 2시간 · 300 C",()=>Work("east-grid",300,2));
        }
        void LocalRide(Vector3 at,int cost)
        {
            if(WantedSystem.Level>0){Body="수배 중에는 셔틀을 이용할 수 없습니다.";Revision++;return;}
            if(!LifeState.Spend(cost)){Body="교통비가 부족합니다.";Revision++;return;}
            Dismiss();game.Player.Respawn(at,false);game.CameraRig.Snap();game.Toast("셔틀 도착 · 요금 "+cost+" C");
        }
    }
}
