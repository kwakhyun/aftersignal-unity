# Four cities: sources and original assets

Architecture is authored parametrically in `CityGeometry` and `FourCityArchitecture`. No commercial game models, branding, logos, or architectural CAD files are redistributed. The following primary references informed spatial organization and construction details:

- [Populous — Tottenham Hotspur Stadium](https://populous.com/showcases/tottenham-hotspur-football-club): tiered seating bowl, concourse, permeable facade, clubrooms, roof trusses and food service.
- [Populous — Yankee Stadium](https://populous.com/showcases/yankee-stadium): baseball field organization, fan concourses and civic exterior rhythm.
- [Safdie Architects — Jewel Changi Airport](https://www.safdiearchitects.com/projects/jewel-changi-airport): glazed canopy, central water feature, planted circulation and elevated walks.
- [Safdie Architects — Marina Bay Sands](https://www.safdiearchitects.com/projects/marina-bay-sands-integrated-resort): separate hotel wings connected by a public sky deck. The game's hotel is an original smaller composition.
- [BIG — Oceanix Busan](https://big.dk/projects/oceanix-busan-4711): connected neighborhoods and marine infrastructure. Oceanix is a floating proposal; the pressure dome and submerged streets in this game are fictional.
- [Frontier — Planet Coaster 2 management](https://www.planetcoaster.com/en-US/news/2024-09-25/deep-dive-mastering-management): distinct ride attendants, boarding queues, operations and guest services. No Frontier assets are used.

Sports rules are implemented as a serializable simulation feeding actors, a ball, scoreboards and ticket settlement:

- [IFAB — duration of the match](https://www.theifab.com/laws/latest/the-duration-of-the-match/?selectedLanguage=en)
- [IFAB — offside](https://theifab.com/laws/latest/offside/)
- [FIBA — official rules and interpretations](https://refereeing.fiba.basketball/en/rules)
- [MLB — regulation game](https://www.mlb.com/glossary/rules/regulation-game)
- [MLB — automatic runner](https://www.mlb.com/news/automatic-runner-permanent-new-mlb-rules-for-position-players-pitching)
- [Formula 1 — flags](https://www.formula1.com/en/latest/article/the-beginners-guide-to-formula-1-flags.T5DqOqbWI6S4Va8Y5yMld)

The city touring-car cup has its own 8-lap, two-team competition format and 10/8/6/4/2/1 team points. It is not an F1 championship. Football and basketball clocks are accelerated for an open-world visit; baseball ends by innings, not by a timer. On-field decisions are simulated, not a human-controlled professional sports engine. Betting uses only the existing fictional Credits balance.

## Cinema

`Assets/StreamingAssets/Cinema/SignalTide.mp4` is a 60-second original 3D short made for AFTERSIGNAL. `Tools/FourCities/create_cinema.py` authors the city, camera animation, titles and original synthesized soundtrack in Blender 4.5.3. There are no external clips, audio samples, brands or logos. Blender's bundled font is used in the rendered titles. The reproducible scene is stored locally in `Artifacts/FourCities/SignalTide.blend`.

Big Buck Bunny's [CC BY 3.0 distribution terms](https://peach.blender.org/about/) were researched, but the external download was rejected by automatic approval review. That film is not included. The original short replaces the proposed download.

## NPC art

Eight original 4-by-4 directional sheets were generated through the built-in image generation tool: FootballPlayer, BaseballPlayer, BasketballPlayer, RacingDriver, AbyssEngineer, AbyssCitizen, AbyssMedic and CorruptedCitizen. The shipped Worker sheet supplied only the existing project's proportions, outline style and grid layout. Each sheet contains front/right/back/left idle, two movement poses and a role-specific action. Source images are copied into `Assets/AfterSignal/Resources/Art/NpcDirections`; import normalization detects gutters, sets a feet pivot, and removes connected neutral background pixels without punching holes in opaque clothing. Prompts are recorded in `ART-PROMPTS.json`.
