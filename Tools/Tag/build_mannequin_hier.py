"""
Build hierarchical Navy Spade crash-dummy mannequin for Tag DummyLocomotor.
Parented empties/meshes with exact bone names — not flat sibling meshes.
Exports Dummy_Mannequin_<Color>_Hier_Hi.fbx (-Z forward, Y up).

Polish pass: slick curved crash-dummy (Navy Spade polymer) — rounded torso
shells (spheres/cylinders, not cubes), higher subsurf, thin inset panels,
featureless egg head, black rubber joints. Arms hang with slight outward A-pose
matching DummyPrimitiveFactory (no Y-twist that V's hands into the butt).
"""
import bpy
import math
import os
import shutil

OUT = r"C:\Users\Zubal\Dev\_ororo_scratch\Tag\hipoly"
CHARS = r"C:\Users\Zubal\Dev\Tag-game-playtest\Assets\Art\Characters\HiPoly"
os.makedirs(OUT, exist_ok=True)
os.makedirs(CHARS, exist_ok=True)

# Soft crash-dummy body foam (slightly desaturated so panels read)
COLORS = {
    "Blue": (0.42, 0.68, 0.92),
    "Mint": (0.42, 0.82, 0.70),
    "Orange": (0.94, 0.42, 0.14),
    "Lavender": (0.70, 0.58, 0.88),
    "Tan": (0.90, 0.76, 0.52),
    "Red": (0.88, 0.22, 0.24),
}
# Saturated polymer accent panels (Navy Spade crash-dummy inlays)
PANELS = {
    "Blue": (0.08, 0.32, 0.78),
    "Mint": (0.06, 0.58, 0.48),
    "Orange": (1.00, 0.48, 0.05),
    "Lavender": (0.48, 0.28, 0.82),
    "Tan": (0.10, 0.48, 0.68),
    "Red": (0.72, 0.06, 0.10),
}
# Near-black matte rubber (joints / chest core / hands / feet)
JOINT = (0.02, 0.02, 0.025)
SENSOR = (0.20, 0.90, 1.0)


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for b in list(bpy.data.meshes):
        bpy.data.meshes.remove(b)
    for b in list(bpy.data.materials):
        bpy.data.materials.remove(b)
    for b in list(bpy.data.objects):
        bpy.data.objects.remove(b, do_unlink=True)


def mat(name, rgb, rough=0.42):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = rough
        # Prefer non-metal polymer / rubber response when the socket exists
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = 0.0
        if "Specular IOR Level" in bsdf.inputs:
            # Rubber absorbs a bit more; polymer keeps a cleaner highlight
            bsdf.inputs["Specular IOR Level"].default_value = 0.25 if rough >= 0.75 else 0.45
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = 0.25 if rough >= 0.75 else 0.45
    return m


def apply_mods(obj, levels=1, bevel_w=0.008):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if bevel_w and bevel_w > 0:
        b = obj.modifiers.new("Bevel", "BEVEL")
        b.width = bevel_w
        b.segments = 3
        b.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier=b.name)
    if levels and levels > 0:
        s = obj.modifiers.new("Subsurf", "SUBSURF")
        s.levels = levels
        s.render_levels = levels
        bpy.ops.object.modifier_apply(modifier=s.name)
    bpy.ops.object.shade_smooth()


def empty(name, parent, world_loc):
    """Create empty at world location, parented (keep world)."""
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=world_loc)
    o = bpy.context.active_object
    o.name = name
    o.empty_display_size = 0.08
    if parent is not None:
        o.parent = parent
        o.matrix_parent_inverse = parent.matrix_world.inverted()
    return o


def mesh_prim(kind, name, parent, world_loc, scale, material, levels=1, bevel_w=0.006, segs=20):
    """Create mesh at world location, parent under parent (keep world)."""
    if kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(
            segments=segs, ring_count=max(16, segs // 2), radius=0.5, location=world_loc
        )
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=segs, radius=0.5, depth=1.0, location=world_loc
        )
    elif kind == "capsule":
        # cylinder + mild subsurf reads as soft crash-dummy limb
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=segs, radius=0.5, depth=1.0, location=world_loc
        )
    else:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=world_loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    if material is not None:
        o.data.materials.append(material)
    apply_mods(o, levels=levels, bevel_w=bevel_w)
    if parent is not None:
        o.parent = parent
        o.matrix_parent_inverse = parent.matrix_world.inverted()
    return o


