# AFTERSIGNAL 1.2 · 도시 확장 검증

2026-09-07, Windows 네이티브 Release 플레이어. Unity 6000.4.0f1 / URP 17.4.0을 유지했다. 원본 웹 프로젝트는 변경하지 않았다. 이번 범위는 마을과 후속 노선 확장, Windows 프레임 출력 지연 해결이다. 이전 캐릭터·공격·사운드 개선은 `../Quality/QUALITY.md`의 1.1 기록을 따른다.

## 적용 내용

- 마을을 44m에서 118m로 확장했다. 지상 광장, 5m 산책로, 6m 작업실, 11m 옥상 정원을 실제 승강기와 에스컬레이터로 연결했다. 주민 5명, 보급품 회수와 옥상 안테나 의뢰, 휴식·치료, 챕터 선택 단말을 배치했다.
- 기존 열차 챕터 뒤에 7 + 3 + 3개 전투 구역과 별도 선착장을 추가했다. 총 18개 씬, 4개 챕터다. 새로운 구역에 일반 적 147명을 배치했으며, 수로 로프 횡단과 외벽·고층 유리 진입을 진행 조건에 연결했다. [노선 상세](ROUTES.md).
- 기존 씬 ID 0–3, Stage/Memories/Completed 저장 키와 무기 피해·보스 규칙을 유지했다. 후속 의뢰는 세 개의 Expansion 키에 별도로 저장한다. 보급과 안테나 보상은 중복 지급을 막았다.
- 표시 메시를 재질·공간 단위로 합치고 원본 충돌체를 유지했다. 승강기, 유리, 셔터, 로프 앵커와 포탈은 합치지 않는다. 이동하는 도시 메시도 자신의 이동 루트 아래에 남긴다.
- MSAA 2x, SSAO 저해상도 처리, 1024 그림자/2캐스케이드/40m, 불필요한 카메라 Opaque Texture 복사 제거를 적용했다. 신규 창문 재질의 과한 광택과 열린 출구 셔터의 잔류 표시를 줄였다.

## 실제 실행 범위

EditMode 14개, PlayMode 21개가 통과했다. 기존 전투 회귀, 승강기의 양방향 탑승 운반, 에스컬레이터와 상층 발판의 연결, 중복 보상 방지, 진행 조건, 메시 결합 후 충돌체 보존을 검사했다. 테스트 원문은 `Evidence/EditMode.xml`, `Evidence/PlayMode.xml`에 있다.

확장 경로의 녹화 없는 전체 주행을 두 번 완료했다. `FocusedPerformance`는 19개 확인 지점을 통과하고 새 일반 적 147명을 처치했다. 마을의 수직 동선, 선착장 왕복, 두 주민 의뢰, 13개 전투 구역, 각 챕터 종료 후 마을 복귀를 포함한다. 이동·공격·로프·상호작용은 일반 게임의 `ControlFrame` 입력 경계를 통과한다. 이 검증은 자동 입력을 사용하며 장시간의 주관적인 사람 플레이 평가와는 구분한다.

최종 재질 반영 빌드의 영상 주행과 기존 열차 챕터 재검증 결과는 아래 최종 실행 표에 기록한다. QA는 시작 전 저장 키 6개를 보관하고 종료 시 복원한다. 테스트와 다른 QA 인스턴스를 동시에 실행하지 않는다.

| 실행 | 결과 | 실제 확인 |
| --- | --- | --- |
| FocusedPerformance | 완료, 19지점 통과, 오류 0 | 147명 처치, 세 후속 챕터와 두 주민 의뢰 |
| VisualRelease | 완료, 19지점 통과, 오류 0 | 최종 재질·출구 수정 빌드의 동일 전 구역 주행과 영상 |
| OriginalRelease | 완료, 4지점 통과, 오류 0 | 역 6명, 객실 10명, 지붕 6명 + 보스, 로프 8회, 코어 3회, 마을 엔딩 |

최종 셔터가 출구 위에 남지 않는 모습과 톤을 낮춘 창문을 실제 캡처에서 확인했다. 녹화 없는 전체 확장 성능 기록은 마지막 창문 재질 조정 직전의 동일 런타임·성능 설정을 사용했고, 최종 빌드는 영상 전체 주행 및 기존 캠페인·역 측정에서 다시 검증했다.

## 60FPS 측정 조건과 결과

