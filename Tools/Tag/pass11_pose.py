#!/usr/bin/env python3
"""Pass 11: stick ground line, matching side view, fitted sticks.

The game still has no root motion. Root and the armature stay at the origin.
capsule_preview_m is the preview path only. The rig is not edited.

The ground contact is whichever sole or palm sits on the reference stick's
ground line. A toe or fingertip that dives below its own limb is interpolated
and does not set that line. The shoe-bottom plant is not used.

solve() starts from the pass-10 keys. The shipped keyed_clips.json already
includes this pass, including the cat frame 86 elbow flare. Do not run it again
on that file.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Vector

import noclip_proof as proof
import pass10_pose as p10
import render_hier_pass5 as p5
import render_hier_v080_stills as rh
import storror_pose as sp

PASS11 = os.path.join(rh.OUT, "pass11")
ERROR_PATH = os.path.join(PASS11, "pose_error_pass11.txt")
JSON_PATH = p10.JSON_PATH

STORY = {
    "03_wall_drop_softland": [0, 4, 9, 13, 18, 27, 36, 45],
    "04_window_drop_softland": [0, 4, 9, 13, 18, 23, 27, 32],
    "10_wallrun_slanted": [0, 11, 22, 34, 45, 57, 68, 80],
    "12_tictac_slanted_wall": [0, 15, 34, 76, 106, 110, 145, 152],
    "15_cat_leap_wall": [0, 21, 43, 65, 86, 108, 130, 152],
}

# MediaPipe y is down. A tip more than 6 cm below its partner, and well
# below the neighbouring frames, is a mis-track.
PAIR = {31: 29, 32: 30, 19: 15, 20: 16, 17: 15, 18: 16, 21: 15, 22: 16}
FEET = {"L": (27, 29, 31), "R": (28, 30, 32)}
HANDS = {"L": (15, 17, 19, 21), "R": (16, 18, 20, 22)}
NAMES = {
    15: "lwrist", 16: "rwrist", 17: "lpinky", 18: "rpinky",
    19: "lindex", 20: "rindex", 21: "lthumb", 22: "rthumb",
    31: "ltoe", 32: "rtoe",
}
EDGES = [
    (11, 12), (11, 23), (12, 24), (23, 24),
    (11, 13), (13, 15), (12, 14), (14, 16),
    (15, 19), (16, 20), (23, 25), (25, 27), (24, 26), (26, 28),
    (27, 29), (29, 31), (27, 31), (28, 30), (30, 32), (28, 32),
    (0, 7), (0, 8),
]
LEFT = {11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 7}
RIGHT = {12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 8}
# Shoulder rotation (Y) and elbow flare (Z). Pitch and elbow flex stay put,
# so the arm does not straighten.
ARM_AXES = (("UpperArm_L", 1), ("UpperArm_L", 2), ("UpperArm_R", 1), ("UpperArm_R", 2))
ARM_MESH = (
    "Mesh_UpperArm_L", "Mesh_UpperArm_R",
    "Mesh_LowerArm_L", "Mesh_LowerArm_R",
    "Mesh_Hand_L", "Mesh_Hand_R",
)
ARM_PAIRS = (
    ("Mesh_LowerArm_L", "Mesh_LowerArm_R"),
    ("Mesh_LowerArm_L", "Mesh_Hand_R"),
    ("Mesh_Hand_L", "Mesh_LowerArm_R"),
    ("Mesh_Hand_L", "Mesh_Hand_R"),
    ("Mesh_UpperArm_L", "Mesh_LowerArm_R"),
    ("Mesh_LowerArm_L", "Mesh_UpperArm_R"),
    ("Mesh_UpperArm_L", "Mesh_UpperArm_R"),
)
LINE = 0.04


def _min_z(name):
    obj = bpy.data.objects[name]
    mw = obj.matrix_world
    best = None
    for v in obj.data.vertices:
        z = (mw @ v.co).z
        if best is None or z < best:
            best = z
    return 0.0 if best is None else best


def _max_x(name):
    obj = bpy.data.objects.get(name)
    if obj is None or obj.type != "MESH":
        return -9.0
    mw = obj.matrix_world
    best = None
    for v in obj.data.vertices:
        x = (mw @ v.co).x
        if best is None or x > best:
            best = x
    return -9.0 if best is None else best


def _spike_flags(worlds):
    n = len(worlds)
    flagged = [set() for _ in range(n)]
    for tip, prox in PAIR.items():
        gaps = [worlds[i][tip][1] - worlds[i][prox][1] for i in range(n)]
        for i, g in enumerate(gaps):
            if g <= 0.06:
                continue
            window = [gaps[k] for k in range(max(0, i - 6), min(n, i + 7)) if k != i]
            if not window:
                continue
            med = sorted(window)[len(window) // 2]
            if g > med + 0.035:
                flagged[i].add(tip)
    return flagged


def _cleaned(worlds, flags):
    """Copy landmarks. Flagged tips are interpolated, or clamped to the limb."""
    n = len(worlds)
    out = [[[float(c) for c in p] for p in W] for W in worlds]
    tips = set()
    for row in flags:
        tips |= row
    for tip in tips:
        known = [i for i in range(n) if tip not in flags[i]]
        prox = PAIR.get(tip)
        for i in range(n):
            if tip not in flags[i]:
                continue
            prev = max((k for k in known if k < i), default=None)
            nxt = min((k for k in known if k > i), default=None)
            if prev is not None and nxt is not None and nxt - prev <= 12:
                u = (i - prev) / float(nxt - prev)
                for c in range(3):
                    out[i][tip][c] = (1.0 - u) * worlds[prev][tip][c] + u * worlds[nxt][tip][c]
            elif prox is not None and out[i][tip][1] > worlds[i][prox][1] + 0.01:
                out[i][tip][1] = worlds[i][prox][1] + 0.01
    return out


def _ground_contacts(frame):
    """Sides whose sole or palm sits within 4 cm of the cleaned ground line.

    clear/hand_clear are metres above that line (0 = the line itself).
    """
    ys = [frame[j][1] for j in range(11, 33)]
    base = max(ys)
    feet, hands = [], []
    clear, hand_clear = {}, {}
    for side, idxs in FEET.items():
        clear[side] = min(base - frame[j][1] for j in idxs)
        if clear[side] <= LINE:
            feet.append(side)
    for side, idxs in HANDS.items():
        hand_clear[side] = min(base - frame[j][1] for j in idxs)
        if hand_clear[side] <= LINE:
            hands.append(side)
    return feet, hands, clear, hand_clear


def _flag_note(flags):
    by_tip = {}
    for i, row in enumerate(flags):
        for tip in row:
            by_tip.setdefault(tip, []).append(i)
    parts = []
    for tip, frames in sorted(by_tip.items()):
        ranges = []
        start = prev = frames[0]
        for i in frames[1:]:
            if i == prev + 1:
                prev = i
            else:
                ranges.append(f"{start}-{prev}" if start != prev else str(start))
                start = prev = i
        ranges.append(f"{start}-{prev}" if start != prev else str(start))
        parts.append(f"{NAMES.get(tip, str(tip))}={','.join(ranges)}")
    return " ".join(parts)


def _apply(arm, bones, cap):
    p10._write(arm, bones)
    arm.location = Vector((cap[0], cap[1], cap[2]))
    arm.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()


def _aim_leg(arm, bones, cap, side, below):
    """Rotate the thigh so this sole drops. The knee flex is left alone."""
    name = f"UpperLeg_{side}"
    other = "R" if side == "L" else "L"
    base = bones[name][0]

    def excess():
        _apply(arm, bones, cap)
        return _min_z(f"Mesh_Foot_{side}") - (_min_z(f"Mesh_Foot_{other}") - below)

    if excess() <= 0.03:
        return 0.0, excess()
    best_off = 0.0
    best_ex = excess()
    for sign in (1, -1):
        hit = False
        for step in range(5, 91, 5):
            bones[name][0] = base + sign * step
            ex = excess()
            if ex < best_ex - 0.005:
                best_ex = ex
                best_off = sign * step
            if ex <= 0.03:
                hit = True
                break
        bones[name][0] = base
        if hit:
            break
    bones[name][0] = base + best_off
    ex = excess()
    return best_off, ex


def _aim_palms(arm, bones, cap, sides):
    """Deepen the crouch and drop the arms until a palm is the lowest point.

    Extra hip flex is more negative UpperLeg X. Elbow flex is not reduced.
    """
    if not sides:
        return 0.0, 0.0

    def hands_feet():
        _apply(arm, bones, cap)
        hz = min(_min_z(f"Mesh_Hand_{s}") for s in sides)
        fz = min(_min_z("Mesh_Foot_L"), _min_z("Mesh_Foot_R"))
        return hz, fz

    hz, fz = hands_feet()
    # The palm has to end below the soles, or the floor pin lands on the shoe.
    if hz <= fz - 0.01:
        return 0.0, 0.0
    base_leg = {s: bones[f"UpperLeg_{s}"][0] for s in ("L", "R")}
    base_arm = {s: bones[f"UpperArm_{s}"][0] for s in ("L", "R")}
    hoff = 0.0
    aoff = 0.0
    best = hz - fz
    # More hip flex lifts the feet; a lower arm pitch drops the hands.
    # Elbow flex is not touched, so the arm does not straighten.
    for _step in range(28):
        improved = False
        for kind, delta, limit in (("hip", -5.0, -80.0), ("arm", -5.0, -60.0)):
            trial_h = hoff + (delta if kind == "hip" else 0.0)
            trial_a = aoff + (delta if kind == "arm" else 0.0)
            if trial_h < limit or trial_a < limit:
                continue
            for s in ("L", "R"):
                bones[f"UpperLeg_{s}"][0] = base_leg[s] + trial_h
            for s in sides:
                bones[f"UpperArm_{s}"][0] = base_arm[s] + trial_a
            hz, fz = hands_feet()
            gap = hz - fz
            if gap < best - 0.005:
                best = gap
                hoff, aoff = trial_h, trial_a
                improved = True
                if hz <= fz - 0.01:
                    return hoff, aoff
            else:
                for s in ("L", "R"):
                    bones[f"UpperLeg_{s}"][0] = base_leg[s] + hoff
                for s in sides:
                    bones[f"UpperArm_{s}"][0] = base_arm[s] + aoff
        if not improved:
            break
    _apply(arm, bones, cap)
    return hoff, aoff


def _pin_z(arm, names):
    """Drop the preview capsule so the named meshes meet z, without burying anything."""
    z = min(_min_z(name) for name in names)
    low, where = p10._lowest_mesh()
    delta = 0.002 - z
    if low + delta < -0.001:
        delta = -0.001 - low
    if abs(delta) > 0.0004:
        arm.location.z += delta
        bpy.context.view_layer.update()
    left = min(_min_z(name) for name in names)
    return delta, left, where


def _wall_foot_high(wall_side):
    """A sole already on the face and well above the floor is a wall plant."""
    if wall_side != "-":
        return False
    for side in ("L", "R"):
        if _max_x(f"Mesh_Foot_{side}") > -0.02 and _min_z(f"Mesh_Foot_{side}") > 0.20:
            return True
    return False


def _arm_depth(arm, locals_c, polys_c):
    packed = {}
    for name in ARM_MESH:
        obj = bpy.data.objects[name]
        verts = proof._world(locals_c[name], obj.matrix_world)
        packed[name] = (obj, verts, proof._bvh(verts, polys_c[name]))
    worst = 0.0
    where = ""
    for a, b in ARM_PAIRS:
        oa, va, ba = packed[a]
        ob, vb, bb = packed[b]
        joint = proof._shared_joint(arm, oa.parent_bone, ob.parent_bone)
        da = proof._inside_depths(va, polys_c[a], ba, bb, joint)
        db = proof._inside_depths(vb, polys_c[b], bb, ba, joint)
        for label, depths in ((f"{a} in {b}", da), (f"{b} in {a}", db)):
            if not depths:
                continue
            d = max(depths.values())
            if d > worst:
                worst = d
                where = label
    return worst, where


def _separate_forearms(arm, bones, track, cap, locals_c, polys_c):
    """±20° of shoulder rotation and elbow flare. Pitch and elbow flex stay."""
    depth, where = _arm_depth(arm, locals_c, polys_c)
    if depth <= proof.LIMIT_M:
        return depth, depth, where, []
    start = depth

    def measure():
        _apply(arm, bones, cap)
        return _arm_depth(arm, locals_c, polys_c)

    for _pass in range(2):
        changed = False
        for name, comp in ARM_AXES:
            base = track[name][comp]
            best = bones[name][comp]
            best_d = depth
            for off in (0, 5, -5, 10, -10, 15, -15, 20, -20):
                bones[name][comp] = base + off
                d, _w = measure()
                if d < best_d - 0.0002:
                    best_d = d
                    best = base + off
                    changed = True
            bones[name][comp] = best
            depth = best_d
        if not changed or depth <= proof.LIMIT_M:
            break
    d, where = measure()
    moved = []
    for name, comp in ARM_AXES:
        delta = bones[name][comp] - track[name][comp]
        if abs(delta) > 0.01:
            moved.append(f"{name}[{comp}]={delta:+.0f}")
    return start, d, where, moved


def _nonfoot_pen(wall_side, skip_foot):
    pen = 0.0
    where = ""
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        if skip_foot and skip_foot in obj.name:
            continue
        mx = _max_x(obj.name)
        if wall_side == "-" and mx > 0.004:
            d = mx - 0.004
        else:
            continue
        if d > pen:
            pen = d
            where = obj.name
    return pen, where


def _aim_wall_hip(arm, bones, cap, ids, local, wall_side):
    """Smallest hip turn that seats the leading sole with the chest still outside."""
    base = list(bones["Hips"])
    best = None

    def trial(dx, dy, dz):
        bones["Hips"] = [base[0] + dx, base[1] + dy, base[2] + dz]
        _apply(arm, bones, cap)
        foot, lead = p10._leading_foot(ids, local, wall_side)
        if wall_side == "-":
            arm.location.x += -0.003 - lead
        bpy.context.view_layer.update()
        pen, where = _nonfoot_pen(wall_side, f"Foot_{foot}")
        foot, lead = p10._leading_foot(ids, local, wall_side)
        gap = max(0.0, -lead) if wall_side == "-" else 0.0
        chest = _max_x("Mesh_Chest")
        chest_pen = max(0.0, chest - 0.004)
        return gap, pen, chest_pen, where, foot

    # Smallest yaw first, then a coarse pitch/roll around that yaw.
    yaws = [0]
    for step in range(4, 41, 4):
        yaws.extend((step, -step))
    for dy in yaws:
        gap, pen, chest_pen, where, foot = trial(0.0, float(dy), 0.0)
        rank = (chest_pen > 0.001 or pen > 0.001, gap + pen, abs(dy))
        if best is None or rank < best[0]:
            best = (rank, 0.0, float(dy), 0.0, gap, pen, chest_pen, where, foot)
        if rank[0] is False and gap <= 0.02:
            break
    y0 = best[2]
    if best[5] > 0.001 or best[4] > 0.02:
        for dx in range(-16, 17, 4):
            for dz in range(-16, 17, 4):
                gap, pen, chest_pen, where, foot = trial(dx, y0, dz)
                rank = (chest_pen > 0.001 or pen > 0.001, gap + pen, abs(dx) + abs(y0) + abs(dz))
                if rank < best[0]:
                    best = (rank, dx, y0, dz, gap, pen, chest_pen, where, foot)
    dx, dy, dz = best[1], best[2], best[3]
    # One-degree refine on the winning yaw.
    for dy2 in range(int(dy) - 3, int(dy) + 4):
        gap, pen, chest_pen, where, foot = trial(dx, dy2, dz)
        rank = (chest_pen > 0.001 or pen > 0.001, gap + pen, abs(dx) + abs(dy2) + abs(dz))
        if rank < best[0]:
            best = (rank, dx, dy2, dz, gap, pen, chest_pen, where, foot)
    dx, dy, dz = best[1], best[2], best[3]
    bones["Hips"] = [base[0] + dx, base[1] + dy, base[2] + dz]
    gap, pen, chest_pen, where, foot = trial(dx, dy, dz)
    return (dx, dy, dz), gap, pen, chest_pen, where, foot


def _slab_pen(names, wall_top):
    """Metres past the contact face, ignoring vertices above the lip."""
    z0, z1 = p10._wall_slab(wall_top)
    pen = 0.0
    for name in names:
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        mw = obj.matrix_world
        for v in obj.data.vertices:
            p = mw @ v.co
            if p.z < z0 or p.z > z1 or p.x <= 0.004:
                continue
            pen = max(pen, p.x - 0.004)
    return pen


def _restore_lip(arm, bones, cap, wall_top):
    """After a height pin, put the palms back on the lip. Feet stay where they are."""
    bases = {s: bones[f"UpperArm_{s}"][0] for s in ("L", "R")}
    watch = (
        "Mesh_Hand_L", "Mesh_Hand_R", "Mesh_LowerArm_L", "Mesh_LowerArm_R",
        "Mesh_Chest", "Mesh_Hips",
    )

    def score():
        _apply(arm, bones, cap)
        gaps = []
        for side in ("L", "R"):
            _pt, gap = rh._palm_gap(arm, side, 0.0, wall_top)
            if gap is not None:
                gaps.append(gap)
        gap = max(gaps) if gaps else 1.0
        pen = _slab_pen(watch, wall_top)
        feet = min(_min_z("Mesh_Foot_L"), _min_z("Mesh_Foot_R"))
        return gap, pen, feet

    for side in ("L", "R"):
        best_off = 0
        best_rank = None
        for off in range(-40, 41, 5):
            bones[f"UpperArm_{side}"][0] = bases[side] + off
            gap, pen, feet = score()
            if feet < -0.01:
                continue
            rank = (max(0.0, gap - 0.02) + pen, abs(off))
            if best_rank is None or rank < best_rank:
                best_rank = rank
                best_off = off
        bones[f"UpperArm_{side}"][0] = bases[side] + best_off
    gap, pen, _feet = score()
    return gap, pen


def solve():
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, local = p10._sole_ids(arm)
    p10._ensure_pose(arm)
    doc = json.load(open(JSON_PATH))
    only = os.environ.get("PASS11_ONLY", "")
    pieces = [bpy.data.objects[n] for n in ARM_MESH]
    locals_c = {o.name: proof._local_coords(o) for o in pieces}
    polys_c = {o.name: proof._polys(o) for o in pieces}
    lines = ["clip frame pelvis_deg hip_deg knee_deg spine_deg height_cm"]
    notes = []
    for spec in sp.CLIPS:
        cid = spec["id"]
        if only and not cid.startswith(only):
            continue
        data = sp.load_clip(cid)
        smooth = sp.clip_curves(cid, keys=24)["raw_smooth"]
        scale = p5._leg_scale(data)
        heights = data["root_height_above_feet_m"]
        worlds = data["world_xyz_m"]
        flags = _spike_flags(worlds)
        cleaned = _cleaned(worlds, flags)
        entry = doc["clips"][cid]
        n = len(entry["keys"])
        wall_side = entry.get("wall_side", "+")
        wall_top = entry.get("wall_top_m")
        do_wall = spec["verb"] != "softland"
        floor = not cid.startswith("10_")
        flag_txt = _flag_note(flags)
        if flag_txt:
            notes.append(f"FLAG {cid} {flag_txt}")
        print(f"POSE {cid} {n} floor={floor}", flush=True)
        for i in range(n):
            track = p10._track(smooth, i, n)
            bones = {k: [float(a) for a in v] for k, v in entry["keys"][i]["bones"].items()}
            cap = [float(c) for c in entry["capsule_preview_m"][i]]
            _apply(arm, bones, cap)
            feet, hands, clear, hand_clear = _ground_contacts(cleaned[i])
            aim_bits = []
            if floor:
                foot_c = min((clear[s] for s in feet), default=9.0)
                hand_c = min((hand_clear[s] for s in hands), default=9.0)
                # A hand on the line that is at least as low as the feet is the plant.
                # Frame 0's toe sits 2 cm above the fingers; planting the shoe there
                # leaves the mannequin standing on its foot.
                palms_lead = bool(hands) and hand_c <= foot_c + 0.01
                if palms_lead:
                    hoff, aoff = _aim_palms(arm, bones, cap, hands)
                    if abs(hoff) > 0.01 or abs(aoff) > 0.01:
                        aim_bits.append(f"crouch={hoff:+.0f} arm={aoff:+.0f}")
                else:
                    for side in feet:
                        other = "R" if side == "L" else "L"
                        below = max(0.0, clear[other] - clear[side])
                        if other not in feet:
                            off, ex = _aim_leg(arm, bones, cap, side, below)
                            if abs(off) > 0.01:
                                aim_bits.append(f"leg{side}={off:+.0f}")
                                if ex > 0.03:
                                    aim_bits.append(f"leg{side}_short={ex * 100:.1f}cm")
                cap = [arm.location.x, arm.location.y, arm.location.z]
                if (not palms_lead) and _wall_foot_high(wall_side):
                    if i in STORY.get(cid, []) or aim_bits:
                        notes.append(f"WALL_HOLD {cid} f={i} sole on the face, height kept")
                        print(notes[-1], flush=True)
                else:
                    names = []
                    if palms_lead:
                        names.extend(f"Mesh_Hand_{s}" for s in hands)
                    elif feet:
                        names.extend(f"Mesh_Foot_{s}" for s in feet)
                    if names:
                        delta, left, low_where = _pin_z(arm, names)
                        cap = [arm.location.x, arm.location.y, arm.location.z]
                        kind = "PALM" if palms_lead else "SOLE"
                        if abs(delta) > 0.001 or left > 0.012 or aim_bits:
                            msg = (
                                f"PIN {cid} f={i} {kind} {''.join(hands if palms_lead else feet)} "
                                f"dz_cm={delta * 100:.1f} contact_cm={left * 100:.1f} "
                                f"{' '.join(aim_bits)} low={low_where}"
                            )
                            if i in STORY.get(cid, []) or aim_bits or left > 0.012:
                                notes.append(msg)
                            if i in STORY.get(cid, []) or aim_bits:
                                print(msg, flush=True)
            if do_wall and cid.startswith("10_") and i == 45:
                delta, gap, pen, chest_pen, where, foot = _aim_wall_hip(
                    arm, bones, cap, ids, local, wall_side,
                )
                cap = [arm.location.x, arm.location.y, arm.location.z]
                _eul, target = p10.pelvis_of(worlds[i])
                perr = p10._pelvis_err(arm, target)
                msg = (
                    f"WALL45 {cid} f=45 hip_d=({delta[0]:+.0f},{delta[1]:+.0f},{delta[2]:+.0f}) "
                    f"sole={foot} gap_cm={gap * 100:.2f} pen_cm={pen * 100:.2f} "
                    f"chest_cm={chest_pen * 100:.2f} {where} pelvis_deg={perr:.2f}"
                )
                notes.append(msg)
                print(msg, flush=True)
            before, after, where, moved = _separate_forearms(
                arm, bones, track, cap, locals_c, polys_c,
            )
            cap = [arm.location.x, arm.location.y, arm.location.z]
            if moved or before > proof.LIMIT_M:
                msg = (
                    f"ARM {cid} f={i} {', '.join(moved) if moved else 'no-move'} "
                    f"depth_cm={after * 100:.2f} was={before * 100:.2f} {where}"
                )
                if after > proof.LIMIT_M or i in STORY.get(cid, []):
                    notes.append(msg)
                if after > 0.01 or (moved and i % 20 == 0):
                    print(msg, flush=True)
            if (cid, i) == ("15_cat_leap_wall", 86) and wall_top is not None:
                gap, pen = _restore_lip(arm, bones, cap, wall_top)
                cap = [arm.location.x, arm.location.y, arm.location.z]
                msg = f"LIP {cid} f=86 gap_cm={gap * 100:.2f} pen_cm={pen * 100:.2f}"
                notes.append(msg)
                print(msg, flush=True)
            if do_wall:
                p10._push_wall(arm, wall_side, (), wall_top)
            lift, was, low_where = p10._ground_clamp(arm)
            cap = [arm.location.x, arm.location.y, arm.location.z]
            if lift > 0.004 and (i in STORY.get(cid, []) or lift > 0.02):
                notes.append(
                    f"LIFT {cid} f={i} {low_where} lift_cm={lift * 100:.2f} was_cm={was * 100:.2f}"
                )
            _eul, target = p10.pelvis_of(worlds[i])
            perr = p10._pelvis_err(arm, target)
            hip = p10._group_err(bones, track, p10.SCORED_HIP)
            knee = p10._group_err(bones, track, p10.SCORED_KNEE)
            spine = p10._group_err(bones, track, ("Spine",))
            h_err = (p10._hip_z(arm) - float(heights[i]) * scale) * 100.0
            lines.append(
                f"{cid} {i} {perr:.3f} {hip:.3f} {knee:.3f} {spine:.3f} {h_err:.2f}"
            )
            entry["keys"][i]["bones"] = p10._round_eulers(bones)
            entry["capsule_preview_m"][i] = [round(c, 4) for c in cap]
            if i % 40 == 0:
                print(f"  {cid} {i}/{n} pelvis={perr:.2f} hip={hip:.1f}", flush=True)
    os.makedirs(PASS11, exist_ok=True)
    if not only:
        with open(JSON_PATH, "w") as f:
            json.dump(doc, f)
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(lines))
        f.write("\n\n")
        f.write(
            "Ground line is the cleaned landmark low (MediaPipe y down). "
            "A tip that dives below its own limb is interpolated and is not planted. "
            "Shoe-bottom planting is not used. Root motion is off. The rig is unchanged.\n"
        )
        if notes:
            f.write("\n".join(notes) + "\n")
    print("wrote", ERROR_PATH, flush=True)
    rh._reset(arm)
    rh._hide_wall()


def _show_floor(arm):
    """A slab, not an edge-on plane, so the side view has a floor band."""
    name = "FloorSlab"
    obj = bpy.data.objects.get(name)
    if obj is None:
        bpy.ops.mesh.primitive_cube_add(location=(0.0, 0.0, -0.11))
        obj = bpy.context.active_object
        obj.name = name
        mat = bpy.data.materials.new("FloorSlabMat")
        mat.use_nodes = True
        mat.diffuse_color = (0.24, 0.22, 0.20, 1)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.24, 0.22, 0.20, 1)
            bsdf.inputs["Roughness"].default_value = 0.95
        obj.data.materials.append(mat)
    obj.location = (arm.location.x, arm.location.y, -0.11)
    obj.scale = (4.0, 4.0, 0.11)
    obj.hide_render = False
    obj.hide_set(False)
    ground = bpy.data.objects.get("Ground")
    if ground is not None:
        ground.hide_render = True


def _show_shadow(x, y):
    name = "ContactShadow"
    obj = bpy.data.objects.get(name)
    if obj is None:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8)
        obj = bpy.context.active_object
        obj.name = name
        mat = bpy.data.materials.new("ContactShadowMat")
        mat.use_nodes = True
        mat.diffuse_color = (0.02, 0.02, 0.02, 1)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.02, 0.02, 0.02, 1)
            bsdf.inputs["Roughness"].default_value = 1.0
        obj.data.materials.append(mat)
    # Just in front of the body so the side elevation sees the smudge on the floor.
    obj.location = (x, y - 0.35, 0.012)
    obj.scale = (0.34, 0.12, 0.018)
    obj.hide_render = False
    obj.hide_set(False)


def _contact_x(arm):
    best = None
    best_z = None
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        mw = obj.matrix_world
        name = obj.name
        for v in obj.data.vertices:
            p = mw @ v.co
            z = p.z
            if best_z is None or z < best_z:
                best_z = z
                best = p.x
    if best is None:
        return arm.location.x
    return best


def _frame_cam(arm):
    """Side elevation along +Y. Image right is +X, the stick's image right."""
    z0, z1 = 0.0, 1.2
    first = True
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        mw = obj.matrix_world
        for v in obj.data.vertices:
            z = (mw @ v.co).z
            if first:
                z0 = z1 = z
                first = False
            else:
                z0 = min(z0, z)
                z1 = max(z1, z)
    z0 = min(z0, -0.24)
    mid = (z0 + z1) * 0.5
    span = max(1.9, (z1 - z0) * 1.18)
    cap = arm.location
    return (cap.x, cap.y - 3.6, mid), (cap.x, cap.y, mid), span


