using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class StorySprites
    {
        public static readonly string[] Names={"노아","민재","은서","다은","수연","지훈","라온","윤","해진","다미","리안","나리","수호","미루","이솔","유건","유라","세린","도윤","시장 상인","아린","한결"};
        public static readonly string[] Atlases={"NoaMinjae","EunseoDaeun","SuyeonJihun","RaonYun","HaejinDami","RianNari","SuhoMiru","IsolYugeon","YuraSerin","DoyunMerchant","ArinHangyeol"};
        static readonly Dictionary<string,Sprite[]> cache=new();
        public static string Key(string name)
        {
            if(string.IsNullOrEmpty(name))return null;
            for(int i=0;i<Names.Length;i++)if(name==Names[i]||name.StartsWith(Names[i]+" ",StringComparison.Ordinal)||name.StartsWith(Names[i]+"·",StringComparison.Ordinal)||name.StartsWith(Names[i]+":",StringComparison.Ordinal))return "Story/"+i;
            return null;
        }
        public static string For(CityNpc npc)=>npc&&(npc.fixedQuest||npc.identity!=null&&npc.identity.StartsWith("authored-"))?Key(npc.displayName):null;
        public static Sprite[] Frames(string key)
        {
            if(cache.TryGetValue(key,out var frames))return frames;
            if(!int.TryParse(key.Substring(6),out int id)||id<0||id>=Names.Length)return Array.Empty<Sprite>();
            var sheet=Resources.LoadAll<Sprite>("Art/StorySprites/"+Atlases[id/2]);Array.Sort(sheet,(a,b)=>string.CompareOrdinal(a.name,b.name));
            frames=new Sprite[Mathf.Min(16,Mathf.Max(0,sheet.Length-id%2*16))];Array.Copy(sheet,id%2*16,frames,0,frames.Length);cache[key]=frames;return frames;
        }
    }
}
