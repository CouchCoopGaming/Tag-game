# World ledger

Status of each map area after pass 10. Dressing is still the pass 8 set. Pass 9 notches the Z9 curb chase around the Z7 walk-up. Pass 10 reshoots eye and top stills from library LOD0 meshes in `Docs/WorldStills/pass10/`. Gray means the live solids are still the Mega Park cubes from `MegaParkP1Layout.BuildSolids`. Polished means library prefabs are placed on top. Z7 hides its infield lumps in play. Z1 through Z6, Z8, Z9, and Z10 leave their gray toys in place. This head includes the `cursor/tag-street-objects` merge (`7dc3e222`), the street-kit merges (`bc02b9c4`, `3040d2e5`, then `0aa3061e`), and the asset-library merges (`723cc137`, then `a066d987`). GateLeaf, the fixed bench and trash colliders, and the StrafeJumpSim proof line stayed. `PoseKeyDump` stayed deleted. Street-kit `0aa3061e` seats compact wheels at y = 0.010 and the pickup bumper at z = 2.493. Library `a066d987` is merged: MARKET, DINER, and WASH are two outward sheets, `Container_20` `Climb_Body` starts at y = 0, and `Dock_Straight` planks top at y = 0.620. Piles still end at y = −1.165, which that branch calls a water seat.

The headless audit still counts every solid. Hiding a lump in `MegaParkP1Bootstrap` does not change `BuildSolids`, so the Mega Park proof line stays the same.

## Mega Park — 160 × 100 m

Zone boxes are `MegaParkP1Layout.Pass6.ZoneBoxes`.

| Area | Bounds | Status |
|---|---|---|
| Z1 Soft-play | x[2, 38] z[2, 36] | Polished pass 2. Decks, tubes, cubes, step, and rim stay gray. 12 props, static-batched. Wall-jump rechecked pass 3. |
| Z2 Cling | x[2, 18] z[38, 78] | Polished pass 3. Gray cling faces stay. 13 props on the east lawn, static-batched. |
| Z3 Merry | x[22, 46] z[34, 60] | Polished pass 3. Crossing B, x[22, 46] × z[44, 52], stays empty. 13 props on the south lawn and the north strip, static-batched. |
| Z4 Slide mountain | x[22, 56] z[72, 98] | Polished pass 5. Gray towers, yellow chutes, rims, and the landmark stay. 13 props on the north lawn, static-batched. |
| Z5 Swings | x[58, 100] z[78, 98] | Polished pass 6. Gray swing frames, vault line, rims, and the landmark stay. 13 props on the north lawn, static-batched. |
| Z6 Twin forts | x[118, 158] z[10, 90] | Polished pass 7. Gray forts, yellow chutes, rims, and both landmarks stay. East spine x[130, 138] and gap z[46, 54] stay empty. 16 props, static-batched. |
| Z7 Kickball | x[64, 114] z[28, 68] | Polished. Pass 5 replaces the east door wall with the closed cabin and parks `Pickup_FullSize_25`. 46 instances, 26 unique, static-batched. |
| Z8 Crash bowl | x[46, 78] z[34, 66] | Polished pass 8. Open rect stays empty. Wash houses and a playground on the north lip, newsstands on the south apron. |
| Z9 Bars | x[38, 118] z[12, 20] | Polished pass 8. Gray bars stay. Crouch slot under the steel stays empty. Newsstands in the north pockets. |
| Z10 Hopscotch | x[118, 156] z[2, 22] | Polished pass 8. Gray hops stay. Alley and subway stair north of the hops, barricades on the south chalk. |
| Perimeter loop | 472 m CCW | Untouched. Spawns stay on the 118 m arcs. |
| Fence and collar | rim | Untouched. |

## Other maps

| Area | Size | Status |
|---|---|---|
| Campus | 72 × 54 graybox at `WorldScale` 10 | Not this pass. Do not densify. |
| Pocket Park | 80 × 50, draw budget 60 | Not this pass. |
| Stack Yard | 110 × 70, draw budget 70 | Not this pass. |

## Z7 this pass

Placements: `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs` (assembly `Tag.World`). Play spawn: `MegaParkP1Bootstrap.BuildWorldDistrict`. Check: `Tools/WorldCheck/check_z7.py`.

