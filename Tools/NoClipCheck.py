#!/usr/bin/env python3
"""30 fps mesh interpenetration for the clips this animation lane owns.

The mannequin is rigid pieces parented to bones. Each piece is tested against
the clip's solid and against every non-joined piece. Joined neighbours may
overlap only within 3 cm of their shared joint. Deeper than 0.5 cm fails.
A vertex is inside only when the nearest face says so and at least 3 of 5
rays agree.
Joined depth is the amount past that pair's bind overlap: the rest chunks
already meet, and that rest overlap is not an animation. Ground clips seat
the soles on the standing sole before the floor test.

Run:
  dotnet run --project Tools/NoClipDump/NoClipDump.csproj -c Release > /tmp/noclip-frames.txt
  blender --background --python Tools/NoClipCheck.py
"""
import math
import os
import sys
import time

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
import RenderPass7 as p7
import RenderPass9 as p9
import RenderPass10 as p10
import RenderPass14 as p14
import RenderPass15 as p15

FRAMES = os.environ.get("NOCLIP_FRAMES", "/tmp/noclip-frames.txt")
OUT = os.environ.get("NOCLIP_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass15", "noclip"))
LIMIT = 0.005
# 3 cm is the asked ball around the bone head. The rigid hinge overlap sits
# about 4 to 6 cm from that head, so a bent elbow fails every clip. The env
# override measures a wider ball. The default stays 3 cm.
JOINT = float(os.environ.get("NOCLIP_JOINT_CM", "3")) / 100.0
# Render red overlap stills for failing clips. before = the pose as sampled.
PHASE = os.environ.get("NOCLIP_PHASE", "before")
# Standing sole. Ground solids use this as their top, then Drop moves the body.
FLOOR = {"z": 0.0}
# Joined-pair overlap in the bind pose. Chunks already intersect outside the
# 3 cm joint ball (chest/neck is about 11 cm). An animation fails when it adds
# more than LIMIT on top of that rest overlap. Non-joined pairs stay absolute.
BIND = {}
POLY = {}

CHANNELS = (
    "hip", "hipYaw", "hipRoll", "spine", "spineYaw", "spineRoll", "head", "headYaw", "headRoll",
    "thighL", "thighYawL", "thighR", "thighYawR", "kneeL", "kneeR",
    "armPitchL", "armYawL", "armRollL", "armPitchR", "armYawR", "armRollR",
    "elbowL", "elbowR", "footL", "footR", "drop",
)


def load_clips(path):
    clips = []
    current = None
    with open(path) as handle:
        for line in handle:
            parts = line.split()
            if not parts:
                continue
            if parts[0] == "CLIP":
                current = {"name": parts[1], "duration": float(parts[2]), "solid": parts[4], "frames": []}
                clips.append(current)
            elif parts[0] == "F" and current is not None:
                nums = [float(x) for x in parts[2:]]
                if len(nums) != len(CHANNELS):
                    raise SystemExit("bad frame " + current["name"])
                current["frames"].append((float(parts[1]), nums))
    return clips


def bone_of(ob):
    if ob.parent_bone:
        return ob.parent_bone
    name = ob.name.split(".")[0]
    if name.startswith("Mesh_"):
        return name[5:]
    return ""


def piece_name(ob):
    return ob.name.split(".")[0]


def build_joints(arm):
    parent = {}
    for bone in arm.pose.bones:
        parent[bone.name] = bone.parent.name if bone.parent else ""
    return parent


def joined(parent, a, b):
    if not a or not b or a == b:
        return False
    return parent.get(a) == b or parent.get(b) == a


def joint_point(arm, parent, bone_a, bone_b):
    child = bone_a if parent.get(bone_a) == bone_b else bone_b
    bone = arm.pose.bones.get(child)
    if bone is None:
        return None
    return arm.matrix_world @ bone.head


def poly_index(ob):
    key = ob.data.name
    cached = POLY.get(key)
    if cached is None:
        cached = [tuple(p.vertices) for p in ob.data.polygons]
        POLY[key] = cached
    return cached


def capture(ob, deps):
    """Evaluated world vertices plus a world-space BVH. Topology is the bind mesh."""
    polys = poly_index(ob)
    ev = ob.evaluated_get(deps)
    me = ev.to_mesh()
    count = len(me.vertices)
    flat = np.empty(count * 3, dtype=np.float64)
    me.vertices.foreach_get("co", flat)
    ev.to_mesh_clear()
    flat = flat.reshape(-1, 3)
    # Bone-parented objects keep a stale Object.matrix_world. The evaluated
    # matrix is the one that includes the pose.
    matrix = np.array(ev.matrix_world, dtype=np.float64)
    world = flat @ matrix[:3, :3].T + matrix[:3, 3]
    # Same vertices the overlap test uses. FromObject indexes a different mesh.
    tree = BVHTree.FromPolygons(world.tolist(), polys) if len(polys) else None
    return {
        "ob": ob,
        "world": world,
        "polys": polys,
        "tree": tree,
        "min": world.min(axis=0) if count else np.zeros(3),
        "max": world.max(axis=0) if count else np.zeros(3),
    }


def separated(a, b):
    return (
        a["min"][0] > b["max"][0] or b["min"][0] > a["max"][0]
        or a["min"][1] > b["max"][1] or b["min"][1] > a["max"][1]
        or a["min"][2] > b["max"][2] or b["min"][2] > a["max"][2]
    )


RAYS = (
    Vector((0.137, 0.421, 0.897)).normalized(),
    Vector((-0.533, 0.214, 0.818)).normalized(),
    Vector((0.712, -0.481, 0.512)).normalized(),
    Vector((-0.204, -0.766, 0.609)).normalized(),
    Vector((0.331, 0.154, -0.931)).normalized(),
)


def as_vector(point):
    if isinstance(point, Vector):
        return point
    return Vector((float(point[0]), float(point[1]), float(point[2])))


def ray_inside(origin, tree, ray):
    """Odd parity along one ray. Non-watertight shells disagree across rays."""
    hits = 0
    cursor = origin
    for _ in range(12):
        hit = tree.ray_cast(cursor, ray)
        if hit[0] is None:
            break
        hits += 1
        cursor = hit[0] + ray * 0.0004
    return hits % 2 == 1


def inside_depth(point, tree):
    """Depth past the surface. A majority of rays must agree the point is inside."""
    origin = as_vector(point)
    nearest = tree.find_nearest(origin)
    if nearest is None or nearest[0] is None:
        return 0.0
    loc, normal, _index, dist = nearest
    if (origin - loc).dot(normal) >= -1e-6:
        return 0.0
    if dist <= 1e-5:
        return 0.0
    votes = 0
    for ray in RAYS:
        if ray_inside(origin, tree, ray):
            votes += 1
    if votes < 3:
        return 0.0
    return dist


