# Movement ledger

`cursor/tag-movement` is the movement branch. The lead owns this ledger, `STANDARD.md`, the checks, and the final call. A helper keys on a sub-branch off this one. A clip joins here only after it has been measured.

Nothing in this file is a git merge of a helper branch. Old PRs stay open.

## What was folded

| Source | Tip | What landed |
|---|---|---|
| This branch | `5d4cd74b` | Played clips, proof locks, vault hip-sit on `MantlePose.Cleared` |
| C1, exits | `176983ee` | One exit beat per move. Recovery keys were then sat on this branch. The played vault was not replaced. |
| E, evasion | `65aa8de0` | `stutter`, `spinL`, `spinR`, `jukeL`, `jukeR`, `dive`. Flag stays off. |

The played vault is `MantlePose.Cleared`. `MantlePose.At` remains the printed proof sample used by the handoff and the still writers. `VerbExitId.Vault` is the recovery after the cross, not a second copy of the vault. The path check looks for `MantlePose.Cleared` on the locomotor.

Evasion `Sample.Drop` is applied on the hips bone. The visual root and the capsule are not moved. `EvasionMoves.Enabled` defaults to false. RT is not a live verb while the flag is off. LT stays the couch rope.

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

Eight recovery beats were sat after the fold: wall run, climb top-out, vault, mantle, slide, launch land, stagger, soft land. Loaded frames only. Spine yaw is 0. Elbows are symmetric. The hips bone is 20 cm down.

`hip-sit clips=8 fails=0 pelvisBackMin=12.2 hingeMin=2.25 kneeMin=79.9 pelvisDropMin=20.0`

`no-clip clips=8 frames=76 worldMax=0.0 pose=0 rigJoint=7.53 fails=0`

The deepest non-adjacent reading on those frames is 0.36 cm (`Spine|UpperLeg_R`), under the 0.5 cm bar, so the pass line is `pose=0`. `rigJoint` 7.53 cm is the hip cuff and belongs to the rig lane.

Per clip, same sit, pose fails 0, world 0: wall run 9, climb top-out 11, vault 10, mantle 9, slide 9, launch land 10, stagger 10, soft land 8.

## Evasion

Owner E. Flag off. The six clips were folded from `65aa8de0`. The dive stretch and the roll-up seat below are measured on the evasion sub-branch and are not folded yet.

`hip-sit clips=11 fails=0 pelvisBackMin=9.72 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.4`

`no-clip clips=11 frames=156 worldMax=0.0 pose=0.0 rigJoint=7.91 fails=0`

Pass 7. The eleven clips are the six evasion moves, `land-soft`, `land-hard`, the 65% `roll`, `exit-Roll`, and `exit-RollAbsorb`. Spin spreads the pivot thigh ±20° and holds the arms at −36°, so the 1.4 cm spine hit is gone. `jukeR` thigh yaw is 0, and the drop table seats the outside sole. The dive roll-up keeps thigh 55°, knee 79°, and adds yaw ±20° with the left foot at 4°, so both soles stay inside 0.5 cm and the spine stays out of the thigh. `EvasionMoves.Enabled` stays false, and RT is sampled only while that flag is on.

## Landings and the 65% roll

The played sit is thigh 55°, knee 79°, yaw ±20°, hip 6° over a spine of 4°, both feet at 8°, hips bone down 20.6 cm. `HardThigh` 74, `HardKnee` −125, and `HardHip` 46 stay the brace constants, so air-feel and `RollStep` do not move. `RollThigh` 62 stays the step constant. The 65% roll plays this sit for the squash. `RollSpeed` stays 36.504. Soft and hard use the same legs. The old palm-down hard land was the 4.35 cm spine hit.

`exit-Roll` and `exit-RollAbsorb` use that sit on the plant, the rise, and the absorb. The invert keys spread the thighs ±24° so the spine stays out. Seconds stay 0.52 and absorb stays 0.32.

## Clip list

