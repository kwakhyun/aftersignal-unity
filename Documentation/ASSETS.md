# Assets and packages

- Four cities: eight original image-generated directional NPC sheets (128 frames), parametric venue architecture, and the original 60-second 3D film `SIGNAL / TIDE`. Creation prompts, reference sources and provenance are documented in `FourCities/REFERENCES.md` and `FourCities/ART-PROMPTS.json`.

- Music: six user-supplied Suno-generated instrumental WAV masters, preserved unchanged in `Assets/AfterSignal/Resources/Audio/Music`. Track mapping, provenance details, playback gain measurements and loop handling are documented in `Audio/MUSIC.md` and `Audio/music-sources.json`. These are supplied recordings, separate from the project's synthesized sound effects.

- Seo: 96 original transparent PNG frames copied from the existing AFTERSIGNAL project (`seo-v2`, `seo-v21`, `seo-v22`, `seo-v23`, `seo-v40`). Generated originally with image_gen for this project.
- Enemies: 48 original frames (six archetypes), from `enemies-v23`.
- Shop items: eight original image-generated illustrations for the two purchasable coats and six food/drink items. Square source PNGs, import settings, item mapping and prompts are documented in `Art/SHOP-ITEMS.md`.
- Law enforcement: three new image-generated sheets (24 frames) for uniformed pistol police, shotgun police and rifle-equipped SWAT. Dedicated art replaces the police gunner reuse. Import settings, frame mapping and generation prompts are in `Art/POLICE-SPRITES.md`.
- Noa, Min, Yun, Dami, Haejin: five original NPC images, from `noa` and `citizens-v40`. Version 1.2 places all five in the expanded, playable town.
- Korean font: a static weight-500 instance of Google Fonts Noto Sans KR, renamed **AfterSignal Sans KR**. SIL Open Font License 1.1. Source: https://github.com/google/fonts/tree/main/ofl/notosanskr . License included beside this document. `Tools/Make-StaticFont.py` records the conversion, performed with fonttools 4.59.2 to avoid Unity importing the variable font at its thin default weight.
- Geometry, procedural surface grain, shaders, interface and synthesized audio: authored for this Unity project.
- Official Unity packages are embedded with Unity's own API-updater migrations for 6000.4 (including the move from UnityEditor.GUID to UnityEngine.GUID in Shader Graph). No gameplay features were added to package code. Their package directories contain the corresponding license and third-party notices. They remain subject to their own terms and are not relicensed by this project.

This project does not require an online generative service during play.

Version 1.1 adds six original image-generated, normalized katana poses, one generated ceramic albedo, authored contact-shadow/effect shaders and 64 authored DSP WAV assets. Sources, actual scope and normalization notes are in `Quality/QUALITY.md`; source generators are in `Tools/Build-QualityAssets.py`. The concept paintover is review material, not a gameplay screenshot. No paid third-party assets were purchased.

Version 1.2 adds original modular Unity environments, district material palettes, moving lifts, escalator paths and segmented route beacons. It reuses the existing original actors and sounds. New environments are authored modular geometry, not newly commissioned hand-painted background art. No additional external assets or paid services were used for this expansion.

- 갱단 전용 인물 3종 / 24동작: `Resources/Art/Gangs`, 제작·투명 임포트 기록은 [GANG-SPRITES.md](Art/GANG-SPRITES.md), 생성 프롬프트는 `Art/gang-prompts.json`.


## City evolution directional sprites (2026-09-07)

Built-in `image_gen` produced 9 Seo motion/idle sheets and 22 NPC role sheets, imported as 496 directional frames. Project assets live in `Assets/AfterSignal/Resources/Art/SeoMotion/` and `Assets/AfterSignal/Resources/Art/NpcDirections/`. Pixel gutters, foot pivots and border-connected background alpha are handled by `DirectionalSheetImporter`. Prompts and original generated paths: [Art/city-evolution-prompts.json](Art/city-evolution-prompts.json). Gameplay and controls: [CITY-EVOLUTION.md](CITY-EVOLUTION.md).


## Seo dialogue portrait and alpha repair (2026-09-07)

`Assets/AfterSignal/Resources/Art/Portraits/SeoDialogue.png` is a new opaque navy-background dialogue portrait made with built-in `image_gen`, preserving the supplied Seo identity. [Prompt and source](Art/seo-dialogue-prompt.json). `SpriteMatte` now removes only border-connected neutral background for Seo action sheets, preserving bright hair inside the silhouette. [Update guide](CHRONICLE.md).


## Seo kinetic movement and combat

18 built-in image_gen atlases under Resources/Art/SeoKinetic: 64 run frames, 96 directional weapon phases, 8 standing views and 16 traversal poses. KineticSpriteImporter removes border-connected pale matte, registers feet/torso and uses a shared scale per sheet. Source prompts: Documentation/Art/seo-kinetic-prompts.json. Unselected generation attempts were not copied into the project.


### Title key art

`Art/Title/AfterlightTitle.png` is a dedicated opaque Seo/Afterlight rooftop illustration generated with built-in image_gen. The title, menu text and gradients render separately in Unity. Identity reference and full prompt: `Documentation/Art/title-art-prompt.json`. Texture import keeps full resolution with bilinear filtering, no mipmaps and no sprite matte extraction.


