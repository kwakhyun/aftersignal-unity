# 서하 제작 도구 검토 · 2026-09-13

원화/명조/엔드필드/원신 수준의 모델은 아직 완성되지 않았습니다. 프로그램 설치나 메시 수치 검사로 미술 품질을 인증하지 않습니다. 게임 스프라이트와 Windows 빌드는 변경하지 않았습니다.

이전 스터디는 계산으로 생성한 얼굴 곡면·머리카락 덩어리·의상 단면에 너무 의존했습니다. 눈꺼풀·입·턱·모발 층·재단과 주름을 충분히 조형하지 못한 상태에서 세분화와 재질을 추가했습니다. 이는 Blender의 표현 한계로 확인된 문제가 아니라 현재 제작 방식과 결과물의 한계입니다.

| 도구 | 실제 상태 | 판단 |
|---|---|---|
| Blender 4.5.3 LTS | 기존 설치 유지 | 조형·의상·리깅 및 실제 메시 검수용 |
| VRM Add-on for Blender 4.7.1 | 공식 릴리스 설치·활성화, 설정 저장 | `IMPORT_SCENE_OT_vrm` 등록 확인. VRM·표정·MToon 편집 경로 확보 |
| VRoid Studio 2.14.0 | 사용자가 설치·실행 완료. 실제 편집, `.vroid` 저장, VRM 1.0 내보내기 완료 | 최신 Blender 제작 기반. [현재 수정본과 한계](README.md) |
| Hunyuan3D-2 | 이전 저장소와 빈 Python 환경 확인 | 라이선스가 한국을 사용 지역에서 제외하므로 가중치 다운로드·추론하지 않음 |
| TripoSR | 공식 MIT 코드·모델, CUDA 의존성 설치 및 실제 로컬 추론 | 정면·사선·측면·후면·얼굴 렌더 검수 결과 불합격 |

VRoid는 얼굴·눈·모발·표정을 편집할 출발점으로 선정했습니다. 프리셋만으로 원화급 품질이 나온다는 뜻은 아닙니다. 얼굴과 표정이 합격하기 전에는 나머지 의상과 전체 액션을 대량 제작하지 않는 순서로 전환합니다.

## TripoSR 시험 결과

- 공식 코드 버전: `107cefdc244c39106fa830359024f6a2f1c78871`.
- 공식 가중치: `stabilityai/TripoSR`, MIT.
- RTX 4060 Ti 8 GB에서 추론 완료. GPU 최대 할당 약 4,203 MiB.
- 40,897 정점, 81,794 삼각형. 리깅되지 않은 실제 색상 메시.
- Windows에서 선택적 CUDA C++ 추출기 대신 scikit-image CPU marching cubes를 사용했습니다. 신경망과 가중치는 변경하지 않았습니다.
- 얼굴·손가락이 소실되고 모발이 뭉치며, 의상 색상과 형태도 흐려졌습니다. 서하 제작 기반으로 채택하지 않습니다. 이 도구의 시험 결과를 모든 3D 서비스에 일반화하지 않습니다.

`Seoha-Reconstruction-*.png`는 image_gen으로 만든 **2D 입력 자료**입니다. `Reconstruction/TripoSR-*.png`는 실제 내보낸 메시를 Blender에서 렌더한 검수본입니다.

## 비공개 로컬 파일

- `Artifacts/Tools/VRoidStudio-v2.14.0-win.exe`: 설치 파일 보관. 직접 설치 완료 후 실제 사용함.
- `Artifacts/Tools/VRM_Addon_for_Blender-4_7_1.zip`: 설치한 애드온.
- `Artifacts/Tools/TripoSR`, `TripoSR-Models`: 코드와 가중치.
- `Artifacts/Tools/Hy3D-env`: 기존 빈 환경에 TripoSR 의존성을 설치. 폴더명과 달리 Hunyuan을 실행하지 않음.
- `Artifacts/SeohaStudio/Seoha-Reconstruction-Front.png`, `Seoha-Reconstruction-Input.png`: 전신 입력 원화.
- `Artifacts/SeohaStudio/Reconstruction/Seoha-TripoSR.glb`: 불합격 시험 메시.
- `Artifacts/SeohaStudio/Reconstruction/Seoha-TripoSR-Review.blend`: 실제 메시와 검수 카메라.
- `Artifacts/SeohaStudio/Reconstruction/report.json`: 추론 수치 및 불합격 상태.

모두 기존 무시 규칙 안에 있으며 공개 업로드하지 않았습니다. 이번 요청에 커밋/푸시는 포함되지 않아 진행하지 않았습니다.

재현: `Tools/SeohaStudio/reconstruct_reference.py`를 위 Python 환경에서 실행하고, `review_reconstruction.py`를 Blender에서 실행합니다. 모델 다운로드가 필요합니다. 두 도구는 게임 에셋을 덮어쓰지 않습니다.

공식 자료: [VRoid Studio](https://vroid.com/en/studio), [VRoid 이용 조건](https://policies.pixiv.net/en.html#vroidstudio), [VRM Blender 도구](https://vrm-addon-for-blender.info/en-us/), [설치한 릴리스](https://github.com/saturday06/VRM-Addon-for-Blender/releases/tag/v4.7.1), [TripoSR](https://github.com/VAST-AI-Research/TripoSR), [Hunyuan 사용 지역 조건](https://github.com/Tencent-Hunyuan/Hunyuan3D-2/blob/main/LICENSE).
