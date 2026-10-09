# Player queue

Models player sub-lead. Draft PR #128 only. The clearance rig stays unbound until Landon accepts the stills. Costumes helper is #131 (`cursor/tag-character-costumes`). Do not refit costumes until the hip flex range is clean.

Lead grade on `35085dbf`: `models-validate assets=7 pass=0 fail=7`. Proof `pose=152 rigJoint=0 worldMax=5.51cm selfMax=4.00cm hip=10 ankle=8 knee=3`. The candidate hip is clear at rest, fails from about 9° through 80°, and is clear again at 110° and 130°.

Hip work in `Tools/Tag/clear_hip_flex.py` is not on the candidate yet. In memory, rest stays `rigJoint=0`, the right leg is clear through 120° flexion, and the left leg fails only at 50° (0.78 cm) and 55° (0.87 cm). Twist of 25° still leaves the pelvis 2.05 cm inside the thigh. An FBX round trip reopens `Mesh_UpperLeg_R>Mesh_LowerLeg_R` at 2.54 cm, so the file is not exported over the clearance rig. The shipped mannequin is untouched. Nothing is bound into #118.

## Order

1. Hips on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`. `pose` 0 through the flex range. No mid-flex window. Close the 50–55° left wedge and the ±25° twist, and keep that clear after export.
2. Hip-sit on loaded plants, landings, and a crouch. Pelvis at least 8 cm behind the support foot on plants and landings, 12 cm in a crouch. Hip flexion at least 1.5× spine flexion. Support knee at least 25° (45° on landings and crouches) and over or ahead of the ankle. Pelvis drop at least 8 cm (20 cm on landings and crouches).
3. Ankles (`ankle=8`), then the knee at 155° (0.97 cm over). Neck (2) in that same pose pass.
4. One CC0-1.0 license row per Hier file, and the still quartet at 1280×720 or larger, each under 400 KB, in `Docs/LocoStills/passN/`: quarter, side, close-up, 1.8 m scale figure.

## Helper

#131 refits the 12 costumes to this rig after the flex range is clean. LOD1/LOD2, joint close-up, scale still. Bram stays a Reed tint until then.
