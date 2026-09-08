"""Cut recorded magazine handling into the game's three reload timing cues."""
import pathlib,json,wave,hashlib,numpy as np
root=pathlib.Path(__file__).resolve().parents[2];source=root/'Artifacts/WorldExpansion/gunreload1.wav'
with wave.open(str(source)) as f:rate=f.getframerate();channels=f.getnchannels();data=np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float).reshape(-1,channels).mean(axis=1)/32768
block=rate//100;envelope=np.max(np.abs(data[:len(data)//block*block].reshape(-1,block)),axis=1)
groups=[]
for t in np.flatnonzero(envelope>max(.025,envelope.max()*.12)):
    if not groups or t-groups[-1][-1]>16:groups.append([t])
    else:groups[-1].append(t)
print('recorded events',[(g[0]/100,g[-1]/100) for g in groups],flush=True)
if len(groups)<3:raise RuntimeError('Need three distinct mechanical handling events')
chosen=[groups[0],groups[len(groups)//2],groups[-1]]
rows=[];out=root/'Assets/AfterSignal/Resources/Audio/Firearms'
for cue,g in zip(['reload_out','reload_in','reload_slide'],chosen):
    a=max(0,int((g[0]/100-.025)*rate));b=min(len(data),int((g[-1]/100+.13)*rate));samples=data[a:b].copy();samples-=samples.mean();samples*=.88/max(.001,np.max(np.abs(samples)))
    samples[:min(len(samples),int(rate*.004))]*=np.linspace(0,1,min(len(samples),int(rate*.004)));samples[-min(len(samples),int(rate*.035)):]*=np.linspace(1,0,min(len(samples),int(rate*.035)))
    for variant in range(3):
        dest=out/(cue+'_'+str(variant)+'.wav')
        with wave.open(str(dest),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((samples*(.94+variant*.03)*32767).astype('<i2').tobytes())
        rows.append({'output':str(dest.relative_to(root)),'start':a/rate,'end':b/rate,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
(root/'Documentation/WorldExpansion/reload-sources.json').write_text(json.dumps({'source':'https://opengameart.org/content/gun-reload-sounds','download':'https://opengameart.org/sites/default/files/gunreload1.wav','author':'SpringySpringo','license':'CC0-1.0','recording':'Airsoft gun magazine and action handling; gunshots remain the real 1911 recordings credited in firearm-sources.json.','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'edits':rows},indent=2),encoding='utf-8')
