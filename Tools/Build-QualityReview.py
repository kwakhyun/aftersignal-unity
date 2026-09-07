"""Assemble QA evidence from real engine recordings; never paints over game frames."""
from pathlib import Path
import json, shutil, subprocess, sys, zipfile
from PIL import Image, ImageDraw, ImageOps
root=Path(__file__).resolve().parents[1];out=root/'Documentation/Quality';qa=root/'Artifacts/Quality'
sys.path.insert(0,str(root/'Tools/.python-deps'))
import imageio_ffmpeg
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
for source,label in [('BeforeFinal','before'),('AfterFinal','after'),('AfterEffectsOff','effects-off')]:
    src=qa/source;dest=out/label;dest.mkdir(exist_ok=True)
    for name in ['00-Station.png','01-station-power.png','02-combat-entry.png','02-boarding.png','playthrough.json','metrics.json','gameplay-with-audio.mp4']:
        if (src/name).exists():shutil.copy2(src/name,dest/name)
    for sec in [18,19,20,21,22,23,24,25,26,27,45,50]:
        subprocess.run([ffmpeg,'-y','-ss',str(sec),'-i',str(src/'gameplay-with-audio.mp4'),'-frames:v','1',str(dest/f'frame-{sec:02}.jpg')],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    strip=Image.new('RGB',(1600,735),'#101b22');draw=ImageDraw.Draw(strip)
    for i,sec in enumerate([18,19,20,21,22,23,24,25,26,27,45,50]):
        x=i%4*400;y=i//4*245;im=Image.open(dest/f'frame-{sec:02}.jpg');strip.paste(im.resize((400,225)),(x,y+20));draw.text((x+8,y+3),f'{source} / {sec}s',fill='#c6d8d1')
    strip.save(out/f'{label}-filmstrip.jpg',quality=94)
before=Image.open(out/'before/00-Station.png');after=Image.open(out/'after/00-Station.png')
compare=Image.new('RGB',(1600,950),'#101b22');draw=ImageDraw.Draw(compare)
for n,(label,im) in enumerate([('BEFORE',before),('AFTER / QUALITY SLICE',after)]):
    compare.paste(im.resize((800,450)),(n*800,25));compare.paste(ImageOps.grayscale(im).convert('RGB').resize((800,450)),(n*800,500));draw.text((n*800+20,8),label,fill='#d5dfd8');draw.text((n*800+20,482),'GRAYSCALE / same initial camera, 1600 x 900 source',fill='#d5dfd8')
compare.save(out/'before-after.jpg',quality=95)
if not (out/'rollback-v1.0-assets.zip').exists():
    with zipfile.ZipFile(out/'rollback-v1.0-assets.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in (qa/'Backup').rglob('*'):
            if p.is_file():
                rel=p.relative_to(qa/'Backup');prefix='' if rel.parts[0]=='ProjectSettings' else 'Assets/'
                z.write(p,prefix+rel.as_posix())
changes=[];backup=qa/'Backup'
for sub,active in [('AfterSignal',root/'Assets/AfterSignal'),('Settings',root/'Assets/Settings'),('ProjectSettings',root/'ProjectSettings')]:
    for p in active.rglob('*'):
        if p.is_file():
            old=backup/sub/p.relative_to(active)
            if not old.exists() or p.read_bytes()!=old.read_bytes():changes.append({'path':p.relative_to(root).as_posix(),'status':'modified' if old.exists() else 'added'})
(out/'changed-files.json').write_text(json.dumps(changes,indent=2),encoding='utf-8')
print('Evidence assembled:',out,flush=True)
