# HiPoly Hierarchical Mannequins

DummyLocomotor-bindable curved crash-test dummies (Navy Spade soft foam + polymer panels + matte rubber joints).

## Assets
| File | Paint |
|------|-------|
| `Dummy_Mannequin_Tan_Hier_Hi.fbx` | Runner — Base `#E8D9C0`, Accent `#2BB3A3` chest band + limb stripes |
| `Dummy_Mannequin_Orange_Hier_Hi.fbx` | It — Base `#FF6A00`, Accent black nested downward-V chevrons chest + outer thighs |

## Bind pose
- **Mild A-pose** — upper arms ~25–30° off torso, elbows soft, wrists neutral.
- Hands / forearms **clear pelvis / butt** (no V-into-butt).
- Mitten hands; egg head + black sensor dots only — **no visor**.

## Bone hierarchy (DummyLocomotor — names unchanged)
`Root` → `Hips` → `Spine` → `Chest` → `Neck` → `Head`  
`Hips` → `UpperLeg_L/R` → `LowerLeg_L/R` → `Foot_L/R`  
`Chest` → `Shoulder_L/R` → `UpperArm_L/R` → `LowerArm_L/R` → `Hand_L/R`

Required aliases present: `Hips`, `Spine`, `Head`, `UpperArm_*`, `LowerArm_*`, `UpperLeg_*`, `LowerLeg_*`.  
**LowerLeg is a real bend joint under UpperLeg** (knee hinge readable).

## Mat slots
`Base`, `Accent`, `ItOverride` (match Dummy_Runner / Dummy_It).

## Export
`-Z` forward, `+Y` up. Rebuild: Blender 4.x  
`blender -b -P /workspace/art-build/scripts/build_mannequin_hier_v2.py`

## Stills
`/workspace/art-build/previews/hipoly_*.png` — idle front/3-4, run knee, slide crouch, punch, It idle.
