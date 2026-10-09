# World ledger

Status of each map area after pass 1. Gray means the live solids are still the Mega Park cubes from `MegaParkP1Layout.BuildSolids`. Polished means library prefabs are placed on top and the stand-in lumps are hidden in play.

The headless audit still counts every solid. Hiding a lump in `MegaParkP1Bootstrap` does not change `BuildSolids`, so the Mega Park proof line stays the same.

## Mega Park — 160 × 100 m

Zone boxes are `MegaParkP1Layout.Pass6.ZoneBoxes`.

| Area | Bounds | Status |
|---|---|---|
| Z1 Soft-play | x[2, 38] z[2, 36] | Gray. Rim chain (`SoftPlay_DeckLow`, `SoftPlay_DeckHigh`) is audit-locked. Next district. |
| Z2 Cling | x[2, 18] z[38, 78] | Gray. Blue cling arrives with a real wall, not a recolor. |
| Z3 Merry | x[22, 46] z[34, 60] | Gray. Crossing B lives here. Leave it open. |
| Z4 Slide mountain | x[22, 56] z[72, 98] | Gray. Decks stay on the rim when they are built. |
| Z5 Swings | x[58, 100] z[78, 98] | Gray. |
| Z6 Twin forts | x[118, 158] z[10, 90] | Gray. East spine x[130, 138] and gap z[46, 54] stay empty. |
| Z7 Kickball | x[64, 114] z[28, 68] | Polished this pass. See below. |
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

Placed at scale 1, 45 instances, 23 unique prefabs:

- Court at (88.6, 0, 53.2), 12 × 22 m. Hoops 1.2 m behind the end lines: south (88.6, 0, 41.0) yaw 0, north (88.6, 0, 65.4) yaw 180.
- Seven `StreetRoad_TwoLane` tiles, yaw 90, along z = 34 from x = 84 to 108. Planted median at (106.2, 0, 35.55).
- Five sidewalk bays at z = 29.6. Brick row on the south edge at z = 28.15: door, window, wall, window, door. `RooftopAC` on the wall top at y = 3.2.
- Two `Brick_Wall` climb faces, yaw 90, at x = 71.75, z = 33 and 37. Union face is 8.00 m, height 3.20 m.
- Gazebo at (77.78, 0, 35). AC on the roof at y = 3.05, the measured roof top.
- Parked `Car_Sedan`, `Car_Hatch`, `Car_Pickup` on the north lane, yaw 90, pivot y = 0.12 (road crown is about 0.11).
- Lights, hydrant, two benches, two trash cans, two scaffold bays, two maples, two planters, a picnic table, a park lamp, a shrub.

`CourtFence` is not placed. The catalog says the gate is closed.

### Routes

Check line:

`world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`

Envelopes the check used: jump apex 13.866 m, flat wall-jump 4.394 m, wall-run 5.89 m, air dash 1.50 m, grapple 28.0 m.

| Route | Kind | Measured |
|---|---|---|
| WestClimb | climb, wall-run, wall-jump | Climb to the gazebo deck at 0.32 m (wall 3.20 m, cap 3.40, practical 3.00). Wall-run 4.00 m on an 8.00 m face (max 5.89). Gap 4.15 m (flat 4.39). |
| WestGrapple | grapple | 12.21 m from the south lane to the climb cornice (range 1.5–28). |
| GazeboVault | vault | Rail 0.95 m (mantle 0.45–2.55, vault band 0.90–1.05). East walk-around open. |
| SlideDash | slide, air dash | Under-clear 1.68 m on both bays (crouch 1.05). Gap 0.92 m (dash 1.50). |
| ChaseLoop | ground chase | 128.3 m. Worst clearance 0.60 m at (89.0, 66.2) against `Hoop_N`. Samples 256, blocked 0. |

The chase is the rectangle (82.5, 31.55) → (111.0, 31.55) → (111.0, 66.20) → (81.5, 66.20) → close. South of the median, west of the east rail, north of the north hoop, east of the gazebo. Kept gray toys (rail, kick dugout, cover vaults) are blockers in the check.

No placement hits the open rect. None sits on the 472 m loop.

### Honest gaps

- WestClimb's 4.15 m gap is inside the 4.39 m flat cap and has little spare if the look is not along the wall.
- Chase clearance to the north hoop is 0.60 m. The minimum is 0.50 m (radius 0.40 + 0.10).
- The planted median's collider tops at 0.66 m, over the 0.30 m step and under the 0.90 m vault band. It is a trip. The chase goes around it.
- AC units overhang the brick thickness. The pivot sits on the wall or the roof, so the support test passes.
- Prefabs load through `UnityEditor.AssetDatabase` by reflection. A player build that does not reference them logs `[MegaPark] world prefab missing` and does not spawn a fallback cube. The five lumps stay hidden.
- District props are static and are not combined. `DrawCap` 120 is the graybox batch only. Four cameras can each see the 23 unique prefabs.
- Stills in `Docs/WorldStills/pass1/` are collider rasterizations. Canopies, glass, and brick courses are not in the image. Unity and Blender are not in this environment.

## Asset bugs for models dept

Do not edit these prefabs from the world lane. The pivot is seated, so they are not `floatingProps`. The collider does not hug the mesh.

Measured in prefab-local space by `check_z7.py` (placement y removed):

| Prefab | What is wrong |
|---|---|
| `Scaffold_Bay` | Lowest collider y = 0.20. Posts do not meet the pivot. Deck collider starts at y = 1.68, about 0.18 m above the post tops. |
| `Car_Sedan` | Lowest collider y = 0.15. Visual tire radius is 0.31 and the axle is y = 0.31 (`sk_car_sedan.py`), so the tread meets the ground and the box does not. |
| `Car_Hatch` | Lowest collider y = 0.15. Same wheel-box gap. |
| `Car_Pickup` | Lowest collider y = 0.18. Same wheel-box gap. |
| `TrashCan_Lidded` | Lowest collider y = 0.19. |
| `Bench_Wood` | Lowest collider y = 0.14. |
| `PicnicTable` | Lowest collider y = 0.37. The catalog top is 0.76 m. The legs do not reach the pivot. |
| `Shrub` | Sphere bottom y = 0.16. |
| `LightPost_Single` | A collider starts at y = 0.60 with a 0.36 m gap under it. The pole itself reaches the ground. |
| `ParkLamp` | A collider starts at y = 0.49 with a 0.24 m gap under it. |
| `Gangway` | Catalog `vault 0.88 m`, under the 0.90–1.05 band (`Docs/AssetLibrary.md`). Not placed. |

Intentional openings the same scan also prints, and that are not bugs: hoop rim above the pole, brick window glass, gazebo rail above the deck.

## Next district

Z1 soft-play. It is the first mass the SW spawn looks along, and it does not need a 22 × 12 court. Leave `SoftPlay_DeckLow` and `SoftPlay_DeckHigh` where the audit expects them. Dress the cubes and the ground around them with brick, street, and park pieces at scale 1. Do not cover the landmark near (20, 10), do not enter the 472 m loop, and do not add another unbatched LOD pile as large as Z7. The bowl and the kickball sightline stay open.
