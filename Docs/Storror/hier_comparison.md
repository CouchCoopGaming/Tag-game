# Storror vs Hier mannequin proportions (v0.7.8)

All values are fractions of standing height (H). Hier H = 1.856 m (sole bottom at z = -0.041 to crown at 1.815). The numbers come from `Tools/Tag/build_mannequin_hier.py` and match the bone offsets in `Dummy_Mannequin_Tan_Hier_Hi.fbx` (UpperLeg 0.5405, LowerLeg 0.49, UpperArm 0.37, LowerArm 0.33, shoulder joint at x = 0.235).

Storror numbers are the median of all 20 clips (01-16 plus the slide, both vertical wall runs, and the wall-pop 180), extracted from 720p/480p video and three vertical Shorts, with about 2,070-2,660 gated frames per segment. They are MediaPipe world landmarks, and stature is rebuilt from the joint chain (see `proportions.json` → `method`). "Winter" is the standard anthropometric table, included as a cross-check. Adding clips 17-20 moved the ratios by at most 0.005H. The recommended constants below are unchanged from the 16-clip pass.

| Segment (joint centre to joint centre) | Storror (MediaPipe) | Winter ref | Hier now | Hier recommended | Constant change |
|---|---|---|---|---|---|
| Torso (mid-hip to mid-shoulder) | 0.310 | ~0.258 | **0.190** | 0.254 | `HIP_Z` 1.05 → **0.93** |
| Thigh | 0.237 | 0.245 | **0.291** | 0.245 | `UL_LEN` 0.540 → **0.455** |
| Shin | 0.253 | 0.246 | 0.264 | 0.245 | `LL_LEN` 0.490 → **0.455** |
| Leg (thigh+shin) | 0.490 | 0.491 | **0.555** | 0.490 | (from the two above) |
| Upper arm | 0.160 | 0.186 | 0.199 | 0.183 | `UA_LEN` 0.370 → **0.340** |
| Forearm (elbow to wrist) | 0.151 | 0.146 | **0.178** | 0.148 | `LA_LEN` 0.330 → **0.275** |
| Whole arm | 0.311 | 0.332 | **0.377** | 0.331 | (from the two above) |
| Shoulder joint spacing | 0.215 | ~0.21-0.22 | 0.253 | 0.221 | `SHOULDER_X` 0.235 → **0.205** |
| Hip joint spacing | 0.129 | ~0.10 | 0.127 | 0.127 | keep `HIP_X` 0.118 |
| Head breadth (ear to ear) | 0.100 | ~0.085 | **0.135** | 0.108 | Head sph x 0.125 → **0.100** (y 0.114 → 0.108) |
| Head height (chin to crown) | ~0.159 (weak estimate) | 0.130 | 0.149 (1/6.7) | ~0.138 (1/7.25) | Head sph z 0.150 → **0.140**, centre z +0.010 |

Ratios that don't depend on height:

| | Storror 3D | Storror 2D check | Winter | Hier now | Hier recommended |
|---|---|---|---|---|---|
| leg / torso | 1.58 | 1.43 | ~1.90 | **2.93** | 1.93 |
| shin / thigh | 1.07 | 0.97 | 1.00 | 0.91 | 1.00 |
| forearm / upper arm | 0.94 | - | 0.79 | 0.89 | 0.81 |
| shoulder / hip width | 1.66 | 1.60 | ~2.1 | 1.99 | 1.74 |
| arm / leg | 0.63 | - | 0.68 | 0.68 | 0.68 |

## What this says
1. **The biggest gap is torso length versus leg length.** Hier's hip joint sits at 0.57H, while a person's is about 0.53H, so the legs are about 13% too long and the torso is far too short. Storror, the 2D check and the Winter table all agree on leg length (about 0.49H). Lowering `HIP_Z` by 12 cm and shortening the thigh more than the shin fixes it without changing stature, shoulder height or the neck.
2. **The arms are long, mostly in the forearm.** Total arm length is 0.377H, against 0.31-0.33H for a person. The hands hang near the knees, which is the "ape" read.
3. **The shoulders are still a bit wide** at joint level, even after the v0.7.8 narrowing. Another 3 cm per side matches the athletes. Hip spacing already matches, so leave `HIP_X` alone.
4. **The head is mainly too wide.** Its breadth (0.25 m) is about 1.4x what the data says. Its height is about 1/6.7 of the body, while the script's own comment targets 1/7.5, so a small z trim is also reasonable.
5. If the long-leg, small-torso look is deliberate stylisation, a **50% blend** keeps it readable while moving toward real athletes. The blend values are: HIP_Z 0.99, UL 0.50, LL 0.47, UA 0.355, LA 0.30, SHOULDER_X 0.22, head x 0.112 / z 0.145.

## Side effects to plan for
- Hip height drops by 12 cm. The capsule centre, foot IK, crouch and slide heights, and ledge-grab reach constants in DummyLocomotor/PlayerMotor need re-tuning.
- Arm reach drops by about 8.5 cm, which changes climb-up and ledge-grab hand targets.
- I found something that isn't a proportion issue: the sole and heel bottoms sit about 4 cm below `Root` (z = 0), at -0.041 and -0.044. Check whether the feet clip into the floor.

## Caveats
- MediaPipe world landmarks are a learned prior trained on average adults, and its hip and shoulder landmarks don't sit exactly on joint centres. That's why the Storror torso reads longer, and the upper arm shorter, than Winter. The recommendations use the values where Storror and Winter agree on direction rather than copying MediaPipe 1:1. The raw 1:1 deltas are in `proportions.json` for reference.
- The head height estimate (ear breadth × 1.59) is the weakest number here.
- These are suggestions only. The Hier model was not edited.

## Typical joint ranges from the clips (p2 to p98, median; degrees)
| Joint | All clips | Notes |
|---|---|---|
| Knee flex | 8 to 124 (49) | soft-land p98 127, wall jump 126, roll 115, slide p98 126 |
| Hip flex | -15 to 134 (50) | wall jump p98 154 (tuck), the 180 lands near 150 |
| Hip elevation | 5 to 132 (54) | 0 = thigh along the torso axis, pointing down |
| Hip lateral abduction | -23 to 45 (6) | |
| Ankle dorsi | -70 to 20 (-20) | neutral standing reads about -15 to -20 with this landmark definition |
| Shoulder elevation | 15 to 145 (63) | vault median 120 (arms overhead on hand plant), sprint 39 |
| Shoulder flex | -74 to 189 (25) | over 180 = overhead and past vertical (unwrapped); the vertical wall run reaches about 220 |
| Elbow flex | 7 to 99 (48) | mantle median 65 |
| Spine twist | -37 to 37 (1) | the 180 is a whole-body yaw, not a spine twist |
| Spine lateral bend | -18 to 36 (5) | |
| Neck flex | -56 to 78 (17) | noisy; neck_lateral is unreliable at these resolutions |
Per-verb tables are in `proportions.json` → `rom_deg_by_verb`.
