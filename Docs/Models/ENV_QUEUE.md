# Environment queue

Re-graded 9 Oct 2026 after the geometry-hash still rule. Buildings (#122) leads. Vehicles (#125) and street props (#129) help. Do not rebase these branches onto each other. Draft lead only. Do not merge.

A colour or paint sibling uses the base quartet when vertex positions, indices, and UVs match on every LOD. The line prints `material-variant of <base>`. A different geometry hash needs its own quartet. `street` and `corner` count. LOD2 is at most 0.6× LOD1.

| Branch | Tip | Previous tightened run | This run |
| --- | --- | --- | --- |
| #122 | `15c0a983` | `pass=12/125` paper 91 / geom 22 at `723cc137` | `models-validate assets=125 pass=12 fail=113` / `models-split paperwork=91 geometry=22` |
| #125 | `3040d2e5` | `pass=14/189` paper 136 / geom 39 at `29caae02` | `models-validate assets=189 pass=18 fail=171` / `models-split paperwork=136 geometry=35` |
| #129 | `30af0ae6` | `pass=27/220` paper 176 / geom 17 at `cf295d9b` | `models-validate assets=220 pass=43 fail=177` / `models-split paperwork=161 geometry=16` |

`Container_20_Blue` and `Container_20_Green` match `Container_20` and pass as `material-variant of Container_20` on #122 and #129. `FireHydrant_Red` does not match the base cage. On #125 the six `_25` paints each have a pass 18 quartet and pass on their own. Their cage still matches `Sedan_Mid_A_25`.

## Passes

#122 (12): WalkUp, WoodFence, WoodFence_Corner, WoodFence_End, WoodFence_Gate, Container_20, Container_20_Blue, Container_20_Green, Rowboat, CourtFence, Gazebo, Road_Junction.

#125 (18): `Pickup_FullSize_25`, the three compacts, `Sedan_Mid_A_22` through `_25`, the six `_25` paints, and the four buses. `3040d2e5` cuts bus LOD2 to 900 and 1192.

#129 (43): the previous 27, plus Brick_Door, Brick_Wall, Brick_Window, RooftopAC, ParkLamp, PicnicTable, Planter, Shrub, Tree_Maple, Sidewalk, Bench_Wood, FireHydrant_Red, LightPost_Single, Scaffold_Bay, StreetRoad_TwoLane, and TrashCan_Lidded. `FireHydrant_Red` uses its own pass 32 quartet.

## Buildings (#122)

1. Leave the 12 passes, including the two container enamels on the red quartet.
2. Replace copied LOD2s. GasCanopy, Dock_Straight, and FishingBoat are LOD2 equals LOD1. Cabin, Ranch_House, Boathouse, and HarborShed are over 0.6× LOD1.
3. New pieces, each with a quartet in the same pass: city alley, subway stair, overpass span, driveway apron, park restroom, bleachers, harbor ferry. No third cabin. No second road junction. No second walk-up.

## Vehicles (#125)

1. `Car_Sedan_25`, `Car_Hatch_25`, and `Car_Pickup_25` stay deleted. `Pickup_FullSize_25` passes: 5.105 × 1.999 × 1.761 m, slack 0.00 cm, LOD 11336/2728/1608, own pass 17 quartet. Leave it.
2. Leave the midsize passes. LOD2 is 2020, 2100, 2020, and 2020 against LOD1 3736, 3880, 3756, and 3676. Each `_25` paint has its own pass 18 quartet.
3. Leave the three compact passes. LOD2 is 1966, 1912, and 1954.
4. Leave the four bus passes. City40 LOD2 is 900 against 1504. City60 LOD2 is 1192 against 2060. Blue and red keep their own stills.
5. Do not restore the court. Take #122's container, gazebo, rowboat, and walk-up when this branch next touches the shared library. #129 already did.
6. Trains and trolleys are the remaining vehicle gap. The midsize shells now pass.

## Street props (#129)

1. Leave the 43 passes. `8f7686a5` copied WalkUp, CourtFence, the containers, Rowboat, HarborShed, Gazebo, and WoodFence_Corner from #122. Do not rebuild those.
2. GasCanopy, Dock_Straight, and FishingBoat pass the 0.6× cut on this tip and are not #122's meshes. Leave them unless #122's cut lands here later.
3. `FireHydrant_Red` passes on `pass32/firehydrant_red_*`. Its cage still does not match `FireHydrant`. Leave that quartet.
4. `Park/Planter` now has its own pass 32 quartet. It is not `Planter_Street`.
5. Do not re-import `Car_*`. Do not seal Planter, Tree_Maple, or RooftopAC a second time. This tip already changed those three cages away from #122.

Restructure-impact count is 7. `Pickup_FullSize_25` is not a redo. See `Docs/Models/LEDGER.md`.