Hidden in play (renderer and collider off, still in the audit list): `Mound`, `Base_Home`, `Base_First`, `Base_Second`, `Base_Third`.

Kept gray, because they are verb toys: `Rail_East` (0.90 m vault), the kick dugout (`Kick_PostS`, `Kick_PostN`, `Kick_Bar`, `Kick_Lip`), `Cover_K1`, `Cover_K2`, `Cover_S2`.

Placed at scale 1, 46 instances, 26 unique prefabs:

- Court at (88.6, 0, 53.2). After the street-objects merge the slab collider measures x[81.10, 96.10] z[42.20, 64.20], 15 × 22 m. The pivot did not move. Hoops stay at south (88.6, 0, 41.0) yaw 0 and north (88.6, 0, 65.4) yaw 180. Their colliders now end 0.77 m outside the slab.
- `CourtFence` on the same pivot, yaw 180, so the gate faces the west chase. The merged prefab in this checkout has the child mesh `GateLeaf` and `Col_Gate`. Play disables `Col_Gate` and hides `GateLeaf`. The fabric gap is the entrance (entry clearance 1.05 m). This lane does not edit the prefab. The west face of the merged fence is x = 80.61.
- Seven `StreetRoad_TwoLane` tiles, yaw 90, along z = 34 from x = 84 to 108. Planted median at (106.2, 0, 35.55).
- Five sidewalk bays at z = 29.6. The west door bay and the window beside it are `WalkUp` at (85.50, 0, 24.63), front +Z, stoop ending at z = 28.55. The east door wall is now `Cabin` at (100.25, 0, 24.65), front +Z, step ending at z = 28.52, west face x = 98.08. The window bay at x = 96 and the wall at x = 92 stay. `RooftopAC` stays on the wall top at y = 3.2.
- Two `Brick_Wall` climb faces, yaw 90, at x = 71.75, z = 33 and 37. Union face is 8.00 m, height 3.19 m.
- Gazebo at (77.53, 0, 35), yaw 180, so the entry faces the climb. The merged gazebo collider tops at y = 3.53, so the roof AC pivot is y = 3.53. The same seat is used in Z2 and Z3. Outer west face (the step) is x = 75.16. Deck west face stays x = 76.33.
- Parked on the north lane, yaw 90: `Sedan_Compact_25` at x = 84.6, `Hatch_Compact_25` at x = 89.4, `Crossover_Compact_25` at x = 94.4, all at pivot y = 0.102, and `Pickup_FullSize_25` at x = 99.8, pivot y = 0.100. The road slab tops at y = 0.112. Compact wheels start at local y = 0.010 and pickup wheels at local y = 0.012, so every tire bottom lands on that slab (gap 0, inside 0.5 cm). The nose still reaches x = 102.29. Gaps to the hatch and the crossover stay about 1.03 m. Nothing overlaps the cabin, the sidewalk, or the next car. The restored `Car_Pickup_25` blockout is no longer placed.
- Lights, hydrant, two benches, two trash cans, two scaffold bays, two maples, two planters, a picnic table, a park lamp, a shrub.

### Routes

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

Envelopes the check used: jump apex 13.866 m, flat wall-jump 4.394 m, wall-run 5.89 m, air dash 1.50 m, grapple 28.0 m.

| Route | Kind | Measured |
|---|---|---|
| WestClimb | climb, wall-run, wall-jump | Wall 3.19 m. Wall-run 4.00 m on an 8.00 m face (max 5.89). Outer gap 3.23 m (was 3.90 m before the entry yaw). Deck gap 4.40 m. At 30° off the wall the capsule leaves with feet at 0.05 m and lands at (76.93, 34.93). At 60° it lands at (77.53, 34.83). Both sit inside the deck by the 0.40 m radius. |
| WestGrapple | grapple | 12.21 m from the south lane to the climb cornice (range 1.5–28). |
| GazeboVault | vault | Rail 0.95 m (mantle 0.45–2.55, vault band 0.90–1.05). East walk-around open. |
| SlideDash | slide, air dash | Under-clear 1.68 m on both bays (crouch 1.05). Gap 0.92 m (dash 1.50). |
| ChaseLoop | ground chase | 132.5 m. Worst clearance 0.84 m at (80.7, 40.9) against the merged `CourtFence`. The north hoop (z max 65.60) is still 0.90 m south of the north leg. Gate entry clearance 1.05 m. Samples 262, blocked 0. |

