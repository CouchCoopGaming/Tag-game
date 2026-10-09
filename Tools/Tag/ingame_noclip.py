"""Measure interpenetration on the FBX using the game's pose keys.

Reads the 30 fps dump from StrafeJumpSim --pose-keys. Does not move
vertices and does not lift the root to escape a solid. A negative Drop
in the dump is the clip's own visual offset (the slide body drop).
"""
import math
import os
import sys

import bpy
from mathutils import Euler, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.dirname(__file__))
import noclip_check as n

FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
KEYS = os.environ.get("POSE_KEYS", "/tmp/pose_keys.tsv")
SEAT = 0.0


def rad(deg):
    return deg * math.pi / 180.0


def load_keys(path):
    frames = []
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            parts = line.split("\t")
            nums = [float(v) for v in parts[1:]]
            frames.append({
                "clip": parts[0],
                "t": nums[0],
                "thL": nums[1], "thR": nums[2], "knL": nums[3], "knR": nums[4],
                "yawL": nums[5], "yawR": nums[6],
                "armL": nums[7], "armR": nums[8], "armYawL": nums[9], "armYawR": nums[10],
                "rollL": nums[11], "rollR": nums[12],
                "elbL": nums[13], "elbR": nums[14],
                "hip": nums[15], "spine": nums[16], "head": nums[17], "lean": nums[18],
                "hipYaw": nums[19], "spineYaw": nums[20],
                "footL": nums[21], "footR": nums[22], "drop": nums[23],
                "elbYawL": nums[24] if len(nums) > 24 else 0.0,
                "elbYawR": nums[25] if len(nums) > 25 else 0.0,
                "shoulderL": nums[26] if len(nums) > 26 else 0.0,
                "thRollL": nums[27] if len(nums) > 27 else 0.0,
                "thRollR": nums[28] if len(nums) > 28 else 0.0,
                "hipDrop": nums[29] if len(nums) > 29 else 0.0,
            })
    return frames


def clear_pose(arm):
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")


def set_bone(arm, name, x, y, z):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = Euler((rad(x), rad(y), rad(z)), "XYZ")


def apply_frame(arm, frame):
    clear_pose(arm)
    set_bone(arm, "Hips", frame["hip"], frame["hipYaw"], frame["lean"])
    set_bone(arm, "Spine", frame["spine"], frame["spineYaw"], 0.0)
    set_bone(arm, "Head", frame["head"], 0.0, 0.0)
    set_bone(arm, "UpperLeg_L", -frame["thL"], frame["yawL"], frame.get("thRollL", 0.0))
    set_bone(arm, "LowerLeg_L", -frame["knL"], frame.get("knYawL", 0.0), frame.get("knRollL", 0.0))
    set_bone(arm, "Foot_L", -frame["footL"], 0.0, 0.0)
    set_bone(arm, "UpperLeg_R", -frame["thR"], frame["yawR"], frame.get("thRollR", 0.0))
    set_bone(arm, "LowerLeg_R", -frame["knR"], frame.get("knYawR", 0.0), frame.get("knRollR", 0.0))
    set_bone(arm, "Foot_R", -frame["footR"], 0.0, 0.0)
    set_bone(arm, "UpperArm_L", frame["armL"], frame["armYawL"], frame["rollL"])
    set_bone(arm, "LowerArm_L", frame["elbL"], frame.get("elbYawL", 0.0), frame.get("elbRollL", 0.0))
    set_bone(arm, "UpperArm_R", frame["armR"], frame["armYawR"], frame["rollR"])
    set_bone(arm, "LowerArm_R", frame["elbR"], frame.get("elbYawR", 0.0), frame.get("elbRollR", 0.0))
    if abs(frame.get("shoulderL", 0.0)) > 0.001:
        set_bone(arm, "Shoulder_L", 0.0, frame["shoulderL"], 0.0)
    # Hips is connected to Root, so a Hips location key does not move.
    # Root has no skin. Dropping Root drops the pelvis bone and the legs.
    # The armature object stays put. This is not the visual-root drop column.
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, -frame.get("hipDrop", 0.0), 0.0)
    arm.location = Vector((0.0, 0.0, frame["drop"] + SEAT))
    bpy.context.view_layer.update()


def mesh_min_z(name):
    obj = bpy.data.objects.get(name)
    if obj is None:
        return None
    return min((obj.matrix_world @ v.co).z for v in obj.data.vertices)


def add_ground():
    bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "Ground"


def prepare(arm):
    """FitVisual seats the rest sole on the floor once. Clips then add their own drop."""
    global SEAT
    clear_pose(arm)
    arm.location = Vector((0.0, 0.0, 0.0))
    bpy.context.view_layer.update()
    sole = min(mesh_min_z("Mesh_Foot_L"), mesh_min_z("Mesh_Foot_R"))
    SEAT = -sole
    print("SEAT_CM", round(SEAT * 100.0, 2))
    rest_hits = n.scan_frame(arm)
    rest = n.rest_map(rest_hits)
    rig = max(rest.values()) if rest else 0.0
    print("RIGJOINT joined_pairs", len(rest), "cm", n.cm(rig))
    return rest, rig


