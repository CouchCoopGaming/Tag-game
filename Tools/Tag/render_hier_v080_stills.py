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
PASS4_OLD_FBX = "/tmp/hier_v079g_tan.fbx"
NEW_BLEND = "/tmp/Dummy_Mannequin_Hier_Hi_v080_Tan.blend"
PASS4 = os.path.join(OUT, "pass4")
# Keyed clips do not translate the armature or the Root bone.
LOCK_ROOT = False

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
            # Only the hero frame gets the cat-leap arm and knee correction.
            _present(arm, spec, idx, pose, stage=abs(u - round(hero, 3)) < 1e-4)
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


def _shot(path, loc, look, ortho=None, up="Y"):
    import bpy
    from mathutils import Vector
    if "ShotCam" in bpy.data.objects:
        cam = bpy.data.objects["ShotCam"]
    else:
        bpy.ops.object.camera_add()
        cam = bpy.context.active_object
        cam.name = "ShotCam"
    cam.data.lens = 48
    if ortho is None:
        cam.data.type = "PERSP"
    else:
        # Elevation: every ray is parallel to the wall, so the lip stays a level line.
        cam.data.type = "ORTHO"
        cam.data.ortho_scale = ortho
    cam.location = loc
    direction = Vector(look) - Vector(loc)
    # `up` is the camera axis aimed at world +Z when the look direction is horizontal.
    # Looking along Y, "Z" keeps image-up as world up and the wall as a vertical edge.
    cam.rotation_euler = direction.to_track_quat("-Z", up).to_euler()
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
    slug = SLUGS[spec["id"]]
    _shot(os.path.join(PREV, slug + ".png"), *POSE_CAM)
    _side_contact_line(spec)
    loc, look, scale = _side_cam(arm, spec)
    _shot(os.path.join(PREV, slug + "_side.png"), loc, look, ortho=scale)
    print(f"{slug} hero_u={u:.3f} frame={idx} verb={spec['verb']}")
    _reset(arm)


# Wall normal points toward +X. The face is built on the contact mesh.
# The armature is not slid along that normal by more than this.
ROOT_SLIDE_MAX = 0.10


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
        _hide_marks()
        _report(arm, spec, None, None, "ground", None)
        return
    if verb == "wallrun":
        # Straighter leg is the plant. Roll stays 0: a +40° roll pushed the
        # chest about 80 cm off the wall. The wall is built on the foot.
        side = "L" if abs(pose["knee_l"]) <= abs(pose["knee_r"]) else "R"
        pitch = min(max(trunk, 6.0), 16.0)
        arm.rotation_euler = Euler((math.radians(pitch), 0.0, math.radians(-15.0)), "XYZ")
        bpy.context.view_layer.update()
        cz = 0.78 if stage else _posed_contact_z(arm, "Foot_" + side, 0.35)
        _seat_foot(arm, spec, "Foot_" + side, cz)
    elif verb == "walljump":
        # Forward kick (the larger thigh pitch) meets the wall at hip height.
        # Yaw turns that kick onto -X. The wall is built on the kicking foot.
        side = "L" if pose["thigh_l"] >= pose["thigh_r"] else "R"
        arm.rotation_euler = Euler((math.radians(6.0), math.radians(10.0), math.radians(-76.0)), "XYZ")
        bpy.context.view_layer.update()
        cz = 0.95 if stage else _posed_contact_z(arm, "Foot_" + side, 0.35)
        _seat_foot(arm, spec, "Foot_" + side, cz)
    else:
        # Cat leap: both feet on the face, both palms on the lip.
        arm.rotation_euler = Euler((math.radians(12.0), 0.0, math.radians(-74.0)), "XYZ")
        bpy.context.view_layer.update()
        _seat_cling(arm, spec, stage)


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


def _body_mesh(obj):
    if obj.type != "MESH":
        return False
    name = obj.name
    if name in ("Ground", "ActionWall") or name.startswith("ContactMark"):
        return False
    return True


def _mesh_min_x(allow):
    """Minimum world x of meshes that allow(name) accepts. None if none match."""
    import bpy
    _ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    best = None
    best_name = None
    for obj in bpy.data.objects:
        if not _body_mesh(obj) or not allow(obj.name):
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
    _ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    best = None
    for obj in bpy.data.objects:
        if not _body_mesh(obj) or not allow(obj.name):
            continue
        ev = obj.evaluated_get(deps)
        mw = ev.matrix_world
        for v in ev.data.vertices:
            z = (mw @ v.co).z
            if best is None or z < best:
                best = z
    return best


def _object_mode(arm):
    import bpy
    if arm is not None and bpy.context.view_layer.objects.active is not arm:
        bpy.context.view_layer.objects.active = arm
    if bpy.context.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()


def _ensure_object():
    """Mesh eval lags in pose mode, so measurements switch back first."""
    import bpy
    if bpy.context.mode == "OBJECT":
        return
    if bpy.context.view_layer.objects.active is None:
        arm = bpy.data.objects.get("DummyArmature")
        if arm is not None:
            bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()


def _iter_world_verts(allow):
    import bpy
    _ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in bpy.data.objects:
        if not _body_mesh(obj) or not allow(obj.name):
            continue
        ev = obj.evaluated_get(deps)
        mw = ev.matrix_world
        for v in ev.data.vertices:
            yield mw @ v.co


def _mesh_max_z(allow):
    best = None
    for w in _iter_world_verts(allow):
        if best is None or w.z > best:
            best = w.z
    return best


def _head_crown():
    return _mesh_max_z(lambda n: "Head" in n)


def _hand_verts(side):
    return list(_iter_world_verts(lambda n, side=side: f"Hand_{side}" in n))


def _palm_cluster(arm, side):
    """Palm and knuckle verts. Fingertips sit farther than 5 cm from the wrist."""
    wrist = _bone_world(arm, f"Hand_{side}")
    verts = _hand_verts(side)
    if wrist is None or not verts:
        return verts
    near = [v for v in verts if (v - wrist).length <= 0.050]
    return near or verts


def _palm_top_z(arm, side):
    cluster = _palm_cluster(arm, side)
    if not cluster:
        return None
    return max(v.z for v in cluster)


def _palm_gap(arm, side, face, top):
    """Closest palm-cluster vertex to the lip corner, in the XZ plane."""
    cluster = _palm_cluster(arm, side)
    if not cluster:
        return None, None
    best = min(cluster, key=lambda v: (v.x - face) ** 2 + (v.z - top) ** 2)
    return best, math.hypot(best.x - face, best.z - top)


def _nudge_bone(arm, name, dx=0.0, dy=0.0, dz=0.0):
    import bpy
    from mathutils import Euler
    if bpy.context.view_layer.objects.active is not arm:
        bpy.context.view_layer.objects.active = arm
    if bpy.context.mode != "POSE":
        bpy.ops.object.mode_set(mode="POSE")
    pb = arm.pose.bones.get(name)
    if pb is None:
        return
    pb.rotation_mode = "XYZ"
    e = pb.rotation_euler
    pb.rotation_euler = Euler((
        e.x + math.radians(dx),
        e.y + math.radians(dy),
        e.z + math.radians(dz),
    ), "XYZ")
    bpy.context.view_layer.update()


def _bone_euler(arm, name):
    pb = arm.pose.bones.get(name)
    if pb is None:
        return (0.0, 0.0, 0.0)
    e = pb.rotation_euler
    return (e.x, e.y, e.z)


def _set_bone_euler(arm, name, eul):
    import bpy
    from mathutils import Euler
    if bpy.context.view_layer.objects.active is not arm:
        bpy.context.view_layer.objects.active = arm
    if bpy.context.mode != "POSE":
        bpy.ops.object.mode_set(mode="POSE")
    pb = arm.pose.bones.get(name)
    if pb is None:
        return
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = Euler(eul, "XYZ")
    bpy.context.view_layer.update()