The chase is (82.5, 31.55) → (111.0, 31.55) → (111.0, 66.50) → (79.70, 66.50) → (79.70, 40.88) → (81.2, 40.88) → (81.2, 31.55) → close. South of the median, west of the east rail, north of the north hoop. Beside the fence the leg steps out to x = 79.70, because a straight line at x = 81.2 now runs through the merged west face. South of the fence it returns to x = 81.2, east of the gazebo. Kept gray toys (rail, kick dugout, cover vaults) are blockers in the check. A separate probe from (81.2, 53.2) to midcourt passes the opened gate.

No placement hits the open rect. None sits on the 472 m loop.

### Honest gaps

- The arc that clears the gazebo eave starts with the feet at 0.05 m. A higher hop meets the roof. The check does not add air accel.
- A look turned back into the wall is shorter: −30° reaches 3.145 m and −60° reaches 2.501 m. The deck gap is 4.40 m, so those looks miss. The pass 3 requirement is the off-wall pair, and that pair lands.
- The north-hoop clearance is 0.90 m. The legal minimum remains 0.50 m (radius 0.40 + 0.10).
- The planted median's collider tops at 0.66 m, over the 0.30 m step and under the 0.90 m vault band. It is a trip. The chase goes around it.
- AC units overhang the brick thickness. The pivot sits on the wall or the roof, so the support test passes.
- District props load from `Resources/World/WorldPropTable`. `WorldPropTable` and the district list live in assembly `Tag.World`. The EditMode asmdef references that assembly. A test asmdef cannot reference `Assembly-CSharp`. A missing table or a missing entry is `Debug.LogError`, and the count line is an error when placed is short of the list. Z7 is 46 instances and 26 unique prefabs. Play disables LOD1 and LOD2 on that group and calls `StaticBatchingUtility.Combine`, the same path as Z1. `DrawCap` 120 is the graybox batch only. Four cameras still submit the combined batch.
- KICKBALL and the other zone labels are two TextMesh faces, yaw 0 and yaw 180, scale (1, 1, 1), on `Tag/SignText` (Cull Back). `ZoneNameMarkers` no longer billboards with `LookRotation(-toCam)`. Brick bays, `WalkUp`, and `Cabin` have no letter mesh. Shop words (MARKET, DINER, WASH) are still inside the store LOD meshes, so one side reads mirrored. That rebuild is logged for buildings #122 in `Docs/Models/ENV_QUEUE.md`. The world lane does not edit those FBX files.
- Play hides `GateLeaf` in the same pass that turns `Col_Gate` off. The merged CourtFence in this checkout has that child, so the missing-leaf error does not fire on these bytes. A fence prefab without the child would still log an error.
- Stills in `Docs/WorldStills/pass4/` are the pass 4 collider rasters. Pass 1, 2, and 3 images were left in place. Canopies, glass, and brick courses are not in the image. Unity and Blender are not in this environment. `Docs/WorldStills/pass10/z7_top.png` and `z7_eye.png` show the four parked cars from their LOD0 meshes.

## Z1 this pass

Placements: `SoftPlay` in `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs`. 12 instances, 10 unique prefabs. Z7 is 46 instances and 26 unique, so this set is smaller. Play disables the extra LOD renderers on the Z1 group and calls `StaticBatchingUtility.Combine`.

Nothing in Z1 is hidden. `SoftPlay_DeckLow` is still `(14, 1, 26)` size `10 × 2 × 8`. `SoftPlay_DeckHigh` is still `(14, 2.75, 26)` size `6 × 1.5 × 5`. The west step, the lip, the tubes, the three cubes, `Rim_SoftN`, and the landmark pole at (20, 10) stay. No prop overlaps those volumes. No prop sits on the 472 m loop, in the open rect, or in the bowl.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| WestClimb | Climb onto DeckLow (wall 3.19 m, deck top 2.00 m). Wall-run 4.00 m on a 4.00 m face. Gap to the deck face 3.67 m. At 30° off the wall the capsule launches from 1.30 m and lands at (9.45, 22.41). At 60° it launches from 1.20 m and lands at (9.57, 22.61). Both clear the 0.40 m radius inset. The step between the two walls is the ground exit. |
| WestGrapple | 23.26 m from the east lawn to the climb cornice. |
| GazeboVault | Rail 0.95 m. East walk-around open. |
| SlideDash | Under-clear 1.68 m. Air-dash gap 0.92 m. |
| ChaseLoop | 72.4 m around the decks. Worst clearance 0.92 m against the shrub. |