Pose numbers are centimetres. A clip with every non-adjacent pair and the world at or under 0.5 cm is `pose=0` in the pass line. The figure in parentheses is the deepest reading. Hip notes are plant, landing, and crouch frames only.

| Clip | Owner | Status | No-clip | Hip-sit |
|---|---|---|---|---|
| vault (played) | lead | plant and land pass | pose 0 (0.42) world 0 fails 0 | plant back 11.8 knee 61.9 drop 10; land t=0.4 back 10.2 knee 79.9 drop 20 |
| climb | lead | open pose | pose 3.74 spine into thigh, fails 12 | cruise, not a plant |
| slide | lead | proof-locked | pose 0 (0.43) fails 0 | crouch knee 8.0, shin behind, back 39.1, drop 52.5. `leadKnee=-10` holds the proof |
| wall | lead | open pose | pose 4.97 chest into upper arm, fails 16 | cruise |
| roll | lead | tuck is not a plant | pose 0 (0.43) fails 0 | airborne tuck, not in kneeMin |
| pad | lead | open pose | pose 4.28, world 0.05, fails 26 | cruise |
| zip | lead | open pose | pose 5.52 chest into upper arm, world 0.66, fails 23 | cruise |
| grapple | lead | pass pose | pose 0 (0.22) fails 0 | hang is not a plant. Hang keys kept |
| punch | lead | open pose | pose 5.09, fails 8 | cruise |
| tag | lead | pass pose | pose 0 (0.42) fails 0 | cruise |
| stagger | lead | open | pose 0.95, world 1.08, fails 5 | land knee about 2–6, back negative, drop 0 |
| idle | lead | open pose | pose 1.80, 263 frames | cruise |
| loco | lead | open pose | pose 0.97, fails 10 | cruise |
| sprint | lead | open pose | pose 0.97, fails 6 | cruise |
| exit-WallRun | lead, from C1 | sat | pose 0 (0.36) fails 0 | in the 8-clip pass |
| exit-WallJump | C1 | airborne | pose 0 (0.42) fails 0 | not a plant |
| exit-ClimbTopOut | lead, from C1 | sat | pose 0 (0.36) fails 0 | in the 8-clip pass |
| exit-ClingDrop | C1 | airborne | pose 0 (0.36) world 0.31 fails 0 | not a plant |
| exit-Vault | lead, from C1 | sat | pose 0 (0.36) fails 0 | recovery, not the played vault |
| exit-Mantle | lead, from C1 | sat | pose 0 (0.36) fails 0 | in the 8-clip pass |
| exit-Slide | lead, from C1 | sat | pose 0 (0.36) fails 0 | in the 8-clip pass. Not the played slide |
| exit-AirDash | C1 | airborne | pose 0 (0.36) fails 0 | not a plant |
| exit-Punch | C1 | open world | pose 0 (0.42) world 0.84 fails 3 | not a plant |
| exit-Lunge | C1 | open world | pose 0.74 world 0.65 fails 2 | not a plant |
| exit-ZipDrop | C1 | pass pose | pose 0 (0.41) fails 0 | not a plant |
| exit-LaunchLand | lead, from C1 | sat | pose 0 (0.36) fails 0 | in the 8-clip pass |
| exit-GrappleArrive | C1 | pass pose | pose 0 (0.43) fails 0 | not a plant |
| exit-GrappleRelease | C1 | pass pose | pose 0 (0.23) fails 0 | not a plant |
| exit-Stagger | lead, from C1 | sat | pose 0 (0.36) fails 0 | in the 8-clip pass. Played stagger is still open |
| exit-TagBackEnd | C1 | open world | pose 0 (0.11) world 0.86 fails 3 | not a plant |
| exit-SoftLand | lead, from C1 | sat | pose 0 (0.36) fails 0 | in the 8-clip pass |
| exit-Roll | E | pass | inside the 11-clip line | plant and rise sit |
| exit-RollAbsorb | E | pass | inside the 11-clip line | absorb sits |
| stutter | E | pass | inside the 11-clip line | plant sits |
| spinL, spinR | E | pass | inside the 11-clip line | plants sit. Thigh yaw ±20, arms −36 |
| jukeL | E | pass | inside the 11-clip line | plant sits |
| jukeR | E | pass | inside the 11-clip line | outside yaw 0. Sole reseated |
| dive | E | pass | inside the 11-clip line | push-off and roll-up sit. Roll-up yaw ±20 |
| land-soft | E | pass | inside the 11-clip line | knee 79, drop 20.6, back 12.4 |
| land-hard | E | pass | inside the 11-clip line | same sit. Brace constants unchanged |
| roll | E | pass | inside the 11-clip line | 65% roll plays the sit |

