# Fidelity engineering and art direction

The current game is a procedural open-world prototype. It does not have the visual or systemic completeness of GTA V, Cyberpunk 2077 or NTE. Installing an editor, increasing object count or switching render pipelines does not close that gap by itself.

## Findings from this repository

- World scale expanded faster than authored street composition. Repeated tower floor plates, oversized empty forecourts and thin details weaken the sense of scale.
- The Urban Surface shader used a fixed ambient RGB value and a partial sun term, ignoring local light, material roughness, normal maps and reflection probes. Night lighting therefore could not affect major surfaces correctly.
- The URP pipeline already used Forward+ and SSAO. Its shadow distance was only 40 metres with a 1024px main-light map. High-frequency facades shimmered, and large distant structures lacked grounding.
- Visual styles differ between the illustrated protagonist, sprite NPCs, parametric architecture and low-detail street props. A coherent character art production pipeline is still needed; the rejected 3D protagonist trial has not been reinstated.
- Several pedestrian controllers move directly towards destinations, with only a forward ray or no obstacle query. More detail would make these navigation limitations more visible.
- All four cities are loaded as a large hierarchy. Local activation exists, but hierarchical world streaming and authored LODs remain a separate major production requirement.

## Current implementation direction

Preserve URP 17.4 and the working game systems. Improve physical materials, local reflection and practical lighting; give buildings legible bases, structural silhouettes and facade rhythms; add metre-scale bevelled street furniture with interactions and bounded activation; improve shared pedestrian avoidance and interaction-query cost. Expose a graphics preset so visual cost is controllable.

Rendering and art are reviewed from pedestrian, aerial, night and interior cameras, not only attractive isolated model renders. A hidden Windows player does not render ordinary frames, so the benchmark explicitly submits frames and fences the GPU with a one-pixel readback. Its timings include synchronization overhead and are comparative measurements, not guaranteed gameplay FPS.

### Implemented changes

- Four CC0 2K material sets, twelve maps: asphalt, concrete paving, cast concrete and painted metal. Major city surfaces now use normal, roughness, AO, physical lighting and local reflection. Dampness affects selected outdoor upward-facing materials.
- A facade shader gives window bays frames, blinds, shallow view-dependent room depth and varied occupied/unoccupied night windows. Core-city `DistrictWindow` was included after the first native review exposed uniform white panes; lit accent panes were dimmed separately.
- Six tower profiles for the newer four-city neighborhoods: setbacks, paired towers with sky bridges, flared and tapered masses, solar fins and stepped terraces. Podiums, canopies, spandrels, roof plant and structural bracing replace the former repeated floor plates in those neighborhoods. This does not replace every existing core-city building with an individually authored asset.
- Seven original bevelled Blender street models: shelter, bench, information kiosk, charging point, bollard, rooftop climate unit and coastal palm. Streamed street sites are capped at 48, constructed at most three per update, with obstruction and floor checks. Benches, information and charging have actual interactions.
- A Poly Haven botanical tree replaces geometric canopies in the four-city landscaping. The 2,062,487-triangle source becomes 83,206 / 29,190 / 8,789 triangles in three shared LOD meshes. Global decimation failed visual review; the final pipeline preserves trunks and branches separately and thins whole leaf islands. Distant canopy coverage is approximate, not identical to the source.
- Recessed ceiling fixtures and a twelve-light pool illuminate the occupied floor of the newer enclosed cultural/civic venues. The lobby floor is raised 3 cm above the plaza to eliminate coplanar surface interference. This is not a full baked-GI/light-probe authoring pass for every old interior.
- Shared local pedestrian steering avoids solid obstacles, separates trigger-body citizens and refuses unsupported ledges. It preserves existing destinations and schedules. It is not multi-floor pathfinding or a new autonomous social simulation.
- Interaction scanning caches nearby candidates for 0.2 seconds, with immediate registry-size/teleport refresh, instead of repeatedly fetching NPC components from every interaction in the world each frame.
- ESC settings expose performance/high/ultra rendering presets. The default uses 4x MSAA, SMAA, four shadow cascades, a 4096px main shadow map and a 140 m shadow range. High/ultra use a local time-sliced reflection probe. Cloud layers and day/night exposure, bloom and ambient light were revised.

## Primary references

- [Unity render-pipeline strategy, 2026](https://unity.com/topics/render-pipelines-strategy-for-2026): URP investment and HDRP maintenance. A wholesale migration would also require rebuilding this project's custom sprite, water and surface shaders.
- [Unity high-end graphics optimization](https://unity.com/how-to/performance-optimization-high-end-graphics): separate CPU/GPU measurement, anti-aliasing and rendering budgets.
- [Unity URP ambient occlusion](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/post-processing-ssao.html): contact shading and depth/normal inputs.
- [Building Night City — CD Projekt Red, GDC](https://media.gdcvault.com/gdc2023/Slides/Buildingnightcity_Tremblay_Charles.pdf): large-world rendering and streaming architecture. This is a technical reference, not a source of game assets.
- [Morpheus — Zaha Hadid Architects](https://www.zaha-hadid.com/2020/02/28/morpheus-model-showcased-at-the-pompidou-centre/): circulation cores and structural exoskeletons.
- [Vancouver House — BIG](https://big.dk/projects/vancouver-house-missing-pictures-7130): setbacks and shaped building massing responding to the site.
- [Poly Haven license](https://polyhaven.com/license): downloaded textures are CC0; individual URLs and hashes are retained in material-provenance.json. Powered by Poly Haven.
- [Tree Small 02 — Rico Cilliers / Poly Haven](https://polyhaven.com/a/tree_small_02): CC0 botanical source, with original file hashes and LOD processing in botanical-provenance.json.

## Tools and ownership

Blender 4.5.3 and Material Maker 1.7 were already installed. Blender is used to generate the new original street kit, with bevelled edges, weighted normals and separate material slots. Reproducible source is in Tools/Fidelity/create_street_kit.py; editable .blend files are kept in Artifacts/Fidelity/StreetKit. Imported FBX models ship with the project. No paid asset packs or other games' extracted assets are used.

## Remaining production gap

High-end finished characters and facial/locomotion animation; authored district layouts and interiors; navigation meshes and systemic encounter design; robust hierarchical streaming; film-quality cutscene content and acting; sound mixing and spatial ambience; prolonged gameplay and hardware QA. These are concrete production work, not claims satisfied by this graphics pass.
