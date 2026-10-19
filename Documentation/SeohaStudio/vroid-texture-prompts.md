# VRoid 기반 서하 UV 도색 프롬프트 · 2026-09-13

두 장 모두 내장 `image_gen`으로 제작했습니다. 3D 검수 렌더는 Blender에서 별도로 만들었습니다. 아래 도색 이미지를 완성된 3D 모델처럼 제시하지 않습니다.

## 얼굴

편집 대상: 로컬 VRoid의 `_04.png`. 스타일 참고: 게임의 `SeoDialogue.png`.
저장: `Artifacts/SeohaStudio/VRoid/Seoha-Face-Albedo.png`.

```text
Edit image 1, an exact 1024x1024 VRoid face UV albedo texture, retaining the exact UV island layout and all feature pixel locations. Image 2 is color/style reference only. Output ONLY the flat square UV albedo, no rendered face, no text. Apply delicate painted anime skin sophistication inspired by this adult woman's complexion: very subtle blush softly below the two eye holes around (350,615) and (675,615), warm fine eyelid shadows around the existing holes, refined subtle nose shading in same location, natural soft rosy lip shading immediately around existing thin mouth slit x447..577 y775, very slight lower lip highlight. Base skin must remain existing pale ivory peach. Important: NO eyeballs or irises, NO eyebrows, NO eyelashes, NO hair, NO body; those have separate mesh textures. Preserve existing holes centered (350,555) and (675,555), nose at (512,635), mouth slit (512,775), the two ear UV islands at top corners, boundary colors and dimensions unchanged. Very restrained character-game texture, no photographic skin pores, no heavy makeup. Mouth should be delicate, tiny and closed in exactly existing position. Return edit target only.
```

## 상의와 소매

편집 대상: 로컬 VRoid의 `_14.png`. 스타일 참고: 게임의 `SeoDialogue.png`.
저장: `Artifacts/SeohaStudio/VRoid/Seoha-Garment-Albedo.png`.

```text
Precise edit to IMAGE 1 ONLY: this is the flattened square 2048x2048 UV texture of a 3D character's cropped shirt and two jacket sleeves. Preserve all UV island outlines and their exact positions, scale and original transparent/black outside areas. Image 2 is the costume STYLE reference ONLY, do not render a character. Repaint the interior of garment islands as richly detailed restrained black/dark graphite leather and cloth for the adult cyberpunk woman in image 2. Remove ALL the original white circles, circuit diagrams, orange marks, words and logos. Center torso remains fitted black cropped undershirt, with delicate vertical seams and natural compression folds. Upper two separate sleeves have fine dark leather grain, small believable creases along sleeve elbows, black reinforced stitched panel seams and just tiny cyan seam accents. Keep existing seam placement. Polished anime game hand-painted texture, muted graphite-purple shadow variation and fine leather highlights, no bulky armor or glowing ornament. Crucially: output EXACT same flat UV atlas with huge unused bottom half preserved empty; no perspective, no full character, no rearranging or enlarging islands, no words, no extra objects. Keep the original outside background transparent where it was transparent.
```

출력은 각각 1254×1254였습니다. 정규화 UV로 실제 메시 위에서 확인했습니다. 상의 도색의 배경은 실제 알파가 아닌 RGB 체크무늬로 출력되어, 원본 의상 알파를 보존하고 밝은 경계 패딩을 재질에서 제한했습니다. 원본 텍스처는 덮어쓰지 않았습니다.