def _add_world_rot(arm, axis, deg):
    """World-axis rotation about the armature origin. Location is unchanged."""
    import bpy
    from mathutils import Matrix
    _object_mode(arm)
    loc = arm.location.copy()
    delta = Matrix.Rotation(math.radians(deg), 4, axis)
    arm.matrix_world = delta @ arm.matrix_world
    arm.location = loc
    bpy.context.view_layer.update()


def _search_world(arm, axis, degrees, score_fn):
    """Pick the world rotation, added to the current pose, with the lowest score."""
    import bpy
    _object_mode(arm)
    saved_r = arm.rotation_euler.copy()
    saved_l = arm.location.copy()
    best_deg = 0.0
    best = score_fn()
    for deg in degrees:
        arm.rotation_euler = saved_r.copy()
        arm.location = saved_l.copy()
        bpy.context.view_layer.update()
        if deg:
            _add_world_rot(arm, axis, deg)
        sc = score_fn()
        if sc < best - 1.0e-6:
            best = sc
            best_deg = deg
    arm.rotation_euler = saved_r.copy()
    arm.location = saved_l.copy()
    bpy.context.view_layer.update()
    if best_deg:
        _add_world_rot(arm, axis, best_deg)
    return best_deg, best


def _foot_xs():
    fl, _ = _mesh_min_x(lambda n: "Foot_L" in n)
    fr, _ = _mesh_min_x(lambda n: "Foot_R" in n)
    return fl, fr


def _feet_spread():
    fl, fr = _foot_xs()
    if fl is None or fr is None:
        return 1.0e6
    return abs(fl - fr)


def _square_feet(arm):
    _search_world(arm, "Z", list(range(-36, 37, 3)), _feet_spread)
    if _feet_spread() > 0.03:
        _search_world(arm, "Z", list(range(-8, 9, 1)), _feet_spread)


def _lift_feet(arm, floor):
    import bpy
    if LOCK_ROOT:
        return
    z = _mesh_min_z(lambda n: "Foot" in n)
    if z is not None and z < floor:
        arm.location.z += floor - z
        bpy.context.view_layer.update()


def _clearance_past(foot_x, ignore_foot):
    """How far a non-foot vertex crosses a plane at foot_x, toward -X."""
    if foot_x is None:
        return 1.0e6
    past = 0.0
    for w in _iter_world_verts(lambda n, ignore_foot=ignore_foot: ignore_foot not in n):
        d = foot_x - w.x
        if d > past:
            past = d
    return past


def _clearance_past_foot(contact_bone):
    foot_x, _ = _mesh_min_x(lambda n, contact_bone=contact_bone: contact_bone in n)
    return _clearance_past(foot_x, contact_bone)


def _raise_knees(arm):
    """Bend each leg until the knee sits between the hip and the chest.

    Which local axis lifts the knee depends on the wall yaw, so each step
    keeps the candidate that moves the knee toward the middle of that band
    and leaves the foot below the knee.
    """
    cands = (
        (-8, 0, 0, 12), (8, 0, 0, 12), (0, 8, 0, 12), (0, -8, 0, 12),
        (0, 0, 8, 12), (0, 0, -8, 12), (-8, 0, 0, 20), (8, 0, 0, 20),
        (-8, 8, 0, 16), (8, -8, 0, 16), (0, 0, 0, 12),
    )
    for side in ("L", "R"):
        name_u = f"UpperLeg_{side}"
        name_l = f"LowerLeg_{side}"
        for _ in range(16):
            hip = _bone_world(arm, "Hips")
            chest = _bone_world(arm, "Chest")
            knee = _bone_world(arm, f"LowerLeg_{side}")
            foot = _bone_world(arm, f"Foot_{side}")
            if hip is None or chest is None or knee is None:
                break
            lo = hip.z + 0.045
            hi = chest.z - 0.025
            foot_ok = foot is None or foot.z < knee.z - 0.08
            if lo < knee.z < hi and foot_ok:
                break
            saved_u = _bone_euler(arm, name_u)
            saved_l = _bone_euler(arm, name_l)
            # Climb into the band. A knee already above the chest has to come down.
            too_high = knee.z >= hi
            best = None
            for ux, uy, uz, lx in cands:
                _set_bone_euler(arm, name_u, saved_u)
                _set_bone_euler(arm, name_l, saved_l)
                if ux or uy or uz:
                    _nudge_bone(arm, name_u, dx=ux, dy=uy, dz=uz)
                if lx:
                    _nudge_bone(arm, name_l, dx=lx)
                knee2 = _bone_world(arm, f"LowerLeg_{side}")
                foot2 = _bone_world(arm, f"Foot_{side}")
                if knee2 is None:
                    continue
                foot_gap = 0.04 if too_high else 0.07
                if foot2 is not None and foot2.z > knee2.z - foot_gap:
                    continue
                if too_high:
                    if knee2.z >= knee.z - 0.003:
                        continue
                    # Closest to the band from above, then inside it.
                    rank = abs(knee2.z - (lo + hi) * 0.5)
                    if best is None or rank < best[0]:
                        best = (rank, ux, uy, uz, lx, knee2.z)
                else:
                    if knee2.z >= hi:
                        continue
                    if best is None or knee2.z > best[0]:
                        best = (knee2.z, ux, uy, uz, lx, knee2.z)
            _set_bone_euler(arm, name_u, saved_u)
            _set_bone_euler(arm, name_l, saved_l)
            if best is None:
                break
            if not too_high and best[5] < knee.z + 0.004:
                break
            _, ux, uy, uz, lx, _kz = best
            if ux or uy or uz:
                _nudge_bone(arm, name_u, dx=ux, dy=uy, dz=uz)
            if lx:
                _nudge_bone(arm, name_l, dx=lx)
    _object_mode(arm)



def _penetration_depth(face, z_top):
    """How far the chest, head or a forearm crosses the face below the lip."""
    if face is None:
        return 0.0
    limit = z_top if z_top is not None else 99.0
    depth = 0.0
    for w in _iter_world_verts(lambda n: "Foot" not in n and "Hand" not in n):
        if w.z < limit - 0.012 and w.x < face - 0.004:
            depth = max(depth, face - w.x)
    return depth



def _finger_overs(arm, side, face, top):
    if face is None or top is None:
        return 0
    wrist = _bone_world(arm, f"Hand_{side}")
    if wrist is None:
        return 0
    n = 0
    for v in _hand_verts(side):
        if (v - wrist).length < 0.055:
            continue
        # Over the lip, but only a short hook — not the whole arm past the wall.
        if face - 0.075 < v.x < face - 0.006 and v.z >= top - 0.006:
            n += 1
    return n


def _hook_score(arm, side, face, top):
    overs = _finger_overs(arm, side, face, top)
    _, dist = _palm_gap(arm, side, face, top)
    wrist = _bone_world(arm, f"Hand_{side}")
    score = float(overs)
    if dist is not None:
        score -= max(0.0, dist - 0.020) * 40.0
    if wrist is not None and top is not None and wrist.z > top - 0.008:
        score -= 8.0
    return score


def _hook_fingers(arm, face, top):
    """Tip each resting fist over the lip without pulling the palm off it."""
    axes = (("dx", 0), ("dy", 1), ("dz", 2))
    for side in ("L", "R"):
        name = f"Hand_{side}"
        for _cycle in range(2):
            for key, _index in axes:
                saved = _bone_euler(arm, name)
                best_step = 0.0
                best_score = _hook_score(arm, side, face, top)
                for step in (-36, -18, -9, 9, 18, 36):
                    _set_bone_euler(arm, name, saved)
                    _nudge_bone(arm, name, **{key: step})
                    sc = _hook_score(arm, side, face, top)
                    if sc > best_score + 0.05:
                        best_score = sc
                        best_step = step
                _set_bone_euler(arm, name, saved)
                if best_step:
                    _nudge_bone(arm, name, **{key: best_step})
    _object_mode(arm)


