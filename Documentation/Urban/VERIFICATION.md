# 도시 1.5 — 작업 인계 상태

최신 기능·검증·미완료 목록은 프로젝트 루트의 [HANDOFF.md](../../HANDOFF.md)를 따른다. 현재 공개 소스는 작업 인계 기준점이며 출시 완료본이 아니다.

Windows Release 빌드, 도시 기능 22개, 시설 16종 왕복, 기존 Rail/Expansion/Residence 회귀가 통과했다. Unity 테스트는 EditMode 19 / PlayMode 32 통과했다. 최종 도시 측정은 평균 17.059ms, P99 16.670ms, 33.34ms 초과 7회였다(Ryzen 5 7500F, RTX 4060 Ti, 1600×900 D3D11 Release).

계단의 마지막 연결 다리 가림, 직접 키보드/마우스 QA, 느린 프레임·종료 충돌 반복 검증, 최종 영상 정리와 1.5 ZIP 생성은 남아 있다. 원본은 로컬 `Artifacts/Urban15`, `Artifacts/Residence/Release15`, `Artifacts/Urban/Release15`에 보존했다. 대용량 녹화·빌드·로그는 Git 저장소에 포함하지 않는다.
