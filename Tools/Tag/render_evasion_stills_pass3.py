"""Pass 3 evasion stills. Mid-grey grid floor, dark backdrop, foot insets."""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import render_evasion_pass3 as p3
import ingame_noclip as g

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass3", "pose_keys.tsv"))
OUT = os.path.join(ROOT, "Docs", "EvasionStills", "pass3")
FRAME_W, FRAME_H = 280, 360
CLIPS = ("stutter", "spinL", "spinR", "jukeL", "jukeR", "dive")


def look_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def make_mat(name, color, rough):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    if node is not None:
        node.inputs["Base Color"].default_value = color
        if "Roughness" in node.inputs:
            node.inputs["Roughness"].default_value = rough
    return mat


def grid_image():
    w = h = 256
    img = bpy.data.images.new("FloorGrid", w, h, alpha=False)
    base = (0.45, 0.45, 0.46)
    line = (0.36, 0.36, 0.37)
    px = []
    step = 32
    for y in range(h):
        for x in range(w):
            on = (x % step == 0) or (y % step == 0) or (x % step == 1) or (y % step == 1)
            c = line if on else base
            px.extend((c[0], c[1], c[2], 1.0))
    img.pixels.foreach_set(px)
    img.pack()
    return img


def grid_mat():
    mat = make_mat("GroundMat", (0.45, 0.45, 0.46, 1.0), 0.86)
    node = mat.node_tree.nodes.get("Principled BSDF")
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = grid_image()
    tex.interpolation = "Closest"
    mat.node_tree.links.new(tex.outputs["Color"], node.inputs["Base Color"])
    return mat


def samples(frames, clip, count):
    pool = [f for f in frames if f["clip"] == clip]
    if len(pool) <= count:
        return pool
    last = len(pool) - 1
    idx = []
    for i in range(count):
        pick = int(round(last * i / (count - 1)))
        if pick not in idx:
            idx.append(pick)
    while len(idx) < count:
        idx.append(last)
    return [pool[i] for i in idx[:count]]


def place_contact(shadow, arm):
    hips = arm.pose.bones.get("Hips")
    origin = arm.matrix_world @ hips.head if hips is not None else Vector((0.0, 0.0, 1.0))
    shadow.location = (origin.x, origin.y, 0.008)
    shadow.scale = (0.42, 0.26, 0.01)


def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    ground = bpy.data.objects["Ground"]
    ground.data.materials.append(grid_mat())
    g.prepare(arm)

    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, location=(0.0, 0.0, 0.008))
    shadow = bpy.context.active_object
    shadow.name = "ContactShadow"
    shadow.scale = (0.42, 0.26, 0.01)
    shadow.data.materials.append(make_mat("ShadowMat", (0.12, 0.12, 0.13, 1.0), 1.0))

    world = bpy.data.worlds.new("Backdrop")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.09, 0.10, 0.12, 1.0)
    bg.inputs["Strength"].default_value = 0.55

    bpy.ops.object.light_add(type="SUN", location=(3.2, -2.4, 6.5))
    sun = bpy.context.active_object
    sun.data.energy = 1.32
    sun.data.angle = 0.45
    sun.data.use_shadow = True
    look_at(sun, Vector((0.0, 0.0, 0.6)))

    bpy.ops.object.light_add(type="AREA", location=(-2.8, 2.6, 2.4))
    rim = bpy.context.active_object
    rim.data.energy = 180
    rim.data.size = 1.6
    rim.data.use_shadow = False
    look_at(rim, Vector((0.0, 0.0, 0.8)))

    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.data.lens = 46
    cam.data.sensor_width = 36
    bpy.context.scene.camera = cam
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = FRAME_W
    scene.render.resolution_y = FRAME_H
    scene.render.film_transparent = False
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    return arm, cam, shadow, scene


