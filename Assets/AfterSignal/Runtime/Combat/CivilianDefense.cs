using UnityEngine;
namespace AfterSignal
{
    public sealed class CivilianDefense:MonoBehaviour
    {
        WorldActor body,aggressor;CityNpc npc;float until,nextAttack;
        public static bool Active(Component c){var d=c.GetComponent<CivilianDefense>();return d&&d.body&&d.body.Alive&&Time.time<d.until;}
        public static bool Eligible(WorldActor body)
        {
            var n=body.GetComponent<CityNpc>();if(!n||n.fixedQuest||body.protectedResident||body.police||body.gang)return false;
            string role=NpcVoice.Role(n);
            if(role.Contains("Student")||role.Contains("Patient")||role.Contains("Elder")||role=="Nurse"||role=="Doctor")return false;
            return role=="FacilityNightMarket3"||role=="Worker"||n.variation%7==3;
        }
        public static void React(WorldActor target,WorldActor source)
        {
            if(!Eligible(target)||source==target||StreetDispute.Contains(target))return;
            var d=target.GetComponent<CivilianDefense>()??target.gameObject.AddComponent<CivilianDefense>();
            d.body=target;d.npc=target.GetComponent<CityNpc>();d.aggressor=source;d.until=Time.time+20;
            d.npc.SocialUntil=0;
            NpcSpeech.Say(target,NpcDialogueBank.Line(d.npc,"retaliate"),4,12);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!Active(this)||CivilianImpact.Active(this))return;
            if(aggressor&&!aggressor.Alive){until=0;return;}
            Vector3 target=aggressor?aggressor.Center:g.Player.Shoulder;
            Vector3 delta=target-(transform.position+Vector3.up);delta.y=0;
            if(delta.magnitude>30){until=0;return;}
            GetComponent<DirectionalPerson>()?.Face(target,1);
            if(delta.magnitude>1.8f)
            {
                var step=delta.normalized*Time.deltaTime*3.6f;
                if(!Physics.Raycast(transform.position+Vector3.up,step.normalized,.65f,1,QueryTriggerInteraction.Ignore)&&Physics.Raycast(transform.position+step+Vector3.up,Vector3.down,2,1,QueryTriggerInteraction.Ignore))transform.position+=step;
            }
            else if(Time.time>=nextAttack&&!Physics.Linecast(transform.position+Vector3.up,target,1,QueryTriggerInteraction.Ignore))
            {
                nextAttack=Time.time+1.25f;
                SignalEffects.Slash(transform.position+Vector3.up,delta.normalized,1.2f,SignalEffects.Gold,0);
                if(aggressor)aggressor.Damage(10,delta.normalized*2,body);else g.Player.ReceiveDamage(8,transform.position);
                NpcSpeech.Say(this,NpcDialogueBank.Line(npc,"retaliate"),2,12);
            }
        }
    }
}
