using UnityEngine;
namespace AfterSignal
{
    public static class RegionalResidents
    {
        static readonly string[] LowlandJobs={"고물 수집상","봉제 노동자","배달 기사","야간 경비원","길거리 요리사","재활용 기술자","공동급식 봉사자","구두 수선공","공사장 일용직","노점 상인","부두 노동자","버스 정비사","골목 이발사","동네 할머니","은퇴한 용접공","세탁소 직원","신호 중계 수리공","고향 친구"};
        public static string Role(CityVenue v,bool staff,int i)
        {
            if(v.kind==VenueKind.Slum)return LowlandJobs[i%LowlandJobs.Length];
            if(v.kind==VenueKind.Island)return new[]{"양식 드론 운영자","자율항해 선장","로봇 조선 기술자","해양 바이오 연구원","해상 물류 상인","항로 데이터 관리자","해상도시 주민"}[i%7];
            if(v.kind==VenueKind.Prison)return staff?"교도관":"수용자";
            if(v.kind==VenueKind.Military)return i%3==0?"작전 장교":i%3==1?"기지 정비병":"경계 대원";
            if(v.kind==VenueKind.Police)return staff?i%3==0?"수사관":i%3==1?"경찰 행정 직원":"순찰 경찰":"민원 방문객";
            if(v.kind==VenueKind.FireStation)return staff?i%2==0?"구조대원":"소방관":"안전 교육 참가자";
            if(v.kind==VenueKind.School)return staff?i%2==0?"교사":"학교 직원":i%2==0?"남학생":"여학생";
            if(v.kind==VenueKind.Library)return staff?"도서관 사서":"도서관 이용자";
            if(v.kind==VenueKind.Bank)return staff?"은행원":"은행 고객";
            if(v.kind==VenueKind.Cafe)return staff?"바리스타":"카페 손님";
            if(v.kind==VenueKind.Restaurant)return staff?i%2==0?"요리사":"서빙 직원":"식당 손님";
            if(v.kind==VenueKind.Laboratory||v.id=="erebos-institute")return i%4==0?"시설 경비대":"잠식 연구원";
            if(v.kind==VenueKind.CityHall)return staff?"시청 공무원":"시민 민원인";
            return null;
        }
        public static string Art(CityVenue v,bool staff,int i)
        {
            if(v.kind==VenueKind.Slum)return new[]{"Worker","CivilianWoman","CivilianMan","ElderMan","Bartender","Worker","Nurse","ElderWoman","Worker","CivilianWoman","CivilianMan","Worker"}[i%12];
            if(v.kind==VenueKind.Prison)return staff?"Police":"Prisoner";
            if(v.kind==VenueKind.Police&&staff)return "Police";
            if(v.kind==VenueKind.Military)return v.city==2?"Swat":"Soldier";
            if(v.kind==VenueKind.School)return staff?(i%2==0?"TeacherMan":"TeacherWoman"):(i%2==0?"StudentBoy":"StudentGirl");
            if(v.kind==VenueKind.Laboratory||v.id=="erebos-institute")return i%4==0?"Swat":i%2==0?"Doctor":"Nurse";
            if(v.kind==VenueKind.FireStation&&staff)return "Firefighter";
            return null;
        }
        public static void Apply(VenueActor actor,VenueRuntime venue,bool staff,int i)
        {
            var npc=actor.GetComponent<CityNpc>();var v=venue.Definition;
            if(v.kind==VenueKind.Slum)NeighborBond.Attach(npc);
            if(v.kind==VenueKind.Prison&&!staff)actor.gameObject.AddComponent<RegionalUniform>().art="Prisoner";
            bool security=v.kind==VenueKind.Police&&staff||v.kind==VenueKind.Military||v.kind==VenueKind.Prison&&staff||(v.kind==VenueKind.Laboratory||v.id=="erebos-institute")&&i%4==0;
            if(security){var body=actor.GetComponent<WorldActor>();body.police=v.kind==VenueKind.Police||v.kind==VenueKind.Prison;body.military=!body.police;body.health=180;if(body.military)GarrisonSupport.Register(body,v.Entrance);var guard=actor.gameObject.AddComponent<RegionalGuard>();guard.venue=venue;if(v.city==2)actor.gameObject.AddComponent<RegionalUniform>().art="Swat";}
        }
    }
    public sealed class RegionalUniform:MonoBehaviour{public string art;}
}
