# Movement ledger

`cursor/tag-movement` is the movement branch. The lead owns this ledger, `STANDARD.md`, the checks, and the final call. A helper keys on a sub-branch off this one. A clip joins here only after it has been measured.

Nothing in this file is a git merge of a helper branch. Old PRs stay open.

## What was folded

| Source | Tip | What landed |
|---|---|---|
| This branch | `5d4cd74b` | Played clips, proof locks, vault hip-sit on `MantlePose.Cleared` |
| C1, exits | `176983ee` | One exit beat per move. Recovery keys were then sat on this branch. The played vault was not replaced. |
| E, evasion | `65aa8de0` | `stutter`, `spinL`, `spinR`, `jukeL`, `jukeR`, `dive`. Flag stays off. |
| C1, climb top-out | `a08d687` | Draft #138. `exit-ClimbTopOut` plant. Folded with `git merge`. |

The played vault is `MantlePose.Cleared`. `MantlePose.At` remains the printed proof sample used by the handoff and the still writers. `VerbExitId.Vault` is the recovery after the cross, not a second copy of the vault. The path check looks for `MantlePose.Cleared` on the locomotor.

Evasion `Sample.Drop` is applied on the hips bone. The visual root and the capsule are not moved. `EvasionMoves.Enabled` defaults to false. RT is not a live verb while the flag is off. LT stays the couch rope.

## Effects, when #127 folds

C2 is draft #127, `cursor/tag-fx-kit`, tip `fc9c8d82` when this note was written. It is not folded. When it joins `cursor/tag-movement`, the fold takes C2's sheets and C2's `ComicPng` loader. This branch's word sheet leaves. One loader, one word sheet, one burst sheet.

- C2 `ComicAtlas.png` is the 36-word sheet, 3072×3072, six columns by six rows of 512. `ComicAtlas.Cells` is 36. The same pixels are under `Assets/StreamingAssets/FX`.
- C2 `ComicBurstAtlas.png` is 2560×1024, five columns by two rows of 512, 10 cells. Art and StreamingAssets both carry it.
- This branch's `Assets/Art/FX/ComicAtlas.png` and its StreamingAssets copy are the older 4096×1024 sheet, four cells of 1024. Both copies leave with the fold. `ComicAtlas` here is `Cells = 4`, `CellWidth = 1024`.
- `ComicPng.cs` is the same blob on both tips today (`6e108f83`). The fold keeps C2's file.

`ComicArt.Holds` and `CompileSmokeTest` on this branch still expect the 4096×1024 sheet. They follow C2's atlas when the sheets change.

## Locks

`StrafeJumpSim --proofs` matches the 14 locked lines, including `ropeBody=0` and `hot-path allocs before=101 after=0`.

```
evasion-moves stutter=0.35 spin=0.90 juke=1.40 dive=3.00 capOK=1 rootMotion=0 flagDefault=off
evasion-gestures cameraFP=0/72 moveFN=0/24 swallow=commit diveFN=0/12 stutterFP=0/15 stutterFN=0/15
```

Exit catalog holds: 17 exits, closest gap 38, chain true, roll 0.52 s. Coyote 0.10, buffer 0.16, cling grace 0.08, jump speed 24.7, terminal 56.16, roll at 65% of terminal, walk 6.9, crouch 3.68, sprint 13.8. Root motion off. One `CharacterController.Move` per Update. No ledge hang.

The pass-21 headline stays historical until a full stride-1 scan of the original 14 is re-run on purpose:

`no-clip clips=14 frames=475 worldMax=12.91 rawSelfMax=7.97 rigJoint=7.97 pose=12.91 fails=12`

## Re-keyed exits

Seven recovery beats stay on the sit measured before this fold: wall run, vault, mantle, slide, launch land, stagger, soft land. Spine yaw is 0. Elbows are symmetric. The hips bone is 20 cm down. That earlier line included the previous climb top-out as well:

`hip-sit clips=8 fails=0 pelvisBackMin=12.2 hingeMin=2.25 kneeMin=79.9 pelvisDropMin=20.0`

`no-clip clips=8 frames=76 worldMax=0.0 pose=0 rigJoint=7.53 fails=0`

The deepest non-adjacent reading on those frames is 0.36 cm (`Spine|UpperLeg_R`), under the 0.5 cm bar, so the pass line is `pose=0`. `rigJoint` 7.53 cm is the hip cuff and belongs to the rig lane.

Per clip, that sit, pose fails 0, world 0: wall run 9, vault 10, mantle 9, slide 9, launch land 10, stagger 10, soft land 8.

`exit-ClimbTopOut` was remeasured after the #138 merge. The capsule is already standing when the exit starts, and both soles stay on the lid, so the frames are a plant. Plant bars are pelvisBack at least 8 cm, knee at least 25°, drop at least 8 cm. All 11 frames:

`hip-sit clips=1 fails=0 pelvisBackMin=20.3 hingeMin=2.25 kneeMin=67.9 pelvisDropMin=17.8`

`no-clip clips=1 frames=11 worldMax=0.0 pose=0 rigJoint=5.99 fails=0`

The keyed knee euler is −70. The geometric support knee is 67.9°. The shin is 17.9 cm ahead of the ankle at the shallowest frame. The sole is 0.05 cm. Pose reading is 0.36 cm (`Spine|UpperLeg_R`), so the pass line is `pose=0`. C1 reported pelvis 13.3 cm back and knee 70°. This scanner reads pelvisBack 20.3 to 20.9 cm and geometric knee 67.9°. The drop matches at 17.8 cm. The plant passes, and it sits inside the +13 to +43 cm, 45–115° band.

## Evasion

Owner E. Flag off. Measured on `65aa8de0` and folded unchanged.

`hip-sit clips=6 loadedFrames=115 pelvisBackMin=8.99 cm hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.9 cm fails=2`

`no-clip clips=6 frames=115 worldMax=0.0 rigJoint=7.89 pose=1.4 fails=1`

The two hip fails are the dive roll-up on the folded keys. The thigh enters the spine past about 48°, so the crouch cannot reach 12 cm behind the foot and 20 cm of drop together. The knee there is 68° and the shin points forward. Pose 1.4 cm is the spine into the spin pivot thigh. `jukeR` is 0.64 cm on that same pair. `jukeL` is clear.

`cursor/tag-movement-evasion` at `1adc2020` seats that roll-up and is not folded here. On that branch the roll-up at 0.867 s and 0.900 s is 13.6 cm behind the foot, knee 79°, sole 0.2 cm, drop past 20 cm, thigh 55°. The spine is 1.74 cm inside both upper legs. Spin and `jukeR` are unchanged. Their lines:

`hip-sit clips=6 fails=0 pelvisBackMin=9.27 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.9`

`no-clip clips=6 frames=115 worldMax=0.0 pose=1.74 rigJoint=7.89 fails=1`

## Full scan

Stride 1 on tip `8ff68bef`, every locomotion clip, every exit, and the six evasion clips. 39 clips, 769 frames.

`HIP clips 39 frames 769 loaded 105 hipFails 27 backMin -63.0 hingeMin 1.12 kneeMin 1.6 dropMin 0.0`

`NOCLIP clips 39 frames 769 worldMax 18.49 pose 5.52 rigJoint 7.97 fails 394`

Loaded frames are plants, landings, and crouches. Cruise and airborne frames are in the pose column and stay out of the hip fail count. A pose pass line is `pose=0` when every frame is at or under 0.5 cm. The number in parentheses is the deepest non-adjacent reading. `rigJoint` 7.97 cm is the hip cuff and stays with the rig lane. This is not a global `fails=0`. The pass-21 14-clip line stays historical.