def _cap_root(arm):
    import bpy
    if LOCK_ROOT:
        arm.location = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()
        return
    if abs(arm.location.x) > ROOT_SLIDE_MAX:
        arm.location.x = max(-ROOT_SLIDE_MAX, min(ROOT_SLIDE_MAX, arm.location.x))
        bpy.context.view_layer.update()


def _lean_until_clear(arm, contact_bone):
    """Smallest world-Y lean that keeps the head and chest off the foot plane."""
    import bpy
    if _clearance_past_foot(contact_bone) <= 0.010:
        return
    saved_r = arm.rotation_euler.copy()
    saved_l = arm.location.copy()
    order = list(range(3, 34, 3)) + list(range(-3, -34, -3))
    for deg in order:
        arm.rotation_euler = saved_r.copy()
        arm.location = saved_l.copy()
        bpy.context.view_layer.update()
        _add_world_rot(arm, "Y", deg)
        if _clearance_past_foot(contact_bone) <= 0.010:
            return
    arm.rotation_euler = saved_r.copy()
    arm.location = saved_l.copy()
    bpy.context.view_layer.update()
    _search_world(arm, "Y", order, lambda: _clearance_past_foot(contact_bone))


def _seat_foot(arm, spec, contact_bone, contact_z):
    """Put the wall on the plant foot. Lean only if the head would cross that plane."""
    import bpy
    _object_mode(arm)
    pivot = _bone_world(arm, contact_bone)
    if pivot is not None and not LOCK_ROOT:
        arm.location.z += contact_z - pivot.z
        bpy.context.view_layer.update()
    _lift_feet(arm, 0.10)
    _lean_until_clear(arm, contact_bone)
    pivot = _bone_world(arm, contact_bone)
    if pivot is not None and not LOCK_ROOT:
        arm.location.z += contact_z - pivot.z
        bpy.context.view_layer.update()
    _cap_root(arm)
    foot_x, _ = _mesh_min_x(lambda n, contact_bone=contact_bone: contact_bone in n)
    face = foot_x if foot_x is not None else -0.40
    foot_p = _bone_world(arm, contact_bone)
    cy = foot_p.y if foot_p is not None else 0.0
    _show_wall(face, None, cy, 1.30)
    _hide_marks()
    z_mesh = _mesh_min_z(lambda n, contact_bone=contact_bone: contact_bone in n)
    _show_foot_mark(face, cy, (z_mesh if z_mesh is not None else contact_z), contact_bone[-1])
    _report(arm, spec, face, None, "foot", contact_bone)



def _shoulder_reach(arm=None):
    """Lower is better. Both shoulders have to sit inside an arm's length of the lip."""
    fl, fr = _foot_xs()
    if fl is None or fr is None:
        return 1.0e6
    face = 0.5 * (fl + fr)
    crown = _head_crown() or 1.6
    lip = crown + 0.012
    depth = _penetration_depth(face, lip)
    if depth > 0.012:
        return 3.0 + depth
    dists = []
    for side in ("L", "R"):
        ua = _bone_world(arm, f"UpperArm_{side}") if arm is not None else None
        if ua is None:
            return 1.0e6
        dists.append(math.hypot(ua.x - face, ua.z - lip))
    chest_x, _ = _mesh_min_x(lambda n: "Chest" in n)
    pen = 0.0
    if chest_x is not None and chest_x - face < 0.06:
        pen += 0.20
    return max(dists) + 0.25 * abs(dists[0] - dists[1]) + pen


def _square_shoulders(arm):
    """Twist the spine so the farther shoulder can still reach the lip."""
    saved = _bone_euler(arm, "Spine")
    best = (1.0e6, 0, 0)
    for dy in range(-40, 41, 8):
        for dz in range(-32, 33, 8):
            _set_bone_euler(arm, "Spine", saved)
            if dy or dz:
                _nudge_bone(arm, "Spine", dy=dy, dz=dz)
            sc = _shoulder_reach(arm)
            chest = _bone_world(arm, "Chest")
            kl = _bone_world(arm, "LowerLeg_L")
            kr = _bone_world(arm, "LowerLeg_R")
            if chest is not None and kl is not None and kr is not None:
                knee_top = kl.z if kl.z > kr.z else kr.z
                if chest.z < knee_top + 0.05:
                    sc += 0.12 + (knee_top + 0.05 - chest.z)
            if sc < best[0] - 0.004:
                best = (sc, dy, dz)
    _set_bone_euler(arm, "Spine", saved)
    if best[1] or best[2]:
        _nudge_bone(arm, "Spine", dy=best[1], dz=best[2])
    _object_mode(arm)
    print(f"  spine dy={best[1]} dz={best[2]} reach_score={best[0]:.3f}")


def _reach_xz(arm):
    """Greedy arm steps that cut palm-to-lip distance without entering the wall."""
    steps = (
        (-8, 0, 0, 0), (8, 0, 0, 0), (0, -8, 0, 0), (0, 8, 0, 0),
        (0, 0, -8, 0), (0, 0, 8, 0), (-8, -8, 0, 0), (-8, 8, 0, 0),
        (-8, 0, 0, 10), (-8, 0, 0, -10), (0, -8, 0, 10), (0, 8, 0, 10),
    )
    for _ in range(16):
        fl, fr = _foot_xs()
        if fl is None or fr is None:
            break
        face = 0.5 * (fl + fr)
        crown = _head_crown()
        if crown is None:
            break
        lip = crown + 0.012
        moved = False
        for side in ("L", "R"):
            _pt, dist = _palm_gap(arm, side, face, lip)
            if dist is not None and dist <= 0.018:
                continue
            name = f"UpperArm_{side}"
            elbow = f"LowerArm_{side}"
            saved = _bone_euler(arm, name)
            saved_e = _bone_euler(arm, elbow)
            base = 1.0e6 if dist is None else dist
            best = None
            for dx, dy, dz, ex in steps:
                _set_bone_euler(arm, name, saved)
                _set_bone_euler(arm, elbow, saved_e)
                if dx or dy or dz:
                    _nudge_bone(arm, name, dx=dx, dy=dy, dz=dz)
                if ex:
                    _nudge_bone(arm, elbow, dx=ex)
                _pt2, dist2 = _palm_gap(arm, side, face, lip)
                if dist2 is None:
                    continue
                depth = _penetration_depth(face, lip)
                score = dist2 + (4.0 + depth if depth > 0.012 else 0.0)
                if best is None or score < best[0]:
                    best = (score, dx, dy, dz, ex)
            _set_bone_euler(arm, name, saved)
            _set_bone_euler(arm, elbow, saved_e)
            if best is not None and best[0] < base - 0.003:
                _dx, _dy, _dz, _ex = best[1:]
                if _dx or _dy or _dz:
                    _nudge_bone(arm, name, dx=_dx, dy=_dy, dz=_dz)
                if _ex:
                    _nudge_bone(arm, elbow, dx=_ex)
                moved = True
        if not moved:
            break
    _object_mode(arm)


