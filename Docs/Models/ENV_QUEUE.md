# Environment queue

Environment sub-lead is buildings, draft PR #122, branch `cursor/tag-asset-library`. Helpers are vehicles B2 #125 (`cursor/tag-asset-street-kit`) and street props B3 #129 (`cursor/tag-street-objects`). The Models lead owns `Docs/Models/STANDARD.md`, the validator, and the ledger. Do not rebase these branches onto each other.

## Vehicles helper (#125)

License first: one CC0-1.0 row per asset. A family note does not count.

Then delete the rejected meshes on that branch: `Car_Sedan`, `Car_Hatch`, `Car_Pickup`, `Sedan_Midsize`, `Sedan_Mid_A_21`. Do not keep iterating the sedan loft. Build a new midsize shell inside 4.70–5.05 × 1.75–1.90 × 1.38–1.50 m, with a 2022–2026 model year in the name (2026 is its own body), hood and roof colliders within 8 cm of the mesh top, a nose close-up in `passN`, and no badges. After that shell, bring the compact, hatch, and crossover into their envelopes, and narrow `Bus_City40` and `Bus_City60` to 2.40–2.65 m. Each needs a model year and LOD2. One 40 ft body and one 60 ft body. Trains and trolleys wait.

## Street props helper (#129)

License first, one row per asset.

Delete `Car_Sedan`, `Car_Hatch`, and `Car_Pickup` there too. Do not re-import the Vehicles shells.

Cut these under the prop ceiling, once for the whole hydrant family: `FireHydrant` and its colors, `NewspaperRack`, `ParkingMeter_Twin`, `Planter_Street`, `PowerPole_Span`, `Sign_AFrame`, `Sign_StreetName`, `StreetMedian_Planted`.

Add the missing LOD2, with the missing still roles in that same pass: `GasCanopy`, `Dock_Straight`, `FishingBoat`, `Bench_WoodIron`, `BikeRack_Hoop3`, `Fountain_Walk`, `LightPost_Globe`, `Newsstand_Corner`.

When that branch next takes roads, copy `Road_Junction` from #122 or #125. Do not draw a second junction. Do not author a third cabin. Copy these closed #122 meshes rather than authoring another: `Container_20` (and the blue and green enamels, same cage), `Gazebo`, `Rowboat`, `HarborShed`, and `CourtFence`. Their LOD0 is unchanged. `HarborShed` LOD2 is now 120 against LOD1 504. The WalkUp gap on #129 is still the worse one; copy the #122 mesh rather than authoring another.

## Buildings (#122)

Pass 31 rendered a still quartet for every geometry-clean library mesh and added `Alley`, `Subway_Entrance`, `Overpass`, `Driveway`, `Restroom`, `Bleachers`, and `Ferry`. `Ranch_House` stays. Do not author a third cabin, and do not draw a second road junction.

LOD2 is at most 0.6× LOD1. `GasCanopy` LOD2 is 120 against LOD1 1064. `Dock_Straight` LOD2 is 336 against LOD1 672. `FishingBoat` LOD2 is 260 against LOD1 1148. `Cabin` LOD2 is 792 against LOD1 1364. `HarborShed` LOD2 is 120 against LOD1 504. Houses, the ranch, and the three stores use a solid shell at LOD2. Meshes whose LOD0 is at or under 2000 no longer ship a copied LOD2. LOD0 and LOD1 of the closed container, gazebo, rowboat, HarborShed, and CourtFence are unchanged.

`Brick_Door` and `Brick_Window` keep their pass 31 pictures. The quartet also lives in `Docs/AssetStills/pass32/brick_door/` and `pass32/brick_window/` so the names `door` and `window` stay in the folder and are not read as a close-up role. Do not rename those two meshes. `Container_20_Blue` and `Container_20_Green` match the red cage and pass as a material variant of `Container_20`.

Lead validator at `7ddb4ed`, after this pass:

`models-validate assets=132 pass=126 fail=6`

`models-split paperwork=5 geometry=1`

Every library mesh passes. The six fails are the player Hiers, assigned below.

`Driveway` replaces two 4 m sidewalk tiles. Its −X face is the curb and butts the road edge the same way `Sidewalk` does. The wings keep the 2 m walk and the 0.27 m top. The apron cuts the curb and rises from the 0.12 m asphalt. `Ferry` is the passenger ferry (bow on −Z). It is not `Boat` and not `FishingBoat`.

## Player rig (A2 #128)

`Dummy_Mannequin_Tan_Hier_Hi` fails the player checks. Leave the fix to A2 #128. Do not patch the rig on this branch. On the lead validator at `7ddb4ed` the failing checks are:

- `sample-rate`
- `noclip-missing`
- `joints-not-on-shipped-mesh`
- `hip-sit`
- `stills-quarter`
- `stills-side`
- `stills-close`
- `stills-scale`

The still quartet belongs with the rig stills under `Docs/LocoStills`, not the library pass. The five colour Hiers no longer borrow the tan quartet: their geometry hash does not match the tan mesh, so each fails `stills-quarter`, `stills-side`, `stills-close`, and `stills-scale`. Leave those stills with A2 #128. The tan mesh still carries the checks above.
