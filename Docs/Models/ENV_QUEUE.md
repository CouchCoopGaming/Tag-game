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

When that branch next takes roads, copy `Road_Junction` from #122 or #125. Do not draw a second junction. Do not author a third cabin. The WalkUp landing on #129 is the worse gap; the buildings tip is closing WalkUp, Cabin, Gazebo, and the container roofs.

## Buildings (#122)

This tip, in order: a license row on every asset, then square `Lib_Roof`, close the WalkUp, Cabin, Gazebo, and container-roof landings, cut the rowboat under the harbor ceiling, and add the missing LOD2s. Keep `Ranch_House`. Side and close-up stills go in the same `passN` as each mesh that is repaired.

Alley, subway entrance, overpass, driveway, restroom, bleachers, and ferry wait until the validator counts move.
