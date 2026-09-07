# 다음 Codex 작업을 위한 인계 — 2026-09-07

## 프로젝트 구분

이 저장소는 **AFTERSIGNAL의 Unity 게임 소스만** 포함한다. Unity 6000.4.0f1, URP 17.4.0, Input System 1.19.0, Windows x64를 사용한다. 상위 폴더의 `src`, `public`, `package.json`, Vite 서버는 별개인 이전 웹 버전이다. Unity 작업에서는 상위 웹 프로젝트를 수정하거나 실행하지 않는다.

현재 PC 경로: `C:\Users\82105\Documents\ChatGPT\ANNO_ Mutationem\AFTERSIGNAL-Unity`

공개 저장소: https://github.com/kwakhyun/aftersignal-unity · 브랜치: `codex/unity-source`.

GitHub 복제 후에는 `SETUP.cmd` → `OPEN_UNITY.cmd`. 정확한 버전의 SDK를 에디터 설치 경로와 Unity 공식 서버에서 복원한다. 로컬 embedded SDK는 Git에서 제외했고 `Packages/sources.json`에 버전·출처·레지스트리 체크섬을 고정했다. Assets, 씬, 이미지, 오디오, `.meta`는 저장소에 포함된다. 에디터가 다른 곳에 설치돼 있으면 `Tools/Restore-UnityPackages.ps1 -Editor <Unity.exe>`를 실행한다.

기존 ShaderGraph의 Unity 6.4 GUID 호환성 수정 등 14개 파일 차이는 `Packages/compatibility.json`으로 보존한다. 패키지 복원 후 원본 해시가 일치할 때만 적용하고, 알 수 없는 사용자 수정은 덮어쓰지 않는다. 빈 패키지 디렉터리에서 복원한 뒤 재실행 안전성과 최종 파일 체크섬을 검사했다. 결과와 핵심 네이티브 보고서는 `Documentation/Urban/Checkpoint`에 있다.

## 최근 적용 내용

- 790×660m 도시, 기존 시설 40곳/실내 16종 유지, 블록별 고층 40동과 사방 두 겹 외곽 빌딩 124동 추가.
- 가까운 도보 시점과 차량 뒤에서 전방을 보는 3인칭 추적 시점.
- 승용차·택시·버스·트럭 프리팹, 엔진/제동/충돌/폭발 음원, 차체 체력·손상·폭발·강제 하차 복구.
- 30개 교차로에서 신호·정지선·차선·횡단보도·시민 이동을 동일 도로 좌표로 연결. 66초 신호 주기, 보호된 보행 구간.
- 메인 의뢰 목적지·거리·표식·도시 지상/지도 경로.
- 기존 차량 탑승·탈취·연료·주유·시민 충돌, 학교·병원·본부 등 시설 연결.
- 무기 3종 연속 공격·장전·대시 공격·스킬, 실제 좌우 미러와 앞뒤 스프라이트, 방→승강기/계단→마을, 외벽 로프 침투 캠페인은 이전 작업에서 구현돼 있다.

## 검증한 결과

- 최신 Windows Release 빌드 성공: `Artifacts/Urban15/build8.log`.
- Unity 테스트: EditMode 19, PlayMode 32 통과. 이 실행은 마지막 계단 카메라 조정 전이다. 계단 카메라는 이후 네이티브 실행으로 확인했다.
- `Artifacts/Residence/Release15`: Rail, Expansion, Residence 모두 completed=true이며 네이티브 종료 코드 0. 기존 열차 보스 3회 코어, 후속 캠페인, 방/승강기/5층 계단 왕복, 학교·병원·본부, 외벽 4회 로프/14경비병/귀환을 통과했다.
- `Artifacts/Urban/Release15/urban.json`: 시설 16종 왕복, 실제 주행·조향·후진·하차, 차량 탈취, 시민 충돌, 주유, 차량 저장 복원 통과.
- `Artifacts/Urban15/ReleasePerf`: 도시 기능 검사 22개 통과. 정상 교통 70초 관찰에서 적색 정차 12대, 횡단 시민 40명, 시민 충돌 0, 차선 오차 약 0.95cm.
- 같은 최종 도시 측정: Ryzen 5 7500F + RTX 4060 Ti, 1600×900 D3D11 Release, 평균 17.059ms, P99 16.670ms, 33.34ms 초과 7회. 앞선 FinalPerf 실행은 초과 0회였지만 **최종 측정의 느린 프레임을 숨기거나 전체 60FPS 합격을 선언하면 안 된다**.
- 열차 종료 시 Unity 네이티브 종료 충돌이 한 번 있었다. 동일 빌드 재실행과 후속 검사에서 정상 종료했으나 원인을 해결했다고 확인하지 않았다. `Artifacts/Urban15/Regressions/Rail-shutdown`에 원본 로그가 있다.

