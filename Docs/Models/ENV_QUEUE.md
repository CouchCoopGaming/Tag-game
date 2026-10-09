# Environment queue

Tightened re-grade, 9 Oct 2026. Buildings (#122) leads. Vehicles (#125) and street props (#129) help. Do not rebase these branches onto each other. Draft lead only. Do not merge.

The checker now requires three things the last run let through. A colour, year, or variant sibling needs its own quartet unless its FBX is byte-identical to the mesh those stills show. `street` and `corner` are name tokens. LOD2 tris are at most 0.6× LOD1 tris (`lod2-ratio` when they are not).

| Branch | Tip | Previous run | This run |
| --- | --- | --- | --- |
| #122 | `723cc137` | `pass=19/125` paper 105 / geom 1 | `models-validate assets=125 pass=10 fail=115` / `models-split paperwork=93 geometry=22` |
| #125 | `bc02b9c4` | `pass=14/191` paper 158 / geom 19 at `b332ca8f` | `models-validate assets=188 pass=0 fail=188` / `models-split paperwork=136 geometry=52` |
| #129 | `a92b5987` | `pass=22/219` paper 188 / geom 9 at `7dc3e222` | `models-validate assets=219 pass=11 fail=208` / `models-split paperwork=177 geometry=31` |

#122 did not move. #125 deleted the three renamed street-car blockouts, so the asset count dropped by 3. #129 reseated lamp colliders and rebuilt the court-fence LOD2.

## What stopped passing

#122 lost 9. `lod2-ratio`: Cabin (1364/948), GasCanopy (1064/1064), Ranch_House (2340/1688), Boathouse (448/364), Dock_Straight (672/672), FishingBoat (1148/1148), HarborShed (504/324). Stills: `Container_20_Blue` and `Container_20_Green` no longer borrow the red container quartet.

#125 lost all 14. The four buses fail `lod2-ratio` only (City40 LOD2 is 1280 against LOD1 1504). `Sedan_Mid_A_22` through `_25` and the six paints fail `lod2-ratio` and no longer share `sedan_mid_a/pass15`. `_25` still picks up `pass12/check_front_25.png` and `check_side_25.png`; close and scale are missing, so it does not pass.

#129 lost 11. `lod2-ratio`: GasCanopy, Dock_Straight, FishingBoat, Bench_WoodIron, BikeRack_Hoop3, Fountain_Walk, Newsstand_Corner, Sign_AFrame. Stills: `Park/Planter` no longer matches `planter_street_*`. `FireHydrant_Red` and `FireHydrant_Silver` no longer match the red hydrant quartet. `FireHydrant_Yellow` keeps its own pass 25 quartet and still passes.

## Passes that remain

#122 (10): WalkUp (2736/2216/1004), WoodFence, WoodFence_Corner, WoodFence_End, WoodFence_Gate, Container_20, Rowboat, CourtFence (3680/2656/620), Gazebo, Road_Junction. WoodFence_Corner now binds `woodfence_corner_*`, which is the return.

#125: none.

#129 (11): WalkUp (1832/1300/680), FireHydrant, FireHydrant_Yellow, LightPost_Globe, NewspaperRack, ParkingMeter_Single, ParkingMeter_Twin, Planter_Street, PowerPole_Span, Sign_StreetName, StreetMedian_Planted.

`Sedan_Compact_25`, `Hatch_Compact_25`, and `Crossover_Compact_25` have pass 17 quartets and fail only `lod2-ratio` (LOD2 is about 0.70× LOD1).

## Buildings (#122)

1. Leave the 10 passes. Do not shoot another landing pass for them.
2. Replace copied LOD2s. GasCanopy, Dock_Straight, and FishingBoat are LOD2 equals LOD1. Cabin, Ranch_House, Boathouse, and HarborShed are under the old "not more than LOD1" rule and over 0.6×.
3. `Container_20_Blue` and `Container_20_Green` need their own quartets. The red container stills stay on `Container_20`.
4. New pieces, each with a quartet in the same pass: city alley, subway stair, overpass span, driveway apron, park restroom, bleachers, harbor ferry. No third cabin. No second road junction. No second walk-up.

## Vehicles (#125)

1. `Car_Sedan_25`, `Car_Hatch_25`, and `Car_Pickup_25` are deleted on `bc02b9c4`. Leave them deleted. #129 already dropped the old names.
2. One midsize body. Cut LOD2 to at most 0.6× LOD1 (today 2356 against 3676 on `_25`). `_22`, `_23`, `_24`, and each paint need a quartet of their own unless the FBX bytes match `_25`.
3. The three compacts have quartets. Cut LOD2 (2344/3325, 2294/3284, 2400/3417) to 0.6×. Do not refit the cage again.
4. Buses keep their own stills. City40 LOD2 1280 is over 0.6× of 1504. City60 LOD2 1708 is over 0.6× of 2060.
5. Do not restore the court. Do not re-close landings. Take #122's container, gazebo, rowboat, and walk-up when this branch next touches the shared library.
6. Trains and trolleys wait until the midsize shell's LOD2 and its own quartet pass.

## Street props (#129)

1. Leave the 11 passes. `FireHydrant_Yellow` has its own stills. `Planter_Street` is the street planter. `Park/Planter` is a different prop and needs its own quartet.
2. Replace LOD2 copies: Bench_WoodIron (368/368), BikeRack_Hoop3 (876/876), Fountain_Walk (976/976), Newsstand_Corner (780/780), Sign_AFrame (196/196), plus GasCanopy, Dock_Straight, and FishingBoat.
3. `FireHydrant_Red` and `FireHydrant_Silver` need their own stills. Silver is 1148 tris. The base hydrant is 1328.
4. Take #122's container, gazebo, rowboat, HarborShed, and CourtFence. `a92b5987` rebuilt CourtFence to 3664/2640/144. #122's passing fence is 3680/2656/620. Retire the #129 fence. Retire this tip's WalkUp (1832) in favor of #122's (2736).
5. Do not re-import `Car_*`. That delete stays.

Restructure-impact count is 6. The log is in `Docs/Models/LEDGER.md`.
