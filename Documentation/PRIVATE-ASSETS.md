# Public source / private visual assets

2026-09-09부터 공개 소스의 현재 트리와 이후 커밋에서 재사용 가능한 시각 에셋을 제외합니다. 로컬 작업 파일은 삭제하지 않습니다.

## 비공개로 유지하는 파일

- `Assets/AfterSignal/Resources/` 아래 `Art`, `Characters`, `Creatures`, `Geometry`, `Maritime`, `Response`, `Security`, `WorldAssets` 및 해당 `.meta`.
- `Assets/AfterSignal/Prefabs`, `Assets/AfterSignal/Scenes` 및 `.meta`. 씬과 프리팹에는 제작한 3D 배치·구조가 직렬화되므로 함께 분리합니다.
- 위치에 관계없이 원본 이미지, 텍스처, 스프라이트 시트, Blender/FBX/OBJ/glTF 등 모델 파일. 문서 폴더의 작업 원본·모델링 미리보기도 포함합니다.
- 공개 예외는 `Documentation/Screenshots/`의 게임 플레이 PNG/JPG 캡처뿐입니다. 이 폴더에 스프라이트 원본이나 모델링 자료를 넣지 마세요.

오디오·폰트와 라이선스, 프로그래밍 소스, 제작 도구·프롬프트 기록, 튜닝·스토리 데이터는 유지합니다. 절차적으로 구조를 만드는 프로그램 코드는 소스에 남고, 그 출력 모델·프리팹·메시 파일은 공개하지 않습니다.

## 다른 PC에서 복원

1. 프로젝트 소스와 같은 버전의 **권한 있는 비공개 에셋 팩**을 별도 전달받습니다. 공개 다운로드 링크는 제공하지 않습니다.
2. 팩의 `Assets/AfterSignal/...` 경로와 `.meta` GUID를 유지하여 프로젝트 루트에 복사합니다. 이미지 파일만 복사하거나 `.meta`를 새로 생성하면 씬 참조가 깨질 수 있습니다.
3. `Tools/Check-PrivateAssets.ps1`을 실행하고 Unity에서 가져오기를 기다립니다.
4. `Tools/Build-Windows.ps1 -Release`로 빌드합니다. 현재 소스 복제만으로는 완성된 플레이어를 재현할 수 없습니다.

기존 작업 PC의 파일은 Git 인덱스에서만 제거했으므로 다시 복원할 필요가 없습니다. `git clean -fdx`는 무시된 로컬 에셋까지 삭제하므로 에셋 백업 없이 사용하지 마세요. 과거 문서의 원본 이미지·모델 상대 링크는 로컬 에셋을 보유한 작업 환경에서만 열릴 수 있습니다.

## 확인과 한계

`node Tools/public-assets.cjs`는 Git 인덱스 전체를 검사합니다. `.gitignore`, 로컬 pre-commit 훅, GitHub Actions를 함께 사용합니다. CI 실패는 이미 전송된 커밋을 원격에서 삭제하는 기능이 아니므로 커밋 전에 로컬 검사를 통과해야 합니다.

**이 변경은 과거 Git 커밋을 재작성하지 않습니다.** 이미 공개됐던 에셋은 기존 커밋·태그·포크·복제본에 남을 수 있습니다. 현재 트리에서 제외하는 것과 과거 공개 이력을 제거하는 것은 다릅니다. 기존 공개 이력을 제거하려면 별도 백업·협업자 조정과 이력 재작성/강제 푸시 작업이 필요합니다.