def _draw_stick(frame, w, h):
    from PIL import Image, ImageDraw
    img = Image.new("RGB", (w, h), (248, 244, 238))
    draw = ImageDraw.Draw(img)
    use = [0, 7, 8] + list(range(11, 33))
    xs = [frame[j][0] for j in use]
    ys = [frame[j][1] for j in use]
    body_y = max(frame[j][1] for j in range(11, 33))
    ys.append(body_y)
    minx, maxx = min(xs), max(xs)
    miny, maxy = min(ys), max(ys)
    pad = 0.08
    minx -= pad
    maxx += pad
    miny -= pad
    maxy += pad
    span_x = max(0.05, maxx - minx)
    span_y = max(0.05, maxy - miny)
    margin = 14
    scale = min((w - 2 * margin) / span_x, (h - 2 * margin) / span_y)

    def pix(j):
        x = margin + (frame[j][0] - minx) * scale
        y = margin + (frame[j][1] - miny) * scale
        return (x, y)

    gy = margin + (body_y - miny) * scale
    draw.line((8, gy, w - 8, gy), fill=(90, 78, 68), width=2)
    for a, b in EDGES:
        if a in LEFT and b in LEFT:
            col = (255, 140, 40)
        elif a in RIGHT and b in RIGHT:
            col = (40, 140, 255)
        else:
            col = (190, 186, 180)
        draw.line((pix(a), pix(b)), fill=col, width=4)
    for j in use:
        x, y = pix(j)
        r = 3
        draw.ellipse((x - r, y - r, x + r, y + r), fill=(40, 36, 32))
    return img


