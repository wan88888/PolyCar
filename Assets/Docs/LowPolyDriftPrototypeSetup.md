# Low Poly Drift Racing Prototype

Unity version: 6000.2.7f2

The prototype now uses road-based levels instead of a flat coin field. Each route has its own road layout, spawn point, coin path, and target count.

- Route 1: Harbor Loop, 10 coins, road width 5.2, 0 obstacles
- Route 2: S-Curve Sprint, 12 coins, road width 4.9, 6 obstacles
- Route 3: Switchback Climb, 14 coins, road width 4.6, 12 obstacles
- Route 4: Quarry Figure Eight, 16 coins, road width 4.4, 16 obstacles
- Route 5: Sunset Slalom, 18 coins, road width 4.2, 20 obstacles
- Route 6: Dockside Spiral, 20 coins, road width 4.0, 24 obstacles
- Route 7: Canyon Needles, 22 coins, road width 3.9, 28 obstacles
- Route 8: Ridge Hairpins, 24 coins, road width 3.8, 32 obstacles
- Route 9: Industrial Gauntlet, 26 coins, road width 3.6, 36 obstacles
- Route 10: Midnight Pinball, 30 coins, road width 3.4, 42 obstacles

You can rebuild everything from Unity with `Tools > PolyCar > Create Drift Prototype Scene`.

## How To Play

Open `Assets/Scenes/DriftPrototype.unity` and press Play. The game now starts on the Home screen.

Home screen buttons:

- Coins: opens Shop.
- SET: opens Settings popup.
- SPIN: opens Spin Wheel popup.
- DAILY: opens Daily Check-in popup.
- SHOP: opens Shop.
- RANK: opens leaderboard.
- LEVEL SELECT: opens the 10-route level select screen.

Level Select:

- Route 1 is available from the start.
- Completing a route unlocks the next route.
- Locked route cards stay visible and show which previous route is required.
- Clicking an unlocked route loads its map and starts gameplay.

In gameplay, drive along the visible road and collect the route coins.

Route guidance:

- The top-center HUD shows the next coin distance and direction.
- Cyan arrows painted on the road show the intended driving line.
- Orange checkpoint marks sit under the coin path.
- The current target coin is larger and cyan-highlighted.

- W / Up Arrow: accelerate
- S / Down Arrow: reverse
- A / D or Left / Right Arrow: steer
- Space: brake and handbrake drift
- Shift: brake
- 1: select Starter Car
- 2: unlock/select Drift Car
- 3: unlock/select Rally Car
- 4: unlock/select Speed Car
- R: restart the current route
- N: go to the next route after completing the current route

Gamepad driving is also supported:

- left stick: steer
- right trigger: accelerate
- left trigger: brake
- south button: handbrake drift

## Gameplay Loop

```text
Drive on the road
-> collect all coins on the route
-> earn total coins
-> unlock the next route
-> choose an unlocked route from Level Select, or press N after completion
-> spend coins in Shop to unlock more cars
```

## Screens

- Home: main entry screen with coin, settings, spin, daily, level, shop, and rank buttons.
- Level Select: shows all 10 routes, lock state, selected state, and coin objective.
- Gameplay HUD: shows speed, drift state, coin progress, total coins, route name, and next-coin guidance.
- Shop: shows the vehicle list and lets the player unlock cars with earned coins.
- Rank: static leaderboard screen for the prototype.
- Settings popup: Music and Sound toggles backed by `PlayerPrefs`.
- Spin popup: one spin reward per session.
- Daily popup: daily +20 coin reward, claimable once per calendar day.

## Difficulty Curve

```text
Route 1: learn the handling
  Road width: 5.2
  Coins: 10
  Obstacles: 0

Route 2: read the line through curves
  Road width: 4.9
  Coins: 12
  Obstacles: 6

Route 3: tighter switchbacks
  Road width: 4.6
  Coins: 14
  Obstacles: 12

Route 4: crossing figure-eight route
  Road width: 4.4
  Coins: 16
  Obstacles: 16

Route 5: longer slalom
  Road width: 4.2
  Coins: 18
  Obstacles: 20

Route 6: spiral path with repeated turning
  Road width: 4.0
  Coins: 20
  Obstacles: 24

Route 7: sharper lane reading
  Road width: 3.9
  Coins: 22
  Obstacles: 28

Route 8: dense hairpins
  Road width: 3.8
  Coins: 24
  Obstacles: 32

Route 9: obstacle-heavy gauntlet
  Road width: 3.6
  Coins: 26
  Obstacles: 36

Route 10: final narrow chaos route
  Road width: 3.4
  Coins: 30
  Obstacles: 42
```

Obstacles are low-poly barriers and traffic cones placed along the route. They keep one side of the road open, so they add steering pressure without fully blocking the path.

## Generated Assets

