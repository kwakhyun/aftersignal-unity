# AFTERSIGNAL 효과음 생성 프롬프트

현재 연결된 단발 효과음 31종, 반복음 10종(헬기 회전음 1종은 추가 연결용)을 정리했다. 기존 합성 효과음은 이미 있으므로 한꺼번에 생성할 필요가 없다.

## 먼저 만들 12종

blade_swing, blade_hit, heavy_swing, heavy_hit, pistol, guard, dash, rope_attach, rope_release, glass, step_tile, step_metal. 나머지는 기존 효과음을 유지하면서 차례로 교체한다.

## Suno 사용법

만들기 화면의 **사운드(Sounds)** 모드에서 아래 영문 프롬프트를 입력한다. 단발음은 One Shot, 엔진·사이렌·환경음은 Loop를 선택한다. 공식 안내: https://help.suno.com/en/articles/10625537 . 이용 가능 여부는 계정과 플랜에 따른다.

- 한 프롬프트로 한 종류의 소리를 생성한다. 아래 목표 길이는 편집 시 기준이며 생성 결과를 보장하는 값이 아니다.
- 효과음은 WAV로 전달한다. 가능하면 48 kHz. 단발 효과음은 앞 무음 없이 빠르게 시작하고 꼬리가 끝까지 남은 파일을 선택한다.
- 반복음은 시작·끝이 비슷한 상태이고 속도·음높이가 일정한 버전을 고른다. 실제 반복 연결부는 적용 시 검사한다.
- 장전은 제거·삽입·슬라이드가 각기 다른 시점에 재생되므로 반드시 별도 생성한다. 전술 연사는 한 발짜리 소리를 게임이 반복한다.
- 최초에는 효과음당 한 파일이면 된다. 자주 반복되는 발걸음·타격·사격은 여유가 있을 때 같은 프롬프트로 2~3개 변형을 만든다. 한 파일에 여러 변형을 이어 붙이지 않는다.
- 파일명 예: blade_hit.wav. 변형이 있으면 blade_hit_0.wav, blade_hit_1.wav, blade_hit_2.wav. 현재 게임은 단발 이벤트마다 3개의 변형 슬롯을 사용하므로 한 파일만 받으면 통합 시 슬롯 대응을 처리해야 한다.
- 경찰 사이렌은 현재 코드 합성음을 쓰므로 파일 교체 연결이 필요하다. 헬기 회전음은 새 루프 재생 연결이 필요하다. 이 문서는 프롬프트 작성이며 게임 오디오를 변경하지 않는다.

## 01. 카타나 휘두르기

파일명: `blade_swing.wav` · One Shot · 목표 0.2–0.4초

```text
One fast katana slash through air. Sharp narrow whoosh, crisp attack, delicate metallic edge, short clean decay. Agile and precise, suitable for rapid repeated sword attacks. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 02. 카타나 명중

파일명: `blade_hit.wav` · One Shot · 목표 0.2–0.5초

```text
One katana strike hitting an armored opponent. Sharp cutting snap followed by a compact solid impact and subtle metal texture. Satisfying, focused and restrained, without gore or screaming. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 03. 중검 휘두르기

파일명: `heavy_swing.wav` · One Shot · 목표 0.4–0.7초

```text
One massive greatsword swinging through air. Broad deep whoosh, heavy air displacement and restrained metallic resonance. Powerful weight and momentum, with a clear attack and short tail. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 04. 중검 명중

파일명: `heavy_hit.wav` · One Shot · 목표 0.4–0.8초

```text
One heavy greatsword impact against reinforced armor. Dense low thud, blunt metallic crunch and a brief resonant tail. Clearly heavier than a katana, controlled bass, no explosion. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 05. 권총 한 발

파일명: `pistol.wav` · One Shot · 목표 0.3–0.6초

```text
One compact futuristic ballistic pistol shot. Sharp dry crack, tight low punch and a subtle mechanical slide click. Immediate attack and short decay, clear during rapid fire. No laser tone. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 06. 전술 연사 한 발

파일명: `pistol_overdrive.wav` · One Shot · 목표 0.3–0.6초

```text
One enhanced ballistic pistol shot with a stronger low punch and a restrained electrical edge. Crisp dry crack and fast decay. A single shot suitable for sequencing into rapid bursts. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 07. 방어·장갑 타격

파일명: `guard.wav` · One Shot · 목표 0.3–0.6초

```text
One weapon strike blocked by a steel blade. Crisp metallic clash, brief bright ring and a solid low impact. Clear defensive feedback with controlled high frequencies and a short tail. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 08. 플레이어 피격

파일명: `hurt.wav` · One Shot · 목표 0.2–0.5초

```text
One blunt impact against a clothed human torso, compact body thud and brief fabric movement. Strong readable damage feedback without gore, bones breaking, voices or exaggerated bass. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 09. 적 쓰러짐

