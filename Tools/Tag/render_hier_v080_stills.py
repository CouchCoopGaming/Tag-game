#!/usr/bin/env python3
"""Before/after Hier stills and one pose from each clean Storror clip.

Run from Blender for the mesh renders, then from system Python to composite:

  blender -b /tmp/Dummy_Mannequin_Hier_Hi_v080_Tan.blend -P Tools/Tag/render_hier_v080_stills.py -- --new
  blender -b -P Tools/Tag/render_hier_v080_stills.py -- --old
  python3 Tools/Tag/render_hier_v080_stills.py --composite
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

OUT = "/workspace/Docs/HierStills/v080"
PREV = "/tmp/hier_v080_stills"
OLD_FBX = "/tmp/hier_v078_tan.fbx"
NEW_BLEND = "/tmp/Dummy_Mannequin_Hier_Hi_v080_Tan.blend"

# Same camera for old and new so the composite is one scale.
CAMS = {
    "front": ((0.05, -4.55, 0.98), (0.0, 0.0, 0.90)),
    "threequarter": ((2.35, -3.15, 1.05), (0.0, 0.0, 0.90)),
    "side": ((4.55, 0.20, 0.98), (0.0, 0.0, 0.90)),
}


def _args():
    if "--" in sys.argv:
        return sys.argv[sys.argv.index("--") + 1:]
    return sys.argv[1:]


def composite():
    from PIL import Image, ImageDraw, ImageFont
    os.makedirs(OUT, exist_ok=True)
    font = ImageFont.load_default()

    def label(im, text):
        d = ImageDraw.Draw(im)
        d.rectangle((16, 16, 16 + 8 * len(text) + 16, 40), fill=(20, 18, 16, 230))
        d.text((26, 20), text, fill=(245, 236, 220, 255), font=font)
        return im

    def pair(name):
        old = Image.open(os.path.join(PREV, f"old_{name}.png")).convert("RGBA")
        new = Image.open(os.path.join(PREV, f"new_{name}.png")).convert("RGBA")
        # Identical resolution and camera: concatenating keeps the scale.
        w, h = old.size
        canvas = Image.new("RGBA", (w * 2 + 24, h), (48, 44, 40, 255))
        canvas.paste(label(old, "v0.7.8"), (0, 0))
        canvas.paste(label(new, "v0.8.0"), (w + 24, 0))
        path = os.path.join(OUT, f"{name}.png")
        canvas.convert("RGB").save(path)
        print("wrote", path)

    for name in ("front", "threequarter", "side"):
        pair(name)

    old = Image.open(os.path.join(PREV, "old_front.png")).convert("RGBA")
    new = Image.open(os.path.join(PREV, "new_front.png")).convert("RGBA")
    # Old in a cool tint, new in the vinyl color, same pixels, same scale.
    tint = Image.new("RGBA", old.size, (70, 110, 170, 0))
    old_px = list(old.get_flattened_data()) if hasattr(old, "get_flattened_data") else list(old.getdata())
    tinted = []
    for r, g, b, a in old_px:
        tinted.append((int(r * 0.45 + 40), int(g * 0.55 + 50), int(b * 0.75 + 70), a))
    old_t = Image.new("RGBA", old.size)
    old_t.putdata(tinted)
    over = Image.blend(old_t, new, 0.55)
    label(over, "overlay  v0.7.8 cool  /  v0.8.0")
    path = os.path.join(OUT, "overlay.png")
    over.convert("RGB").save(path)
    print("wrote", path)

    # Clip poses are already framed on the new model.
    for name in (
        "pose_03_softland",
        "pose_04_softland",
        "pose_10_wallrun",
        "pose_12_tictac",
        "pose_15_catleap",
    ):
        src = os.path.join(PREV, name + ".png")
        if not os.path.isfile(src):
            print("missing", src)
            continue
        im = Image.open(src).convert("RGB")
        dst = os.path.join(OUT, name + ".png")
        im.save(dst)
        print("wrote", dst)


def composite_anim():
    """Stick cell from the Storror strip beside the matching Hier pose."""
    from PIL import Image, ImageDraw, ImageFont
    import storror_pose as sp

    dest = os.path.join(OUT, "anim")
    os.makedirs(dest, exist_ok=True)
    font = ImageFont.load_default()
    strips = os.path.join(HERE, "..", "..", "Docs", "Storror", "out", "strips")
    names = {
        "03_wall_drop_softland": ("pose_03_softland", "03 soft land"),
        "04_window_drop_softland": ("pose_04_softland", "04 soft land"),
        "10_wallrun_slanted": ("pose_10_wallrun", "10 wall run"),
        "12_tictac_slanted_wall": ("pose_12_tictac", "12 tic-tac"),
        "15_cat_leap_wall": ("pose_15_catleap", "15 cat leap"),
    }
    for spec in sp.CLIPS:
        pose_name, label = names[spec["id"]]
        curves = sp.clip_curves(spec["id"], keys=24)
        u, idx = sp.hero_u(spec["id"], curves["raw_smooth"])
        cell = int(round(u * 11))
        if cell < 0:
            cell = 0
        if cell > 11:
            cell = 11
        strip = Image.open(os.path.join(strips, spec["id"] + ".png")).convert("RGB")
        # 26 px caption, then 12 cells of 160 x 300.
        x0 = cell * 160
        stick = strip.crop((x0, 26, x0 + 160, 26 + 300))
        hier = Image.open(os.path.join(OUT, pose_name + ".png")).convert("RGB")
        stick = stick.resize((int(160 * hier.height / 300), hier.height), Image.Resampling.NEAREST)
        canvas = Image.new("RGB", (stick.width + hier.width + 24, hier.height), (48, 44, 40))
        canvas.paste(stick, (0, 0))
        canvas.paste(hier, (stick.width + 24, 0))
        d = ImageDraw.Draw(canvas)
        text = label + "   strip frame " + str(idx)
        d.rectangle((16, 16, 16 + 8 * len(text) + 16, 40), fill=(20, 18, 16))
        d.text((26, 20), text, fill=(245, 236, 220), font=font)
        path = os.path.join(dest, pose_name + ".png")
        canvas.save(path)
        print("wrote", path)


def render_new():
    import bpy
    from mathutils import Vector, Euler
    import storror_pose as sp

    os.makedirs(PREV, exist_ok=True)
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    _prepare_scene()
    _render_views(arm, "new")
    for spec in sp.CLIPS:
        curves = sp.clip_curves(spec["id"], keys=24)
        u, idx = sp.hero_u(spec["id"], curves["raw_smooth"])
        ch = sp.sample_raw(curves["raw_smooth"], u)
        pose = sp.unity_pose(ch)
        eulers = sp.blender_euler(pose)
        _pose_still(arm, spec, u, idx)
    # Leave the file at rest. Do not save over the blend.


def render_poses():
    import bpy
    import storror_pose as sp

    os.makedirs(PREV, exist_ok=True)
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    _prepare_scene()
    for spec in sp.CLIPS:
        curves = sp.clip_curves(spec["id"], keys=24)
        u, idx = sp.hero_u(spec["id"], curves["raw_smooth"])
        ch = sp.sample_raw(curves["raw_smooth"], u)
        pose = sp.unity_pose(ch)
        eulers = sp.blender_euler(pose)
        _pose_still(arm, spec, u, idx)


def render_old():
    import bpy
    os.makedirs(PREV, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=OLD_FBX, automatic_bone_orientation=False, global_scale=1.0)
    arm = None
    for o in bpy.data.objects:
        if o.type == "ARMATURE":
            arm = o
            break
    if arm is None:
        raise SystemExit("old FBX imported no armature")
    _prepare_scene()
    _render_views(arm, "old")


def _prepare_scene():
    import bpy
    sc = bpy.context.scene
    engines = {e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items}
    if "BLENDER_EEVEE" in engines:
        sc.render.engine = "BLENDER_EEVEE"
        sc.eevee.taa_render_samples = 28
    else:
        sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x = 900
    sc.render.resolution_y = 1200
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.film_transparent = False
    world = bpy.data.worlds.get("StillWorld") or bpy.data.worlds.new("StillWorld")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.55, 0.50, 0.46, 1.0)
        bg.inputs[1].default_value = 0.65
    if "Ground" not in bpy.data.objects:
        bpy.ops.mesh.primitive_plane_add(size=12, location=(0, 0, 0))
        g = bpy.context.active_object
        g.name = "Ground"
        m = bpy.data.materials.new("GroundMat")
        m.use_nodes = True
        m.diffuse_color = (0.32, 0.30, 0.28, 1)
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.32, 0.30, 0.28, 1)
            bsdf.inputs["Roughness"].default_value = 0.92
        g.data.materials.append(m)
    if "KeySun" not in bpy.data.objects:
        bpy.ops.object.light_add(type="SUN", location=(2.4, -3.2, 6.0))
        sun = bpy.context.active_object
        sun.name = "KeySun"
        sun.data.energy = 3.4
        sun.data.color = (1.0, 0.90, 0.78)
        sun.rotation_euler = (math.radians(50), math.radians(8), math.radians(-24))
        bpy.ops.object.light_add(type="AREA", location=(-2.4, 1.6, 3.0))
        fill = bpy.context.active_object
        fill.name = "Fill"
        fill.data.energy = 80
        fill.data.size = 4.0


def _shot(path, loc, look):
    import bpy
    from mathutils import Vector
    if "ShotCam" in bpy.data.objects:
        cam = bpy.data.objects["ShotCam"]
    else:
        bpy.ops.object.camera_add()
        cam = bpy.context.active_object
        cam.name = "ShotCam"
        cam.data.lens = 48
    cam.location = loc
    direction = Vector(look) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("rendered", path)


def _render_views(arm, tag):
    _reset(arm)
    for name, (loc, look) in CAMS.items():
        _shot(os.path.join(PREV, f"{tag}_{name}.png"), loc, look)


SLUGS = {
    "03_wall_drop_softland": "pose_03_softland",
    "04_window_drop_softland": "pose_04_softland",
    "10_wallrun_slanted": "pose_10_wallrun",
    "12_tictac_slanted_wall": "pose_12_tictac",
    "15_cat_leap_wall": "pose_15_catleap",
}


def _pose_still(arm, spec, u, idx):
    import storror_pose as sp
    ch = sp.sample_raw(sp.clip_curves(spec["id"], keys=24)["raw_smooth"], u)
    # clip_curves twice is wasteful; caller already sampled. Recompute pose only.
    pose = sp.unity_pose(ch)
    _apply_eulers(arm, sp.blender_euler(pose))
    _present(arm, spec, idx)
    _shot(os.path.join(PREV, SLUGS[spec["id"]] + ".png"), *CAMS["threequarter"])
    print(f"{SLUGS[spec['id']]} hero_u={u:.3f} frame={idx} verb={spec['verb']}")
    _reset(arm)


# Near face of the action wall. The mannequin stays on the +X side of this plane.
WALL_FACE = -0.58


def _present(arm, spec, frame_idx):
    """Pitch from the camera-up trunk lean, then seat the contact on the ground or a wall.

    Bone curves stay in torso space. Positive hip flexion already kicks the knee
    toward the face (-Y). A negative yaw swings that forward limb onto the wall
    at -X and leaves the hips on the near side. Positive yaw did the opposite
    and pushed the pelvis through the wall.
    """
    import json
    import bpy
    from mathutils import Euler
    path = os.path.abspath(os.path.join(HERE, "..", "..", "Docs", "Storror", "out", "json", spec["id"] + ".json"))
    with open(path) as f:
        ang = json.load(f)["joint_angles_deg"][frame_idx] or {}
    trunk = float(ang.get("trunk_lean_from_cam_vertical") or 0.0)
    verb = spec["verb"]
    _hide_wall()
    arm.location = (0.0, 0.0, 0.0)
    pitch = trunk
    roll = 0.0
    yaw = 0.0
    if verb == "softland":
        pitch = min(trunk, 16.0)
    elif verb == "wallrun":
        # Feet roll toward -X. Face stays toward the camera, running along the wall.
        pitch = min(max(trunk, 8.0), 20.0)
        roll = 50.0
    elif verb == "walljump":
        pitch = min(max(trunk, 18.0), 40.0)
        yaw = -64.0
    else:
        pitch = min(max(trunk, 28.0), 50.0)
        yaw = -72.0
    arm.rotation_euler = Euler((math.radians(pitch), math.radians(roll), math.radians(yaw)), "XYZ")
    bpy.context.view_layer.update()
    if verb == "softland":
        _plant(arm)
        return
    if verb == "wallrun":
        _seat_side_wall(arm, contact_z=0.72, contact="feet")
    elif verb == "walljump":
        _seat_side_wall(arm, contact_z=1.05, contact="feet")
    else:
        _seat_side_wall(arm, contact_z=1.48, contact="hands")


def _bone_world(arm, name):
    pb = arm.pose.bones.get(name)
    if pb is None:
        return None
    return arm.matrix_world @ pb.head


def _seat_side_wall(arm, contact_z, contact="feet"):
    """Put the forward contact on the wall face and keep the hips on the near side."""
    import bpy
    from mathutils import Vector
    bpy.context.view_layer.update()
    if contact == "hands":
        names = ("Hand_L", "Hand_R")
    else:
        names = ("Foot_L", "Foot_R")
    pts = [(n, p) for n in names if (p := _bone_world(arm, n)) is not None]
    if not pts:
        return
    # After the yaw, the limb that reaches toward -X is the one that hits the wall.
    name, outer = min(pts, key=lambda item: item[1].x)
    delta = Vector((WALL_FACE + 0.02 - outer.x, 0.0, contact_z - outer.z))
    arm.location += delta
    bpy.context.view_layer.update()
    feet = [p for p in (_bone_world(arm, "Foot_L"), _bone_world(arm, "Foot_R")) if p is not None]
    if feet:
        lower = min(feet, key=lambda p: p.z)
        if lower.z < 0.10:
            arm.location.z += 0.10 - lower.z
            bpy.context.view_layer.update()
    # Keep a raised hand inside the still frame. Do not drop a kick onto the ground.
    tops = [p.z for n in ("Head", "Hand_L", "Hand_R") if (p := _bone_world(arm, n)) is not None]
    if tops and max(tops) > 1.70:
        arm.location.z -= max(tops) - 1.70
        bpy.context.view_layer.update()
        feet = [p for p in (_bone_world(arm, "Foot_L"), _bone_world(arm, "Foot_R")) if p is not None]
        if feet:
            lower = min(feet, key=lambda p: p.z)
            if lower.z < 0.28:
                arm.location.z += 0.28 - lower.z
                bpy.context.view_layer.update()
    hips = _bone_world(arm, "Hips")
    if hips is not None and hips.x < WALL_FACE + 0.10:
        arm.location.x += (WALL_FACE + 0.10) - hips.x
        bpy.context.view_layer.update()
    hips = _bone_world(arm, "Hips")
    foot_l = _bone_world(arm, "Foot_L")
    foot_r = _bone_world(arm, "Foot_R")
    print(
        f"  seat contact={name} hips=({hips.x:+.2f},{hips.z:.2f}) "
        f"feet=({foot_l.x:+.2f},{foot_l.z:.2f}) ({foot_r.x:+.2f},{foot_r.z:.2f})"
    )
    _show_wall(WALL_FACE)


def _show_wall(face_x):
    import bpy
    # Default cube is 2 m. scale 0.04 → 8 cm thick. The +X face is the contact face.
    half = 0.04
    if "ActionWall" in bpy.data.objects:
        wall = bpy.data.objects["ActionWall"]
    else:
        bpy.ops.mesh.primitive_cube_add(location=(face_x - half, 0.0, 1.40))
        wall = bpy.context.active_object
        wall.name = "ActionWall"
        mat = bpy.data.materials.new("WallMat")
        mat.use_nodes = True
        mat.diffuse_color = (0.62, 0.60, 0.57, 1)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.62, 0.60, 0.57, 1)
            bsdf.inputs["Roughness"].default_value = 0.88
        wall.data.materials.append(mat)
    wall.location = (face_x - half, 0.05, 1.40)
    wall.scale = (0.04, 1.60, 1.55)
    wall.hide_render = False
    wall.hide_set(False)


def _hide_wall():
    import bpy
    wall = bpy.data.objects.get("ActionWall")
    if wall is not None:
        wall.hide_render = True
        wall.hide_set(True)


def _reset(arm):
    import bpy
    arm.location = (0.0, 0.0, 0.0)
    arm.rotation_euler = (0.0, 0.0, 0.0)
    _hide_wall()
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()


def _plant(arm):
    """Slide the posed figure so the lowest foot vertex sits on z = 0."""
    import bpy
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    lowest = None
    for obj in bpy.data.objects:
        if obj.type != "MESH" or "Foot" not in obj.name:
            continue
        ev = obj.evaluated_get(deps)
        mw = ev.matrix_world
        for v in ev.data.vertices:
            z = (mw @ v.co).z
            if lowest is None or z < lowest:
                lowest = z
    if lowest is None:
        return
    arm.location.z -= lowest
    bpy.context.view_layer.update()


def _apply_eulers(arm, eulers):
    import bpy
    import math
    from mathutils import Euler
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0, 0, 0)
    for name, deg in eulers.items():
        pb = arm.pose.bones.get(name)
        if pb is None:
            print("missing bone", name)
            continue
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = Euler(tuple(math.radians(a) for a in deg), "XYZ")
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()


if __name__ == "__main__":
    args = _args()
    if "--anim" in args:
        composite_anim()
    elif "--composite" in args or ("bpy" not in sys.modules and "--new" not in args and "--old" not in args and "--poses" not in args):
        composite()
    elif "--old" in args:
        render_old()
    elif "--poses" in args:
        render_poses()
    else:
        render_new()