def aim(cam, kind, arm):
    hips = arm.pose.bones.get("Hips")
    origin = arm.matrix_world @ hips.head if hips is not None else Vector((0.0, 0.0, 0.9))
    height = origin.z
    if height < 0.55:
        height = 0.55
    if height > 1.05:
        height = 1.05
    target = Vector((origin.x * 0.35, origin.y * 0.35, height))
    if kind == "side":
        cam.location = Vector((target.x + 2.85, target.y + 0.12, height + 0.16))
    else:
        cam.location = Vector((target.x + 1.85, target.y - 2.25, height + 0.22))
    look_at(cam, target)


def save_small(path, image):
    from PIL import Image
    image.save(path, "PNG", optimize=True)
    if os.path.getsize(path) > 390 * 1024:
        image.quantize(colors=96, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)
    if os.path.getsize(path) > 390 * 1024:
        smaller = image.resize((image.width * 3 // 4, image.height * 3 // 4), Image.Resampling.LANCZOS)
        smaller.quantize(colors=64, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)


def foot_inset(tile, scene, cam, arm):
    from bpy_extras.object_utils import world_to_camera_view
    from PIL import Image
    gl = p3.sole_gap(arm, "L")
    gr = p3.sole_gap(arm, "R")
    side = "L" if gl <= gr else "R"
    if min(gl, gr) > 0.008:
        return tile
    pb = arm.pose.bones["Foot_" + side]
    co = world_to_camera_view(scene, cam, arm.matrix_world @ pb.head)
    cx = int(co.x * FRAME_W)
    cy = int((1.0 - co.y) * FRAME_H)
    crop = 40
    x0 = max(0, min(FRAME_W - crop, cx - crop // 2))
    y0 = max(0, min(FRAME_H - crop, cy - crop // 2))
    patch = tile.crop((x0, y0, x0 + crop, y0 + crop)).resize((crop * 4, crop * 4), Image.Resampling.NEAREST)
    out = tile.copy()
    out.paste(patch, (FRAME_W - crop * 4 - 8, FRAME_H - crop * 4 - 8))
    return out


def main():
    from PIL import Image, ImageDraw, ImageFont
    os.makedirs(OUT, exist_ok=True)
    frames = g.load_keys(KEYS)
    arm, cam, shadow, scene = setup()
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 16)
    except OSError:
        font = ImageFont.load_default()
    tmp = os.path.join(OUT, "_frame.png")
    # One floor sample so the grey can be checked.
    p3.apply_posed(arm, [f for f in frames if f["clip"] == "stutter"][4])
    aim(cam, "side", arm)
    scene.render.filepath = tmp
    bpy.ops.render.render(write_still=True)
    sample = Image.open(tmp).convert("RGB")
    px = sample.getpixel((20, FRAME_H - 24))
    print("FLOOR_RGB", px, "value", round(sum(px) / (3 * 255), 3), flush=True)

    for clip in CLIPS:
        chosen = samples(frames, clip, 7)
        for view in ("side", "threequarter"):
            tiles = []
            for frame in chosen:
                p3.apply_posed(arm, frame)
                place_contact(shadow, arm)
                aim(cam, view, arm)
                scene.render.filepath = tmp
                bpy.ops.render.render(write_still=True)
                tile = Image.open(tmp).convert("RGB")
                if clip.startswith("stutter") or clip.startswith("juke"):
                    tile = foot_inset(tile, scene, cam, arm)
                tiles.append(tile)
            strip = Image.new("RGB", (FRAME_W * len(tiles), FRAME_H), (18, 20, 24))
            draw = ImageDraw.Draw(strip)
            for i, tile in enumerate(tiles):
                strip.paste(tile, (i * FRAME_W, 0))
                label = "%s  %.2fs" % (clip, chosen[i]["t"])
                draw.rectangle((i * FRAME_W + 6, 6, i * FRAME_W + 8 + 8 * len(label), 28), fill=(0, 0, 0))
                draw.text((i * FRAME_W + 10, 8), label, fill=(245, 245, 245), font=font)
            path = os.path.join(OUT, "%s-%s.png" % (clip, view))
            save_small(path, strip)
            print("STILL", path, os.path.getsize(path), flush=True)
    if os.path.exists(tmp):
        os.remove(tmp)
    print("EXIT")


if __name__ == "__main__":
    main()