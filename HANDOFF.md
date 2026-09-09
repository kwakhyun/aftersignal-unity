# Current handoff · 2026-09-09 · facility response and public source

- Latest validated player: Builds/FacilityResponse/AFTERSIGNAL.exe; PLAY.cmd follows Builds/active-player.txt.
- Added FacilityParking: two local dedicated fire/police/ambulance bays per station, generic parking exclusions, safe bay checks, cooldown refill, and preservation of player/active/burning vehicles. Fire service requisitions waiting engines; distant traffic cleanup no longer deletes en-route fire engines. Corrected fire-body/lightbar lifecycle and chassis.
- Strong motorcycle/car collisions launch both bike and rider a bounded distance. Player vehicle ownership is released safely during Drive; ground settling, traffic and drift pause during flight. NPC ejections preserve driver identity. Bike regains upright control after landing.
- Main quest navigation now uses live objectives and interaction points, active combat/extraction targets, and the current small-house entrance. Road-edge attachment prevents junction backtracking; the world pin follows route progress.
- NPC-first conversation removes the fabricated default Seoha opener. HTTP payload includes opening mode and nearby place/time/injury/danger context. The gateway generates the NPC opening, with local contextual fallback on connection failure.
- README rewritten against current controls and implementation, with three inspected native gameplay captures in Documentation/Screenshots.
- IMPORTANT PUBLIC POLICY: 20,546 visual asset paths were removed from the Git index, all 20,546 retained locally. Reusable images/models, Geometry/WorldAssets, authored scenes/prefabs and metadata must not be re-added. Tools/public-assets.cjs, .gitignore, local pre-commit hook and CI enforce the current policy. Earlier public Git history is NOT rewritten and may still contain old assets. Fresh checkouts require the matching private asset pack; see Documentation/PRIVATE-ASSETS.md.
- Verification: native FacilityResponseProbe 15 passed / 0 errors; Windows release 0 errors / 91 existing warnings. Offline gateway opener/follow-up and public asset boundary checks passed. Evidence: Documentation/FacilityResponse/result.json and build-result.json. Did not repeat old diagnostic suites or use live API credentials. Screenshots exclude synthetic collision fixtures and the obscured Nereid overview.

---

# 다음 Codex 작업을 위한 인계 — 2026-09-09

## 최신 작업: 카메라 복구 · 도시 선택 리스폰 · 선박/차량 경로 · 군부대/폭격기

활성 실행 파일은 `Builds/DefenseMobility/AFTERSIGNAL.exe`, `PLAY.cmd`는 `Builds/active-player.txt`를 따른다. 이번 요청에는 커밋·푸시가 없으므로 수행하지 않았다. 이전 노아/타이틀/주변 시뮬레이션/도시 안전 수정과 생성 이미지를 모두 보존했다.

CameraRig는 평소 기존 어깨 오프셋(.75m), 45도 화각, .15m near clip, 보간된 카메라 위치에서 Focus를 바라보는 회전을 복구했다. 추가 focus/겹침 보정, 근접 주인공 숨김과 화각 확대는 WallClimbing 상태에서만 적용한다. 사망 화면은 4개 도시 선택 후 확인 방식이며 기본은 애프터라이트 서하의 집이다. 다른 도시는 RespawnNetwork가 FourCityWorld 생성 완료와 안전한 바닥을 기다린다. 그 동안 게임 입력을 막는다. 도시 재진입 시 sim.Prompt가 초기화되지 않아 HUD가 예외를 내던 문제를 빈 문자열 초기화와 null-safe 검사로 수정했다. 플레이 HUD의 7개 무기 목록은 제거했고 현재 장착 무기 패널은 유지한다.

VehicleNavigator는 정적 장애물/지면을 실제 크기로 검사하는 제한 A*를 일반 교통과 ResponseDrive에 공유한다. 최대 850노드, 프레임당 1검색, 경로 재사용. 도로 그래프도 최소 힙을 적용했다. SeaTraffic은 모든 수상 탈것에 등록되며 선체 방향별 투영/이동 구간 충돌/회전 취소/초기 겹침 분리와 공통 회피를 제공한다. 수동 조종 배는 자동 옆이동 없이 충돌만 제한한다.

GarrisonSupport는 기존 군 시설 병력에만 붙으며 기존 생활/경비 루틴을 잠시 중단하고 근처 위협에 사격 지원한 뒤 복귀한다. 공간 인덱스 0.8초 탐색, 테러는 군 대응 제외. MilitaryArmory는 루멘 육군기지·공군·해군에 물리 진열대를 제공한다. E로 13종 무기 장착/탄약 보급, 군사시설 서비스 메뉴에서도 선택 가능하다. 추가 비용을 내는 일반 무기상점은 유지한다.

CityVehicleType.Bomber는 기존 enum 뒤에 추가하여 저장 호환을 유지한다. BomberAirframe/BomberBay는 별도 비행익 메시/충돌/2인 조종석/투하창/공군 승무원, 공군 기지 2대, 적재 96발, 우클릭 24발 연속 투하, 착륙 후 R 8초 보급을 제공한다. FallingBomb는 중력 낙하/충돌 폭발하며 전역 활성 수 72 제한. NPC FlyRun은 탄도 투하점과 진입·이탈 항로를 사용한다. 조종자 입력 필터에도 Bomber를 군용 무장 기체로 추가했다.

에레보스 사건은 최대 5마리의 분산된 잠식체를 한 사건 슬롯에 등록한다. 출동군은 트럭 4대/전차 7대/헬기 5대/전투기 3대/폭격기 2대, 30초 준비 후 7초 간격이다. 일반 도시는 기존 편성과 준비시간 유지. ResponseDispatch의 에레보스 출동지 제외 조건을 목표 도시가 에레보스이면 허용하도록 고쳤다. 기존 240/420/550m 사건 시작/전투/정리 범위, 도입 미션 보호, 단일 사건 제약을 유지한다.

최종 Windows 빌드 성공(오류 0, 기존 경고 91). `-defense-mobility-probe` 네이티브 필수 검증 29개 모두 통과/실행 오류 0. 카메라 회전, 사망 UI 선택, 실제 네레이드 재로드/생존/바닥, 일반 차량과 출동 트럭의 물리 우회 도착, 기존 군인 지원 사격/복귀, 선박 분리/이동 관통 방지/저속 회피, 13종 무기, 플레이어 폭격기 조종석 탑승/24발 실제 투하·폭발, NPC 탄도 투하, 5마리 동시 출현을 확인했다. 초기 테스트에서 바운드 경계의 기대값과 이미 등록된 선박을 서로 통과하도록 재배치하는 잘못된 테스트 준비를 수정했다. UI 재진입 예외는 실제 구현에서 수정했다. 결과/캡처/설명은 `Documentation/DefenseMobility`에 있다. 이 진단은 사용자의 진행/재화를 저장하지 않는다. 전체 교차로나 캠페인/장시간 최대 교전의 전수 테스트는 하지 않았다.


## 최신 작업: NPC 교통사고 추격 · 자연 회복 · 해안/외벽 충돌 · 중력 포획 · 소방 구조

실행 파일은 `Builds/CitySafetyActions/AFTERSIGNAL.exe`, PLAY.cmd는 `Builds/active-player.txt`를 따른다. 사용자가 실행 중인 LocalSimulation 게임(PID 92084)은 종료하지 않았다. 이번 요청에는 커밋·푸시가 없다. 이전 노아/타이틀/주변 시뮬레이션 수정 등 모든 기존 작업을 보존했다.

TrafficOffense는 NPC 차량 보행자 충돌을 서하 수배와 분리해 신고/경찰차 추격/운전자 정차·하차로 연결한다(3초 신고, 사건 2건·경찰차 각 2대). 몬스터 진압을 우선한다. PlayerRecovery는 8초 무피해 후 초당 2.5 HP 회복. ShoreAccess는 해안 지하 진입 차단/수면 등반/기존 침투 복구, 실제 모래 해안 경계와 물 판정 정합, 수중도시 통로 예외를 처리한다. DistrictTower의 외형 62% 코어를 폐기하고 CityGeometry.SolidPrism의 실제 외벽·후퇴·쌍둥이 타워를 따르는 건물당 하나의 정적 충돌 메시로 교체했다.

CivilianImpact.Blast는 군인·경찰의 넉백을 약 8m 이하로 제한하고 중첩 재발사를 막는다. 자동차 고속 충돌은 별도 Launch를 유지한다. 몬스터 출동군은 탱크 4·전투헬기 3·전투기 1·트럭 2로 순차 투입. TitanGravitySnare는 260m 내 탑승 항공기를 2.6초 예고 후 최대 4.5초 동안 당겨 VehicleFailure 추락으로 전환한다. 지상 적 어그로와 항공 차단 대상을 분리하고, 빈 항공기 제외/몬스터 사망 시 취소/조종·자동항로 경쟁 차단/F 탈출을 적용했다.

소방서는 실제 Door(2)를 기준으로 출동한다. FireEngine은 방수 시야 차단 시 위치 변경, 진압·구조 담당 분리, 구조 완료와 승무원 복귀 대기, 5대 제한을 유지한다. FireRescue는 시민·중상자를 안전지대까지 구조하며 경상자 응급처치/중상자 안정화·구급 신고를 한다. 기존 EMS 예약을 공유하고 EmergencyAmbulance가 소방 구조 중 환자를 동시에 옮기지 않게 FireRescueClaim으로 인계한다.

대화 인물 21명(SeoDialogue/CoreCast/WorldCast)의 배경을 imagegen으로 분리했다. 알파 대신 체크무늬가 나온 첫 생성은 사용하지 않았고, 순색 키 배경으로 재생성한 원본을 PortraitAlpha가 실제 RGBA PNG로 변환했다. 검은색은 마스킹 기준이 아니다. 머리카락 안쪽 음영의 잔여 키 색은 순색 내부 공간에서 연결된 배경을 추가 탐색해 제거한다. 생성 프롬프트·산출물 위치와 동작 설명은 Documentation/CitySafetyActions에 있다.

최종 Windows 빌드 오류 0/기존 경고 91, 별도 네이티브 필수 확인 29개 통과/실행 오류 0. 지상 공격자 어그로 중에도 자동 중력 포획이 선택되는 경우와 초상화 내부 머리카락 경계 마감까지 반영한 결과다. 최종 결과는 Documentation/CitySafetyActions/result.json 및 build-result.json과 PNG 캡처 3개를 참고한다. 진단은 -city-safety-actions-probe로만 작동하며 저장 파일을 쓰지 않는다. 전체 도시의 모든 내부/전체 캠페인을 전수 검증하지는 않았다.

## 최신 작업: 주변 사건 시뮬레이션 · 원격 알림 차단 · 갈고리/벽 카메라

실행 대상은 `Builds/LocalSimulation/AFTERSIGNAL.exe`이며 `PLAY.cmd`에 반영했다. 이전 노아 지원/타이틀 변경을 포함하며 기존 로컬 수정도 유지했다. 사용자 실행 중인 `Builds/NoaSupport` 게임은 종료하지 않았다. 새 버전은 기존 게임을 닫고 PLAY.cmd로 다시 실행해야 한다. 이번 요청에는 커밋·푸시가 없었다.

거리와 무관하게 전 해역에서 진행되던 SeaCombat의 탐색/사격 및 공통 사건 슬롯 우회를 수정했다. LocalSimulation: 사건 시작 240m, 전투 420m, 이탈 정리 550m 기준. 원격 경찰/군인/로봇/갱단/몬스터의 전투 판단을 쉬고 원격 구조/소방 사건을 만들지 않는다. 여객 이동과 출동 경로는 유지한다. 몬스터 이탈 유예 12초 동안 슬롯을 유지하고 중도 종료된 생존 몬스터를 바로 제거해 사건 재중첩을 막는다. VehicleFailure 탈출 안내는 현재 탑승 차량만, ToastNear는 표시 중 이탈해도 숨긴다.

CameraOcclusion은 100m 이내 시각 후보를 캐시하고 복원 완료 대상을 추적 사전에서 제거한다. 원거리/변화 없는 수목 업로드, 파편/차량 연기/일시 효과 부담을 줄였다. 바닥/도로/건물 컬링 정책은 변경하지 않았다. 구형 사격 직선/경찰 헬기 예고선을 제거하고 현행 움직이는 탄도/섬광/탄착을 유지한다.

갈고리는 탐험 160m, 우클릭 연결 유지/다시 클릭 해제, 자동 감기, W 가속/S 풀기/SPACE 도약이다. 좁은 조준 보조 및 클릭 버퍼, 12Hz 후보 검색. 벽 근처 카메라 초점/충돌/침투를 보정하고 시야각 확대 및 가까운 서하 가림을 적용한다. 마우스 회전과 캐릭터 가시성 복구를 확인했다.

최종 Windows 빌드 오류 0, 경고 91개. `-local-simulation-probe` 필수 동작 20개 통과, 실행 오류 0. 가까운 해상 교전 5회 발포, 멀어진 뒤 추가 0회. 사건 중첩 차단/중도 정리, 원격 알림, 탑승자 경고, 로프 연결/해제/보조, 카메라 벽 충돌/회전/복원을 확인했다. 시내 캡처에서 도로/건물 유지 확인. 첫 검사에서 지역 안내가 시험 알림을 교체한 문제는 검사 순서를 수정했고 초기 기록을 Artifacts/LocalSimulation/FirstRun에 남겼다. 장시간 FPS/전체 캠페인 검증은 반복하지 않았으며 모든 멈춤 제거를 보장하는 측정은 아니다. 상세 범위와 최종 결과/캡처는 `Documentation/LocalSimulation`에 있다.

## 최신 작업: 노아 수배 해제 지원 · 대화 상반신 확대

수배가 발생하면 노아가 12쌍의 무작위 무전 중 하나로 지원하고 약 4초의 플레이 시간 뒤 기존 WantedSystem.Clear 경로로 수배/추격/대기 신고를 해제한다. 단계 상승은 타이머를 연장하지 않는다. 완료 무전은 5초 표시하며 직전 대사 중복을 피한다. 일시정지/대화/타이틀/전환 중에는 진행하지 않고, 외부 해제·사망·체포는 무전을 취소한다. API 호출 없이 작동하며 이미 수감된 플레이어를 석방하지 않는다.

StoryPortraits.Bust가 원본 텍스처를 공유하며 얼굴·상반신 구도를 캐시한다. 일반 대화/스토리 초상화 폭을 396 기준 픽셀로 늘리고 대사·스크롤·입력칸·계속 버튼 위치를 조정했다. 전투 무전/시네마틱도 확대했다. 노아 수배 무전은 이동을 막지 않는 하단 패널이고 캠페인 무전과 겹치지 않게 표시한다. 지난 타이틀 변경도 포함되어 있다.

최신 실행 대상은 `Builds/NoaSupport/AFTERSIGNAL.exe`이며 `PLAY.cmd`에 반영됐다. Windows 빌드 오류 0/기존 경고 91개. 네이티브 필수 확인 11개 통과/실행 오류 0: 5단계 수배 해제 실측 4.62초(캡처 작업 포함, 일시정지 제외), 단계 상승/일시정지/신고 정리/무전 중복 회피/외부 취소/초상화 캐시 및 UI 표시. 실제 5개 캡처를 `Documentation/NoaSupport`에 보존했다. 전체 캠페인 검증은 반복하지 않았다. 사용자 실행 중 게임은 종료하지 않았으며 이번 요청에 커밋·푸시는 없었다. 아래 실행 경로는 이전 작업 기록이다.

