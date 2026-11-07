# Current handoff · 2026-11-07 · feature and UI/UX improvement pass

- Full summary, verification and the Unity checklist: [Documentation/Improvements/README.md](Documentation/Improvements/README.md).
- Not run in Unity. Runtime C# compiled against reference DLLs with Input System/URP stubs; NPC gateway tests, story validator and public-asset check pass.
- New systems: SaveStore save slots (progress) vs PlayerPrefs (settings), SettingsPanel shared by title/pause, KeyHints + rebindable Controls.inputactions with gamepad, DifficultySettings + GameTuning balance fields, data-driven campaign flags and validator.
- First thing to do in the editor: run EditMode/PlayMode tests and the smoke probes; update smoke expectations that encode old behaviour (car ownership, wanted clearing, quest kinds).
- Earlier handoff notes (2026-09 … 2026-10, about 140 KB) moved to [Documentation/History/HANDOFF-ARCHIVE.md](Documentation/History/HANDOFF-ARCHIVE.md). They describe the state at their dates.

## 보존과 실행 주의

- 기존 StageId 0–22와 저장 데이터, `.meta` GUID 유지. 도시/공용 실내는 23/24이다.
- 네이티브 QA와 Unity 테스트를 동시에 실행하지 않는다. 설정은 PlayerPrefs, 진행은 `persistentDataPath/saves` 슬롯 파일을 함께 쓰며 QA가 시작/종료 때 복원한다.
- 실행 중인 게임을 먼저 정상 종료한 뒤 빌드한다. 에디터/파이프라인 업그레이드, 대규모 재생성은 현재 작업에 필요하지 않다.
- 과거 일회성 소스 이전 스크립트는 정리 과정에서 제거했다. 현재 에디터의 도시 생활 업그레이드 메뉴를 사용한다.
- 일반 실행은 씬을 다시 만들지 않는다. 에디터 생성 메뉴는 지정된 씬을 다시 작성하므로 수동 씬 변경을 보존한다.
- `Artifacts`, `Builds`, 로컬 SDK, 녹화 파일은 Git에서 제외했다. 현재 PC에는 남아 있다. 과거 Documentation 문서들은 해당 버전의 기록이며 최신 완성도 보증이 아니다.
- 공개 테스트 XML의 어셈블리 경로는 프로젝트 상대 경로로 정리했다. 실행 시각, 결과, 소요 시간과 실패 정보는 원본 그대로 유지한다.

이 시점의 소스는 작업 인계 기준점이며 출시 완료 태그가 아니다.
