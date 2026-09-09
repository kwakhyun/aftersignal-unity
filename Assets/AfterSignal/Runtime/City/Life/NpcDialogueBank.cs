using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class NpcDialogueBank
    {
        static readonly Dictionary<string,string[]> bank=new Dictionary<string,string[]>();
        static readonly Dictionary<string,int> previous=new Dictionary<string,int>();
        static void Load(){if(bank.Count>0)return;var text=Resources.Load<TextAsset>("Dialogue/StreetVoices");if(!text)return;foreach(var line in text.text.Split('\n')){int cut=line.IndexOf('\t');if(cut<=0)continue;bank[line.Substring(0,cut)]=line.Substring(cut+1).Trim().Split('|');}}
        public static int Count {get{Load();int n=0;foreach(var x in bank.Values)n+=x.Length;return n;}}
        public static string Line(CityNpc npc,string eventName)
        {
            Load();string role=npc?NpcVoice.Role(npc):"";string group=NpcPersona.Group(npc);
            string key=eventName+"."+group;
            var family=npc?npc.GetComponent<FamilyMember>():null;
            if(family&&eventName.StartsWith("companion_"))key=eventName+"."+(family.Child?"child":family.Group&&family.Group.Family?"parent":"lover");
            if(eventName=="ambient")
            {
                float hour=LifeState.Hour;string period=hour>=6&&hour<9?"morning":hour>=17&&hour<21?"evening":hour>=21||hour<6?"night":"day";
                if(bank.ContainsKey(period+"."+group))key=period+"."+group;
                else if((period=="morning"||period=="evening")&&bank.ContainsKey(period+".general"))key=period+".general";
            }
            if(eventName=="ambient"&&!bank.ContainsKey(key))
            {
                string site=role.Contains("Aircrew")||role.Contains("Travellers")?"airport":role.Contains("Harbour")?"harbour":role.Contains("Coast")?"coast":role.Contains("NightMarket")?"market":role.Contains("Utilities")?"transit":LifeState.Hour>=20||LifeState.Hour<6?"night":"day";
                key="ambient."+site;
                if(npc&&NeonHarbor.Region(npc.transform.position))key="ambient.nova";
            }
            if(!bank.TryGetValue(key,out var choices)&&!bank.TryGetValue(eventName+".general",out choices)&&!bank.TryGetValue("ambient.day",out choices))return "...";
            string memory=(npc?npc.identity:"world")+":"+key;int next=Random.Range(0,choices.Length);
            if(previous.TryGetValue(memory,out int last)&&last==next)next=(next+1)%choices.Length;
            if(previous.Count>2048)previous.Clear();previous[memory]=next;return choices[next];
        }
        public static string[] Exchange(CityNpc npc)
        {
            string pair=Line(npc,"exchange");return pair.Split('~');
        }
    }
}