Stills: `Docs/WorldStills/pass2/`. Collider rasters, same as pass 1. Pass 4 did not move these props. The brick climb face measures 3.19 m after the street-kit wall, and the gap is 3.67 m. A look 30° or 60° back into the wall (ranges 3.145 m and 2.501 m) does not cross that gap.

## Z2 this pass

Placements: `Cling` in `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs`. 13 instances, 11 unique prefabs. Z7 is 46 instances and 26 unique, so this set is smaller. Play static-batches the group the same way as Z7 and Z1.

The eight gray cling walls stay on lanes x = 2.55 and x = 6.15. Nothing overlaps them, the landmark pole at (15.2, 77.4), the rims at x = 20, Crossing B, the bowl, the open rect, or the x = 8 loop. The brick climb is the east lawn, west face x = 8.95, which is 0.95 m off the loop.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| EastClimb | Wall 3.19 m. Wall-run 4.00 m on an 8.00 m face. Deck gap 4.10 m. At 30° and 60° off the wall the capsule leaves with feet at 0.05 m and lands on the gazebo deck. At 30° the land is (14.32, 52.18). At 60° it is (14.92, 51.83). The entry faces the climb (yaw 180). |
| EastGrapple | 11.06 m from the north lawn to the climb cornice. |
| GazeboVault | Rail 0.95 m. East walk-around open. |
| SlideDash | Under-clear 1.68 m. Air-dash gap 0.92 m. |
| ChaseLoop | 49.0 m around the east lawn. Worst clearance 1.00 m against `Rim_W1`. |

The low launch is the one that clears the gazebo eave. A higher hop meets the roof, same limit as Z7. Into-wall looks at −30° and −60° miss the 4.10 m deck gap.

Stills: `Docs/WorldStills/pass3/` (`before_*.png`, `after_*.png`, `split4.png`). Collider rasters. Regenerated after the merge so the scaffold, bench, picnic, and shrub boxes match the fixed prefabs.

## Z3 this pass

Placements: `Merry` in `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs`. 13 instances, 11 unique prefabs. Z7 is 46 instances and 26 unique, so this set is smaller. Play static-batches the group the same way as Z7.

Crossing B, x[22, 46] × z[44, 52], has no prop. The two strips outside it do hold five routes, so Z4 stays gray. Nothing overlaps the podium, the four posts, the two tables, `Merry_A`, `Merry_B`, the hook, or the landmark pole at (40, 36.6). Nothing sits in the bowl, the open rect, or on the 472 m loop.

The south lawn is the climb: two `Brick_Wall` faces, yaw 90, at x = 23.55, z = 37.05 and 41.05. Gazebo at (29.03, 0, 39.05), yaw 180, entry toward the climb. AC on the roof at y = 3.53. The north strip, z = 52.70, holds two scaffold bays 2.75 m apart. Bench, trash, maple, planter, and shrub dress the southeast lawn. Picnic and light sit east of the north tables.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| WestClimb | Wall 3.19 m. Wall-run 4.00 m on an 8.00 m face. Deck gap 4.10 m. At 30° off the wall the capsule leaves with feet at 0.05 m and lands at (28.73, 39.23). At 60° it lands at (29.33, 38.88). Both sit inside the deck by the 0.40 m radius. |
| NorthGrapple | 22.19 m from the north strip to the climb cornice. The rope crosses the empty band. No prop is placed in it. |
| GazeboVault | Rail 0.95 m. East walk-around open. The podium is 0.30 m, under a step, so it is not a cage. |
| SlideDash | Under-clear 1.68 m. Air-dash gap 0.92 m. The bays start at z = 52.08, north of Crossing B. |
| ChaseLoop | 86.0 m around both strips. Worst clearance 0.95 m against `My_ClimbA`. |

The low launch is the one that clears the gazebo eave. A higher hop meets the roof, same limit as Z7. Into-wall looks at −30° and −60° miss the 4.10 m deck gap.

Stills: `Docs/WorldStills/pass3/z3_*.png`. Collider rasters. The Z2 set in the same folder keeps its own names.

