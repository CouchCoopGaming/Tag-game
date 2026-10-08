"""Range-of-motion no-clip for the clearance candidate.

Absolute depth. No bind subtraction. rigJoint is the MenuNoClip rest count:
directed piece pairs that fail at rest. pose is a directed pair that fails on
a live frame and did not fail at rest.
"""
import json
import os
import sys

import bpy
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_loco_stills as loco

ROOT = loco.ROOT
CAND = os.environ.get(
    "PROVE_FBX",
    os.path.join(
        ROOT, "Assets", "Art", "Characters", "HiPoly", "Candidate", "Dummy_Mannequin_Tan_Hier_Clearance.fbx"
    ),
)
OUT = os.environ.get("PROVE_OUT", os.path.join(ROOT, "Docs", "LocoStills", "pass5"))
STORROR = os.environ.get("STORROR_JSON", "/tmp/loco/storror")


def load():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=CAND)
    arm = bpy.data.objects["DummyArmature"]
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    return arm


def pack(arm):
    packed = {}
    for name in loco.active_meshes():
        verts, polys = loco.world_verts(name)
        packed[name] = (verts, BVHTree.FromPolygons(verts, polys))
    return packed


def directed_fails(arm, packed):
    names = list(packed)
    fails = {}
    for a in names:
        for b in names:
            if a == b or not loco.boxes_near(packed[a][0], packed[b][0]):
                continue
            joined = loco.bones_joined(a, b)
            joint = None
            if joined:
                child = loco.hinge_bone(a, b)
                joint = arm.matrix_world @ arm.pose.bones[child].head
            depth = loco.side_depth(packed[a][0], packed[b][1], joined, joint, None)
            if depth > 0.0:
                fails[a + ">" + b] = depth
    return fails


def joint_bucket(pair):
    name = pair
    if "LowerLeg" in name and "UpperLeg" in name:
        return "knee"
    if "UpperLeg" in name and ("Hips" in name or "Spine" in name or "Chest" in name):
        return "hip"
    if ("Head" in name and "Neck" in name) or ("Neck" in name and "Chest" in name):
        return "neck"
    if "Foot" in name and "LowerLeg" in name:
        return "ankle"
    if "LowerArm" in name and "UpperArm" in name:
        return "elbow"
    if "Hand" in name and "LowerArm" in name:
        return "wrist"
    if "UpperArm" in name and ("Shoulder" in name or "Chest" in name):
        return "shoulder"
    return "other"


def measure(arm):
    """Absolute ground depth. The root is not lifted to hide a sole below the origin."""
    bpy.context.view_layer.update()
    world, self_max, _note, _pairs = loco.noclip(arm)
    fails = directed_fails(arm, pack(arm))
    return world, self_max, fails


def apply_angles(arm, s):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, -float(s.get("Drop", 0.0)))
    loco.set_e(arm, "Hips", s.get("Hip", 0.0), s.get("HipYaw", 0.0), s.get("HipRoll", 0.0))
    loco.set_e(arm, "Spine", s.get("Spine", 0.0), s.get("SpineYaw", 0.0), s.get("SpineRoll", 0.0))
    loco.set_e(arm, "Head", s.get("Head", 0.0), s.get("HeadYaw", 0.0), 0.0)
    # Positive thigh in the clip reaches forward. Blender negative X is that reach.
    # Negative knee in the clip is a bend. Blender positive X is that bend.
    loco.set_e(arm, "UpperLeg_L", -s.get("ThighL", 0.0), -s.get("ThighYawL", 0.0), 0.0)
    loco.set_e(arm, "UpperLeg_R", -s.get("ThighR", 0.0), s.get("ThighYawR", 0.0), 0.0)
    loco.set_e(arm, "LowerLeg_L", -s.get("KneeL", 0.0), 0.0, 0.0)
    loco.set_e(arm, "LowerLeg_R", -s.get("KneeR", 0.0), 0.0, 0.0)
    loco.set_e(arm, "Foot_L", s.get("FootL", 0.0), 0.0, 0.0)
    loco.set_e(arm, "Foot_R", s.get("FootR", 0.0), 0.0, 0.0)
    loco.set_e(arm, "UpperArm_L", s.get("ArmPitchL", 0.0), s.get("ArmYawL", 0.0), s.get("ArmRollL", 0.0))
    loco.set_e(arm, "UpperArm_R", s.get("ArmPitchR", 0.0), s.get("ArmYawR", 0.0), s.get("ArmRollR", 0.0))
    loco.set_e(arm, "LowerArm_L", s.get("ElbowL", 0.0), 0.0, 0.0)
    loco.set_e(arm, "LowerArm_R", s.get("ElbowR", 0.0), 0.0, 0.0)
    bpy.context.view_layer.update()


