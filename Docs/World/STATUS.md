# Mega Park status

Pass 10 on `cursor/tag-world-c420`. Draft only. Feel numbers are unchanged. `MegaParkP1Layout.BuildSolids` is unchanged.

Z1 through Z10 are dressed. This pass did not add a district. Pass 9 eye stills were collider boxes on empty sky. Pass 10 reshoots from library LOD0 meshes, with the placed diffuse colors, on park ground. Each file is under 400 KB. Unity and Blender are not in this environment, so these are mesh rasters, not engine captures.

Parked cars were not re-seated. B2 `20f0d9fa` adds 2022–2026 bodies and leaves the shipped 2025 shells alone. `Sedan_Compact_25`, `Hatch_Compact_25`, `Crossover_Compact_25`, and `Pickup_FullSize_25` match `0aa3061e` byte for byte. Compact pivots stay y = 0.102. The pickup pivot stays y = 0.100. The road slab tops at y = 0.112 and every wheel bottom sits on it (gap 0).

The Z9 curb line used to run through the Z7 walk-up (south face z = 21.17, x 81.23–89.77). The return now dips to the south curb through that pinch. Nothing else was sitting on a crouch slot or a route centerline.

| Zone | Theme | Routes | World-check | Known flaws |
|---|---|---|---|---|
| Z1 | Soft-play lawn | 5 | floating 0, missing 0, scale 0 | Same gazebo-and-scaffold kit as Z2–Z5. `LightPost_Single` has a 0.36 m gap under the pole. |
| Z2 | Cling strip | 5 | floating 0, missing 0, scale 0 | Same kit. Looks of −30° and −60° miss the 4.10 m deck. |
| Z3 | Merry strip | 5 | floating 0, missing 0, scale 0 | Same kit. A high hop meets the gazebo roof. Crossing B stays empty. |
| Z4 | Slide grove | 5 | floating 0, missing 0, scale 0 | Same 13 props and the same 4.10 m deck gap as Z5. |
| Z5 | Swing grove | 5 | floating 0, missing 0, scale 0 | Same 13 props and the same 4.10 m deck gap as Z4. |
| Z6 | Harbor yard | 5 | floating 0, missing 0, scale 0 | Own prop set. Dock piles still end at y = −1.165. That is the library's water seat. |
| Z7 | Kickball street | 5 | floating 0, missing 0, scale 0 | Cars sit at gap 0. Underbody openings remain (0.27 m compacts, 0.29 m pickup). `WalkUp` climb faces start at y = 0.40. `Cabin` has a 0.20 m internal gap. Light post 0.36 m, park lamp 0.24 m. |
| Z8 | Crash bowl | 5 | floating 0, missing 0, scale 0 | Open rect stays empty. `FireEscape` foot sits at y = 0.15. Wash-house deck gap is 2.62 m. |
| Z9 | Bar highway | 5 | floating 0, missing 0, scale 0 | Crouch slot under the steel stays empty. Chase is 155.0 m and notches around the walk-up. Clearance 0.94 m against `BarPost_N0`. |
| Z10 | Hopscotch | 5 | floating 0, missing 0, scale 0 | Hops stay uncovered. Subway steps run to y = −1.51 and clip park ground. Alley-to-roof gap is 5.00 m. |

Every zone line is `world-check routes=5 reachable=5/5 floatingProps=0 missingColliders=0 scaleFails=0`. Re-run this pass, each checker exit 0.

Stills (library LOD0, 1.8 m figure in frame):

- `Docs/WorldStills/pass10/z8_top.png` and `z8_eye.png` — wash houses and playground on the north lip
- `Docs/WorldStills/pass10/z9_top.png` and `z9_eye.png` — three newsstands, mailbox, and meter
- `Docs/WorldStills/pass10/z7_top.png` and `z7_eye.png` — sedan, hatch, crossover, and pickup on the road
- `Docs/WorldStills/pass10/z10_top.png` and `z10_eye.png` — subway entrance and alley north of the hops

The Z8 pair frames the north lip. The south-apron newsstands stay in the bowl placement and are the subject of the Z9 pair.
