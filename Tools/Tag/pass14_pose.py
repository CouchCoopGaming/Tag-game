#!/usr/bin/env python3
"""Pass 14: tic-tac reads as a tic-tac.

The spine stays on the reference curl. A frame may bend that curl by at most
10° when that is what keeps the abdomen plates off the thigh. Clearance comes
from that curl and from the thigh twist, not from straightening the torso and
not from dropping the preview capsule. Contact soles are seated with the knee,
the hip and the ankle. If a sole cannot reach, the frame is reported.

The game still has no root motion. Root and the armature stay at the origin.
The rig is not edited. solve() always rebuilds tic-tac from the pass-12 keys.
"""
import json
import math
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Vector

import noclip_proof as proof
import pass10_pose as p10
import pass11_pose as p11
import pass12_pose as p12
import pass13_pose as p13
import render_hier_v080_stills as rh
import storror_pose as sp

PASS14 = os.path.join(rh.OUT, "pass14")
ERROR_PATH = os.path.join(PASS14, "pose_error_pass14.txt")
JSON_PATH = p10.JSON_PATH
TIC = "12_tictac_slanted_wall"
PASS12_REV = "3fb582c0"
KNEE_MIN = 20.0
CLEAR = 0.0045
# Pass-12 ranges whose stick jumps. Re-lerped from the pass-12 anchors so the
# spine curl survives. The crash and the heap are bridged separately.
JUMPS = (
    (16, 16), (26, 27), (29, 33), (38, 38), (40, 41),
    (47, 51), (60, 61), (63, 63), (65, 67), (99, 100),
    (107, 107), (114, 116), (158, 159), (162, 163), (167, 167),
)
STORY = (0, 15, 34, 76, 106, 110, 145, 152)
PLATES = ("Mesh_Spine", "Mesh_Chest")


def _shortest(a, b):
    return (b - a + 180.0) % 360.0 - 180.0


def _lerp(a, b, u):
    return a + _shortest(a, b) * u


def _copy_bones(bones):
    return {k: [float(a) for a in v] for k, v in bones.items()}


def _pass12_clip():
    raw = subprocess.check_output(
        ["git", "show", f"{PASS12_REV}:Docs/HierStills/v080/pass5/keyed_clips.json"],
        cwd=os.path.abspath(os.path.join(HERE, "..", "..")),
    )
    return json.loads(raw)["clips"][TIC]


def _blend_range(keys, caps, start, end, left_b, right_b, left_c, right_c):
    names = sorted(set(left_b) | set(right_b))
    span = max(1, end + 1 - start + (1 if start > 0 else 0))
    # u = 0 on the left anchor, 1 on the right anchor.
    left_i = start - 1
    right_i = end + 1
    span = max(1, right_i - left_i)
    for i in range(start, end + 1):
        u = (i - left_i) / span
        bones = {}
        for name in names:
            a = left_b.get(name, [0.0, 0.0, 0.0])
            b = right_b.get(name, [0.0, 0.0, 0.0])
            bones[name] = [_lerp(a[k], b[k], u) for k in range(3)]
        keys[i]["bones"] = bones
        caps[i] = [left_c[k] + (right_c[k] - left_c[k]) * u for k in range(3)]


def _torso_pitch(arm):
    hip = arm.matrix_world @ arm.pose.bones["Hips"].head
    neck = arm.matrix_world @ arm.pose.bones["Neck"].head
    v = neck - hip
    if v.length < 1e-6:
        return 0.0
    v.normalize()
    return math.degrees(math.acos(max(-1.0, min(1.0, v.z))))


def _landmark_pitch(frame):
    sh = (Vector(frame[11]) + Vector(frame[12])) * 0.5
    hp = (Vector(frame[23]) + Vector(frame[24])) * 0.5
    v = p10._mp(sh - hp)
    if v.length < 1e-6:
        return 0.0
    v.normalize()
    return math.degrees(math.acos(max(-1.0, min(1.0, v.z))))


def _plate(locals_c, polys, side):
    worst = 0.0
    for limb in (f"Mesh_UpperLeg_{side}", f"Mesh_LowerLeg_{side}"):
        for torso in PLATES:
            worst = max(worst, p13._pair_depth(limb, torso, locals_c, polys))
    return worst


def _apply(arm, bones, cap):
    p11._apply(arm, bones, cap)


def _knee_for_sole(arm, bones, cap, side, ids, local, knee0):
    """Pick a knee bend that puts this sole near the floor. None if it cannot."""
    ll = f"LowerLeg_{side}"
    best = None
    for dk in (0.0, -18.0, 18.0, 36.0, -32.0):
        bones[ll][0] = max(KNEE_MIN, knee0 + dk)
        _apply(arm, bones, cap)
        med = p12._sole_z(ids, local, side)[1]
        if med < -0.006 or med > 0.022:
            continue
        err = abs(med - 0.005)
        if best is None or err < best[0]:
            best = (err, bones[ll][0], med)
    return best


def _fit_leg(arm, bones, cap, locals_c, polys, side, ids, local, contact):
    """Smallest thigh change that clears the plates.

    A contact sole has to stay reachable. Twist alone is rejected when it
    leaves the foot in the air. Hip extension is only used when the curl and
    the twist cannot both clear and plant.
    """
    ul, ll = f"UpperLeg_{side}", f"LowerLeg_{side}"
    saved = (bones[ul][:], bones[ll][0])
    if _plate(locals_c, polys, side) <= CLEAR:
        if not contact:
            return True, 0.0
        med = p12._sole_z(ids, local, side)[1]
        if -0.004 <= med <= 0.015:
            return True, _plate(locals_c, polys, side)
    x0, y0, z0 = saved[0]
    k0 = saved[1]
    best = None
    for x in (0.0, 12.0, 24.0, 36.0):
        hit = False
        for y in (0.0, 35.0, -35.0, 60.0, -60.0):
            for z in (0.0, 30.0, -30.0, 45.0, -45.0):
                bones[ul] = [x0 + x, y0 + y, z0 + z]
                if contact:
                    planted = _knee_for_sole(arm, bones, cap, side, ids, local, k0)
                    if planted is None:
                        continue
                    bones[ll][0] = planted[1]
                _apply(arm, bones, cap)
                depth = _plate(locals_c, polys, side)
                if depth <= CLEAR:
                    best = (abs(x) * 10 + abs(y) + abs(z), _copy_bones(bones))
                    hit = True
                    break
            if hit:
                break
        if hit:
            break
    if best is None:
        bones[ul] = saved[0][:]
        bones[ll][0] = k0
        _apply(arm, bones, cap)
        return False, _plate(locals_c, polys, side)
    for name, vals in best[1].items():
        bones[name] = vals
    _apply(arm, bones, cap)
    return True, _plate(locals_c, polys, side)


def _clear_frame(arm, bones, cap, locals_c, polys, ref_spine, contacts, ids, local, flex_lock=None):
    """Spine may leave the reference by at most 10° on each component."""
    saved = _copy_bones(bones)
    flexes = (0.0,) if flex_lock is None else (flex_lock,)
    best = None
    for flex in flexes:
        for roll in (0.0, -10.0, 10.0):
            bones.clear()
            bones.update(_copy_bones(saved))
            bones["Spine"] = [ref_spine[0] + flex, ref_spine[1], ref_spine[2] + roll]
            ok = True
            depth = 0.0
            for side in ("L", "R"):
                _apply(arm, bones, cap)
                if _plate(locals_c, polys, side) <= CLEAR and side not in contacts:
                    continue
                fit, depth = _fit_leg(
                    arm, bones, cap, locals_c, polys, side, ids, local, side in contacts,
                )
                if not fit:
                    ok = False
                    break
            if ok:
                _apply(arm, bones, cap)
                if max(_plate(locals_c, polys, "L"), _plate(locals_c, polys, "R")) > CLEAR:
                    ok = False
            if not ok:
                _apply(arm, bones, cap)
                depth = max(_plate(locals_c, polys, "L"), _plate(locals_c, polys, "R"))
                if best is None or depth < best[0]:
                    best = (depth, _copy_bones(bones))
                continue
            for name, vals in _copy_bones(bones).items():
                saved[name] = vals
            bones.clear()
            bones.update(saved)
            _apply(arm, bones, cap)
            return (flex, 0.0, roll), max(_plate(locals_c, polys, "L"), _plate(locals_c, polys, "R"))
    if best is not None:
        bones.clear()
        bones.update(best[1])
        _apply(arm, bones, cap)
        return (0.0, 0.0, 0.0), best[0]
    bones.clear()
    bones.update(saved)
    _apply(arm, bones, cap)
    return (0.0, 0.0, 0.0), max(_plate(locals_c, polys, "L"), _plate(locals_c, polys, "R"))


def _seat(arm, bones, cap, side, ids, local):
    """Bring a contact sole to the floor with the knee, hip and ankle only."""
    ll = f"LowerLeg_{side}"
    ul = f"UpperLeg_{side}"
    knee0 = bones[ll][0]
    hip0 = bones[ul][0]
    p12._level_foot(arm, bones, cap, side, ids, local, span=20, step=4)
    reached = False
    for _step in range(18):
        _apply(arm, bones, cap)
        med = p12._sole_z(ids, local, side)[1]
        low = p11._min_z(f"Mesh_Foot_{side}")
        if low >= -0.003 and med <= 0.012:
            reached = True
            break
        if low < -0.003 or med < 0.002:
            if bones[ll][0] >= 150.0:
                break
            bones[ll][0] += 3.0
        elif bones[ll][0] - 3.0 >= max(KNEE_MIN, knee0 - 30.0):
            bones[ll][0] -= 3.0
        elif abs(bones[ul][0] - hip0) < 12.0:
            # Knee is spent. A small hip change is still the leg, not the capsule.
            trial = bones[ul][0]
            best = (med, trial)
            for delta in (-8.0, 8.0, -12.0, 12.0):
                bones[ul][0] = hip0 + delta
                p12._level_foot(arm, bones, cap, side, ids, local, span=12, step=4)
                med_t = p12._sole_z(ids, local, side)[1]
                if med_t < best[0] - 0.004:
                    best = (med_t, hip0 + delta)
            bones[ul][0] = best[1]
            if abs(best[1] - trial) < 0.5:
                break
        else:
            break
    p12._level_foot(arm, bones, cap, side, ids, local, span=12, step=4)
    _apply(arm, bones, cap)
    med = p12._sole_z(ids, local, side)[1]
    low = p11._min_z(f"Mesh_Foot_{side}")
    return med, low, reached or (low >= -0.003 and med <= 0.015)


def _lift_buried(arm, bones, cap, side):
    foot = f"Mesh_Foot_{side}"
    ll = f"LowerLeg_{side}"
    for _step in range(14):
        if p11._min_z(foot) >= -0.003:
            return
        if bones[ll][0] >= 150.0:
            return
        bones[ll][0] += 4.0
        _apply(arm, bones, cap)


def _sole_lead(ids, local, side):
    obj = bpy.data.objects[f"Mesh_Foot_{side}"]
    mw = obj.matrix_world
    xs = [(mw @ local[side][i]).x for i in ids[side]]
    return max(xs)