def P(**kwargs):
    return kwargs


EXITS = {
    "wall-run": [
        P(Hip=6, Spine=8, Head=-8, ThighL=48, ThighR=18, KneeL=-36, KneeR=-18, ArmPitchL=-70, ArmPitchR=16, ArmYawL=22, ArmYawR=-28, ElbowL=-20, ElbowR=-16, HipRoll=-16, SpineRoll=-12),
        P(Hip=22, Spine=16, Head=6, ThighL=86, ThighR=10, KneeL=-28, KneeR=-42, ArmPitchL=-52, ArmPitchR=-18, ArmYawL=14, ArmYawR=20, ElbowL=-22, ElbowR=-30, Drop=0.03),
        P(Hip=4, Spine=2, ThighL=22, ThighR=8, KneeL=-16, KneeR=-12, ArmPitchL=-18, ArmPitchR=-14, ArmYawL=16, ArmYawR=-14, ElbowL=-12, ElbowR=-10),
    ],
    "wall-jump": [
        P(Hip=14, Spine=10, Head=-6, ThighL=88, ThighR=80, KneeL=-110, KneeR=-104, ArmPitchL=-36, ArmPitchR=-28, ArmYawL=8, ArmYawR=-10, ElbowL=-96, ElbowR=-88, SpineRoll=6),
        P(Hip=20, Spine=24, Head=-16, ThighL=104, ThighR=98, KneeL=-124, KneeR=-116, ArmPitchL=-42, ArmPitchR=-34, ElbowL=-108, ElbowR=-100),
        P(Hip=-4, Spine=-8, Head=4, ThighL=12, ThighR=8, KneeL=-10, KneeR=-8, ArmYawL=62, ArmYawR=-58, ElbowL=-8, ElbowR=-6, ArmRollL=-12, ArmRollR=12),
    ],
    "climb-top": [
        P(Hip=26, Spine=38, Head=-14, ThighL=34, ThighR=16, KneeL=-48, KneeR=-22, ArmPitchL=-112, ArmPitchR=-104, ElbowL=-92, ElbowR=-86),
        P(Hip=18, Spine=28, Head=-6, ThighL=112, ThighR=14, KneeL=-126, KneeR=-20, ArmPitchL=-96, ArmPitchR=-88, ElbowL=-78, ElbowR=-70, Drop=0.04),
        P(Hip=6, Spine=4, ThighL=18, ThighR=14, KneeL=-22, KneeR=-16, ArmPitchL=-16, ArmPitchR=-12, ElbowL=-14, ElbowR=-12),
    ],
    "cling-drop": [
        P(Hip=8, Spine=6, Head=-10, ThighL=36, ThighR=28, KneeL=-40, KneeR=-24, ArmPitchL=-102, ArmPitchR=-96, ElbowL=-18, ElbowR=-16),
        P(Hip=-14, Spine=-6, Head=8, ThighL=16, ThighR=12, KneeL=-34, KneeR=-28, ArmPitchL=-22, ArmPitchR=-18, ArmYawL=46, ArmYawR=-42, ElbowL=-36, ElbowR=-30, SpineYaw=-8),
        P(Hip=4, Spine=2, Head=-4, ThighL=8, ThighR=6, KneeL=-14, KneeR=-12, ArmPitchL=18, ArmPitchR=14, ArmYawL=28, ArmYawR=-26, ElbowL=-16, ElbowR=-14),
    ],
    "vault": [
        P(Hip=20, Spine=32, Head=-8, ThighL=28, ThighR=46, KneeL=-36, KneeR=-22, ArmPitchL=-48, ArmPitchR=-40, ElbowL=-24, ElbowR=-20),
        P(Hip=8, Spine=18, Head=2, ThighL=14, ThighR=94, KneeL=-16, KneeR=-28, ArmPitchL=-16, ArmPitchR=24, ArmYawL=18, ArmYawR=-30, ElbowL=-12, ElbowR=-18, ThighYawR=38, HipYaw=-12, SpineYaw=10),
        P(Hip=4, Spine=2, ThighL=34, ThighR=-6, KneeL=-18, KneeR=-10, ArmPitchL=-22, ArmPitchR=8, ElbowL=-10, ElbowR=-8),
    ],
    "mantle": [
        P(Hip=22, Spine=30, Head=-12, ThighL=24, ThighR=20, KneeL=-30, KneeR=-26, ArmPitchL=-108, ArmPitchR=-104, ElbowL=-84, ElbowR=-80),
        P(Hip=10, Spine=16, Head=-4, ThighL=58, ThighR=52, KneeL=-48, KneeR=-44, ArmPitchL=-90, ArmPitchR=-86, ElbowL=-64, ElbowR=-60, Drop=0.03),
        P(Hip=2, Spine=6, ThighL=16, ThighR=14, KneeL=-14, KneeR=-12, ArmPitchL=-10, ArmPitchR=-8, ElbowL=-8, ElbowR=-8),
    ],
    "slide-exit": [
        P(Hip=-22, Spine=-12, Head=18, ThighL=64, ThighR=36, KneeL=-12, KneeR=-124, ArmPitchL=-30, ArmPitchR=40, ElbowL=-24, ElbowR=-32),
        P(Hip=16, Spine=6, Head=-4, ThighL=20, ThighR=52, KneeL=-18, KneeR=-72, ArmPitchL=-28, ArmPitchR=-36, ElbowL=-16, ElbowR=-40, HipYaw=8),
        P(Hip=4, Spine=2, ThighL=28, ThighR=6, KneeL=-16, KneeR=-12, ArmPitchL=-20, ArmPitchR=-12, ElbowL=-10, ElbowR=-8),
    ],
    "air-dash": [
        P(Hip=12, Spine=14, Head=-6, ThighL=22, ThighR=-8, KneeL=-16, KneeR=-8, ArmPitchL=28, ArmPitchR=22, ElbowL=-10, ElbowR=-8, SpineYaw=-6),
        P(Hip=2, Spine=2, ThighL=8, ThighR=6, KneeL=-8, KneeR=-6, ArmYawL=34, ArmYawR=-32, ElbowL=-8, ElbowR=-6),
        P(ThighL=6, ThighR=4, KneeL=-6, KneeR=-4, ArmPitchL=-12, ArmPitchR=-10, ElbowL=-8, ElbowR=-8),
    ],
    "punch": [
        P(Hip=8, Spine=6, Head=-4, ThighL=16, ThighR=-10, KneeL=-8, KneeR=-6, ArmPitchL=-20, ArmPitchR=-72, ElbowL=-18, ElbowR=-6, HipYaw=18, SpineYaw=16),
        P(Hip=-12, Spine=-4, Head=2, ThighL=8, ThighR=-16, KneeL=-14, KneeR=-10, ArmPitchL=-16, ArmPitchR=-34, ElbowL=-20, ElbowR=-104, HipYaw=-14, SpineYaw=-8, ArmRollR=-16),
        P(Hip=2, ThighL=10, ThighR=4, KneeL=-10, KneeR=-8, ArmPitchL=-14, ArmPitchR=-18, ElbowL=-12, ElbowR=-36),
    ],
    "lunge": [
        P(Hip=16, Spine=22, Head=-8, ThighL=62, ThighR=-12, KneeL=-20, KneeR=-8, ArmPitchL=-48, ArmPitchR=-40, ElbowL=-16, ElbowR=-12),
        P(Hip=-16, Spine=-6, Head=4, ThighL=40, ThighR=8, KneeL=-58, KneeR=-16, ArmPitchL=-12, ArmPitchR=-18, ElbowL=-22, ElbowR=-48, HipYaw=-8),
        P(Hip=4, Spine=2, ThighL=24, ThighR=8, KneeL=-16, KneeR=-10, ArmPitchL=-16, ArmPitchR=-12, ElbowL=-10, ElbowR=-12),
    ],
    "zip-drop": [
        P(Hip=-8, Spine=-6, Head=-8, ThighL=30, ThighR=26, KneeL=-16, KneeR=-12, ArmPitchL=-148, ArmPitchR=-142, ElbowL=-8, ElbowR=-8),
        P(Hip=12, Spine=8, Head=-6, ThighL=-6, ThighR=-4, KneeL=-12, KneeR=-10, ArmPitchL=-46, ArmPitchR=-42, ElbowL=-28, ElbowR=-24),
        P(Hip=2, Head=2, ThighL=8, ThighR=6, KneeL=-8, KneeR=-6, ArmPitchL=12, ArmPitchR=10, ArmYawL=36, ArmYawR=-34, ElbowL=-12, ElbowR=-10),
    ],
    "launch-land": [
        P(Hip=2, Spine=2, Head=4, ThighL=6, ThighR=4, KneeL=-8, KneeR=-6, ArmYawL=70, ArmYawR=-66, ElbowL=-8, ElbowR=-6),
        P(Hip=14, Spine=12, Head=-2, ThighL=44, ThighR=40, KneeL=-68, KneeR=-62, ArmPitchL=-20, ArmPitchR=-16, ElbowL=-24, ElbowR=-20, Drop=0.04),
        P(Hip=4, Spine=2, ThighL=20, ThighR=10, KneeL=-16, KneeR=-12, ArmPitchL=-14, ArmPitchR=-10, ElbowL=-10, ElbowR=-8),
    ],
    "grapple-arrive": [
        P(Hip=6, Spine=4, Head=-6, ThighL=18, ThighR=10, KneeL=-16, KneeR=-10, ArmPitchL=-96, ArmPitchR=-20, ElbowL=-14, ElbowR=-16),
        P(Hip=10, Spine=14, Head=-4, ThighL=28, ThighR=16, KneeL=-36, KneeR=-18, ArmPitchL=-74, ArmPitchR=-16, ElbowL=-76, ElbowR=-18, SpineYaw=8),
        P(Hip=4, Spine=4, ThighL=14, ThighR=10, KneeL=-14, KneeR=-10, ArmPitchL=-36, ArmPitchR=-12, ElbowL=-28, ElbowR=-12),
    ],
    "grapple-release": [
        P(Hip=4, Spine=6, Head=-2, ThighL=12, ThighR=8, KneeL=-12, KneeR=-8, ArmPitchL=-70, ArmPitchR=-14, ElbowL=-20, ElbowR=-12),
        P(Hip=2, Spine=2, Head=2, ThighL=8, ThighR=6, KneeL=-10, KneeR=-8, ArmPitchL=-26, ArmPitchR=-10, ElbowL=-90, ElbowR=-14, ArmRollL=18),
        P(ThighL=6, ThighR=4, KneeL=-6, KneeR=-4, ArmPitchL=-12, ArmPitchR=-10, ElbowL=-16, ElbowR=-10),
    ],
    "stagger": [
        P(Hip=22, Spine=18, Head=6, ThighL=18, ThighR=-14, KneeL=-28, KneeR=-12, ArmPitchL=-10, ArmPitchR=16, ArmYawL=36, ArmYawR=-28, ElbowL=-20, ElbowR=-16, SpineYaw=18),
        P(Hip=12, Spine=8, Head=2, ThighL=38, ThighR=6, KneeL=-50, KneeR=-16, ArmYawL=42, ArmYawR=-18, ElbowL=-22, ElbowR=-14, HipYaw=10, SpineYaw=14),
        P(Hip=2, Spine=2, ThighL=16, ThighR=8, KneeL=-12, KneeR=-8, ArmPitchL=-12, ArmPitchR=-10, ElbowL=-10, ElbowR=-8),
    ],
    "tag-back": [
        P(Hip=6, Spine=4, Head=-4, ThighL=8, ThighR=-6, KneeL=-10, KneeR=-8, ArmPitchL=-14, ArmPitchR=-24, ElbowL=-16, ElbowR=-28, SpineYaw=-12, HipYaw=-8),
        P(Hip=2, Spine=2, Head=2, ThighL=6, ThighR=4, KneeL=-8, KneeR=-6, ArmPitchL=6, ArmPitchR=-12, ElbowL=-12, ElbowR=-18, SpineRoll=16, HipRoll=-6),
        P(ThighL=4, ThighR=4, KneeL=-4, KneeR=-4, ArmPitchL=-10, ArmPitchR=-10, ElbowL=-8, ElbowR=-8),
    ],
    "soft-land": [
        P(Hip=6, Spine=4, Head=-2, ThighL=16, ThighR=16, KneeL=-18, KneeR=-18, ArmPitchL=-12, ArmPitchR=-10, ElbowL=-10, ElbowR=-8),
        P(Hip=12, Spine=10, Head=-4, ThighL=32, ThighR=30, KneeL=-46, KneeR=-44, ArmPitchL=-14, ArmPitchR=-12, ElbowL=-16, ElbowR=-14, Drop=0.035),
        P(Hip=2, Spine=1, ThighL=12, ThighR=8, KneeL=-10, KneeR=-8, ArmPitchL=-12, ArmPitchR=-10, ElbowL=-8, ElbowR=-8),
    ],
    "roll": [
        P(Hip=28, Spine=22, Head=-40, ThighL=62, ThighR=56, KneeL=-96, KneeR=-90, ArmPitchL=-42, ArmPitchR=-55, ElbowL=-108, ElbowR=-100, SpineRoll=8),
        P(Hip=22, Spine=26, Head=-36, ThighL=72, ThighR=66, KneeL=-108, KneeR=-102, ArmPitchL=-30, ArmPitchR=-78, ElbowL=-96, ElbowR=-112, SpineRoll=14),
        P(Hip=14, Spine=18, Head=-32, ThighL=90, ThighR=82, KneeL=-118, KneeR=-110, ArmPitchL=-18, ArmPitchR=-64, ElbowL=-80, ElbowR=-104, SpineRoll=20),
        P(Hip=8, Spine=6, Head=-38, ThighL=112, ThighR=104, KneeL=-120, KneeR=-112, ArmPitchL=-12, ArmPitchR=-28, ElbowL=-70, ElbowR=-92, SpineRoll=28, HipRoll=-10),
        P(Hip=-4, Spine=-6, Head=-22, ThighL=118, ThighR=108, KneeL=-108, KneeR=-100, ArmPitchL=-10, ArmPitchR=-8, ElbowL=-52, ElbowR=-64, SpineRoll=18),
        P(Hip=12, Spine=8, Head=-8, ThighL=38, ThighR=22, KneeL=-32, KneeR=-18, ArmPitchL=-20, ArmPitchR=-12, ElbowL=-36, ElbowR=-28, SpineRoll=8),
        P(Hip=6, Spine=2, Head=-2, ThighL=24, ThighR=10, KneeL=-18, KneeR=-12, ArmPitchL=-18, ArmPitchR=-10, ElbowL=-16, ElbowR=-14),
    ],
    "roll-absorb": [
        P(Hip=46, Spine=70, Head=-32, ThighL=100, ThighR=96, KneeL=-136, KneeR=-132, ArmPitchL=-52, ArmPitchR=-48, ElbowL=-12, ElbowR=-14),
        P(Hip=28, Spine=22, Head=-40, ThighL=62, ThighR=56, KneeL=-96, KneeR=-90, ElbowL=-108, ElbowR=-100),
        P(Hip=6, Spine=2, Head=-2, ThighL=24, ThighR=10, KneeL=-18, KneeR=-12, ElbowL=-16, ElbowR=-14),
    ],
}


