using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class RiftIncursion:MonoBehaviour
    {
        public static RiftIncursion Instance {get;private set;}
        public bool Active {get;private set;}public Vector3 Position {get;private set;}
        public int Incidents {get;private set;}public int MilitaryShots {get;set;}
        readonly List<RiftCreature> creatures=new();MilitaryResponse response;
        float next,started,pulse,warning;Vector3 pending;
        void Awake(){Instance=this;next=Time.time+Random.Range(35,65);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||g.stage!=StageId.UrbanCity)return;
            if(!Active)
            {
                if(warning>0){warning-=Time.deltaTime;if(warning<=0)Trigger(pending);return;}
                if(Time.time<next)return;next=Time.time+18;
                if(OceanLife.Swimming||PrisonSystem.Instance&&PrisonSystem.Instance.Jailed||CampaignBattle.Active)return;
                if(!ResponseDispatch.TryOrigin(g.Player.transform.position,true,false,0,out pending))return;
                warning=7;g.Toast("긴급 재난 경보 · 인근에 대형 잠식 반응! 건물에서 떨어져 대피하세요.",7);return;
            }
            if(Time.time>pulse){pulse=Time.time+6;SignalEffects.Ring(Position+Vector3.up*.12f,new Color(.7f,.2f,1),24,1.4f);}
            bool alive=creatures.Exists(c=>c&&c.Body.Alive);
            if(!alive)
            {
                bool assisted=creatures.Exists(c=>c&&c.Body.LastPlayerHit>started);
                if(assisted){LifeState.Earn(1200);g.Toast("거대 잠식체 진압 · 전투 지원금 +1,200 C",6);}else g.Toast("대형 잠식체 격파 · 현장 통제 및 구조 작업 시작",5);
                End();
            }
            // Distant incidents may be retired to bound simulation cost. Visible living titans never time out.
            else if(Time.time-started>600&&(g.Player.transform.position-Position).sqrMagnitude>1200*1200)End();
        }
        public bool Trigger(Vector3 requested)
        {
            if(Active||!CityGangWar.FindGround(requested,out var at))return false;
            Position=at;Active=true;Incidents++;started=Time.time;creatures.Clear();creatures.Add(RiftCreature.Create(at,Incidents%3));
            if(Incidents%3==0&&CityGangWar.FindGround(at+Vector3.right*35,out var second))creatures.Add(RiftCreature.Create(second,(Incidents+1)%3));
            response=MilitaryResponse.ForIncident(this);
            foreach(var c in WorldActor.All)if(c&&!c.monster&&!c.helicopter&&(c.Center-at).sqrMagnitude<140*140){c.GetComponent<CityNpc>()?.Panic(at,35);NpcSpeech.Say(c,NpcDialogueBank.Line(c.GetComponent<CityNpc>(),"monster"),5,6);}
            GameDirector.Instance.Toast("대형 잠식체 출현 · 군 지원 도착까지 시민 대피 / 발광 기관이 열릴 때 공격",8);return true;
        }
        void End(){Active=false;next=Time.time+Random.Range(65,115);if(response)response.Withdraw();foreach(var c in creatures)if(c)Destroy(c.gameObject,12);}
        void OnDestroy(){if(response)response.Withdraw();if(Instance==this)Instance=null;}
    }
}
