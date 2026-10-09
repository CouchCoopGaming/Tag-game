# Hip-floor + no-clip table, pass 2 (Oct 9)

Same scanner as `HIPFLOOR-OCT9.md` (`Tools/Tag/scan_hip_floors.py`), with three corrections applied to BOTH the before and after runs:

1. **Evasion column layout.** `EvasionPose.WriteKeys` writes `HeadYaw, Bank, Seat, ThighRollL, ThighRollR` after the elbow yaws; the loader read those as `shoulderL, thRollL, thRollR, hipDrop`. ThighRollL (degrees) was landing in the hips-bone drop (metres), which is why the jukes read the sole 12 m off the floor and spinR read 6.97 cm. `Tools/Tag/evasion_keys_to_ingame.py` rewrites the evasion dump into the in-game layout first. With it, the jukes do plant (7 of 14 frames inside 0.5 cm each) and spinR's real worst overlap was 1.40 cm. The first table's evasion rows were wrong.
2. **Signed hip/spine.** Floors count forward flexion only. The played baseball slide leans back (hip -22, spine -14), which `abs()` had been counting as flexion.
3. **Rolls scored per phase** against the reference in `Docs/Movement/pose/HIP_TARGETS.md` (cursor/tag-storror-mocap, REFERENCE-ONLY film, numbers only): entry chest +19.2 / pelvis +19.8 cm (02_drop_roll_gravel 0.10 s), contact +10.3 / -11.4 cm (01_roll_grass 0.57 s), exit +19.9 / +18.0 cm (01_roll_grass 0.80 s). chest = hip + spine. A grounded roll frame passes at chest >= ref - 6 and pelvis >= ref - 6 cm. Tucked or airborne frames are not scored. Dive takeoff (0-0.22 s into the move) is a plant; the dive roll-up (from 0.76 s) is the roll exit. `stagger` (PunchStaggerPose hit reaction) is no longer called a landing: its only grounded frame is weight 0, the neutral stand. It is scored for no-clip only.

Before (69f588af keys): `HIP clips 39 frames 769 loaded 145 hipFails 67` / `NOCLIP worldMax 1.08 pose 5.09 rigJoint 8.00 poseFails 74`

After: `HIP clips 39 frames 769 loaded 146 hipFails 21` / `NOCLIP worldMax 1.08 pose 5.09 rigJoint 8.00 poseFails 67`

`--proofs` output is byte-identical. `--evasion` matches the ledger lines. The new `--pose-holds` (VerbExitProof, LandingRollPose, VerbExitFit, EvasionPose) passes. Its only text change is `roll-clip` 13.7 -> 11.9 in the verb-exit line, which still holds. Feel locks are untouched and the rig is not bound.

## Re-keys

| Clip | Change | Result |
|---|---|---|
| stutter | hip 14->26, thighs +12 (60/24), thigh yaw +/-20, drop -0.104->-0.1235 | 12 fails -> 0; pelvis 10.0 cm; pose 0.43 |
| spinL/spinR | hip 14->26, pivot knee -55->-29, pivot thigh yaw 25, drop -0.104->-0.0603 | 22 fails -> 0; pelvis 10.8 cm; pose 1.40 -> 0.37 (8 pose fails -> 0) |
| dive takeoff (DiveReach) | hip 14->26, thighs 48->60, yaw +/-20, drop -0.093->-0.1058 | plant passes |
| dive roll-up (DiveCrouch) | hip/spine 6/4 -> 14/6 (chest 20), thighs 46->56, knees -68->-62, yaw +/-16, drop -0.141->-0.1477 | 9 fails -> 0; pelvis 17.0 cm |
| exit-Roll (Rise, Plant) | Rise 6/2 -> 14/6, thighs 40/40, knees -30, hips-bone drop 4.8 cm; Plant legs match | 2 fails -> 0; end pelvis 19.0 cm |
| exit-RollAbsorb (HandsDown, AbsorbAt) | palms pitch -16 and yaw out 10, thighs out 8; the absorb rise now uses Rise's legs | 1 fail -> 0; worst overlap 3.32 -> 1.36 cm |

## Still open

