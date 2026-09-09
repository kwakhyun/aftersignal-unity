using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class UrbanCrime:MonoBehaviour
    {
        public static UrbanCrime Instance{get;private set;}
        public static int RobbedSite=-1;
        public static float RobberyExpires;
        public static bool InsideRobbery=>GameDirector.Instance&&GameDirector.Instance.stage==StageId.UrbanInterior&&UrbanCatalog.Current==RobbedSite&&Time.realtimeSinceStartup<RobberyExpires;
        public int CompletedHeists{get;private set;}
        void Awake(){Instance=this;}
        IEnumerator Start()
        {
            yield return new WaitForSeconds(.5f);
            if(InsideRobbery)
            {
                for(int i=0;i<2;i++){var g=GangMember.Create(new Vector3(18+i*3,.1f,3),i,0);g.gameObject.AddComponent<GangCrime>().inside=true;NpcSpeech.Say(g,"움직이지 마! 돈부터 내놔!",6);}
                CitySafety.Shock(new Vector3(18,0,3));
            }
        }
        public void RecordHeist()=>CompletedHeists++;
        public void StartHeist()
        {
            if(CashLocations.Instance&&CashLocations.Instance.Counter){CashLocations.Instance.Counter.Open();return;}
            GameDirector.Instance?.Toast("매장 안의 현금 보관함에 접근하세요.");
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class StolenVehicle:MonoBehaviour{public GangMember Driver;}
    public sealed class GangCrime:MonoBehaviour
    {
        public bool inside;
        public int CrimeKind{get;private set;}
        public int Loot{get;private set;}
        public string State{get;private set;}="배회";
        GangMember gang;CityNpc victim;CityVehicle stolen;Vector3 target;float timer,nextPlan;
        bool acting,hidden;int site;
        public bool ActiveCrime=>acting||hidden||stolen||inside;
        void Start(){gang=GetComponent<GangMember>();nextPlan=Time.time+Random.Range(2,8);}
        void Update()
        {
            var game=GameDirector.Instance;if(!game||game.Blocked||!gang||!gang.Body.Alive)return;
            if(!LocalSimulation.Combat(transform.position))return;
            if(CityEventGate.Busy&&CityEventGate.Kind!=CityEventKind.Gang)return;
            if(stolen)
            {
                transform.position=stolen.transform.position;
                if(stolen.Wrecked||stolen.owned||Time.time>timer){SetHidden(false);GetComponent<CharacterController>().enabled=true;transform.position=CityRoadNetwork.Sidewalk(stolen.transform.position);stolen.traffic=stolen.owned?false:stolen.traffic;stolen.occupied=stolen.owned;Destroy(stolen.GetComponent<StolenVehicle>());stolen=null;State="도주";nextPlan=Time.time+35;}
                return;
            }
            if(inside){if(Time.time>nextPlan){nextPlan=Time.time+9;NpcSpeech.Say(this,"현금을 전부 가방에 넣어!",4);}return;}
            if(gang.Target&&!hidden){if(acting){acting=false;State="경찰과 대치";}return;}
            if(hidden)
            {
                if(Time.time>timer){Loot+=UrbanCatalog.Kind(site)==3?1200:320;SetHidden(false);GetComponent<CharacterController>().enabled=true;acting=false;State="도주";nextPlan=Time.time+50;}
                return;
            }
            if(!acting&&Time.time>nextPlan)Plan();
            if(!acting)return;
            if(CrimeKind==0&&victim)target=victim.transform.position;
            gang.GoTo(target);
            if(Vector3.Distance(transform.position,target)>2.6f)return;
            if(CrimeKind==0&&victim)
            {
                State="시민 협박";NpcSpeech.Say(this,"가지고 있는 돈 내놔!",4);NpcSpeech.Say(victim,"가져가세요... 다치게 하지 마세요!",4);
                Loot+=victim.TakeCash(Random.Range(45,141));victim.Panic(transform.position,9);CitySafety.Alarm(transform.position,gang.Body,victim);
                acting=false;nextPlan=Time.time+30;
            }
            else if(CrimeKind==1)
            {
                var sim=UrbanSimulation.Instance;
                CityVehicle car=null;float closest=8;
                foreach(var c in sim.Cars)if(c&&!c.owned&&!c.traffic&&!c.Wrecked&&!c.GetComponent<PoliceCar>()&&!c.GetComponent<EmergencyAmbulance>()){float d=Vector3.Distance(c.transform.position,transform.position);if(d<closest){closest=d;car=c;}}
                if(car)
                {
                    CitySafety.Alarm(transform.position,gang.Body);NpcSpeech.Say(this,"이 차면 되겠어.",3);
                    stolen=car;car.gameObject.AddComponent<StolenVehicle>().Driver=gang;car.occupied=true;car.traffic=true;car.route=CityRoadNetwork.TrafficLoop(Mathf.Clamp(Mathf.RoundToInt((car.transform.position.x-40)/140),0,4),Mathf.Clamp(Mathf.RoundToInt((car.transform.position.z+280)/140),0,3));car.waypoint=0;
                    timer=Time.time+28;SetHidden(true);GetComponent<CharacterController>().enabled=false;State="차량 절도";
                }
                else{acting=false;nextPlan=Time.time+8;}
            }
            else
            {
                State="실내 강도";UrbanCrime.RobbedSite=site;UrbanCrime.RobberyExpires=Time.realtimeSinceStartup+65;
                NpcSpeech.Say(this,"안으로 들어가. 금고부터 챙겨!",4);CitySafety.Alarm(target,gang.Body);
                timer=Time.time+30;SetHidden(true);GetComponent<CharacterController>().enabled=false;
            }
        }
        public void Plan(int requested=-1)
        {
            if(!gang||!CityEventGate.JoinGang(gang.Body)){nextPlan=Time.time+15;return;}
            CrimeKind=requested>=0?requested:Random.Range(0,3);var sim=UrbanSimulation.Instance;if(!sim)return;
            acting=false;
            if(CrimeKind==0)
            {
                float distance=60;
                foreach(var n in FindObjectsByType<CityNpc>())if(n&&n.GetComponent<WorldActor>().Alive&&!n.GetComponent<WorldActor>().Downed&&!n.GetComponent<WorldActor>().gang&&!n.GetComponent<WorldActor>().police&&!n.GetComponent<WorldActor>().military&&!n.fixedQuest){float d=Vector3.Distance(transform.position,n.transform.position);if(d<distance){distance=d;victim=n;}}
                if(victim){target=victim.transform.position;acting=true;}
            }
            else if(CrimeKind==1)
            {
                float distance=75;foreach(var c in sim.Cars)if(c&&!c.owned&&!c.traffic&&!c.Wrecked){float d=Vector3.Distance(transform.position,c.transform.position);if(d<distance){distance=d;target=c.transform.position+Vector3.forward*(c.HalfWidth+1);acting=true;}}
            }
            else
            {
                float distance=95;for(int i=0;i<UrbanCatalog.SiteCount;i++)if(UrbanCatalog.Kind(i)==3||UrbanCatalog.Kind(i)==7||UrbanCatalog.Kind(i)==9||UrbanCatalog.IsBar(i)){float d=Vector3.Distance(transform.position,UrbanCatalog.Door(i));if(d<distance){distance=d;target=UrbanCatalog.Door(i);site=i;acting=true;}}
            }
            State=CrimeKind==0?"대상에게 접근":CrimeKind==1?"주차 차량 접근":"시설로 이동";nextPlan=Time.time+12;
        }
        public void EjectFromWreck(CityVehicle vehicle,bool fatal=true)
        {
            SetHidden(false);stolen=null;acting=false;hidden=false;
            transform.position=vehicle.transform.position+vehicle.transform.forward*(vehicle.HalfWidth+1);
            var motor=GetComponent<CharacterController>();motor.enabled=true;
            var body=GetComponent<WorldActor>();if(fatal)body.health=0;
            gang.OnHit(vehicle.transform.forward*5,false);
            Destroy(vehicle.GetComponent<StolenVehicle>());
        }
        void OnDestroy(){if(stolen){stolen.occupied=stolen.owned;Destroy(stolen.GetComponent<StolenVehicle>());}}
        void SetHidden(bool value){hidden=value;foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=!value;}
    }
}