def _aim_wall(arm, bones, cap, ids, local):
    """Put the left sole on the wall face. The capsule stays put."""
    z0 = bones["UpperLeg_L"][2]
    foot0 = bones["Foot_L"][2]
    best = None
    for dz in (0.0, 10.0, -10.0, 20.0, -20.0, 30.0):
        for yaw in (0.0, 20.0, -20.0, 40.0, -40.0):
            bones["UpperLeg_L"][2] = z0 + dz
            bones["Foot_L"][2] = foot0 + yaw
            _apply(arm, bones, cap)
            lead = _sole_lead(ids, local, "L")
            med_z = p12._sole_z(ids, local, "L")[1]
            if med_z < 0.25:
                continue
            err = abs(lead + 0.003)
            if best is None or err < best[0]:
                best = (err, _copy_bones(bones), lead)
            if err <= 0.005:
                for name, vals in best[1].items():
                    bones[name] = vals
                _apply(arm, bones, cap)
                return lead
    if best is None:
        bones["UpperLeg_L"][2] = z0
        bones["Foot_L"][2] = foot0
        _apply(arm, bones, cap)
        return _sole_lead(ids, local, "L")
    for name, vals in best[1].items():
        bones[name] = vals
    _apply(arm, bones, cap)
    return best[2]


def _angle_away(arm, bones, cap, ids, local, ref_pitch):
    """Yaw the pelvis so the chest leaves the wall, then put the sole back."""
    y0 = bones["Hips"][1]
    saved = _copy_bones(bones)
    best = None
    for delta in (0.0, -6.0, 6.0, -12.0, 12.0, -18.0, 18.0):
        for name, vals in saved.items():
            bones[name] = [float(v) for v in vals]
        bones["Hips"][1] = y0 + delta
        _apply(arm, bones, cap)
        pitch = _torso_pitch(arm)
        if abs(pitch - ref_pitch) > 10.0:
            continue
        hip = arm.matrix_world @ arm.pose.bones["Hips"].head
        chest = arm.matrix_world @ arm.pose.bones["Chest"].head
        away = hip.x - chest.x
        lead = _aim_wall(arm, bones, cap, ids, local)
        gap = abs(lead + 0.003)
        score = (0 if gap <= 0.005 else gap, 0 if away >= 0.06 else 0.06 - away, abs(delta))
        if best is None or score < best[0]:
            best = (score, delta, _copy_bones(bones), lead, away)
        if gap <= 0.005 and away >= 0.06:
            break
    if best is None:
        bones["Hips"][1] = y0
        _apply(arm, bones, cap)
        return _sole_lead(ids, local, "L"), 0.0
    for name, vals in best[2].items():
        bones[name] = vals
    _apply(arm, bones, cap)
    return best[3], best[4]


def _frame_scene(arm):
    """Side camera on the body, with the wall face and a floor band in frame.

    The floor slab and the wall are metres wide. Framing their full boxes
    shrinks the figure to a speck, so only a strip past the face is included.
    """
    xs, zs = [], []
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        mw = obj.matrix_world
        for v in obj.data.vertices:
            p = mw @ v.co
            xs.append(p.x)
            zs.append(p.z)
    x0, x1 = min(xs), max(xs)
    z0, z1 = min(zs), max(zs)
    # Wall solid is x > 0, 9 cm thick. Keep that strip, not the whole wall.
    x1 = max(x1, 0.22)
    z0 = min(z0, -0.14)
    pad_x = 0.10
    pad_z = 0.10
    x0 -= pad_x
    x1 += pad_x
    z1 += pad_z
    cx = (x0 + x1) * 0.5
    cz = (z0 + z1) * 0.5
    aspect = 560.0 / 720.0
    span = max(z1 - z0, (x1 - x0) / aspect)
    cap = arm.location
    return (cx, cap.y - 3.6, cz), (cx, cap.y, cz), span


def _plant_kick(arm, bones, cap, locals_c, polys, ids, local):
    """Put the raised sole on the wall face. The thigh may stay in the plate.

    The splay that clears the abdomen pulls this sole about 8 cm off the wall.
    The kick that sits on the face is kept, and the plate depth is reported.
    """
    base = _copy_bones(bones)
    x0, y0, z0 = base["UpperLeg_L"]
    lx = base["LowerLeg_L"][0]
    best = None
    for dz in range(-20, 21, 4):
        for dx in range(-20, 21, 4):
            for dk in (0.0, -15.0, 15.0, -30.0):
                trial = _copy_bones(base)
                trial["UpperLeg_L"] = [x0 + dx, y0, z0 + dz]
                trial["LowerLeg_L"][0] = max(KNEE_MIN, lx + dk)
                _apply(arm, trial, cap)
                med = p12._sole_z(ids, local, "L")[1]
                if med < 0.28:
                    continue
                ld = _sole_lead(ids, local, "L")
                if not (-0.005 <= ld <= 0.0):
                    continue
                depth = max(_plate(locals_c, polys, "L"), _plate(locals_c, polys, "R"))
                gap = abs(ld + 0.002)
                if best is None or (depth, gap) < (best[0], best[1]):
                    best = (depth, gap, ld, med, trial)
    if best is None:
        _apply(arm, bones, cap)
        return _sole_lead(ids, local, "L"), 0.0, _plate(locals_c, polys, "L")
    bones.clear()
    bones.update(best[4])
    _apply(arm, bones, cap)
    hip = arm.matrix_world @ arm.pose.bones["Hips"].head
    sh = (
        arm.matrix_world @ arm.pose.bones["Shoulder_L"].head
        + arm.matrix_world @ arm.pose.bones["Shoulder_R"].head
    ) * 0.5
    return best[2], hip.x - sh.x, best[0]


def _staged_keys():
    """Pass-12 tic-tac after the bridges and the story re-keys, before clearance."""
    src = _pass12_clip()
    keys = [{"bones": _copy_bones(k["bones"])} for k in src["keys"]]
    caps = [[float(v) for v in c] for c in src["capsule_preview_m"]]
    ref = [_copy_bones(k["bones"]) for k in keys]
    ref_caps = [c[:] for c in caps]
    for start, end in JUMPS:
        if end >= len(keys) or start < 1:
            continue
        _blend_range(
            keys, caps, start, end,
            ref[start - 1], ref[min(end + 1, len(keys) - 1)],
            ref_caps[start - 1], ref_caps[min(end + 1, len(keys) - 1)],
        )
    run = _copy_bones(ref[22])
    run_cap = ref_caps[22][:]
    keys[0]["bones"] = run
    caps[0] = run_cap[:]
    _blend_range(keys, caps, 1, 11, run, ref[12], run_cap, ref_caps[12])
    b15 = keys[15]["bones"]
    b15["UpperLeg_L"][0] = b15["UpperLeg_L"][0] - 18.0
    b15["LowerLeg_L"][0] = b15["LowerLeg_L"][0] + 14.0
    crouch = _copy_bones(ref[110])
    crouch_cap = ref_caps[110][:]
    rise = ref[156]
    rise_cap = ref_caps[156]
    for i in range(118, 153):
        keys[i]["bones"] = _copy_bones(crouch)
        caps[i] = crouch_cap[:]
    _blend_range(keys, caps, 153, 155, crouch, rise, crouch_cap, rise_cap)
    return keys


def _feet_for(i, cleaned):
    if i <= 11:
        return ["L"]
    if 118 <= i <= 152 or i == 110:
        return ["L", "R"]
    if i == 34:
        return ["R"]
    feet, _hands, _clear, _hc = p12._contacts(cleaned[i])
    return list(feet)


def _clamp_twist(val, ref_val, x_lo, x_hi):
    return [
        min(x_hi, max(x_lo, val[0])),
        min(ref_val[1] + 75.0, max(ref_val[1] - 75.0, val[1])),
        min(ref_val[2] + 50.0, max(ref_val[2] - 50.0, val[2])),
    ]


def _plant_knee(arm, bones, cap, side, ids, local, k0):
    """Bend or straighten the knee until this sole is on the floor. False if it cannot."""
    ll = f"LowerLeg_{side}"
    lo = k0 if k0 < KNEE_MIN else KNEE_MIN
    best = None
    for dk in (0.0, 8.0, -8.0, 16.0, -16.0, 28.0, -28.0, 40.0, -40.0, 56.0, 72.0):
        k = k0 + dk
        if k < lo - 0.1 or k > 145.0:
            continue
        bones[ll][0] = k
        _apply(arm, bones, cap)
        med = p12._sole_z(ids, local, side)[1]
        low = p11._min_z(f"Mesh_Foot_{side}")
        shin = p11._min_z(f"Mesh_LowerLeg_{side}")
        planted = low >= -0.004 and shin >= -0.004 and -0.004 <= med <= 0.022
        err = abs(med - 0.005) + (0.0 if low >= -0.004 else (-low) * 4.0) + (0.0 if shin >= -0.004 else (-shin) * 4.0)
        if best is None or (planted and (not best[0] or err < best[1])) or (not planted and not best[0] and err < best[1]):
            best = (planted, err, k)
    if best is None:
        bones[ll][0] = k0
        _apply(arm, bones, cap)
        return False
    bones[ll][0] = best[2]
    _apply(arm, bones, cap)
    return best[0]


def _search_side(arm, bones, cap, side, contact, air_min, ids, local, locals_c, polys, packs, x_cap, ref_ul):
    """Twist and a little hip extension. A contact sole has to stay on the floor."""
    ul, ll = f"UpperLeg_{side}", f"LowerLeg_{side}"
    saved_ul = bones[ul][:]
    saved_k = bones[ll][0]
    if p13._leg_depth(side, locals_c, polys, packs) <= CLEAR:
        return
    x0, y0, z0 = saved_ul
    best = None
    xs = (0.0, 12.0) if x_cap <= 16.0 else (0.0, 12.0, 24.0, 36.0)
    for y in (0.0, 30.0, -30.0, 55.0, -55.0, 70.0, -70.0, 80.0, -80.0):
        for z in (0.0, 24.0, -24.0, 45.0, -45.0, 55.0, -55.0):
            hit = False
            for x in xs:
                bones[ul] = _clamp_twist([x0 + x, y0 + y, z0 + z], ref_ul, x0 - 12.0, x0 + x_cap)
                bones[ll][0] = saved_k
                if contact:
                    if not _plant_knee(arm, bones, cap, side, ids, local, saved_k):
                        continue
                else:
                    _apply(arm, bones, cap)
                    med = p12._sole_z(ids, local, side)[1]
                    if p11._min_z(f"Mesh_Foot_{side}") < -0.004 or p11._min_z(f"Mesh_LowerLeg_{side}") < -0.004:
                        continue
                    if air_min is not None and air_min > 0.12 and med < 0.08:
                        continue
                depth = p13._leg_depth(side, locals_c, polys, packs)
                rank = (depth, abs(x) + 0.25 * (abs(y) + abs(z)))
                if best is None or rank < best[0]:
                    best = (rank, _copy_bones(bones))
                if depth <= CLEAR:
                    hit = True
                    break
            if hit:
                break
        if best is not None and best[0][0] <= CLEAR:
            break
    if best is None:
        bones[ul] = saved_ul
        bones[ll][0] = saved_k
        _apply(arm, bones, cap)
        return
    for name, vals in best[1].items():
        bones[name] = vals
    _apply(arm, bones, cap)


