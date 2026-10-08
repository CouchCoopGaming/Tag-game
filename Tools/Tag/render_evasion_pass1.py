"""Evasion clips: 30 fps no-clip on the shipped Hier mannequin, then stills.

Reads the --pose-keys-evasion dump. Does not lift the root, move shell
vertices, or damp a pose to clear the 0.5 cm limit. Joined pieces stay
exempt inside 3 cm of the joint, which is noclip_check.scan_frame.
Rest pairs at or under their rest depth are rigJoint. Anything else is pose.
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import noclip_check as n

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass1", "pose_keys.tsv"))
OUT = os.path.join(ROOT, "Docs", "EvasionStills", "pass1")
NOCLIP = os.path.join(OUT, "noclip")


def main_measure():
    frames = g.load_keys(KEYS)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    rest, rig = g.prepare(arm)

    order = []
    seen = []
    for frame in frames:
        if frame["clip"] not in seen:
            seen.append(frame["clip"])
            order.append({
                "name": frame["clip"], "frames": 0,
                "world": 0.0, "raw": 0.0, "pose": 0.0,
                "worst": None, "pose_at": None, "raw_at": None, "world_at": None,
            })
    by_name = {row["name"]: row for row in order}
    world_max = 0.0
    pose_max = 0.0
    raw_max = 0.0
    total = 0
    ranked = []
    for index, frame in enumerate(frames):
        g.apply_frame(arm, frame)
        hits = n.scan_frame(arm)
        total += 1
        row = by_name[frame["clip"]]
        row["frames"] += 1
        for hit in hits:
            if hit["kind"] == "world" and hit["depth"] > row["world"]:
                row["world"] = hit["depth"]
                row["world_at"] = (frame["t"], hit["a"], hit["b"], hit["depth"])
            if hit["kind"] == "world" and hit["depth"] > world_max:
                world_max = hit["depth"]
            if hit["kind"] != "self":
                continue
            if hit["depth"] > row["raw"]:
                row["raw"] = hit["depth"]
                row["raw_at"] = (frame["t"], hit["a"], hit["b"], hit["depth"])
            if hit["depth"] > raw_max:
                raw_max = hit["depth"]
            key = tuple(sorted((hit["a"], hit["b"])))
            rest_depth = rest.get(key, 0.0)
            if rest_depth > 0.0 and hit["depth"] <= rest_depth + 0.0005:
                ranked.append((hit["depth"], index, frame, "rigJoint", hit["a"], hit["b"]))
                continue
            absolute = hit["depth"]
            if absolute > row["pose"]:
                row["pose"] = absolute
                row["pose_at"] = (frame["t"], hit["a"], hit["b"], absolute)
                row["worst"] = hit
            if absolute > pose_max:
                pose_max = absolute
            ranked.append((absolute, index, frame, "pose", hit["a"], hit["b"]))
        if total % 20 == 0:
            print("PROGRESS", frame["clip"], round(frame["t"], 3), flush=True)

    fails = 0
    for row in order:
        pose_at = row["pose_at"]
        raw_at = row["raw_at"]
        if row["pose"] > 0.0005 or row["world"] > n.LIMIT_M:
            fails += 1
        print(
            "RAW", row["name"],
            "frames", row["frames"],
            "rawSelf", n.cm(row["raw"]),
            "pose", n.cm(row["pose"]),
            "world", n.cm(row["world"]),
            "rawAt", "-" if raw_at is None else "%.3f %s/%s" % (raw_at[0], raw_at[1], raw_at[2]),
            "poseAt", "-" if pose_at is None else pose_at[1] + "/" + pose_at[2],
            "worldAt", "-" if row["world_at"] is None else row["world_at"][1],
        )
    line = (
        "no-clip clips=" + str(len(order))
        + " frames=" + str(total)
        + " worldMax=" + str(n.cm(world_max))
        + " rawSelfMax=" + str(n.cm(raw_max))
        + " rigJoint=" + str(n.cm(rig))
        + " pose=" + str(n.cm(pose_max))
        + " fails=" + str(fails)
    )
    print(line)
    ranked.sort(key=lambda item: item[0], reverse=True)
    # Three deepest frames, one row each, pose hits first when they exist.
    picked = []
    used = set()
    for item in ranked:
        if item[3] != "pose":
            continue
        key = (item[2]["clip"], round(item[2]["t"], 3))
        if key in used:
            continue
        used.add(key)
        picked.append(item)
        if len(picked) == 3:
            break
    if len(picked) < 3:
        for item in ranked:
            key = (item[2]["clip"], round(item[2]["t"], 3))
            if key in used:
                continue
            used.add(key)
            picked.append(item)
            if len(picked) == 3:
                break
    os.makedirs(NOCLIP, exist_ok=True)
    with open(os.path.join(NOCLIP, "worst.txt"), "w", encoding="utf-8") as handle:
        handle.write(line + "\n")
        for depth, _index, frame, kind, a, b in picked:
            handle.write(
                "%s\t%.3f\t%s\t%s\t%s\t%.4f\n" % (frame["clip"], frame["t"], kind, a, b, depth)
            )
    print("WORST", len(picked))
    if fails:
        print("NOCLIPFAIL")
    print("EXIT:0")


if __name__ == "__main__":
    main_measure()
