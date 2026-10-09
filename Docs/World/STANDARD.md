# World standard — 4-player tag / parkour

Layout rules for dressing a Tag map. Numbers below are copied from the movement config, the Mega Park layout, or the asset catalog. This pass does not retune them.

Feel locks, left alone: coyote `0.10`, jump buffer `0.16`, cling grace `0.08`, `jumpSpeed` `24.7`, terminal `56.16`, walk `6.9`, crouch `3.68`, sprint `13.8`. There is no ledge hang and no shimmy. `Assets/Editor/SettingsInputProof.cs` fails the build if `PlayerMotor.cs` contains `LedgeHang` or `Shimmy`. `Assets/Scripts/Art/WallPose.cs` (`CableHang`) is a pose only: "Not a ledge grab and not a shimmy. Nothing here writes velocity or the root."

## Maps

| Map | Size | Where |
|---|---|---|
| Mega Park | 160 × 100 m, origin SW, +X east, +Z north, scale 1 | `MegaParkP1Layout.MapW` / `MapD` |
| Campus | 72 × 54 graybox meters, world = × `WorldScale` 10 | `CutArenaBootstrap` |
| Pocket Park | 80 × 50 m, draw budget 60 | `PocketParkLayout` |
| Stack Yard | 110 × 70 m, draw budget 70 | `StackYardLayout` |

Campus stays the separate scaled graybox. Do not densify it (`Docs/MEGA-PARK-P1-graybox-plan.md`). Pocket and Stack share the same feel locks and the same spawn-clear test.

## Sightlines and chase loops

Mega Park is one cooker. Kickball (Z7) and the crash bowl (Z8) are the open middle the four cameras share. `Docs/MEGA-PARK-P1-graybox-plan.md` ("Read rules"): one loop, two crossings, bowl center empty, no roof a dummy can hide inside.

- The CCW loop is `LoopLengthM = 472` m (`MegaParkP1Layout.LoopCcw`). At sprint 13.8 m/s that is 472 / 13.8 = 34.2 s.
- Crossing A is the open rect x[52, 72] × z[40, 58]. No props. The Z7 dressing stays east of x=72 or south of z=40.
- Crossing B is merry, x[22, 46] × z[44, 52]. Not dressed this pass.
- East spine x[130, 138] and the fort gap z[46, 54] inside x[118, 158] stay empty. Corridor minimum is 8 m.
- A chase loop is a ground polyline. Floors (road, sidewalk, court, anything under the 0.30 m step) do not block. Clearance is the play capsule radius 0.40 m plus 0.10 m. `PawnRadius` and `PawnStep` are in `MegaParkP1Layout`.
- A district chase is a shortcut around that district. It does not replace the 472 m loop and it does not move the spawn pads.

`LandmarkReport` in `MegaParkP1Layout.Pass6.cs` requires every zone box to have a landmark, and the bowl camera at (62, 50) must see each zone. Landmark crowns sit at `LandmarkCrown = 16` m, above the +5 decks, so a zone reads from open ground.

## No dead end without an escape verb

`Docs/MEGA-PARK-P1-graybox-plan.md` ("Two exits"): every toy is a lane with two ways out.

Reserved clears, still in force:

| Clear | Lock | Source |
|---|---|---|
| Soft-play aisles | ≥ 1.1 m | graybox plan, Z1 |
| Fort crawl mouths | ≥ 0.56 m | graybox plan, Z6 |
| Bar under-clear | ≥ 1.05 m | `BarUnderClear` |
| Fort corridor | ≥ 8 m | east spine and the army/knight gap |

A closed cage is a dead end. `CourtFence` is a closed gate (`Docs/AssetLibrary.md`: "The gate is closed"). Do not place it. A gazebo, a scaffold, or a facade must leave a walk-around, a vault, or a jump back to the ground.

## Verb envelopes

Rising gravity is `MovementConfig.gravity = 22`. Falling gravity is `gravity * fallGravityMult` with `fallGravityMult = 1.62` (`KinematicStep.AirGravity`: fall when `vy < 0`). `MegaParkP1Layout` names the same pair `RiseGravity = 22` and `FallGravity = 1.62`.

### Jump

`jumpSpeed = 24.7`. `PlayerMotor.TryJump` sets `v.y = JumpHeightNow()`. Apex from rest:

`24.7² / (2 × 22) = 13.866 m`. Time to apex `24.7 / 22 = 1.123 s`.

Fatigued floor is `jumpFatigueMin = 9.8`, apex `9.8² / (2 × 22) = 2.183 m`. A fresh jump clears a 3.2 m brick cornice. A fatigued jump does not. Route a required climb to a low deck, not to the cornice.

