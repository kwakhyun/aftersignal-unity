# 타이틀 화면

프로젝트 루트의 `PLAY.cmd`를 실행하면 전용 일러스트가 있는 타이틀 화면이 열립니다.

- **이어하기**: 저장된 구역의 입구에서 이어집니다. 저장이 없으면 비활성화됩니다.
- **새 게임**: 새벽아파트에서 시작합니다. 기존 저장이 있으면 이야기·재화 초기화를 확인하는 창이 먼저 열립니다. 취소하면 진행을 유지합니다.
- **설정**: 전체 음량, 배경음악, 화면 흔들림, 후처리, 창/전체 화면을 조절합니다.
- **게임 종료**: 애플리케이션을 종료합니다.
- 게임 중 `ESC → 저장하고 타이틀로`를 선택하면 현재 구역과 진행을 저장하고 타이틀로 돌아옵니다.

마우스로 메뉴를 선택하거나 방향키와 Enter를 사용할 수 있습니다. 설정·확인 창에서 ESC를 누르면 돌아갑니다. 타이틀에서는 시뮬레이션이 멈추고 커서가 표시됩니다. 시작 시 연결 상태와 진행 막대를 표시하며 중복 실행을 막습니다.

새 일러스트는 서하의 기존 얼굴·의상을 기준으로 고층 옥상과 애프터라이트 야경, 고가 열차를 함께 구성했습니다. 이미지와 글자를 분리해 해상도에 맞춰 표시하며, 타이틀에서는 기존 애프터라이트 BGM을 사용합니다.

- 실제 타이틀: [이어하기 화면](Title/title-continue.png)
- 부가 화면: [설정](Title/title-settings.png), [새 게임 확인](Title/title-confirm.png)
- 일러스트: `Assets/AfterSignal/Resources/Art/Title/AfterlightTitle.png`
- 제작 방식과 전체 프롬프트: [title-art-prompt.json](Art/title-art-prompt.json)
- 구현: `Runtime/Presentation/TitleScreen.cs`, `Runtime/GameDirector.Title.cs`

Windows 빌드는 `Builds/Title/AFTERSIGNAL.exe`에 생성했습니다. 실행 중인 이전 게임은 유지하며, `PLAY.cmd`가 `Builds/active-player.txt`의 최신 성공 빌드를 선택합니다. 기존 대화 API 실행 연결도 유지합니다. 이후 일반 Windows 빌드를 만들면 같은 파일이 새 경로로 갱신됩니다.

필수 네이티브 실행 확인 12개가 통과했습니다. 타이틀 표시·전용 이미지·HUD 숨김·저장 없는 이어하기 비활성화·설정·새 게임 확인과 취소·로딩·실제 이어하기·타이틀 복귀·새 게임의 재화와 이야기 초기화를 확인했습니다. 확인 중 변경한 저장 항목은 종료 전에 복구합니다. 기록은 `Artifacts/Title/Final/title.json`입니다. 전체 캠페인 검증은 진행하지 않았습니다.

최종 Windows 빌드: 오류 0개, 경고 39개. 빌드 로그는 `Artifacts/title-final-build.log`, 최종 실행 로그는 `Artifacts/title-final-player.log`입니다. 런처 PowerShell 구문 오류도 0개입니다.
