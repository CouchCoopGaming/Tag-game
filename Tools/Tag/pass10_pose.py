#!/usr/bin/env python3
"""Pass 10: pelvis world rotation on the Hips bone, plus contacts.

The game still has no root motion. Root and the armature rotation stay at
the origin. The clip's own pelvis pitch, roll and yaw are Hips keys.
capsule_preview_m is the preview path only.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Euler, Matrix, Vector

import noclip_proof as proof
import render_hier_pass5 as p5
import render_hier_v080_stills as rh
import storror_pose as sp

PASS10 = os.path.join(rh.OUT, "pass10")
ERROR_PATH = os.path.join(PASS10, "pose_error_pass10.txt")
JSON_PATH = os.path.join(p5.OUT, "keyed_clips.json")

# Rest Hips columns: local X = +X, local Y = +Z (up), local Z = -Y (face).
REST = Matrix((
    (1.0, 0.0, 0.0),
    (0.0, 0.0, -1.0),
    (0.0, 1.0, 0.0),
))
SCORED_HIP = ("UpperLeg_L", "UpperLeg_R")
SCORED_KNEE = ("LowerLeg_L", "LowerLeg_R")
# Widen budget. Abduction is Z, hip rotation is Y, spine flex is X.
WIDEN = (("UpperLeg_L", 2), ("UpperLeg_R", 2), ("UpperLeg_L", 1), ("UpperLeg_R", 1), ("Spine", 0))
ARM_NUDGE = (
    ("UpperArm_L", 0), ("UpperArm_L", 2),
    ("UpperArm_R", 0), ("UpperArm_R", 2),
    ("LowerArm_L", 0), ("LowerArm_R", 0),
)

# 2x4 cells. Cat includes 1.43s / 2.87s / 3.60s. Tic-tac includes the plant,
# the feet-down crouch (3.67s), and the later inversion.
STORY = {
    "03_wall_drop_softland": [0, 4, 9, 13, 18, 27, 36, 45],
    "04_window_drop_softland": [0, 4, 9, 13, 18, 23, 27, 32],
    "10_wallrun_slanted": [0, 11, 22, 34, 45, 57, 68, 80],
    "12_tictac_slanted_wall": [0, 15, 30, 34, 61, 76, 110, 145],
    "15_cat_leap_wall": [0, 21, 43, 65, 86, 108, 130, 152],
}
HANDS = {("15_cat_leap_wall", 86)}
SOLE_EXTRA = {("15_cat_leap_wall", 108)}
# Feet-down crouch after the tic-tac plant. Frame 145 is the hip-height
# minimum, but the torso points at the ground, so planting the shoe bottom
# there buries the head. It is lifted clear and is not a landing.
# 111-115 cannot put both soles inside 1 cm within ±10° of knee and hip
# flexion (113 reached 1.05 cm). 109, 110 and 116 can.
TIC_SOLE = (109, 110, 116)
FOOT_WALL = {("12_tictac_slanted_wall", 34)}


def _mp(v):
    # Image-right, depth, up. det +1, so the side view matches the stick.
    return Vector((v.x, -v.z, -v.y))


def _n(v):
    if v.length < 1e-8:
        return Vector((0.0, 0.0, 1.0))
    return v.normalized()


def pelvis_of(W):
    """(euler XYZ, target 3x3) of the landmark pelvis in Blender."""
    hip = (Vector(W[23]) + Vector(W[24])) * 0.5
    sh = (Vector(W[11]) + Vector(W[12])) * 0.5
    up = _n(_mp(sh - hip))
    right = _mp(Vector(W[24]) - Vector(W[23]))
    right = _n(right - up * right.dot(up))
    fwd = up.cross(right)
    if fwd.dot(_mp(Vector(W[0]) - sh)) < 0.0:
        fwd = -fwd
    fwd = _n(fwd - up * fwd.dot(up))
    left = _n(up.cross(fwd))
    target = Matrix((
        (left.x, up.x, fwd.x),
        (left.y, up.y, fwd.y),
        (left.z, up.z, fwd.z),
    ))
    return (REST.inverted() @ target).to_euler("XYZ"), target


def _ang(a, b):
    return math.degrees((a.inverted() @ b).to_quaternion().angle)


def _diff(a, b):
    d = abs(a - b) % 360.0
    if d > 180.0:
        d = 360.0 - d
    return d


def _track(smooth, i, n):
    u = 0.0 if n <= 1 else i / (n - 1)
    return sp.blender_euler(sp.unity_pose(sp.sample_raw(smooth, u)))


def _group_err(got, track, names):
    worst = 0.0
    for name in names:
        g = got.get(name, (0.0, 0.0, 0.0))
        r = track.get(name, (0.0, 0.0, 0.0))
        for c in range(3):
            worst = max(worst, _diff(g[c], r[c]))
    return worst


def _sole_ids(arm):
    rh._reset(arm)
    bpy.context.view_layer.update()
    ids, local = {}, {}
    for side in ("L", "R"):
        obj = bpy.data.objects[f"Mesh_Foot_{side}"]
        co = [Vector((v.co.x, v.co.y, v.co.z)) for v in obj.data.vertices]
        local[side] = co
        mw = obj.matrix_world
        order = sorted(range(len(co)), key=lambda i, mw=mw, co=co: (mw @ co[i]).z)
        ids[side] = order[: max(6, len(order) // 5)]
    return ids, local


def _ensure_pose(arm):
    bpy.context.view_layer.objects.active = arm
    if bpy.context.mode != "POSE":
        bpy.ops.object.mode_set(mode="POSE")
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"


def _write(arm, eulers):
    _ensure_pose(arm)
    for pb in arm.pose.bones:
        if pb.name == "Root":
            pb.rotation_euler = (0.0, 0.0, 0.0)
            pb.location = (0.0, 0.0, 0.0)
            continue
        deg = eulers.get(pb.name)
        if deg is None:
            pb.rotation_euler = (0.0, 0.0, 0.0)
        else:
            pb.rotation_euler = Euler(
                (math.radians(deg[0]), math.radians(deg[1]), math.radians(deg[2])),
                "XYZ",
            )
    arm.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()


def _write_bone(arm, name, deg):
    pb = arm.pose.bones[name]
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = Euler(
        (math.radians(deg[0]), math.radians(deg[1]), math.radians(deg[2])),
        "XYZ",
    )
    bpy.context.view_layer.update()


def _set_height(arm, target_z):
    arm.location = (arm.location.x, arm.location.y, 0.0)
    bpy.context.view_layer.update()
    head = arm.matrix_world @ Vector(arm.pose.bones["Hips"].head)
    arm.location.z = target_z - head.z
    bpy.context.view_layer.update()


def _hip_z(arm):
    return (arm.matrix_world @ Vector(arm.pose.bones["Hips"].head)).z


def _sole_zs(ids, local, side):
    obj = bpy.data.objects[f"Mesh_Foot_{side}"]
    mw = obj.matrix_world
    zs = []
    for i in ids[side]:
        z = (mw @ local[side][i]).z
        zs.append(z)
    return min(zs), max(zs)


def _foot_min_z(local, side):
    obj = bpy.data.objects[f"Mesh_Foot_{side}"]
    mw = obj.matrix_world
    best = None
    for co in local[side]:
        z = (mw @ co).z
        if best is None or z < best:
            best = z
    return best


def _mesh_xs(names):
    best_lo = None
    best_hi = None
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not any(tok in obj.name for tok in names):
            continue
        mw = obj.matrix_world
        for v in obj.data.vertices:
            x = (mw @ v.co).x
            if best_lo is None or x < best_lo:
                best_lo = x
            if best_hi is None or x > best_hi:
                best_hi = x
    return best_lo, best_hi


def _palm_xz(side):
    pts = []
    cluster = rh._palm_cluster(bpy.data.objects["DummyArmature"], side)
    for v in cluster[::6]:
        pts.append((v.x, v.z))
    return pts


def _window(side):
    # x range that is outside the solid and within 2 cm of the face.
    if side == "-":
        return (-0.020, 0.003)
    return (-0.003, 0.020)


def _best_edge(side):
    """Smallest gap from a palm on each hand to the wall-top edge, via one shift."""
    L = _palm_xz("L")
    R = _palm_xz("R")
    lo_w, hi_w = _window(side)
    best = None
    span = 9.0
    if not L or not R:
        return None
    for p in L:
        for q in R:
            span = min(span, abs(p[0] - q[0]))
            lo = max(lo_w - p[0], lo_w - q[0])
            hi = min(hi_w - p[0], hi_w - q[0])
            if lo > hi:
                continue
            aim = (-0.006 if side == "-" else 0.006) - 0.5 * (p[0] + q[0])
            dx = min(hi, max(lo, aim))
            for T in (p[1], q[1], 0.5 * (p[1] + q[1])):
                gap = max(
                    math.hypot(p[0] + dx, p[1] - T),
                    math.hypot(q[0] + dx, q[1] - T),
                )
                if best is None or gap < best[0]:
                    best = (gap, dx, T)
    if best is None:
        return (span + 0.05, 0.0, 0.0)
    return best


def _nudge_arms(arm, eulers, side):
    """Minimal arm change, at most 10°, so both palms can share the top edge."""
    base = {name: list(eulers[name]) for name, _c in ARM_NUDGE}
    deltas = {key: 0 for key in ARM_NUDGE}
    gap0 = _best_edge(side)
    if gap0 is not None and gap0[0] <= 0.020:
        return {}, gap0

    def apply(deltas):
        for (name, comp), d in deltas.items():
            eulers[name][comp] = base[name][comp] + d
        for name, _c in ARM_NUDGE:
            _write_bone(arm, name, eulers[name])

    def rank(deltas):
        apply(deltas)
        gap = _best_edge(side)[0]
        over = max(0.0, gap - 0.020)
        return (over, sum(abs(v) for v in deltas.values()), gap)

    for _pass in range(2):
        for key in ARM_NUDGE:
            best = deltas[key]
            best_rank = rank(deltas)
            for d in range(-10, 11, 2):
                trial = dict(deltas)
                trial[key] = d
                got = rank(trial)
                if got < best_rank:
                    best_rank = got
                    best = d
            deltas[key] = best
    apply(deltas)
    used = {f"{n}[{c}]": d for (n, c), d in deltas.items() if d}
    return used, _best_edge(side)


def _roll_ground(arm, eulers, ids, local, side):
    name = f"Foot_{side}"
    base = eulers[name][0]
    best = None
    for deg in list(range(-80, 81, 10)):
        eulers[name][0] = base + deg
        _write_bone(arm, name, eulers[name])
        lo, hi = _sole_zs(ids, local, side)
        if lo > _foot_min_z(local, side) + 0.008:
            continue
        span = hi - lo
        if best is None or span < best[0]:
            best = (span, deg)
    eulers[name][0] = base + (best[1] if best else 0)
    _write_bone(arm, name, eulers[name])
    return 0 if best is None else best[1]


def _close_gap(arm, eulers, ids, local, high, name, comp):
    """Move one channel at most 10° so this sole meets the other, without passing it."""
    other = "R" if high == "L" else "L"
    base = eulers[name][comp]
    best_d = 0
    best_gap = abs(_sole_zs(ids, local, high)[0] - _sole_zs(ids, local, other)[0])
    for d in range(-10, 11):
        eulers[name][comp] = base + d
        _write_bone(arm, name, eulers[name])
        gap = abs(_sole_zs(ids, local, high)[0] - _sole_zs(ids, local, other)[0])
        if gap < best_gap - 0.0005:
            best_gap = gap
            best_d = d
    eulers[name][comp] = base + best_d
    _write_bone(arm, name, eulers[name])
    return best_d


def _drop_high_foot(arm, eulers, ids, local):
    """Knee first, then hip flexion, each within 10°, so both soles can land."""
    zs = {s: _sole_zs(ids, local, s)[0] for s in ("L", "R")}
    high = "L" if zs["L"] > zs["R"] else "R"
    if abs(zs["L"] - zs["R"]) <= 0.010:
        return 0, 0, high
    knee_d = _close_gap(arm, eulers, ids, local, high, f"LowerLeg_{high}", 0)
    hip_d = 0
    zs = {s: _sole_zs(ids, local, s)[0] for s in ("L", "R")}
    if abs(zs["L"] - zs["R"]) > 0.010:
        hip_d = _close_gap(arm, eulers, ids, local, high, f"UpperLeg_{high}", 0)
    _refine_sole_gap(arm, eulers, ids, local, high)
    return knee_d, hip_d, high


def _refine_sole_gap(arm, eulers, ids, local, high):
    """A few degrees of foot roll so the higher sole meets the lower one."""
    other = "R" if high == "L" else "L"
    name = f"Foot_{high}"
    base = eulers[name][0]
    best_d = 0
    best_gap = abs(_sole_zs(ids, local, high)[0] - _sole_zs(ids, local, other)[0])
    if best_gap <= 0.010:
        return 0
    for d in range(-15, 16):
        eulers[name][0] = base + d
        _write_bone(arm, name, eulers[name])
        lo, _hi = _sole_zs(ids, local, high)
        if lo > _foot_min_z(local, high) + 0.008:
            continue
        gap = abs(lo - _sole_zs(ids, local, other)[0])
        if gap < best_gap - 0.0003:
            best_gap = gap
            best_d = d
    eulers[name][0] = base + best_d
    _write_bone(arm, name, eulers[name])
    return best_d


def _plant_soles(arm, ids, local):
    zs = {s: _sole_zs(ids, local, s)[0] for s in ("L", "R")}
    arm.location.z += 0.002 - min(zs.values())
    bpy.context.view_layer.update()
    return {s: _sole_zs(ids, local, s)[0] for s in ("L", "R")}


def _lowest_mesh():
    """Lowest Mesh_* vertex. Read .z immediately; mathutils reuses one Vector."""
    lowest = None
    where = ""
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        mw = obj.matrix_world
        name = obj.name
        for v in obj.data.vertices:
            z = (mw @ v.co).z
            if lowest is None or z < lowest:
                lowest = z
                where = name
    if lowest is None:
        return 0.0, ""
    return lowest, where


def _ground_clamp(arm):
    """Lift the preview capsule so the lowest shell is 0.1 cm under z = 0."""
    lowest, where = _lowest_mesh()
    lift = 0.0
    if lowest < -0.004:
        lift = -0.001 - lowest
        arm.location.z += lift
        bpy.context.view_layer.update()
    return lift, lowest, where


def _wall_slab(top_z):
    """World z and the half-thickness of the action wall. Matches _show_wall."""
    if top_z is None:
        height = 2.55
        center_z = 0.04 + height * 0.5
    else:
        height = max(1.10, top_z - 0.02)
        center_z = top_z - height * 0.5
    return center_z - height * 0.5, center_z + height * 0.5


def _push_wall(arm, side, skip, top_z=None):
    """Keep shells out of the wall slab. Vertices above the lip are not in it."""
    z0, z1 = _wall_slab(top_z)
    y0 = arm.location.y
    into = 0.0
    where = ""
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        if any(tok in obj.name for tok in skip):
            continue
        mw = obj.matrix_world
        name = obj.name
        for v in obj.data.vertices:
            p = mw @ v.co
            x, y, z = p.x, p.y, p.z
            if z < z0 or z > z1 or abs(y - y0) > 1.20:
                continue
            # Anything on the solid's side of the face, including a limb that has
            # already passed through. Pulling only the inside of the slab drags
            # those vertices back into the wall.
            if side == "-":
                if x <= 0.004:
                    continue
                pen = x - 0.004
            else:
                if x >= -0.004:
                    continue
                pen = -0.004 - x
            if pen > into:
                into = pen
                where = name
    if into > 0.0:
        arm.location.x += -into if side == "-" else into
        bpy.context.view_layer.update()
    return into, where


def _roll_wall(arm, eulers, ids, local, side_foot, wall_side):
    name = f"Foot_{side_foot}"
    base = eulers[name][0]
    obj = bpy.data.objects[f"Mesh_Foot_{side_foot}"]

    def stats():
        mw = obj.matrix_world
        sole = [(mw @ local[side_foot][i]).x for i in ids[side_foot]]
        foot = [(mw @ co).x for co in local[side_foot]]
        return min(sole), max(sole), min(foot), max(foot)

    best = None
    for deg in range(-80, 81, 10):
        eulers[name][0] = base + deg
        _write_bone(arm, name, eulers[name])
        slo, shi, flo, fhi = stats()
        if wall_side == "-":
            if shi < fhi - 0.008:
                continue
            span = shi - slo
        else:
            if slo > flo + 0.008:
                continue
            span = shi - slo
        if best is None or span < best[0]:
            best = (span, deg)
    eulers[name][0] = base + (best[1] if best else 0)
    _write_bone(arm, name, eulers[name])
    return 0 if best is None else best[1]


def _leading_foot(ids, local, wall_side):
    best = None
    side = "L"
    for s in ("L", "R"):
        obj = bpy.data.objects[f"Mesh_Foot_{s}"]
        mw = obj.matrix_world
        xs = [(mw @ local[s][i]).x for i in ids[s]]
        lead = max(xs) if wall_side == "-" else min(xs)
        if best is None or (wall_side == "-" and lead > best) or (wall_side == "+" and lead < best):
            best = lead
            side = s
    return side, best


def _seat_foot(arm, ids, local, wall_side, foot_side):
    obj = bpy.data.objects[f"Mesh_Foot_{foot_side}"]
    mw = obj.matrix_world
    xs = [(mw @ local[foot_side][i]).x for i in ids[foot_side]]
    lead = max(xs) if wall_side == "-" else min(xs)
    # Put the sole 0.3 cm outside the face, then let the chest push back.
    if wall_side == "-":
        arm.location.x += -0.003 - lead
    else:
        arm.location.x += 0.003 - lead
    bpy.context.view_layer.update()
    _push_wall(arm, wall_side, (f"Foot_{foot_side}",))
    mw = obj.matrix_world
    xs = [(mw @ local[foot_side][i]).x for i in ids[foot_side]]
    lead = max(xs) if wall_side == "-" else min(xs)
    return abs(lead)


def _pelvis_err(arm, target):
    got = (arm.matrix_world @ arm.pose.bones["Hips"].matrix).to_3x3()
    return _ang(got, target)


def _round_eulers(eulers):
    return {k: [round(float(a), 2) for a in v] for k, v in eulers.items()}


def _landing_frames(heights):
    """Contiguous crouch around the lowest hip, not a later step that happens to be low."""
    finite = [(i, h) for i, h in enumerate(heights) if h is not None]
    if not finite:
        return set()
    i_min = min(finite, key=lambda r: r[1])[0]
    thresh = heights[i_min] + 0.06
    lo = i_min
    while lo > 0 and heights[lo - 1] is not None and heights[lo - 1] <= thresh:
        lo -= 1
    hi = i_min
    while hi + 1 < len(heights) and heights[hi + 1] is not None and heights[hi + 1] <= thresh:
        hi += 1
    return set(range(lo, hi + 1))


def _soft_soles(cid, i, heights):
    if not cid.startswith("03") and not cid.startswith("04"):
        return False
    return i in _landing_frames(heights)


def _is_sole(cid, i, heights):
    if (cid, i) in SOLE_EXTRA:
        return True
    if cid.startswith("12_") and i in TIC_SOLE:
        return True
    return _soft_soles(cid, i, heights)


def _is_hands(cid, i):
    return (cid, i) in HANDS


def _is_foot(cid, i):
    return (cid, i) in FOOT_WALL or cid.startswith("10_")


def write_pose():
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = _sole_ids(arm)
    _ensure_pose(arm)
    doc = json.load(open(JSON_PATH))
    lines = ["clip frame pelvis_deg hip_deg knee_deg spine_deg height_cm"]
    contacts = []
    for spec in sp.CLIPS:
        cid = spec["id"]
        data = sp.load_clip(cid)
        smooth = sp.clip_curves(cid, keys=24)["raw_smooth"]
        scale = p5._leg_scale(data)
        n = data["frame_count"]
        heights = data["root_height_above_feet_m"]
        # The reference lean goes to image-right, which is Blender +X, so the
        # body stands on the -X side of the face.
        wall_side = "-" if spec["verb"] != "softland" else "+"
        keys_out = []
        caps = []
        wall_top = None
        over = {"pelvis": 0, "hip": 0, "knee": 0, "spine": 0}
        worst = {"pelvis": 0.0, "hip": 0.0, "knee": 0.0, "spine": 0.0}
        for i in range(n):
            eul, target = pelvis_of(data["world_xyz_m"][i])
            track = _track(smooth, i, n)
            eulers = {k: [v[0], v[1], v[2]] for k, v in track.items()}
            eulers["Hips"] = [math.degrees(eul.x), math.degrees(eul.y), math.degrees(eul.z)]
            _write(arm, eulers)
            target_h = float(heights[i]) * scale
            _set_height(arm, target_h)
            arm.location.x = 0.0
            arm.location.y = 0.0
            bpy.context.view_layer.update()
            note = []
            if _is_hands(cid, i):
                used, edge = _nudge_arms(arm, eulers, wall_side)
                if edge is not None:
                    arm.location.x += edge[1]
                    bpy.context.view_layer.update()
                    wall_top = edge[2] - edge[1] * 0.0
                    # edge[2] is the world z of the edge before the x shift.
                    # z is unchanged by the x shift, but the body z is already set.
                    wall_top = edge[2]
                _push_wall(arm, wall_side, ("Hand_",), wall_top)
                bpy.context.view_layer.update()
                # Re-measure after the chest push. The edge z stays.
                if wall_top is not None:
                    gaps = []
                    for side in ("L", "R"):
                        pts = _palm_xz(side)
                        if pts and wall_top is not None:
                            gaps.append(min(math.hypot(x, z - wall_top) for x, z in pts))
                    note.append(f"hands {used} gap_cm={[round(g * 100, 2) for g in gaps]}")
            elif _is_sole(cid, i, heights):
                rolls = []
                for side in ("L", "R"):
                    rolls.append(_roll_ground(arm, eulers, ids, local, side))
                knee_d, hip_d, high = _drop_high_foot(arm, eulers, ids, local)
                zs = _plant_soles(arm, ids, local)
                if spec["verb"] != "softland":
                    _push_wall(arm, wall_side, (), wall_top)
                note.append(
                    f"soles roll={rolls} knee{high}={knee_d} hip{high}={hip_d} "
                    f"z_cm=({zs['L'] * 100:.2f},{zs['R'] * 100:.2f})"
                )
            elif _is_foot(cid, i) and spec["verb"] != "softland":
                foot, _lead = _leading_foot(ids, local, wall_side)
                rolled = _roll_wall(arm, eulers, ids, local, foot, wall_side)
                gap = _seat_foot(arm, ids, local, wall_side, foot)
                _ground_clamp(arm)
                note.append(f"foot {foot} roll={rolled} gap_cm={gap * 100:.2f}")
            else:
                _ground_clamp(arm)
                if spec["verb"] != "softland":
                    _push_wall(arm, wall_side, (), wall_top)
            perr = _pelvis_err(arm, target)
            hip_e = _group_err(eulers, track, SCORED_HIP)
            knee_e = _group_err(eulers, track, SCORED_KNEE)
            spine_e = _group_err(eulers, track, ("Spine",))
            h_err = (_hip_z(arm) - target_h) * 100.0
            for name, val in (("pelvis", perr), ("hip", hip_e), ("knee", knee_e), ("spine", spine_e)):
                worst[name] = max(worst[name], val)
                if val > 10.0:
                    over[name] += 1
            lines.append(
                f"{cid} {i} {perr:.3f} {hip_e:.3f} {knee_e:.3f} {spine_e:.3f} {h_err:.2f}"
            )
            if note and (i in STORY.get(cid, []) or _is_hands(cid, i) or _is_sole(cid, i, heights) or (cid, i) in FOOT_WALL):
                msg = f"CONTACT {cid} f={i} " + " ".join(note) + f" cap=({arm.location.x:.3f},{arm.location.z:.3f})"
                print(msg, flush=True)
                contacts.append(msg)
            keys_out.append({"frame": i, "t": round(i / float(data["fps"]), 4), "bones": _round_eulers(eulers)})
            caps.append([round(arm.location.x, 4), round(arm.location.y, 4), round(arm.location.z, 4)])
            if i % 40 == 0 or i == n - 1:
                print(f"  {cid} {i + 1}/{n} pelv={perr:.2f} hip={hip_e:.2f}", flush=True)
        entry = doc["clips"][cid]
        entry["keys"] = keys_out
        entry["capsule_preview_m"] = caps
        entry["root_location_keys"] = False
        entry["frames"] = n
        entry["fps"] = float(data["fps"])
        entry["pelvis_in_hips"] = True
        entry["wall_side"] = wall_side
        entry["wall_top_m"] = None if wall_top is None else round(wall_top, 4)
        summary = (
            f"TRACK {cid} frames={n} wall={wall_side} top={entry['wall_top_m']} "
            f"over10 pelvis={over['pelvis']} hip={over['hip']} knee={over['knee']} spine={over['spine']} "
            f"worst pelvis={worst['pelvis']:.2f} hip={worst['hip']:.2f} "
            f"knee={worst['knee']:.2f} spine={worst['spine']:.2f}"
        )
        print(summary, flush=True)
        contacts.append(summary)
    doc["root_motion"] = False
    doc["pelvis_in_hips"] = True
    os.makedirs(PASS10, exist_ok=True)
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(lines + [""] + contacts) + "\n")
    print("wrote", ERROR_PATH, flush=True)
    rh._reset(arm)
    rh._hide_wall()


def _pose_depth(arm, pieces, locals_c, polys_c, bind_pairs):
    """Worst non-neighbour absolute depth, metres. World solids are not in this."""
    packed = []
    for obj in pieces:
        verts = proof._world(locals_c[obj.name], obj.matrix_world)
        packed.append((obj, verts, proof._bvh(verts, polys_c[obj.name])))
    worst = 0.0
    where = ""
    for i, (oa, va, ba) in enumerate(packed):
        alo, ahi = proof._aabb(va)
        for ob, vb, bb in packed[i + 1:]:
            joint = proof._shared_joint(arm, oa.parent_bone, ob.parent_bone)
            base = bind_pairs.get((oa.name, ob.name), 0.0)
            if proof._pair_kind(joint, base) != "pose":
                continue
            blo, bhi = proof._aabb(vb)
            if (
                ahi.x < blo.x - 0.01 or bhi.x < alo.x - 0.01
                or ahi.y < blo.y - 0.01 or bhi.y < alo.y - 0.01
                or ahi.z < blo.z - 0.01 or bhi.z < alo.z - 0.01
            ):
                continue
            da = proof._inside_depths(va, polys_c[oa.name], ba, bb, joint)
            db = proof._inside_depths(vb, polys_c[ob.name], bb, ba, joint)
            depth = 0.0
            label = f"{oa.name} in {ob.name}"
            if da:
                d = max(da.values())
                if d > depth:
                    depth = d
                    label = f"{oa.name} in {ob.name}"
            if db:
                d = max(db.values())
                if d > depth:
                    depth = d
                    label = f"{ob.name} in {oa.name}"
            if depth > worst:
                worst = depth
                where = label
    return worst, where


def _search_window(arm, bones, track, cap, pieces, locals_c, polys_c, bind_pairs, limit, depth):
    """Coordinate descent inside ±limit degrees of the track. Returns the new depth."""
    def measure():
        _write(arm, bones)
        arm.location = Vector(cap)
        bpy.context.view_layer.update()
        return _pose_depth(arm, pieces, locals_c, polys_c, bind_pairs)

    for _pass in range(2):
        moved = False
        for name, comp in WIDEN:
            base = track[name][comp]
            best = bones[name][comp]
            best_d = depth
            step = 5
            steps = [0]
            while step <= limit:
                steps.append(step)
                steps.append(-step)
                step += 5
            for off in steps:
                bones[name][comp] = base + off
                d, _w = measure()
                if d < best_d - 0.0002:
                    best_d = d
                    best = base + off
                    moved = True
            bones[name][comp] = best
            depth = best_d
            if depth <= proof.LIMIT_M:
                return depth
        if not moved:
            break
    return depth


def widen_pose():
    """±10°, then ±20° on abduction, hip rotation and spine flex only.

    Contact frames keep the bone change only when the palm or sole
    tolerance still holds after the preview capsule is seated again.
    """
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    pieces = proof._pieces(arm)
    locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
    polys_c = {obj.name: proof._polys(obj) for obj in pieces}
    bind_pairs = proof._bind_self(arm, pieces, locals_c, polys_c)
    ids, local = _sole_ids(arm)
    _ensure_pose(arm)
    doc = json.load(open(JSON_PATH))
    widened = []
    pose_n = 0
    worst_pairs = []
    for spec in sp.CLIPS:
        cid = spec["id"]
        data = sp.load_clip(cid)
        smooth = sp.clip_curves(cid, keys=24)["raw_smooth"]
        heights = data["root_height_above_feet_m"]
        entry = doc["clips"][cid]
        n = len(entry["keys"])
        wall_side = entry.get("wall_side", "+")
        wall_top = entry.get("wall_top_m")
        print(f"WIDEN {cid} {n}", flush=True)
        for i in range(n):
            track = _track(smooth, i, n)
            original = {k: list(v) for k, v in entry["keys"][i]["bones"].items()}
            bones = {k: list(v) for k, v in original.items()}
            cap = list(entry["capsule_preview_m"][i])
            _write(arm, bones)
            arm.location = Vector(cap)
            bpy.context.view_layer.update()
            depth, where = _pose_depth(arm, pieces, locals_c, polys_c, bind_pairs)
            if depth <= proof.LIMIT_M:
                continue
            start = depth
            depth = _search_window(
                arm, bones, track, cap, pieces, locals_c, polys_c, bind_pairs, 10, depth,
            )
            if depth > proof.LIMIT_M:
                depth = _search_window(
                    arm, bones, track, cap, pieces, locals_c, polys_c, bind_pairs, 20, depth,
                )
            _write(arm, bones)
            arm.location = Vector(cap)
            bpy.context.view_layer.update()
            # Seat the preview again so a hip change does not lift a contact off.
            contact_ok = True
            if _is_hands(cid, i) and wall_top is not None:
                edge = _best_edge(wall_side)
                if edge is not None:
                    arm.location.x += edge[1]
                    bpy.context.view_layer.update()
                _push_wall(arm, wall_side, ("Hand_",), wall_top)
                gaps = []
                for side in ("L", "R"):
                    pts = _palm_xz(side)
                    if pts:
                        gaps.append(min(math.hypot(x - 0.0, z - wall_top) for x, z in pts))
                contact_ok = bool(gaps) and max(gaps) <= 0.020
                cap = [arm.location.x, arm.location.y, arm.location.z]
            elif _is_sole(cid, i, heights):
                zs = _plant_soles(arm, ids, local)
                if spec["verb"] != "softland":
                    _push_wall(arm, wall_side, (), wall_top)
                contact_ok = max(abs(z) for z in zs.values()) <= 0.010
                cap = [arm.location.x, arm.location.y, arm.location.z]
            elif _is_foot(cid, i) and spec["verb"] != "softland":
                # Measured on the unwidened pose, before this branch's reseat.
                _write(arm, original)
                arm.location = Vector(entry["capsule_preview_m"][i])
                bpy.context.view_layer.update()
                _foot0, lead0 = _leading_foot(ids, local, wall_side)
                was_on = abs(lead0) <= 0.020
                _write(arm, bones)
                arm.location = Vector(cap)
                bpy.context.view_layer.update()
                foot, _lead = _leading_foot(ids, local, wall_side)
                gap = _seat_foot(arm, ids, local, wall_side, foot)
                _ground_clamp(arm)
                contact_ok = (not was_on) or gap <= 0.020
                cap = [arm.location.x, arm.location.y, arm.location.z]
            if not contact_ok:
                bones = original
                cap = list(entry["capsule_preview_m"][i])
                _write(arm, bones)
                arm.location = Vector(cap)
                bpy.context.view_layer.update()
                depth, where = _pose_depth(arm, pieces, locals_c, polys_c, bind_pairs)
            else:
                entry["capsule_preview_m"][i] = [round(c, 4) for c in cap]
                _write(arm, bones)
                arm.location = Vector(cap)
                bpy.context.view_layer.update()
                depth, where = _pose_depth(arm, pieces, locals_c, polys_c, bind_pairs)
            dev = []
            wide = False
            for name, comp in WIDEN:
                delta = bones[name][comp] - track[name][comp]
                # Wrap the reported delta into ±180 so a 350° reading is -10.
                if delta > 180.0:
                    delta -= 360.0
                if delta < -180.0:
                    delta += 360.0
                if abs(delta) > 0.05:
                    dev.append(f"{name}[{comp}]={delta:+.1f}")
                if abs(delta) > 10.05:
                    wide = True
            if wide and dev:
                widened.append(
                    f"WIDEN {cid} f={i} {', '.join(dev)} depth_cm={depth * 100:.2f} was={start * 100:.2f}"
                )
                print(widened[-1], flush=True)
            entry["keys"][i]["bones"] = _round_eulers(bones)
            if depth > proof.LIMIT_M:
                pose_n += 1
                worst_pairs.append((depth, cid, i, where))
            if i % 40 == 0:
                print(f"  {cid} {i}/{n} pose_so_far={pose_n} depth_cm={depth * 100:.2f}", flush=True)
    worst_pairs.sort(key=lambda r: -r[0])
    _refresh_error_scores(doc)
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "a") as f:
        if widened:
            f.write("\n".join(widened) + "\n")
        f.write(f"pose={pose_n}\n")
        for depth, cid, i, where in worst_pairs[:12]:
            f.write(f"POSE {cid} f={i} {where} {depth * 100:.2f}\n")
    print(f"pose={pose_n}", flush=True)
    for row in worst_pairs[:8]:
        print(f"POSE {row[1]} f={row[2]} {row[3]} {row[0] * 100:.2f}", flush=True)
    rh._reset(arm)


def _refresh_error_scores(doc):
    """Hip, knee and spine scores follow the widened keys. Pelvis does not move."""
    old = open(ERROR_PATH).read().splitlines()
    kept = {}
    tail = []
    for line in old:
        parts = line.split()
        if len(parts) >= 7 and parts[0].endswith("_wall") or (len(parts) >= 7 and "_" in parts[0] and parts[1].isdigit()):
            if parts[1].isdigit():
                kept[(parts[0], int(parts[1]))] = (parts[2], parts[6])
                continue
        if line.startswith("CONTACT") or line.startswith("TRACK"):
            tail.append(line)
    lines = ["clip frame pelvis_deg hip_deg knee_deg spine_deg height_cm"]
    for spec in sp.CLIPS:
        cid = spec["id"]
        smooth = sp.clip_curves(cid, keys=24)["raw_smooth"]
        entry = doc["clips"][cid]
        n = len(entry["keys"])
        for i in range(n):
            track = _track(smooth, i, n)
            bones = entry["keys"][i]["bones"]
            pel, height = kept.get((cid, i), ("0.000", "0.00"))
            hip = _group_err(bones, track, SCORED_HIP)
            knee = _group_err(bones, track, SCORED_KNEE)
            spine = _group_err(bones, track, ("Spine",))
            lines.append(f"{cid} {i} {pel} {hip:.3f} {knee:.3f} {spine:.3f} {height}")
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(lines + [""] + tail) + "\n")


def wall_clamp():
    """Push the preview capsule out of the wall slab. Bones stay put.

    The ground clamp does not move x. A vertex in the 9 cm wall was reporting
    at most 4.5 cm because the near face is the back face once it passes the
    middle. Vertices above the lip are outside the slab and are left there.
    """
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = _sole_ids(arm)
    _ensure_pose(arm)
    doc = json.load(open(JSON_PATH))
    notes = []
    for spec in sp.CLIPS:
        if spec["verb"] == "softland":
            continue
        cid = spec["id"]
        entry = doc["clips"][cid]
        n = len(entry["keys"])
        wall_side = entry.get("wall_side", "+")
        wall_top = entry.get("wall_top_m")
        print(f"WALL {cid} {n} side={wall_side} top={wall_top}", flush=True)
        for i in range(n):
            bones = entry["keys"][i]["bones"]
            cap = [float(c) for c in entry["capsule_preview_m"][i]]
            _write(arm, bones)
            arm.location = Vector(cap)
            bpy.context.view_layer.update()
            hands_before = None
            if _is_hands(cid, i) and wall_top is not None:
                hands_before = []
                for side in ("L", "R"):
                    pts = _palm_xz(side)
                    if pts:
                        hands_before.append(min(math.hypot(x, z - wall_top) for x, z in pts))
            foot_before = None
            if _is_foot(cid, i):
                _foot, lead = _leading_foot(ids, local, wall_side)
                foot_before = abs(lead)
            skip = ("Hand_",) if _is_hands(cid, i) else ()
            into, where = _push_wall(arm, wall_side, skip, wall_top)
            if into <= 0.0005:
                continue
            revert = False
            reason = ""
            if hands_before:
                gaps = []
                for side in ("L", "R"):
                    pts = _palm_xz(side)
                    if pts:
                        gaps.append(min(math.hypot(x, z - wall_top) for x, z in pts))
                if gaps and max(gaps) > 0.020:
                    revert = True
                    reason = f"hands_cm={[round(g * 100, 2) for g in gaps]}"
            if foot_before is not None and foot_before <= 0.020:
                _foot, lead = _leading_foot(ids, local, wall_side)
                if abs(lead) > 0.020:
                    revert = True
                    reason = f"foot_cm={abs(lead) * 100:.2f} was={foot_before * 100:.2f}"
            if revert:
                arm.location = Vector(cap)
                bpy.context.view_layer.update()
                notes.append(
                    f"WALL_REVERT {cid} f={i} {where} push_cm={into * 100:.2f} {reason}"
                )
            else:
                cap = [arm.location.x, arm.location.y, arm.location.z]
                entry["capsule_preview_m"][i] = [round(c, 4) for c in cap]
                extra = ""
                if hands_before:
                    gaps = []
                    for side in ("L", "R"):
                        pts = _palm_xz(side)
                        if pts:
                            gaps.append(min(math.hypot(x, z - wall_top) for x, z in pts))
                    extra = f" hands_cm={[round(g * 100, 2) for g in gaps]}"
                if foot_before is not None:
                    _foot, lead = _leading_foot(ids, local, wall_side)
                    extra += f" foot_cm={abs(lead) * 100:.2f}"
                notes.append(
                    f"WALL {cid} f={i} {where} push_cm={into * 100:.2f}{extra}"
                )
            if into > 0.005 or i in STORY.get(cid, []):
                print(notes[-1], flush=True)
        print(f"  {cid} done", flush=True)
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "a") as f:
        f.write("WALL clamp\n")
        if notes:
            f.write("\n".join(notes) + "\n")
    print(f"wall moves={len(notes)}", flush=True)
    rh._reset(arm)
    rh._hide_wall()


def _restore_plant(eulers, track):
    """Undo a sole plant. Widen channels (hip Y/Z, spine X) stay."""
    for name, comp in (
        ("Foot_L", 0), ("Foot_R", 0),
        ("LowerLeg_L", 0), ("LowerLeg_R", 0),
        ("UpperLeg_L", 0), ("UpperLeg_R", 0),
    ):
        if name in eulers and name in track:
            eulers[name][comp] = track[name][comp]


def _try_sole_seat(arm, eulers, ids, local, wall_side, do_wall):
    """Roll and close the feet, then keep the seat only if the body stays clear."""
    snap_e = {k: list(v) for k, v in eulers.items()}
    snap_loc = Vector((arm.location.x, arm.location.y, arm.location.z))
    rolls = [_roll_ground(arm, eulers, ids, local, side) for side in ("L", "R")]
    knee_d, hip_d, high = _drop_high_foot(arm, eulers, ids, local)
    _plant_soles(arm, ids, local)
    if do_wall:
        _push_wall(arm, wall_side, ())
    _ground_clamp(arm)
    zs = {s: _sole_zs(ids, local, s)[0] for s in ("L", "R")}
    low, where = _lowest_mesh()
    ok = max(abs(z) for z in zs.values()) <= 0.010 and low >= -0.004
    if not ok:
        for k, v in snap_e.items():
            eulers[k][:] = v
        _write(arm, eulers)
        arm.location = snap_loc
        bpy.context.view_layer.update()
    return ok, rolls, knee_d, hip_d, high, zs, low, where


def clamp_ground():
    """No mesh more than 0.4 cm under the floor. An inverted sole plant is dropped.

    Tic-tac frame 145 had its shoe bottoms on z=0 while the head and hands
    were inside the ground. The feet-down crouch after the plant (frames
    109-116) is seated instead, and only kept when both soles stay within
    1 cm and every other shell stays clear.
    """
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = _sole_ids(arm)
    _ensure_pose(arm)
    doc = json.load(open(JSON_PATH))
    updates = {}
    notes = []
    for spec in sp.CLIPS:
        cid = spec["id"]
        data = sp.load_clip(cid)
        smooth = sp.clip_curves(cid, keys=24)["raw_smooth"]
        scale = p5._leg_scale(data)
        heights = data["root_height_above_feet_m"]
        entry = doc["clips"][cid]
        n = len(entry["keys"])
        wall_side = entry.get("wall_side", "+")
        wall_top = entry.get("wall_top_m")
        do_wall = spec["verb"] != "softland"
        print(f"CLAMP {cid} {n}", flush=True)
        for i in range(n):
            track = _track(smooth, i, n)
            bones = {k: [float(a) for a in v] for k, v in entry["keys"][i]["bones"].items()}
            cap = [float(c) for c in entry["capsule_preview_m"][i]]
            if cid.startswith("12_") and i == 145:
                _restore_plant(bones, track)
            _write(arm, bones)
            arm.location = Vector(cap)
            bpy.context.view_layer.update()
            if cid.startswith("12_") and i in TIC_SOLE:
                ok, rolls, knee_d, hip_d, high, zs, low, where = _try_sole_seat(
                    arm, bones, ids, local, wall_side, do_wall,
                )
                flag = "SEAT" if ok else "SEAT_MISS"
                notes.append(
                    f"{flag} {cid} f={i} roll={rolls} knee{high}={knee_d} hip{high}={hip_d} "
                    f"z_cm=({zs['L'] * 100:.2f},{zs['R'] * 100:.2f}) "
                    f"low_cm={low * 100:.2f} {where}"
                )
                print(notes[-1], flush=True)
            lift, was, where = _ground_clamp(arm)
            cap = [arm.location.x, arm.location.y, arm.location.z]
            if lift > 0.001:
                msg = f"LIFT {cid} f={i} {where} lift_cm={lift * 100:.2f} was_cm={was * 100:.2f}"
                if i in STORY.get(cid, []) or lift > 1.0:
                    notes.append(msg)
                if i % 20 == 0 or lift > 1.0 or i in STORY.get(cid, []):
                    print(msg, flush=True)
            if _is_hands(cid, i) and wall_top is not None:
                gaps = []
                for side in ("L", "R"):
                    pts = _palm_xz(side)
                    if pts:
                        gaps.append(min(math.hypot(x, z - wall_top) for x, z in pts))
                notes.append(
                    f"HANDS {cid} f={i} gap_cm={[round(g * 100, 2) for g in gaps]} "
                    f"cap=({cap[0]:.3f},{cap[2]:.3f})"
                )
                print(notes[-1], flush=True)
            if _is_sole(cid, i, heights) or (cid.startswith("12_") and i == 145):
                zs = {s: _sole_zs(ids, local, s)[0] for s in ("L", "R")}
                _eul, target = pelvis_of(data["world_xyz_m"][i])
                up_z = target.col[1].z
                if cid.startswith("12_") and i == 145:
                    kind = "INVERT"
                elif max(abs(z) for z in zs.values()) <= 0.010:
                    kind = "SOLE"
                else:
                    kind = "SOLE_OFF"
                notes.append(
                    f"{kind} {cid} f={i} upz={up_z:+.2f} "
                    f"z_cm=({zs['L'] * 100:.2f},{zs['R'] * 100:.2f}) "
                    f"cap=({cap[0]:.3f},{cap[2]:.3f})"
                )
                print(notes[-1], flush=True)
            if (cid, i) in FOOT_WALL or (cid.startswith("10_") and i in STORY.get(cid, [])):
                foot, lead = _leading_foot(ids, local, wall_side)
                notes.append(
                    f"FOOT {cid} f={i} {foot} gap_cm={abs(lead) * 100:.2f} cap=({cap[0]:.3f},{cap[2]:.3f})"
                )
            low, low_where = _lowest_mesh()
            if low < -0.004:
                print(f"STILL_UNDER {cid} f={i} {low_where} {low * 100:.2f}", flush=True)
            entry["keys"][i]["bones"] = _round_eulers(bones)
            entry["capsule_preview_m"][i] = [round(c, 4) for c in cap]
            hip = _group_err(bones, track, SCORED_HIP)
            knee = _group_err(bones, track, SCORED_KNEE)
            spine = _group_err(bones, track, ("Spine",))
            h_err = (_hip_z(arm) - float(heights[i]) * scale) * 100.0
            updates[(cid, i)] = (hip, knee, spine, h_err)
            if i % 40 == 0:
                print(f"  {cid} {i}/{n} lift_cm={lift * 100:.2f}", flush=True)
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    _rewrite_scores(updates, notes)
    print("clamp wrote", ERROR_PATH, flush=True)
    rh._reset(arm)
    rh._hide_wall()


def _rewrite_scores(updates, notes):
    old = open(ERROR_PATH).read().splitlines()
    if "GROUND clamp" in old:
        old = old[: old.index("GROUND clamp")]
    while old and old[-1] == "":
        old.pop()
    body = []
    tail = []
    seen = False
    for line in old:
        parts = line.split()
        if (
            not seen
            and len(parts) >= 7
            and parts[1].isdigit()
            and (parts[0], int(parts[1])) in updates
        ):
            hip, knee, spine, height = updates[(parts[0], int(parts[1]))]
            body.append(
                f"{parts[0]} {parts[1]} {parts[2]} {hip:.3f} {knee:.3f} {spine:.3f} {height:.2f}"
            )
            continue
        if line.startswith("clip "):
            body.append(line)
            continue
        seen = True
        tail.append(line)
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(body + tail))
        f.write("\nGROUND clamp\n")
        if notes:
            f.write("\n".join(notes) + "\n")
        f.write(
            "widen_frames=455 counts frames whose worst non-neighbour self depth "
            "stayed over 0.5 cm. pose= on the proof line is the pair count.\n"
        )


def _stick_cell(clip_id, frame, n):
    from PIL import Image
    path = os.path.join(sp.JSON_DIR, "..", "strips", clip_id + ".png")
    strip = Image.open(path).convert("RGB")
    keys = [int(i * (n - 1) / 11.0) for i in range(12)] if n > 1 else [0]
    cell = min(range(len(keys)), key=lambda i: abs(keys[i] - frame))
    return strip.crop((cell * 160, 26, cell * 160 + 160, 26 + 300))


def render_strips():
    from PIL import Image, ImageDraw
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 640
    bpy.context.scene.render.resolution_y = 800
    doc = json.load(open(JSON_PATH))
    os.makedirs(PASS10, exist_ok=True)
    prev = "/tmp/pass10_cells"
    os.makedirs(prev, exist_ok=True)
    labels = {
        "03_wall_drop_softland": "03 soft land",
        "04_window_drop_softland": "04 soft land",
        "10_wallrun_slanted": "10 wall run",
        "12_tictac_slanted_wall": "12 tic-tac",
        "15_cat_leap_wall": "15 cat leap",
    }
    only = os.environ.get("PASS10_ONLY", "")
    for spec in sp.CLIPS:
        cid = spec["id"]
        if only and not cid.startswith(only):
            continue
        entry = doc["clips"][cid]
        data = sp.load_clip(cid)
        t0 = data["times_s"][0]
        has_wall = spec["verb"] != "softland"
        wall_top = entry.get("wall_top_m")
        side = entry.get("wall_side", "+")
        frames = [i for i in STORY[cid] if i < len(entry["keys"])]
        shots = []
        for s, i in enumerate(frames):
            key = entry["keys"][i]
            cap = entry["capsule_preview_m"][i]
            rh._apply_eulers(arm, key["bones"])
            arm.location = Vector(cap)
            arm.rotation_euler = (0.0, 0.0, 0.0)
            root = arm.pose.bones.get("Root")
            if root is not None:
                root.location = (0.0, 0.0, 0.0)
                root.rotation_euler = (0.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            if has_wall:
                rh._show_wall(0.0, wall_top, arm.location.y, 2.4, side=side)
            else:
                rh._hide_wall()
            loc, look, ortho = p5._shot_at(arm, spec, True)
            path = os.path.join(prev, f"{cid}_{s}.png")
            rh._shot(path, loc, look, ortho=ortho)
            shots.append(path)
            print(f"ref {cid} f={i}", flush=True)
        cell_w, cell_h = 400, 300
        canvas = Image.new("RGB", (cell_w * 4, cell_h * 2 + 28), (48, 44, 40))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle((0, 0, 1600, 28), fill=(20, 18, 16))
        draw.text((12, 8), labels[cid] + "   pelvis on Hips, beside the reference stick", fill=(245, 236, 220))
        n = len(entry["keys"])
        for s, i in enumerate(frames):
            stick = _stick_cell(cid, i, n).resize((150, 280), Image.Resampling.LANCZOS)
            man = Image.open(shots[s]).convert("RGB")
            man.thumbnail((240, 280), Image.Resampling.LANCZOS)
            col = s % 4
            row = s // 4
            x0 = col * cell_w
            y0 = 28 + row * cell_h
            canvas.paste(stick, (x0 + 4, y0 + 10))
            canvas.paste(man, (x0 + 156 + (240 - man.width) // 2, y0 + 10 + (280 - man.height) // 2))
            t = data["times_s"][i] - t0
            draw.text((x0 + 8, y0 + 12), f"{t:.2f}s", fill=(245, 236, 220))
        out = os.path.join(PASS10, f"ref_{rh.SLUGS[cid]}.png")
        canvas.save(out, optimize=True)
        if os.path.getsize(out) > 400_000:
            q = canvas.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
            q.save(out, optimize=True)
        print("wrote", out, os.path.getsize(out), flush=True)
    rh._reset(arm)
    rh._hide_wall()


if __name__ == "__main__":
    stage = os.environ.get("PASS10_STAGE", "pose")
    if stage == "pose":
        write_pose()
    elif stage == "widen":
        widen_pose()
    elif stage == "stills":
        render_strips()
    elif stage == "clamp":
        clamp_ground()
    elif stage == "wall":
        wall_clamp()
    else:
        write_pose()
        widen_pose()
