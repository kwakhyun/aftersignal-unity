# Compact city integration

The Afterlight and Nova cultural/public districts now occupy existing city parcels. No new city land was added. The former Afterlight northward slabs, Nova southern slabs, and Nereid public annex dome are no longer generated.

- `Assets/AfterSignal/Resources/WorldAssets/CompactCityLayout.json` is the authoritative parcel plan: 8 Afterlight, 20 Nova, and 8 Nereid relocations. Facility IDs and list indices remain stable.
- `prefab-layout-v1.json` records 209 existing buildings relocated into free lots inside the original cities. Their interactive children and separate batched geometry move together. Roads and airport/harbour campuses remain in place.
- `CompactCityBuilder` removes presentation triangles only within redeveloped parcels and reconstructs their boundary polygons. Ground collision remains available while runtime parcel geometry is constructed. The layout stamp prevents applying the old-to-new translations twice.
- The new Dawn Alley is 280 × 220 m with 172 small furnished dwellings, shopfronts, overhead utility cables, rooftop extensions, and a single-storey house for Seoha. It uses the old city's northwest edge, not the former detached northern district. `HavenQuarter.prefab` is retired. Home centres are approximately 15 × 17 m apart, with wider clearance for the existing road and Seoha's entrance.
- Seoha's separate home interior is also rebuilt as a small ground-floor house. The bed, wardrobe, personal safe, and direct street exit remain usable.
- Nereid's eight additional public facilities share the original 2,060 × 1,860 m pressure dome. Island villages use solar roofs, glazed modular homes, bio infrastructure, electric-vehicle loops, and autonomous ferry terminals.

## Controls and population

- **ESC** closes the map before pause-menu handling.
- **V** requests Seoha's motorcycle; the pause menu also has a call button. Delivery selects a clear, supported location. Indoor requests are delivered after returning outside; unsupported locations remain queued until safe delivery is possible.
- The roof panorama shortcut moves to **P** to avoid conflicting with motorcycle delivery.
- Road traffic and pedestrians select from original streets, new facility access roads, Nereid roads, and island rings. Population budgets count vehicles near the player instead of distant persistent fleets. Erebus remains excluded from ordinary street population replenishment.

## Essential validation

`-compact-city-smoke` checks the 36 moved facilities, entrance support, small-house functionality, home-area vehicle entry, motorcycle delivery, map ESC handling, regional population and traffic, and captures six native views. Final results are in `Artifacts/CompactCities/Final/result.json`: 91 checks passed, no runtime errors. The full game regression suite was not run.

Visual inspection exposed ground triangles being moved with the buildings above them. `Tools/Compact-GroundManifest.py` obtains original mesh references from the pre-layout commit. `CompactCityBuilder.RepairGround` restores only horizontal ground triangles outside the new parcels, keeping the relocated building geometry intact. 252 ground batches were corrected; original road and pavement fragments stay in place. The main rewrite method now excludes ground from building translations. Do not blindly regenerate the four prefabs from an older world generator, which would undo the packed layout.

The final native capture confirms the exposed triangular gaps are gone. Floor, plaza, road, and foundation heights were separated to avoid coincident surfaces. This is a scoped visual review, not a claim that every surface throughout the world was manually inspected.

The existing campaign data is migrated at load through stable facility IDs/previous parcel positions. Save identifiers and player progress are preserved. Old saved car coordinates on removed extensions are relocated to the corresponding compact facility entrance.
