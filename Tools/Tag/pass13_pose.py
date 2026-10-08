#!/usr/bin/env python3
"""Pass 13: take tic-tac to pose=0 before any other clip.

Garbage stick frames are bridged between the nearest good keys. A palm-plant
crouch at frame 0 is hand-keyed, with the thighs unfolded and the knees
splayed until the chest is out of the thigh. Every other torso hit loses the
smallest amount of hip flexion that pulls the thigh out of the abdomen plates.

The game still has no root motion. Root and the armature stay at the origin.
capsule_preview_m is the preview path only. The rig is not edited.

solve() starts from the pass-12 keys. Do not run it twice on the written file.
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
import pass11_pose as p11
import pass12_pose as p12
import render_hier_v080_stills as rh
import storror_pose as sp

PASS13 = os.path.join(rh.OUT, "pass13")
ERROR_PATH = os.path.join(PASS13, "pose_error_pass13.txt")
JSON_PATH = p10.JSON_PATH
TIC = "12_tictac_slanted_wall"
# Plants, push, and landing the track is allowed to keep.
PROTECTED = {15, 34, 76, 106, 110}
# The mannequin collapses here while the stick is still a forward crouch.
HEAP = set(range(145, 154))
LIMBS = (
    ("uaL", 11, 13), ("uaR", 12, 14),
    ("laL", 13, 15), ("laR", 14, 16),
    ("ulL", 23, 25), ("ulR", 24, 26),
    ("llL", 25, 27), ("llR", 26, 28),
)
TORSOS = ("Mesh_Spine", "Mesh_Chest")
CLEAR = 0.0045


def _dist(a, b):
    return math.sqrt(sum((a[i] - b[i]) ** 2 for i in range(3)))


def _sub(a, b):
    return tuple(a[i] - b[i] for i in range(3))


def _add(a, b):
    return tuple(a[i] + b[i] for i in range(3))


def _mul(a, s):
    return tuple(a[i] * s for i in range(3))


def _dot(a, b):
    return sum(a[i] * b[i] for i in range(3))


def _seg_dist(a, b, c, d):
    u, v, w = _sub(b, a), _sub(d, c), _sub(a, c)
    uu, vv, uv = _dot(u, u), _dot(v, v), _dot(u, v)
    uw, vw = _dot(u, w), _dot(v, w)
    den = uu * vv - uv * uv
    if den < 1e-12:
        s = t = 0.0
    else:
        s = max(0.0, min(1.0, (uv * vw - vv * uw) / den))
        t = max(0.0, min(1.0, (uu * vw - uv * uw) / den))
    return _dist(_add(a, _mul(u, s)), _add(c, _mul(v, t))), s, t


def _shortest(a, b):
    return (b - a + 180.0) % 360.0 - 180.0


def _lerp(a, b, u):
    return a + _shortest(a, b) * u


def _bad_frames(worlds):
    """Frame-to-frame limb jumps over 15%, or a limb crossing the torso."""
    n = len(worlds)
    lengths = {
        name: [_dist(frame[a], frame[b]) for frame in worlds]
        for name, a, b in LIMBS
    }
    bad = {}
    for i in range(1, n):
        jumps = []
        for name, _a, _b in LIMBS:
            prev = lengths[name][i - 1]
            cur = lengths[name][i]
            if prev > 1e-4 and (cur > prev * 1.15 or cur < prev * 0.85):
                jumps.append(name)
        if jumps:
            bad.setdefault(i, []).append("jump " + ",".join(jumps))
    for i, frame in enumerate(worlds):
        sh = tuple(0.5 * (frame[11][k] + frame[12][k]) for k in range(3))
        hp = tuple(0.5 * (frame[23][k] + frame[24][k]) for k in range(3))
        crosses = []
        for name, a, b in LIMBS:
            dist, s, t = _seg_dist(frame[a], frame[b], sh, hp)
            if name.startswith("ua") and s < 0.25:
                continue
            if name.startswith("ul") and s < 0.25:
                continue
            if dist < 0.06 and 0.15 < t < 0.85 and s > 0.15:
                crosses.append(name)
        if crosses:
            bad.setdefault(i, []).append("cross " + ",".join(crosses))
    for i in HEAP:
        bad.setdefault(i, []).append("heap")
    bad.setdefault(0, []).append("tangle")
    for i in PROTECTED:
        bad.pop(i, None)
    return bad


def _ranges(frames):
    if not frames:
        return []
    ordered = sorted(frames)
    ranges = []
    start = prev = ordered[0]
    for i in ordered[1:]:
        if i == prev + 1:
            prev = i
        else:
            ranges.append((start, prev))
            start = prev = i
    ranges.append((start, prev))
    return ranges


def _copy_bones(bones):
    return {k: [float(a) for a in v] for k, v in bones.items()}


def _bridge(keys, caps, bad):
    """Hand-lerp each bad range between the nearest frames that stayed."""
    n = len(keys)
    bridged = []
    for start, end in _ranges(bad):
        if start == 0:
            # Frame 0 is hand-keyed below. It is not a lerp.
            continue
        left = start - 1
        right = end + 1
        if right >= n:
            right = left
        lb = _copy_bones(keys[left]["bones"])
        rb = _copy_bones(keys[right]["bones"])
        names = sorted(set(lb) | set(rb))
        lc = caps[left]
        rc = caps[right]
        span = max(1, right - left)
        for i in range(start, end + 1):
            u = (i - left) / span
            bones = {}
            for name in names:
                a = lb.get(name, [0.0, 0.0, 0.0])
                b = rb.get(name, [0.0, 0.0, 0.0])
                bones[name] = [_lerp(a[k], b[k], u) for k in range(3)]
            keys[i]["bones"] = bones
            caps[i] = [lc[k] + (rc[k] - lc[k]) * u for k in range(3)]
        bridged.append((start, end, left, right))
    return bridged


def _pack(name, locals_c, polys):
    obj = bpy.data.objects[name]
    verts = proof._world(locals_c[name], obj.matrix_world)
    return verts, proof._bvh(verts, polys[name])


def _pair_depth(a, b, locals_c, polys, pack_b=None):
    va, ba = _pack(a, locals_c, polys)
    if pack_b is None:
        vb, bb = _pack(b, locals_c, polys)
    else:
        vb, bb = pack_b
    da = proof._inside_depths(va, polys[a], ba, bb, None)
    db = proof._inside_depths(vb, polys[b], bb, ba, None)
    depth = 0.0
    if da:
        depth = max(depth, max(da.values()))
    if db:
        depth = max(depth, max(db.values()))
    return depth


def _leg_depth(side, locals_c, polys, torsos):
    depth = 0.0
    for limb in (f"Mesh_UpperLeg_{side}", f"Mesh_LowerLeg_{side}"):
        for torso, pack in torsos.items():
            depth = max(depth, _pair_depth(limb, torso, locals_c, polys, pack))
    return depth


def _torso_packs(locals_c, polys):
    return {name: _pack(name, locals_c, polys) for name in TORSOS}


def _apply(arm, bones, cap):
    p11._apply(arm, bones, cap)


def _body_depth(arm, bones, cap, locals_c, polys):
    _apply(arm, bones, cap)
    packs = _torso_packs(locals_c, polys)
    return max(
        _leg_depth("L", locals_c, polys, packs),
        _leg_depth("R", locals_c, polys, packs),
    )


def _open_palm(bones):
    """Frame 0 stays a palm plant, but the knees splay and the hips rise.

    The abdomen plates are parented at the spine head, so rolling the spine
    forward drives the chest deeper into the thighs. The splay and the hip
    lift are what open the crouch. Spine and chest go back to rest with the
    other frames.
    """
    bones["UpperLeg_L"][2] = bones["UpperLeg_L"][2] + 35.0
    bones["UpperLeg_R"][2] = bones["UpperLeg_R"][2] - 35.0
    bones["Spine"] = [0.0, 0.0, 0.0]
    bones["Chest"] = [0.0, 0.0, 0.0]
    return bones


def _rest_plates(bones):
    """Put the abdomen plates back on the rest spine so the thigh can leave."""
    bones["Spine"] = [0.0, 0.0, 0.0]
    if "Chest" in bones:
        bones["Chest"] = [0.0, 0.0, 0.0]


def _relieve_leg(arm, bones, cap, side, locals_c, polys):
    """Smallest hip extension, then thigh twist, that pulls this leg out."""
    name = f"UpperLeg_{side}"
    base_x = bones[name][0]
    base_y = bones[name][1]
    _apply(arm, bones, cap)
    packs = _torso_packs(locals_c, polys)
    depth = _leg_depth(side, locals_c, polys, packs)
    if depth <= CLEAR:
        return 0.0, 0.0, depth
    added = 0.0
    for step in range(5, 181, 5):
        nxt = min(20.0, base_x + step)
        if nxt <= base_x + added + 0.1 and step > 5:
            break
        bones[name][0] = nxt
        _apply(arm, bones, cap)
        packs = _torso_packs(locals_c, polys)
        depth = _leg_depth(side, locals_c, polys, packs)
        added = bones[name][0] - base_x
        if depth <= CLEAR:
            return added, 0.0, depth
    prefer = -1.0 if side == "L" else 1.0
    best = (depth, 0.0, bones[name][1])
    for sign in (prefer, -prefer):
        for twist in (15, 30, 45, 60):
            bones[name][1] = base_y + sign * twist
            _apply(arm, bones, cap)
            packs = _torso_packs(locals_c, polys)
            depth = _leg_depth(side, locals_c, polys, packs)
            if depth < best[0] - 0.0005:
                best = (depth, sign * twist, bones[name][1])
            if depth <= CLEAR:
                return added, sign * twist, depth
    bones[name][1] = best[2]
    _apply(arm, bones, cap)
    packs = _torso_packs(locals_c, polys)
    depth = _leg_depth(side, locals_c, polys, packs)
    return added, best[1], depth


def _replant(arm, bones, cap, side, ids, local, knee0):
    """Bend the knee so a thigh that just unfolded does not bury the sole."""
    ll = f"LowerLeg_{side}"
    foot = f"Mesh_Foot_{side}"
    for _step in range(28):
        _apply(arm, bones, cap)
        med = p12._sole_z(ids, local, side)[1]
        low = p11._min_z(foot)
        if low >= -0.002 and med <= 0.012:
            break
        if low < -0.002 or med < 0.002:
            if bones[ll][0] >= 145.0:
                break
            bones[ll][0] += 3.0
        elif bones[ll][0] - 3.0 >= max(20.0, knee0):
            bones[ll][0] -= 3.0
        else:
            break
    p12._level_foot(arm, bones, cap, side, ids, local, span=24, step=4)
    _apply(arm, bones, cap)
    med = p12._sole_z(ids, local, side)[1]
    low = p11._min_z(foot)
    if low < -0.003:
        # The knee cannot lift this sole. A preview-capsule nudge keeps the
        # mesh out of the floor. It is the plant, not a clearance lift.
        cap[2] += -0.003 - low
        _apply(arm, bones, cap)
        med = p12._sole_z(ids, local, side)[1]
    return med


def _separate_pair(arm, bones, cap, locals_c, polys, mesh_a, mesh_b, channel):
    """Splay one channel until a crossing pair is under the limit."""
    depth = _pair_depth(mesh_a, mesh_b, locals_c, polys)
    if depth <= CLEAR:
        return depth
    left, right = channel
    base_l = bones[left[0]][left[1]]
    base_r = bones[right[0]][right[1]]
    best = (depth, 0.0)
    for sign in (1.0, -1.0):
        for step in (8, 16, 24, 36):
            bones[left[0]][left[1]] = base_l + sign * step
            bones[right[0]][right[1]] = base_r - sign * step
            _apply(arm, bones, cap)
            depth = _pair_depth(mesh_a, mesh_b, locals_c, polys)
            torso = _torso_packs(locals_c, polys)
            hold = max(
                _leg_depth("L", locals_c, polys, torso),
                _leg_depth("R", locals_c, polys, torso),
            )
            if hold > CLEAR + 0.001:
                continue
            if depth < best[0]:
                best = (depth, sign * step)
            if depth <= CLEAR:
                return depth
    bones[left[0]][left[1]] = base_l + best[1]
    bones[right[0]][right[1]] = base_r - best[1]
    _apply(arm, bones, cap)
    return _pair_depth(mesh_a, mesh_b, locals_c, polys)


def solve():
    if os.path.exists(ERROR_PATH) and "PASS13 APPLIED" in open(ERROR_PATH).read():
        if os.environ.get("PASS13_FORCE") != "1":
            raise SystemExit("pass 13 already applied; set PASS13_FORCE=1 to redo from this file")
    os.makedirs(PASS13, exist_ok=True)
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
    bad = _bad_frames(worlds)
    notes = ["PASS13 APPLIED", f"clip {TIC}"]
    notes.append("Bad tracking is a frame-to-frame limb length outside 15%, a limb crossing the torso, the 0.00 s tangle, or the 4.84–5.07 s heap.")
    notes.append("Protected real-track frames: " + ", ".join(str(i) for i in sorted(PROTECTED)))
    notes.append(
        "HAND 0 is a palm-plant crouch with the knees splayed 35°. "
        "The 0.00 s stick is a tangle, so the pose is not a literal retarget. "
        "Forward spine roll pushed the chest into the thighs, so the spine "
        "returns to rest and the hips unfold instead."
    )
    bridged = _bridge(entry["keys"], entry["capsule_preview_m"], bad)
    for start, end, left, right in bridged:
        why = []
        for i in range(start, end + 1):
            why.extend(bad.get(i, []))
        kinds = sorted(set(why))
        t0 = data["times_s"][start] - data["times_s"][0]
        t1 = data["times_s"][end] - data["times_s"][0]
        notes.append(
            f"BRIDGE f={start}-{end} t={t0:.2f}-{t1:.2f}s anchors {left},{right} ({'; '.join(kinds)})"
        )
    relief = []
    for i, key in enumerate(entry["keys"]):
        bones = _copy_bones(key["bones"])
        cap = [float(v) for v in entry["capsule_preview_m"][i]]
        if i == 0:
            bones = _open_palm(bones)
        feet, hands, clear, hand_clear = p12._contacts(cleaned[i])
        depth = _body_depth(arm, bones, cap, locals_c, polys)
        cross = max(
            _pair_depth("Mesh_LowerLeg_L", "Mesh_LowerLeg_R", locals_c, polys),
            _pair_depth("Mesh_Foot_L", "Mesh_Foot_R", locals_c, polys),
        )
        if depth <= CLEAR and cross <= CLEAR and i != 0:
            continue
        knee0 = {s: bones[f"LowerLeg_{s}"][0] for s in ("L", "R")}
        _rest_plates(bones)
        added = {}
        for side in ("L", "R"):
            dx, twist, depth = _relieve_leg(arm, bones, cap, side, locals_c, polys)
            added[side] = (dx, twist, depth)
            if side in feet and (abs(dx) > 0.1 or abs(twist) > 0.1):
                _replant(arm, bones, cap, side, ids, local, knee0[side])
        # A foot that the unfold buried, contact or not.
        for side in ("L", "R"):
            if p11._min_z(f"Mesh_Foot_{side}") < -0.003:
                _replant(arm, bones, cap, side, ids, local, knee0[side])
        _separate_pair(
            arm, bones, cap, locals_c, polys,
            "Mesh_LowerLeg_L", "Mesh_LowerLeg_R",
            (("UpperLeg_L", 2), ("UpperLeg_R", 2)),
        )
        _separate_pair(
            arm, bones, cap, locals_c, polys,
            "Mesh_Foot_L", "Mesh_Foot_R",
            (("Foot_L", 2), ("Foot_R", 2)),
        )
        foot_c = min((clear[sd] for sd in feet), default=9.0)
        hand_c = min((hand_clear[sd] for sd in hands), default=9.0)
        if hands and hand_c <= foot_c + 0.01:
            _apply(arm, bones, cap)
            hand_z = min(p11._min_z("Mesh_Hand_L"), p11._min_z("Mesh_Hand_R"))
            cap[2] += 0.004 - hand_z
            _apply(arm, bones, cap)
            low, _where = p10._lowest_mesh()
            if low < -0.003:
                cap[2] += -0.003 - low
        key["bones"] = p10._round_eulers(bones)
        entry["capsule_preview_m"][i] = [round(float(v), 4) for v in cap]
        if any(abs(added[s][0]) > 0.1 or abs(added[s][1]) > 0.1 for s in ("L", "R")):
            relief.append(
                f"f={i} "
                f"L flex+{added['L'][0]:.0f} yaw{added['L'][1]:+.0f} d={added['L'][2]*100:.2f} "
                f"R flex+{added['R'][0]:.0f} yaw{added['R'][1]:+.0f} d={added['R'][2]*100:.2f}"
            )
        if i % 20 == 0:
            print(f"cleared {i}", flush=True)
    notes.append(f"RELIEF frames={len(relief)}")
    notes.extend(relief)
    notes.append(
        "Thigh flexion was reduced only until the thigh and shin left the abdomen plates. "
        "The knee took the extra bend so the sole stayed down. Root motion is off. The rig is unchanged."
    )
    with open(JSON_PATH, "w") as f:
        json.dump(doc, f)
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(notes) + "\n")
    print("wrote", JSON_PATH, flush=True)
    print("wrote", ERROR_PATH, flush=True)
    rh._reset(arm)
    rh._hide_wall()


def _frame_body(arm):
    """Side camera along +Y, framed on the full body box in X and Z."""
    xs = []
    zs = []
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
    pad_x = (x1 - x0) * 0.14 + 0.06
    pad_z = (z1 - z0) * 0.14 + 0.08
    x0 -= pad_x
    x1 += pad_x
    z0 = min(z0 - pad_z, -0.08)
    z1 += pad_z
    cx = (x0 + x1) * 0.5
    cz = (z0 + z1) * 0.5
    # ortho_scale is the vertical span. The portrait frame is narrower, so a
    # wide reach has to enlarge the vertical span until the width fits too.
    aspect = 560.0 / 720.0
    span = max(z1 - z0, (x1 - x0) / aspect)
    cap = arm.location
    return (cx, cap.y - 3.6, cz), (cx, cap.y, cz), span


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
    os.makedirs(PASS13, exist_ok=True)
    prev = "/tmp/pass13_cells"
    os.makedirs(prev, exist_ok=True)
    only = os.environ.get("PASS13_ONLY", TIC)
    spec = next(s for s in sp.CLIPS if s["id"] == only)
    entry = doc["clips"][only]
    data = sp.load_clip(only)
    t0 = data["times_s"][0]
    cleaned = p11._cleaned(data["world_xyz_m"], p11._spike_flags(data["world_xyz_m"]))
    has_wall = spec["verb"] != "softland"
    frames = [i for i in p11.STORY[only] if i < len(entry["keys"])]
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
        feet, hands, clear, hand_clear = p12._contacts(cleaned[i])
        foot_c = min((clear[sd] for sd in feet), default=9.0)
        hand_c = min((hand_clear[sd] for sd in hands), default=9.0)
        palms = bool(hands) and hand_c <= foot_c + 0.01
        cap_txt = "" if palms or only.startswith("10_") else p12._sole_caption(ids, local, feet)
        captions.append(cap_txt)
        p11._show_floor(arm)
        p11._show_shadow(p11._contact_x(arm), arm.location.y)
        if has_wall:
            rh._show_wall(0.0, entry.get("wall_top_m"), arm.location.y, 2.4, side=entry.get("wall_side", "+"))
        else:
            rh._hide_wall()
        loc, look, ortho = _frame_body(arm)
        path = os.path.join(prev, f"{only}_{s}.png")
        rh._shot(path, loc, look, ortho=ortho)
        shots.append(path)
        print(f"ref {only} f={i} ortho={ortho:.2f} {cap_txt}", flush=True)
    cell_w, cell_h = 400, 300
    canvas = Image.new("RGB", (cell_w * 4, cell_h * 2 + 28), (48, 44, 40))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle((0, 0, 1600, 28), fill=(20, 18, 16))
    draw.text((12, 8), "12 tic-tac   pass 13", fill=(245, 236, 220))
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
    out = os.path.join(PASS13, f"ref_{rh.SLUGS[only]}.png")
    canvas.save(out, optimize=True)
    if os.path.getsize(out) > 400_000:
        q = canvas.quantize(colors=128, method=Image.Quantize.MEDIANCUT).convert("RGB")
        q.save(out, optimize=True)
    print("wrote", out, os.path.getsize(out), flush=True)
    with open(ERROR_PATH, "a") as f:
        f.write("STRIP " + only + " " + " | ".join(listed) + "\n")
    rh._reset(arm)
    rh._hide_wall()


if __name__ == "__main__":
    stage = os.environ.get("PASS13_STAGE", "pose")
    if stage == "pose":
        solve()
    elif stage == "stills":
        render_strips()
    else:
        solve()
        render_strips()
