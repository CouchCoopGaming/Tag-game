#!/usr/bin/env python3
"""Pass 12: plant soles, match stance hip height, open the deep crouch, split wall-run f45.

The game still has no root motion. Root and the armature stay at the origin.
capsule_preview_m is the preview path only. The rig is not edited.

solve() starts from the pass-11 keys. Do not run it twice on the written file.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Euler, Vector

import noclip_proof as proof
import pass10_pose as p10
import pass11_pose as p11
import render_hier_pass5 as p5
import render_hier_v080_stills as rh
import storror_pose as sp

PASS12 = os.path.join(rh.OUT, "pass12")
ERROR_PATH = os.path.join(PASS12, "pose_error_pass12.txt")
JSON_PATH = p10.JSON_PATH
LINE = 0.03
# Standing hip. A scaled stick taller than the leg cannot be matched by floating.
HIP_CAP = 0.92
KNEE_MIN = 20.0


def _contacts(frame):
    """Feet and hands within 3 cm of the cleaned ground line."""
    ys = [frame[j][1] for j in range(11, 33)]
    base = max(ys)
    feet, hands = [], []
    clear, hand_clear = {}, {}
    for side, idxs in p11.FEET.items():
        clear[side] = min(base - frame[j][1] for j in idxs)
        if clear[side] <= LINE:
            feet.append(side)
    for side, idxs in p11.HANDS.items():
        hand_clear[side] = min(base - frame[j][1] for j in idxs)
        if hand_clear[side] <= LINE:
            hands.append(side)
    return feet, hands, clear, hand_clear


def _hip_above_heel(frame, feet):
    """Metres from the hip centre down to the contact heel or ankle.

    A toe that sticks past the heel does not raise the target. That is the
    59 cm reading on tic-tac 2.54 s, not the toe-line 64 cm.
    """
    hip_y = 0.5 * (frame[23][1] + frame[24][1])
    ground = None
    for side in feet:
        idxs = (27, 29) if side == "L" else (28, 30)
        y = max(frame[j][1] for j in idxs)
        ground = y if ground is None else max(ground, y)
    if ground is None:
        ground = max(frame[j][1] for j in range(11, 33))
    return ground - hip_y


def _sole_z(ids, local, side):
    obj = bpy.data.objects[f"Mesh_Foot_{side}"]
    mw = obj.matrix_world
    zs = [(mw @ local[side][i]).z for i in ids[side]]
    zs.sort()
    return zs[0], zs[len(zs) // 2], zs[-1]


def _sync(arm, cap):
    cap[0] = arm.location.x
    cap[1] = arm.location.y
    cap[2] = arm.location.z


def _foot_rank(ids, local, side):
    lo, med, hi = _sole_z(ids, local, side)
    foot_min = p11._min_z(f"Mesh_Foot_{side}")
    return med, foot_min, (round(med - lo, 4), round(max(0.0, med - foot_min), 4))


def _level_foot(arm, bones, cap, side, ids, local, span=80, step=10):
    """Foot pitch, then a little yaw, so the sole median meets the shoe bottom."""
    name = f"Foot_{side}"
    p11._apply(arm, bones, cap)
    base = bones[name][0]
    med, foot_min, rank0 = _foot_rank(ids, local, side)
    best = (rank0 + (0,), 0.0)
    if rank0[0] <= 0.012 and rank0[1] <= 0.012:
        return 0.0
    for dx in range(-span, span + 1, step):
        bones[name][0] = base + dx
        p11._apply(arm, bones, cap)
        med, foot_min, rank = _foot_rank(ids, local, side)
        key = rank + (abs(dx),)
        if key < best[0]:
            best = (key, float(dx))
    dx = best[1]
    fine_step = 2 if step >= 10 else 2
    for fine in range(int(dx) - 8, int(dx) + 9, fine_step):
        bones[name][0] = base + fine
        p11._apply(arm, bones, cap)
        med, foot_min, rank = _foot_rank(ids, local, side)
        key = rank + (abs(fine),)
        if key < best[0]:
            best = (key, float(fine))
    bones[name][0] = base + best[1]
    p11._apply(arm, bones, cap)
    med, foot_min, _rank = _foot_rank(ids, local, side)
    if med - foot_min > 0.012:
        bz = bones[name][2]
        best_z = (med - foot_min, 0.0)
        for dz in range(-50, 51, 10):
            bones[name][2] = bz + dz
            p11._apply(arm, bones, cap)
            med, foot_min, _rank = _foot_rank(ids, local, side)
            if (med - foot_min, abs(dz)) < best_z:
                best_z = (med - foot_min, float(dz))
        bones[name][2] = bz + best_z[1]
        p11._apply(arm, bones, cap)
    return best[1]


def _pin_soles(arm, ids, local, sides):
    """Drop so the higher contact sole sits near the floor, without burying a mesh."""
    meds = [_sole_z(ids, local, s)[1] for s in sides]
    target = max(meds)
    low, where = p10._lowest_mesh()
    delta = 0.005 - target
    if low + delta < -0.003:
        delta = -0.003 - low
    if abs(delta) > 0.0004:
        arm.location.z += delta
        bpy.context.view_layer.update()
    return delta, where


def _raise_foot(arm, bones, cap, side, target_z):
    """Lift a non-contact foot so it cannot block the sole pin."""
    ul, ll = f"UpperLeg_{side}", f"LowerLeg_{side}"
    ul0, ll0 = bones[ul][0], bones[ll][0]
    for _step in range(12):
        p11._apply(arm, bones, cap)
        z = p11._min_z(f"Mesh_Foot_{side}")
        if z >= target_z - 0.004:
            return
        kept = False
        for name, comp, delta, limit in (
            (ul, 0, -8.0, ul0 - 80.0),
            (ll, 0, 8.0, ll0 + 90.0),
        ):
            old = bones[name][comp]
            nxt = old + delta
            if (delta < 0.0 and nxt < limit) or (delta > 0.0 and nxt > limit):
                continue
            bones[name][comp] = nxt
            p11._apply(arm, bones, cap)
            if p11._min_z(f"Mesh_Foot_{side}") > z + 0.008:
                kept = True
                break
            bones[name][comp] = old
        if not kept:
            return


def _sole_med(ids, local, feet):
    return max(_sole_z(ids, local, s)[1] for s in feet)


def _settle(arm, bones, cap, feet, ids, local, local_level=False):
    for side in feet:
        if local_level:
            _level_foot(arm, bones, cap, side, ids, local, span=24, step=4)
        else:
            _level_foot(arm, bones, cap, side, ids, local)
    _pin_soles(arm, ids, local, feet)
    _sync(arm, cap)
    return p10._hip_z(arm), _sole_med(ids, local, feet)


def _lower_sole(arm, bones, cap, side, ids, local):
    """Bring a high contact sole down beside the other one. Knee stays bent."""
    ll, ul = f"LowerLeg_{side}", f"UpperLeg_{side}"
    foot = f"Foot_{side}"
    saved = (bones[ll][0], bones[ul][0], bones[ul][1], bones[ul][2], bones[foot][:])
    p11._apply(arm, bones, cap)
    before = _sole_z(ids, local, side)[1]
    best = before
    best_pose = None
    cands = []
    if saved[0] - 8.0 >= KNEE_MIN:
        cands.append((ll, 0, saved[0] - 8.0))
    cands.append((ul, 0, saved[1] + 8.0))
    cands.append((ul, 0, saved[1] - 8.0))
    cands.append((ul, 1, saved[2] - 8.0))
    cands.append((ul, 2, saved[3] + 8.0))
    cands.append((ul, 2, saved[3] - 8.0))
    for name, comp, val in cands:
        bones[ll][0], bones[ul][0], bones[ul][1], bones[ul][2] = saved[:4]
        bones[foot] = saved[4][:]
        bones[name][comp] = val
        _level_foot(arm, bones, cap, side, ids, local, span=24, step=8)
        med = _sole_z(ids, local, side)[1]
        if med < best - 0.006:
            best = med
            best_pose = (
                bones[ll][0], bones[ul][0], bones[ul][1], bones[ul][2], bones[foot][:],
            )
    if best_pose is None:
        bones[ll][0], bones[ul][0], bones[ul][1], bones[ul][2] = saved[:4]
        bones[foot] = saved[4][:]
        return False
    bones[ll][0], bones[ul][0], bones[ul][1], bones[ul][2] = best_pose[:4]
    bones[foot] = best_pose[4]
    return True


def _finish_plant(arm, bones, cap, feet, hands, hand_clear, ids, local):
    """Level the contact soles and clear whatever mesh is blocking the pin."""
    for _round in range(8):
        if len(feet) > 1:
            p11._apply(arm, bones, cap)
            meds = {s: _sole_z(ids, local, s)[1] for s in feet}
            high = max(meds, key=meds.get)
            if meds[high] > min(meds.values()) + 0.012:
                _lower_sole(arm, bones, cap, high, ids, local)
        hip, med = _settle(arm, bones, cap, feet, ids, local, local_level=False)
        if med <= 0.011:
            return hip, med
        _low, where = p10._lowest_mesh()
        if "Hand" in where or "Arm" in where:
            side = "L" if where.endswith("_L") else "R"
            if side in hands and hand_clear.get(side, 9.0) <= LINE:
                high = max(feet, key=lambda s: _sole_z(ids, local, s)[1])
                before = _sole_z(ids, local, high)[1]
                ll = f"LowerLeg_{high}"
                ul = f"UpperLeg_{high}"
                if bones[ll][0] - 8.0 >= KNEE_MIN:
                    bones[ll][0] -= 8.0
                    _level_foot(arm, bones, cap, high, ids, local)
                    if _sole_z(ids, local, high)[1] > before - 0.004:
                        bones[ll][0] += 8.0
                        bones[ul][0] += 8.0
                else:
                    bones[ul][0] += 8.0
            else:
                ua = f"UpperArm_{side}"
                hand = f"Mesh_Hand_{side}"
                z0 = p11._min_z(hand)
                bones[ua][0] += 12.0
                p11._apply(arm, bones, cap)
                up = p11._min_z(hand)
                bones[ua][0] -= 24.0
                p11._apply(arm, bones, cap)
                down = p11._min_z(hand)
                bones[ua][0] += 12.0
                if up >= down and up > z0 + 0.006:
                    bones[ua][0] += 12.0
                elif down > z0 + 0.006:
                    bones[ua][0] -= 12.0
                else:
                    break
        elif "Foot" in where:
            side = "L" if where.endswith("_L") else "R"
            if side in feet:
                break
            bones[f"UpperLeg_{side}"][0] -= 8.0
        else:
            break
    return _settle(arm, bones, cap, feet, ids, local, local_level=False)


def _try_delta(arm, bones, cap, feet, ids, local, names, comp, delta, lo, hi):
    """Add delta to one channel on every contact leg. Keep it only if the hip gets closer."""
    old = {s: bones[f"{names}_{s}"][comp] for s in feet}
    moved = False
    for side, cur in old.items():
        nxt = cur + delta
        if nxt < lo or nxt > hi[side]:
            continue
        bones[f"{names}_{side}"][comp] = nxt
        moved = True
    if not moved:
        return None
    hip, med = _settle(arm, bones, cap, feet, ids, local, local_level=True)
    return hip, med, old


def _restore(bones, names, comp, old):
    for side, cur in old.items():
        bones[f"{names}_{side}"][comp] = cur


def _match_hip(arm, bones, cap, feet, target, ids, local, knee0):
    """Bend the contact knees and hips, then re-pin. The knee stays at least 20°."""
    hip, med = _settle(arm, bones, cap, feet, ids, local, local_level=True)
    for _step in range(6):
        err = hip - target
        if abs(err) <= 0.05 and med <= 0.011:
            break
        delta = -12.0 if err < 0.0 else 12.0
        hi = {s: knee0[s] + 80.0 for s in feet}
        trial = _try_delta(arm, bones, cap, feet, ids, local, "LowerLeg", 0, delta, KNEE_MIN, hi)
        if trial is not None and trial[1] <= 0.012 and abs(trial[0] - target) < abs(err) - 0.004:
            hip, med = trial[0], trial[1]
            continue
        if trial is not None:
            _restore(bones, "LowerLeg", 0, trial[2])
        step_h = -12.0 if err > 0.0 else 12.0
        # Both signs: on a folded leg, +X can lower the hip and -X can raise it.
        improved = False
        for step in (step_h, -step_h):
            old = {s: bones[f"UpperLeg_{s}"][0] for s in feet}
            for side in feet:
                bones[f"UpperLeg_{side}"][0] = old[side] + step
            trial_h, trial_m = _settle(arm, bones, cap, feet, ids, local, local_level=True)
            if trial_m <= 0.012 and abs(trial_h - target) < abs(err) - 0.004:
                hip, med = trial_h, trial_m
                improved = True
                break
            for side, cur in old.items():
                bones[f"UpperLeg_{side}"][0] = cur
        if improved:
            continue
        break
    if hip < target - 0.05:
        for _step in range(6):
            old = {s: bones[f"LowerLeg_{s}"][0] for s in feet}
            moved = False
            for side, cur in old.items():
                nxt = cur - 8.0
                if nxt < KNEE_MIN:
                    continue
                bones[f"LowerLeg_{side}"][0] = nxt
                moved = True
            if not moved:
                break
            trial_h, trial_m = _settle(arm, bones, cap, feet, ids, local, local_level=False)
            if trial_m <= 0.012 and trial_h > hip + 0.004:
                hip = trial_h
                if hip >= target - 0.05:
                    break
            else:
                for side, cur in old.items():
                    bones[f"LowerLeg_{side}"][0] = cur
                break
        for side in feet:
            name = f"UpperLeg_{side}"
            y0 = bones[name][1]
            best = None
            for yd in range(-48, 17, 8):
                bones[name][1] = y0 + yd
                trial_h, trial_m = _settle(arm, bones, cap, feet, ids, local, local_level=False)
                if trial_m <= 0.012 and (best is None or abs(trial_h - target) < best[0]):
                    best = (abs(trial_h - target), yd, trial_h)
            if best is not None and best[0] < abs(hip - target) - 0.004:
                bones[name][1] = y0 + best[1]
                hip = best[2]
            else:
                bones[name][1] = y0
            if hip >= target - 0.05:
                break
        if hip < target - 0.05:
            for side in feet:
                name = f"UpperLeg_{side}"
                y0 = bones[name][1]
                best = None
                for yd in (-8.0, -4.0, 4.0, 8.0):
                    bones[name][1] = y0 + yd
                    trial_h, trial_m = _settle(arm, bones, cap, feet, ids, local, local_level=False)
                    if trial_m <= 0.012 and (best is None or abs(trial_h - target) < best[0]):
                        best = (abs(trial_h - target), yd, trial_h)
                if best is not None and best[0] < abs(hip - target) - 0.002:
                    bones[name][1] = y0 + best[1]
                    hip = best[2]
                else:
                    bones[name][1] = y0
        if hip < target - 0.05:
            x0 = {s: bones[f"UpperLeg_{s}"][0] for s in feet}
            best = None
            for xd in (-32.0, -16.0, 16.0, 32.0):
                for side in feet:
                    bones[f"UpperLeg_{side}"][0] = x0[side] + xd
                trial_h, trial_m = _settle(arm, bones, cap, feet, ids, local, local_level=False)
                if trial_m <= 0.012 and (best is None or abs(trial_h - target) < best[0]):
                    best = (abs(trial_h - target), xd, trial_h)
            if best is not None and best[0] < abs(hip - target) - 0.004:
                for side in feet:
                    bones[f"UpperLeg_{side}"][0] = x0[side] + best[1]
                hip = best[2]
            else:
                for side, cur in x0.items():
                    bones[f"UpperLeg_{side}"][0] = cur
        if hip < target - 0.05:
            for comp, deltas in ((1, (-40.0, -20.0, 20.0, 40.0)), (2, (-30.0, -15.0, 15.0, 30.0))):
                if hip >= target - 0.05:
                    break
                base = {s: bones[f"UpperLeg_{s}"][comp] for s in feet}
                best = None
                for delta in deltas:
                    for side in feet:
                        bones[f"UpperLeg_{side}"][comp] = base[side] + delta
                    trial_h, trial_m = _settle(arm, bones, cap, feet, ids, local, local_level=False)
                    if trial_m <= 0.012 and (best is None or abs(trial_h - target) < best[0]):
                        best = (abs(trial_h - target), delta, trial_h)
                if best is not None and best[0] < abs(hip - target) - 0.004:
                    for side in feet:
                        bones[f"UpperLeg_{side}"][comp] = base[side] + best[1]
                    hip = best[2]
                else:
                    for side, cur in base.items():
                        bones[f"UpperLeg_{side}"][comp] = cur
    hip, med = _settle(arm, bones, cap, feet, ids, local, local_level=False)
    return hip, med


def _plant_stance(arm, bones, cap, feet, hands, hand_clear, ids, local, target, knee0):
    """Plant every stick foot that is on the ground line, then match the hip."""
    p11._apply(arm, bones, cap)
    for _raise in range(3):
        med = _sole_med(ids, local, feet)
        raised = False
        for side in ("L", "R"):
            if side in feet:
                continue
            if p11._min_z(f"Mesh_Foot_{side}") < med + 0.03:
                _raise_foot(arm, bones, cap, side, med + 0.05)
                raised = True
        if not raised:
            break
    _finish_plant(arm, bones, cap, feet, hands, hand_clear, ids, local)
    hip, med = _match_hip(arm, bones, cap, feet, target, ids, local, knee0)
    if hip > target + 0.05 and hands:
        for _step in range(3):
            if hip <= target + 0.05:
                break
            old_k = {s: bones[f"LowerLeg_{s}"][0] for s in feet}
            old_a = {s: bones[f"UpperArm_{s}"][0] for s in ("L", "R")}
            moved = False
            for side in feet:
                nxt = min(knee0[side] + 80.0, old_k[side] + 8.0)
                if nxt > old_k[side] + 0.5:
                    bones[f"LowerLeg_{side}"][0] = nxt
                    moved = True
            if not moved:
                break
            best = None
            for arm_d in (0.0, 8.0, -8.0, 16.0, -16.0):
                for side in ("L", "R"):
                    bones[f"UpperArm_{side}"][0] = old_a[side] + arm_d
                trial_h, trial_m = _settle(arm, bones, cap, feet, ids, local, local_level=False)
                if trial_m <= 0.012 and (best is None or abs(trial_h - target) < best[0]):
                    best = (abs(trial_h - target), arm_d, trial_h, trial_m)
            if best is not None and best[0] < abs(hip - target) - 0.003:
                for side in ("L", "R"):
                    bones[f"UpperArm_{side}"][0] = old_a[side] + best[1]
                hip, med = _settle(arm, bones, cap, feet, ids, local, local_level=False)
            else:
                for side, cur in old_k.items():
                    bones[f"LowerLeg_{side}"][0] = cur
                for side, cur in old_a.items():
                    bones[f"UpperArm_{side}"][0] = cur
                hip, med = _settle(arm, bones, cap, feet, ids, local, local_level=False)
                break
    return hip, med


def _pair_depth(arm, a_name, b_name, locals_c, polys_c):
    oa = bpy.data.objects[a_name]
    ob = bpy.data.objects[b_name]
    va = proof._world(locals_c[a_name], oa.matrix_world)
    vb = proof._world(locals_c[b_name], ob.matrix_world)
    ba = proof._bvh(va, polys_c[a_name])
    bb = proof._bvh(vb, polys_c[b_name])
    joint = proof._shared_joint(arm, oa.parent_bone, ob.parent_bone)
    da = proof._inside_depths(va, polys_c[a_name], ba, bb, joint)
    db = proof._inside_depths(vb, polys_c[b_name], bb, ba, joint)
    depth = 0.0
    if da:
        depth = max(depth, max(da.values()))
    if db:
        depth = max(depth, max(db.values()))
    return depth


def _chest_depth(arm, locals_c, polys_c):
    worst = 0.0
    where = ""
    for leg in ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"):
        d = _pair_depth(arm, "Mesh_Chest", leg, locals_c, polys_c)
        if d > worst:
            worst = d
            where = leg
    return worst, where


def _hand_foot_gap():
    hand_z = min(p11._min_z("Mesh_Hand_L"), p11._min_z("Mesh_Hand_R"))
    foot_z = min(p11._min_z("Mesh_Foot_L"), p11._min_z("Mesh_Foot_R"))
    return hand_z - foot_z


def _open_chest(arm, bones, cap, track, target_m, locals_c, polys_c):
    """Open the chest out of the thighs without giving the floor pin to the shoes.

    A Hips-bone rotation cannot separate the chest from the thighs: both hang
    off the pelvis. The relative pitch lives on the spine, and the thighs
    abduct in their own local Z. Those channels stay within 20° of the track.
    Hip flexion stays deep enough that the palms remain below the feet.
    """
    base_arm = {s: bones[f"UpperArm_{s}"][0] for s in ("L", "R")}
    # Measured palm-leading start: spine and abduction inside the window,
    # hip flexion about 55° past the track so the hands still lead.
    bones["UpperLeg_L"] = [
        track["UpperLeg_L"][0] - 55.0,
        track["UpperLeg_L"][1],
        track["UpperLeg_L"][2] + 5.0,
    ]
    bones["UpperLeg_R"] = [
        track["UpperLeg_R"][0] - 60.0,
        track["UpperLeg_R"][1] - 5.0,
        track["UpperLeg_R"][2],
    ]
    bones["Spine"] = [
        track["Spine"][0] - 15.0,
        track["Spine"][1],
        track["Spine"][2] + 20.0,
    ]
    bones["Chest"] = [-20.0, -10.0, 0.0]
    for side in ("L", "R"):
        bones[f"UpperArm_{side}"][0] = base_arm[side] + 10.0

    def metrics():
        p11._apply(arm, bones, cap)
        depth, where = _chest_depth(arm, locals_c, polys_c)
        return depth, where, _hand_foot_gap()

    depth, where, gap = metrics()
    chans = [
        ("UpperLeg_L", 2, track["UpperLeg_L"][2]),
        ("UpperLeg_R", 2, track["UpperLeg_R"][2]),
        ("UpperLeg_L", 1, track["UpperLeg_L"][1]),
        ("UpperLeg_R", 1, track["UpperLeg_R"][1]),
        ("Spine", 0, track["Spine"][0]),
        ("Spine", 2, track["Spine"][2]),
        ("Spine", 1, track["Spine"][1]),
        ("Chest", 0, 0.0),
        ("Chest", 1, 0.0),
        ("Chest", 2, 0.0),
    ]
    for _pass in range(2):
        for name, comp, origin in chans:
            best_v, best_d = bones[name][comp], depth
            for off in range(-20, 21, 4):
                bones[name][comp] = origin + off
                trial_d, _w, trial_g = metrics()
                if trial_g > -0.01:
                    continue
                if trial_d < best_d - 0.0002:
                    best_d, best_v = trial_d, origin + off
            bones[name][comp] = best_v
            depth = best_d
    for name, origin in (
        ("UpperLeg_L", track["UpperLeg_L"][0]),
        ("UpperLeg_R", track["UpperLeg_R"][0]),
    ):
        best_v, best_d = bones[name][0], depth
        off = -70.0
        while off <= 20.0:
            bones[name][0] = origin + off
            trial_d, _w, trial_g = metrics()
            if trial_g <= -0.01 and trial_d < best_d - 0.0002:
                best_d, best_v = trial_d, origin + off
            off += 4.0
        bones[name][0] = best_v
        depth = best_d
    depth, where, gap = metrics()
    perr = p10._pelvis_err(arm, target_m)
    abd = {
        "L": bones["UpperLeg_L"][2] - track["UpperLeg_L"][2],
        "R": bones["UpperLeg_R"][2] - track["UpperLeg_R"][2],
    }
    spine_d = [bones["Spine"][c] - track["Spine"][c] for c in range(3)]
    return abd, spine_d, bones["Chest"][:], depth, where, perr, gap


def _wall_split(arm, bones, cap, ids, local, wall_side, target_m):
    """Keep the sole on the face and the chest outside, with the pelvis within 20°."""
    # Pass 11 added (+12, -24, +12) on top of the landmark pelvis.
    landmark = [bones["Hips"][0] - 12.0, bones["Hips"][1] + 24.0, bones["Hips"][2] - 12.0]
    spine0 = bones["Spine"][:]
    best = None

    def trial(dx, dy, dz, sx, sy, sz):
        bones["Hips"] = [landmark[0] + dx, landmark[1] + dy, landmark[2] + dz]
        bones["Spine"] = [spine0[0] + sx, spine0[1] + sy, spine0[2] + sz]
        p11._apply(arm, bones, cap)
        perr = p10._pelvis_err(arm, target_m)
        if perr > 20.05:
            return None
        foot, lead = p10._leading_foot(ids, local, wall_side)
        if wall_side == "-":
            arm.location.x += -0.003 - lead
        bpy.context.view_layer.update()
        pen, where = p11._nonfoot_pen(wall_side, f"Foot_{foot}")
        foot, lead = p10._leading_foot(ids, local, wall_side)
        gap = max(0.0, -lead) if wall_side == "-" else 0.0
        chest_pen = max(0.0, p11._max_x("Mesh_Chest") - 0.004)
        return perr, gap, pen, chest_pen, where, foot

    scales = (0.35, 0.45, 0.55, 0.65, 0.75)
    for scale in scales:
        dx, dy, dz = 12.0 * scale, -24.0 * scale, 12.0 * scale
        for sy in range(-36, 37, 12):
            for sx in (0.0, -20.0, 20.0, -40.0, 40.0):
                for sz in (0.0, -20.0, 20.0):
                    row = trial(dx, dy, dz, sx, sy, sz)
                    if row is None:
                        continue
                    perr, gap, pen, chest_pen, where, foot = row
                    clear = chest_pen <= 0.001 and pen <= 0.001 and gap <= 0.02
                    rank = (not clear, gap + chest_pen + pen, perr)
                    pose = (dx, dy, dz, sx, sy, sz)
                    if best is None or rank < best[0]:
                        best = (rank, pose, row)
    if best is None:
        bones["Hips"] = [landmark[0] + 12.0, landmark[1] - 24.0, landmark[2] + 12.0]
        bones["Spine"] = spine0
        p11._apply(arm, bones, cap)
        perr = p10._pelvis_err(arm, target_m)
        return (12.0, -24.0, 12.0), (0.0, 0.0, 0.0), (perr, 9.0, 9.0, 9.0, "", "?")
    dx, dy, dz, sx, sy, sz = best[1]
    for sy2 in range(int(sy) - 8, int(sy) + 9, 4):
        for dx2 in (dx - 2.0, dx, dx + 2.0):
            row = trial(dx2, dy, dz, sx, float(sy2), sz)
            if row is None:
                continue
            perr, gap, pen, chest_pen, where, foot = row
            clear = chest_pen <= 0.001 and pen <= 0.001 and gap <= 0.02
            rank = (not clear, gap + chest_pen + pen, perr)
            if rank < best[0]:
                best = (rank, (dx2, dy, dz, sx, float(sy2), sz), row)
    dx, dy, dz, sx, sy, sz = best[1]
    row = trial(dx, dy, dz, sx, sy, sz)
    _sync(arm, cap)
    return (dx, dy, dz), (sx, sy, sz), row


def _restore_cat_lip(arm, bones, cap, wall_top):
    """After a height change, put the palms back on the lip without entering the wall."""
    bases = {s: bones[f"UpperArm_{s}"][0] for s in ("L", "R")}
    watch = (
        "Mesh_Hand_L", "Mesh_Hand_R", "Mesh_LowerArm_L", "Mesh_LowerArm_R",
        "Mesh_Chest", "Mesh_Hips",
    )

    def score():
        p11._apply(arm, bones, cap)
        gaps = []
        for side in ("L", "R"):
            _pt, gap = rh._palm_gap(arm, side, 0.0, wall_top)
            if gap is not None:
                gaps.append(gap)
        gap = max(gaps) if gaps else 1.0
        pen = p11._slab_pen(watch, wall_top)
        return gap, pen

    moved = []
    for side in ("L", "R"):
        best_off = 0.0
        best_rank = None
        for off in range(-20, 21, 5):
            bones[f"UpperArm_{side}"][0] = bases[side] + off
            gap, pen = score()
            rank = (max(0.0, gap - 0.02) + pen * 3.0, abs(off))
            if best_rank is None or rank < best_rank:
                best_rank = rank
                best_off = off
        bones[f"UpperArm_{side}"][0] = bases[side] + best_off
        if abs(best_off) > 0.01:
            moved.append(f"{side}{best_off:+.0f}")
    gap, pen = score()
    _sync(arm, cap)
    return gap, pen, moved


def solve():
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = p10._sole_ids(arm)
    p10._ensure_pose(arm)
    doc = json.load(open(JSON_PATH))
    only = os.environ.get("PASS12_ONLY", "")
    chest_names = ("Mesh_Chest", "Mesh_UpperLeg_L", "Mesh_UpperLeg_R")
    pieces = [bpy.data.objects[n] for n in chest_names]
    locals_c = {o.name: proof._local_coords(o) for o in pieces}
    polys_c = {o.name: proof._polys(o) for o in pieces}
    lines = ["clip frame pelvis_deg hip_deg knee_deg spine_deg height_cm sole_cm"]
    notes = []
    planted = {spec["id"]: [] for spec in sp.CLIPS}
    for spec in sp.CLIPS:
        cid = spec["id"]
        if only and not cid.startswith(only):
            continue
        data = sp.load_clip(cid)
        smooth = sp.clip_curves(cid, keys=24)["raw_smooth"]
        scale = p5._leg_scale(data)
        worlds = data["world_xyz_m"]
        cleaned = p11._cleaned(worlds, p11._spike_flags(worlds))
        entry = doc["clips"][cid]
        n = len(entry["keys"])
        wall_side = entry.get("wall_side", "+")
        wall_top = entry.get("wall_top_m")
        do_wall = spec["verb"] != "softland"
        floor = not cid.startswith("10_")
        print(f"POSE {cid} {n} scale={scale:.3f}", flush=True)
        for i in range(n):
            track = p10._track(smooth, i, n)
            bones = {k: [float(a) for a in v] for k, v in entry["keys"][i]["bones"].items()}
            cap = [float(c) for c in entry["capsule_preview_m"][i]]
            z_before = cap[2]
            p11._apply(arm, bones, cap)
            feet, hands, clear, hand_clear = _contacts(cleaned[i])
            foot_c = min((clear[s] for s in feet), default=9.0)
            hand_c = min((hand_clear[s] for s in hands), default=9.0)
            palms = bool(hands) and hand_c <= foot_c + 0.01
            story = i in p11.STORY.get(cid, [])
            if floor and feet and not palms:
                knee0 = {s: bones[f"LowerLeg_{s}"][0] for s in ("L", "R")}
                above = _hip_above_heel(cleaned[i], feet)
                target = min(HIP_CAP, max(0.35, above * scale))
                hip, med = _plant_stance(
                    arm, bones, cap, feet, hands, hand_clear, ids, local, target, knee0,
                )
                if story or abs(hip - target) > 0.05 or med > 0.011:
                    bits = []
                    for side in feet:
                        kd = bones[f"LowerLeg_{side}"][0] - knee0[side]
                        hd = bones[f"UpperLeg_{side}"][0] - entry["keys"][i]["bones"][f"UpperLeg_{side}"][0]
                        yd = bones[f"UpperLeg_{side}"][1] - entry["keys"][i]["bones"][f"UpperLeg_{side}"][1]
                        if abs(kd) > 0.5 or abs(hd) > 0.5 or abs(yd) > 0.5:
                            bits.append(f"{side} knee={kd:+.0f} hip={hd:+.0f} yaw={yd:+.0f}")
                    msg = (
                        f"HIP {cid} f={i} hip_cm={hip * 100:.1f} "
                        f"target_cm={target * 100:.1f} stick_cm={above * 100:.1f} "
                        f"sole_cm={med * 100:.1f} scale={scale:.3f} {' '.join(bits)}"
                    )
                    notes.append(msg)
                    if story:
                        print(msg, flush=True)
            if cid.startswith("12_") and i == 0:
                _eul, target_m = p10.pelvis_of(worlds[i])
                abd, spine_d, chest_e, depth, where, perr, gap = _open_chest(
                    arm, bones, cap, track, target_m, locals_c, polys_c,
                )
                if palms and hands:
                    names = [f"Mesh_Hand_{s}" for s in hands]
                    p11._pin_z(arm, names)
                    _sync(arm, cap)
                    depth, where = _chest_depth(arm, locals_c, polys_c)
                msg = (
                    f"CHEST {cid} f=0 abd_L={abd['L']:+.0f} abd_R={abd['R']:+.0f} "
                    f"spine_d=({spine_d[0]:+.0f},{spine_d[1]:+.0f},{spine_d[2]:+.0f}) "
                    f"chest=({chest_e[0]:+.0f},{chest_e[1]:+.0f},{chest_e[2]:+.0f}) "
                    f"depth_cm={depth * 100:.2f} {where} pelvis_deg={perr:.2f} "
                    f"hand_gap_cm={gap * 100:.1f}"
                )
                notes.append(msg)
                print(msg, flush=True)
            if do_wall and cid.startswith("10_") and i == 45:
                _eul, target_m = p10.pelvis_of(worlds[i])
                hip_d, spine_d, row = _wall_split(
                    arm, bones, cap, ids, local, wall_side, target_m,
                )
                perr, gap, pen, chest_pen, where, foot = row
                msg = (
                    f"WALL45 {cid} f=45 hip_d=({hip_d[0]:+.0f},{hip_d[1]:+.0f},{hip_d[2]:+.0f}) "
                    f"spine_d=({spine_d[0]:+.0f},{spine_d[1]:+.0f},{spine_d[2]:+.0f}) "
                    f"sole={foot} gap_cm={gap * 100:.2f} pen_cm={pen * 100:.2f} "
                    f"chest_cm={chest_pen * 100:.2f} {where} pelvis_deg={perr:.2f}"
                )
                notes.append(msg)
                print(msg, flush=True)
            if (cid, i) == ("15_cat_leap_wall", 86) and wall_top is not None and abs(cap[2] - z_before) > 0.004:
                gap, pen, moved = _restore_cat_lip(arm, bones, cap, wall_top)
                msg = (
                    f"LIP {cid} f=86 gap_cm={gap * 100:.2f} pen_cm={pen * 100:.2f} "
                    f"pitch={','.join(moved) if moved else '0'}"
                )
                notes.append(msg)
                print(msg, flush=True)
            if do_wall:
                p10._push_wall(arm, wall_side, (), wall_top)
                _sync(arm, cap)
            lift, was, low_where = p10._ground_clamp(arm)
            _sync(arm, cap)
            if lift > 0.004 and (story or lift > 0.02):
                notes.append(
                    f"LIFT {cid} f={i} {low_where} lift_cm={lift * 100:.2f} was_cm={was * 100:.2f}"
                )
            if floor and feet and not palms:
                bits = []
                worst = 0.0
                for side in feet:
                    _lo, med, _hi = _sole_z(ids, local, side)
                    worst = max(worst, med)
                    bits.append(f"{side}={med * 100:.1f}")
                planted[cid].append((i, " ".join(bits), worst))
                if story or worst > 0.012:
                    notes.append(f"SOLE {cid} f={i} {' '.join(bits)}")
                if story and worst > 0.012:
                    print(f"SOLE {cid} f={i} {' '.join(bits)}", flush=True)
            _eul, target_m = p10.pelvis_of(worlds[i])
            perr = p10._pelvis_err(arm, target_m)
            hip_e = p10._group_err(bones, track, p10.SCORED_HIP)
            knee = p10._group_err(bones, track, p10.SCORED_KNEE)
            spine = p10._group_err(bones, track, ("Spine",))
            if floor and feet and not palms:
                above = _hip_above_heel(cleaned[i], feet)
                target = min(HIP_CAP, max(0.35, above * scale))
                h_err = (p10._hip_z(arm) - target) * 100.0
                sole_cm = max(_sole_z(ids, local, s)[1] for s in feet) * 100.0
            else:
                h_err = 0.0
                sole_cm = -1.0
            lines.append(
                f"{cid} {i} {perr:.3f} {hip_e:.3f} {knee:.3f} {spine:.3f} "
                f"{h_err:.2f} {sole_cm:.2f}"
            )
            entry["keys"][i]["bones"] = p10._round_eulers(bones)
            entry["capsule_preview_m"][i] = [round(c, 4) for c in cap]
            if i % 40 == 0:
                print(f"  {cid} {i}/{n}", flush=True)
        if floor:
            over = sum(1 for _i, _b, w in planted[cid] if w > 0.01)
            notes.append(
                f"PLANTED {cid} frames={len(planted[cid])} sole_over_1cm={over}"
            )
            print(notes[-1], flush=True)
    os.makedirs(PASS12, exist_ok=True)
    if not only:
        with open(JSON_PATH, "w") as f:
            json.dump(doc, f)
        print("wrote", JSON_PATH, flush=True)
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(lines))
        f.write("\n\n")
        f.write(
            "A stick foot within 3 cm of the cleaned ground line plants that sole. "
            "Sole cm is the median of the sole vertices. "
            "Hip target is the contact heel height times the clip leg scale, "
            "capped at 92 cm. Root motion is off. The rig is unchanged.\n"
        )
        for cid, rows in planted.items():
            if only and not cid.startswith(only):
                continue
            story = []
            for i, bits, _w in rows:
                if i in p11.STORY.get(cid, []):
                    story.append(f"f={i} {bits}")
            if story:
                f.write(f"STRIP {cid} " + " | ".join(story) + "\n")
        if notes:
            f.write("\n".join(notes) + "\n")
    print("wrote", ERROR_PATH, flush=True)
    rh._reset(arm)
    rh._hide_wall()


def _sole_caption(ids, local, feet):
    if not feet:
        return ""
    bits = []
    for side in feet:
        _lo, med, _hi = _sole_z(ids, local, side)
        bits.append(f"{side} {med * 100:.1f}cm")
    return "sole " + " ".join(bits)


def render_strips():
    from PIL import Image, ImageDraw
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = p10._sole_ids(arm)
    p10._ensure_pose(arm)
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
        if hasattr(bpy.context.scene.eevee, "use_soft_shadows"):
            bpy.context.scene.eevee.use_soft_shadows = True
    sun = bpy.data.objects.get("KeySun")
    if sun is not None and hasattr(sun.data, "use_shadow"):
        sun.data.use_shadow = True
    bpy.context.scene.render.resolution_x = 560
    bpy.context.scene.render.resolution_y = 720
    doc = json.load(open(JSON_PATH))
    os.makedirs(PASS12, exist_ok=True)
    prev = "/tmp/pass12_cells"
    os.makedirs(prev, exist_ok=True)
    labels = {
        "03_wall_drop_softland": "03 soft land",
        "04_window_drop_softland": "04 soft land",
        "10_wallrun_slanted": "10 wall run",
        "12_tictac_slanted_wall": "12 tic-tac",
        "15_cat_leap_wall": "15 cat leap",
    }
    only = os.environ.get("PASS12_ONLY", "")
    strip_lines = []
    for spec in sp.CLIPS:
        cid = spec["id"]
        if only and not cid.startswith(only):
            continue
        entry = doc["clips"][cid]
        data = sp.load_clip(cid)
        t0 = data["times_s"][0]
        worlds = data["world_xyz_m"]
        cleaned = p11._cleaned(worlds, p11._spike_flags(worlds))
        has_wall = spec["verb"] != "softland"
        wall_top = entry.get("wall_top_m")
        side = entry.get("wall_side", "+")
        frames = [i for i in p11.STORY[cid] if i < len(entry["keys"])]
        shots = []
        captions = []
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
            feet, _hands, _c, _hc = _contacts(cleaned[i])
            _foot_c = min((_c[sd] for sd in feet), default=9.0)
            _hand_c = min((_hc[sd] for sd in _hands), default=9.0)
            palms = bool(_hands) and _hand_c <= _foot_c + 0.01
            cap_txt = "" if palms or cid.startswith("10_") else _sole_caption(ids, local, feet)
            captions.append(cap_txt)
            p11._show_floor(arm)
            p11._show_shadow(p11._contact_x(arm), arm.location.y)
            if has_wall:
                rh._show_wall(0.0, wall_top, arm.location.y, 2.4, side=side)
            else:
                rh._hide_wall()
            loc, look, ortho = p11._frame_cam(arm)
            path = os.path.join(prev, f"{cid}_{s}.png")
            rh._shot(path, loc, look, ortho=ortho)
            shots.append(path)
            print(f"ref {cid} f={i} {cap_txt}", flush=True)
        cell_w, cell_h = 400, 300
        canvas = Image.new("RGB", (cell_w * 4, cell_h * 2 + 28), (48, 44, 40))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle((0, 0, 1600, 28), fill=(20, 18, 16))
        draw.text(
            (12, 8),
            labels[cid] + "   planted soles on the strip",
            fill=(245, 236, 220),
        )
        listed = []
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
            if captions[s]:
                listed.append(f"{t:.2f}s {captions[s]}")
        out = os.path.join(PASS12, f"ref_{rh.SLUGS[cid]}.png")
        canvas.save(out, optimize=True)
        if os.path.getsize(out) > 400_000:
            q = canvas.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
            q.save(out, optimize=True)
        print("wrote", out, os.path.getsize(out), flush=True)
        strip_lines.append(f"STRIP {cid} " + " | ".join(listed))
    with open(ERROR_PATH, "a") as f:
        f.write("\n".join(strip_lines) + "\n")
    rh._reset(arm)
    rh._hide_wall()


if __name__ == "__main__":
    stage = os.environ.get("PASS12_STAGE", "pose")
    if stage == "pose":
        solve()
    elif stage == "stills":
        render_strips()
    else:
        solve()
        render_strips()
