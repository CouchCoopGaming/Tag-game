#!/usr/bin/env python3
"""Capsule-space clips for the slide, the vertical wall run, and the wall-pop 180.

Same rules as pass 5: bone keys only, Root at the origin, capsule path is
preview-only. Clip 19 is step timing, not a pose. Nothing here is wired into
gameplay.
"""
import copy
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import render_hier_pass5 as p5
import render_hier_v080_stills as rh
import storror_pose as sp

OUT = os.path.join(rh.OUT, "pass6")
PREV = os.path.join(rh.PREV, "pass6")
HIER_LEG = p5.HIER_LEG
HIP_Z = p5.HIP_Z

# MediaPipe left/right pairs. Swapping these and negating z undoes a back view
# that the tracker read as facing the camera.
_PAIRS = (
    (1, 2), (3, 4), (5, 6), (7, 8), (9, 10),
    (11, 12), (13, 14), (15, 16), (17, 18), (19, 20), (21, 22),
    (23, 24), (25, 26), (27, 28), (29, 30), (31, 32),
)


def _args():
    if "--" in sys.argv:
        return sys.argv[sys.argv.index("--") + 1:]
    return sys.argv[1:]


def _rel(data):
    t0 = float(data["times_s"][0])
    return [float(t) - t0 for t in data["times_s"]]


def _slice(data, t0, t1):
    rel = _rel(data)
    idx = [i for i, t in enumerate(rel) if t0 - 1.0e-6 <= t <= t1 + 1.0e-6]
    out = dict(data)
    n = data["frame_count"]
    for key, val in data.items():
        if isinstance(val, list) and len(val) == n:
            out[key] = [val[i] for i in idx]
    out["frame_count"] = len(idx)
    return out


def _swap_lr_mirror(data):
    """Swap left/right and mirror depth. Clip 18 is filmed from behind."""
    d = copy.deepcopy(data)
    for key in ("world_xyz_m", "world_xyz_m_raw"):
        if key not in d:
            continue
        for frame in d[key]:
            if not frame or frame[0][0] is None:
                continue
            for a, b in _PAIRS:
                frame[a], frame[b] = frame[b], frame[a]
            for p in frame:
                if p[2] is not None:
                    p[2] = -float(p[2])
    for key in ("visibility", "confidence_mask"):
        if key not in d:
            continue
        for frame in d[key]:
            if not frame:
                continue
            for a, b in _PAIRS:
                frame[a], frame[b] = frame[b], frame[a]
    for ang in d.get("joint_angles_deg") or []:
        if not ang:
            continue
        for key in [k for k in ang if k.endswith("_L")]:
            other = key[:-2] + "_R"
            if other in ang:
                ang[key], ang[other] = ang[other], ang[key]
        for key in ("spine_twist", "spine_lateral_bend", "neck_lateral"):
            if ang.get(key) is not None:
                ang[key] = -float(ang[key])
    return d


def _curves(data):
    frames = data["joint_angles_deg"]
    worlds = data["world_xyz_m"]
    vis = data["visibility"]
    usable = data["usable"]
    n = len(frames)
    series = {k: [None] * n for k in sp.CHANNELS}
    for i in range(n):
        if not usable[i]:
            continue
        ang = frames[i] or {}
        for k in sp.CHANNELS:
            if k in ang and ang[k] is not None and "elev" not in k and "abd_lat" not in k:
                series[k][i] = float(ang[k])
        W = worlds[i]
        v = vis[i]
        if not W or not v:
            continue
        for side in ("L", "R"):
            got = sp.elev_and_lat(W, v, side)
            if not got:
                continue
            series[f"hip_elev_{side}"][i] = got["hip"][0]
            series[f"hip_abd_lat_{side}"][i] = got["hip"][1]
            series[f"shoulder_elev_{side}"][i] = got["shoulder"][0]
            series[f"shoulder_abd_lat_{side}"][i] = got["shoulder"][1]
    return {k: sp._smooth(series[k]) for k in sp.CHANNELS}


