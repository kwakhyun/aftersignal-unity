"""Collect native evidence; never synthesize screenshots or successful outcomes."""
from pathlib import Path
import csv,json,shutil,hashlib,statistics,xml.etree.ElementTree as ET,wave
import numpy as np
root=Path(__file__).resolve().parents[1];doc=root/'Documentation/Urban';media=doc/'Media';evidence=doc/'Evidence'
media.mkdir(parents=True,exist_ok=True);evidence.mkdir(parents=True,exist_ok=True)
def copy(a,b):
 assert a.is_file(),a
 b.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(a,b)
reports={
 'cityIteration1':root/'Artifacts/Urban15/Iteration1/city-upgrade.json',
 'cityIteration2':root/'Artifacts/Urban15/Iteration2/city-upgrade.json',
 'cityFinalPerformance':root/'Artifacts/Urban15/ReleasePerf/city-upgrade.json',
 'cityFinalVideo':root/'Artifacts/Urban15/FinalVideo/city-upgrade.json',
 'stairCameraRegression':root/'Artifacts/Urban15/StairReview/residence.json',
 'cityFacilities':root/'Artifacts/Urban/Release15/urban.json',
 'railRegression':root/'Artifacts/Residence/Release15/Rail/Haven/playthrough.json',
 'expansionRegression':root/'Artifacts/Residence/Release15/Expansion/expansion.json',
 'residenceRegression':root/'Artifacts/Residence/Release15/Residence/residence.json'
}
verified={}
for name,folder in [('cityFinalPerformance','Artifacts/Urban15/ReleasePerf'),('cityFinalVideo','Artifacts/Urban15/FinalVideo'),('stairCameraRegression','Artifacts/Urban15/StairReview'),('cityFacilities','Artifacts/Urban/Release15'),('railRegression','Artifacts/Residence/Release15/Rail'),('expansionRegression','Artifacts/Residence/Release15/Expansion'),('residenceRegression','Artifacts/Residence/Release15/Residence')]:
 log=root/folder/'player.log';content=log.read_text(encoding='utf-8',errors='replace')
 assert 'Crash!!!' not in content and 'Exception:' not in content,(name,'native log failure')
 assert 'CodeReloadManager destroyed' in content,(name,'native shutdown missing')
 copy(log,evidence/'Logs'/(name+'.log'))
for name,path in reports.items():
 d=json.loads(path.read_text(encoding='utf-8'));assert d['completed'] and not d['errors'],(name,d)
 checks=d.get('passed',d.get('checks',[]));assert all(not isinstance(v,dict) or v.get('passed',False) for v in checks),name
 verified[name]={'completed':True,'checks':len(checks),'errors':[]};copy(path,evidence/(name+'.json'))
for name in ['EditMode','PlayMode']:
 path=root/f'Artifacts/{name}.xml';xml=ET.parse(path).getroot();assert xml.get('result')=='Passed'
 verified[name]={'completed':True,'passed':int(xml.get('passed')),'failed':int(xml.get('failed'))};copy(path,evidence/path.name)
manual=json.loads((root/'Artifacts/Urban15/Manual/manual-input.json').read_text())
assert manual['directions']==15 and manual['wheelEvents']>0 and manual['shots']>0 and manual['reloads']>0 and manual['entries']>0,manual
verified['realDeviceInput']={'completed':True,'method':'Windows UI input observed by UrbanManualProbe; short taps, not a sustained human handling assessment','snapshot':manual};copy(root/'Artifacts/Urban15/Manual/manual-input.json',evidence/'manual-input.json')
(doc/'verification.json').write_text(json.dumps(verified,ensure_ascii=False,indent=2),encoding='utf-8')
perf={}
for label,folder in [('cityBefore',root/'Artifacts/Urban/Iteration2/UrbanCity-0'),('cityFinal',root/'Artifacts/Urban15/ReleasePerf'),('rail',root/'Artifacts/Residence/Release15/Rail'),('expansion',root/'Artifacts/Residence/Release15/Expansion'),('residence',root/'Artifacts/Residence/Release15/Residence')]:
 rows=[];stages=[]
 for p in folder.rglob('frames.csv'):
  rows.extend(csv.DictReader(p.open(encoding='utf-8-sig')));copy(p,evidence/'Performance'/label/p.relative_to(folder))
 values=sorted(float(v['frame_ms']) for v in rows)
 for p in folder.rglob('metrics.json'):
  d=json.loads(p.read_text());stages.append(d);copy(p,evidence/'Performance'/label/p.relative_to(folder))
 assert values,label
 perf[label]={'frames':len(values),'meanMs':statistics.mean(values),'p99Ms':values[int(len(values)*.99)],'maxMs':values[-1],'over33ms':sum(v>33.34 for v in values),'unfocused':sum(float(v['focused'])==0 for v in rows),'stages':stages}
