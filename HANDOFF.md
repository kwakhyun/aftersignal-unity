# 다음 Codex 작업을 위한 인계 — 2026-09-08

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
