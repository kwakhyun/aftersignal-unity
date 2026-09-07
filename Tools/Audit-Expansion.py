"""Summarize raw native frame logs, without dropping unfocused or slow samples."""
from pathlib import Path
import json,csv,statistics,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
out=root/'Documentation/Expansion';out.mkdir(parents=True,exist_ok=True)
def quantile(v,q):return sorted(v)[min(len(v)-1,int(len(v)*q))] if v else None
def stat(path):
    m=json.loads(path.read_text());m['source']=str(path.relative_to(root));m['stage']=path.parent.name
    if (path.parent/'frames.csv').exists():
        rows=list(csv.DictReader((path.parent/'frames.csv').open()))
        active=[float(r['frame_ms']) for r in rows if r['focused']=='1.0000']
        main=[float(r['CPU Main Thread Frame Time']) for r in rows if float(r['CPU Main Thread Frame Time'])>=0]
        m.update(focusPercent=100*len(active)/max(1,len(rows)),focusedSamples=len(active),focusedMeanMs=statistics.mean(active) if active else None,focusedP99Ms=quantile(active,.99),cpuMainMeanMs=statistics.mean(main) if main else None,cpuMainP99Ms=quantile(main,.99))
    return m
tests=[]
for kind in ('EditMode','PlayMode'):
    p=root/f'Artifacts/{kind}.xml'
    if p.exists():
        x=ET.parse(p).getroot();tests.append({'suite':kind,**{k:x.get(k) for k in ('total','passed','failed','result','duration')}})
runs={}
for name in ('FullPerformance','FocusedPerformance','VisualRelease','OriginalRelease'):
    folder=root/'Artifacts/Expansion'/name
    if folder.exists():
        checks=list(folder.rglob('expansion.json'))+list(folder.rglob('playthrough.json'))
        reports=[json.loads(p.read_text()) for p in checks]
        runs[name]={'checks':reports,'final':max(reports,key=lambda r:len(r.get('checks',[]))) if reports else None,'metrics':[stat(p) for p in sorted(folder.rglob('metrics.json'))]}
station={}
for name in ('ExpansionBaseline','StationRelease','StationVideo'):
    p=root/'Artifacts/Quality'/name/'metrics.json'
    if p.exists():station[name]=stat(p)
result={'tests':tests,'runs':runs,'station':station,'note':'Raw metrics retain all captured samples. Focused statistics are additional, explicitly labelled data. Startup excludes the first two seconds per scene. Recording runs are not interchangeable with no-capture performance runs.'}
(out/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
for name,data in runs.items():
    r=data['final'];print(name,(r.get('scope','Rail'),r.get('completed'),len(r.get('errors',[]))) if r else None)
    for m in data['metrics']:print(m['stage'],round(m['meanMs'],3),round(m['p99Ms'],3),'focus',round(m.get('focusPercent',-1),1),'slow',m['over33ms'])