## 최신 작업: 핵심 3인 타이틀 원화 · 좌측 로고

타이틀을 기존 7인에서 서하·노아·이솔 3인으로 다시 제작했다. 서하는 자연스럽게 서 있는 자세, 조연 둘은 뒤쪽으로 배치했다. 영문 `AFTER / SIGNAL` 로고는 이미지 좌측에 포함했고 중복 UI 로고·상단 장식을 제거했다. 원화·메뉴를 같은 1600×900 프레임으로 묶어 다른 화면 비율에서도 잘리지 않게 했다. 메뉴 하단 그라데이션만 남겨 로고 밝기를 유지했다.

최신 실행 대상은 `Builds/TitleTrio/AFTERSIGNAL.exe`이며 `PLAY.cmd`에 반영됐다. Windows 빌드 오류 0/기존 경고 91개, 타이틀 핵심 확인 3개 통과/실행 오류 0. 실제 타이틀 캡처에서 3인·좌측 로고·메뉴 간격을 확인했다. 전체 게임 플레이 검증은 반복하지 않았다. `Documentation/TitleTrio`에 생성 프롬프트·빌드/표시 결과·캡처를 보존했다. 기존 사용자 게임은 종료하지 않았다. 이번 요청에 커밋·푸시는 없었다. 아래 실행 경로는 이전 작업 기록이다.

## 최신 작업: 튜토리얼 표시 범위·범용 로프·프레임 부담 완화

실행 대상은 `Builds/TraversalPerformance/AFTERSIGNAL.exe`, `PLAY.cmd`로 실행한다. 기존 로컬 변경을 유지했다. 이번 요청에 커밋·푸시는 없다. 상세 범위/검증은 `Documentation/TraversalPerformance/IMPLEMENTATION.md`와 결과 JSON을 참고한다.

ThreatOverlay는 현재 main01 전투에 등록된 실제 생존 전투원만 표시한다. 추격 경찰·군인·무관한 갱단·구조물은 제외한다. 집 앞 임시 엄폐물과 main01 두 번째 단계의 소거 장치를 제거하고 대피로 확보 전투/대사로 교체했다. 다른 캠페인의 파괴 장치는 유지한다.

RopeMotor는 조준한 실제 충돌 표면에 연결한다. 탐험 범위 110m, 움직이는 차량/플랫폼의 로컬 연결 좌표 추종, W/S 감기·풀기, SPACE 도약과 해제 후 관성, 대상 삭제/비활성화/차단 시 해제를 적용했다. 명시적 캠페인 축전기 앵커는 유지한다. 후보/연결점 재사용, 조준 검색은 화면 프레임당 한 번이다.

기존 그래픽을 성능 프리셋으로 일회성 전환하고 이후 사용자 품질 선택은 저장한다. 85% 내부 해상도/SMAA, 그림자 거리·해상도·캐스케이드 하향, 중복 MSAA 제거. 주기적인 6면 실시간 반사는 최고 품질에서만 128/20초 간격이다. 성능 프리셋의 주변 일반 시민 상한 150명/운행 차량 목표 32대/관람객 최대 156명. 도로·바닥·건물의 컬링이나 비활성화는 변경하지 않았다.

생성·원거리 지면 보정을 프레임 예산으로 분산하고 보행의 중복 물리 질의를 제거했다. TrafficSpatialIndex는 앞차 검색을 지역 격자로 공유한다. 차체 크기 조회는 일반 차량에서 선박 컴포넌트를 검색하지 않는다. 차량 접촉은 실제 자식 차체 콜라이더도 찾고 명시적 위치의 ComputePenetration으로 보정해 각 차마다 반복하던 전체 Physics.SyncTransforms를 제거했다. maximumDeltaTime 0.08초로 긴 프레임 뒤 물리 따라잡기 폭주를 제한했다.

최종 빌드 성공: 2,179,290,478바이트, 오류 0, 경고 91개. 네이티브 기능 확인 18개 모두 통과, 실행 오류 0. 첫 진단에서 차량 콜라이더가 루트에 있다고 가정한 테스트가 중단됐으며 실제 자식 콜라이더 선택과 검증을 수정한 뒤 최종 통과했다. 초기 실패 기록은 Artifacts에 보존한다. 전체 캠페인 완주/장시간 교전 검증은 수행하지 않았다.

동일 1600×900 진단에서 평균 프레임 시간은 시내 25.64→20.78ms, 경기장 주변 22.76→19.86ms. 화질/인구 조정이 포함된 결과이며 실제 FPS 보장은 아니다. 시내 p95 30.44→23.81ms, 경기장 p95 27.91→28.36ms로 후자는 개선되지 않았다. After 캡처에서 도로/건물/지면을 확인했다. 모든 순간 멈춤이 제거됐다는 근거는 없으며 상세 원본은 `Documentation/TraversalPerformance`와 Artifacts의 Before/After를 참고한다.

## 최신 작업: 서하·주연 원화, 전용 스프라이트, 길 안내, 현장 판단

현재 실행 대상은 `Builds/CastNavigation/AFTERSIGNAL.exe`이며 `PLAY.cmd`가 이 경로를 읽는다. 기존 로컬 변경과 BGM/API 설정을 유지했다. 이번 요청에 커밋·푸시는 없었다. 이미 실행 중인 사용자 게임은 종료하지 않았다. 아래 빌드 경로는 과거 기록이다.

서하의 새 상반신 초상화와 여성 6명·남성 1명의 주연 합동 타이틀 원화를 적용했다. 주요 NPC 22명은 각각 4방향/정지·걷기·대화 16프레임, 총 352프레임의 고유 스프라이트를 사용한다. StorySpriteImporter가 11개 전체 시트의 배경 제거·발 기준점·크기를 정규화한다. StorySprites는 고정/저작된 인물에만 적용하고 네 도시 대면 협력자도 해당 시설에 배치했다. 원본 경로와 프롬프트는 `Documentation/CastNavigation/art-prompts.json`에 있다.

NpcGroundSupport는 변위 경로의 벽·턱과 바닥을 확인하고 마지막 안전 위치로 복구한다. 차량 탑승·들것 운반·능동적인 충격 비행은 보정에서 제외했다. ProneHitVolume은 빌보드 회전과 독립적인 낮은 피격 박스이며 플레이어와 진영 사격이 같은 충돌 필터를 사용한다. 중상자의 바닥 이미지는 수평으로 배치한다.

TacticalJudgment는 표적의 생존·활성·진영·승무원과 실제 사선을 판단한다. 경찰은 사망/비활성 출동 표적과 예약 연사를 취소하고, 경찰·군인·로봇은 아군/시민이 막은 사격을 보류하고 측면을 찾는다. 갱단도 무효 표적을 해제한다. 구조대는 가까운 위협에 경찰 엄호를 요청하고 안전해지면 처치를 재개하며, 소방대는 안전한 방수 위치를 찾고 위협을 피한다. 외부 AI API 호출은 추가하지 않았다.

NavigationGuide는 도로 경로 진행·회전·남은 거리·도착·연결 불가/항구 환승 안내를 제공한다. 지도에 방향 화살표/방위각, 목적지 지정·보기·해제를 연결했고 단일 선을 가까운 지면 위의 청록 화살표 메시로 교체했다. 길 안내 카드가 무기 슬롯과 겹치지 않도록 위로 옮겼다. 실제 교통 혼잡 ETA와 모든 실내 층의 NavMesh 길찾기는 범위 밖이다.

Windows 빌드 성공: 2,179,277,115바이트, 오류 0, 경고 90개. 핵심 네이티브 기능 확인 16개 통과/실행 오류 0, 최종 리소스·UI 확인 4개 통과/실행 오류 0. `Documentation/CastNavigation`에 결과와 타이틀·대화·주연·지도·길 안내·누운 피격 캡처를 보존했다. 최종 빌드 로그는 `Artifacts/CastNavigation/build-final.log`. 전체 캠페인 완주·장시간 부하 검증은 수행하지 않았다.

## 최신 작업: 첫 캠페인·구급/소방 출동·군 시설 개선

실행 대상은 `Builds/CityResponseUpgrade/AFTERSIGNAL.exe`이고 `PLAY.cmd`가 이 경로를 읽는다. 이전 집 복귀·스프라이트 개선을 포함하며 기존 로컬 변경을 유지했다. 이번 요청에 커밋·푸시는 없었다.

`main01` 전투는 진입 반경 34m, 준비 16초, 웨이브당 2명/체력 48/소총 피해 3으로 조정했다. `CityChronicle.IntroProtected`가 첫 임무 완료 전 무작위 도시 사건과 집 주변 야간 갱단 생성을 막는다. ThreatOverlay는 벽 뒤를 제외한 근거리 적의 머리 위에 빨간 표식을 표시한다. 주인공 개인 보상을 제외한 사건 알림은 ToastNear로 거리를 제한한다.

20명 분량의 새 StoryCast 상반신 초상화와 남녀 소방대원 4방향/호스 자세 16프레임 ResponseCrew를 생성했다. 이미지·프롬프트는 `Documentation/CityResponseUpgrade`, 실제 리소스는 `Resources/Art/StoryCast` 및 `ResponseCrew`에 있다. StoryPortraits는 일반 메인 대화·작전 무전·CityCinematic의 발화자를 구분한다. 기존 음악/API 키 설정은 수정하지 않았다.

CitySafety의 구급차 상한은 5대이며 중상자 우선, 대기열 처리, 병원 복귀 후 슬롯 반환을 적용했다. MedicalPending 자체는 보행을 막지 않고 중상/운반 상태만 막는다. 경상 피격 시 충격 방향이 0이면 기존 보행 목적지를 보존한다. 들것은 환자 원래 스프라이트를 수평 복제하여 매트리스 위에 배치하고 운반 중 원래 렌더러를 숨긴다. FirstAid 후에도 병원으로 귀환한다.

GangTactics는 일반 갱단에 RPG/투척 폭탄·민간인 표적·저체력 차량 도주를 추가했다. TacticalTransport는 움직이는 도주 차량을 추격하고 몬스터 재난 대응 우선순위를 유지한다. CityFireService는 활성 화재 24개, 소방차 5대 상한으로 폭발/차량 손상 화재를 처리한다. 소방차당 2대원, 호스·방수포 소화, 복귀 후 재출동 방식이다. 실제 유체/연소 시뮬레이션은 아니다.

군 기지 FamilyGroup 생성 경로를 제외하고 ExpansionWorld 시설 제목으로 군인 직업·복장을 보완했다. MaritimeWorld가 루멘 기지 항공기를 이전시키던 코드를 제거했다. 별도 공군 3개 격납고/관제탑/작전실/5항공기, 해군 2개 부두/작전실/정비창/2정박함, 각 16명의 전용 근무자를 추가했다. DefenseBaseArchitecture는 합친 메시와 독립 충돌을 사용한다.

필수 네이티브 기능 확인 23항목 모두 통과, 실행 오류 0: `Artifacts/CityResponseUpgrade/functional-result.json`. 부상 보행·중상 정지, 5대 구급차 대기열, 들것 수평 환자, 응급처치 후 복귀, 5대 소방차/10대원/진압, 중화기/시민 공격 표적/도주 탑승, 초기 난이도·거리 알림·기지 항공기 유지 등을 확인했다. 들것 측면 캡처도 확인했다. 이후 변경은 기지 바닥 재질, UI 캡처 방식, 몬스터 대응 중 도주차 추격 우선순위 가드이며 전체 기능 검증을 다시 반복하지 않았다. 최종 표시 확인 결과와 빌드 기록은 같은 Artifacts 디렉터리에 저장한다. 전체 캠페인 완주·장시간 부하 테스트는 수행하지 않았다.

최종 표시/리소스 확인 10항목도 통과, 실행 오류 0. `Documentation/CityResponseUpgrade`에 기능·표시 결과, 대화창/들것/소방차/공군·해군기지 캡처를 보존했다. 최종 빌드 성공: 2,109,815,342바이트, 오류 0, 경고 85개(기존 obsolete API/렌더 관련 경고 포함). 최종 빌드 로그는 `Artifacts/CityResponseUpgrade/build-release.log`. 이후에는 문서·검증 결과 복사만 수행했다.

## 이전 작업: 캐릭터 스프라이트 검토·개선

활성 실행 대상은 `Builds/SpriteQuality/AFTERSIGNAL.exe`이며 `PLAY.cmd`로 실행한다. 직전 집 복귀 수정도 포함한다. 이번 요청에는 커밋·푸시가 없어 변경은 로컬에 유지했다.

캐릭터 시트 80개/1,096프레임의 가져오기 결과를 점검했다. 내장 이미지 생성 도구로 시민 남녀·의사·간호사 4종을 24프레임(4방향 × 정지/걷기 4단계/대화) 시트로 재제작하여 `Art/NpcPolished`에 연결했다. PeopleArt는 기존 호출의 포즈 의미를 유지하면서 이동 시 새 4단계 프레임을 사용한다. 시설별 직업의 기존 유니폼 매핑에도 적용된다. 원본·프롬프트·범위 및 한계는 `Documentation/SpriteQuality`에 보존했다. 모든 역사적 캐릭터를 다시 그린 작업은 아니다.

SpriteSilhouetteFinish는 가져오기 시 작은 잔여 픽셀을 제거한다. 서하의 비전투 액션에는 팔다리 사이의 흰 배경 제거를 적용하고 보라색 머리카락 인접 영역을 보호한다. 수영은 균등 그리드가 손끝/신발을 자르던 문제 때문에 전체 이미지에서 8개 인물 윤곽을 먼저 분리하고 공통 크기 비율과 여백으로 다시 배치한다. 원본 파일은 유지하며 런타임 픽셀 처리 루프를 추가하지 않았다.

최종 Windows 빌드 성공(오류 0, 경고 7). `SpriteQualityProbe`의 패키지 프레임 조회/게임 재질 렌더링 필수 확인 14항목 통과, 오류 0. `Artifacts/SpriteQuality/Final`에 시트 검토 결과, `Native`에 실제 플레이어 렌더 캡처/결과가 있다. 최종 수영 손끝 보존과 흰 배경 제거를 이미지로 확인했다. 전체 캠페인/장시간 플레이 검증은 수행하지 않았다.

## 최신 작업: 이어하기·사망 복귀를 서하의 집으로 고정

사용자 요청에 따라 `BeginFromTitle(true)`는 마지막 저장 장면 대신 Residence를 로드한다. 진행 상태/재화는 초기화하지 않으며 이전 이동 도착 좌표와 이웃집 방문 상태만 해제한다. `RespawnNetwork.Respawn`도 기존 회복센터 등록과 관계없이 CompactHome.Spawn으로 복귀한다. 네 도시 회복센터는 체력 회복 시설로 유지하고 사망 UI의 목적지 설명을 집으로 수정했다. 이전 회복센터 리스폰 지정 요구는 이번 요청으로 대체됐다.

직전 전체 변경은 `a76a530a`로 main/origin/main에 커밋·푸시 완료됐다. 이번 집 복귀 변경은 별도 로컬 변경이다. 새 실행 대상은 `Builds/HomeReturn/AFTERSIGNAL.exe`; 아래 빌드 경로는 과거 기록이다. 필수 진단은 `HomeReturnProbe`의 실제 이어하기/사망 Retry 장면 전환이며 결과는 `Artifacts/HomeReturn/result.json`이다.

