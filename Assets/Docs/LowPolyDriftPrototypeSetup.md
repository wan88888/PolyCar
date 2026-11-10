# PolyCar / Harbor Sessions

Unity 6000.2.7f2. Open `Assets/Scenes/DriftPrototype.unity` and press Play.

## Current slice

Route 1 is a single-lap harbor introduction: a 12 m road, 12 ordered gates and no obstacles. Route 2 is a 300 m flowing S-bend lesson with an 11 m road and two edge cones. Routes 3–10 combine rounded bends, gradually narrower roads and more obstacles; all use continuous road surfaces and themed scenery.

Cars use licensed Kenney FBX meshes, rims, lights, wheel animation, visual body lean, slip-dependent tire trails and smoke. The home screen renders the selected vehicle in a separate preview camera. Driving HUD separates route/timing, speed and drift score.

## Controls and flow

- W / Up: accelerate. S / Down: reverse.
- A / D or Left / Right: steer.
- Space: handbrake drift. Shift: brake.
- R during a run: recover to the last passed gate, with a 3 s time penalty; progress is retained.
- Escape or PAUSE: pause, resume, restart the whole run, or leave it.
- R on results: replay. N on results: next route when available.
- Change and purchase cars in the garage, outside an active run.
- Home focuses the play button; route selection focuses the current route. Enter submits the focused button.
- Gamepad: left stick steering, right trigger throttle, left trigger brake, south button handbrake. Full gamepad menu navigation is not yet validated.

Choose a route, wait for the three-second countdown, and cross each amber-highlighted gate in the forward direction. Coins are optional; collecting all of them does not finish a checkpoint race. Harbor requires one lap / 12 gates. Flipping for two seconds or falling below the course automatically recovers the car.

## Scores and rewards

- Bronze: finish the route.
- Silver: finish with the route-specific drift target (100 on Harbor, 150 on S-Curve Sprint).
- Gold: meet the route-specific time and drift targets with no recoveries. Harbor: 55 s / 300 drift; S-Curve Sprint: 34 s / 450 drift.
- Finishing alone unlocks the next route; neither medals nor coins are mandatory.
- Drift requires at least 18 km/h and 0.5 m/s lateral slip. Clean segments must last at least 1.1 s and earn at least 120 points.
- Three seconds without scoring resets the combo. A collision invalidates the current drift.
- Coins and drift bonuses are banked only at completion, plus a 10-coin finish reward, minus run penalties. Leaving/restarting discards unbanked rewards. Collisions do not spend the player's existing wallet.
- Best time and drift score are saved per route. Records show only the player's real results.
- Legacy reward popups remain in the scene but are hidden from the streamlined home screen. The welcome spin is persisted once per save.

## Authoring

`Tools > PolyCar > Create Drift Prototype Scene` rebuilds the scene and generated prefab/model/material assets. It replaces the generated scene, so preserve bespoke scene edits before rebuilding.

- `LowPolyDriftPrototypeBuilder`: vehicle tuning, references, menus, remaining route layouts.
- `HarborArtBuilder`: harbor centerline, road mesh, scenery, gates and faceted vehicle meshes.
- `RaceCourse`: ordered gate geometry, lap count, medal thresholds and active gate highlight.
- `LevelManager`: Ready / Countdown / Racing / Finished lifecycle, race clock, recovery and completion settlement.
- `CarController`: grounded driving forces; no propulsion, steering or grip in the air.
- `CarPresentation`: wheel, body, brake-light, smoke and tire-trail feedback, separate from physics.
- `GaragePreview`: isolated render texture using layer 31, excluded from the main camera.

## Validation

Run EditMode tests in Unity Test Runner. `DrivingRegressionTests` covers drivetrain and drift rules; `RaceFlowTests` covers countdown, pause timing, gates, pending rewards, recovery, medals, purchases and airborne control.

