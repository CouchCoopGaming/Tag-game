# Environment queue

Re-graded 9 Oct 2026 after the geometry-hash still rule. Buildings (#122) leads. Vehicles (#125) and street props (#129) help. Do not rebase these branches onto each other. Draft lead only. Do not merge.

A colour or paint sibling uses the base quartet when vertex positions, indices, and UVs match on every LOD. The line prints `material-variant of <base>`. A different geometry hash needs its own quartet. `street` and `corner` count. LOD2 is at most 0.6× LOD1.

| Branch | Tip | Previous tightened run | This run |
| --- | --- | --- | --- |
| #122 | `09958348` | `pass=126/132` paper 5 / geom 1 at `7cee8bbf` | `models-validate assets=132 pass=126 fail=6` / `models-split paperwork=5 geometry=1` |
| #125 | `0aa3061e` | `pass=18/189` paper 136 / geom 35 at `a5e0e40d` | `models-validate assets=189 pass=18 fail=171` / `models-split paperwork=136 geometry=35` |
| #129 | `918ce6d2` | `pass=43/220` paper 161 / geom 16 at `30af0ae6` | `models-validate assets=220 pass=81 fail=139` / `models-split paperwork=137 geometry=2` |

`Container_20_Blue` and `Container_20_Green` match `Container_20` and pass as `material-variant of Container_20` on #122 and #129. `FireHydrant_Red` does not match the base cage. On #125 the six `_25` paints each have a pass 19 quartet and pass on their own. Their cage still matches `Sedan_Mid_A_25`. The four years do not match each other.

## Passes

#122 (126): every library asset. The six fails are the player Hiers that ride on this branch. `Brick_Door` and `Brick_Window` bind `pass32/brick_door/` and `pass32/brick_window/`. Container enamels still print `material-variant of Container_20`.

#125 (18): `Pickup_FullSize_25`, the three compacts, `Sedan_Mid_A_22` through `_25`, the six `_25` paints, and the four buses. `0aa3061e` does not change an FBX. The pass count is the same. Bus LOD2 stays 900 and 1192.

#129 (81): the previous 43, plus the pass 33 quartets. New shells: Boathouse 264/120/72, Garage 500/196/108, House_Gable 2000/1724/760, House_Hip 2008/1732/728, Roof_Parapet 112/36/12, ShopFront 292/152/84, Store_Corner 4732/3388/1368, Store_Diner 2604/1932/660, Store_Laundromat 5228/3868/1560, Storefront_Glass 300/144/72, HarborCrane 472/208/124, Tree_Palm 120/60/36, Tree_Pine 2932/816/372, WaterTank 608/228/124. Also new: AC_Roof_Large, AC_Roof_Small, Awning_Door, Barrel_Traffic, Barricade_Type3, Barrier_Jersey, Barrier_Water, Bollard_Fixed, Bollard_Removable, Cabinet_Electrical, Court, Delineator_Post, Fence_Iron, Hoop, Kiosk_ATM, Kiosk_Charge, Rail_Sidewalk, Road_Crosswalk, Sidewalk_Gap, Sidewalk_Joint, Sign_Parking_2H, Sign_Speed_25, Sign_Stop, Sign_Street. The garage and house-gable quarters are those buildings.

## Buildings (#122)

1. Leave the 126 passes. The count did not change at `09958348`. That tip puts LOD2 back on the six new pieces, under 0.6×, and leaves LOD0 and LOD1 alone: Alley 260/132/48, Subway_Entrance 300/180/96, Driveway 72/48/24, Bleachers 300/216/108, Restroom 208/144/60, Ferry 340/180/72. RooftopAC stays 488/148 with no LOD2. The required cuts from `7cee8bbf` still stand: Cabin 4972/1364/792, GasCanopy 2272/1064/120, Ranch_House 2604/2340/296, Dock_Straight 3720/672/336, FishingBoat 2932/1148/260, HarborShed 3296/504/120, Tree_Pine 2932/816/384.
2. GasCanopy and Tree_Pine rewrote LOD0 index order. Vertex positions, UVs, and the face set are the same, including against #129. That is not a new cage.
3. No third cabin. No second road junction. No second walk-up. The player Hiers on this branch are #128's work.

## Vehicles (#125)

1. `Car_Sedan_25`, `Car_Hatch_25`, and `Car_Pickup_25` stay deleted. `Pickup_FullSize_25` passes: 5.105 × 1.999 × 1.761 m, slack 0.00 cm, LOD 11336/2728/1608, own pass 17 quartet. Leave the mesh. `0aa3061e` adds nose, hood, fender, and bumper boxes and seats the wheel boxes at y=0.03. The manifest still says `slackCm` 0.00. The checker did not remeasure that.
2. Leave this midsize body. Do not reshape it again. `a5e0e40d` replaced the passing cages. Size is 4.900 × 1.816 × 1.440 m. LOD is 4716/3588/1968, 4716/3636/2016, 4736/3608/1968, and 4608/3528/1968. The pass 19 side of `_25` is a three-box sedan. Each paint has its own quartet and the same cage as `_25`.
3. Leave the three compact passes. LOD2 is 1966, 1912, and 1954.
4. Leave the four bus passes. City40 LOD2 is 900 against 1504. City60 LOD2 is 1192 against 2060. Blue and red keep their own stills.
5. Do not restore the court. Take #122's container, gazebo, rowboat, and walk-up when this branch next touches the shared library. #129 already did.
6. Trains and trolleys are the remaining vehicle gap. The midsize shells now pass.

## Street props (#129)

1. Leave the 81 passes. The previous 43 all still pass. Do not rebuild the #122 copies: WalkUp, CourtFence, the containers, Rowboat, Gazebo, and WoodFence_Corner.
2. `HarborShed` is the remaining geometry fail: 3296/504/324. #122's LOD2 is 120 on the same 3296/504 hero. Take that cut. Do not rebuild the hero.
3. Garage changed 72 faces and Storefront_Glass changed 30. Neither was a pass, and neither matches #122. Leave those new cages. Tree_Pine faces still match #122. Its LOD2 is 372.
4. The other fails are stills. Many quarters are under 1280×720. `FireHydrant_Red` and `Park/Planter` keep their own quartets.
5. Do not re-import `Car_*`. Do not seal Planter, Tree_Maple, or RooftopAC a second time.

Restructure-impact count is 8. `Pickup_FullSize_25` is not a redo. See `Docs/Models/LEDGER.md`.
