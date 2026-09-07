"""Collect verified native evidence without altering the recorded frames."""
from pathlib import Path
import csv, json, shutil, statistics, xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
doc=root/'Documentation/Residence';media=doc/'Media';evidence=doc/'Evidence'
media.mkdir(parents=True,exist_ok=True);evidence.mkdir(parents=True,exist_ok=True)

def copy(src,dst):
    assert src.is_file(),src
    dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)

reports={
    'rail':root/'Artifacts/Residence/Release13/Rail/Haven/playthrough.json',
    'expansion':root/'Artifacts/Residence/Release13/Expansion/expansion.json',
    'residence':root/'Artifacts/Residence/Release13/Residence/residence.json',
    'residenceVideo':root/'Artifacts/Residence/Iteration5/residence.json',
    'effectsOn':root/'Artifacts/Quality/Residence13On/playthrough.json',
    'effectsOff':root/'Artifacts/Quality/Residence13Off/playthrough.json',
    'fps15':root/'Artifacts/Quality/Residence13Low/playthrough.json',
    'fps120':root/'Artifacts/Quality/Residence13High/playthrough.json',
}
results={}
for label,path in reports.items():
    data=json.loads(path.read_text());assert data['completed'] and not data['errors'],(label,data)
    assert all(c['passed'] for c in data['checks']),label
    results[label]={'completed':True,'checks':len(data['checks']),'errors':[]}
    copy(path,evidence/(label+'.json'))
for name in ('EditMode','PlayMode'):
    path=root/f'Artifacts/{name}.xml';test=ET.parse(path).getroot();assert test.get('result')=='Passed'
    results[name]={'passed':int(test.get('passed')),'failed':int(test.get('failed'))};copy(path,evidence/path.name)
for run,file in [('FocusedResidence','residence.json'),('FocusedTower','expansion.json')]:
    folder=root/'Artifacts/Residence'/run
    data=json.loads((folder/file).read_text());assert data['completed'] and not data['errors']
    results[run]={'completed':True,'checks':len(data['checks']),'errors':[]}
    for path in folder.rglob('*'):
        if path.is_file() and path.suffix in ('.json','.csv'):copy(path,evidence/run/path.relative_to(folder))

perf={}
for scope in ('Rail','Expansion','Residence'):
    folder=root/'Artifacts/Residence/Release13'/scope
    rows=[];stages=[]
    for path in folder.rglob('frames.csv'):
        rows.extend(list(csv.DictReader(path.open())));copy(path,evidence/'Release13'/scope/path.relative_to(folder))
    values=sorted(float(r['frame_ms']) for r in rows)
    assert values
    for path in folder.rglob('metrics.json'):
        value=json.loads(path.read_text());assert value['build']=='Release';stages.append({'stage':path.parent.name,**value});copy(path,evidence/'Release13'/scope/path.relative_to(folder))
    cpu=[float(r['CPU Main Thread Frame Time']) for r in rows if float(r['CPU Main Thread Frame Time'])>=0]
    perf[scope]={'frames':len(values),'seconds':sum(values)/1000,'meanMs':statistics.mean(values),'p95Ms':values[int(len(values)*.95)],'p99Ms':values[int(len(values)*.99)],'maxMs':values[-1],'over33ms':sum(v>33.34 for v in values),'unfocusedFrames':sum(float(r['focused'])==0 for r in rows),'cpuMainMeanMs':statistics.mean(cpu) if cpu else None,'stages':stages}
(doc/'performance.json').write_text(json.dumps(perf,indent=2),encoding='utf-8')
(doc/'verification.json').write_text(json.dumps(results,indent=2),encoding='utf-8')

files={
 'home-before.png':'Artifacts/Residence/Iteration2/Residence-0/entry.png',
 'home-after.png':'Artifacts/Residence/Iteration5/Residence-0/entry.png',
 'town-before.png':'Artifacts/Residence/TownBefore/Haven-0/entry.png',
 'town-after.png':'Artifacts/Residence/VisualTown/Haven-0/entry.png',
 'town-depth.png':'Artifacts/Residence/VisualTown/Haven-0/cross-street.png',
 'town-east.png':'Artifacts/Residence/VisualTown/Haven-0/east-avenue.png',
 'school-before.png':'Artifacts/Residence/Iteration5/School-2/classroom.png',
 'school-after.png':'Artifacts/Residence/VisualSchool/School-0/classroom.png',
 'clinic.png':'Artifacts/Residence/VisualClinic/Clinic-0/treatment.png',
 'hq.png':'Artifacts/Residence/VisualHeadquarters/Headquarters-0/mission-locked.png',
 'window.png':'Artifacts/Residence/Iteration5/Breach-5/window-breach.png',
 'climb.png':'Artifacts/Residence/Iteration5/Breach-5/climb-2.png',
 'left.png':'Artifacts/Residence/Iteration5/Residence-0/left-walk-0.png',
 'front.png':'Artifacts/Residence/Iteration5/Residence-0/front-walk-0.png',
 'back.png':'Artifacts/Residence/Iteration5/Residence-0/back-walk-0.png',
 'home.mp4':'Artifacts/Residence/Iteration5/Residence-0/gameplay-with-audio.mp4',
 'breach.mp4':'Artifacts/Residence/Iteration5/Breach-5/gameplay-with-audio.mp4',
 'combat-before.mp4':'Artifacts/Quality/BeforeFinal/gameplay-with-audio.mp4',
 'combat-on.mp4':'Artifacts/Quality/Residence13On/gameplay-with-audio.mp4',
 'combat-off.mp4':'Artifacts/Quality/Residence13Off/gameplay-with-audio.mp4',
 'boss.mp4':'Artifacts/Residence/RailVisual/Roof/gameplay-with-audio.mp4',
}
for name,path in files.items():copy(root/path,media/name)
for path in (root/'Artifacts/Residence/Iteration5/Residence-0').glob('gpu-*'):copy(path,evidence/path.name)

