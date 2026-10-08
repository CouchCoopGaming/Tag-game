"""Red overlap stills of the three worst in-game clips.

Same floor, same camera, same frame time for before and after.
POSE_KEYS selects the dump. SHOT is before or after.
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import noclip_check as n
import importlib.util

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
spec = importlib.util.spec_from_file_location(
    "pass17", os.path.join(ROOT, "Tools", "Tag", "render_pass17_hier.py")
)
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)

KEYS = os.environ.get("POSE_KEYS", "/tmp/pose_keys.tsv")
SHOT = os.environ.get("SHOT", "before")
OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass20", "noclip")

# Times of the three worst pose frames on the unflared keys.
def ghost_body():
    """The overlap sits inside the shell. A light body lets the red read."""
    for mat in bpy.data.materials:
        if mat.node_tree is None:
            continue
        node = mat.node_tree.nodes.get("Principled BSDF")
        if node is None or "Alpha" not in node.inputs:
            continue
        if mat.name.startswith("Noclip"):
            continue
        node.inputs["Alpha"].default_value = 0.28
        mat.blend_method = "BLEND"
        mat.show_transparent_back = True


WORST = (
    ("pad", 0.767),
    ("grapple", 0.500),
    ("punch", 0.000),
)


def pose_hit(arm, rest):
    worst = None
    for hit in n.scan_frame(arm):
        if hit["kind"] != "self":
            continue
        key = tuple(sorted((hit["a"], hit["b"])))
        rest_depth = rest.get(key, 0.0)
        if rest_depth > 0.0 and hit["depth"] <= rest_depth + 0.0005:
            continue
        if worst is None or hit["depth"] > worst["depth"]:
            worst = hit
    return worst


def nearest(frames, clip, t):
    pool = [f for f in frames if f["clip"] == clip]
    return min(pool, key=lambda f: abs(f["t"] - t))


def main():
    os.makedirs(OUT, exist_ok=True)
    frames = g.load_keys(KEYS)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    p.tint()
    ghost_body()
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    ground = bpy.data.objects["Ground"]
    ground.data.materials.append(p.make_mat("GroundMat", p.GROUND, 0.92))
    rest, _rig = g.prepare(arm)
    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = p.SKY
    bg.inputs["Strength"].default_value = 0.9
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.light_add(type="SUN", location=(2.4, 3.6, 6.0))
    sun = bpy.context.active_object
    sun.data.energy = 3.2
    p.look_at(sun, Vector((0.0, -0.2, 1.0)))
    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.data.sensor_width = 36.0
    cam.data.lens = 32.0
    cam.data.clip_start = 0.05
    cam.location = Vector((1.7, 2.6, 1.35))
    p.look_at(cam, Vector((0.05, -0.15, 1.05)))
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    from PIL import Image, ImageDraw, ImageFont

    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
    except OSError:
        font = ImageFont.load_default()
    for clip, age in WORST:
        frame = nearest(frames, clip, age)
        g.apply_frame(arm, frame)
        hit = pose_hit(arm, rest)
        n.clear_hot()
        depth = 0.0 if hit is None else hit["depth"]
        if hit is not None:
            n.paint(hit)
            hot = bpy.data.objects.get("NoclipHot")
            if hot is not None:
                for vert in hot.data.vertices:
                    direction = vert.co - Vector((0.0, 0.0, 1.0))
                    if direction.length > 0.001:
                        vert.co += direction.normalized() * 0.035
                hot.data.update()
        path = os.path.join(OUT, "%s-%s.png" % (clip, SHOT))
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        n.clear_hot()
        image = Image.open(path).convert("RGB")
        draw = ImageDraw.Draw(image)
        label = "%s  %.2fs  pose %.2fcm" % (clip, frame["t"], n.cm(depth))
        draw.rectangle((8, 8, 8 + 8 * len(label) + 12, 34), fill=(0, 0, 0))
        draw.text((14, 12), label, fill=(245, 245, 245), font=font)
        image.save(path, "PNG", optimize=True)
        if os.path.getsize(path) > 400 * 1024:
            image.quantize(colors=128, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)
        print("STILL", path, os.path.getsize(path), label)
    print("EXIT")


if __name__ == "__main__":
    main()
