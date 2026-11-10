# PolyCar development status

## Playable routes and environments

- Countdown, ordered checkpoints, single-lap routes, timed finish and bronze/silver/gold evaluation.
- Optional coin collection and completion-only reward settlement.
- Pause/resume/restart/leave, coherent result shortcuts, penalized manual and automatic recovery.
- Mid-run vehicle purchase/selection disabled; welcome reward persisted; real personal records replace placeholder competitors.
- Grounded vehicle forces, lower speed/acceleration tuning and more stable yaw damping.
- Three-second combo expiry, collision invalidation and actual-slip scoring.
- Ten continuous routes with distinct authored environments, real mountain elevation, collidable shoulders and smooth off-road return ramps.
- Per-route sky, fog and sunlight; sunset coast and night streets with emissive signs and local lamps.
- Faceted vehicle meshes, wheel/brake/body feedback, smoke, tire trails and live garage preview.
- Compact driving HUD and independent timing/score layout.

## Validation scope

Automated tests and deterministic physics route following are distinct from human playtesting. The route follower completes all ordered gates with the starter tune; the current per-route evidence is saved in `Review/Environment`. UI renders are reviewed at 1280x720 and 1024x768. Keep the exact latest test count and build evidence in the task report rather than claiming every possible bug is covered.

## Next work

1. Human playtest keyboard steering, drift entry/exit and difficulty; tune medal thresholds from actual runs.
2. Improve authored engine/tire/crash audio and introduce a short interactive drift lesson.
3. Add saved personal ghost replays and checkpoint split-time comparison.
4. Tune routes 2–10 from human feedback; progressive layouts and ten authored environments are implemented, including real elevation on the mountain routes.
5. Verify gamepad navigation, accessible typography/contrast, ultrawide and lower resolutions, and target-device performance.
6. Move remaining generated menu layouts into reusable UI prefabs once art direction stabilizes.

The project now includes ten playable routes with authored environments. Human driving-feel review, final audio and broader device/accessibility testing remain follow-up work.

## External art integration completed

- Verified a first-car/first-route sample before applying to all four cars and ten routes.
- Integrated licensed Kenney vehicle, industrial, racing-facility and nature models with source/license records.
- Converted source materials to URP with pack-specific atlases; normalized pivots and scale.
- Preserved drivetrain tuning, chassis colliders, ordered gates and existing obstacle colliders.
- Added wheel bind rotations and per-vehicle rear tire effect positions; adapted pickup/hit colors to URP.
- Retained the earlier Harbor build for comparison; the asset-upgraded player is built separately.