파일명: `death.wav` · One Shot · 목표 0.5–0.9초

```text
One armored opponent collapsing onto a hard floor. A compact body thump with a short equipment rattle and fabric movement. Restrained, readable and non-graphic, without a scream. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 10. 대시

파일명: `dash.wav` · One Shot · 목표 0.2–0.4초

```text
One extremely fast directional dash. Tight rush of displaced air with a subtle futuristic energy accent, immediate attack and quick taper. Agile and forceful without a teleport explosion. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 11. 점프 시작

파일명: `jump.wav` · One Shot · 목표 0.15–0.3초

```text
One athletic jump takeoff. A short shoe push against a hard surface, slight clothing movement and a subtle upward air swish. Light, dry and understated, no cartoon spring. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 12. 착지

파일명: `land.wav` · One Shot · 목표 0.3–0.5초

```text
One controlled two-foot landing in sturdy shoes on a hard surface. Compact low impact, brief sole contact and light clothing movement. Firm weight, no large debris or vocal effort. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 13. 타일·포장 바닥 발걸음

파일명: `step_tile.wav` · One Shot · 목표 0.15–0.3초

```text
One single footstep in sturdy rubber-soled shoes on smooth hard tile. Defined heel contact followed closely by the sole, subtle weight, dry close perspective. No walking sequence. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 14. 객실·열차 지붕 발걸음

파일명: `step_metal.wav` · One Shot · 목표 0.2–0.4초

```text
One single footstep in sturdy shoes on a metal train floor. Firm sole impact with a brief hollow metallic resonance. Compact, dry and repeatable, without train ambience. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 15. 로프 앵커 체결

파일명: `rope_attach.wav` · One Shot · 목표 0.25–0.5초

```text
One grappling hook locking firmly onto a metal anchor. Sharp mechanical clack, a tight cable tension snap and a brief metal ring. Immediate secure attachment feedback, no long cable launch. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 16. 로프 해제

파일명: `rope_release.wav` · One Shot · 목표 0.15–0.3초

```text
One grappling cable latch releasing under tension. A crisp small mechanical click followed by a short cable flick. Lighter and shorter than attachment, clean immediate release feedback. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 17. 유리 격벽 파괴

파일명: `glass.wav` · One Shot · 목표 0.7–1.2초

```text
One reinforced glass partition shattering from a decisive impact. A sharp initial fracture followed by many small glass fragments falling briefly. Clear sparkling detail with controlled highs, no explosion. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 18. 탄창 분리

파일명: `reload_out.wav` · One Shot · 목표 0.15–0.3초

```text
One pistol magazine release. A small button click followed by the short mechanical slide of a magazine leaving the grip. Dry close detail. Only magazine removal, no full reload. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 19. 탄창 삽입

파일명: `reload_in.wav` · One Shot · 목표 0.15–0.3초

```text
One pistol magazine inserted firmly into the grip. Short polymer and metal contact followed by a decisive locking click. Dry and tactile. Only magazine insertion, no slide action. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 20. 슬라이드 조작

파일명: `reload_slide.wav` · One Shot · 목표 0.2–0.4초

```text
One pistol slide pulled back and released. Brief metallic rail scrape, spring tension and a sharp forward locking clack. Tight close detail. No gunshot or magazine handling. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 21. 무기 교체

파일명: `weapon_switch.wav` · One Shot · 목표 0.2–0.4초

```text
One compact tactical weapon-ready movement. A short equipment shift, restrained fabric rustle and a precise mechanical locking click. Neutral enough for a sword or pistol, quick and unobtrusive. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 22. 월광참

파일명: `katana_wave.wav` · One Shot · 목표 0.5–0.9초

```text
One crescent-shaped energy slash released from a katana. Razor-thin air cut layered with a luminous electrical shimmer and a short traveling tail. Elegant and forceful, no charge-up sequence. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 23. 지각 붕괴

파일명: `heavy_slam.wav` · One Shot · 목표 0.7–1.2초

```text
One heavy greatsword slamming into the ground and releasing a compact shockwave. Dense impact, low pressure burst and short gritty debris. Powerful but controlled, no long rumbling aftermath. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 24. 건물 출입문

파일명: `door.wav` · One Shot · 목표 0.3–0.6초

```text
One sturdy interior door latch operating with a short hinge movement. Natural mechanical click and restrained wooden panel resonance. Neutral transition detail suitable for opening or closing, no prolonged creak. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 25. 단말·선택 확인

파일명: `ui_confirm.wav` · One Shot · 목표 0.15–0.35초

```text
One subtle futuristic interface confirmation. A soft tactile click with a very brief clean upward electronic ping. Warm, precise and reassuring, minimal tonal content, no musical jingle. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 26. 대화·메뉴 닫기

파일명: `ui_cancel.wav` · One Shot · 목표 0.1–0.25초

```text
One subtle futuristic interface dismissal. A soft dry click and a brief downward electronic tick. Quiet, neutral and clearly shorter than a confirmation sound, no error alarm. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 27. 차량 제동