`HarborDriveValidation.Run` follows the real road with a deterministic input controller and Rigidbody simulation. It validates physical traversability, not human driving feel. `HarborPreviewCapture.Run` produces offline scene/UI review images in `/tmp/polycar-harbor-*.png`.

Human playtesting, broader aspect-ratio testing, final audio production, ghost replays and full gamepad accessibility remain follow-up work.

### Drift feel refinement

Space / gamepad south now loosens grip with a mild 3 m/s² deceleration, independent of the full-strength Shift / left-trigger brake. Tap it on entry, then hold throttle and balance steering; countersteering retains the slide while meaningful lateral motion remains. Grip restores gradually on exit, and steering further into a large slip angle is reduced to help avoid spins. The controller still requires real lateral motion for drift scoring.

Tire marks use the `PolyCar/TireMarks` transparent shader, soft tread edges, rounded ends and a five-second fade. Intermediate checkpoints retain their light beacons and ground stripes; a slim, widened finish gantry replaces the large imported arches.

The 38 EditMode tests include separate keyboard/gamepad handbrake mapping, coasting handbrake versus pedal stopping, and four-second powered drifts plus countersteering for all four vehicle prefabs. These deterministic physics checks complement, rather than replace, interactive playtesting.

## External asset pipeline

65 selected source FBX models from Kenney Car Kit 3.1, City Kit Industrial 2.0, Racing Kit 2.0 and Nature Kit 2.1 are stored under `Assets/ThirdParty/Kenney`. Each pack includes its original `License.txt` (CC0); `Sources.json` records source URLs, selected files and archive SHA-256 values. No paid assets or third-party scripts are required.

`ExternalArtBuilder` normalizes imported pivots/scale, maps each pack to its own URP materials/atlas, binds four independent wheel pivots, and replaces the visual parts of obstacles while retaining existing collision/penalty logic. Nature palette colors are adapted to the scene. Imported scenery is decorative; the existing course collision geometry remains authoritative.

The normal scene builder applies all external art when the source manifest is present. `Tools > PolyCar > Build External Art Sample` rebuilds a first-car/first-route comparison; `Build All Routes With External Art` applies all four vehicles and ten routes. Generated scene edits will be replaced by these commands.

The shop uses a two-column vehicle catalog with a house icon for returning home, a centered Shop title, a coin-icon balance matching Home, vehicle names, three stat bars and purchase/selection status. Redundant body-style labels, description sentences and footer tags are removed. Page and card headings use sentence case. `ShopPreviewBuilder` renders the four actual playable prefabs after external art replacement into `Assets/UI/VehiclePreviews`; these static images add no runtime preview cameras. Rebuilding the scene refreshes them when models change and requires graphics rendering. Display names describe the sedan, hatchback, SUV and open-wheel racer; saved vehicle IDs and unlock costs remain unchanged.

Home, Shop and Records use plain opaque page surfaces. Home removes promotional/helper copy and keeps the title, vehicle and navigation; `MenuIcon` draws scalable coin and settings symbols, and the coin balance is numeric. The home vehicle preview clears to transparency so it blends into the page. Records uses ten aligned rows for route, best time, best drift and status, with dashes for unset results and a completion count. All record values continue to come from the player's save.

Routes, pause, settings and results now share the same plain surfaces. All action/navigation buttons use `MenuIcon` symbols; vehicle and route cards retain identifying text and numeric data, with play, lock or check symbols for their actions/states. Audio setting names remain visible beside their symbols and check controls. `IconButtonHint` explains actions on hover or keyboard/controller selection, stays inside the canvas on resize, does not block raycasts and hides on click, submit or panel close. The last route disables and dims the next-route symbol. Reward popups use the same icon buttons but remain hidden from the normal home flow.

