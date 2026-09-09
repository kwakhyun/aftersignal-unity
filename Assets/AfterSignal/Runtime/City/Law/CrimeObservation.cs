using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CrimeObservation:MonoBehaviour
    {
        sealed class Call {public WorldActor witness;public Vector3 point;public float severity,remaining;public int fatalities;}
        public static CrimeObservation Instance{get;private set;}
        readonly List<Call> calls=new();
        public int ConfirmedFatalities{get;private set;}
        public int PendingCalls=>calls.Count;
        public const int MilitaryFatalityThreshold=30;
        float violenceUntil;
        void Awake()=>Instance=this;
        public static void Forget(){if(Instance){Instance.calls.Clear();Instance.ConfirmedFatalities=0;Instance.violenceUntil=0;}}
        public static bool Witnesses(WorldActor witness,Vector3 point,float range)
        {
            if(!witness||!witness.Alive||!witness.gameObject.activeInHierarchy||witness.gang||witness.monster)return false;
            if((witness.Center-(point+Vector3.up)).sqrMagnitude>range*range)return false;
            foreach(var hit in Physics.RaycastAll(witness.Center,(point+Vector3.up-witness.Center).normalized,Vector3.Distance(witness.Center,point+Vector3.up),1,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(witness.transform)&&hit.distance>.15f&&hit.distance<Vector3.Distance(witness.Center,point+Vector3.up)-1.5f)return false;
            return true;
        }
        public static void Observe(float severity,Vector3 where,WorldActor victim=null,bool fatal=false)
        {
            if(!Instance)return;
            var g=GameDirector.Instance;if(!g||g.Player.Health<=0||g.Dead)return;
            fatal=fatal&&victim&&!victim.police&&!victim.military&&!victim.gang&&!victim.monster;
            FacilitySecurity.Alert(where);
            WorldActor civilian=null;
            foreach(var witness in WorldActor.All)
            {
                if(!Witnesses(witness,where,witness.police||witness.military?55:32))continue;
                if(witness.police||witness.military){Instance.Confirm(severity,where,fatal?1:0);return;}
                if(!civilian||witness==victim)civilian=witness;
            }
            if(civilian)Queue(civilian,severity,where,fatal?1:0);
        }
        public static void Queue(WorldActor witness,float severity,Vector3 point,int fatalities=0)
        {
            if(!Instance||!witness||!witness.Alive)return;
            var existing=Instance.calls.Find(c=>c.witness==witness);
            if(existing!=null){existing.severity=Mathf.Min(170,existing.severity+severity);existing.fatalities+=fatalities;return;}
            Instance.calls.Add(new Call{witness=witness,point=point,severity=severity,remaining=5,fatalities=fatalities});
            NpcSpeech.Say(witness,NpcDialogueBank.Line(witness.GetComponent<CityNpc>(),"report"),4);
            GameDirector.Instance?.ToastNear("목격자가 신고하고 있습니다",point,100,2);
        }
        void Confirm(float severity,Vector3 point,int fatalities)
        {
            if(Time.time>violenceUntil)ConfirmedFatalities=0;
            ConfirmedFatalities+=fatalities;violenceUntil=Time.time+180;
            WantedSystem.ConfirmReport(severity,point);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            for(int i=calls.Count-1;i>=0;i--)
            {
                var call=calls[i];
                if(!call.witness||!call.witness.Alive||g.Player.Health<=0){calls.RemoveAt(i);continue;}
                call.remaining-=Time.deltaTime;if(call.remaining>0)continue;
                Confirm(call.severity,call.point,call.fatalities);calls.RemoveAt(i);
            }
            if(Time.time<violenceUntil&&ConfirmedFatalities>=MilitaryFatalityThreshold&&WantedSystem.Level>=5&&g.stage==StageId.UrbanCity&&!g.GetComponent<MilitaryResponse>())g.gameObject.AddComponent<MilitaryResponse>();
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