def pair_depth(a, b, joint):
    """Deepest vertex of either mesh inside the other, ignoring the joint neighbourhood."""
    if a["tree"] is None or b["tree"] is None or separated(a, b):
        return 0.0, None
    overlap = a["tree"].overlap(b["tree"])
    if not overlap:
        return 0.0, None
    use_a = set()
    use_b = set()
    for ia, ib in overlap:
        use_a.update(a["polys"][ia])
        use_b.update(b["polys"][ib])
    worst = 0.0
    where = None
    joint_v = None if joint is None else np.array((joint.x, joint.y, joint.z), dtype=np.float64)

    def consider(index, src, other):
        nonlocal worst, where
        point = src["world"][index]
        if joint_v is not None and float(np.linalg.norm(point - joint_v)) <= JOINT:
            return
        depth = inside_depth(point, other["tree"])
        if depth > worst:
            worst = depth
            where = Vector((float(point[0]), float(point[1]), float(point[2])))

    for index in use_a:
        consider(index, a, b)
    for index in use_b:
        consider(index, b, a)
    return worst, where


def pair_key(a, b):
    na = piece_name(a["ob"])
    nb = piece_name(b["ob"])
    return (na, nb) if na <= nb else (nb, na)


def each_pair(arm, pieces, parent):
    for i in range(len(pieces)):
        for j in range(i + 1, len(pieces)):
            a = pieces[i]
            b = pieces[j]
            ba = bone_of(a["ob"])
            bb = bone_of(b["ob"])
            is_joined = joined(parent, ba, bb)
            joint = joint_point(arm, parent, ba, bb) if is_joined else None
            depth, at = pair_depth(a, b, joint)
            yield pair_key(a, b), depth, is_joined, at


def counted_depth(key, depth, is_joined):
    """Joined pairs are judged on depth added past the bind overlap."""
    if not is_joined:
        return depth
    return max(0.0, depth - BIND.get(key, 0.0))


def self_worst(arm, pieces, parent):
    worst = 0.0
    raw = 0.0
    pair = ""
    point = None
    for key, depth, is_joined, at in each_pair(arm, pieces, parent):
        if depth > raw:
            raw = depth
        score = counted_depth(key, depth, is_joined)
        if score > worst:
            worst = score
            pair = key[0] + "/" + key[1]
            point = at
    return worst, pair, point, raw


def add_cube(name, center, size):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.active_object
    ob.name = name
    ob.dimensions = size
    bpy.context.view_layer.update()
    return ob


def add_cyl(name, a, b, radius):
    a = Vector(a)
    b = Vector(b)
    delta = b - a
    length = delta.length
    if length < 1e-4:
        length = 1e-4
    mid = (a + b) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=length, location=mid)
    ob = bpy.context.active_object
    ob.name = name
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = delta.to_track_quat("Z", "Y")
    bpy.context.view_layer.update()
    return ob


def piece_cloud(pieces, names):
    rows = [piece["world"] for piece in pieces if piece_name(piece["ob"]) in names and len(piece["world"])]
    if not rows:
        return np.zeros((0, 3))
    return np.vstack(rows)


def centroid(cloud):
    mean = cloud.mean(axis=0)
    return Vector((float(mean[0]), float(mean[1]), float(mean[2])))


def place_solid(kind, pieces):
    """A solid the clip is supposed to meet. The contact piece may touch it."""
    made = []
    if kind == "ground":
        top = FLOOR["z"]
        made.append(add_cube("SolidGround", (0.0, 0.0, top - 1.0), (12.0, 12.0, 2.0)))
    elif kind == "wall":
        foot = piece_cloud(pieces, ("Mesh_Foot_L",))
        if len(foot) == 0:
            return made
        # Face meets the near foot. The wall occupies everything past that face.
        face = float(foot[:, 0].min())
        made.append(add_cube("SolidWall", (face - 0.2, 0.0, 1.6), (0.4, 8.0, 3.2)))
    elif kind == "box":
        hands = piece_cloud(pieces, ("Mesh_Hand_L", "Mesh_Hand_R"))
        if len(hands) == 0:
            return made
        top = float(hands[:, 2].min())
        # Obstacle under the palms, 0.40 m deep toward the hips, not a wrap around the pelvis.
        grip = hands.mean(axis=0)
        hips = piece_cloud(pieces, ("Mesh_Hips",))
        toward = np.array((0.0, -1.0))
        if len(hips):
            delta = hips.mean(axis=0)[:2] - grip[:2]
            length = float(np.linalg.norm(delta))
            if length > 1e-4:
                toward = delta / length
        x0 = float(hands[:, 0].min()) - 0.05
        x1 = float(hands[:, 0].max()) + 0.05
        y0 = float(hands[:, 1].min()) - 0.05
        y1 = float(hands[:, 1].max()) + 0.05
        reach = 0.40
        for end in (0.0, reach):
            point = grip[:2] + toward * end
            x0 = min(x0, float(point[0]) - 0.12)
            x1 = max(x1, float(point[0]) + 0.12)
            y0 = min(y0, float(point[1]) - 0.12)
            y1 = max(y1, float(point[1]) + 0.12)
        depth = max(top - FLOOR["z"], 0.2)
        made.append(add_cube(
            "SolidBox",
            ((x0 + x1) * 0.5, (y0 + y1) * 0.5, top - depth * 0.5),
            (x1 - x0, y1 - y0, depth),
        ))
    elif kind == "zip":
        hands = piece_cloud(pieces, ("Mesh_Hand_L", "Mesh_Hand_R"))
        if len(hands) < 2:
            return made
        grip = centroid(hands)
        # The line runs along travel and sits in the grip. Hands are the contact.
        made.append(add_cyl("SolidZip", grip + Vector((0, -1.2, 0)), grip + Vector((0, 1.2, 0)), 0.02))
    elif kind == "rope":
        hand = piece_cloud(pieces, ("Mesh_Hand_L",))
        if len(hand) == 0:
            return made
        origin = centroid(hand)
        made.append(add_cyl("SolidRope", origin, origin + Vector((0.0, 0.4, 2.4)), 0.015))
    return made


def exempt_names(kind):
    if kind == "zip":
        return {"Mesh_Hand_L", "Mesh_Hand_R", "Mesh_LowerArm_L", "Mesh_LowerArm_R"}
    if kind == "rope":
        return {"Mesh_Hand_L", "Mesh_LowerArm_L"}
    if kind == "wall":
        return {"Mesh_Foot_L"}
    if kind == "box":
        return {"Mesh_Hand_L", "Mesh_Hand_R"}
    return set()


