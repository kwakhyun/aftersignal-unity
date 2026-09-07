# Assets and packages

- Seo: 96 original transparent PNG frames copied from the existing AFTERSIGNAL project (`seo-v2`, `seo-v21`, `seo-v22`, `seo-v23`, `seo-v40`). Generated originally with image_gen for this project.
- Enemies: 48 original frames (six archetypes), from `enemies-v23`.
- Noa, Min, Yun, Dami, Haejin: five original NPC images, from `noa` and `citizens-v40`. Version 1.2 places all five in the expanded, playable town.
- Korean font: a static weight-500 instance of Google Fonts Noto Sans KR, renamed **AfterSignal Sans KR**. SIL Open Font License 1.1. Source: https://github.com/google/fonts/tree/main/ofl/notosanskr . License included beside this document. `Tools/Make-StaticFont.py` records the conversion, performed with fonttools 4.59.2 to avoid Unity importing the variable font at its thin default weight.
- Geometry, procedural surface grain, shaders, interface and synthesized audio: authored for this Unity project.
- Official Unity packages are embedded with Unity's own API-updater migrations for 6000.4 (including the move from UnityEditor.GUID to UnityEngine.GUID in Shader Graph). No gameplay features were added to package code. Their package directories contain the corresponding license and third-party notices. They remain subject to their own terms and are not relicensed by this project.

This project does not require an online generative service during play.

Version 1.1 adds six original image-generated, normalized katana poses, one generated ceramic albedo, authored contact-shadow/effect shaders and 64 authored DSP WAV assets. Sources, actual scope and normalization notes are in `Quality/QUALITY.md`; source generators are in `Tools/Build-QualityAssets.py`. The concept paintover is review material, not a gameplay screenshot. No paid third-party assets were purchased.

Version 1.2 adds original modular Unity environments, district material palettes, moving lifts, escalator paths and segmented route beacons. It reuses the existing original actors and sounds. New environments are authored modular geometry, not newly commissioned hand-painted background art. No additional external assets or paid services were used for this expansion.
