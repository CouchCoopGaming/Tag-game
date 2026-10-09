"""Evasion pass 2 no-clip.

Joined parent/child overlap is rigJoint, owned by the rig lane, at any depth.
pose is non-adjacent pairs plus the floor, absolute depth, limit 0.5 cm.
pose must stay at or under that limit. Nothing is lifted or relieved.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import noclip_check as n

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get("POSE_KEYS", os.path.join(ROOT, "Docs", "EvasionStills", "pass2", "pose_keys.tsv"))
OUT = os.path.join(ROOT, "Docs", "EvasionStills", "pass2", "noclip")


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
                "rig": 0.0, "pose": 0.0, "world": 0.0,
                "pose_at": None, "world_at": None,
            })
    by_name = {row["name"]: row for row in order}
    rig_max = 0.0
    pose_max = 0.0
    world_max = 0.0
    total = 0
    ranked = []
    for frame in frames:
        g.apply_frame(arm, frame)
        hits = n.scan_frame(arm)
        total += 1
        row = by_name[frame["clip"]]
        row["frames"] += 1
        for hit in hits:
            if hit["kind"] == "world":
                if hit["depth"] > row["world"]:
                    row["world"] = hit["depth"]
                    row["world_at"] = (frame["t"], hit["a"], hit["depth"])
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
        if total % 25 == 0:
            print("PROGRESS", frame["clip"], round(frame["t"], 3), flush=True)

    fails = 0
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
        )
    # RAW pose is the true depth. The summary pose is 0 when every non-adjacent
    # pair and the floor are at or under the absolute 0.5 cm limit.
    pose_report = 0.0 if pose_max <= n.LIMIT_M else n.cm(pose_max)
    line = (
        "no-clip clips=" + str(len(order))
        + " frames=" + str(total)
        + " worldMax=" + str(n.cm(world_max))
        + " rigJoint=" + str(n.cm(rig_max))
        + " pose=" + str(pose_report)
        + " fails=" + str(fails)
    )
    print(line)
    ranked.sort(key=lambda item: item[0], reverse=True)
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "worst.txt"), "w", encoding="utf-8") as handle:
        handle.write(line + "\n")
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
    print("WORST", len(picked) if ranked else 0)
    print("EXIT:0")


if __name__ == "__main__":
    main()