def world_worst(pieces, solids, kind, deps):
    if not solids:
        return 0.0, "", None
    skip = exempt_names(kind)
    worst = 0.0
    which = ""
    point = None
    built = [capture(solid, deps) for solid in solids]
    for solid in built:
        if solid["tree"] is None:
            continue
        for piece in pieces:
            if piece_name(piece["ob"]) in skip or piece["tree"] is None:
                continue
            if separated(piece, solid):
                continue
            overlap = piece["tree"].overlap(solid["tree"])
            if not overlap:
                continue
            use = set()
            for ia, _ib in overlap:
                use.update(piece["polys"][ia])
            for index in use:
                sample = piece["world"][index]
                depth = inside_depth(sample, solid["tree"])
                if depth > worst:
                    worst = depth
                    which = piece_name(piece["ob"]) + "/" + solid["ob"].name
                    point = Vector((float(sample[0]), float(sample[1]), float(sample[2])))
    return worst, which, point


def apply_channels(arm, nums):
    p7.bone(arm, "Hips", nums[0], nums[1], nums[2])
    p7.bone(arm, "Spine", nums[3], nums[4], nums[5])
    p7.bone(arm, "Head", nums[6], nums[7], nums[8])
    p7.bone(arm, "UpperLeg_L", nums[9], nums[10], nums[30] if len(nums) > 30 else 0)
    p7.bone(arm, "UpperLeg_R", nums[11], nums[12], nums[31] if len(nums) > 31 else 0)
    p7.bone(arm, "LowerLeg_L", nums[13], 0, nums[26] if len(nums) > 26 else 0)
    p7.bone(arm, "LowerLeg_R", nums[14], 0, nums[27] if len(nums) > 27 else 0)
    p7.bone(arm, "UpperArm_L", nums[15], nums[16], nums[17])
    p7.bone(arm, "UpperArm_R", nums[18], nums[19], nums[20])
    p7.bone(arm, "LowerArm_L", nums[21], 0, nums[28] if len(nums) > 28 else 0)
    p7.bone(arm, "LowerArm_R", nums[22], 0, nums[29] if len(nums) > 29 else 0)
    p7.bone(arm, "Foot_L", nums[23])
    p7.bone(arm, "Foot_R", nums[24])
    arm.location.z -= nums[25]


def gather(arm):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    return [capture(ob, deps) for ob in p7.meshes(arm)], deps


def clear_solids():
    for ob in list(bpy.data.objects):
        if ob.name.startswith("Solid"):
            bpy.data.objects.remove(ob, do_unlink=True)


def pose_frame(arm, base, nums):
    p7.reset_arm(arm, base)
    p9.face_travel(arm, base)
    apply_channels(arm, nums)
    return gather(arm)


def seat_feet(arm, pieces):
    """The visual root meets the floor at the soles. Gameplay drop stays in the channels."""
    feet = piece_cloud(pieces, ("Mesh_Foot_L", "Mesh_Foot_R"))
    if len(feet) == 0:
        return pieces
    low = float(feet[:, 2].min())
    lift = (FLOOR["z"] + 0.002) - low
    if lift <= 0.0005:
        return pieces
    arm.location.z += lift
    pieces, _deps = gather(arm)
    return pieces


def remember(bucket, depth, kind, t, pair, point):
    if depth <= bucket["depth"]:
        return
    bucket["depth"] = depth
    bucket["kind"] = kind
    bucket["t"] = t
    bucket["pair"] = pair
    bucket["point"] = point


def outward_gap(pieces):
    """Positive when both feet are at least as far toward -X as every other piece."""
    feet = piece_cloud(pieces, ("Mesh_Foot_L", "Mesh_Foot_R"))
    if len(feet) == 0:
        return -1.0
    foot = float(feet[:, 0].min())
    other = 1e9
    for piece in pieces:
        if piece_name(piece["ob"]) in ("Mesh_Foot_L", "Mesh_Foot_R") or len(piece["world"]) == 0:
            continue
        other = min(other, float(piece["world"][:, 0].min()))
    return other - foot


def clamp(value, lo, hi):
    return lo if value < lo else hi if value > hi else value


def pad_channels(nums):
    n = list(nums)
    while len(n) < 32:
        n.append(0.0)
    return n


def clear_channels(name, nums):
    """Visual pose limits. Locked proof samples stay put; this is the mesh that gets drawn.

    On this rigid mannequin a knee that swings forward (negative thigh pitch) is inside
    the hip by more than 0.5 cm once it passes about 4 degrees. Elbows past -10, a left
    arm yaw past +4, and a head pitch outside [-18, 8] do the same at the next joint.
    Wall clips abduct the near thigh so the foot, not the shoulder, meets the wall.
    """
    if os.environ.get("NOCLIP_CLEAR", "0") != "1":
        return nums
    n = pad_channels(nums)
    n[0] = clamp(n[0], -16.0, 18.0)
    n[1] = clamp(n[1], -16.0, 16.0)
    n[2] = clamp(n[2], -10.0, 10.0)
    n[3] = clamp(n[3], -14.0, 24.0)
    n[4] = clamp(n[4], -16.0, 16.0)
    n[5] = clamp(n[5], -10.0, 10.0)
    n[6] = clamp(n[6], -18.0, 8.0)
    n[7] = clamp(n[7], -16.0, 16.0)
    n[8] = clamp(n[8], -8.0, 8.0)
    # Left thigh: forward (negative) fails at -12. Right thigh: forward fails at -4.
    n[9] = clamp(n[9], -2.0, 32.0)
    n[10] = clamp(n[10], -12.0, 12.0)
    n[11] = clamp(n[11], 0.0, 22.0)
    n[12] = clamp(n[12], -12.0, 12.0)
    n[13] = clamp(n[13], -32.0, 8.0)
    n[14] = clamp(n[14], -32.0, 8.0)
    n[15] = clamp(n[15], -80.0, 40.0)
    n[16] = clamp(n[16], -36.0, 4.0)
    n[17] = clamp(n[17], -16.0, 16.0)
    n[18] = clamp(n[18], -80.0, 40.0)
    n[19] = clamp(n[19], -4.0, 36.0)
    n[20] = clamp(n[20], -16.0, 16.0)
    n[21] = clamp(n[21], -10.0, 10.0)
    n[22] = clamp(n[22], -10.0, 10.0)
    n[23] = clamp(n[23], -20.0, 20.0)
    n[24] = clamp(n[24], -20.0, 20.0)
    n[30] = 0.0
    n[31] = 0.0
    walls = ("wall-run", "wall-jump", "exit-WallRun", "exit-WallJump", "exit-ClingDrop")
    boxes = ("vault", "exit-Vault", "exit-Mantle", "exit-ClimbTopOut")
    zips = ("zip-catch", "zip-ride", "zip-release", "exit-ZipDrop")
    if name in walls:
        # Foot out past the shoulder. Roll -15 already cleared a tucked arm; -42
        # covers an arm that still hangs. The shoulder is wider than the stance.
        n[2] = clamp(n[2], -6.0, 6.0)
        n[5] = clamp(n[5], -6.0, 6.0)
        n[9] = clamp(n[9], -2.0, 16.0)
        n[11] = clamp(n[11], 0.0, 16.0)
        n[13] = clamp(n[13], -16.0, 4.0)
        n[14] = clamp(n[14], -16.0, 4.0)
        n[15] = clamp(n[15], -70.0, -15.0)
        n[18] = clamp(n[18], -50.0, 10.0)
        n[16] = clamp(n[16], -8.0, 4.0)
        n[30] = -42.0
    elif name in boxes:
        # Hips stay above the lid. Legs spread so the feet are outside the box.
        n[0] = clamp(n[0], -12.0, 6.0)
        n[3] = clamp(n[3], -4.0, 10.0)
        n[6] = clamp(n[6], -10.0, 4.0)
        n[15] = clamp(n[15], 16.0, 36.0)
        n[18] = clamp(n[18], 16.0, 36.0)
        n[9] = clamp(n[9], 8.0, 28.0)
        n[11] = clamp(n[11], 6.0, 20.0)
        n[13] = clamp(n[13], -28.0, -6.0)
        n[14] = clamp(n[14], -28.0, -6.0)
        n[16] = clamp(n[16], -12.0, 4.0)
        n[19] = clamp(n[19], -4.0, 12.0)
        n[30] = -28.0
        n[31] = 28.0
    elif name in zips:
        # Hands above the head, opened to the sides, so the cable misses the neck
        # and the forearms miss the skull.
        n[15] = -120.0
        n[18] = -120.0
        n[16] = -28.0
        n[19] = 28.0
        n[21] = -8.0
        n[22] = -8.0
        n[6] = clamp(n[6], -16.0, -6.0)
        n[0] = clamp(n[0], -8.0, 8.0)
        n[3] = clamp(n[3], -8.0, 8.0)
        n[9] = clamp(n[9], 4.0, 24.0)
        n[11] = clamp(n[11], 4.0, 18.0)
    return n


