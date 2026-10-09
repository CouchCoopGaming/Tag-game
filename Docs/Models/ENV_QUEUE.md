# Environment queue

Re-graded 9 Oct 2026 from the current tips. Buildings (#122) leads. Vehicles (#125) and street props (#129) help. Do not rebase these branches onto each other.

Measured with `Tools/Models/validate_assets.py` on this lead branch. Self-reports are listed beside them. #125's current tip is `b332ca8f`, which is past the `f32539e8` note.

| Branch | Tip | Baseline (pass 2) | This run | Self-report |
| --- | --- | --- | --- | --- |
| #122 | `723cc137` | `pass=0/125` paper 103 / geom 22 | `models-validate assets=125 pass=19 fail=106` / `models-split paperwork=105 geometry=1` | `pass=24/125` |
| #125 | `b332ca8f` | `pass=0/193` paper 152 / geom 41 | `models-validate assets=191 pass=14 fail=177` / `models-split paperwork=158 geometry=19` | `pass=14/191` at `f32539e8` |
| #129 | `7dc3e222` | `pass=0/222` paper 190 / geom 32 | `models-validate assets=219 pass=22 fail=197` / `models-split paperwork=188 geometry=9` | `pass=27/219` |

#122 and #129 each quote a pass count 5 above this run. The fresh script is the number to use. The only geometry fail left on #122 is the player Hier, which is not this lane.

## Spot-check

Three passes opened per branch. These are real objects, not buried shells.

- #122 `Ranch_House` pass 30 scale: hip roof, porch rail, garage, 1.8 m figure. Reads.
- #122 `WalkUp` pass 30 quarter: fire escape, cornice, roof cap, sidewalk. Reads. Different mesh from #129's walk-up (2736 tris here, 1832 there).
- #122 `Container_20` pass 30 quarter: corrugated box, doors, figure. The still is one color. `Container_20_Blue` and `Container_20_Green` pass on that same quartet.
- #125 `Sedan_Mid_A_25` pass 15 side and nose: hard panels, greenhouse, wheel gap, hood shutline. Not the old loft. No badge. Width in the manifest is 1.816 m.
- #125 `Bus_City40` pass 1 side: city bus, folding door, dark glass. The blue livery has its own blue side still.
- #129 `WalkUp`, `FireHydrant`, and `Sign_AFrame` pass 25: each reads (simpler walk-up, bonnet and caps, OPEN A-frame, figure in frame). Not torn color on a grey body.

## Validator holes

The pass counts are inflated by the still matcher.

- A color or year suffix is stripped, so one quartet covers every sibling. `Sedan_Mid_A_22` through `_25` and the six paints are 10 passes on `sedan_mid_a/pass15`. LOD0 is 5016, 5176, 5036, and 4908, so the years are not one mesh. `FireHydrant_Silver` is 1148 tris and passes on the red hydrant stills.
- The token `street` is ignored, so `Park/Planter` passes on `Planter_Street`'s pass 25 quartet.
- `corner` is a generic suffix, so `WoodFence_Corner` binds `woodfence_quarter.jpg` even though `woodfence_corner_quarter.jpg` exists and shows the return.
- LOD2 with the same triangle count as LOD1 passes. That is true of `GasCanopy` (1064), `Dock_Straight`, and `FishingBoat` on both #122 and #129, and of `Bench_WoodIron`, `BikeRack_Hoop3`, `Fountain_Walk`, `Newsstand_Corner`, and `Sign_AFrame` on #129. Those are not shown to be coarser meshes.

Do not treat a shared still or an equal LOD2 as done.

## Restructure impact

Four redos, one undo.

1. Court, twice. #129 `8effda99` and #125 `c84459a3` both restore #122's 22 m by 15 m court and the 0.375 m face-to-rim gap. `Court`, `CourtFence`, and `Hoop` sizes match on all three tips. Same fix, two helpers.
2. Walk-up, two meshes. #122 and #129 each closed the landing and each pass. The meshes differ. #125 still fails `land-gap=54.0cm`. Pick the #122 mesh. Do not author a third.
3. LOD2 shortcut, both tips. #122 and #129 both "added LOD2" on the gas canopy, the straight dock, and the fishing boat with LOD2 tris equal to LOD1. Same numbers, same shortcut.
4. Cars, deleted on one side and kept on the other. #129 dropped `Car_Sedan`, `Car_Hatch`, and `Car_Pickup`. #125 removed `Sedan_Midsize` and `Sedan_Mid_A_21`, then renamed the three blockouts to `Car_Sedan_25`, `Car_Hatch_25`, and `Car_Pickup_25`. They fail only stills now. The rejection said delete them. The rename undoes that on #125 only.

Container roofs (23 cm), the gazebo (22.2 cm), the rowboat budget, `HarborShed`, and `CourtFence` are closed on #122 and still failing on #125 and #129. That is a missed copy, not a second design. Copy #122. Do not re-close them.

## Buildings (#122)

Geometry on this tip moved. Next is the library that still has no quartet (105 paperwork fails), not another landing pass.

1. Leave the spot-checked passes. Replace copied LOD2s (`GasCanopy`, `Dock_Straight`, `FishingBoat`) with a mesh that has fewer triangles than LOD1.
2. Give `Container_20_Blue` and `Container_20_Green` their own quartet, or one still that shows the enamel. The red quartet does not cover them.
3. New pieces, each with a quartet in the same pass: city alley, subway stair, overpass span, driveway apron, park restroom, bleachers, harbor ferry. No third cabin. No second road junction. No second walk-up.

## Vehicles (#125)

1. Delete `Car_Sedan_25`, `Car_Hatch_25`, and `Car_Pickup_25`. #129 already deleted the old names. Do not bring the blockouts back.
2. Keep one midsize body. `Sedan_Mid_A_25` is the shell that passed the eye test. Paints share it only when the tris match and the still shows that paint. `_22`, `_23`, and `_24` need their own stills or the same mesh.
3. `Sedan_Compact_25`, `Hatch_Compact_25`, and `Crossover_Compact_25` are inside the envelope and fail only stills. Shoot the quartet. Do not refit the cage again.
4. Buses can sit. Width is 2.641 m, LOD2 exists, and the blue side still is actually blue.
5. Do not restore the court again. Do not re-close landings. Take #122's container, gazebo, rowboat, and walk-up when this branch next touches the shared library.
6. Trains and trolleys wait until the renamed blockouts are gone.

## Street props (#129)

1. The pass 25 props that were opened read. Keep them. Replace LOD2 copies (`Bench_WoodIron`, `BikeRack_Hoop3`, `Fountain_Walk`, `Newsstand_Corner`, `Sign_AFrame`, and the three shared with #122) with a coarser mesh.
2. `FireHydrant_Silver` needs its own still. The red quartet is the red hydrant.
3. Copy #122's container, gazebo, rowboat, `HarborShed`, and `CourtFence`. Do not rebuild them here. `WalkUp` on this tip is the simpler mesh. Retire it in favor of #122's.
4. Do not re-import `Car_*`. That delete stays.
5. `Park/Planter` is not a pass. The checker used `Planter_Street`'s stills.
