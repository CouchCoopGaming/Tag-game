"""Evasion pass 3 no-clip and ground contact.

Joined parent/child overlap is rigJoint. pose is non-adjacent pairs plus
the floor, absolute, limit 0.5 cm. A planted support sole has to sit within
0.5 cm of the floor, the same band as the storror sole gap. Nothing is lifted.
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import noclip_check as n

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass3", "pose_keys.tsv"))
OUT = os.path.join(ROOT, "Docs", "EvasionStills", "pass3", "noclip")
ENTRY = 0.10
MOVE = {"stutter": 0.38, "spinL": 0.35, "spinR": 0.35, "jukeL": 0.22, "jukeR": 0.22, "dive": 0.217391 + 0.60}
CLEAR = 0.005
TARGET = 0.003


def apply_posed(arm, frame):
    arm.rotation_euler = (0.0, 0.0, 0.0)
    g.apply_frame(arm, frame)
    bank = frame.get("bank", 0.0)
    seat = frame.get("seat", 0.0)
    if abs(bank) < 0.05 and abs(seat) < 1.0e-5:
        return
    axis = Vector((-0.62, -0.78, -0.10)).normalized()
    rot = Matrix.Rotation(math.radians(bank), 4, axis)
    pivot = Vector((-0.30, -0.12, 1.15))
    loc = arm.location.copy()
    arm.location = pivot + (rot @ (loc - pivot))
    arm.location.z += seat
    arm.rotation_euler = rot.to_euler()
    bpy.context.view_layer.update()


def sole_gap(arm, side):
    pb = arm.pose.bones["Foot_" + side]
    nrm = ((arm.matrix_world @ pb.matrix).to_3x3() @ Vector((0.0, 0.0, 1.0))).normalized()
    obj = bpy.data.objects.get("Mesh_Foot_" + side)
    if obj is None:
        return 99.0
    verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    scored = sorted(verts, key=lambda v: nrm.dot(v), reverse=True)
    patch = scored[: max(16, len(scored) // 8)]
    return min(v.z for v in patch)


def hand_gap(side):
    obj = bpy.data.objects.get("Mesh_Hand_" + side)
    if obj is None:
        return 99.0, 99.0
    zs = sorted((obj.matrix_world @ v.co).z for v in obj.data.vertices)
    k = max(1, len(zs) // 6)
    return zs[0], zs[k - 1] - zs[0]


def lowest_name():
    best_z = 99.0
    best_n = ""
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        z = min((obj.matrix_world @ v.co).z for v in obj.data.vertices)
        if z < best_z:
            best_z = z
            best_n = obj.name
    return best_n, best_z


def support(clip, gl, gr):
    if clip == "jukeR":
        return "R", gr
    if clip == "jukeL":
        return "L", gl
    if clip == "spinR":
        return "L", gl
    if clip == "spinL":
        return "R", gr
    if gl <= gr:
        return "L", gl
    return "R", gr


def main():
    frames = g.load_keys(KEYS)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    g.prepare(arm)

    order = []
    seen = []
    for frame in frames:
        if frame["clip"] not in seen:
            seen.append(frame["clip"])
            order.append({
                "name": frame["clip"], "frames": 0,
                "rig": 0.0, "pose": 0.0, "world": 0.0, "pose_at": None,
                "plants": [], "move": 0, "air": [],
            })
    by_name = {row["name"]: row for row in order}
    rig_max = 0.0
    pose_max = 0.0
    world_max = 0.0
    total = 0
    ranked = []
    dive_notes = []

    for frame in frames:
        apply_posed(arm, frame)
        hits = n.scan_frame(arm)
        total += 1
        row = by_name[frame["clip"]]
        row["frames"] += 1
        for hit in hits:
            if hit["kind"] == "world":
                if hit["depth"] > row["world"]:
                    row["world"] = hit["depth"]
                if hit["depth"] > world_max:
                    world_max = hit["depth"]
                if hit["depth"] > row["pose"]:
                    row["pose"] = hit["depth"]
                    row["pose_at"] = (frame["t"], "world", hit["a"], hit["depth"])
                if hit["depth"] > pose_max:
                    pose_max = hit["depth"]
                if hit["depth"] > n.LIMIT_M:
                    ranked.append((hit["depth"], frame, "world", hit["a"], "Ground"))
                continue
            if hit.get("joined"):
                if hit["depth"] > row["rig"]:
                    row["rig"] = hit["depth"]
                if hit["depth"] > rig_max:
                    rig_max = hit["depth"]
                continue
            if hit["depth"] > row["pose"]:
                row["pose"] = hit["depth"]
                row["pose_at"] = (frame["t"], hit["a"], hit["b"], hit["depth"])
            if hit["depth"] > pose_max:
                pose_max = hit["depth"]
            if hit["depth"] > n.LIMIT_M:
                ranked.append((hit["depth"], frame, "pose", hit["a"], hit["b"]))

        clip = frame["clip"]
        t = frame["t"]
        move = MOVE[clip]
        in_move = ENTRY - 1.0e-4 <= t <= ENTRY + move + 1.0e-4
        gl = sole_gap(arm, "L")
        gr = sole_gap(arm, "R")
        if clip != "dive" and in_move:
            row["move"] += 1
            side, gap = support(clip, gl, gr)
            if -0.0008 <= gap <= CLEAR:
                row["plants"].append((t, side, gap))
            else:
                row["air"].append((t, side, gap, gl, gr))
        if clip == "dive":
            hl, span_l = hand_gap("L")
            hr, span_r = hand_gap("R")
            who, z = lowest_name()
            hips = arm.matrix_world @ arm.pose.bones["Hips"].head
            dive_notes.append((t, frame.get("bank", 0.0), min(hl, hr), max(span_l, span_r), who, z, hips.z, gl, gr))
        if total % 25 == 0:
            print("PROGRESS", clip, round(t, 3), flush=True)

    fails = 0
    lines = []
    for row in order:
        over = row["pose"] > n.LIMIT_M
        if over:
            fails += 1
        where = row["pose_at"]
        print(
            "RAW", row["name"],
            "frames", row["frames"],
            "rigJoint", n.cm(row["rig"]),
            "pose", n.cm(row["pose"]),
            "world", n.cm(row["world"]),
            "at", "-" if where is None else "%.3f %s/%s" % (where[0], where[1], where[2]),
            flush=True,
        )
        if row["name"] == "dive":
            continue
        gaps = [item[2] for item in row["plants"]]
        max_gap = max(gaps) if gaps else -1.0
        bits = ["%.3f:%s:%.2f" % (t, side, gap * 100.0) for t, side, gap in row["plants"]]
        line = (
            "ground-contact %s planted=%d/%d maxGap=%.2f frames=%s"
            % (row["name"], len(row["plants"]), row["move"], max_gap * 100.0 if gaps else -1.0, ",".join(bits))
        )
        print(line, flush=True)
        lines.append(line)
        if row["air"]:
            air_bits = ["%.3f:%s:%.2f" % (t, side, gap * 100.0) for t, side, gap, _, _ in row["air"][:8]]
            print("  airborne", row["name"], len(row["air"]), " ".join(air_bits), flush=True)

    print("DIVE", flush=True)
    for t, bank, hand, span, who, z, hipz, gl, gr in dive_notes:
        if t < 0.20 or (0.30 <= t <= 0.95) or bank > 20:
            print(
                "DIVE", "%.3f" % t, "bank", round(bank, 1),
                "hand", round(hand * 100, 2), "span", round(span * 100, 2),
                "on", who, "z", round(z * 100, 2), "hip", round(hipz, 3),
                "sole", round(min(gl, gr) * 100, 1),
                flush=True,
            )

    pose_report = 0.0 if pose_max <= n.LIMIT_M else n.cm(pose_max)
    line = (
        "no-clip clips=" + str(len(order))
        + " frames=" + str(total)
        + " worldMax=" + str(n.cm(world_max))
        + " rigJoint=" + str(n.cm(rig_max))
        + " pose=" + str(pose_report)
        + " fails=" + str(fails)
    )
    print(line, flush=True)
    ranked.sort(key=lambda item: item[0], reverse=True)
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "worst.txt"), "w", encoding="utf-8") as handle:
        handle.write(line + "\n")
        for extra in lines:
            handle.write(extra + "\n")
        picked = []
        used = set()
        for depth, frame, kind, a, b in ranked:
            key = (frame["clip"], round(frame["t"], 3))
            if key in used:
                continue
            used.add(key)
            picked.append((frame, kind, a, b, depth))
            if len(picked) == 3:
                break
        for frame, kind, a, b, depth in picked:
            handle.write("%s\t%.3f\t%s\t%s\t%s\t%.4f\n" % (frame["clip"], frame["t"], kind, a, b, depth))
        if not picked:
            handle.write("none\n")
    print("WORST", len(picked) if ranked else 0, flush=True)
    print("EXIT:0", flush=True)


if __name__ == "__main__":
    main()