On the clips this scanner already agreed with the earlier exit rescan, the hip fails are 21: played slide 7, played stagger 8, exit-Roll 4, exit-RollAbsorb 2. The other 6 hip fails, `backMin -63.0`, and `worldMax 18.49` are the dive rows from a column remap that does not match `EvasionPose.Apply`. Stutter, spin, and juke came back with zero loaded frames in that remap. The folded evasion lines above are the keys on this branch. The seated roll-up is on `cursor/tag-movement-evasion` and is not folded here.

## Clip table

| Clip | Owner | Hip-sit fails | Pose |
|---|---|---|---|
| vault (played) | C1 | 0 (plant and land) | 0 (0.42) world 0 fails 0 |
| climb | C1 | 0, cruise | 3.74 Spine\|UpperLeg_L world 0 fails 12 |
| slide | A1 | 7 knee, shin | 0 (0.43) world 0 fails 0 |
| wall | A1 | 0, cruise | 4.97 Chest\|UpperArm_R world 0 fails 16 |
| roll | E | 0, tuck is airborne | 0 (0.43) world 0 fails 0 |
| pad | A1 | 0, cruise | 4.28 Spine\|UpperLeg_L world 0.05 fails 26 |
| zip | A1 | 0, cruise | 5.52 Chest\|UpperArm_L world 0.66 fails 23 |
| grapple | A1 | 0, hang | 0 (0.22) world 0 fails 0 |
| punch | A1 | 0, cruise | 5.09 Chest\|UpperArm_R world 0.40 fails 8 |
| tag | A1 | 0, cruise | 0 (0.42) world 0 fails 0 |
| stagger | E | 8 back, hinge, knee, kneeBehind, drop, sole, shin | 0.95 Chest\|UpperArm_R world 1.08 fails 5 |
| idle | A1 | 0, cruise | 1.80 Chest\|UpperArm_L world 0.44 fails 263 |
| loco | A1 | 0, cruise | 0.97 Chest\|UpperArm_L world 0 fails 10 |
| sprint | A1 | 0, cruise | 0.97 Chest\|UpperArm_L world 0 fails 6 |
| exit-WallRun | C1 | 0 | 0 (0.36) fails 0 |
| exit-WallJump | C1 | 0, airborne | 0 (0.42) fails 0 |
| exit-ClimbTopOut | C1 | 0 plant, back 20.3, knee 67.9, drop 17.8 | 0 (0.36) world 0 fails 0 |
| exit-ClingDrop | C1 | 0, airborne | 0 (0.36) world 0.31 fails 0 |
| exit-Vault | C1 | 0, recovery | 0 (0.36) fails 0 |
| exit-Mantle | C1 | 0 | 0 (0.36) fails 0 |
| exit-Slide | C1 | 0 | 0 (0.36) fails 0 |
| exit-AirDash | C1 | 0, airborne | 0 (0.36) fails 0 |
| exit-Punch | C1 | 0, airborne | 0 (0.42) world 0.84 fails 3 |
| exit-Lunge | C1 | 0, airborne | 0.74 world 0.65 fails 2 |
| exit-ZipDrop | C1 | 0, airborne | 0 (0.41) fails 0 |
| exit-LaunchLand | E | 0 | 0 (0.36) fails 0 |
| exit-GrappleArrive | C1 | 0, airborne | 0 (0.43) fails 0 |
| exit-GrappleRelease | C1 | 0, airborne | 0 (0.23) fails 0 |
| exit-Stagger | E | 0 | 0 (0.36) fails 0 |
| exit-TagBackEnd | C1 | 0, airborne | 0 (0.11) world 0.86 fails 3 |
| exit-SoftLand | E | 0 | 0 (0.36) fails 0 |
| exit-Roll | E | 4 back, knee, drop | 0.94 Spine\|UpperLeg_L world 0.25 fails 3 |
| exit-RollAbsorb | E | 2 back, knee, drop | 3.32 LowerArm_R\|UpperLeg_R world 0.23 fails 8 |
| stutter | E | E's plant sits | E's 6-clip line |
| spinL, spinR | E | E's plant sits | pose 1.4 on the pivot thigh |
| jukeL | E | E's plant sits | clear |
| jukeR | E | E's plant sits | pose 0.64 spine into the outside thigh |
| dive | E | 2 on the folded keys; seated on the evasion branch | folded line; branch pose 1.74 |

