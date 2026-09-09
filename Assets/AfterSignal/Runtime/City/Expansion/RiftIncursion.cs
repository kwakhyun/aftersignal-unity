using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class RiftIncursion:MonoBehaviour
    {
        public static RiftIncursion Instance {get;private set;}
        public bool Active {get;private set;}public Vector3 Position {get;private set;}
        public int Incidents {get;private set;}public int MilitaryShots {get;set;}
        public int CreatureCount=>creatures.Count;public bool RiftCity=>FourCityCatalog.CityAt(Position)==2;
        readonly List<RiftCreature> creatures=new();MilitaryResponse response;
        float next,started,pulse,warning,farSince;Vector3 pending;
        void Awake(){Instance=this;next=Time.time+Random.Range(35,65);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||g.stage!=StageId.UrbanCity)return;
            if(!Active)
            {
                if(warning>0){if(!LocalSimulation.CanStart(pending)){warning=0;CityEventGate.Cancel(this);return;}warning-=Time.deltaTime;if(warning<=0&&!Trigger(pending))CityEventGate.Cancel(this);return;}
                if(Time.time<next||CityEventGate.Busy)return;next=Time.time+18;
                if(OceanLife.Swimming||PrisonSystem.Instance&&PrisonSystem.Instance.Jailed||CampaignBattle.Active)return;
                if(!NearbySite(g.Player.transform.position,out pending))return;
                if(!CityEventGate.Begin(this,CityEventKind.Monster,pending))return;warning=7;g.ToastNear("긴급 재난 경보 · 인근에 대형 잠식 반응! 건물에서 떨어져 대피하세요.",pending,180,7);return;
            }
            if(!LocalSimulation.Within(Position,LocalSimulation.RetireRadius)){if(farSince==0)farSince=Time.time;if(Time.time-farSince>12){End();return;}}else farSince=0;
            if(LocalSimulation.Combat(Position)&&Time.time>pulse){pulse=Time.time+6;SignalEffects.Ring(Position+Vector3.up*.12f,new Color(.7f,.2f,1),24,1.4f);}
            bool alive=creatures.Exists(c=>c&&c.Body.Alive);
            if(!alive)
            {
                bool assisted=creatures.Exists(c=>c&&c.Body.LastPlayerHit>started);
                if(assisted){LifeState.Earn(1200);g.Toast("거대 잠식체 진압 · 전투 지원금 +1,200 C",6);}else g.ToastNear("대형 잠식체 격파 · 현장 통제 및 구조 작업 시작",Position,180,5);
                End();
            }
        }
        static bool NearbySite(Vector3 player,out Vector3 result)
        {
            for(int i=0;i<8;i++)
            {
                var desired=player+Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward*Random.Range(90,170);
                if(CityGangWar.FindGround(desired,out result)&&LocalSimulation.CanStart(result)&&!OceanLife.Contains(result))return true;
            }
            result=default;return false;
        }
        public bool Trigger(Vector3 requested)
        {
            if(Active||!LocalSimulation.CanStart(requested)||!CityGangWar.FindGround(requested,out var at))return false;
            if(!CityEventGate.Begin(this,CityEventKind.Monster,at))return false;
            Position=at;Active=true;Incidents++;started=Time.time;farSince=0;creatures.Clear();creatures.Add(RiftCreature.Create(at,Incidents%3));
            int count=RiftCity?5:Incidents%3==0?2:1;
            for(int i=1;i<count;i++)for(int attempt=0;attempt<12;attempt++)
            {
                var desired=at+Quaternion.Euler(0,i*72+attempt*29,0)*Vector3.forward*(38+i*9+attempt*2);
                if(!CityGangWar.FindGround(desired,out var second)||OceanLife.Contains(second)||!LocalSimulation.CanStart(second))continue;
                bool close=false;foreach(var c in creatures)if((c.transform.position-second).sqrMagnitude<30*30){close=true;break;}
                if(close)continue;creatures.Add(RiftCreature.Create(second,(Incidents+i)%3));break;
            }
            foreach(var c in creatures)CityEventGate.Enroll(c.Body);
            response=MilitaryResponse.ForIncident(this);SecurityResponse.Request(creatures[0].Body,true);
            foreach(var c in WorldActor.All)if(c&&!c.monster&&!c.helicopter&&!c.military&&!c.police&&(c.Center-at).sqrMagnitude<140*140){c.GetComponent<CityNpc>()?.Panic(at,35);NpcSpeech.Say(c,NpcDialogueBank.Line(c.GetComponent<CityNpc>(),"monster"),5,6);}
            GameDirector.Instance.ToastNear(RiftCity?"잠식체 군집 출현 · 전차·항공·폭격 전력 순차 출동":"대형 잠식체 출현 · 군 지원 도착까지 시민 대피 / 발광 기관이 열릴 때 공격",at,180,8);return true;
        }
        void End(){CityEventGate.Cancel(this);Active=false;next=Time.time+Random.Range(65,115);if(response)response.Withdraw();foreach(var c in creatures)if(c){if(c.Body.Alive)c.enabled=false;Destroy(c.gameObject,c.Body.Alive?0:12);}}
        void OnDestroy(){CityEventGate.Cancel(this);if(response)response.Withdraw();if(Instance==this)Instance=null;}
    }
}
