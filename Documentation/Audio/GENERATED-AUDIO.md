# Mirelo SFX 2.0 게임 효과음

Fish Audio Drama 3 제작은 사용자 요청으로 중단했습니다. Mirelo SFX 2.0 효과음 71종·123파일을 생성해 로컬 프로젝트에 적용했습니다. 무료 3,336크레딧을 사용했고 추가 결제는 없습니다. BGM 6곡과 기존 효과음 원본은 유지합니다.

제작·설치·검증의 최종 수치는 `Artifacts/AudioProduction/Mirelo/production-summary.json` 및 Unity 검사 XML에 기록됩니다. 사람이 실제로 들었다는 기록과 자동 검사를 구분하며, 청취 검수 전에는 비상업용 로컬 미리보기로 설치합니다.

## 범위와 게임 연결

| 범위 | 파일 수 | 재생 경로 |
| --- | ---: | --- |
| 검격·타격·총기·재장전·충돌·이동·UI·경적 등 52종 | 104 | `Audio/Generated/SFX/<cue>_0.wav`, `<cue>_1.wav` |
| 차량·항공기·선박 엔진, 환경·수중·사이렌 19종 | 19 | `Audio/Generated/Loops/<cue>.wav` |

단발음은 두 변형을 번갈아 선택합니다. 생성 음원이 없으면 기존 음원으로 돌아갑니다. `pistol`과 `pistol_overdrive` 호출은 `gun_pistol`로 연결합니다. 경적은 기존 VehicleHorn의 공간 음원, 엔진은 기존 회전수·가속·거리·실내 필터 설정, 사이렌은 기존 수배 상태와 음량 제어를 사용합니다. 환경음과 수중 반복음도 별도 대체 경로를 사용합니다.

전체 프롬프트와 기존 파일 매핑은 [mirelo-sfx-plan.json](mirelo-sfx-plan.json)에 있습니다. [production-plan.json](production-plan.json)은 초기 8종 계획의 기록이며 실제 설치 목록은 아닙니다. 선택형 스토리 음성 연결 코드는 남아 있지만 Fish 음원 생성과 다운로드는 수행하지 않습니다.

## 무료 한도와 출처

사용자는 Mirelo API 약관 동의, 전용 API 키 생성 및 로컬 보관을 승인했습니다. 키는 Git에서 제외된 `.env.mirelo.local`의 `MIRELO_API_KEY`에 보관하며 명령행, 로그, 음원, 빌드에 포함하지 않습니다. 기존 `.env.local`은 사용하지 않습니다.

승인 한도는 무료 5,000크레딧이고 구매·구독 변경은 하지 않습니다. 초기 스튜디오 생성 16크레딧도 총량에 포함합니다. 각 요청 전에 API preflight로 비용과 잔액 준비 상태를 확인하고, 요청 전에 idempotency key를 로컬 원장에 저장합니다. 완료된 요청은 다시 생성하지 않습니다. 제출 결과가 불확실하면 자동 재요청을 중단합니다.

