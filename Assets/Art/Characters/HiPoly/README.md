# HiPoly (Blender 5.2)
High-subdivision crash dummies / mannequins.

## Prefer hierarchical for limb anim
- `Dummy_Mannequin_<Color>_Hier_Hi.fbx` — parented empties (Hips/Spine/Head/UpperArm_*/LowerArm_*/UpperLeg_*/LowerLeg_*) for `DummyLocomotor`.
- Flat `Dummy_Mannequin_<Color>_Hi.fbx` / `Dummy_Runner_Hi.fbx` / `Dummy_It_Hi.fbx` — sibling meshes only (no limb hierarchy).

`DummyAvatarBinder` prefers `*_Hier_Hi`, then flat HiPoly, then Navy Spade primitive when `HasBindableBones` fails.

Rebuild: Blender 5.2 `--background --python C:\Users\Zubal\Dev\_ororo_scratch\Tag\hipoly\build_mannequin_hier.py`
Colors: Blue, Mint, Orange, Lavender, Tan, Red. Export `-Z` forward, `Y` up.
