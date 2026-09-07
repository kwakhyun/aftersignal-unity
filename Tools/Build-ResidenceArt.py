"""Deterministic asset preparation. Sources remain untouched; semantic artwork is generated separately."""
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import json, shutil, wave

ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('C:/Users/82105/.codex/generated_images/01a0732c-239b-7072-99fe-6782f54c0ec8')
ART=ROOT/'Assets/AfterSignal/Resources/Art'
DOC=ROOT/'Documentation/Residence'
(DOC/'Sources').mkdir(parents=True,exist_ok=True)
(DOC/'Previews').mkdir(exist_ok=True)

def source(name):
    dest=DOC/'Sources'/name
    if not dest.exists(): shutil.copy2(SOURCE/name,dest)
    return dest

def components(mask):
    # Run-length connected components preserve weapons extending past nominal sheet cells.
    parent=[]; runs=[]; previous=[]
    def find(i):
        while parent[i]!=i:
            parent[i]=parent[parent[i]];i=parent[i]
        return i
    for y,row in enumerate(mask):
        edges=np.flatnonzero(np.diff(np.r_[False,row,False].astype(np.int8)))
        current=[]
        for a,b in zip(edges[::2],edges[1::2]):
            i=len(parent);parent.append(i);runs.append((y,int(a),int(b)));current.append(i)
            for j in previous:
                _,c,d=runs[j]
                if c>b:break
                if d>=a:parent[find(i)]=find(j)
        previous=current
    groups={}
    for i,r in enumerate(runs):groups.setdefault(find(i),[]).append(r)
    return list(groups.values())

def extent(runs):return (min(a for y,a,b in runs),min(y for y,a,b in runs),max(b for y,a,b in runs),max(y for y,a,b in runs)+1)
def count(runs):return sum(b-a for y,a,b in runs)