Slide crouch sample, t=0.10: back 39.1, hinge 1.57, knee 8.0, shin −32.1, drop 52.5, sole 0.00. Exit-Roll contact, t=0.433: back 4.2, knee 16.2, drop 0.0. Exit-RollAbsorb contact, t=0.30: back 2.0, knee 11.9, drop 0.0. Stagger land, t=0.00: back −1.0, knee 2.5, drop 0.0, knee behind the pelvis.

## Target shapes

S1, `cursor/tag-storror-mocap`, commit `8fccec65178afb30c4f3bd0dccdc4aa8240d8301`. The note is `Docs/Movement/HIPREF.md` and the curves are `hipref/measurements.json` on that commit. `ship` is `REFERENCE-ONLY`. This branch keeps the numbers below. It does not carry the note, the JSON, the side stills, or any frame. Clips 22 and 24 on that branch are unlicensed and stay there.

Typical loaded plants in that measurement: pelvis +13 to +43 cm behind the support foot, support knee 45–115°, shin forward.

Roll, for E, on exit-Roll and exit-RollAbsorb. The played tuck is airborne and is not the target frame.

| Moment | Pelvis behind | Support knee | Shin |
|---|---|---|---|
| `02_drop_roll_gravel` 0.10 s, early fold | +19.8 cm | 115° | +9.9 cm |
| `02_drop_roll_gravel` 2.34 s, late crouch | +33.7 cm | 102° | +2.3 cm |
| `01_roll_grass` 0.80 s, stand-up sit | +18.0 cm | 52° | +7.4 cm |

`02` at 1.24 s is +68.2 cm with the shin at −42.2 cm. `02` at 1.44 s is −46.5 cm. `24_landing_roll` at 0.64 s is −9.0 cm with the knee at 152°, and that film is unlicensed. Those three stay out of the target.

Vault, for C1. The played vault already passes its plant and its land. This row is the landing-absorb shape for that pass. `exit-Vault` is the sat recovery.

| Moment | Pelvis behind | Support knee | Shin |
|---|---|---|---|
| `09_run_vault_park` 3.67 s, landing absorb | +28.8 cm | 92° | +5.7 cm |

The run, the pre-vault, and the cross on `09` are under 8 cm or pelvis-in-front. `08_vault_block_close` and `22_pike_vault` keep the pelvis in front through the cross. `22` is unlicensed.

Climb, for C1. The played climb is a cruise and was not in the #138 diff. The pose fail on that clip stays open. `exit-ClimbTopOut` is the plant measured above. The reference sits, if a wall plant is keyed on the played climb, are:

| Moment | Pelvis behind | Support knee | Shin |
|---|---|---|---|
| `15_cat_leap_wall` 0.67 s, into the wall | +30.4 cm | 58° | −0.6 cm |
| `15_cat_leap_wall` 2.37 s, cling | +19.1 cm | 102° | +3.9 cm |
| `07_wall_climb_traverse` 5.84 s, later plant | +29.3 cm | 79° | +5.3 cm |

The into-wall shin is just trailing. The cling is the shin-forward sit. The high reach at 3.67 s is +53.4 cm with the shin at −20.0 cm, outside the band.

Mantle, for C1. `exit-Mantle` is the sat recovery. These moments are the climb onto the ledge.

| Moment | Pelvis behind | Support knee | Shin |
|---|---|---|---|
| `06_run_walljump_climb_window` 2.37 s, on the ledge | +40.3 cm | 47° | −6.8 cm |
| `06_run_walljump_climb_window` 3.44 s, hip over the ledge | +43.1 cm | 45° | −5.4 cm |

