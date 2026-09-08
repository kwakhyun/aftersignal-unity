using System;
using UnityEngine;
namespace AfterSignal
{
    public sealed class ArmoryItem
    {
        public readonly string name,description;public readonly int slot,price,magazine,pack;public readonly float damage,interval,multiplier;
        public ArmoryItem(string name,int slot,int price,string description,float damage=0,float interval=.2f,int magazine=0,int pack=0,float multiplier=1)
        {this.name=name;this.slot=slot;this.price=price;this.description=description;this.damage=damage;this.interval=interval;this.magazine=magazine;this.pack=pack;this.multiplier=multiplier;}
    }
    public static class ArmoryInventory
    {
        const string Key="AFTERSIGNAL.Unity.Armory.v1";
        [Serializable]sealed class State{public int owned=7;public int[] equipped={0,1,2,-1,-1,-1,-1};public int[] rounds=new int[13],reserve=new int[13];}
        static State state;
        public static readonly ArmoryItem[] Items={
            new("월광 카타나",0,0,"기본 신호 검"),new("균열 대검",1,0,"기본 중량 검"),new("P9 권총",2,0,"기본 9mm 권총"),
            new("사쿠라 와키자시",0,420,"가벼운 단검형 검 · 피해 15% 증가",multiplier:1.15f),new("제로 엣지 카타나",0,1250,"단분자 날 · 피해 40% 증가",multiplier:1.4f),
            new("브레이커 대검",1,1700,"강화 중량 검 · 피해 35% 증가",multiplier:1.35f),new("M45 중권총",2,980,"대구경 권총 · 피해 70% 증가",multiplier:1.7f),new("위스퍼 권총",2,760,"안정화 권총 · 피해 25% 증가",multiplier:1.25f),
            new("AR-7 돌격소총",3,1600,"연사 · 30발 탄창 / 예비탄 90발 포함",24,.11f,30,90),new("MR-12 지정사수 소총",3,2400,"정밀 사격 · 12발 탄창 / 예비탄 36발",65,.44f,12,36),
            new("파편 수류탄",4,120,"투척 후 2.4초 폭발 · 1개",125,.8f,1,0),new("LANCER 바주카",5,2800,"직격 시 범위 폭발 · 로켓 4발 포함",210,1.2f,1,3),new("S12 산탄총",6,1450,"근거리 7펠릿 · 6발 탄창 / 예비탄 24발",15,.78f,6,24)
        };
        static void Load(){if(state!=null)return;try{state=JsonUtility.FromJson<State>(PlayerPrefs.GetString(Key,""));}catch{}if(state==null||state.equipped==null||state.equipped.Length!=7||state.rounds==null||state.rounds.Length!=Items.Length||state.reserve==null||state.reserve.Length!=Items.Length)state=new State();}
        public static void Save(){if(LifeState.SuppressSave)return;PlayerPrefs.SetString(Key,JsonUtility.ToJson(state));PlayerPrefs.Save();}
        public static void Reset(){state=new State();Save();}
        public static bool Owns(int id){Load();return id>=0&&id<Items.Length&&(state.owned&(1<<id))!=0;}
        public static int Equipped(int slot){Load();return slot>=0&&slot<7?state.equipped[slot]:-1;}
        public static int Rounds(int id){Load();return state.rounds[id];}
        public static int Reserve(int id){Load();return state.reserve[id];}
        public static bool Buy(int id)
        {
            Load();if(id<0||id>=Items.Length)return false;var item=Items[id];
            if(Owns(id)&&item.slot!=4){Equip(id);return true;}
            if(!LifeState.Spend(item.price))return false;
            bool owned=Owns(id);state.owned|=1<<id;state.equipped[item.slot]=id;
            if(item.slot==4)state.rounds[id]++;else if(!owned){state.rounds[id]=item.magazine;state.reserve[id]=item.pack;}
            Save();return true;
        }
        public static void Equip(int id){if(!Owns(id))return;state.equipped[Items[id].slot]=id;Save();}
        public static bool BuyAmmo(int id)
        {
            Load();if(!Owns(id)||Items[id].slot<3)return false;
            int price=Items[id].slot==5?180:Items[id].slot==4?120:75;
            if(!LifeState.Spend(price))return false;
            if(Items[id].slot==4)state.rounds[id]++;else state.reserve[id]+=Items[id].slot==5?1:Mathf.Max(12,Items[id].magazine*2);Save();return true;
        }
        public static bool Consume(int id){Load();if(state.rounds[id]<=0)return false;state.rounds[id]--;Save();return true;}
        public static bool Reload(int id){Load();int amount=Mathf.Min(Items[id].magazine-state.rounds[id],state.reserve[id]);if(amount<=0)return false;state.rounds[id]+=amount;state.reserve[id]-=amount;Save();return true;}
    }
}
