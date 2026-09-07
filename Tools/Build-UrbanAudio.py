"""Original deterministic synthesis; no third-party recordings."""
from pathlib import Path
import numpy as np,wave
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/AfterSignal/Resources/Audio/Quality'
rng=np.random.default_rng(6107);sr=48000
def save(path,x):
 x=np.clip(x,-.9,.9);path.parent.mkdir(parents=True,exist_ok=True)
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((x*32767).astype('<i2').tobytes())
t=np.arange(sr*2)/sr
engine=sum(np.sin(2*np.pi*f*t)*a for f,a in [(40,.14),(80,.1),(120,.06),(160,.03)])*(.88+.12*np.sin(2*np.pi*20*t))
save(OUT.parent/'urban_engine.wav',engine)
for cue,sec in [('urban_door',.38),('urban_crash',.65),('urban_impact',.4)]:
 for i in range(3):
  t=np.arange(int(sr*sec))/sr;n=rng.normal(0,1,len(t));low=np.convolve(n,np.ones(24)/24,'same')
  f=85+i*7;x=(np.sin(2*np.pi*f*t)*.4+low*.7)*np.exp(-t*(18 if cue=='urban_door' else 9))
  if cue=='urban_crash':x+=n*.075*np.exp(-t*7)
  if cue=='urban_door':x+=np.roll(n*.09*np.exp(-t*50),int(sr*.18))
  save(OUT/f'{cue}_{i}.wav',x)
for name,base in [('sedan',48),('taxi',55),('bus',30),('truck',34)]:
 t=np.arange(sr*2)/sr
 signal=sum(np.sin(2*np.pi*base*k*t)*(.18/k) for k in range(1,7))
 signal*=.78+.22*np.sin(2*np.pi*(base/2)*t)
 save(OUT.parent/f'urban_engine_{name}.wav',signal)
for i in range(3):
 t=np.arange(int(sr*1.8))/sr;n=rng.normal(0,1,len(t));low=np.convolve(n,np.ones(180)/180,'same')
 thump=np.sin(2*np.pi*(42*t-9*t*t))*.6*np.exp(-t*3)
 debris=n*.09*np.exp(-t*2.5)*(np.sin(t*95)**8)
 save(OUT/f'urban_explosion_{i}.wav',thump+low*2*np.exp(-t*2)+debris)
 t=np.arange(int(sr*.9))/sr
 save(OUT/f'urban_brake_{i}.wav',(.12*np.sin(2*np.pi*(640+i*45)*t)+rng.normal(0,.025,len(t)))*np.sin(np.pi*t/.9)**2)
print('20 original vehicle WAVs: four engine characters, brake, explosion, door and impacts.')
