#!/usr/bin/env python3
"""Per-frame interpenetration proof for Hier clips.

The mannequin is rigid piece meshes parented to bones. Each sampled frame
builds a mathutils.bvhtree for every piece and for the clip's obstacle
meshes (a ground slab, and the action wall when that clip has one).
BVHTree.overlap is the broad phase. Depth is how far a vertex of an
overlapping triangle sits past the other surface.

Joined neighbours (a parent bone and its child) ignore hits within 3 cm of
the shared joint, the child bone head. The fail limit is 0.5 cm of absolute
depth. Bind-pose depth is not subtracted.

A pair that shares a joint, or that already nests in the rest pose, is a
rigJoint fail when that absolute depth is over the limit. PR #128 owns the
trimmed-rig fix, so those hits are reported and the rig is left alone. A
world hit, or a hit between pieces that are not neighbours and do not nest
at rest, is a pose fail. Pass 10 reports whatever those hits cannot
clear inside the allowed joint window as pose=N.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Vector, Euler
from mathutils.bvhtree import BVHTree

import render_hier_v080_stills as rh
import render_hier_pass5 as p5
import storror_pose as sp

LIMIT_M = 0.005
JOINT_M = 0.03
PASS5_JSON = os.path.join(rh.OUT, "pass5", "keyed_clips.json")
PASS6_JSON = os.path.join(rh.OUT, "pass6", "keyed_clips.json")

PASS6_FACING_DEG = {
    "17_slide_slope_crouch": (-14.0, 0.0, 0.0),
    "18_vertical_wallrun_back": (12.0, 0.0, -85.0),
    "20_wallpop_180": (8.0, 0.0, -80.0),
}
PASS6_WALL_PAD = {
    "18_vertical_wallrun_back": 1.9,
    "20_wallpop_180": 1.6,
}


def _flush(msg):
    print(msg, flush=True)


def _pieces(arm):
    found = []
    for obj in bpy.data.objects:
        if obj.type != "MESH" or obj.parent != arm or obj.parent_type != "BONE":
            continue
        if not obj.name.startswith("Mesh_"):
            continue
        found.append(obj)
    found.sort(key=lambda o: o.name)
    return found


def _local_coords(obj):
    n = len(obj.data.vertices)
    flat = [0.0] * (n * 3)
    obj.data.vertices.foreach_get("co", flat)
    return [Vector((flat[i], flat[i + 1], flat[i + 2])) for i in range(0, n * 3, 3)]


def _polys(obj):
    return [tuple(p.vertices) for p in obj.data.polygons]


def _world(local, mw):
    return [mw @ v for v in local]


def _bvh(verts, polys):
    return BVHTree.FromPolygons(verts, polys)


def _shared_joint(arm, bone_a, bone_b):
    pa = arm.pose.bones.get(bone_a)
    pb = arm.pose.bones.get(bone_b)
    if pa is None or pb is None:
        return None
    if pa.parent is not None and pa.parent.name == bone_b:
        return arm.matrix_world @ Vector(pa.head)
    if pb.parent is not None and pb.parent.name == bone_a:
        return arm.matrix_world @ Vector(pb.head)
    return None


def _inside_depths(verts, polys, own_bvh, other_bvh, joint):
    """Vertex index -> metres inside other_bvh. Joint-ball hits are omitted."""
    pairs = own_bvh.overlap(other_bvh)
    if not pairs:
        return {}
    used = set()
    for ia, _ib in pairs:
        used.update(polys[ia])
    out = {}
    r2 = JOINT_M * JOINT_M
    for vi in used:
        v = verts[vi]
        if joint is not None and (v - joint).length_squared <= r2:
            continue
        loc, normal, _idx, _dist = other_bvh.find_nearest(v)
        if loc is None:
            continue
        signed = (v - loc).dot(normal)
        if signed < -1.0e-6:
            out[vi] = -signed
    return out


def _ensure_ground():
    name = "NoclipGround"
    obj = bpy.data.objects.get(name)
    if obj is None:
        bpy.ops.mesh.primitive_cube_add(location=(0.0, 0.0, -1.0))
        obj = bpy.context.active_object
        obj.name = name
    obj.location = (0.0, 0.0, -1.0)
    obj.scale = (20.0, 20.0, 1.0)
    obj.hide_render = True
    obj.hide_set(True)
    bpy.context.view_layer.update()
    return obj


def _sample_indices(fps, n):
    step = 1 if fps <= 31.0 else max(1, int(round(fps / 30.0)))
    return list(range(0, n, step))


def _apply_key(arm, bones, capsule, facing):
    rh._apply_eulers(arm, bones)
    arm.location = Vector(capsule)
    arm.rotation_euler = facing
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()


def _deg_euler(deg):
    return Euler(tuple(math.radians(a) for a in deg), "XYZ")


def _pass5_facing(clip_id):
    spec = next(s for s in sp.CLIPS if s["id"] == clip_id)
    curves = sp.clip_curves(spec["id"], keys=24)
    return p5._facing_for(spec, curves["raw_smooth"])


def _pass6_facing(clip_id):
    return _deg_euler(PASS6_FACING_DEG[clip_id])


def _cling_lip(arm, entry, facing):
    hero = int(entry["hero_frame"])
    key = entry["keys"][hero]
    _apply_key(arm, key["bones"], entry["capsule_preview_m"][hero], facing)
    zs = []
    for side in ("L", "R"):
        cluster = rh._palm_cluster(arm, side)
        if cluster:
            zs.append(sum(v.z for v in cluster) / len(cluster))
    if not zs:
        return None
    return sum(zs) / len(zs)


def _wall_plan(arm, which, cid, entry, facing):
    """(has_wall, top_z) matching the stills for this clip."""
    if which == "pass5":
        spec = next(s for s in sp.CLIPS if s["id"] == cid)
        if spec["verb"] == "softland":
            return False, None
        if entry.get("pelvis_in_hips"):
            return True, entry.get("wall_top_m")
        if spec["verb"] == "cling":
            return True, _cling_lip(arm, entry, facing)
        return True, None
    pad = PASS6_WALL_PAD.get(cid)
    if pad is None:
        return False, None
    top = max(p[2] for p in entry["capsule_preview_m"]) + pad
    return True, top


def _clip_jobs():
    jobs = []
    doc5 = json.load(open(PASS5_JSON))
    for cid, entry in doc5["clips"].items():
        jobs.append(("pass5", cid, entry))
    doc6 = json.load(open(PASS6_JSON))
    for cid, entry in doc6["clips"].items():
        jobs.append(("pass6", cid, entry))
    only = {s for s in os.environ.get("NOCLIP_ONLY", "").split(",") if s}
    if only:
        jobs = [j for j in jobs if j[1] in only]
    return jobs


def _consider(worst, row):
    worst.append(row)
    worst.sort(key=lambda r: -r["counted"])
    del worst[20:]


def _bind_self(arm, pieces, locals_c, polys_c):
    rh._reset(arm)
    packed = []
    for obj in pieces:
        verts = _world(locals_c[obj.name], obj.matrix_world)
        bvh = _bvh(verts, polys_c[obj.name])
        packed.append((obj, verts, bvh))
    pair_max = {}
    for i, (oa, va, ba) in enumerate(packed):
        for ob, vb, bb in packed[i + 1:]:
            joint = _shared_joint(arm, oa.parent_bone, ob.parent_bone)
            da = _inside_depths(va, polys_c[oa.name], ba, bb, joint)
            db = _inside_depths(vb, polys_c[ob.name], bb, ba, joint)
            worst = 0.0
            if da:
                worst = max(worst, max(da.values()))
            if db:
                worst = max(worst, max(db.values()))
            if worst > 0.0:
                pair_max[(oa.name, ob.name)] = worst
                pair_max[(ob.name, oa.name)] = worst
    _flush("--- bind nesting after the 3 cm joint exemption (cm) ---")
    rows = sorted(
        ((a, b, d) for (a, b), d in pair_max.items() if a < b),
        key=lambda r: -r[2],
    )
    for a, b, depth in rows[:12]:
        _flush(f"BIND {a} {b} {depth * 100:.2f}")
    if not rows:
        _flush("BIND none")
    return pair_max


def _aabb(verts):
    lo = Vector((min(v.x for v in verts), min(v.y for v in verts), min(v.z for v in verts)))
    hi = Vector((max(v.x for v in verts), max(v.y for v in verts), max(v.z for v in verts)))
    return lo, hi


def _in_aabb(v, lo, hi, pad=0.01):
    return (
        lo.x - pad <= v.x <= hi.x + pad
        and lo.y - pad <= v.y <= hi.y + pad
        and lo.z - pad <= v.z <= hi.z + pad
    )


def _pair_kind(joint, bind_depth):
    """rig = rest nesting or a joined pair. pose = a non-neighbour hit."""
    if joint is not None or bind_depth > 0.001:
        return "rig"
    return "pose"


def _scan_frame(pieces, locals_c, polys_c, obstacles, bind_pairs, arm, clip_id, frame, worst, totals):
    packed = []
    for obj in pieces:
        verts = _world(locals_c[obj.name], obj.matrix_world)
        bvh = _bvh(verts, polys_c[obj.name])
        packed.append((obj, verts, bvh))
    failed_pairs = set()
    for i, (oa, va, ba) in enumerate(packed):
        for ob, vb, bb in packed[i + 1:]:
            joint = _shared_joint(arm, oa.parent_bone, ob.parent_bone)
            da = _inside_depths(va, polys_c[oa.name], ba, bb, joint)
            db = _inside_depths(vb, polys_c[ob.name], bb, ba, joint)
            depth = 0.0
            label = f"{oa.name} in {ob.name}"
            if da:
                d = max(da.values())
                if d > depth:
                    depth = d
                    label = f"{oa.name} in {ob.name}"
            if db:
                d = max(db.values())
                if d > depth:
                    depth = d
                    label = f"{ob.name} in {oa.name}"
            if depth <= 0.0:
                continue
            base = bind_pairs.get((oa.name, ob.name), 0.0)
            kind = _pair_kind(joint, base)
            totals["abs"] = max(totals["abs"], depth)
            key = tuple(sorted((oa.name, ob.name)))
            prev = totals["pairs"].get(key, 0.0)
            if depth > prev:
                totals["pairs"][key] = depth
                totals["pair_kind"][key] = kind
            if depth > LIMIT_M:
                failed_pairs.add((kind, key))
                if kind == "pose" and depth >= totals["pose_abs"]:
                    totals["pose_abs"] = depth
                    totals["pose_where"] = (frame, label, depth)
                if depth >= totals["abs_where_d"]:
                    totals["abs_where_d"] = depth
                    totals["abs_where"] = (frame, kind, label, depth, base)
                _consider(worst, {
                    "clip": clip_id,
                    "frame": frame,
                    "kind": kind,
                    "pair": label,
                    "abs_cm": round(depth * 100.0, 2),
                    "rest_cm": round(base * 100.0, 2),
                    "counted": depth,
                })
    for obj, verts, bvh in packed:
        for oname, obvh, lo, hi in obstacles:
            # overlap() is the broad phase the rule asks for. Vertices buried
            # in the obstacle volume can miss a surface triangle, so anything
            # inside the obstacle box is tested as well.
            bvh.overlap(obvh)
            deep = 0.0
            for v in verts:
                if not _in_aabb(v, lo, hi):
                    continue
                loc, normal, _idx, _dist = obvh.find_nearest(v)
                if loc is None:
                    continue
                signed = (v - loc).dot(normal)
                if signed < -deep:
                    deep = -signed
            if deep <= 0.0:
                continue
            if deep >= totals["world"]:
                totals["world"] = deep
                totals["world_where"] = (frame, f"{obj.name} in {oname}", deep)
            label = f"{obj.name} in {oname}"
            if deep > LIMIT_M:
                failed_pairs.add(("world", label))
                _consider(worst, {
                    "clip": clip_id,
                    "frame": frame,
                    "kind": "world",
                    "pair": label,
                    "abs_cm": round(deep * 100.0, 2),
                    "rest_cm": 0.0,
                    "counted": deep,
                })
    totals["rig"] += sum(1 for k, _p in failed_pairs if k == "rig")
    totals["pose"] += sum(1 for k, _p in failed_pairs if k in ("pose", "world"))


def _scan_clip(arm, pieces, locals_c, polys_c, rest, which, cid, entry, facing,
               idxs, has_wall, wall_top, worst, wall_side="+"):
    totals = {
        "world": 0.0,
        "abs": 0.0,
        "pose_abs": 0.0,
        "abs_where_d": 0.0,
        "rig": 0,
        "pose": 0,
        "world_where": None,
        "pose_where": None,
        "abs_where": None,
        "pairs": {},
        "pair_kind": {},
    }
    ground = _ensure_ground()
    g_local = _local_coords(ground)
    g_polys = _polys(ground)
    for n_i, i in enumerate(idxs):
        key = entry["keys"][i]
        _apply_key(arm, key["bones"], entry["capsule_preview_m"][i], facing)
        gv = _world(g_local, ground.matrix_world)
        gb = _bvh(gv, g_polys)
        glo, ghi = _aabb(gv)
        obstacles = [("ground", gb, glo, ghi)]
        if has_wall:
            rh._show_wall(0.0, wall_top, arm.location.y, 2.4, side=wall_side)
            bpy.context.view_layer.update()
            wall = bpy.data.objects["ActionWall"]
            if "ActionWall" not in locals_c:
                locals_c["ActionWall"] = _local_coords(wall)
                polys_c["ActionWall"] = _polys(wall)
            wv = _world(locals_c["ActionWall"], wall.matrix_world)
            wlo, whi = _aabb(wv)
            obstacles.append(("wall", _bvh(wv, polys_c["ActionWall"]), wlo, whi))
        _scan_frame(
            pieces, locals_c, polys_c, obstacles, rest, arm, cid, i, worst, totals,
        )
        if n_i == 0 or (n_i + 1) % 40 == 0 or n_i + 1 == len(idxs):
            _flush(
                f"  {cid} {n_i + 1}/{len(idxs)} "
                f"w={totals['world'] * 100:.2f} abs={totals['abs'] * 100:.2f} "
                f"rig={totals['rig']} pose={totals['pose']}"
            )
    return totals


def run():
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("no DummyArmature")
    rh._prepare_scene()
    pieces = _pieces(arm)
    _flush("pieces " + str(len(pieces)))
    locals_c = {obj.name: _local_coords(obj) for obj in pieces}
    polys_c = {obj.name: _polys(obj) for obj in pieces}
    rest = _bind_self(arm, pieces, locals_c, polys_c)
    jobs = _clip_jobs()
    worst = []
    grand_world = 0.0
    grand_abs = 0.0
    frames = 0
    rig_fails = 0
    pose_fails = 0
    for which, cid, entry in jobs:
        if which == "pass5" and entry.get("pelvis_in_hips"):
            # The clip's own pelvis is on the Hips bone. A second facing yaw
            # would turn the body off the reference.
            facing = Euler((0.0, 0.0, 0.0), "XYZ")
        else:
            facing = _pass5_facing(cid) if which == "pass5" else _pass6_facing(cid)
        has_wall, wall_top = _wall_plan(arm, which, cid, entry, facing)
        wall_side = entry.get("wall_side", "+") if which == "pass5" else "+"
        n = len(entry["keys"])
        fps = float(entry["fps"])
        idxs = _sample_indices(fps, n)
        frames += len(idxs)
        _flush(
            f"CLIP {cid} samples={len(idxs)} fps={fps:.2f} wall={has_wall} top={wall_top}"
        )
        totals = _scan_clip(
            arm, pieces, locals_c, polys_c, rest, which, cid, entry, facing,
            idxs, has_wall, wall_top, worst, wall_side=wall_side,
        )
        rig_fails += totals["rig"]
        pose_fails += totals["pose"]
        grand_world = max(grand_world, totals["world"])
        grand_abs = max(grand_abs, totals["abs"])
        ww = totals["world_where"]
        aw = totals["abs_where"]
        pw = totals["pose_where"]
        _flush(
            f"DONE {cid} frames={len(idxs)} absMax={totals['abs'] * 100:.2f} "
            f"worldMax={totals['world'] * 100:.2f} "
            f"rigJoint={totals['rig']} poseFails={totals['pose']}"
        )
        if ww:
            _flush(f"  WORLD {cid} f={ww[0]} {ww[1]} {ww[2] * 100:.2f}")
        if pw:
            _flush(f"  POSE {cid} f={pw[0]} {pw[1]} {pw[2] * 100:.2f}")
        if aw:
            _flush(
                f"  ABS {cid} f={aw[0]} {aw[1]} {aw[2]} "
                f"abs={aw[3] * 100:.2f} rest={aw[4] * 100:.2f}"
            )
        ranked = sorted(totals["pairs"].items(), key=lambda kv: -kv[1])
        for (a, b), depth in ranked[:6]:
            if depth <= LIMIT_M:
                break
            _flush(f"  PAIR {totals['pair_kind'][(a, b)]} {a} {b} {depth * 100:.2f}")
    line = (
        f"no-clip clips={len(jobs)} frames={frames} "
        f"absMax={grand_abs * 100:.2f} worldMax={grand_world * 100:.2f} "
        f"rigJoint={rig_fails} poseFails={pose_fails} pose={pose_fails}"
    )
    _flush(line)
    _flush("--- worst frames (absolute depth; fail above 0.50 cm) ---")
    shown = sorted(worst, key=lambda r: -r["counted"])[:16]
    if not shown:
        _flush("WORST none")
    for r in shown:
        _flush(
            f"WORST {r['clip']} f={r['frame']} {r['kind']} {r['pair']} "
            f"abs={r['abs_cm']:.2f} rest={r['rest_cm']:.2f}"
        )
    rh._reset(arm)
    rh._hide_wall()
    return line


if __name__ == "__main__":
    run()
