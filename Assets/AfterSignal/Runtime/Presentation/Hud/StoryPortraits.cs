using System;
using UnityEngine;
namespace AfterSignal
{
    public static class StoryPortraits
    {
        static readonly string[] core={"노아","민재","은서","다은","수연","지훈","라온","윤"},world={"해진","다미","리안","나리","수호","미루","이솔","유건","유라","세린","도윤","시장 상인"};
        static Sprite[] first,second;static Sprite seo;
        static Sprite[] Load(string name){var a=Resources.LoadAll<Sprite>("Art/StoryCast/"+name);Array.Sort(a,(x,y)=>string.CompareOrdinal(x.name,y.name));return a;}
        public static Sprite Get(string name)
        {
            name??="";if(name.Contains("서하")){if(!seo)seo=Resources.Load<Sprite>("Art/Portraits/SeoDialogue");return seo;}
            first??=Load("CoreCast");second??=Load("WorldCast");Sprite result=null;int longest=0;
            for(int i=0;i<core.Length&&i<first.Length;i++)if(name.Contains(core[i])&&core[i].Length>longest){longest=core[i].Length;result=first[i];}
            for(int i=0;i<world.Length&&i<second.Length;i++)if(name.Contains(world[i])&&world[i].Length>longest){longest=world[i].Length;result=second[i];}
            if(!result&&name.StartsWith("민"))result=first.Length>1?first[1]:null;return result;
        }
    }
}