def pair_joined(key):
    parts = key.split(">")
    if len(parts) != 2:
        return False
    return loco.bones_joined(parts[0], parts[1])


def note_pair(stats, clip, key, depth):
    """Count a live pair. Joined hinges stay out of the non-adjacent table."""
    row = stats.get(key)
    if row is None:
        row = {"frames": 0, "depth": 0.0, "clips": {}, "joined": pair_joined(key)}
        stats[key] = row
    row["frames"] += 1
    if depth > row["depth"]:
        row["depth"] = depth
    row["clips"][clip] = max(row["clips"].get(clip, 0.0), depth)


def clip_row(arm, name, samples, rest, pose_seen, buckets, stats):
    frames = 0
    world_max = 0.0
    self_max = 0.0
    fresh = set()
    for sample in samples:
        apply_angles(arm, sample)
        world, self_max_f, fails = measure(arm)
        frames += 1
        world_max = max(world_max, world)
        self_max = max(self_max, self_max_f)
        for key, depth in fails.items():
            if key in rest:
                continue
            fresh.add(key)
            note_pair(stats, name, key, depth)
            if key not in pose_seen and joint_bucket(key) in ("knee", "hip", "neck"):
                print(
                    "PAIR", name, key, round(depth, 2),
                    "kneeL", round(-sample.get("KneeL", 0.0), 1),
                    "kneeR", round(-sample.get("KneeR", 0.0), 1),
                    "thighL", round(sample.get("ThighL", 0.0), 1),
                    "thighR", round(sample.get("ThighR", 0.0), 1),
                    "head", round(sample.get("Head", 0.0), 1),
                    flush=True,
                )
            pose_seen.add(key)
            buckets.setdefault(joint_bucket(key), set()).add(key)
            self_max = max(self_max, depth)
    print("CLIP", name, frames, round(world_max, 2), round(self_max, 2), len(fresh), flush=True)
    return {
        "name": name, "frames": frames, "world": world_max, "self": self_max, "pose": len(fresh),
    }