def render_strips():
    from PIL import Image, ImageDraw
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
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
    os.makedirs(PASS11, exist_ok=True)
    prev = "/tmp/pass11_cells"
    os.makedirs(prev, exist_ok=True)
    labels = {
        "03_wall_drop_softland": "03 soft land",
        "04_window_drop_softland": "04 soft land",
        "10_wallrun_slanted": "10 wall run",
        "12_tictac_slanted_wall": "12 tic-tac",
        "15_cat_leap_wall": "15 cat leap",
    }
    only = os.environ.get("PASS11_ONLY", "")
    for spec in sp.CLIPS:
        cid = spec["id"]
        if only and not cid.startswith(only):
            continue
        entry = doc["clips"][cid]
        data = sp.load_clip(cid)
        t0 = data["times_s"][0]
        worlds = data["world_xyz_m"]
        cleaned = _cleaned(worlds, _spike_flags(worlds))
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
            _show_floor(arm)
            _show_shadow(_contact_x(arm), arm.location.y)
            if has_wall:
                rh._show_wall(0.0, wall_top, arm.location.y, 2.4, side=side)
            else:
                rh._hide_wall()
            if s == 0:
                foot = _max_x("Mesh_Foot_L")
                ank = cleaned[i][27][0]
                print(f"HAND {cid} Foot_L.maxx={foot:.3f} lank.x={ank:.3f}", flush=True)
            loc, look, ortho = _frame_cam(arm)
            path = os.path.join(prev, f"{cid}_{s}.png")
            rh._shot(path, loc, look, ortho=ortho)
            shots.append(path)
            print(f"ref {cid} f={i}", flush=True)
        cell_w, cell_h = 400, 300
        canvas = Image.new("RGB", (cell_w * 4, cell_h * 2 + 28), (48, 44, 40))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle((0, 0, 1600, 28), fill=(20, 18, 16))
        draw.text(
            (12, 8),
            labels[cid] + "   same side as the stick, orange=left blue=right",
            fill=(245, 236, 220),
        )
        for s, i in enumerate(frames):
            stick = _draw_stick(cleaned[i], 172, 268)
            man = Image.open(shots[s]).convert("RGB")
            man.thumbnail((214, 268), Image.Resampling.LANCZOS)
            col = s % 4
            row = s // 4
            x0 = col * cell_w
            y0 = 28 + row * cell_h
            canvas.paste(stick, (x0 + 6, y0 + 16))
            canvas.paste(man, (x0 + 182 + (214 - man.width) // 2, y0 + 16 + (268 - man.height) // 2))
            t = data["times_s"][i] - t0
            draw.text((x0 + 10, y0 + 4), f"{t:.2f}s", fill=(245, 236, 220))
        out = os.path.join(PASS11, f"ref_{rh.SLUGS[cid]}.png")
        canvas.save(out, optimize=True)
        if os.path.getsize(out) > 400_000:
            q = canvas.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
            q.save(out, optimize=True)
        print("wrote", out, os.path.getsize(out), flush=True)
    rh._reset(arm)
    rh._hide_wall()


if __name__ == "__main__":
    stage = os.environ.get("PASS11_STAGE", "pose")
    if stage == "pose":
        solve()
    elif stage == "stills":
        render_strips()
    else:
        solve()
        render_strips()