def main():
    frames = load_keys(KEYS)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    add_ground()
    rest, rig = prepare(arm)

    clips = []
    order = []
    for frame in frames:
        if frame["clip"] not in clips:
            clips.append(frame["clip"])
            order.append({
                "name": frame["clip"], "frames": 0,
                "world": 0.0, "raw": 0.0, "pose": 0.0, "rig": 0.0,
                "worst": None, "raw_at": None, "world_at": None, "pose_at": None,
            })
    by_name = {row["name"]: row for row in order}
    world_max = 0.0
    pose_max = 0.0
    raw_max = 0.0
    total = 0
    for index, frame in enumerate(frames):
        played = frame
        apply_frame(arm, played)
        hits = n.scan_frame(arm)
        total += 1
        row = by_name[frame["clip"]]
        row["frames"] += 1
        for hit in hits:
            if hit["kind"] == "world" and hit["depth"] > row["world"]:
                row["world"] = hit["depth"]
                row["world_at"] = (frame["t"], hit["a"], hit["b"], hit["depth"])
            if hit["kind"] == "self" and hit["depth"] > row["raw"]:
                row["raw"] = hit["depth"]
                row["raw_at"] = (frame["t"], hit["a"], hit["b"], hit["depth"])
            if hit["kind"] == "self" and hit["depth"] > raw_max:
                raw_max = hit["depth"]
            if hit["kind"] == "self" and n.is_rig_pair(hit, rest):
                if hit["depth"] > rig:
                    rig = hit["depth"]
                if hit["depth"] > row["rig"]:
                    row["rig"] = hit["depth"]
                continue
            if n.is_pose_hit(hit, rest):
                absolute = hit["depth"]
                if absolute > row["pose"]:
                    row["pose"] = absolute
                    row["pose_at"] = (frame["t"], hit["a"], hit["b"], absolute, hit["depth"])
                    row["worst"] = (index, frame["t"], hit["a"], hit["b"], hit["kind"], hit["depth"], absolute)
                if absolute > pose_max:
                    pose_max = absolute
            if hit["kind"] == "world" and hit["depth"] > world_max:
                world_max = hit["depth"]
        if frame["clip"] == "slide" and abs(played["drop"] + 0.525) < 0.04:
            print(
                "SLIDECONTACT t", round(frame["t"], 3),
                "heelL", round(mesh_min_z("Mesh_Foot_L") * 100.0, 2),
                "heelR", round(mesh_min_z("Mesh_Foot_R") * 100.0, 2),
                "hip", round(mesh_min_z("Mesh_Hips") * 100.0, 2),
                "thighL", round(mesh_min_z("Mesh_UpperLeg_L") * 100.0, 2),
                "thighR", round(mesh_min_z("Mesh_UpperLeg_R") * 100.0, 2),
            )
        if total % 40 == 0:
            print("PROGRESS", frame["clip"], round(frame["t"], 3), flush=True)

    fails = 0
    print("RAWCLIP")
    for row in order:
        raw_at = row["raw_at"]
        pieces = "-"
        t = 0.0
        if raw_at is not None:
            t, a, b, _d = raw_at
            pieces = a + " " + b
        world_at = row["world_at"]
        world_pieces = "-"
        if world_at is not None:
            world_pieces = world_at[1] + " " + world_at[2]
        pose_at = row["pose_at"]
        pose_pieces = "-"
        pose_t = t
        if pose_at is not None:
            pose_t = pose_at[0]
            pose_pieces = pose_at[1] + " " + pose_at[2]
        # Pose is a non-adjacent pair or the world, absolute 0.5 cm.
        if row["pose"] > n.LIMIT_M:
            fails += 1
        print(
            "RAW", row["name"],
            "frames", row["frames"],
            "t", round(pose_t, 3),
            "rawPieces", pieces,
            "posePieces", pose_pieces,
            "worldPieces", world_pieces,
            "rawSelf", n.cm(row["raw"]),
            "rig", n.cm(row["rig"]),
            "pose", n.cm(row["pose"]),
            "world", n.cm(row["world"]),
        )
    print(
        "no-clip clips=" + str(len(order))
        + " frames=" + str(total)
        + " worldMax=" + str(n.cm(world_max))
        + " rawSelfMax=" + str(n.cm(raw_max))
        + " rigJoint=" + str(n.cm(rig))
        + " pose=" + str(n.cm(pose_max))
        + " fails=" + str(fails)
    )
    print("INGAME")
    if fails:
        print("NOCLIPFAIL")
    print("EXIT:0")


if __name__ == "__main__":
    main()
