"""Before/after stills of the pass-7 hips and ankles, from the reimported FBX.

Side and three-quarter views of a deep crouch, a plant, a 90 degree step, and a landing.
Each still is stamped with rigJoint and the live pose count.
"""
import os
import sys

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_hip_ankle as hip
import prove_pass4 as prove
import render_loco_stills as loco

ROOT = loco.ROOT
OUT = os.path.join(ROOT, "Docs", "Models", "RigStills", "pass7")
BEFORE = os.environ.get("STILL_BEFORE", "/tmp/loco/pass7_before.fbx")
AFTER = os.environ.get(
    "STILL_AFTER",
    os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Candidate", "Dummy_Mannequin_Tan_Hier_Clearance.fbx"),
)
FONT = loco.FONT
W, H = 1280, 720


def sole_band(name):
    verts, _ = loco.world_verts(name)
    zmin = min(v.z for v in verts)
    band = [v for v in verts if v.z <= zmin + 0.012]
    return band, zmin


def sole_pitch(name):
    """Positive when the toes are higher than the heel."""
    band, _zmin = sole_band(name)
    if len(band) < 4:
        return 0.0
    ys = [v.y for v in band]
    mid = (min(ys) + max(ys)) * 0.5
    front = [v.z for v in band if v.y <= mid]
    back = [v.z for v in band if v.y > mid]
    if len(front) < 2 or len(back) < 2:
        return 0.0
    return (sum(front) / len(front)) - (sum(back) / len(back))


def flatten_foot(arm, side):
    lo, hi = -35.0, 45.0
    for _ in range(14):
        mid = (lo + hi) * 0.5
        loco.set_e(arm, "Foot_" + side, mid, 0.0, 0.0)
        bpy.context.view_layer.update()
        if sole_pitch("Mesh_Foot_" + side) > 0.0:
            lo = mid
        else:
            hi = mid
    loco.set_e(arm, "Foot_" + side, (lo + hi) * 0.5, 0.0, 0.0)
    bpy.context.view_layer.update()


def plant_root(arm, names):
    """Put the lowest body vertex on the ground so no mesh sits below the floor."""
    bpy.context.view_layer.update()
    lowest = 0.0
    for name in loco.MESHES:
        if name not in bpy.data.objects:
            continue
        zmin = min(v.z for v in loco.world_verts(name)[0])
        lowest = min(lowest, zmin)
    for name in names:
        lowest = min(lowest, sole_band(name)[1])
    arm.location.z += -lowest + 0.002
    bpy.context.view_layer.update()


def leg(arm, side, flex, knee, abd, flat):
    """flex and abd in degrees. Flexion is negative local X. Abduction opens away from the midline."""
    z = -abd if side == "L" else abd
    loco.set_e(arm, "UpperLeg_" + side, -flex, 0.0, z)
    loco.set_e(arm, "LowerLeg_" + side, knee, 0.0, 0.0)
    bpy.context.view_layer.update()
    if flat:
        flatten_foot(arm, side)
    else:
        loco.set_e(arm, "Foot_" + side, 8.0, 0.0, 0.0)
        bpy.context.view_layer.update()


def apply_pose(arm, pose):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    loco.set_e(arm, "Hips", pose.get("hips", 0.0), 0.0, 0.0)
    loco.set_e(arm, "Spine", pose.get("spine", 0.0), 0.0, 0.0)
    loco.set_e(arm, "Chest", pose.get("chest", 0.0), 0.0, 0.0)
    loco.set_e(arm, "UpperArm_L", pose.get("arm", -40.0), 0.0, 18.0)
    loco.set_e(arm, "UpperArm_R", pose.get("arm", -40.0), 0.0, -18.0)
    loco.set_e(arm, "LowerArm_L", pose.get("elbow", -28.0), 0.0, 0.0)
    loco.set_e(arm, "LowerArm_R", pose.get("elbow", -28.0), 0.0, 0.0)
    for side in ("L", "R"):
        spec = pose[side]
        leg(arm, side, spec["flex"], spec["knee"], spec.get("abd", 0.0), spec.get("flat", False))
    plant_root(arm, pose["plant"])
    bpy.context.view_layer.update()


POSES = (
    {
        "name": "crouch",
        "hips": -36.0, "spine": 2.0, "chest": 2.0, "arm": -28.0, "elbow": -22.0,
        "L": {"flex": 22.0, "knee": 110.0, "abd": 8.0, "flat": True},
        "R": {"flex": 22.0, "knee": 110.0, "abd": 8.0, "flat": True},
        "plant": ("Mesh_Foot_L", "Mesh_Foot_R"),
    },
    {
        "name": "plant",
        "hips": -14.0, "spine": 3.0, "chest": 2.0, "arm": -22.0, "elbow": -16.0,
        "L": {"flex": -6.0, "knee": 16.0, "abd": 4.0, "flat": True},
        "R": {"flex": 36.0, "knee": 58.0, "abd": 4.0, "flat": True},
        "plant": ("Mesh_Foot_L", "Mesh_Foot_R"),
    },
    {
        "name": "step90",
        "hips": -6.0, "spine": 2.0, "chest": 2.0, "arm": -24.0, "elbow": -18.0,
        "L": {"flex": 90.0, "knee": 96.0, "abd": 6.0, "flat": False},
        "R": {"flex": 14.0, "knee": 22.0, "abd": 3.0, "flat": True},
        "plant": ("Mesh_Foot_R",),
    },
    {
        "name": "landing",
        "hips": -16.0, "spine": 3.0, "chest": 2.0, "arm": -18.0, "elbow": -30.0,
        "L": {"flex": 34.0, "knee": 82.0, "abd": 8.0, "flat": True},
        "R": {"flex": 34.0, "knee": 82.0, "abd": 8.0, "flat": True},
        "plant": ("Mesh_Foot_L", "Mesh_Foot_R"),
    },
)


