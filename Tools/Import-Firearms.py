"""Extract game-ready shots from the CC0 Free Firearm Sound Library.
Input: extracted Prepared SFX Library directory. Original recordings remain untouched.
Requires numpy; output is 48 kHz mono PCM16 WAV plus a provenance manifest.
"""
import argparse, hashlib, json, wave
from pathlib import Path
import numpy as np

SOURCES={
 "gun_pistol":("1911","A_42P.wav","A_34P.wav",1.18),
 "gun_police_pistol":("Walther PPQ","X_39P.wav","X_31P.wav",1.12),
 "gun_gang_pistol":("Bersa","F_47P.wav","F_41P.wav",1.10),
 "gun_shotgun":("Mossberg","N_30P.wav","N_26P.wav",1.45),
 "gun_rifle":("AR-15","D_32P.wav","D_24P.wav",1.18),
 "gun_auto":("AK-47","C_29P.wav","C_34P.wav",1.55),
}
def read_pcm(path):
 with wave.open(str(path)) as w:
  rate=w.getframerate();channels=w.getnchannels();width=w.getsampwidth();raw=w.readframes(w.getnframes())
 if width==3:
  a=np.frombuffer(raw,np.uint8).reshape(-1,3).astype(np.int32)
  a=a[:,0]|a[:,1]<<8|a[:,2]<<16
  a=np.where(a&0x800000,a-0x1000000,a)/8388608.
 elif width==2: a=np.frombuffer(raw,"<i2")/32768.
 else: raise ValueError("Unsupported PCM width")
 return a.reshape(-1,channels),rate
def shot_groups(a,rate):
 block=rate//100
 envelope=np.max(np.abs(a[:len(a)//block*block].reshape(-1,block,a.shape[1])),axis=(1,2))
 candidates=np.flatnonzero(envelope>max(.08,envelope.max()*.4))
 groups=[]
 for sample in candidates:
  if not groups or sample-groups[-1][-1]>16: groups.append([sample])
  else: groups[-1].append(sample)
 # A delayed single reflection must not count as a second trigger pull.
 return [g[0]/100 for g in groups if g[-1]-g[0]>=1]
def main():
 parser=argparse.ArgumentParser();parser.add_argument("source",type=Path);parser.add_argument("--output",type=Path,default=Path("Assets/AfterSignal/Resources/Audio/Firearms"));args=parser.parse_args()
 args.output.mkdir(parents=True,exist_ok=True);rows=[]
 for cue,(weapon,near,mid,duration) in SOURCES.items():
  for variant in range(3):
   source=args.source/weapon/(near if variant<2 else mid)
   a,rate=read_pcm(source);groups=shot_groups(a,rate)
   event=groups[min(variant if variant<2 else 0,len(groups)-1)]
   channel=variant%a.shape[1]
   lo=max(0,int((event-.06)*rate));hi=int((event+.07)*rate)
   region=np.abs(a[lo:hi,channel]);hits=np.flatnonzero(region>max(.025,region.max()*.035))
   begin=max(0,lo+int(hits[0])-int(rate*.003))
   samples=a[begin:begin+int(duration*rate),channel].copy()
   samples-=samples.mean()
   if rate!=48000:
    # Windowed sinc low pass before reducing 96 kHz source recordings.
    assert rate==96000
    x=np.arange(-32,33);kernel=np.sinc(x*.46)*np.hanning(65);kernel/=kernel.sum()
    samples=np.convolve(samples,kernel,mode="same")[::2]
   samples*=.89/max(.001,np.max(np.abs(samples)))
   samples[:24]*=np.linspace(0,1,24);samples[-4800:]*=np.linspace(1,0,4800)
   output=args.output/(cue+"_"+str(variant)+".wav")
   with wave.open(str(output),"wb") as w:
    w.setnchannels(1);w.setsampwidth(2);w.setframerate(48000);w.writeframes(np.round(np.clip(samples,-1,1)*32767).astype("<i2").tobytes())
   rows.append(dict(cue=cue,variant=variant,weapon=weapon,source=str(source.relative_to(args.source)),channel=channel,start_seconds=round(begin/rate,6),duration_seconds=round(len(samples)/48000,4),peak=round(float(np.max(np.abs(samples))),4),rms=round(float(np.sqrt(np.mean(samples*samples))),5),source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),output=str(output).replace("\\","/")))
 manifest=dict(license="CC0-1.0",license_url="https://creativecommons.org/publicdomain/zero/1.0/",authors=["Ben Jaszczak","Brian Nelson","Kevin Heras","Matthew Nanney"],source_page="https://opengameart.org/content/the-free-firearm-sound-library",archive_url="https://opengameart.org/sites/default/files/Prepared%20SFX%20Library.7z",processing="Individual real shots/bursts; onset trim, DC removal, antialias downsample, mono channel selection, -1 dBFS peak normalization, short boundary fades. No synthesized gunshots.",clips=rows)
 Path("Documentation/Audio/firearm-sources.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
 print(json.dumps([dict(cue=r["cue"],variant=r["variant"],start=r["start_seconds"],rms=r["rms"]) for r in rows],indent=2))
if __name__=="__main__": main()