### Restored sound and recorded firearms

`Audio/Firearms/` contains 18 edited real firearm recordings from The Free Firearm Sound Library, CC0 1.0, by Ben Jaszczak, Brian Nelson, Kevin Heras and Matthew Nanney. Full per-file provenance and hashes: `Documentation/Audio/firearm-sources.json`; license: `Documentation/Audio/FIREARM-LICENSE.txt`. Runtime uses these recordings for all gunshot categories. Existing `Audio/Quality/` movement/combat effects now preload as PCM and use the restored SFX mix. Corpse blood is a procedural mesh and project-owned shader, with no third-party image asset.


### Vehicle portraits and refined Seo actions

`Art/VehiclePortraits/` has 80 generated upper-body sprites: Seo driver/passenger eight-direction views and sixteen four-direction NPC drivers. `Art/SeoRefined/` has 80 generated full-body run/action frames. Built-in image generation provenance and final source paths: `Documentation/Art/vehicle-motion-prompts.jsonl`. Vehicle passengers crop their existing individual artwork. Unity handles slicing and exterior matte removal. Runtime action scale follows the corresponding run canvas.

### Seo 3D trial withdrawn (2026-09-08)

The rejected trial character, rig, scripts and build have been withdrawn from this project. Seo uses the existing directional sprites. The withdrawn source is archived outside this repository in `../AFTERSIGNAL-Withdrawn/Seo3D-20260908`.

### Coastal metropolitan expansion (2026-09-08)

`WorldAssets/` contains the authored terminal, runway, aircraft, port cranes, container vessel, coastal facilities, roads and district prefab. `DetailedSedan/DetailedSedan.fbx` is an original bevelled vehicle with articulated wheels; editable Blender source and recreation script are in `Documentation/WorldExpansion/` and `Tools/WorldExpansion/`.

Poly Haven CC0 lamp, bench, barrier and apartment components and four PBR ground surfaces are recorded with download URLs and hashes in `WorldExpansion/polyhaven-sources.json` and `WorldExpansion/surface-sources.json`. Existing authored city facade textures are reused on the new building shells.

Six new generated facility citizen atlases provide 24 identities and 96 directional frames, sliced and processed as RGBA32 by `FacilitySpriteImporter`. Prompts: `WorldExpansion/npc-prompts.json`. The recorded reload phases are sourced from SpringySpringo's CC0 airsoft handling recordings; source, edits and hashes are in `WorldExpansion/reload-sources.json`. Existing real firearm firing recordings remain in use.

Implementation scope, controls and references: [WorldExpansion/README.md](WorldExpansion/README.md).

### Transport, inhabited districts and ocean (2026-09-08)

Seven original Blender transport assets are in `WorldAssets/{Motorcycle,SportsCar,Boat,Airliner,CombatHelicopter,Fighter,Tank}`. Editable sources: `Documentation/Mobility/Models`; generator: `Tools/WorldExpansion/create_transport.py`. Runtime corrects the FBX forward axis, applies body/lens/glazing materials and rotates the articulated wheel pivots around the axle. No third-party branded vehicle or aircraft model is used.

`MobilityDistricts.prefab` adds connected roads, furnished multistorey buildings, military base, prison, passenger terminal, parking and the seabed. Coral and marine animal meshes are procedural original geometry. These facilities reuse the project's existing original NPC art and documented Poly Haven surfaces. `ContainerShip.prefab` converts the previously authored cargo ship to a controllable, batched vessel. Confirmed unused generated meshes from replaced world batches were removed; source models and active mesh GUIDs are retained. Scope and controls: [Mobility/README.md](Mobility/README.md).

### Physical city surfaces, street kit and botanical LODs

`Resources/Materials/Scanned/` contains four CC0 Poly Haven 2K surface sets (12 JPEG maps): asphalt_04, concrete_tiles_02, concrete_wall_006 and blue_metal_plate. Individual URLs, checksums and license: [Fidelity/material-provenance.json](Fidelity/material-provenance.json).

`Resources/WorldAssets/StreetKit/` contains seven original Blender assets: PromenadeBench, TransitShelter, CivicKiosk, ChargePoint, SmartBollard, ClimateUnit and CoastalPalm. All are used by the runtime. Editable sources can be recreated with `Tools/Fidelity/create_street_kit.py`; studio review uses `preview_street_kit.py`. These are original generic designs, not extracted commercial-game assets.

`Resources/WorldAssets/Botanical/` contains three reduced meshes and a Unity LOD prefab derived from Rico Cilliers' CC0 Tree Small 02 from Poly Haven. Nine maps include channel-packed URP masks. Original file hashes, processing and triangle counts: [Fidelity/botanical-provenance.json](Fidelity/botanical-provenance.json). Download, reduction, texture packing and preview scripts are in `Tools/Fidelity/`. The rejected uniform-decimation version is not the shipped model.

Architecture, facade-interior shading, weather response and practical lighting are project-authored code. Scope, external technical/architecture references and remaining production gaps: [Fidelity/QUALITY-DIRECTION.md](Fidelity/QUALITY-DIRECTION.md).