def _grid_reach(arm, side):
    """Coarse arm search for a hand the greedy steps left short of the lip."""
    fl, fr = _foot_xs()
    if fl is None or fr is None:
        return
    face = 0.5 * (fl + fr)
    crown = _head_crown()
    if crown is None:
        return
    lip = crown + 0.012
    _pt, dist = _palm_gap(arm, side, face, lip)
    ua = _bone_world(arm, f"UpperArm_{side}")
    if ua is not None:
        reach = math.hypot(ua.x - face, ua.z - lip)
        print(f"  grid {side} shoulder_to_lip_cm={reach * 100.0:.1f} palm_cm={-1 if dist is None else dist * 100.0:.1f}")
    if dist is not None and dist <= 0.020:
        return
    name = f"UpperArm_{side}"
    elbow = f"LowerArm_{side}"
    saved = _bone_euler(arm, name)
    saved_e = _bone_euler(arm, elbow)
    best = None
    for ex in (0, -30, -55):
        for ux in range(-80, 81, 20):
            for uz in (-40, 0, 40):
                _set_bone_euler(arm, name, saved)
                _set_bone_euler(arm, elbow, saved_e)
                if ux or uz:
                    _nudge_bone(arm, name, dx=ux, dz=uz)
                if ex:
                    _nudge_bone(arm, elbow, dx=ex)
                pt, dist2 = _palm_gap(arm, side, face, lip)
                if dist2 is None:
                    continue
                depth = 0.0
                if dist2 < 0.06:
                    depth = _penetration_depth(face, lip)
                    if depth > 0.015:
                        continue
                if best is None or dist2 < best[0]:
                    best = (dist2, ux, uz, ex, depth)
    _set_bone_euler(arm, name, saved)
    _set_bone_euler(arm, elbow, saved_e)
    if best is None:
        return
    _ux, _uz, _ex, _depth = best[1:]
    if _ux or _uz:
        _nudge_bone(arm, name, dx=_ux, dz=_uz)
    if _ex:
        _nudge_bone(arm, elbow, dx=_ex)
    if _penetration_depth(face, lip) > 0.015:
        _set_bone_euler(arm, name, saved)
        _set_bone_euler(arm, elbow, saved_e)
    _object_mode(arm)
    print(f"  grid {side} palm_cm={best[0] * 100.0:.1f}")


def _finish_cling(arm):
    """Last centimetre: the short palm, then a knee that sits above the chest."""
    saved_spine = _bone_euler(arm, "Spine")
    best_spine = None
    for dy in range(-16, 17, 4):
        for dz in range(-16, 17, 4):
            _set_bone_euler(arm, "Spine", saved_spine)
            if dy or dz:
                _nudge_bone(arm, "Spine", dy=dy, dz=dz)
            fl, fr = _foot_xs()
            if fl is None or fr is None:
                continue
            face = 0.5 * (fl + fr)
            crown = _head_crown()
            if crown is None:
                continue
            lip = crown + 0.012
            _pl, dl = _palm_gap(arm, "L", face, lip)
            _pr, dr = _palm_gap(arm, "R", face, lip)
            if dl is None or dr is None:
                continue
            chest = _bone_world(arm, "Chest")
            kl = _bone_world(arm, "LowerLeg_L")
            kr = _bone_world(arm, "LowerLeg_R")
            if chest is None or kl is None or kr is None:
                continue
            if chest.z < max(kl.z, kr.z) + 0.035:
                continue
            if _penetration_depth(face, lip) > 0.006 or abs(fl - fr) > 0.04:
                continue
            score = max(dl, dr)
            if best_spine is None or score < best_spine[0]:
                best_spine = (score, dy, dz)
    _set_bone_euler(arm, "Spine", saved_spine)
    if best_spine is not None and (best_spine[1] or best_spine[2]):
        _nudge_bone(arm, "Spine", dy=best_spine[1], dz=best_spine[2])
        saved_spine = _bone_euler(arm, "Spine")
        fine = (best_spine[0], 0, 0)
        for dy in range(-6, 7, 2):
            for dz in range(-6, 7, 2):
                _set_bone_euler(arm, "Spine", saved_spine)
                if dy or dz:
                    _nudge_bone(arm, "Spine", dy=dy, dz=dz)
                fl, fr = _foot_xs()
                if fl is None or fr is None:
                    continue
                face = 0.5 * (fl + fr)
                crown = _head_crown()
                if crown is None:
                    continue
                lip = crown + 0.012
                _pl, dl = _palm_gap(arm, "L", face, lip)
                _pr, dr = _palm_gap(arm, "R", face, lip)
                if dl is None or dr is None:
                    continue
                chest = _bone_world(arm, "Chest")
                kl = _bone_world(arm, "LowerLeg_L")
                kr = _bone_world(arm, "LowerLeg_R")
                if chest is None or kl is None or kr is None:
                    continue
                if chest.z < max(kl.z, kr.z) + 0.035:
                    continue
                if _penetration_depth(face, lip) > 0.006 or abs(fl - fr) > 0.04:
                    continue
                score = max(dl, dr)
                if score < fine[0]:
                    fine = (score, dy, dz)
        _set_bone_euler(arm, "Spine", saved_spine)
        if fine[1] or fine[2]:
            _nudge_bone(arm, "Spine", dy=fine[1], dz=fine[2])
    fl, fr = _foot_xs()
    if fl is None or fr is None:
        return
    face = 0.5 * (fl + fr)
    crown = _head_crown()
    if crown is None:
        return
    lip = crown + 0.012
    for _ in range(8):
        _pt, dist = _palm_gap(arm, "R", face, lip)
        if dist is None or dist <= 0.019:
            break
        bones = ("Shoulder_R", "UpperArm_R", "LowerArm_R")
        saved = [_bone_euler(arm, n) for n in bones]
        best = None
        trials = []
        for bi, bone in enumerate(bones):
            for key in ("dx", "dy", "dz"):
                for deg in (-8.0, -4.0, 4.0, 8.0):
                    trials.append((bi, key, deg))
        for bi, key, deg in trials:
            for n, eul in zip(bones, saved):
                _set_bone_euler(arm, n, eul)
            _nudge_bone(arm, bones[bi], **{key: deg})
            _p2, dist2 = _palm_gap(arm, "R", face, lip)
            _pl, dist_l = _palm_gap(arm, "L", face, lip)
            depth = _penetration_depth(face, lip)
            if dist2 is None or depth > 0.012 or (dist_l is not None and dist_l > 0.025):
                continue
            if best is None or dist2 < best[0]:
                best = (dist2, bi, key, deg)
        for n, eul in zip(bones, saved):
            _set_bone_euler(arm, n, eul)
        if best is None or best[0] > dist - 0.001:
            break
        _nudge_bone(arm, bones[best[1]], **{best[2]: best[3]})
    for side in ("L", "R"):
        for _ in range(6):
            fl, fr = _foot_xs()
            if fl is None or fr is None:
                break
            face = 0.5 * (fl + fr)
            crown = _head_crown()
            if crown is None:
                break
            lip = crown + 0.012
            _pt, dist = _palm_gap(arm, side, face, lip)
            if dist is None or dist <= 0.018:
                continue
            name = f"Hand_{side}"
            saved_h = _bone_euler(arm, name)
            best_h = None
            for key in ("dx", "dy", "dz"):
                for deg in (-18.0, -9.0, 9.0, 18.0):
                    _set_bone_euler(arm, name, saved_h)
                    _nudge_bone(arm, name, **{key: deg})
                    _p2, dist2 = _palm_gap(arm, side, face, lip)
                    wrist = _bone_world(arm, f"Hand_{side}")
                    if dist2 is None or wrist is None or wrist.z > lip - 0.008:
                        continue
                    if _penetration_depth(face, lip) > 0.006:
                        continue
                    if best_h is None or dist2 < best_h[0]:
                        best_h = (dist2, key, deg)
            _set_bone_euler(arm, name, saved_h)
            if best_h is None or best_h[0] > dist - 0.001:
                break
            _nudge_bone(arm, name, **{best_h[1]: best_h[2]})
    # Drop the crown onto the palms so the lip and the hands share a height.
    saved_head = _bone_euler(arm, "Head")
    best_head = None
    for dx in (-14.0, -8.0, -4.0, 4.0, 8.0, 14.0):
        _set_bone_euler(arm, "Head", saved_head)
        if dx:
            _nudge_bone(arm, "Head", dx=dx)
        fl, fr = _foot_xs()
        if fl is None or fr is None:
            continue
        face = 0.5 * (fl + fr)
        crown = _head_crown()
        if crown is None:
            continue
        _pl, dl = _palm_gap(arm, "L", face, crown)
        _pr, dr = _palm_gap(arm, "R", face, crown)
        if dl is None or dr is None or _penetration_depth(face, crown) > 0.006:
            continue
        score = max(dl, dr)
        if best_head is None or score < best_head[0]:
            best_head = (score, dx)
    _set_bone_euler(arm, "Head", saved_head)
    if best_head is not None and best_head[1]:
        _nudge_bone(arm, "Head", dx=best_head[1])
    # Drop a knee that ended above the chest, and only keep the step if the
    # feet and palms stay on the wall.
    for side in ("L", "R"):
        for _ in range(6):
            hip = _bone_world(arm, "Hips")
            chest = _bone_world(arm, "Chest")
            knee = _bone_world(arm, f"LowerLeg_{side}")
            if hip is None or chest is None or knee is None:
                break
            if hip.z + 0.03 < knee.z < chest.z - 0.015:
                break
            if knee.z <= hip.z + 0.03:
                break
            name_u = f"UpperLeg_{side}"
            name_l = f"LowerLeg_{side}"
            saved_u = _bone_euler(arm, name_u)
            saved_l = _bone_euler(arm, name_l)
            best = None
            for ux, lx in ((6, 10), (-6, 10), (0, 8), (8, 14), (-8, 14), (0, 0)):
                _set_bone_euler(arm, name_u, saved_u)
                _set_bone_euler(arm, name_l, saved_l)
                if ux:
                    _nudge_bone(arm, name_u, dx=ux)
                if lx:
                    _nudge_bone(arm, name_l, dx=lx)
                knee2 = _bone_world(arm, f"LowerLeg_{side}")
                foot2 = _bone_world(arm, f"Foot_{side}")
                if knee2 is None or knee2.z >= knee.z - 0.004:
                    continue
                if foot2 is not None and foot2.z > knee2.z - 0.05:
                    continue
                fl2, fr2 = _foot_xs()
                if fl2 is None or fr2 is None or abs(fl2 - fr2) > 0.035:
                    continue
                depth = _penetration_depth(face, lip)
                if depth > 0.012:
                    continue
                if best is None or knee2.z < best[0]:
                    best = (knee2.z, ux, lx)
            _set_bone_euler(arm, name_u, saved_u)
            _set_bone_euler(arm, name_l, saved_l)
            if best is None:
                break
            if best[1]:
                _nudge_bone(arm, name_u, dx=best[1])
            if best[2]:
                _nudge_bone(arm, name_l, dx=best[2])
    _object_mode(arm)


