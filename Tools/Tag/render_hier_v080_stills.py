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
# Pose shots include a raised hand and a wall, so the frame sits higher than the idle.
POSE_CAM = ((2.70, -3.85, 1.30), (-0.10, 0.05, 1.20))


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


def render_solve():
    """Pose the three wall clips and print the contact solve. No pixels."""
    import bpy
    import storror_pose as sp

    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    for spec in sp.CLIPS:
        if spec["verb"] == "softland":
            continue
        curves = sp.clip_curves(spec["id"], keys=24)
        u, idx = sp.hero_u(spec["id"], curves["raw_smooth"])
        ch = sp.sample_raw(curves["raw_smooth"], u)
        pose = sp.unity_pose(ch)
        _apply_eulers(arm, sp.blender_euler(pose))
        _present(arm, spec, idx, pose)
        print(f"  {spec['id']} u={u:.3f} frame={idx}")
        _reset(arm)


def _motion_us(hero):
    """Five samples across the clip, with the hero frame kept in the strip."""
    samples = [0.08, 0.28, 0.50, 0.72, 0.92]
    nearest = min(range(len(samples)), key=lambda i: abs(samples[i] - hero))
    samples[nearest] = round(hero, 3)
    samples.sort()
    return samples


def render_motion():
    """Five frames across each clip, same wall solve, smaller stills for the strip."""
    import bpy
    import storror_pose as sp

    dest = os.path.join(PREV, "motion")
    os.makedirs(dest, exist_ok=True)
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    _prepare_scene()
    sc = bpy.context.scene
    sc.render.resolution_x = 480
    sc.render.resolution_y = 640
    if hasattr(sc, "eevee"):
        sc.eevee.taa_render_samples = 16
    for spec in sp.CLIPS:
        curves = sp.clip_curves(spec["id"], keys=24)
        n = curves["frames"]
        hero, _ = sp.hero_u(spec["id"], curves["raw_smooth"])
        for i, u in enumerate(_motion_us(hero)):
            ch = sp.sample_raw(curves["raw_smooth"], u)
            pose = sp.unity_pose(ch)
            _apply_eulers(arm, sp.blender_euler(pose))
            idx = int(round(u * (n - 1)))
            if idx < 0:
                idx = 0
            if idx >= n:
                idx = n - 1
            _present(arm, spec, idx, pose, stage=False)
            path = os.path.join(dest, f"{SLUGS[spec['id']]}_{i}.png")
            _shot(path, *POSE_CAM)
            print(f"motion {SLUGS[spec['id']]} {i} u={u:.2f} frame={idx}")
            _reset(arm)


def composite_motion():
    """One horizontal strip of five retargeted frames per clip."""
    from PIL import Image, ImageDraw, ImageFont
    import storror_pose as sp

    dest = os.path.join(OUT, "anim")
    os.makedirs(dest, exist_ok=True)
    font = ImageFont.load_default()
    src = os.path.join(PREV, "motion")
    labels = {
        "03_wall_drop_softland": "03 soft land",
        "04_window_drop_softland": "04 soft land",
        "10_wallrun_slanted": "10 wall run",
        "12_tictac_slanted_wall": "12 tic-tac",
        "15_cat_leap_wall": "15 cat leap",
    }
    for spec in sp.CLIPS:
        slug = SLUGS[spec["id"]]
        frames = []
        for i in range(5):
            frames.append(Image.open(os.path.join(src, f"{slug}_{i}.png")).convert("RGB"))
        w, h = frames[0].size
        gap = 12
        canvas = Image.new("RGB", (w * 5 + gap * 4, h), (48, 44, 40))
        for i, im in enumerate(frames):
            canvas.paste(im, (i * (w + gap), 0))
        d = ImageDraw.Draw(canvas)
        text = labels[spec["id"]] + "   retarget across the clip"
        d.rectangle((16, 16, 16 + 8 * len(text) + 16, 40), fill=(20, 18, 16))
        d.text((26, 20), text, fill=(245, 236, 220), font=font)
        path = os.path.join(dest, f"strip_{slug}.png")
        canvas.save(path)
        print("wrote", path)


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
    _present(arm, spec, idx, pose)
    _shot(os.path.join(PREV, SLUGS[spec["id"]] + ".png"), *POSE_CAM)
    print(f"{SLUGS[spec['id']]} hero_u={u:.3f} frame={idx} verb={spec['verb']}")
    _reset(arm)


