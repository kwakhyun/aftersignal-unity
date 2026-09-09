# Response Renewal

This update builds on the local LivingHarbor and CyberConflict work. It does not commit or push the workspace.

## Player controls and recovery

The default death/retry destination is Seoha's small house. Each city has one recovery terminal attached to an existing medical facility or the Erebos military facility. The full map labels these terminals. Press E at a terminal to heal and select it as the persistent respawn destination. Starting a new game resets the selection to home. F6 reports nearby casualties to medical dispatch.

## Encounter coordination

`CityEventGate` reserves one global encounter slot for monster warnings/arrivals, gang assaults or terrorist attacks. A convoy and its deployed gang belong to the same slot. Other event spawners are blocked until the live threats and arriving carrier have gone; a terrorist bomb also holds the slot. An eight-second clearance period follows the last threat. Existing player-initiated fights and campaign targets retain their own combat rules.

The titan's radial incineration attack has a 2.8-second warning, 48 m range, 2.6-second rotating beam presentation and repeated heat pulses. Close exposed people receive lethal damage; cover blocks heat checks. Vehicles heat up through repeated strikes. Existing ordinary-building demolition/reconstruction applies after the barrage.

## Injury and rescue

Damage is classified from the actual received damage and remaining health. Overwhelming hits and follow-up lethal shots kill; survivable low health becomes critical; lesser significant injuries remain conscious. Warhead centres and close monster impacts are lethal to ordinary people. Outer splash falls off instead of forcing death. Protected story residents keep their existing protection.

Lightly wounded people can be treated on site. Critical patients retain a hittable prone collision volume, are stabilized, carried by a two-person team and transported to hospital. Gang members may deliberately shoot a downed opponent. Civilians witnessing a casualty report it, conscious injured people can self-report, and police/military have radio calls. Medical dispatch scales with reported casualties up to 24 concurrent ambulances, launching at most four per second and leaving excess patients queued.

The ambulance has an original authored body, cab, patient cell, observation windows, loading step, rear doors, climate unit and wheel pivots. Source: `Tools/WorldExpansion/create_ambulance.py`; asset: `Assets/AfterSignal/Resources/Response/Ambulance.fbx`. Injury presentation retains each original NPC's identity and layers the eight generated wound/treatment sprites onto its pose. The generated original and exact prompt are preserved in this folder.

Medical dispatch operates in the shared outdoor city scene within 650 m of the player. Minor ambulance collisions keep the medical crew on board; a destroyed ambulance still follows the normal fatal evacuation path. Upper emergency lights use bounded red/blue emission. Unassigned casualties remain in the queue when the 24-vehicle budget is occupied.

## Combat response and presentation

Police update their current target at burst time and prefer nearby opponents over a distant dispatch target. Legacy police/SWAT frames remain in a portion of the response roster alongside cyber police. Faction speech bubbles choose separate gang, police and military lines rather than civilian chatter.

Security vehicle FBX orientation is corrected to the actual driving axis. Empty mounted vehicles cannot fire. Police/army robots have 6,500/12,000 health, remember their attacker, reject civilian launch physics and explode into debris on destruction.

Emergency driving uses the road route plus local clear-corridor sampling and requests ordinary traffic to yield. Responders use their own driving input, bypassing traffic signals, civilian following-distance stops and pedestrian-crossing waits while retaining physical collisions with buildings/vehicles.

The corridor probe covers the vehicle width without intersecting the road. It prefers a clear nearby turn before reaching a queued car and retains physical checks for yielding traffic. A road graph on a different floor is rejected in favour of local navigation. This is bounded local avoidance; it cannot make a completely enclosed street passable.

Gun presentation includes weapon-sized gas flashes, muzzle smoke, ejected brass, short moving tracers, surface-specific hit particles/sounds and damped view recoil. Explosions add pressure dust, incandescent fragment trails, directional distance-scaled camera impulses and low-frequency pressure audio. Temporary light strength is limited to avoid the previous white-screen bloom problem. Presentation settings still control motion and effects. Existing recorded gunshots are preserved; new tails are derived from those recordings and impacts/pressure layers are original sound synthesis.

These are improvements to the existing Unity simulation; they are not a claim of parity with a commercial FPS production. Essential native validation and captures are recorded separately.