Windows 빌드 성공(오류 0), native 필수 확인 5개 통과/오류 0. 기존 도시 저장의 이어하기→집, 저장 재화 유지, 실제 사망 상태→Retry→집, 회복센터 사용 후에도 집 복귀, 실내 바닥 지지를 확인했다. `PLAY.cmd`는 새 빌드를 가리킨다.

## 최신 작업: 시민 생성 밀집 방지·게임 성능 최적화

활성 실행 대상은 `Builds/CrowdPerformance/AFTERSIGNAL.exe`, `PLAY.cmd`로 실행한다. 이전 미커밋 작업을 보존했으며 이번 요청에는 커밋·푸시가 없어 로컬에 유지한다. 아래 활성 빌드 경로는 과거 기록이다. 상세 구현 및 측정 한계는 `Documentation/CrowdPerformance/README.md` 참고.

ExpansionWorld의 실패한 지면 검색이 시설 원점으로 되돌아가던 스폰을 제거했다. 실패하면 생성을 미루고 재시도한다. PopulationBudget은 프레임당 4명, 보행 생성 간격 1.45m, 주변 보행 밀도 및 180m 내 이동 시민 220명 한도를 적용한다. 가족 구성원도 각각 집계한다. 좌석 관객/선수는 이동 인구 한도와 분리한다. VenueRuntime은 대규모 관객을 여러 프레임에 생성하며 멀리 있는 NPC 루트만 쉰다. 지면/도로/건물 렌더러는 변경하지 않았다.

공항·항구의 TransitTraveller가 반복된 5×4 대기 좌표에 생성되고 한 탑승구에 동시에 모이던 문제도 수정했다. 서비스별 안전한 대기 위치를 찾아 생성하며 공간이 없으면 재시도한다. 한 명씩 탑승구에 접근하고 하차 공간이 막히면 정차 상태에서 재시도한다. CrowdFlow는 이동 경로 끝점 허용 거리 및 생성 예약을 사용하고 방문했던 공간 격자를 계속 누적하지 않는다.

ActorSpatialIndex의 재사용 격자와 리스트로 차량별 전체 WorldActor 배열 복사, 전투 표적 및 일상 대화의 전체 장면 검색을 줄였다. 원거리 NPC 일과/스프라이트/지면 확인/접촉 그림자/차내 이미지 갱신 주기를 조절했다. 긴급 행동은 우회한다. 들리지 않는 차량 엔진 DSP를 일시 정지하며 BGM은 변경하지 않았다. VehicleGround는 비할당 ray 버퍼와 포화 시 fallback을 사용한다.

동일 RTX 4060 Ti, 1600×900 진단 렌더 조건에서 평균 프레임 처리 시간이 시내 36.25→29.83ms, 경기장 주변 36.15→24.41ms로 감소했다. 시내 인물 수는 217→222명. GPU readback을 포함한 진단값이므로 실제 플레이 FPS 보장은 아니다. 수치는 최종 터미널/밀도 보완 전의 통합 빌드에서 측정했고 원본은 `Artifacts/CrowdPerformance/Before`, `After`다. Mono 할당 카운터의 0은 측정 불가로 취급한다.

최종 Windows 빌드 성공(오류 0, 경고 85), native 필수 확인 14항목 통과/오류 0. `Artifacts/CrowdPerformance/SafetyFinal/result.json`과 `build-result.json`을 참고한다. 초기 Safety에서 발견된 밀도 실패를 실제 수정 후 재확인했으며 승객 간격과 실제 탑승도 통과했다. 전체 캠페인/장시간 전투/모든 도시 장기 검증은 수행하지 않았다. 사용자 게임은 종료하지 않았고 진단 저장은 억제했다.

## 최신 작업: 리스폰·사건 직렬화·피해 단계·응급 출동·전투 연출

활성 실행 대상은 `Builds/ResponseRenewal/AFTERSIGNAL.exe`, `PLAY.cmd`로 실행한다. 현재 요청에 커밋·푸시는 없어 이전 두 작업과 함께 로컬에 유지했다. 상세 내용은 `Documentation/ResponseRenewal/README.md`, `VALIDATION.md`, 자료 출처는 `REFERENCES.md`. 마지막 푸시 체크포인트는 여전히 `61e34d8d`다.

`RespawnNetwork`는 기본 사망 복귀를 서하 집으로 변경하고 네 도시의 기존 병원/작전기지 앞에 E로 등록하는 회복 단말을 하나씩 배치한다. 지도에 표시하며 신규 게임은 집으로 초기화한다. `CityEventGate`는 몬스터 예고/출현, 갱단 교전/차량, 테러를 한 슬롯으로 관리한다. 폭탄과 도착 중 차량도 사건을 유지하며 마지막 위협 후 8초 정리 시간을 둔다. 갱단이 전투/범죄를 멈추고 장시간 배회하면 슬롯을 반환한다. 별도 캠페인 적과 사용자가 시작한 전투는 기존 규칙을 유지한다.

`MedicalState`는 받은 피해와 남은 체력을 기준으로 부상/중상/즉사를 구분한다. 중상자는 실제로 맞출 수 있는 낮은 BoxCollider를 가지며 갱단의 확인 사격으로 사망한다. 경상은 현장 처치, 중상은 안정화/2인 들것/병원 이송. 시민 목격/자체 무전/F6 신고로 최대 24대 구급차(초당 최대 4대)를 배정한다. 범위는 외부 도시 장면 및 플레이어 650m이며 나머지는 대기열에 남는다. 전용 Blender 구급차는 `Tools/WorldExpansion/create_ambulance.py`로 생성한다. `MedicalArtImporter`는 생성된 4×2 RGB 체크무늬 원본을 RGBA로 변환해 기존 NPC 외형 위의 상처/치료 레이어로 사용한다. 단순 원본 PNG 재복사는 금지.

경찰은 발포 시 현재 상대를 다시 조준하고 가까운 갱단과 교전한다. LegacyPolice/LegacySwat 외형을 NpcPersona가 덮어쓰던 문제를 수정했다. 경찰/군/갱단 말풍선은 전용 FactionVoice를 거친다. SecurityVehicleArt의 FBX +180Y 보정으로 차체 앞/진행축을 맞춘다. 빈 차량 총좌 발포 차단. 로봇은 6500/12000 체력, 공격자 기억/반격, 인간용 날림 제외, 사망 폭발을 적용한다.

긴급차량은 ResponseDrive와 EmergencyTraffic으로 신호 대기를 건너뛰고 전방 차량에게 양보를 요청하며 우회한다. 바닥을 관통하지 않는 BoxCast, 다른 층 도로 제외, 조기 회피 점수가 핵심이다. 물리 충돌은 유지한다. 구급차는 가벼운 충돌 시 대원을 내리지 않으며 전용 지붕 위 경광등의 발광을 제한했다.

CombatVfx는 발포 가스/연기/탄피/이동 예광탄, 재질별 피격 입자/소리, 폭압 먼지/불붙은 파편/거리별 흔들림/시점 반동을 추가한다. 기존 실제 발포 소리는 보존하고 23개 파생 잔향/자체 합성 효과를 CombatDetail에 추가했다. TitanBarrage는 48m 예고 후 회전 레이저/반복 열 피해/차량 점화/건물 파괴를 사용한다. 품질 설정과 효과 예산을 유지한다.

완료한 필수 native 실행은 초기 39항목, 후속 8항목, 최종 6항목 통과. 초기 네 실패는 수정 후 재확인했고 최종 Complete 결과 오류 0이다. 실제 집 복귀, 경찰 사격, 로봇 반격, 4대 이상 구급차, 부상/이송, 차량 우회, 포탄/레이저 피해를 확인했다. 마지막 경광등 재질만 shipping 빌드에서 컴파일 확인한다. 전체 수동 캠페인/장시간 도시 부하 검사는 하지 않았다. 진단의 첫 실제 복귀 검사는 저장 Stage를 Residence로 남겼을 수 있어, 이후 TravelRoutine에도 SuppressSave 가드를 적용했다. 사용자 게임은 종료하지 않았다.

## 최신 작업: 군중·구출 경로·관람 경기·해상 활동

활성 실행 대상은 `Builds/LivingHarbor/AFTERSIGNAL.exe`, `PLAY.cmd`로 실행한다. 이번 요청에는 커밋·푸시가 없어 직전 CyberConflict 구현과 함께 로컬 작업 트리에 유지했다. 마지막 푸시된 체크포인트는 `61e34d8d`다. 상세 구현/제한/필수 확인은 `Documentation/LivingHarbor/IMPLEMENTATION.md`, `VALIDATION.md`, 이미지 프롬프트는 `ART-PROMPTS.md`에 있다. 아래의 활성 빌드 경로는 과거 기록이다.

CrowdFlow는 장면별 재생성, 공간 격자 분산, 실패한 스폰 생략으로 한 점 집중을 줄인다. PedestrianGround의 턱 윗면 선행 검사와 PursuitPath의 높이 유지가 핵심이며, EscortFollower는 안전한 플레이어 발자국만 복구 지점으로 사용한다. 구출 응급처치와 보스 웨이브 완료 조건을 보완했다. 가족·커플의 대피는 FamilyGroup이 단독으로 이동을 담당해 CityNpc 도주와 충돌하지 않는다. 출퇴근은 인근 보도 목적지 일과이며 모든 NPC에 실내 직장까지 지정한 것은 아니다.

경기장 관람 정원은 180~360명, 지정 좌석/분산 출입을 사용한다. VenueSafety가 난입·부상 시 경기 시간과 배팅 정산을 멈추고 안전 확보 후 재개한다. 선수/직원/관객별 반응과 4종 스포츠 공·동작 연출을 추가했다. 스포츠 점수/규칙은 기존 이벤트 시뮬레이션이며 완전한 공 접촉 물리는 아니다.

CityIncidentBoard는 몬스터·활동 중 갱단·해적·테러 위치와 플레이어 기여 보상을 관리한다. 몬스터가 무너뜨린 일반 건물은 180초 후 안전할 때 복구한다. ReversibleMeshCut은 원본 메시와 겹친 절개를 유지하며 반드시 지면 위로 절개 하한을 제한해야 한다. 지하 기초 Bounds를 그대로 절개하면 공용 바닥이 사라진다. 고유 시설 VenueRuntime은 붕괴 제외다.

HarborAccess는 조종석/조타석 실제 접근 지점과 터미널 승객 대기를 제공한다. HarborParcelRepair는 11개 부두 고층 건물을 기존 도시 빈 부지로 이동했다. 독립 메시를 복제하지 말고 직접 이동하며 통합 메시 추출은 바닥을 제외한다. 별도 해군/해경/공군 시설과 군용기 12대 이동, 해군 함정·해경 경비정·해적 보트 순찰/실탄 교전/수동 포격을 적용했다. 함정은 비치명 피해 시 승조원을 유지하고 치명 피해 이후에는 VehicleFailure가 침몰을 담당한다.

CoastGuard/NavyCrew/AirForceCrew/SeaRaider/NullCell 5종 원본을 `Documentation/LivingHarbor/SourceArt`에 보존했다. CyberSecurityImporter가 RGB 체크무늬를 RGBA로 변환하고 4방향×4포즈를 가져온다. 자체 함정 모델은 `Tools/WorldExpansion/create_maritime_fleet.py`로 생성한다. 폐쇄 선체의 바깥쪽 노멀을 반드시 재계산해야 Unity에서 갑판이 사라지지 않는다. MaritimeMaterials는 전용 URP/Lit 재질이다.

CivicTerrorEvents는 갱단과 별도 세력이며 시민/경찰 목격 후 경찰·SWAT만 대응한다. 테러리스트 자신·갱단·군인은 신고 목격자로 쓰지 않는다. 군인 표적 검색은 테러리스트를 제외한다. 경찰은 기존 SecurityResponse의 차량 출동을 사용한다.

Windows 최종 빌드 오류 0, 경고 77. 주요 미션 6유형/군중/경기/보상/탑승을 확인한 초기 실행에서 49항목, 후속 실행에서 14항목 통과했다. 초기의 절대 좌표·파괴된 배 선택·무적 시간 관련 진단 fixture 오류는 수정하고 해당 항목을 재확인했다. 마지막 `Artifacts/LivingHarbor/Complete/result.json`은 2항목 통과/오류 0이며 함정 3종 native 화면도 확인했다. 전체 캠페인 수동 완주는 하지 않았다. 사용자 기존 게임은 종료하지 않았고 진단 저장은 억제했다.

## 최신 작업: 사이버펑크 무장 조직·공권력·구조 이송

작업 시작 시 기존 변경 전체 1,083파일을 `61e34d8d`로 main에 커밋하고 origin/main에 정상 푸시했다. 그 이후 이번 구현은 로컬 작업 트리에 있다. 활성 실행 대상은 `Builds/CyberConflict/AFTERSIGNAL.exe`, `PLAY.cmd`로 실행한다. 아래 CompactCities 이하의 활성 경로·미커밋 상태는 과거 기록이다. 상세 내용과 필수 확인 기록은 `Documentation/CyberConflict` 참고.

운전 조작은 문맥 카드 한 곳으로 통합했다. 차량 내부·하차 외형은 동일한 PeopleArt 식별자를 사용한다. 일반 경찰차에 TacticalTransport가 붙어 장갑차 좌석 좌표가 적용되던 분기를 바로잡았다. VehicleSweep는 보행자 풀에 없는 경찰·군인·시설 인물까지 검사하며, NPC 운전 사고는 환경 피해 원인을 전달한다. 갱단 실제 운전자의 탈취·폭발 하차는 GangConvoy와 VehicleCabin에서 한 번만 처리한다.

새 4방향·4포즈 CyberGang/CyberPolice 원본 생성 이미지는 `Documentation/CyberConflict/SourceArt`에 보존한다. 생성 결과는 RGB 체크무늬 배경이었으므로 게임용 PNG는 반드시 RGBA로 변환해 사용해야 한다. CyberSecurityImporter가 연결된 매트 제거, 발 피벗, 2.12m 기준 크기를 적용한다. 일반 ArtImporter와 중복 처리하지 않는다. 군 수송차·갱단 습격차·경찰 로봇·군 로봇은 자체 Blender/FBX 모델이며 `Tools/WorldExpansion/create_security_units.py`로 재생성한다. SecurityMaterials에서 URP 재질을 적용하고 바퀴·후방 램프·로봇 관절은 독립적으로 움직인다.

GangStrongholds는 기존 애프터라이트·노바 내부 빈 부지를 찾아 아지트 3곳을 만든다. 새 땅은 추가하지 않았다. 인근 경비, 최대 3개 습격 차량, 단계적 경찰·특수대 출동으로 예산을 제한했다. CityChronicle에는 기존 ID·진행을 보존하면서 메인 작전 3개/전투 단계 9개를 추가했다. 아지트 실제 배치 후 목표 좌표를 갱신한다. 전체 캠페인을 수동 완주하지는 않았다.

IncidentCommand는 거신 재난 우선순위를 공유한다. 수배 열기는 보존하되 경찰·군의 서하 추격을 멈추고 몬스터에 집중한다. SecurityResponse는 사건 중복을 합치고 원거리 차량 출동 뒤 현장 하차하며, 기존 군 대응 지연을 유지한다. RiftCreature는 최근 공격자 위협도, 체력·공격 예고 HUD, 충격파·파편·카메라 흔들림을 사용한다.