changes=[];guid_errors=[]
for before_name,after_name in [('AfterSignal','Assets/AfterSignal'),('Settings','Assets/Settings'),('ProjectSettings','ProjectSettings')]:
    before=root/'Artifacts/Residence/Before'/before_name;after=root/after_name
    for path in before.rglob('*'):
        if not path.is_file():continue
        target=after/path.relative_to(before);assert target.exists(),target
        old,new=path.read_bytes(),target.read_bytes()
        if old!=new:changes.append({'path':target.relative_to(root).as_posix(),'change':'modified'})
        if path.suffix=='.meta':
            guid=lambda data:next((s for s in data.decode('utf-8-sig').splitlines() if s.startswith('guid:')),None)
            if guid(old)!=guid(new):guid_errors.append(str(target))
    for path in after.rglob('*'):
        if path.is_file() and not (before/path.relative_to(after)).exists():changes.append({'path':path.relative_to(root).as_posix(),'change':'added'})
assert not guid_errors,guid_errors
(doc/'changed-files.json').write_text(json.dumps({'existingGuidChanges':guid_errors,'changes':changes},ensure_ascii=False,indent=2),encoding='utf-8')

def pair(title,a,b,note):
    return f'<section><h2>{title}</h2><p>{note}</p><div class="pair"><figure><img src="Media/{a}"><figcaption>변경 전</figcaption></figure><figure><img src="Media/{b}"><figcaption>변경 후 · 실제 게임 캡처</figcaption></figure></div></section>'
