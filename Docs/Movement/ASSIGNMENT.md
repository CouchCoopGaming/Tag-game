# C1 assignment

Branch `cursor/tag-movement-exits`. Draft only. The lead merges with `git merge` after a measured pass.

## Clips

Exits, the played climb, and the played wall run. The lead folded the climb and wall-run re-key from draft #140. Pose on both is 0. Landings and rolls stay with E. #137 is not folded. E is redoing the landings in pass 8.

Plants need hip flexion of at least 25°. Landings and crouches need hip flexion of at least 35° and spine flexion of at least 15°. The ratio stays at least 1.5.

| Clip | Hip-sit fails | Pose |
|---|---|---|
| vault (played) | 2 hipFlex, spineFlex. Plant hip 18°. Land hip 20° spine 8° | 0 (0.42) fails 0 |
| climb | 0, cruise | 0 (0.42) fails 0 |
| wall | 0, cruise | 0 (0.43) world 0.31 fails 0 |
| exit-WallRun | 9 hipFlex, spineFlex. Land hip 18° spine 8° | 0 (0.36) |
| exit-WallJump | 0, airborne | 0 (0.42) |
| exit-ClimbTopOut | 11 hipFlex. Plant hip 18° < 25° | 0 (0.36) |
| exit-ClingDrop | 0, airborne | 0 (0.36) world 0.31 |
| exit-Vault | 10 hipFlex, spineFlex. Land hip 18° spine 8° | 0 (0.36) |
| exit-Mantle | 9 hipFlex, spineFlex | 0 (0.36) |
| exit-Slide | 9 hipFlex, spineFlex. Crouch hip 18° spine 8° | 0 (0.36) |
| exit-AirDash | 0, airborne | 0 (0.36) |
| exit-Punch | 0, airborne | world 0.84 fails 3 |
| exit-Lunge | 0, airborne | 0.74 world 0.65 fails 2 |
| exit-ZipDrop | 0, airborne | 0 (0.41) |
| exit-GrappleArrive | 0, airborne | 0 (0.43) |
| exit-GrappleRelease | 0, airborne | 0 (0.23) |
| exit-TagBackEnd | 0, airborne | world 0.86 fails 3 |

Open work is the hip floors on the played vault, `exit-ClimbTopOut`, `exit-WallRun`, `exit-Vault`, `exit-Mantle`, and `exit-Slide`, plus exit-Punch, exit-Lunge, and exit-TagBackEnd. Climb and the wall run pass pose. The shared recovery sit is hip 18° and spine 8°. Do not reset the spine to rest to clear the floor.

Slide, idle, loco, sprint, punch, zip, and pad stay on the lead branch. The lead cleared zip, idle, loco, and sprint. Slide stays an honest fail under `leadKnee=-10`. Landings and rolls stay on `cursor/tag-movement-evasion`.

## Target shapes

S1, `cursor/tag-storror-mocap`, commit `8fccec65178afb30c4f3bd0dccdc4aa8240d8301`. The source note is `Docs/Movement/HIPREF.md` and the curves are `hipref/measurements.json` on that commit. `ship` is `REFERENCE-ONLY`.

This branch cites the numbers below. It does not carry that note, the JSON, the side stills, pose json, strips, or any video frame. Clips 22 and 24 on that commit are unlicensed and stay there.

Typical loaded plants in that measurement: pelvis +13 to +43 cm behind the support foot, support knee 45–115°, shin forward.

Vault, landing absorb on `09_run_vault_park` at 3.67 s: +28.8 cm, knee 92°, shin +5.7 cm. The run, the pre-vault, and the cross are under 8 cm or pelvis-in-front. `08_vault_block_close` keeps the pelvis in front through the cross. `22_pike_vault` is unlicensed and pelvis-in-front.

Climb. `15_cat_leap_wall` into the wall at 0.67 s: +30.4 cm, knee 58°, shin −0.6 cm. Cling at 2.37 s: +19.1 cm, knee 102°, shin +3.9 cm. `07_wall_climb_traverse` later plant at 5.84 s: +29.3 cm, knee 79°, shin +5.3 cm. The high reach at 3.67 s is +53.4 cm with the shin at −20.0 cm, outside the band. The into-wall shin trails; the cling is the shin-forward sit.

Mantle, climb onto the ledge on `06_run_walljump_climb_window`. On the ledge at 2.37 s: +40.3 cm, knee 47°, shin −6.8 cm. Hip over the ledge at 3.44 s: +43.1 cm, knee 45°, shin −5.4 cm. Those two sit in the pelvis and knee band with the shin trailing, so the shin still has to come forward to −0.5 cm or ahead. The stood-up frame is knee 20°, under the 25° plant bar. In-band shin-forward frames on the same track: 0.07 s at +24.8 cm, knee 51°, shin +6.3 cm; 4.00 s at +13.8 cm, knee 52°, shin +7.9 cm. `exit-Mantle` is the sat recovery.