MedicalState는 인간의 중상·전투 불능·출혈·안정화·회복을 관리하며 사망자는 부활시키지 않는다. CitySafety가 최대 4대 구급차를 배정한다. EmergencyAmbulance의 두 구조대원은 같은 부상자를 들것으로 운반하고 구급차에 싣고 병원에 내려 치료한다. 경찰·군인도 동일하다. 구조 도중 차량·구조대 손상으로 중단되면 부상자 배정을 해제해 재출동할 수 있다. Downed는 전투 대상·임무 잔존 적·일상 대화에서 제외한다. CitySocial은 구조대나 교전 중 전투원의 잡담을 막는다. NpcPersona와 StreetVoices는 직업·연령에 따른 위기 및 일상 대사를 제공한다.

필수 native 결과 `Artifacts/CyberConflict/Final/result.json`: 18항목 통과, 오류 0. 새 아틀라스, 좌석·탑승자 동일성, 군인 차량 충돌과 중상, 로봇·군 수송차, 공격자 우선 조준, 거신 우선 진압, 들것·병원 회복을 확인했다. 첫 검수에서 RGB 배경과 구조대 잡담을 발견해 수정했다. 검증은 LifeState/CityChronicle 저장을 억제한다. 사용자의 기존 게임 프로세스는 종료하지 않았다.

## 최신 작업: 기존 도시 안으로 시설·빈민가 통합

활성 실행 대상은 `Builds/CompactCities/AFTERSIGNAL.exe`, `PLAY.cmd`로 실행한다. 이번 요청에는 커밋·푸시가 없어 기존 미커밋 전투 캠페인 변경과 함께 로컬에 유지했다. 상세 내용은 `Documentation/CompactCities/README.md`, 배포·검증 기록은 같은 폴더 `validation.json`.

`CompactCityLayout.json`이 시설 36곳의 최종 위치다. 애프터라이트 8곳, 노바 20곳, 네레이드 8곳을 원래 도시 영역 안에 넣었다. 기존 빌딩 209개와 상호작용 하위 요소, 별도 배치 외관을 함께 빈 부지로 이동했다. `Documentation/CompactCities/prefab-layout-v1.json`은 이미 적용한 이동을 반복하지 않도록 하는 스탬프다. 해당 스탬프만 삭제하고 Prepare를 재실행하지 말 것. 도시 땅은 새로 늘리지 않았고 애프터라이트 북쪽·노바 남쪽 확장 지면과 수중 공공기관 별도 돔을 제거했다.

새벽 골목은 원래 애프터라이트 북서쪽 280×220m 안에 작은 주택 172채, 좁은 골목, 허름한 점포와 전선·옥상 설비를 만든다. HomeQuarter=(220,0,435). 서하 집은 작은 단층 주택이며 `CompactHome`이 Residence의 과거 WORLD를 비활성화하고 지상 실내를 생성한다. 침대·옷장·금고·직접 출입구를 유지했다. 구형 HavenQuarter 프리팹은 삭제했다. 섬 4곳은 태양광 모듈 주택·보행 데크·바이오 설비·순환 교통로와 스마트 항구로 개편했다.

건물 이동 필터가 그 밑 보도 조각까지 옮긴 것을 화면에서 발견했다. Ground는 건물 이동 대상에서 제외해야 한다. `Compact-GroundManifest.py`는 14a5ed78 원본 메시 GUID를 읽고 `CompactCityBuilder.RepairGround`가 새 시설 부지 밖 수평 바닥을 원위치로 복구한다. 252개 배치 수정 완료. 490개 CompactMeshes 중 다른 건물 구조·외관 이동은 유지한다. 바닥·광장·도로의 겹치는 높이도 분리했다.

ESC는 지도부터 닫고, V 또는 일시정지 메뉴로 서하 바이크를 부른다. 실내와 착지 불가 위치에서는 외부 평지에 나올 때까지 호출을 보류한다. 옥상 전망 단축키는 P로 변경. LocalCityRoutes는 원래 도시 길·시설 연결로·네레이드 길·섬 순환도로를 통합하고, 차량 예산은 플레이어 주변을 기준으로 계산한다. 잠식 도시는 일반 시민 보충에서 제외한다. 이전 외곽의 의뢰 목표와 차량 저장 위치는 CompactCityLayout.Migrate로 옮긴다.

최종 기능 확인 `Artifacts/CompactCities/Final/result.json`: 91항목 통과, 오류 0. 노바 근처 주민 246/교통 4, 네레이드 122/6, 스마트 섬 82/4. 위치·바닥·집 주변 차량 탑승·바이크 호출/탑승·ESC·작은 집 시설 확인과 6개 렌더 캡처를 진행했다. 이후 변경은 P 단축키 안내 문구와 기존 지역 진단의 옛 배치 기대값 정리뿐이다. 배포 빌드 로그 `Artifacts/CompactCities/build-shipping.log`. 사용자의 기존 실행 중 게임은 종료하지 않았다.

## 최신 작업: 미사일 취약성·원거리 출동·잠식 거신·전투 캠페인

시작 시 기존 변경 전체 178파일을 `14a5ed78` (main)에 커밋하고 origin/main으로 정상 푸시했다. 그 이후 이번 구현은 로컬 작업 트리에 있다. 활성 실행 파일은 `Builds/IncursionCampaign/AFTERSIGNAL.exe`, PLAY.cmd로 실행한다. `Documentation/IncursionCampaign/README.md`, `REFERENCES.md`, `validation.json` 참고.

WarheadDamage/BlastPayload로 일반 충돌·총탄과 미사일/포탄을 분리했다. 폭발에 실제 맞은 차량을 전달하고 차체 최근접점을 사용한다. 군 탄두의 WorldActor source를 끝까지 유지한다. 빈 주차 차량 Enter는 신고하지 않으며 점유 차량 강탈만 신고한다. 군 투입 임계값은 확인된 민간인 사망 30명이다.

ResponseDispatch는 공통 도로 그래프 9,813노드와 카메라 밖/차폐된 원거리 합류 지점을 사용한다. 경찰·군 차량은 ResponseDrive로 접근하며, TacticalTransport와 군 트럭은 현장에 도착한 후에만 하차한다. MilitaryResponse는 괴물 사건 44초, 범죄 사건 55초 동원 대기 뒤 수송차/전차/항공기를 순차 출동시킨다. 네레이드 내부에는 지상 지원을 보낸다. RegionalWorld의 에레보스 교전도 RiftIncursion 경유로 바꿔 즉시 특수대 생성 경로를 없앴다.

RiftCreature 전용 원본 3종은 `Tools/WorldExpansion/create_rift_titans.py`로 작성한 Blender/FBX다. Blender Z-up 변환은 model 로컬 회전에 보존하고 바깥 form을 이동 방향으로 회전할 것. 원본 루트 회전을 identity로 덮으면 누워 버린다. 대형 거신은 외피/공격 후 취약 시간, 시민 우선 탐색, 광역 타격, 예고 후 대공 레이저를 사용한다. 실험 결과 3모델의 크기·바로 선 머리 높이·헬기 피격을 확인했다.

CityGangWar.FindGround는 요청 높이를 기준으로 바닥을 찾고, 첫 후보는 반드시 요청 좌표 그대로다. 절대 높이 0m 제한은 수중 NPC 생성을 막고, 첫 후보에 -0.75m를 더하는 코드는 이동 NPC를 옆으로 계속 밀어낸다. CampaignBattle의 호위 경로는 이를 수정한 바닥 검사와 PursuitPath를 사용한다.

CityChronicle main01~main30의 id와 3단계 저장 구조는 유지하면서 90개 전투 단계로 교체했다. 현장에 70m 이내 접근하면 자동 시작, 엄폐물·경비대·파괴 장치·구출 대상·보스와 전투 HUD/교신 생성. 돌파, 방어, 파괴, 호위, 탈출, 보스전이 연결된다. 첫 작전의 세 단계 완료와 main02 자동 추적까지 native에서 통과했다. 서브 의뢰는 유지한다. 대사 188개 추가로 StreetVoices 총 552개.

기능 실행 결과는 `Artifacts/IncursionCampaign/Release/result.json` (57항목 통과, 런타임 오류 0), HUD 최종 확인은 `Artifacts/IncursionCampaign/UI/result.json`에 기록한다. 최종 빌드 로그 `Artifacts/IncursionCampaign/build-ui.log`. 사용자의 기존 실행 중 게임을 종료하지 않았고 진단 실행은 LifeState/CityChronicle 저장을 억제한다. 이번 변경 후 전체 30작전을 수동 완주한 것은 아니며 첫 작전의 실제 완료 경로와 각 작전 지역 바닥을 필수 확인했다.

## 최신 작업: 차량 디자인·새벽 저지대·지역 기관·섬 확장

현재 `PLAY.cmd` → `Builds/RegionalExpansion/AFTERSIGNAL.exe`. `Documentation/RegionalExpansion/README.md`와 `REFERENCES.md`에 구현·조작·출처를 기록했다. 최종 빌드 오류 0건(기존 경고 47개), native 116항목 통과, 런타임 오류 0건. 결과 `Documentation/RegionalExpansion/validation.json`, 캡처 `Artifacts/RegionalExpansion/Final`, 로그 `Artifacts/RegionalExpansion/player-final.log`. 별도 커밋·푸시 요청은 없어 기존 미커밋 변경과 함께 유지했다.

실차 참고 자체 모델 10종은 `Tools/WorldExpansion/create_reference_fleet.py`로 제작했다. 독립 바퀴와 휠하우스/유리/좌석을 갖추고 FleetDesign에서 타입·디자인 번호로 선택한다. 모델 좌표는 Blender +X 앞, FBX 후 런타임 180도 회전 유지. 휠하우스 Boolean을 차체 전체 폭으로 절개하지 말 것. 슈퍼카 지붕 1.29m, 좌석/머리받침과 VehicleSeats 높이를 함께 맞춰 두었다. `VehicleDrift`는 SPACE+조향의 횡방향 관성을 담당하며 특수 탈것·탱크 전용 물리와 분리된다.

FourCityCatalog의 기존 28개 인덱스를 보존한 뒤 RegionalCatalog가 32항목을 추가한다. 새벽 저지대 주택/증축층 534개, 주민 180명과 순찰 12명. 전체 신규 지역 배치 정원 1,560명은 근거리 활성화한다. 기존 Haven WORLD를 HavenQuarter.prefab으로 추출하고 (430,0,2670)으로 이동했다. ResidentWalker의 절대 경로도 같이 이동한다. CivicWorld/GameDirector/RegionalOrigin이 Haven 목적지·저장 진입을 연속된 도시 고향으로 연결한다. 원본 Haven 장면은 삭제하지 않는다. NeighborBond는 친밀도를 저장하고 AI context에 고향 관계를 넣는다.

에레보스 싱크홀은 단순 검은 판이 아니다. 지면을 네 조각+고리 MeshCollider로 만들고 중앙을 뚫었다. 전체 바다 메시 생성 시 붕괴구를 제외해야 해수면이 구멍을 덮지 않는다. 진입 도로를 275m 반경으로 우회시킨다. ErebosThreat와 ArmyResponder는 FactionCombat으로 서로 공격한다. RegionalUniform이 에레보스 특수부대/시설 죄수의 외형을 보존한다.

노바 12기관, 네레이드 8신규 기관+기존 시청/병원 개편, 에레보스 7기관/지형, 저지대, 섬 4개. RegionalBuilding의 Floor는 기존 계단/승강기 구멍을 유지한다. CashContainer.locationId로 신규 은행 금고가 다른 시설의 일일 탈취 상태와 섞이지 않는다. 죄수는 실내 셀에 생성하고 외부 산책 목록으로 이동하지 않는다. 섬은 불규칙 해안 MeshCollider+해안 재질, 부두, 보트와 RegionalFerry 순환선을 사용한다. x>2700의 모든 바다를 에레보스로 취급하면 섬 하늘까지 검어지므로 CityAt의 z 경계를 유지할 것.

사용자가 실행해 둔 기존 게임 프로세스를 종료하지 않았다. 이 작업의 build/native 도구 프로세스만 실행했으며 검증은 저장을 억제한다.

## 최신 작업: 차량 내구도·범죄 대응·시설 현금

최신 실행 대상은 `PLAY.cmd` → `Builds/TrafficJustice/AFTERSIGNAL.exe`. 전체 내용과 조작은 `Documentation/TrafficJustice/README.md`, 필수 확인 결과는 동 폴더 `validation.json`. 이전 지면 복구 변경을 유지했다. 이번 작업의 커밋·푸시는 요청받지 않아 실행하지 않았다.

최종 Windows 빌드 성공(오류 0/경고 47), 필수 네이티브 검사 34개 통과 및 런타임 오류 0. 충돌 피해·탑승 사격·투명 창·추락/침몰·경찰 헬기 손상·신고/사고 책임 구분·장갑차 4인 하차·군 대응 5대·근접 생포·금고 강탈·개인 금고 입출금·수배 사망 비수감까지 확인했다. 최종 로그 `Artifacts/TrafficJustice/player-final.log`.

VehicleDurability가 구형 100 체력을 차종별 용량으로 1회 변환한다. 차량 UI/수리/저장/승객 반응은 HealthFraction을 사용해야 한다. VehicleFailure는 치명 피해 이후 NPC 항로와 군 조종을 비활성화해 추락·침몰의 이동을 단독 관리한다. VehicleDamagePresentation은 연기/화염을 근거리에서만 만든다. MountedCombat은 기존 보유 총기/탄약을 사용하며 Ballistics가 현재 탑승 차량을 제외한다. VehicleHorn의 NPC 답신은 재귀 경적을 만들지 않는다.

WantedSystem.Report는 CrimeObservation의 시야/목격 신고를 거치고, ConfirmReport만 실제 수배를 올린다. 플레이어 피해의 기존 null source 규약을 유지하되 환경 사고는 TrafficDamageSource.Environment를 전달한다. GameDirector.Die에서 Capture를 호출하지 않는다. PrisonSystem.Capture는 살아 있는 수배 대상만 허용한다. TacticalTransport, MilitaryResponse/VehicleAI, FacilitySecurity/StationDefender를 분리했다. CashLocations/CashContainer는 물리 금고/현금함과 탈취 시간을 담당하고 HomeCash는 LifeState에서 저장한다.

검증 프로세스와 사용자 게임 프로세스를 구분할 것. 작업 시작부터 실행되어 있던 과거 Fidelity 게임을 종료하지 않았다. 본 작업의 native 검증은 저장을 억제하며 종료 후 실행 프로세스가 남지 않아야 한다.

## 최신 작업: 대규모 지면 복구·수중 돔 렌더링

시작 커밋은 `13459344`. 최신 실행은 `PLAY.cmd` → `Builds/WorldRecovery/AFTERSIGNAL.exe`. `Documentation/WorldRecovery/README.md`에 원인·복구·검증·미완성 범위를 정리했다. 아래 Fidelity 실행 경로·성능 결과는 과거 기록이다.

`AfterlightExpansion.prefab`의 비어 있던 배치 하위 트리가 실제 누락 원인이다. `2dc22dce`에서 691개 배치/3,455개 컴포넌트를 복구하고 715개 의존 GUID가 존재함을 확인했다. 최신 게임플레이 데이터는 유지했다. `restore_coastal_batches.py`를 반복 실행하면 이미 채워진 트리를 덮어쓰지 않도록 중단한다. 정확한 배치 삭제 작업은 확정하지 않았다. `WorldPresentationPreflight`가 빌드 전 5개 프리팹의 배치·메시를 확인하며 원본 정리 코드에서 배치 내부를 보호한다.