(doc/'performance.json').write_text(json.dumps(perf,indent=2),encoding='utf-8')
shots={
 'city-before.png':'Artifacts/Urban/Iteration2/city-entry.png',
 'drive-before.png':'Artifacts/Urban/Iteration2/driving.png',
 'city-after.png':'Artifacts/Urban15/ReleasePerf/01-close-city.png',
 'drive-after.png':'Artifacts/Urban15/ReleasePerf/04-chase-driving.png',
 'explosion-after.png':'Artifacts/Urban15/FinalVideo/06-explosion.png',
 'map.png':'Artifacts/Urban/Release15/city-map.png',
 'fuel.png':'Artifacts/Urban/Release15/fuel-station.png'
}
for i in range(4):shots[f'fleet-{i}.png']=f'Artifacts/Urban15/ReleasePerf/fleet-{i}.png'
for i in range(5):shots[f'stair-{i}.png']=f'Artifacts/Urban15/StairReview/Residence-0/stair-middle-{i}.png'
for i in [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15]:shots[f'interior-{i}.png']=f'Artifacts/Urban/Release15/interior-{i}.png'
for name,path in shots.items():copy(root/path,media/name)
copy(root/'Artifacts/Urban15/FinalVideo/gameplay-with-audio.mp4',media/'city-gameplay.mp4')
audio=root/'Artifacts/Urban15/FinalVideo/game-audio.wav'
with wave.open(str(audio),'rb') as w:
 data=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
 audio_report={'sampleRate':w.getframerate(),'channels':w.getnchannels(),'seconds':w.getnframes()/w.getframerate(),'peak':float(abs(data).max()),'rms':float(np.sqrt(np.mean(data*data))),'clippedSamples':int(np.sum(abs(data)>=.9999)),'subjectiveListening':'not assessed'}
(doc/'audio.json').write_text(json.dumps(audio_report,indent=2),encoding='utf-8')
before=root/'Artifacts/Urban/Before/AfterSignal';after=root/'Assets/AfterSignal';changes=[];guid_changes=[]
for p in after.rglob('*'):
 if not p.is_file():continue
 old=before/p.relative_to(after)
 if not old.exists():status='added'
 elif hashlib.sha256(p.read_bytes()).digest()!=hashlib.sha256(old.read_bytes()).digest():status='changed'
 else:continue
 changes.append({'path':'Assets/AfterSignal/'+p.relative_to(after).as_posix(),'status':status})
 if p.suffix=='.meta' and old.exists():
  guid=lambda f:next((line for line in f.read_text(encoding='utf-8-sig').splitlines() if line.startswith('guid:')),'')
  if guid(p)!=guid(old):guid_changes.append(str(p))
assert not guid_changes,guid_changes
(doc/'changed-files.json').write_text(json.dumps({'baseline':'1.3 before urban extension','files':changes,'existingGuidChanges':guid_changes},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'verified':verified,'performance':{k:{a:b for a,b in v.items() if a!='stages'} for k,v in perf.items()},'audio':audio_report},ensure_ascii=False,indent=2))