def _seat_cling(arm, spec, stage):
    """Both feet on the face. On the hero frame, both palms on a lip at the crown."""
    import bpy
    _object_mode(arm)
    _lift_feet(arm, 0.16)
    _square_feet(arm)
    if stage:
        # Square the chest to the wall so both shoulders can reach the lip.
        _raise_knees(arm)
        _search_world(arm, "Y", list(range(-15, 75, 3)), lambda: _shoulder_reach(arm))
        _square_shoulders(arm)
        _square_feet(arm)
        _reach_xz(arm)
        _grid_reach(arm, "L")
        _grid_reach(arm, "R")
        _finish_cling(arm)
    else:
        def _near_clear():
            fl, fr = _foot_xs()
            xs = [x for x in (fl, fr) if x is not None]
            if not xs:
                return 1.0e6
            return _clearance_past(min(xs), "Foot")
        _search_world(arm, "Y", list(range(-18, 19, 3)), _near_clear)
    _cap_root(arm)
    fl, fr = _foot_xs()
    if fl is not None and fr is not None:
        face = 0.5 * (fl + fr)
    else:
        face = fl if fl is not None else (fr if fr is not None else -0.40)
    crown = _head_crown()
    top = None
    if stage and crown is not None:
        zs = [z for z in (_palm_top_z(arm, "L"), _palm_top_z(arm, "R")) if z is not None]
        if zs and min(zs) >= crown - 0.005:
            top = max(crown, min(zs))
        else:
            top = crown
        _hook_fingers(arm, face, top)
        zs = [z for z in (_palm_top_z(arm, "L"), _palm_top_z(arm, "R")) if z is not None]
        crown = _head_crown()
        if crown is not None and zs and min(zs) >= crown - 0.005:
            top = max(crown, min(zs))
        elif crown is not None:
            top = crown
    flb = _bone_world(arm, "Foot_L")
    frb = _bone_world(arm, "Foot_R")
    ys = [p.y for p in (flb, frb) if p is not None]
    cy = sum(ys) / len(ys) if ys else 0.0
    span = (max(ys) - min(ys)) if len(ys) == 2 else 0.40
    length = max(1.20, min(1.60, span + 0.45))
    _show_wall(face, top, cy, length)
    _hide_marks()
    for side, bone in (("L", flb), ("R", frb)):
        z_mesh = _mesh_min_z(lambda n, side=side: f"Foot_{side}" in n)
        y = bone.y if bone is not None else cy
        z = z_mesh if z_mesh is not None else 0.40
        _show_foot_mark(face, y, z, side)
    if top is not None:
        _show_lip_mark(face, top, cy, max(0.34, span + 0.12))
    _report(arm, spec, face, top, "cling", "Foot_L")


def _show_wall(face_x, top_z, center_y, length):
    import bpy
    # Default cube is 2 m. scale 0.045 on X is a 9 cm wall. The +X face is the contact.
    half = 0.045
    if top_z is None:
        height = 2.55
        center_z = 0.04 + height * 0.5
    else:
        height = max(1.10, top_z - 0.02)
        center_z = top_z - height * 0.5
    scale_y = max(0.55, length * 0.5)
    if "ActionWall" in bpy.data.objects:
        wall = bpy.data.objects["ActionWall"]
    else:
        bpy.ops.mesh.primitive_cube_add(location=(face_x - half, center_y, center_z))
        wall = bpy.context.active_object
        wall.name = "ActionWall"
        mat = bpy.data.materials.new("WallMat")
        mat.use_nodes = True
        mat.blend_method = "OPAQUE"
        mat.diffuse_color = (0.42, 0.40, 0.38, 1)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.42, 0.40, 0.38, 1)
            bsdf.inputs["Roughness"].default_value = 0.9
            if "Alpha" in bsdf.inputs:
                bsdf.inputs["Alpha"].default_value = 1.0
        wall.data.materials.append(mat)
    wall.location = (face_x - half, center_y, center_z)
    wall.scale = (half, scale_y, height * 0.5)
    wall.hide_render = False
    wall.hide_set(False)


def _mark_mat():
    import bpy
    mat = bpy.data.materials.get("ContactMarkMat")
    if mat is not None:
        return mat
    mat = bpy.data.materials.new("ContactMarkMat")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    emit = nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (0.15, 1.0, 1.0, 1.0)
    emit.inputs["Strength"].default_value = 12.0
    links.new(emit.outputs["Emission"], out.inputs["Surface"])
    mat.diffuse_color = (0.15, 1.0, 1.0, 1)
    return mat


def _ensure_mark(name):
    import bpy
    ob = bpy.data.objects.get(name)
    if ob is not None:
        return ob
    bpy.ops.mesh.primitive_cube_add()
    ob = bpy.context.active_object
    ob.name = name
    ob.data.materials.append(_mark_mat())
    return ob


def _show_foot_mark(face, y, z, side):
    ob = _ensure_mark(f"ContactMark_foot_{side}")
    # Beside the shoe, not inside it: a thin bar on the face at the contact.
    ob.scale = (0.012, 0.012, 0.13)
    ob.location = (face + 0.016, y + 0.07, z + 0.12)
    ob.hide_render = False
    ob.hide_set(False)


def _show_lip_mark(face, top, center_y, span):
    ob = _ensure_mark("ContactMark_lip")
    # A thin bar along the top edge, long enough to read in both cameras.
    ob.scale = (0.008, max(0.16, span * 0.5), 0.010)
    ob.location = (face + 0.006, center_y, top + 0.004)
    ob.hide_render = False
    ob.hide_set(False)


