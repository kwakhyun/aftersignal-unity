using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void VenueMenu(VenueService service)
        {
            var v=FourCityCatalog.Venues[service.index];var live=FourCityWorld.Instance?.Find(v.id);
            if(service.action=="campaign"){FourCityCampaign.Instance?.Interact(service.index);return;}
            if(service.action=="lift"){if(live&&live.Lift){if(live.Lift.Aboard(game.Player))LiftServices(live.Lift);else live.Lift.Call(service.floor);return;}}
            if(RegionalMenu(service,v,live))return;
            Panel("venue",v.title,v.description+"\n\n"+FacilityGuide.For(v)+"\n\n"+(v.Sport?"경기 관람과 승부예측은 매표소에서 이용합니다.":"직원에게 질문하거나 안내 단말을 이용하세요."));
            if(v.Sport){Option("오늘 경기 · 점수 / 규칙 / 승부예측",()=>VenueMatch(v));Option("관중석으로 이동",()=>{Dismiss();live?.Observe();});}
            else if(v.kind==VenueKind.Cinema){Option("영화 관람 · 25 C",()=>{if(!LifeState.Spend(25)){game.Toast("보유 크레딧이 부족합니다.");return;}Dismiss();live?.WatchFilm();});Option("상영작 / 이용 허가",()=>{Panel("venue","SIGNAL / 심해의 빛","이 게임을 위해 직접 제작한 60초 길이의 3D 애니메이션 영상입니다.\n도시의 기억이 바다 아래에서 깨어나는 장면을 상영합니다.\n\n제작: AFTERSIGNAL 프로젝트 · 외부 영화·음악·상표 미사용");Option("돌아가기",()=>VenueMenu(service));});}
            else if(v.kind==VenueKind.Amusement){Option("대관람차 · 30 C",()=>{Dismiss();live?.Ride(0);});Option("궤도 코스터 · 40 C",()=>{Dismiss();live?.Ride(1);});Option("회전목마 · 15 C",()=>{Dismiss();live?.Ride(2);});}
            else if(v.kind==VenueKind.Hotel){Option("객실 취침 · 120 C",()=>{if(LifeState.Spend(120)){LifeState.Hours+=8;game.Player.Heal(100);Dismiss();live?.Rest();}else game.Toast("보유 크레딧이 부족합니다.");});Option("레스토랑 · 식사 30 C",()=>{if(LifeState.Spend(30)){game.Player.Heal(35);Dismiss();game.Toast("식사를 마쳤습니다 · 체력 회복");}});}
            else if(v.kind==VenueKind.Hospital){Option("진료 / 완전 회복 · 80 C",()=>{if(LifeState.Spend(80)){game.Player.Heal(100);Dismiss();game.Toast("진료 완료 · 체력 회복");}});}
            else if(v.kind==VenueKind.Market){Option("해조 도시락 · 25 C",()=>{if(LifeState.Spend(25)){game.Player.Heal(30);Dismiss();}});Option("휴대 전력 충전 · 35 C",()=>{if(LifeState.Spend(35)){game.Player.RestoreEnergy(100);Dismiss();}});}
            else if(v.kind==VenueKind.Garden||v.kind==VenueKind.Museum||v.kind==VenueKind.Monument){Option("전시 / 전망 안내",()=>{Dismiss();live?.Observe();game.Toast(v.description,7);});}
            if(live&&live.Lift)Option("승강기 층 선택",()=>LiftServices(live.Lift));
            if(FourCityCampaign.Contains(service.index))Option("메인 캠페인 · 잔향 기록",()=>{Dismiss();FourCityCampaign.Instance?.Interact(service.index);});
            Option("닫기",Dismiss);
        }
        public void VenueMatch(CityVenue v)
        {
            var league=FourCitySports.Instance;if(!league)return;var m=league.Get(v.id);
            Panel("venue",v.title+" · "+m.round+"회차",m.home+"   "+m.homeScore+" : "+m.awayScore+"   "+m.away+"\n"+m.Status+"\n"+m.lastEvent+"\n\n"+FacilityGuide.Rules(v.kind)+"\n\n"+(m.stake>0?"예측: "+(m.selection==0?m.home:m.away)+" · "+m.stake+" C"+(m.settled?" / 정산 "+m.payout+" C":""):"시작 전 팀당 100 C · 적중 200 C(원금 포함), 무승부는 원금 반환. 게임 재화 전용."));
            if(m.CanBet){Option(m.home+" 승 · 100 C",()=>{if(!league.Bet(m,0,100))game.Toast("예측 마감 또는 잔액 부족");VenueMatch(v);});Option(m.away+" 승 · 100 C",()=>{if(!league.Bet(m,1,100))game.Toast("예측 마감 또는 잔액 부족");VenueMatch(v);});}
            Option("실시간 경기 관람",()=>{Dismiss();FourCityWorld.Instance?.Find(v.id)?.Observe();});
            Option("갱신",()=>VenueMatch(v));Option("닫기",Dismiss);
        }
    }
}
