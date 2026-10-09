"""Side and three-quarter strips for the evasion clips, plus red-overlap stills.

Floor, contact shadow, and a sun. Frames are the 30 fps keys. No root lift.
"""
import os
import sys

import bpy
from mathutils import Vector
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import noclip_check as n

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass1", "pose_keys.tsv"))
OUT = os.path.join(ROOT, "Docs", "EvasionStills", "pass1")
NOCLIP = os.path.join(OUT, "noclip")
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


def nearest(frames, clip, t):
    pool = [f for f in frames if f["clip"] == clip]
    return min(pool, key=lambda f: abs(f["t"] - t))


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
    shadow.scale = (0.46, 0.28, 0.01)


def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    ground = bpy.data.objects["Ground"]
    ground.data.materials.append(make_mat("GroundMat", (0.62, 0.64, 0.60, 1.0), 0.84))
    g.prepare(arm)

    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, location=(0.0, 0.0, 0.012))
    shadow = bpy.context.active_object
    shadow.name = "ContactShadow"
    shadow.scale = (0.46, 0.28, 0.01)
    shadow.data.materials.append(make_mat("ShadowMat", (0.10, 0.11, 0.12, 1.0), 1.0))

    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.55, 0.66, 0.78, 1.0)
    bg.inputs["Strength"].default_value = 0.85

    bpy.ops.object.light_add(type="SUN", location=(2.6, -3.2, 7.0))
    sun = bpy.context.active_object
    sun.data.energy = 2.4
    sun.data.use_shadow = True
    if hasattr(sun.data, "use_contact_shadow"):
        sun.data.use_contact_shadow = True
        sun.data.contact_shadow_distance = 0.2
        sun.data.contact_shadow_bias = 0.001
        sun.data.contact_shadow_thickness = 0.02
    look_at(sun, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.light_add(type="AREA", location=(-2.4, -1.6, 2.8))
    fill = bpy.context.active_object
    fill.data.energy = 40
    fill.data.size = 3.0
    fill.data.use_shadow = False
    look_at(fill, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.data.lens = 48
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
    target = Vector((0.0, 0.05, 0.98))
    if kind == "side":
        cam.location = Vector((3.35, 0.15, 1.15))
    else:
        cam.location = Vector((2.15, -2.55, 1.28))
    look_at(cam, target)


def save_small(path, image):
    image.save(path, "PNG", optimize=True)
    if os.path.getsize(path) > 400 * 1024:
        image.quantize(colors=96, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)
    if os.path.getsize(path) > 400 * 1024:
        smaller = image.resize((image.width * 3 // 4, image.height * 3 // 4), Image.Resampling.LANCZOS)
        smaller.quantize(colors=64, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)


def render_strips(arm, cam, shadow, scene, frames):
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
            strip = Image.new("RGB", (FRAME_W * len(tiles), FRAME_H), (40, 44, 48))
            draw = ImageDraw.Draw(strip)
            for i, tile in enumerate(tiles):
                strip.paste(tile, (i * FRAME_W, 0))
                label = "%s  %.2fs" % (clip, chosen[i]["t"])
                draw.rectangle((i * FRAME_W + 6, 6, i * FRAME_W + 8 + 8 * len(label), 28), fill=(0, 0, 0))
                draw.text((i * FRAME_W + 10, 8), label, fill=(245, 245, 245), font=font)
            path = os.path.join(OUT, "%s-%s.png" % (clip, view))
            save_small(path, strip)
            print("STILL", path, os.path.getsize(path))
    if os.path.exists(tmp):
        os.remove(tmp)


def render_overlap(arm, cam, shadow, scene, frames):
    path = os.path.join(NOCLIP, "worst.txt")
    if not os.path.exists(path):
        print("NO WORST")
        return
    rows = []
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            parts = line.strip().split("\t")
            if len(parts) < 6:
                continue
            rows.append(parts)
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
    except OSError:
        font = ImageFont.load_default()
    aim(cam, "threequarter")
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    # Let the red read through the shell.
    for mat in bpy.data.materials:
        if mat.node_tree is None or mat.name.startswith("Noclip") or mat.name.startswith("Shadow"):
            continue
        node = mat.node_tree.nodes.get("Principled BSDF")
        if node is None or "Alpha" not in node.inputs:
            continue
        node.inputs["Alpha"].default_value = 0.42
        mat.blend_method = "BLEND"
        mat.show_transparent_back = True
    for parts in rows:
        clip, t, kind, a, b, depth = parts
        frame = nearest(frames, clip, float(t))
        g.apply_frame(arm, frame)
        place_contact(shadow, arm)
        n.clear_hot()
        hit = {"a": a, "b": b, "kind": "self", "faces": ()}
        n.paint(hit)
        hot = bpy.data.objects.get("NoclipHot")
        if hot is not None:
            for vert in hot.data.vertices:
                direction = vert.co - Vector((0.0, 0.0, 1.0))
                if direction.length > 0.001:
                    vert.co += direction.normalized() * 0.02
            hot.data.update()
        raw = os.path.join(NOCLIP, "_raw.png")
        scene.render.filepath = raw
        bpy.ops.render.render(write_still=True)
        n.clear_hot()
        image = Image.open(raw).convert("RGB")
        draw = ImageDraw.Draw(image)
        label = "%s  %.2fs  %s  %.2fcm  %s %s" % (clip, frame["t"], kind, float(depth) * 100.0, a, b)
        draw.rectangle((8, 8, 12 + 8 * len(label), 34), fill=(0, 0, 0))
        draw.text((14, 10), label, fill=(245, 245, 245), font=font)
        out = os.path.join(NOCLIP, "%s-%.2f-%s.png" % (clip, frame["t"], kind))
        save_small(out, image)
        print("OVERLAP", out, os.path.getsize(out), label)
        if os.path.exists(raw):
            os.remove(raw)


def main():
    os.makedirs(OUT, exist_ok=True)
    frames = g.load_keys(KEYS)
    arm, cam, shadow, scene = setup()
    render_strips(arm, cam, shadow, scene, frames)
    render_overlap(arm, cam, shadow, scene, frames)
    print("EXIT")


if __name__ == "__main__":
    main()
