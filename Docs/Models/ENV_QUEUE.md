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

When that branch next takes roads, copy `Road_Junction` from #122 or #125. Do not draw a second junction. Do not author a third cabin. The WalkUp, Cabin, Gazebo, and container landings are closed on #122. The WalkUp gap on #129 is still the worse one; copy the #122 mesh rather than authoring another.

## Buildings (#122)

Pass 31 rendered a still quartet for every geometry-clean library mesh and added `Alley`, `Subway_Entrance`, `Overpass`, `Driveway`, `Restroom`, `Bleachers`, and `Ferry`. `Ranch_House` stays. Do not author a third cabin, and do not draw a second road junction.

Lead validator at `1299187`, after this pass:

`models-validate assets=132 pass=129 fail=3`

`models-split paperwork=2 geometry=1`

The geometry fail is the tan Hier, assigned below. The two paperwork fails are `Brick_Door` and `Brick_Window`. Their quartets are in `Docs/AssetStills/pass31/` at 1280×720, but the lead still matcher treats the tokens `door` and `window` as close-up role words and drops them, so the asset key cannot match. That needs a lead-side exception. Do not rename those two meshes.

`Driveway` replaces two 4 m sidewalk tiles. Its −X face is the curb and butts the road edge the same way `Sidewalk` does. The wings keep the 2 m walk and the 0.27 m top. The apron cuts the curb and rises from the 0.12 m asphalt. `Ferry` is the passenger ferry (bow on −Z). It is not `Boat` and not `FishingBoat`.

## Player rig (A2 #128)

`Dummy_Mannequin_Tan_Hier_Hi` fails the player checks. Leave the fix to A2 #128. Do not patch the rig on this branch. On the lead validator at `1299187` the failing checks are:

- `sample-rate`
- `noclip-missing`
- `joints-not-on-shipped-mesh`
- `hip-sit`
- `stills-quarter`
- `stills-side`
- `stills-close`
- `stills-scale`

The still quartet belongs with the rig stills under `Docs/LocoStills`, not the library pass. The five color Hier files are the same sculpture. A license table row clears them. The tan mesh still carries the checks above.
