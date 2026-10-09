"""Side stills with a vertical line through the support foot and a dot on the pelvis.

  POSE_KEYS=Docs/EvasionStills/pass4/pose_keys.tsv OUT_DIR=Docs/EvasionStills/pass5/before \
    blender --background --python Tools/Tag/render_evasion_hip_sides.py
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import render_evasion_pass4 as p4
import render_evasion_stills_pass4 as stills

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass4", "pose_keys.tsv"))
OUT = os.environ.get("OUT_DIR", os.path.join(ROOT, "Docs", "EvasionStills", "pass5", "before"))
# One loaded frame per clip the hip-sit rule covers this pass.
PICKS = (
    ("stutter", 0.200),
    ("spinL", 0.200),
    ("spinR", 0.200),
    ("jukeL", 0.200),
    ("jukeR", 0.200),
    ("dive-takeoff", 0.133),
    ("dive-rollup", 0.867),
)


def chosen_picks():
    raw = os.environ.get("PICKS", "")
    if not raw:
        return PICKS
    picks = []
    for bit in raw.split(","):
        name, _, t = bit.partition(":")
        picks.append((name, float(t)))
    return tuple(picks)


def nearest(frames, clip, t):
    name = "dive" if clip.startswith("dive") else clip
    pool = [f for f in frames if f["clip"] == name]
    return min(pool, key=lambda f: abs(f["t"] - t))


def main():
    from PIL import Image, ImageDraw
    os.makedirs(OUT, exist_ok=True)
    frames = g.load_keys(KEYS)
    arm, cam, shadow, scene = stills.setup()
    scene.render.resolution_x = 720
    scene.render.resolution_y = 960
    tmp = os.path.join(OUT, "_frame.png")
    for name, t in chosen_picks():
        frame = nearest(frames, name, t)
        p4.apply_posed(arm, frame)
        stills.place_contact(shadow, arm)
        view = os.environ.get("VIEW", "side")
        stills.aim(cam, view, arm)
        scene.render.filepath = tmp
        bpy.ops.render.render(write_still=True)
        tile = Image.open(tmp).convert("RGB")
        gl = p4.sole_gap(arm, "L")
        gr = p4.sole_gap(arm, "R")
        clip = frame["clip"]
        side, _gap = p4.support(clip, gl, gr)
        from bpy_extras.object_utils import world_to_camera_view
        sole = __import__("measure_evasion_hipsit", fromlist=["sole_mid"]).sole_mid(arm, side)
        hips = arm.matrix_world @ arm.pose.bones["Hips"].head
        w, h = tile.size

        def px(world):
            co = world_to_camera_view(scene, cam, world)
            return int(co.x * w), int((1.0 - co.y) * h)

        fx, fy = px(Vector((sole.x, sole.y, 0.0)))
        hx, hy = px(hips)
        draw = ImageDraw.Draw(tile)
        draw.line((fx, 0, fx, h - 1), fill=(40, 220, 90), width=3)
        r = 10
        draw.ellipse((hx - r, hy - r, hx + r, hy + r), fill=(230, 40, 40))
        path = os.path.join(OUT, "%s-%s.png" % (name, view))
        tile.save(path, "PNG", optimize=True)
        if os.path.getsize(path) > 390 * 1024:
            tile.quantize(colors=96, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)
        print("STILL", path, os.path.getsize(path), flush=True)
    if os.path.exists(tmp):
        os.remove(tmp)
    print("EXIT", flush=True)


if __name__ == "__main__":
    main()
