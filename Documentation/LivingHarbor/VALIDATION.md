# Essential validation — 2026-09-09

Windows release: `Builds/LivingHarbor/AFTERSIGNAL.exe`, selected by `Builds/active-player.txt` and launched by `PLAY.cmd`.

Final build: succeeded, 2,058,051,356 bytes, 0 errors, 77 warnings. Log: `Artifacts/LivingHarbor/build8.log`; build report: `Artifacts/build-result.json`. Warnings include existing obsolete API/URP and large-triangle notices; they were not treated as a clean warning-free build.

## Exercised behavior

- Twelve overlapping civilians separated to a minimum of 1.04 m. Family/couple roles, children, time-dependent dialogue and companion injury/death reactions were exercised.
- A rescue follower crossed a 0.8 m curb. Actual campaign Begin/Update/Advance paths completed assault, defend, sabotage, rescue, escape and boss scenarios. This was not a manual playthrough of every campaign mission.
- Building collapse and restoration, overlapping reversible mesh cuts and below-ground foundation protection passed. Shared street-floor geometry survived the foundation cut.
- Football, baseball, basketball and racing populated crowds and paused/resumed on intrusion. Injury triggered venue evacuation behavior. Root visitor totals in the fixtures were 362/362/298/354, including family roots; sports remain an event/rules simulation with improved visual motion, not a fully physical sports engine.
- Monster/gang events reached the map feed. Contributions granted 650 + 90 credits, and the terrorist reward was separately verified at 160 credits with no duplicate award.
- Airport passenger boarding charged once. Aircraft cockpit and cargo-ship helm access passed. The generated world relocated 11 obstructive harbor high-rises and moved 12 aircraft from the army compound to the separate air base.
- Navy/coast guard/pirate hulls, new uniforms, disembarked faction preservation and armed-ship crew retention passed. Naval rounds collided with and damaged an opposing vessel.
- The independent terrorist faction could not report itself as a witness. A civilian report initiated police response, and army targeting excluded terrorists. Retaliatory gunfire hit the player when spawn protection was cleared in the diagnostic fixture.

## Run records and corrected fixtures

The initial `Artifacts/LivingHarbor/Native/result.json` contains 49 passing checks and two diagnostic failures: the curb assertion compared an absolute negative world coordinate to a positive threshold, and a naval fixture selected a destroyed ambient vessel. Follow-up fixtures corrected both.

`Artifacts/LivingHarbor/Followup/result.json` contains eight passing checks and an idle-guard incident-marker assertion. The guard was changed to an active threat, matching the incident board's intentional filter.

`Artifacts/LivingHarbor/Final/result.json` contains 14 passing checks and one shooting assertion. That fixture disabled GameDirector, so the normal PlayerMotor tick never expired Respawn's invulnerability. The corrected fixture explicitly clears that protection; no production damage exception was added.

The final focused run, `Artifacts/LivingHarbor/Complete/result.json`, completed with **2 passing checks and 0 errors**: player hit detection and re-exported ship ballistic collision. Already-passing broad scenarios were not repeated. The native diagnostic suppresses LifeState/CityChronicle save writes. The user's existing game process was left running; only the opt-in diagnostic player exited.

## Visual inspection

The five original character sheets and their imported alpha backgrounds were inspected. In native captures, the stadium crowd, families, curb rescue and cleared harbor frontage were inspected. Blender hull faces initially rendered in the authoring preview despite inverted winding; outward-normal recalculation fixed Unity's missing deck faces.

Final native hull captures were inspected after that fix:

- `Artifacts/LivingHarbor/Complete/SeaPatrol.png`
- `Artifacts/LivingHarbor/Complete/CoastGuardCutter.png`
- `Artifacts/LivingHarbor/Complete/PirateInterceptor.png`

Other captures: `Artifacts/LivingHarbor/Native`, `Artifacts/LivingHarbor/Followup`, `Artifacts/LivingHarbor/Final`.
