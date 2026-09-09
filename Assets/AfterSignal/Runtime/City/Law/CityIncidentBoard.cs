using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CityIncidentBoard:MonoBehaviour
    {
        public sealed class Incident{public Vector3 position;public string title;public Color color;public float radius;public WorldActor threat;}
        public static readonly List<Incident> Active=new();static readonly Dictionary<int,(float amount,float time)> contributions=new();static readonly HashSet<int> rewarded=new();
        public static int TotalReward{get;private set;}static int pending;float refresh,payout;
        public static void Contribution(WorldActor target,float amount,WorldActor source)
        {
            if(source||!target||!(target.monster||target.gang||target.terrorist)||target.environmental)return;
            var campaign=target.GetComponent<GangMember>();if(campaign&&campaign.CampaignUnit||target.Titan&&target.Titan.Campaign)return;
            int id=target.GetInstanceID();contributions.TryGetValue(id,out var old);contributions[id]=(old.amount+amount,Time.time);if(target.Downed||!target.Alive)Neutralized(target);
        }
        public static void Neutralized(WorldActor target)
        {
            if(!target)return;int id=target.GetInstanceID();if(rewarded.Contains(id)||!contributions.TryGetValue(id,out var record)||Time.time-record.time>100||record.amount<(target.monster?100:25))return;
            if(target.Alive&&!target.Downed)return;rewarded.Add(id);contributions.Remove(id);int reward=target.monster?650:target.terrorist?160:90;pending+=reward;TotalReward+=reward;LifeState.Earn(reward);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            if(pending>0&&Time.time>payout){payout=Time.time+4;g.Toast("현장 지원 보상 · 위협 제압 기여 +"+pending+" C",5);pending=0;}
            if(Time.time<refresh)return;refresh=Time.time+1;Active.Clear();
            foreach(var actor in WorldActor.All)
            {
                if(!actor||!actor.Alive||actor.Downed||!LocalSimulation.Combat(actor.transform.position)||!(actor.monster||actor.gang||actor.terrorist))continue;
                var gang=actor.GetComponent<GangMember>();if(gang&&gang.CampaignUnit||actor.Titan&&actor.Titan.Campaign)continue;
                string title=actor.monster?"잠식 몬스터 출현":actor.terrorist?"테러 신고 · 경찰 대응":actor.GetComponent<SeaCombat>()?"해적 습격 · 해경·해군 대응":"무장 조직 습격";
                if(actor.gang&&!actor.GetComponent<SeaCombat>()&&gang&&!gang.Target&&!gang.AttackingPlayer&&!actor.GetComponent<GangCrime>())continue;
                var p=actor.transform.position;bool merged=false;foreach(var item in Active)if(item.title==title&&(item.position-p).sqrMagnitude<100*100){merged=true;break;}
                if(!merged)Active.Add(new Incident{position=p,title=title,threat=actor,radius=actor.monster?65:35,color=actor.monster?new Color(.82f,.35f,1):actor.terrorist?new Color(1,.25f,.15f):new Color(1,.6f,.18f)});
            }
        }
        void OnDestroy(){Active.Clear();contributions.Clear();rewarded.Clear();pending=0;}
    }
}
