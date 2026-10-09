# Environment queue

Re-graded 9 Oct 2026 after the geometry-hash still rule. Buildings (#122) leads. Vehicles (#125) and street props (#129) help. Do not rebase these branches onto each other. Draft lead only. Do not merge.

A colour or paint sibling uses the base quartet when vertex positions, indices, and UVs match on every LOD. The line prints `material-variant of <base>`. A different geometry hash needs its own quartet. `street` and `corner` count. LOD2 is at most 0.6× LOD1.

| Branch | Tip | Previous tightened run | This run |
| --- | --- | --- | --- |
| #122 | `723cc137` | `pass=10/125` paper 93 / geom 22 | `models-validate assets=125 pass=12 fail=113` / `models-split paperwork=91 geometry=22` |
| #125 | `9c61fe70` | `pass=0/188` paper 136 / geom 52 at `bc02b9c4` | `models-validate assets=188 pass=0 fail=188` / `models-split paperwork=136 geometry=52` |
| #129 | `8f7686a5` | `pass=11/219` paper 177 / geom 31 at `a92b5987` | `models-validate assets=220 pass=27 fail=193` / `models-split paperwork=176 geometry=17` |

`Container_20_Blue` and `Container_20_Green` match `Container_20` and pass as `material-variant of Container_20` on #122 and #129. The six `_25` paints match `Sedan_Mid_A_25` and print `material-variant of Sedan_Mid_A_25`. Pass 18 gives that body a full quartet, so the paints now fail only `lod2-ratio` (2356 against 3676). `FireHydrant_Red` does not match the base cage. `Sedan_Mid_A_22`, `_23`, and `_24` do not match `_25`.

## Passes

#122 (12): WalkUp, WoodFence, WoodFence_Corner, WoodFence_End, WoodFence_Gate, Container_20, Container_20_Blue, Container_20_Green, Rowboat, CourtFence, Gazebo, Road_Junction.

#125: none. `Sedan_Mid_A_22`, `_23`, `_24`, and `_25` (plus the six paints on the `_25` quartet) fail only `lod2-ratio`. The four buses fail only `lod2-ratio`. `9c61fe70` says LOD2 was cut. The shipped FBX counts did not change.

#129 (27): GasCanopy, WalkUp (now #122's 2736/2216/1004 cage), WoodFence_Corner, Container_20 plus both enamels, Dock_Straight, FishingBoat, Rowboat, CourtFence, Gazebo, Bench_WoodIron, BikeRack_Hoop3, FireHydrant, FireHydrant_Silver (own pass 31 quartet), FireHydrant_Yellow, Fountain_Walk, LightPost_Globe, NewspaperRack, Newsstand_Corner, ParkingMeter_Single, ParkingMeter_Twin, Planter_Street, PowerPole_Span, Sign_AFrame, Sign_StreetName, StreetMedian_Planted.

## Buildings (#122)

1. Leave the 12 passes, including the two container enamels on the red quartet.
2. Replace copied LOD2s. GasCanopy, Dock_Straight, and FishingBoat are LOD2 equals LOD1. Cabin, Ranch_House, Boathouse, and HarborShed are over 0.6× LOD1.
3. New pieces, each with a quartet in the same pass: city alley, subway stair, overpass span, driveway apron, park restroom, bleachers, harbor ferry. No third cabin. No second road junction. No second walk-up.

## Vehicles (#125)

1. `Car_Sedan_25`, `Car_Hatch_25`, and `Car_Pickup_25` stay deleted. `Pickup_FullSize_25` is the requested F-150-style truck, not that blockout. Pass 17 stills are already 1280×720 and under 400 KB. Export the FBX and add the manifest row, then it gets graded. Until both are in the tree it is not an asset.
2. One midsize body per year. `_22`, `_23`, and `_24` have their own pass 16 quartets. Cut LOD2 to at most 0.6× LOD1 (2356 against 3736, 2436 against 3880, 2356 against 3756). The FBX on this tip is still those counts.
3. `_25` has a pass 18 quartet. The six paints use it (`material-variant of Sedan_Mid_A_25`) and fail only `lod2-ratio` (2356 against 3676). Cut that LOD2 in the shipped file.
4. The three compacts have quartets. Cut LOD2 (2344/3325, 2294/3284, 2400/3417) to 0.6×.
5. Buses keep their own stills. City40 LOD2 1280 is over 0.6× of 1504. City60 LOD2 1708 is over 0.6× of 2060. Blue and red match the City40 cage.
6. Do not restore the court. Take #122's container, gazebo, rowboat, and walk-up when this branch next touches the shared library. #129 already did.
7. Trains and trolleys wait until a midsize shell passes LOD2 and its own quartet.

## Street props (#129)

1. Leave the 27 passes. `8f7686a5` copied WalkUp, CourtFence, the containers, Rowboat, HarborShed, Gazebo, and WoodFence_Corner from #122. Do not rebuild those.
2. GasCanopy, Dock_Straight, and FishingBoat pass the 0.6× cut on this tip and are not #122's meshes. Leave them unless #122's cut lands here later.
3. `FireHydrant_Red` still needs its own quartet. Its cage does not match `FireHydrant` or `FireHydrant_Yellow`. Silver's pass 31 quartet is the one to keep.
4. `Park/Planter` is not `Planter_Street`. It still needs its own quartet.
5. Do not re-import `Car_*`.

Restructure-impact count is 6. `Pickup_FullSize_25` is not a redo. #129 did not move past `8f7686a5`. See `Docs/Models/LEDGER.md`.