Terminal fall is `maxFallSpeed = 56.16`.

### Mantle and vault

`mantleMinLedgeHeight = 0.45`, `mantleMaxLedgeHeight = 2.55`, `mantleDuration = 0.40`, `mantleForward = 0.95`. The layout audit rejects a vault lip outside `MantleMin + 0.04` to `MantleMax − 0.04`.

The asset catalog's park vault band is 0.90–1.05 m (`Docs/AssetLibrary.md` line 3). A rail in that band is the one to build a route on. `Gangway` is `vault 0.88 m`, under the band. Do not place it as a vault.

`Gazebo` is `vault 0.95 m`. Deck is 0.32 m, rail center y = 1.27 (`Docs/AssetLibrary.md`).

### Climb

`climbSpeed = 6.0`, `climbMaxHeight = 3.4`, `climbMaxTime = 1.00`, `climbDecayStart = 0.12`, `climbSlipSpeed = 3.7`. The config comment: "Rises ~3 m then vertical speed reverses (slip) before climbMaxHeight." Practical climb used for routes is 3.0 m. A 3.2 m brick wall is a cling face, not a guaranteed cornice climb.

`Brick_Wall` and `Brick_Door` / `Brick_Window` piers are the cling surfaces (`climb` in the catalog). Exterior is +Z. A wall-run wants a face at least as long as the run.

### Wall run

`wallRunSpeed = 9.5`, `wallRunMaxTime = 0.62`, so the run is `9.5 × 0.62 = 5.89 m`. `wallRunGravity = 6.5`, end multiplier `5.4`. `clingReleaseGrace = 0.08` keeps a wall jump legal after the into-wall wish drops. It does not keep the climb alive.

### Wall jump

`DoWallRunJump` sets

`v = away * wallRunJumpOut + up * wallRunJumpUp + look * 3.5`

with `wallRunJumpOut = 8` and `wallRunJumpUp = 6.2`. The `3.5` is the look term in `PlayerMotor.cs`, not a config field.

Flat, same-height, look along the wall:

- horizontal speed `sqrt(8² + 3.5²) = 8.732 m/s`
- time up `6.2 / 22 = 0.282 s`, height `6.2² / (2 × 22) = 0.874 m`
- time down `sqrt(2 × 0.874 / (22 × 1.62)) = 0.221 s`
- range `8.732 × (0.282 + 0.221) = 4.394 m`

`Docs/MegaPark_SkillRoutes.md` uses 4.40 m for its wall-jump segments. A new gap has to land inside 4.39 m. A look that is not along the wall is shorter.

### Slide

`slideEntrySpeed = 7.5`. Crouch capsule is `crouchHeight = 1.05`, and `BarUnderClear = 1.05`. A deck the route slides under must clear 1.05 m. The play step is 0.30 m: a lip under that can be walked.

### Air dash

`airDashSpeed = 15`, `airDashDuration = 0.10`, so the burst is `15 × 0.10 = 1.50 m` planar. Cooldown is 30 s. The dash does not hover. `MaxAirSpeed = 16` is the fastest planar replace; the dash sits inside it. A gap between two landings has to be ≤ 1.50 m.

### Grapple

`ExperimentalGrapple.maxRange = 28`. `MegaParkP1Layout.GrappleRange = 28`. The route filter also rejects a target closer than 1.5 m (`MegaParkP1Layout.Route.cs`). Pull is a second click on a static surface. The grapple does not write `jumpSpeed` or velocity.

## Landmark readability in a quarter screen

Four chase cameras. Each pane is a quarter of the couch view (`Docs/Performance.md` split line: `cameras=4`).

A landmark has to read at that size:

- Vertical mass at least the player (1.8 m) and preferably a full story. Brick bays are 3.2 m (`Brick_Wall` 4.1 × 3.2 × 0.38). Hoop rims are 3.05 m, measured board top about 3.97 m. Light posts are about 5.5 m.
- A unique silhouette per district. Z7's read is the blue court plus two hoops, with the brick row as the south wall. Do not repeat that court in the next district.
- Color stays the verb rule from the graybox plan. Blue is cling, yellow is a real slide. This dressing uses library materials and does not recolor graybox cling or slide.
- The bowl at (62, 50) must still see the zone. Do not drop a roof over Z7 or a solid into the open rect.

## Spawn fairness

Four pads on 118 m arcs of the 472 m loop (`MegaParkP1Layout` spawn comment):

| Pad | (x, z) | Yaw |
|---|---|---|
| Spawn_SW | (8, 8) | 90 |
| Spawn_SE | (118, 16) | 180 |
| Spawn_NW | (42, 92) | −90 |
| Spawn_NE | (152, 84) | 0 |

