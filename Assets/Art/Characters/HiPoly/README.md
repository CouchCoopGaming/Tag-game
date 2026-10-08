# HiPoly Hierarchical Mannequins

DummyLocomotor-bindable **Hybrid III hard-shell** crash-test dummies (v0.8.0).
Storror proportions: longer torso, shorter thigh and forearm, narrower shoulders, smaller head.
Soles rest on z = 0. Resting-fist finger curl and shoulders-in joints are unchanged.
Bone names and the hierarchy match v0.7.8.

## Assets
| File | Paint |
|------|-------|
| `Dummy_Mannequin_Tan_Hier_Hi.fbx` | Runner — cream `#E8D9C0`, Accent teal tick. **ZERO nested Vs.** |
| `Dummy_Mannequin_Blue_Hier_Hi.fbx` | Runner swatch — blue body, blue panel accent |
| `Dummy_Mannequin_Mint_Hier_Hi.fbx` | Runner swatch — mint body, mint panel accent |
| `Dummy_Mannequin_Lavender_Hier_Hi.fbx` | Runner swatch — lavender body, lavender panel accent |
| `Dummy_Mannequin_Red_Hier_Hi.fbx` | Runner swatch — red body, red panel accent |
| `Dummy_Mannequin_Orange_Hier_Hi.fbx` | It — warm tan body + black nested Vs chest + outer thighs (NOT #FF6A00) |

## Bind pose
- Mild A-pose ~20–35°; hands clear pelvis.
- Human head scale + molded face; flat dark eye insets — zero orbs / tip stacks.
- Flat chest plate (narrower); dense accordion bellows; hard pelvis w/ mild hip curve.
- Hybrid III limb shell mass; tiny dark Bionicle joints under SOLID vinyl capsule; hard-shell hands w/ soft resting-fist curl + knuckles/mid joints (no Finger_ bones).
- Materials: satin vinyl Base / Accent / ItOverride + Joint metal + Rubber + Bellows.

## Bone hierarchy (DummyLocomotor — names unchanged)
`Root` → `Hips` → `Spine` → `Chest` → `Neck` → `Head`  
`Hips` → `UpperLeg_L/R` → `LowerLeg_L/R` → `Foot_L/R`  
`Chest` → `Shoulder_L/R` → `UpperArm_L/R` → `LowerArm_L/R` → `Hand_L/R`

## Export
`-Z` forward, `+Y` up. Materials: `Base`, `Accent`, `ItOverride`.