def export_fbx(name):
    path = os.path.join(OUT, f"{name}.fbx")
    bpy.ops.object.select_all(action="DESELECT")
    for o in bpy.context.scene.objects:
        if o.type in {"MESH", "EMPTY"}:
            o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        apply_unit_scale=True,
        use_mesh_modifiers=True,
        object_types={"EMPTY", "MESH"},
        bake_space_transform=False,
    )
    dest = os.path.join(CHARS, f"{name}.fbx")
    # Prefer overwrite without quitting Unity; retry briefly if file is locked
    last_err = None
    for attempt in range(8):
        try:
            shutil.copy2(path, dest)
            last_err = None
            break
        except OSError as e:
            last_err = e
            import time
            time.sleep(0.75)
    if last_err is not None:
        raise last_err
    names = {o.name: (o.parent.name if o.parent else None) for o in bpy.context.scene.objects}
    required = [
        "Hips",
        "Spine",
        "Head",
        "UpperArm_L",
        "LowerArm_L",
        "UpperArm_R",
        "LowerArm_R",
        "UpperLeg_L",
        "LowerLeg_L",
        "UpperLeg_R",
        "LowerLeg_R",
    ]
    missing = [n for n in required if n not in names]
    checks = [
        ("LowerArm_L", "UpperArm_L"),
        ("LowerArm_R", "UpperArm_R"),
        ("UpperArm_L", "Spine"),
        ("UpperArm_R", "Spine"),
        ("LowerLeg_L", "UpperLeg_L"),
        ("LowerLeg_R", "UpperLeg_R"),
        ("UpperLeg_L", "Hips"),
        ("UpperLeg_R", "Hips"),
    ]
    bad = []
    for child, parent in checks:
        if names.get(child) != parent:
            bad.append(f"{child}->parent={names.get(child)} (want {parent})")
    print("EXPORTED", dest)
    print("HIER_OK", missing == [] and bad == [], "missing", missing, "bad", bad)
    return missing == [] and bad == []


