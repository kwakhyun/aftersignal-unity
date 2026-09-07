using System;
using System.IO;
using UnityEngine;
namespace AfterSignal
{
    // Captures this game's listener output only. No microphone or system loopback.
    public sealed class QualityAudioCapture:MonoBehaviour
    {
        float[] samples;int count,channels=2,rate;readonly object gate=new object();
        void Awake(){rate=AudioSettings.outputSampleRate;samples=new float[rate*2*100];}
        void OnAudioFilterRead(float[] data,int channelCount){lock(gate){channels=channelCount;int n=Math.Min(data.Length,samples.Length-count);Array.Copy(data,0,samples,count,n);count+=n;}}
        public void Save(string folder)
        {
            lock(gate){if(count==0){File.WriteAllText(Path.Combine(folder,"audio-capture-unavailable.txt"),"The listener filter did not supply samples.");return;}
                using(var writer=new BinaryWriter(File.Create(Path.Combine(folder,"game-audio.wav")))){
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);for(int i=0;i<count;i++)writer.Write((short)(Math.Max(-1,Math.Min(1,samples[i]))*32767));
                }
            }
        }
    }
}