FourCityWorld는 경계 기반 거리 판단·항공 시점 가시거리·히스테리시스·forceRenderingOff 전환 갱신을 사용한다. CameraOcclusion은 대형 통합 배치와 발밑 지면을 제외하고 재질 교체를 줄인다. StructuralGlass는 돔/기밀 터널용으로 투명도·반사를 제한하며 돔 중복 삼각형을 제거했다. Bloom clamp 6, 야간 발광 대상 판별도 보완했다. 원래 흰 번짐의 정확한 재현에는 실패했으므로 완전한 원인 확정으로 보고하지 말 것.

네이티브 `WorldRecoverySmoke`에서 12개 화면과 지면 충돌/표시 3개 통과. 이전 자동 촬영의 `SingleCameraRequest`가 URP volume 갱신을 건너뛰는 문제를 발견해 `StandardRequest`로 변경했다. FidelityBenchmark도 같은 방식으로 수정했다. 이전 프레임 시간 수치는 최종 후처리 경로의 성능으로 인용하지 말 것.

서하 실험은 `Artifacts/CharacterLab/Refined`에 있다. 팔·손 두께 보정, 27종 검토 액션과 발 접촉 제어까지 진행했다. 얼굴/헤어/의상 연결부/동작은 목표 품질 미달이며 게임에는 적용하지 않았다. 기존 스프라이트를 대체하거나 고품질 3D 완성으로 보고하지 않는다.

## 최신 작업: 물리 재질·건축 표현·거리 소품·식생·보행자 품질

기존 네 도시 변경사항을 `0968b341`로 main에 커밋·푸시한 뒤 진행했다. 최신 실행은 `PLAY.cmd` → `Builds/Fidelity/AFTERSIGNAL.exe`. 적용 범위·조작·AAA 제작 격차는 `Documentation/Fidelity/README.md`, 1차 참고 자료와 코드 조사 결과는 `QUALITY-DIRECTION.md`, 최종 빌드·네이티브 검사·비교 프레임 시간은 `validation.json`에 있다. AAA 게임 수준을 완성했다고 보고하지 말 것.

URP 17.4를 유지한다. UrbanSurface의 고정 주변광을 PBR 조명/법선/ARM/반사로 교체했고 CC0 2K 재질 4종을 적용했다. ArchitecturalGlass는 창틀·실내 깊이·블라인드·점등 차이와 기존 FacadeGlass 파손 마스크를 지원한다. 파손 변수는 UnityPerMaterial 버퍼에 두어 일반 창문 렌더링에도 개별 전역 파라미터 처리를 강요하지 않는다. 설정은 ESC 그래픽 버튼의 성능/고품질/최고 품질. CityClock의 그림자·환경광, FidelityPresentation의 노출/반사 프로브, CitySky 구름, CameraOcclusion 잔상을 보완했다.

DistrictTower는 새 네 도시 구역의 여섯 타워 형태와 상업부·차양·테라스·외부 구조·옥상 설비를 만든다. 기존 시내 전체를 개별 수작업 건축물로 교체하지 않았다. StreetKit 7종은 Blender 제작물이며 UrbanDetailStreaming이 근거리 최대 48개 지점을 운영한다. FBX 루트의 축 변환 회전을 초기화하면 소품이 눕는다. StreetKit.Place에서 임포트 회전을 반드시 보존한다. 나무는 Poly Haven CC0 원본을 재가공한 83,206/29,190/8,789 삼각형 LOD이며, 초기 일괄 감량은 줄기·잎 손상 때문에 폐기했다. 재생성 절차와 출처는 Tools/Fidelity, Documentation/Fidelity에 있다.

VenuePracticalLights는 신설 실내 시설의 현재 층 주변 등 12개만 켠다. 로비 바닥을 외부 광장보다 3cm 높여 겹침을 제거했다. PedestrianSteering은 짧은 회피 지점을 유지해야 벽 앞에서 맴돌지 않는다. 절벽 방지와 NPC 트리거 몸체 간격을 처리하지만 NavMesh 전체 경로 탐색은 아니다. InteractionScanner는 주변 후보를 0.2초 캐싱하며 비활성화와 텔레포트에 대응한다.

렌더 성능 비교는 숨겨진 Windows 플레이어에서 URP 프레임을 명시적으로 제출하고 GPU readback을 기다리는 방식이다. `BeforeRendered`가 유효 기준이며 이전 `Before`의 일반 백그라운드 창 수치는 사용하지 않는다. 측정치는 동기화 비용을 포함하므로 일반 플레이 FPS라고 보고하지 않는다. 실행 오류가 나면 `Artifacts/Fidelity/build-shipping.log`, `after-shipping-player.log`, `AfterShipping/benchmark.json`을 먼저 본다. 기존 서하 3D 시안과 BGM은 이번 작업에서 변경하지 않았다. Unity 임시 PerformanceTestRun 파일 네 개와 Resources 폴더 메타데이터 삭제는 자동 승인 검토가 거부했다. 삭제하지 않고 정확한 경로를 .gitignore에 추가했다. 실제 Resources 에셋을 이 폴더에 새로 추가할 때에는 폴더 메타데이터 제외 규칙을 제거한다.

## 최신 작업: 네 도시 · 문화 시설 · 실내 캠페인

시작 시 기존 전체 변경을 `d1eacacd`로 main에 커밋·푸시했다. 이번 확장은 그 이후 작업 트리에 반영했다. 실행 경로 `PLAY.cmd` → `Builds/FourCities/AFTERSIGNAL.exe`. 상세 조작·시설 목록·적용 범위: `Documentation/FourCities/README.md`, 공식 건축·경기 규칙 참고: `REFERENCES.md`.

`Runtime/Worlds`에 카탈로그/절차적 메시 제작/실내 층과 승강기/28개 시설 운영/종목별 경기 상태/예측 정산/놀이기구/12장 연계 캠페인/전용 적/지도 검색과 지명 표시/도로 경로 탐색/즉시 NPC 안내를 분리했다. 애프터라이트 북쪽 및 노바 남쪽 문화지구, 에레보스 잠식도시, 네레이드 수중도시를 추가했다. 설정 방문객 정원은 1,631명이고 근거리 생성·활성화하며 경기 선수·승객은 별도다. 인구 정원이 동시에 모두 처리되는 숫자는 아니다.

선수 3종·레이서·심해 NPC 3종·잠식 시민 등 원본 16프레임 시트 8종을 imagegen으로 제작·임포트했다. 영화관은 직접 제작한 60초 3D 단편 `Assets/StreamingAssets/Cinema/SignalTide.mp4`를 VideoPlayer로 상영한다. 원본 Blender 생성기는 `Tools/FourCities/create_cinema.py`. 외부 Big Buck Bunny 다운로드는 자동 승인 검토에서 차단되어 시도 종료했고 자체 제작 영상으로 대체했다.

첫 네이티브 실행의 필수 기능 72개가 통과했다. 시각 검수에서 영화관 위치 덮어쓰기, 순간 이동 후 표시 지연, 수면 하부, 과한 반사, 잘못 배치된 지도 지명, 연출 중 HUD 재표시, 간판의 깊이 검사 누락을 수정했다. 후속 네이티브 실행에서 캠페인 12장 끝까지 진행과 선택 엔딩을 확인했다. 최종 빌드 오류 0/경고 46, 규칙 25개 및 최종 네이티브 검사 77개(출입/층 이동 59 + 표현/캠페인 18) 통과. 집계는 `Documentation/FourCities/validation.json`. 상용 스포츠 게임의 모든 세부 규칙/직접 조작 엔진이나 AAA 컷신·수작업 건축 자산을 완료했다고 설명하지 말 것.


## 최신 작업: NPC·고층 건물·택시·접촉 보정

이전 전체 변경사항을 `2dc22dce`로 main에 커밋·푸시했다. 새 변경사항은 작업 트리에 있다. 세부 구현·조작·범위·건축 참고는 `Documentation/CityContinuity/README.md`.

`NpcGroundSupport`, 사망 전 스프라이트 보관, 남색 Worker/전용 Soldier/교도소 전용 Prisoner, 기존 4방향 도트 스타일 매핑을 적용했다. `HighriseBuilding`/`HighriseInterior`는 424개 고층 타워의 10–30층 진입·승강기·최대 4층 스트리밍·직원·실내 가구를 처리한다. `FacadeGlass`는 타워 커튼월의 파손 위치를 셰이더에 전달하고 E로 해당 층 진입을 제공한다. 열린 시설의 `BreakableGlass`는 실제 개별 판의 충돌을 제거한다. 일반 타워의 큰 충돌체 자체를 파손 구멍 모양으로 다시 생성하는 것은 아니다.

`MultiFloorLift`는 탑승자 이동 중 객실 충돌체를 잠시 제외해 상승 바닥이 캐릭터 내부로 들어가던 문제를 수정했다. `VehicleContactRecovery`는 실제 겹침과 sweep 여유 간격을 모두 처리한다. 최초 네이티브 검증의 차량 후진·승강기 실패를 수정했다. **최종 17개 기능 확인 전부 통과**, `Artifacts/CityContinuity/Final/continuity.json` 및 `Documentation/CityContinuity/essential.json`. 최종 빌드 오류 0/경고 45, `PLAY.cmd` → `Builds/CityContinuity/AFTERSIGNAL.exe`. 빌드 로그는 `Artifacts/continuity-final-build.log`.

`CityTaxiService`/`TaxiNetwork`: 공중 무인 24대, 수상 32대. 전용 Blender 모델과 지정 노선·정차 탑승(120/35 C), F 하차·C 객실 시점. 공중 택시에는 운전자와 무장이 없다. `OceanLife.Surface`는 기존 해안 프리팹에 빠진 수면을 복구한다. 노바 도로와 겹치던 공공시설을 옮기고 지도 좌표도 변경했다. `BuildContinuityDistricts`로 MobilityDistricts와 NeonHarbor를 재생성했다.

서하 3D는 **미완료·미적용**. MPFB/MakeHuman CC0 코어 인체로 새 시안과 163개 뼈대·가중치를 만들고 5차례 검수했지만 머리·측면·팔/손·의상 품질이 부족하다. `Artifacts/CharacterLab/Seo-Trial.blend`와 렌더링, `Tools/CityContinuity` 생성기를 남겼다. 게임에 적용하거나 전체 3D 액션을 완료했다고 보고하지 말 것. MB-Lab/Hunyuan3D 자산은 사용하지 않았다.

미사용 생성 메시 1,923개(507,980,278바이트)의 일괄 삭제는 자동 승인 검토가 `blocked by policy`로 거부하여 파일을 남겼다. `Artifacts/unused-generated-meshes.json`은 읽기 전용 조사 결과다.

## 최신 작업: 지역 지도·도시 간 여객 교통·미래형 건축

작업 시작 전 전체 변경사항을 `270c1e30`으로 main에 커밋·푸시했다. 아래 후속 작업은 현재 작업 트리에 있으며 아직 커밋하지 않았다. `Documentation/FutureCity/README.md`에 조작·도구·제작 원본을 정리했다.

`AtlasViewport`가 현재 위치 기준 820m 지도와 360m 미니맵을 표시한다. 휠 확대, 드래그, 시설/교통 필터, 목적지 목록, 임의 경유지 지정, 내 위치 복귀를 제공한다. `IntercityService`의 항공편 2대와 여객선은 두 도시 사이를 실제 운항한다. 고유한 승객 엔티티가 게이트까지 이동하고 객실에 동일한 외형으로 표시된 후 도착지에서 하차한다. G 탑승권 구매(항공 240 C / 선박 80 C), F 모든 탈것에서 이동 중 탈출. 서하의 탈출 관성과 조종자 없는 탈것의 감속·하강을 처리했다.

`StructuralImpact`, `BreakableStreetProp`, `CollapsibleBuilding`이 차량 중량·속도에 따른 가로등/나무 충돌과 고속 항공기 충돌 시 건물 붕괴를 처리한다. 저장되는 MonoBehaviour는 클래스와 같은 파일명으로 분리해야 한다. 처음에는 다중 클래스 파일의 컴포넌트가 임베디드 MonoScript/누락 스크립트로 저장되어 네이티브 장면 로드가 종료되었다. 파일 분리와 scene/prefab의 GUID 참조 복구 후 실제 도시 진입을 확인했다.

Blender로 8개 건축 프로필과 근거리/원거리/충돌 메시, 미래형 세단·스포츠카·바이크·버스·트럭을 제작했다. 도시의 590개 일반 타워에 독립된 메시/충돌/LOD를 적용했다. 기존 시설 입구와 실내 동선은 보존했다. 물은 깊이 기반 흡수·굴절·환경 반사·거품·불규칙 잔물결을 사용한다. 물리 기반 파괴나 광선 추적 반사는 아니다. Material Maker 1.7은 공식 릴리스에서 설치했으며 `ART-TOOLS.cmd`로 Blender/Material Maker를 실행한다. Material Maker CLI 텍스처 생성은 적용하지 않았다.

주변 구역 주민 생성/해제, 생존·체력 유지, 먼 차량 객실 갱신 제한, 비할당 충돌 검사, 건물/가로등 LOD를 적용했다. GUID 참조가 없는 생성 메시 9,914개와 메타(2.82GB)를 제거했다. `Tools/WorldExpansion/audit_unused_meshes.py`는 읽기 전용 재검사 도구다. 원본 음악과 서하 스프라이트는 유지하며 철회된 서하 3D 실험을 재적용하지 않았다.

게임플레이 필수 확인 15개와 마지막 지도 표시 순서·수면 수정 확인 5개 통과. 결과 `Documentation/FutureCity/essential-gameplay.json`, `essential-presentation.json`. 공항의 짧은 90프레임 구간 평균 16.53ms는 전체 도시 성능 보장을 의미하지 않는다. 최종 Windows 빌드 `Builds/FutureCity/AFTERSIGNAL.exe`(오류 0, 경고 45, 1.58GB). `Builds/active-player.txt` 및 `PLAY.cmd`가 이 파일을 실행한다. 이전 Unity 실행 오류를 해결하고 실제 실행까지 확인했다.

## 최신 작업: 노바 해협과 탈것 조종석

이번 요청 시작 시 기존 변경 전체를 main의 `dbc0255c`로 커밋하고 origin/main에 푸시했다. 그 이후 작업은 [Documentation/NEON-HARBOR.md](Documentation/NEON-HARBOR.md)에 정리했다. 실행 파일은 `Builds/NeonRelease/AFTERSIGNAL.exe`, `PLAY.cmd`가 active-player를 통해 선택한다. 아래 이전 작업의 커밋·빌드 경로 안내는 당시 기록이다.

`WorldAssets/NeonHarbor.prefab`이 기존 확장에 추가된다. 4개 섬, 운하·교량·11개 도로, 추가 건물 538동(기존 도시 125동 포함), 진입 가능한 다층 건물 31동, 주민 외형 24종, main25–30 및 서브 의뢰 4건을 연결했다. 여객선은 실제 해협을 왕복한다. C는 탈것 전용 1인칭, 전차는 독립 포탑과 제자리 선회, 전투헬기·전투기는 우클릭 미사일/R 장전이다. 수영에는 별도 32프레임과 호흡·잠수·턱 오르기를 적용했다. 효과음 18개 출처와 라이선스는 `Documentation/Audio/TRANSPORT-LICENSES.md` 및 빌드 크레딧에 기록했다.