def _leg_scale(data):
    """Hier hip-to-ankle over the median MediaPipe thigh+shin."""
    lengths = []
    for W in data["world_xyz_m"]:
        if not W or W[23][0] is None:
            continue
        for hip_i, knee_i, ank_i in ((23, 25, 27), (24, 26, 28)):
            thigh = math.dist(W[hip_i], W[knee_i])
            shin = math.dist(W[knee_i], W[ank_i])
            if thigh > 0.15 and shin > 0.15:
                lengths.append(thigh + shin)
    lengths.sort()
    med = lengths[len(lengths) // 2] if lengths else 0.60
    return HIER_LEG / med, med


def _integrate(data, scale):
    fps = float(data["fps"])
    fwd = 0.0
    rise = 0.0
    raw_f = []
    raw_r = []
    for v in data["root_vel_proxy_mps"]:
        if v:
            fwd += math.hypot(float(v[0]), float(v[1])) / fps * scale
            rise += -float(v[1]) / fps * scale
        raw_f.append(fwd)
        raw_r.append(rise)
    return p5._smooth(raw_f, 4), p5._smooth(raw_r, 4), fps


def _smoothstep(u):
    u = max(0.0, min(1.0, u))
    return u * u * (3.0 - 2.0 * u)


def _path(data, scale, fps, rise):
    n = data["frame_count"]
    return {
        "scale": scale,
        "fps": fps,
        "fwd": [0.0] * n,
        "rise": rise,
        "height": [None if h is None else float(h) * scale for h in data["root_height_above_feet_m"]],
        "frames": n,
    }


def _beats_around(n, hero, extra):
    frames = {0, n - 1, hero}
    for i in extra:
        if 0 <= i < n:
            frames.add(i)
    frames = sorted(frames)
    while len(frames) > 8:
        mid = [i for i in frames[1:-1] if i != hero]
        mid.sort(key=lambda i: -abs(i - hero))
        frames.remove(mid[0])
    return frames


def _slide_spec(raw):
    data = _slice(raw, 0.55, 1.05)
    scale, med = _leg_scale(data)
    travel, _rise, fps = _integrate(data, scale)
    dist = travel[-1] - travel[0]
    # The low-crouch window is half a second. Stretch a short reading so the
    # feet-first travel is visible, and drop the slope's vertical component.
    if dist < 1.05:
        gain = 1.05 / max(dist, 1.0e-3)
        travel = [v * gain for v in travel]
        dist = travel[-1] - travel[0]
    heights = []
    for h in data["root_height_above_feet_m"]:
        heights.append(0.20 if h is None else float(h))
    lo, hi = min(heights), max(heights)
    span = max(hi - lo, 1.0e-4)
    n = data["frame_count"]
    y0 = travel[0]
    caps = []
    hip = []
    for i in range(n):
        # Smallest source height is the deepest crouch. Map it to a low hip
        # on level ground. The slope pitch is not applied.
        u = (heights[i] - lo) / span
        hip_z = 0.42 + 0.12 * u
        hip.append(hip_z)
        # Feet point along -Y. Slide that way.
        caps.append((0.0, -(travel[i] - y0), hip_z - HIP_Z))
    hero = min(range(n), key=lambda i: hip[i])
    rel = _rel(data)
    print(
        f"PLAN slide frames={n} scale={scale:.3f} leg={med:.3f} travel={dist:.2f}m "
        f"hero={hero} t={rel[hero]:.2f} hip={hip[hero]:.2f}",
        flush=True,
    )
    return {
        "id": "17_slide_slope_crouch",
        "verb": "slide",
        "loop": False,
        "data": data,
        "smooth": _curves(data),
        "path": _path(data, scale, fps, [0.0] * n),
        "caps": caps,
        "hero_i": hero,
        "beats": _beats_around(n, hero, [n // 7, 2 * n // 7, 3 * n // 7, 4 * n // 7, 5 * n // 7, 6 * n // 7]),
        "sliding_feet": True,
        "fit": _fit_caps,
        "force": _force_slide,
        "facing_kind": "slide",
        "note": (
            "Feet-first crouch from 0.55-1.05 s, slope flattened onto level ground. "
            "The first 0.45 s is dropped. Feet slide on z=0 with the capsule; "
            "slide_cm is sole skate against that moving contact."
        ),
        "slug": "17_slide",
        "label": "17 slide",
    }


def _period_19(data):
    """Step period from the side view, before the L/R flip at 1.6 s."""
    rel = _rel(data)
    prev = None
    switches = []
    for i, t in enumerate(rel):
        if t > 1.55:
            break
        W = data["world_xyz_m"][i]
        if not W or W[27][0] is None:
            continue
        # Larger image-y is lower in frame: that foot is the support.
        side = "L" if W[27][1] >= W[28][1] else "R"
        if side != prev:
            switches.append(t)
            prev = side
    gaps = [switches[i + 1] - switches[i] for i in range(len(switches) - 1)]
    gaps = [g for g in gaps if g > 0.25]
    gaps.sort()
    period = gaps[len(gaps) // 2] if gaps else 0.55
    print(f"PLAN clip19 step period={period:.2f}s from {len(gaps)} gaps before 1.6s", flush=True)
    return period


def _plant_runs(data, t_lo, t_hi, min_frames, half=0.14):
    """Support foot from the straighter knee, held briefly around each step.

    A long stretch where one knee stays slightly straighter (the top of the
    wall) is not one plant. The pin is the peak of the difference, about a
    quarter-second, which matches clip 19's step period.
    """
    rel = _rel(data)
    ang = data["joint_angles_deg"]
    raw = []
    for i, t in enumerate(rel):
        a = ang[i] or {}
        if t < t_lo or t > t_hi:
            raw.append(None)
            continue
        k_l = float(a.get("knee_flex_L") or 0.0)
        k_r = float(a.get("knee_flex_R") or 0.0)
        if k_l + 10.0 < k_r:
            raw.append("L")
        elif k_r + 10.0 < k_l:
            raw.append("R")
        else:
            raw.append(None)
    runs = []
    i = 0
    n = len(raw)
    while i < n:
        if raw[i] is None:
            i += 1
            continue
        j = i
        while j < n and raw[j] == raw[i]:
            j += 1
        if j - i >= min_frames:
            runs.append((i, j - 1, raw[i]))
        i = j
    pins = []
    for i0, i1, side in runs:
        peak = max(
            range(i0, i1 + 1),
            key=lambda k: abs(
                float((ang[k] or {}).get("knee_flex_L") or 0.0)
                - float((ang[k] or {}).get("knee_flex_R") or 0.0)
            ),
        )
        a = ang[peak] or {}
        if max(float(a.get("knee_flex_L") or 0.0), float(a.get("knee_flex_R") or 0.0)) < 36.0:
            continue
        t_peak = rel[peak]
        a0 = min(range(len(rel)), key=lambda k: abs(rel[k] - (t_peak - half)))
        a1 = min(range(len(rel)), key=lambda k: abs(rel[k] - (t_peak + half)))
        a0 = max(a0, i0)
        a1 = min(max(a1, a0 + 1), i1)
        pins.append((a0, a1, side))
    return pins


def _vwall_spec(raw18, raw19):
    data = _swap_lr_mirror(raw18)
    scale, med = _leg_scale(data)
    _travel, rise, fps = _integrate(data, scale)
    period = _period_19(raw19)
    # Clip 19's period only sets how long a plant is held. Which foot comes
    # from clip 18 after the swap; labels after 1.6 s on clip 19 are ignored.
    half = max(0.12, min(0.18, 0.28 * period))
    runs = _plant_runs(data, 0.32, 1.55, 4, half)
    rel = _rel(data)
    # Rise of the climb, then a gentle cap so each step stays inside a leg.
    i_climb = min(range(len(rel)), key=lambda i: abs(rel[i] - 0.40))
    i_top = min(range(len(rel)), key=lambda i: abs(rel[i] - 1.90))
    span = rise[i_top] - rise[i_climb]
    target_span = span
    if span > 2.70:
        target_span = 2.60
    elif span < 1.70:
        target_span = 2.00
    gain = target_span / span if abs(span) > 1.0e-3 else 1.0
    z_target = [(rise[i] - rise[i_climb]) * gain for i in range(len(rel))]
    for i in range(len(z_target)):
        if z_target[i] < 0.0:
            z_target[i] = 0.0
    z = list(z_target)
    for i0, i1, _side in runs:
        z0 = z_target[i0]
        for i in range(i0, i1 + 1):
            z[i] = z0
    plant = [None] * len(rel)
    for i0, i1, side in runs:
        lock_z = z[i0] + 0.58
        for i in range(i0, i1 + 1):
            plant[i] = (side, lock_z)
    hero = (runs[len(runs) // 2][0] + runs[len(runs) // 2][1]) // 2 if runs else len(rel) // 2
    climb_t = rel[runs[0][0]] if runs else 0.40
    # Top reach: highest shoulder elevation after the climb starts.
    smooth = _curves(data)
    best = i_top
    best_s = -1.0
    for i in range(i_climb, len(rel)):
        score = smooth["shoulder_elev_L"][i] + smooth["shoulder_elev_R"][i]
        if score > best_s:
            best_s = score
            best = i
    beats = [0, max(0, (runs[0][0] - 4) if runs else 6)]
    for i0, i1, _side in runs:
        beats.append((i0 + i1) // 2)
    if runs:
        beats.append((runs[-1][1] + best) // 2)
    beats.extend((best, len(rel) - 1, hero))
    print(
        f"PLAN vwall frames={len(rel)} scale={scale:.3f} leg={med:.3f} "
        f"rise={span:.2f}->{target_span:.2f} plants={[(rel[a], rel[b], s) for a, b, s in runs]} "
        f"hero={hero} reach={best}",
        flush=True,
    )
    return {
        "id": "18_vertical_wallrun_back",
        "verb": "vwall",
        "loop": False,
        "data": data,
        "smooth": smooth,
        "path": _path(data, scale, fps, z),
        "z": z,
        "plant": plant,
        "climb_t": climb_t,
        "hero_i": hero,
        "beats": _beats_around(len(rel), hero, beats),
        "fit": _fit_vwall,
        "force": _force_vwall,
        "facing_kind": "vwall",
        "wall_top_pad": 1.9,
        "note": (
            "Vertical wall run from clip 18 after swapping L/R and mirroring depth. "
            "Step rhythm from clip 19 before the 1.6 s label flip; which foot plants "
            "follows clip 18's swapped knee. Clip 19 is not keyed."
        ),
        "slug": "18_vwall",
        "label": "18 vertical wall run",
    }


def _plant_side_20(data):
    rel = _rel(data)
    best = -1.0
    side = "R"
    for i, t in enumerate(rel):
        if not (0.40 <= t <= 0.75):
            continue
        W = data["world_xyz_m"][i]
        if not W or W[27][0] is None:
            continue
        hip_x = 0.5 * (W[23][0] + W[24][0])
        for name, idx in (("L", 27), ("R", 28)):
            reach = abs(W[idx][0] - hip_x)
            if reach > best:
                best = reach
                side = name
    return side


def _turn_spec(raw):
    data = raw
    scale, med = _leg_scale(data)
    _travel, _rise, fps = _integrate(data, scale)
    rel = _rel(data)
    side = _plant_side_20(data)
    n = len(rel)

    def yaw_at(i):
        t = rel[i]
        # The fisheye pans, so this is a smooth half-turn, not the image yaw.
        if t <= 0.85:
            return 0.0
        if t >= 1.35:
            return -180.0
        return -180.0 * _smoothstep((t - 0.85) / 0.50)

    hero = min(range(n), key=lambda i: abs(rel[i] - 0.55))
    extra = [
        min(range(n), key=lambda i: abs(rel[i] - t))
        for t in (0.15, 0.70, 0.95, 1.15, 1.45, 1.70)
    ]
    print(
        f"PLAN turn frames={n} scale={scale:.3f} leg={med:.3f} plant={side} "
        f"hero={hero} t={rel[hero]:.2f}",
        flush=True,
    )
    return {
        "id": "20_wallpop_180",
        "verb": "turn180",
        "loop": False,
        "data": data,
        "smooth": _curves(data),
        "path": _path(data, scale, fps, [0.0] * n),
        "rel": rel,
        "plant_side": side,
        "plant_z": 0.70,
        "hero_i": hero,
        "beats": _beats_around(n, hero, extra),
        "hip_yaw": yaw_at,
        "fit": _fit_turn,
        "force": _force_turn,
        "facing_kind": "turn",
        "note": (
            "Foot plant, pop, and an approximate 180 degree hips yaw over 0.85-1.35 s. "
            "The fisheye camera pans, so the angle is not the measured image yaw. "
            "The yaw is a Hips bone key, not Root motion. Landing is a crouch on the floor."
        ),
        "slug": "20_turn180",
        "label": "20 wall-pop 180",
    }


def _fit_caps(spec, path, hero_i, loc):
    from mathutils import Vector
    caps = [Vector(c) for c in spec["caps"]]
    return caps, None, None, {}


def _force_slide(spec, key, i, hero_i, pt, face, lip_z, meta):
    from mathutils import Vector
    if key[0] != "foot":
        return None, None
    sole = p5._foot_point(p5._token("foot", key[1]), "floor") or pt
    return Vector((sole.x, sole.y, 0.0)), "floor"


def _fit_vwall(spec, path, hero_i, loc):
    from mathutils import Vector
    runs_side = None
    for rec in spec["plant"]:
        if rec is not None:
            runs_side = rec[0]
            break
    side = runs_side or "L"
    foot = loc["foot"].get(side) or loc["foot"]["L"]
    chest = loc["chest"] if loc["chest"] is not None else (foot.x + 0.2)
    face = 0.0
    contact = max(face - foot.x, face + 0.08 - chest)
    rel = _rel(spec["data"])
    caps = []
    for i, t in enumerate(rel):
        approach = 0.0
        if t < spec["climb_t"]:
            approach = 1.20 * (1.0 - _smoothstep(t / spec["climb_t"]))
        caps.append(Vector((contact + approach, 0.0, spec["z"][i])))
    return caps, face, None, {"plant": spec["plant"], "rel": rel, "climb_t": spec["climb_t"]}


def _force_vwall(spec, key, i, hero_i, pt, face, lip_z, meta):
    from mathutils import Vector
    if key[0] != "foot":
        return None, None
    t = meta["rel"][i]
    if t < meta["climb_t"]:
        sole = p5._foot_point(p5._token("foot", key[1]), "floor") or pt
        return Vector((sole.x, sole.y, 0.0)), "floor"
    rec = meta["plant"][i]
    if rec is not None and key[1] == rec[0]:
        return Vector((face, pt.y, rec[1])), "wall"
    return None, None


def _fit_turn(spec, path, hero_i, loc):
    from mathutils import Vector
    side = spec["plant_side"]
    foot = loc["foot"].get(side) or loc["foot"]["R"]
    chest = loc["chest"] if loc["chest"] is not None else (foot.x + 0.25)
    face = 0.0
    contact = max(face - foot.x, face + 0.10 - chest)
    rel = spec["rel"]
    caps = []
    for t in rel:
        if t < 0.42:
            x = contact + 1.35 * (1.0 - _smoothstep(t / 0.42))
            z = 0.0
        elif t < 0.78:
            x = contact
            z = 0.0
        elif t < 1.25:
            u = _smoothstep((t - 0.78) / 0.47)
            x = contact + 1.20 * u
            # A short pop, then back down into the crouch.
            if t < 1.05:
                z = 0.20 * _smoothstep((t - 0.78) / 0.27)
            else:
                z = 0.20 * (1.0 - _smoothstep((t - 1.05) / 0.30))
        else:
            x = contact + 1.20
            z = 0.0
        caps.append(Vector((x, 0.0, z)))
    return caps, face, None, {
        "rel": rel,
        "side": side,
        "plant_z": spec["plant_z"],
        "contact": contact,
    }


def _force_turn(spec, key, i, hero_i, pt, face, lip_z, meta):
    from mathutils import Vector
    if key[0] != "foot":
        return None, None
    t = meta["rel"][i]
    if 0.42 <= t <= 0.78 and key[1] == meta["side"]:
        return Vector((face, pt.y, meta["plant_z"])), "wall"
    if t >= 1.30:
        sole = p5._foot_point(p5._token("foot", key[1]), "floor") or pt
        return Vector((sole.x, sole.y, 0.0)), "floor"
    return None, None


def _facing(spec):
    from mathutils import Euler
    kind = spec["facing_kind"]
    if kind == "slide":
        # Slight recline on level ground. The slope lean is not used.
        return Euler((math.radians(-14.0), 0.0, 0.0), "XYZ")
    if kind == "vwall":
        # Chest toward the wall (-X), a little pitch into the face.
        return Euler((math.radians(12.0), 0.0, math.radians(-85.0)), "XYZ")
    return Euler((math.radians(8.0), 0.0, math.radians(-80.0)), "XYZ")


def _shot(arm, spec, ortho):
    cap = arm.location
    if spec["verb"] == "slide":
        focus_z = cap.z + 0.72
        # Feet lead along -Y. Aim a little that way and stay wide enough
        # that the extended leg is inside the frame.
        if ortho:
            return (cap.x + 3.6, cap.y, focus_z), (cap.x, cap.y - 0.55, focus_z), 4.80
        return (cap.x + 5.0, cap.y - 6.6, focus_z + 0.1), (cap.x, cap.y - 0.55, focus_z), None
    if spec["verb"] == "vwall":
        focus_z = cap.z + 1.05
        if ortho:
            return (cap.x, cap.y - 3.6, focus_z), (cap.x, cap.y, focus_z), 3.15
        return (cap.x + 2.8, cap.y - 4.0, focus_z + 0.1), (cap.x - 0.05, cap.y, focus_z - 0.05), None
    focus_z = cap.z + 0.85
    if ortho:
        return (cap.x, cap.y - 3.6, focus_z), (cap.x, cap.y, focus_z), 2.70
    return (cap.x + 2.7, cap.y - 3.9, focus_z + 0.12), (cap.x - 0.05, cap.y, focus_z - 0.05), None


def _show_marks(solved, i, face):
    rh._hide_marks()
    info = solved["plants"][i]
    for side in ("L", "R"):
        for kind_name in ("foot", "hand"):
            rec = info.get(kind_name + side)
            if not rec or rec[0] < 0.5:
                continue
            _w, kind, lock = rec
            if kind == "wall" and face is not None:
                rh._show_foot_mark(face, lock[1], lock[2], side)
            elif kind == "floor":
                rh._show_foot_mark(lock[0], lock[1], 0.02, side)


def _knee_forward(arm):
    """Horizontal direction the knees point, so a turn can be checked."""
    from mathutils import Vector
    hip = p5._head_w(arm, "Hips")
    acc = Vector((0.0, 0.0, 0.0))
    n = 0
    for name in ("LowerLeg_L", "LowerLeg_R"):
        knee = p5._head_w(arm, name)
        acc += knee - hip
        n += 1
    acc /= max(n, 1)
    return acc.x, acc.y


def build_specs():
    raw17 = sp.load_clip("17_slide_slope_crouch")
    raw18 = sp.load_clip("18_vertical_wallrun_back")
    raw19 = sp.load_clip("19_vertical_wallrun_side")
    raw20 = sp.load_clip("20_wallpop_180")
    return [_slide_spec(raw17), _vwall_spec(raw18, raw19), _turn_spec(raw20)]


def render_pass6(do_render=True):
    import bpy
    os.makedirs(PREV, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    rh._prepare_scene()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 10
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 1200
    doc = {
        "root_motion": False,
        "note": (
            "Bone rotation keys are in capsule space. The armature and Root stay "
            "at the origin in the clip. capsule_preview_m is only the preview path. "
            "The 180 is an approximate Hips yaw, not a root-motion key."
        ),
        "clips": {},
    }
    summary = []
    solved_all = {}
    only = {s for s in os.environ.get("PASS6_ONLY", "").split(",") if s}
    for spec in build_specs():
        if only and spec["id"] not in only:
            continue
        facing = _facing(spec)
        solved = p5.solve_clip(arm, spec, spec["smooth"], facing, spec["path"])
        solved["facing"] = facing
        solved["spec"] = spec
        solved_all[spec["id"]] = solved
        slides = [v * 100.0 for v in solved["slide"].values()]
        slide_cm = max(slides) if slides else 0.0
        pen_cm = solved["pen"] * 100.0
        torso_cm = solved["torso_clear"] * 100.0 if solved["face"] is not None else None
        head_cm = solved["head_clear"] * 100.0 if solved["face"] is not None else None
        line = (
            f"PASS6 id={spec['id']} slide_cm={slide_cm:.2f} pen_cm={pen_cm:.2f} "
            f"torso_clear_cm={torso_cm if torso_cm is not None else float('nan'):.2f} "
            f"head_clear_cm={head_cm if head_cm is not None else float('nan'):.2f} "
            f"face={solved['face']} hero={solved['hero_i']}"
        )
        print(line, flush=True)
        summary.append(line)
        # Direction of the knees at the story frames, for the turn check.
        beats = p5._beats(spec, len(solved["keys"]), solved["hero_i"])
        for i in beats:
            eulers = solved["keys"][i]
            rh._apply_eulers(arm, {k: v for k, v in eulers.items()})
            arm.location = solved["capsule"][i]
            arm.rotation_euler = facing
            root = arm.pose.bones.get("Root")
            if root is not None:
                root.location = (0.0, 0.0, 0.0)
                root.rotation_euler = (0.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            kx, ky = _knee_forward(arm)
            fl = p5._foot_point("Foot_L", "floor")
            fr = p5._foot_point("Foot_R", "floor")
            print(
                f"POSE {spec['id']} f={i} knee=({kx:.2f},{ky:.2f}) "
                f"soleL={None if fl is None else round(fl.z, 3)} "
                f"soleR={None if fr is None else round(fr.z, 3)} "
                f"cap=({arm.location.x:.2f},{arm.location.y:.2f},{arm.location.z:.2f})",
                flush=True,
            )
        fps = solved["fps"]
        keys_out = []
        caps_out = []
        yaw_out = []
        for i, eulers in enumerate(solved["keys"]):
            bones = {name: [round(a, 2) for a in deg] for name, deg in eulers.items()}
            keys_out.append({"frame": i, "t": round(i / fps, 4), "bones": bones})
            c = solved["capsule"][i]
            caps_out.append([round(c.x, 4), round(c.y, 4), round(c.z, 4)])
            if spec.get("hip_yaw"):
                yaw_out.append(round(float(spec["hip_yaw"](i)), 2))
        entry = {
            "fps": fps,
            "frames": spec["path"]["frames"],
            "hero_frame": solved["hero_i"],
            "foot_slide_cm": round(slide_cm, 2),
            "penetration_cm": round(pen_cm, 2),
            "torso_clearance_cm": None if torso_cm is None else round(torso_cm, 2),
            "head_clearance_cm": None if head_cm is None else round(head_cm, 2),
            "root_location_keys": False,
            "capsule_preview_m": caps_out,
            "note": spec["note"],
            "keys": keys_out,
        }
        if yaw_out:
            entry["hips_yaw_preview_deg"] = yaw_out
            entry["turn_angle"] = "approximate"
        doc["clips"][spec["id"]] = entry
        rh._reset(arm)
    out_path = os.path.join(OUT, "keyed_clips.json")
    if only and os.path.isfile(out_path):
        prev = json.load(open(out_path))
        prev_clips = prev.get("clips") or {}
        prev_clips.update(doc["clips"])
        doc["clips"] = prev_clips
    with open(out_path, "w") as f:
        json.dump(doc, f)
    print("wrote", os.path.join(OUT, "keyed_clips.json"), flush=True)
    if not do_render:
        for line in summary:
            print(line, flush=True)
        return solved_all
    for spec in build_specs():
        if spec["id"] not in solved_all:
            continue
        solved = solved_all[spec["id"]]
        beats = p5._beats(spec, len(solved["keys"]), solved["hero_i"])
        facing = solved["facing"]
        slug = spec["slug"]
        zs = [c.z for c in solved["capsule"]]
        wall_top = (max(zs) + spec.get("wall_top_pad", 1.6)) if solved["face"] is not None else None
        for s, i in enumerate(beats):
            eulers = solved["keys"][i]
            rh._apply_eulers(arm, {k: v for k, v in eulers.items()})
            arm.location = solved["capsule"][i]
            arm.rotation_euler = facing
            root = arm.pose.bones.get("Root")
            if root is not None:
                root.location = (0.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            if solved["face"] is None:
                rh._hide_wall()
            else:
                rh._show_wall(solved["face"], wall_top, arm.location.y, 2.4)
            _show_marks(solved, i, solved["face"])
            loc, look, _ortho = _shot(arm, spec, False)
            rh._shot(os.path.join(PREV, f"{slug}_{s}.png"), loc, look)
            loc, look, ortho = _shot(arm, spec, True)
            rh._shot(os.path.join(PREV, f"{slug}_{s}_side.png"), loc, look, ortho=ortho)
            print(f"pass6 {slug} {s} frame={i}", flush=True)
        rh._reset(arm)
    for line in summary:
        print(line, flush=True)
    return solved_all


def composite_pass6():
    from PIL import Image, ImageDraw, ImageFont
    os.makedirs(OUT, exist_ok=True)
    font = ImageFont.load_default()
    labels = {
        "17_slide": "17 slide",
        "18_vwall": "18 vertical wall run",
        "20_turn180": "20 wall-pop 180",
    }
    for slug, label in labels.items():
        for suffix, tag in (("", "3/4"), ("_side", "side")):
            frames = []
            for i in range(8):
                frames.append(Image.open(os.path.join(PREV, f"{slug}_{i}{suffix}.png")).convert("RGB"))
            w, h = frames[0].size
            gap = 8
            canvas = Image.new("RGB", (w * 8 + gap * 7, h), (48, 44, 40))
            for i, im in enumerate(frames):
                canvas.paste(im, (i * (w + gap), 0))
            d = ImageDraw.Draw(canvas)
            text = label + "  " + tag
            d.rectangle((12, 12, 16 + 8 * len(text) + 12, 36), fill=(20, 18, 16))
            d.text((20, 16), text, fill=(245, 236, 220), font=font)
            path = os.path.join(OUT, f"strip_{slug}{suffix}.png")
            canvas.save(path)
            print("wrote", path)


if __name__ == "__main__":
    args = _args()
    if "--plan" in args and "bpy" not in sys.modules:
        build_specs()
    elif "--composite" in args and "bpy" not in sys.modules:
        composite_pass6()
    elif "--metrics" in args:
        render_pass6(do_render=False)
    else:
        render_pass6(do_render=True)