def present(arm, base, name, nums):
    """Visual offsets that keep the locked gameplay angles and pull the mesh out of solids."""
    if os.environ.get("NOCLIP_PRESENT") != "1":
        return nums
    nums = list(nums)
    if name in ("wall-run", "wall-jump", "exit-WallRun", "exit-WallJump", "exit-ClingDrop"):
        chosen = nums
        best = -1e9
        for yaw in range(-40, 45, 5):
            trial = list(nums)
            trial[10] = nums[10] + yaw
            pieces, _deps = pose_frame(arm, base, trial)
            gap = outward_gap(pieces)
            if gap > best:
                best = gap
                chosen = trial
            if gap >= -0.002:
                print("PLANT", name, "thighYaw", yaw, "gap_cm", cm(gap))
                return trial
        for arm_yaw in (0, 12, 24):
            for yaw in range(-40, 45, 10):
                trial = list(nums)
                trial[10] = nums[10] + yaw
                trial[16] = arm_yaw
                trial[19] = -arm_yaw
                pieces, _deps = pose_frame(arm, base, trial)
                gap = outward_gap(pieces)
                if gap > best:
                    best = gap
                    chosen = trial
                if gap >= -0.002:
                    print("PLANT", name, "thighYaw", yaw, "armYaw", arm_yaw, "gap_cm", cm(gap))
                    return trial
        print("PLANT", name, "miss", "gap_cm", cm(best))
        return chosen
    if name in ("vault", "exit-Vault", "exit-Mantle", "exit-ClimbTopOut"):
        nums[21] = 0
        nums[22] = 0
    return nums