Both ledge moments sit in the pelvis and knee band with the shin trailing. The acceptance window still wants the shin forward, at or ahead of −0.5 cm. The stood-up frame is knee 20°, under the 25° plant bar. In-band shin-forward frames on the same track: 0.07 s at +24.8 cm, knee 51°, shin +6.3 cm; 4.00 s at +13.8 cm, knee 52°, shin +7.9 cm.

Wall, for A1. The played wall run is a cruise, so the hip rule does not score it. The pose fail stays open under the wall holds. A plant, if one is keyed later, uses:

| Moment | Pelvis behind | Support knee | Shin |
|---|---|---|---|
| `12_tictac_slanted_wall` 0.40 s, early sit | +40.8 cm | 84° | −5.2 cm |
| `10_wallrun_slanted` 0.93 s, in-band stride | +24.3 cm | 59° | +3.0 cm |
| `20_wallpop_180` 0.77 s, pop land | +39.8 cm | 77° | −5.8 cm |

The wall plant on `20` is −4.4 cm at 0.27 s and −16.5 cm at 0.43 s. Tic-tac at 1.24 s is −13.0 cm and at 2.54 s is −6.0 cm. Those are pelvis-in-front. The retired S2 frames 8 and 15 agree. Those keys were not copied. The 1.07 s wall-run sit is +9.4 cm, over 8 cm and under the +13 cm typical.

Slide, for A1. `leadKnee=-10` stays. The played crouch cannot meet a 45° knee while that lock holds, so the shape is recorded and the keys stay. `exit-Slide` is the sat recovery.

| Moment | Pelvis behind | Support knee | Shin |
|---|---|---|---|
| `17_slide_slope_crouch` 0.00 s, entry | +16.9 cm | 77° | +12.5 cm |
| `17_slide_slope_crouch` 0.80 s, in band | +33.9 cm | 108° | +1.7 cm |
| `17_slide_slope_crouch` 0.83 s, most behind | +34.8 cm | 110° | −1.6 cm |
| `17_slide_slope_crouch` 1.55 s, exit | +13.1 cm | 35° | +3.3 cm |

The exit knee is under 45°. The deepest support knee, 0.60 s, is +9.7 cm at 137°, under the +13 cm typical. The played crouch measured back 39.1, knee 8.0, shin −32.1, drop 52.5.

Stagger is absent from that measurement. E keeps the eight-frame landing fail as measured here.

## Helper queues