## Asset bugs for the models lane

The world lane does not edit the vehicle, walk-up, or store meshes. This checkout merged street-kit `0aa3061e` and asset-library `a066d987`. `Tools/WorldCheck/check_z7.py` was re-run on the parked cars, and `check_z6.py` was re-run on the seated container and the 0.62 m dock.

Closed, remeasured here:

| Prefab | Was | Now |
|---|---|---|
| `Scaffold_Bay` | Posts from y = 0.20. Deck 0.18 m above the posts. | Posts from y = 0.015 to y = 1.655. Deck starts at y = 1.680 (gap 0.025 m). The scanner no longer prints this prefab. |
| `TrashCan_Lidded` | Lowest y = 0.19. | Base at y = 0.013. |
| `Bench_Wood` | Lowest y = 0.14. Gap 0.18 m under y = 0.65. | Legs at y = 0.020. No internal gap over 0.15 m. |
| `PicnicTable` | Lowest y = 0.37. Gap 0.28 m under y = 0.74. | Feet at y = 0.025. |
| `Shrub` | Sphere bottom y = 0.16. | Sphere bottom y = 0.020. |
| Street-kit pickup tailgate | `Col_Tailgate` started at y = 0.73, 0.17 m above the bed. | `Pickup_FullSize_25` starts the tailgate at y = 0.585. The bed tops at y = 0.560, so the gap is 0.025 m. The blockout file is gone. |
| `Sedan_Compact_25`, `Hatch_Compact_25`, `Crossover_Compact_25` | Wheel boxes started at y = 0.167, 0.168, and 0.192. | `0aa3061e` seats `Col_Wheel_*` at y = 0.010–0.046. Not edited here. |
| `Pickup_FullSize_25` nose | Boxes stopped at local z = 1.625. | `Col_Nose` reaches z = 2.420 and `Col_Bumper` reaches z = 2.493. Wheels stay at y = 0.012. Not edited here. |
| `Store_Corner`, `Store_Diner`, `Store_Laundromat` | MARKET, DINER, and WASH mirrored from the back. | `a066d987` rebuilds each word as two outward sheets at positive scale. Not edited here. Not placed in Mega Park. |
| `Container_20`, `Container_20_Blue` | `Climb_Body` started at y = 0.22. | `a066d987` seats the body at y = 0.000–2.320. Roof stays y = 2.48–2.54. Not edited here. |

`CourtFence` in this checkout draws the gate as the child mesh `GateLeaf`. Play hides that mesh and turns `Col_Gate` off. That leaf is not an open models bug.

Still open. Relayed to the models lane. Not edited here:

| Prefab | Exact gap |
|---|---|
| `LightPost_Single` | 0.36 m under the pole collider at y = 0.60. The base reaches y = 0.08. |
| `ParkLamp` | 0.24 m under the pole collider at y = 0.49. The base reaches y = 0.07. |
| `WalkUp` | Climb faces start at y = 0.40, 0.25 m above the stoop. The body is enclosed. Not edited here. |
| `Cabin` | Placed. Door is closed. An internal gap of 0.20 m sits under a collider at y = 2.38. Not edited here. |
| `Dock_Straight` piles | Placed in Z6. Deck top is now 0.620. Piles still run to y = −1.165. Library `a066d987` calls that a water seat on purpose. Not edited here. |
| `FireEscape` | Placed in Z8. Lowest collider is y = 0.15, so the ladder foot sits 15 cm off the ground. Not edited here. |
| `Subway_Entrance` steps | Placed in Z10, yaw 90. The stair boxes run down to y = −1.51. That is a below-grade stair. On park ground the steps clip the floor. Not edited here. |

`Gangway` is dropped from Mega Park. The prefab field is `vaultHeightMeters: 0.88`, and the rail boxes top at y = 1.758 over a plate whose top is y = 0.892, so the rail is 0.87 m above the plate. The catalog band is 0.90–1.05 m. The plate itself starts at y = 0.728, which is a harbor ramp seat, not a park ground contact. Raising the rail would edit the harbor mesh. It stays in the harbor library and is not a Mega Park route.

