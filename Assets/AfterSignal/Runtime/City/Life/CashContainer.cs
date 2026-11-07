using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CashContainer:MonoBehaviour
    {
        public string locationId;public bool Home,Bank;public Transform Door,Cash;
        public bool Opening{get;private set;}public int LastLoot{get;private set;}
        // One key per location holding "campaignSerial:dayLastOpened" (bounded), replacing the per-day keys.
        static readonly Dictionary<string,string> session=new();float remaining,next;
        string Location=>GameDirector.Instance.stage+"."+(string.IsNullOrEmpty(locationId)?UrbanCatalog.Current.ToString():locationId);
        string Key=>"AFTERSIGNAL.Unity.CashLoot."+Location;
        string LegacyKey=>"AFTERSIGNAL.CashLoot."+LifeState.CampaignSerial+"."+Location+"."+LifeState.Day;
        bool cached;int openedDay;
        int OpenedDay
        {
            get
            {
                if(cached)return openedDay;cached=true;openedDay=int.MinValue/2;
                string value=session.TryGetValue(Key,out var v)?v:SaveStore.GetString(Key,"");int split=value.IndexOf(':');
                if(split>0&&int.TryParse(value.Substring(0,split),out int serial)&&serial==LifeState.CampaignSerial&&int.TryParse(value.Substring(split+1),out int day))openedDay=day;
                else if(SaveStore.GetInt(LegacyKey,0)>0)openedDay=LifeState.Day;
                return openedDay;
            }
        }
        public int CooldownDays=>Bank?GameTuning.Active.vaultCooldownDays:GameTuning.Active.registerCooldownDays;
        public int DaysUntilRefill=>Home?0:Mathf.Max(0,Mathf.Min(OpenedDay,LifeState.Day)+CooldownDays-LifeState.Day);
        public bool Empty=>DaysUntilRefill>0;
        void MarkOpened()
        {
            string value=LifeState.CampaignSerial+":"+LifeState.Day;openedDay=LifeState.Day;cached=true;session[Key]=value;
            if(!LifeState.SuppressSave){SaveStore.SetString(Key,value);SaveStore.DeleteKey(LegacyKey);SaveStore.Save();}
        }
        public void Open()=>CityLife.Instance.CashMenu(this);
        public void StartRobbery()
        {
            if(Home||Opening||Empty)return;
            var t=GameTuning.Active;float report=Bank?t.vaultReportHeat:t.registerReportHeat;
            CityLife.Instance.Dismiss();Opening=true;remaining=Bank?t.vaultSeconds:t.registerSeconds;
            var g=GameDirector.Instance;CrimeObservation.Observe(report,transform.position);
            // The employee can report from the counter; completion still takes five
            // live seconds and is cancelled if the reporting witness is incapacitated.
            WorldActor clerk=null;float best=45*45;
            foreach(var npc in FindObjectsByType<CityNpc>()){var body=npc.GetComponent<WorldActor>();float d=(npc.transform.position-transform.position).sqrMagnitude;if(body&&body.Alive&&d<best){clerk=body;best=d;}}
            if(clerk)CrimeObservation.Queue(clerk,report,transform.position);
            CitySafety.Shock(transform.position);NpcSpeech.Say(g.Player,Bank?"금고 열어. 현금을 가져간다.":"계산대 돈을 내놔.",3);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            if(Cash)Cash.gameObject.SetActive(Home?LifeState.HomeCash>0:!Empty);
            if(Door)Door.localRotation=Quaternion.RotateTowards(Door.localRotation,Quaternion.Euler(0,Opening||!Home&&Empty?-95:0,0),Time.deltaTime*35);
            if(!Opening)return;
            if(g.Player.Health<=0||Vector3.Distance(g.Player.transform.position,transform.position)>6){Opening=false;g.Toast("금고에서 벗어나 현금 탈취가 중단됐습니다.");return;}
            remaining-=Time.deltaTime;if(Time.time>next){next=Time.time+1;g.Toast((Bank?"금고 개방":"현금 회수")+" · "+Mathf.CeilToInt(remaining)+"초",1.1f);}
            if(remaining>0)return;Opening=false;
            var t=GameTuning.Active;
            LastLoot=Mathf.RoundToInt((Bank?Random.Range(t.vaultMin,t.vaultMax+1):Random.Range(t.registerMin,t.registerMax+1))*DifficultySettings.Payout);
            MarkOpened();LifeState.Earn(LastLoot);UrbanCrime.Instance?.RecordHeist();
            // The vault alarm reports directly to the police, witnesses or not.
            float alarm=Bank?t.vaultAlarmHeat:t.registerAlarmHeat;if(alarm>0)WantedSystem.ConfirmReport(alarm,transform.position);
            g.Toast("현금 "+LastLoot.ToString("N0")+" C 획득"+(alarm>0?" · 금고 경보 작동 · 수배 "+WantedSystem.Level+"단계":"")+" · "+CooldownDays+"일 동안 다시 채워지지 않습니다",6);
        }
    }
}
