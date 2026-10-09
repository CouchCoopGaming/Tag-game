"""Pass 2 evasion stills. Mid-grey floor, dark backdrop, key and rim."""
import os
import sys

import bpy
from mathutils import Vector
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass2", "pose_keys.tsv"))
OUT = os.path.join(ROOT, "Docs", "EvasionStills", "pass2")
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
    shadow.location = (origin.x, origin.y, 0.012)
    shadow.scale = (0.42, 0.26, 0.01)


def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    ground = bpy.data.objects["Ground"]
    ground.data.materials.append(make_mat("GroundMat", (0.46, 0.47, 0.49, 1.0), 0.78))
    g.prepare(arm)

    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, location=(0.0, 0.0, 0.012))
    shadow = bpy.context.active_object
    shadow.name = "ContactShadow"
    shadow.scale = (0.42, 0.26, 0.01)
    shadow.data.materials.append(make_mat("ShadowMat", (0.16, 0.17, 0.18, 1.0), 1.0))

    world = bpy.data.worlds.new("Backdrop")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.09, 0.10, 0.12, 1.0)
    bg.inputs["Strength"].default_value = 1.0

    bpy.ops.object.light_add(type="SUN", location=(3.2, -2.4, 6.5))
    sun = bpy.context.active_object
    sun.data.energy = 3.6
    sun.data.angle = 0.35
    sun.data.use_shadow = True
    if hasattr(sun.data, "use_contact_shadow"):
        sun.data.use_contact_shadow = True
        sun.data.contact_shadow_distance = 0.18
        sun.data.contact_shadow_bias = 0.001
        sun.data.contact_shadow_thickness = 0.02
    look_at(sun, Vector((0.0, 0.0, 0.8)))

    bpy.ops.object.light_add(type="AREA", location=(-2.8, 2.6, 2.4))
    rim = bpy.context.active_object
    rim.data.energy = 280
    rim.data.size = 1.6
    rim.data.use_shadow = False
    look_at(rim, Vector((0.0, 0.0, 0.9)))

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


def aim(cam, kind):
    target = Vector((0.0, 0.0, 0.82))
    if kind == "side":
        cam.location = Vector((2.85, 0.12, 0.98))
    else:
        cam.location = Vector((1.85, -2.25, 1.08))
    look_at(cam, target)


def save_small(path, image):
    image.save(path, "PNG", optimize=True)
    if os.path.getsize(path) > 400 * 1024:
        image.quantize(colors=96, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)
    if os.path.getsize(path) > 400 * 1024:
        smaller = image.resize((image.width * 3 // 4, image.height * 3 // 4), Image.Resampling.LANCZOS)
        smaller.quantize(colors=64, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)


def main():
    os.makedirs(OUT, exist_ok=True)
    frames = g.load_keys(KEYS)
    arm, cam, shadow, scene = setup()
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 16)
    except OSError:
        font = ImageFont.load_default()
    tmp = os.path.join(OUT, "_frame.png")
    for clip in CLIPS:
        chosen = samples(frames, clip, 7)
        for view in ("side", "threequarter"):
            tiles = []
            aim(cam, view)
            for frame in chosen:
                g.apply_frame(arm, frame)
                place_contact(shadow, arm)
                scene.render.filepath = tmp
                bpy.ops.render.render(write_still=True)
                tiles.append(Image.open(tmp).convert("RGB"))
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