def loco_samples(arm):
    jobs = [
        ("idle", lambda a: loco.bake_idle(a)),
        ("walk", lambda a: loco.bake_gait(a, loco.WALK)),
        ("run", lambda a: loco.bake_gait(a, loco.RUN)),
        ("sprint", lambda a: loco.bake_gait(a, loco.SPRINT)),
        ("start", lambda a: loco.bake_start(a)),
        ("stop", lambda a: loco.bake_stop(a, False)),
        ("skid", lambda a: loco.bake_stop(a, True)),
        ("turn", lambda a: loco.bake_turn(a)),
        ("lean", lambda a: loco.bake_lean(a)),
        ("crouch", lambda a: loco.bake_gait(a, loco.CROUCH, crouch=True)),
        ("land", lambda a: loco.bake_land(a)),
        ("blend", lambda a: loco.bake_blend(a)),
    ]
    out = []
    for name, fn in jobs:
        _frames, log = loco.bake_with_log(arm, fn)
        out.append((name, log))
        print("BAKED", name, len(log), flush=True)
    return out


def apply_log_measure(arm, entry):
    loco.apply_log(arm, entry)
    return measure(arm)


def menu_samples():
    # MenuAlive, a few beats. Same sign map as the exit clips.
    rows = []
    rows.append(("menu-idle", [P(Spine=2.2, HeadYaw=12, ThighL=1.6, KneeL=-3.2, KneeR=-2.2, ArmPitchL=1.2, ArmPitchR=1.2)]))
    rows.append(("menu-ready", [P(Hip=-10, Spine=-2, KneeL=-3, KneeR=-3, ThighL=1.5, ThighR=1.5, ElbowL=-3, ElbowR=-3, ArmYawL=-5, ArmYawR=5, ArmPitchL=-1.5, ArmPitchR=-1.5)]))
    rows.append(("menu-run", [P(Hip=8, ThighL=2.2, ThighR=-2.2, KneeL=-3.2, KneeR=-2.2, ArmPitchL=-1.8, ArmPitchR=1.8, ElbowL=-2, ElbowR=-2, HeadYaw=6, Head=-1.2, Spine=-3)]))
    rows.append(("menu-step", [P(Hip=-14, ThighL=2.2, ThighR=-1.5, KneeL=-3, KneeR=-2, ArmPitchL=-1.5, ArmPitchR=-1.5, Spine=-1.5)]))
    rows.append(("menu-cheer", [P(Hip=-16, HeadYaw=10, Spine=-2, ArmPitchL=-1.5, ArmPitchR=-1.5, ArmYawL=-4, ArmYawR=4, ElbowL=-2, ElbowR=-2)]))
    rows.append(("menu-slump", [P(Hip=16, Spine=2, Head=1, HeadYaw=-8, KneeL=-3, KneeR=-3, ArmPitchL=1.5, ArmPitchR=1.5)]))
    return rows


