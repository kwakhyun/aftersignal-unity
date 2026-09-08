using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class RiftIncursion:MonoBehaviour
    {
        public static RiftIncursion Instance {get;private set;}
        public bool Active {get;private set;}public Vector3 Position {get;private set;}
        public int Incidents {get;private set;}public int MilitaryShots {get;set;}
        readonly List<RiftCreature> creatures=new();readonly List<ArmyResponder> troops=new();CityVehicle support;
        float next,dispatch,started,pulse;GameObject beacon;
        void Awake(){Instance=this;next=Time.time+Random.Range(75,130);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked)return;
            if(!Active)
            {
                if(Time.time<next)return;next=Time.time+30;
                if(g.Player.transform.position.y>3||OceanLife.Swimming||PrisonSystem.Instance&&PrisonSystem.Instance.Jailed)return;
                var direction=Random.insideUnitCircle.normalized;var desired=g.Player.transform.position+new Vector3(direction.x,0,direction.y)*105;
                var at=CityRoadNetwork.NearestJunction(desired);if(Vector3.Distance(at,g.Player.transform.position)<48)return;
                Trigger(at);return;
            }
            if(dispatch>0&&Time.time>=dispatch){dispatch=0;Dispatch();}
            if(Time.time>pulse){pulse=Time.time+2;SignalEffects.Ring(Position+Vector3.up*.12f,new Color(.7f,.2f,1),8,1.4f);}
            bool alive=false;foreach(var c in creatures)if(c&&c.Body.Alive)alive=true;
            if(!alive||Time.time-started>210)
            {
                if(!alive){bool assisted=creatures.Exists(c=>c&&c.Body.LastPlayerHit>started);if(assisted){LifeState.Earn(350);g.Toast("잔향체 진압 협력 · 정부 대응 수당 +350 C",6);}else g.Toast("국방대응부가 신호 균열을 봉쇄했습니다.",5);}
                else g.Toast("해당 구역의 신호 균열이 약화되어 소멸했습니다.",5);
                End();
            }
        }
        public bool Trigger(Vector3 requested)
        {
            if(Active)return false;
            if(!CityGangWar.FindGround(requested,out var at))return false;
            Position=at;Active=true;Incidents++;started=Time.time;dispatch=Time.time+8;creatures.Clear();troops.Clear();
            for(int i=0;i<3+Incidents%3;i++)
            {var p=at+new Vector3(Mathf.Cos(i*2.1f)*5,0,Mathf.Sin(i*2.1f)*5);if(CityGangWar.FindGround(p,out var ground))creatures.Add(RiftCreature.Create(ground,i%3));}
            if(creatures.Count==0){Active=false;return false;}
            beacon=new GameObject("Signal rupture / lost collective memory");beacon.transform.position=at;
            var l=beacon.AddComponent<Light>();l.type=LightType.Point;l.range=24;l.intensity=12;l.color=new Color(.5f,.12f,1);
            CitySafety.Shock(at);foreach(var c in WorldActor.All)if(c&&!c.monster&&!c.helicopter&&(c.Center-at).sqrMagnitude<60*60){c.GetComponent<CityNpc>()?.Panic(at,25);NpcSpeech.Say(c,"저건 사람이 아니야! 신호가 찢어지고 있어!",5,6);}
            GameDirector.Instance.Toast("신호 이상 · 정체불명의 잔향체 출몰 / 국방대응부 출동",7);return true;
        }
        void Dispatch()
        {
            for(int i=0;i<6;i++)
            {var near=Position+new Vector3(34+i%3*3,0,-15+i/3*4);if(CityGangWar.FindGround(near,out var at))troops.Add(ArmyResponder.Create(at,i,this));}
            if(UrbanSimulation.Instance)
            {
                support=UrbanSimulation.Instance.Spawn(Position+new Vector3(40,28,-30),false,(int)CityVehicleType.CombatHelicopter);
                support.name="국방대응부 / 잔향체 진압 헬기";support.gameObject.AddComponent<ArmySupport>().Initialize(this,support,troops.Count>0?troops[0].Body:null);
            }
            foreach(var t in troops)if(t)NpcSpeech.Say(t,"민간인 대피! 잔향체만 조준한다!",4,5);
        }
        void End()
        {
            Active=false;next=Time.time+Random.Range(190,350);if(beacon)Destroy(beacon);
            foreach(var c in creatures)if(c)Destroy(c.gameObject,8);foreach(var t in troops)if(t)Destroy(t.gameObject,18);
            if(support&&(!UrbanSimulation.Instance||UrbanSimulation.Instance.Current!=support))Destroy(support.gameObject,20);
        }
        void OnDestroy(){if(beacon)Destroy(beacon);if(Instance==this)Instance=null;}
    }
}
