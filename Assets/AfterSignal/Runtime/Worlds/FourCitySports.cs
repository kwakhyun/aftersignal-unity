using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed class FourCitySports:MonoBehaviour
    {
        [Serializable] sealed class League {public List<SportsMatch> matches=new();}
        const string Key="AFTERSIGNAL.Unity.FourCities.Sports";
        public static FourCitySports Instance{get;private set;}
        static League league;
        float saveTime;
        public IReadOnlyList<SportsMatch> Matches=>league.matches;
        void Awake(){Instance=this;if(league==null){try{league=JsonUtility.FromJson<League>(PlayerPrefs.GetString(Key,""));}catch{}league??=new League();}foreach(var v in FourCityCatalog.Venues)if(v.Sport&&!league.matches.Exists(m=>m.venue==v.id)){var m=new SportsMatch(v);if(v.kind==VenueKind.Baseball)m.battingTeam=1;league.matches.Add(m);}}
        public SportsMatch Get(string id)=>league.matches.Find(m=>m.venue==id);
        public bool Bet(SportsMatch m,int team,int amount)
        {if(m==null||!m.CanBet||team<0||team>1||amount<=0||amount>500||!LifeState.Spend(amount))return false;m.stake=amount;m.selection=team;m.settled=false;Save();return true;}
        public void Settle(SportsMatch m)
        {if(m.phase!=MatchPhase.Final||m.settled)return;m.settled=true;m.payout=m.stake==0?0:m.Winner==-1?m.stake:m.Winner==m.selection?m.stake*2:0;Save();if(m.payout>0)LifeState.Earn(m.payout);if(m.stake>0)GameDirector.Instance?.Toast(m.payout>0?"경기 결과 · "+m.payout+" C 정산":"경기 종료 · 승부예측 미적중",5);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Paused||g.Title||g.Dead)return;
            foreach(var m in league.matches){m.Tick(Mathf.Min(Time.deltaTime,.1f));Settle(m);}
            for(int i=0;i<league.matches.Count;i++){var m=league.matches[i];if(m.phase==MatchPhase.Final&&m.finalSeconds>90){var v=Array.Find(FourCityCatalog.Venues,v=>v.id==m.venue);var next=new SportsMatch(v){round=m.round+1,random=m.random,wait=90};for(int car=0;car<6;car++)next.racePace[car]=(next.Roll()-.5f)*4;if(v.kind==VenueKind.Baseball)next.battingTeam=1;league.matches[i]=next;}}
            if(Time.unscaledTime>saveTime){saveTime=Time.unscaledTime+10;Save();}
        }
        public void Save(){if(LifeState.SuppressSave)return;PlayerPrefs.SetString(Key,JsonUtility.ToJson(league));PlayerPrefs.Save();}
        void OnApplicationQuit()=>Save();
        void OnDestroy(){Save();if(Instance==this)Instance=null;}
    }
}