def sprites(filename,name,rows,scale,npc=False):
    arr=np.array(Image.open(source(filename)).convert('RGB'));rgb=arr.astype(np.int16)
    mask=~((rgb[:,:,1]>rgb[:,:,0]+45)&(rgb[:,:,1]>rgb[:,:,2]+45))
    groups=components(mask);big=[g for g in groups if count(g)>2200]
    assert len(big)==rows*4,(name,'expected poses',len(big),[(count(g),extent(g)) for g in big])
    big.sort(key=lambda g:extent(g)[3]);ordered=[]
    for row in range(rows):ordered+=sorted(big[row*4:(row+1)*4],key=lambda g:extent(g)[0])
    # Associate only small detached details such as the dropped magazine with the nearest pose.
    for g in groups:
        if count(g)>2200 or count(g)<8:continue
        a,b,c,d=extent(g);gx=(a+c)/2;gy=(b+d)/2
        def distance(p):
            x,y,z,w=extent(p);return max(x-gx,0,gx-z)**2+max(y-gy,0,gy-w)**2
        near=min(ordered,key=distance)
        if distance(near)<30**2:near.extend(g)
    preview=Image.new('RGB',(4*288,rows*288),(29,43,49));points=[];report=[]
    for i,g in enumerate(ordered):
        x,y,z,w=extent(g);isolated=np.zeros((w-y,z-x,4),dtype=np.uint8)
        for yy,a,b in g:isolated[yy-y,a-x:b-x,:3]=arr[yy,a:b];isolated[yy-y,a-x:b-x,3]=255
        sy,sx=np.where(isolated[:,:,3]>0);feet=sx[sy>=max(sy)-7];anchor=(feet.min()+feet.max())/2
        factor=scale if not npc else 107/(extent(ordered[(i//4)*4])[3]-extent(ordered[(i//4)*4])[1])
        im=Image.fromarray(isolated).resize((round((z-x)*factor),round((w-y)*factor)),Image.Resampling.NEAREST)
        dx=round(144-anchor*factor);dy=272-im.height
        assert dx>=1 and dy>=1 and dx+im.width<288,(name,i,im.size,dx,dy)
        canvas=Image.new('RGBA',(288,288));canvas.alpha_composite(im,(dx,dy))
        if npc:
            role=['teacher','medic','commander','concierge'][i//4];out=ART/'NPC/Civic'/role;file=f'{role}-{i%4:02}.png'
        else:out=ART/'Hero/Actions'/name;file=f'{name}-{i:02}.png'
        out.mkdir(parents=True,exist_ok=True);canvas.save(out/file)
        preview.paste(canvas,((i%4)*288,(i//4)*288),canvas);ImageDraw.Draw(preview).text(((i%4)*288+8,(i//4)*288+10),str(i),fill='white')
        if name=='Pistol':
            # Authoritative barrel tips are taken from the visible right-facing gun at each firing pose.
            aa=np.array(canvas);fg=aa[:,:,3]>0;ys,xs=np.where(fg & (np.indices(fg.shape)[0]<dy+im.height*.6))
            mx=int(xs.max());my=int(np.median(ys[xs>=mx-1]));points.append({'x':mx,'y':my})
        report.append({'index':i,'source_bounds':[x,y,z,w],'scale':factor,'canvas':[288,288],'pivot':[144,272],'opaque_pixels':int((np.array(canvas)[:,:,3]>0).sum())})
    preview.save(DOC/'Previews'/f'{name}-contact-sheet.png')
    if points:(ART/'Hero/Actions/gun-points.json').write_text(json.dumps({'points':points}),encoding='utf-8')
    return report

reports={}
reports['Katana']=sprites('exec-cdfee031-99c3-404a-83f7-8b1ce8283a9a.png','Katana',4,.525)
reports['Greatsword']=sprites('exec-c3b364cb-0dcf-47fa-92f9-b49eef24d712.png','Greatsword',4,.513)
reports['Pistol']=sprites('exec-540ae330-02b2-4ccd-a82c-0a976feae321.png','Pistol',6,.442)
reports['Citizens']=sprites('exec-b8236dfe-8b22-4867-98ce-4943f82efd8e.png','Citizens',4,1,True)
(DOC/'sprite-audit.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')

atlases={
 'exec-cec5bc06-48be-459e-a014-de2282ecc890.png':['Haven','Market','Harbor','Skyline'],
 'exec-d85906bb-018a-43e6-8071-b8f834ed3f8a.png':['Residence','School','Clinic','Headquarters'],
 'exec-5881ab31-5fc1-4ecc-add5-00780930a1d0.png':['Station','Carriage','Transit','Roof'],
 'exec-7ab66d53-539c-4870-9696-8927f24ea203.png':['Arcade','Lab','Archive','Tower'],
 'exec-2ab99f8c-cc98-4fcf-b2e5-e212855050b7.png':['Foundry','Crown','Origin','Breach'],
 'exec-aaa0f2ef-9fea-4afc-8603-eafafb814b2b.png':['Canal','Aqueduct','Observatory','CivicAvenue']}
(ART/'Environment').mkdir(exist_ok=True)
for file,names in atlases.items():
    im=Image.open(source(file));w,h=im.size
    for i,name in enumerate(names):
        x=i%2*w//2;y=i//2*h//2
        im.crop((x+2,y+2,x+w//2-2,y+h//2-2)).save(ART/'Environment'/f'{name}.png')

# Original synthesized Foley and energy cues. No third-party samples or music.
audio=ROOT/'Assets/AfterSignal/Resources/Audio/Quality';audio.mkdir(parents=True,exist_ok=True)
specs={'weapon_switch':(.15,950),'reload_out':(.20,370),'reload_in':(.16,520),'reload_slide':(.25,720),'pistol_overdrive':(.22,165),'katana_wave':(.48,370),'heavy_slam':(.55,65),'door':(.65,180)}
audit=[]
for cue,(duration,freq) in specs.items():
    for variant in range(3):
        rng=np.random.default_rng(1300+variant+int(freq));sr=44100;t=np.arange(int(sr*duration))/sr
        noise=rng.uniform(-1,1,len(t));smooth=np.convolve(noise,np.ones(9)/9,mode='same')
        f=freq*(1+(variant-1)*.045)
        if cue=='heavy_slam':signal=.65*np.sin(2*np.pi*(f*t-25*t*t))*np.exp(-t*12)+smooth*np.exp(-t*8)
        elif cue=='katana_wave':signal=smooth*np.sin(np.pi*t/duration)**.7+.22*np.sin(2*np.pi*(f*t+120*t*t))*np.exp(-t*6)
        elif cue=='door':signal=.28*smooth*np.sin(np.pi*t/duration)+.4*np.sin(2*np.pi*f*t)*np.exp(-t*22)+.5*smooth*np.exp(-((t-duration*.8)/.03)**2)
        else:signal=noise*np.exp(-t*45)*.55+np.sin(2*np.pi*f*t)*np.exp(-t*35)*.35+smooth*np.exp(-((t-duration*.45)/.015)**2)*.5
        signal*=np.minimum(t/.002,1)*np.minimum((duration-t)/.012,1)
        signal=signal/(max(abs(signal))+.001)*.7
        with wave.open(str(audio/f'{cue}_{variant}.wav'),'wb') as out:out.setnchannels(1);out.setsampwidth(2);out.setframerate(sr);out.writeframes((signal*32767).astype('<i2').tobytes())
        audit.append({'cue':cue,'variant':variant,'duration':duration,'peak':float(max(abs(signal)))})
(DOC/'audio-audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print('Prepared 56 hero poses, 16 citizens, 24 environment plates, 24 original audio cues.')

# Authored surface atlases are mechanically split, preserving their source artwork.
for file,folder,names in [
 ('exec-8356537a-7bc6-48ad-85d1-107832b8b0b4.png','Facades',['Residence','School','Clinic','Headquarters']),
 ('exec-98102c74-8afa-4452-8861-bebcfce5951e.png','Furniture',['Wardrobe','Refrigerator','Quilt','Cabinet'])]:
    im=Image.open(source(file));w,h=im.size;out=ART/'Environment'/folder;out.mkdir(exist_ok=True)
    for i,name in enumerate(names):
        x=i%2*w//2;y=i//2*h//2;tile=im.crop((x+3,y+3,x+w//2-3,y+h//2-3))
        if name=='Refrigerator':tile=tile.crop((0,0,tile.width//2,tile.height))
        tile.save(out/(name+'.png'))
foliage=np.array(Image.open(source('exec-1d3c72e9-f73c-4265-9582-ec94e1816765.png')).convert('RGBA'))
rgb=foliage[:,:,:3].astype(np.int16);key=(rgb[:,:,1]>rgb[:,:,0]+18)&(rgb[:,:,1]>rgb[:,:,2]+28)
foliage[key]=0
im=Image.fromarray(foliage);im=im.crop(im.getbbox());im.thumbnail((768,768),Image.Resampling.LANCZOS);im.save(ART/'Environment/Foliage.png')
print('Prepared 8 architectural/furniture surfaces and a transparent foliage card.')
shutil.copy2(source('exec-75abdd48-6429-4bb9-8b8a-d843bc5062df.png'),ART/'Environment/Haven.png')
