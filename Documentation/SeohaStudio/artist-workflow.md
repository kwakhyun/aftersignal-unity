# 서하 · 전문 캐릭터 제작 과정 조사와 적용

조사일: 2026-09-13. Meshy 구독 없이 로컬 제작을 계속한다는 사용자 요청에 따른 기록입니다. 아래는 공개된 제작 교육·제품 문서에서 확인한 방법이며, 명조·원신·엔드필드 개발사의 비공개 제작 방식을 확인했다는 뜻은 아닙니다.

## 도구와 작업 구분

| 단계 | 전문 제작 도구의 역할 | 이번 로컬 작업 |
|---|---|---|
| 얼굴·몸 조형 | ZBrush의 세분화 단계별 조형, Blender의 세분화·메시 편집 | Blender 4.5.3에서 기존 표정 메시의 눈매·턱·볼·코·입술을 직접 변형 |
| 토폴로지 | Maya Quad Draw 또는 Blender에서 변형에 맞는 면 흐름 설계 | 얼굴의 기존 표정 토폴로지는 보존. 새 모발·상의는 사각형 위주 면과 UV를 제작 |
| 모발 | 먼저 큰 덩어리와 흐름을 설계한 뒤 실루엣과 작은 가닥을 세분화 | 가르마·앞머리·후두부·낮은 묶음·옆머리·잔머리를 별도 메시로 제작. 수정용 곡선 보관 |
| 의상 | Marvelous Designer는 2D 패턴·봉제·시뮬레이션, Blender는 직접 메시 제작과 조형 | 몸에 맞춘 연속 상의와 재킷, 주름 원본, 봉제선·지퍼·버클·손목 장비 제작. 패턴 시뮬레이션을 실행했다고 주장하지 않음 |
| 표면 디테일 | Substance Painter 또는 Blender에서 고밀도 형상을 저밀도 메시의 노멀맵으로 베이크 | 상세 상의의 주름을 2048×2048 탄젠트 노멀맵으로 실제 베이크 |
| 리깅·표정 | 관절 회전·눈 감기·발음 등으로 변형 확인 | 기존 117개 뼈대와 57개 표정 유지, 추가 메시 가중치 부여, 실제 렌더 확인 |

새 상용 프로그램을 설치하지 않았습니다. 이번에 필요한 폴리곤 편집, 곡선, 세분화, UV, 노멀 베이킹과 뼈대 연결 기능은 설치된 Blender에서 사용할 수 있습니다. 전용 프로그램이 작업을 편하게 만들 수는 있지만 조형의 정확도를 자동으로 보장하지는 않습니다.

## 공개 자료에서 가져온 원칙