def metrics(arm, pose):
    bpy.context.view_layer.update()
    hip_head = arm.matrix_world @ arm.pose.bones["Hips"].head
    soles = []
    lowest = 0.0
    names = pose.get("plant") or ("Mesh_Foot_L", "Mesh_Foot_R")
    for name in names:
        band, zmin = sole_band(name)
        lowest = min(lowest, zmin)
        if band:
            acc = Vector((0.0, 0.0, 0.0))
            for v in band:
                acc += v
            soles.append(acc / len(band))
    mid = sum(soles, Vector()) / len(soles)
    # Forward is -Y. Positive pelvisBack means the hips are behind the feet.
    pelvis_back = (hip_head.y - mid.y) * 100.0
    below = []
    for name in loco.MESHES:
        if name not in bpy.data.objects:
            continue
        _v, zmin = sole_band(name) if name.startswith("Mesh_Foot") else (None, min(v.z for v in loco.world_verts(name)[0]))
        if zmin < -0.004:
            below.append((name, round(zmin * 100.0, 1)))
    return pelvis_back, lowest, below


def project(scene, cam, world):
    co = world_to_camera_view(scene, cam, world)
    return co.x * W, (1.0 - co.y) * H


def stamp(path, lines, scene, cam, arm, pose):
    image = Image.open(path).convert("RGB")
    draw = ImageDraw.Draw(image)
    if pose["name"] in ("crouch", "landing", "plant", "step90"):
        hip_head = arm.matrix_world @ arm.pose.bones["Hips"].head
        band, _z = sole_band(pose["plant"][0])
        if band:
            foot = sum(band, Vector()) / len(band)
            fx, fy = project(scene, cam, Vector((foot.x, foot.y, 0.0)))
            tx, ty = project(scene, cam, Vector((foot.x, foot.y, 1.35)))
            draw.line((fx, fy, tx, ty), fill=(255, 196, 64), width=3)
            px, py = project(scene, cam, hip_head)
            r = 8
            draw.ellipse((px - r, py - r, px + r, py + r), fill=(255, 80, 64))
    image.save(path, "PNG", optimize=True)


def render_one(path, label):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = bpy.data.objects["DummyArmature"]
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    rest = prove.directed_fails(arm, prove.pack(arm))
    scene, cam = loco.scene_setup()
    scene.render.resolution_x = W
    scene.render.resolution_y = H
    loco.tint()
    notes = []
    for pose in POSES:
        apply_pose(arm, pose)
        live = prove.directed_fails(arm, prove.pack(arm))
        pose_n = len([key for key in live if key not in rest])
        back, lowest, below = metrics(arm, pose)
        line1 = "{0}  {1}  rigJoint={2}  pose={3}".format(label, pose["name"], len(rest), pose_n)
        line2 = "hip L{0:.0f}/R{1:.0f}  knee L{2:.0f}/R{3:.0f}  pelvisBack {4:.1f} cm".format(
            pose["L"]["flex"], pose["R"]["flex"], pose["L"]["knee"], pose["R"]["knee"], back,
        )
        print(line1, "|", line2, "lowest", round(lowest * 100, 2), "below", below, flush=True)
        notes.append((pose["name"], len(rest), pose_n, round(back, 1), below))
        for view in ("side", "threeq"):
            hip_w = arm.matrix_world @ arm.pose.bones["Hips"].head
            look = hip_w + Vector((0.0, 0.0, 0.02))
            if view == "side":
                cam.data.type = "ORTHO"
                cam.data.ortho_scale = 2.15
                cam.location = look + Vector((2.6, 0.05, 0.02))
            else:
                cam.data.type = "PERSP"
                cam.data.lens = 52
                cam.location = look + Vector((1.15, -1.55, 0.28))
            cam.rotation_euler = (look - cam.location).to_track_quat("-Z", "Y").to_euler()
            bpy.context.view_layer.update()
            out = os.path.join(OUT, "{0}_{1}_{2}.png".format(label, pose["name"], view))
            scene.render.filepath = out
            bpy.ops.render.render(write_still=True)
            stamp(out, (line1, line2), scene, cam, arm, pose)
            with open(os.path.join(OUT, "captions.txt"), "a", encoding="utf-8") as handle:
                handle.write("{0}\t{1}\t{2}\n".format(os.path.basename(out), line1, line2))
            print("wrote", out, os.path.getsize(out), flush=True)
    return notes


def main():
    os.makedirs(OUT, exist_ok=True)
    cap = os.path.join(OUT, "captions.txt")
    if os.path.exists(cap):
        os.remove(cap)
    render_one(BEFORE, "before")
    render_one(AFTER, "after")


if __name__ == "__main__":
    main()
