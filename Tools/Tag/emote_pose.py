#!/usr/bin/env python3
"""Retarget two short emotes onto the Hier and render side and front stills.

Reference only. The armature stays at the origin. No root motion, no capsule
move. Floor dances stay a crouch because the rig cannot lie down.
"""
import json
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
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "Docs", "Movement", "emotes"))
CLIPS = ("emote_6step", "emote_charleston")


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


def _keys(doc):
    smooth = _channels(doc)
    keys = []
    for i in range(len(doc["times_s"])):
        ch = {k: smooth[k][i] for k in sp.CHANNELS}
        eulers = sp.blender_euler(sp.unity_pose(ch))
        bones = {name: [round(float(a), 2) for a in ang] for name, ang in eulers.items()}
        keys.append({"t": doc["times_s"][i], "bones": bones})
    return keys


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


def _measure(arm, keys, ids, local):
    rest_z = p14._rest_hip_z(arm)
    backs, knees, drops, shins = [], [], [], []
    for key in keys:
        p14._apply(arm, key["bones"], [0.0, 0.0, 0.0])
        feet = _support_feet(arm, ids, local)
        back, hip, fwd, _foot = p14._support_back(arm, ids, local, feet)
        knee = min(p14._knee_flex_deg(arm, side) for side in feet)
        shin = min(p14._shin_forward_m(arm, side, fwd) for side in feet)
        backs.append(back)
        knees.append(knee)
        drops.append(rest_z - hip.z)
        shins.append(shin)
    return {
        "frames": len(keys),
        "pelvisBackMin": min(backs),
        "pelvisBackMax": max(backs),
        "kneeMin": min(knees),
        "kneeMax": max(knees),
        "dropMin": min(drops),
        "dropMax": max(drops),
        "shinMin": min(shins),
        "shinMax": max(shins),
    }


def _noclip(arm, cid, keys):
    pieces = proof._pieces(arm)
    locals_c = {obj.name: proof._local_coords(obj) for obj in pieces}
    polys = {obj.name: proof._polys(obj) for obj in pieces}
    rest = proof._bind_self(arm, pieces, locals_c, polys)
    entry = {
        "keys": keys,
        "capsule_preview_m": [[0.0, 0.0, 0.0] for _ in keys],
        "fps": 30.0,
    }
    facing = Euler((0.0, 0.0, 0.0), "XYZ")
    totals = proof._scan_clip(
        arm, pieces, locals_c, polys, rest, "pass5", cid, entry, facing,
        list(range(len(keys))), False, None, [],
    )
    return (
        f"no-clip clips=1 frames={len(keys)} absMax={totals['abs'] * 100:.2f} "
        f"worldMax={totals['world'] * 100:.2f} rigJoint={totals['rig']} "
        f"poseFails={totals['pose']} pose={totals['pose']}"
    )


def _render(arm, cid, keys, ids, local):
    prev = "/tmp/emote_cells"
    os.makedirs(prev, exist_ok=True)
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 480
    bpy.context.scene.render.resolution_y = 640
    pick = [int(round(i * (len(keys) - 1) / 5)) for i in range(6)]
    shots = {"side": [], "front": []}
    for s, i in enumerate(pick):
        rh._apply_eulers(arm, keys[i]["bones"])
        arm.location = Vector((0.0, 0.0, 0.0))
        arm.rotation_euler = (0.0, 0.0, 0.0)
        root = arm.pose.bones.get("Root")
        if root is not None:
            root.location = (0.0, 0.0, 0.0)
            root.rotation_euler = (0.0, 0.0, 0.0)
        bpy.context.view_layer.update()
        p14.p11._show_floor(arm)
        t = keys[i]["t"] - keys[0]["t"]
        views = {
            "side": ((3.8, 0.0, 0.95), (0.0, 0.0, 0.9)),
            "front": ((0.0, -3.8, 0.95), (0.0, 0.0, 0.9)),
        }
        for name, (loc, look) in views.items():
            path = os.path.join(prev, f"{cid}_{name}_{s}.png")
            rh._shot(path, loc, look, ortho=2.15)
            shots[name].append((path, f"{t:.2f}s"))
    os.makedirs(OUT, exist_ok=True)
    written = []
    for name, cells in shots.items():
        canvas = Image.new("RGB", (320 * 6, 420), (48, 44, 40))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle((0, 0, canvas.width, 26), fill=(20, 18, 16))
        draw.text((10, 6), f"{cid}  {name}  reference emote", fill=(245, 236, 220))
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
    catalog = {"reference_only": True, "root_motion": False, "clips": {}}
    lines = [
        "EMOTE RETARGET",
        "These are emotes, not plants. not-a-plant / reference-only.",
        "The rig stays standing. A floor step reads as a crouch because there is no root drop.",
        "Tightened plant thresholds are reported and are not the target for an emote.",
    ]
    for cid in CLIPS:
        doc = json.load(open(os.path.join(POSE, cid + ".json")))
        keys = _keys(doc)
        catalog["clips"][cid] = {
            "fps": doc["fps"],
            "license": doc["license"],
            "source": doc["source"],
            "move": doc["move"],
            "keys": keys,
        }
        stats = _measure(arm, keys, ids, local)
        hip = (
            f"hip-sit clips=1 loadedFrames={stats['frames']} "
            f"pelvisBackMin={stats['pelvisBackMin'] * 100:.1f} cm "
            f"hingeMin=n/a fails=emote"
        )
        tight = (
            f"tightened {cid} knee={stats['kneeMin']:.0f}-{stats['kneeMax']:.0f} deg "
            f"pelvisDrop={stats['dropMin'] * 100:.1f}-{stats['dropMax'] * 100:.1f} cm "
            f"shin={stats['shinMin'] * 100:.1f}-{stats['shinMax'] * 100:.1f} cm "
            f"label=not-a-plant / emote reference"
        )
        clip_line = _noclip(arm, cid, keys)
        lines.append(cid)
        lines.append(hip)
        lines.append(tight)
        lines.append(clip_line)
        print(hip, flush=True)
        print(tight, flush=True)
        print(clip_line, flush=True)
        _render(arm, cid, keys, ids, local)
    with open(os.path.join(OUT, "keyed_emotes.json"), "w") as f:
        json.dump(catalog, f)
    with open(os.path.join(OUT, "metrics.txt"), "w") as f:
        f.write("\n".join(lines) + "\n")
    print("wrote", os.path.join(OUT, "metrics.txt"), flush=True)
    rh._reset(arm)


if __name__ == "__main__":
    main()
