# Essential validation — 2026-09-09

The opt-in Windows diagnostic is `ResponseRenewalSmoke` (`-response-renewal-smoke`). It loads the real city, vehicle/NPC assets and runtime systems. A raised physical deck isolates the requested damage and traffic scenarios from the populated world. This is targeted native execution, not a complete city or campaign playthrough.

## Results

- `Artifacts/ResponseRenewal/Native/result.json`: 39 passed; four findings retained for correction. Covers home respawn through actual scene travel, four interactive recovery points, wound/critical/fatal damage, gang execution of a critical victim, police fire, robot retaliation/heavy health/no human launch, empty mounted vehicle fire rejection, seven casualty reports dispatching more than four ambulances, player rescue call, actual stabilization/loading/hospital recovery, heavy blast falloff, titan heat damage and nine combat audio cue families.
- `Artifacts/ResponseRenewal/Followup/result.json`: eight passed; one remaining early-steering finding. Confirmed event release after the clearance window, distinct legacy police art, actual emergency driving past an obstruction, medical crew retention and roof light height. The isolated event fixture also disables pre-existing gang convoys so they cannot enroll unrelated reinforcements into its synthetic event.
- `Artifacts/ResponseRenewal/Complete/result.json`: six passed, zero errors. The previously failing early bypass now chooses an 18-degree clear corridor and requests traffic to yield; the vehicle also physically passes the stopped car. Final titan/armour-piercing damage checks pass after the visual beam layers were added.

There were no native runtime exceptions in these completed runs. One intermediate diagnostic was stopped because it had launched before its replacement build finished; it is not counted as a completed run. No user game process was stopped.

## Corrections found during validation

The persona initializer was overwriting legacy uniforms. Corridor sampling began too close to the floor, road routing could select a different level, and the early turn penalty preferred a blocked lane. These were corrected without disabling world collisions. Ambulance collision reactions incorrectly evacuated its medical crew, and its lightbar was below the patient-cell roof. Dedicated roof lights now also use bounded red/blue materials to avoid white bloom.

## Visual review and limits

Reviewed native captures: `Native/TriageSprites.png`, `Followup/AmbulanceFinal.png`, `Complete/ResponseVehicles.png`, `Complete/TitanBarrage.png`; the Blender ambulance studio render is in `Artifacts/ResponseRenewal/Models`. The final roof-light material adjustment is a bounded color/emission change, checked by the shipping compile; broad scenarios were not repeated for that cosmetic change.

Rescue dispatch is limited to the outdoor city scene, 650 m from the player and 24 concurrent ambulances. Critical transport validation uses a nearby test arrival for the hospital endpoint; it does not establish that every possible city route is free of obstruction. Emergency avoidance preserves physical collisions and cannot cross fully sealed geometry. All-city manual endurance and a full campaign playthrough were intentionally omitted.

`LifeState`, `CityChronicle` and recovery selection are suppressed during diagnostics. Scene travel now also respects `LifeState.SuppressSave` for stage/memory preferences. The first completed home-travel probe predated that added stage/memory guard; it may have left the saved resume stage at Residence.

Shipping artifact: `Builds/ResponseRenewal/AFTERSIGNAL.exe`; launcher: `PLAY.cmd` through `Builds/active-player.txt`. Build outcome is recorded in `Artifacts/build-result.json` and `Artifacts/ResponseRenewal/build-shipping.log`. `git diff --check` passed.
