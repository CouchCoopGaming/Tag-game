#!/usr/bin/env python3
"""Put a planted foot or palm back on the wall without entering it.

The no-clip pass opens a fold that would put a shell inside the body. That
swings the plant off the face. This pass slides the preview capsule toward
the wall until the plant meets it, and swings a blocking hand or knee back
only while the 0.5 cm test still passes. Frames that were not planted are
left on their approach.
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Vector

import render_hier_v080_stills as rh
import render_hier_pass5 as p5
import noclip_proof as nc
import noclip_clear as nclear
import storror_pose as sp

ON_M = 0.04
FACE_M = -0.003


def _min_point(name):
    obj = bpy.data.objects.get(name)
    if obj is None:
        return None
    best = None
    mw = obj.matrix_world
    for v in obj.data.vertices:
        p = mw @ v.co
        if best is None or p.x < best.x:
            best = p
    return best


def _closest(pieces):
    best_x = 99.0
    best_name = None
    for obj in pieces:
        p = _min_point(obj.name)
        if p is not None and p.x < best_x:
            best_x = p.x
            best_name = obj.name
    return best_name, best_x


def _clear(arm, pieces, locals_c, polys_c, bind, obstacles):
    excess, world = nclear._fill(arm, pieces, locals_c, polys_c, bind, obstacles)
    return nclear._worst(excess, world) is None


_STOP = {"Root", "Hips", "Spine", "Chest", "Neck", "Head"}


def _swing_back(arm, mesh, dest_x, pieces, locals_c, polys_c, bind, obstacles):
    """Swing this shell back off the wall, walking up the chain if the
    owning bone cannot move the vertex. Keep the last step that stays clear."""
    bone = mesh[5:] if mesh.startswith("Mesh_") else mesh
    if bpy.data.objects.get(mesh) is None:
        return False
    moved_any = False
    for _ in range(3):
        pb = arm.pose.bones.get(bone)
        if pb is None:
            break
        cur = _min_point(mesh)
        if cur is None or cur.x >= dest_x - 0.005:
            break
        step = nclear._snap(arm)

        def point_fn(mesh=mesh):
            return _min_point(mesh)

        p5._seat_end(arm, bone, point_fn, Vector((dest_x, cur.y, cur.z)))
        after = _min_point(mesh)
        progressed = after is not None and after.x > cur.x + 0.005
        if progressed and _clear(arm, pieces, locals_c, polys_c, bind, obstacles):
            moved_any = True
            if after.x >= dest_x - 0.01:
                break
        else:
            nclear._restore(arm, step)
        parent = arm.pose.bones[bone].parent if arm.pose.bones.get(bone) else None
        if parent is None or parent.name in _STOP:
            break
        bone = parent.name
    return moved_any


def seat_plants(arm, pieces, locals_c, polys_c, bind, obstacles, plant_meshes):
    """Slide and un-block until a planted shell is on the face. True if kept."""
    if not plant_meshes:
        return False
    snap = nclear._snap(arm)
    before = min(_min_point(n).x for n in plant_meshes if _min_point(n) is not None)
    for _ in range(5):
        name, gx = _closest(pieces)
        if name is None or name in plant_meshes:
            break
        px = min(_min_point(n).x for n in plant_meshes)
        if gx >= px - 0.01:
            break
        if not _swing_back(arm, name, px, pieces, locals_c, polys_c, bind, obstacles):
            break
        after = _min_point(name)
        if after is None or after.x < gx + 0.005:
            break
    px = min(_min_point(n).x for n in plant_meshes)
    _name, gx = _closest(pieces)
    room = gx - FACE_M
    need = px - FACE_M
    shift = min(room, need)
    if shift > 0.002:
        arm.location.x -= shift
        bpy.context.view_layer.update()
    if not _clear(arm, pieces, locals_c, polys_c, bind, obstacles):
        nclear._restore(arm, snap)
        return False
    now = min(_min_point(n).x for n in plant_meshes)
    if now > before - 0.005:
        nclear._restore(arm, snap)
        return False
    return True


def _plants_from_pins(pins):
    meshes = []
    if not pins:
        return meshes
    for key, st in pins.items():
        if st.get("w", 0.0) < 0.5 or st.get("lock") is None:
            continue
        if st.get("kind") not in ("wall", "lip"):
            continue
        kind, side = key
        token = "Foot" if kind == "foot" else "Hand"
        meshes.append(f"Mesh_{token}_{side}")
    return meshes


def seat_solved_frame(arm, pins, face, wall_top):
    """Hook for solve_clip, after the no-clip edit. face is None off the wall."""
    if face is None:
        return
    meshes = _plants_from_pins(pins)
    if not meshes:
        return
    state = seat_solved_frame
    if not hasattr(state, "pieces"):
        state.pieces = nc._pieces(arm)
        state.locals = {obj.name: nc._local_coords(obj) for obj in state.pieces}
        state.polys = {obj.name: nc._polys(obj) for obj in state.pieces}
        snap = nclear._snap(arm)
        state.bind = nc._bind_self(arm, state.pieces, state.locals, state.polys)
        nclear._restore(arm, snap)
    obstacles = nclear._obstacle_bvh(arm, True, wall_top, state.locals, state.polys)
    seat_plants(
        arm, state.pieces, state.locals, state.polys, state.bind, obstacles, meshes,
    )


def pin_pass5(prenoclip_path):
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    pieces = nc._pieces(arm)
    locals_c = {obj.name: nc._local_coords(obj) for obj in pieces}
    polys_c = {obj.name: nc._polys(obj) for obj in pieces}
    bind = nc._bind_self(arm, pieces, locals_c, polys_c)
    old = json.load(open(prenoclip_path))
    new = json.load(open(nc.PASS5_JSON))
    for spec in sp.CLIPS:
        if spec["verb"] == "softland":
            continue
        cid = spec["id"]
        before = old["clips"][cid]
        entry = new["clips"][cid]
        facing = nc._pass5_facing(cid)
        has_wall, wall_top = nc._wall_plan(arm, "pass5", cid, entry, facing)
        if not has_wall:
            continue
        planted = []
        for i, key in enumerate(before["keys"]):
            nc._apply_key(arm, key["bones"], before["capsule_preview_m"][i], facing)
            meshes = []
            for side in ("L", "R"):
                foot = p5._foot_point(f"Foot_{side}", "wall")
                if foot is not None and foot.x < ON_M:
                    meshes.append(f"Mesh_Foot_{side}")
                cluster = rh._palm_cluster(arm, side)
                if cluster and min(v.x for v in cluster) < ON_M:
                    meshes.append(f"Mesh_Hand_{side}")
            planted.append(meshes)
        n_hit = 0
        gaps = []
        obstacles = None
        for i, key in enumerate(entry["keys"]):
            meshes = planted[i]
            nc._apply_key(arm, key["bones"], entry["capsule_preview_m"][i], facing)
            if obstacles is None:
                obstacles = nclear._obstacle_bvh(arm, True, wall_top, locals_c, polys_c)
            else:
                # Keep the wall under the capsule as it travels along Y.
                rh._show_wall(0.0, wall_top, arm.location.y, 2.4)
                bpy.context.view_layer.update()
                wall = bpy.data.objects["ActionWall"]
                wv = nc._world(locals_c["ActionWall"], wall.matrix_world)
                obstacles = [obstacles[0], ("wall", nc._bvh(wv, polys_c["ActionWall"]), nc._aabb(wv))]
            if meshes:
                if seat_plants(arm, pieces, locals_c, polys_c, bind, obstacles, meshes):
                    n_hit += 1
                    key["bones"] = nclear._read_bones(arm, key["bones"])
                    cpos = arm.location
                    entry["capsule_preview_m"][i] = [round(cpos.x, 4), round(cpos.y, 4), round(cpos.z, 4)]
            xs = [_min_point(n).x for n in meshes if _min_point(n) is not None]
            if xs:
                gaps.append(min(xs))
        near = min(gaps) if gaps else None
        nc._flush(
            f"PIN {cid} planted={sum(1 for m in planted if m)} "
            f"moved={n_hit} near_cm={None if near is None else round(near * 100, 1)}"
        )
    with open(nc.PASS5_JSON, "w") as f:
        json.dump(new, f)
    nc._flush("wrote " + nc.PASS5_JSON)
    rh._reset(arm)
    rh._hide_wall()


if __name__ == "__main__":
    src = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "/tmp/pass5_prenoclip.json"
    pin_pass5(src)