- **slide (7 frames):** the played slide is a back-lean baseball slide (chest -36). The slide-crouch floor (forward hip >= 30) does not describe it. Leaning it further back would only beat `abs()`. A forward crouch would break the locked `slide pose` and `slide-pose-polish` proof lines. This needs a call from Landon on which slide the floor means.
- **jukeL/jukeR (14 frames, hip 14):** the soles plant now. Every hip >= 25 key tried either clips the thigh into the spine at 0.8-1.8 cm, lifts the sole up to 1.8 cm, or needs the hips-bone drop shallower than the 8 cm that `EvasionPose.Holds` locks. Not re-keyed.
- **punch 5.09 (Chest|UpperArm_R), pad 4.28 (Spine|UpperLeg_L):** both are locked to printed proof numbers (`punch-tag-polish` cockY/strikeZ, `handoff2`/`body-line` pad). Not moved.
- **exit-RollAbsorb 1.36 cm:** the deep hands-down crouch (hip 28, spine 36, thighs 72) still puts the spine into the left thigh.
- **jukeR 0.64, exit-Roll 0.94, stagger world 1.08 (foot into the floor), exit-Punch, exit-TagBackEnd, exit-Lunge:** unchanged.
- **exit-Roll 0.433-0.467 s:** the soles sit 0.75-1.08 cm above the floor, so those frames are not scored.
- **rigJoint 8.00 cm:** stays with #128.

## Per clip (after)

