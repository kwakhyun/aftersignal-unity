"""Copy native QA evidence into the distributable project; preserve source captures."""
from pathlib import Path
import csv, hashlib, json, shutil, statistics, subprocess, sys

root=Path(__file__).resolve().parents[1]
doc=root/'Documentation/Expansion'
for run in ('FullPerformance','FocusedPerformance','VisualRelease'):
    report=json.loads((root/f'Artifacts/Expansion/{run}/expansion.json').read_text())
    assert report['completed'] and not report['errors'] and all(c['passed'] for c in report['checks']),run
rail=json.loads((root/'Artifacts/Expansion/OriginalRelease/Haven/playthrough.json').read_text())
assert rail['completed'] and not rail['errors'] and len(rail['checks'])==4
station=json.loads((root/'Artifacts/Quality/StationRelease/playthrough.json').read_text())
assert station['completed'] and not station['errors']
subprocess.run([sys.executable,str(root/'Tools/Audit-Expansion.py')],check=True)
evidence=doc/'Evidence'; evidence.mkdir(parents=True,exist_ok=True)
media=doc/'Media'; media.mkdir(parents=True,exist_ok=True)

def copy(source,destination):
    assert source.is_file(),source
    destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,destination)

for run in ('FullPerformance','FocusedPerformance','VisualRelease','OriginalRelease'):
    folder=root/'Artifacts/Expansion'/run
    for source in folder.rglob('*'):
        if source.is_file() and source.suffix in ('.json','.csv'):
            copy(source,evidence/run/source.relative_to(folder))
for run in ('ExpansionBaseline','StationRelease'):
    for name in ('metrics.json','frames.csv','playthrough.json'):
        source=root/'Artifacts/Quality'/run/name
        if source.exists():copy(source,evidence/run/name)
for name in ('EditMode.xml','PlayMode.xml'):
    copy(root/'Artifacts'/name,evidence/name)
copy(root/'Artifacts/Expansion/geometry-batches.txt',evidence/'geometry-batches.txt')
copy(root/'Artifacts/Runtime/00-Haven.png',media/'Haven-before-1.1.png')
for stage in ('Haven-0','Archive-2','Origin-4'):
    folder=root/'Artifacts/Expansion/VisualRelease'/stage
    for name in ('entry.png','exit.png','promenade.png','rooftop.png','town-walk.png','traversal.png','combat.png','hit.png','quest.png','gameplay-with-audio.mp4'):
        source=folder/name
        if source.exists():copy(source,media/(stage+'-'+name))
    assert (media/(stage+'-gameplay-with-audio.mp4')).is_file(),stage

changes=[];guid_errors=[]
for previous,current in [('AfterSignal','Assets/AfterSignal'),('Settings','Assets/Settings'),('ProjectSettings','ProjectSettings')]:
    before=root/'Artifacts/Expansion/Before'/previous
    after=root/current
    for source in before.rglob('*'):
        if not source.is_file():continue
        target=after/source.relative_to(before)
        if not target.exists():changes.append({'path':str(target.relative_to(root)),'change':'missing'});continue
        old=source.read_bytes();new=target.read_bytes()
        if old!=new:changes.append({'path':str(target.relative_to(root)),'change':'modified'})
        if source.suffix=='.meta':
            old_guid=next((s for s in old.decode('utf-8-sig').splitlines() if s.startswith('guid:')),None)
            new_guid=next((s for s in new.decode('utf-8-sig').splitlines() if s.startswith('guid:')),None)
            if old_guid!=new_guid:guid_errors.append(str(target.relative_to(root)))
    for target in after.rglob('*'):
        if target.is_file() and not (before/target.relative_to(after)).exists():
            changes.append({'path':str(target.relative_to(root)),'change':'added'})
assert not guid_errors,guid_errors
assert not [c for c in changes if c['change']=='missing'],'Original backed-up files must remain present'
(doc/'changed-files.json').write_text(json.dumps({'existingGuidChanges':guid_errors,'changes':changes},ensure_ascii=False,indent=2),encoding='utf-8')

frames=[]
for source in (root/'Artifacts/Expansion/FocusedPerformance').rglob('frames.csv'):
    frames.extend(float(row['frame_ms']) for row in csv.DictReader(source.open()))
frames.sort()
aggregate={'scope':'FocusedPerformance: all recorded frames, all scenes, no focus filtering','samples':len(frames),'seconds':sum(frames)/1000,'meanMs':statistics.mean(frames),'p95Ms':frames[int(len(frames)*.95)],'p99Ms':frames[int(len(frames)*.99)],'maxMs':frames[-1],'over33_34ms':sum(f>33.34 for f in frames)}
(doc/'aggregate-performance.json').write_text(json.dumps(aggregate,indent=2),encoding='utf-8')
manifest=[]
for folder in (evidence,media):
    for source in folder.rglob('*'):
        if source.is_file():manifest.append({'path':str(source.relative_to(doc)),'bytes':source.stat().st_size,'sha256':hashlib.file_digest(source.open('rb'),'sha256').hexdigest()})
(doc/'evidence-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(aggregate,indent=2));print('Preserved GUIDs. Changed/added files:',len(changes),'Evidence files:',len(manifest))
