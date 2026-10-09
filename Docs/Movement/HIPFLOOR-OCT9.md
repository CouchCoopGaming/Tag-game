# Hip-floor + no-clip table, Oct 9 floors

Scanner: `Tools/Tag/scan_hip_floors.py` (Blender 4.2.3, `Dummy_Mannequin_Tan_Hier_Hi.fbx`, keys from `StrafeJumpSim --pose-keys` + `--pose-keys-evasion`, stride 1, nothing lifted or relieved). Per-frame rows: `hipfloor-oct9-before.tsv`, `hipfloor-oct9-after.tsv`.

Floors: landing hip>=30 spine>=10 pelvis>=12 cm; run/sprint/evasion plant hip>=25 pelvis>=8; hand-supported hip>=12, pelvis waived only on hand-loaded frames (played vault plant); slide crouch hip>=30 spine>=10 pelvis>=12; rolls per reference (not floor-scored here); hinge>=1.5 on every loaded frame. Loaded = floor class AND support sole within 0.5 cm. No-clip limit 0.5 cm; joined joint-cuff overlap is rigJoint (#128); pose = non-adjacent self + world.

Before (98047a4a keys): `HIP clips 39 frames 769 loaded 120 hipFails 107` / `NOCLIP worldMax 1.08 pose 6.97 rigJoint 8.00 poseFails 86`

After (shared sit re-keyed): `HIP clips 39 frames 769 loaded 120 hipFails 42` / `NOCLIP worldMax 1.08 pose 6.97 rigJoint 8.00 poseFails 86`

Re-key: `VerbExitClips.Sit` hip 18/spine 8 -> 32/12 (hinge 2.67), thighs 64 -> 86, knees -82 -> -72, thigh yaw 26 -> 42, hips-bone drop 0.20 -> 0.315 m (soles 0.10 cm), and `Spread` arm pitch -10 deg (-50/-56/-42) so the forearms clear the raised thighs. The seven sit exits (WallRun, Vault, Mantle, Slide, LaunchLand, Stagger, SoftLand, 65 loaded frames) go from 65 fails to 0: pelvis 4.2 -> 15.3 cm behind, pose 0.36 -> 0.41 cm (Chest|UpperLeg_R), world 0. `StrafeJumpSim --proofs` output is byte-identical before/after; feel locks untouched; new rig not bound.

Note: this scanner reads the old sit's pelvis at 4.2 cm behind, not the 12.2 cm the earlier ledger line printed; the earlier scan script was never committed, so the two are not directly comparable. ClimbTopOut reads 13.3 cm here (C1's figure).

## Per clip (after)

| Clip | Class | Frames | Loaded | Hip fails | Why | hipMin | spineMin | backMin cm | pose cm | worst pair | world cm | rigJoint cm | pose fails |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| vault | hand | 13 | 2 | 0 | - | 18.0 | 8.0 | 1.5 | 0.42 | Spine\|UpperLeg_R | 0.00 | 7.61 | 0 |
| climb | cruise | 12 | 0 | 0 | - | - | - | - | 0.42 | Chest\|UpperLeg_R | 0.00 | 7.61 | 0 |
| slide | slide | 13 | 7 | 7 | hip | 22.0 | 14.0 | 70.5 | 0.43 | Spine\|UpperLeg_L | 0.00 | 7.52 | 0 |
| wall | cruise | 16 | 0 | 0 | - | - | - | - | 0.43 | Spine\|UpperLeg_R | 0.31 | 7.81 | 0 |
| roll | roll | 11 | 0 | 0 | - | - | - | - | 0.43 | Spine\|UpperLeg_L | 0.00 | 7.93 | 0 |
| pad | cruise | 49 | 0 | 0 | - | - | - | - | 4.28 | Spine\|UpperLeg_L | 0.05 | 7.97 | 26 |
| zip | cruise | 24 | 0 | 0 | - | - | - | - | 0.42 | Spine\|UpperLeg_R | 0.00 | 7.96 | 0 |
| grapple | cruise | 22 | 0 | 0 | - | - | - | - | 0.22 | Spine\|UpperLeg_R | 0.00 | 7.44 | 0 |
| punch | cruise | 12 | 0 | 0 | - | - | - | - | 5.09 | Chest\|UpperArm_R | 0.40 | 7.84 | 8 |
| tag | cruise | 14 | 0 | 0 | - | - | - | - | 0.42 | Spine\|UpperLeg_R | 0.00 | 7.77 | 0 |
| stagger | landing | 8 | 1 | 1 | back,hinge,hip,spine | 0.0 | 0.0 | -2.1 | 1.08 | Foot_R\|Ground | 1.08 | 7.84 | 5 |
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
| exit-Roll | roll | 17 | 0 | 0 | - | - | - | - | 0.94 | Spine\|UpperLeg_L | 0.25 | 7.90 | 3 |
| exit-RollAbsorb | roll | 11 | 0 | 0 | - | - | - | - | 3.32 | LowerArm_R\|UpperLeg_R | 0.23 | 7.97 | 8 |
| stutter | plant | 19 | 12 | 12 | hip | 14.0 | 4.0 | 11.1 | 0.42 | Spine\|UpperLeg_L | 0.00 | 7.83 | 0 |
| spinL | plant | 18 | 11 | 11 | hip | 14.0 | 4.0 | 11.1 | 1.40 | Spine\|UpperLeg_R | 0.00 | 7.72 | 4 |
| spinR | plant | 18 | 11 | 11 | hip | 14.0 | 4.0 | 11.1 | 6.97 | Hand_L\|UpperLeg_L | 0.00 | 7.89 | 8 |
| jukeL | plant | 14 | 0 | 0 | - | - | - | - | 0.77 | Spine\|UpperLeg_L | 0.00 | 7.72 | 8 |
| jukeR | plant | 14 | 0 | 0 | - | - | - | - | 1.86 | Spine\|UpperLeg_R | 0.00 | 7.91 | 8 |
| dive | roll | 32 | 0 | 0 | - | - | - | - | 0.43 | Spine\|UpperLeg_R | 0.00 | 7.80 | 0 |

## Still open (not touched this pass)

- `slide` (A1 played clip): 7 loaded frames hip 22 < 30; spine 14, pelvis 70 cm behind pass.
- `stutter`, `spinL`, `spinR` (E): 34 plant frames hip 14 < 25.
- `stagger`: 1 contact frame at hip 0 / pelvis -2.1 cm.
- `jukeL`/`jukeR`: support sole never within 0.5 cm in this contact test, so 0 loaded frames; the plant itself needs checking (E's own script uses the move window instead).
- Rolls (`roll`, `exit-Roll`, `exit-RollAbsorb`, `dive`) are not floor-scored; there is no per-phase reference table in the repo to score them against. The dive roll-up (hip 6/spine 4) still needs one.
- Pose (non-adjacent) fails unchanged at 86 frames: pad 26 (4.28 Spine|UpperLeg_L), punch 8 (5.09 Chest|UpperArm_R), spinR 8 (6.97 Hand_L|UpperLeg_L), RollAbsorb 8 (3.32), jukeL/R 8+8, stagger 5 (world 1.08 Foot_R|Ground), spinL 4, exit-Roll 3, exit-Punch 3, exit-TagBackEnd 3, exit-Lunge 2.
- rigJoint 8.00 cm (sprint hip cuff) stays with #128.

Stills (side, before | after): `Docs/Movement/stills/hipfloor-oct9/{exit-Vault,exit-Slide,exit-WallRun,exit-LaunchLand}-side.png`.