def _lift_edge(arm, bones, cap, side, ids, local, locals_c, polys, packs, x_cap):
    """Raise a sole that digs into the floor. The thigh twist stays put."""
    foot = f"Foot_{side}"
    ll = f"LowerLeg_{side}"
    ul = f"UpperLeg_{side}"
    mesh = f"Mesh_Foot_{side}"
    _apply(arm, bones, cap)
    low = p11._min_z(mesh)
    if low >= -0.003:
        return
    med0 = p12._sole_z(ids, local, side)[1]
    if med0 > 0.10 and low > 0.0:
        return
    plate0 = p13._leg_depth(side, locals_c, polys, packs)
    saved = _copy_bones(bones)
    # Pitch the foot first. A buried median needs the knee or the hip.
    trials = [(dx, 0.0, 0.0) for dx in (0.0, 8.0, -8.0, 16.0, -16.0, 28.0, -28.0, 40.0, -40.0)]
    if med0 < 0.0 or low < -0.012:
        for dk in (8.0, 16.0, 24.0, 36.0, 48.0, -8.0, -16.0):
            for hx in (0.0, -8.0, 8.0, -16.0, 16.0):
                trials.append((0.0, dk, hx))
    best = None
    for dx, dk, hx in trials:
        bones[foot][0] = saved[foot][0] + dx
        k = saved[ll][0] + dk
        lo = saved[ll][0] if saved[ll][0] < KNEE_MIN else KNEE_MIN
        if k < lo or k > 145.0:
            continue
        bones[ll][0] = k
        if abs(hx) > x_cap + 0.1:
            continue
        bones[ul][0] = saved[ul][0] + hx
        _apply(arm, bones, cap)
        low_t = p11._min_z(mesh)
        med = p12._sole_z(ids, local, side)[1]
        if low_t < -0.003:
            continue
        if med0 < 0.04 and med > 0.028:
            continue
        if med0 > 0.12 and med < 0.08:
            continue
        plate = p13._leg_depth(side, locals_c, polys, packs)
        if plate > max(CLEAR, plate0 + 0.001):
            continue
        best = (abs(dx) + abs(dk) + abs(hx), _copy_bones(bones))
        break
    if best is None:
        for name, vals in saved.items():
            bones[name] = vals
        _apply(arm, bones, cap)
        return
    for name, vals in best[1].items():
        bones[name] = vals
    _apply(arm, bones, cap)


def _splay(arm, bones, cap, contacts, ids, local, locals_c, polys, packs, x_cap, lock_left):
    depth = max(
        p13._pair_depth("Mesh_UpperLeg_L", "Mesh_UpperLeg_R", locals_c, polys),
        p13._pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
    )
    if depth <= CLEAR:
        return
    saved = _copy_bones(bones)
    zL = saved["UpperLeg_L"][2]
    zR = saved["UpperLeg_R"][2]
    best = (depth, saved)
    for dz in (8.0, 16.0, 24.0, 36.0):
        for sign in (1.0, -1.0):
            trial = _copy_bones(saved)
            if not lock_left:
                trial["UpperLeg_L"][2] = zL + sign * dz
            trial["UpperLeg_R"][2] = zR - sign * dz
            for name, vals in trial.items():
                bones[name] = vals
            ok = True
            for side in contacts:
                if lock_left and side == "L":
                    continue
                if not _plant_knee(arm, bones, cap, side, ids, local, saved[f"LowerLeg_{side}"][0]):
                    ok = False
                    break
            if not ok:
                continue
            _apply(arm, bones, cap)
            d = max(
                p13._pair_depth("Mesh_UpperLeg_L", "Mesh_UpperLeg_R", locals_c, polys),
                p13._pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
            )
            plate = max(
                p13._leg_depth("L", locals_c, polys, packs),
                p13._leg_depth("R", locals_c, polys, packs),
            )
            if plate > CLEAR + 0.002:
                continue
            if d < best[0]:
                best = (d, _copy_bones(bones))
            if d <= CLEAR:
                break
        if best[0] <= CLEAR:
            break
    for name, vals in best[1].items():
        bones[name] = vals
    _apply(arm, bones, cap)


def _arm_body(arm, side, locals_c, polys):
    """How far this hand and forearm sit inside the torso or a thigh."""
    depth = 0.0
    other = "L" if side == "R" else "R"
    for a in (f"Mesh_Hand_{side}", f"Mesh_LowerArm_{side}"):
        for b in (f"Mesh_UpperLeg_{side}", f"Mesh_UpperLeg_{other}", "Mesh_Chest", "Mesh_Hips", "Mesh_Head"):
            depth = max(depth, p13._pair_depth(a, b, locals_c, polys))
    return depth


def _pull_arm(arm, bones, cap, side, locals_c, polys):
    """Swing an arm off the wall and out of the thigh. The capsule stays put."""
    meshes = (f"Mesh_Hand_{side}", f"Mesh_LowerArm_{side}", f"Mesh_UpperArm_{side}")
    _apply(arm, bones, cap)
    wall0 = max(max(0.0, p11._max_x(name)) for name in meshes)
    body0 = _arm_body(arm, side, locals_c, polys)
    if wall0 <= CLEAR and body0 <= CLEAR:
        return
    base = _copy_bones(bones)
    origin = {bone: base[bone][:] for bone in (f"UpperArm_{side}", f"LowerArm_{side}")}

    def metric():
        wall = max(max(0.0, p11._max_x(name)) for name in meshes)
        body = _arm_body(arm, side, locals_c, polys)
        return max(wall, body), wall, body

    best = (max(wall0, body0), _copy_bones(bones))
    for bone in (f"UpperArm_{side}", f"LowerArm_{side}"):
        for axis in range(3):
            for delta in (12.0, -12.0, 24.0, -24.0, 36.0, -36.0):
                if abs(delta) > 40.0:
                    continue
                bones[bone][axis] = origin[bone][axis] + delta
                _apply(arm, bones, cap)
                # Wall is a vertex test. Only pay for the body when the wall improved
                # or the arm was already inside the body.
                wall = max(max(0.0, p11._max_x(name)) for name in meshes)
                if wall > best[0] and body0 <= CLEAR:
                    bones[bone][axis] = origin[bone][axis]
                    continue
                score, _wall, _body = metric()
                if score < best[0] - 0.0008:
                    best = (score, _copy_bones(bones))
                bones[bone][axis] = origin[bone][axis]
    for name, vals in best[1].items():
        bones[name] = vals
    _apply(arm, bones, cap)


def _foot_off_wall(arm, bones, cap, side):
    mesh = f"Mesh_Foot_{side}"
    name = f"Foot_{side}"
    _apply(arm, bones, cap)
    if p11._max_x(mesh) <= 0.002:
        return
    base = bones[name][:]
    best = (p11._max_x(mesh), base[:])
    for axis in (2, 0, 1):
        for delta in (10.0, -10.0, 20.0, -20.0, 35.0, -35.0):
            bones[name][axis] = base[axis] + delta
            _apply(arm, bones, cap)
            mx = p11._max_x(mesh)
            if mx < best[0] - 0.001:
                best = (mx, bones[name][:])
            bones[name] = base[:]
    bones[name] = best[1]
    _apply(arm, bones, cap)


def _metrics(arm, locals_c, polys, packs):
    plate = max(p13._leg_depth("L", locals_c, polys, packs), p13._leg_depth("R", locals_c, polys, packs))
    chest = p13._pair_depth("Mesh_Chest", "Mesh_Hips", locals_c, polys)
    cross = max(
        p13._pair_depth("Mesh_UpperLeg_L", "Mesh_UpperLeg_R", locals_c, polys),
        p13._pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
    )
    ground = 0.0
    for side in ("L", "R"):
        for part in ("Foot", "LowerLeg"):
            ground = max(ground, max(0.0, -p11._min_z(f"Mesh_{part}_{side}")))
    wall = max(0.0, p11._max_x("Mesh_Head"))
    for name in (
        "Mesh_Hand_L", "Mesh_Hand_R", "Mesh_LowerArm_L", "Mesh_LowerArm_R",
        "Mesh_Foot_L", "Mesh_Foot_R",
    ):
        wall = max(wall, max(0.0, p11._max_x(name)))
    return plate, chest, cross, ground, wall


def _story_ok(i, arm, bones, cap, ids, local, stick, orig_pitch, air_r):
    pitch = _torso_pitch(arm)
    if i <= 11:
        if abs(pitch - orig_pitch) > 8.0:
            return False
        med = p12._sole_z(ids, local, "L")[1]
        low = p11._min_z("Mesh_Foot_L")
        if low < -0.008 or med > 0.04:
            return False
        if air_r > 0.12 and p12._sole_z(ids, local, "R")[1] < 0.08:
            return False
        return True
    if i == 34:
        if abs(pitch - stick) > 10.0:
            return False
        lead = _sole_lead(ids, local, "L")
        if not (-0.005 <= lead <= 0.001):
            return False
        if p12._sole_z(ids, local, "L")[1] < 0.25:
            return False
        med = p12._sole_z(ids, local, "R")[1]
        low = p11._min_z("Mesh_Foot_R")
        if low < -0.008 or med > 0.035:
            return False
        hip = arm.matrix_world @ arm.pose.bones["Hips"].head
        sh = (
            arm.matrix_world @ arm.pose.bones["Shoulder_L"].head
            + arm.matrix_world @ arm.pose.bones["Shoulder_R"].head
        ) * 0.5
        if hip.x - sh.x < 0.03:
            return False
        return True
    if i in (76, 106):
        return abs(pitch - stick) <= 10.0
    if i == 110 or 118 <= i <= 152:
        return abs(pitch - orig_pitch) <= 8.0
    return True


