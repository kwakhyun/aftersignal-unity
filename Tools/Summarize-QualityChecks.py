"""Collect observed results without converting unavailable counters into a pass."""
from pathlib import Path
import json, shutil, wave, xml.etree.ElementTree as ET
import numpy as np
root=Path(__file__).resolve().parents[1];out=root/'Documentation/Quality';checks=out/'checks';checks.mkdir(exist_ok=True)
result={'scope':'Station quality slice; 4 original scenes retained. No web-map expansion in this revision.','tests':{},'measurements':{},'runs':{},'audio':{},'unmeasured':['Minimum supported device','GPU pass costs','Valid draw-call counter','Transparent overdraw','Long-duration memory/GC soak','All aspect ratios','Gamepad navigation','Subjective listening and sustained manual input feel']}
for mode in ['EditMode','PlayMode']:
    path=root/f'Artifacts/{mode}.xml';node=ET.parse(path).getroot();result['tests'][mode]={k:node.get(k) for k in ['result','total','passed','failed','duration','start-time']};shutil.copy2(path,checks/path.name)
for name in ['BeforeFinal','AfterFinal','AfterEffectsOff','BeforePerformance','AfterPerformance','After15Hz','After120Hz','AfterD3D11','ReleaseForeground','BeforeForeground']:
    folder=root/'Artifacts/Quality'/name
    for file,key in [('metrics.json','measurements'),('playthrough.json','runs')]:
        path=folder/file
        if path.exists():
            data=json.loads(path.read_text());
            if file=='metrics.json' and data.get('maxBatches',0)<=0:data['maxBatches']=None
            if file=='metrics.json' and data.get('build')=='Release':data['meanGcBytes']=None
            if name=='ReleaseForeground':data['windowCondition']='Foreground attempt; Windows security dialog remained above game. Not a clean foreground benchmark.'
            result[key][name]=data;shutil.copy2(path,checks/f'{name}-{file}')
    path=folder/'game-audio.wav'
    if path.exists() and name in ['BeforeFinal','AfterFinal','AfterEffectsOff']:
        with wave.open(str(path),'rb') as f:
            a=np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(np.float64)/32768
            result['audio'][name]={'seconds':len(a)/f.getnchannels()/f.getframerate(),'channels':f.getnchannels(),'sampleRate':f.getframerate(),'peak':float(np.max(np.abs(a))),'rms':float(np.sqrt(np.mean(a*a))),'clippedSamples':int(np.sum(np.abs(a)>=.9999))}
campaign=root/'Artifacts/Runtime/playthrough.json'
if campaign.exists():result['campaign']=json.loads(campaign.read_text());shutil.copy2(campaign,checks/'campaign-playthrough.json')
build=root/'Artifacts/build-result.json'
if build.exists():result['build']=json.loads(build.read_text());shutil.copy2(build,checks/'build-result.json')
(out/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'tests':result['tests'],'campaign':result.get('campaign'),'build':result.get('build'),'runs':{k:v['completed'] for k,v in result['runs'].items()}},ensure_ascii=False,indent=2))
