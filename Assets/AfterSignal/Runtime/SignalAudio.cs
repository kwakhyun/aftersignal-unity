using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public enum GunshotKind { Pistol, PolicePistol, GangPistol, Shotgun, Rifle, Automatic }
    public sealed class SignalAudio : MonoBehaviour
    {
        const int VoiceCount=28;
        AudioSource ambience;
        readonly AudioSource[] voices=new AudioSource[VoiceCount];
        readonly int[] importance=new int[VoiceCount];
        readonly float[] voiceGain=new float[VoiceCount];
        readonly Dictionary<string,AudioClip> bank=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,int> variations=new Dictionary<string,int>();
        readonly Dictionary<string,float> lastPlayed=new Dictionary<string,float>();
        readonly Dictionary<string,int> cueCounts=new Dictionary<string,int>();
        readonly HashSet<string> missing=new HashSet<string>();
        bool paused;float duck;
        public float Volume {get;private set;}=.45f;
        public float SfxVolume {get;private set;}=.9f;
        public float MusicDuck => duck>0?.76f:1;
        public int Played {get;private set;}
        public int MissingCues=>missing.Count;
        public int ActiveVoices {get{int n=0;foreach(var v in voices)if(v&&v.isPlaying)n++;return n;}}
        public int Count(string cue)=>cueCounts.TryGetValue(cue,out var count)?count:0;
        public string LastClip {get;private set;}
        public float LastGain {get;private set;}
        public void Initialize()
        {
            SignalMusic.Ensure();
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/Quality"))Register(clip);
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/Firearms"))Register(clip);
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/Transport"))Register(clip);
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/CombatDetail"))Register(clip);
            SfxVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.SfxVolume",.9f));
            for(int i=0;i<voices.Length;i++)
            {
                var v=voices[i]=gameObject.AddComponent<AudioSource>();
                v.playOnAwake=false;v.spatialBlend=0;v.dopplerLevel=0;v.priority=150;
            }
            ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.playOnAwake=false;ambience.spatialBlend=0;
            var g=GameDirector.Instance;
            string scene=g.stage==StageId.Station?"station":g.stage==StageId.Carriage?"train":g.stage==StageId.Roof?"roof":"town";
            bank.TryGetValue("ambient_"+scene,out var ambient);ambience.clip=ambient;
            SetVolume(PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.Volume",.45f));
            if(ambient)ambience.Play();
        }
        void Register(AudioClip clip){bank[clip.name]=clip;if(clip.loadState==AudioDataLoadState.Unloaded)clip.LoadAudioData();}
        static string ResolveCue(string cue)
        {
            // Keep old call sites compatible, including enhanced pistol shots.
            if(cue=="pistol"||cue=="pistol_overdrive")return "gun_pistol";
            return cue;
        }
        public bool HasCue(string cue){cue=ResolveCue(cue);return bank.ContainsKey(cue+"_0")||bank.ContainsKey(cue);}
        AudioClip Next(string cue)
        {
            int next=variations.TryGetValue(cue,out var value)?value:0;variations[cue]=next+1;
            for(int i=0;i<3;i++)if(bank.TryGetValue(cue+"_"+((next+i)%3),out var clip))return clip;
            bank.TryGetValue(cue,out var single);return single;
        }
        public void SetVolume(float value)
        {
            Volume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.Volume",Volume);ApplyVolumes();
            if(SignalMusic.Instance)SignalMusic.Instance.SetMasterVolume(Volume);
        }
        public void SetSfxVolume(float value){SfxVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.SfxVolume",SfxVolume);ApplyVolumes();}
        void ApplyVolumes()
        {
            for(int i=0;i<voices.Length;i++)if(voices[i])voices[i].volume=Volume*SfxVolume*voiceGain[i];
            if(ambience)ambience.volume=Volume*SfxVolume*.07f;
        }
        public void SetPaused(bool value)
        {
            if(paused==value)return;paused=value;
            if(SignalMusic.Instance)SignalMusic.Instance.SetPaused(value);
            foreach(var v in voices)if(v){if(value)v.Pause();else v.UnPause();}
            if(ambience){if(value)ambience.Pause();else ambience.UnPause();}
        }
        static float MixGain(string cue,float gain)
        {
            // Legacy call gains were tuned for the old quiet background, before full BGM.
            float reference=cue.StartsWith("step_")?.12f:cue=="jump"?.18f:cue=="rope_release"?.13f:.3f;
            float weight=cue.StartsWith("step_")?.17f:cue=="jump"?.8f:cue.StartsWith("ui_")?.42f:.85f;
            if(cue.StartsWith("gun_")){reference=.38f;weight=cue=="gun_shotgun"?.98f:.88f;}
            return Mathf.Clamp(gain/reference*weight,0,1.2f);
        }
        public void PlayGun(GunshotKind kind,Vector3 position,float strength=1)
        {
            string cue=kind==GunshotKind.PolicePistol?"gun_police_pistol":kind==GunshotKind.GangPistol?"gun_gang_pistol":kind==GunshotKind.Shotgun?"gun_shotgun":kind==GunshotKind.Rifle?"gun_rifle":kind==GunshotKind.Automatic?"gun_auto":"gun_pistol";
            CombatVfx.Muzzle(position,kind);Play(cue,position,.38f*strength,3);
            Play(kind==GunshotKind.Shotgun?"tail_shotgun":kind==GunshotKind.Rifle||kind==GunshotKind.Automatic?"tail_rifle":"tail_pistol",position,.105f*strength,2);
        }
        public void Play(string cue,Vector3 position,float gain=.45f,int priority=2)
        {
            if(paused||Volume<=0||SfxVolume<=0||gain<=0)return;
            string requested=cue;cue=ResolveCue(cue);
            float now=Time.unscaledTime;
            // Nearby NPCs keep separate impulses; steps cannot monopolize the voice pool.
            string gate=cue.StartsWith("gun_")?cue+":"+Mathf.RoundToInt(position.x*.5f)+":"+Mathf.RoundToInt(position.z*.5f):cue;
            if(lastPlayed.TryGetValue(gate,out float last)&&now-last<(cue.StartsWith("step_")?.055f:.018f))return;
            var clip=Next(cue);
            if(!clip){if(missing.Add(cue))Debug.LogWarning("Missing SFX cue: "+cue);return;}
            if(clip.loadState!=AudioDataLoadState.Loaded){clip.LoadAudioData();if(clip.loadState!=AudioDataLoadState.Loaded)return;}
            var game=GameDirector.Instance;
            var listener=game&&game.Player?game.Player.Shoulder:Camera.main?Camera.main.transform.position:position;
            var delta=position-listener;float distance=delta.magnitude;
            float attenuation=distance<4?1:1/(1+Mathf.Pow((distance-4)/19f,1.45f));
            bool battle=cue.StartsWith("gun_")||cue.StartsWith("tail_")||cue=="blast_pressure"||cue=="urban_explosion"||cue=="cannon";
            if(distance>(battle?650:135))return;
            if(battle&&distance>12){attenuation=1/(1+Mathf.Pow((distance-4)/38f,1.35f));if(Physics.Linecast(listener,position,1,QueryTriggerInteraction.Ignore))attenuation*=.48f;}
            int rank=priority+(distance<3?4:0);
            int slot=-1;
            for(int i=0;i<voices.Length;i++)if(!voices[i].isPlaying){slot=i;break;}
            if(slot<0)
            {
                float quietest=float.MaxValue;
                for(int i=0;i<voices.Length;i++)if(importance[i]<=rank&&voices[i].volume<quietest){slot=i;quietest=voices[i].volume;}
            }
            if(slot<0)return;
            lastPlayed[gate]=now;
            if(lastPlayed.Count>350)lastPlayed.Clear();
            var voice=voices[slot];voice.Stop();voice.clip=clip;
            voiceGain[slot]=MixGain(cue,gain)*attenuation;
            voice.volume=Volume*SfxVolume*voiceGain[slot];
            voice.pitch=cue.StartsWith("gun_")?1:1+((variations[cue]%3)-1)*.022f;
            var right=Camera.main?Camera.main.transform.right:Vector3.right;
            voice.panStereo=distance<2?0:Mathf.Clamp(Vector3.Dot(delta.normalized,right)*Mathf.Min(1,distance/7),-.82f,.82f);
            voice.priority=distance<3?24:priority>=3?40:priority>=1?80:130;importance[slot]=rank;voice.Play();
            Played++;cueCounts[requested]=Count(requested)+1;LastClip=clip.name;LastGain=voice.volume;
            if(priority>=3)duck=.26f;
        }
        public float OutputPeak()
        {
            var data=new float[512];float peak=0;
            foreach(var v in voices)if(v&&v.isPlaying){v.GetOutputData(data,0);foreach(float n in data)peak=Mathf.Max(peak,Mathf.Abs(n));}
            return peak;
        }
        public void PlayCue(float frequency,float duration,float level){Play(frequency>1000?"glass":frequency<110?"hurt":frequency>850?"guard":frequency>650?"rope_attach":"ui_confirm",transform.position,Mathf.Clamp(level*3,.12f,.6f));}
        void Update(){duck=Mathf.Max(0,duck-Time.unscaledDeltaTime);if(ambience)ambience.volume=Volume*SfxVolume*(duck>0?.035f:.07f);}
    }
}