이 확장의 생성 메시를 다시 만들 때는 `WorldExpansionBuilder.BuildNeonHarbor`만 사용한다. 기존 씬 전체를 재생성할 필요가 없다. 배칭 후 꺼진 원본 렌더러·불필요한 메시를 제거하도록 구현했고, 현재 확장의 미사용 메시 781개(약 75MB)와 메타를 정리했다. 수영 PNG 원본은 RGB이므로 임포터의 RGBA32 지정과 전용 처리 경로를 유지해야 한다. 전체 회귀 대신 `-neon-harbor-smoke` 필수 동작만 확인한다.

## 추가 적용: 구매 아이템 이미지 8종

`Resources/Art/ShopItems`에 새 이미지 생성 그림 8개를 추가했다. 야간 순찰/도시 여행 코트, 도시락, 에너지 음료, 따뜻한 정식, 든든한 특선, 커피, 커피와 샌드위치이다. `LifeOption.image`와 `ShopItemArt`로 품목을 연결하고 `LifeHud`가 이미지·이름·효과·가격·구매 표시를 가진 카드로 렌더링한다. 코트는 보유 의상 선택 화면에도 표시한다. 이미지 없는 일반 서비스는 작은 버튼 방식으로 돌아간다. 결제·회복·의상 콜백은 유지했다.

`ArtImporter`의 ShopItems 분기는 Sprite Single, 최대 512px, 가운데 피벗, Bilinear, 무압축 설정을 사용한다. 원본과 프롬프트/품목 안내는 `Documentation/Art/SHOP-ITEMS.md` 및 `shop-item-prompts.json`에 있다. Windows 빌드 오류 0/기존 경고 35, 네이티브 UI 필수 확인 9개 통과. `Artifacts/ShopItems/Final/shop-art.json`이 최종 결과이며 `build-release.log`가 최종 빌드 로그다. 열려 있는 사용자 Unity를 유지하려고 별도 작업 복사본에서 빌드했고 실행 파일을 기존 `Builds/Windows`에 반영했다. 임시 복사본 삭제는 자동 실행 정책이 차단하여 `Artifacts/ShopItems/BuildWorkspace`에 남았다(Git 제외). 커밋·푸시는 하지 않았다.

## 추가 적용: 도시 갱단과 경찰 교전

`CityLife.Initialize`가 도시 외부에서 `CityGangWar`를 설치한다. 12개 보도 구역에 갱단 3명/경찰 2명, 가까운 최대 3개 교전을 배치한다. 도시 첫 화면 전에 초기 배치하고 이후에는 화면 밖에서만 재등장한다. 먼 구역 정리, 120초 재등장 대기, 시체 12초 제거로 개체 수를 제한한다. 갱단은 기존 `Enemies/gunner`를 세 색상 계열로 사용하며 경찰은 전용 에셋을 유지한다.

`FactionCombat`이 실제 총탄 충돌/시야/진영 피해를 공유한다. `GangMember`와 `PoliceOfficer`가 상대를 선택해 접근·조준·사격하고, `WorldActor.Damage`에 공격자 인자를 추가해 NPC 피해가 플레이어 수배로 귀속되지 않게 했다. 갱단 공격은 수배를 올리지 않고 반격을 유발한다. 시민/경찰에 대한 플레이어 공격은 수배를 유지한다. 순찰 경찰은 수배 출동 예산과 별도이며 수배 플레이어를 목격하면 추격 판정에 참여한다.

Windows Release 갱신 완료(오류 0, 기존 경고 35), 실제 도시의 필수 확인 12개 통과. `Artifacts/GangWar/Release/gang-war.json`: `completed=true`, 갱단 사격 25회/경찰 16회, 상호 피해·제압·벽 차단·수배 귀속 확인. 전체 캠페인 검사는 반복하지 않았다. `Documentation/Urban/CITY-GANGS.md`에 안내와 실제 교전 화면이 있다. 커밋·푸시는 하지 않았다.

## 추가 적용: 경찰·특수대응팀 전용 스프라이트

`Resources/Art/LawEnforcement`에 새 이미지 생성 에셋 3종, 총 24프레임을 추가했다. 일반 경찰은 제복·정모에 권총/샷건, 특수대응팀은 헬멧·고글·방탄 조끼에 소총을 든다. `PoliceSpriteCatalog`와 `PoliceOfficer`로 출동 무기에 연결했고 경찰의 `Enemies/gunner` 및 중복 `EnemyWeaponRig` 사용을 제거했다. 사격 트레이서는 조준 프레임 총구 위치를 사용하며, 최종 쓰러짐은 이미 누운 그림이어서 추가 회전하지 않는다.

`PoliceSpriteImporter`가 흰 배경 PNG를 Unity에서 투명화하고 개별 인물을 분리하며 발 피벗과 2.1m 대기 키를 맞춘다. 가져오기 로직 변경 시 `GetVersion()`을 올려 캐시를 갱신한다. 제작 프롬프트와 사용 안내는 `Documentation/Art/POLICE-SPRITES.md`에 있다. Windows Release 갱신 성공(오류 0, 기존 경고 35). `-police-art-smoke`의 세 무기별 로드/배정/크기 9개 확인이 통과했고 게임 URP 렌더링으로 프레임과 투명 가장자리를 확인했다. 최종 결과는 `Artifacts/PoliceArt/Release`, 빌드 로그는 `Artifacts/PoliceArt/build-release.log`. 기존 장시간 검사는 반복하지 않았다. 이 작업은 커밋·푸시하지 않았다.

## 추가 적용: 필수 BGM 6곡

사용자 제공 Suno WAV 6곡을 `Assets/AfterSignal/Resources/Audio/Music`에 원본 그대로 가져왔다. `Documentation/Audio/MUSIC.md`에 장소 배정, 출처, 측정 음량과 반복 방식이 있다. `SignalMusic`은 씬 간 지속되는 스트리밍 재생기로, 같은 곡의 재시작 방지, 다른 곡 1.8초 크로스페이드, 곡 끝 2초 겹침 반복, 대화 중 감쇠, 일시정지/사망과 전체 음소거 연동을 담당한다. 보스전은 컨덕터 접근 시 시작하고 후퇴 시 유지, 처치 후 마을곡으로 복귀한다. ESC 메뉴에서 BGM만 꺼짐/30/55/80/100%로 설정할 수 있다(기본 55%).

이번 음악 검증은 `Artifacts/Music`: EditMode 2개와 PlayMode 3개 통과, 원본 해시 일치, AudioListener 실제 출력 확인. 전체 캠페인 회귀나 주관적 청취 검수를 반복한 것은 아니다. 배치 에디터의 메뉴 PNG 캡처는 생성되지 않았다. 기존 씬과 도시 1.6 기능을 재생성하지 않고 BuildRelease로 Windows 실행 파일 갱신 완료(오류 0 / 경고 36). 새 빌드의 별도 네이티브 실행 검사는 하지 않았다. 커밋·푸시는 하지 않았다.

## 현재 작업: 도시 생활 1.6

추가 요청으로 코드·미사용 파일 정리를 진행했다. 소스 90개와 메타를 기능별 폴더로 이동하고, CityLife를 대화/시설/취침/의뢰 partial 파일로 분리했다. 생성 메시 856개와 메타 및 과거 변환 도구를 정리했다. 자세한 경로와 정리 기준은 `Documentation/PROJECT-STRUCTURE.md`에 있다. **같은 프로젝트의 다른 채팅에서 장소별 BGM을 작업 중이므로 음원·오디오 코드 및 공유 초기화 파일은 정리에서 제외했다.**

정리 후 최종 Windows 빌드는 `Artifacts/Maintenance/build.log`에서 성공(오류 0, 경고 35), 필수 동작 17개는 `Artifacts/Maintenance/Smoke/life-smoke.json`에서 통과했다. 스크립트 메타 90개 해시 유지, 서식 검사 통과, 미사용 생성 메시 0개도 확인했다. 아래 이전 버전의 장시간 측정은 새 버전 성능 보증으로 쓰지 않는다.

사용자는 기능 구현을 우선하고 필수 검증만 요청했다. 1.6 기능 및 가격·조작·AI 연결 안내는 `Documentation/Urban/CITY-LIFE-16.md`에 있다. 아래 1.5 검증 기록은 과거 기준이며 1.6 전체 회귀 검사로 해석하지 않는다.

- 앞뒤·대각선 대시, 승강기/에스컬레이터 접지, 안전한 내부 진입, 카메라 가림 투명화, 열차 및 내부의 부적절한 간판 정리를 적용했다.
- 40개 시설과 40개 고층의 실제 옥상, 외벽 로프 발판·가로등 앵커, 전망 모드와 낮/밤 조명을 연결했다. 시설 NPC 다수 배치와 시민 의복·직업·성격 다양화, 시설 결제·은행·옷장·취침을 추가했다.
- 시민 공격/차량 탈취에 반응하는 수배 1~5단계, 경찰/경찰차/특수대응팀/무장 헬기, 은신 또는 출동 병력 제압으로 해제를 구현했다. 캠페인 적과 경찰은 별도 집계한다.
- `Tools/NpcGateway.py`가 서버에서 `gpt-5.6-luna` Responses API를 호출한다. 이후 사용자가 텍스트로 로컬 저장을 다시 승인하여 `AFTERSIGNAL` 키를 Git에서 제외된 `.env.local`에 저장했다. 실제 요청 2회로 NPC별 대화, 소방서 배달 의뢰(180 C), 의뢰 금지 요청에서 `quest=null`을 확인했다. 키는 출력·커밋하지 않는다. 다른 PC에서는 별도 설정이 필요하다. 현재 실행은 `PLAY.cmd`가 로컬 대화 서버를 함께 시작한다.
- 일반 실행은 `PLAY.cmd`. 이미 빌드된 플레이어와 로컬 Python 대화 서버를 함께 실행하며 키가 없어도 게임을 실행한다. 키를 게임 에셋이나 PlayerPrefs에 넣지 않는다.
- 기존 씬을 유지하며 갱신하는 에디터 메서드는 `ProjectBuilder.CityLifeAndRelease`이다. 구형 전체 씬 생성 후에는 이 업그레이드를 다시 적용한다. 업그레이드는 자신이 추가한 루트만 교체하고 가림 렌더러를 전역 정적 배치에서 제외한다.
- `-life-smoke`는 요청 시에만 실행되는 짧은 통합 검사다. 저장을 스냅샷/복원하고 실제 로프 물리, 앞뒤 대시, 수배 출동/제압, 내부 바닥/NPC, 시설 결제, 의상, 취침, 승강기를 검사한다. 장시간 성능·전체 캠페인·직접 키보드 조작감 검수는 이번에 반복하지 않았다.

도시 생활·코드 정리·BGM·AI 연결 안내를 통합한 최신 소스는 `main`을 공유 기준으로 사용한다. 로컬 API 키와 Windows 빌드 결과물은 Git에 포함하지 않는다.

## 프로젝트 구분

이 저장소는 **AFTERSIGNAL의 Unity 게임 소스만** 포함한다. Unity 6000.4.0f1, URP 17.4.0, Input System 1.19.0, Windows x64를 사용한다. 상위 폴더의 `src`, `public`, `package.json`, Vite 서버는 별개인 이전 웹 버전이다. Unity 작업에서는 상위 웹 프로젝트를 수정하거나 실행하지 않는다.

작업 폴더: `AFTERSIGNAL-Unity`. `Assets`, `Packages`, `ProjectSettings`가 있는 이 저장소의 루트를 Codex 프로젝트와 Unity Hub에 등록한다.

공개 저장소: https://github.com/kwakhyun/aftersignal-unity · 기본 브랜치: `main`. 기존 `codex/unity-source` 브랜치도 유지한다.

GitHub 복제 후에는 `SETUP.cmd` → `OPEN_UNITY.cmd`. 정확한 버전의 SDK를 에디터 설치 경로와 Unity 공식 서버에서 복원한다. 로컬 embedded SDK는 Git에서 제외했고 `Packages/sources.json`에 버전·출처·레지스트리 체크섬을 고정했다. Assets, 씬, 이미지, 오디오, `.meta`는 저장소에 포함된다. 에디터가 다른 곳에 설치돼 있으면 `Tools/Restore-UnityPackages.ps1 -Editor <Unity.exe>`를 실행한다.

기존 ShaderGraph의 Unity 6.4 GUID 호환성 수정 등 14개 파일 차이는 `Packages/compatibility.json`으로 보존한다. 패키지 복원 후 원본 해시가 일치할 때만 적용하고, 알 수 없는 사용자 수정은 덮어쓰지 않는다. 빈 패키지 디렉터리에서 복원한 뒤 재실행 안전성과 최종 파일 체크섬을 검사했다. 결과와 핵심 네이티브 보고서는 `Documentation/Urban/Checkpoint`에 있다.

## 이전 1.5 적용 내용

- 790×660m 도시, 기존 시설 40곳/실내 16종 유지, 블록별 고층 40동과 사방 두 겹 외곽 빌딩 124동 추가.
- 가까운 도보 시점과 차량 뒤에서 전방을 보는 3인칭 추적 시점.
- 승용차·택시·버스·트럭 프리팹, 엔진/제동/충돌/폭발 음원, 차체 체력·손상·폭발·강제 하차 복구.
- 30개 교차로에서 신호·정지선·차선·횡단보도·시민 이동을 동일 도로 좌표로 연결. 66초 신호 주기, 보호된 보행 구간.
- 메인 의뢰 목적지·거리·표식·도시 지상/지도 경로.
- 기존 차량 탑승·탈취·연료·주유·시민 충돌, 학교·병원·본부 등 시설 연결.
- 무기 3종 연속 공격·장전·대시 공격·스킬, 실제 좌우 미러와 앞뒤 스프라이트, 방→승강기/계단→마을, 외벽 로프 침투 캠페인은 이전 작업에서 구현돼 있다.

## 이전 1.5 검증 결과

- 최신 Windows Release 빌드 성공: `Artifacts/Urban15/build8.log`.
- Unity 테스트: EditMode 19, PlayMode 32 통과. 이 실행은 마지막 계단 카메라 조정 전이다. 계단 카메라는 이후 네이티브 실행으로 확인했다.
- `Artifacts/Residence/Release15`: Rail, Expansion, Residence 모두 completed=true이며 네이티브 종료 코드 0. 기존 열차 보스 3회 코어, 후속 캠페인, 방/승강기/5층 계단 왕복, 학교·병원·본부, 외벽 4회 로프/14경비병/귀환을 통과했다.
- `Artifacts/Urban/Release15/urban.json`: 시설 16종 왕복, 실제 주행·조향·후진·하차, 차량 탈취, 시민 충돌, 주유, 차량 저장 복원 통과.
- `Artifacts/Urban15/ReleasePerf`: 도시 기능 검사 22개 통과. 정상 교통 70초 관찰에서 적색 정차 12대, 횡단 시민 40명, 시민 충돌 0, 차선 오차 약 0.95cm.
- 같은 최종 도시 측정: Ryzen 5 7500F + RTX 4060 Ti, 1600×900 D3D11 Release, 평균 17.059ms, P99 16.670ms, 33.34ms 초과 7회. 앞선 FinalPerf 실행은 초과 0회였지만 **최종 측정의 느린 프레임을 숨기거나 전체 60FPS 합격을 선언하면 안 된다**.
- 열차 종료 시 Unity 네이티브 종료 충돌이 한 번 있었다. 동일 빌드 재실행과 후속 검사에서 정상 종료했으나 원인을 해결했다고 확인하지 않았다. `Artifacts/Urban15/Regressions/Rail-shutdown`에 원본 로그가 있다.

## 이전 1.5에서 남긴 검토 항목