def _repair_frame(i, arm, bones, cap, ref_spine, ref_bones, contacts, ids, local, locals_c, polys, stick, orig_pitch, air_r):
    if i == 15:
        return
    base = _copy_bones(bones)
    _apply(arm, bones, cap)
    packs = p13._torso_packs(locals_c, polys)
    plate, chest, cross, ground, head = _metrics(arm, locals_c, polys, packs)
    hand_wall = max(p11._max_x("Mesh_Hand_L"), p11._max_x("Mesh_Hand_R"), p11._max_x("Mesh_LowerArm_L"), p11._max_x("Mesh_LowerArm_R"))
    if max(plate, chest, cross, ground, head, max(0.0, hand_wall)) <= CLEAR:
        # Arms can still pass through a thigh while staying off the wall.
        tangled = False
        for side in ("L", "R"):
            if p13._pair_depth(f"Mesh_Hand_{side}", f"Mesh_UpperLeg_{side}", locals_c, polys) > CLEAR:
                tangled = True
        if not tangled:
            return
    x_cap = 12.0 if i in (0, 15, 34, 76, 106, 110) or i >= 118 or i <= 11 else 36.0
    lock_left = i == 34
    need_spine = chest > CLEAR or head > 0.002
    cur = base.get("Spine", [0.0, 0.0, 0.0])
    options = [(cur[0] - ref_spine[0], cur[2] - ref_spine[2])]
    if need_spine:
        for flex in (-10.0, 0.0, 10.0):
            for roll in (-10.0, 0.0, 10.0):
                options.append((flex, roll))
    seen = set()
    _apply(arm, base, cap)
    packs = p13._torso_packs(locals_c, polys)
    base_vals = _metrics(arm, locals_c, polys, packs)
    best = ((max(base_vals), sum(base_vals)), _copy_bones(base))
    for flex, roll in options:
        key = (round(flex, 1), round(roll, 1))
        if key in seen:
            continue
        seen.add(key)
        trial = _copy_bones(base)
        trial["Spine"] = [ref_spine[0] + flex, ref_spine[1], ref_spine[2] + roll]
        if lock_left:
            for name in ("UpperLeg_L", "LowerLeg_L", "Foot_L"):
                trial[name] = base[name][:]
        for name, vals in trial.items():
            bones[name] = vals
        _apply(arm, bones, cap)
        packs = p13._torso_packs(locals_c, polys)
        for side in ("L", "R"):
            if lock_left and side == "L":
                continue
            air = air_r if side == "R" and i <= 11 else None
            ref_ul = ref_bones.get(f"UpperLeg_{side}", bones[f"UpperLeg_{side}"])
            if p13._leg_depth(side, locals_c, polys, packs) > CLEAR:
                _search_side(
                    arm, bones, cap, side, side in contacts, air, ids, local,
                    locals_c, polys, packs, x_cap, ref_ul,
                )
        _splay(arm, bones, cap, contacts, ids, local, locals_c, polys, packs, x_cap, lock_left)
        _apply(arm, bones, cap)
        packs = p13._torso_packs(locals_c, polys)
        for side in ("L", "R"):
            if lock_left and side == "L":
                continue
            if side in contacts or p11._min_z(f"Mesh_Foot_{side}") < -0.003:
                _lift_edge(arm, bones, cap, side, ids, local, locals_c, polys, packs, x_cap)
        if not _story_ok(i, arm, bones, cap, ids, local, stick, orig_pitch, air_r):
            continue
        packs = p13._torso_packs(locals_c, polys)
        vals = _metrics(arm, locals_c, polys, packs)
        score = (max(vals), sum(vals))
        if score < best[0]:
            best = (score, _copy_bones(bones))
        if score[0] <= CLEAR and not need_spine:
            break
    chosen = best[1]
    for name, vals in chosen.items():
        bones[name] = vals
    _apply(arm, bones, cap)
    body_pose = _copy_bones(bones)
    for side in ("L", "R"):
        _pull_arm(arm, bones, cap, side, locals_c, polys)
        _foot_off_wall(arm, bones, cap, side)
    if lock_left:
        for name in ("UpperLeg_L", "LowerLeg_L", "Foot_L"):
            bones[name] = base[name][:]
        _apply(arm, bones, cap)
    packs = p13._torso_packs(locals_c, polys)
    for side in ("L", "R"):
        if lock_left and side == "L":
            continue
        if p11._min_z(f"Mesh_Foot_{side}") < -0.003:
            _lift_edge(arm, bones, cap, side, ids, local, locals_c, polys, packs, x_cap)
    if not _story_ok(i, arm, bones, cap, ids, local, stick, orig_pitch, air_r):
        for name, vals in body_pose.items():
            bones[name] = vals
        _apply(arm, bones, cap)
        if not _story_ok(i, arm, bones, cap, ids, local, stick, orig_pitch, air_r):
            for name, vals in base.items():
                bones[name] = vals
            _apply(arm, bones, cap)


def _repair_keys(arm, entry, locals_c, polys, ids, local, cleaned, worlds, times):
    """Clear what the thigh search left. Spine stays within 10° of the staged curl.

    Hands leave the wall through the shoulder and the elbow. Buried soles leave
    the floor through the knee, the hip and the ankle. The capsule is not moved.
    """
    staged = _staged_keys()
    ref_spines = [[float(v) for v in k["bones"].get("Spine", [0.0, 0.0, 0.0])] for k in staged]
    keys = entry["keys"]
    caps = entry["capsule_preview_m"]
    notes = [
        "REPAIR",
        "Hands come off the wall through the arm. Buried soles come up through the knee, the hip and the ankle.",
        "Thigh twist clears the abdomen. Spine flex stays within 10° of the bridged reference curl.",
        "The preview capsule is not moved. Frame 15 is left as the deep crouch.",
    ]
    before_110 = _copy_bones(keys[110]["bones"])
    order = [110] + [i for i in range(len(keys)) if i != 110]
    copied = False
    for i in order:
        if copied and 118 <= i <= 152:
            continue
        bones = _copy_bones(keys[i]["bones"])
        cap = [float(v) for v in caps[i]]
        cap_before = cap[:]
        _apply(arm, bones, cap)
        orig_pitch = _torso_pitch(arm)
        air_r = p12._sole_z(ids, local, "R")[1]
        stick = _landmark_pitch(worlds[i])
        _repair_frame(
            i, arm, bones, cap, ref_spines[i], staged[i]["bones"],
            _feet_for(i, cleaned), ids, local, locals_c, polys, stick, orig_pitch, air_r,
        )
        if [round(v, 4) for v in cap] != [round(v, 4) for v in cap_before]:
            raise RuntimeError(f"capsule moved on repair f={i}")
        keys[i]["bones"] = p10._round_eulers(bones)
        if i == 110 and _copy_bones(keys[118]["bones"]) == before_110:
            for j in range(118, 153):
                keys[j]["bones"] = _copy_bones(keys[110]["bones"])
            copied = True
            notes.append("REPAIR copied the landing crouch from f=110 through f=152.")
        if i % 10 == 0:
            _apply(arm, bones, caps[i])
            packs = p13._torso_packs(locals_c, polys)
            plate, chest, cross, ground, wall = _metrics(arm, locals_c, polys, packs)
            print(
                f"repair {i} plate={plate*100:.2f} chest={chest*100:.2f} "
                f"cross={cross*100:.2f} ground={ground*100:.2f} wall={wall*100:.2f}",
                flush=True,
            )
    # Residuals the proxy can see. Full pose count is the noclip proof.
    residuals = []
    for i, key in enumerate(keys):
        _apply(arm, key["bones"], caps[i])
        packs = p13._torso_packs(locals_c, polys)
        plate, chest, cross, ground, wall = _metrics(arm, locals_c, polys, packs)
        worst = max(plate, chest, cross, ground, wall)
        if worst > 0.005:
            residuals.append(
                f"f={i} plate={plate*100:.2f} chest={chest*100:.2f} cross={cross*100:.2f} "
                f"ground={ground*100:.2f} wall={wall*100:.2f}"
            )
    notes.append("REPAIR residuals over 0.5 cm (plate, chest-hips, thighs crossing, ground, head in the wall):")
    notes.extend(residuals if residuals else ["none"])
    _apply(arm, keys[34]["bones"], caps[34])
    lead = _sole_lead(ids, local, "L")
    hip = arm.matrix_world @ arm.pose.bones["Hips"].head
    sh = (
        arm.matrix_world @ arm.pose.bones["Shoulder_L"].head
        + arm.matrix_world @ arm.pose.bones["Shoulder_R"].head
    ) * 0.5
    packs = p13._torso_packs(locals_c, polys)
    plate = max(p13._leg_depth("L", locals_c, polys, packs), p13._leg_depth("R", locals_c, polys, packs))
    notes.append(
        f"WALL3 f=34 soleLead={lead*100:.2f}cm kickZ={p12._sole_z(ids, local, 'L')[1]*100:.1f}cm "
        f"supportSole={p12._sole_z(ids, local, 'R')[1]*100:.1f}cm "
        f"shoulderBehindHip={(hip.x - sh.x)*100:.1f}cm plate={plate*100:.2f}cm"
    )
    notes.append("REPAIR spine flex versus the bridged reference. World pitch is neck-hip from vertical.")
    for i in STORY:
        spine = keys[i]["bones"].get("Spine", [0.0, 0.0, 0.0])
        ref_s = ref_spines[i]
        _apply(arm, keys[i]["bones"], caps[i])
        world = _torso_pitch(arm)
        stick = _landmark_pitch(worlds[i])
        dlocal = max(abs(spine[k] - ref_s[k]) for k in range(3))
        notes.append(
            f"f={i} t={times[i] - times[0]:.2f}s "
            f"spine={spine[0]:.1f},{spine[1]:.1f},{spine[2]:.1f} "
            f"ref={ref_s[0]:.1f},{ref_s[1]:.1f},{ref_s[2]:.1f} "
            f"dLocal={dlocal:.1f}° world={world:.1f}° stick={stick:.1f}° dWorld={world - stick:+.1f}°"
        )
    return notes


def repair():
    os.makedirs(PASS14, exist_ok=True)
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    p10._ensure_pose(arm)
    pieces = proof._pieces(arm)
    locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
    polys = {obj.name: proof._polys(obj) for obj in pieces}
    ids, local = p10._sole_ids(arm)
    doc = json.load(open(JSON_PATH))
    entry = doc["clips"][TIC]
    data = sp.load_clip(TIC)
    cleaned = p11._cleaned(data["world_xyz_m"], p11._spike_flags(data["world_xyz_m"]))
    notes = _repair_keys(
        arm, entry, locals_c, polys, ids, local, cleaned, data["world_xyz_m"], data["times_s"],
    )
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "a") as f:
        f.write("\n".join(notes) + "\n")
    print("wrote", JSON_PATH, flush=True)
    print("wrote", ERROR_PATH, flush=True)
    rh._reset(arm)


