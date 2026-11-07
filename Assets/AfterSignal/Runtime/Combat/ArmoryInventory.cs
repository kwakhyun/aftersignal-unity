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
        static State state;static int stateGeneration=-1;
        public static readonly ArmoryItem[] Items={
            new("월광 카타나",0,0,"기본 신호 검"),new("균열 대검",1,0,"기본 중량 검"),new("P9 권총",2,0,"기본 9mm 권총"),
            new("사쿠라 와키자시",0,420,"가벼운 단검형 검 · 피해 15% 증가",multiplier:1.15f),new("제로 엣지 카타나",0,1250,"단분자 날 · 피해 40% 증가",multiplier:1.4f),
            new("브레이커 대검",1,1700,"강화 중량 검 · 피해 35% 증가",multiplier:1.35f),new("M45 중권총",2,980,"대구경 권총 · 피해 70% 증가",multiplier:1.7f),new("위스퍼 권총",2,760,"안정화 권총 · 피해 25% 증가",multiplier:1.25f),
            new("AR-7 돌격소총",3,1600,"연사 · 30발 탄창 / 예비탄 90발 포함",24,.11f,30,90),new("MR-12 지정사수 소총",3,2400,"정밀 사격 · 12발 탄창 / 예비탄 36발",65,.44f,12,36),
            new("파편 수류탄",4,120,"투척 후 2.4초 폭발 · 1개",125,.8f,1,0),new("LANCER 바주카",5,2800,"직격 시 범위 폭발 · 로켓 4발 포함",210,1.2f,1,3),new("S12 산탄총",6,1450,"근거리 7펠릿 · 6발 탄창 / 예비탄 24발",15,.78f,6,24)
        };
        static void Load(){if(state!=null&&stateGeneration==SaveStore.Generation)return;string json=SaveStore.GetString(Key,"");try{state=string.IsNullOrEmpty(json)?null:JsonUtility.FromJson<State>(json);}catch{state=null;}state=Migrate(state);stateGeneration=SaveStore.Generation;}
        /// <summary>
        /// Adding or removing armory items in a patch used to reset every purchase (array lengths no longer matched).
        /// Resize the per-item arrays instead, drop ownership bits for items that no longer exist and repair equipped slots.
        /// </summary>
        public static string MigrateJson(string json){State s=null;try{s=string.IsNullOrEmpty(json)?null:JsonUtility.FromJson<State>(json);}catch{}return JsonUtility.ToJson(Migrate(s));}
        static State Migrate(State s)
        {
            var fresh=new State();if(s==null)return fresh;
            s.rounds=Resize(s.rounds,Items.Length);s.reserve=Resize(s.reserve,Items.Length);
            s.owned&=Items.Length>=31?~0:(1<<Items.Length)-1;s.owned|=fresh.owned;
            var equipped=fresh.equipped;
            if(s.equipped!=null)for(int slot=0;slot<Mathf.Min(equipped.Length,s.equipped.Length);slot++){int id=s.equipped[slot];if(id>=0&&id<Items.Length&&Items[id].slot==slot&&(s.owned&(1<<id))!=0)equipped[slot]=id;}
            s.equipped=equipped;return s;
        }
        static int[] Resize(int[] values,int length){var result=new int[length];if(values!=null)System.Array.Copy(values,result,Mathf.Min(length,values.Length));for(int i=0;i<length;i++)result[i]=Mathf.Max(0,result[i]);return result;}
        public static void Save(){if(LifeState.SuppressSave)return;SaveStore.SetString(Key,JsonUtility.ToJson(state));SaveStore.Save();}
        public static void Reset(){state=new State();stateGeneration=SaveStore.Generation;Save();}
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
        public static bool Issue(int id)
        {
            Load();if(id<0||id>=Items.Length)return false;var item=Items[id];
            state.owned|=1<<id;state.equipped[item.slot]=id;
            state.rounds[id]=Mathf.Max(state.rounds[id],item.slot==4?6:item.magazine);
            state.reserve[id]=Mathf.Max(state.reserve[id],item.pack);Save();return true;
        }
        public static void Equip(int id){if(!Owns(id))return;state.equipped[Items[id].slot]=id;Save();}
        public static bool BuyAmmo(int id)
        {
            Load();if(!Owns(id)||Items[id].slot<3)return false;
            int price=Items[id].slot==5?180:Items[id].slot==4?120:75;
            if(!LifeState.Spend(price))return false;
            if(Items[id].slot==4)state.rounds[id]++;else state.reserve[id]+=Items[id].slot==5?1:Mathf.Max(12,Items[id].magazine*2);Save();return true;
        }
        /// <summary>Removes reserve ammunition of owned firearms (rifle, launcher, shotgun slots); loaded rounds stay. Returns rounds seized.</summary>
        public static int ConfiscateReserve()
        {
            Load();int seized=0;
            for(int id=0;id<Items.Length;id++){var item=Items[id];if(item.slot<3||item.slot==4||!Owns(id))continue;seized+=state.reserve[id];state.reserve[id]=0;}
            if(seized>0)Save();return seized;
        }
        public static bool Consume(int id){Load();if(state.rounds[id]<=0)return false;state.rounds[id]--;Save();return true;}
        public static bool Reload(int id){Load();int amount=Mathf.Min(Items[id].magazine-state.rounds[id],state.reserve[id]);if(amount<=0)return false;state.rounds[id]+=amount;state.reserve[id]-=amount;Save();return true;}
    }
}
