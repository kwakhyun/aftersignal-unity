using System;using System.Collections.Generic;using UnityEngine;
namespace AfterSignal
{
    public static class FacilityPeople
    {
        public static readonly string[] Sheets={"Aircrew","Travellers","Harbour","Coast","NightMarket","Utilities","Pelagic","NeonTrade","Arcology","CanalLife","CivicFuture","SkyWard"};
        public static readonly string[] Jobs={"항공기 기장","승무원","지상 조업원","공항 보안 요원","여행객","출장객","배낭여행객","입국 여행객","부두 하역원","크레인 기사","상선 선원","세관 검사관","해변 구조대원","서퍼","어부","해변 상인","국수 요리사","야시장 상인","특송 배달원","클럽 경호원","역무원","시설 정비사","환경미화원","항만 구급대원","해양 로봇 기술자","해조류 연구원","해협 운항 관제사","침몰선 잠수부","네온 간판 장인","수로시장 요리사","해협 특송원","거리 연주자","기업 조사관","데이터 기록사","도시 설계사","기업 보안관","여객항 매표원","항만 기관사","클럽 가수","노바 대학생","해안경비대원","방벽 정비사","노바 의사","교통 검표원","드론 조종사","공중정원 관리사","해협 기자","시민의회 의원"};
        static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        public static string Key(int role)=>"Facility"+Sheets[Mathf.Clamp(role,0,Jobs.Length-1)/4]+(role%4);
        public static Sprite Get(string key,int facing)
        {
            if(!key.StartsWith("Facility"))return null;string sheet=key.Substring(8,key.Length-9);int row=key[key.Length-1]-'0';
            if(!cache.TryGetValue(sheet,out var frames)){frames=Resources.LoadAll<Sprite>("Art/FacilityCitizens/"+sheet);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));cache[sheet]=frames;}
            if(frames.Length<16)return null;
            // The traveller and market sheets place their right profile in column one.
            bool reversed=sheet=="Travellers"||sheet=="NightMarket";
            int col=facing==0?0:facing==2?2:facing==1?(reversed?1:3):(reversed?3:1);return frames[row*4+col];
        }
        public static bool Flip(string key,int facing)=>key.StartsWith("Facility")&&System.Array.IndexOf(Sheets,key.Substring(8,key.Length-9))<6&&facing==3&&!(key.Contains("Travellers")||key.Contains("NightMarket")||key.Contains("Aircrew"));
    }
}
