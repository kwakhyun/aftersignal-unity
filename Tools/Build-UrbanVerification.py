"""Render a concise verification note from collected native results."""
from pathlib import Path
import json, shutil
root=Path(__file__).resolve().parents[1];doc=root/'Documentation/Urban'
v=json.loads((doc/'verification.json').read_text(encoding='utf-8'))
perf=json.loads((doc/'performance.json').read_text(encoding='utf-8'))
audio=json.loads((doc/'audio.json').read_text(encoding='utf-8'))
city=json.loads((doc/'Evidence/cityFinalPerformance.json').read_text(encoding='utf-8'))
rows=['| 검사 | 완료한 항목 |','|---|---|']
for name,result in v.items():
    rows.append(f'| {name} | {result.get("checks",result.get("passed","실제 장치 입력 관찰"))} · 통과 |')
measure=['| 실행 | 프레임 수 | 평균 ms | P99 ms | 최대 ms | 33.34ms 초과 |','|---|---:|---:|---:|---:|---:|']
for name,p in perf.items():measure.append(f'| {name} | {p["frames"]} | {p["meanMs"]:.3f} | {p["p99Ms"]:.3f} | {p["maxMs"]:.3f} | {p["over33ms"]} |')
regressions=root/'Artifacts/Urban15/Regressions'
if regressions.exists():shutil.copytree(regressions,doc/'Evidence/Investigations',dirs_exist_ok=True)
text=f'''# AFTERSIGNAL 1.5 실행 검증 — 2026-09-07

블록 고층 건물·사방 외곽, 근접 도보/차량 추적 카메라, 승용차·택시·버스·트럭, 신호와 도로 이미지 정렬, 차량 손상·폭발, 메인 의뢰 안내를 Windows 실행 파일에 적용했습니다. 기존 40개 시설과 16종 내부, 캠페인 진행 및 저장 인덱스를 유지했습니다.

## 실제 검증 결과

{chr(10).join(rows)}

최종 도시 관찰에서 적색 신호에 정차한 차량 {city['redStops']}대, 보호된 횡단 구간을 이용한 시민 {city['pedestrianCrossings']}명을 확인했습니다. 관찰한 차량 경로 오차 최대 {city['maxLaneError']*100:.2f}cm, 일반 교통의 시민 충돌 {city['ambientImpacts']}회입니다. 70초 관찰과 다음 검사 배치를 통과한 결과이며 모든 교통 상황을 수학적으로 증명한 결과는 아닙니다.

도시 주행 검사는 실제 이동 입력으로 50m 이상 가속, 추적 카메라 방향, 제동·연료 감소·하차를 확인합니다. 충돌 장애물에 직접 운전해 체력 감소를 확인하고, 마지막 파괴 검사는 명시적인 체력 0 배치로 폭발 1회·플레이어 복귀·재탑승 금지를 확인합니다. 시민·차량 탈취·주유·실내 검사는 별도 UrbanSmoke 실행입니다. 퀘스트 고정 NPC는 충돌 가능한 시민 풀과 분리돼 있습니다.

직접 입력 검증은 Windows 게임 창에 WASD, 마우스 클릭과 휠, R, E, M을 전달하고 실제 Input System 수신·발사·장전·차량 탑승을 관찰했습니다. 자동 경로 입력과 구분했으며 짧은 키 누름으로 장시간 운전의 손맛까지 평가했다고 주장하지 않습니다.

## 성능과 조건

Ryzen 5 7500F / RTX 4060 Ti / 1600×900 / D3D11 / Unity 6000.4.0f1 / URP 17.4.0 / Windows x64 Mono Release / vSync 0 / 60FPS 제한. 최종 cityFinal은 녹화 없이 측정했고 첫 2초는 제외했습니다. 로딩·검사 위치 재배치와 실제 이동은 프레임 원자료의 조건을 함께 확인해야 합니다.

{chr(10).join(measure)}

cityBefore는 도시 확장 전의 짧은 검사 기록으로 카메라와 이동 시퀀스가 다릅니다. 같은 비용의 전후 벤치마크로 보거나 개선율을 계산하면 안 됩니다. 전체 캠페인의 합산에는 씬 전환 직후 비용과 의도된 상호작용이 포함됩니다. GPU 평균과 CPU 스레드 비용은 performance.json 및 Evidence/Performance의 기록에서 확인할 수 있습니다. Release에서 제공되지 않은 GC·배치 카운터는 -1(미측정)로 남겼습니다. 최소 사양·다른 GPU·장시간 실행의 성능 합격은 선언하지 않습니다.

## 비교와 발견한 결함

1. 첫 시안에서 택시의 차체가 빠진 Prefab 연결 문제를 실제 캡처로 발견해 원본 연결을 해제한 뒤 독립 차체와 충돌체로 생성했습니다.
2. 먼 풍경이 잘려 검게 보이던 카메라 원거리 범위를 도시에서 1100m로 늘리고, 외곽 건물을 두 겹으로 배치했습니다.
3. 시민이 코너 한 점에 모이던 초기 배치를 보도 선분으로 분산하고, 신호 잔여 시간에 맞춰 횡단 시작을 제한했습니다.
4. 시작 차량이 횡단보도를 막던 위치를 주차 구역으로 옮겼습니다. 휠이 타이어 안에 숨던 문제와 불투명 사각형 연기를 수정했습니다.
5. 고층 저층부의 이미지가 벽 뒤에 가리던 위치를 수정하고 이동 구역에 걸친 외곽 건물에 충돌체를 적용했습니다.
6. 새 3D 도시까지 평면 배경 PNG를 요구하던 기존 에셋 검사 조건을 실제 입면·재질 검사로 고쳤습니다.
7. 열차 보스의 코어가 예약된 공격으로 즉시 소모되면 QA가 충전 비트만 보고 실패하던 관찰 시점을 수정했습니다. 실제 로프 부착과 코어 타격 사건을 함께 확인합니다.
8. 최종 직접 화면 확인에서 비상계단의 위쪽 발판이 플레이어를 가리는 문제를 발견했습니다. 계단 구역에서 낮고 가까운 시선으로 전환해 첫 네 구간은 개선됐지만, 마지막 연결 다리 가림은 남아 있습니다. HANDOFF.md의 후속 시야 검증 결과를 확인해야 합니다.

첫 열차 회귀 실행은 마을 복귀 성공 후 Unity 종료 처리에서 네이티브 충돌이 1회 발생했습니다. 같은 빌드의 재실행에서는 정상 종료됐습니다. 코드 원인이나 영구 해결을 확인한 것은 아니며 해당 로그를 Evidence/Investigations/Rail-shutdown에 보존했습니다. 출시 전 반복 종료 검증 대상으로 남깁니다.

Iteration1 → Iteration2 → FinalPerf → ReleasePerf/FinalVideo 순서의 실제 실행 자료를 보존했습니다. 대표 화면은 서로 다른 카메라 설정과 일부 다른 도로 위치를 포함합니다. 캐릭터·시민·교통의 무작위 배치를 동일한 장면이라고 표시하지 않습니다.

## 영상과 오디오

Media/city-gameplay.mp4는 실제 URP 렌더와 게임 오디오입니다. 앞부분 신호 관찰 뒤 차종·주행·충돌 검사 배치로 이동합니다. 위치 변경 배치를 포함하며 자연스럽게 전 구역을 연속 운전한 영상은 아닙니다. 렌더 캡처는 30fps이며 정상 게임의 60FPS 측정과 별도입니다.

차량 4종 엔진·제동·문·충돌·폭발은 프로젝트에서 제작한 원본 합성 WAV를 사용합니다. 기록 오디오 {audio['seconds']:.2f}초 / {audio['sampleRate']}Hz / {audio['channels']}채널, 피크 {audio['peak']:.4f}, 클리핑 표본 {audio['clippedSamples']}개입니다. 이벤트 연결과 파일 파형은 확인했으나 실제 스피커 청취에 따른 믹싱 완성도는 미평가입니다.

## 재현과 배포

- 일반 실행: 프로젝트의 PLAY.cmd 또는 Builds/Windows/AFTERSIGNAL.exe. 새 게임은 서하의 방에서 시작하며 마을 동쪽 도시대로로 시내에 진입합니다.
- 현재 도시 검사: Tools/Test-CityUpgrade.ps1 -Label Review. 영상은 -Video 추가.
- 시설·주유·탈취 검사: Tools/Test-Urban.ps1. 기존 이야기 검사: Tools/Test-ReleaseRoutes.ps1.
- Unity 검사: Tools/Test-Project.ps1. 같은 저장소를 사용하는 게임과 에디터 검사를 동시에 실행하지 않습니다.
- 자료 수집: Tools/Collect-Urban.py → Tools/Build-UrbanVerification.py → Tools/Build-UrbanReview.py. 원본 실행 결과가 없거나 실패하면 수집이 중단됩니다.
- 버전별 Windows/Unity ZIP은 이전 파일을 덮어쓰지 않고 별도 생성합니다. 각 manifest.json에서 모든 파일의 바이트 검증과 SHA-256을 확인할 수 있습니다.

## 범위와 남은 품질 차이

이번 요청의 기능은 적용·검증했습니다. 시설은 16종 내부 구성을 재사용하며 추가 고층 건물은 외형입니다. 건축·차량은 원본 메시와 이미지 조합으로, 상용 레퍼런스 수준의 수작업 아트라고 주장하지 않습니다. 우선 남은 과제는 장시간 교통 밀집·종료 반복 검증, 시민 외형 다양화, 차체 세부 손상 아트, 실제 청취 믹싱입니다. 경찰 추격이나 경제 시스템은 이번 구현 범위에 포함하지 않습니다.

변경 경로 전체와 GUID 비교는 changed-files.json에, 제작 출처는 sources.json에 있습니다. 기존 에셋 GUID 변경은 0개입니다. 되돌리기는 URBAN-15.md의 보존 범위를 확인하세요. Artifacts/Urban15/Before는 일부 1.4 핵심 소스의 사본이며 전체 1.4 프로젝트 백업으로 표시하지 않습니다.
'''
(doc/'VERIFICATION.md').write_text(text,encoding='utf-8')
print(doc/'VERIFICATION.md')
