"""Original impact layers and reverberation tails derived from the project's licensed gun recordings."""
from pathlib import Path
import wave
import numpy as np
R=Path(__file__).resolve().parents[2];OUT=R/'Assets/AfterSignal/Resources/Audio/CombatDetail';OUT.mkdir(parents=True,exist_ok=True)
rate=44100;rng=np.random.default_rng(1947)
def write(name,samples):
 samples=np.nan_to_num(samples);samples=samples/max(1.,float(np.max(np.abs(samples)))/.8)
 with wave.open(str(OUT/(name+'.wav')),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((samples*32767).astype('<i2').tobytes())
def lowpass(x,width):return np.convolve(x,np.ones(width)/width,mode='same')
def recording(path):
 with wave.open(str(path),'rb') as f:
  assert f.getsampwidth()==2;arr=np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float)/32768;arr=arr.reshape(-1,f.getnchannels()).mean(axis=1);r=f.getframerate()
 return np.interp(np.arange(int(len(arr)*rate/r))*r/rate,np.arange(len(arr)),arr)
for gun in ['pistol','rifle','shotgun']:
 source=recording(R/f'Assets/AfterSignal/Resources/Audio/Firearms/gun_{gun}_0.wav')
 for variant in range(3):
  result=np.zeros(rate*3)
  for delay,gain in [(.09,.45),(.17,.28),(.29,.17),(.46,.1),(.71,.055)]:
   at=int((delay+variant*.006)*rate);n=min(len(source),len(result)-at);result[at:at+n]+=lowpass(source[:n],13+variant*4)*gain
  write(f'tail_{gun}_{variant}',result)
for variant in range(3):
 t=np.arange(int(rate*.45))/rate;n=rng.uniform(-1,1,len(t))
 write(f'impact_metal_{variant}',(np.sin(2*np.pi*(2100+variant*230)*t)+.35*np.sin(2*np.pi*3277*t))*np.exp(-t*24)*.27+n*np.exp(-t*120)*.5)
 write(f'impact_concrete_{variant}',lowpass(n,4)*np.exp(-t*35)*1.4+np.sin(2*np.pi*170*t)*np.exp(-t*50)*.18)
 write(f'impact_flesh_{variant}',lowpass(n,22)*np.exp(-t*55)*1.1+np.sin(2*np.pi*95*t)*np.exp(-t*45)*.3)
 write(f'impact_glass_{variant}',(n-lowpass(n,12))*np.exp(-t*18)*.28)
t=np.arange(rate*4)/rate;n=rng.uniform(-1,1,len(t));boom=lowpass(n,90)*np.exp(-t*1.1)*2.8+np.sin(2*np.pi*(48*t-4*t*t))*np.exp(-t*2)*.4
boom+=lowpass(n,4)*np.exp(-t*18)*.55;write('blast_pressure',boom)
t=np.arange(int(rate*2.9))/rate;n=rng.uniform(-1,1,len(t));envelope=np.minimum(1,t*12)*np.clip((2.9-t)*3,0,1)
write('titan_barrage',(np.sin(2*np.pi*(160*t+21*np.sin(t*4))) *.25+lowpass(n,4)*.35+np.sin(2*np.pi*56*t)*.14)*envelope)
print(f'Wrote {len(list(OUT.glob("*.wav")))} combat detail clips')
