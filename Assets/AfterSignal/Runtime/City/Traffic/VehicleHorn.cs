using UnityEngine;
namespace AfterSignal
{
    public sealed class VehicleHorn:MonoBehaviour
    {
        CityVehicle car;AudioSource sound;float cooldown,blocked,reply;static AudioClip carClip,heavyClip;
        public int Honks{get;private set;}
        void Awake(){car=GetComponent<CityVehicle>();sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=1;sound.minDistance=4;sound.maxDistance=65;sound.rolloffMode=AudioRolloffMode.Linear;}
        static AudioClip Clip(bool heavy)
        {
            if(heavy&&heavyClip)return heavyClip;if(!heavy&&carClip)return carClip;
            const int rate=22050;var data=new float[rate/2];float f=heavy?190:380;
            for(int i=0;i<data.Length;i++){float t=(float)i/rate;float envelope=Mathf.Min(1,t*35)*Mathf.Clamp01((.5f-t)*14);data[i]=(Mathf.Sin(t*f*Mathf.PI*2)+.55f*Mathf.Sin(t*f*1.26f*Mathf.PI*2)+.2f*Mathf.Sin(t*f*2*Mathf.PI*2))*.23f*envelope;}
            var clip=AudioClip.Create(heavy?"Dual-tone truck horn":"Dual-tone road horn",data.Length,1,rate,false);clip.SetData(data,0);if(heavy)heavyClip=clip;else carClip=clip;return clip;
        }
        public bool Honk(bool reaction=true)
        {
            var g=GameDirector.Instance;if(!car||car.Wrecked||cooldown>0||!g||g.Blocked)return false;
            cooldown=2.2f;Honks++;sound.PlayOneShot(Clip(car.IsHeavy||car.IsWatercraft),.38f*g.Audio.Volume*g.Audio.SfxVolume);
            if(reaction)
            {
                foreach(var npc in FindObjectsByType<CityNpc>())
                    if(npc&&(npc.transform.position-transform.position).sqrMagnitude<22*22&&npc.GetComponent<WorldActor>().Alive)
                    {var local=transform.InverseTransformPoint(npc.transform.position);if(Mathf.Abs(local.z)<car.HalfWidth+2&&local.x>0)npc.Panic(transform.position,1.6f);NpcSpeech.Say(npc,Random.value<.5f?"알았어요, 지나가세요!":"조심해서 운전해요!",2);}
                var sim=UrbanSimulation.Instance;if(sim)foreach(var other in sim.Cars)
                    if(other&&other!=car&&other.traffic&&(other.transform.position-transform.position).sqrMagnitude<30*30){var horn=other.GetComponent<VehicleHorn>();if(horn&&Random.value<.35f)horn.reply=Time.time+Random.Range(.8f,1.8f);}
            }
            return true;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!car||!g||g.Blocked)return;
            cooldown=Mathf.Max(0,cooldown-Time.deltaTime);
            if(reply>0&&Time.time>reply){reply=0;Honk(false);}
            if(car.traffic&&!car.WaitingAtSignal&&car.speed<1&&car.route!=null){blocked+=Time.deltaTime;if(blocked>7){blocked=0;Honk();}}else blocked=0;
        }
    }
}
