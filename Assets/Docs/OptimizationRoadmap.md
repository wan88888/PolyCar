# PolyCar Optimization Roadmap

## Current Priorities

1. Menu and game-state flow
   - Home should be the clear entry point.
   - Gameplay needs a visible return-to-menu path.
   - Escape should close popups or return to Home.
   - Status: implemented.

2. Home / Shop / Rank visual polish
   - Replace engineering-looking panels with a stronger low-poly racing style.
   - Add a vehicle hero area on Home.
   - Add readable vehicle cards, stat bars, lock states, and selected states in Shop.
   - Make Rank look like a leaderboard instead of plain text.
   - Status: implemented.

3. Level selection and progression
   - Add a Level Select screen instead of only pressing N after completion.
   - Lock later routes until earlier routes are completed.
   - Save highest unlocked route.
   - Status: implemented.

4. Route guidance
   - Add next-coin direction indicator.
   - Add route arrows/checkpoints on the road.
   - Highlight the active route objective.
   - Status: implemented.

5. Driving feel and drift reward
   - Add drift score and combo multiplier.
   - Reward clean drifts with bonus coins.
   - Tune each car so unlocks feel meaningfully different.
   - Status: implemented.

6. Collision and obstacle feedback
   - Add hit feedback when touching barriers/cones.
   - Add small time/coin penalties or combo break.
   - Add obstacle variation by route.
   - Status: implemented.

7. Audio and settings
   - Add engine, coin, button, crash, and drift sounds.
   - Wire Music/Sound toggles to actual audio mixers.
   - Status: implemented.

8. Save data and economy
   - Save completed routes, best coin totals, selected vehicle, unlocked vehicles, settings, and daily reward state.
   - Balance car prices against total coin rewards.
   - Status: implemented.

9. Performance and maintainability
   - Move procedural UI and level data toward reusable prefabs or ScriptableObjects.
   - Avoid generating large UI hierarchies by code once visual direction stabilizes.
   - Runtime cleanup: centralized economy constants, cached manager references, and material property blocks for feedback visuals.
   - Status: implemented.

10. Presentation polish
    - Add camera intro on Home.
    - Add transitions between Home, Shop, Rank, and gameplay.
    - Add completion screen after each route.
    - Status: implemented.

## Implementation Order

The best order is to improve the largest player-facing friction first:

```text
Menu/game-state flow
-> UI visual polish
-> level select/progression
-> route guidance
-> drift scoring
-> audio/feedback
-> save/economy
-> performance/code cleanup
-> transitions/completion
```