`SpawnClearMeters = 20`. `SpawnSightSeconds = 2`. A pad fails if another It is inside 20 m or inside a 2 s sprint of line of sight (`ParkArena`). Overflow pads `Spawn_RunS` (59, 20.5) and `Spawn_RunN` (101, 92) are not It starts. Dressing a district does not move these pads. The older graybox-plan spawn table (SE at 152, 8) is not the live table.

## Collider rules

- Pivot is the ground contact (`Docs/AssetLibrary.md`, `LibraryPropMeta`). Seat the pivot on y = 0, or on another prop's top within about 0.08 m with an XZ overlap. Do not judge a float from the lowest collider: several shipped prefabs keep the mesh pivot on the ground and leave a gap inside the collider. Those gaps are asset bugs, listed in `LEDGER.md`, not a reason to rescale the prop.
- Colliders hug the mesh. No invisible walls. Paint, wires, and road decals have no collider on purpose (`LaneArrow`, `StopBar`, utility-pole wires).
- Hiding a graybox stand-in turns the renderer and the collider off together. `NoteHiddenColliders` logs a hidden collider when the collider is on and the renderer is not.
- Do not add a box that is larger than the mesh to "fix" a route. If the prefab is wrong, leave it and write it up.
- Placement scale stays (1, 1, 1). A scaled prefab is a scale fail.
- A solid whose bottom is under 1.05 m and whose top is over 0.05 m stays at least 0.90 m off the 472 m loop.

## Performance budget for 4-way split

`Docs/Performance.md`: 60 Hz, frame weight cap 240. The four-human split line is

`frame-budget-split ... cameras=4 map=mega-park median=97 worst=97 ratio=1.00 ... budget=240 headroom=143 steady=ok`

The hot-path line stays `hot-path allocs before=101 after=0 flags=dropped`. Do not edit the scan or the movement code to chase that line.

Graybox static draws after batching are capped at `MegaParkP1Layout.Pass7.DrawCap = 120`. Pocket is 60, Stack is 70. That cap is the graybox audit. Library props placed by the district are marked static and are not passed through `StaticBatchingUtility.Combine`, so their LOD groups stay. Four cameras can each draw those unique meshes. The next district should not add another unbatched set on top of Z7.

## Scale versus the Hier player

`MovementConfig.standingHeight = 1.8`. `PawnHeight = 1.8`. `Docs/AssetLibrary.md`: "Players are about 1.8 m." The showcase mannequin is 1.80 m and is not a gameplay character. Harbor and street stills use the Hier mannequin at that height.

Use the catalog sizes as the scale check:

| Piece | Catalog size | Against 1.8 m |
|---|---|---|
| Brick bay | 4.0 × 3.2 × ~0.4 | One story, about 1.8× the player. Cornice at 3.2 m. |
| Court | 12 × 0.12 × 22 | A street full court. Hoop pole 1.2 m behind the end line. |
| Road module | 6 m wide × 4 m long, asphalt top 0.12 m | Two lanes. Sidewalk top 0.27 m. |
| Bench | 1.8 × 0.88 × 1, seat 0.45 m | One player wide. Under the vault band. |
| Sedan | length 4.56 m (z −2.28 to 2.28), roof 1.56 m, tire radius 0.31 | `Tools/Blender/AssetLibrary/sk_car_sedan.py`. Roof under the player's head. |

Do not scale a prop to "fit" a gap. Move it, or pick a different module.

Dressed districts load from `Resources/World/WorldPropTable` (`WorldPropTable.Load`). That ScriptableObject holds the prefab references, so a player build includes them. `MegaParkP1Bootstrap` does not call `UnityEditor.AssetDatabase`. A missing table or a missing entry is `Debug.LogError`, and the placed count is an error when it is short of the list. `Assets/Tests/EditMode/WorldPropTableTests` resolves every placement through `Resources.Load` and fails if any prefab is null.

A dressed wall-jump stays at or under 4.0 m along the wall. The flat envelope is still 4.39 m. The Z7 chase keeps 0.80 m of clearance past the north hoop. The legal blocker clearance remains the 0.40 m radius plus 0.10 m.

## What a route check must print

`Tools/WorldCheck/check_z7.py` reads placements and prefab colliders (box, capsule, sphere) and prints one line:

`world-check routes=N reachable=N/N floatingProps=0 missingColliders=0 scaleFails=0`

Plus the apex, the flat wall-jump, the wall-run, the dash, the grapple range, and one gap/height line per route. A route that only fits by editing `jumpSpeed` or gravity is a layout bug.
