#!/usr/bin/env python3
"""Joint-angle curves from the five clean Storror clips.

Uses hip/shoulder elevation and lateral abduction (not raw abduction) for
raised limbs, matching Docs/Storror/tools/proportions.py. Curves are smoothed
and resampled. Nothing here is root motion.
"""
import json
import math
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Storror"))
JSON_DIR = os.path.join(ROOT, "out", "json")

# Grade A clips. verb is the gameplay pose they inform.
CLIPS = (
    {"id": "03_wall_drop_softland", "verb": "softland", "loop": False},
    {"id": "04_window_drop_softland", "verb": "softland", "loop": False},
    {"id": "10_wallrun_slanted", "verb": "wallrun", "loop": True},
    {"id": "12_tictac_slanted_wall", "verb": "walljump", "loop": False},
    {"id": "15_cat_leap_wall", "verb": "cling", "loop": True},
)

# Bind offsets of the v0.8.0 A-pose, degrees. Pose eulers are relative to this.
BIND_ELEV = 26.0
BIND_ABD = 24.0
BIND_HIP = 12.0
BIND_KNEE = 12.0
BIND_ELBOW = 15.0
BIND_ANKLE = -18.0
BIND_NECK = 22.0

CHANNELS = (
    "hip_flex_L", "hip_flex_R",
    "hip_elev_L", "hip_elev_R",
    "hip_abd_lat_L", "hip_abd_lat_R",
    "knee_flex_L", "knee_flex_R",
    "ankle_dorsi_L", "ankle_dorsi_R",
    "shoulder_flex_L", "shoulder_flex_R",
    "shoulder_elev_L", "shoulder_elev_R",
    "shoulder_abd_lat_L", "shoulder_abd_lat_R",
    "elbow_flex_L", "elbow_flex_R",
    "spine_twist", "spine_lateral_bend", "neck_flex",
)


