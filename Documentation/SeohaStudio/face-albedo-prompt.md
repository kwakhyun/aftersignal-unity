# 얼굴 전용 알베도 제작 기록

- 도구: Codex 내장 `image_gen`
- 참조: `Assets/AfterSignal/Resources/Art/Portraits/SeoDialogue.png`
- 결과: 비공개 `Artifacts/SeohaStudio/Seoha-Face-Albedo.png`
- 용도: 실제 얼굴 메시와 눈 곡면에 적용할 색상 텍스처. 최종 3D 모델 렌더가 아닙니다.

```text
Use case: identity-preserve / game texture authoring.
Image 1 is the identity and rendering-style reference: Seoha, an adult Korean anime heroine with delicate mature features, almond violet eyes, soft peach skin and slender face. Create a single square 1024x1024 face albedo texture sheet for her actual 3D model.
This is a texture asset, NOT a finished portrait or 3D beauty render. Completely frontal orthographic view, symmetrical, level eyes, neutral relaxed faintly friendly mouth. NO HAIR, no bangs, no eyelashes made of hair over the face, no neck, no torso, no ears, no clothing or jewelry. Her facial details alone are painted on a completely uniform pale peach skin-color square canvas, seamless skin color extending to all edges. There must be NO head silhouette boundary, no bald dome outline, no background, no checkerboard.
Precise UV landmark layout: left eye center at 30% image width, right eye center at 70%, both at 44% image height. Each almond eye approximately 29% canvas width and 10% height, mature delicate eye shape matching reference. Nose tip at x50% y60%. Mouth center x50% y74%, width 20%. Thin tapered taupe-lavender brows around y29%. Clean fine upper eyelid lashes, dark outer corners, elegant lavender irises with intricate hand-painted radial fibers and two tiny light glints, subtle warm shaded lower lids, faint blush below outer eyes. Soft subtle small nose and warm relaxed delicate lips. Skin tone warm very pale peach like the source. Keep lighting flat and soft, no cast shadows, no dramatic light, no strong contours, no text, no symbols.
Highest-quality hand-painted anime character diffuse-map finish matching the exact woman in the reference.
```
