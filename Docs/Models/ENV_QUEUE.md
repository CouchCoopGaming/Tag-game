# Environment queue

Environment sub-lead is buildings, draft PR #122, branch `cursor/tag-asset-library`. Helpers are vehicles B2 #125 (`cursor/tag-asset-street-kit`) and street props B3 #129 (`cursor/tag-street-objects`). The Models lead owns `Docs/Models/STANDARD.md`, the validator, and the ledger. Do not rebase these branches onto each other.

## Vehicles helper (#125)

Seat the compact wheel boxes on the ground. Measured after merging `3040d2e5`: `Sedan_Compact_25` `Col_Wheel_*` is y = 0.167–0.476, `Hatch_Compact_25` is y = 0.168–0.477, and `Crossover_Compact_25` is y = 0.192–0.546. `Pickup_FullSize_25` wheels already meet y = 0.012. The world lane is not editing these prefabs.

`Pickup_FullSize_25` mesh runs about z = −2.61 to 2.50 (5.06 m). The prefab boxes stop at z = 1.625, so the nose past the front wheels has no collider. Add a bumper box that meets the mesh.

License first: one CC0-1.0 row per asset. A family note does not count.

Then delete the rejected meshes on that branch: `Car_Sedan`, `Car_Hatch`, `Car_Pickup`, `Sedan_Midsize`, `Sedan_Mid_A_21`. Do not keep iterating the sedan loft. Build a new midsize shell inside 4.70–5.05 × 1.75–1.90 × 1.38–1.50 m, with a 2022–2026 model year in the name (2026 is its own body), hood and roof colliders within 8 cm of the mesh top, a nose close-up in `passN`, and no badges. After that shell, bring the compact, hatch, and crossover into their envelopes, and narrow `Bus_City40` and `Bus_City60` to 2.40–2.65 m. Each needs a model year and LOD2. One 40 ft body and one 60 ft body. Trains and trolleys wait.

## Street props helper (#129)

License first, one row per asset.

Delete `Car_Sedan`, `Car_Hatch`, and `Car_Pickup` there too. Do not re-import the Vehicles shells.

Cut these under the prop ceiling, once for the whole hydrant family: `FireHydrant` and its colors, `NewspaperRack`, `ParkingMeter_Twin`, `Planter_Street`, `PowerPole_Span`, `Sign_AFrame`, `Sign_StreetName`, `StreetMedian_Planted`.

Add the missing LOD2, with the missing still roles in that same pass: `GasCanopy`, `Dock_Straight`, `FishingBoat`, `Bench_WoodIron`, `BikeRack_Hoop3`, `Fountain_Walk`, `LightPost_Globe`, `Newsstand_Corner`.

When that branch next takes roads, copy `Road_Junction` from #122 or #125. Do not draw a second junction. Do not author a third cabin. The WalkUp, Cabin, Gazebo, and container landings are closed on #122. The WalkUp gap on #129 is still the worse one; copy the #122 mesh rather than authoring another.

## Buildings (#122)

Rebuild the baked shop words. `Store_Corner` reads MARKET, `Store_Diner` reads DINER, and `Store_Laundromat` reads WASH. Those letters are inside the LOD meshes. One face is readable from the street. The other side shows the same triangles mirrored, because the mesh has no second face. Each word needs two outward faces, positive scale, readable from the street and from the alley. Do not fix it with a negative scale. The world lane will not edit these FBX files. Zone labels already use `WorldSign` (two culled TextMesh faces). These store words are mesh letters, so they have to be rebuilt here.

Done on `abeaee4`: a CC0 row on every library asset, `Lib_Roof` squared to 512, the WalkUp, Cabin, Gazebo, container, and ranch landings closed, the rowboat under the harbor ceiling, and the missing LOD2s. `Ranch_House` stays. The lead validator on this tip is `models-validate assets=125 pass=24 fail=101`.

Next, now that those counts moved: a city alley, a subway stair entrance, an overpass span, a driveway apron that meets the existing curb, a park restroom, bleachers, and a harbor ferry. Each one gets a still quartet in the same pass. Do not author a third cabin, and do not draw a second road junction.

## World pass 7

`Container_20` and `Container_20_Blue` share one cage. `Climb_Body` starts at y = 0.22, so on park ground the shell floats 0.22 m above the pivot. `Dock_Straight` piles run to y = −1.165. That is a water seat. On park ground the piles clip the floor. The world lane placed both in Z6 and did not edit the meshes.

The lettering request and the compact-wheel / pickup-nose requests above are still open. Library `09958348` cuts LOD2 and does not rebuild MARKET, DINER, or WASH. Street-kit `a5e0e40d` reshapes the midsize sedan. Compact wheel boxes still start at y = 0.167, 0.168, and 0.192. Pickup boxes still stop at local z = 1.625.