def build_mannequin(color_name):
    reset()
    body_rgb = COLORS[color_name]
    panel_rgb = PANELS[color_name]
    body = mat(f"Body_{color_name}", body_rgb, 0.40)
    panel = mat(f"Panel_{color_name}", panel_rgb, 0.26)  # slick polymer
    joint = mat("Joint", JOINT, 0.90)  # matte black rubber
    sensor = mat("Sensor", SENSOR, 0.22)

    root = empty("DummyRoot", None, (0.0, 0.0, 0.0))

    # ---- torso: rounded rubber shells + thin curved polymer inlays (not cubes) ----
    # Chest as squashed sphere reads as crash-dummy shell; pelvis as soft capsule.
    mesh_prim("sphere", "ChestPlate", root, (0.0, 0.02, 1.26), (0.50, 0.30, 0.48), joint, 1, 0.012, segs=24)
    mesh_prim("sphere", "PelvisMesh", root, (0.0, 0.0, 0.90), (0.44, 0.30, 0.20), joint, 1, 0.01, segs=20)
    # Thin polymer panels — high bevel/subsurf so edges stay rounded
    mesh_prim("sphere", "Panel_Chest", root, (0.0, 0.16, 1.30), (0.36, 0.05, 0.24), panel, 1, 0.01, segs=20)
    mesh_prim("sphere", "Panel_Abs", root, (0.0, 0.15, 1.06), (0.30, 0.045, 0.15), panel, 1, 0.008, segs=24)
    mesh_prim("sphere", "Panel_Back", root, (0.0, -0.15, 1.24), (0.38, 0.05, 0.32), body, 1, 0.01, segs=20)
    mesh_prim("sphere", "Panel_Side_L", root, (-0.26, 0.0, 1.20), (0.06, 0.18, 0.26), panel, 1, 0.008, segs=24)
    mesh_prim("sphere", "Panel_Side_R", root, (0.26, 0.0, 1.20), (0.06, 0.18, 0.26), panel, 1, 0.008, segs=24)

    # Featureless egg head + black rubber neck collar
    head = empty("Head", root, (0.0, 0.0, 1.66))
    mesh_prim("sphere", "HeadMesh", head, (0.0, 0.0, 1.66), (0.34, 0.34, 0.38), body, 1, 0.01, segs=24)
    mesh_prim("cylinder", "Neck", root, (0.0, 0.0, 1.47), (0.12, 0.12, 0.09), joint, 1, 0.005, segs=24)
    mesh_prim("cylinder", "NeckCollar", root, (0.0, 0.0, 1.52), (0.20, 0.20, 0.05), joint, 1, 0.005, segs=24)

    if color_name == "Red":
        mesh_prim("sphere", "Sensor", root, (0.0, 0.12, 1.80), (0.18, 0.035, 0.03), sensor, 1, 0.002, segs=16)

    hips = empty("Hips", root, (0.0, 0.0, 0.92))
    spine = empty("Spine", root, (0.0, 0.0, 1.22))

    def build_arm(side, sx):
        ua = empty(f"UpperArm_{side}", spine, (sx * 0.33, 0.0, 1.32))
        mesh_prim("sphere", f"Shoulder_{side}", ua, (sx * 0.33, 0.0, 1.34), (0.16, 0.16, 0.16), joint, 1, 0.006, segs=20)
        mesh_prim(
            "capsule",
            f"UpperArmMesh_{side}",
            ua,
            (sx * 0.33, 0.0, 1.14),
            (0.125, 0.125, 0.38),
            body,
            1,
            0.006,
            segs=20,
        )
        # Thin curved polymer inlay (sphere squash, not cube)
        mesh_prim(
            "sphere",
            f"UpperArmPanel_{side}",
            ua,
            (sx * 0.33, 0.055, 1.14),
            (0.09, 0.028, 0.24),
            panel,
            1,
            0.004,
            segs=20,
        )
        la = empty(f"LowerArm_{side}", ua, (sx * 0.33, 0.0, 0.94))
        mesh_prim("sphere", f"Elbow_{side}", la, (sx * 0.33, 0.0, 0.94), (0.115, 0.115, 0.115), joint, 1, 0.005, segs=24)
        mesh_prim(
            "capsule",
            f"LowerArmMesh_{side}",
            la,
            (sx * 0.33, 0.0, 0.78),
            (0.10, 0.10, 0.32),
            body,
            1,
            0.005,
            segs=20,
        )
        mesh_prim(
            "sphere",
            f"LowerArmPanel_{side}",
            la,
            (sx * 0.33, 0.045, 0.78),
            (0.075, 0.025, 0.20),
            panel,
            1,
            0.003,
            segs=20,
        )
        hand = empty(f"Hand_{side}", la, (sx * 0.33, 0.0, 0.62))
        mesh_prim("sphere", f"HandMesh_{side}", hand, (sx * 0.33, 0.0, 0.62), (0.10, 0.09, 0.07), joint, 1, 0.005, segs=20)
        # Slight outward A-pose (Blender Y ~ Unity Z after FBX). No twist into butt.
        ua.rotation_euler[1] = math.radians(12.0 if sx < 0 else -12.0)
        return ua

    def build_leg(side, sx):
        ul = empty(f"UpperLeg_{side}", hips, (sx * 0.12, 0.0, 0.92))
        mesh_prim("sphere", f"Hip_{side}", ul, (sx * 0.12, 0.0, 0.92), (0.16, 0.16, 0.16), joint, 1, 0.006, segs=20)
        mesh_prim(
            "capsule",
            f"UpperLegMesh_{side}",
            ul,
            (sx * 0.12, 0.0, 0.68),
            (0.15, 0.15, 0.44),
            body,
            1,
            0.006,
            segs=20,
        )
        mesh_prim(
            "sphere",
            f"ThighPanel_{side}",
            ul,
            (sx * 0.12, 0.06, 0.68),
            (0.11, 0.028, 0.28),
            panel,
            1,
            0.004,
            segs=20,
        )
        ll = empty(f"LowerLeg_{side}", ul, (sx * 0.12, 0.0, 0.46))
        mesh_prim("sphere", f"Knee_{side}", ll, (sx * 0.12, 0.0, 0.46), (0.12, 0.12, 0.12), joint, 1, 0.005, segs=24)
        mesh_prim(
            "capsule",
            f"LowerLegMesh_{side}",
            ll,
            (sx * 0.12, 0.0, 0.26),
            (0.115, 0.115, 0.38),
            body,
            1,
            0.005,
            segs=20,
        )
        mesh_prim(
            "sphere",
            f"ShinPanel_{side}",
            ll,
            (sx * 0.12, 0.05, 0.26),
            (0.085, 0.025, 0.24),
            panel,
            1,
            0.003,
            segs=20,
        )
        foot = empty(f"Foot_{side}", ll, (sx * 0.12, 0.06, 0.08))
        mesh_prim("sphere", f"FootMesh_{side}", foot, (sx * 0.12, 0.06, 0.08), (0.13, 0.24, 0.065), joint, 1, 0.005, segs=20)
        return ul

    build_arm("L", -1)
    build_arm("R", 1)
    build_leg("L", -1)
    build_leg("R", 1)

    ok = export_fbx(f"Dummy_Mannequin_{color_name}_Hier_Hi")
    return ok


def main():
    results = {}
    for cname in COLORS:
        print("BUILD", cname)
        results[cname] = build_mannequin(cname)
    print("ALL_DONE", results)
    if not all(results.values()):
        raise SystemExit(1)


if __name__ == "__main__":
    main()