```text
Assets/
├── Scenes/
│   └── DriftPrototype.unity
├── Prefabs/
│   ├── Pickups/
│   │   └── Coin.prefab
│   └── Vehicles/
│       ├── StarterCar.prefab
│       ├── DriftCar.prefab
│       ├── RallyCar.prefab
│       └── SpeedCar.prefab
├── Materials/
│   ├── MAT_Road.mat
│   ├── MAT_Curb.mat
│   ├── MAT_Barrier.mat
│   ├── MAT_TrafficCone.mat
│   ├── MAT_Car_Starter_Body.mat
│   ├── MAT_Car_Drift_Body.mat
│   ├── MAT_Car_Rally_Body.mat
│   ├── MAT_Car_Speed_Body.mat
│   ├── MAT_Car_Glass.mat
│   ├── MAT_Coin.mat
│   ├── MAT_RouteGuide.mat
│   ├── MAT_Checkpoint.mat
│   ├── MAT_Ground.mat
│   └── MAT_Tire.mat
└── Scripts/
    ├── CameraFollow.cs
    ├── CarController.cs
    ├── CarGarage.cs
    ├── CoinPickup.cs
    ├── GameManager.cs
    ├── LevelManager.cs
    ├── MainMenuUI.cs
    └── SaveManager.cs
```

## Scene Hierarchy

```text
DriftPrototype
├── GameManager
├── LevelManager
├── CarGarage
├── Main Camera
├── LevelMaps
│   ├── Level_01_HarborLoop
│   │   ├── PlayerSpawnPoint
│   │   ├── Road
│   │   ├── RouteCoins
│   │   ├── Guidance
│   │   ├── Obstacles
│   │   └── StartGate
│   ├── Level_02_SCurveSprint
│   ├── ...
│   └── Level_10_MidnightPinball
├── GrassGround
├── Directional Light
└── Canvas
    ├── GameplayHUD
    ├── HomePanel
    ├── LevelSelectPanel
    ├── ShopPanel
    ├── RankPanel
    ├── SettingsPopup
    ├── SpinPopup
    └── DailyPopup
```

`LevelManager` activates only the current route, resets that route's coins, and tells `CarGarage` which spawn point to use. `CarGarage` spawns the selected vehicle prefab at the active route's `PlayerSpawnPoint`.
`MainMenuUI` starts the game paused on Home, opens `LevelSelectPanel` from LEVEL SELECT, then shows `GameplayHUD` and resumes time when the player starts an unlocked route.

## Vehicle Prefabs

Keep these components on the vehicle prefab root:

- `Rigidbody`
- `BoxCollider`
- `CarController`

Keep low-poly visual meshes under the `LowPolyVisuals` child. The car root should face Unity +Z because `CarController` treats +Z as forward.

## Core Scripts

- `CarController`: Rigidbody acceleration, braking, steering, side grip, drift detection.
- `CameraFollow`: third-person follow camera with smoothing and look-ahead.
- `GameManager`: speed and drift UI.
- `CoinPickup`: rotating/bobbing trigger pickup, resettable per route, with current-target highlight support.
- `LevelManager`: multiple road routes, active map switching, coin objective, completion reward, route unlock checks, and next-coin guidance.
- `SaveManager`: PlayerPrefs-backed total coins, unlocked cars, selected car, selected route, and highest unlocked route.
- `CarGarage`: car unlock, selection, and respawn at each route's spawn point.
- `MainMenuUI`: Home, Shop, Rank, Settings, Spin, Daily, and gameplay HUD flow.

## Key Parameters

`LevelManager`

- `levels`: route definitions with display name, root object, spawn point, and target coin count.
- `targetCoins`: per-route completion requirement. Current routes scale from 10 to 30.
- `guidanceText`: top-center HUD text that points toward the current target coin.
- `guidanceColor` / `guidanceCloseColor`: HUD color states for normal and nearby target coins.

`LowPolyDriftPrototypeBuilder`

- `LevelSpec.RoadWidth`: generated road width per route.
- `LevelSpec.RoutePoints`: route shape; edit these points to create new maps.
- `LevelSpec.TargetCoins`: number of coins generated along a route.
- `LevelSpec.ObstacleCount`: number of barriers/cones generated along a route.
- `MAT_RouteGuide`: cyan road arrows showing driving direction.
- `MAT_Checkpoint`: orange checkpoint marks under the coin path.

`CarController`

- `acceleration`: forward acceleration through `Rigidbody.AddForce`.
- `steeringAcceleration`: yaw torque through `Rigidbody.AddTorque`.
- `driftStartSpeed`: minimum speed before high-speed steering can enter drift.
- `normalLateralGrip`: side-velocity cancellation in normal driving.
- `driftLateralGrip`: lower side-velocity cancellation while drifting.

`CarGarage`

- `unlockCost`: total coins needed to unlock a car. Drift, Rally, and Speed cars cost 10, 30, and 60.
- `prefab`: vehicle prefab to spawn when the car is selected.