Mirelo Free 출력은 비상업용이며, 공유·게시할 때 제공사 표기가 필요합니다. 게임 타이틀과 미리듣기에 Mirelo 제공사 표기를 추가합니다. 현재 무료 음원을 상업 배포할 수 있는 것으로 취급하지 않습니다. [이용약관 2.1 및 7.5](https://mirelo.ai/terms), [API 약관](https://mirelo.ai/api-terms), [요금 기준](https://mirelo.ai/pricing/usage)

## 음원 가공과 검수

원본은 `Artifacts/AudioProduction/Mirelo/raw/`, 가공본은 `prepared/`에 보관합니다. 원본 SHA-256과 작업 영수증을 원장에 기록합니다. 출력은 PCM16, 모노, 48 kHz입니다. 단발음의 시작 무음을 줄이고 짧은 양 끝 페이드를 적용하며, 기존 동작 음원의 활성 구간 RMS를 기준으로 음량을 맞춥니다. 피크는 0.8 이하로 제한합니다. 이는 지각 음량·음색을 사람이 평가한 결과와 다릅니다.

SFX 2.0에는 루프 생성 옵션이 없으므로 10초 원본을 250 ms 교차 페이드로 연결해 9.75초 반복음으로 만듭니다. 파일 경계의 PCM 값과 Unity 반복 재생을 검사합니다. 주기 변화나 반복 피로감은 청취 검수가 필요합니다. [공식 API 명세](https://api.mirelo.ai/v3/openapi.json)

기본 `audio_pack.py install`은 `reviewed: true`와 반복음의 `loop_seam_reviewed: true`를 요구합니다. `--local-preview`는 Mirelo Free·비상업용 SFX만 허용하며, 실제 PCM의 피크·시작 무음·루프 경계를 검사합니다. 이 모드는 청취 여부를 true로 바꾸지 않으며 영수증에 `noncommercial_local_preview`를 남깁니다. 음원의 의미·적합성과 실제 스피커 청취를 자동 검사로 승인하지 않습니다.

## 재현과 미리듣기

Python 표준 라이브러리로 API 요청을 실행하며 가공·미리듣기 도구는 NumPy가 필요합니다. 현재 PC의 Blender 번들 Python에는 NumPy가 설치되어 있습니다.

```powershell
# 원장을 확인하고 미완료 작업만 재개. 새 요청은 무료 잔액을 사용합니다.
python Tools/AudioProduction/mirelo_generate.py --scope all --max-total-credits 5000
python Tools/AudioProduction/mirelo_prepare.py --scope all
python Tools/AudioProduction/audio_pack.py validate Artifacts/AudioProduction/Mirelo/prepared/pack.json --local-preview
# 이미 설치된 이름을 덮어쓰지 않습니다. 새로 복원한 프로젝트에서만 실행합니다.
python Tools/AudioProduction/audio_pack.py install Artifacts/AudioProduction/Mirelo/prepared/pack.json --local-preview
python Tools/AudioProduction/mirelo_review.py
python -m unittest discover -s Tools/AudioProduction -p "test_*.py" -v
./Tools/Test-Audio.ps1
```

`Artifacts/AudioProduction/Mirelo/listen.html`을 열면 전체 71종을 기존 음원과 번갈아 들을 수 있습니다. 대표 샘플 모음은 `mirelo-preview.wav`, 순서는 `preview-timeline.json`입니다. 실제 게임 리스너 녹음은 `Playback/`에 별도로 기록합니다.

가져오기는 파일 충돌·잘린 WAV·클리핑·경로 이탈을 검사하고 기존 음원을 덮어쓰지 않습니다. 설치 영수증은 `Artifacts/AudioProduction/receipts/`에 남습니다. 원본과 가공 음원, API 영수증, 생성 음원 폴더는 Git에서 제외합니다. 다른 PC로 옮기려면 이 로컬 음원 묶음과 `.meta` 파일을 별도로 전달해야 합니다. API 키 파일은 음원 묶음에 넣지 않습니다.

## 이번 적용의 검증 결과

2026년 10월 20일 기준: Python 검사 18개와 Unity의 서로 다른 34개 검사(EditMode 22개, PlayMode 12개)가 최종 통과했습니다. 104개 단발음은 각각 SignalAudio를 통해 실제 DSP 출력이 있었고, 19개 반복음은 반복 경계를 넘은 뒤에도 출력됐습니다. 123개 설치 파일의 SHA-256이 가공본과 일치합니다. 패킹된 WAV 용량은 약 27.74 MiB입니다.

24개 파일에서 10 ms를 넘는 시작 무음을 줄였습니다. 가공 파일의 최대 피크는 0.8, 반복 경계의 PCM 차이는 0입니다. 실제 게임 리스너의 전체 SFX 기록은 26.56초이며 RMS 0.022372, 피크 0.274902입니다. 효과음 음소거와 전체 음소거 기록은 각각 피크 0입니다.

최초 PlayMode 실행은 12개 중 11개가 통과했습니다. 반복음 테스트용 AudioSource와 리스너 캡처를 같은 카메라 객체에 붙여 Unity 필터가 충돌한 1개는 재생용 자식 객체로 분리하고 재실행해 통과했습니다. 음원을 다시 생성하거나 검사를 완화하지 않았습니다. EditMode 종료 뒤 살아 있는 ADB 보조 서비스가 실행기를 붙잡는 문제도 에디터 프로세스만 기다리도록 수정했습니다.

- 에셋·구조 검사: `Artifacts/AudioProduction/Mirelo/Tests/editmode.xml`
- 재생 회귀 검사: `Artifacts/AudioProduction/Mirelo/Tests/Playback/playmode.xml`
- 수정 후 반복음 검사: `Artifacts/AudioProduction/Mirelo/Tests/Loops/playmode.xml`
- 최종 검사별 결과와 리스너 측정값: `Artifacts/AudioProduction/Mirelo/verification.json`
- 개별 음원 DSP 출력: `Artifacts/AudioProduction/Mirelo/Playback/sfx-dsp.json`, `loops-dsp.json`

이 결과는 음원 선택·출력·반복·일시정지·음소거·기존 음악 회귀 검증입니다. 오픈월드 전체의 주행·수중·전투 상황을 사람이 플레이하거나 소리를 직접 듣고 판단한 검수로 기록하지 않습니다. `reviewed`와 `human_listening_reviewed`는 false를 유지합니다.

## Windows 실행 파일

`Builds/MireloSfxPreview/AFTERSIGNAL.exe`에 새 음원을 포함해 Development 빌드를 만들었습니다. 빌드 결과는 Succeeded, 오류 0개, 경고 98개, 약 2.28 GB입니다. 경고에는 기존 Unity API 폐기 예정 안내와 URP 스트리퍼 경고 등이 포함됩니다. 새 플레이어의 시작 검사 3개도 통과했고, 실제 타이틀 화면에서 Mirelo 효과음 제공사 표기가 잘리지 않는 것을 확인했습니다.

`Builds/active-player.txt`는 새 실행 파일을 가리킵니다. 이전 `Builds/BaseMobilization/` 실행 파일은 보존했습니다. 비상업용 로컬 미리보기이며 공개 배포나 상업 라이선스 승인을 수행한 빌드가 아닙니다.

- 빌드 결과: `Artifacts/AudioProduction/Mirelo/Build/build-result.json`
- 시작 검사: `Artifacts/AudioProduction/Mirelo/Build/Launch/title.json`
- 실제 화면: `Artifacts/AudioProduction/Mirelo/Build/Launch/title-composition.png`

## 이전 Unity 라이선스 복구 기록

다음은 새 Mirelo 음원 설치 전 수행한 재생 경로 검증 기록입니다. 새 음원의 검증 증거는 Mirelo 폴더에서 구분해 보관합니다.

### 무료 Unity 라이선스와 실제 재생 검사

초기 실행은 오프라인 라이선스 유효기간 만료로 종료 코드 198에서 중단됐습니다. 이후 Hub 로그에서 2026년 10월 20일 02:04:55 KST의 무료 Unity Personal 갱신 완료를 확인했고, 에디터를 다시 실행해 정상 라이선스 인식과 테스트 실행을 확인했습니다. 라이선스 파일을 직접 바꾸거나 유료 플랜을 구매하지 않았습니다. Personal 활성화는 [공식 Unity Hub 경로](https://docs.unity.com/en-us/hub/manage-license)를 사용합니다.

Unity 6000.4.0f1에서 최종적으로 **서로 다른 29개 검사(EditMode 20개, PlayMode 9개)가 통과**했습니다. 생성 효과음 선택·기존 음원 대체 경로, 프로젝트 계약, 음악 파일과 루프 설정, 음성 재생·종료·일시정지·음소거·BGM 감쇠, 음악 장면 전환과 보스 전환을 검사했습니다.

첫 PlayMode 실행은 9개 중 8개가 통과했습니다. 나머지 음악 장면 전환 검사는 Haven 장면이 이제 오픈월드로 자동 이동하여 기존 테스트 제어 대상과 대기 조건이 맞지 않는 문제였습니다. 동일한 Town 음악을 쓰는 School 장면으로 테스트를 수정하고, 12초 제한과 모든 재생 단언을 유지하여 음악 검사 3개를 다시 통과시켰습니다. 이번 검증은 전체 오픈월드 이동 흐름의 검증을 포함하지 않습니다.

음성 검사에는 메모리에서 만든 220 Hz 테스트음을 사용했습니다. 실제 Unity 오디오 리스너에서 48 kHz 스테레오 PCM을 기록하여 재생 중 신호가 있고, 전체 음량을 0으로 바꾸면 출력 피크가 0인 것을 확인했습니다. 기존 BGM도 리스너 PCM에 기록됐습니다. 이는 재생 경로 검증이며, 생성 대사의 발음·연기나 실제 스피커 청취 검수는 아닙니다. 테스트 중 진행 상황 저장을 억제하고 변경한 음량 설정을 복원합니다.

재현할 때 프로젝트 루트에서 실행합니다. PlayMode는 오디오 장치 초기화가 필요하므로 `-nographics`를 사용하지 않습니다.

```powershell
./Tools/Test-Audio.ps1
# 한 단계만 검사할 때:
./Tools/Test-Audio.ps1 -Phase PlayMode
```

스크립트는 매번 새 `Artifacts/AudioProduction/Tests/<UTC 시각>/` 폴더에 XML과 로그를 기록하며, 라이선스 또는 테스트 실패를 오류로 반환합니다. 현재 증거는 다음 위치에 있습니다.

- 종합 결과 및 PCM 측정값: `Artifacts/AudioProduction/verification.json`
- 최초 실행: `Artifacts/AudioProduction/LicenseRecovery/editmode.xml`, `playmode.xml`
- 음악 카탈로그 추가 검사와 BGM 재검사: `Artifacts/AudioProduction/LicenseRecovery/Followup/`
- BGM 녹음: `Artifacts/AudioProduction/LicenseRecovery/MusicOutput/game-audio.wav`
- 음성 재생·음소거 녹음: `Artifacts/AudioProduction/LicenseRecovery/VoiceOutput/playing/game-audio.wav`, `muted/game-audio.wav`

XML과 로그는 각 실행 폴더에 보존됩니다. PCM과 스크린샷 증거는 위 고정 위치의 최신 실행 결과로 갱신됩니다.
