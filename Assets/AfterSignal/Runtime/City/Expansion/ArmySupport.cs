using UnityEngine;
namespace AfterSignal
{
    public sealed class ArmySupport:MonoBehaviour
    {
        RiftIncursion incident;CityVehicle vehicle;WorldActor source;float phase,cooldown;
        public void Initialize(RiftIncursion incident,CityVehicle vehicle,WorldActor source){this.incident=incident;this.vehicle=vehicle;this.source=source;vehicle.occupied=true;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!vehicle||vehicle.Wrecked||UrbanSimulation.Instance.Current==vehicle)return;
            phase+=Time.deltaTime*.12f;cooldown-=Time.deltaTime;
            var center=incident?incident.Position:transform.position;var target=center+new Vector3(Mathf.Cos(phase)*42,32,Mathf.Sin(phase)*42);
            transform.position=Vector3.MoveTowards(transform.position,target,Time.deltaTime*25);transform.rotation=Quaternion.Euler(0,-phase*Mathf.Rad2Deg-90,0);
            if(!source||!source.Alive||!incident||!incident.Active)return;
            WorldActor victim=null;foreach(var a in WorldActor.All)if(a&&a.monster&&a.Alive&&(a.Center-transform.position).sqrMagnitude<150*150){victim=a;break;}
            if(victim&&cooldown<=0&&BlastDamage.Exposed(transform.position,victim.Center,victim.transform,vehicle))
            {cooldown=.23f;incident.MilitaryShots++;FactionCombat.Fire(source,transform.position+Vector3.down*1.5f,victim.Center,160,12,SignalEffects.Gold,false,vehicle.transform);g.Audio.PlayGun(GunshotKind.Automatic,transform.position,.7f);}
        }
    }
}
