# Movement ledger

Evasion-family notes for pass 6. A1 owns `Docs/Movement/STANDARD.md`, this ledger, and the final call. `origin/cursor/tag-movement` was not on the remote when this pass fetched, so the work stayed on `cursor/tag-evasion-moves` for A1 to fold, including `65aa8de0`.

## Rig-blocked — spin pivot thigh and jukeR

Same pair the rig lane (#128) is rebuilding with ball-and-socket hips. The poses were not moved this pass.

| Clip | Frames (s) | Pair | Depth |
|---|---|---|---|
| `spinL` | 0.267, 0.300, 0.333, 0.367 | `Mesh_Spine` / `Mesh_UpperLeg_R` | 1.4 cm |
| `spinR` | 0.200, 0.233, 0.267, 0.300 | `Mesh_Spine` / `Mesh_UpperLeg_L` | 1.4 cm |
| `jukeR` | 0.100, 0.133, 0.167, 0.200, 0.233, 0.267, 0.300 | `Mesh_Spine` / `Mesh_UpperLeg_R` | 0.64 cm |
| `jukeR` | 0.333 | `Mesh_Spine` / `Mesh_UpperLeg_R` | 0.55 cm |

The pivot thigh and the juke's outside thigh are both 48°. `jukeL` at that angle stays under 0.5 cm. The right thigh mesh sits closer to the spine than the left.

## Landings and the 65% terminal roll

Inventory only. Nothing in `LandPose` or `HandoffFeel` was edited. The tightened landing rule is pelvisBack ≥ 8 cm, knee ≥ 45°, pelvis drop ≥ 20 cm, shin forward. Poses were applied with the game euler (positive thigh, knee already negative) on the Tan Hier. Drop is the mesh drop at full absorb. The capsule and the root do not move.

### Soft land

`LandPose.Soft`. The printed land-pose line locks `softKnee=-26`, `softThigh=18`, `softDrop=0.02`. Holds keeps the knee between −40° and −12° and the drop at or under 6 cm, so the 45° knee and the 20 cm drop are out of reach.

| Check | Measured | Bar |
|---|---|---|
| pelvisBack | −6.4 cm | ≥ 8 cm |
| knee | 26° | ≥ 45° |
| shin | +13.5 cm | forward |
| knee ahead of pelvis | +6.0 cm | ≥ 0 |
| pelvis drop | 2.0 cm | ≥ 20 cm |
| sole gap | −0.3 cm | within 0.5 cm |
| spine / upper leg | 0.34 cm | clear |
| pose | `Mesh_Chest` / `Mesh_UpperArm_L` 1.40 cm | 0.5 cm |

Proof-locked. The spine is clear at thigh 18°. This is not the #128 hip pair.

### Hard land

`LandPose.Hard`, full 50 cm drop. The printed line locks `hardKnee=-125`, `hardThigh=74`, `hardDrop=0.50`. Holds requires the thigh above 60° and the knee past −100°.

| Check | Measured | Bar |
|---|---|---|
| pelvisBack | −25.8 cm | ≥ 8 cm |
| knee | 125° | ≥ 45° |
| shin | +48.6 cm | forward |
| knee ahead of pelvis | +25.1 cm | ≥ 0 |
| pelvis drop | 50 cm | ≥ 20 cm |
| sole gap | 3.2 cm | within 0.5 cm |
| spine / upper leg | 4.35 cm | rig-blocked |

The foot folds behind the pelvis. PelvisBack cannot reach +8 cm while the thigh stays at 74°, and 74° is already 4.35 cm through the spine. Same #128 pair. Not retuned.

### Landing roll at 65% of terminal

`HandoffFeel.RollSpeed` is 36.504. The peak is roll weight 1 at halfway through the absorb, so the mesh drop is half of the hard drop (25 cm). `RollThigh` is 62° and `RollKnee` is −108° / −88°. Changing the thigh changes `RollStep` and the handoff proof line, so it was left.

| Check | Measured | Bar |
|---|---|---|
| pelvisBack | −34.2 cm | ≥ 8 cm |
| support knee | 88° (trail knee 108°) | ≥ 45° |
| shin | +46.6 cm | forward |
| pelvis drop | 25 cm | ≥ 20 cm |
| sole gap | 6.1 cm right, 14.3 cm left | within 0.5 cm |
| spine / upper leg | 1.86 cm | rig-blocked |
| pose | `Mesh_Hand_L` / `Mesh_UpperLeg_L` 6.91 cm | the hand passes through the folded thigh |

The impact frame, before the roll weight rises, is the hard land (spine 4.35 cm). Both frames are rig-blocked on the thigh.

## Dive stretch

The 0.17–0.53 s window was a symmetric crouch and then a knee tuck with the chest still up. It is now a forward lean: hip 58°, spine 4°, arms −66° / −60° with the elbows nearly straight, thighs 14° / 6°, knees −36° / −24°. The feet stay about 40 cm below the hips. Forearms meet the floor after that window. Root motion stays off.

Hip-sit and no-clip on the six evasion clips, after that change:

`hip-sit clips=6 loadedFrames=115 pelvisBackMin=8.99 cm hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.9 cm fails=2`

`no-clip clips=6 frames=115 worldMax=0.0 rigJoint=7.89 pose=1.4 fails=1`

The two hip-sit fails are the dive roll-up at 0.867 s and 0.900 s: pelvisBack 9.0 cm (crouch bar 12), pelvis drop 14.1 cm (bar 20), knee 68°, shin forward, sole gap 0.3 cm. Thigh 46° keeps the spine at 0.37 cm. Thigh 50° was 1.48 cm into the spine. Left as the rig limit, not retuned.
