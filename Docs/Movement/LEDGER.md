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

Owner E. Flag off. Pass 7, measured on this branch. The folded read on `65aa8de0` was pose 1.4 cm and two hip fails. These lines replace it.

`hip-sit clips=11 fails=0 pelvisBackMin=9.72 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.4`

`no-clip clips=11 frames=156 worldMax=0.0 pose=0.0 rigJoint=7.91 fails=0`

Pass 7. The eleven clips are the six evasion moves, `land-soft`, `land-hard`, the 65% `roll`, `exit-Roll`, and `exit-RollAbsorb`. Spin spreads the pivot thigh ±20° and holds the arms at −36°, so the 1.4 cm spine hit is gone. `jukeR` thigh yaw is 0, and the drop table seats the outside sole. The dive roll-up keeps thigh 55°, knee 79°, and adds yaw ±20° with the left foot at 4°, so both soles stay inside 0.5 cm and the spine stays out of the thigh. `EvasionMoves.Enabled` stays false, and RT is sampled only while that flag is on.

## Landings and the 65% roll

The played sit is thigh 55°, knee 79°, yaw ±20°, hip 6° over a spine of 4°, both feet at 8°, hips bone down 20.6 cm. `HardThigh` 74, `HardKnee` −125, and `HardHip` 46 stay the brace constants, so air-feel and `RollStep` do not move. `RollThigh` 62 stays the step constant. The 65% roll plays this sit for the squash. `RollSpeed` stays 36.504. Soft and hard use the same legs. The old palm-down hard land was the 4.35 cm spine hit.

`exit-Roll` and `exit-RollAbsorb` use that sit on the plant, the rise, and the absorb. The invert keys spread the thighs ±24° so the spine stays out. Seconds stay 0.52 and absorb stays 0.32.

## Full scan

Stride 1 on the lead fold, before pass 7, every locomotion clip, every exit, and the six evasion clips. 39 clips, 769 frames. Evasion keys are the game Euler. `Sample.Drop` is the hips-bone drop, not the visual root. Thigh roll is the last two fields. `exit-ClimbTopOut` is scored as a plant.

`HIP clips 39 frames 769 loaded 159 hipFails 37 backMin -18.5 hingeMin 1.12 kneeMin 1.6 dropMin 0.0`

`NOCLIP clips 39 frames 769 worldMax 1.08 pose 5.09 rigJoint 8.00 fails 102`

Loaded frames are plants, landings, and crouches. Cruise and airborne frames are in the pose column and stay out of the hip fail count. A pose pass line is `pose=0` when every frame is at or under 0.5 cm. The number in parentheses is the deepest non-adjacent reading. `rigJoint` 8.00 cm is the hip cuff on the sprint spread and stays with the rig lane. This is not a global `fails=0`. The pass-21 14-clip line stays historical.

Hip fails on that lead scan are 37: slide 7, stagger 8, exit-Roll 4, exit-RollAbsorb 2, spinL 7, spinR 7, dive 2. The spin count faces world −Y. Dive's two fails are the roll-up drop, 14.1 cm against the 20 cm crouch bar. Those E rows are the keys from before pass 7. The eleven-clip lines in the evasion section replace them.

## Clip table

| Clip | Owner | Hip-sit fails | Pose |
|---|---|---|---|
| vault (played) | C1 | 0 (plant and land) | 0 (0.42) world 0 fails 0 |
| climb | C1 | 0, cruise | 3.74 Spine\|UpperLeg_L world 0 fails 12 |
| slide | A1 | 7 knee, shin | 0 (0.43) world 0 fails 0 |
| wall | C1 | 0, cruise | 4.97 Chest\|UpperArm_R world 0 fails 16 |
| roll | E | pass, sit | inside the 11-clip line |
| pad | A1 | 0, cruise | 4.28 Spine\|UpperLeg_L world 0.05 fails 26 |
| zip | A1 | 0, cruise | 0 (0.42) world 0 fails 0 |
| grapple | A1 | 0, hang | 0 (0.22) world 0 fails 0 |
| punch | A1 | 0, cruise | 5.09 Chest\|UpperArm_R world 0.40 fails 8 |
| tag | A1 | 0, cruise | 0 (0.42) world 0 fails 0 |
| stagger | E | 8 back, hinge, knee, kneeBehind, drop, sole, shin | 0.95 Chest\|UpperArm_R world 1.08 fails 5 |
| idle | A1 | 0, cruise | 0 (0.09) world 0.44 fails 0 |
| loco | A1 | 0, cruise | 0 (0.41) world 0 fails 0 |
| sprint | A1 | 0, cruise | 0 (0.41) world 0 fails 0 |
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
| exit-Roll | E | pass | inside the 11-clip line |
| exit-RollAbsorb | E | pass | inside the 11-clip line |
| stutter | E | pass | inside the 11-clip line |
| spinL, spinR | E | pass | inside the 11-clip line |
| jukeL | E | pass | inside the 11-clip line |
| jukeR | E | pass | inside the 11-clip line |
| dive | E | pass | inside the 11-clip line |
| land-soft | E | pass | inside the 11-clip line |
| land-hard | E | pass | inside the 11-clip line |

