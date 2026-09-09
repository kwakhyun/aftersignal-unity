using UnityEngine;
namespace AfterSignal
{
    public sealed class RegionalGuard:MonoBehaviour
    {
        public VenueRuntime venue;public bool Alerted;WorldActor body;float shot;VenueActor activity;
        void Start(){body=GetComponent<WorldActor>();activity=GetComponent<VenueActor>();}
        public static void Alert(Vector3 p){foreach(var guard in FindObjectsByType<RegionalGuard>())if(guard.venue.Definition.kind==VenueKind.Police&&(p-guard.venue.transform.position).sqrMagnitude<guard.venue.Definition.size.sqrMagnitude*.4f){guard.Alerted=true;NpcSpeech.Say(guard,"경찰서 내 범죄 발생! 전 직원 대응!",3);}}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!body||!body.Alive)return;
            var opponent=FactionCombat.NearestOpponent(body,65);bool player=!IncidentCommand.Emergency&&Alerted&&WantedSystem.Level>0;
            if(activity)activity.enabled=!opponent&&!player;
            if(!opponent&&!player)return;Vector3 aim=opponent?opponent.Center:g.Player.Shoulder;
            GetComponent<DirectionalPerson>()?.Face(aim,.6f);
            if((aim-body.Center).sqrMagnitude>25*25)PedestrianSteering.For(this).Move(aim,2.8f*Time.deltaTime);
            if(Time.time<shot||!FactionCombat.Visible(body.Center,aim,65))return;shot=Time.time+(body.military?.33f:.8f);
            FactionCombat.Fire(body,body.Center+Vector3.up*.15f,aim,65,body.military?17:10,SignalEffects.Gold,player);g.Audio.PlayGun(body.military?GunshotKind.Rifle:GunshotKind.Pistol,body.Center,.75f);
        }
    }
}
