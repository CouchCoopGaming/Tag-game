# E assignment

Branch `cursor/tag-movement-evasion`. Draft only. The lead merges with `git merge` after a measured pass. `EvasionMoves.Enabled` stays off. `EvasionPose.Holds` stays as folded.

## Clips

Evasion and landings. E is on soft land, hard land, roll, spin, and jukeR. The lead scan below is the current read. The seated roll-up on this branch is not folded onto the lead.

| Clip | Hip-sit fails | Pose |
|---|---|---|
| roll | 0, tuck is airborne | 0 (0.43) |
| stagger | 8 back, hinge, knee, kneeBehind, drop, sole, shin | 0.95 Chest\|UpperArm_R world 1.08 fails 5 |
| exit-LaunchLand | 0 | 0 (0.36) |
| exit-Stagger | 0 | 0 (0.36) |
| exit-SoftLand | 0 | 0 (0.36) |
| exit-Roll | 4 back, knee, drop | 0.94 Spine\|UpperLeg_L world 0.25 fails 3 |
| exit-RollAbsorb | 2 back, knee, drop | 3.32 LowerArm_R\|UpperLeg_R world 0.23 fails 8 |
| stutter | 0 plant, back 14.8, knee 52.9, drop 10.4 | 0 (0.42) |
| spinL | 7 back, shin, kneeBehind along world −Y | 1.40 Spine\|UpperLeg_R |
| spinR | 7 back, shin, kneeBehind along world −Y | 1.40 Spine\|UpperLeg_L |
| jukeL | 0 plant, back 11.9, knee 52.9, drop 8.9 | 0 (0.42) |
| jukeR | 0 plant, back 11.9, knee 52.9, drop 8.9 | 0.64 Spine\|UpperLeg_R |
| dive | 2 drop, roll-up is 14.1 cm on the lead | 0 (0.43) on the lead; this branch seats it |

This branch already seats the dive roll-up, measured at `1adc2020`:

`hip-sit clips=6 fails=0 pelvisBackMin=9.27 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.9`

`no-clip clips=6 frames=115 worldMax=0.0 pose=1.74 rigJoint=7.89 fails=1`

The roll-up at 0.867 s and 0.900 s sits 13.6 cm behind the support foot, knee 79°, sole 0.2 cm, and clears the 20 cm drop. The thigh is 55°. The spine is 1.74 cm inside both upper legs. That pair stays rig-blocked. Spin stays 1.4 cm. `jukeR` stays 0.64 cm. The table above is the lead's full scan of the folded keys. The seated roll-up stays on this branch until the lead merges it.

Stagger contact at t=0.00: back −1.0 cm, knee 2.5°, drop 0, knee behind the pelvis. Exit-Roll contact at t=0.433: back 4.2 cm, knee 16.2°, drop 0. Exit-RollAbsorb contact at t=0.30: back 2.0 cm, knee 11.9°, drop 0.

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

HIPREF has no stagger row and no dive row. The stagger landing stays open on the measurement above. The dive roll-up is already seated on this branch, inside the +13 to +43 cm band at 13.6 cm with the knee at 79°. `EvasionPose.Holds` stays as folded.
