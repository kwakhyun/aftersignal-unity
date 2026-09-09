using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class VenueSafety:MonoBehaviour
    {
        static readonly List<VenueSafety> all=new();public VenueRuntime Venue;public bool Emergency=>Time.time<resume;public bool Intrusion{get;private set;}public bool Suspended=>Emergency||Intrusion;
        public Vector3 Danger;float resume,next,warning,intruded;readonly HashSet<int> spoken=new();
        void Awake(){Venue=GetComponent<VenueRuntime>();all.Add(this);}
        public static bool IsSuspended(string id){foreach(var v in all)if(v&&v.Venue.Definition.id==id)return v.Suspended;return false;}
        public static void Report(WorldActor victim,WorldActor source)
        {
            if(!victim)return;foreach(var v in all)if(v&&v.Venue.Definition.Sport&&new Bounds(v.Venue.transform.position,new Vector3(v.Venue.Definition.size.x+45,45,v.Venue.Definition.size.y+45)).Contains(victim.transform.position)){v.Danger=victim.transform.position;if(!v.Emergency)v.spoken.Clear();v.resume=Time.time+45;}
        }
        public bool React(VenueActor actor)
        {
            if(!Suspended)return false;var body=actor.GetComponent<WorldActor>();if(!body||!body.Alive||body.Downed)return true;
            if(spoken.Add(actor.GetInstanceID()))
            {
                string[] lines=Emergency?(actor.athlete?new[]{"경기 중단! 관중석 쪽으로 가지 마!","팀원 확인해! 다친 사람은 의료진을 불러!","선수 통로로 빠져! 서로 떨어지지 마!"}:actor.staff?new[]{"경기를 중단합니다! 비상구로 천천히 이동해 주세요!","구급대 요청했습니다. 부상자 주변을 비워 주세요!","밀지 마세요! 아이와 보호자부터 안내하겠습니다!"}:new[]{"사람이 다쳤어! 의료진 좀 불러 줘요!","뒤에서 밀지 마! 출구가 저쪽이야!","경기 보러 왔는데 이게 무슨 일이야!","우리 일행 못 봤어요? 같이 나가야 해요!"}):actor.athlete?new[]{"심판! 사람이 경기장 안에 들어왔어요!","경기 멈춰. 다칠 수 있어!"}:actor.staff?new[]{"경기 구역에서 나와 주세요. 관람석으로 안내하겠습니다."}:new[]{"누가 난입했어! 경기를 볼 수가 없잖아!","선수들 다치면 어떡해. 얼른 나와요!"};
                NpcSpeech.Say(actor,lines[Mathf.Abs(actor.GetInstanceID())%lines.Length],5,7);
            }
            if(Emergency){var exit=Venue.Definition.Entrance+new Vector3((actor.serial%13-6)*1.5f,0,-8-actor.serial/13*1.3f);actor.EmergencyMove(exit);}
            else if(actor.staff){var p=GameDirector.Instance.Player.transform.position;var d=actor.transform.position-p;actor.EmergencyMove(p+d.normalized*3);}
            return true;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Venue||!Venue.Definition.Sport||Time.time<next)return;next=Time.time+.35f;
            var p=Venue.transform.InverseTransformPoint(g.Player.transform.position);bool near=Mathf.Abs(p.y)<5;
            bool inside=Venue.Definition.kind switch{VenueKind.Football=>Mathf.Abs(p.x)<53&&Mathf.Abs(p.z)<35,VenueKind.Basketball=>Mathf.Abs(p.x)<15&&Mathf.Abs(p.z)<9,VenueKind.Baseball=>p.x*p.x+(p.z+10)*(p.z+10)<65*65,_=>OnRaceTrack(p)};
            var match=FourCitySports.Instance?.Get(Venue.Definition.id);bool invaded=near&&inside&&match!=null&&match.phase==MatchPhase.Playing;
            if(invaded&&!Intrusion){intruded=Time.time;spoken.Clear();}if(!invaded&&Intrusion)spoken.Clear();Intrusion=invaded;
            if(Intrusion&&Time.time>warning){warning=Time.time+12;g.Toast("경기 일시 중단 · 선수 구역에서 나와 주세요",4);if(Time.time-intruded>10)WantedSystem.Report(3,g.Player.transform.position);}
            if(Emergency)foreach(var a in WorldActor.All)if(a&&a.Alive&&!a.Downed&&(a.terrorist||a.monster||a.gang)&&(a.transform.position-transform.position).sqrMagnitude<180*180){resume=Mathf.Max(resume,Time.time+20);break;}
        }
        bool OnRaceTrack(Vector3 p){for(int i=0;i<80;i++)if((FourCityArchitecture.Track(i/80f)-p).sqrMagnitude<9*9)return true;return false;}
        void OnDestroy(){all.Remove(this);}
    }
}