def _hide_marks():
    import bpy
    for ob in bpy.data.objects:
        if ob.name.startswith("ContactMark"):
            ob.hide_render = True
            ob.hide_set(True)


def _fmt(v):
    if v is None:
        return "nan"
    return f"{v:.2f}"


def _report(arm, spec, face, top, kind, contact_bone):
    root_cm = arm.location.x * 100.0
    chest_x, _ = _mesh_min_x(lambda n: "Chest" in n)
    gap = (chest_x - face) * 100.0 if chest_x is not None and face is not None else None
    fl, fr = _foot_xs()

    def rel(x):
        if x is None or face is None:
            return None
        return (x - face) * 100.0

    palm_l = palm_r = None
    if face is not None and top is not None:
        _, dl = _palm_gap(arm, "L", face, top)
        _, dr = _palm_gap(arm, "R", face, top)
        palm_l = None if dl is None else dl * 100.0
        palm_r = None if dr is None else dr * 100.0
    crown = _head_crown()
    crown_cm = (crown - top) * 100.0 if crown is not None and top is not None else None

    def wrist_below(side):
        w = _bone_world(arm, f"Hand_{side}")
        if w is None or top is None:
            return None
        return (top - w.z) * 100.0

    hip = _bone_world(arm, "Hips")
    chest = _bone_world(arm, "Chest")
    kl = _bone_world(arm, "LowerLeg_L")
    kr = _bone_world(arm, "LowerLeg_R")
    overs_l = _finger_overs(arm, "L", face, top)
    overs_r = _finger_overs(arm, "R", face, top)
    fore_x, _ = _mesh_min_x(lambda n: "LowerArm" in n)
    fore_cm = (fore_x - face) * 100.0 if fore_x is not None and face is not None else None
    line = (
        f"METRIC id={spec['id']} kind={kind} root_cm={root_cm:.2f} "
        f"chest_gap_cm={_fmt(gap)} footL_cm={_fmt(rel(fl))} footR_cm={_fmt(rel(fr))} "
        f"palmL_cm={_fmt(palm_l)} palmR_cm={_fmt(palm_r)} "
        f"crown_minus_top_cm={_fmt(crown_cm)} "
        f"wrist_below_L_cm={_fmt(wrist_below('L'))} wrist_below_R_cm={_fmt(wrist_below('R'))} "
        f"forearm_cm={_fmt(fore_cm)} "
        f"kneeL_z={kl.z if kl else float('nan'):.3f} kneeR_z={kr.z if kr else float('nan'):.3f} "
        f"hip_z={hip.z if hip else float('nan'):.3f} chest_z={chest.z if chest else float('nan'):.3f} "
        f"finger_over_L={overs_l} finger_over_R={overs_r} "
        f"face={face} top={top}"
    )
    print(line)


def _side_cam(arm, spec):
    """Side elevation. Camera and target share x and z, so the axis is +Y."""
    if spec["verb"] == "softland":
        return ((4.40, 0.15, 0.95), (0.0, 0.0, 0.95), 2.35)
    hips = _bone_world(arm, "Hips")
    hx = hips.x if hips is not None else 0.0
    if spec["verb"] == "cling":
        hz, scale = 0.82, 2.05
    elif spec["verb"] == "wallrun":
        hz, scale = 1.20, 2.55
    else:
        hz, scale = 1.05, 2.45
    return ((hx, -4.20, hz), (hx, 0.05, hz), scale)


def _side_contact_line(spec):
    """A bar along the wall normal so the lip reads as a level line in elevation.

    The lip mark itself runs along the wall, which is the camera axis, so it
    collapses to a speck. This tick crosses that axis.
    """
    import bpy
    if spec["verb"] == "softland":
        return
    wall = bpy.data.objects.get("ActionWall")
    if wall is not None and not wall.hide_render:
        # Long enough that the top edge is the lip across the whole figure.
        wall.scale.y = max(wall.scale.y, 1.35)
    lip = bpy.data.objects.get("ContactMark_lip")
    if lip is None or lip.hide_render:
        return
    tick = _ensure_mark("ContactMark_lip_side")
    tick.scale = (0.09, 0.008, 0.007)
    tick.location = (lip.location.x - 0.04, lip.location.y, lip.location.z)
    tick.hide_render = False
    tick.hide_set(False)



def _hide_wall():
    import bpy
    wall = bpy.data.objects.get("ActionWall")
    if wall is not None:
        wall.hide_render = True
        wall.hide_set(True)
    _hide_marks()


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
    if lowest is None or LOCK_ROOT:
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


def _facing_euler(spec, trunk):
    from mathutils import Euler
    verb = spec["verb"]
    if verb == "softland":
        return Euler((math.radians(min(float(trunk), 16.0)), 0.0, 0.0), "XYZ")
    if verb == "wallrun":
        pitch = min(max(float(trunk), 6.0), 16.0)
        # +90 yaw puts a foot on the wall face and leaves the chest off it.
        # -15 left the hand on the face and both feet in the air.
        return Euler((math.radians(pitch), 0.0, math.radians(90.0)), "XYZ")
    if verb == "walljump":
        return Euler((math.radians(6.0), math.radians(10.0), math.radians(-76.0)), "XYZ")
    return Euler((math.radians(12.0), 0.0, math.radians(-74.0)), "XYZ")


def _apply_curve(arm, smooth, u, facing):
    """Bone rotations from the joint curves. The armature stays at the origin."""
    import bpy
    import storror_pose as sp
    ch = sp.sample_raw(smooth, u)
    pose = sp.unity_pose(ch)
    _apply_eulers(arm, sp.blender_euler(pose))
    arm.location = (0.0, 0.0, 0.0)
    arm.rotation_euler = facing
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    return pose


def _foot_probe(side):
    """Min-x and min-z vertices of one foot mesh."""
    import bpy
    _ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    token = f"Foot_{side}"
    min_x = at_x = min_z = at_z = None
    for obj in bpy.data.objects:
        if not _body_mesh(obj) or token not in obj.name:
            continue
        ev = obj.evaluated_get(deps)
        mw = ev.matrix_world
        for v in ev.data.vertices:
            w = mw @ v.co
            if min_x is None or w.x < min_x:
                min_x = w.x
                at_x = w.copy()
            if min_z is None or w.z < min_z:
                min_z = w.z
                at_z = w.copy()
    return min_x, at_x, min_z, at_z


def _plant_side(spec, pose):
    if spec["verb"] == "wallrun":
        return "L" if abs(pose["knee_l"]) <= abs(pose["knee_r"]) else "R"
    if spec["verb"] == "walljump":
        return "L" if pose["thigh_l"] >= pose["thigh_r"] else "R"
    return None


def _skate(pts):
    """How far a planted contact wanders, in metres. pts are (along, across)."""
    if len(pts) < 2:
        return 0.0, 0.0
    step = 0.0
    for a, b in zip(pts, pts[1:]):
        step = max(step, math.hypot(b[0] - a[0], b[1] - a[1]))
    span = math.hypot(max(p[0] for p in pts) - min(p[0] for p in pts),
                      max(p[1] for p in pts) - min(p[1] for p in pts))
    return span, step


