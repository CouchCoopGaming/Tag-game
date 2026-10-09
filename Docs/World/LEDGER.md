# World ledger

Status of each map area after pass 3. Gray means the live solids are still the Mega Park cubes from `MegaParkP1Layout.BuildSolids`. Polished means library prefabs are placed on top. Z7 hides its infield lumps in play. Z1 and Z2 leave their gray toys in place.

The headless audit still counts every solid. Hiding a lump in `MegaParkP1Bootstrap` does not change `BuildSolids`, so the Mega Park proof line stays the same.

## Mega Park — 160 × 100 m

Zone boxes are `MegaParkP1Layout.Pass6.ZoneBoxes`.

| Area | Bounds | Status |
|---|---|---|
| Z1 Soft-play | x[2, 38] z[2, 36] | Polished pass 2. Decks, tubes, cubes, step, and rim stay gray. 12 props, static-batched. Wall-jump rechecked pass 3. |
| Z2 Cling | x[2, 18] z[38, 78] | Polished pass 3. Gray cling faces stay. 13 props on the east lawn, static-batched. |
| Z3 Merry | x[22, 46] z[34, 60] | Gray. Crossing B lives here. Leave it open. |
| Z4 Slide mountain | x[22, 56] z[72, 98] | Gray. Decks stay on the rim when they are built. |
| Z5 Swings | x[58, 100] z[78, 98] | Gray. |
| Z6 Twin forts | x[118, 158] z[10, 90] | Gray. East spine x[130, 138] and gap z[46, 54] stay empty. |
| Z7 Kickball | x[64, 114] z[28, 68] | Polished pass 1. Pass 2 opened the court gate. Pass 3 static-batches the 24 prefabs and turns the gazebo entry toward the climb. |
| Z8 Crash bowl | x[46, 78] z[34, 66] | Gray. Open rect x[52, 72] z[40, 58] has no new props. |
| Z9 Bars | x[38, 118] z[12, 20] | Gray. South spine. Bar under-clear 1.05 m stays empty. |
| Z10 Hopscotch | x[118, 156] z[2, 22] | Gray. |
| Perimeter loop | 472 m CCW | Untouched. Spawns stay on the 118 m arcs. |
| Fence and collar | rim | Untouched. |

## Other maps

| Area | Size | Status |
|---|---|---|
| Campus | 72 × 54 graybox at `WorldScale` 10 | Not this pass. Do not densify. |
| Pocket Park | 80 × 50, draw budget 60 | Not this pass. |
| Stack Yard | 110 × 70, draw budget 70 | Not this pass. |

## Z7 this pass

Placements: `Assets/Scripts/Level/MegaParkWorldDistrict.cs`. Play spawn: `MegaParkP1Bootstrap.BuildWorldDistrict`. Check: `Tools/WorldCheck/check_z7.py`.

Hidden in play (renderer and collider off, still in the audit list): `Mound`, `Base_Home`, `Base_First`, `Base_Second`, `Base_Third`.

Kept gray, because they are verb toys: `Rail_East` (0.90 m vault), the kick dugout (`Kick_PostS`, `Kick_PostN`, `Kick_Bar`, `Kick_Lip`), `Cover_K1`, `Cover_K2`, `Cover_S2`.

Placed at scale 1, 46 instances, 24 unique prefabs:

- Court at (88.6, 0, 53.2), 12 × 22 m. Hoops 1.2 m behind the end lines: south (88.6, 0, 41.0) yaw 0, north (88.6, 0, 65.4) yaw 180.
- `CourtFence` on the same pivot, yaw 180, so the gate faces the west chase. Play disables `Col_Gate`. The fabric gap is the entrance (entry clearance 0.90 m). The mesh still draws the closed leaf; that draw is in the models prefab and was not edited.
- Seven `StreetRoad_TwoLane` tiles, yaw 90, along z = 34 from x = 84 to 108. Planted median at (106.2, 0, 35.55).
- Five sidewalk bays at z = 29.6. Brick row on the south edge at z = 28.15: door, window, wall, window, door. `RooftopAC` on the wall top at y = 3.2.
- Two `Brick_Wall` climb faces, yaw 90, at x = 71.75, z = 33 and 37. Union face is 8.00 m, height 3.20 m.
- Gazebo at (77.53, 0, 35), yaw 180, so the entry faces the climb. AC on the roof at y = 3.05, the measured roof top. Outer west face (the step) is x = 75.16. Deck west face stays x = 76.33.
- Parked `Car_Sedan`, `Car_Hatch`, `Car_Pickup` on the north lane, yaw 90, pivot y = 0.12 (road crown is about 0.11).
- Lights, hydrant, two benches, two trash cans, two scaffold bays, two maples, two planters, a picnic table, a park lamp, a shrub.