| Clip | Class | Frames | Loaded | Hip fails | Why | hipMin | spineMin | backMin cm | pose cm | worst pair | world cm | rigJoint cm | pose fails |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| vault | hand | 13 | 2 | 0 | - | 18.0 | 8.0 | 1.5 | 0.42 | Spine\|UpperLeg_R | 0.00 | 7.61 | 0 |
| climb | cruise | 12 | 0 | 0 | - | - | - | - | 0.42 | Chest\|UpperLeg_R | 0.00 | 7.61 | 0 |
| slide | slide | 13 | 7 | 7 | hip,spine | -22.0 | -14.0 | 70.5 | 0.43 | Spine\|UpperLeg_L | 0.00 | 7.52 | 0 |
| wall | cruise | 16 | 0 | 0 | - | - | - | - | 0.43 | Spine\|UpperLeg_R | 0.31 | 7.81 | 0 |
| roll | roll | 11 | 0 | 0 | - | - | - | - | 0.43 | Spine\|UpperLeg_L | 0.00 | 7.93 | 0 |
| pad | cruise | 49 | 0 | 0 | - | - | - | - | 4.28 | Spine\|UpperLeg_L | 0.05 | 7.97 | 26 |
| zip | cruise | 24 | 0 | 0 | - | - | - | - | 0.42 | Spine\|UpperLeg_R | 0.00 | 7.96 | 0 |
| grapple | cruise | 22 | 0 | 0 | - | - | - | - | 0.22 | Spine\|UpperLeg_R | 0.00 | 7.44 | 0 |
| punch | cruise | 12 | 0 | 0 | - | - | - | - | 5.09 | Chest\|UpperArm_R | 0.40 | 7.84 | 8 |
| tag | cruise | 14 | 0 | 0 | - | - | - | - | 0.42 | Spine\|UpperLeg_R | 0.00 | 7.77 | 0 |
| stagger | cruise | 8 | 0 | 0 | - | - | - | - | 1.08 | Foot_R\|Ground | 1.08 | 7.84 | 5 |
| idle | cruise | 263 | 0 | 0 | - | - | - | - | 0.44 | Foot_R\|Ground | 0.44 | 7.72 | 0 |
| loco | cruise | 10 | 0 | 0 | - | - | - | - | 0.41 | Spine\|UpperLeg_R | 0.00 | 7.82 | 0 |
| sprint | cruise | 8 | 0 | 0 | - | - | - | - | 0.41 | Spine\|UpperLeg_R | 0.00 | 8.00 | 0 |
| exit-WallRun | landing | 9 | 9 | 0 | - | 32.0 | 12.0 | 15.3 | 0.41 | Chest\|UpperLeg_R | 0.00 | 7.33 | 0 |
| exit-WallJump | cruise | 10 | 0 | 0 | - | - | - | - | 0.42 | Spine\|UpperLeg_L | 0.00 | 7.92 | 0 |
| exit-ClimbTopOut | hand | 11 | 11 | 0 | - | 18.0 | 8.0 | 13.3 | 0.36 | Spine\|UpperLeg_R | 0.00 | 5.99 | 0 |
| exit-ClingDrop | cruise | 8 | 0 | 0 | - | - | - | - | 0.36 | Spine\|UpperLeg_R | 0.31 | 7.78 | 0 |
| exit-Vault | landing | 10 | 10 | 0 | - | 32.0 | 12.0 | 15.3 | 0.41 | Chest\|UpperLeg_R | 0.00 | 7.33 | 0 |
| exit-Mantle | hand | 9 | 9 | 0 | - | 32.0 | 12.0 | 15.3 | 0.41 | Chest\|UpperLeg_R | 0.00 | 7.33 | 0 |
| exit-Slide | slide | 9 | 9 | 0 | - | 32.0 | 12.0 | 15.3 | 0.41 | Chest\|UpperLeg_R | 0.00 | 7.33 | 0 |
| exit-AirDash | cruise | 6 | 0 | 0 | - | - | - | - | 0.36 | Spine\|UpperLeg_L | 0.08 | 7.91 | 0 |
| exit-Punch | cruise | 9 | 0 | 0 | - | - | - | - | 0.84 | Foot_R\|Ground | 0.84 | 7.83 | 3 |
| exit-Lunge | cruise | 9 | 0 | 0 | - | - | - | - | 0.74 | Spine\|UpperLeg_L | 0.65 | 7.91 | 2 |
| exit-ZipDrop | cruise | 8 | 0 | 0 | - | - | - | - | 0.41 | Spine\|UpperLeg_R | 0.00 | 7.74 | 0 |
| exit-LaunchLand | landing | 10 | 10 | 0 | - | 32.0 | 12.0 | 15.3 | 0.41 | Chest\|UpperLeg_R | 0.00 | 7.33 | 0 |
| exit-GrappleArrive | cruise | 9 | 0 | 0 | - | - | - | - | 0.43 | Spine\|UpperLeg_L | 0.25 | 7.77 | 0 |
| exit-GrappleRelease | cruise | 8 | 0 | 0 | - | - | - | - | 0.23 | Spine\|UpperLeg_L | 0.01 | 7.59 | 0 |
| exit-Stagger | landing | 10 | 10 | 0 | - | 32.0 | 12.0 | 15.3 | 0.41 | Chest\|UpperLeg_R | 0.00 | 7.53 | 0 |
| exit-TagBackEnd | cruise | 8 | 0 | 0 | - | - | - | - | 0.86 | Foot_L\|Ground | 0.86 | 7.62 | 3 |
| exit-SoftLand | landing | 8 | 8 | 0 | - | 32.0 | 12.0 | 15.3 | 0.41 | Chest\|UpperLeg_R | 0.00 | 7.33 | 0 |
| exit-Roll | roll | 17 | 2 | 0 | - | 13.7 | 6.0 | 19.0 | 0.94 | Spine\|UpperLeg_L | 0.00 | 7.77 | 3 |
| exit-RollAbsorb | roll | 11 | 2 | 0 | - | 14.0 | 6.0 | 16.8 | 1.36 | Spine\|UpperLeg_L | 0.00 | 7.10 | 9 |
| stutter | plant | 19 | 12 | 0 | - | 26.0 | 4.0 | 10.0 | 0.43 | Spine\|UpperLeg_L | 0.00 | 7.72 | 0 |
| spinL | plant | 18 | 11 | 0 | - | 26.0 | 4.0 | 10.8 | 0.34 | Spine\|UpperLeg_R | 0.00 | 7.72 | 0 |
| spinR | plant | 18 | 11 | 0 | - | 26.0 | 4.0 | 10.8 | 0.37 | Spine\|UpperLeg_L | 0.00 | 7.72 | 0 |
| jukeL | plant | 14 | 7 | 7 | hip | 14.0 | 6.0 | 9.3 | 0.42 | Spine\|UpperLeg_L | 0.00 | 7.72 | 0 |
| jukeR | plant | 14 | 7 | 7 | hip | 14.0 | 6.0 | 9.3 | 0.64 | Spine\|UpperLeg_R | 0.00 | 7.72 | 8 |
| dive | roll | 32 | 9 | 0 | - | 14.0 | 4.0 | 13.7 | 0.43 | Spine\|UpperLeg_L | 0.02 | 7.77 | 0 |

Stills (side, before | after): `Docs/Movement/stills/hipfloor-oct9b/{stutter,spinR,dive,exit-RollAbsorb}-side.png`.
