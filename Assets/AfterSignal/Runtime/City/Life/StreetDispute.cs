using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Local, infrequent arguments between healthy adults; they stop before lethal injury.
    public sealed class StreetDispute:MonoBehaviour
    {
        static StreetDispute active;static float nextAllowed;CityNpc a,b;WorldActor first,second;float began,nextPunch;int turn;bool stopped;
        public int Punches{get;private set;}public bool Fighting=>!stopped&&Time.time>began+3;
        public static bool Contains(Component actor)=>active&&!active.stopped&&actor&&(active.a&&actor.gameObject==active.a.gameObject||active.b&&actor.gameObject==active.b.gameObject);
        public static bool Eligible(CityNpc npc)
        {
            if(!npc||npc.fixedQuest||npc.Fleeing||npc.GetComponent<FamilyMember>()||npc.GetComponent<MedicalState>()?.NeedsRescue==true)return false;
            var w=npc.GetComponent<WorldActor>();if(!w||!w.Alive||w.Downed||w.health<55||w.police||w.military||w.gang||w.terrorist||w.monster||w.robot||w.protectedResident)return false;
            var group=NpcPersona.Group(npc);return group!="child"&&group!="student"&&group!="elder"&&group!="medical"&&group!="patient"&&!MilitaryBaseOperations.Restricted(npc.transform.position);
        }
        public static bool TryBegin(CityNpc a,CityNpc b)
        {if(active||Time.time<nextAllowed||IncidentCommand.Emergency||WantedSystem.Level>0||Random.value>.16f)return false;return Begin(a,b);}
        public static bool Begin(CityNpc a,CityNpc b)
        {
            if(active||a==b||!Eligible(a)||!Eligible(b)||Vector3.Distance(a.transform.position,b.transform.position)>7)return false;
            active=new GameObject("시민 말다툼").AddComponent<StreetDispute>();active.a=a;active.b=b;active.first=a.GetComponent<WorldActor>();active.second=b.GetComponent<WorldActor>();active.began=Time.time;nextAllowed=Time.time+Random.Range(100,180);a.SocialUntil=b.SocialUntil=Time.time+24;
            NpcSpeech.Say(a,NpcDialogueBank.Line(a,"argument"),3,7);NpcSpeech.Say(b,NpcDialogueBank.Line(b,"argument_reply"),3,7);return true;
        }
        readonly List<WorldActor> nearby=new();float scan;
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            if(a)transform.position=a.transform.position;
            if(!a||!b||!first.Alive||!second.Alive||first.Downed||second.Downed||first.health<36||second.health<36||Time.time>began+16||IncidentCommand.Emergency||!LocalSimulation.Within(transform.position,70)){Finish();return;}
            if(Time.time>scan){scan=Time.time+.6f;ActorSpatialIndex.Nearby(a.transform.position,12,nearby);foreach(var w in nearby)if(w&&w.Alive&&(w.police||w.military)){NpcSpeech.Say(w,"거기 두 사람! 싸움 멈추고 떨어지세요!",4,5);Finish();return;}}
            a.SocialUntil=b.SocialUntil=Time.time+2;a.GetComponent<DirectionalPerson>()?.Face(b.transform.position,1);b.GetComponent<DirectionalPerson>()?.Face(a.transform.position,1);
            if(Time.time<began+3)return;
            var delta=b.transform.position-a.transform.position;delta.y=0;
            if(delta.magnitude>1.7f){PedestrianSteering.For(a).Move(b.transform.position,Time.deltaTime*1.7f);PedestrianSteering.For(b).Move(a.transform.position,Time.deltaTime*1.7f);return;}
            if(Time.time<nextPunch)return;nextPunch=Time.time+1.1f;var attacker=turn++%2==0?first:second;var victim=attacker==first?second:first;var d=(victim.Center-attacker.Center).normalized;
            Punches++;SignalEffects.Slash(attacker.Center,d,.8f,SignalEffects.Gold,0);victim.Damage(6,d,attacker);if(Punches%3==1)NpcSpeech.Say(attacker,NpcDialogueBank.Line(attacker.GetComponent<CityNpc>(),"scuffle"),2,8);
        }
        void Finish(){if(stopped)return;stopped=true;if(a&&first.Alive){a.SocialUntil=Time.time+12;NpcSpeech.Say(a,NpcDialogueBank.Line(a,"scuffle_end"),4,9);}if(b&&second.Alive){b.SocialUntil=Time.time+12;b.Panic(a?a.transform.position:b.transform.position,4);}Destroy(gameObject);}
        void OnDestroy(){if(active==this)active=null;}
    }
}