1. **계단 최상단 시야**: `CameraRig.cs`의 Stairwell 시점(오프셋 .7, 2.35, -10)이 첫 네 구간 가림을 줄였지만, `Artifacts/Urban15/StairReview/Residence-0/stair-middle-4.png`에서는 6층 연결 다리가 캐릭터를 여전히 가린다. 낮은 카메라만으로 해결됐다고 표시하지 말 것. 6층 다리의 국소 가림 처리 또는 구도 보완 후 실제 캡처로 확인한다. 물리/이동 경로는 이미 통과했다.
2. **실제 키보드·마우스 확인**: 이번 도시 버전의 `-urban-manual` 모드로 WASD, 휠, LMB 사격, R 장전, E 탑승/하차, M 지도를 직접 조작하는 QA는 아직 하지 않았다. UrbanManualProbe는 관찰자이며 합성 입력을 넣지 않는다. 자동 ControlFrame 검사와 수동 조작감 평가를 구분한다.
3. **최종 느린 프레임 분석**: ReleasePerf/frames.csv의 7개 긴 프레임, 창 포커스와 부하 조건, 반복 종료를 확인한다. 장시간 교통 밀집·다른 GPU·최소 사양은 미검증이다.
4. **최종 증거와 배포본 정리**: FinalVideo의 실제 렌더 영상과 오디오를 합치고, 위 결함 해결/수동 QA 후 Collect-Urban.py → Build-UrbanVerification.py → Build-UrbanReview.py를 실행한다. 수집기는 누락·실패 자료가 있으면 중단한다. 버전 1.5 ZIP은 아직 생성하지 않았다. 기존 1.1/1.2 ZIP을 덮어쓰지 않는다.
5. **아트/오디오 잔여**: 시민 외형 다양성, 반복 건물·차체 세부 손상 아트, 실제 스피커 청취 믹싱. 현재 원본 합성 음원은 이벤트/파일 파형 위주로 검사했다. 출시를 위한 최종 아트 검수는 남아 있다.

## 보존과 실행 주의

- 기존 StageId 0–22와 저장 데이터, `.meta` GUID 유지. 도시/공용 실내는 23/24이다.
- 네이티브 QA와 Unity 테스트를 동시에 실행하지 않는다. 같은 PlayerPrefs를 사용하며 QA가 시작/종료 때 복원한다.
- 실행 중인 게임을 먼저 정상 종료한 뒤 빌드한다. 에디터/파이프라인 업그레이드, 대규모 재생성은 현재 작업에 필요하지 않다.
- 과거 일회성 소스 이전 스크립트는 정리 과정에서 제거했다. 현재 에디터의 도시 생활 업그레이드 메뉴를 사용한다.
- 일반 실행은 씬을 다시 만들지 않는다. 에디터 생성 메뉴는 지정된 씬을 다시 작성하므로 수동 씬 변경을 보존한다.
- `Artifacts`, `Builds`, 로컬 SDK, 녹화 파일은 Git에서 제외했다. 현재 PC에는 남아 있다. 과거 Documentation 문서들은 해당 버전의 기록이며 최신 완성도 보증이 아니다.
- 공개 테스트 XML의 어셈블리 경로는 프로젝트 상대 경로로 정리했다. 실행 시각, 결과, 소요 시간과 실패 정보는 원본 그대로 유지한다.

사용자는 다음 Codex 프로젝트 채팅에서 작업을 계속하기 위해 소스 공개 저장소 생성과 커밋·푸시를 요청했다. 이 시점의 소스는 작업 인계 기준점이며 출시 완료 태그가 아니다.

## 2026-09-07 · 갱단 전용 이미지 / 3인칭 자유 시점

- 갱단 3종 × 8동작을 Resources/Art/Gangs에 제작·적용. 흰 배경은 경찰과 공유하는 임포터에서 투명 처리, 조준 이미지 총구와 사격 연결, 전용 쓰러짐 사용. Documentation/Art/GANG-SPRITES.md 및 gang-prompts.json 참조.
- 가운데 마우스 드래그로 자유 회전, HOME으로 기본 카메라 복귀. 회전 기준 이동·대시·로프 좌우 가속 및 조준 평면/표시 방향 적용. 벽 충돌 시 카메라 거리 축소, 메뉴/창 전환 시 커서 잠금 해제. 옥상 전망 모드에서도 마우스 회전 지원. Documentation/FREE-CAMERA.md 참조.
- Windows BuildRelease 성공: errors 0, warnings 35, bytes 363369423. Builds/Windows/AFTERSIGNAL.exe 갱신.
- 필수 네이티브 확인만 실행: Artifacts/GangCamera/Smoke/gang-camera.json 15개 통과; Battle/gang-war.json 12개 통과, 갱단 21발/경찰 15발 및 상호 피해 확인. 전용 스프라이트 24동작 캡처는 Smoke/lineup-*.png, 대표 이미지는 Documentation/Art/gang-lineup.png.
- 이번 요청에 대한 커밋/푸시는 실행하지 않음. 기존 재질·품질 설정 및 다른 작업 변경사항 보존.

## 2026-09-07 서하 이미지·도시 생활

- 첨부된 서하 일러스트를 기준으로 신규 10개 시트 / 80프레임을 제작하고 모든 플레이어 상태에 연결. 방향별 걷기·달리기, 앞뒤 대시, 피격·운전, 무기·로프 동작 포함. WASD 기본 걷기, 같은 방향 키를 0.28초 안에 두 번 누르면 달리기.
- 동쪽 시내 입구 자동 이동(도보/차량), 하단 대화창·투명 서하 초상화·스크롤·응답 생성 경과 표시 적용.
- 투명 차량 유리와 탑승자, 5개 버스 노선/25개 정류장, 정차·승객 교체·30 C 요금·하차벨 구현.
- 식당/카페 주문 접수·레시피 선택·타이밍 조리·테이블 서빙·급여 미니게임. UI에서 진행되며 5명 응대 후 근무 완료.
- 주민 24명, 아파트 호실 선택과 단독주택 5채 방문, 3D 가구와 야간 실내 조명. 주민은 게임 시간표에 따라 출퇴근·교류·귀가하며 주소/현재 일과를 기존 AI 대화 문맥에 전달. 일과 경로는 게임 로직으로 결정.
- 필수 실행 확인 24개 통과: Artifacts/CityLiving/Final/city-living.json. AI API 실호출은 이번 확인에 포함하지 않음. Windows 실행 파일 갱신. 상세 안내 Documentation/CITY-LIVING.md.


## 2026-09-07 — Eight-direction motion and city evolution

- Implemented Seo 8-direction walk/run (8 phases per gait), directional idle, movement-distance timing and stable foot pivots. Added 22 role-specific four-direction NPC sheets and varied bus occupants. Image generation used the built-in tool; final art and prompts are under `Resources/Art/SeoMotion`, `Resources/Art/NpcDirections`, and `Documentation/Art/city-evolution-prompts.json`.
- Added `CommunityWorld`, `CivicRoutine`, `InteriorSupport`, and `NpcSpeech`: expanded school, differentiated hospital/HQ personnel and routines, 12 neighbor apartment entrances, continuous collision foundations, and removal of overlapping legacy batches/themes in rebuilt interiors.
- Added night-time gang mugging/car theft/building robberies, witness and victim reporting, patrol dispatch, panic, player facility heists, ambulance/paramedic pickup and hospital treatment, vehicle garage repair/paint/wheel/spoiler persistence, holding cells, and a neon bar.
- NPC routine decisions are local schedules/state transitions. Existing OpenAI NPC conversation and music connections are preserved.
- Final Windows build succeeded with 0 errors (37 warnings). Essential native `CityEvolutionSmoke` integration passed; detailed result in `Artifacts/CityEvolution/Release/city-evolution.json`. Diagnostics restore original PlayerPrefs on finish. Build uses existing authored scenes; no scene regeneration.
- Launch via `PLAY.cmd`; controls and details: `Documentation/CITY-EVOLUTION.md`. Existing uncommitted work was preserved. This turn did not commit or push.


## 2026-09-07 — Third-person controls, portrait and linked city story

- Fixed SeoIllustrated alpha erasing bright internal hair/highlights by applying border-connected matte removal (SpriteMatte). PortraitImporter uses the same conservative path for the original cutout; new SeoDialogue.png is an opaque high-resolution portrait generated with the built-in image tool and is used by fixed and live conversations.
- Default mouse-look third-person orbit, shoulder offset, central crosshair/world aim, wheel zoom (3.5–18), numeric 1–3 weapon selection, HOME reset. Menus/dialogues/maps release the cursor. Updated HUD control hints.
- Added CityChronicle JSON catalog: 24 main quests across six acts plus 18 side quests in six connected resident chains, 126 objectives. Includes in-world witnesses/evidence terminals, timed scans, gang encounters, deliveries, choices, ending text, reward deduplication, prerequisite locks, tracked guidance, persistent progress, and J journal. Original rail/expansion/breach mission terminals are retained in rebuilt headquarters. Story markers are small diamonds to keep the closer camera clear.
- Added CitySocial exchanges and non-modal NPC-initiated greetings, pausing walkers while chatting. NpcVoice uses role-specific injury lines with speech priority so panic does not immediately overwrite the victim's words. Existing AI conversations remain on explicit talk interaction; local social and scripted story actions do not make extra API calls.
- Essential native ChronicleSmoke checks cover camera/zoom/weapon separation, representative dialogue-scan-delivery progression, old campaign terminal access, save/choice/reward behavior and citizen speech. All 22 passed in the final Windows build, including the final marker presentation changes. Final report: Artifacts/Chronicle/Final/chronicle.json. Windows build succeeded with 0 errors and 37 warnings. No full long-campaign manual playthrough was performed.
- User controls and story overview: Documentation/CHRONICLE.md. Image provenance: Documentation/Art/seo-dialogue-prompt.json. Launch PLAY.cmd. Existing uncommitted work preserved; no commit/push in this turn.


## 2026-09-08 — Movement, directional combat, vehicle reactions and client UX

- Replaced Seo's active movement/action art with 18 newly generated slim-body sheets (184 frames): eight run phases in eight directions, three weapons with four attack phases in eight directions, eight idle and sixteen jump/climb frames. Distance-driven run timing preserves stride across turns; single WASD movement runs and double-tap gait selection is removed. Conservative border-connected matte extraction preserves internal hair and outfit detail. Runtime facing corrections are applied where source sheets face the opposite way.
- Added double jump with a two-jump limit, facade grabbing, upward/lateral climbing, wall kicks and clear-roof mantling. Directional melee, travelling cuts, projectile aim and effects now follow the world attack heading. Updated Seo's vehicle appearance as well.
- Occupied vehicles accelerate away when moving and viable, or evacuate occupants when stopped, badly damaged or blocked. Buses cancel stops/boarding during emergencies. Explosions empty cabins exactly once and eject incapacitated occupants, including the actual gang driver of a stolen car. Police/gang projectiles hit civilian trigger colliders. Added dedicated police-car bodywork, POLICE/112 markings, bumper, light-bar support and antenna.
- Fixed the apartment elevator shaft blocked by the sixth-floor safety slab; platform carry, remote calls and both travel directions now work. Added contextual interaction cards, traversal status, weapon quick slots and active selection, updated pause controls, and aspect-aware HUD scaling.
- Final Windows build succeeded: 0 errors, 39 warnings, 752622914 bytes. Essential native KineticSmoke checks: 23 passed, no errors. Actual checks include wall grab/ascent/kick/roof arrival, vehicle evacuation/escape/explosion, police and gang civilian hits, and the authored elevator travelling from floor six to ground and back with Seo. Reports: Artifacts/Kinetic/Final/kinetic.json and Artifacts/kinetic-final-build.log. No full campaign playthrough or long performance run was performed for this change. Diagnostic save state is restored on completion.
- Launch PLAY.cmd. Controls: WASD run; SPACE jump and second airborne jump; jump toward a wall with W to climb, A/D traverse, S release, SPACE wall kick; E use/call lift. Full guide and captured previews: Documentation/KINETIC.md and Documentation/Kinetic/. Built-in image_gen provenance: Documentation/Art/seo-kinetic-prompts.json; final art: Assets/AfterSignal/Resources/Art/SeoKinetic/.
- Preserved previous uncommitted work and existing authored scenes. No commit or push in this turn.


## 2026-09-08 — Illustrated title screen

- Added a dedicated full-screen title canvas with generated Seo/Afterlight rooftop key art, independent engine-rendered logo, continue/new game/settings/quit actions, keyboard/mouse navigation, entrance fade, save summary and loading progress. The world and in-game HUD are suspended while the title is active.
- Continue reloads the saved area's entrance. New game confirms replacement when a save exists, resets story/life state and starts in Residence. Pause now offers save-and-return-to-title; death menu also offers title return. Settings share master/music/motion/post preferences and persist borderless/window mode. Existing Town music is selected on the title.
- Built-in image_gen artwork: Assets/AfterSignal/Resources/Art/Title/AfterlightTitle.png. Prompt/provenance: Documentation/Art/title-art-prompt.json. User guide and actual captures: Documentation/TITLE.md and Documentation/Title/.
- Existing AFTERSIGNAL process was left running. The new player is in Builds/Title/AFTERSIGNAL.exe; successful builds write Builds/active-player.txt and PLAY.cmd/Play-WithDialogue.ps1 resolve that path within Builds. Default future builds still target Builds/Windows. No authored scenes were regenerated.
- Essential native TitleSmoke check passed all 12 start/menu/save-transition assertions, restoring all touched save values before exit. Artifacts/Title/Final/title.json and Artifacts/title-final-player.log; final build log Artifacts/title-final-build.log. No full campaign run. Preserved prior work; no commit/push for this request.

- Final title release: 758935718 bytes, 0 build errors and 39 warnings. Rechecked the final binary: all 12 title checks passed, no runtime errors; launcher syntax errors 0.


## 2026-09-08 — Restored effects, recorded gunfire and corpse blood

- Restored effect audibility with explicit PCM preloading, per-cue variation fallback, a 28-voice pool, readable mix levels, player-near voice priority, distance attenuation and camera-relative panning. Master changes preserve each voice's intended gain. Added independent SFX volume (default 90%) to title settings and pause; engine/siren loops follow it. BGM is briefly ducked for strong combat feedback.
- Retained and reconnected existing jump/landing/running/tile/metal footsteps, sword impact/swing, dash, hurt/guard, reload and rope sounds. Added wall grab/climb/kick and rope-jump feedback, campaign gunner shots and enemy attack/hurt calls. Running steps now follow the 4.1 m stride timing.
- Downloaded the CC0 Free Firearm Sound Library (Ben Jaszczak, Brian Nelson, Kevin Heras, Matthew Nanney) from OpenGameArt. Generated 18 trimmed 48 kHz mono PCM16 clips from real 1911, Walther PPQ, Bersa, Mossberg, AR-15 and AK-47 recordings. All player, police, gang, campaign and helicopter firing calls use the matching recorded categories; the helicopter uses an AK-47 short burst, not a helicopter-mounted weapon recording. No synthetic gunshots are selected at runtime. Sources, processing and hashes: Documentation/Audio/firearm-sources.json; importer: Tools/Import-Firearms.py; source archive remains in ignored Artifacts/Sound/Source.
- Added CorpseBlood procedural floor surfaces for dead WorldActors and campaign enemies, including manually incapacitated vehicle occupants. Blood follows the actual floor height, excludes vehicles, waits for grounded bodies, and is removed on corpse hiding/destruction or revival/pool reuse. Existing corpse lifetimes are preserved. Shared shader: Resources/Shaders/CorpseBlood.shader.
- Final Windows release succeeded: 762777706 bytes, 0 errors and 40 warnings. All 38 essential native SoundSmoke checks passed in the final player: measured nonzero audio output for the requested cues and six firearm categories, actual movement/jump/attack/hurt/rope events, mute/pause behavior, elevated-floor blood placement, and corpse cleanup. Reports: Artifacts/Sound/Final/sound.json, Artifacts/sound-final-player.log; build: Artifacts/sound-final-build.log. No full campaign or long performance rerun. Audio preferences touched by checks are restored; life/story writes are suppressed.
- Launch PLAY.cmd, which now resolves Builds/Sound/AFTERSIGNAL.exe through Builds/active-player.txt. Existing running player left untouched. Guide/source attribution: Documentation/Audio/RESTORED-AUDIO.md and FIREARM-LICENSE.txt; preview: Documentation/Sound/corpse-blood.png. Prior uncommitted work preserved; no commit or push.


