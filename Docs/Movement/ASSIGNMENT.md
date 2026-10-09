# E assignment

Branch `cursor/tag-movement-evasion`, cut from `cursor/tag-movement` at `1d54919b`. Draft only. The lead merges with `git merge` after a measured pass. `EvasionMoves.Enabled` stays off. `EvasionPose.Holds` stays as folded.

## Clips

Evasion, the played roll, the played stagger, and the landing and roll exits.

| Clip | Hip-sit fails | Pose |
|---|---|---|
| roll | pass, sit | inside the 11-clip line |
| stagger | 8 back, hinge, knee, kneeBehind, drop, sole, shin | 0.95 Chest\|UpperArm_R world 1.08 fails 5 |
| exit-LaunchLand | 0 | 0 (0.36) |
| exit-Stagger | 0 | 0 (0.36) |
| exit-SoftLand | 0 | 0 (0.36) |
| exit-Roll | pass | inside the 11-clip line |
| exit-RollAbsorb | pass | inside the 11-clip line |
| stutter | pass | inside the 11-clip line |
| spinL, spinR | pass | inside the 11-clip line |
| jukeL | pass | inside the 11-clip line |
| jukeR | pass | inside the 11-clip line |
| dive | pass | inside the 11-clip line |
| land-soft | pass | inside the 11-clip line |
| land-hard | pass | inside the 11-clip line |

Pass 7, on this branch:

`hip-sit clips=11 fails=0 pelvisBackMin=9.72 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.4`

`no-clip clips=11 frames=156 worldMax=0.0 pose=0.0 rigJoint=7.91 fails=0`

The eleven clips are the six evasion moves, `land-soft`, `land-hard`, the 65% `roll`, `exit-Roll`, and `exit-RollAbsorb`. Spin, `jukeR`, the dive spine, the hard-land thigh, and the roll spine are inside the 0.5 cm bar. Soft, hard, and the roll sit the pelvis behind the support foot. Stagger stays open and was not re-keyed. Contact at t=0.00: back −1.0 cm, knee 2.5°, drop 0, knee behind the pelvis.

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

HIPREF has no stagger row and no dive row. The stagger landing stays open on the measurement above. The dive roll-up and the two roll exits are inside the pass 7 line. `EvasionPose.Holds` was not loosened. `EvasionMoves.Enabled` stays false.
