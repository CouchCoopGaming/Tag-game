"""Reset the lab blend to bind pose and remeasure the cloth band.

The fit pass left the armature yawed. Costume vertices are armature-local.
This measures those vertices against the mannequin at rest and rewrites the
band lines in fit.txt. The costume-fit line is left as it was.
"""
import os
import sys

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "Art", "CharacterLab"))

import costume_lab as lab

FIT_PATH = lab.FIT_PATH
BAND_MIN = lab.BAND_MIN
BAND_MAX = lab.BAND_MAX


def reset_arm(arm):
    arm.rotation_euler = (0.0, 0.0, 0.0)
    arm.location = (0.0, 0.0, 0.0)
    arm.scale = (1.0, 1.0, 1.0)
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_euler = (0.0, 0.0, 0.0)
        bone.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()


def main():
    arm = bpy.data.objects["DummyArmature"]
    before = tuple(round(v, 4) for v in arm.rotation_euler)
    print("ARM-YAW-BEFORE", before, flush=True)
    reset_arm(arm)
    body = lab.capture_body()
    cloth_gaps = []
    acc_best = (0.0, "")
    per = []
    under = 0
    over = 0
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Lab_"):
            continue
        kind = obj.get("costume_kind")
        own = obj.get("costume_own")
        if kind not in ("cloth", "accessory") or own not in body:
            print("SKIP", obj.name, kind, own, flush=True)
            continue
        info = body[own]
        gaps = []
        mw = obj.matrix_world
        for vert in obj.data.vertices:
            point = mw @ vert.co
            hit = lab.visual_gap(info, point)
            if hit is None:
                continue
            gaps.append(hit[0])
        if not gaps:
            per.append((obj.name, kind, 0, None, None))
            continue
        lo, hi = min(gaps), max(gaps)
        per.append((obj.name, kind, len(gaps), lo, hi))
        if kind == "cloth":
            cloth_gaps.extend(gaps)
            under += sum(1 for g in gaps if g < BAND_MIN)
            over += sum(1 for g in gaps if g > BAND_MAX)
        elif hi > acc_best[0]:
            acc_best = (hi, obj.name)
    if not cloth_gaps:
        raise SystemExit("no cloth vertices")
    bmin, bmax = min(cloth_gaps), max(cloth_gaps)
    print(
        "CLOTH-BAND min-cm %.2f max-cm %.2f under=%d over=%d"
        % (bmin * 100.0, bmax * 100.0, under, over),
        flush=True,
    )
    print("ACCESSORY-MAX cm %.2f %s" % (acc_best[0] * 100.0, acc_best[1]), flush=True)
    for name, kind, n, lo, hi in per:
        if lo is None:
            print("PIECE %s %s verts=0" % (name, kind), flush=True)
        else:
            print(
                "PIECE %s %s verts=%d min-cm %.2f max-cm %.2f" % (name, kind, n, lo * 100.0, hi * 100.0),
                flush=True,
            )
    # Lowest point, for the stills.
    zmin = 1e9
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        for corner in obj.bound_box:
            zmin = min(zmin, (obj.matrix_world @ Vector(corner)).z)
    print("ZMIN %.4f" % zmin, flush=True)
    lines = open(FIT_PATH, encoding="utf-8").read().splitlines()
    out = []
    for line in lines:
        if line.startswith("cloth-band "):
            out.append(
                "cloth-band min=%.2f max=%.2f under=%d over=%d" % (bmin * 100.0, bmax * 100.0, under, over)
            )
        elif line.startswith("accessory-max="):
            out.append("accessory-max=%.2f piece=%s" % (acc_best[0] * 100.0, acc_best[1]))
        else:
            out.append(line)
    text = "\n".join(out) + "\n"
    if not text.startswith("costume-fit sets="):
        raise SystemExit("fit line missing")
    with open(FIT_PATH, "w", encoding="utf-8") as handle:
        handle.write(text)
    lab.save_blend()
    print("SAVED", lab.BLEND, flush=True)


if __name__ == "__main__":
    main()