The eleven clips in the pass 7 lines above are fails=0. The played slide crouch and the played stagger land are still the lead's, and they are not in that line. This is not a global fails=0 across every locomotion clip.

## Reference, not owned

S2 (`cursor/tag-storror-clips`, tip `80cd5f14`) is retired. Slide, wall run, curved wall run, and the 180 are reference. Nothing was copied. The keys fail `pelvisBack` and were not measured for knee flex or pelvis drop.

`hip-sit clips=4 loadedFrames=203 pelvisBackMin=-30.5cm fails=36`

`no-clip pose=0 rigJoint=119`

Remaining fails there: slide crouch frames down to -17 cm, wall-run takeoff and the knee on the lip, 180 frames 8 and 15. Report: `80cd5f14:Docs/HierStills/v080/clips/pass14/hip_sit.txt`. S1 will write `Docs/Movement/REFERENCE.md`. This ledger does not.

The Hier rig rebuild is not approved and is not bound.

## Helper queues

- C1 `bc-621b414b` — vault, climb, mantle, and the exit catalog, on a sub-branch off `cursor/tag-movement`. The eight sits above are the current keys. Open pose on the played climb and the played wall run is still theirs to propose. Do not add a second clip for a move that already has one.
- E `bc-ad550372` — evasion, plus landings and rolls, on `cursor/tag-movement-evasion`. Pass 7 clears the eleven clips above. `EvasionMoves.Enabled` stays false. `EvasionPose.Holds` was not loosened. `leadKnee` on the played slide was not retuned.

## Seats

Default paint, colour-blind setting off: P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Palettes 1–4 stay behind the setting. Palette 0 skips the colour-vision distance and the ground-contrast check because red and orange sit under 3:1 on wood. It and Glow still clear the ground on every palette. The accessibility proof line is unchanged.

## Stills

Side-by-side before and after for the five recovery sits that were worst before the re-key: exit slide, exit vault, climb top-out, exit stagger, soft land. Same camera. Vertical through the support foot, dot on the pelvis. `Docs/AnimStills/movement-pass1/`.

Pass 7 before and after for the three deepest misses this pass: hard land (spine 4.35 cm), the 65% roll, and exit roll absorb. Same camera. `Docs/Movement/evasion/pass7/`.

## Full sim

`StrafeJumpSim` was run after the fold. It stops at the zone check with `a zone color missed 3:1`. That check compares zone swatches to every seat color and to cling, slide, plate, zip, and tag. The previous palette already misses 3:1 against those gameplay swatches, so this stop is not new. The run returns before the smooth-still writers. `Docs/SmoothStills/pass11/air.png` and `Docs/SmoothStills/pass12/handoff.png` were not rewritten. The evasion lines above were printed by `--evasion` on the same build.

## Honest flaws

- Played slide crouch cannot meet a 45° knee while `leadKnee=-10` keeps the slide proof.
- Played stagger landing is a lean with the knee behind the pelvis. `PunchStaggerPose` was not re-keyed.
- Nine played clips from the historical scan still fail pose: zip, punch, wall, pad, climb, idle, stagger, loco, sprint. Those stay with the lead.
- Default red and orange are under 3:1 on wood. Shapes separate those seats. Palette 0 is the exemption.
- Soles on the eight sits read 0.47 cm, inside the 0.5 cm window, so they are not a float fail. They are not glued to 0.
