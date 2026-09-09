using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void FacilityServices(FacilityFunction kind,string title)
        {
            Panel("service",string.IsNullOrEmpty(title)?kind.ToString():title,"잔액 "+LifeState.Credits.ToString("N0")+" C · 현장 시설 운영 안내");
            void Job(string label,string id,int reward,float hours){Option(label,()=>{Dismiss();FacilityOperation.Begin(id,reward,hours);});}
            void Buy(string label,int cost,System.Action effect){Option(label,()=>{if(!LifeState.Spend(cost)){Body="잔액이 부족합니다.";Revision++;return;}effect();FacilityServices(kind,title);Body+="\n이용 완료 · "+cost+" C 사용";Revision++;});}
            if(kind==FacilityFunction.Military||kind==FacilityFunction.Garage)
            {
                CityVehicle equipment=null;float near=35;
                if(UrbanSimulation.Instance)foreach(var c in UrbanSimulation.Instance.Cars)if(c&&!c.Wrecked&&c.GetComponent<VehicleArmament>()){float d=Vector3.Distance(c.transform.position,game.Player.transform.position);if(d<near){near=d;equipment=c;}}
                if(equipment){var target=equipment;Buy("근처 군용 탈것 탄약 재보급 / 200 C",200,()=>target.GetComponent<VehicleArmament>().Resupply());}
            }
            switch(kind)
            {
                case FacilityFunction.Registry:
                    Body+="\n시민 등록 · 복원 지원 · 기록공개 민원을 처리합니다.";
                    if(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.CivicPermit",0)==0)Buy("도시 복원 근무자 등록 / 100 C",100,()=>{if(!LifeState.SuppressSave)PlayerPrefs.SetInt("AFTERSIGNAL.Unity.CivicPermit",1);game.Toast("복원 근무자 등록 완료 · 현장 업무 보수 10% 가산");});
                    else Body+="\n복원 근무자 등록 완료 · 현장 업무 보수 10% 가산";
                    Job("민원 서류 접수 업무 / 220 C","registry",220,1);
                    Option("신호 균열 대응 기록 열람",()=>{Body="컨덕터의 기억 수집망에서 유출된 잔향이 시민 기억을 모방하고 있습니다. 국방대응부는 정부청사와 본부의 기록을 대조하며 균열을 봉쇄합니다.\n현재 도시 신호 이상: "+(RiftIncursion.Instance&&RiftIncursion.Instance.Active?"출몰 발생 · 군 출동 중":"감시 중");Revision++;});
                    break;
                case FacilityFunction.Archive:Job("기억 기록 분류·복원 / 250 C","archive",250,1);Option("도시 기억 조사 기록",()=>{Body="첫 열차의 기억, 중앙역의 잔류 신호, 잔향체의 출몰은 같은 복원망에 연결되어 있습니다. 시민과 본부, 정부청사의 기록을 대조하세요.";Revision++;});break;
                case FacilityFunction.Clinic:Buy("진료 / 체력 완전 회복 · 80 C",80,()=>game.Player.Heal(100));Job("의료 물자 확인 / 160 C","medical",160,1);break;
                case FacilityFunction.Office:Job("설비 점검 및 사무 지원 / 180 C","office",180,1);break;
                case FacilityFunction.Home:Option("보관 의상 변경",Wardrobe);Option("거실에서 휴식",()=>Rest(2,0));break;
                case FacilityFunction.Power:Job("배전반 3곳 복구 / 300 C","grid",300,2);break;
                case FacilityFunction.Freight:Job("화물 3건 검수·봉인 / 320 C","freight",320,2);break;
                case FacilityFunction.Hotel:Option("숙박 / 8시간 · 240 C",()=>Rest(8,240));Buy("룸서비스 / 65 C",65,()=>game.Player.Heal(70));break;
                case FacilityFunction.School:Job("도서·교실·보건물품 점검 / 160 C","school",160,1);break;
                case FacilityFunction.Police:Job("분실물 증거 대조 / 200 C","police",200,1);Buy("벌금 정산 / "+Mathf.Max(1,WantedSystem.Level)*180+" C",Mathf.Max(1,WantedSystem.Level)*180,()=>WantedSystem.Clear("벌금 납부 완료"));break;
                case FacilityFunction.Fire:Job("소화전·호스·경보장치 점검 / 240 C","fire",240,1);break;
                case FacilityFunction.Market:Buy("구급 식량 / 45 C",45,()=>game.Player.Heal(40));Buy("배터리 음료 / 35 C",35,()=>game.Player.RestoreEnergy(60));Option("무기 매장",()=>Armory());break;
                case FacilityFunction.Cafe:Option("바리스타 아르바이트",()=>BeginShift(true));Buy("커피 / 35 C",35,()=>game.Player.RestoreEnergy(70));break;
                case FacilityFunction.Bank:
                    Body+="\n예금 "+LifeState.Savings+" C";Option("500 C 예금",()=>{bool ok=LifeState.Deposit(500);FacilityServices(kind,title);Body+=ok?"\n예금 완료":"\n현금이 부족합니다.";Revision++;});Option("500 C 인출",()=>{bool ok=LifeState.Withdraw(500);FacilityServices(kind,title);Body+=ok?"\n인출 완료":"\n예금이 부족합니다.";Revision++;});break;
                case FacilityFunction.Bar:Buy("시그널 칵테일 / 60 C",60,()=>game.Player.RestoreEnergy(90));Job("오픈 준비·재고 관리 / 210 C","bar",210,1);break;
                case FacilityFunction.Garage:Option("차량 수리·튜닝",Garage);Job("공구·부품 점검 / 240 C","repair",240,1);break;
                case FacilityFunction.Airport:Job("수하물 보안 검사 / 280 C","airport",280,2);TransitOptions(true);break;
                case FacilityFunction.Ferry:Job("선박 안전 점검 / 250 C","harbour",250,1);TransitOptions(false);break;
                case FacilityFunction.Military:Job("통신·보급·무장 점검 / 300 C","military",300,2);Option("방위 장비 보급",()=>Armory());break;
            }
        }
        void TransitOptions(bool aircraft)
        {
            Body+="\n"+(aircraft?"도시 간 항공편 / 240 C":"도시 간 여객선 / 80 C")+" · G 승차권 구매 · F 이동 중 탈출";
            bool nova=NeonHarbor.Region(game.Player.transform.position);
            foreach(var line in IntercityService.All)if(line&&line.Aircraft==aircraft)
            {
                if(line.AtNova!=nova)continue;
                var selected=line;Option(line.Boarding?"지금 승객으로 탑승 · "+line.Fare+" C":line.Status,()=>{Dismiss();if(selected.Boarding)selected.BuyTicket();else HarborAccess.Instance?.Queue(aircraft,nova);});
                if(line.AtNova==nova)Option("탑승구로 안내",()=>{Dismiss();game.Player.Respawn(selected.Terminal(nova),false);game.CameraRig.Snap();});
            }
            Option("다음 편 자동 탑승 대기",()=>{Dismiss();HarborAccess.Instance?.Queue(aircraft,nova);});
            if(HarborAccess.Instance&&HarborAccess.Instance.Waiting)Option("탑승 대기 취소",()=>{HarborAccess.Instance.Cancel();Dismiss();});
        }
    }
}