def solve():
    os.makedirs(PASS14, exist_ok=True)
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    p10._ensure_pose(arm)
    pieces = proof._pieces(arm)
    locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
    polys = {obj.name: proof._polys(obj) for obj in pieces}
    ids, local = p10._sole_ids(arm)
    doc = json.load(open(JSON_PATH))
    src = _pass12_clip()
    entry = doc["clips"][TIC]
    entry["keys"] = src["keys"]
    entry["capsule_preview_m"] = src["capsule_preview_m"]
    keys = entry["keys"]
    caps = entry["capsule_preview_m"]
    data = sp.load_clip(TIC)
    worlds = data["world_xyz_m"]
    cleaned = p11._cleaned(worlds, p11._spike_flags(worlds))
    notes = [
        "PASS14 APPLIED",
        f"clip {TIC}",
        "Spine and chest are the pass-12 reference curl. A frame may leave that curl by at most 10°.",
        "The thigh twist opens the abdomen. The torso is not straightened to rest.",
        "Pass-13 capsule drops that seated soles are removed. The preview path is the pass-12 capsule.",
        "Bridged ranges replace a garbage stick. Their capsule is the anchor path, not a sole drop.",
        "Contact soles are seated with the knee, the hip and the ankle. The capsule is not moved.",
    ]
    # Jump bridges from the pass-12 anchors, before the crash and heap replacement.
    ref = [_copy_bones(k["bones"]) for k in keys]
    ref_caps = [[float(v) for v in c] for c in caps]
    for start, end in JUMPS:
        if end >= len(keys) or start < 1:
            continue
        _blend_range(
            keys, caps, start, end,
            ref[start - 1], ref[min(end + 1, len(keys) - 1)],
            ref_caps[start - 1], ref_caps[min(end + 1, len(keys) - 1)],
        )
        t0 = data["times_s"][start] - data["times_s"][0]
        t1 = data["times_s"][end] - data["times_s"][0]
        notes.append(f"BRIDGE f={start}-{end} t={t0:.2f}-{t1:.2f}s anchors {start - 1},{end + 1} (stick jump)")
    # 0.00 s crash becomes the approach run, blended into the first upright frame.
    run = _copy_bones(ref[22])
    run_cap = ref_caps[22][:]
    keys[0]["bones"] = run
    caps[0] = run_cap[:]
    _blend_range(keys, caps, 1, 11, run, ref[12], run_cap, ref_caps[12])
    notes.append(
        "BRIDGE f=0-11 t=0.00-0.37s into the approach run. "
        "Frame 0 is the stride at 0.73 s. The 0.00 s stick is a forward crash and is not retargeted."
    )
    # Deepen 0.50 s. Pass 12 had the hip height and a lifted rear foot, but the
    # support thigh was too straight, so the pose read as a lean-back stand.
    b15 = keys[15]["bones"]
    b15["UpperLeg_L"][0] = b15["UpperLeg_L"][0] - 18.0
    b15["LowerLeg_L"][0] = b15["LowerLeg_L"][0] + 14.0
    notes.append(
        "REKEY f=15 t=0.50s support thigh -18° and knee +14° so the stick's deep crouch is not a straight leg."
    )
    # 4.84 s heap becomes the landing crouch (the 3.67 s pose), then rises.
    crouch = _copy_bones(ref[110])
    crouch_cap = ref_caps[110][:]
    rise = ref[156]
    rise_cap = ref_caps[156]
    for i in range(118, 153):
        keys[i]["bones"] = _copy_bones(crouch)
        caps[i] = crouch_cap[:]
    _blend_range(keys, caps, 153, 155, crouch, rise, crouch_cap, rise_cap)
    notes.append(
        "BRIDGE f=118-155 t=3.94-5.17s into the landing crouch at 3.67 s, "
        "then up to the recovery at 5.21 s. The 4.84 s stick is a prone heap and is not retargeted."
    )
    ref_spine = []
    for key in keys:
        spine = key["bones"].get("Spine", [0.0, 0.0, 0.0])
        ref_spine.append([float(v) for v in spine])
    misses = []
    wall_lead = None
    wall_away = None
    wall_plate = None
    for i, key in enumerate(keys):
        bones = _copy_bones(key["bones"])
        cap = [float(v) for v in caps[i]]
        cap_before = cap[:]
        if i <= 11:
            feet = ["L"]
        elif 118 <= i <= 152:
            feet = ["L", "R"]
        else:
            feet, _hands, _clear, _hc = p12._contacts(cleaned[i])
        if i == 34:
            feet = ["R"]
        # The wall-kick curl. Ten degrees of flexion leans the chest off the wall
        # and leaves the kicking sole where the pass-12 plant put it.
        flex_lock = -10.0 if i == 34 else None
        _apply(arm, bones, cap)
        delta, depth = _clear_frame(
            arm, bones, cap, locals_c, polys, ref_spine[i], feet, ids, local, flex_lock,
        )
        for side in feet:
            _apply(arm, bones, cap)
            med = p12._sole_z(ids, local, side)[1]
            low = p11._min_z(f"Mesh_Foot_{side}")
            if med > 0.015 or low < -0.003:
                held = _copy_bones(bones)
                med, low, _ok = _seat(arm, bones, cap, side, ids, local)
                if _plate(locals_c, polys, side) > CLEAR + 0.002:
                    bones.clear()
                    bones.update(held)
                    _apply(arm, bones, cap)
                    med = p12._sole_z(ids, local, side)[1]
                    low = p11._min_z(f"Mesh_Foot_{side}")
                if med > 0.015 or low < -0.004:
                    misses.append(f"f={i} {side} sole={med * 100:.1f}cm low={low * 100:.1f}cm")
        for side in ("L", "R"):
            if side in feet:
                continue
            if p11._min_z(f"Mesh_Foot_{side}") < -0.003:
                _lift_buried(arm, bones, cap, side)
        if i == 34:
            wall_lead, wall_away, wall_plate = _plant_kick(
                arm, bones, cap, locals_c, polys, ids, local,
            )
        # The capsule is the preview path. Seating must not have written it.
        if [round(v, 4) for v in cap] != [round(v, 4) for v in cap_before]:
            raise RuntimeError(f"capsule moved on f={i}")
        key["bones"] = p10._round_eulers(bones)
        caps[i] = [round(float(v), 4) for v in cap_before]
        if i % 25 == 0:
            print(f"cleared {i} depth={depth * 100:.2f} spineΔ={delta}", flush=True)
    notes.extend(_repair_keys(arm, entry, locals_c, polys, ids, local, cleaned, worlds, data["times_s"]))
    # Spine flex against the reference curl, and against the stick's torso pitch.
    notes.append("SPINE flex is the local Spine euler versus the reference curl. World pitch is the neck-hip angle from vertical.")
    for i in STORY:
        bones = keys[i]["bones"]
        spine = bones.get("Spine", [0.0, 0.0, 0.0])
        ref_s = ref_spine[i]
        _apply(arm, bones, caps[i])
        world = _torso_pitch(arm)
        stick = _landmark_pitch(worlds[i])
        dlocal = max(abs(spine[k] - ref_s[k]) for k in range(3))
        notes.append(
            f"f={i} t={data['times_s'][i] - data['times_s'][0]:.2f}s "
            f"spine={spine[0]:.1f},{spine[1]:.1f},{spine[2]:.1f} "
            f"ref={ref_s[0]:.1f},{ref_s[1]:.1f},{ref_s[2]:.1f} "
            f"dLocal={dlocal:.1f}° world={world:.1f}° stick={stick:.1f}° dWorld={world - stick:+.1f}°"
        )
    if wall_lead is not None:
        notes.append(
            f"WALL f=34 soleLead={wall_lead * 100:.2f}cm "
            f"shoulderBehindHip={wall_away * 100:.1f}cm "
            f"plate={wall_plate * 100:.2f}cm"
        )
        notes.append(
            "The splay that clears the abdomen pulls the kick sole off the wall. "
            "Frame 34 keeps the sole on the face. plate is the abdomen residual on that frame."
        )
    notes.append("SOLE misses (contact sole not within 1.5 cm without moving the capsule):")
    notes.extend(misses if misses else ["none"])
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(notes) + "\n")
    print("wrote", JSON_PATH, flush=True)
    print("wrote", ERROR_PATH, flush=True)
    rh._reset(arm)
    rh._hide_wall()