def _measure_clip(arm, spec, smooth, facing):
    """Foot slide and penetration of the in-place curve against a fixed contact."""
    import storror_pose as sp
    n = len(smooth["knee_flex_L"])
    hero_u, hero_i = sp.hero_u(spec["id"], smooth)
    _apply_curve(arm, smooth, hero_u, facing)
    hero_pose = sp.unity_pose(sp.sample_raw(smooth, hero_u))
    side = _plant_side(spec, hero_pose)
    probes = {s: _foot_probe(s) for s in ("L", "R")}
    if spec["verb"] == "softland":
        face = None
        top = None
        kind = "ground"
    else:
        if side is None:
            xs = [probes[s][0] for s in ("L", "R") if probes[s][0] is not None]
            face = sum(xs) / len(xs) if xs else 0.0
        else:
            face = probes[side][0] if probes[side][0] is not None else 0.0
        top = _head_crown() if spec["verb"] == "cling" else None
        kind = "cling" if spec["verb"] == "cling" else "foot"
    series = {s: [] for s in ("L", "R")}
    pen = {s: 0.0 for s in ("L", "R")}
    gap = {s: 1.0e6 for s in ("L", "R")}
    palm_pen = 0.0
    for i in range(n):
        u = 0.0 if n <= 1 else i / (n - 1)
        _apply_curve(arm, smooth, u, facing)
        for s in ("L", "R"):
            min_x, at_x, min_z, at_z = _foot_probe(s)
            if spec["verb"] == "softland":
                if min_z is None:
                    continue
                depth = -min_z
                near = abs(min_z) <= 0.04
                series[s].append((near, at_z.x, at_z.y))
                gap[s] = min(gap[s], abs(min_z))
            else:
                if min_x is None or face is None:
                    continue
                depth = face - min_x
                near = abs(min_x - face) <= 0.04
                series[s].append((near, at_x.y, at_x.z))
                gap[s] = min(gap[s], abs(min_x - face))
            if depth > pen[s]:
                pen[s] = depth
        if spec["verb"] == "cling" and face is not None and top is not None:
            for s in ("L", "R"):
                _best, dist = _palm_gap(arm, s, face, top)
                if dist is not None and -dist > palm_pen:
                    # dist is unsigned. Penetration is the palm past the face.
                    pass
            for s in ("L", "R"):
                cluster = _palm_cluster(arm, s)
                if not cluster:
                    continue
                depth = max(face - v.x for v in cluster)
                if depth > palm_pen:
                    palm_pen = depth
    slides = {}
    for s in ("L", "R"):
        planted = [(a, b) for near, a, b in series[s] if near]
        span, step = _skate(planted)
        slides[s] = (span, step, len(planted))
    return {
        "hero_u": hero_u,
        "hero_i": hero_i,
        "frames": n,
        "kind": kind,
        "side": side,
        "face": face,
        "top": top,
        "pen": pen,
        "gap": gap,
        "slides": slides,
        "palm_pen": palm_pen,
        "root": (arm.location.x, arm.location.y, arm.location.z),
    }


def _key_clip(arm, spec, smooth, facing):
    """Rotation keys only. No Root or armature location keys."""
    import bpy
    import storror_pose as sp
    from mathutils import Euler
    n = len(smooth["knee_flex_L"])
    action = bpy.data.actions.new("Storror_" + spec["id"])
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = action
    keys = []
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    for i in range(n):
        u = 0.0 if n <= 1 else i / (n - 1)
        ch = sp.sample_raw(smooth, u)
        eulers = sp.blender_euler(sp.unity_pose(ch))
        frame = i + 1
        bones = {}
        for name, deg in eulers.items():
            pb = arm.pose.bones.get(name)
            if pb is None:
                continue
            pb.rotation_mode = "XYZ"
            pb.rotation_euler = Euler(tuple(math.radians(a) for a in deg), "XYZ")
            pb.keyframe_insert(data_path="rotation_euler", frame=frame)
            bones[name] = [round(float(a), 2) for a in deg]
        keys.append({"frame": i, "t": round(i / float(smooth and 1 or 1), 4), "bones": bones})
    # Timing is the source fps. Location channels are not inserted.
    for fc in list(action.fcurves):
        if fc.data_path.endswith("location"):
            action.fcurves.remove(fc)
    bpy.ops.object.mode_set(mode="OBJECT")
    return action, keys


def render_pass4_shots():
    """Re-render the five-frame strips from the curves. Does not rebuild the keys."""
    import bpy
    import storror_pose as sp
    global LOCK_ROOT
    LOCK_ROOT = True
    dest = os.path.join(PREV, "pass4")
    os.makedirs(dest, exist_ok=True)
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    if arm.animation_data is not None:
        arm.animation_data.action = None
    _prepare_scene()
    sc = bpy.context.scene
    sc.render.resolution_x = 900
    sc.render.resolution_y = 1200
    if hasattr(sc, "eevee"):
        sc.eevee.taa_render_samples = 12
    sc.frame_set(1)
    for spec in sp.CLIPS:
        curves = sp.clip_curves(spec["id"], keys=24)
        smooth = curves["raw_smooth"]
        hero_u, hero_i = sp.hero_u(spec["id"], smooth)
        raw = sp.load_clip(spec["id"])
        ang = raw["joint_angles_deg"][hero_i] or {}
        trunk = float(ang.get("trunk_lean_from_cam_vertical") or 0.0)
        facing = _facing_euler(spec, trunk)
        _apply_curve(arm, smooth, hero_u, facing)
        pose = sp.unity_pose(sp.sample_raw(smooth, hero_u))
        side = _plant_side(spec, pose) or "L"
        if spec["verb"] == "softland":
            _hide_wall()
        else:
            probes = {s: _foot_probe(s) for s in ("L", "R")}
            if spec["verb"] == "cling":
                xs = [probes[s][0] for s in ("L", "R") if probes[s][0] is not None]
                face = sum(xs) / len(xs) if xs else 0.0
                top = _head_crown()
            else:
                face = probes[side][0] if probes[side][0] is not None else 0.0
                top = None
            foot = _bone_world(arm, "Foot_" + side)
            cy = foot.y if foot is not None else 0.0
            _show_wall(face, top, cy, 1.40)
            _hide_marks()
            z_mesh = _mesh_min_z(lambda n, side=side: f"Foot_{side}" in n)
            _show_foot_mark(face, cy, z_mesh if z_mesh is not None else 0.2, side)
            if top is not None:
                _show_lip_mark(face, top, cy, 0.40)
        slug = SLUGS[spec["id"]]
        for i, u in enumerate(_motion_us(hero_u)):
            _apply_curve(arm, smooth, u, facing)
            _shot(os.path.join(dest, f"{slug}_{i}.png"), *POSE_CAM)
            _side_contact_line(spec)
            loc, look, scale = _side_cam(arm, spec)
            _shot(os.path.join(dest, f"{slug}_{i}_side.png"), loc, look, ortho=scale)
            print(f"pass4 {slug} {i} u={u:.2f}")
        _reset(arm)