## 2026-09-08 - Vehicle upper bodies and Seo motion refinement

- VehiclePortraits and VehicleCabin now use eighty authored upper-body sprites for Seo and sixteen NPC driver appearances, with camera-relative directions and steering-wheel poses. Other passengers retain their individual cropped torso art. Sedan/taxi torso height .72 m, truck .98 m, bus 1.06 m. Truck seats moved into the front cab. NPC cropping uses sprite pivot/stature metadata, not CPU pixel access.
- Added SeoRefined: forty run phases and forty action poses over five canonical angles with mirrored left views, resolving eight directions. New rise/fall/land/hurt/dash/rope/alternating climb states; shared atlas scale avoids crouch/raised-arm resizing. SeoLocomotion maintains physical stride phase and smooth lean. Removed illustrated actor pixel snapping. Stride and footfalls share 3.4 m; one-frame ground contacts now trigger landing feedback. Existing weapon combat atlases/contact timing retained.
- Final Windows release Builds/Motion/AFTERSIGNAL.exe: 857251823 bytes, 0 errors, 40 warnings. All 17 essential native MotionSmoke checks passed, no runtime errors. Source/readability and missed-landing findings were fixed and the final binary rerun. Report Artifacts/Motion/Final/motion.json, build Artifacts/motion-release-build.log, native log Artifacts/motion-final-player.log. Previews Documentation/Motion/; review Documentation/MOTION-REFINEMENT.md; provenance Documentation/Art/vehicle-motion-prompts.jsonl.
- PLAY.cmd routes to the new Motion player. Existing running player and previous uncommitted changes preserved. No commit/push for this request.

## 2026-09-08 - Seo rigged 3D trial

- User authorized installing modelling tools and requested an original anime-style Seo model/rig test integrated into the game. Installed official Blender 4.5.3 LTS portably under ignored Artifacts/Tools, verified its official SHA-256. No external character mesh, paid asset or 3D service was used.
- Created editable Documentation/Character3D/Seo.blend and deterministic Tools/Character3D/{meshkit,build_seo,rig_animation}.py. Original silver/lilac side-bun hair, sculpted anime face, violet eyes, asymmetric cropped black jacket, slim trousers, fingerless gloves, wrist terminal, boots and three weapons. Final mesh library: 81,734 vertices / 161,222 triangles, 60 deform bones plus source IK controls, Blink shape key, four hair bones, 23 FK animation clips. Unity uses a Generic rig; this is not yet a Humanoid retargeting asset.
- Runtime resource names deliberately differ: SeoModel.fbx versus Seo.prefab. The initial same-name FBX/prefab collision produced a null controller and was fixed before the final build. Seo3DImporter.BuildTrial imports materials/clips/controller/prefab then builds current authored scenes without regeneration. Toon shader converts Blender linear palette correctly for Unity colour properties.
- PlayerMotor adds Seo3DActor, which defaults to 3D, uses continuous world-facing rotation and state crossfades, matches run stride / attack durations, supports jumping/double jump/dash/rope/climb/weapon actions/hurt/death/drive, blinks and moves hair, applies jacket colours and bakes actual mesh afterimages. F8 toggles the prior sprite presentation and persists the choice. VehicleCabin suppresses Seo's duplicate sprite and displays a physical wheel with the rigged driver. NPC sprite presentation remains intact.
- Final release Builds/Seo3D/AFTERSIGNAL.exe: 868799907 bytes, 0 errors, 40 warnings. Essential native Seo3DSmoke checks all 13 passed with no runtime errors, including binding, clips, materials, run progression, two jumps, attack selection, sprite fallback, vehicle entry and one visible mesh driver. Reports: Artifacts/Seo3D/Final/seo3d.json, Artifacts/seo3d-final-player.log, Artifacts/seo3d-final-build.log. Source render and actual final Unity captures are in Documentation/Character3D. No full campaign/performance rerun.
- PLAY.cmd now resolves this new player. EDIT-SEO-3D.cmd opens the source in the prepared Blender. This is an explicitly documented first prototype: facial likeness, extreme garment deformation, two-hand weapon contact and exact steering-wheel grips remain art refinement areas; hair uses bounded secondary rotations, not cloth physics. Existing running player and all previous uncommitted work preserved; no commit/push in this task.

## 2026-09-08 · 서하 3D 철회 / 해안 도시와 전투 연출 확장

- 위의 Seo rigged 3D trial 기록은 철회된 과거 작업이다. Seo3DActor, 리깅·모델·재질·제작 도구·전용 빌드 연결을 프로젝트에서 제거하고 서하를 기존 스프라이트로 복귀했다. 철회한 파일은 저장소 밖 `../AFTERSIGNAL-Withdrawn/Seo3D-20260908`에 보관했다. 범용 Blender 설치는 차량 제작에 사용한다.
- 차량 폭발 섬광·불덩이·연기·불꽃·후속 연소 및 실제 메시 파편, 차량/파편 정리, 기존 탑승자 방출 연동. 고속 충돌은 시민을 속도에 비례해 날리고 지면·벽을 검사한다. 헬기는 체력 소진 후 회전·중력 추락, 지형 충돌 시 폭발·파편 방출.
- 조준점 획득과 총구 충돌 필터를 통일, 총기 사거리 1,400m. 실제 네이티브 실행에서 450m 시민 명중과 중간 벽 차단을 확인했다. 일부 성인 시민 보복, 카타나·블레이드 치명타의 확률적 스프라이트 신체 영역 분리 적용.
- 기존 실제 권총 발사 녹음 유지, 장전 세 단계는 SpringySpringo CC0 에어소프트 조작 녹음으로 교체. NPC 말풍선 은행 340항목, 그중 40항목은 두 화자의 교환 대화.
- 2,200m 폭의 확장 지도에 곡선 해안도로·대각선 도로·공항 순환로·교통·보행·경찰 출동 좌표·지도 경로 연결. 기존 authored scene은 재생성하지 않고 런타임 `WorldAssets/AfterlightExpansion.prefab`을 추가한다.
- 국제공항·해변·항만·야시장·환승역·전망공원·물류센터·호텔·항만 진료소·변전소 10목적지. 식사·휴식·치료·근무·유료 셔틀은 기존 경제/메뉴 시스템과 연결. M 지도 상단 새 목적지, E 시설 안내. 항공기와 선박은 배경 모델이며, 모든 신축 건물의 전용 실내나 실제 운항을 구현한 것은 아니다.
- 시설 전용 NPC 24종 / 96방향 프레임 / 344명 배치. RGBA32 임포트로 배경 투명도를 보존하며 장소별 직업·AI 대화 문맥·보행·위험 반응 연결. 210m 이내 주민 활성화. 생성 프롬프트와 출처는 Documentation/WorldExpansion 참조.
- 원본 세단을 Blender에서 제작해 세단/택시에 적용, 대형 차량 세부 추가. Poly Haven CC0 창호·가로등·벽등·벤치·방호벽 및 4종 PBR 표면, 기존 외벽 아트 재사용. 초기 차체 면 방향, NPC 컴포넌트 직렬화, 파티클 커브 설정, 글자의 벽 통과, 스프라이트 알파 누락을 수정했다. 큰 지형 충돌 삼각형을 세분화했다.
- Windows 빌드 성공, 오류 0. 기존 obsolete API/URP 관련 경고는 40개. 기능 확인 32개 통과, 오류 0: `Artifacts/WorldExpansion/Final/world-expansion.json`, `final-player.log`. 이후 변경은 네온 강도와 진단 카메라의 조명 대기 처리이며, 게임플레이 검증을 반복하지 않고 5구역 화면만 추가 확인했으며 오류가 없었다 (`Artifacts/WorldExpansion/Presentation/world-expansion.json`). 최종 빌드 로그: `Artifacts/WorldExpansion/presentation-release.log`.
- PLAY.cmd는 Builds/WorldExpansion/AFTERSIGNAL.exe를 사용한다. 전체 캠페인/장시간 성능 테스트와 커밋·푸시는 이번 요청에서 실행하지 않았다. 상세 범위·이용 방법·재생성·출처: Documentation/WorldExpansion/README.md.

## 2026-09-08 · 차량 상승 오류 / 교통·해양·거주 시설 확장

- Fixed self-roof grounding: VehicleGround excludes the subject vehicle, other vehicles, Rigidbody debris and steep faces, limits upward ground steps, and recovers previously elevated saved cars. Eight connecting routes join old/new road data. Removed 41 old skyline blockers at exits and rebaked the existing city presentation without regenerating campaign scenes.
- Original Blender motorcycle, sports car, ferry, airliner, combat helicopter, fighter and tank models with editable sources in Documentation/Mobility/Models. Runtime rotates FBX presentation 180 degrees to align Blender-exported noses with vehicle local +X, preserves that rotation during craft banking, spins wheels about the actual axle and steers front wheels. White front/red rear lenses, differentiated body paints, individual aircraft windows and sports-car door details. Cabin uses driver/front/rear/passenger seat positions, E driver/G passenger input, Shift boost and Space braking. Player-controlled military vehicles have weapons.
- Regular nearby traffic cap 48 (previously 14); 45 street-facing parking areas with 276 bays use proximity spawning. Original airport aircraft converted to controllable stands, original cargo ship to a moving craft. Pilot/captain routes carry changing passenger groups; these are scheduled waypoint circuits, with arcade flight/water controls, not a physical aviation simulation. Cargo rendering combined to eight material batches and three hull colliders.
- Added MobilityDistricts prefab: 24 walk-in infill buildings plus military HQ/barracks/maintenance, prison block and passenger terminal. 29 stair/lift connections across 2–4 floors, role-specific furniture and residents, 891 registered expansion NPCs activated nearby. Fixed upper-floor spawn checks and interior wandering to reject table surfaces and other floors. Existing interiors receive additive furniture/wall/ceiling refinements. Buildings are procedural modular environments; bespoke commercial art polish is not complete.
- Ocean seabed, 115 reef groups, 180 proximity-activated fish/turtles/rays, swimming/diving without fall-respawn. Corrected seabed winding and rendered both sides of water. Police arrest/death while wanted transfers to actual prison cell; H surrender near police, sentence/bail release. Footstep mix .65 to .17. Limited close-up NPC speech width so interiors remain visible.
- Essential native functional checks all 19 passed, no runtime errors: Artifacts/Mobility/Final/mobility.json (copy Documentation/Mobility/functional-checks.json). Later display-only changes and removal of 1,783 verified unreferenced generated meshes are followed by a focused final presentation/resource run. Gameplay, locations, controls and scope: Documentation/Mobility/README.md. PLAY.cmd resolves Builds/MobilityRelease/AFTERSIGNAL.exe. Preserve prior uncommitted work; no commit/push requested this turn.
- Final release succeeded: 1,905,528,799 bytes, 0 errors, 42 existing obsolete API/URP warnings. Build log Artifacts/WorldExpansion/mobility-final-release.log. Final player presentation/resource run passed all 3 focused checks with no errors; Artifacts/Mobility/Presentation/mobility.json and presentation-player.log. Seven final game captures and both reports copied to Documentation/Mobility. No further full functional rerun after display-only changes.

## 2026-09-08 · NPC 충돌 / 무기 상점 / 정부청사 / 잔향체 대응

- Added NpcBody: separate upright physical body capsules (player layer 8 vs NPC layer 9 enabled; NPC-to-NPC collision remains ignored), contact replies with cooldowns, and movement overlap handling. Shared WorldActor death callback drops collectible credits with a 78% chance, respects remaining NPC cash and fixed-quest protection. Citizen hits play impact and hurt/death audio. Army visual billboards are children of upright controllers.
- Shared BlastDamage applies distance-scaled damage, cover checks and knockback to people, player, enemies and other vehicles. Queued updates prevent recursive chain explosions. Added ArmoryInventory, PlayerEquipment, CombatProjectile and HeldArmory: 13 items, 7 number-key equipment slots, finite rifle/shotgun/rocket magazines and reserve ammo, purchased grenades, weapon-specific held geometry and real projectile impacts/fuses. Preserve legacy WeaponId 0–2 because sprite/tuning arrays depend on it; additional guns reuse the pistol body pose. Existing starter pistol ammunition rules remain intact.
- CivicRenewal.prefab adds distinct facade treatments to 40 original sites and 29 inhabited expansion buildings, BLACKLINE weapon shop at (950,0,260), and government campus at (840,0,870). Government includes 6 floors, a three-level open atrium with solid galleries/rails, 24 department rooms, stairs/lift, 2 external non-enterable data towers, plaza and road. Final runtime counted 158 FacilityConsole and 152 UsableProp components. InteriorDistinct adds room-theme furniture and interactions to existing indoor scenes. These are modular additive environments, not complete bespoke replacements of every building.
- Facility menus connect banking, treatment, hospitality, food, wardrobe, maintenance and existing minigames. New field jobs require visiting three stations before paying once per game day. Government worker registration grants +10% job pay. NightIllumination adjusts neon/window/sign emission and reuses up to 40 local point lights. Existing BGM and OpenAI dialogue configuration untouched.
- RiftIncursion spawns story-linked procedural articulated memory creatures, dispatches six rifle troops and a combat helicopter, with shared faction targeting/damage, evacuation replies and participation reward. Verified 77 military shots and actual creature damage in the essential run. This uses local gameplay AI, not OpenAI calls.
- SeoEdgeFinish decontaminates only neutral boundary pixels and extrudes transparent gutter RGB while preserving lilac hair and original source PNGs. Native final Seo capture shows intact silhouette. 2D Seo retained; no 3D character restoration.
- Essential functional run: all 16 checks passed, no runtime errors (Artifacts/CivicRenewal/Essential/civic.json). Subsequently improved empty government rooms and obstructed shop displays; focused presentation/resource run passed all 4 checks with no errors (Artifacts/CivicRenewal/Presentation/civic.json), including solid government galleries and Clinic/School interaction details. No repeated full functional suite after these scene edits. No full campaign or long-duration performance run.
- Final Windows build: 1,955,846,238 bytes, 0 errors, 43 warnings. Artifacts/civic-final-build.log; Builds/active-player.txt now resolves Builds/CivicRelease/AFTERSIGNAL.exe through PLAY.cmd. Final reports and captures copied to Documentation/CivicRenewal; usage/scope in Documentation/CIVIC-RENEWAL.md. A final editor-only removal of an unused local in SeoEdgeFinish does not change importer behaviour or runtime output. No commit/push requested this turn; preserve earlier uncommitted work.
