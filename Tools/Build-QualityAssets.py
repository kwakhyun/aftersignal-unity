"""Original authored DSP sound assets and transparent sprite-strip normalization.
No third-party recordings. Deterministic seeds; source images remain untouched.
"""
from pathlib import Path
from collections import deque
import json, wave, shutil
import numpy as np
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets/AfterSignal/Resources'
OUT=ROOT/'Documentation/Quality';OUT.mkdir(parents=True,exist_ok=True)

def sprites():
    source=Path('C:/Users/82105/.codex/generated_images/01a0732c-239b-7072-99fe-6782f54c0ec8/exec-76c4d448-941c-40d6-8475-f66048f5f78a.png')
    shutil.copy2(source,OUT/'katana-generated-DRAFT.png')
    rgb=np.array(Image.open(source).convert('RGB'));h,w=rgb.shape[:2]
    eligible=(rgb[:,:,1].astype(int)>rgb[:,:,0].astype(int)+65)&(rgb[:,:,1].astype(int)>rgb[:,:,2].astype(int)+65)
    # Remove only pale neutral background connected to the outer canvas. White hair enclosed by its outline survives.
    bg=np.zeros((h,w),bool);q=deque()
    for y,x in [(0,x) for x in range(w)]+[(h-1,x) for x in range(w)]+[(y,0) for y in range(h)]+[(y,w-1) for y in range(h)]:
        if eligible[y,x] and not bg[y,x]: bg[y,x]=True;q.append((y,x))
    while q:
        y,x=q.popleft()
        for yy,xx in ((y-1,x),(y+1,x),(y,x-1),(y,x+1)):
            if 0<=yy<h and 0<=xx<w and eligible[yy,xx] and not bg[yy,xx]:bg[yy,xx]=True;q.append((yy,xx))
    mask=~eligible; seen=np.zeros_like(mask);components=[]
    for y,x in zip(*np.where(mask)):
        if seen[y,x]:continue
        seen[y,x]=True;q=deque([(y,x)]);points=[]
        while q:
            yy,xx=q.popleft();points.append((yy,xx))
            for y2,x2 in ((yy-1,xx),(yy+1,xx),(yy,xx-1),(yy,xx+1)):
                if 0<=y2<h and 0<=x2<w and mask[y2,x2] and not seen[y2,x2]:seen[y2,x2]=True;q.append((y2,x2))
        if len(points)>2500:components.append(np.array(points))
    assert len(components)==6, f'Expected 6 isolated silhouettes; got {len(components)}'
    components.sort(key=lambda a:(int(a[:,0].mean()>h*.5),a[:,1].mean()))
    dest=ASSETS/'Art/Hero/Quality';dest.mkdir(parents=True,exist_ok=True)
    scale=.32;sheet=Image.new('RGBA',(224*3,260*2),(14,24,30,255));audit=[]
    # All poses share one scale; fixed hip/stance anchors avoid auto-centering by weapon-tip extent.
    anchors=[(209,550),(596,550),(940,550),(190,1020),(598,1020),(944,1020)]
    for i,points in enumerate(components):
        ymin,xmin=points.min(axis=0);ymax,xmax=points.max(axis=0);rgba=np.zeros((h,w,4),np.uint8);rgba[points[:,0],points[:,1],:3]=rgb[points[:,0],points[:,1]];rgba[points[:,0],points[:,1],3]=255
        crop=Image.fromarray(rgba).crop((xmin,ymin,xmax+1,ymax+1));crop=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.NEAREST)
        frame=Image.new('RGBA',(224,224));ax,ay=anchors[i];x=round(112+(xmin-ax)*scale);y=round(212+(ymin-ay)*scale)
        assert x>=0 and y>=0 and x+crop.width<=224 and y+crop.height<=224,(i,x,y,crop.size)
        frame.alpha_composite(crop,(x,y));frame.save(dest/f'katana-{i:02}.png');sheet.alpha_composite(frame,((i%3)*224,(i//3)*260))
        audit.append({'frame':i,'size':[224,224],'scale':scale,'anchor':[112,212],'bounds':frame.getbbox(),'alpha':'binary','sourceBounds':[int(xmin),int(ymin),int(xmax),int(ymax)]})
    draw=ImageDraw.Draw(sheet)
    labels=['00-40ms ANTICIPATE','40-90ms COIL','90-120ms CUT / HIT 100ms','120-195ms CONTACT','195-275ms FOLLOW','275-320ms RECOVER']
    for i,label in enumerate(labels):draw.text(((i%3)*224+7,(i//3)*260+230),label,fill=(170,215,205))
    sheet.convert('RGB').save(OUT/'katana-timing-sheet.png');(OUT/'sprite-audit.json').write_text(json.dumps(audit,indent=2))

RATE=44100
def wav(path,data):
    data=np.asarray(data);data=data-np.mean(data,axis=0);peak=np.abs(data).max();data=data/(max(peak,.001))*.72
    with wave.open(str(path),'wb') as f:f.setnchannels(1 if data.ndim==1 else data.shape[1]);f.setsampwidth(2);f.setframerate(RATE);f.writeframes((data*32767).astype('<i2').tobytes())
    return {'name':path.name,'seconds':round(len(data)/RATE,3),'peak':round(float(np.abs(data).max()),4),'rms':round(float(np.sqrt(np.mean(data**2))),4)}
def audio():
    dest=ASSETS/'Audio/Quality';dest.mkdir(parents=True,exist_ok=True);report=[];montage=[]
    kinds=['blade_swing','blade_hit','heavy_swing','heavy_hit','pistol','hurt','guard','death','step_tile','step_metal','land','jump','dash','rope_attach','rope_release','skill','glass','ui_confirm','ui_cancel','ui_warn']
    for k,name in enumerate(kinds):
        for variant in range(3):
            rng=np.random.default_rng(902+k*41+variant);duration=.12 if name.startswith('step') else .55 if name in ['heavy_hit','death','skill','glass'] else .28
            t=np.arange(round(RATE*duration))/RATE;n=rng.uniform(-1,1,len(t));low=np.convolve(n,np.ones(35)/35,'same');high=n-np.convolve(n,np.ones(7)/7,'same');freq=1+(variant-1)*.055
            env=np.exp(-t/(duration*.17));attack=np.minimum(t/.003,1)
            tone=lambda hz:np.sin(2*np.pi*hz*freq*t)
            if 'swing' in name or name in ['dash','jump','rope_release']:
                center=duration*.28;whoosh=np.exp(-((t-center)/(duration*.18))**2);d=low*2.4*whoosh+high*.15*whoosh+tone(115 if name=='heavy_swing' else 320)*whoosh*.06
            elif name=='pistol':
                d=high*.48*np.exp(-t/.012)+np.sin(2*np.pi*(160*t-140*t*t))*.7*np.exp(-t/.034)+low*.6*np.exp(-t/.065)
            elif name in ['heavy_hit','land','death','hurt']:
                base={'heavy_hit':72,'land':115,'death':58,'hurt':94}[name];d=tone(base)*env*.7+tone(base*1.58)*np.exp(-t/.065)*.25+low*3*env+high*.2*np.exp(-t/.015)
            elif name in ['blade_hit','guard','step_metal','rope_attach','glass']:
                base=720 if name=='glass' else 430 if name=='guard' else 210;d=high*.35*np.exp(-t/.019)+low*1.5*env
                for j,ratio in enumerate([1,1.47,2.19,3.62]):d+=tone(base*ratio)*np.exp(-t/(duration*.22/(1+j*.25)))*(.23/(j+1))
                if name=='glass':d+=high*np.maximum(0,np.sin(t*133))**8*np.exp(-t/.16)*.25
            elif name.startswith('ui_'):
                base={'ui_confirm':660,'ui_cancel':390,'ui_warn':880}[name];d=(tone(base)+.35*tone(base*1.5))*np.exp(-t/.045)*.3
            elif name=='skill':
                d=np.sin(2*np.pi*(45*t+350*t*t))*np.exp(-t/.16)*.7+low*3*np.exp(-t/.1)+tone(880)*np.exp(-((t-.07)/.04)**2)*.18
            else:d=low*3*env+high*.12*np.exp(-t/.012)+tone(170)*env*.2
            d*=attack*np.minimum((duration-t)/.012,1);report.append(wav(dest/f'{name}_{variant}.wav',d))
            if variant==0:montage.extend([d/max(np.abs(d).max(),.001)*.55,np.zeros(int(RATE*.25))])
    # Seamless loop: sum of integer-period oscillators and periodic filtered noise, no copyrighted recordings.
    for name in ['station','train','roof','town']:
        seconds=8;t=np.arange(RATE*seconds)/RATE;rng=np.random.default_rng(701+len(name));noise=rng.normal(0,1,len(t));smooth=np.convolve(np.r_[noise[-300:],noise,noise[:300]],np.ones(601)/601,'valid')[:len(t)]
        base=.1*np.sin(2*np.pi*55*t)+.055*np.sin(2*np.pi*82.5*t)+.028*np.sin(2*np.pi*110*t)
        if name=='station':d=base*.3+smooth*.35+.013*np.sin(2*np.pi*430*t)*(np.sin(2*np.pi*.25*t)**16)
        elif name=='train':d=base*.3+smooth*.65+.06*np.sin(2*np.pi*74*t)*np.maximum(0,np.cos(t*np.pi*4))**18
        elif name=='roof':d=base*.2+smooth*1.3*(.5+.15*np.sin(2*np.pi*.375*t))
        else:d=base*.3+smooth*.2+.017*np.sin(2*np.pi*330*t)*np.sin(np.pi*t/2)**8
        stereo=np.stack([d,np.roll(d,73)],axis=1)*.3;report.append(wav(dest/f'ambient_{name}.wav',stereo))
    wav(OUT/'sound-bank-preview.wav',np.concatenate(montage));(OUT/'audio-audit.json').write_text(json.dumps(report,indent=2))

if __name__=='__main__':sprites();audio();print('Normalized 6 poses and authored 64 WAV assets.')