def render_pass4():
    """Idle stills against v0.7.9g, then in-place keyed clips."""
    import bpy
    import json
    import storror_pose as sp
    global LOCK_ROOT
    LOCK_ROOT = True
    os.makedirs(PREV, exist_ok=True)
    dest = os.path.join(PREV, "pass4")
    os.makedirs(dest, exist_ok=True)
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    _prepare_scene()
    _render_views(arm, "pass4_new")

    sc = bpy.context.scene
    sc.render.resolution_x = 900
    sc.render.resolution_y = 1200
    if hasattr(sc, "eevee"):
        sc.eevee.taa_render_samples = 12
    doc = {
        "root_motion": False,
        "note": "Bone rotation keys only. Armature location and Root location stay at the origin.",
        "clips": {},
    }
    for spec in sp.CLIPS:
        curves = sp.clip_curves(spec["id"], keys=24)
        smooth = curves["raw_smooth"]
        hero_u, _hero_i = sp.hero_u(spec["id"], smooth)
        raw = sp.load_clip(spec["id"])
        ang = raw["joint_angles_deg"][int(round(hero_u * (curves["frames"] - 1)))] or {}
        trunk = float(ang.get("trunk_lean_from_cam_vertical") or 0.0)
        facing = _facing_euler(spec, trunk)
        stats = _measure_clip(arm, spec, smooth, facing)
        action, keys = _key_clip(arm, spec, smooth, facing)
        fps = float(curves["fps"])
        for k in keys:
            k["t"] = round(k["frame"] / fps, 4)
        sides = stats["slides"]
        plant = stats["side"] or "L"
        span, step, nplant = sides[plant]
        other = "R" if plant == "L" else "L"
        ospan, ostep, onplant = sides[other]
        line = (
            f"CLIP id={spec['id']} frames={stats['frames']} fps={fps:.2f} "
            f"root_cm=({stats['root'][0]*100:.2f},{stats['root'][1]*100:.2f},{stats['root'][2]*100:.2f}) "
            f"plant={plant} slide_cm={span*100:.2f} step_cm={step*100:.2f} planted={nplant} "
            f"other_slide_cm={ospan*100:.2f} "
            f"pen_L_cm={stats['pen']['L']*100:.2f} pen_R_cm={stats['pen']['R']*100:.2f} "
            f"gap_L_cm={stats['gap']['L']*100:.2f} gap_R_cm={stats['gap']['R']*100:.2f} "
            f"palm_pen_cm={stats['palm_pen']*100:.2f} "
            f"face={stats['face']} top={stats['top']} keys={len(keys)} action={action.name}"
        )
        print(line)
        doc["clips"][spec["id"]] = {
            "fps": fps,
            "frames": stats["frames"],
            "hero_frame": stats["hero_i"],
            "plant": plant,
            "foot_slide_cm": round(span * 100.0, 2),
            "foot_step_cm": round(step * 100.0, 2),
            "other_slide_cm": round(ospan * 100.0, 2),
            "penetration_cm": {
                "L": round(stats["pen"]["L"] * 100.0, 2),
                "R": round(stats["pen"]["R"] * 100.0, 2),
                "palm": round(stats["palm_pen"] * 100.0, 2),
            },
            "min_gap_cm": {
                "L": round(stats["gap"]["L"] * 100.0, 2),
                "R": round(stats["gap"]["R"] * 100.0, 2),
            },
            "root_cm": [0.0, 0.0, 0.0],
            "keys": keys,
        }
        # Five-frame strip from the same keys. The wall stays on the hero contact.
        _apply_curve(arm, smooth, hero_u, facing)
        if stats["kind"] == "ground":
            _hide_wall()
        else:
            cy = 0.0
            foot = _bone_world(arm, "Foot_" + plant)
            if foot is not None:
                cy = foot.y
            _show_wall(stats["face"], stats["top"], cy, 1.40)
            _hide_marks()
            z_mesh = _mesh_min_z(lambda n, plant=plant: f"Foot_{plant}" in n)
            y = foot.y if foot is not None else 0.0
            _show_foot_mark(stats["face"], y, z_mesh if z_mesh is not None else 0.2, plant)
            if stats["top"] is not None:
                _show_lip_mark(stats["face"], stats["top"], cy, 0.40)
        slug = SLUGS[spec["id"]]
        # The action would override every still with frame 1. The keys live in the action
        # and in keyed_clips.json; the strip is posed from the same curves directly.
        if arm.animation_data is not None:
            arm.animation_data.action = None
        bpy.context.scene.frame_set(1)
        for i, u in enumerate(_motion_us(stats["hero_u"])):
            _apply_curve(arm, smooth, u, facing)
            _shot(os.path.join(dest, f"{slug}_{i}.png"), *POSE_CAM)
            _side_contact_line(spec)
            loc, look, scale = _side_cam(arm, spec)
            _shot(os.path.join(dest, f"{slug}_{i}_side.png"), loc, look, ortho=scale)
            print(f"pass4 {slug} {i} u={u:.2f}")
        _reset(arm)
        if arm.animation_data is not None:
            arm.animation_data.action = None
    os.makedirs(PASS4, exist_ok=True)
    with open(os.path.join(PASS4, "keyed_clips.json"), "w") as f:
        json.dump(doc, f)
    print("wrote", os.path.join(PASS4, "keyed_clips.json"))

    # v0.7.9g idle, same cameras, after the v0.8.0 shots are on disk.
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=PASS4_OLD_FBX, automatic_bone_orientation=False, global_scale=1.0)
    old = None
    for o in bpy.data.objects:
        if o.type == "ARMATURE":
            old = o
            break
    if old is None:
        raise SystemExit("v0.7.9g FBX imported no armature")
    _prepare_scene()
    _render_views(old, "pass4_old")


def composite_pass4():
    """v0.7.9g beside v0.8.0, plus the five in-place strips."""
    from PIL import Image, ImageDraw, ImageFont
    import storror_pose as sp
    os.makedirs(PASS4, exist_ok=True)
    font = ImageFont.load_default()

    def label(im, text):
        d = ImageDraw.Draw(im)
        d.rectangle((16, 16, 16 + 8 * len(text) + 16, 40), fill=(20, 18, 16))
        d.text((26, 20), text, fill=(245, 236, 220), font=font)
        return im

    for name in ("front", "threequarter", "side"):
        old = Image.open(os.path.join(PREV, f"pass4_old_{name}.png")).convert("RGB")
        new = Image.open(os.path.join(PREV, f"pass4_new_{name}.png")).convert("RGB")
        w, h = old.size
        canvas = Image.new("RGB", (w * 2 + 24, h), (48, 44, 40))
        canvas.paste(label(old, "v0.7.9g"), (0, 0))
        canvas.paste(label(new, "v0.8.0"), (w + 24, 0))
        path = os.path.join(PASS4, f"{name}.png")
        canvas.save(path)
        print("wrote", path)
    old = Image.open(os.path.join(PREV, "pass4_old_front.png")).convert("RGB")
    new = Image.open(os.path.join(PREV, "pass4_new_front.png")).convert("RGB")
    tinted = []
    for r, g, b in old.getdata():
        tinted.append((int(r * 0.45 + 40), int(g * 0.55 + 50), int(b * 0.75 + 70)))
    old_t = Image.new("RGB", old.size)
    old_t.putdata(tinted)
    over = Image.blend(old_t, new, 0.55)
    label(over, "overlay  v0.7.9g cool  /  v0.8.0")
    path = os.path.join(PASS4, "overlay.png")
    over.save(path)
    print("wrote", path)

    src = os.path.join(PREV, "pass4")
    labels = {
        "03_wall_drop_softland": "03 soft land",
        "04_window_drop_softland": "04 soft land",
        "10_wallrun_slanted": "10 wall run",
        "12_tictac_slanted_wall": "12 tic-tac",
        "15_cat_leap_wall": "15 cat leap",
    }
    for spec in sp.CLIPS:
        slug = SLUGS[spec["id"]]
        for suffix, tag in (("", "3/4"), ("_side", "side")):
            frames = []
            for i in range(5):
                frames.append(Image.open(os.path.join(src, f"{slug}_{i}{suffix}.png")).convert("RGB"))
            w, h = frames[0].size
            gap = 12
            canvas = Image.new("RGB", (w * 5 + gap * 4, h), (48, 44, 40))
            for i, im in enumerate(frames):
                canvas.paste(im, (i * (w + gap), 0))
            text = labels[spec["id"]] + "  " + tag + "  in place"
            label(canvas, text)
            path = os.path.join(PASS4, f"strip_{slug}{suffix}.png")
            canvas.save(path)
            print("wrote", path)


if __name__ == "__main__":
    args = _args()
    if "--anim" in args:
        composite_anim()
    elif "--motion-composite" in args:
        composite_motion()
    elif "--pass4-composite" in args:
        composite_pass4()
    elif "--composite" in args or (
        "bpy" not in sys.modules
        and "--new" not in args
        and "--old" not in args
        and "--poses" not in args
        and "--solve" not in args
        and "--motion" not in args
        and "--pass4" not in args
        and "--pass4-shots" not in args
    ):
        composite()
    elif "--pass4-shots" in args:
        render_pass4_shots()
    elif "--pass4" in args:
        render_pass4()
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
