# ArtCraft adoption — 2026-10-16

This is a working local asset-tool integration and a first map/UI art pass. It is not a replacement of every game asset, a new Seoha 3D model, or a measured whole-game FPS improvement.

## Installed, from the official upstream releases

- PhotoCraft **0.9.0**, image editing / headless conversion / resampling / MCP.
- VectorCraft **0.11.0**, editable vectors / headless raster export / MCP.
- Windows x64 portable archives, SHA-256 pinned in `Tools/ArtCraft/Install-ArtCraft.ps1`.
- Binaries and their included MIT/Apache/font notices remain in ignored `Artifacts/Tools/ArtCraft`. No external application source is embedded in the Unity runtime.
- Both CLIs ran successfully. The toolchain needs no account for the operations used here. No generation API, upload or paid subscription was used.

Official sources:
- https://getartcraft.com/apps
- https://github.com/storytold/photocraft/releases/tag/v0.9.0
- https://github.com/storytold/vectorcraft/releases/tag/v0.11.0
- https://github.com/storytold/photocraft/blob/v0.9.0/README.md#built-for-agents
- https://github.com/storytold/vectorcraft/blob/v0.11.0/README.md#use-it-from-claude-code-and-other-agents
- https://github.com/storytold/artcraft

PhotoCraft and VectorCraft are still alpha software. Their actual export, transparency, dimensions and Unity import must be checked before adopting outputs. ArtCraft Studio's image/mesh generation depends on its selected generation service; open-source editing does not make third-party GPU generation free. Local editing CLIs do not replace Blender modelling, character rigging, or the still-unmet Seoha likeness target.

## Applied to the game

40 original facility/navigation symbols cover the 31 venue kinds plus airport, port, fuel, garage, station, respawn, incident, waypoint and fallback. VectorCraft imports the authored vector source and saves an editable native document; it rasterizes at 2x, then PhotoCraft resamples to the 1024×640 transparent game atlas.

- Facility glyphs now appear in the map search results and full-map labels across all four cities.
- All 40 sprites share one cached, non-readable texture (2.5 MiB uncompressed RGBA8, no mip chain).
- Map labels are pooled and capped at 64. The full search list remains available.
- The list, map markers and click selection use consistent facility categories. Police/fire/bank and other life facilities are no longer omitted from the map's Life category.
- Transport classification includes ports without interpreting generic “구역/지역/해역” as railway stations.
- The procedural map explicitly requires a CanvasRenderer; missing renderer setup can no longer hide its land/road layer. Uninitialized respawn positions are not drawn as stacked labels.
- The minimap reuses the existing city-name array instead of allocating an array every update.
- Source-only checkouts retain text labels if private icons are missing.

Private artwork:
- `Artifacts/ArtCraft/Sources/FacilityGlyphs.svg`
- `Artifacts/ArtCraft/Sources/FacilityGlyphs.vectorcraft`
- `Assets/AfterSignal/Resources/Art/ArtCraft/UI/FacilityGlyphs.png`
- `Artifacts/ArtCraft/FacilityGlyphs-Review.png`
- `Artifacts/ArtCraft/Unity-Map-Review.png` (runtime map component in an editor preview scene, not a live gameplay capture)

No existing title image, character artwork, sprite slicing, world geometry, audio or 3D player activation was replaced.

## Reuse the toolchain

```powershell
powershell -File Tools/ArtCraft/Install-ArtCraft.ps1
python Tools/ArtCraft/artcraft.py doctor
python Tools/ArtCraft/artcraft.py audit
python Tools/ArtCraft/artcraft.py export Artifacts/ArtCraft/Sources/FacilityGlyphs.svg Assets/AfterSignal/Resources/Art/ArtCraft/UI/FacilityGlyphs.png --max-size 1024 --force
```

Requires Python 3.11+. No Python imaging package is required by the public exporter. The inventory reads PNG headers and importer metadata; it does **not** represent actual loaded VRAM or FPS.

`ARTCRAFT-TOOLS.cmd` opens the editable facility atlas in VectorCraft; `ARTCRAFT-TOOLS.cmd photo` opens PhotoCraft. To attach an agent via stdio MCP, run one of:
```powershell
powershell -File Tools/ArtCraft/Start-ArtCraftMcp.ps1 -Tool photocraft
powershell -File Tools/ArtCraft/Start-ArtCraftMcp.ps1 -Tool vectorcraft
```
These MCP processes can read/write only `Artifacts/ArtCraft`. No global agent configuration is changed. The current Codex task can call the CLIs directly without MCP registration.

The exporter writes only to the private work directory or the designated ArtCraft runtime-art subtree, validates PNG output, preserves the source, backs up replaced destinations when `--force` is used, and logs source/output SHA-256 and tool versions. Export success is not visual approval. Pixel-art resizing needs `--nearest`; never resize a sliced character sheet independently of its sprite rectangles/pivots.

## Wider asset work

| Area | Suitable use | Current status |
| --- | --- | --- |
| Map, signs, interface symbols | VectorCraft editable geometry and consistent raster exports | Map symbols integrated; other signage unchanged |
| Portraits and sprites | PhotoCraft masks/layers and repeatable export profiles | Export tool available; original portraits/sheets preserved |
| World materials | PhotoCraft albedo editing, Blender/Material Maker for PBR baking | Existing PBR pipeline retained; no global colour/normal edits |
| Seoha and NPC 3D | ArtCraft Studio concept/staging or external mesh generation, then Blender topology/rigging and Unity verification | No new mesh generated or activated |
| Audio / cinematics | SoundCraft / FilmCraft can be assessed separately | Not installed or used in this pass |

Editable `.pcraft`, `.vectorcraft`, `.drawcraft`, `.vctemplate` artwork is now also covered by the public-source exclusion check. Restore the native sources and runtime atlas with the private asset pack. Public source remains usable without downloading creative-tool binaries, but visual output requires the private assets.

## Essential checks

Unity EditMode: **5 passed, 0 failed**. Final compilation and map preview succeed. The preview uses the actual atlas/label components in an isolated editor scene; it is not a live playtest.

See `Artifacts/ArtCraft/tests.xml`, `unity-tests.log`, `unity-preview.log`, and export records. Only ArtCraft import/category checks and the actual runtime-map preview are run for this change; full campaign, physics and performance tests are not repeated.