def storror_clip(path):
    with open(path, encoding="utf-8") as fh:
        data = json.load(fh)
    samples = []
    for ang in data["joint_angles_deg"]:
        if not isinstance(ang, dict):
            continue
        if any(ang.get(key) is None for key in ("hip_flex_L", "knee_flex_L", "hip_flex_R", "knee_flex_R")):
            continue
        samples.append(P(
            ThighL=ang["hip_flex_L"],
            ThighR=ang["hip_flex_R"],
            ThighYawL=ang["hip_abd_L"] or 0.0,
            ThighYawR=-(ang["hip_abd_R"] or 0.0),
            KneeL=-(ang["knee_flex_L"] or 0.0),
            KneeR=-(ang["knee_flex_R"] or 0.0),
            FootL=ang["ankle_dorsi_L"] or 0.0,
            FootR=ang["ankle_dorsi_R"] or 0.0,
            ArmPitchL=-(ang["shoulder_flex_L"] or 0.0),
            ArmPitchR=-(ang["shoulder_flex_R"] or 0.0),
            ArmYawL=-(ang["shoulder_abd_L"] or 0.0),
            ArmYawR=ang["shoulder_abd_R"] or 0.0,
            ElbowL=-(ang["elbow_flex_L"] or 0.0),
            ElbowR=-(ang["elbow_flex_R"] or 0.0),
            Head=ang["neck_flex"] or 0.0,
            HeadYaw=ang["neck_lateral"] or 0.0,
            Hip=ang["trunk_lean_from_cam_vertical"] or 0.0,
            HipRoll=ang["spine_lateral_bend"] or 0.0,
            SpineYaw=ang["spine_twist"] or 0.0,
        ))
    return samples


