# Crowd and runtime performance — 2026-09-09

The previous facility spawner fell back to the exact facility origin after every failed ground search. Multiple residents could consequently be created inside the same obstacle or unsupported point. `ExpansionWorld.TrySpawnPosition` now reports failure and retries missing residents later; it never creates a fallback pile.

Ambient spawners share a per-frame creation budget and a local crowd-density check. New walking positions require 1.45 m separation; an already busy 12 m neighbourhood declines further walkers. The active mobile civilian budget is 220 within 180 m. Responders, hostiles, players, athletes and seated spectators are not removed to meet that mobile budget. Facility crowds are created progressively; their NPC-only roots sleep at distance. Stadium seats remain separate from mobile crowd limits.

The later safety pass additionally limits growth around existing walkers (18 within 12 m), counts family members individually against the four-person frame budget, and reuses crowd separation buckets without retaining every visited map cell. Airport/ferry passengers previously repeated a 5-by-4 placement pattern and simultaneously walked to one door. They now use ground-checked, spaced waiting positions, defer creation when full, and approach each service in turn. Disembarkation also finds free positions and retries while the terminal is busy.

Street walkers advance to their next route segment before mutual separation can keep them indefinitely short of a shared endpoint. Placement reservations also prevent multiple same-frame requests from selecting the same currently empty position.

`ActorSpatialIndex` reuses 12 m spatial buckets and caller-owned result lists for nearby actor queries. Vehicle sweeps no longer copy every world actor for every moving vehicle; faction searches and ambient conversations query nearby actors. Registration/removal invalidates the index immediately, while moving actors refresh on a short timer with a query margin. Ground and body collisions remain enabled.

Distance-dependent routine and presentation work reduces repeated navigation, ground checks, billboard sprite evaluation, contact shadows and cabin portrait work. Nearby movement and urgent activity retain frequent updates. Vehicle engine layers outside their audible range pause DSP playback and resume when approached. Vehicle floor samples use a reusable nonallocating hit buffer with a saturation fallback. Scoreboards avoid rebuilding unchanged world text every frame.

No ground, road or building renderers were removed or disabled by this update. The changes target NPC simulation/presentation, actor searches, vehicle audio and transient allocations. The existing graphics settings and BGM configuration are retained.

## Before/after measurement

The opt-in `CrowdPerformanceProbe` uses the same two positions, 1600×900 rendering, fixed random seed, disabled ambient conflict generators and a rendered-frame GPU fence. This includes diagnostic readback overhead and must not be interpreted as an exact gameplay FPS promise. The same RTX 4060 Ti was used for both runs.

| View | Before mean | After mean | Before p95 | After p95 |
|---|---:|---:|---:|---:|
| City | 36.25 ms | 29.83 ms | 43.93 ms | 36.81 ms |
| Stadium surroundings | 36.15 ms | 24.41 ms | 44.34 ms | 35.04 ms |

Active human actor counts were 217 → 222 in the city view and 628 → 499 around the stadium. These counts include responders and people inside nearby facilities. Dense spectator populations remain, while adjacent distant facilities do less work. All-world close pairs include authored characters and scripted formation/seat poses, and are not a reliable zero-overlap assertion for the player neighbourhood.

Raw reports and captures: `Artifacts/CrowdPerformance/Before` and `After`. Measurements precede the final terminal-spacing and density refinements; those receive targeted functional checks. The Mono release allocation counter reported zero and is treated as unavailable; no numerical GC-reduction claim is made. The implementation removes known per-frame arrays, but this probe does not establish a universal framerate across all four cities, heavy combat or every hardware configuration.

The narrow functional probe `CrowdSafetyProbe` checks blocked/unsupported spawn rejection, bounded creation and spacing, actor query registration/removal, distant work throttling, urgent bypass, faction search and preserved floor support. The initial run identified growth around an already crowded point and repeated terminal passenger positions; both were fixed. Final native result: **14 checks passed, zero errors**, including passenger spacing and actual boarding. Results are stored in `Artifacts/CrowdPerformance/SafetyFinal`; the earlier failing record remains in `Safety` for traceability.

Windows shipping build: `Builds/CrowdPerformance/AFTERSIGNAL.exe`, succeeded with **zero errors / 85 warnings**. `Builds/active-player.txt` selects this build. Build report: `Artifacts/CrowdPerformance/build-result.json`. No complete campaign replay or long-duration four-city soak test was run.

Run the updated player with `PLAY.cmd`. This request does not commit or push the accumulated local changes.
