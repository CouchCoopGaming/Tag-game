#!/usr/bin/env python3
"""Pass 16: CC0 standing emotes with the Hips bone keyed.

Limb angles come from the MediaPipe track. The Hips bone also gets the
pelvis orientation, then a local-Y translation that puts the lower sole
back on the floor. That translation is the pelvis drop. The armature
location stays at the origin. No root motion, no capsule move.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Euler, Vector
from PIL import Image, ImageDraw

import noclip_proof as proof
import pass10_pose as p10
import pass14_pose as p14
import render_hier_v080_stills as rh
import storror_pose as sp

POSE = os.path.abspath(os.path.join(HERE, "..", "..", "Docs", "Movement", "pose"))
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "Docs", "Movement", "emotes", "pass16"))
CLIPS = ("emote_agbadja", "emote_tizi")
SOLE_TARGET = 0.008
# A standing dance should not be sat into a floor pose by a bad foot track.
DROP_CAP_M = 0.22


def _channels(doc):
    n = len(doc["world_xyz_m"])
    series = {k: [None] * n for k in sp.CHANNELS}
    for i in range(n):
        if not doc["usable"][i]:
            continue
        ang = doc["joint_angles_deg"][i] or {}
        for k in sp.CHANNELS:
            if k in ang and ang[k] is not None and "elev" not in k and "abd_lat" not in k:
                series[k][i] = float(ang[k])
        world = doc["world_xyz_m"][i]
        vis = doc["visibility"][i]
        if world is None or vis is None:
            continue
        for side in ("L", "R"):
            got = sp.elev_and_lat(world, vis, side)
            if not got:
                continue
            series[f"hip_elev_{side}"][i] = got["hip"][0]
            series[f"hip_abd_lat_{side}"][i] = got["hip"][1]
            series[f"shoulder_elev_{side}"][i] = got["shoulder"][0]
            series[f"shoulder_abd_lat_{side}"][i] = got["shoulder"][1]
    smooth = {k: sp._smooth(series[k]) for k in sp.CHANNELS}
    for k, vals in smooth.items():
        for i, v in enumerate(vals):
            if v is None:
                vals[i] = 0.0
    return smooth


def _limb_bones(smooth, i):
    ch = {k: smooth[k][i] for k in sp.CHANNELS}
    eulers = sp.blender_euler(sp.unity_pose(ch))
    return {name: [round(float(a), 2) for a in ang] for name, ang in eulers.items()}


def _hips_euler(world):
    if not world or any(p is None or any(c is None for c in p) for p in world):
        return None
    eul, _target = p10.pelvis_of(world)
    # Pitch and roll stay small so the chest does not fold onto the thighs.
    # Yaw is the turn. A full spin is not invented by unwrapping this angle.
    pitch = max(-28.0, min(28.0, math.degrees(eul.x)))
    yaw = max(-80.0, min(80.0, math.degrees(eul.y)))
    roll = max(-22.0, min(22.0, math.degrees(eul.z)))
    return [round(pitch, 2), round(yaw, 2), round(roll, 2)]


def _apply(arm, bones, loc):
    p10._write(arm, bones)
    arm.location = (0.0, 0.0, 0.0)
    arm.rotation_euler = (0.0, 0.0, 0.0)
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
    arm.pose.bones["Hips"].location = (float(loc[0]), float(loc[1]), float(loc[2]))
    bpy.context.view_layer.update()


def _lowest_sole(arm, ids, local):
    return min(p14.p12._sole_z(ids, local, side)[1] for side in ("L", "R"))


def _head_above(arm):
    hip = arm.matrix_world @ arm.pose.bones["Hips"].head
    head = arm.matrix_world @ arm.pose.bones["Head"].head
    return head.z - hip.z


def _seat(arm, bones, ids, local, rest_z):
    """Drop Hips local Y until the lower sole meets the floor, capped."""
    loc = [0.0, 0.0, 0.0]
    _apply(arm, bones, loc)
    for _ in range(3):
        sole = _lowest_sole(arm, ids, local)
        loc[1] -= sole - SOLE_TARGET
        hip_z = (arm.matrix_world @ arm.pose.bones["Hips"].head).z
        # Predict the drop after this step. Local Y is world up, one to one.
        predicted = rest_z - (hip_z - (sole - SOLE_TARGET))
        if predicted > DROP_CAP_M:
            loc[1] += predicted - DROP_CAP_M
        _apply(arm, bones, loc)
    dropped = rest_z - (arm.matrix_world @ arm.pose.bones["Hips"].head).z
    capped = dropped >= DROP_CAP_M - 0.005
    return [round(v, 4) for v in loc], capped


def _keys_for(arm, doc, ids, local, rest_z):
    smooth = _channels(doc)
    keys = []
    upright_rot = 0
    capped = 0
    for i in range(len(doc["times_s"])):
        bones = _limb_bones(smooth, i)
        eul = _hips_euler(doc["world_xyz_m"][i])
        use_rot = eul is not None
        if use_rot:
            bones["Hips"] = eul
            loc, hit_cap = _seat(arm, bones, ids, local, rest_z)
            if _head_above(arm) < 0.20:
                bones.pop("Hips", None)
                use_rot = False
                loc, hit_cap = _seat(arm, bones, ids, local, rest_z)
        else:
            loc, hit_cap = _seat(arm, bones, ids, local, rest_z)
        if use_rot:
            upright_rot += 1
        if hit_cap:
            capped += 1
        if abs(arm.location.x) + abs(arm.location.y) + abs(arm.location.z) > 1e-6:
            raise RuntimeError("armature moved")
        keys.append({
            "t": doc["times_s"][i],
            "bones": bones,
            "hips_location_m": loc,
        })
    return keys, upright_rot, capped


def _support_feet(arm, ids, local):
    low = []
    for side in ("L", "R"):
        z = p14.p12._sole_z(ids, local, side)[1]
        low.append((z, side))
    low.sort()
    feet = [low[0][1]]
    if low[1][0] < 0.12:
        feet.append(low[1][1])
    return feet


def _measure(arm, keys, ids, local, rest_z):
    backs, knees, both, drops, shins, hinges, soles = [], [], [], [], [], [], []
    for key in keys:
        _apply(arm, key["bones"], key["hips_location_m"])
        feet = _support_feet(arm, ids, local)
        back, hip, fwd, _foot = p14._support_back(arm, ids, local, feet)
        knee = min(p14._knee_flex_deg(arm, side) for side in feet)
        both.append(max(p14._knee_flex_deg(arm, side) for side in ("L", "R")))
        shin = min(p14._shin_forward_m(arm, side, fwd) for side in feet)
        _up = Vector((0.0, 0.0, 1.0))
        hinge, _hip_deg, _lumbar, _chest = p14._hinge(arm, feet, fwd, _up)
        backs.append(back)
        knees.append(knee)
        drops.append(rest_z - hip.z)
        shins.append(shin)
        hinges.append(hinge)
        soles.append(_lowest_sole(arm, ids, local))
    return {
        "frames": len(keys),
        "pelvisBackMin": min(backs),
        "kneeMin": min(knees),
        "kneeMax": max(knees),
        "bothMin": min(both),
        "bothMax": max(both),
        "dropMin": min(drops),
        "dropMax": max(drops),
        "shinMin": min(shins),
        "shinMax": max(shins),
        "hingeMin": min(hinges),
        "soleMin": min(soles),
        "soleMax": max(soles),
    }


def _noclip(arm, cid, keys):
    orig = proof._apply_key

    def _apply_key(arm, bones, capsule, facing):
        loc = bones.get("_hips_location_m")
        eulers = {k: v for k, v in bones.items() if not str(k).startswith("_")}
        rh._apply_eulers(arm, eulers)
        arm.location = Vector(capsule)
        arm.rotation_euler = facing
        root = arm.pose.bones.get("Root")
        if root is not None:
            root.location = (0.0, 0.0, 0.0)
            root.rotation_euler = (0.0, 0.0, 0.0)
        hips = arm.pose.bones["Hips"]
        hips.location = (0.0, 0.0, 0.0) if loc is None else tuple(loc)
        bpy.context.view_layer.update()

    proof._apply_key = _apply_key
    try:
        pieces = proof._pieces(arm)
        locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
        polys = {obj.name: proof._polys(obj) for obj in pieces}
        rest = proof._bind_self(arm, pieces, locals_c, polys)
        packed = []
        for key in keys:
            bones = dict(key["bones"])
            bones["_hips_location_m"] = key["hips_location_m"]
            packed.append({"t": key["t"], "bones": bones})
        entry = {
            "keys": packed,
            "capsule_preview_m": [[0.0, 0.0, 0.0] for _ in packed],
            "fps": 30.0,
        }
        facing = Euler((0.0, 0.0, 0.0), "XYZ")
        totals = proof._scan_clip(
            arm, pieces, locals_c, polys, rest, "pass5", cid, entry, facing,
            list(range(len(packed))), False, None, [],
        )
    finally:
        proof._apply_key = orig
    return (
        f"no-clip clips=1 frames={len(keys)} absMax={totals['abs'] * 100:.2f} "
        f"worldMax={totals['world'] * 100:.2f} rigJoint={totals['rig']} "
        f"poseFails={totals['pose']} pose={totals['pose']}"
    )


def _render(arm, cid, keys):
    prev = "/tmp/emote_cells_pass16"
    os.makedirs(prev, exist_ok=True)
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 480
    bpy.context.scene.render.resolution_y = 640
    # The first two cells sit inside the opening second, where the extreme has to read.
    want = [0.15, 0.45, 1.1, 1.9, 2.7]
    end_t = keys[-1]["t"] - keys[0]["t"]
    want.append(end_t)
    pick = []
    for w in want:
        pick.append(min(range(len(keys)), key=lambda i, w=w: abs((keys[i]["t"] - keys[0]["t"]) - w)))
    shots = {"side": [], "front": [], "rear": []}
    for s, i in enumerate(pick):
        _apply(arm, keys[i]["bones"], keys[i]["hips_location_m"])
        t = keys[i]["t"] - keys[0]["t"]
        drop = None
        if bpy.context.mode != "OBJECT":
            bpy.context.view_layer.objects.active = arm
            bpy.ops.object.mode_set(mode="OBJECT")
        views = {
            "side": ((3.8, 0.0, 0.95), (0.0, 0.0, 0.9)),
            "front": ((0.0, -3.8, 0.95), (0.0, 0.0, 0.9)),
            "rear": ((0.0, 3.8, 0.95), (0.0, 0.0, 0.9)),
        }
        for name, (loc, look) in views.items():
            path = os.path.join(prev, f"{cid}_{name}_{s}.png")
            rh._shot(path, loc, look, ortho=2.15)
            shots[name].append((path, f"{t:.2f}s"))
        del drop
    os.makedirs(OUT, exist_ok=True)
    written = []
    for name, cells in shots.items():
        canvas = Image.new("RGB", (320 * 6, 420), (48, 44, 40))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle((0, 0, canvas.width, 26), fill=(20, 18, 16))
        draw.text((10, 6), f"{cid}  {name}  CC0  Hips keyed", fill=(245, 236, 220))
        for s, (path, cap) in enumerate(cells):
            im = Image.open(path).convert("RGB")
            im.thumbnail((300, 370), Image.Resampling.LANCZOS)
            x0 = s * 320 + (320 - im.width) // 2
            canvas.paste(im, (x0, 36))
            draw.text((s * 320 + 8, 28), cap, fill=(245, 236, 220))
        out = os.path.join(OUT, f"{cid}_{name}.png")
        canvas.save(out, optimize=True)
        if os.path.getsize(out) > 400_000:
            q = canvas.quantize(colors=80, method=Image.Quantize.MEDIANCUT)
            q.save(out, optimize=True)
        written.append(out)
        print("wrote", out, os.path.getsize(out), flush=True)
    return written


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
    catalog = {
        "ship": "CC0",
        "reference_only": False,
        "root_motion": False,
        "hips_keyed": True,
        "hips_location": "Hips local Y is world up. Negative Y drops the pelvis. The armature stays at the origin.",
        "clips": {},
    }
    lines = [
        "PASS 16 EMOTE RETARGET",
        "CC0 standing dances. Hips rotation comes from the tracked pelvis.",
        "Hips local Y drops the pelvis until the lower sole is on the floor.",
        "The armature location stays at the origin. not-a-plant / emote.",
        "Tightened plant thresholds are reported and are not the target.",
    ]
    for cid in CLIPS:
        doc = json.load(open(os.path.join(POSE, cid + ".json")))
        keys, upright_rot, capped = _keys_for(arm, doc, ids, local, rest_z)
        catalog["clips"][cid] = {
            "ship": "CC0",
            "fps": doc["fps"],
            "license": doc["license"],
            "license_line": (
                "This file is made available under the Creative Commons CC0 1.0 "
                "Universal Public Domain Dedication."
            ),
            "source": doc["source"],
            "move": doc["move"],
            "hips_rotation_frames": upright_rot,
            "hips_drop_capped_frames": capped,
            "keys": keys,
        }
        stats = _measure(arm, keys, ids, local, rest_z)
        hip = (
            f"hip-sit clips=1 loadedFrames={stats['frames']} "
            f"pelvisBackMin={stats['pelvisBackMin'] * 100:.1f} cm "
            f"hingeMin={stats['hingeMin']:.2f} fails=emote"
        )
        tight = (
            f"tightened {cid} knee={stats['kneeMin']:.0f}-{stats['kneeMax']:.0f} deg "
            f"bothKnee={stats['bothMin']:.0f}-{stats['bothMax']:.0f} deg "
            f"pelvisDrop={stats['dropMin'] * 100:.1f}-{stats['dropMax'] * 100:.1f} cm "
            f"shin={stats['shinMin'] * 100:.1f}-{stats['shinMax'] * 100:.1f} cm "
            f"sole={stats['soleMin'] * 100:.1f}-{stats['soleMax'] * 100:.1f} cm "
            f"hipsRot={upright_rot}/{stats['frames']} dropCapped={capped} "
            f"label=not-a-plant / emote"
        )
        print(hip, flush=True)
        print(tight, flush=True)
        clip_line = _noclip(arm, cid, keys)
        print(clip_line, flush=True)
        lines.extend([cid, hip, tight, clip_line])
        _render(arm, cid, keys)
    with open(os.path.join(OUT, "keyed_emotes.json"), "w") as f:
        json.dump(catalog, f)
    with open(os.path.join(OUT, "metrics.txt"), "w") as f:
        f.write("\n".join(lines) + "\n")
    print("wrote", os.path.join(OUT, "metrics.txt"), flush=True)
    rh._reset(arm)
    arm.pose.bones["Hips"].location = (0.0, 0.0, 0.0)


if __name__ == "__main__":
    main()