def main():
    os.environ["LOCO_LIFT"] = "0"
    os.makedirs(OUT, exist_ok=True)
    arm = load()
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    rest = directed_fails(arm, pack(arm))
    print("REST rigJoint", len(rest), flush=True)
    for key, depth in sorted(rest.items(), key=lambda item: -item[1]):
        print(" ", key, round(depth, 2), flush=True)
    pose_seen = set()
    buckets = {}
    stats = {}
    rows = []
    for name, log in loco_samples(arm):
        frames = 0
        world_max = 0.0
        self_max = 0.0
        fresh = set()
        for entry in log:
            world, self_f, fails = apply_log_measure(arm, entry)
            frames += 1
            world_max = max(world_max, world)
            self_max = max(self_max, self_f)
            for key, depth in fails.items():
                if key in rest:
                    continue
                fresh.add(key)
                note_pair(stats, name, key, depth)
                if key not in pose_seen and joint_bucket(key) in ("knee", "hip", "neck"):
                    print("PAIR", name, key, round(depth, 2), flush=True)
                pose_seen.add(key)
                buckets.setdefault(joint_bucket(key), set()).add(key)
        print("CLIP", name, frames, round(world_max, 2), round(self_max, 2), len(fresh), flush=True)
        rows.append({"name": name, "frames": frames, "world": world_max, "self": self_max, "pose": len(fresh)})
    for name, samples in EXITS.items():
        rows.append(clip_row(arm, "exit-" + name, samples, rest, pose_seen, buckets, stats))
    for name, samples in menu_samples():
        rows.append(clip_row(arm, name, samples, rest, pose_seen, buckets, stats))
    if os.path.isdir(STORROR):
        for lane in sorted(os.listdir(STORROR)):
            lane_dir = os.path.join(STORROR, lane)
            if not os.path.isdir(lane_dir):
                continue
            for filename in sorted(os.listdir(lane_dir)):
                if not filename.endswith(".json"):
                    continue
                samples = storror_clip(os.path.join(lane_dir, filename))
                clip = lane + "-" + filename[:-5]
                # Long clips: every frame is the request. Measure them.
                rows.append(clip_row(arm, clip, samples, rest, pose_seen, buckets, stats))
    frames = sum(row["frames"] for row in rows)
    world_max = max((row["world"] for row in rows), default=0.0)
    self_max = max((row["self"] for row in rows), default=0.0)
    line = "no-clip clips={0} frames={1} worldMax={2:.2f} selfMax={3:.2f} rigJoint={4} pose={5}".format(
        len(rows), frames, world_max, self_max, len(rest), len(pose_seen),
    )
    order = ("knee", "hip", "neck", "ankle", "elbow", "wrist", "shoulder", "other")
    joint_line = "joints " + " ".join("{0}={1}".format(name, len(buckets.get(name, ()))) for name in order)
    print(joint_line, flush=True)
    print(line, flush=True)
    apart = [(row["frames"], row["depth"], key, row) for key, row in stats.items() if not row["joined"]]
    apart.sort(reverse=True)
    print("TOP15 non-adjacent", flush=True)
    for frames, depth, key, row in apart[:15]:
        clips = sorted(row["clips"].items(), key=lambda item: (-item[1], item[0]))[:5]
        clip_s = " ".join("{0}@{1:.2f}".format(clip, dep) for clip, dep in clips)
        print("TOP", frames, "{0:.2f}".format(depth), key, clip_s, flush=True)
    path = os.path.join(OUT, "proof.txt")
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(line + "\n")
        fh.write(joint_line + "\n")
        fh.write("absolute depth, no bind subtraction. rigJoint = directed pairs that fail at rest. pose = directed pairs that fail on a live frame and did not fail at rest.\n")
        fh.write("clip frames worldCm selfCm poseNew\n")
        for row in rows:
            fh.write("{name} {frames} {world:.2f} {self:.2f} {pose}\n".format(**row))
        fh.write("top non-adjacent pairs by frame count\n")
        for frames, depth, key, row in apart[:15]:
            clips = sorted(row["clips"].items(), key=lambda item: (-item[1], item[0]))
            clip_s = " ".join("{0}@{1:.2f}".format(clip, dep) for clip, dep in clips[:8])
            fh.write("{0} {1:.2f} {2} {3}\n".format(frames, depth, key, clip_s))
        fh.write("pose pairs\n")
        for name in order:
            for key in sorted(buckets.get(name, ())):
                fh.write("{0} {1}\n".format(name, key))
        fh.write("rest pairs\n")
        for key, depth in sorted(rest.items(), key=lambda item: -item[1]):
            fh.write("{0} {1:.2f}\n".format(key, depth))
    print("WROTE", path, flush=True)


if __name__ == "__main__":
    main()
