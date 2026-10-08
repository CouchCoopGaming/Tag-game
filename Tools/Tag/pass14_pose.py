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


if __name__ == "__main__":
    stage = os.environ.get("PASS14_STAGE", "pose")
    if stage == "pose":
        solve()
    elif stage == "stills":
        render_strips()
    else:
        solve()
        render_strips()
