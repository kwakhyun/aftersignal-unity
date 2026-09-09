using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CityActivityDirector:MonoBehaviour
    {
        readonly List<FamilyGroup> groups=new();float next;
        IEnumerator Start()
        {
            while(!GameDirector.Instance||!GameDirector.Instance.Ready)yield return null;
            if(GameDirector.Instance.stage==StageId.UrbanCity){gameObject.AddComponent<CityIncidentBoard>();gameObject.AddComponent<HarborAccess>();gameObject.AddComponent<MaritimeWorld>();gameObject.AddComponent<CivicTerrorEvents>();}
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||Time.time<next)return;next=Time.time+5;
            if(g.stage!=StageId.UrbanCity||FourCityCatalog.CityAt(g.Player.transform.position)==2)return;
            groups.RemoveAll(x=>!x);foreach(var group in groups)if(group&&(group.transform.position-g.Player.transform.position).sqrMagnitude>320*320)Destroy(group.gameObject);
            foreach(var a in WorldActor.All)if(a&&!a.GetComponent<Commuter>()&&!a.GetComponent<FamilyMember>()&&a.GetComponent<CityPedestrian>()&&!a.police&&!a.military&&!a.gang&&!a.protectedResident)a.gameObject.AddComponent<Commuter>();
            if(groups.Count>=8)return;
            var p=g.Player.transform.position+Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward*Random.Range(45,105);
            p=LocalCityRoutes.Sidewalk(p);if(!CrowdFlow.Place(p,groups.Count,out p,14))return;
            var v=Camera.main.WorldToViewportPoint(p);if(v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1)return;
            var createdGroup=FamilyGroup.Create(p,Random.Range(0,100000),groups.Count%2==0);groups.Add(createdGroup);

        }
    }
    public sealed class Commuter:MonoBehaviour
    {
        Vector3 home,work;CityPedestrian walker;CityNpc npc;int period=-1;float next;public string Journey{get;private set;}
        void Start(){walker=GetComponent<CityPedestrian>();npc=GetComponent<CityNpc>();home=transform.position;work=LocalCityRoutes.Sidewalk(home+Quaternion.Euler(0,npc.variation*31,0)*Vector3.forward*(45+npc.variation*2));}
        void Update()
        {
            var g=GameDirector.Instance;var body=GetComponent<WorldActor>();if(!g||g.Blocked||!walker||!npc||!body||!body.Alive||body.Downed||npc.Fleeing||Time.time<next)return;next=Time.time+2;
            float h=LifeState.Hour;int p=h>=6.5f&&h<9?1:h>=17.5f&&h<21?2:0;
            if(p!=period){period=p;Journey=p==1?"출근 중":p==2?"퇴근 중":"동네 산책 중";npc.context="현재 "+Journey+". 직업은 "+npc.occupation+"이며 아침에는 직장으로, 저녁에는 집으로 이동한다.";}
            if(p>0){var goal=(p==1?work:home);if((transform.position-goal).sqrMagnitude>9)walker.WalkTo(goal,false);}
        }
    }
    [DefaultExecutionOrder(1000)]
    public sealed class FamilyMember:MonoBehaviour
    {
        public FamilyGroup Group;public string Role;public bool Child,DeathAnnounced;public bool Following=>Group;
        void LateUpdate(){if(Child)transform.localScale=Vector3.one*.73f;}
    }
    public sealed class FamilyGroup:MonoBehaviour
    {
        public readonly List<WorldActor> Members=new();public bool Family;public Vector3 Home,Destination;public VenueRuntime Venue;
        Vector3 danger,heading=Vector3.forward;float alarmUntil,plan,chat,lastReaction,tick,lastTick;int seed;readonly PursuitPath path=new();
        public static FamilyGroup Create(Vector3 at,int seed,bool family)
        {
            if(!PopulationBudget.Room(at)||!PopulationBudget.ClaimFrame(family?3:2))return null;
            var g=new GameObject(family?"함께 다니는 가족":"함께 다니는 연인").AddComponent<FamilyGroup>();g.transform.position=at;g.Home=at;g.seed=seed;g.Family=family;g.Destination=at;
            for(int i=0;i<(family?3:2);i++)
            {
                var go=new GameObject(family?i==0?"가족 / 보호자":i==1?"가족 / 부모":"가족 / 자녀":"커플 / 연인",typeof(SpriteRenderer),typeof(CityNpc),typeof(FamilyMember));go.transform.SetParent(g.transform,false);go.transform.position=at+Vector3.right*(i-1)*1.3f;
                string art=i==2?(seed%2==0?"StudentBoy":"StudentGirl"):i==0?"CivilianMan":"CivilianWoman";string role=family?i==2?"자녀":i==0?"아버지":"어머니":"연인";
                var npc=go.GetComponent<CityNpc>();npc.Configure(seed+i, i==2?"초등학생":i==0?"회사원":"기술자",null,"가족 및 연인과 함께 생활한다. 일행의 안전을 중요하게 생각한다.");
                go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");PeopleArt.Attach(go,art);var m=go.GetComponent<FamilyMember>();m.Group=g;m.Role=role;m.Child=i==2;g.Members.Add(go.GetComponent<WorldActor>());
            }
            return g;
        }
        public void Panic(Vector3 at,float duration){danger=at;alarmUntil=Mathf.Max(alarmUntil,Time.time+duration);plan=0;}
        public static void Harm(WorldActor victim,bool dead,Vector3 danger,WorldActor source)
        {
            var m=victim.GetComponent<FamilyMember>();if(!m||!m.Group||dead&&m.DeathAnnounced)return;if(dead)m.DeathAnnounced=true;var g=m.Group;if(!dead&&Time.time<g.lastReaction+3)return;g.lastReaction=Time.time;g.danger=danger;g.alarmUntil=Time.time+(dead?38:24);g.plan=0;
            foreach(var a in g.Members)if(a&&a!=victim&&a.Alive&&!a.Downed)
            {
                var role=a.GetComponent<FamilyMember>();string key=dead?"companion_death":source&&source.environmental?"companion_accident":"companion_hurt";string line=NpcDialogueBank.Line(a.GetComponent<CityNpc>(),key);
                NpcSpeech.Say(a,line,6,12);a.GetComponent<CityNpc>().SocialUntil=Time.time+2;
            }
            CitySafety.Shock(victim.transform.position);CitySafety.Alarm(victim.transform.position,source,victim.GetComponent<CityNpc>());
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||game.Blocked)return;WorldActor leader=null;
            foreach(var a in Members)if(a&&a.Alive&&!a.Downed){leader=a;break;}if(!leader)return;
            bool fleeing=Time.time<alarmUntil;if(!ActorWorkBudget.Tick(this,ref tick,ref lastTick,out var dt,fleeing))return;float hour=LifeState.Hour;
            if(Time.time>plan){plan=Time.time+14+seed%9;Vector3 proposed=fleeing?leader.transform.position+(leader.transform.position-danger).normalized*38:Venue&&Venue.activityPoints.Count>0?Venue.transform.TransformPoint(Venue.activityPoints[Random.Range(0,Venue.activityPoints.Count)]):hour>=18||hour<7?Home:LocalCityRoutes.Sidewalk(Home+Quaternion.Euler(0,seed+Time.time*.1f,0)*Vector3.forward*Random.Range(20,65));if(CrowdFlow.Place(proposed,seed,out var safe,12))Destination=safe;}
            Vector3 travel=path.Direction(leader.transform.position,Destination);if(travel.sqrMagnitude>.01f)heading=travel;
            for(int i=0;i<Members.Count;i++)
            {
                var a=Members[i];if(!a||!a.Alive||a.Downed||a.GetComponent<MedicalPending>()||CivilianImpact.Active(a))continue;
                var goal=a==leader?(Vector3.Distance(leader.transform.position,Destination)>1?leader.transform.position+travel*2:leader.transform.position):leader.transform.position+Quaternion.Euler(0,90,0)*heading*(i==1?1.25f:-1.25f)-heading*.65f;
                if((goal-a.transform.position).sqrMagnitude>.18f)PedestrianSteering.For(a).Move(goal,dt*(fleeing?3.8f:1.6f));
            }
            if(Time.time>chat&&!fleeing){chat=Time.time+22+seed%13;var member=Members[seed%Members.Count];if(member&&member.Alive)NpcSpeech.Say(member,Family?(hour<9?"학교 가기 전에 손 꼭 잡자.":hour>=18?"오늘 저녁은 집에서 같이 먹자.":"저기 구경하고 같이 돌아가자. 혼자 멀리 가지 말고."):(hour<9?"출근길에 같이 걸으니 좋다. 퇴근하고 또 보자.":hour>=18?"오늘도 고생했어. 집에 가기 전에 밥 먹을까?":"너랑 걸으면 익숙한 거리도 새롭게 보여."),4,1);}
        }
    }
}
