using System.Collections;
using System.Linq;
using UnityEngine;
namespace AfterSignal
{
    public static class NpcVoice
    {
        public static string Role(CityNpc n)=>n.GetComponent<DirectionalPerson>()?.art??"CivilianMan";
        public static string Hurt(CityNpc n,bool down)=>NpcDialogueBank.Line(n,down?"down":"hurt");
        public static void React(CityNpc n,bool down){if(!n)return;n.SocialUntil=0;NpcSpeech.Say(n,Hurt(n,down),5,10);}
        public static string Greeting(CityNpc n)=>NpcDialogueBank.Line(n,"greeting");
    }
    public sealed class CitySocial:MonoBehaviour
    {
        public static CitySocial Instance{get;private set;}
        public int Exchanges{get;private set;}public int Greetings{get;private set;}
        float next=4,hailAt=10;GameDirector game;
        readonly System.Collections.Generic.List<WorldActor> nearby=new();
        readonly System.Collections.Generic.List<CityNpc> people=new();
        void Awake(){Instance=this;game=GetComponent<GameDirector>();}
        static bool Available(CityNpc n)
        {
            if(!n||!n.gameObject.activeInHierarchy||n.fixedQuest||n.Fleeing||n.GetComponent<MedicalPending>()||n.GetComponent<RescueMedic>())return false;
            var body=n.GetComponent<WorldActor>();var visual=n.GetComponent<SpriteRenderer>();
            if(!body||!body.Alive||body.Downed||body.monster||body.robot||!visual||!visual.enabled)return false;
            if((body.police||body.military||body.gang)&&(IncidentCommand.Emergency||WantedSystem.Level>0||FactionCombat.NearestOpponent(body,65)))return false;
            return true;
        }
        bool Eligible(CityNpc n)=>Available(n)&&Time.time>=n.SocialUntil;
        void Update()
        {
            if(!game||!game.Ready||game.Blocked||game.stage!=StageId.UrbanCity||Time.time<next)return;
            next=Time.time+Random.Range(4,7);
            ActorSpatialIndex.Nearby(game.Player.transform.position,48,nearby);people.Clear();foreach(var candidate in nearby){var n=candidate.GetComponent<CityNpc>();if(Eligible(n))people.Add(n);}
            if(people.Count==0)return;
            if(Time.time>hailAt&&WantedSystem.Level==0&&!(UrbanSimulation.Instance&&UrbanSimulation.Instance.Driving))
            {
                var n=people.Where(p=>Vector3.Distance(p.transform.position,game.Player.transform.position)<10).OrderBy(p=>Vector3.Distance(p.transform.position,game.Player.transform.position)).FirstOrDefault();
                if(n&&Vector3.Distance(n.transform.position,game.Player.transform.position)>2.5f&&!Physics.Linecast(n.transform.position+Vector3.up,game.Player.Shoulder,1,QueryTriggerInteraction.Ignore))
                {Hail(n);hailAt=Time.time+Random.Range(40,65);return;}
            }
            var a=people[Random.Range(0,people.Count)];
            var b=people.FirstOrDefault(n=>n!=a&&Vector3.Distance(n.transform.position,a.transform.position)<7&&Mathf.Abs(n.transform.position.y-a.transform.position.y)<1.2f&&!Physics.Linecast(n.transform.position+Vector3.up,a.transform.position+Vector3.up,1,QueryTriggerInteraction.Ignore));
            if(b)Exchange(a,b);
        }
        public void Hail(CityNpc npc)
        {
            if(!Eligible(npc))return;Greetings++;npc.SocialUntil=Time.time+8;
            npc.GetComponent<DirectionalPerson>()?.Face(game.Player.transform.position,8);
            string line=NpcVoice.Greeting(npc);NpcSpeech.Say(npc,line,7,1);
            npc.context+=" 방금 서하에게 먼저 말을 걸었다: "+line;
            game.Toast(npc.displayName+" 님이 말을 걸었습니다 · 가까이서 E",5);
        }
        public void Exchange(CityNpc a,CityNpc b)
        {
            if(!Eligible(a)||!Eligible(b))return;Exchanges++;a.SocialUntil=b.SocialUntil=Time.time+8;
            a.GetComponent<DirectionalPerson>()?.Face(b.transform.position,8);b.GetComponent<DirectionalPerson>()?.Face(a.transform.position,8);
            var pair=NpcDialogueBank.Exchange(a);
            NpcSpeech.Say(a,pair[0],4,1);if(pair.Length>1)StartCoroutine(Reply(a,b,pair[1]));
        }
        IEnumerator Reply(CityNpc a,CityNpc b,string text)
        {
            yield return new WaitForSeconds(2.2f);
            if(Available(a)&&Available(b))NpcSpeech.Say(b,text,4,1);
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
