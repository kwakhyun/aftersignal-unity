# Vehicle occupants and Seo motion refinement

## Review and changes
- Cars previously displayed full-body sprites at 0.42 scale (buses 0.65), so occupants' heads were too small. Dedicated steering-wheel upper bodies now render at 0.72 m in sedans/taxis, 0.98 m in trucks, and 1.06 m in buses.
- Seo has eight camera-relative driving views and eight seated passenger views. Sixteen NPC driver appearances have four views each, including police, tactical officers, medical staff, office workers and citizens.
- Passenger torsos are cropped from their own four-direction artwork, preserving individual faces/outfits. Boarding identities and evacuation logic remain connected. Truck occupants now sit in the front cab rather than at the sedan's old seat position.
- New running artwork provides eight phases for five canonical views; left-side views use the matching mirrored right-side angle, covering eight directions.
- Front/back sequences were revised after visual review to distinguish the alternating lead leg and high-knee passing frames.
- New ascent, descent, landing, flinch, dash, rope swing and alternating wall-climb poses replace shared jump/idle placeholders.
- Shared atlas scale preserves anatomy without enlarging crouches or shrinking raised-arm poses. One-frame landings also trigger the landing pose and sound. Pixel-grid position snapping is removed for Seo's illustrated 3D presentation.
- Running advances from actual horizontal distance, keeps phase when turning, and shares a 3.4 m stride with footstep timing. Direction boundaries retain hysteresis. Lean settles smoothly instead of snapping.
- Existing three-weapon/eight-direction combat artwork and contact timing remain in use; hurt and traversal states now have explicit animation priority.

## Assets
- `Assets/AfterSignal/Resources/Art/VehiclePortraits/`: five atlases, 80 authored upper bodies.
- `Assets/AfterSignal/Resources/Art/SeoRefined/`: ten atlases, 80 authored action/run frames.
- `Documentation/Art/vehicle-motion-prompts.jsonl`: generation prompts, reference paths and original output provenance.
- All raster art generated with the built-in image generation tool. Unity imports split cells and remove only exterior light mattes.

## Essential verification
Windows release: Builds/Motion/AFTERSIGNAL.exe, 857251823 bytes, 0 errors and 40 warnings. All 17 essential native checks passed, no runtime errors. Checks cover actual run/ascent/descent/landing/hurt/dash transitions, four vehicle types, Seo entering, NPC passenger variety, driver position and wreck cleanup. Final report: Artifacts/Motion/Final/motion.json; build log: Artifacts/motion-release-build.log. Captured previews are in Documentation/Motion/. No full campaign or extended performance run. Launch PLAY.cmd.