1. **큰 형태부터 결정:** Blender Studio의 Julien Kaspar 교육은 두상·전신의 면과 비율을 먼저 만들고, 의상과 표면을 추가한 뒤 표정을 시험하는 과정으로 구성됩니다. 서하도 원화의 실루엣과 얼굴 비율을 먼저 비교했습니다. [Blender Studio · Body & Outfit Sculpting](https://studio.blender.org/training/stylized-character-workflow/chapter/5d384eb4a5b8f5c2c32c8505/)
2. **얼굴 토폴로지는 표정용 구조:** 눈꺼풀·입 주변은 모양뿐 아니라 움직일 때의 면 배치를 고려해야 합니다. 이번에는 이미 존재하는 표정 연결을 보존하면서 모든 표정 좌표에 같은 조형 변형을 적용했습니다. [Blender Studio · Planning the Facial Retopology](https://studio.blender.org/training/stylized-character-workflow/5e5407ec8faf011a381510d7/), [Autodesk · Retopology / Quad Draw](https://help.autodesk.com/cloudhelp/2022/ENU/Maya-Modeling/files/GUID-74867E07-5CCD-4D55-B60B-C90C3AD65DF4.htm)
3. **모발의 형태를 메시로 설계:** Blender Studio의 헤어 토폴로지 예제는 모발의 형태 정보를 메시 자체에 담습니다. 서하의 새 모발도 상반신 이미지를 붙인 판이 아니라 회전 가능한 입체 메시입니다. [Blender Studio · Hair Retopology](https://studio.blender.org/training/stylized-character-workflow/9-hair-retopology/)
4. **작은 주름은 별도 상세 원본에서:** ZBrush의 세분화 이력과 Blender의 Multires는 큰 형태와 세부 수정 수준을 구분합니다. 이번 상의는 저밀도 원본과 세분화한 주름 원본을 분리했습니다. [ZBrush · Subdivision Levels](https://help.maxon.net/zbr/en-us/Content/html/user-guide/3d-modeling/modeling-basics/subdivision-levels/subdivision-levels.html), [Blender · Multiresolution](https://docs.blender.org/manual/en/4.5/modeling/modifiers/generate/multiresolution.html)
5. **베이크 후 실제 결과 확인:** 상세 원본의 정보를 저밀도 메시로 옮길 때 케이지 거리, UV 경계 여백과 투영 오차를 확인해야 합니다. 이번에는 Blender Cycles의 Selected to Active 노멀 베이킹을 사용했습니다. [Adobe · Baking parameters](https://www.adobe.com/learn/substance-3d-painter/web/substance-3d-painter-baking-parameters), [Blender · Render Baking](https://docs.blender.org/manual/en/latest/render/cycles/baking.html)

의상 도구 참고: [Marvelous Designer · Simulation](https://support.marvelousdesigner.com/hc/en-us/articles/47358232630937-Simulation). 2D 패턴을 봉제하고 시뮬레이션하는 방식은 이번 직접 메시 제작과 구별됩니다.

## 실제 제작 범위와 한계

이번 변경은 Blender의 실제 메시·재질·뼈대를 편집한 결과입니다. 반복되는 형상 처리와 가중치·렌더 작업은 Python으로 실행했습니다. 전문 아티스트가 펜으로 전체 형상을 수작업 조각했다거나, 얼굴 전체를 새 토폴로지로 교체했다고 표현하지 않습니다.

원화 수준으로 합격한 최종 캐릭터는 아닙니다. 얼굴 개성, 머리카락의 뿌리와 묶음 연결, 의상 재단·접촉, 극단적인 동작과 모발 물리, 게임용 재질·LOD·완성된 액션 세트에는 후속 작업이 필요합니다. Unity 플레이어는 교체하지 않았습니다.

## 편집 원본

최신 파일과 렌더는 Git 제외 경로인 `Artifacts/SeohaStudio/VRoid/Atelier/`에 있습니다.

- `Seoha-Atelier-Review.blend`: 실제 상세 수정과 베이크를 포함한 편집 원본.
- `00 AUTHORING`: 모발 흐름을 수정할 수 있는 제작용 곡선. 기본 숨김. 변환 이전 설계 자료이며, 곡선 수정이 메시로 자동 반영되는 연결은 아님.
- `00 REFERENCE`: Blender에서 확인할 수 있는 원본 서하 일러스트. 기본 숨김.
- `01 HAIR`, `02 OUTFIT`, `03 DETAILS`: 모발·의상·장비의 별도 제작 메시.
- `90 SCULPT`: 상의의 고밀도 주름 원본. 기본 숨김.
- `Seoha-TechnicalTop-Normal.png`: 실제 고밀도→저밀도 베이크 결과.
- `Review-*.png`: 실제 3D 메시 렌더. 이미지 생성 도구로 만든 가상 3D 결과가 아님.
- `atelier-review.json`: 제한된 구조·변형 검사 기록. 미술 품질 합격 증명서가 아님.

재현: 로컬 기반 파일을 유지한 상태에서 `build_atelier.py`를 실행한 뒤 `finish_atelier.py`를 실행합니다. `--quick`은 중간 각도 렌더만 줄이며, 최종 검수는 별도 실행됩니다. 기존 Anatomy 원본도 보존했습니다. 의상이 바뀌지 않은 후속 모발 수정은 `--reuse-bake`로 기존 의상 베이크를 재사용했습니다. `--surface-review`는 표면 수정 후 각도 렌더만 갱신하고, 앞서 확인한 표정·관절 렌더는 보존합니다.