Layout reference: [Forza Horizon 5 vehicle catalog on the official community forum](https://forums.forza.net/t/4-ford-focus-still-missing-from-autoshow-after-hotfix-update-05-04-2023/592716). Only the vehicle-card presentation pattern is referenced; the images are renders of this project's licensed models.

`ExternalArtValidation.Run` checks all four vehicles for wheel bindings and unwanted colliders, checks all ten routes for missing meshes/materials/atlases, and writes per-route triangle/material budgets plus renders to `/tmp/polycar-external-review`. Editor camera rendering times are diagnostic only and are not standalone game FPS measurements.


### Route preview cards

Routes now displays two large rounded preview cards per page, with an orange name ribbon, route number and a compact progress/unlock line. The five pages cover all ten routes. Locked previews use a dark overlay and gold lock; clicking them retains the unlock explanation. Returning to Routes opens the page containing the current route. Previous/next arrows stop at the first and last page.

`RoutePreviewBuilder.BuildThumbnails` photographs the ten actual `RaceCourse` scenes into `Assets/UI/RoutePreviews`, after external scenery is applied. It temporarily disables distance fog for readable previews and restores scene activation/render settings afterwards. No preview cameras run in the player. Rebuilding the generated scene refreshes the images and rounded card sprite.


### First two showcase environments

`ShowcaseSceneryBuilder` authors distinct layouts for Harbor Loop and S-Curve Sprint. Harbor adds cargo aprons, warehouses/loading bays, crane braces, a tapered cargo vessel, mooring gear, quay walls, a distant bank and gently animated stylized water. The works yard uses warehouse blocks, storage tanks, chimneys, an elevated pipe rack, drains and service parking. Imported ground-level props have explicit collision boxes and must clear the course corridor before the builder accepts them.

S-Curve Sprint uses one continuous road mesh with painted edges and rounded transitions, replacing the small angular yard. Its new centerline is about 300 m long, with an 11 m road, eleven gates and two cones. The works district is repositioned around this larger layout. Decorative checkpoint ticks and the low start beam are removed, and actual gate markers are reduced in size for readability.

`SceneLook` owns each showcase level's sky, fog and sun tint, restoring shared state when disabled. It is also applied explicitly for editor captures. `ShowcaseSky` and `HarborWater` are lightweight procedural URP shaders. The other eight levels keep their existing presentation. Scene rebuilding updates the route-card previews.

### Progressive difficulty

`CourseDifficultyBuilder` owns geometry, widths, obstacle counts, skill focus and medal targets. Roads taper from 12 m to 7.2 m in steps no greater than 1 m. Obstacle counts rise by two per route (0 to 18); placement reserves at least 24 m after the start, 20 m before the finish, 7 m around gates and 15 m between obstacles, preferring straight approaches over blind corners. Entry-level obstacles are edge cones. Continuous road and off-road collider heights prevent snagging when returning from a shoulder.

| Route | Width (m) | Obstacles | Gold time (s) | Gold drift |
|---|---:|---:|---:|---:|
| 1 | 12 | 0 | 55 | 300 |
| 2 | 11 | 2 | 34 | 450 |
| 3 | 10.5 | 4 | 62 | 650 |
| 4 | 10 | 6 | 65 | 850 |
| 5 | 9.5 | 8 | 62 | 1100 |
| 6 | 9 | 10 | 95 | 1350 |
| 7 | 8.5 | 12 | 60 | 1600 |
| 8 | 8 | 14 | 105 | 1900 |
| 9 | 7.6 | 16 | 92 | 2200 |
| 10 | 7.2 | 18 | 102 | 2600 |

Gold times reflect different route lengths: approximately 40% spare time over the deterministic starter-car reference early on, tightening to 19% by the finale. `DifficultyValidation.Run` drives all ten courses with the same car and controller, without teleporting or recovering; `DriftRun` checks the first two with additional handbrake inputs. These are repeatable physics checks, not proof that every human player will perceive a strictly increasing difficulty.

Revised-route time, drift and coin records use the `PolyCar_v2_` namespace. Existing legacy records remain stored; wallet balance, car ownership, selected car and route unlock/completion progress retain their keys. This prevents obsolete short-route best times from becoming unbeatable records on the longer courses.