The scanner's first internal gap on the compact shells is now the underbody: 0.27 m under `Col_Cabin` at y = 0.32, over wheels at y = 0.010. The pickup's first gap is 0.29 m under a collider at y = 0.34, over wheels at y = 0.012. Same class of opening: hoop rim above the pole (2.95 m under y = 2.99), brick window glass (0.75 m under y = 0.93), gazebo rail above the deck (0.97 m under y = 1.26).

## Z4 this pass

Placements: `Slide` in `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs`. 13 instances, 11 unique prefabs. Z7 is 46 instances and 26 unique, so this set is smaller. Play static-batches the group the same way as Z7.

The gray towers, yellow chutes, rims, spiral, crawl, and `Landmark_Z4` stay. Nothing overlaps them, the bowl, the open rect, Crossing B, or the z = 92 loop. Two brick climb faces, yaw 90, sit at x = 24, z = 86 and 88.5. The union face is 6.50 m, height 3.19 m. Gazebo at (29.48, 0, 87.52), yaw 180, entry toward the climb. AC on the roof at y = 3.53. The scaffold dash is east of the gazebo.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| WestClimb | Wall 3.19 m. Wall-run 4.00 m on a 6.50 m face. Deck gap 4.10 m. At 30° off the wall the capsule leaves with feet at 0.05 m and lands at (29.18, 87.68). At 60° it lands at (29.78, 87.58). |
| EastGrapple | 25.90 m from the east lawn to the climb cornice. |
| GazeboVault | Rail 0.95 m. East walk-around open. |
| SlideDash | Under-clear 1.68 m. Air-dash gap 0.92 m. |
| ChaseLoop | 74.2 m around the north lawn. Worst clearance 0.85 m against `Rim_SlideIn`. |

Stills: `Docs/WorldStills/pass5/`. Z4 rasters are `z4_*.png`. Z7 rasters in that folder are `before_*.png`, `after_*.png`, and `split4.png`. Collider rasters. Pass 1 through 4 images were left in place.

## Z5 this pass

Placements: `Swing` in `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs`. 13 instances, 11 unique prefabs. Z7 is 46 instances and 26 unique, so this set is smaller. Play static-batches the group the same way as Z7.

The gray swing posts, beams, rails, the five vault lips, the north rims, and `Landmark_Z5` stay. Nothing overlaps them, the bowl, the open rect, Crossing B, or the z = 92 loop. Two brick climb faces, yaw 90, sit at x = 62, z = 86.15 and 88.5. The union face is 6.35 m, height 3.19 m. Gazebo at (67.48, 0, 87.32), yaw 180, entry toward the climb. AC on the roof at y = 3.53. The scaffold dash is east of the gazebo.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| WestClimb | Wall 3.19 m. Wall-run 4.00 m on a 6.35 m face. Deck gap 4.10 m. At 30° off the wall the capsule leaves with feet at 0.05 m and lands at (67.18, 87.33). At 60° it lands at (67.78, 87.23). |
| EastGrapple | 27.90 m from the east lawn to the climb cornice. |
| GazeboVault | Rail 0.95 m. East walk-around open. |
| SlideDash | Under-clear 1.68 m. Air-dash gap 0.92 m. |
| ChaseLoop | 79.4 m around the north lawn. Worst clearance 0.85 m against `Sw_ClimbB`. |

Stills: `Docs/WorldStills/pass6/z5_*.png`. Collider rasters. Pass 1 through 5 images were left in place.

## Z6 this pass

Placements: `Forts` in `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs`. 16 instances, 15 unique prefabs. Z7 is 46 instances and 26 unique, so this set is smaller. Play static-batches the group the same way as Z7.

The gray crawls, spirals, decks, rims, hooks, yellow chutes, and both `Landmark_Z6` masts stay. Nothing new sits in the east spine x[130, 138] or the gap z[46, 54]. Hopscotch, `Bar_EndDeck`, the bowl, the open rect, and Crossing B stay clear. This is not the Z4/Z5 lawn: no brick wall, gazebo, rooftop AC, scaffold, maple, planter, bench, trash can, shrub, picnic table, or single light post. The climb faces west, onto a dock, with a 3.60 m deck gap.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| YardClimb | West jump off `Container_20`. After `a066d987` the wall is 2.54 m on a 5.54 m face. Deck gap 3.60 m onto `Dock_Straight` planks (top 0.62 m). At 30° off the wall the capsule leaves with feet at 0.05 m and lands at (121.22, 17.48). At 60° it lands at (120.69, 16.95). |
| GapGrapple | 12.38 m from (128.5, 1.6, 43.0), south of the empty gap, to the blue container's south cornice. The rope crosses z[46, 54]. No prop sits in that band. |
| RailVault | `HarborRail` at 1.05 m, the top of the vault band. West walk-up is open. |
| ShelterDash | Two `BusShelter` roofs, underside 2.44 m. The dash gap between them is 0.90 m. |
| FortChase | 167.5 m around the forts. South leg under the hopscotch, east leg through the empty gap, north leg past the shelters, west leg clear of the rims. Worst clearance 0.83 m against `Ft_Anchor`. |

