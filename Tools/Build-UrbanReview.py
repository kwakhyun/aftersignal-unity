from pathlib import Path
import json,html
root=Path(__file__).resolve().parents[1];doc=root/'Documentation/Urban'
verification=json.loads((doc/'verification.json').read_text(encoding='utf-8'));perf=json.loads((doc/'performance.json').read_text());p=perf['cityFinal']
def figure(file,title):return f'<figure><img src="Media/{file}" loading="lazy"><figcaption>{html.escape(title)}</figcaption></figure>'
content='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>AFTERSIGNAL 1.5 — 도시와 주행 검증</title>
<style>body{margin:0;background:#101b23;color:#e8f1ed;font:17px/1.7 system-ui,sans-serif}main{max-width:1320px;margin:auto;padding:48px 28px}h1{font-size:42px;line-height:1.2}h2{margin-top:64px}p{max-width:920px;color:#bccdd1}.tag{color:#eabd6e;letter-spacing:.15em}.grid{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0;background:#192b34;border:1px solid #29434e}img,video{width:100%;display:block}figcaption{padding:12px 18px;color:#c3d5d9}.cards{display:flex;gap:12px;flex-wrap:wrap}.card{padding:20px 30px;background:#20333c;min-width:180px}.card b{display:block;font-size:28px;color:#e9bf74}a{color:#7fd4cf}li{margin:8px 0}table{border-collapse:collapse;width:100%}td,th{padding:12px;text-align:left;border-bottom:1px solid #30434e}@media(max-width:780px){.grid{grid-template-columns:1fr}h1{font-size:30px}}</style><main>
<div class="tag">AFTERSIGNAL / NATIVE UNITY RELEASE 1.5</div><h1>걷는 도시에서<br>운전하는 도시로.</h1><p>근접 카메라와 3인칭 차량 시점, 고층 건물과 외곽 스카이라인, 네 차종, 신호·횡단보도, 차량 손상·폭발, 자동 메인 퀘스트 안내를 실제 씬에 적용했습니다.</p>'''
content+=f'<div class="cards"><div class="card"><b>{p["meanMs"]:.3f} ms</b>도시 평균 프레임</div><div class="card"><b>{p["p99Ms"]:.3f} ms</b>P99 프레임</div><div class="card"><b>{p["over33ms"]}회</b>33.34ms 초과</div><div class="card"><b>40 / 16</b>시설 출입구 / 실내 유형</div></div>'
content+='<p>Ryzen 5 7500F · RTX 4060 Ti · 1600×900 · D3D11 · Release · 60FPS 제한. 최종 도시 QA를 녹화 없이 측정했습니다. 최소 사양 인증이나 모든 상황의 60FPS 보장을 의미하지 않습니다.</p><h2>실제 화면 비교</h2><p>같은 해상도의 실제 게임 화면입니다. 요청에 따라 카메라 구도를 변경했으며, 주행 비교는 서로 다른 도로 위치를 포함합니다. 시민·교통은 무작위로 배치됩니다.</p><div class="grid">'
content+=figure('city-before.png','변경 전: 먼 도보 시점, 적은 건물, 고정 신호등')+figure('city-after.png','변경 후: 근접 도보 시점과 연결된 도시')
content+=figure('drive-before.png','변경 전: 고정된 원거리 차량 시점')+figure('drive-after.png','변경 후: 차량 뒤에서 전방을 바라보는 시점')+'</div><h2>차종과 피드백</h2><div class="grid">'
for i,name in enumerate(['승용차','택시 · 지붕등과 체크 띠','버스 · 긴 객실과 출입문','트럭 · 운전석과 적재함']):content+=figure(f'fleet-{i}.png',name)
content+=figure('explosion-after.png','차량 파손: 짧은 화염, 파편, 연기와 하차 복구')+figure('fuel.png','주유: 정차 후 E로 연료 보충')+'</div>'
content+='<h2>실제 실행 영상</h2><p>네이티브 렌더 화면과 게임 오디오입니다. 앞부분은 신호 한 주기 관찰, 후반은 명시적인 차종·주행·충돌 검사 배치입니다. 위치를 바꾸는 테스트 구간을 포함하며, 도시 전체를 끊임없이 주행한 영상으로 표시하지 않습니다.</p><video controls preload="metadata" src="Media/city-gameplay.mp4" poster="Media/drive-after.png"></video>'
content+='<h2>지도와 시설</h2><div class="grid">'+figure('map.png','M 지도: 메인 경로, 40개 시설, 선택한 시설의 안내')+figure('interior-14.png','본부: 기존 캠페인과 연결된 작전 단말')
for i,name in [(0,'아파트'),(1,'경찰서'),(4,'병원'),(5,'학교'),(6,'백화점'),(9,'식당'),(10,'지하철역'),(15,'카페')]:content+=figure(f'interior-{i}.png',name+' 내부')
content+='</div><h2>계단 시야 보완</h2><p>위층 발판이 주인공을 가리는 구간을 실제 실행에서 발견했습니다. 계단 구역의 카메라 높이를 낮추고 다섯 층 왕복을 다시 확인했습니다.</p><div class="grid">'+figure('stair-1.png','계단의 낮은 시선 · 뒤쪽 계단')+figure('stair-3.png','다른 높이에서도 주인공과 발판 확인')+'</div><h2>검증 범위</h2><table><tr><th>검사</th><th>결과</th></tr>'
for key,v in verification.items():content+=f'<tr><td>{html.escape(key)}</td><td>통과 · {v.get("checks",v.get("passed","실제 입력 관찰"))}</td></tr>'
content+='</table><h2>남은 품질 차이</h2><p>건물과 차량은 직접 구성한 메시·이미지 조합이며 상용 게임 수준의 수작업 에셋으로 완성됐다고 주장하지 않습니다. 실내는 16종 유형을 재사용하고, 외곽 건물의 내부·모든 층은 만들지 않았습니다. 장시간·최소 사양·다른 GPU 성능, 실제 청취 믹싱과 장시간 수동 조작감 검수는 추가 확인 대상입니다.</p><p><a href="URBAN-15.md">조작·구현·복원 방법</a> · <a href="VERIFICATION.md">검증과 성능 상세</a> · <a href="verification.json">검증 원자료</a> · <a href="changed-files.json">변경 파일과 GUID 검사</a> · <a href="sources.json">에셋 출처</a></p></main></html>'
(doc/'review.html').write_text(content,encoding='utf-8');print(doc/'review.html')
