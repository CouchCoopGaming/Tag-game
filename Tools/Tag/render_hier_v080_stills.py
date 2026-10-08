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
        _apply_eulers(arm, eulers)
        slug = {
            "03_wall_drop_softland": "pose_03_softland",
            "04_window_drop_softland": "pose_04_softland",
            "10_wallrun_slanted": "pose_10_wallrun",
            "12_tictac_slanted_wall": "pose_12_tictac",
            "15_cat_leap_wall": "pose_15_catleap",
        }[spec["id"]]
        _plant(arm)
        _shot(os.path.join(PREV, slug + ".png"), *CAMS["threequarter"])
        print(f"{slug} hero_u={u:.3f} frame={idx}")
        _reset(arm)
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
        _apply_eulers(arm, eulers)
        slug = {
            "03_wall_drop_softland": "pose_03_softland",
            "04_window_drop_softland": "pose_04_softland",
            "10_wallrun_slanted": "pose_10_wallrun",
            "12_tictac_slanted_wall": "pose_12_tictac",
            "15_cat_leap_wall": "pose_15_catleap",
        }[spec["id"]]
        _plant(arm)
        _shot(os.path.join(PREV, slug + ".png"), *CAMS["threequarter"])
        print(f"{slug} hero_u={u:.3f} frame={idx}")
        _reset(arm)


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


def _reset(arm):
    import bpy
    arm.location.z = 0.0
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
    if "--composite" in args or ("bpy" not in sys.modules and "--new" not in args and "--old" not in args and "--poses" not in args):
        composite()
    elif "--old" in args:
        render_old()
    elif "--poses" in args:
        render_poses()
    else:
        render_new()