def video(title,path):return f'<figure><video controls preload="metadata" src="Media/{path}"></video><figcaption>{title}</figcaption></figure>'
table=''.join(f'<tr><td>{k}</td><td>{v["frames"]:,}</td><td>{v["meanMs"]:.3f} ms</td><td>{v["p99Ms"]:.3f} ms</td><td>{v["over33ms"]}</td></tr>' for k,v in perf.items())
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>AFTERSIGNAL 1.3 / Play Review</title><style>
*{box-sizing:border-box}body{margin:0;background:#0a1218;color:#dce6e5;font:16px/1.65 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:48px 24px}h1{font-size:44px;margin:8px 0;color:#efdaa9}h2{font-size:23px}p{color:#a9c0c2;max-width:1000px}a{color:#86dfd0}section{margin:56px 0}figure{margin:0;background:#101e26}figcaption{padding:10px 16px;color:#a7c4c6;font-size:14px}img,video{width:100%;display:block}img{cursor:zoom-in}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}.triple{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}.toolbar{position:sticky;top:0;padding:12px;background:#15232bef;display:flex;gap:8px;flex-wrap:wrap;z-index:1}button,select{background:#243c46;color:#dce8e5;border:1px solid #42606c;border-radius:4px;padding:8px 13px;cursor:pointer}.gray img,.gray video{filter:grayscale(1)}.small .pair,.small .triple{max-width:900px}.tag{letter-spacing:3px;color:#83d0c3}table{border-collapse:collapse;width:100%}td,th{padding:12px;text-align:left;border-bottom:1px solid #2d414a}dialog{padding:0;border:0;background:#000;max-width:98vw;max-height:96vh}dialog img{width:auto;max-width:96vw;max-height:90vh;object-fit:contain}dialog::backdrop{background:#000c}@media(max-width:850px){.pair,.triple{grid-template-columns:1fr}h1{font-size:32px}}</style>
<main><div class="tag">AFTERSIGNAL / 1.3</div><h1>방에서 도시로, 외벽에서 기록실로</h1><p>실제 Windows 플레이어 캡처와 무편집 게임 영상입니다. 최종 아트 검수는 아직 남아 있습니다. 좌우 렌더링 오류·NPC 비율·앞뒤 이동·무기 동작·시민 시설·외벽 경로를 적용하고 검증했습니다.</p>
<nav class="toolbar"><button onclick="document.body.classList.toggle('gray')">흑백 비교</button><button onclick="document.body.classList.toggle('small')">축소 화면</button><button onclick="document.querySelectorAll('video').forEach(v=>v.pause())">영상 모두 정지</button><select onchange="document.querySelectorAll('video').forEach(v=>v.playbackRate=Number(this.value))"><option value="1">정상 속도</option><option value="0.5">0.5배 느린 재생</option><option value="0.25">0.25배 포즈 확인</option></select><button onclick="document.querySelectorAll('video').forEach(v=>v.muted=!v.muted)">소리 ON / OFF</button><a href="VERIFICATION.md">검증·타이밍·남은 차이</a></nav>
'''
html+=pair('생활 공간','home-before.png','home-after.png','동일 시작점. 가구 표면·나무 바닥·창문광과 HUD를 수정했고 방 카메라를 더 가깝게 조정했습니다. 카메라까지 동일한 A/B로 주장하지 않습니다.')
html+=pair('도시와 인물의 크기','town-before.png','town-after.png','같은 마을 진입 경로의 실제 화면. 투명 여백 때문에 작던 NPC를 몸체 기준으로 수정했습니다.')
html+=pair('시설 내부','school-before.png','school-after.png','같은 교실 기록 지점. 반복 벤치 대신 의자·책상·서가, 전경 바닥과 재질을 정리했습니다.')
html+='<section><h2>좌측 · 정면 · 후면</h2><p>별도의 방향 스프라이트와 실제 GPU 반전을 확인했습니다. PNG 확대는 원본 화면을 보여 줍니다.</p><div class="triple">'+''.join(f'<figure><img src="Media/{p}"></figure>' for p in ('left.png','front.png','back.png'))+'</div></section>'
html+='<section><h2>3D 거리와 시설</h2><div class="pair">'+''.join(f'<figure><img src="Media/{p}"><figcaption>{t}</figcaption></figure>' for p,t in [('town-depth.png','앞뒤 거리'),('town-east.png','동쪽 가로와 차량'),('clinic.png','시민 병원'),('hq.png','신호 복원 본부')])+'</div></section>'
html+='<section><h2>무편집 실행 영상</h2><p>자동 QA가 일반 ControlFrame 경계로 움직입니다. 로프·이동·전투를 순간이동으로 대체하지 않았습니다. 이것만으로 사람의 주관적인 입력감을 검증한 것은 아닙니다.</p><div class="pair">'+video('방 탐색 · 세 무기 · 장전 · 승강기 · 계단','home.mp4')+video('4회 등반 · 유리 파괴 · 14명 전투 · 기록','breach.mp4')+video('기존 품질 구간 Before','combat-before.mp4')+video('현재 전투 / 효과 ON','combat-on.mp4')+video('현재 전투 / 효과 OFF','combat-off.mp4')+video('열차 지붕 / 보스 목표 안내','boss.mp4')+'</div></section>'
html+='<section><h2>최종 릴리스 프레임 측정</h2><p>Ryzen 5 7500F · RTX 4060 Ti · Windows · D3D11 BitBlt · 1600×900 · 60 FPS 제한. 녹화·캡처 OFF, 각 씬 첫 2초 제외. 로딩 프레임과 최소 사양 인증은 포함하지 않습니다.</p><table><tr><th>경로</th><th>측정 프레임</th><th>평균</th><th>P99</th><th>33.34ms 초과</th></tr>'+table+'</table><p><a href="performance.json">원본 수치</a> · <a href="verification.json">경로·테스트 결과</a> · <a href="changed-files.json">변경 파일과 GUID 보존 검사</a></p></section>'
html+='''<section><h2>남은 제작 차이</h2><p>앞뒤 걷기는 방향당 4프레임이며 시민 루프도 짧습니다. 수작업으로 발디딤·스카프 후행을 다듬고, 상점마다 다른 실루엣과 재질을 더 제작해야 합니다. 일부 소품과 원경은 단순한 3D 형태·그림 판입니다. 실제 오디오 출력은 동봉했으나 사람의 청취 믹스 검수와 최소 사양·초광폭 검증은 남았습니다.</p></section></main><dialog onclick="this.close()"><img></dialog><script>document.querySelectorAll('main img').forEach(i=>i.onclick=()=>{let d=document.querySelector('dialog');d.querySelector('img').src=i.src;d.showModal()});</script></html>'''
(doc/'review.html').write_text(html,encoding='utf-8')
print(json.dumps({'checks':results,'performance':{k:{a:b for a,b in v.items() if a!='stages'} for k,v in perf.items()},'existingGuidChanges':len(guid_errors)},indent=2))
