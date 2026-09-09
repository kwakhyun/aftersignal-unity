using UnityEngine;
namespace AfterSignal
{
    public static class NpcPersona
    {
        public static string Job(string art)=>art.Contains("Police")||art=="Swat"?"도시 치안대":art.Contains("Gang")?"무장 조직원":art=="Soldier"?"방위군 소총수":art.Contains("Doctor")?"의사":art.Contains("Nurse")?"간호사":art.Contains("Teacher")?"교사":art.Contains("Student")?"학생":art.Contains("Patient")?"통원 환자":art.Contains("Elder")?"은퇴한 주민":art.Contains("Office")?"회사원":art=="Worker"?"현장 기술자":art=="Bartender"?"바텐더":"도시 주민";
        public static string Group(CityNpc npc)
        {
            if(!npc)return "general";string art=NpcVoice.Role(npc),job=npc.occupation??"";var body=npc.GetComponent<WorldActor>();
            if(npc.GetComponent<FamilyMember>()?.Child==true)return "child";
            if(body&&body.terrorist)return "terrorist";
            if(body&&body.military)return "military";if(body&&body.police||art.Contains("Police")||art=="Swat")return "police";if(body&&body.gang||art.Contains("Gang"))return "gang";
            if(art.Contains("Elder"))return "elder";if(art.Contains("Student")||job.Contains("학생"))return "student";
            if(job.Contains("교사")||art.Contains("Teacher"))return "teacher";
            if(job.Contains("의사")||job.Contains("간호")||job.Contains("구급")||art=="Doctor"||art=="Nurse")return "medical";
            if(art.Contains("Patient")||job.Contains("환자"))return "patient";
            if(job.Contains("파일럿")||job.Contains("항공")||art.Contains("Aircrew"))return "pilot";
            if(job.Contains("선장")||job.Contains("항해")||job.Contains("조선")||art.Contains("Harbour"))return "seafarer";
            if(job.Contains("연구")||job.Contains("데이터"))return "research";
            if(job.Contains("상인")||job.Contains("은행")||job.Contains("전당")||job.Contains("판매"))return "merchant";
            if(job.Contains("회사")||job.Contains("행정")||job.Contains("공무")||art.Contains("Office"))return "office";
            if(job.Contains("음악")||job.Contains("사진")||job.Contains("예술"))return "artist";
            if(job.Contains("요리")||job.Contains("서빙")||job.Contains("카페")||job.Contains("바리스타")||job.Contains("호텔")||art=="Bartender")return "service";
            if(art=="Worker"||job.Contains("기술")||job.Contains("배달")||job.Contains("정비")||job.Contains("노동")||job.Contains("수리"))return "worker";
            return "general";
        }
        public static int Age(CityNpc npc){string group=Group(npc);int n=npc?npc.variation:0;return group=="child"?8+n%5:group=="elder"?65+n%20:group=="student"?14+n%8:24+n%30;}
        public static void Ensure(WorldActor body,string art,string job)
        {
            var npc=body.GetComponent<CityNpc>();if(!npc){npc=body.gameObject.AddComponent<CityNpc>();npc.Configure(Mathf.Abs(body.GetInstanceID()),job,null,"애프터라이트의 "+job+". 위기에는 직책과 지휘 계통에 맞게 행동한다.");}
            PeopleArt.Attach(body.gameObject,art);npc.personality+=" 나이 "+Age(npc)+"세. 직업에 맞는 어휘와 자연스러운 말투를 사용한다.";
        }
    }
}
