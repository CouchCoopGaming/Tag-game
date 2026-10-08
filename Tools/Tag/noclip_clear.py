#!/usr/bin/env python3
"""Pull keyed Hier poses out of walls and out of each other.

The check lives in noclip_proof.py. This pass edits the IK pose: a few degrees
of abduction or a shorter fold, then the ankle or the sole is put back so a
plant does not skate. It does not rebuild the body mesh.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Vector

import render_hier_v080_stills as rh
import render_hier_pass5 as p5
import noclip_proof as nc

LEG = ("UpperLeg", "LowerLeg", "Foot")
ARM = ("UpperArm", "LowerArm", "Hand", "Shoulder")
TORSO = ("Hips", "Spine", "Chest", "Neck", "Head")
LIMIT = nc.LIMIT_M

# Worst frame of each failing clip, for the red overlap stills.
SHOTS = (
    ("pass5", "03_wall_drop_softland", 2, ("Mesh_Spine", "Mesh_UpperLeg_L"), "side"),
    ("pass5", "04_window_drop_softland", 33, ("Mesh_Hips", "Mesh_UpperLeg_R"), "side"),
    ("pass5", "10_wallrun_slanted", 6, ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), "side"),
    ("pass5", "12_tictac_slanted_wall", 0, ("Mesh_Chest", "Mesh_UpperLeg_L"), "side"),
    ("pass5", "15_cat_leap_wall", 74, ("Mesh_Hand_R",), "persp"),
    ("pass5", "15_cat_leap_wall", 72, ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), "side"),
    ("pass6", "17_slide_slope_crouch", 14, ("Mesh_LowerArm_L", "Mesh_UpperLeg_L"), "persp"),
    ("pass6", "18_vertical_wallrun_back", 36, ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), "side"),
    ("pass6", "20_wallpop_180", 14, ("Mesh_UpperLeg_L", "Mesh_UpperLeg_R"), "side"),
)


def _bone(mesh):
    return mesh[5:] if mesh.startswith("Mesh_") else mesh


def _family(bone):
    if bone.startswith(LEG):
        return "leg"
    if bone.startswith(ARM):
        return "arm"
    if bone.startswith(TORSO):
        return "torso"
    return "other"


def _side(bone):
    if bone.endswith("_L"):
        return "L"
    if bone.endswith("_R"):
        return "R"
    return ""


def _add(arm, bone, axis, deg):
    pb = arm.pose.bones[bone]
    pb.rotation_mode = "XYZ"
    e = list(pb.rotation_euler)
    e[axis] += math.radians(deg)
    pb.rotation_euler = tuple(e)


def _snap(arm):
    pose = {}
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pose[pb.name] = pb.rotation_euler.copy()
    return pose, arm.location.copy()


def _restore(arm, snap):
    pose, loc = snap
    for name, eul in pose.items():
        pb = arm.pose.bones[name]
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = eul.copy()
    arm.location = loc
    bpy.context.view_layer.update()


def _capture_leg(arm, side):
    ankle = p5._tail_w(arm, f"LowerLeg_{side}").copy()
    sole = p5._foot_point(f"Foot_{side}", "floor")
    return ankle, (None if sole is None else sole.copy())


def _restore_leg(arm, side, cap):
    ankle, sole = cap
    p5._point_bone(arm, f"LowerLeg_{side}", ankle)
    if sole is not None:
        p5._seat_end(
            arm, f"Foot_{side}",
            lambda s=side: p5._foot_point(f"Foot_{s}", "floor"),
            sole,
        )


def _pack(obj, locals_c, polys_c):
    verts = nc._world(locals_c[obj.name], obj.matrix_world)
    return verts, nc._bvh(verts, polys_c[obj.name])


def _pair_depth(arm, name_a, name_b, locals_c, polys_c, bind):
    oa = bpy.data.objects[name_a]
    ob = bpy.data.objects[name_b]
    va, ba = _pack(oa, locals_c, polys_c)
    vb, bb = _pack(ob, locals_c, polys_c)
    joint = nc._shared_joint(arm, oa.parent_bone, ob.parent_bone)
    da = nc._inside_depths(va, polys_c[oa.name], ba, bb, joint)
    db = nc._inside_depths(vb, polys_c[ob.name], bb, ba, joint)
    depth = 0.0
    if da:
        depth = max(depth, max(da.values()))
    if db:
        depth = max(depth, max(db.values()))
    base = bind.get((oa.name, ob.name), 0.0)
    excess = depth - base
    if excess < 0.0:
        excess = 0.0
    return excess, depth


def _obstacle_bvh(arm, has_wall, wall_top, locals_c, polys_c):
    ground = nc._ensure_ground()
    if "NoclipGround" not in locals_c:
        locals_c["NoclipGround"] = nc._local_coords(ground)
        polys_c["NoclipGround"] = nc._polys(ground)
    gv = nc._world(locals_c["NoclipGround"], ground.matrix_world)
    obstacles = [("ground", nc._bvh(gv, polys_c["NoclipGround"]), nc._aabb(gv))]
    if has_wall:
        rh._show_wall(0.0, wall_top, arm.location.y, 2.4)
        bpy.context.view_layer.update()
        wall = bpy.data.objects["ActionWall"]
        if "ActionWall" not in locals_c:
            locals_c["ActionWall"] = nc._local_coords(wall)
            polys_c["ActionWall"] = nc._polys(wall)
        wv = nc._world(locals_c["ActionWall"], wall.matrix_world)
        obstacles.append(("wall", nc._bvh(wv, polys_c["ActionWall"]), nc._aabb(wv)))
    return obstacles


def _mesh_world(obj, verts, obstacles):
    deep = 0.0
    where = None
    for oname, obvh, (lo, hi) in obstacles:
        obvh_hit = False
        # Broad phase, then every vertex that sits in the obstacle box.
        # A buried triangle can miss the surface faces.
        own = None
        for v in verts:
            if not nc._in_aabb(v, lo, hi):
                continue
            if not obvh_hit:
                obvh_hit = True
            loc, normal, _i, _d = obvh.find_nearest(v)
            if loc is None:
                continue
            signed = (v - loc).dot(normal)
            if signed < -deep:
                deep = -signed
                where = oname
        if obvh_hit and own is None:
            pass
    return deep, where


def _fill(arm, pieces, locals_c, polys_c, bind, obstacles):
    packed = {}
    for obj in pieces:
        packed[obj.name] = _pack(obj, locals_c, polys_c)
    excess = {}
    names = list(packed.keys())
    for i, na in enumerate(names):
        oa = bpy.data.objects[na]
        va, ba = packed[na]
        for nb in names[i + 1:]:
            ob = bpy.data.objects[nb]
            vb, bb = packed[nb]
            joint = nc._shared_joint(arm, oa.parent_bone, ob.parent_bone)
            da = nc._inside_depths(va, polys_c[na], ba, bb, joint)
            db = nc._inside_depths(vb, polys_c[nb], bb, ba, joint)
            depth = 0.0
            if da:
                depth = max(depth, max(da.values()))
            if db:
                depth = max(depth, max(db.values()))
            if depth <= 0.0:
                continue
            base = bind.get((na, nb), 0.0)
            ex = depth - base
            if ex > 1.0e-6:
                excess[(na, nb)] = ex
    world = {}
    for obj in pieces:
        verts = packed[obj.name][0]
        # overlap() against each obstacle, as the standing rule requires.
        bvh = packed[obj.name][1]
        for _oname, obvh, _box in obstacles:
            bvh.overlap(obvh)
        deep, _where = _mesh_world(obj, verts, obstacles)
        if deep > 0.0:
            world[obj.name] = deep
    return excess, world


def _worst(excess, world):
    ws = max(world.values()) if world else 0.0
    if ws > LIMIT:
        mesh = max(world.items(), key=lambda kv: kv[1])[0]
        return ("world", mesh, ws)
    if not excess:
        return None
    pair, ex = max(excess.items(), key=lambda kv: kv[1])
    if ex <= LIMIT:
        return None
    return ("self", pair, ex)


def _attempts(kind, payload):
    if kind == "world":
        return [("pull", 1.0)]
    na, nb = payload
    ba, bb = _bone(na), _bone(nb)
    fa, fb = _family(ba), _family(bb)
    sa, sb = _side(ba), _side(bb)
    out = []
    if fa == "leg" and fb == "leg" and sa and sb and sa != sb:
        out.append(("spread_legs", 8.0))
        out.append(("split_feet", 2.5))
    if fa == "leg" and fb == "leg" and sa and sa == sb:
        out.append(("open_knee", sa, 6.0))
    leg_side = sa if fa == "leg" else (sb if fb == "leg" else "")
    if (fa == "leg" and fb == "torso") or (fb == "leg" and fa == "torso"):
        out.append(("drop_knee", leg_side, 10.0))
        out.append(("unflex_leg", leg_side, 14.0))
        out.append(("abduct_leg", leg_side, 12.0))
    arm_side = sa if fa == "arm" else (sb if fb == "arm" else "")
    other = fb if fa == "arm" else fa
    if arm_side and other in ("leg", "torso", "arm"):
        out.append(("abduct_arm", arm_side, 6.0))
        out.append(("open_elbow", arm_side, 6.0))
    if fa == "arm" and fb == "arm" and sa and sb and sa != sb:
        out.append(("spread_arms", 6.0))
    return out


def _apply_attempt(arm, kind, payload, attempt, sign, scale, world_depth):
    name, *rest = attempt
    deg = (rest[-1] if rest else 6.0) * scale * sign
    moved = []
    if kind == "world":
        mesh = payload
        bone = _bone(mesh)
        fam = _family(bone)
        side = _side(bone)
        step = min(world_depth, 0.025) + 0.003
        if fam == "arm" and side:
            wrist = p5._tail_w(arm, f"LowerArm_{side}")
            p5._point_bone(
                arm, f"LowerArm_{side}",
                Vector((wrist.x + step, wrist.y, wrist.z)),
            )
            moved = [f"Mesh_LowerArm_{side}", f"Mesh_Hand_{side}", f"Mesh_UpperArm_{side}"]
        elif fam == "leg" and side:
            ankle = p5._tail_w(arm, f"LowerLeg_{side}")
            p5._point_bone(
                arm, f"LowerLeg_{side}",
                Vector((ankle.x + step, ankle.y, max(ankle.z, 0.02))),
            )
            sole = p5._foot_point(f"Foot_{side}", "floor")
            if sole is not None and sole.z < 0.0:
                p5._seat_end(
                    arm, f"Foot_{side}",
                    lambda s=side: p5._foot_point(f"Foot_{s}", "floor"),
                    Vector((sole.x, sole.y, 0.0)),
                )
            moved = [f"Mesh_UpperLeg_{side}", f"Mesh_LowerLeg_{side}", f"Mesh_Foot_{side}"]
        else:
            arm.location.x += step
            bpy.context.view_layer.update()
            moved = ["*"]
        return moved
    if name == "spread_legs":
        caps = {s: _capture_leg(arm, s) for s in ("L", "R")}
        _add(arm, "UpperLeg_L", 2, deg)
        _add(arm, "UpperLeg_R", 2, -deg)
        bpy.context.view_layer.update()
        for s in ("L", "R"):
            _restore_leg(arm, s, caps[s])
        return [f"Mesh_{p}_{s}" for p in ("UpperLeg", "LowerLeg", "Foot") for s in ("L", "R")]
    if name == "abduct_leg":
        side = rest[0]
        cap = _capture_leg(arm, side)
        _add(arm, f"UpperLeg_{side}", 2, deg)
        bpy.context.view_layer.update()
        _restore_leg(arm, side, cap)
        return [f"Mesh_{p}_{side}" for p in ("UpperLeg", "LowerLeg", "Foot")]
    if name == "unflex_leg":
        side = rest[0]
        cap = _capture_leg(arm, side)
        _add(arm, f"UpperLeg_{side}", 0, deg)
        bpy.context.view_layer.update()
        _restore_leg(arm, side, cap)
        return [f"Mesh_{p}_{side}" for p in ("UpperLeg", "LowerLeg", "Foot")]
    if name == "drop_knee":
        side = rest[0]
        cap = _capture_leg(arm, side)
        _add(arm, f"UpperLeg_{side}", 0, deg)
        _add(arm, f"UpperLeg_{side}", 2, deg if side == "L" else -deg)
        bpy.context.view_layer.update()
        _restore_leg(arm, side, cap)
        return [f"Mesh_{p}_{side}" for p in ("UpperLeg", "LowerLeg", "Foot")]
    if name == "split_feet":
        cm = rest[-1] * scale * sign
        lat = arm.matrix_world.to_3x3() @ Vector((1.0, 0.0, 0.0))
        if lat.length < 1.0e-6:
            return []
        lat.normalize()
        for side, s in (("L", 1.0), ("R", -1.0)):
            ankle, sole = _capture_leg(arm, side)
            shift = lat * (cm * 0.01 * s)
            knee = p5._head_w(arm, f"LowerLeg_{side}")
            hip = p5._head_w(arm, f"UpperLeg_{side}")
            pole = (knee - hip) + lat * s * 0.4
            p5._solve_chain(
                arm, f"UpperLeg_{side}", f"LowerLeg_{side}", ankle + shift, pole,
            )
            if sole is not None:
                p5._seat_end(
                    arm, f"Foot_{side}",
                    lambda sd=side: p5._foot_point(f"Foot_{sd}", "floor"),
                    sole + shift,
                )
        return [f"Mesh_{p}_{s}" for p in ("UpperLeg", "LowerLeg", "Foot") for s in ("L", "R")]
    if name == "open_knee":
        side = rest[0]
        cap = _capture_leg(arm, side)
        _add(arm, f"LowerLeg_{side}", 0, deg)
        bpy.context.view_layer.update()
        _restore_leg(arm, side, cap)
        return [f"Mesh_LowerLeg_{side}", f"Mesh_Foot_{side}"]
    if name == "abduct_arm":
        side = rest[0]
        _add(arm, f"UpperArm_{side}", 2, deg)
        bpy.context.view_layer.update()
        return [f"Mesh_UpperArm_{side}", f"Mesh_LowerArm_{side}", f"Mesh_Hand_{side}"]
    if name == "open_elbow":
        side = rest[0]
        _add(arm, f"LowerArm_{side}", 0, deg)
        bpy.context.view_layer.update()
        return [f"Mesh_LowerArm_{side}", f"Mesh_Hand_{side}"]
    if name == "spread_arms":
        _add(arm, "UpperArm_L", 2, deg)
        _add(arm, "UpperArm_R", 2, -deg)
        bpy.context.view_layer.update()
        return [f"Mesh_{p}_{s}" for p in ("UpperArm", "LowerArm", "Hand") for s in ("L", "R")]
    return []


def _refresh(arm, moved, pieces, locals_c, polys_c, bind, obstacles, excess, world):
    if "*" in moved:
        ex, wo = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
        excess.clear()
        excess.update(ex)
        world.clear()
        world.update(wo)
        return
    moved_set = set(moved)
    names = [obj.name for obj in pieces]
    for na in names:
        if na not in moved_set:
            continue
        oa = bpy.data.objects[na]
        va, ba = _pack(oa, locals_c, polys_c)
        for nb in names:
            if nb == na:
                continue
            key = tuple(sorted((na, nb)))
            ob = bpy.data.objects[nb]
            vb, bb = _pack(ob, locals_c, polys_c)
            joint = nc._shared_joint(arm, oa.parent_bone, ob.parent_bone)
            da = nc._inside_depths(va, polys_c[na], ba, bb, joint)
            db = nc._inside_depths(vb, polys_c[nb], bb, ba, joint)
            depth = 0.0
            if da:
                depth = max(depth, max(da.values()))
            if db:
                depth = max(depth, max(db.values()))
            base = bind.get(key, 0.0)
            ex = depth - base
            if ex < 0.0:
                ex = 0.0
            if ex > 1.0e-6:
                excess[key] = ex
            else:
                excess.pop(key, None)
        bvh = ba
        for _oname, obvh, _box in obstacles:
            bvh.overlap(obvh)
        deep, _w = _mesh_world(oa, va, obstacles)
        if deep > 0.0:
            world[na] = deep
        else:
            world.pop(na, None)


def _moved_world(moved, locals_c, polys_c, obstacles, world):
    """True when a nudge drives some mesh past the 0.5 cm world limit."""
    if "*" in moved:
        return False
    for name in moved:
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        verts, bvh = _pack(obj, locals_c, polys_c)
        for _oname, obvh, _box in obstacles:
            bvh.overlap(obvh)
        deep, _where = _mesh_world(obj, verts, obstacles)
        if deep > LIMIT and deep > world.get(name, 0.0) + 0.001:
            return True
    return False


def _blend_toward(arm, bone, deg_xyz, amount):
    pb = arm.pose.bones.get(bone)
    if pb is None:
        return
    pb.rotation_mode = "XYZ"
    e = list(pb.rotation_euler)
    for i in range(3):
        target = math.radians(deg_xyz[i])
        e[i] = e[i] + (target - e[i]) * amount
    pb.rotation_euler = tuple(e)


def _plant_soles(arm):
    import render_hier_pass5 as p5
    low = None
    for side in ("L", "R"):
        sole = p5._foot_point(f"Foot_{side}", "floor")
        if sole is None:
            continue
        if low is None or sole.z < low:
            low = sole.z
    if low is not None and low < -0.002:
        arm.location.z -= low
        bpy.context.view_layer.update()


# Where a folded limb should sit so the shells clear. Degrees.
_SAFE = {
    "UpperLeg_L": (-14.0, 0.0, 6.0),
    "UpperLeg_R": (-14.0, 0.0, -6.0),
    "LowerLeg_L": (10.0, 0.0, 0.0),
    "LowerLeg_R": (10.0, 0.0, 0.0),
    "Foot_L": (10.0, 0.0, 0.0),
    "Foot_R": (10.0, 0.0, 0.0),
    "UpperArm_L": (0.0, 0.0, -18.0),
    "UpperArm_R": (0.0, 0.0, 18.0),
    "LowerArm_L": (-8.0, 0.0, 0.0),
    "LowerArm_R": (-8.0, 0.0, 0.0),
    "Hand_L": (0.0, 0.0, 0.0),
    "Hand_R": (0.0, 0.0, 0.0),
    "Head": (0.0, 0.0, 0.0),
    "Neck": (0.0, 0.0, 0.0),
}


# A pose that measured under 0.5 cm on this mesh: hips only slightly flexed,
# knees soft, arms near the bind pose. Deeper folds put the thigh shell
# through the torso, and solving the ankle back to the old plant puts the
# fold back. Hips yaw is not in this table — clip 20's turn lives there.
# Measured on this mesh. Hip flexion of -12° and -24° stay under 0.5 cm
# with the knee soft and the thigh uncrossed. -16° and -20° do not: the
# thigh shell passes through the pelvis on the way there, so a partial
# blend that stops in that pocket is rejected and the pose is set here.
_CLEAR = {
    "UpperLeg_L": (-24.0, 0.0, 0.0),
    "UpperLeg_R": (-24.0, 0.0, 0.0),
    "LowerLeg_L": (8.0, 0.0, 0.0),
    "LowerLeg_R": (8.0, 0.0, 0.0),
    "Foot_L": (12.0, 0.0, 0.0),
    "Foot_R": (12.0, 0.0, 0.0),
    "UpperArm_L": (0.0, 0.0, 0.0),
    "UpperArm_R": (0.0, 0.0, 0.0),
    "LowerArm_L": (0.0, 0.0, 0.0),
    "LowerArm_R": (0.0, 0.0, 0.0),
    "Hand_L": (0.0, 0.0, 0.0),
    "Hand_R": (0.0, 0.0, 0.0),
    "Head": (0.0, 0.0, 0.0),
    "Neck": (0.0, 0.0, 0.0),
}


def _self_max(excess):
    return max(excess.values()) if excess else 0.0


def _world_max(world):
    return max(world.values()) if world else 0.0


def _blend_failing(arm, excess, t):
    """Ease every overlapping limb toward _CLEAR in one step.

    Both thighs move together. Opening one side and re-solving its ankle
    used to leave the other thigh, or put the opened hip straight back.
    Hips yaw is left alone so a keyed turn stays a turn.
    """
    limbs = set()
    torsos = set()
    for (na, nb), ex in excess.items():
        if ex <= LIMIT:
            continue
        for mesh in (na, nb):
            bone = _bone(mesh)
            if bone == "Hips":
                continue
            if bone in ("Spine", "Chest", "Neck", "Head"):
                torsos.add(bone)
            else:
                limbs.add(bone)
    done = set()
    extras = []
    for bone in limbs:
        _blend_toward(arm, bone, _CLEAR.get(bone, (0.0, 0.0, 0.0)), t)
        done.add(bone)
        side = _side(bone)
        if bone.startswith(("UpperLeg", "LowerLeg", "Foot")) and side:
            extras.extend((f"UpperLeg_{side}", f"LowerLeg_{side}", f"Foot_{side}"))
        if bone.startswith(("UpperArm", "LowerArm", "Hand")) and side:
            extras.extend((f"UpperArm_{side}", f"LowerArm_{side}", f"Hand_{side}"))
    for bone in extras:
        if bone in done:
            continue
        _blend_toward(arm, bone, _CLEAR.get(bone, (0.0, 0.0, 0.0)), t)
        done.add(bone)
    for bone in torsos:
        pb = arm.pose.bones.get(bone)
        if pb is None:
            continue
        pb.rotation_mode = "XYZ"
        e = list(pb.rotation_euler)
        e[0] *= (1.0 - 0.8 * t)
        e[1] *= (1.0 - t)
        e[2] *= (1.0 - t)
        pb.rotation_euler = tuple(e)
    bpy.context.view_layer.update()


def _force_clear(arm):
    """Last resort: the measured clear limb pose. Hips yaw stays."""
    for bone, deg in _CLEAR.items():
        pb = arm.pose.bones.get(bone)
        if pb is None:
            continue
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = tuple(math.radians(a) for a in deg)
    for bone in ("Spine", "Chest", "Neck", "Head"):
        pb = arm.pose.bones.get(bone)
        if pb is None:
            continue
        pb.rotation_mode = "XYZ"
        e = list(pb.rotation_euler)
        e[0] *= 0.15
        e[1] = 0.0
        e[2] = 0.0
        pb.rotation_euler = tuple(e)
    bpy.context.view_layer.update()
    _plant_soles(arm)


def _deepest(obj, locals_c, obstacles):
    verts = nc._world(locals_c[obj.name], obj.matrix_world)
    best = None
    best_n = None
    best_d = 0.0
    where = None
    for oname, obvh, (lo, hi) in obstacles:
        for v in verts:
            if not nc._in_aabb(v, lo, hi):
                continue
            loc, normal, _i, _d = obvh.find_nearest(v)
            if loc is None or normal.length < 1.0e-8:
                continue
            signed = (v - loc).dot(normal)
            if signed < -best_d:
                best_d = -signed
                best = v.copy()
                best_n = normal.normalized()
                where = oname
    return best, best_n, best_d, where


def _plane_push(obj, locals_c, obstacles):
    """How far to move this mesh back through the contact face.

    The wall's contact is its +X face and the ground's is its top. A vertex
    that has already passed through is outside the solid, so the overlap test
    misses it, and the nearest-face normal on a buried vertex points at the
    back face. Any vertex on the far side of the contact, and still in the
    slab's other two axes, is pushed back out the contact.
    """
    verts = nc._world(locals_c[obj.name], obj.matrix_world)
    push = Vector((0.0, 0.0, 0.0))
    margin = 0.003
    for oname, _obvh, (lo, hi) in obstacles:
        if oname == "wall":
            candidates = [
                v for v in verts
                if v.x < hi.x - margin
                and lo.y - 0.02 <= v.y <= hi.y + 0.02
                and lo.z - 0.02 <= v.z <= hi.z + 0.02
            ]
            if not candidates:
                continue
            need = (hi.x - margin) - min(v.x for v in candidates)
            if need > push.x:
                push.x = need
        elif oname == "ground":
            candidates = [
                v for v in verts
                if v.z < hi.z - margin
                and lo.x <= v.x <= hi.x
                and lo.y <= v.y <= hi.y
            ]
            if not candidates:
                continue
            need = (hi.z - margin) - min(v.z for v in candidates)
            if need > push.z:
                push.z = need
    return push


def _pull_mesh(arm, mesh, locals_c, obstacles):
    """Rotate the owning bone so the mesh clears the contact face.

    The parent hip or shoulder is not touched, so a wall pull cannot fold
    the limb back into the torso.
    """
    obj = bpy.data.objects.get(mesh)
    if obj is None:
        return False
    push = _plane_push(obj, locals_c, obstacles)
    if push.length < 1.0e-4:
        return False
    verts = nc._world(locals_c[obj.name], obj.matrix_world)
    axis = push.normalized()
    vert = min(verts, key=lambda v: v.dot(axis))
    bone = obj.parent_bone or _bone(mesh)
    pb = arm.pose.bones.get(bone)
    if pb is None:
        return False
    head = p5._head_w(arm, bone)
    if (vert - head).length < 0.035 and pb.parent is not None and pb.parent.name != "Root":
        bone = pb.parent.name
    lock = vert + push

    def point_fn(obj=obj, locals_c=locals_c, axis=axis):
        cur = nc._world(locals_c[obj.name], obj.matrix_world)
        return min(cur, key=lambda v: v.dot(axis))

    p5._seat_end(arm, bone, point_fn, lock)
    return True


def _shift_out(arm, world, locals_c, obstacles):
    """Move the whole preview capsule back out through the contact face.

    Meshes that have already passed through the wall are included, not only
    the ones the overlap test still reports.
    """
    push = Vector((0.0, 0.0, 0.0))
    names = list(world.keys())
    for name in locals_c:
        if name.startswith("Mesh_"):
            names.append(name)
    seen = set()
    for mesh in names:
        if mesh in seen:
            continue
        seen.add(mesh)
        obj = bpy.data.objects.get(mesh)
        if obj is None:
            continue
        part = _plane_push(obj, locals_c, obstacles)
        if part.x > push.x:
            push.x = part.x
        if part.z > push.z:
            push.z = part.z
    if push.length < 1.0e-4:
        return False
    arm.location = arm.location + push
    bpy.context.view_layer.update()
    _plant_soles(arm)
    return True


def _clear_posed(arm, pieces, locals_c, polys_c, bind, has_wall, wall_top):
    obstacles = _obstacle_bvh(arm, has_wall, wall_top, locals_c, polys_c)
    excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
    if _worst(excess, world) is None:
        return excess, world

    for step in range(8):
        if _self_max(excess) <= LIMIT:
            break
        before = _self_max(excess)
        pairs = dict(excess)
        snap = _snap(arm)
        t = 1.0 if step >= 5 else 0.55
        _blend_failing(arm, pairs, t)
        _plant_soles(arm)
        excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
        if _self_max(excess) <= before - 0.0003:
            continue
        _restore(arm, snap)
        if t < 1.0:
            _blend_failing(arm, pairs, 1.0)
            _plant_soles(arm)
            excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
            if _self_max(excess) <= before - 0.0003:
                continue
            _restore(arm, snap)
        excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
        break

    if _self_max(excess) > LIMIT:
        before = _self_max(excess)
        snap = _snap(arm)
        _force_clear(arm)
        excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
        if _self_max(excess) > before - 0.0003:
            _restore(arm, snap)
            excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)

    for _step in range(6):
        if _world_max(world) <= LIMIT:
            break
        before_w = _world_max(world)
        before_s = _self_max(excess)
        world_before = dict(world)
        snap = _snap(arm)
        pulled = False
        for mesh, depth in world_before.items():
            if depth > LIMIT:
                pulled = _pull_mesh(arm, mesh, locals_c, obstacles) or pulled
        _plant_soles(arm)
        if not pulled:
            _shift_out(arm, world_before, locals_c, obstacles)
        excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
        better = (
            _world_max(world) < before_w - 0.0003
            and _self_max(excess) <= max(before_s, LIMIT) + 0.001
        )
        if better:
            continue
        _restore(arm, snap)
        _shift_out(arm, world_before, locals_c, obstacles)
        excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
        better = (
            _world_max(world) < before_w - 0.0003
            and _self_max(excess) <= max(before_s, LIMIT) + 0.001
        )
        if better:
            continue
        _restore(arm, snap)
        excess, world = _fill(arm, pieces, locals_c, polys_c, bind, obstacles)
        break
    return excess, world


def _jobs():
    jobs = []
    doc5 = json.load(open(nc.PASS5_JSON))
    for cid, entry in doc5["clips"].items():
        jobs.append(("pass5", cid, entry, nc.PASS5_JSON))
    doc6 = json.load(open(nc.PASS6_JSON))
    for cid, entry in doc6["clips"].items():
        jobs.append(("pass6", cid, entry, nc.PASS6_JSON))
    only = {s for s in os.environ.get("NOCLIP_ONLY", "").split(",") if s}
    if only:
        jobs = [j for j in jobs if j[1] in only]
    return jobs


def _read_bones(arm, orig):
    out = dict(orig)
    for pb in arm.pose.bones:
        if pb.name == "Root":
            continue
        pb.rotation_mode = "XYZ"
        ang = [round(math.degrees(a), 2) for a in pb.rotation_euler]
        old = orig.get(pb.name, [0.0, 0.0, 0.0])
        if max(abs(ang[i] - float(old[i])) for i in range(3)) > 0.05:
            out[pb.name] = ang
    return out


def clear_clips():
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    pieces = nc._pieces(arm)
    locals_c = {obj.name: nc._local_coords(obj) for obj in pieces}
    polys_c = {obj.name: nc._polys(obj) for obj in pieces}
    bind = nc._bind_self(arm, pieces, locals_c, polys_c)
    docs = {}
    changed = {}
    for which, cid, entry, path in _jobs():
        facing = nc._pass5_facing(cid) if which == "pass5" else nc._pass6_facing(cid)
        has_wall, wall_top = nc._wall_plan(arm, which, cid, entry, facing)
        n_fixed = 0
        worst_left = 0.0
        for i, key in enumerate(entry["keys"]):
            nc._apply_key(arm, key["bones"], entry["capsule_preview_m"][i], facing)
            before = _snap(arm)
            excess, world = _clear_posed(
                arm, pieces, locals_c, polys_c, bind, has_wall, wall_top,
            )
            w = _worst(excess, world)
            left = 0.0 if w is None else w[2]
            worst_left = max(worst_left, left)
            if i == 0 or i == len(entry["keys"]) - 1 or left > LIMIT:
                nc._flush(
                    f"  {cid} f={i} left_cm={left * 100:.2f} "
                    f"{'' if w is None else w[0] + ' ' + str(w[1])}"
                )
            after_pose, after_loc = _snap(arm)
            bones_moved = False
            for name, eul in after_pose.items():
                old = before[0][name]
                if max(abs(eul[i] - old[i]) for i in range(3)) > 1.0e-4:
                    bones_moved = True
                    break
            if bones_moved or (after_loc - before[1]).length > 1.0e-4:
                n_fixed += 1
                key["bones"] = _read_bones(arm, key["bones"])
                c = arm.location
                entry["capsule_preview_m"][i] = [round(c.x, 4), round(c.y, 4), round(c.z, 4)]
        nc._flush(
            f"CLEAR {cid} frames_edited={n_fixed} left_cm={worst_left * 100:.2f}"
        )
        if path not in docs:
            docs[path] = json.load(open(path))
        docs[path]["clips"][cid] = entry
        changed[path] = True
    for path, doc in docs.items():
        with open(path, "w") as f:
            json.dump(doc, f)
        nc._flush("wrote " + path)
    rh._reset(arm)
    rh._hide_wall()


def _red():
    mat = bpy.data.materials.get("NoclipHot")
    if mat is not None:
        return mat
    mat = bpy.data.materials.new("NoclipHot")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    emit = nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (1.0, 0.04, 0.03, 1.0)
    emit.inputs["Strength"].default_value = 6.0
    links.new(emit.outputs["Emission"], out.inputs["Surface"])
    return mat


def _tint(names):
    saved = {}
    mat = _red()
    for name in names:
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        saved[name] = [slot.material for slot in obj.material_slots]
        obj.data.materials.clear()
        obj.data.materials.append(mat)
    return saved


def _untint(saved):
    for name, mats in saved.items():
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        obj.data.materials.clear()
        for mat in mats:
            obj.data.materials.append(mat)


def render_stills(phase):
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 1200
    rh._hide_marks()
    docs = {
        "pass5": json.load(open(nc.PASS5_JSON)),
        "pass6": json.load(open(nc.PASS6_JSON)),
    }
    for which, cid, frame, meshes, view in SHOTS:
        entry = docs[which]["clips"][cid]
        facing = nc._pass5_facing(cid) if which == "pass5" else nc._pass6_facing(cid)
        has_wall, wall_top = nc._wall_plan(arm, which, cid, entry, facing)
        nc._apply_key(arm, entry["keys"][frame]["bones"], entry["capsule_preview_m"][frame], facing)
        if has_wall:
            rh._show_wall(0.0, wall_top, arm.location.y, 2.4)
        else:
            rh._hide_wall()
        bpy.context.view_layer.update()
        saved = _tint(meshes)
        cap = arm.location
        focus = (cap.x, cap.y, cap.z + 0.85)
        if view == "side":
            loc = (cap.x + 0.15, cap.y - 3.8, cap.z + 0.9)
            ortho = 2.7
        else:
            loc = (cap.x + 2.7, cap.y - 3.5, cap.z + 1.15)
            ortho = None
        folder = os.path.join(rh.OUT, which, "noclip")
        os.makedirs(folder, exist_ok=True)
        tag = cid.split("_")[0]
        path = os.path.join(folder, f"{phase}_{tag}_f{frame}.png")
        rh._shot(path, loc, focus, ortho=ortho)
        _untint(saved)
        nc._flush("still " + path)
    rh._reset(arm)
    rh._hide_wall()


def _contact_marks(arm, has_wall, wall_top):
    rh._hide_marks()
    if not has_wall:
        return
    ys = []
    for side in ("L", "R"):
        foot = p5._foot_point(f"Foot_{side}", "wall")
        if foot is not None and foot.x < 0.04:
            rh._show_foot_mark(0.0, foot.y, max(foot.z, 0.02), side)
            ys.append(foot.y)
        hand = p5._palm_point(arm, side, Vector((0.0, arm.location.y, arm.location.z)))
        if hand is not None and hand.x < 0.04:
            rh._show_foot_mark(0.0, hand.y, hand.z, side)
    if wall_top is not None and ys:
        rh._show_lip_mark(0.0, wall_top, sum(ys) / len(ys), 0.7)


def render_keyed_strips():
    """Eight-frame strips from the keyed JSON, without solving again."""
    import storror_pose as sp
    import render_hier_pass6 as p6
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 1200
    os.makedirs(p5.PREV, exist_ok=True)
    os.makedirs(p6.PREV, exist_ok=True)
    doc5 = json.load(open(nc.PASS5_JSON))
    for spec in sp.CLIPS:
        entry = doc5["clips"][spec["id"]]
        facing = nc._pass5_facing(spec["id"])
        has_wall, wall_top = nc._wall_plan(arm, "pass5", spec["id"], entry, facing)
        beats = p5._beats(spec, len(entry["keys"]), int(entry["hero_frame"]))
        slug = rh.SLUGS[spec["id"]]
        for s, i in enumerate(beats):
            nc._apply_key(arm, entry["keys"][i]["bones"], entry["capsule_preview_m"][i], facing)
            if has_wall:
                rh._show_wall(0.0, wall_top, arm.location.y, 2.4)
            else:
                rh._hide_wall()
            _contact_marks(arm, has_wall, wall_top)
            loc, look, _ortho = p5._shot_at(arm, spec, False)
            rh._shot(os.path.join(p5.PREV, f"{slug}_{s}.png"), loc, look)
            loc, look, ortho = p5._shot_at(arm, spec, True)
            rh._shot(os.path.join(p5.PREV, f"{slug}_{s}_side.png"), loc, look, ortho=ortho)
            nc._flush(f"strip pass5 {slug} {s} frame={i}")
        rh._reset(arm)
    p5.composite_pass5()
    doc6 = json.load(open(nc.PASS6_JSON))
    for spec in p6.build_specs():
        entry = doc6["clips"][spec["id"]]
        facing = nc._pass6_facing(spec["id"])
        has_wall, wall_top = nc._wall_plan(arm, "pass6", spec["id"], entry, facing)
        beats = p5._beats(spec, len(entry["keys"]), int(entry["hero_frame"]))
        slug = spec["slug"]
        for s, i in enumerate(beats):
            nc._apply_key(arm, entry["keys"][i]["bones"], entry["capsule_preview_m"][i], facing)
            if has_wall:
                rh._show_wall(0.0, wall_top, arm.location.y, 2.4)
            else:
                rh._hide_wall()
            _contact_marks(arm, has_wall, wall_top)
            loc, look, _ortho = p6._shot(arm, spec, False)
            rh._shot(os.path.join(p6.PREV, f"{slug}_{s}.png"), loc, look)
            loc, look, ortho = p6._shot(arm, spec, True)
            rh._shot(os.path.join(p6.PREV, f"{slug}_{s}_side.png"), loc, look, ortho=ortho)
            nc._flush(f"strip pass6 {slug} {s} frame={i}")
        rh._reset(arm)
    p6.composite_pass6()
    rh._hide_wall()


def clear_solved_frame(arm, face, wall_top):
    """Hook for solve_clip. face is 0 when a wall is in the shot, else None.

    Bind depth is measured on the rest pose. That reset is undone before the
    posed frame is edited, so a live solve does not lose the current key.
    """
    state = clear_solved_frame
    if not hasattr(state, "pieces"):
        snap = _snap(arm)
        state.pieces = nc._pieces(arm)
        state.locals = {obj.name: nc._local_coords(obj) for obj in state.pieces}
        state.polys = {obj.name: nc._polys(obj) for obj in state.pieces}
        state.bind = nc._bind_self(arm, state.pieces, state.locals, state.polys)
        _restore(arm, snap)
    has_wall = face is not None
    _clear_posed(
        arm, state.pieces, state.locals, state.polys, state.bind, has_wall, wall_top,
    )


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if "--stills" in argv:
        phase = argv[argv.index("--stills") + 1]
        render_stills(phase)
    elif "--clear" in argv:
        clear_clips()
    elif "--strips" in argv:
        render_keyed_strips()
    else:
        raise SystemExit("pass --clear, --strips, or --stills before|after")