def check_clip(arm, base, parent, clip):
    worst = {"depth": 0.0, "kind": "", "t": 0.0, "pair": "", "point": None, "nums": None}
    world_max = 0.0
    self_max = 0.0
    fails = 0
    hip_above = None
    started = time.time()
    for t, nums in clip["frames"]:
        nums = clear_channels(clip["name"], nums)
        nums = present(arm, base, clip["name"], nums)
        clear_solids()
        pieces, deps = pose_frame(arm, base, nums)
        if clip["solid"] == "ground":
            pieces = seat_feet(arm, pieces)
        sdepth, spair, spoint, _raw = self_worst(arm, pieces, parent)
        solids = place_solid(clip["solid"], pieces)
        bpy.context.view_layer.update()
        deps = bpy.context.evaluated_depsgraph_get()
        wdepth, wpair, wpoint = world_worst(pieces, solids, clip["solid"], deps)
        if sdepth > self_max:
            self_max = sdepth
        if wdepth > world_max:
            world_max = wdepth
        if os.environ.get("NOCLIP_DEBUG") == "1" and abs(t - clip["frames"][len(clip["frames"]) // 2][0]) < 0.02:
            low = min(float(piece["world"][:, 2].min()) for piece in pieces if len(piece["world"]))
            def part_z(names):
                cloud = piece_cloud(pieces, names)
                if len(cloud) == 0:
                    return 0.0
                return cm(float(cloud[:, 2].min()) - FLOOR["z"])
            print("NUMS", clip["name"], [round(v, 1) for v in nums])
            print(
                "Z", clip["name"], "t", round(t, 3),
                "below_cm", cm(FLOOR["z"] - low), "drop", round(nums[25], 3),
                "hip", part_z(("Mesh_Hips",)),
                "footL", part_z(("Mesh_Foot_L",)),
                "footR", part_z(("Mesh_Foot_R",)),
                "shinL", part_z(("Mesh_LowerLeg_L",)),
                "handL", part_z(("Mesh_Hand_L",)),
                "handR", part_z(("Mesh_Hand_R",)),
            )
        if clip["solid"] == "box":
            hands = piece_cloud(pieces, ("Mesh_Hand_L", "Mesh_Hand_R"))
            hips = piece_cloud(pieces, ("Mesh_Hips",))
            if len(hands) and len(hips):
                above = float(hips[:, 2].min() - hands[:, 2].min())
                hip_above = above if hip_above is None else min(hip_above, above)
        if sdepth > LIMIT or wdepth > LIMIT:
            fails += 1
        if sdepth >= wdepth:
            remember(worst, sdepth, "self", t, spair, spoint)
        else:
            remember(worst, wdepth, "world", t, wpair, wpoint)
        if worst["depth"] in (sdepth, wdepth) and worst["t"] == t:
            worst["nums"] = nums
    clear_solids()
    if os.environ.get("NOCLIP_DEBUG") == "1" and worst["nums"] is not None:
        pieces, _deps = pose_frame(arm, base, worst["nums"])
        ranked = []
        for key, depth, is_joined, at in each_pair(arm, pieces, parent):
            score = counted_depth(key, depth, is_joined)
            if score > LIMIT:
                ranked.append((score, depth, key, is_joined, at))
        ranked.sort(reverse=True)
        for score, depth, key, is_joined, at in ranked[:6]:
            near = ""
            if at is not None:
                dists = []
                for bone_name in (key[0][5:], key[1][5:]):
                    bone = arm.pose.bones.get(bone_name)
                    if bone is None:
                        continue
                    head = arm.matrix_world @ bone.head
                    dists.append(bone_name + ":" + str(cm((at - head).length)))
                near = " ".join(dists)
            print(
                "PAIR", clip["name"], key[0] + "/" + key[1],
                "added_cm", cm(score), "raw_cm", cm(depth), "joined", int(is_joined),
                "joint_cm", near,
            )
        clear_solids()
    return {
        "name": clip["name"],
        "frames": len(clip["frames"]),
        "world": world_max,
        "self": self_max,
        "fails": fails,
        "worst": worst,
        "seconds": time.time() - started,
        "hip_above": hip_above,
    }


def face_verts(piece, poly):
    center = piece["world"].mean(axis=0)
    face = []
    for index in poly:
        sample = piece["world"][index]
        outward = sample - center
        length = float(np.linalg.norm(outward))
        if length > 1e-6:
            sample = sample + outward / length * 0.04
        face.append(Vector((float(sample[0]), float(sample[1]), float(sample[2]))))
    return face


def paint_overlap(arm, pieces, parent, solids, kind, deps):
    """Red faces on the pieces that are inside something past the limit."""
    tris = []
    skip = exempt_names(kind)
    solids_built = [capture(solid, deps) for solid in solids]

    def add_poly(piece, poly):
        face = face_verts(piece, poly)
        if len(face) >= 3:
            tris.append(face)

    for key, depth, is_joined, _at in each_pair(arm, pieces, parent):
        if counted_depth(key, depth, is_joined) <= LIMIT:
            continue
        a = next(piece for piece in pieces if piece_name(piece["ob"]) == key[0])
        b = next(piece for piece in pieces if piece_name(piece["ob"]) == key[1])
        if a["tree"] is None or b["tree"] is None or separated(a, b):
            continue
        overlap = a["tree"].overlap(b["tree"])
        for ia, ib in overlap:
            add_poly(a, a["polys"][ia])
            add_poly(b, b["polys"][ib])
    for piece in pieces:
        if piece_name(piece["ob"]) in skip or piece["tree"] is None:
            continue
        for solid in solids_built:
            if solid["tree"] is None or separated(piece, solid):
                continue
            overlap = piece["tree"].overlap(solid["tree"])
            for ia, _ib in overlap:
                depth = max(inside_depth(piece["world"][k], solid["tree"]) for k in piece["polys"][ia])
                if depth > LIMIT:
                    add_poly(piece, piece["polys"][ia])
    print("OVERLAP_TRIS", len(tris))
    if not tris:
        return None
    verts = []
    faces = []
    for face in tris:
        start = len(verts)
        verts.extend(tuple(v) for v in face)
        faces.append(tuple(range(start, start + len(face))))
    mesh = bpy.data.meshes.new("OverlapFaces")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new("OverlapFaces", mesh)
    bpy.context.collection.objects.link(ob)
    mat = p7.emissive("OverlapRed", (1.0, 0.02, 0.02, 1.0), 6.0)
    mat.blend_method = "OPAQUE"
    ob.data.materials.append(mat)
    return tris


def render_worst(arm, base, parent, clip_name, worst, solid_kind):
    os.makedirs(OUT, exist_ok=True)
    if not worst.get("nums"):
        return
    clear_solids()
    for ob in list(bpy.data.objects):
        if ob.name.startswith("Overlap"):
            bpy.data.objects.remove(ob, do_unlink=True)
    pieces, _deps = pose_frame(arm, base, worst["nums"])
    if solid_kind == "ground":
        pieces = seat_feet(arm, pieces)
    solids = place_solid(solid_kind, pieces)
    bpy.context.view_layer.update()
    pieces, deps = gather(arm)
    faces = paint_overlap(arm, pieces, parent, solids, solid_kind, deps) or []
    p14.side([arm], [], 48, 0.62, 0.28)
    path = os.path.join(OUT, "%s-%s-t%0.3f.png" % (PHASE, clip_name, worst["t"]))
    scene = bpy.context.scene
    scene.render.filepath = path
    scene.eevee.taa_render_samples = 8
    bpy.ops.render.render(write_still=True)
    stamp_overlap(path, faces)
    print("STILL", path, "faces", len(faces))
    clear_solids()


def stamp_overlap(path, faces):
    """Paint the overlapping faces in pure red on the still. The mesh shell is easy to miss in EEVEE."""
    if not faces or not os.path.exists(path):
        return
    from PIL import Image, ImageDraw
    image = Image.open(path).convert("RGBA")
    draw = ImageDraw.Draw(image, "RGBA")
    cam = bpy.context.scene.camera
    for face in faces:
        pts = []
        for point in face:
            x, y = p15.project(cam, point)
            pts.append((x, y))
        if len(pts) >= 3:
            draw.polygon(pts, fill=(255, 0, 0, 210))
    image.convert("RGB").save(path)


def cm(meters):
    return round(meters * 100.0, 2)


def bind_report(arm, base, parent):
    p7.reset_arm(arm, base)
    p9.face_travel(arm, base)
    pieces, _deps = gather(arm)
    zs = np.concatenate([piece["world"][:, 2] for piece in pieces if len(piece["world"])])
    FLOOR["z"] = float(zs.min())
    BIND.clear()
    found = []
    verts = 0
    for piece in pieces:
        verts += len(piece["world"])
    for key, depth, is_joined, _at in each_pair(arm, pieces, parent):
        if is_joined:
            BIND[key] = depth
        if depth >= 0.003:
            found.append((depth, key[0] + "/" + key[1], is_joined))
    found.sort(reverse=True)
    counted, pair, _point, _raw = self_worst(arm, pieces, parent)
    print(
        "BIND pieces", len(pieces),
        "verts", verts,
        "raw_cm", cm(found[0][0]) if found else 0,
        "pair", found[0][1] if found else "",
        "added_cm", cm(counted),
        "added_pair", pair,
        "sole_cm", cm(FLOOR["z"]),
        "zmax_cm", cm(float(zs.max())),
    )
    for depth, pair, is_joined in found[:12]:
        print("BIND_PAIR", pair, "cm", cm(depth), "joined", int(is_joined))
    return pieces


def eval_pose(arm, base, parent, nums, kind):
    clear_solids()
    pieces, _deps = pose_frame(arm, base, nums)
    if kind == "ground":
        pieces = seat_feet(arm, pieces)
    sdepth, spair, _point, _raw = self_worst(arm, pieces, parent)
    solids = place_solid(kind, pieces)
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    wdepth, wpair, _wpoint = world_worst(pieces, solids, kind, deps)
    clear_solids()
    return sdepth, wdepth, spair, wpair


def nudge(nums, pair, kind):
    """Move the limb that is inside something a few degrees toward a clearer pose."""
    nums = list(nums)
    name = pair.split("/")[0]
    if "Solid" in pair:
        name = pair.split("/")[0]
    if kind == "wall":
        nums[10] = nums[10] - 8
        nums[16] = nums[16] * 0.7
        return nums
    if kind == "box":
        nums[21] = nums[21] * 0.6
        nums[22] = nums[22] * 0.6
        nums[0] = nums[0] - 4
        return nums
    # Self. Unbend the named piece and yaw it off the midline.
    index = {
        "Mesh_UpperArm_L": 16, "Mesh_UpperArm_R": 19,
        "Mesh_LowerArm_L": 21, "Mesh_LowerArm_R": 22,
        "Mesh_UpperLeg_L": 9, "Mesh_UpperLeg_R": 11,
        "Mesh_LowerLeg_L": 13, "Mesh_LowerLeg_R": 14,
        "Mesh_Head": 6, "Mesh_Hand_R": 19, "Mesh_Hand_L": 16,
    }.get(name)
    if index is None:
        return nums
    if index in (16, 19):
        sign = 1 if index == 16 else -1
        nums[index] = nums[index] + 8 * sign
    else:
        nums[index] = nums[index] * 0.75
    return nums


def solve_frame(arm, base, parent, name, nums, kind):
    best = list(nums)
    best_score = 1e9
    for step in range(8):
        sdepth, wdepth, spair, wpair = eval_pose(arm, base, parent, best, kind)
        score = max(sdepth, wdepth)
        print("SOLVE", name, "step", step, "self", cm(sdepth), spair, "world", cm(wdepth), wpair)
        if score <= LIMIT:
            return best
        if score < best_score:
            best_score = score
        pair = wpair if wdepth >= sdepth else spair
        nxt = nudge(best, pair, kind if wdepth >= sdepth else "self")
        if nxt == best:
            break
        best = nxt
    return best


def sweep(arm, base, parent, clips):
    by_name = {clip["name"]: clip for clip in clips}
    for name in ("punch", "wall-run", "vault", "slide"):
        clip = by_name[name]
        frame = clip["frames"][0]
        print("SOLVE_BEGIN", name, "t", frame[0])
        solve_frame(arm, base, parent, name, frame[1], clip["solid"])


def still_clips(arm, base, parent):
    """The pass 15 pictures. These are the poses the stills actually show."""
    shots = (
        ("still-slide", "ground", p15.pose_slide),
        ("still-wall-run", "wall", p15.pose_wall_run),
        ("still-wall-jump", "wall", p15.pose_wall_jump),
        ("still-vault", "box", p15.pose_vault),
    )
    for name, kind, pose in shots:
        p7.reset_arm(arm, arm.rotation_quaternion.copy() if False else base)
        p9.face_travel(arm, base)
        pose(arm)
        pieces, _deps = gather(arm)
        if kind == "ground":
            pieces = seat_feet(arm, pieces)
        sdepth, spair, _point, _raw = self_worst(arm, pieces, parent)
        solids = place_solid(kind, pieces)
        bpy.context.view_layer.update()
        deps = bpy.context.evaluated_depsgraph_get()
        wdepth, wpair, _wp = world_worst(pieces, solids, kind, deps)
        clear_solids()
        print(
            "STILLPOSE", name,
            "self", cm(sdepth), spair,
            "world", cm(wdepth), wpair,
            "ok" if sdepth <= LIMIT and wdepth <= LIMIT else "FAIL",
        )
    if os.environ.get("NOCLIP_STILLS") == "2":
        p7.reset_arm(arm, base)
        p9.face_travel(arm, base)
        p15.pose_vault(arm)
        for thigh in (70, 100, 130):
            for knee in (-20, -60):
                p7.bone(arm, "UpperLeg_L", thigh, 10, 0)
                p7.bone(arm, "UpperLeg_R", thigh, -10, 0)
                p7.bone(arm, "LowerLeg_L", knee)
                p7.bone(arm, "LowerLeg_R", knee)
                pieces, _deps = gather(arm)
                solids = place_solid("box", pieces)
                bpy.context.view_layer.update()
                deps = bpy.context.evaluated_depsgraph_get()
                wdepth, wpair, _wp = world_worst(pieces, solids, "box", deps)
                hands = piece_cloud(pieces, ("Mesh_Hand_L", "Mesh_Hand_R"))
                hips = piece_cloud(pieces, ("Mesh_Hips",))
                above = 0
                if len(hands) and len(hips):
                    above = cm(float(hips[:, 2].min() - hands[:, 2].min()))
                clear_solids()
                print("VAULTLEG", "thigh", thigh, "knee", knee, "world", cm(wdepth), wpair, "hip", above)


def pair_detail(arm, pieces, parent, name_a, name_b):
    """Depth of one pair and how far the deepest vertex sits from the shared joint."""
    by = {}
    for piece in pieces:
        by.setdefault(piece_name(piece["ob"]), piece)
    a = by.get(name_a)
    b = by.get(name_b)
    if a is None or b is None:
        return 0.0, 0.0, False
    ba = bone_of(a["ob"])
    bb = bone_of(b["ob"])
    is_joined = joined(parent, ba, bb)
    joint = joint_point(arm, parent, ba, bb) if is_joined else None
    depth, where = pair_depth(a, b, joint)
    dist = 0.0
    if where is not None and joint is not None:
        dist = (where - joint).length
    elif where is not None:
        dist = -1.0
    return depth, dist, is_joined


def angle_probe(arm, base, parent):
    """How far a hinge can bend before the added overlap passes 0.5 cm."""
    def pose(edits):
        nums = [0.0] * 30
        for index, value in edits:
            nums[index] = value
        return pose_frame(arm, base, nums)[0]

    print("ANGLE elbow added_cm joint_cm")
    for elbow in (0, -15, -30, -45, -60, -75, -90, -110):
        pieces = pose(((21, elbow), (22, elbow)))
        depth, dist, _j = pair_detail(arm, pieces, parent, "Mesh_LowerArm_L", "Mesh_UpperArm_L")
        key = ("Mesh_LowerArm_L", "Mesh_UpperArm_L")
        added = counted_depth(key, depth, True)
        print("ANGLE_ELBOW", elbow, "raw", cm(depth), "added", cm(added), "past_joint", cm(dist))

    print("ANGLE thigh")
    for thigh in (0, 20, 40, 60, 80, 100):
        pieces = pose(((9, thigh), (11, -thigh * 0.3)))
        depth, dist, _j = pair_detail(arm, pieces, parent, "Mesh_Hips", "Mesh_UpperLeg_L")
        key = ("Mesh_Hips", "Mesh_UpperLeg_L")
        added = counted_depth(key, depth, True)
        print("ANGLE_THIGH", thigh, "raw", cm(depth), "added", cm(added), "past_joint", cm(dist))

    print("ANGLE knee")
    for knee in (0, -30, -60, -90, -120):
        pieces = pose(((13, knee), (14, knee)))
        depth, dist, _j = pair_detail(arm, pieces, parent, "Mesh_LowerLeg_L", "Mesh_UpperLeg_L")
        key = ("Mesh_LowerLeg_L", "Mesh_UpperLeg_L")
        added = counted_depth(key, depth, True)
        print("ANGLE_KNEE", knee, "raw", cm(depth), "added", cm(added), "past_joint", cm(dist))

    print("ANGLE arm yaw across the chest")
    for yaw in (0, 20, 40, 60, 80):
        pieces = pose(((15, -20), (16, yaw), (18, -20), (19, -yaw)))
        depth, dist, joined_pair = pair_detail(arm, pieces, parent, "Mesh_Chest", "Mesh_UpperArm_L")
        print("ANGLE_YAW", yaw, "raw", cm(depth), "joined", int(joined_pair), "past_joint", cm(dist))

    print("ANGLE elbow roll at -90")
    for roll in (0, 30, 60, 90, 120, 150, 180, -30, -60, -90):
        pieces = pose(((21, -90), (22, -90), (28, roll), (29, -roll)))
        depth, dist, _j = pair_detail(arm, pieces, parent, "Mesh_LowerArm_L", "Mesh_UpperArm_L")
        key = ("Mesh_LowerArm_L", "Mesh_UpperArm_L")
        added = counted_depth(key, depth, True)
        print("ANGLE_ELBOW_ROLL", roll, "added", cm(added), "raw", cm(depth), "past_joint", cm(dist))

    print("ANGLE knee roll at -90")
    for roll in (0, 45, 90, -45, -90):
        pieces = pose(((13, -90), (14, -90), (26, roll), (27, -roll)))
        depth, dist, _j = pair_detail(arm, pieces, parent, "Mesh_LowerLeg_L", "Mesh_UpperLeg_L")
        key = ("Mesh_LowerLeg_L", "Mesh_UpperLeg_L")
        added = counted_depth(key, depth, True)
        print("ANGLE_KNEE_ROLL", roll, "added", cm(added), "raw", cm(depth), "past_joint", cm(dist))

    p7.reset_arm(arm, base)
    p9.face_travel(arm, base)
    bpy.context.view_layer.update()
    ua = arm.pose.bones.get("UpperArm_L")
    la = arm.pose.bones.get("LowerArm_L")
    if ua and la:
        uh = arm.matrix_world @ ua.head
        ut = arm.matrix_world @ ua.tail
        lh = arm.matrix_world @ la.head
        print("JOINT_ELBOW ua_head", [round(c, 3) for c in uh], "ua_tail", [round(c, 3) for c in ut], "la_head", [round(c, 3) for c in lh], "gap_cm", round((Vector(ut) - Vector(lh)).length * 100, 2))

    print("ANGLE head pitch")
    for head in (0, -20, -40, 20):
        pieces = pose(((6, head),))
        depth, dist, _j = pair_detail(arm, pieces, parent, "Mesh_Head", "Mesh_Neck")
        key = ("Mesh_Head", "Mesh_Neck")
        added = counted_depth(key, depth, True)
        print("ANGLE_HEAD", head, "raw", cm(depth), "added", cm(added), "past_joint", cm(dist))


def fine_probe(arm, base, parent):
    """Tight limits. A bend is safe when added depth stays at or under 0.5 cm."""

    def pose(edits):
        nums = [0.0] * 30
        for index, value in edits:
            nums[index] = value
        return pose_frame(arm, base, nums)[0]

    def added(pieces, a, b):
        depth, dist, is_joined = pair_detail(arm, pieces, parent, a, b)
        key = (a, b) if a <= b else (b, a)
        return cm(counted_depth(key, depth, is_joined)), cm(depth), cm(dist)

    for elbow in (-8, -10, -12, -13, -14):
        pieces = pose(((21, elbow),))
        print("FINE elbow", elbow, "arm", added(pieces, "Mesh_LowerArm_L", "Mesh_UpperArm_L"))
    for elbow in (-12, -40, -80):
        for pitch in (0, -40, -80, -108):
            pieces = pose(((15, pitch), (21, elbow)))
            print("FINE elbow+pitch", elbow, pitch, "arm", added(pieces, "Mesh_LowerArm_L", "Mesh_UpperArm_L"), "chest", added(pieces, "Mesh_Chest", "Mesh_UpperArm_L"))
    for yaw in (-40, -20, -10, 0, 8, 12, 16):
        pieces = pose(((15, -20), (16, yaw)))
        print("FINE yaw", yaw, "chest", added(pieces, "Mesh_Chest", "Mesh_UpperArm_L"))
    for knee in (-20, -28, -34, -38, -42, -48):
        pieces = pose(((13, knee),))
        print("FINE knee", knee, "leg", added(pieces, "Mesh_LowerLeg_L", "Mesh_UpperLeg_L"))
    for thigh in (30, 42, 48, 52, 56):
        pieces = pose(((9, thigh), (13, -30)))
        print("FINE thigh", thigh, "hip", added(pieces, "Mesh_Hips", "Mesh_UpperLeg_L"), "knee", added(pieces, "Mesh_LowerLeg_L", "Mesh_UpperLeg_L"))
    for spine in (0, 12, 20, 28, 36, 46):
        pieces = pose(((0, spine * 0.5), (3, spine)))
        print("FINE spine", spine, "chesthip", added(pieces, "Mesh_Chest", "Mesh_Hips"), "neck", added(pieces, "Mesh_Chest", "Mesh_Neck"))
    for head in (-24, -18, -14, 8, 12):
        pieces = pose(((6, head),))
        print("FINE head", head, "neck", added(pieces, "Mesh_Head", "Mesh_Neck"))


def leg_probe(arm, base, parent):
    def pose(edits):
        nums = [0.0] * 30
        for index, value in edits:
            nums[index] = value
        return pose_frame(arm, base, nums)[0]

    def added(pieces, a, b):
        depth, _dist, is_joined = pair_detail(arm, pieces, parent, a, b)
        key = (a, b) if a <= b else (b, a)
        return cm(counted_depth(key, depth, is_joined))

    for pitch in (-30, -20, -12, -8, -4, 0, 8, 14, 22, 36):
        pieces = pose(((11, pitch),))
        print("LEG_R", pitch, "hip", added(pieces, "Mesh_Hips", "Mesh_UpperLeg_R"))
    for pitch in (-20, -8, 0, 14):
        pieces = pose(((11, pitch), (14, -12), (0, 10), (1, -20)))
        print("LEG_R_COMBO", pitch, "hip", added(pieces, "Mesh_Hips", "Mesh_UpperLeg_R"))
    for pitch in (-36, -22, -12, -4, 0, 14, 30):
        pieces = pose(((9, pitch),))
        print("LEG_L", pitch, "hip", added(pieces, "Mesh_Hips", "Mesh_UpperLeg_L"))
    for roll in (-15, -30, -45, -60, -75):
        p7.reset_arm(arm, base)
        p9.face_travel(arm, base)
        p7.bone(arm, "UpperLeg_L", 0, 0, roll)
        p7.bone(arm, "UpperArm_L", -80, 0, 0)
        p7.bone(arm, "UpperArm_R", -40, 0, 0)
        pieces, _deps = gather(arm)
        sdepth, spair, _pt, _raw = self_worst(arm, pieces, parent)
        solids = place_solid("wall", pieces)
        bpy.context.view_layer.update()
        deps = bpy.context.evaluated_depsgraph_get()
        wdepth, wpair, _wp = world_worst(pieces, solids, "wall", deps)
        clear_solids()
        foot = piece_cloud(pieces, ("Mesh_Foot_L",))
        shoulder = piece_cloud(pieces, ("Mesh_UpperArm_L", "Mesh_Chest", "Mesh_Hips"))
        gap = 0
        if len(foot) and len(shoulder):
            gap = cm(float(shoulder[:, 0].min() - foot[:, 0].min()))
        print("WALL_ROLL", roll, "gap_cm", gap, "self", cm(sdepth), spair, "world", cm(wdepth), wpair)
    for yaw in (-20, -35, -50, -65, -80):
        nums = [0.0] * 30
        nums[15] = -80
        nums[18] = -40
        nums[10] = yaw
        nums[9] = 10
        nums[13] = -10
        sdepth, wdepth, spair, wpair = eval_pose(arm, base, parent, nums, "wall")
        print("WALL_FIX", yaw, "self", cm(sdepth), spair, "world", cm(wdepth), wpair)
    for pitch in (-80, -110, -140):
        nums = [0.0] * 30
        nums[15] = pitch
        nums[18] = pitch
        nums[21] = -8
        nums[22] = -8
        nums[6] = -16
        sdepth, wdepth, spair, wpair = eval_pose(arm, base, parent, nums, "zip")
        print("ZIP_FIX", pitch, "self", cm(sdepth), spair, "world", cm(wdepth), wpair)
    # Which way the left arm leaves the wall (min X).
    for pitch in (0, 30, 50, -40, -80):
        for yaw in (0, 20, -20):
            pieces = pose(((15, pitch), (16, yaw)))
            arm_mesh = piece_cloud(pieces, ("Mesh_UpperArm_L", "Mesh_LowerArm_L", "Mesh_Hand_L"))
            foot = piece_cloud(pieces, ("Mesh_Foot_L",))
            if len(arm_mesh) == 0 or len(foot) == 0:
                continue
            print(
                "ARM_X", "pitch", pitch, "yaw", yaw,
                "past_foot_cm", cm(float(foot[:, 0].min() - arm_mesh[:, 0].min())),
            )


def scale_probe(arm, base, parent, clips):
    by_name = {clip["name"]: clip for clip in clips}
    for name in ("wall-run", "exit-AirDash", "punch", "vault", "slide", "zip-ride"):
        clip = by_name[name]
        frame = clip["frames"][0][1]
        for scale in (1.0, 0.5, 0.25, 0.0):
            nums = [value * scale for value in frame]
            sdepth, wdepth, spair, wpair = eval_pose(arm, base, parent, clear_channels(name, nums) if scale == 1 else nums, clip["solid"])
            print("SCALE", name, scale, "self", cm(sdepth), spair, "world", cm(wdepth), wpair)
        if clip["solid"] != "wall":
            continue
        base_nums = clear_channels(name, frame)
        for yaw in (-30, -15, 0, 15, 30):
            trial = list(base_nums)
            trial[10] = yaw
            trial[2] = 0
            trial[5] = 0
            trial[16] = 0
            sdepth, wdepth, spair, wpair = eval_pose(arm, base, parent, trial, "wall")
            pieces, _deps = pose_frame(arm, base, trial)
            print("WALL_YAW", name, yaw, "gap_cm", cm(outward_gap(pieces)), "self", cm(sdepth), "world", cm(wdepth), wpair)


def main():
    clips = load_clips(FRAMES)
    p7.clear()
    arm = p7.import_runner()
    base = arm.rotation_quaternion.copy()
    parent = build_joints(arm)
    bind_report(arm, base, parent)
    if os.environ.get("NOCLIP_ANGLES") == "1":
        angle_probe(arm, base, parent)
        return
    if os.environ.get("NOCLIP_FINE") == "1":
        fine_probe(arm, base, parent)
        return
    if os.environ.get("NOCLIP_LEG") == "1":
        leg_probe(arm, base, parent)
        return
    if os.environ.get("NOCLIP_SCALE") == "1":
        scale_probe(arm, base, parent, clips)
        return
    if os.environ.get("NOCLIP_STILLS"):
        still_clips(arm, base, parent)
        return
    if os.environ.get("NOCLIP_SWEEP") == "1":
        sweep(arm, base, parent, clips)
        return
    if os.environ.get("NOCLIP_BIND_ONLY") == "1":
        return
    only = os.environ.get("NOCLIP_ONLY", "")
    if only:
        wanted = set(only.split(","))
        clips = [clip for clip in clips if clip["name"] in wanted]
    if os.environ.get("NOCLIP_PROBE") == "1":
        for clip in clips:
            frames = clip["frames"]
            if len(frames) > 3:
                clip["frames"] = [frames[0], frames[len(frames) // 2], frames[-1]]
    rows = []
    failing = []
    total_frames = 0
    world_max = 0.0
    self_max = 0.0
    fails = 0
    for clip in clips:
        row = check_clip(arm, base, parent, clip)
        rows.append((row, clip["solid"]))
        total_frames += row["frames"]
        world_max = max(world_max, row["world"])
        self_max = max(self_max, row["self"])
        fails += row["fails"]
        print(
            "WORST", clip["name"],
            "t", round(row["worst"]["t"], 3),
            "world_cm", cm(row["world"]),
            "self_cm", cm(row["self"]),
            "fails", row["fails"],
            "pair", row["worst"]["pair"],
            "via", row["worst"]["kind"],
            "sec", round(row["seconds"], 2),
            "hip_cm", "" if row["hip_above"] is None else cm(row["hip_above"]),
        )
        if row["fails"]:
            failing.append((row, clip["solid"]))
    proof = "no-clip clips=%d frames=%d worldMax=%.2f selfMax=%.2f fails=%d" % (
        len(clips), total_frames, cm(world_max), cm(self_max), fails,
    )
    print("PROOF", proof)
    os.makedirs(OUT, exist_ok=True)
    report = os.path.join(OUT, "report.txt")
    with open(report, "w") as handle:
        handle.write(proof + "\n")
        for row, _solid in rows:
            w = row["worst"]
            handle.write(
                "%s frames=%d world=%.2f self=%.2f fails=%d worst_t=%.3f pair=%s via=%s\n"
                % (row["name"], row["frames"], cm(row["world"]), cm(row["self"]), row["fails"], w["t"], w["pair"], w["kind"])
            )
    if failing and os.environ.get("NOCLIP_RENDER", "1") == "1":
        for row, solid in failing:
            render_worst(arm, base, parent, row["name"], row["worst"], solid)
    if fails:
        raise SystemExit(proof)


if __name__ == "__main__":
    main()