Stills: `Docs/WorldStills/pass7/z6_*.png`. Collider rasters. Pass 1 through 6 images were left in place.

## Z8 this pass

Placements: `Bowl` in `Assets/Scripts/Level/World/MegaParkWorldDistrict.cs`. 10 instances, 8 unique prefabs. The open rect x[52, 72] z[40, 58] stays empty. Sand toys, the landmark mast, and the rim blocks stay. Nothing new sits in the bowl.

Two `Restroom` wash houses on the north lip. The east wall of the west house is 2.34 m on a 3.24 m face. The jump lands on the east house roof. The deck gap is 2.62 m. `Playground` sits north of them. The rope from the east roof to the swing beam is 9.62 m. `FireEscape` on the south apron is the 1.05 m vault. Two newsstands there are the dash, gap 0.89 m.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| WashClimb | East jump. Wall 2.34 m. Deck gap 2.62 m. +30° lands at (68.94, 67.86). +60° lands at (69.73, 67.78). |
| BeamGrapple | 9.62 m from the east wash roof to the playground beam. |
| EscapeVault | Fire-escape rail 1.05 m. West walk-up open. |
| StandDash | Newsstand undersides 1.10 m. Gap 0.89 m. |
| LipChase | 115.0 m around the lips. Worst clearance 0.90 m against `Rim_DropN`. |

Stills: `Docs/WorldStills/pass10/z8_top.png` and `z8_eye.png` show the wash houses and playground. Pass 8 collider rasters stay in `pass8/`.

## Z9 this pass

Placements: `Bars`. 10 instances, 7 unique prefabs. Gray posts, bars, and vault lips stay. The crouch volume under the steel (y up to 1.05, z 15.4–16.6) stays empty. The arch at x = 66.8 stays clear.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| BarCrouch | Bar underside 1.14 m. Slot empty. |
| LipMantle | North vault lip 0.96 m. West approach open. |
| CurbGrapple | 15.00 m from the west newsstand to the third stand. |
| StandDash | Newsstand undersides 1.10 m. Gap 0.89 m. |
| CurbChase | 155.0 m on the north curb. The return notches around the Z7 walk-up, whose south face is z = 21.17. Worst clearance 0.94 m against `BarPost_N0`. |

Stills: `Docs/WorldStills/pass10/z9_top.png` and `z9_eye.png` show the three newsstands. Pass 8 collider rasters stay in `pass8/`.

## Z10 this pass

Placements: `Hops`. 8 instances, 7 unique prefabs. Gray hops stay uncovered. `Subway_Entrance` and `Alley` sit north of the hops, yaw 90, so the long axis runs east. Barricades sit on the south chalk, clear of the z = 8 loop.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| StairClimb | West jump off the alley back wall. Wall 3.28 m. Deck gap 5.00 m onto the subway roof. +30° lands at (135.45, 19.42). +60° lands at (135.41, 18.66). |
| HopVault | `Hop_5` lip 0.94 m. South approach open. |
| HopGrapple | 9.59 m from the south barricade over the hops to the alley header. |
| ChalkDash | Barricade rail underside 1.33 m. Gap 0.90 m. |
| HopChase | 60.0 m around the hops. Worst clearance 0.86 m against `Hp_Subway`. |

Stills: `Docs/WorldStills/pass10/z10_top.png` and `z10_eye.png` show the subway entrance and the alley. Pass 8 collider rasters stay in `pass8/`.

## Next district

Z1 through Z10 are dressed. The open rect, the bar crouch slot, the hops, the fort spine, the fort gap, Crossing B, and the kickball sightline stay empty. Do not recolor the yellow chutes. Keep later sets batched, and keep loading them from `WorldPropTable`.
