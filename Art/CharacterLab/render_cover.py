"""Eight-angle cover check. Clothing pixels versus body-grey in the clothed region.

Does not save the blend. Writes Docs/Characters/pass4/cover.txt.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass1 as rp

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = "/tmp/charlab/cover"
OUT = os.environ.get(
    "COSTUME_COVER",
    os.path.join(ROOT, "Docs", "Characters", "pass4", "cover.txt"),
)
RES_X, RES_Y = 160, 280


def log(msg):
    print(msg, flush=True)


def shot(cam, path):
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.resolution_x = RES_X
    scene.render.resolution_y = RES_Y
    scene.render.filepath = path
    scene.render.film_transparent = True
    scene.render.image_settings.color_mode = "RGBA"
    bpy.ops.render.render(write_still=True)


def show_only(meshes, pred):
    for obj in meshes:
        show = pred(obj) and len(obj.data.vertices) > 0
        obj.hide_render = not show
        obj.hide_set(not show)


def main():
    os.makedirs(RAW, exist_ok=True)
    os.environ["COSTUME_ENGINE"] = "BLENDER_EEVEE"
    arm = bpy.data.objects["DummyArmature"]
    arm.rotation_euler = (0.0, 0.0, 0.0)
    arm.location = (0.0, 0.0, 0.0)
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    specs = json.load(open(rp.LOADOUTS, encoding="utf-8"))["sets"]
    only = os.environ.get("COSTUME_COVER_ONLY", "")
    if only:
        wanted = set(only.split(","))
        specs = [spec for spec in specs if spec["id"] in wanted]
    clones = []
    for spec in specs:
        show_arm, meshes = rp.clone_rig(spec["id"])
        rp.apply_loadout(meshes, spec["pieces"], spec.get("hide"))
        rp.paint(meshes, spec["color"], spec["id"])
        clones.append((spec, show_arm, meshes))
    rp.hide_source()
    rp.set_group(clones[:1], True)
    rp.set_group(clones[1:], False)
    rp.place(clones[:1], "front", 1.2, 0.0)
    mins, _maxs = rp.world_bounds(rp.shown_meshes(clones[:1]))
    z_lift = -mins.z
    rp.make_floor()
    floor = bpy.data.objects["LabFloor"]
    floor.hide_render = True
    rp.make_lights()
    rp.make_world()
    # Few samples. The count is coverage, not a beauty frame.
    rp.configure_render()
    bpy.context.scene.eevee.taa_render_samples = 4
    cam = bpy.data.objects.new("LabCam", bpy.data.cameras.new("LabCam"))
    rp.link(cam)
    cam.data.type = "ORTHO"
    rows = []
    total_cloth = 0
    total_grey = 0
    from PIL import Image

    for spec, show_arm, meshes in clones:
        rp.set_group(clones, False)
        rp.set_group([(spec, show_arm, meshes)], True)
        rp.place([(spec, show_arm, meshes)], "front", 1.2, z_lift)
        bpy.context.view_layer.update()
        hidden = set(spec.get("hide") or [])
        cloth_px = 0
        grey_px = 0
        for step in range(8):
            yaw = math.tau * step / 8.0
            eye = Vector((math.sin(yaw) * 4.2, -math.cos(yaw) * 4.2, 1.05))
            rp.aim(cam, eye, Vector((0.0, 0.0, 0.95)))
            cam.data.ortho_scale = 2.15
            body_path = os.path.join(RAW, "%s-%d-body.png" % (spec["id"], step))
            cloth_path = os.path.join(RAW, "%s-%d-cloth.png" % (spec["id"], step))
            show_only(meshes, lambda obj, hidden=hidden: obj.get("source") in hidden)
            shot(cam, body_path)
            show_only(
                meshes,
                lambda obj, allowed=set(spec["pieces"]): obj.get("piece") in allowed,
            )
            shot(cam, cloth_path)
            body = Image.open(body_path).convert("RGBA")
            cloth = Image.open(cloth_path).convert("RGBA")
            bp = body.getdata()
            cp = cloth.getdata()
            for bpix, cpix in zip(bp, cp):
                cloth_on = cpix[3] > 40
                body_on = bpix[3] > 40
                if cloth_on:
                    cloth_px += 1
                elif body_on:
                    grey_px += 1
        denom = cloth_px + grey_px
        pct = 0.0 if denom == 0 else 100.0 * grey_px / denom
        rows.append((spec["id"], pct, cloth_px, grey_px))
        total_cloth += cloth_px
        total_grey += grey_px
        log("COVER %s buriedPct=%.2f cloth=%d grey=%d" % (spec["id"], pct, cloth_px, grey_px))
    worst = max(pct for _id, pct, _c, _g in rows)
    pooled = 0.0 if total_cloth + total_grey == 0 else 100.0 * total_grey / (total_cloth + total_grey)
    line = "costume-cover sets=%d buriedPct=%.2f" % (len(rows), worst)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as handle:
        handle.write(line + "\n")
        handle.write("pooledPct=%.2f\n" % pooled)
        for name, pct, cloth_px, grey_px in rows:
            handle.write("%s buriedPct=%.2f cloth=%d grey=%d\n" % (name, pct, cloth_px, grey_px))
    log(line)
    log("POOLED %.2f" % pooled)


if __name__ == "__main__":
    main()