기기: AMD Ryzen 5 7500F, NVIDIA RTX 4060 Ti. 1600×900 창 모드, Mono Release, 목표 60FPS. Unity가 보고한 모니터 주사율은 143.992Hz다. 최종 프로파일은 Direct3D11 BitBlt, vSync 0, `Application.targetFrameRate = 60`이다. 기준 기기 사양은 별도로 지정되지 않았으므로 이 PC의 측정 범위에만 성능 결과를 적용한다.

다른 Unreal 게임 실행은 사용자 요청 없이 종료하지 않았다. AFTERSIGNAL 창을 활성화한 상태에서 측정했다. 에디터 테스트·빌드·동영상 인코딩을 청정 성능 주행과 동시에 실행하지 않았다. 각 씬의 시작 2초와 씬 로딩은 샘플에서 제외한다. 평균 수치에 로딩 시간이 포함된다는 의미가 아니다.

| 녹화 없는 실행 | 샘플 | 평균 | p95 | p99 | 33.34ms 초과 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 변경 전 1.1, 역 70초 노선 | 2,664 | 25.52ms | 53.54ms | 64.20ms | 1,324 |
| 최종 1.2, 같은 역 70초 노선 | 4,081 | 16.67ms | 16.67ms | 16.70ms | 0 |
| 확장 전체, 375.37초 기록 | 22,520 | 16.67ms | 16.67ms | 16.70ms | 0 |

역 비교는 같은 1600×900 해상도, 같은 이동·공격 시퀀스이며 두 실행 모두 적 6명, 공격 24회를 기록했다. 배경 배치와 렌더 품질 설정, Direct3D 출력 방식이 함께 바뀐 전후 비교다. 확장 전체 샘플에서 가장 느린 프레임은 17.26ms였다. 최종 빌드의 기존 객실과 지붕 보스 주행도 각각 p99 16.72ms, 16.70ms, 33.34ms 초과 0회를 기록했다. 각 실행의 구간별 원문은 `Evidence` 및 `verification.json`, 전체 합산은 `aggregate-performance.json`에 있다.

`FocusedPerformance`의 모든 기록 프레임을 사용하며 느린 프레임이나 비활성 샘플을 사후 삭제하지 않았다. 전 구역의 포커스 기록은 100%다. `FullPerformance`에서는 다른 게임으로 포커스가 넘어간 구간도 그대로 보존했다. 그 실행은 Transit의 p99가 36.41ms로 나빠졌으며, 이를 60FPS 합격 데이터로 사용하지 않았다. `verification.json`은 원시 통계와 별도로 명시한 포커스 통계를 함께 담는다.

