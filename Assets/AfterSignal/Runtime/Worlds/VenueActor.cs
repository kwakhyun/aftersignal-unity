using UnityEngine;
namespace AfterSignal
{
    [DefaultExecutionOrder(950)]
    public sealed class VenueActor:MonoBehaviour
    {
        public VenueRuntime venue;public Vector3 origin;
        public bool staff,spectator,athlete;
        public int team,slot,serial;
        public string Activity{get;private set;}="이동 중";
        CityNpc npc;WorldActor body;DirectionalPerson art;SpriteRenderer sprite;Vector3 target;float wait,chat,actionUntil,tick,lastTick;int state;PedestrianSteering steering;
        public static VenueActor Create(VenueRuntime venue,int id,string role,string sheet,Vector3 at)
        {
            var go=new GameObject(role+" / "+id,typeof(SpriteRenderer),typeof(CityNpc),typeof(VenueActor));go.transform.SetParent(venue.transform,false);go.transform.localPosition=at;
            var sr=go.GetComponent<SpriteRenderer>();sr.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");sr.sprite=PeopleArt.Get(sheet,0);
            var npc=go.GetComponent<CityNpc>();npc.Configure(id,role,null,venue.Definition.title+"에서 "+role+"으로 생활한다. "+FacilityGuide.For(venue.Definition));PeopleArt.Attach(go,sheet);
            var a=go.GetComponent<VenueActor>();a.venue=venue;a.origin=at;a.target=at;a.npc=npc;a.body=go.GetComponent<WorldActor>();a.art=go.GetComponent<DirectionalPerson>();a.sprite=sr;a.chat=Time.time+10+id%19;return a;
        }
        public void TakeStartingPosition(SportsMatch match){Play(match);transform.localPosition=target;if(athlete&&!GetComponent<AthleteMotion>())gameObject.AddComponent<AthleteMotion>();}
        public void EmergencyMove(Vector3 point){if(art){art.Sitting=false;art.Face(point,.2f);}Activity=staff?"대피 안내":"비상구로 대피";if(!steering)steering=PedestrianSteering.For(this);steering.Move(point,(staff?2.6f:athlete?5.2f:3.1f)*Mathf.Min(Time.deltaTime,.06f));}
        public void Play(SportsMatch m)
        {
            if(!body||!body.Alive)return;
            float side=-m.AttackDirection(team);var kind=venue.Definition.kind;
            if(kind==VenueKind.Football)
            {
                if(slot==0)target=new(side*49,.08f,Mathf.Clamp(m.ballZ*.24f,-3,3));
                else {int row=(slot-1)/4,col=(slot-1)%4;target=new(side*(30-row*18)+m.ballX*.22f,.08f,(col-1.5f)*14+m.ballZ*.12f);}
                if(team==m.possessingTeam&&slot==1+m.passes%10){target=new(m.ballX,.08f,m.ballZ);actionUntil=Time.time+.55f;}
                else if(team!=m.possessingTeam&&slot==2+m.passes%8)target=new(m.ballX-side*4,.08f,m.ballZ+2);
            }
            else if(kind==VenueKind.Basketball)
            {
                float direction=m.AttackDirection(m.possessingTeam);
                target=new(direction*(slot==0?5:slot<3?8:11)+(team==m.possessingTeam?0:direction*1.3f),.08f,(slot-2)*2.7f);
                if(team==m.possessingTeam&&slot==m.passes%5){target=new(m.ballX,.08f,m.ballZ);actionUntil=Time.time+.5f;}
            }
            else
            {
                Vector3[] field={new(0,.3f,-25.56f),new(0,.08f,-46),new(22,.08f,-25),new(10,.08f,-8),new(-22,.08f,-25),new(-10,.08f,-8),new(-42,.08f,22),new(0,.08f,40),new(42,.08f,22)};
                if(team!=m.battingTeam){target=field[slot];if(slot>=2&&m.lastEvent.Contains("타"))target=Vector3.Lerp(target,new Vector3(m.ballX,.08f,m.ballZ),.45f);}
                else if(slot==m.Batter){target=new(-1,.08f,-44);actionUntil=Time.time+.5f;}
                else if(slot<3&&(m.bases&(1<<slot))!=0)target=slot==0?new(19.4f,.08f,-24.6f):slot==1?new(0,.08f,-5.2f):new(-19.4f,.08f,-24.6f);
                else target=new(team==0?-28:28,.08f,-37+slot*.35f);
            }
            if(m.phase==MatchPhase.Interval||m.phase==MatchPhase.Final)target=new(side*(kind==VenueKind.Basketball?19:61),.08f,(slot-4)*1.6f);
            Activity=m.lastEvent;art.Face(venue.transform.TransformPoint(new Vector3(m.ballX,0,m.ballZ)),.6f);
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||!game.Ready||game.Paused||!body||!body.Alive||body.Downed||CivilianImpact.Active(this))return;
            var safety=venue.GetComponent<VenueSafety>();if(safety&&safety.React(this))return;
            if(!ActorWorkBudget.Tick(this,ref tick,ref lastTick,out var dt,athlete))return;
            if(npc.Fleeing||Time.time<npc.SocialUntil)return;
            if(!athlete&&Time.time>wait)ChooseActivity();
            var world=venue.transform.TransformPoint(target);var delta=world-transform.position;delta.y=0;
            bool arrived=delta.sqrMagnitude<.2f;
            art.Sitting=!athlete&&arrived&&(Activity=="경기 관람"||Activity=="영화 관람"||Activity=="휴식"||Activity=="시설 업무");
            if(!arrived)
            {
                float speed=athlete?venue.Definition.kind==VenueKind.Basketball?4.5f:5.8f:1.1f+serial%5*.13f;var dir=delta.normalized;
                if(!steering)steering=PedestrianSteering.For(this);steering.Move(world,speed*dt);
            }
            else if(spectator)art.Face(venue.transform.TransformPoint(venue.lookPoint),.6f);
            if(!athlete&&Time.time>chat&&Vector3.Distance(game.Player.transform.position,transform.position)<24)
            {chat=Time.time+24+serial%19;NpcSpeech.Say(npc,staff?StaffLine():VisitorLine(),4);}
        }
        void ChooseActivity()
        {
            state++;wait=Time.time+12+serial%13;
            if(venue.Definition.kind==VenueKind.Prison&&!staff){Activity="수용동 생활";target=origin+new Vector3(0,0,state%2==0?.4f:0);return;}
            if(staff)
            {
                Activity=state%4==0?"시설 점검":"시설 업무";
                target=origin+(state%4==0?new Vector3(1.6f,0,-2):Vector3.zero);
                if(state%4==1&&venue.ActiveVisitors>0)Activity="방문객 응대";
            }
            else if(spectator&&state%5!=0){Activity=venue.Definition.kind==VenueKind.Cinema?"영화 관람":"경기 관람";target=origin;}
            else
            {
                var choices=venue.activityPoints;Activity=state%3==0?"휴식":venue.Definition.kind==VenueKind.Museum?"전시 관람":venue.Definition.city==3?"심해 산책":"산책";
                if(choices.Count>0){var candidate=choices[(serial+state*3)%choices.Count];if(Mathf.Abs(candidate.y-origin.y)<1&&CrowdFlow.Place(venue.transform.TransformPoint(candidate),serial,out var safe,7))target=venue.transform.InverseTransformPoint(safe);}
            }
        }
        string StaffLine()=>venue.Definition.Sport?new[]{"입장 안내 도와드릴게요. 전광판에서 경기 상황을 보실 수 있어요.","승부예측은 경기 시작 전에만 접수합니다.","선수 통로를 비워 주세요. 곧 경기가 시작됩니다.","휴게 공간과 매점은 중앙 통로에 있습니다."}[state%4]:venue.Definition.city==3?new[]{"돔 내부 압력은 정상입니다. 편하게 숨 쉬셔도 돼요.","심해 도로의 안내선을 따라가면 지상 도시와 이어집니다.","시설 이용 방법이 궁금하시면 말씀해 주세요.","순환 공기와 수질을 점검하고 있습니다."}[state%4]:new[]{"어서 오세요. 시설 이용을 안내해 드릴게요.","불편한 점이 있으시면 가까운 직원에게 알려 주세요.","승강기와 계단은 건물 양쪽에 있습니다.","편안한 관람 되세요."}[state%4];
        string VisitorLine()=>venue.Definition.Sport?new[]{"이번 경기는 누가 이길까?","패스 좋다!","끝날 때까지 응원하자.","경기 끝나면 매점에 들르자."}[state%4]:venue.Definition.city==3?new[]{"유리 너머로 물고기가 지나가네.","수중 정원에 가면 잠시 바다라는 것도 잊게 돼.","오늘은 지상에 있는 친구가 놀러 오기로 했어.","돔 불빛이 산호처럼 아름답지?"}[state%4]:new[]{"이 도시에도 이렇게 쉬어 갈 곳이 생겼네.","전시를 보고 나서 커피 한 잔 할까?","사진 한 장만 찍고 가자.","다음에는 친구들도 같이 오면 좋겠다."}[state%4];
        void LateUpdate()
        {
            if(!athlete||!body||!body.Alive||!sprite)return;
            if(Time.time<actionUntil){var d=PeopleArt.Direction(venue.transform.TransformPoint(target)-transform.position);var frame=PeopleArt.Get(art.art,d,3);if(frame)sprite.sprite=frame;}
            if(team==1)sprite.color=new Color(.7f,.83f,1);else sprite.color=Color.white;
        }
    }
}
