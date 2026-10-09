# Environment queue

Re-graded 9 Oct 2026 after the geometry-hash still rule. Buildings (#122) leads. Vehicles (#125) and street props (#129) help. Do not rebase these branches onto each other. Draft lead only. Do not merge.

A colour or paint sibling uses the base quartet when vertex positions, indices, and UVs match on every LOD. The line prints `material-variant of <base>`. A different geometry hash needs its own quartet. `street` and `corner` count. LOD2 is at most 0.6× LOD1.

| Branch | Tip | Previous tightened run | This run |
| --- | --- | --- | --- |
| #122 | `723cc137` | `pass=10/125` paper 93 / geom 22 | `models-validate assets=125 pass=12 fail=113` / `models-split paperwork=91 geometry=22` |
| #125 | `2b8480a7` | `pass=0/188` paper 136 / geom 52 at `9c61fe70` | `models-validate assets=189 pass=14 fail=175` / `models-split paperwork=136 geometry=39` |
| #129 | `cf295d9b` | `pass=27/220` paper 176 / geom 17 at `8f7686a5` | `models-validate assets=220 pass=27 fail=193` / `models-split paperwork=176 geometry=17` |

`Container_20_Blue` and `Container_20_Green` match `Container_20` and pass as `material-variant of Container_20` on #122 and #129. `FireHydrant_Red` does not match the base cage. On #125 the six `_25` paints each have a pass 18 quartet and pass on their own. Their cage still matches `Sedan_Mid_A_25`.

## Passes

#122 (12): WalkUp, WoodFence, WoodFence_Corner, WoodFence_End, WoodFence_Gate, Container_20, Container_20_Blue, Container_20_Green, Rowboat, CourtFence, Gazebo, Road_Junction.

#125 (14): `Pickup_FullSize_25`, the three compacts, `Sedan_Mid_A_22`, `_23`, `_24`, `_25`, and the six `_25` paints. The four buses still fail only `lod2-ratio`. #129's model count did not change at `cf295d9b` (a compile fix in `SmoothMotion.cs` only).

#129 (27): GasCanopy, WalkUp (now #122's 2736/2216/1004 cage), WoodFence_Corner, Container_20 plus both enamels, Dock_Straight, FishingBoat, Rowboat, CourtFence, Gazebo, Bench_WoodIron, BikeRack_Hoop3, FireHydrant, FireHydrant_Silver (own pass 31 quartet), FireHydrant_Yellow, Fountain_Walk, LightPost_Globe, NewspaperRack, Newsstand_Corner, ParkingMeter_Single, ParkingMeter_Twin, Planter_Street, PowerPole_Span, Sign_AFrame, Sign_StreetName, StreetMedian_Planted.

## Buildings (#122)

1. Leave the 12 passes, including the two container enamels on the red quartet.
2. Replace copied LOD2s. GasCanopy, Dock_Straight, and FishingBoat are LOD2 equals LOD1. Cabin, Ranch_House, Boathouse, and HarborShed are over 0.6× LOD1.
3. New pieces, each with a quartet in the same pass: city alley, subway stair, overpass span, driveway apron, park restroom, bleachers, harbor ferry. No third cabin. No second road junction. No second walk-up.

## Vehicles (#125)

1. `Car_Sedan_25`, `Car_Hatch_25`, and `Car_Pickup_25` stay deleted. `Pickup_FullSize_25` passes: 5.105 × 1.999 × 1.761 m, slack 0.00 cm, LOD 11336/2728/1608, own pass 17 quartet. Leave it.
2. Leave the midsize passes. LOD2 is 2020, 2100, 2020, and 2020 against LOD1 3736, 3880, 3756, and 3676. Each `_25` paint has its own pass 18 quartet.
3. Leave the three compact passes. LOD2 is 1966, 1912, and 1954.
4. Buses keep their own stills. City40 LOD2 1280 is over 0.6× of 1504. City60 LOD2 1708 is over 0.6× of 2060. Blue and red match the City40 cage.
5. Do not restore the court. Take #122's container, gazebo, rowboat, and walk-up when this branch next touches the shared library. #129 already did.
6. Trains and trolleys are the remaining vehicle gap. The midsize shells now pass.

## Street props (#129)

1. Leave the 27 passes. `8f7686a5` copied WalkUp, CourtFence, the containers, Rowboat, HarborShed, Gazebo, and WoodFence_Corner from #122. Do not rebuild those.
2. GasCanopy, Dock_Straight, and FishingBoat pass the 0.6× cut on this tip and are not #122's meshes. Leave them unless #122's cut lands here later.
3. `FireHydrant_Red` still needs its own quartet. Its cage does not match `FireHydrant` or `FireHydrant_Yellow`. Silver's pass 31 quartet is the one to keep.
4. `Park/Planter` is not `Planter_Street`. It still needs its own quartet.
5. Do not re-import `Car_*`.

Restructure-impact count is 6. `Pickup_FullSize_25` is not a redo. #129 did not move past `8f7686a5`. See `Docs/Models/LEDGER.md`.
