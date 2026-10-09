#!/usr/bin/env python3
"""Pass 17: four original hand-keyed emotes.

The poses are authored on the Hier. Commons clips are not copied.
Hips local Y is the pelvis drop and the hop. The armature stays at the origin.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Euler
from PIL import Image, ImageDraw

import noclip_proof as proof
import pass10_pose as p10
import pass12_pose as p12
import pass14_pose as p14
import render_hier_v080_stills as rh

OUT = os.path.abspath(os.path.join(HERE, "..", "..", "Docs", "Movement", "emotes", "pass17"))
SHIP = "SHIPPABLE (own work, CC0)"
# Sole sample median on a plant, and the deepest foot vertex.
SOLE_MED = 0.0035
SOLE_MIN = -0.0020
FPS = 30.0

# Athletic crouch. UpperLeg X negative folds the hip. Z negative on the
# left turns that thigh out, which keeps it off the spine.
PLANT = {
    "UpperLeg_L": (-40.0, 0.0, -6.0),
    "UpperLeg_R": (-40.0, 0.0, 6.0),
    "LowerLeg_L": (52.0, 0.0, 0.0),
    "LowerLeg_R": (52.0, 0.0, 0.0),
    "Foot_L": (-12.0, 0.0, -4.0),
    "Foot_R": (-12.0, 0.0, 4.0),
    "Head": (-4.0, 0.0, 0.0),
}
# Arms a little out, clear of the ribs, before a gesture starts.
ARMS_EASY = {
    "UpperArm_L": (22.0, 0.0, -42.0),
    "UpperArm_R": (22.0, 0.0, 42.0),
    "LowerArm_L": (-18.0, 0.0, 0.0),
    "LowerArm_R": (-18.0, 0.0, 0.0),
}
V_UP = {
    "UpperArm_L": (-8.0, 0.0, -118.0),
    "UpperArm_R": (-8.0, 0.0, 118.0),
    "LowerArm_L": (-8.0, 0.0, 0.0),
    "LowerArm_R": (-8.0, 0.0, 0.0),
    "Head": (-8.0, 0.0, 0.0),
}
# Elbow stays at local X -82, which is 90 degrees on this rig.
# The pump swings the upper arm. The other arm stays slightly out.
PUMP_LOW = {
    "UpperArm_R": (48.0, 0.0, 28.0),
    "LowerArm_R": (-82.0, 0.0, 0.0),
    "UpperArm_L": (18.0, 0.0, -32.0),
    "LowerArm_L": (-22.0, 0.0, 0.0),
}
PUMP_HIGH = {
    "UpperArm_R": (40.0, 0.0, 100.0),
    "LowerArm_R": (-82.0, 0.0, 0.0),
    "UpperArm_L": (18.0, 0.0, -32.0),
    "LowerArm_L": (-22.0, 0.0, 0.0),
}
# Elbows wide and up, hands back in. Wider than a run at 240 px.
SHRUG = {
    "Shoulder_L": (32.0, 0.0, 12.0),
    "Shoulder_R": (32.0, 0.0, -12.0),
    "UpperArm_L": (6.0, 0.0, -72.0),
    "UpperArm_R": (6.0, 0.0, 72.0),
    "LowerArm_L": (-78.0, 0.0, 0.0),
    "LowerArm_R": (-78.0, 0.0, 0.0),
    "Head": (10.0, 0.0, 0.0),
}
# Free leg steps out. Support leg takes a little more hip flex.
STEP_L = {
    "UpperLeg_L": (-36.0, 0.0, -38.0),
    "LowerLeg_L": (70.0, 0.0, 0.0),
    "Foot_L": (-4.0, 0.0, 0.0),
    "UpperLeg_R": (-42.0, 0.0, 8.0),
    "LowerLeg_R": (54.0, 0.0, 0.0),
    "Foot_R": (-12.0, 0.0, 4.0),
    "Shoulder_L": (18.0, 0.0, 8.0),
    "UpperArm_L": (8.0, 0.0, -58.0),
    "LowerArm_L": (-28.0, 0.0, 0.0),
    "UpperArm_R": (16.0, 0.0, 28.0),
    "LowerArm_R": (-20.0, 0.0, 0.0),
}
STEP_R = {
    "UpperLeg_R": (-36.0, 0.0, 38.0),
    "LowerLeg_R": (70.0, 0.0, 0.0),
    "Foot_R": (-4.0, 0.0, 0.0),
    "UpperLeg_L": (-42.0, 0.0, -8.0),
    "LowerLeg_L": (54.0, 0.0, 0.0),
    "Foot_L": (-12.0, 0.0, -4.0),
    "Shoulder_R": (18.0, 0.0, -8.0),
    "UpperArm_R": (8.0, 0.0, 58.0),
    "LowerArm_R": (-28.0, 0.0, 0.0),
    "UpperArm_L": (16.0, 0.0, -28.0),
    "LowerArm_L": (-20.0, 0.0, 0.0),
}


def _smooth(u):
    u = 0.0 if u < 0.0 else (1.0 if u > 1.0 else u)
    return u * u * (3.0 - 2.0 * u)


def _lerp(a, b, u):
    return a + (b - a) * u


def _lerp3(a, b, u):
    return [round(_lerp(a[i], b[i], u), 2) for i in range(3)]


def _blend(a, b, u):
    out = {}
    for name in set(a) | set(b):
        out[name] = _lerp3(a.get(name, (0.0, 0.0, 0.0)), b.get(name, (0.0, 0.0, 0.0)), u)
    return out


def _bump(t, center, width):
    d = abs(t - center)
    if d >= width:
        return 0.0
    return 0.5 * (1.0 + math.cos(math.pi * d / width))


def _times(duration):
    n = int(round(duration * FPS))
    return [round(i / FPS, 4) for i in range(n + 1)]


def _apply(arm, bones, hy):
    p10._write(arm, bones)
    arm.location = (0.0, 0.0, 0.0)
    arm.rotation_euler = (0.0, 0.0, 0.0)
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
    arm.pose.bones["Hips"].location = (0.0, float(hy), 0.0)
    bpy.context.view_layer.update()


def _mesh_min(side):
    obj = bpy.data.objects[f"Mesh_Foot_{side}"]
    mw = obj.matrix_world
    return min((mw @ v.co).z for v in obj.data.vertices)


def _seat(arm, bones, ids, local):
    """Drop Hips local Y until the lower sole is on the floor, without a deep bury."""
    _apply(arm, bones, 0.0)
    meds = [p12._sole_z(ids, local, side)[1] for side in ("L", "R")]
    hy = -(min(meds) - SOLE_MED)
    _apply(arm, bones, hy)
    buried = min(_mesh_min("L"), _mesh_min("R"))
    if buried < SOLE_MIN:
        hy += SOLE_MIN - buried
        _apply(arm, bones, hy)
    return hy


def _pose_v(t):
    bones = dict(PLANT)
    # Out first, then up, so the hands do not travel through the head.
    if t < 0.10:
        u = _smooth(t / 0.10)
        bones.update(_blend(ARMS_EASY, {
            "UpperArm_L": (4.0, 0.0, -78.0),
            "UpperArm_R": (4.0, 0.0, 78.0),
            "LowerArm_L": (-12.0, 0.0, 0.0),
            "LowerArm_R": (-12.0, 0.0, 0.0),
        }, u))
    elif t < 0.20:
        u = _smooth((t - 0.10) / 0.10)
        mid = {
            "UpperArm_L": (4.0, 0.0, -78.0),
            "UpperArm_R": (4.0, 0.0, 78.0),
            "LowerArm_L": (-12.0, 0.0, 0.0),
            "LowerArm_R": (-12.0, 0.0, 0.0),
            "Head": (-4.0, 0.0, 0.0),
        }
        bones.update(_blend(mid, V_UP, u))
    else:
        bones.update(V_UP)
    hop = 0.0
    if 0.28 <= t <= 0.78:
        hop = math.sin((t - 0.28) / 0.50 * math.pi) * 0.22
    return bones, hop


def _pose_pump(t):
    # Two pumps in one second. The elbow local X never changes.
    wave = math.sin(t * math.pi * 2.0) ** 2
    bones = dict(PLANT)
    bones.update(_blend(PUMP_LOW, PUMP_HIGH, wave))
    return bones, 0.0


def _pose_shrug(t):
    if t < 0.18:
        u = _smooth(t / 0.18)
    elif t < 0.62:
        u = 1.0
    else:
        u = 1.0 - _smooth((t - 0.62) / 0.38)
    bones = dict(PLANT)
    bones.update(ARMS_EASY)
    bones = _blend(bones, {**PLANT, **SHRUG}, u)
    return bones, 0.0


def _pose_groove(t):
    left = _bump(t, 0.50, 0.50)
    right = _bump(t, 1.50, 0.50)
    bones = dict(PLANT)
    bones.update(ARMS_EASY)
    posed = _blend(bones, {**PLANT, **STEP_L}, left)
    posed = _blend(posed, {**PLANT, **STEP_R}, right)
    return posed, 0.0


CLIPS = (
    {
        "id": "emote_vcheer",
        "duration": 1.2,
        "pose": _pose_v,
        "move": "both arms in a V, hop, land",
        "stills": (0.05, 0.22, 0.52, 0.84, 1.15),
        "hero": 0.52,
    },
    {
        "id": "emote_fistpump",
        "duration": 1.0,
        "pose": _pose_pump,
        "move": "one-arm fist pump, elbow at 90 degrees, twice",
        "stills": (0.00, 0.25, 0.50, 0.75, 1.00),
        "hero": 0.25,
    },
    {
        "id": "emote_groove",
        "duration": 2.0,
        "pose": _pose_groove,
        "move": "side-step groove, shoulder bounce, hip hinge on each step",
        "stills": (0.00, 0.50, 1.00, 1.50, 2.00),
        "hero": 0.50,
    },
    {
        "id": "emote_shrug",
        "duration": 1.0,
        "pose": _pose_shrug,
        "move": "wide taunt shrug",
        "stills": (0.00, 0.18, 0.40, 0.70, 1.00),
        "hero": 0.40,
    },
)


def _build(arm, spec, ids, local):
    keys = []
    for t in _times(spec["duration"]):
        bones, hop = spec["pose"](t)
        hy = _seat(arm, bones, ids, local) + hop
        if abs(arm.location.x) + abs(arm.location.y) + abs(arm.location.z) > 1e-6:
            raise RuntimeError("armature moved")
        keys.append({
            "t": t,
            "bones": {k: [round(float(c), 2) for c in v] for k, v in bones.items()},
            "hips_location_m": [0.0, round(hy, 4), 0.0],
            "airborne": hop > 0.001,
        })
    return keys


def _support(ids, local):
    soles = {side: p12._sole_z(ids, local, side) for side in ("L", "R")}
    feet = [side for side, sole in soles.items() if sole[1] < 0.12]
    return feet, soles


def _measure(arm, keys, ids, local, rest_z):
    rows = []
    fails = []
    for i, key in enumerate(keys):
        _apply(arm, key["bones"], key["hips_location_m"][1])
        feet, soles = _support(ids, local)
        use = feet or ["L", "R"]
        back, hip, fwd, _foot = p14._support_back(arm, ids, local, use)
        _hip, fwd, up = p14._rig_fwd(arm)
        ratio, hip_deg, lumbar, chest = p14._hinge(arm, use, fwd, up)
        both_down = len(feet) >= 2
        target = 0.12 if (hip.z < 0.78 and both_down) else 0.08
        if back < target - 0.01 or ratio < 1.5:
            fails.append(i)
        elbows = {}
        tips = {}
        for side in ("L", "R"):
            sh = arm.matrix_world @ arm.pose.bones[f"UpperArm_{side}"].head
            el = arm.matrix_world @ arm.pose.bones[f"LowerArm_{side}"].head
            hd = arm.matrix_world @ arm.pose.bones[f"Hand_{side}"].head
            tip = arm.matrix_world @ arm.pose.bones[f"Hand_{side}"].tail
            elbows[side] = 180.0 - p14._joint_angle(sh, el, hd)
            tips[side] = tip
        # Airborne frames are the hop. A stepping foot sits above the support
        # foot and is not a plant. The support sole is the lower one, and only
        # while that foot is still on the floor.
        low_med = min(soles["L"][1], soles["R"][1])
        plant = {}
        if not key.get("airborne") and low_med <= 0.02:
            for side in ("L", "R"):
                med = soles[side][1]
                if med > low_med + 0.0015:
                    continue
                plant[side] = (
                    _mesh_min(side),
                    med,
                    p14._knee_flex_deg(arm, side),
                    p14._shin_forward_m(arm, side, fwd),
                )
        rows.append({
            "back": back,
            "hinge": ratio,
            "hip": hip_deg,
            "hipz": hip.z,
            "drop": rest_z - hip.z,
            "elbows": elbows,
            "tips": tips,
            "plant": plant,
            "target": target,
        })
    return rows, fails


def _noclip(arm, cid, keys, pieces, locals_c, polys, rest):
    orig = proof._apply_key

    def _apply_key(arm, bones, capsule, facing):
        loc = bones.get("_hips_location_m")
        eulers = {k: v for k, v in bones.items() if not str(k).startswith("_")}
        rh._apply_eulers(arm, eulers)
        arm.location = (0.0, 0.0, 0.0)
        arm.rotation_euler = (0.0, 0.0, 0.0)
        root = arm.pose.bones.get("Root")
        if root is not None:
            root.location = (0.0, 0.0, 0.0)
            root.rotation_euler = (0.0, 0.0, 0.0)
        hips = arm.pose.bones["Hips"]
        hips.location = (0.0, 0.0, 0.0) if loc is None else tuple(loc)
        bpy.context.view_layer.update()

    proof._apply_key = _apply_key
    try:
        packed = []
        for key in keys:
            bones = dict(key["bones"])
            bones["_hips_location_m"] = key["hips_location_m"]
            packed.append({"t": key["t"], "bones": bones})
        entry = {
            "keys": packed,
            "capsule_preview_m": [[0.0, 0.0, 0.0] for _ in packed],
            "fps": FPS,
        }
        totals = proof._scan_clip(
            arm, pieces, locals_c, polys, rest, "pass5", cid, entry,
            Euler((0.0, 0.0, 0.0), "XYZ"), list(range(len(packed))), False, None, [],
        )
    finally:
        proof._apply_key = orig
    pairs = []
    for (a, b), depth in sorted(totals["pairs"].items(), key=lambda kv: -kv[1]):
        kind = totals["pair_kind"][(a, b)]
        if kind == "pose" and depth > proof.LIMIT_M:
            pairs.append(f"{a} {b} {depth * 100:.2f}")
    line = (
        f"no-clip clips=1 frames={len(keys)} absMax={totals['abs'] * 100:.2f} "
        f"worldMax={totals['world'] * 100:.2f} rigJoint={totals['rig']} "
        f"poseFails={totals['pose']} pose={totals['pose']}"
    )
    return line, totals, pairs


def _nearest(keys, t):
    return min(range(len(keys)), key=lambda i: abs(keys[i]["t"] - t))


def _render(arm, spec, keys):
    prev = "/tmp/emote_cells_pass17"
    os.makedirs(prev, exist_ok=True)
    sc = bpy.context.scene
    if hasattr(sc, "eevee"):
        sc.eevee.taa_render_samples = 8
    sc.render.resolution_x = 200
    sc.render.resolution_y = 240
    sc.render.resolution_percentage = 100
    views = {
        "side": ((3.4, 0.0, 0.95), (0.0, 0.0, 0.95)),
        "front": ((0.0, -3.4, 0.95), (0.0, 0.0, 0.95)),
        "threequarter": ((2.4, -2.4, 1.05), (0.0, 0.0, 0.95)),
    }
    shots = {name: [] for name in views}
    hero_i = _nearest(keys, spec["hero"])
    want = list(spec["stills"])
    if keys[hero_i]["t"] not in want:
        want.append(keys[hero_i]["t"])
    for s, t in enumerate(want):
        i = _nearest(keys, t)
        _apply(arm, keys[i]["bones"], keys[i]["hips_location_m"][1])
        if bpy.context.mode != "OBJECT":
            bpy.context.view_layer.objects.active = arm
            bpy.ops.object.mode_set(mode="OBJECT")
        for name, (loc, look) in views.items():
            path = os.path.join(prev, f"{spec['id']}_{name}_{s}.png")
            rh._shot(path, loc, look, ortho=2.35)
            shots[name].append((path, f"{keys[i]['t']:.2f}s", i == hero_i))
    os.makedirs(OUT, exist_ok=True)
    written = []
    for name, cells in shots.items():
        canvas = Image.new("RGB", (200 * len(cells), 268), (32, 28, 26))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle((0, 0, canvas.width, 28), fill=(18, 16, 14))
        draw.text((8, 8), f"{spec['id']}  {name}  240px  SHIPPABLE own work CC0", fill=(245, 236, 220))
        for s, (path, cap, _hero) in enumerate(cells):
            im = Image.open(path).convert("RGB")
            im.thumbnail((200, 240), Image.Resampling.LANCZOS)
            canvas.paste(im, (s * 200, 28))
            draw.text((s * 200 + 6, 30), cap, fill=(245, 236, 220))
        out = os.path.join(OUT, f"{spec['id']}_{name}.png")
        canvas.save(out, optimize=True)
        if os.path.getsize(out) > 400_000:
            q = canvas.quantize(colors=80, method=Image.Quantize.MEDIANCUT)
            q.save(out, optimize=True)
        # The hero cell alone, at the 240 px render, is the silhouette check.
        hero = next(c for c in cells if c[2])
        hero_im = Image.open(hero[0]).convert("RGB")
        hero_path = os.path.join(OUT, f"{spec['id']}_{name}_hero.png")
        hero_im.save(hero_path, optimize=True)
        written.append(out)
        print("wrote", out, os.path.getsize(out), flush=True)
    return written


def _report(spec, keys, rows, fails, rest_z):
    backs = [r["back"] for r in rows]
    hinges = [r["hinge"] for r in rows]
    plants = []
    for r in rows:
        plants.extend(r["plant"].values())
    hip = (
        f"hip-sit clips=1 loadedFrames={len(keys)} "
        f"pelvisBackMin={min(backs) * 100:.1f} cm hingeMin={min(hinges):.2f} fails={len(fails)}"
    )
    if plants:
        sole_med = max(p[1] for p in plants)
        sole_min = min(p[0] for p in plants)
        knee = min(p[2] for p in plants)
        shin = min(p[3] for p in plants)
    else:
        sole_med = sole_min = knee = shin = float("nan")
    drops = [r["drop"] for r in rows if r["plant"]]
    sole = (
        f"plants {spec['id']} frames={sum(1 for r in rows if r['plant'])} "
        f"soleMedMax={sole_med * 100:.2f} cm soleMeshMin={sole_min * 100:.2f} cm "
        f"kneeMin={knee:.0f} shinMin={shin * 100:.1f} cm "
        f"drop={min(drops) * 100:.1f}-{max(drops) * 100:.1f} cm"
    )
    extra = ""
    if spec["id"] == "emote_fistpump":
        elb = [r["elbows"]["R"] for r in rows]
        tip_z = [r["tips"]["R"].z for r in rows]
        extra = f"elbowR={min(elb):.1f}-{max(elb):.1f} tipZ={min(tip_z):.2f}-{max(tip_z):.2f}"
    if spec["id"] == "emote_vcheer":
        tip_l = max(rows, key=lambda r: r["tips"]["L"].z)
        extra = (
            f"handLz={tip_l['tips']['L'].z:.2f} handLx={tip_l['tips']['L'].x:.2f} "
            f"handRz={tip_l['tips']['R'].z:.2f}"
        )
    if fails:
        extra = (extra + " " if extra else "") + "failFrames=" + ",".join(str(i) for i in fails[:20])
    return hip, sole, extra


def main():
    os.makedirs(OUT, exist_ok=True)
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    p10._ensure_pose(arm)
    ids, local = p10._sole_ids(arm)
    arm.pose.bones["Hips"].location = (0.0, 0.0, 0.0)
    p10._write(arm, {})
    rest_z = (arm.matrix_world @ arm.pose.bones["Hips"].head).z
    print(f"rest hip z {rest_z:.3f}", flush=True)
    pieces = proof._pieces(arm)
    locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
    polys = {obj.name: proof._polys(obj) for obj in pieces}
    rest = proof._bind_self(arm, pieces, locals_c, polys)
    catalog = {
        "ship": SHIP,
        "reference_only": False,
        "root_motion": False,
        "hips_keyed": True,
        "origin": (
            "Original poses authored for this rig. Commons clips were used only "
            "as a reminder of how long a cheer or a step feels. No tracked pose was copied."
        ),
        "hips_location": "Hips local Y is world up. Negative Y drops the pelvis. Added Y is the hop. The armature stays at the origin.",
        "clips": {},
    }
    lines = [
        "PASS 17 HAND-KEYED EMOTES",
        SHIP,
        "Poses are original. Video-derived clips stay reference-only.",
        "The armature location stays at the origin.",
    ]
    gate_ok = True
    built = []
    for spec in CLIPS:
        keys = _build(arm, spec, ids, local)
        if spec["id"] == "emote_groove":
            a = keys[0]
            b = keys[-1]
            if a["bones"] != b["bones"] or a["hips_location_m"] != b["hips_location_m"]:
                print("LOOP MISMATCH", a["hips_location_m"], b["hips_location_m"], flush=True)
                gate_ok = False
        rows, fails = _measure(arm, keys, ids, local, rest_z)
        hip, sole, extra = _report(spec, keys, rows, fails, rest_z)
        print(hip, flush=True)
        print(sole, flush=True)
        if extra:
            print(extra, flush=True)
        if fails:
            gate_ok = False
        plant_bad = []
        for i, r in enumerate(rows):
            for side, (mn, med, _knee, _shin) in r["plant"].items():
                if med > 0.005 or mn < -0.005 or mn > 0.005:
                    plant_bad.append(f"f={i} {side} min={mn * 100:.2f} med={med * 100:.2f}")
        if plant_bad:
            gate_ok = False
            print("SOLE", " ".join(plant_bad[:8]), flush=True)
        clip_line, totals, pairs = _noclip(arm, spec["id"], keys, pieces, locals_c, polys, rest)
        print(clip_line, flush=True)
        if pairs:
            print("POSE PAIRS", " | ".join(pairs[:8]), flush=True)
        if totals["pose"] != 0:
            gate_ok = False
        lines.extend([spec["id"], hip, sole])
        if extra:
            lines.append(extra)
        lines.append(clip_line)
        catalog["clips"][spec["id"]] = {
            "ship": SHIP,
            "fps": FPS,
            "duration_s": spec["duration"],
            "move": spec["move"],
            "license": "CC0. Original work authored for Tag. No video pose was copied.",
            "root_motion": False,
            "keys": keys,
        }
        built.append((spec, keys))
    with open(os.path.join(OUT, "keyed_emotes.json"), "w") as f:
        import json
        json.dump(catalog, f)
    with open(os.path.join(OUT, "metrics.txt"), "w") as f:
        f.write("\n".join(lines) + "\n")
    print("wrote", os.path.join(OUT, "metrics.txt"), "gate", gate_ok, flush=True)
    if not gate_ok:
        raise SystemExit(2)
    if os.environ.get("SKIP_RENDER") == "1":
        return
    for spec, keys in built:
        _render(arm, spec, keys)
    rh._reset(arm)
    arm.pose.bones["Hips"].location = (0.0, 0.0, 0.0)


if __name__ == "__main__":
    main()
