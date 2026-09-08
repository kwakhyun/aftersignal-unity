using System;using System.Collections.Generic;using UnityEngine;
namespace AfterSignal
{
    public static class FacilityPeople
    {
        public static readonly string[] Sheets={"Aircrew","Travellers","Harbour","Coast","NightMarket","Utilities"};
        public static readonly string[] Jobs={"항공기 기장","승무원","지상 조업원","공항 보안 요원","여행객","출장객","배낭여행객","입국 여행객","부두 하역원","크레인 기사","상선 선원","세관 검사관","해변 구조대원","서퍼","어부","해변 상인","국수 요리사","야시장 상인","특송 배달원","클럽 경호원","역무원","시설 정비사","환경미화원","항만 구급대원"};
        static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        public static string Key(int role)=>"Facility"+Sheets[Mathf.Clamp(role,0,23)/4]+(role%4);
        public static Sprite Get(string key,int facing)
        {
            if(!key.StartsWith("Facility"))return null;string sheet=key.Substring(8,key.Length-9);int row=key[key.Length-1]-'0';
            if(!cache.TryGetValue(sheet,out var frames)){frames=Resources.LoadAll<Sprite>("Art/FacilityCitizens/"+sheet);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));cache[sheet]=frames;}
            if(frames.Length<16)return null;
            // The traveller and market sheets place their right profile in column one.
            bool reversed=sheet=="Travellers"||sheet=="NightMarket";
            int col=facing==0?0:facing==2?2:facing==1?(reversed?1:3):(reversed?3:1);return frames[row*4+col];
        }
        public static bool Flip(string key,int facing)=>key.StartsWith("Facility")&&facing==3&&!(key.Contains("Travellers")||key.Contains("NightMarket")||key.Contains("Aircrew"));
    }
}