def _sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def _add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def _mul(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def _dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _cross(a, b):
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def _norm(a):
    n = math.sqrt(_dot(a, a))
    if n < 1e-9:
        return (0.0, 0.0, 0.0), 0.0
    return (a[0] / n, a[1] / n, a[2] / n), n


def _mid(W, i, j):
    return (
        0.5 * (W[i][0] + W[j][0]),
        0.5 * (W[i][1] + W[j][1]),
        0.5 * (W[i][2] + W[j][2]),
    )


def _pt(W, i):
    p = W[i]
    return (float(p[0]), float(p[1]), float(p[2]))


def elev_and_lat(W, vis, side):
    """Elevation and lateral abduction for one side. None if the limb is hidden."""
    if side == "L":
        s, e, h, k = 11, 13, 23, 25
    else:
        s, e, h, k = 12, 14, 24, 26
    if vis[s] < 0.5 or vis[e] < 0.5 or vis[h] < 0.5 or vis[k] < 0.5:
        return None
    hipm = _mid(W, 23, 24)
    shm = _mid(W, 11, 12)
    up, upn = _norm(_sub(shm, hipm))
    if upn < 1e-6:
        return None
    right = _sub(_pt(W, 24), _pt(W, 23))
    right = _sub(right, _mul(up, _dot(right, up)))
    right, rn = _norm(right)
    if rn < 1e-6:
        return None
    out = _mul(right, -1.0) if side == "L" else right
    out_h = {}
    for nm, a, b in (("hip", h, k), ("shoulder", s, e)):
        u, un = _norm(_sub(_pt(W, b), _pt(W, a)))
        if un < 1e-6:
            return None
        elev = math.degrees(math.acos(max(-1.0, min(1.0, -_dot(u, up)))))
        lat = math.degrees(math.asin(max(-1.0, min(1.0, _dot(u, out)))))
        out_h[nm] = (elev, lat)
    return out_h


def _smooth(vals, radius=2):
    """Moving average. None samples are skipped, then filled by neighbours."""
    n = len(vals)
    acc = [None] * n
    for i in range(n):
        s = 0.0
        c = 0
        for j in range(max(0, i - radius), min(n, i + radius + 1)):
            if vals[j] is None:
                continue
            s += vals[j]
            c += 1
        if c:
            acc[i] = s / c
    # Fill remaining gaps so a curve can be sampled.
    last = None
    for i in range(n):
        if acc[i] is None and last is not None:
            acc[i] = last
        elif acc[i] is not None:
            last = acc[i]
    nxt = None
    for i in range(n - 1, -1, -1):
        if acc[i] is None and nxt is not None:
            acc[i] = nxt
        elif acc[i] is not None:
            nxt = acc[i]
    for i in range(n):
        if acc[i] is None:
            acc[i] = 0.0
    return acc


def _resample(vals, count):
    n = len(vals)
    if n == 0:
        return [0.0] * count
    if count == 1:
        return [vals[n // 2]]
    out = []
    for i in range(count):
        t = i / (count - 1) * (n - 1)
        a = int(t)
        b = min(n - 1, a + 1)
        u = t - a
        out.append(vals[a] * (1.0 - u) + vals[b] * u)
    return out


def load_clip(clip_id):
    path = os.path.join(JSON_DIR, clip_id + ".json")
    with open(path, "r") as f:
        return json.load(f)


def clip_curves(clip_id, keys=24):
    """Smoothed, resampled channels. Also returns the source frame count and fps."""
    data = load_clip(clip_id)
    frames = data["joint_angles_deg"]
    worlds = data["world_xyz_m"]
    vis = data["visibility"]
    usable = data["usable"]
    n = len(frames)
    series = {k: [None] * n for k in CHANNELS}
    for i in range(n):
        if not usable[i]:
            continue
        ang = frames[i] or {}
        for k in CHANNELS:
            if k in ang and ang[k] is not None and "elev" not in k and "abd_lat" not in k:
                series[k][i] = float(ang[k])
        W = worlds[i]
        v = vis[i]
        if W is None or v is None:
            continue
        for side in ("L", "R"):
            got = elev_and_lat(W, v, side)
            if not got:
                continue
            series[f"hip_elev_{side}"][i] = got["hip"][0]
            series[f"hip_abd_lat_{side}"][i] = got["hip"][1]
            series[f"shoulder_elev_{side}"][i] = got["shoulder"][0]
            series[f"shoulder_abd_lat_{side}"][i] = got["shoulder"][1]
    smooth = {k: _smooth(series[k]) for k in CHANNELS}
    sampled = {k: _resample(smooth[k], keys) for k in CHANNELS}
    return {
        "id": clip_id,
        "fps": float(data.get("fps") or 30.0),
        "frames": n,
        "keys": keys,
        "curves": sampled,
        "raw_smooth": smooth,
    }


def sample_raw(smooth, u):
    """u in 0..1 across the source frames."""
    n = len(next(iter(smooth.values())))
    if n <= 1:
        return {k: smooth[k][0] for k in smooth}
    t = max(0.0, min(1.0, u)) * (n - 1)
    a = int(t)
    b = min(n - 1, a + 1)
    w = t - a
    return {k: smooth[k][a] * (1.0 - w) + smooth[k][b] * w for k in smooth}


def hero_u(clip_id, smooth):
    """One readable frame: the deepest crouch, the tuck, or a mid stride."""
    n = len(smooth["knee_flex_L"])
    best_i = n // 2
    best = -1e9
    if clip_id.startswith("03") or clip_id.startswith("04"):
        for i in range(n):
            score = smooth["knee_flex_L"][i] + smooth["knee_flex_R"][i]
            if score > best:
                best = score
                best_i = i
    elif clip_id.startswith("12"):
        # One thigh kicked up, the other still down, root high. That is the
        # plant at hip height. A later trunk-lean search picked a tuck where
        # both feet had already dropped.
        raw = load_clip(clip_id)
        heights = raw["root_height_above_feet_m"]
        for i in range(n):
            rh = heights[i] if i < len(heights) else None
            if rh is None or rh < 0.65:
                continue
            flex_l = smooth["hip_flex_L"][i]
            flex_r = smooth["hip_flex_R"][i]
            thigh_l = smooth["hip_elev_L"][i] if flex_l >= 0.0 else -smooth["hip_elev_L"][i]
            thigh_r = smooth["hip_elev_R"][i] if flex_r >= 0.0 else -smooth["hip_elev_R"][i]
            hi = thigh_l if thigh_l >= thigh_r else thigh_r
            lo = thigh_r if thigh_l >= thigh_r else thigh_l
            if hi < 70.0 or lo > 25.0:
                continue
            score = (hi - lo) + 30.0 * rh - 0.05 * i
            if score > best:
                best = score
                best_i = i
    elif clip_id.startswith("15"):
        # Hands up with the elbows and both knees bent: the cling on the wall,
        # not the straight-arm reach that spears the plane.
        for i in range(n):
            elbow_lo = min(smooth["elbow_flex_L"][i], smooth["elbow_flex_R"][i])
            knee_lo = min(smooth["knee_flex_L"][i], smooth["knee_flex_R"][i])
            elev = smooth["shoulder_elev_L"][i] + smooth["shoulder_elev_R"][i]
            if elbow_lo < 50.0 or knee_lo < 55.0 or elev < 160.0:
                continue
            score = elbow_lo + knee_lo + 0.1 * elev
            if score > best:
                best = score
                best_i = i
    elif clip_id.startswith("10"):
        # A stride: knees differ, not the two ends.
        for i in range(n // 5, (4 * n) // 5):
            score = abs(smooth["knee_flex_L"][i] - smooth["knee_flex_R"][i])
            if score > best:
                best = score
                best_i = i
    if n <= 1:
        return 0.0, best_i
    return best_i / (n - 1), best_i


def _signed_elev(elev, flex):
    return elev if flex >= 0.0 else -elev


def unity_pose(ch):
    """Bone degrees in the locomotor convention. +thigh is forward, knee flex is negative."""
    def g(name):
        return float(ch.get(name, 0.0))

    def thigh(side):
        signed = _signed_elev(g(f"hip_elev_{side}"), g(f"hip_flex_{side}"))
        return signed - BIND_HIP

    def arm_pitch(side):
        signed = _signed_elev(g(f"shoulder_elev_{side}"), g(f"shoulder_flex_{side}"))
        return -(signed - BIND_ELEV)

    pose = {
        "thigh_l": thigh("L"),
        "thigh_r": thigh("R"),
        "thigh_yaw_l": g("hip_abd_lat_L"),
        "thigh_yaw_r": -g("hip_abd_lat_R"),
        "knee_l": -(g("knee_flex_L") - BIND_KNEE),
        "knee_r": -(g("knee_flex_R") - BIND_KNEE),
        "foot_l": g("ankle_dorsi_L") - BIND_ANKLE,
        "foot_r": g("ankle_dorsi_R") - BIND_ANKLE,
        "arm_pitch_l": arm_pitch("L"),
        "arm_pitch_r": arm_pitch("R"),
        "arm_yaw_l": g("shoulder_abd_lat_L") - BIND_ABD,
        "arm_yaw_r": -(g("shoulder_abd_lat_R") - BIND_ABD),
        "elbow_l": -(g("elbow_flex_L") - BIND_ELBOW),
        "elbow_r": -(g("elbow_flex_R") - BIND_ELBOW),
        "spine_yaw": g("spine_twist") * 0.65,
        "spine_roll": g("spine_lateral_bend") * 0.45,
        "head": (g("neck_flex") - BIND_NECK) * 0.35,
    }
    return pose


def blender_euler(pose):
    """Same pose on the build-script armature. Leg X is opposite Unity."""
    return {
        "UpperLeg_L": (-pose["thigh_l"], 0.0, pose["thigh_yaw_l"]),
        "UpperLeg_R": (-pose["thigh_r"], 0.0, pose["thigh_yaw_r"]),
        "LowerLeg_L": (-pose["knee_l"], 0.0, 0.0),
        "LowerLeg_R": (-pose["knee_r"], 0.0, 0.0),
        "Foot_L": (-pose["foot_l"], 0.0, 0.0),
        "Foot_R": (-pose["foot_r"], 0.0, 0.0),
        "UpperArm_L": (pose["arm_pitch_l"], 0.0, pose["arm_yaw_l"]),
        "UpperArm_R": (pose["arm_pitch_r"], 0.0, pose["arm_yaw_r"]),
        "LowerArm_L": (pose["elbow_l"], 0.0, 0.0),
        "LowerArm_R": (pose["elbow_r"], 0.0, 0.0),
        "Spine": (0.0, pose["spine_yaw"], pose["spine_roll"]),
        "Head": (pose["head"], 0.0, 0.0),
    }
