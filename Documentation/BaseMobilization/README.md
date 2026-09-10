# 시민 다툼과 주둔 기지 방어

- 건강한 성인 시민 두 명이 드물게 말다툼을 벌이고 주먹다짐으로 이어진다. 전체 지역에 한 쌍만 활성화되며, 사건/수배 중에는 새 다툼을 시작하지 않는다. 100~180초 간격 제한, 부상 전 종료, 경찰 접근 시 해산을 적용했다. 어린이·가족·노인·환자 및 직무 인력은 제외한다.
- 군부대 구역의 기존 일반인 외형을 소속 군복으로 교정하고 일반 거리 시민 생성 지점에서 제외한다. 군인은 실제 주둔 명부에 등록된다.
- 기지 경보는 주둔 인원 전원이 대응한다. 기존 주차 장비를 병사가 예약한 뒤 실제 문까지 이동해 탑승한다. 장비 개수는 실제 기지 재고이며, 일반 출동의 인원 상한은 적용하지 않는다. 병사 객체를 유지해 탑승/탈취/하차 시 인물이 바뀌지 않는다.
- 기지 내부 사건에는 별도의 지상/항공 군 증원 생성을 막는다. 기지 외부의 기존 단계별 출동은 유지한다.
- 루멘 기지에 작전 통신실, 차량 정비실, 생활관, 급양·의무실을 추가하고, 실제 출입구를 통해 실내 병력이 나온다. 상층 인원은 건물 승강기를 예약해 내려온다.
- KESTREL 계열 6륜 기관총 군용차 10대: 방탄 운전실, 원격 회전 포탑, 상하 조준 기관총, 적재함, 장비 상자, 방호 그릴, 휠/범퍼/윈치/조명. +X를 차체 전방으로 통일했다. 플레이어는 기존 탑승 조작, 좌클릭 기관총, R 재장전으로 사용한다.
- 정비·연료·탄약 보급 지점 및 기존 13종 실물 무기고를 이용할 수 있다.

## 제작 참고

외부 모델 파일을 복제하지 않고, 아래 실제 시설과 차량 장비의 배치·형태를 참고해 게임용 사이버펑크 구조물을 코드로 제작했다.

- [미 육군 DEVCOM — CROWS 원격 무장 스테이션](https://ac.devcom.army.mil/news/hands-on-crows-maintenance-course-boosts-soldier-lethality-and-readiness/)
- [미 육군 — 차량 정비장 개선: 정비 베이, 교육·관리 공간, 도구 보관](https://www.army.mil/article-amp/251587/fort_hood_motor_pools_receive_much_needed_upgrades)
- [Tobyhanna Army Depot — 전술차량 정비 지원](https://www.tobyhanna.army.mil/Capabilities/Integration-Support-Services/Tactical-Vehicle_Component-Repair_Overhaul-Support/)

## 필수 확인 결과

Windows 빌드 성공: 오류 0, 경고 95. 저장을 비활성화한 실제 실행에서 14개 확인을 통과했고 종료까지 예외가 없었다. 141명 주둔 병력의 대응, 기존 탱크·전투기·헬기·기관총 차량 탑승/사격, 실내 병력 출동, 동일 병사 하차, 플레이어 탑승/기관총 사격을 확인했다. 확인 내역: [result.json](result.json).

PLAY.cmd는 Builds/BaseMobilization/AFTERSIGNAL.exe를 실행한다. 실제 캡처는 로컬 Artifacts/BaseMobilization/Native/에 보관한다. 원본 모델·이미지를 공개 저장소에 추가하지 않았다. 전체 회귀 테스트와 외부 API 호출은 진행하지 않았다.