## 다음 작업의 우선순위

1. **계단 최상단 시야**: `CameraRig.cs`의 Stairwell 시점(오프셋 .7, 2.35, -10)이 첫 네 구간 가림을 줄였지만, `Artifacts/Urban15/StairReview/Residence-0/stair-middle-4.png`에서는 6층 연결 다리가 캐릭터를 여전히 가린다. 낮은 카메라만으로 해결됐다고 표시하지 말 것. 6층 다리의 국소 가림 처리 또는 구도 보완 후 실제 캡처로 확인한다. 물리/이동 경로는 이미 통과했다.
2. **실제 키보드·마우스 확인**: 이번 도시 버전의 `-urban-manual` 모드로 WASD, 휠, LMB 사격, R 장전, E 탑승/하차, M 지도를 직접 조작하는 QA는 아직 하지 않았다. UrbanManualProbe는 관찰자이며 합성 입력을 넣지 않는다. 자동 ControlFrame 검사와 수동 조작감 평가를 구분한다.
3. **최종 느린 프레임 분석**: ReleasePerf/frames.csv의 7개 긴 프레임, 창 포커스와 부하 조건, 반복 종료를 확인한다. 장시간 교통 밀집·다른 GPU·최소 사양은 미검증이다.
4. **최종 증거와 배포본 정리**: FinalVideo의 실제 렌더 영상과 오디오를 합치고, 위 결함 해결/수동 QA 후 Collect-Urban.py → Build-UrbanVerification.py → Build-UrbanReview.py를 실행한다. 수집기는 누락·실패 자료가 있으면 중단한다. 버전 1.5 ZIP은 아직 생성하지 않았다. 기존 1.1/1.2 ZIP을 덮어쓰지 않는다.
5. **아트/오디오 잔여**: 시민 외형 다양성, 반복 건물·차체 세부 손상 아트, 실제 스피커 청취 믹싱. 현재 원본 합성 음원은 이벤트/파일 파형 위주로 검사했다. 상용 레퍼런스와 같은 품질을 달성했다는 근거는 없다.

## 보존과 실행 주의

- 기존 StageId 0–22와 저장 데이터, `.meta` GUID 유지. 도시/공용 실내는 23/24이다.
- 네이티브 QA와 Unity 테스트를 동시에 실행하지 않는다. 같은 PlayerPrefs를 사용하며 QA가 시작/종료 때 복원한다.
- 실행 중인 게임을 먼저 정상 종료한 뒤 빌드한다. 에디터/파이프라인 업그레이드, 대규모 재생성은 현재 작업에 필요하지 않다.
- `Tools/upgrade_urban15.py`는 이미 적용한 일회성 이전 스크립트다. 다시 실행하지 않는다.
- 일반 실행은 씬을 다시 만들지 않는다. 에디터 생성 메뉴는 지정된 씬을 다시 작성하므로 수동 씬 변경을 보존한다.
- `Artifacts`, `Builds`, 로컬 SDK, 녹화 파일은 Git에서 제외했다. 현재 PC에는 남아 있다. 과거 Documentation 문서들은 해당 버전의 기록이며 최신 완성도 보증이 아니다.

사용자는 다음 Codex 프로젝트 채팅에서 작업을 계속하기 위해 소스 공개 저장소 생성과 커밋·푸시를 요청했다. 이 시점의 소스는 작업 인계 기준점이며 출시 완료 태그가 아니다.
