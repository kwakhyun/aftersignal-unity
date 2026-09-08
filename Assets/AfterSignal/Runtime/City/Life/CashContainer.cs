using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CashContainer:MonoBehaviour
    {
        public string locationId;public bool Home,Bank;public Transform Door,Cash;
        public bool Opening{get;private set;}public int LastLoot{get;private set;}
        static readonly HashSet<string> emptied=new();float remaining,next;
        string Key=>"AFTERSIGNAL.CashLoot."+LifeState.CampaignSerial+"."+GameDirector.Instance.stage+"."+(string.IsNullOrEmpty(locationId)?UrbanCatalog.Current.ToString():locationId)+"."+LifeState.Day;
        public bool Empty=>emptied.Contains(Key)||PlayerPrefs.GetInt(Key,0)>0;
        public void Open()=>CityLife.Instance.CashMenu(this);
        public void StartRobbery()
        {
            if(Home||Opening||Empty)return;
            CityLife.Instance.Dismiss();Opening=true;remaining=Bank?12:4;
            var g=GameDirector.Instance;CrimeObservation.Observe(Bank?45:20,transform.position);
            // The employee can report from the counter; completion still takes five
            // live seconds and is cancelled if the reporting witness is incapacitated.
            WorldActor clerk=null;float best=45*45;
            foreach(var npc in FindObjectsByType<CityNpc>()){var body=npc.GetComponent<WorldActor>();float d=(npc.transform.position-transform.position).sqrMagnitude;if(body&&body.Alive&&d<best){clerk=body;best=d;}}
            if(clerk)CrimeObservation.Queue(clerk,Bank?45:20,transform.position);
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
            if(remaining>0)return;Opening=false;LastLoot=Bank?Random.Range(8000,18001):Random.Range(350,1501);emptied.Add(Key);if(!LifeState.SuppressSave){PlayerPrefs.SetInt(Key,1);PlayerPrefs.Save();}LifeState.Earn(LastLoot);UrbanCrime.Instance?.RecordHeist();g.Toast("현금 "+LastLoot.ToString("N0")+" C 획득",5);
        }
    }
}