# Near face of the action wall. The mannequin stays on the +X side of this plane.
# The wall normal points toward +X. Contact limbs may touch x = WALL_FACE.
# Every other vertex stays at x >= WALL_FACE.
WALL_FACE = -0.58


def _present(arm, spec, frame_idx, pose, stage=True):
    """Orient the clip, then solve the body against the wall plane.

    The retarget stays in bone space. This only moves the visual root (the
    armature object) along the wall normal and leans it about the contact so
    the kicking foot or the hands touch, and the head, chest and arms do not
    cross the plane. The capsule is not involved.
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
    arm.rotation_euler = (0.0, 0.0, 0.0)
    if verb == "softland":
        pitch = min(trunk, 16.0)
        arm.rotation_euler = Euler((math.radians(pitch), 0.0, 0.0), "XYZ")
        bpy.context.view_layer.update()
        _plant(arm)
        return
    if verb == "wallrun":
        # Straighter leg is the plant. Roll tips that side toward the wall.
        side = "L" if abs(pose["knee_l"]) <= abs(pose["knee_r"]) else "R"
        pitch = min(max(trunk, 6.0), 16.0)
        arm.rotation_euler = Euler((math.radians(pitch), math.radians(40.0), math.radians(-15.0)), "XYZ")
        bpy.context.view_layer.update()
        cz = 0.78 if stage else _posed_contact_z(arm, "Foot_" + side, 0.35)
        _solve_wall(arm, "Foot_" + side, contact_z=cz, kind="foot", top=None)
    elif verb == "walljump":
        # Forward kick (the larger thigh pitch) meets the wall at hip height.
        # Yaw turns that kick onto -X. The solve then leans the torso off the plane.
        side = "L" if pose["thigh_l"] >= pose["thigh_r"] else "R"
        arm.rotation_euler = Euler((math.radians(6.0), math.radians(10.0), math.radians(-76.0)), "XYZ")
        bpy.context.view_layer.update()
        cz = 0.95 if stage else _posed_contact_z(arm, "Foot_" + side, 0.35)
        _solve_wall(arm, "Foot_" + side, contact_z=cz, kind="foot", top=None)
    else:
        # Cat leap: both feet on the face, hands on the top edge, chest just clear.
        arm.rotation_euler = Euler((math.radians(16.0), 0.0, math.radians(-78.0)), "XYZ")
        bpy.context.view_layer.update()
        cz = 0.52 if stage else _posed_contact_z(arm, "Foot_L", 0.28)
        _solve_wall(arm, "Foot_L", contact_z=cz, kind="cling", top="hands")


def _posed_contact_z(arm, bone, floor):
    """Height the pose already gave this bone, kept off the ground."""
    import bpy
    bpy.context.view_layer.update()
    p = _bone_world(arm, bone)
    if p is None:
        return floor
    return p.z if p.z > floor else floor


def _bone_world(arm, name):
    pb = arm.pose.bones.get(name)
    if pb is None:
        return None
    return arm.matrix_world @ pb.head


def _mesh_min_x(allow):
    """Minimum world x of meshes that allow(name) accepts. None if none match."""
    import bpy
    deps = bpy.context.evaluated_depsgraph_get()
    best = None
    best_name = None
    for obj in bpy.data.objects:
        if obj.type != "MESH" or obj.name in ("Ground", "ActionWall"):
            continue
        if not allow(obj.name):
            continue
        ev = obj.evaluated_get(deps)
        mw = ev.matrix_world
        for v in ev.data.vertices:
            x = (mw @ v.co).x
            if best is None or x < best:
                best = x
                best_name = obj.name
    return best, best_name


def _mesh_min_z(allow):
    """Minimum world z of meshes that allow(name) accepts."""
    import bpy
    deps = bpy.context.evaluated_depsgraph_get()
    best = None
    for obj in bpy.data.objects:
        if obj.type != "MESH" or obj.name in ("Ground", "ActionWall"):
            continue
        if not allow(obj.name):
            continue
        ev = obj.evaluated_get(deps)
        mw = ev.matrix_world
        for v in ev.data.vertices:
            z = (mw @ v.co).z
            if best is None or z < best:
                best = z
    return best


def _rotate_about(arm, pivot, axis, deg):
    import bpy
    from mathutils import Matrix, Vector
    delta = Matrix.Rotation(math.radians(deg), 4, axis)
    t = Matrix.Translation(Vector(pivot))
    arm.matrix_world = t @ delta @ t.inverted() @ arm.matrix_world
    bpy.context.view_layer.update()


def _solve_wall(arm, contact_bone, contact_z, kind, top):
    """Plant the contact on the plane and push the visual root off the wall.

    kind 'foot' lets that kicking foot touch. kind 'cling' lets both feet touch
    the face and both hands meet the lip. Head, chest and forearms stay on the
    near side. A lean about the contact foot (world Y) is the first correction.
    If something is still through, the root slides along +X and the foot is
    stuck back on the plane. The capsule is not involved.
    """
    import bpy
    bpy.context.view_layer.update()

    def foot_touch(name):
        if kind == "foot":
            return name == "Mesh_" + contact_bone or name.endswith(contact_bone)
        return "Foot" in name

    def blocks(name):
        # Hands are a contact on the cat-leap lip. Everywhere else they count
        # as body and have to stay out of the wall.
        if foot_touch(name):
            return False
        if kind == "cling" and "Hand" in name:
            return False
        return True

    def stick_feet():
        cx, _ = _mesh_min_x(foot_touch)
        if cx is not None:
            arm.location.x += WALL_FACE - cx
            bpy.context.view_layer.update()

    pivot = _bone_world(arm, contact_bone)
    if pivot is None:
        _show_wall(WALL_FACE, None)
        return
    # Drop or lift the contact to the requested height, then stick its mesh to the face.
    arm.location.z += contact_z - pivot.z
    bpy.context.view_layer.update()
    stick_feet()

    # Feet stay above the ground. A kick is not a sit.
    feet = [p for p in (_bone_world(arm, "Foot_L"), _bone_world(arm, "Foot_R")) if p is not None]
    if feet:
        lower = min(feet, key=lambda p: p.z)
        if lower.z < 0.12:
            arm.location.z += 0.12 - lower.z
            bpy.context.view_layer.update()
            stick_feet()

    sign = 1.0
    leaned = 0.0
    for _ in range(14):
        ox, oname = _mesh_min_x(blocks)
        if ox is None or ox >= WALL_FACE - 0.006:
            break
        pivot = _bone_world(arm, contact_bone)
        if pivot is None:
            break
        _rotate_about(arm, pivot, "Y", 3.0 * sign)
        leaned += 3.0 * sign
        stick_feet()
        ox2, _ = _mesh_min_x(blocks)
        if ox2 is not None and ox2 < ox - 0.002:
            # This step dug the body in. Undo it. The first time, try the other way.
            pivot = _bone_world(arm, contact_bone)
            _rotate_about(arm, pivot, "Y", -3.0 * sign)
            leaned -= 3.0 * sign
            stick_feet()
            if abs(leaned) <= 0.1:
                sign *= -1.0
                continue
            break

    # Root push: if a non-contact part is still through, slide along the normal
    # and lean back so the contact foot returns to the plane.
    ox, oname = _mesh_min_x(blocks)
    if ox is not None and ox < WALL_FACE - 0.006:
        arm.location.x += (WALL_FACE + 0.01) - ox
        bpy.context.view_layer.update()
        pivot = _bone_world(arm, contact_bone)
        for _ in range(10):
            cx, _ = _mesh_min_x(foot_touch)
            if cx is None or cx <= WALL_FACE + 0.012:
                break
            if pivot is None:
                break
            _rotate_about(arm, pivot, "Y", 3.0 * sign)
            leaned += 3.0 * sign
            pivot = _bone_world(arm, contact_bone)
            ox, oname = _mesh_min_x(blocks)
            if ox is not None and ox < WALL_FACE - 0.004:
                _rotate_about(arm, pivot, "Y", -3.0 * sign)
                leaned -= 3.0 * sign
                break
        stick_feet()

    # Cat leap: yaw about the feet until both shoes meet the face, then lean
    # the torso off the wall if a shin crossed. Hands come back to the lip
    # only while the chest, head and forearms stay clear.
    hand_z = None
    if kind == "cling":
        saved = arm.matrix_world.copy()
        fl0 = _bone_world(arm, "Foot_L")
        fr0 = _bone_world(arm, "Foot_R")
        best_yaw = 0.0
        best_gap = 1.0e9
        mid = None
        if fl0 is not None and fr0 is not None:
            mid = (fl0 + fr0) * 0.5
            for deg in range(-48, 12, 4):
                arm.matrix_world = saved.copy()
                bpy.context.view_layer.update()
                if deg:
                    _rotate_about(arm, mid, "Z", float(deg))
                stick_feet()
                flx, _ = _mesh_min_x(lambda n: "Foot_L" in n)
                frx, _ = _mesh_min_x(lambda n: "Foot_R" in n)
                if flx is None or frx is None:
                    continue
                gap = abs(flx - frx)
                if gap < best_gap:
                    best_gap = gap
                    best_yaw = float(deg)
            arm.matrix_world = saved.copy()
            bpy.context.view_layer.update()
            if best_yaw:
                _rotate_about(arm, mid, "Z", best_yaw)
            stick_feet()
            for _ in range(8):
                blocked, _ = _mesh_min_x(blocks)
                if blocked is None or blocked >= WALL_FACE - 0.006:
                    break
                _rotate_about(arm, mid, "Y", 3.0)
                leaned += 3.0
                stick_feet()
                flx, _ = _mesh_min_x(lambda n: "Foot_L" in n)
                frx, _ = _mesh_min_x(lambda n: "Foot_R" in n)
                if flx is None or frx is None or abs(flx - frx) > 0.045 or max(flx, frx) > WALL_FACE + 0.04:
                    _rotate_about(arm, mid, "Y", -3.0)
                    leaned -= 3.0
                    stick_feet()
                    break
        for _ in range(14):
            hx, _ = _mesh_min_x(lambda n: "Hand" in n)
            if hx is None or hx <= WALL_FACE + 0.012:
                break
            pivot = mid if mid is not None else _bone_world(arm, contact_bone)
            if pivot is None:
                break
            _rotate_about(arm, pivot, "Y", -1.0)
            leaned -= 1.0
            stick_feet()
            blocked, _ = _mesh_min_x(blocks)
            flx, _ = _mesh_min_x(lambda n: "Foot_L" in n)
            frx, _ = _mesh_min_x(lambda n: "Foot_R" in n)
            feet_left = flx is None or frx is None or abs(flx - frx) > 0.045 or max(flx, frx) > WALL_FACE + 0.04
            if (blocked is not None and blocked < WALL_FACE - 0.004) or feet_left:
                _rotate_about(arm, pivot, "Y", 1.0)
                leaned += 1.0
                stick_feet()
                break
        hand_z = _mesh_min_z(lambda n: "Hand" in n)

    # Last resort: slide along the normal until every non-contact mesh is on
    # the near side, then walk back toward the wall so the contact foot meets
    # the face again if it still can.
    ox, _ = _mesh_min_x(blocks)
    if ox is not None and ox < WALL_FACE - 0.005:
        arm.location.x += (WALL_FACE + 0.004) - ox
        bpy.context.view_layer.update()
    for _ in range(16):
        cx_back, _ = _mesh_min_x(foot_touch)
        if cx_back is None or cx_back <= WALL_FACE + 0.012:
            break
        arm.location.x -= 0.015
        bpy.context.view_layer.update()
        ox, _ = _mesh_min_x(blocks)
        if ox is not None and ox < WALL_FACE - 0.004:
            arm.location.x += 0.015
            bpy.context.view_layer.update()
            break

    ox, oname = _mesh_min_x(blocks)
    cx, _ = _mesh_min_x(foot_touch)
    hx, _ = _mesh_min_x(lambda n: "Hand" in n)
    flx, _ = _mesh_min_x(lambda n: "Foot_L" in n)
    frx, _ = _mesh_min_x(lambda n: "Foot_R" in n)
    chest = _bone_world(arm, "Chest")
    hips = _bone_world(arm, "Hips")
    foot_l = _bone_world(arm, "Foot_L")
    foot_r = _bone_world(arm, "Foot_R")
    hand_l = _bone_world(arm, "Hand_L")
    hand_r = _bone_world(arm, "Hand_R")
    print(
        f"  solve {contact_bone} lean={leaned:.0f} foot_x={cx} hand_x={hx} other={oname}:{ox} "
        f"footmesh=({flx},{frx}) chest=({chest.x:+.2f},{chest.z:.2f}) "
        f"hips=({hips.x:+.2f},{hips.z:.2f}) "
        f"feet=({foot_l.x:+.2f},{foot_l.z:.2f}) ({foot_r.x:+.2f},{foot_r.z:.2f}) "
        f"hands=({hand_l.x:+.2f},{hand_l.z:.2f}) ({hand_r.x:+.2f},{hand_r.z:.2f})"
    )
    _show_wall(WALL_FACE, hand_z if top == "hands" else None)


def _show_wall(face_x, hand_z):
    import bpy
    # Default cube is 2 m. scale 0.045 → 9 cm thick. The +X face is the contact face.
    half = 0.045
    if hand_z is None:
        center_z = 1.35
        scale_z = 1.35
    else:
        # hand_z is the underside of the lower hand, so the palm sits on the lip.
        top = hand_z
        height = max(1.15, top - 0.02)
        center_z = top - height * 0.5
        scale_z = height * 0.5
    if "ActionWall" in bpy.data.objects:
        wall = bpy.data.objects["ActionWall"]
    else:
        bpy.ops.mesh.primitive_cube_add(location=(face_x - half, 0.0, center_z))
        wall = bpy.context.active_object
        wall.name = "ActionWall"
        mat = bpy.data.materials.new("WallMat")
        mat.use_nodes = True
        mat.blend_method = "OPAQUE"
        mat.diffuse_color = (0.55, 0.53, 0.50, 1)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.55, 0.53, 0.50, 1)
            bsdf.inputs["Roughness"].default_value = 0.9
            if "Alpha" in bsdf.inputs:
                bsdf.inputs["Alpha"].default_value = 1.0
        wall.data.materials.append(mat)
    wall.location = (face_x - half, 0.05, center_z)
    wall.scale = (half, 1.7, scale_z)
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
    elif "--motion-composite" in args:
        composite_motion()
    elif "--composite" in args or (
        "bpy" not in sys.modules
        and "--new" not in args
        and "--old" not in args
        and "--poses" not in args
        and "--solve" not in args
        and "--motion" not in args
    ):
        composite()
    elif "--old" in args:
        render_old()
    elif "--poses" in args:
        render_poses()
    elif "--solve" in args:
        render_solve()
    elif "--motion" in args:
        render_motion()
    else:
        render_new()
