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

---

# E assignment

Branch `cursor/tag-movement-evasion`. Draft only. The lead merges with `git merge` after a measured pass. `EvasionMoves.Enabled` stays off. `EvasionPose.Holds` stays as folded.

## Clips

Evasion, the played roll, the played stagger, and the landing and roll exits. Pass 8 seats the landings, the 65% roll, both roll exits, and the stagger on the deep absorb. Dive, spin, juke, and stutter stay on the pass 7 keys.

| Clip | Hip-sit fails | Pose |
|---|---|---|
| roll | pass, sit | inside the 12-clip line |
| stagger | pass, absorb | inside the 12-clip line |
| exit-LaunchLand | 0 | 0 (0.36) |
| exit-Stagger | 0 | 0 (0.36) |
| exit-SoftLand | 0 | 0 (0.36) |
| exit-Roll | pass | inside the 12-clip line |
| exit-RollAbsorb | pass | inside the 12-clip line |
| stutter | pass | inside the 12-clip line |
| spinL, spinR | pass | inside the 12-clip line |
| jukeL | pass | inside the 12-clip line |
| jukeR | pass | inside the 12-clip line |
| dive | pass | inside the 12-clip line |
| land-soft | pass | inside the 12-clip line |
| land-hard | pass | inside the 12-clip line |

Pass 8, on this branch:

`hip-sit clips=12 fails=0 pelvisBackMin=8.52 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.4`

`no-clip clips=12 frames=164 worldMax=0.0 pose=0.0 rigJoint=7.91 fails=0`

The twelve clips add `stagger` to the pass 7 set. The absorb is hip 35° over a spine of 15°, thigh 116°, knee 96°, yaw ±56°, thigh roll ±20°, hips bone down 49.1 cm. Pelvis 8.8 cm behind the support foot. Chest 11.1 cm ahead of the hips. Stagger holds that absorb for the quarter second. The weight curve still starts and ends at 0.

The played slide stays on the lead branch. `leadKnee=-10` stays. Vault, climb, mantle, and the other exits stay on `cursor/tag-movement-exits`.

## Target shapes

S1, `cursor/tag-storror-mocap`, commit `8fccec65178afb30c4f3bd0dccdc4aa8240d8301`. The source note is `Docs/Movement/HIPREF.md` and the curves are `hipref/measurements.json` on that commit. `ship` is `REFERENCE-ONLY`.

This branch cites the numbers below. It does not carry that note, the JSON, the side stills, pose json, strips, or any video frame. Clip 24 on that commit is unlicensed and stays there.

Typical loaded plants in that measurement: pelvis +13 to +43 cm behind the support foot, support knee 45–115°, shin forward.

The roll targets are for exit-Roll and exit-RollAbsorb. The played tuck is airborne and is not the target frame.

| Moment | Pelvis behind | Support knee | Shin |
|---|---|---|---|
| `02_drop_roll_gravel` 0.10 s, early fold | +19.8 cm | 115° | +9.9 cm |
| `02_drop_roll_gravel` 2.34 s, late crouch | +33.7 cm | 102° | +2.3 cm |
| `01_roll_grass` 0.80 s, stand-up sit | +18.0 cm | 52° | +7.4 cm |

`02` at 1.24 s is +68.2 cm with the shin at −42.2 cm. `02` at 1.44 s is −46.5 cm. `24_landing_roll` at 0.64 s is −9.0 cm with the knee at 152°, and that film is unlicensed. Those three stay out of the target.

HIPREF has no stagger row and no dive row. The stagger now uses the landing absorb above. The dive roll-up and the two roll exits are inside the pass 8 line. `EvasionPose.Holds` was not loosened. `EvasionMoves.Enabled` stays false.