파일명: `urban_brake.wav` · One Shot · 목표 0.5–0.9초

```text
One brief vehicle braking sound. Controlled rubber friction on dry asphalt, a restrained tire squeal that quickly settles. Moderate speed stop, no crash, horn or engine recording. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 28. 차량 충돌

파일명: `urban_crash.wav` · One Shot · 목표 0.5–1초

```text
One moderate vehicle collision. Dense metal panel crunch, low chassis impact and a few brief plastic rattles. Immediate readable impact with a short tail, no explosion or extended crash sequence. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 29. 차량 폭발

파일명: `urban_explosion.wav` · One Shot · 목표 1–2초

```text
One damaged vehicle exploding. A sharp ignition crack followed by a deep compact pressure burst and brief metallic debris. Powerful controlled bass, natural decay, no cinematic riser or secondary explosions. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 30. 차량 탑승·하차

파일명: `urban_door.wav` · One Shot · 목표 0.3–0.6초

```text
One solid passenger car door latch and close. A short metal latch click followed by a compact insulated door thump. Close mechanical detail, no engine, footsteps or remote-lock beep. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 31. 차량·보행자 충돌

파일명: `urban_impact.wav` · One Shot · 목표 0.3–0.6초

```text
One restrained soft-body impact against a vehicle panel. A compact padded thud, slight fabric movement and brief muted panel resonance. Non-graphic, no screaming, bones breaking or gore. Isolated dry game sound effect, one event only. No music, speech, background ambience or long reverb.
```

## 32. 승용차 엔진

파일명: `urban_engine_sedan.wav` · Loop · 목표 6–10초

```text
Steady compact gasoline car engine running at a constant moderate low RPM. Smooth layered mechanical hum with subtle combustion pulses. Stable pitch and intensity, seamless loop, no acceleration, gear changes or ambience. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 33. 택시 엔진

파일명: `urban_engine_taxi.wav` · Loop · 목표 6–10초

```text
Steady well-used sedan engine at a constant moderate low RPM. Warm mechanical hum with a slight restrained rattle, reliable and even. Stable pitch and intensity, seamless loop, no acceleration or traffic ambience. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 34. 버스 엔진

파일명: `urban_engine_bus.wav` · Loop · 목표 6–10초

```text
Steady city bus diesel engine at a constant low RPM. Deep even combustion pulses and restrained mechanical vibration. Heavy but smooth, stable pitch and intensity, seamless loop, no air brakes or gear changes. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 35. 트럭 엔진

파일명: `urban_engine_truck.wav` · Loop · 목표 6–10초

```text
Steady medium-duty diesel truck engine at a constant low RPM. Weighty throaty mechanical rhythm and a slightly rough metallic texture. Stable pitch and intensity, seamless loop, no acceleration or reversing beeps. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 36. 경찰 사이렌

파일명: `police_siren.wav` · Loop · 목표 6–10초

```text
An isolated two-tone urban police siren completing several regular alternating cycles at a constant distance. Clear electronic warning tones with controlled piercing frequencies. Seamless loop, no Doppler movement, engine or radio speech. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 37. 경찰 헬기 회전음 · 추가 연결 필요

파일명: `helicopter_rotor.wav` · Loop · 목표 6–10초

```text
A helicopter rotor operating steadily at a constant moderate speed and distance. Distinct rhythmic blade chop with a restrained turbine hum. Stable intensity, seamless loop, no flyby, wind gusts, weapons or voices. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 38. 중앙역 환경음

파일명: `ambient_station.wav` · Loop · 목표 15–30초

```text
Quiet enclosed railway station ambience at night. Low ventilation hum, subtle distant electrical infrastructure and spacious enclosed air. Stable seamless atmospheric loop, no announcements, identifiable voices, footsteps, arriving train or music. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 39. 달리는 객실 환경음

파일명: `ambient_train.wav` · Loop · 목표 15–30초

```text
Inside a steadily moving passenger train. Continuous subdued rail rumble, gentle carriage vibration and faint ventilation. Consistent speed and perspective, seamless loop, no announcements, voices, braking or prominent isolated clacks. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 40. 열차 지붕 환경음

파일명: `ambient_roof.wav` · Loop · 목표 15–30초

```text
On the roof of a steadily moving train at night. Broad controlled rushing air over a distant low rail rumble. Consistent speed and intensity, seamless loop, no strong gusts, horns or voices. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```

## 41. 마을·시내 환경음

파일명: `ambient_town.wav` · Loop · 목표 15–30초

```text
Quiet late-night urban neighborhood ambience. Soft distant traffic wash, restrained building ventilation and open nighttime air. Calm continuous background texture, seamless loop, no identifiable speech, sirens, horns, distinct footsteps or music. Isolated game audio loop. No music or speech, no fade-in or fade-out.
```