def render_strips():
    from PIL import Image, ImageDraw
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = p10._sole_ids(arm)
    p10._ensure_pose(arm)
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 560
    bpy.context.scene.render.resolution_y = 720
    doc = json.load(open(JSON_PATH))
    os.makedirs(PASS14, exist_ok=True)
    prev = "/tmp/pass14_cells"
    os.makedirs(prev, exist_ok=True)
    entry = doc["clips"][TIC]
    data = sp.load_clip(TIC)
    t0 = data["times_s"][0]
    cleaned = p11._cleaned(data["world_xyz_m"], p11._spike_flags(data["world_xyz_m"]))
    frames = [i for i in p11.STORY[TIC] if i < len(entry["keys"])]
    shots = []
    captions = []
    kick_shot = None
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
        feet, _hands, _c, _hc = p12._contacts(cleaned[i])
        if i == 34:
            feet = ["R"]
        elif i <= 11:
            feet = ["L"]
        elif i in (145, 152):
            feet = ["L", "R"]
        captions.append(p12._sole_caption(ids, local, feet) if feet else "")
        p11._show_floor(arm)
        p11._show_shadow(p11._contact_x(arm), arm.location.y)
        rh._show_wall(0.0, entry.get("wall_top_m"), arm.location.y, 3.2, side=entry.get("wall_side", "-"))
        loc, look, ortho = _frame_scene(arm)
        path = os.path.join(prev, f"{TIC}_{s}.png")
        rh._shot(path, loc, look, ortho=ortho)
        shots.append(path)
        if i == 34:
            kick_shot = (cap, loc, look)
        print(f"ref {TIC} f={i} ortho={ortho:.2f}", flush=True)
    cell_w, cell_h = 400, 300
    canvas = Image.new("RGB", (cell_w * 4, cell_h * 2 + 28), (48, 44, 40))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle((0, 0, 1600, 28), fill=(20, 18, 16))
    draw.text((12, 8), "12 tic-tac   pass 14", fill=(245, 236, 220))
    for s, i in enumerate(frames):
        stick = p11._draw_stick(cleaned[i], 172, 268)
        man = Image.open(shots[s]).convert("RGB")
        man.thumbnail((214, 268), Image.Resampling.LANCZOS)
        col = s % 4
        row = s // 4
        x0 = col * cell_w
        y0 = 28 + row * cell_h
        canvas.paste(stick, (x0 + 6, y0 + 16))
        canvas.paste(man, (x0 + 182 + (214 - man.width) // 2, y0 + 16 + (268 - man.height) // 2))
        t = data["times_s"][i] - t0
        draw.text((x0 + 10, y0 + 2), f"{t:.2f}s  {captions[s]}", fill=(245, 236, 220))
    out = os.path.join(PASS14, f"ref_{rh.SLUGS[TIC]}.png")
    canvas.save(out, optimize=True)
    if os.path.getsize(out) > 400_000:
        q = canvas.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
        q.save(out, optimize=True)
    print("wrote", out, os.path.getsize(out), flush=True)
    if kick_shot is not None:
        cap, _loc, _look = kick_shot
        # Three-quarter: from the body's open side, looking at the sole on the wall.
        bpy.context.scene.render.resolution_x = 720
        bpy.context.scene.render.resolution_y = 720
        cam = (cap[0] - 1.15, cap[1] - 2.35, 1.05)
        look = (0.0, cap[1], 0.72)
        kick_path = os.path.join(prev, "kick.png")
        rh._shot(kick_path, cam, look, ortho=None)
        kick = Image.open(kick_path).convert("RGB")
        kick.thumbnail((640, 640), Image.Resampling.LANCZOS)
        kick_out = os.path.join(PASS14, f"ref_{rh.SLUGS[TIC]}_kick.png")
        kick.save(kick_out, optimize=True)
        if os.path.getsize(kick_out) > 400_000:
            q = kick.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
            q.save(kick_out, optimize=True)
        print("wrote", kick_out, os.path.getsize(kick_out), flush=True)
    rh._reset(arm)
    rh._hide_wall()


def _flat_fwd(hip, shoulder_l, shoulder_r, nose):
    up = ((shoulder_l + shoulder_r) * 0.5) - hip
    if up.length < 1e-6:
        up = Vector((0.0, 0.0, 1.0))
    up.normalize()
    right = shoulder_r - shoulder_l
    right = right - up * right.dot(up)
    if right.length < 1e-6:
        right = Vector((1.0, 0.0, 0.0))
    right.normalize()
    fwd = up.cross(right)
    if nose is not None and fwd.dot(nose - (shoulder_l + shoulder_r) * 0.5) < 0.0:
        fwd = -fwd
    flat = Vector((fwd.x, fwd.y, 0.0))
    if flat.length < 1e-6:
        flat = Vector((0.0, -1.0, 0.0))
    flat.normalize()
    return flat, up


def _sole_point(ids, local, side):
    obj = bpy.data.objects[f"Mesh_Foot_{side}"]
    mw = obj.matrix_world
    acc = Vector((0.0, 0.0, 0.0))
    n = 0
    for vi in ids[side]:
        acc += mw @ local[side][vi]
        n += 1
    if n == 0:
        return arm_fallback()
    return acc / n


def arm_fallback():
    return Vector((0.0, 0.0, 0.0))


def _rig_fwd(arm):
    hip = arm.matrix_world @ arm.pose.bones["Hips"].head
    sl = arm.matrix_world @ arm.pose.bones["Shoulder_L"].head
    sr = arm.matrix_world @ arm.pose.bones["Shoulder_R"].head
    nose = arm.matrix_world @ arm.pose.bones["Head"].head
    flat, up = _flat_fwd(hip, sl, sr, nose)
    return hip, flat, up


def _support_back(arm, ids, local, feet):
    """Metres the pelvis sits behind the support-foot midpoint, along facing."""
    hip, fwd, _up = _rig_fwd(arm)
    sides = list(feet) if feet else ["L", "R"]
    pts = [_sole_point(ids, local, side) for side in sides]
    foot = sum(pts, Vector((0.0, 0.0, 0.0))) / len(pts)
    return (foot - hip).dot(fwd), hip, fwd, foot


def _sagittal(child_dir, parent_dir, fwd):
    c = child_dir.normalized()
    p = parent_dir.normalized()
    left = p.cross(fwd)
    if left.length < 1e-6:
        return 0.0
    left.normalize()
    return math.degrees(math.atan2((c - p * c.dot(p)).dot(fwd), max(-1.0, min(1.0, c.dot(p)))))


def _hinge(arm, feet, fwd, up):
    """Hip flexion over lumbar plus chest flexion. A neutral spine scores high."""
    flexes = []
    for side in (feet or ["L", "R"]):
        thigh = (arm.matrix_world @ arm.pose.bones[f"LowerLeg_{side}"].head) - (
            arm.matrix_world @ arm.pose.bones[f"UpperLeg_{side}"].head
        )
        flexes.append(_sagittal(thigh, -up, fwd))
    hip_deg = min(flexes) if flexes else 0.0
    spine = arm.pose.bones["Spine"]
    chest = arm.pose.bones["Chest"]
    spine_dir = (arm.matrix_world @ spine.tail) - (arm.matrix_world @ spine.head)
    chest_dir = (arm.matrix_world @ chest.tail) - (arm.matrix_world @ chest.head)
    lumbar = abs(_sagittal(spine_dir, up, fwd))
    chest_deg = abs(_sagittal(chest_dir, spine_dir, fwd))
    denom = lumbar + chest_deg
    if denom < 1.0 and hip_deg > 0.0:
        ratio = 99.0
    elif denom >= 1.0:
        ratio = hip_deg / denom
    else:
        ratio = 0.0
    return ratio, hip_deg, lumbar, chest_deg


def _track_back(frame, feet):
    hip = (Vector(frame[23]) + Vector(frame[24])) * 0.5
    hip_b = p10._mp(hip)
    sl, sr = p10._mp(Vector(frame[11])), p10._mp(Vector(frame[12]))
    nose = p10._mp(Vector(frame[0]))
    fwd, _up = _flat_fwd(hip_b, sl, sr, nose)
    pts = []
    for side in feet:
        idxs = (27, 29, 31) if side == "L" else (28, 30, 32)
        acc = Vector((0.0, 0.0, 0.0))
        for j in idxs:
            acc += p10._mp(Vector(frame[j]))
        pts.append(acc / 3.0)
    if not pts:
        return None
    foot = sum(pts, Vector((0.0, 0.0, 0.0))) / len(pts)
    return (foot - hip_b).dot(fwd)


def _garbage_track(i):
    """Bridged frames whose stick is a crash, a heap, or a tracked jump."""
    if i <= 11 or 118 <= i <= 155:
        return True
    for start, end in JUMPS:
        if start <= i <= end:
            return True
    return False


def _sit_target(i, hip_z, feet, track):
    crouch = hip_z < 0.78 and len(feet) >= 2
    target = 0.12 if crouch else 0.08
    if not _garbage_track(i) and track is not None and track > target:
        reach = math.sqrt(max(0.04, 0.91 ** 2 - max(0.05, hip_z - 0.08) ** 2))
        target = min(track, reach * 0.9)
    return target


def _sit_frame(i, arm, bones, cap, feet, ids, local, locals_c, polys, target):
    """Swing each support thigh so the pelvis sits behind the foot. No capsule move."""
    lock_left = i == 34
    start = _copy_bones(bones)
    accepted = _copy_bones(bones)
    _apply(arm, bones, cap)
    back, _hip, _fwd, _foot = _support_back(arm, ids, local, feet)
    if back >= target - 0.008:
        return back, "met"
    packs = p13._torso_packs(locals_c, polys)
    plate0 = max(p13._leg_depth("L", locals_c, polys, packs), p13._leg_depth("R", locals_c, polys, packs))
    cross0 = max(
        p13._pair_depth("Mesh_UpperLeg_L", "Mesh_UpperLeg_R", locals_c, polys),
        p13._pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
    )
    why = "met"
    for _step in range(7):
        _apply(arm, bones, cap)
        back, _hip, _fwd, _foot = _support_back(arm, ids, local, feet)
        if back >= target - 0.008:
            why = "met"
            break
        cands = []
        for side in feet:
            if lock_left and side == "L":
                continue
            ul = f"UpperLeg_{side}"
            ll = f"LowerLeg_{side}"
            base_ul = bones[ul][:]
            k0 = bones[ll][0]
            for axis in range(3):
                for delta in (-12.0, 12.0, -24.0, 24.0):
                    if abs((base_ul[axis] + delta) - start[ul][axis]) > 48.0:
                        continue
                    bones[ul] = base_ul[:]
                    bones[ul][axis] = base_ul[axis] + delta
                    bones[ll][0] = k0
                    planted = _plant_knee(arm, bones, cap, side, ids, local, k0)
                    _apply(arm, bones, cap)
                    med = p12._sole_z(ids, local, side)[1]
                    low = p11._min_z(f"Mesh_Foot_{side}")
                    if not planted and (low < -0.008 or med > 0.04):
                        continue
                    if low < -0.008 or med > 0.045:
                        continue
                    new_back, _h, _f, _ft = _support_back(arm, ids, local, feet)
                    if new_back < back + 0.012:
                        continue
                    cands.append((new_back, _copy_bones(bones)))
            bones[ul] = base_ul
            bones[ll][0] = k0
        if not cands:
            why = "leg cannot put the sole far enough forward without leaving the floor"
            break
        cands.sort(key=lambda c: -c[0])
        took = False
        for new_back, pose in cands[:4]:
            for name, vals in pose.items():
                bones[name] = vals
            _apply(arm, bones, cap)
            packs = p13._torso_packs(locals_c, polys)
            plate = max(p13._leg_depth("L", locals_c, polys, packs), p13._leg_depth("R", locals_c, polys, packs))
            cross = max(
                p13._pair_depth("Mesh_UpperLeg_L", "Mesh_UpperLeg_R", locals_c, polys),
                p13._pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
            )
            if plate > 0.005 and plate > plate0 + 0.0015:
                continue
            if cross > 0.005 and cross > cross0 + 0.0015:
                continue
            took = True
            back = new_back
            break
        if not took:
            why = "the thigh meets the spine before the pelvis is far enough back"
            break
        accepted = _copy_bones(bones)
    else:
        why = "met" if back >= target - 0.008 else "leg cannot put the sole far enough forward without leaving the floor"
    if why != "met":
        for name, vals in accepted.items():
            bones[name] = vals
        _apply(arm, bones, cap)
        back, _hip, _fwd, _foot = _support_back(arm, ids, local, feet)
    return back, why


def _mark_mat(name, color):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = color
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = color
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 8.0
        mat.diffuse_color = color
    return mat


def _ensure_marker(name, primitive):
    ob = bpy.data.objects.get(name)
    if ob is not None:
        return ob
    if primitive == "cube":
        bpy.ops.mesh.primitive_cube_add()
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8)
    ob = bpy.context.active_object
    ob.name = name
    return ob


def _show_hip_marks(arm, ids, local, feet):
    back, hip, _fwd, foot = _support_back(arm, ids, local, feet)
    line = _ensure_marker("HipSitLine", "cube")
    line.data.materials.clear()
    line.data.materials.append(_mark_mat("HipSitLineMat", (0.15, 0.95, 1.0, 1.0)))
    line.location = (foot.x, foot.y, 0.85)
    line.scale = (0.007, 0.007, 0.85)
    line.hide_render = False
    line.hide_set(False)
    dot = _ensure_marker("HipSitDot", "sphere")
    dot.data.materials.clear()
    dot.data.materials.append(_mark_mat("HipSitDotMat", (1.0, 0.85, 0.1, 1.0)))
    # Toward the side camera so the dot is not buried in the pelvis.
    dot.location = (hip.x, hip.y - 0.06, hip.z)
    dot.scale = (0.045, 0.045, 0.045)
    dot.hide_render = False
    dot.hide_set(False)
    return back


def _hide_hip_marks():
    for name in ("HipSitLine", "HipSitDot"):
        ob = bpy.data.objects.get(name)
        if ob is not None:
            ob.hide_render = True
            ob.hide_set(True)


def render_hipsit(tag):
    """Full-size side views. A vertical line through the support foot, a dot on the pelvis."""
    from PIL import Image, ImageDraw
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = p10._sole_ids(arm)
    p10._ensure_pose(arm)
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 560
    bpy.context.scene.render.resolution_y = 720
    doc = json.load(open(JSON_PATH))
    entry = doc["clips"][TIC]
    data = sp.load_clip(TIC)
    cleaned = p11._cleaned(data["world_xyz_m"], p11._spike_flags(data["world_xyz_m"]))
    os.makedirs(PASS14, exist_ok=True)
    prev = "/tmp/pass14_hip_cells"
    os.makedirs(prev, exist_ok=True)
    frames = [i for i in STORY if i < len(entry["keys"])]
    shots = []
    captions = []
    for s, i in enumerate(frames):
        rh._apply_eulers(arm, entry["keys"][i]["bones"])
        arm.location = Vector(entry["capsule_preview_m"][i])
        arm.rotation_euler = (0.0, 0.0, 0.0)
        root = arm.pose.bones.get("Root")
        if root is not None:
            root.location = (0.0, 0.0, 0.0)
            root.rotation_euler = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()
        feet = _feet_for(i, cleaned)
        back = _show_hip_marks(arm, ids, local, feet)
        p11._show_floor(arm)
        p11._show_shadow(p11._contact_x(arm), arm.location.y)
        rh._show_wall(0.0, entry.get("wall_top_m"), arm.location.y, 3.2, side=entry.get("wall_side", "-"))
        loc, look, ortho = _frame_scene(arm)
        path = os.path.join(prev, f"{tag}_{s}.png")
        rh._shot(path, loc, look, ortho=ortho)
        shots.append(path)
        t = data["times_s"][i] - data["times_s"][0]
        captions.append(f"{t:.2f}s  hip {back * 100:.0f} cm behind")
        print(f"hipstill {tag} f={i} back={back * 100:.1f}", flush=True)
    _hide_hip_marks()
    cell_w, cell_h = 420, 520
    canvas = Image.new("RGB", (cell_w * 4, cell_h * 2 + 28), (48, 44, 40))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle((0, 0, canvas.width, 28), fill=(20, 18, 16))
    draw.text((12, 8), f"12 tic-tac  hip sit  {tag}", fill=(245, 236, 220))
    for s, i in enumerate(frames):
        im = Image.open(shots[s]).convert("RGB")
        im.thumbnail((cell_w - 16, cell_h - 36), Image.Resampling.LANCZOS)
        col, row = s % 4, s // 4
        x0, y0 = col * cell_w, 28 + row * cell_h
        canvas.paste(im, (x0 + (cell_w - im.width) // 2, y0 + 22))
        draw.text((x0 + 8, y0 + 4), captions[s], fill=(245, 236, 220))
    out = os.path.join(PASS14, f"hipsit_{rh.SLUGS[TIC]}_{tag}.png")
    canvas.save(out, optimize=True)
    if os.path.getsize(out) > 400_000:
        q = canvas.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
        q.save(out, optimize=True)
    print("wrote", out, os.path.getsize(out), flush=True)
    rh._reset(arm)
    rh._hide_wall()


def sit_hips():
    """Pelvis behind the support foot. The hinge stays in the hip, not the waist.

    The capsule is not moved. The spine curl is not reset to rest. A sole that
    cannot reach the sit is reported.
    """
    os.makedirs(PASS14, exist_ok=True)
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    p10._ensure_pose(arm)
    pieces = proof._pieces(arm)
    locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
    polys = {obj.name: proof._polys(obj) for obj in pieces}
    ids, local = p10._sole_ids(arm)
    render_hipsit("before")
    doc = json.load(open(JSON_PATH))
    entry = doc["clips"][TIC]
    data = sp.load_clip(TIC)
    worlds = data["world_xyz_m"]
    cleaned = p11._cleaned(worlds, p11._spike_flags(worlds))
    keys = entry["keys"]
    caps = entry["capsule_preview_m"]
    notes = [
        "HIP SIT",
        "The pelvis sits behind the support foot along the facing. The torso hinges at the hip.",
        "Plants and landings want 8 cm. A crouch wants 12 cm, or the tracked offset when that is larger and the frame was not bridged.",
        "The capsule is not moved. The spine is not reset to rest.",
    ]
    before_110 = _copy_bones(keys[110]["bones"])
    reasons = []
    order = [110] + [i for i in range(len(keys)) if i != 110]
    copied = False
    for i in order:
        if copied and 118 <= i <= 152:
            continue
        bones = _copy_bones(keys[i]["bones"])
        cap = [float(v) for v in caps[i]]
        cap_before = cap[:]
        feet = _feet_for(i, cleaned)
        _apply(arm, bones, cap)
        _back0, hip, _fwd, _foot = _support_back(arm, ids, local, feet)
        track_feet = list(feet)
        track = None if _garbage_track(i) else _track_back(worlds[i], track_feet or ["L", "R"])
        target = _sit_target(i, hip.z, feet, track)
        back, why = _sit_frame(i, arm, bones, cap, feet, ids, local, locals_c, polys, target)
        if [round(v, 4) for v in cap] != [round(v, 4) for v in cap_before]:
            raise RuntimeError(f"capsule moved on hip-sit f={i}")
        keys[i]["bones"] = p10._round_eulers(bones)
        if back < target - 0.01:
            reasons.append(f"f={i} back={back * 100:.1f}cm target={target * 100:.1f}cm {why}")
        if i == 110 and _copy_bones(keys[118]["bones"]) == before_110:
            for j in range(118, 153):
                keys[j]["bones"] = _copy_bones(keys[110]["bones"])
            copied = True
            notes.append("HIP SIT copied the landing crouch from f=110 through f=152.")
        if i % 15 == 0:
            print(f"sit {i} back={back * 100:.1f} target={target * 100:.1f} {why}", flush=True)
    backs = []
    hinges = []
    fails = []
    for i, key in enumerate(keys):
        _apply(arm, key["bones"], caps[i])
        feet = _feet_for(i, cleaned)
        back, hip, fwd, _foot = _support_back(arm, ids, local, feet)
        _hip2, fwd, up = _rig_fwd(arm)
        ratio, hip_deg, lumbar, chest = _hinge(arm, feet, fwd, up)
        track = None if _garbage_track(i) else _track_back(worlds[i], feet or ["L", "R"])
        target = _sit_target(i, hip.z, feet, track)
        backs.append(back)
        hinges.append(ratio)
        if back < target - 0.01 or ratio < 1.5:
            fails.append(i)
        if i in STORY:
            notes.append(
                f"f={i} t={data['times_s'][i] - data['times_s'][0]:.2f}s "
                f"pelvisBack={back * 100:.1f}cm target={target * 100:.1f}cm "
                f"hinge={ratio:.2f} hip={hip_deg:.0f} lumbar={lumbar:.0f} chest={chest:.0f}"
            )
    notes.append("HIP SIT frames that miss the sit or the hinge:")
    notes.extend(reasons if reasons else ["none"])
    line = (
        f"hip-sit clips=1 loadedFrames={len(keys)} "
        f"pelvisBackMin={min(backs) * 100:.1f} cm hingeMin={min(hinges):.2f} fails={len(fails)}"
    )
    notes.append(line)
    if fails:
        notes.append("fail frames " + ",".join(str(i) for i in fails))
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "a") as f:
        f.write("\n".join(notes) + "\n")
    print(line, flush=True)
    print("wrote", JSON_PATH, flush=True)
    render_hipsit("after")
    rh._reset(arm)


def _joint_angle(a, b, c):
    u = a - b
    v = c - b
    n = u.length * v.length
    if n < 1e-8:
        return 0.0
    return math.degrees(math.acos(max(-1.0, min(1.0, u.dot(v) / n))))


def _knee_flex_deg(arm, side):
    hip = arm.matrix_world @ arm.pose.bones[f"UpperLeg_{side}"].head
    knee = arm.matrix_world @ arm.pose.bones[f"LowerLeg_{side}"].head
    ankle = arm.matrix_world @ arm.pose.bones[f"Foot_{side}"].head
    return 180.0 - _joint_angle(hip, knee, ankle)


def _shin_forward_m(arm, side, fwd):
    """Metres the knee sits ahead of the ankle along facing. Positive is a forward shin."""
    knee = arm.matrix_world @ arm.pose.bones[f"LowerLeg_{side}"].head
    ankle = arm.matrix_world @ arm.pose.bones[f"Foot_{side}"].head
    return (knee - ankle).dot(fwd)


def _rest_hip_z(arm):
    saved = [(pb, pb.rotation_euler.copy()) for pb in arm.pose.bones]
    for pb, _rot in saved:
        pb.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    z = (arm.matrix_world @ arm.pose.bones["Hips"].head).z
    for pb, rot in saved:
        pb.rotation_euler = rot
    bpy.context.view_layer.update()
    return z


def _in_jumps(i):
    for start, end in JUMPS:
        if start <= i <= end:
            return True
    return False


def _retarget_error_frames(arm, keys, caps, worlds, cleaned, ids, local):
    """Frames where the footage plants at >= 8 cm and the keyed pose does not."""
    out = []
    for i, key in enumerate(keys):
        if _garbage_track(i) or _in_jumps(i):
            continue
        _apply(arm, key["bones"], caps[i])
        feet = _feet_for(i, cleaned)
        if not feet:
            continue
        soles = [p12._sole_z(ids, local, side)[1] for side in feet]
        if min(soles) > 0.08:
            continue
        back, _hip, _fwd, _foot = _support_back(arm, ids, local, feet)
        track = _track_back(worlds[i], feet)
        if track is None or track < 0.08:
            continue
        if back < 0.08 - 0.005:
            out.append(i)
    return out


def _sit_to_eight(i, arm, bones, cap, feet, ids, local, locals_c, polys):
    """Creep the support thigh until the pelvis is 8 cm back. Stop at the spine.

    The target is the plant minimum, not the full tracked offset. A step is
    kept when the sole stays on the floor and the thigh does not enter the
    torso or the other shin.
    """
    target = 0.08
    start = _copy_bones(bones)
    accepted = _copy_bones(bones)
    _apply(arm, bones, cap)
    back, _hip, _fwd, _foot = _support_back(arm, ids, local, feet)
    if back >= target - 0.005:
        return back, "met"
    packs = p13._torso_packs(locals_c, polys)
    plate0 = max(p13._leg_depth("L", locals_c, polys, packs), p13._leg_depth("R", locals_c, polys, packs))
    cross0 = max(
        p13._pair_depth("Mesh_UpperLeg_L", "Mesh_UpperLeg_R", locals_c, polys),
        p13._pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
    )
    why = "met"
    lock_left = i == 34
    for _step in range(10):
        _apply(arm, bones, cap)
        back, _hip, _fwd, _foot = _support_back(arm, ids, local, feet)
        if back >= target - 0.005:
            why = "met"
            break
        cands = []
        for side in feet:
            if lock_left and side == "L":
                continue
            ul = f"UpperLeg_{side}"
            ll = f"LowerLeg_{side}"
            base_ul = bones[ul][:]
            k0 = bones[ll][0]
            for axis in range(3):
                for delta in (-6.0, 6.0, -12.0, 12.0, -18.0, 18.0):
                    if abs((base_ul[axis] + delta) - start[ul][axis]) > 36.0:
                        continue
                    bones[ul] = base_ul[:]
                    bones[ul][axis] = base_ul[axis] + delta
                    bones[ll][0] = k0
                    planted = _plant_knee(arm, bones, cap, side, ids, local, k0)
                    _apply(arm, bones, cap)
                    med = p12._sole_z(ids, local, side)[1]
                    low = p11._min_z(f"Mesh_Foot_{side}")
                    shin_low = p11._min_z(f"Mesh_LowerLeg_{side}")
                    if not planted and (low < -0.008 or med > 0.04):
                        continue
                    if low < -0.008 or med > 0.045 or shin_low < -0.004:
                        continue
                    new_back, _h, _f, _ft = _support_back(arm, ids, local, feet)
                    if new_back < back + 0.004:
                        continue
                    cands.append((new_back, _copy_bones(bones)))
            bones[ul] = base_ul
            bones[ll][0] = k0
        if not cands:
            why = "leg cannot put the sole far enough forward without leaving the floor"
            break
        cands.sort(key=lambda c: -c[0])
        took = False
        for _new_back, pose in cands[:6]:
            for name, vals in pose.items():
                bones[name] = vals
            _apply(arm, bones, cap)
            packs = p13._torso_packs(locals_c, polys)
            plate = max(p13._leg_depth("L", locals_c, polys, packs), p13._leg_depth("R", locals_c, polys, packs))
            cross = max(
                p13._pair_depth("Mesh_UpperLeg_L", "Mesh_UpperLeg_R", locals_c, polys),
                p13._pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
            )
            if plate > 0.005 and plate > plate0 + 0.001:
                continue
            if cross > 0.005 and cross > cross0 + 0.001:
                continue
            took = True
            back = _new_back
            break
        if not took:
            why = "the thigh meets the spine before the pelvis is far enough back"
            break
        accepted = _copy_bones(bones)
    else:
        why = "met" if back >= target - 0.005 else "stopped short of 8 cm"
    if why != "met":
        for name, vals in accepted.items():
            bones[name] = vals
        _apply(arm, bones, cap)
        back, _hip, _fwd, _foot = _support_back(arm, ids, local, feet)
    if back >= target - 0.005:
        why = "met"
    return back, why


def fix_retarget():
    """Sit footage-plants to 8 cm. Leave frames the footage does not plant."""
    os.makedirs(PASS14, exist_ok=True)
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    p10._ensure_pose(arm)
    pieces = proof._pieces(arm)
    locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
    polys = {obj.name: proof._polys(obj) for obj in pieces}
    ids, local = p10._sole_ids(arm)
    doc = json.load(open(JSON_PATH))
    entry = doc["clips"][TIC]
    data = sp.load_clip(TIC)
    worlds = data["world_xyz_m"]
    cleaned = p11._cleaned(worlds, p11._spike_flags(worlds))
    keys = entry["keys"]
    caps = entry["capsule_preview_m"]
    todo = _retarget_error_frames(arm, keys, caps, worlds, cleaned, ids, local)
    notes = [
        "RETARGET FIX",
        "Only frames the footage plants (tracked pelvisBack >= 8 cm, sole down, not a bridged crash).",
        "Target is 8 cm behind the support foot. The tracked offset above 8 cm is kept as reference, not forced.",
        "Frame 34 is not in this pass: the footage has the pelvis in front of the trailing foot.",
    ]
    fixed = []
    short = []
    for i in todo:
        bones = _copy_bones(keys[i]["bones"])
        cap = [float(v) for v in caps[i]]
        before_cap = cap[:]
        feet = _feet_for(i, cleaned)
        _apply(arm, bones, cap)
        back0, _h, _f, _ft = _support_back(arm, ids, local, feet)
        back, why = _sit_to_eight(i, arm, bones, cap, feet, ids, local, locals_c, polys)
        if [round(v, 4) for v in cap] != [round(v, 4) for v in before_cap]:
            raise RuntimeError(f"capsule moved on retarget fix f={i}")
        keys[i]["bones"] = p10._round_eulers(bones)
        line = f"f={i} {back0 * 100:.1f}cm -> {back * 100:.1f}cm {why}"
        notes.append(line)
        print(line, flush=True)
        if back >= 0.08 - 0.005:
            fixed.append(i)
        else:
            short.append(i)
        if i == 110:
            for j in range(118, 153):
                keys[j]["bones"] = _copy_bones(keys[110]["bones"])
            notes.append("Copied the landing crouch from f=110 through f=152.")
    notes.append(f"retarget-fix fixed={len(fixed)} stillShort={len(short)} fixedFrames={','.join(str(i) for i in fixed) or 'none'}")
    notes.append(f"retarget-limit frames={','.join(str(i) for i in short) or 'none'}")
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "a") as f:
        f.write("\n".join(notes) + "\n")
    print(notes[-2], flush=True)
    print("wrote", JSON_PATH, flush=True)


def label_plants():
    """Say which loaded frames are plants, and which the footage itself is not.

    A plant the footage sits behind the support foot (tracked pelvisBack >= 8 cm)
    is a retarget error when the keyed pose is still under 8 cm. A frame the
    footage puts the pelvis in front of the support foot, or a bridged crash,
    jump, or heap, is labeled not-a-plant / reference-only. Those stay honest.
    """
    os.makedirs(PASS14, exist_ok=True)
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    p10._ensure_pose(arm)
    ids, local = p10._sole_ids(arm)
    doc = json.load(open(JSON_PATH))
    entry = doc["clips"][TIC]
    data = sp.load_clip(TIC)
    worlds = data["world_xyz_m"]
    cleaned = p11._cleaned(worlds, p11._spike_flags(worlds))
    keys = entry["keys"]
    caps = entry["capsule_preview_m"]
    rest_z = _rest_hip_z(arm)
    lines = [
        "PLANT LABELS after the 8 cm retarget fix",
        "Compared the keyed pelvisBack with the tracked footage on every loaded frame.",
        "Footage plant: tracked pelvisBack >= 8 cm, sole down, not a bridged crash or jump.",
        "plant-ok: footage plant and the keyed pelvis is at least 8 cm behind the support foot.",
        "retarget-limit / reference-only: the footage plants, and the leg cannot reach 8 cm without the thigh entering the spine or the sole leaving the floor. Do not copy these as plants.",
        "not-a-plant / reference-only: the footage itself is under 8 cm, or the frame is airborne, a wall drive, a crash, or a heap. Do not copy these as plants.",
        "f=34 wall kick is not-a-plant / reference-only. Tracked pelvisBack is -14.7 cm (pelvis in front of the trailing foot). The keyed pose keeps that drive. It is not a heel-sit plant.",
        "f=76 is not-a-plant / reference-only. Tracked pelvisBack is -13.6 cm. The body is ahead of the foot in the footage.",
        "Frames 118-152 are the landing crouch copied from f=110. The track there is a prone heap and is not a plant.",
        f"Standing hip height {rest_z * 100:.1f} cm. Pelvis drop is that height minus the posed hip.",
        "Tightened plant: knee flex >= 25 deg, pelvis drop >= 8 cm, shin forward (knee ahead of the ankle).",
        "Tightened landing: knee flex >= 45 deg, pelvis drop >= 20 cm, shin forward.",
    ]
    counts = {"footage-plant": 0, "retarget-limit": 0, "not-a-plant": 0, "plant-ok": 0, "landing-copy": 0}
    backs = []
    hinges = []
    knee_miss = []
    drop_miss = []
    shin_miss = []
    for i, key in enumerate(keys):
        _apply(arm, key["bones"], caps[i])
        feet = _feet_for(i, cleaned)
        back, hip, fwd, _foot = _support_back(arm, ids, local, feet)
        garbage = _garbage_track(i) or _in_jumps(i)
        track = None if garbage else _track_back(worlds[i], feet or ["L", "R"])
        soles = []
        for side in feet or ["L", "R"]:
            soles.append(p12._sole_z(ids, local, side)[1])
        airborne = min(soles) > 0.08 if soles else True
        landing = i == 110 or 118 <= i <= 152
        knee = min(_knee_flex_deg(arm, side) for side in (feet or ["L", "R"]))
        shin = min(_shin_forward_m(arm, side, fwd) for side in (feet or ["L", "R"]))
        drop = rest_z - hip.z
        _hip2, _fwd2, up = _rig_fwd(arm)
        ratio, _hip_deg, _lumbar, _chest = _hinge(arm, feet, fwd, up)
        backs.append(back)
        hinges.append(ratio)
        if 118 <= i <= 152:
            kind = "landing-copy of f=110"
            counts["landing-copy"] += 1
        elif garbage or airborne or track is None or track < 0.08:
            kind = "not-a-plant / reference-only"
            counts["not-a-plant"] += 1
        elif back < 0.08 - 0.005:
            kind = "retarget-limit / reference-only"
            counts["retarget-limit"] += 1
            counts["footage-plant"] += 1
        else:
            kind = "plant-ok"
            counts["plant-ok"] += 1
            counts["footage-plant"] += 1
        if kind in ("plant-ok", "landing-copy of f=110", "retarget-limit / reference-only"):
            need_knee = 45.0 if landing else 25.0
            need_drop = 0.20 if landing else 0.08
            if knee < need_knee:
                knee_miss.append(i)
            if drop < need_drop:
                drop_miss.append(i)
            if shin <= 0.0:
                shin_miss.append(i)
        if i in STORY or kind == "retarget-limit / reference-only":
            track_cm = "n/a" if track is None else f"{track * 100:.1f}cm"
            lines.append(
                f"f={i} {kind} keyed={back * 100:.1f}cm track={track_cm} "
                f"knee={knee:.0f} drop={drop * 100:.1f}cm shin={shin * 100:.1f}cm "
                f"feet={''.join(feet) or '-'} landing={int(landing)} air={int(airborne)} garbage={int(garbage)}"
            )
    lines.append(
        "plant-labels "
        f"frames={len(keys)} footagePlants={counts['footage-plant']} "
        f"plantOk={counts['plant-ok']} retargetLimit={counts['retarget-limit']} "
        f"landingCopy={counts['landing-copy']} notAPlant={counts['not-a-plant']}"
    )
    lines.append(
        f"hip-sit clips=1 loadedFrames={len(keys)} "
        f"pelvisBackMin={min(backs) * 100:.1f} cm hingeMin={min(hinges):.2f} "
        f"fails={counts['retarget-limit'] + counts['not-a-plant']}"
    )
    lines.append(
        "tightened "
        f"kneeMiss={len(knee_miss)} dropMiss={len(drop_miss)} shinMiss={len(shin_miss)} "
        f"kneeFrames={','.join(str(i) for i in knee_miss) or 'none'} "
        f"dropFrames={','.join(str(i) for i in drop_miss) or 'none'} "
        f"shinFrames={','.join(str(i) for i in shin_miss) or 'none'}"
    )
    text = "\n".join(lines) + "\n"
    with open(ERROR_PATH, "a") as f:
        f.write(text)
    label_path = os.path.join(PASS14, "plant_labels_pass14.txt")
    with open(label_path, "w") as f:
        f.write(text)
    print(text, flush=True)
    print("wrote", label_path, flush=True)


if __name__ == "__main__":
    stage = os.environ.get("PASS14_STAGE", "pose")
    if stage == "pose":
        solve()
    elif stage == "repair":
        repair()
    elif stage == "stills":
        render_strips()
    elif stage == "hipsit":
        sit_hips()
    elif stage == "hipstill":
        render_hipsit(os.environ.get("HIP_TAG", "before"))
    elif stage == "label":
        label_plants()
    elif stage == "fix":
        fix_retarget()
    else:
        solve()
        repair()
        render_strips()
