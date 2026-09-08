# 효과음 복구와 실제 총성

`PLAY.cmd`로 최신 실행 파일을 열면 적용됩니다. 타이틀의 **설정 → 효과음** 또는 게임 중 **ESC → 효과음**에서 BGM과 별도로 조절할 수 있습니다. 기본 효과음 음량은 90%이며 기존 전체 음량 설정은 유지합니다.

기존 동작 효과음 파일을 미리 읽어 두고, 호출 음량을 새 BGM 믹스에 맞춰 조정했습니다. 효과음 28개를 동시에 처리하며 가까운 플레이어 동작을 우선합니다. 존재하는 변형을 선택하고 누락된 소리는 경고로 남기므로 조용히 누락되지 않습니다. 차량 엔진과 경찰 사이렌도 효과음 음량을 따릅니다.

- 서하: 도검 공격과 타격, 권총, 장전, 기본 달리기, 타일·금속 발소리, 점프·이단 점프·착지, 대시, 피격·방어, 로프 부착·해제.
- 등반: 벽 붙잡기, 등반 접촉음, 벽 차기와 로프 점프.
- NPC: 캠페인 적의 발사·근접 공격, 시민·경찰·갱단 피격과 쓰러짐.
- 총성: 거리 감쇠와 카메라 방향에 따른 좌우 위치, 근접 발사 우선 처리. 전투 타격 순간에는 BGM을 잠깐 낮춰 피드백을 구분합니다.

## 총기별 실제 녹음

출처는 [The Free Firearm Sound Library](https://opengameart.org/content/the-free-firearm-sound-library)이며, 공개 페이지와 라이브러리 메타데이터를 확인했습니다. 제작자는 Ben Jaszczak, Brian Nelson, Kevin Heras, Matthew Nanney입니다. 라이선스는 [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)입니다.

| 인게임 사용 | 실제 녹음 소스 | 리소스 |
|---|---|---|
| 서하 권총·강화 사격 | 1911 | gun_pistol |
| 경찰 권총 | Walther PPQ | gun_police_pistol |
| 갱단 권총 | Bersa | gun_gang_pistol |
| 경찰 산탄총 | Mossberg | gun_shotgun |
| 특수부대·캠페인 소총 | AR-15 | gun_rifle |
| 헬기 연사 | AK-47 짧은 연사 | gun_auto |

종류마다 3개씩 총 18개 WAV를 적용했습니다. 헬기에는 실제 AK-47 연사 녹음을 사용하며, 헬기 탑재 총기를 현장에서 녹음한 자료는 아닙니다. 기존 합성 권총 호출도 실제 녹음으로 연결됩니다.

긴 무음에서 개별 발사·연사 구간을 추출하고, DC 보정·안티앨리어싱 리샘플링·끝부분 페이드·피크 정규화를 거쳐 48 kHz 모노 PCM16으로 저장했습니다. 원본 녹음은 변경하지 않았습니다.

- 게임용 파일: `Assets/AfterSignal/Resources/Audio/Firearms/`
- 파일별 출처·해시·추출 시점: [firearm-sources.json](firearm-sources.json)
- 재현 도구: `Tools/Import-Firearms.py`
- 원본 다운로드: `Artifacts/Sound/Source/Prepared-SFX-Library.7z` (작업용, Git 제외)

## NPC 바닥 피자국

사망한 시민·경찰·갱단·캠페인 적 및 폭발 차량에서 나온 사망 탑승자에 적용합니다. 시체 아래의 실제 지면을 찾아 불규칙한 피자국과 작은 방울을 표시합니다. 실내층·옥상 높이를 반영하고 차량 차체에는 투영하지 않습니다. 공중에서 떨어지는 시체는 지면에 닿은 뒤 표시합니다.

피자국의 수명은 시체와 연결됩니다. 시체가 숨겨지거나 제거되거나, 재활용된 시민의 체력이 초기화되면 함께 사라집니다. 기존 시체 유지 시간은 바꾸지 않았습니다.

[실제 실행 화면](../Sound/corpse-blood.png)

## 확인 범위

요청 기능에 한해 네이티브 실행을 확인했습니다. 효과음별 오디오 출력, 6종 총성의 실제 녹음 연결, 달리기·이단 점프·공격·피격·로프의 이벤트, 독립 음소거, 높은 바닥의 피자국과 시체 수명 동기화를 확인했습니다. 결과와 출력 레벨은 `Artifacts/Sound/Final/sound.json`에 기록합니다. 전체 캠페인이나 장시간 성능 검사는 진행하지 않았습니다.

최신 실행 파일은 `Builds/Sound/AFTERSIGNAL.exe`이며 `PLAY.cmd`가 자동 선택합니다.

Final release: 38 essential checks passed; 0 build errors, 40 warnings. Logs: Artifacts/sound-final-build.log and Artifacts/sound-final-player.log.