### Routes

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

Envelopes the check used: jump apex 13.866 m, flat wall-jump 4.394 m, wall-run 5.89 m, air dash 1.50 m, grapple 28.0 m.

| Route | Kind | Measured |
|---|---|---|
| WestClimb | climb, wall-run, wall-jump | Wall 3.20 m. Wall-run 4.00 m on an 8.00 m face (max 5.89). Outer gap 3.22 m (was 3.90 m before the entry yaw). Deck gap 4.39 m. At 30° off the wall the capsule leaves with feet at 0.05 m and lands at (76.94, 34.93). At 60° it lands at (77.54, 34.83). Both sit inside the deck by the 0.40 m radius. |
| WestGrapple | grapple | 12.21 m from the south lane to the climb cornice (range 1.5–28). |
| GazeboVault | vault | Rail 0.95 m (mantle 0.45–2.55, vault band 0.90–1.05). East walk-around open. |
| SlideDash | slide, air dash | Under-clear 1.68 m on both bays (crouch 1.05). Gap 0.92 m (dash 1.50). |
| ChaseLoop | ground chase | 129.5 m. Worst clearance 0.90 m at (91.3, 66.5) against `Planter_N`. The north hoop (z max 65.60) is 0.90 m south of that leg. Gate entry clearance 0.90 m. Samples 256, blocked 0. |

The chase is the rectangle (82.5, 31.55) → (111.0, 31.55) → (111.0, 66.50) → (81.2, 66.50) → close. South of the median, west of the east rail, north of the north hoop, east of the gazebo. Kept gray toys (rail, kick dugout, cover vaults) are blockers in the check. A line from (81.2, 53.2) on that west leg to midcourt passes the opened gate.

No placement hits the open rect. None sits on the 472 m loop.

### Honest gaps

- The arc that clears the gazebo eave starts with the feet at 0.05 m. A higher hop meets the roof. The check does not add air accel.
- A look turned back into the wall is shorter: −30° reaches 3.145 m and −60° reaches 2.501 m. The deck gap is 4.39 m, so those looks miss. The pass 3 requirement is the off-wall pair, and that pair lands.
- The north-hoop clearance is 0.90 m. The legal minimum remains 0.50 m (radius 0.40 + 0.10).
- The planted median's collider tops at 0.66 m, over the 0.30 m step and under the 0.90 m vault band. It is a trip. The chase goes around it.
- AC units overhang the brick thickness. The pivot sits on the wall or the roof, so the support test passes.
- District props load from `Resources/World/WorldPropTable`. A missing table or a missing entry is `Debug.LogError`, and the count line is an error when placed is short of the list. Z7 is 46 instances and 24 unique prefabs. Play disables LOD1 and LOD2 on that group and calls `StaticBatchingUtility.Combine`, the same path as Z1. `DrawCap` 120 is the graybox batch only. Four cameras still submit the combined batch.
- The fence mesh still draws a closed leaf. Only `Col_Gate` is turned off.
- Stills in `Docs/WorldStills/pass1/` are collider rasterizations. Canopies, glass, and brick courses are not in the image. Unity and Blender are not in this environment.

## Z1 this pass

Placements: `SoftPlay` in `Assets/Scripts/Level/MegaParkWorldDistrict.cs`. 12 instances, 10 unique prefabs. Z7 is 46 instances and 24 unique, so this set is smaller. Play disables the extra LOD renderers on the Z1 group and calls `StaticBatchingUtility.Combine`.

Nothing in Z1 is hidden. `SoftPlay_DeckLow` is still `(14, 1, 26)` size `10 × 2 × 8`. `SoftPlay_DeckHigh` is still `(14, 2.75, 26)` size `6 × 1.5 × 5`. The west step, the lip, the tubes, the three cubes, `Rim_SoftN`, and the landmark pole at (20, 10) stay. No prop overlaps those volumes. No prop sits on the 472 m loop, in the open rect, or in the bowl.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| WestClimb | Climb onto DeckLow (wall 3.20 m, deck top 2.00 m). Wall-run 4.00 m on a 4.00 m face. Gap to the deck face 3.66 m, unchanged this pass. At 30° off the wall the capsule launches from 1.30 m and lands at x = 9.46, 0.46 m onto the deck. At 60° it launches from 1.20 m and lands at x = 9.58. Both clear the 0.40 m radius inset. The step between the two walls is the ground exit. |
| WestGrapple | 23.26 m from the east lawn to the climb cornice. |
| GazeboVault | Rail 0.95 m. East walk-around open. |
| SlideDash | Under-clear 1.68 m. Air-dash gap 0.92 m. |
| ChaseLoop | 72.4 m around the decks. Worst clearance 0.90 m against the shrub. |