Slide crouch sample, t=0.10: back 39.1, hinge 1.57, knee 8.0, shin −32.1, drop 52.5, sole 0.00. Stagger land, t=0.00: back −1.0, knee 2.5, drop 0.0, knee behind the pelvis. Exit-Roll, exit-RollAbsorb, land-soft, land-hard, and the 65% roll are inside the pass 7 eleven-clip line above. The exit-Roll and exit-RollAbsorb numbers in the scan paragraph are the readings from before that re-key.

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

Wall, for C1. The played wall run is a cruise, so the hip rule does not score it. C1 is already re-keying the 4.97 cm chest overlap. A plant, if one is keyed later, uses:

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

- C1, branch `cursor/tag-movement-exits` (#139). Exits, the played climb, and the played wall run. C1 is already re-keying the played climb at 3.74 cm and the wall run at 4.97 cm. Still open on the exits: exit-Punch world 0.84 cm, exit-Lunge pose 0.74 cm and world 0.65 cm, exit-TagBackEnd world 0.86 cm. `exit-ClimbTopOut` is folded from #138. The other sat recoveries stay on the 20 cm sit. The vault, climb, mantle, and wall rows above are the target shapes. The filmed files stay on S1's branch.
- E, branch `cursor/tag-movement-evasion` (#137). The six evasion clips, the played roll, the played stagger, and the landing and roll exits: exit-LaunchLand, exit-Stagger, exit-SoftLand, exit-Roll, exit-RollAbsorb. Pass 7 closed spin, jukeR, the dive pose, soft, hard, the 65% roll, exit-Roll, and exit-RollAbsorb. Those eleven clips are the fails=0 lines in the evasion section. Stagger remains open: 8 hip fails and pose 0.95 cm. The roll rows in the target table are the shape those two exits were keyed toward. `EvasionPose.Holds` was not loosened. `leadKnee` stays −10 on the played slide, which is A1's clip. `EvasionMoves.Enabled` stays false.

A1 keeps idle, loco, sprint, slide, punch, zip, pad, grapple, and tag. This pass cleared zip, idle, loco, and sprint. Slide stays an honest fail under `leadKnee=-10`. Punch stays: cock yaw, strike yaw, and spine yaw are printed. Pad stays: rise pitch and apex pitch are printed, and a thigh spread or an arm roll does not clear the tuck. Grapple and tag already pass.

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

The four clips this pass moved, same camera, before and after. The after plate carries a ghost of the before pose. Vertical through the support foot, dot on the pelvis:

- `Docs/Movement/stills/lead1/zip-before.png` and `zip-after.png`. Before, the raised arm is 5.52 cm inside the chest at yaw +14. After, yaw is −30 and the sole is pitched 12°. Pose 0.42 cm.
- `Docs/Movement/stills/lead1/idle-before.png` and `idle-after.png`. Before, pitch −12 sits the upper arm 1.80 cm in the chest. After, pitch +12, pose 0.09 cm.
- `Docs/Movement/stills/lead1/loco-before.png` and `loco-after.png`. Before, yaw 8 is 0.97 cm in the chest. After, yaw 0 and the thighs turn out 6°. Pose 0.41 cm.
- `Docs/Movement/stills/lead1/sprint-before.png` and `sprint-after.png`. Same gait change. Pose 0.41 cm.

Punch 5.09 cm, wall 4.97 cm, and pad 4.28 cm are deeper and do not have an after plate. Punch and the pad are locked to printed pitches and yaws. The wall run is C1's. The earlier measured plates stay: `zip-measured.png`, `punch-measured.png`, `wall-measured.png`, `pad-measured.png`.

Climb top-out, same camera, before the #138 keys and after: `Docs/Movement/stills/lead1/exit-ClimbTopOut-before.png` and `exit-ClimbTopOut-after.png`. That pair is a small plant change and does not show a ghost.

Pass 7 before and after for the three deepest misses this pass: hard land (spine 4.35 cm), the 65% roll, and exit roll absorb. Same camera. `Docs/Movement/evasion/pass7/`.

## Full sim

`StrafeJumpSim` was run after the fold. It stops at the zone check with `a zone color missed 3:1`. That check compares zone swatches to every seat color and to cling, slide, plate, zip, and tag. The previous palette already misses 3:1 against those gameplay swatches, so this stop is not new. The run returns before the smooth-still writers. `Docs/SmoothStills/pass11/air.png` and `Docs/SmoothStills/pass12/handoff.png` were not rewritten. The evasion lines above were printed by `--evasion` on the same build.

## Honest flaws

- Played slide crouch measures knee 8.0° and shin −32.1 cm. The HIPREF entry is knee 77° and shin +12.5 cm. `leadKnee=-10` keeps the slide proof, so the keys stay and the target is recorded.
- Played stagger landing measures back −1.0 cm, knee 2.5°, drop 0. HIPREF has no stagger row. It is still open. This pass did not re-key `PunchStaggerPose`.
- Pose still open on A1: punch 5.09 cm, pad 4.28 cm. The printed cock, strike, rise, and apex numbers stay. Wall 4.97 cm and climb 3.74 cm are C1's. Zip, idle, loco, and sprint now pass.
- Default red and orange are under 3:1 on wood. Shapes separate those seats. Palette 0 is the exemption.
- Soles on the eight sits read 0.47 cm, inside the 0.5 cm window.
