# 프로젝트 구조와 파일 정리

2026-09-07 기준. Unity 6000.4.0f1과 기존 런타임/에디터 어셈블리는 유지한다. 스크립트를 이동할 때 `.meta`를 함께 옮겼으므로 씬과 프리팹의 MonoBehaviour GUID가 바뀌지 않는다.

## 소스 위치

| 경로 (`Assets/AfterSignal/` 아래) | 책임 |
| --- | --- |
| `Runtime/` | GameDirector, GameTuning, SignalHud, SignalAudio 등 기존 초기화·설정 진입점 |
| `Runtime/Player/` | 입력, 이동·대시, 로프, 캐릭터 스프라이트·의복 |
| `Runtime/Combat/` | 무기 행동, 적 AI, 피해 대상과 무기 표현 |
| `Runtime/Campaign/` | 캠페인 목록, 체크포인트, 의뢰 안내 |
| `Runtime/World/` | 출입·상호작용, 승강기·에스컬레이터, 주민, 열차 이동 |
| `Runtime/City/` | 도시 시설·내부, 옥상, 시간과 시뮬레이션 |
| `Runtime/City/Traffic/` | 도로·신호·차량·보행자 |
| `Runtime/City/Life/` | 재화, NPC 프로필, AI 대화, 서비스·취침·히든 의뢰 |
| `Runtime/City/Law/` | 수배, 경찰 경로·무기·차량·헬기 |
| `Runtime/Presentation/` | 카메라·가림·효과, `Hud/`의 HUD 구성 요소 |
| `Runtime/Performance/` | 렌더링·프레임 예산과 씬 배치 정보 |
| `Runtime/Diagnostics/` | 명시적 실행 옵션으로만 켜지는 기능 검사·진단 |
| `Editor/World/`, `Editor/City/` | 기존 씬 생성 및 도시 업그레이드 |
| `Editor/Maintenance/` | 메시 배치와 미사용 생성 메시 정리 |

`CityLife.cs`는 초기화·상호작용과 공통 패널 상태를 맡는다. `CityLife.Conversation.cs`, `.Services.cs`, `.Home.cs`, `.Errands.cs`는 같은 컴포넌트의 기능별 partial 구현이다. 씬에서 연결하는 컴포넌트는 기존 `CityLife` 하나로 유지한다.

장소별 BGM은 다른 작업에서 진행하므로 오디오 소스·음원·음원 생성 도구와 공유 초기화 파일은 이번 정리 대상에서 제외했다. `ProjectBuilder.cs`, `ArtImporter.cs`, `QualityPass.cs`의 기존 경로도 유지했다.

## 제거한 파일과 유지 기준

- 씬·프리팹·에셋·프로젝트 설정에서 GUID 참조가 없는 생성 메시 **856개와 `.meta` 856개**, 합계 **54,280,862바이트(약 51.8 MiB)**를 제거했다.
- 이미 적용된 `upgrade_urban15.py`, `Integrate-Urban.py`, `Apply-ActionPass.py`와 현재 패키지 복원 도구로 대체된 `Repair-LocalPackageCache.ps1`을 제거했다.
- 어떤 품질 설정에서도 사용하지 않는 Unity 템플릿의 Mobile 렌더 파이프라인·렌더러 및 메타 파일을 제거했다. 현재 두 품질 항목은 모두 기존 PC 파이프라인을 사용한다.
- 런타임에서 이름으로 로딩하는 Resources, 원본 아트·글꼴, 씬 재생성에 필요한 에디터 입력 자료, 과거 검증 기록과 현재 작업 중인 자료는 유지한다. 직렬화 참조가 없다는 이유만으로 이들을 미사용으로 판단하지 않는다.

정리 내역은 로컬 `Artifacts/Maintenance/cleanup-applied.json`, 소스 이동 내역은 `Artifacts/Maintenance/source-layout.json`에 기록했다.

## 유지 관리

`Tools/Cleanup-GeneratedAssets.py`는 기본적으로 검사 결과만 기록한다. `--apply`로 확인된 미사용 생성 메시만 제거한다. 이름 패턴과 프로젝트 경로를 검증하며 오디오·원본 아트는 삭제 대상에 포함하지 않는다. 에디터에서도 `AFTERSIGNAL > Maintenance > Remove unused generated geometry`를 사용할 수 있다. 전체 씬 메시 배치 후에는 이 정리가 자동으로 실행된다.

`.editorconfig`는 C# 네 칸 들여쓰기와 LF 줄바꿈을 지정한다. `pwsh -File Tools/Format-Source.ps1`은 C# 구문 분석기로 서식을 정리하고 모든 토큰의 종류·내용이 동일한지 확인한다. `-Check`는 파일을 쓰지 않는다. 별도 BGM 작업과 공유하는 파일은 제외한다.

정리 후 Windows Release 빌드는 성공했다(오류 0, 경고 35). `Artifacts/Maintenance/build.log`에 원본 로그가 있다. 이동한 90개 스크립트의 메타 파일 해시는 모두 유지됐으며, 생성 메시 재검사 결과 미사용 0개다. 서식 검사도 통과했다.

`Artifacts/Maintenance/Smoke/life-smoke.json`에서 도시 생활 필수 동작 17개가 통과했다. 앞뒤 대시, 실제 로프 등반, 수배 5단계 출동·해제, 실내 바닥·다수 NPC, 결제, 의상 변경, 취침과 승강기를 확인했다. 전체 캠페인, 장시간 성능 측정, 실제 OpenAI 호출, BGM 검수는 이번 검사에서 수행하지 않았다.
