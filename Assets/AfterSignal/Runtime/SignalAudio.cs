using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class SignalAudio : MonoBehaviour
    {
        AudioSource ambience;readonly AudioSource[] voices=new AudioSource[14];readonly int[] importance=new int[14];readonly Dictionary<string,AudioClip> bank=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,float> lastPlayed=new Dictionary<string,float>();int variation;bool paused;float duck;
        public float Volume {get;private set;}=.45f;
        public int ActiveVoices {get{int n=0;foreach(var v in voices)if(v&&v.isPlaying)n++;return n;}}
        public int Played {get;private set;}
        public void Initialize()
        {
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/Quality"))bank[clip.name]=clip;
            for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
            ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.playOnAwake=false;ambience.spatialBlend=0;
            var g=GameDirector.Instance;string scene=g.stage==StageId.Station?"station":g.stage==StageId.Carriage?"train":g.stage==StageId.Roof?"roof":"town";
            bank.TryGetValue("ambient_"+scene,out var ambient);ambience.clip=ambient;SetVolume(PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.Volume",.45f));if(ambient)ambience.Play();
        }
        public void SetVolume(float value){Volume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.Volume",Volume);foreach(var v in voices)if(v)v.volume=Volume;if(ambience)ambience.volume=Volume*.085f;}
        public void SetPaused(bool value)
        {
            if(paused==value)return;paused=value;
            foreach(var v in voices)if(v){if(value)v.Pause();else v.UnPause();}
            if(ambience){if(value)ambience.Pause();else ambience.UnPause();}
        }
        public void Play(string cue,Vector3 position,float gain=.45f,int priority=2)
        {
            if(paused||Volume<=0)return;
            float now=Time.unscaledTime;if(lastPlayed.TryGetValue(cue,out float last)&&now-last<(cue.StartsWith("step")?.075f:.035f))return;
            if(!bank.TryGetValue(cue+"_"+(variation++%3),out var clip))return;
            int slot=-1;for(int i=0;i<voices.Length;i++)if(!voices[i].isPlaying){slot=i;break;}
            if(slot<0){for(int i=0;i<voices.Length;i++)if(importance[i]<priority){slot=i;break;}}if(slot<0)return;
            lastPlayed[cue]=now;var voice=voices[slot];voice.Stop();voice.clip=clip;voice.volume=Volume*Mathf.Clamp01(gain);voice.pitch=1;voice.panStereo=Camera.main?Mathf.Clamp((position.x-Camera.main.transform.position.x)/18,-.65f,.65f):0;
            voice.priority=128-priority*24;importance[slot]=priority;voice.Play();Played++;if(priority>=3)duck=.14f;
        }
        public void PlayCue(float frequency,float duration,float level){Play(frequency>1000?"glass":frequency<110?"hurt":frequency>850?"guard":frequency>650?"rope_attach":"ui_confirm",transform.position,Mathf.Clamp(level*3,.12f,.6f));}
        void Update(){duck=Mathf.Max(0,duck-Time.unscaledDeltaTime);if(ambience)ambience.volume=Volume*(duck>0?.045f:.085f);}
    }
}