- C1, branch `cursor/tag-movement-exits` (#139), off this tip. Vault, climb, mantle, and every exit that is not a landing or a roll. Open on that list: played climb pose 3.74 cm, exit-Punch world 0.84 cm, exit-Lunge pose 0.74 cm and world 0.65 cm, exit-TagBackEnd world 0.86 cm. `exit-ClimbTopOut` is folded from #138. The other sat recoveries stay on the 20 cm sit. The vault, climb, and mantle rows above are the target shapes. The filmed files stay on S1's branch.
- E, branch `cursor/tag-movement-evasion` (#137). The six evasion clips, the played roll, the played stagger, and the landing and roll exits: exit-LaunchLand, exit-Stagger, exit-SoftLand, exit-Roll, exit-RollAbsorb. The dive roll-up is seated on that branch (13.6 cm, knee 79°, pose 1.74 cm) and is not folded here. Still open: stagger 8 hip fails and pose 0.95 cm, exit-Roll 4 hip fails and pose 0.94 cm, exit-RollAbsorb 2 hip fails and pose 3.32 cm, spin pose 1.4 cm, jukeR pose 0.64 cm. The roll rows above are the target shape for the two roll exits. `EvasionPose.Holds` stays as folded. `leadKnee` stays −10 on the played slide, which is A1's clip.

A1 keeps idle, loco, sprint, slide, wall, punch, zip, pad, grapple, and tag. Slide and wall targets are above. Zip, punch, pad, idle, loco, and sprint were left on the measured keys: zip hang pitch is `WallPose.ReachPitch` and must stay at or under −90°, punch spine yaw is held, wall arm pitch and yaw are held, and the pad is `LaunchPose.At` on the ballistic arc. Gait arm yaw is the locomotor read, and this pass leaves it.

## Reference, not owned

S2 (`cursor/tag-storror-clips`, tip `80cd5f14`) is retired. Slide, wall run, curved wall run, and the 180 are reference. Nothing was copied. The keys fail `pelvisBack` and were not measured for knee flex or pelvis drop.

`hip-sit clips=4 loadedFrames=203 pelvisBackMin=-30.5cm fails=36`

`no-clip pose=0 rigJoint=119`

Remaining fails there: slide crouch frames down to -17 cm, wall-run takeoff and the knee on the lip, 180 frames 8 and 15. Report: `80cd5f14:Docs/HierStills/v080/clips/pass14/hip_sit.txt`. S1 will write `Docs/Movement/REFERENCE.md`. This ledger does not.

The Hier rig rebuild is not approved and is not bound.

## Seats

Default paint, colour-blind setting off: P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Palettes 1–4 stay behind the setting. Palette 0 skips the colour-vision distance and the ground-contrast check because red and orange sit under 3:1 on wood. It and Glow still clear the ground on every palette. The accessibility proof line is unchanged.

## Stills

Recovery sits, before and after the re-key, same camera: `Docs/AnimStills/movement-pass1/`.

The four deepest pose clips on this scan are zip 5.52 cm, punch 5.09 cm, wall 4.97 cm, and pad 4.28 cm. Those keys were not changed, so there is one measured plate each, same camera, vertical through the support foot, dot on the pelvis: `Docs/Movement/stills/lead1/zip-measured.png`, `punch-measured.png`, `wall-measured.png`, `pad-measured.png`.

Climb top-out, same camera, before the #138 keys and after: `Docs/Movement/stills/lead1/exit-ClimbTopOut-before.png` and `exit-ClimbTopOut-after.png`.

## Full sim

`StrafeJumpSim` was run after the fold. It stops at the zone check with `a zone color missed 3:1`. That check compares zone swatches to every seat color and to cling, slide, plate, zip, and tag. The previous palette already misses 3:1 against those gameplay swatches, so this stop is not new. The run returns before the smooth-still writers. `Docs/SmoothStills/pass11/air.png` and `Docs/SmoothStills/pass12/handoff.png` were not rewritten. The evasion lines above were printed by `--evasion` on the same build.

## Honest flaws

- Played slide crouch measures knee 8.0° and shin −32.1 cm. The HIPREF entry is knee 77° and shin +12.5 cm. `leadKnee=-10` keeps the slide proof, so the keys stay and the target is recorded.
- Played stagger landing measures back −1.0 cm, knee 2.5°, drop 0. HIPREF has no stagger row.
- Exit-Roll and exit-RollAbsorb still fail the landing sit. E's target is the roll table above (+19.8 cm / 115°, +33.7 cm / 102°, +18.0 cm / 52°).
- Pose still open on A1: zip 5.52, punch 5.09, wall 4.97, pad 4.28, idle 1.80, loco 0.97, sprint 0.97. Climb 3.74 is C1's. The printed holds on zip, punch, wall, and the pad arc were left in place.
- The folded dive roll-up is still the two hip fails on this branch. The evasion branch seats it at 13.6 cm, knee 79°, with the spine 1.74 cm inside both thighs. Spin pose is 1.4 cm. `jukeR` pose is 0.64 cm. This scan's evasion remap is not a replacement for those lines.
- Default red and orange are under 3:1 on wood. Shapes separate those seats. Palette 0 is the exemption.
- Soles on the eight sits read 0.47 cm, inside the 0.5 cm window.
