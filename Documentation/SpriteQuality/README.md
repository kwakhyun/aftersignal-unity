# Character sprite review

This pass reviews the imported game assets, rather than only the original image backgrounds. `SpriteQualityReview.Export` exports contact sheets and frame/stature counts for NPC directions, civic/security forces, Seoha's locomotion/combat/traversal/swimming, and cabin portraits. Before/after captures are under `Artifacts/SpriteQuality`.

## Findings and changes

- Several walking atlases repeat almost the same two contact poses. CivilianMan, CivilianWoman, Doctor and Nurse have newly illustrated 6-column × 4-direction atlases: idle, four walking/passing frames, and conversation. Faces, clothing colours and jobs remain recognizable. `PeopleArt` preserves the previous four-pose public lookup and explicitly selects the expanded walk sequence for these four atlases.
- Sparse matte residue creates bright dots around several citizen, athlete, worker and corrupted-citizen sprites. Shared import-time connected-component cleanup removes small isolated pixel fragments before bounds/pivots are measured.
- Seoha's traversal and swimming sheets retain enclosed pale background patches between limbs. Neutral paper islands are cleaned separately from the exterior matte, with lilac hair adjacency protection. This operation excludes weapon/combat sheets and other characters' pale clothing. Existing hair-edge decontamination remains.
- A common foot/torso anchor and a shared per-sheet scale are used; crouched or jumping frames are not individually stretched to standing height.
- Cabin NPC images already crop the actual passenger's sprite rather than using an unrelated portrait. They therefore inherit the corrected character art. Existing dedicated Seoha cabin art remains.

New runtime artwork: `Assets/AfterSignal/Resources/Art/NpcPolished`. Original generated source is preserved in `SourceArt`; the previous 16-frame citizen assets remain available for existing authored references and comparisons. Source checkerboard matte, when returned by generation, is removed by the Unity importer. No extra texture-processing loop is added to the running game. Facility roles that map to these same uniforms also receive the new sprites.

These are illustrated sprite cycles, not skeletal character animation. Some contact poses remain similar and this pass does not claim a full redraw of every historical character or every combat frame. Existing usable identities, weapons and action layouts are retained. Swimming import segments the eight complete source silhouettes before slicing, then packs them into padded cells at a common scale. This preserves outstretched hands/boots that crossed the old grid and prevents neighbouring fragments. Raw source images and pose order are preserved.

## Generation

Built-in image generation was used, with the existing matching character sheet as identity reference. The initial 16-frame male candidate was rejected because it did not add useful passing poses. Selected prompts and source references are in `PROMPTS.md`.

## Review and verification

- Imported-atlas review: 80 sheets / 1,096 frames; zero empty sprite regions. Contact sheets sample poses in every atlas, with full frame bounds counted in `Artifacts/SpriteQuality/Final/review.json`.
- Four new 24-frame NPC atlases (96 frames total) were integrated through `PeopleArt`, including existing facility uniform mappings.
- Legacy campaign enemy and named-story-NPC art was sampled separately and retained. The review did not redraw all historical assets.
- The opt-in `SpriteQualityProbe` checks packaged four-direction NPC frame selection and captures the shipping character material for four walk phases and representative Seoha movement/swimming poses. It suppresses save writes.
- Editor image normalization and player lookup are separate: cleanup happens during asset import, never as per-frame pixel manipulation.

Final Windows build: succeeded, 0 errors / 7 warnings. Native essentials: 14 passed / 0 errors (`Artifacts/SpriteQuality/Native/result.json`). The final shipping captures confirm complete swimming fingertips and cleared neutral paper patches; no full campaign or long-duration playthrough was run. `PLAY.cmd` now launches `Builds/SpriteQuality/AFTERSIGNAL.exe`, including the preceding home-return fixes.