프레임 대기 마커에서 `TimeUpdate.WaitForLastPresentationAndUpdateTime`, `DXGI.WaitOnSwapChain`의 비용이 컸다. GPU 비용이 낮아도 출력 큐에서 대기할 수 있다. D3D11 전환만으로는 해결되지 않았고, Flip 대신 BitBlt 출력을 사용한 뒤 제한 주기가 안정됐다. 이는 이 PC에서 비교한 결과다. Unity가 일반적으로 권장하는 Flip을 모든 PC에서 비활성화해야 한다는 결론은 아니다. [Unity 출력 마커](https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-markers.html), [Windows Swapchain 설정](https://docs.unity.cn/2023.3/Documentation/ScriptReference/PlayerSettings-useFlipModelSwapchain.html), [vSync와 프레임 제한](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings-vSyncCount.html).

확장 구간의 Unity GPU FrameTiming 평균은 구역별 약 0.59–0.69ms, Unity 메모리 카운터의 구역별 최대는 약 110–114MiB였다. 이 메모리는 시스템 전체나 GPU VRAM 사용량이 아니다. Release에서 GC 할당과 실제 Draw Call 카운터는 지원되지 않아 `-1`로 남긴다. 이전 기준 파일에 기록된 GC 0도 유효한 측정값으로 간주하지 않는다. `geometry-batches.txt`는 메시 표시 단위 감소이며 실제 Draw Call 측정치로 대체하지 않는다. CPU/GPU 프레임 원문은 각 `frames.csv`에 있다. 수 시간 누적, 최소 사양, 여러 드라이버·화면 비율은 미검증이다.

## 화면과 영상

모든 PNG와 영상은 실제 플레이어의 URP 카메라와 HUD에서 캡처했다. 컨셉 이미지나 연출용 가짜 씬을 사용하지 않았다. 영상은 1600×900, 30FPS의 연속 녹화이고 게임 오디오를 함께 저장했다. 캡처와 인코딩의 추가 비용이 있으므로 영상 실행의 성능은 녹화 없는 측정과 구분한다.

- `Media/Haven-before-1.1.png`: 작업 전 1.1의 실제 마을 진입 화면.
- `Media/Haven-0-entry.png`, `Haven-0-promenade.png`, `Haven-0-rooftop.png`: 최종 마을 진입·상층·옥상. 공간이 확장되었으므로 상층과 옥상은 기존 화면의 동일 좌표 비교가 아니다.
- `Media/Haven-0-gameplay-with-audio.mp4`: 마을 승강기·에스컬레이터·주민 의뢰의 연속 주행.
- `Media/Archive-2-gameplay-with-audio.mp4`: 승강기, 외벽 로프, 고층 유리 진입과 전투.
- `Media/Origin-4-gameplay-with-audio.mp4`: 마지막 구역의 상층 진입과 다수 적 전투.

## 재현과 되돌리기

일반 플레이는 프로젝트 루트의 `PLAY.cmd`를 실행한다. WASD 이동, 좌클릭 공격, 우클릭 유지 로프, Space 점프, Shift 대시, Ctrl 방어, Q 스킬, E 상호작용, 1–3 무기 변경이다. 첫 챕터 완료 후 마을의 노아와 대화하고 광장 아래 노선 단말을 사용한다.

PowerShell에서 프로젝트 루트를 기준으로 다음을 각각 단독 실행한다. QA 중에는 AFTERSIGNAL 창을 활성화한다. 정상 플레이에는 이 QA 플래그를 넣지 않는다.

```powershell
& .\Builds\Windows\AFTERSIGNAL.exe -expansion-smoke -quality-no-captures -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -quality-output "$PWD\Artifacts\Expansion\MyExpansionRun"
& .\Builds\Windows\AFTERSIGNAL.exe -aftersignal-smoke -quality-campaign -quality-no-captures -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -quality-output "$PWD\Artifacts\Expansion\MyRailRun"
python .\Tools\Record-Quality.py MyStationRun --no-video --no-captures
.\Tools\Test-Project.ps1
.\Tools\Build-Windows.ps1 -Release
```

영상이 필요하면 실행 파일에 `-quality-ffmpeg "<ffmpeg.exe의 절대 경로>" -quality-record-stages Haven,Archive,Origin`을 추가한다. 설치된 FFmpeg 경로는 PC마다 다르며 배포 게임 실행에는 FFmpeg가 필요 없다. 씬 재생성은 `AFTERSIGNAL / Expansion / Build town and new districts`, 메시 재결합은 `AFTERSIGNAL / Quality / Bake geometry batches`를 사용한다. 씬 생성 도구는 수동 씬 편집본을 덮어쓸 수 있으므로 편집본을 보존한 후 사용한다.

기존 파일과 GUID 비교는 `changed-files.json`, 증거 해시는 `evidence-manifest.json`에 있다. 이 작업 전 원본은 로컬 `Artifacts/Expansion/Before`에 보존했다. 완전한 되돌리기는 보존한 `AFTERSIGNAL-Unity-v1.1.0.zip`을 별도 폴더에 풀어 실행하면 된다. 1.1은 새 Expansion 저장 키를 읽지 않는다. 새 진행 데이터를 직접 삭제할 필요는 없다. 현재 프로젝트 위로 ZIP을 덮어써서 새 파일을 섞는 방식은 사용하지 않는다.

## 남은 품질 차이

새 공간은 플레이 가능한 자체 제작 모듈 환경이다. 반복 창문·벽·소품의 고유 아트 밀도, 일부 무기의 달리기·공중·방어 핵심 포즈, 컷신의 개별 연출은 추가 제작 대상이다. 기존 보스 이후의 신규 보스 종류나 별도 RPG 성장 규칙은 이번에 추가하지 않았다. 일부 주민 위치에서는 플레이어 스프라이트가 겹쳐 보일 수 있다. 다음 아트 작업은 반복 배경의 개별화와 NPC 대화 위치 정리, 남은 무기 포즈의 원본 제작 순서가 적절하다. 이 결과를 ANNO: Mutationem 수준 또는 상용 출시 품질 달성으로 표시하지 않는다.

새로운 외부 아트·음원 구매 없이 기존 자체 제작 스프라이트·DSP 오디오와 직접 구성한 Unity 메시를 사용했다. Noto 계열 폰트와 공식 패키지 라이선스는 `../ASSETS.md` 및 동봉된 고지를 따른다.
