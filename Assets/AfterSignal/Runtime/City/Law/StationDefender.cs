using UnityEngine;
namespace AfterSignal
{
    public sealed class StationDefender:MonoBehaviour
    {
        WorldActor body;CityNpc npc;float fire,restraint;bool active;
        public void Engage(){active=true;body=GetComponent<WorldActor>();npc=GetComponent<CityNpc>();if(npc)NpcSpeech.Say(npc,"경찰서 내 공격 발생! 용의자를 제압해!",3);}
        void Update()
        {
            var g=GameDirector.Instance;if(!active||!g||g.Blocked)return;
            if(!body||!body.Alive){active=false;return;}
            if(WantedSystem.Level==0){active=false;if(npc)npc.enabled=true;return;}
            if(npc)npc.enabled=false;
            var direction=g.Player.Shoulder-body.Center;fire-=Time.deltaTime;
            if(!FactionCombat.Visible(body.Center,g.Player.Shoulder,70))return;
            var look=GetComponent<DirectionalPerson>();if(look){look.Look=direction;look.LookUntil=Time.time+.2f;}
            if(direction.magnitude<2.2f){restraint+=Time.deltaTime;if(restraint>2){PrisonSystem.Capture(g);restraint=0;}return;}restraint=0;
            if(fire>0)return;fire=1.5f;FactionCombat.Fire(body,body.Center+direction.normalized*.6f,g.Player.Shoulder,70,8,SignalEffects.Gold,true);g.Audio.PlayGun(GunshotKind.PolicePistol,body.Center,.7f);
        }
    }
}