Stills: `Docs/WorldStills/pass2/`. Collider rasters, same as pass 1. Pass 3 did not move these props. A look 30° or 60° back into the wall (ranges 3.145 m and 2.501 m) does not cross the 3.66 m gap.

## Z2 this pass

Placements: `Cling` in `Assets/Scripts/Level/MegaParkWorldDistrict.cs`. 13 instances, 11 unique prefabs. Z7 is 46 instances and 24 unique, so this set is smaller. Play static-batches the group the same way as Z7 and Z1.

The eight gray cling walls stay on lanes x = 2.55 and x = 6.15. Nothing overlaps them, the landmark pole at (15.2, 77.4), the rims at x = 20, Crossing B, the bowl, the open rect, or the x = 8 loop. The brick climb is the east lawn, west face x = 8.95, which is 0.95 m off the loop.

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

| Route | Measured |
|---|---|
| EastClimb | Wall 3.20 m. Wall-run 4.00 m on an 8.00 m face. Deck gap 4.09 m. At 30° and 60° off the wall the capsule leaves with feet at 0.05 m and lands on the gazebo deck. The entry faces the climb (yaw 180). |
| EastGrapple | 11.06 m from the north lawn to the climb cornice. |
| GazeboVault | Rail 0.95 m. East walk-around open. |
| SlideDash | Under-clear 1.68 m. Air-dash gap 0.92 m. |
| ChaseLoop | 49.0 m around the east lawn. Worst clearance 1.00 m against `Rim_W1`. |

The low launch is the one that clears the gazebo eave. A higher hop meets the roof, same limit as Z7. Into-wall looks at −30° and −60° miss the 4.09 m deck gap.

Stills: `Docs/WorldStills/pass3/`. Collider rasters.

## Asset bugs for the models lane

The models lead (`cursor/tag-models-lead`) and the street props worker should take these. The world lane does not edit the prefabs. Pivots are seated, so `floatingProps` stays 0. The collider does not hug the mesh. Gaps are prefab-local, placement y removed, from `Tools/WorldCheck/check_z7.py`.

| Prefab | Exact gap |
|---|---|
| `Scaffold_Bay` | Lowest collider at y = 0.20 m (posts miss the pivot by 0.20 m). Deck collider starts at y = 1.68 m, 0.18 m above the post tops. |
| `Car_Sedan` | Lowest collider at y = 0.15 m. Visual tire radius is 0.31 m and the axle is y = 0.31 (`Tools/Blender/AssetLibrary/sk_car_sedan.py`), so the tread meets the ground and the wheel box does not. A second gap of 0.80 m sits under the collider at y = 1.50. |
| `Car_Hatch` | Lowest collider at y = 0.15 m. Same wheel-box gap. A gap of 0.72 m sits under the collider at y = 1.44. |
| `Car_Pickup` | Lowest collider at y = 0.18 m. Same wheel-box gap. A gap of 0.17 m sits under the collider at y = 0.73. |
| `TrashCan_Lidded` | Lowest collider at y = 0.19 m. |
| `Bench_Wood` | Lowest collider at y = 0.14 m. A gap of 0.18 m sits under the collider at y = 0.65. |
| `PicnicTable` | Lowest collider at y = 0.37 m. A gap of 0.28 m sits under the collider at y = 0.74. Catalog top is 0.76 m. |
| `Shrub` | Sphere bottom at y = 0.16 m. |

Also filed, not in that list: `LightPost_Single` has a 0.36 m gap under a collider at y = 0.60 (the pole itself reaches the ground). `ParkLamp` has a 0.24 m gap under a collider at y = 0.49. `Gangway` is catalog `vault 0.88 m`, under the 0.90–1.05 band, and is not placed.

Intentional openings the same scan prints, and that are not bugs: hoop rim above the pole (2.95 m under y = 2.99), brick window glass (0.75 m under y = 0.93), gazebo rail above the deck (0.97 m under y = 1.26).

## Next district

The next row is Z3 merry. Crossing B, x[22, 46] × z[44, 52], stays empty, so a Z3 dress has to live on the merry toys outside that band. Z4 slide is the following gray district if that band cannot hold five routes. Do not fill the bowl, the kickball sightline, the soft-play decks, or the cling lanes. Keep the next set batched, and keep loading it from `WorldPropTable`.
