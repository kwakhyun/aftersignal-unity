using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public bool RegionalMenu(VenueService service,CityVenue v,VenueRuntime live)
        {
            if(v.kind<VenueKind.Police)return false;
            Panel("venue",v.title,v.description+"\n\n"+FacilityGuide.For(v));
            void Refresh()=>VenueMenu(service);
            void Buy(int cost,System.Action action){if(LifeState.Spend(cost)){action();Dismiss();}else game.Toast("보유 크레딧이 부족합니다.");}
            if(v.kind==VenueKind.Bank){Body+="\n예금 "+LifeState.Savings.ToString("N0")+" C";Option("500 C 예금",()=>{LifeState.Deposit(500);Refresh();});Option("500 C 인출",()=>{LifeState.Withdraw(500);Refresh();});}
            else if(v.kind==VenueKind.Restaurant||v.kind==VenueKind.Cafe){Option(v.kind==VenueKind.Cafe?"음료 / 20 C":"식사 / 35 C",()=>Buy(v.kind==VenueKind.Cafe?20:35,()=>{game.Player.Heal(35);game.Player.RestoreEnergy(30);}));Option("아르바이트 시작",()=>BeginShift(v.kind==VenueKind.Cafe));}
            else if(v.kind==VenueKind.Library){Option("자료 열람 · 도시 연혁",()=>{Panel("venue","도시 기록","새벽 저지대의 노동자들이 초기 신호망을 세웠습니다. 서하는 그곳에서 자랐고 이웃의 도움으로 복원 작업에 참여했습니다.\n\n노바는 해협 교역과 시민 의회를, 네레이드는 공동 공기·수질 관리 체계를 기반으로 운영됩니다. 에레보스의 영점 싱크홀은 아직 조사 중입니다.");Option("돌아가기",Refresh);});}
            else if(v.kind==VenueKind.School){Option("공개 수업 참가 · 1시간",()=>{LifeState.Hours+=1;game.Player.RestoreEnergy(25);Dismiss();game.Toast("도시 생태와 신호 안전 수업을 들었습니다",5);});}
            else if(v.kind==VenueKind.FireStation){Option("구조 교육 / 응급처치 · 무료",()=>{game.Player.Heal(20);Dismiss();game.Toast("응급처치 교육 완료 · 부상 회복",4);});}
            else if(v.kind==VenueKind.Police){Option("민원 접수 / 안전 안내",()=>{Panel("venue","치안 안내","범죄는 경찰의 목격 또는 시민 신고가 완료되면 수배됩니다.\n수배 중 가까운 경찰 앞에서 H를 유지하면 투항할 수 있습니다.\n도움이 필요하면 접수 직원에게 말씀해 주세요.");Option("돌아가기",Refresh);});}
            else if(v.kind==VenueKind.Prison){Option("면회 안내",()=>{game.Toast("접수 구역에서 교도관에게 문의하세요. 수용동의 철창은 통행을 제한합니다.",6);});}
            else if(v.kind==VenueKind.Military){Option("작전 구역 안내",()=>{game.Toast(v.city==2?"특수부대는 잠식체와 교전 중입니다. 북쪽 연구기관과 동쪽 실험실로 이어지는 도로를 확보하세요.":"지휘동, 전차 대기장, 항공 격납고, 정비동을 이용할 수 있습니다.",7);});}
            else if(v.kind==VenueKind.Slum)
            {
                Option("이웃 공동식사 · 8 C",()=>Buy(8,()=>game.Player.Heal(35)));
                Option("암시장 거래",()=>BlackMarket(service));
                Option("비공식 운송 의뢰",()=>{Dismiss();RegionalErrand.Accept(live);});
                Option("이웃 심부름 정산",()=>{Dismiss();RegionalErrand.Complete();});
                Option("서하의 집으로",()=>{Dismiss();CivicWorld.Travel(game,StageId.Residence,new Vector3(8,22.15f,-1));});
            }
            else if(v.kind==VenueKind.Island){Option("선박 이용 안내",()=>game.Toast("부두 옆 보트는 E로 조종합니다. 순환 여객선이 정차하면 G로 승객 탑승, F로 하선하세요.",7));Option("마을 식사 · 18 C",()=>Buy(18,()=>game.Player.Heal(30)));}
            else if(v.kind==VenueKind.Sinkhole){Option("붕괴구 관측",()=>{Dismiss();live?.Observe();game.Toast("기록: 공동 지하 신호망의 노드들이 한 지점으로 수렴하고 있다.",7);});}
            if(live&&live.Lift)Option("승강기 층 선택",()=>LiftServices(live.Lift));
            Option("닫기",Dismiss);return true;
        }
        void BlackMarket(VenueService service)
        {
            Panel("venue","새벽 저지대 암시장","비공식 무기·부품 거래와 도난 물품 회수. 주변에서 순찰 경찰이 거래를 목격하면 신고 대상이 됩니다.");
            foreach(int id in new[]{7,8,10,12}){int item=id;Option(ArmoryInventory.Items[item].name+" · "+ArmoryInventory.Items[item].price+" C",()=>{bool already=ArmoryInventory.Owns(item)&&item!=10;if(ArmoryInventory.Buy(item)){if(!already)CrimeObservation.Observe(8,game.Player.transform.position);game.Toast("거래 완료");}else game.Toast("보유 크레딧이 부족합니다.");});}
            Option("비공식 운송 의뢰",()=>{Dismiss();RegionalErrand.Accept(FourCityWorld.Instance.Find("dawn-lowlands"));});Option("돌아가기",()=>VenueMenu(service));
        }
    }
    public static class RegionalErrand
    {
        static bool carrying;static float deadline;static Vector3 destination;
        public static bool Carrying=>carrying;
        public static void Reset()=>carrying=false;
        public static Vector3 Destination=>destination;
        public static void Accept(VenueRuntime slum)
        {
            var g=GameDirector.Instance;if(carrying){g.Toast("이미 운송 중입니다. 동쪽 골목 인계점으로 가세요.");return;}
            carrying=true;deadline=Time.time+240;destination=slum.transform.position+new Vector3(800,.15f,40);FourCityAtlasSelection.Select(slum.Definition,false);g.Toast("비공식 의뢰 · 동쪽 골목 인계점까지 밀봉 부품 운송 / 제한 4분 / 보수 450 C",8);
            var go=new GameObject("동쪽 골목 · 비공식 운송 인계");go.transform.position=destination;var point=go.AddComponent<InteractionPoint>();point.kind=InteractionKind.Furniture;point.title="부품 인계 · 450 C";point.radius=4;go.AddComponent<RegionalDelivery>();
        }
        public static void Complete()
        {
            var g=GameDirector.Instance;if(!carrying){g.Toast("진행 중인 운송 의뢰가 없습니다.");return;}
            if(Time.time>deadline){carrying=false;g.Toast("운송 의뢰 시간이 만료되었습니다.");return;}
            if((g.Player.transform.position-destination).sqrMagnitude>8*8){g.Toast("저지대 동쪽 골목의 인계점으로 이동하세요.");return;}
            carrying=false;LifeState.Earn(450);CrimeObservation.Observe(9,destination);g.Toast("부품 인계 완료 · +450 C",5);
        }
    }
    public sealed class RegionalDelivery:MonoBehaviour{void Update(){if(!RegionalErrand.Carrying)Destroy(gameObject);}}
}
