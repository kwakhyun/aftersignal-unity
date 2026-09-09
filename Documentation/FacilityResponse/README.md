# Facility fleets, collision and dialogue update

2026-09-09. Windows release player: `Builds/FacilityResponse/AFTERSIGNAL.exe`; `PLAY.cmd` follows `Builds/active-player.txt`.

## Changes

- Nearby fire stations, police stations and hospitals maintain two dedicated, unoccupied vehicles in clear forecourt positions. Authored generic parking and ambient random parking exclude these forecourts. Player-owned vehicles and active emergency or burning vehicles are preserved. Removed/requisitioned service slots refill after a cooldown.
- Fire dispatch can requisition a waiting fire engine. En-route engines are excluded from ordinary distant-car disposal; dispatch searches for a clear launch bay. The dedicated body retains response light lenses, uses a matching physical chassis, and supports hose/first-aid crew deployment.
- Strong motorcycle/vehicle collisions eject the rider and launch the bike in a bounded arc. Light collisions do not eject riders. The bike uses sweep collisions, limits horizontal travel and regains upright, drivable orientation when settled. Both player and NPC riders are handled.
- Navigation attaches to road segments rather than forcing travel through a previous junction. The HUD pin follows the active route segment. Main combat objectives select the active enemy, device, rescue or extraction goal; the home objective uses the current small-house entrance. Live interaction points replace the early cached quest-point snapshot.
- NPC encounters start with an NPC turn, without a fabricated Seoha greeting or user history entry. The gateway receives occupation, local place, time and immediate danger/injury context. Missing connectivity falls back to local contextual opening dialogue; later player input and fixed campaign dialogue remain available.
- Reusable visual assets, serialized visual worlds and their metadata were removed from the Git index while preserving every local file. See [private asset policy](../PRIVATE-ASSETS.md), including the limitation for earlier public commits.

## Essential verification

Final native run: **15 checks passed, 0 errors**. Windows release: **Succeeded, 0 build errors**, with 91 existing build warnings. Public asset index check and offline gateway request checks passed.

`result.json` records the native `-facility-response-probe` run: facility-specific parked fleets, a real local fire dispatch and crew deployment, motorcycle collision/ejection/landing, route progress and extraction guidance, and the NPC-first UI/HTTP flow.

The collision fixture synchronizes its ground collider before vehicle initialization, since the project deliberately disables automatic transform synchronization for performance. Fixture geometry is not included in the public screenshots. The HTTP test uses an offline local response; it validates message shape and display, not current provider credentials or live model output. The Python gateway's opening and follow-up request construction was checked offline separately.

`build-result.json` records the Windows release build. Older diagnostic suites were not repeated. These checks do not assert every station layout, every road or every simultaneous encounter is error-free.

## Published screenshots

The [fire station](../Screenshots/fire-station.png), [Afterlight streets](../Screenshots/afterlight-streets.png), and [Nova City](../Screenshots/nova-city.png) images are native game/HUD captures with the regular world geometry. They are flattened presentation images, not reusable model or sprite sources. A camera placed above Nereid's air enclosure produced an obscured underwater overview and was not selected for the README.
