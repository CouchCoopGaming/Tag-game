#!/usr/bin/env python3
"""Key three Storror reference clips on the Hier v0.8.0 rig.

Slide (17), vertical wall run (18, L/R corrected; 19 is timing only), and
wall-pop 180 (20). Bone keys are rotations. The armature location and yaw in
these stills are a preview path, not root motion.

  blender -b /tmp/Dummy_Mannequin_Hier_Hi_v080_Tan.blend -P Tools/Tag/key_storror_clips_1720.py -- --pass 1
  blender -b /tmp/Dummy_Mannequin_Hier_Hi_v080_Tan.blend -P Tools/Tag/key_storror_clips_1720.py -- --pass 1 --metrics
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import storror_pose as sp
import render_hier_v080_stills as rh
import render_hier_pass5 as p5

PASS = 1
OUT = "/workspace/Docs/HierStills/v080/clips/pass1"
PREV = "/tmp/hier_v080_clips_pass1"
FPS = 30.0
JOINT_EXEMPT_M = 0.03
DEPTH_LIMIT_M = 0.005

# Parent, child. The shared joint is the child bone's head.
NEIGHBORS = (
    ("Hips", "Spine"),
    ("Spine", "Chest"),
    ("Chest", "Neck"),
    ("Neck", "Head"),
    ("Chest", "Shoulder_L"),
    ("Chest", "Shoulder_R"),
    ("Shoulder_L", "UpperArm_L"),
    ("Shoulder_R", "UpperArm_R"),
    ("UpperArm_L", "LowerArm_L"),
    ("UpperArm_R", "LowerArm_R"),
    ("LowerArm_L", "Hand_L"),
    ("LowerArm_R", "Hand_R"),
    ("Hips", "UpperLeg_L"),
    ("Hips", "UpperLeg_R"),
    ("UpperLeg_L", "LowerLeg_L"),
    ("UpperLeg_R", "LowerLeg_R"),
    ("LowerLeg_L", "Foot_L"),
    ("LowerLeg_R", "Foot_R"),
)

# Readable action windows inside the source clips (seconds from each clip's t0).
# 17's first 0.40 s is a small, noisy figure. 18 after ~1.75 s is the stall
# at the top, not the run up. 20's last frames are a close-up with feet
# leaving the frame.
WINDOWS = {
    "17_slide_slope_crouch": (0.40, 1.40),
    "18_vertical_wallrun_back": (0.25, 1.75),
    "20_wallpop_180": (0.15, 1.75),
}

SPECS = (
    {
        "id": "17_slide_slope_crouch",
        "verb": "slide",
        "swap_lr": False,
        "strip": "17_slide_slope_crouch.png",
        "label": "17 slide",
    },
    {
        "id": "18_vertical_wallrun_back",
        "verb": "wallrun_vertical",
        "swap_lr": True,
        "strip": "18_vertical_wallrun_back.png",
        "label": "18 wall run up",
    },
    {
        "id": "20_wallpop_180",
        "verb": "turn180",
        "swap_lr": False,
        "strip": "20_wallpop_180.png",
        "label": "20 wall-pop 180",
    },
)


def _args():
    if "--" in sys.argv:
        return sys.argv[sys.argv.index("--") + 1:]
    return sys.argv[1:]


def _clamp(v, lo, hi):
    return max(lo, min(hi, v))


def _smoothstep(u):
    u = _clamp(u, 0.0, 1.0)
    return u * u * (3.0 - 2.0 * u)


def _swap_lr(ch):
    """Clip 18 is filmed from behind, so MediaPipe's left/right are swapped."""
    out = dict(ch)
    for key, val in ch.items():
        if key.endswith("_L"):
            out[key[:-2] + "_R"] = val
        elif key.endswith("_R"):
            out[key[:-2] + "_L"] = val
    return out


def _tame(ch):
    """Keep the reference shape, but stop folds the rigid shells cannot survive.

    A 150° hip flex on this mannequin puts the thigh through the pelvis.
    """
    out = dict(ch)
    for side in ("L", "R"):
        out[f"knee_flex_{side}"] = _clamp(out.get(f"knee_flex_{side}", 0.0), 4.0, 118.0)
        flex = out.get(f"hip_flex_{side}", 0.0)
        elev = out.get(f"hip_elev_{side}", 0.0)
        # Signed elevation is what becomes the thigh pitch.
        signed = elev if flex >= 0.0 else -elev
        signed = _clamp(signed, -15.0, 100.0)
        if flex >= 0.0:
            out[f"hip_elev_{side}"] = signed
            out[f"hip_flex_{side}"] = max(flex, 0.0)
        else:
            out[f"hip_elev_{side}"] = -signed
            out[f"hip_flex_{side}"] = min(flex, 0.0)
        out[f"elbow_flex_{side}"] = _clamp(out.get(f"elbow_flex_{side}", 0.0), 4.0, 110.0)
        # Shoulder yaw past this puts the upper-arm shell through the ribs.
        abd = out.get(f"shoulder_abd_lat_{side}", sp.BIND_ABD)
        out[f"shoulder_abd_lat_{side}"] = _clamp(abd, sp.BIND_ABD - 14.0, sp.BIND_ABD + 14.0)
    return out


def load_window(spec):
    """30 fps samples across the action window. Returns list of channel dicts and times."""
    data = sp.load_clip(spec["id"])
    curves = sp.clip_curves(spec["id"], keys=24)
    smooth = curves["raw_smooth"]
    t0 = float(data["times_s"][0])
    t1 = float(data["times_s"][-1])
    span = max(1.0e-4, t1 - t0)
    a, b = WINDOWS[spec["id"]]
    n = int(round((b - a) * FPS)) + 1
    samples = []
    times = []
    for i in range(n):
        t = a + (b - a) * (i / (n - 1))
        u = _clamp((t) / span, 0.0, 1.0)
        ch = sp.sample_raw(smooth, u)
        if spec["swap_lr"]:
            ch = _swap_lr(ch)
        samples.append(_tame(ch))
        times.append(t)
    return samples, times, data


def _clear_pose_channels(arm):
    """Drop location and scale so a later clip cannot inherit them."""
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)


def _apply_sample(arm, ch, location, rotation):
    import bpy
    rh._apply_eulers(arm, sp.blender_euler(sp.unity_pose(ch)))
    _clear_pose_channels(arm)
    arm.location = location
    arm.rotation_euler = rotation
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
        root.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()


def _deepest(token, normal):
    from mathutils import Vector
    verts = p5._verts(token)
    if not verts:
        return None
    n = Vector(normal)
    return min(verts, key=lambda v: n.dot(v))


def _pin_limb(arm, kind, side, lock, normal, pole):
    """Two-bone IK so the deepest mesh point sits on lock. Normal points out of the solid."""
    import bpy
    from mathutils import Vector
    upper, lower, end = p5._chain(kind, side)
    token = p5._token(kind, side)
    nrm = Vector(normal).normalized()
    lock = Vector(lock)
    nudge = Vector((0.0, 0.0, 0.0))

    def point():
        return _deepest(token, nrm)

    for _ in range(8):
        vert = point()
        if vert is None:
            return
        joint = p5._head_w(arm, end)
        p5._solve_chain(arm, upper, lower, lock + (joint - vert) + nudge, pole)
        p5._seat_end(arm, end, point, lock)
        # Matrix solves can leak scale. Keys are rotations, so the mesh must
        # hold the contact without it or the replay sinks into the floor.
        for name in (upper, lower, end):
            bone = arm.pose.bones[name]
            bone.location = (0.0, 0.0, 0.0)
            bone.scale = (1.0, 1.0, 1.0)
        bpy.context.view_layer.update()
        seated = point()
        if seated is None:
            return
        err = lock - seated
        normal_err = err.dot(nrm)
        tangent = err - nrm * normal_err
        if abs(normal_err) < 0.0025 and tangent.length < 0.008:
            break
        nudge += nrm * normal_err
        if tangent.length > 0.035:
            tangent = tangent.normalized() * 0.035
        nudge += tangent * 0.65
    bpy.context.view_layer.update()


def _foot_on_lock(arm, side, lock, normal, normal_slop=0.004, tangent_slop=0.008):
    """True when the sole is on the lock and not inside the solid."""
    from mathutils import Vector
    vert = _deepest(f"Foot_{side}", normal)
    if vert is None:
        return False
    err = Vector(vert) - Vector(lock)
    n = Vector(normal).normalized()
    along = err.dot(n)
    if along < -normal_slop:
        return False
    return (err - n * along).length <= tangent_slop


def _clear_thigh(arm, side, lock, normal, pole):
    """Yaw the thigh off the hip shell when the pose is still in the rest-pose corner.

    The v0.8.0 hip and thigh shells already cross by 0.59 cm at bind, and that
    crossing sits 3.2 cm from the joint, outside the exemption. A thigh yaw of
    about 12° swings the shell off that corner. The search keeps a candidate
    only when the sole is still on the same lock, so the plant does not skate.
    """
    pb = arm.pose.bones.get(f"UpperLeg_{side}")
    if pb is None or lock is None:
        return
    pb.rotation_mode = "XYZ"
    e = pb.rotation_euler
    if abs(e.z) >= math.radians(12.0) or abs(e.x) >= math.radians(18.0):
        return
    depth0, pair0 = _self_worst(arm)
    if depth0 <= DEPTH_LIMIT_M or f"UpperLeg_{side}" not in pair0:
        return
    from mathutils import Vector
    snap = _snap_rots(arm)
    loc = arm.location.copy()
    rot = arm.rotation_euler.copy()
    best = None
    best_depth = depth0
    for mag in (0.22, 0.38):
        for extra in (
            Vector((0.0, mag, 0.0)),
            Vector((0.0, -mag, 0.0)),
            Vector((mag, 0.0, 0.0)),
            Vector((-mag, 0.0, 0.0)),
        ):
            _restore_rots(arm, snap)
            arm.location = loc
            arm.rotation_euler = rot
            bpy_update()
            _pin_limb(arm, "foot", side, lock, normal, Vector(pole) + extra)
            if not _foot_on_lock(arm, side, lock, normal):
                continue
            depth, pair = _self_worst(arm)
            if f"UpperLeg_{side}" in pair and depth < best_depth - 0.0004:
                best_depth = depth
                best = _snap_rots(arm)
            elif depth <= DEPTH_LIMIT_M and depth < best_depth - 0.0004:
                best_depth = depth
                best = _snap_rots(arm)
                _restore_rots(arm, best)
                arm.location = loc
                arm.rotation_euler = rot
                bpy_update()
                return
    if best is None:
        _restore_rots(arm, snap)
    else:
        _restore_rots(arm, best)
    arm.location = loc
    arm.rotation_euler = rot
    bpy_update()


def _yaw_loose_thighs(arm, planes):
    """Yaw a thigh that is still in the bind-pose corner and is not holding a contact.

    The hip shell crosses the thigh by 0.59 cm at rest. About 12° of yaw swings
    that corner clear. A sole already on a plane is a plant, and it is put back
    if the yaw moves it.
    """
    from mathutils import Vector
    for side in ("L", "R"):
        pb = arm.pose.bones.get(f"UpperLeg_{side}")
        if pb is None:
            continue
        pb.rotation_mode = "XYZ"
        e = pb.rotation_euler.copy()
        if abs(e.z) >= math.radians(12.0) or abs(e.x) >= math.radians(18.0):
            continue
        planted = False
        for origin, normal in planes:
            n = Vector(normal).normalized()
            vert = _deepest(f"Foot_{side}", n)
            if vert is None:
                continue
            gap = n.dot(Vector(vert) - Vector(origin))
            if -0.008 <= gap <= 0.03:
                planted = True
                break
        if planted:
            continue
        depth0, pair0 = _self_worst(arm)
        if depth0 <= DEPTH_LIMIT_M or f"UpperLeg_{side}" not in pair0:
            continue
        snap = _snap_rots(arm)
        loc = arm.location.copy()
        target = math.copysign(math.radians(12.0), e.z if abs(e.z) > math.radians(1.0) else (1.0 if side == "L" else -1.0))
        pb.rotation_euler = (e.x, e.y, target)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
        bpy_update()
        depth1, _pair1 = _self_worst(arm)
        keep = depth1 < depth0 - 0.0004
        for origin, normal in planes:
            if _min_plane(origin, normal) < -0.004:
                keep = False
                break
        if not keep:
            _restore_rots(arm, snap)
            arm.location = loc
            bpy_update()


def _untwist_planted(arm, side, normal, lock_vert):
    """Take twist out of a planted thigh and put the sole back on the same point.

    Euler Y on the thigh is the twist that nests the hip shell. Aiming the shin
    at the captured ankle reaches the old contact without that twist, when the
    chain can still get there.
    """
    from mathutils import Vector
    if lock_vert is None:
        return
    n = Vector(normal).normalized()
    pb = arm.pose.bones.get(f"UpperLeg_{side}")
    if pb is None:
        return
    pb.rotation_mode = "XYZ"
    e0 = pb.rotation_euler.copy()
    if abs(e0.y) < math.radians(4.0):
        return
    ankle = p5._tail_w(arm, f"LowerLeg_{side}").copy()
    snap = _snap_rots(arm)
    loc = arm.location.copy()
    best = None
    best_depth, _pair0 = _self_worst(arm)
    for scale in (0.0, 0.35, 0.65):
        _restore_rots(arm, snap)
        arm.location = loc
        bpy_update()
        pb = arm.pose.bones[f"UpperLeg_{side}"]
        pb.rotation_mode = "XYZ"
        e = pb.rotation_euler.copy()
        pb.rotation_euler = (e.x, e0.y * scale, e.z)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
        bpy_update()
        p5._point_bone(arm, f"LowerLeg_{side}", ankle)
        lower = arm.pose.bones[f"LowerLeg_{side}"]
        lower.location = (0.0, 0.0, 0.0)
        lower.scale = (1.0, 1.0, 1.0)
        bpy_update()

        def point(side=side):
            return _deepest(f"Foot_{side}", n)

        for _ in range(4):
            p5._seat_end(arm, f"Foot_{side}", point, lock_vert)
            foot_b = arm.pose.bones[f"Foot_{side}"]
            foot_b.location = (0.0, 0.0, 0.0)
            foot_b.scale = (1.0, 1.0, 1.0)
            bpy_update()
        foot = point()
        if foot is None:
            continue
        err = Vector(foot) - Vector(lock_vert)
        along = err.dot(n)
        tangent = (err - n * along).length
        if along < -0.0015 or along > 0.0035 or tangent > 0.006:
            continue
        depth, _pair = _self_worst(arm)
        if depth < best_depth - 0.0004:
            best_depth = depth
            best = _snap_rots(arm)
            if depth <= DEPTH_LIMIT_M:
                break
    if best is None:
        _restore_rots(arm, snap)
    else:
        _restore_rots(arm, best)
    arm.location = loc
    bpy_update()


def _pole_knee(side, normal, out_axis):
    from mathutils import Vector
    n = Vector(normal).normalized()
    out = Vector(out_axis)
    if side == "R":
        out = -out
    pole = n * 0.35 + out * 0.85 + Vector((0.0, 0.0, 0.25))
    return pole


def _save_pose(arm):
    import math as _m
    bones = {}
    for pb in arm.pose.bones:
        if pb.name == "Root":
            continue
        pb.rotation_mode = "XYZ"
        e = pb.rotation_euler
        bones[pb.name] = (math.degrees(e.x), math.degrees(e.y), math.degrees(e.z))
    return bones


def _along(point, origin, direction):
    from mathutils import Vector
    return Vector(direction).dot(Vector(point) - Vector(origin))


# --- world surfaces -------------------------------------------------------

SLOPE_DEG = 36.0


def _slope_frame():
    """Steep ramp. Downslope is -Y. The surface is z = (y - Y0) * tan(angle), z>=0."""
    from mathutils import Vector
    ang = math.radians(SLOPE_DEG)
    y0 = -0.55
    normal = Vector((0.0, -math.sin(ang), math.cos(ang)))
    down = Vector((0.0, -math.cos(ang), -math.sin(ang)))
    return ang, y0, normal, down


def _slope_z(y, y0, ang):
    return max(0.0, (y - y0) * math.tan(ang))


def _project_plane(point, origin, normal):
    from mathutils import Vector
    n = Vector(normal).normalized()
    p = Vector(point)
    return p - n * n.dot(p - Vector(origin))


def _all_body_verts():
    # Props (the ramp, the wall) are meshes too. Only the mannequin shells count.
    return list(rh._iter_world_verts(lambda name: name.startswith("Mesh_")))


def _min_plane(origin, normal, verts=None):
    """Smallest signed distance to the plane. Negative is inside the solid."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    o = Vector(origin)
    best = 1.0e9
    src = verts if verts is not None else _all_body_verts()
    for v in src:
        best = min(best, n.dot(Vector(v) - o))
    return best


def _shift_plane(arm, origin, normal, clearance=0.0):
    """Move the armature along the normal until no body vert is inside the plane."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    gap = _min_plane(origin, n)
    if gap >= clearance or gap > 1.0e8:
        return 0.0
    arm.location = arm.location + n * (clearance - gap)
    bpy_update()
    return clearance - gap


def _make_box(name, location, scale, color):
    import bpy
    ob = bpy.data.objects.get(name)
    if ob is None:
        bpy.ops.mesh.primitive_cube_add(location=location)
        ob = bpy.context.active_object
        ob.name = name
        mat = bpy.data.materials.new(name + "Mat")
        mat.use_nodes = True
        mat.diffuse_color = color
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = color
            bsdf.inputs["Roughness"].default_value = 0.92
        ob.data.materials.append(mat)
    ob.location = location
    ob.scale = scale
    ob.rotation_euler = (0.0, 0.0, 0.0)
    ob.hide_render = False
    ob.hide_set(False)
    return ob


def _hide_named(names):
    import bpy
    for name in names:
        ob = bpy.data.objects.get(name)
        if ob is not None:
            ob.hide_render = True
            ob.hide_set(True)


def show_slide_world(_y_center):
    """Closed ramp whose top face is the slide plane. Built once, in world space."""
    import bpy
    from mathutils import Vector
    ang, y0, normal, _down = _slope_frame()
    ob = bpy.data.objects.get("ActionSlope")
    if ob is None:
        y_a, y_b = -0.8, 2.2
        x_a, x_b = -1.6, 1.6
        thick = 0.40
        corners = ((x_a, y_a), (x_b, y_a), (x_b, y_b), (x_a, y_b))

        def top(x, y):
            return Vector((x, y, _slope_z(y, y0, ang)))

        verts = [tuple(top(x, y)) for x, y in corners]
        verts += [tuple(top(x, y) - normal * thick) for x, y in corners]
        # Top winds with the outward normal. Bottom and sides close the slab.
        # Top winding follows the outward normal (0, -sin, cos). Bottom faces out the other way.
        faces = [
            (0, 1, 2, 3),
            (4, 7, 6, 5),
            (0, 4, 5, 1),
            (1, 5, 6, 2),
            (2, 6, 7, 3),
            (3, 7, 4, 0),
        ]
        mesh = bpy.data.meshes.new("ActionSlopeMesh")
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        ob = bpy.data.objects.new("ActionSlope", mesh)
        bpy.context.scene.collection.objects.link(ob)
        mat = bpy.data.materials.new("SlopeMat")
        mat.use_nodes = True
        mat.diffuse_color = (0.38, 0.36, 0.34, 1)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.38, 0.36, 0.34, 1)
            bsdf.inputs["Roughness"].default_value = 0.94
        ob.data.materials.append(mat)
    ob.hide_render = False
    ob.hide_set(False)
    _hide_named(("ActionWall", "LandingWall"))
    ground = bpy.data.objects.get("Ground")
    if ground is not None:
        ground.hide_render = True
        ground.hide_set(True)


def show_wall_world(face_x, center_z, length_z=4.2):
    import bpy
    # Solid occupies x < face_x. The +X face is the contact. Scale is half-extents
    # because the primitive cube is 2 m on a side (scale 1 → 2 m).
    half_thick = 0.12
    _make_box(
        "ActionWall",
        (face_x - half_thick, 0.0, center_z),
        (half_thick, 2.4, length_z * 0.5),
        (0.42, 0.40, 0.38, 1),
    )
    _hide_named(("ActionSlope", "LandingWall"))
    ground = bpy.data.objects.get("Ground")
    if ground is not None:
        ground.hide_render = False
        ground.hide_set(False)


def show_turn_world(face_x, landing_top, landing_y):
    import bpy
    show_wall_world(face_x, 1.6, 3.6)
    # Low wall he lands on, opposite the plant.
    _make_box(
        "LandingWall",
        (0.35, landing_y, landing_top * 0.5),
        (0.55, 0.38, landing_top * 0.5),
        (0.46, 0.44, 0.40, 1),
    )
    slope = bpy.data.objects.get("ActionSlope")
    if slope is not None:
        slope.hide_render = True
        slope.hide_set(True)


# --- solvers --------------------------------------------------------------

def _foot_local_lock(arm, side, normal, plane_origin):
    """Contact point in armature-local space, on the plane."""
    from mathutils import Vector
    token = f"Foot_{side}"
    vert = _deepest(token, normal)
    if vert is None:
        return None
    on = _project_plane(vert, plane_origin, normal)
    return arm.matrix_world.inverted() @ Vector(on)


def _pin_on_plane(arm, kind, side, lock, normal, pole):
    _pin_limb(arm, kind, side, lock, normal, pole)


def solve_slide(arm, samples, times):
    """Both feet ride the ramp with the capsule. The plane is the unclamped slope."""
    from mathutils import Vector, Euler
    _ang, y0, normal, down = _slope_frame()
    plane_o = Vector((0.0, y0, 0.0))
    nrm = Vector(normal).normalized()
    start = _project_plane(Vector((0.0, 1.45, 1.0)), plane_o, nrm)
    travel = 2.05
    n = len(samples)
    keys = []
    caps = []
    facings = []
    skate = 0.0
    pen = 0.0
    foot_off = {}
    hand_off = None
    hand_side = None
    for i, ch in enumerate(samples):
        u = i / (n - 1)
        crouch = _smoothstep(u / 0.28) * (1.0 - _smoothstep((u - 0.72) / 0.28))
        lean = math.radians(14.0 + 16.0 * crouch)
        # Negative X lays the chest back up the slope (head toward +Y).
        facing = Euler((-lean, 0.0, 0.0), "XYZ")
        anchor = start + down * (u * travel)
        _apply_sample(arm, ch, anchor, facing)
        _shift_plane(arm, plane_o, nrm, 0.0)
        show_slide_world(arm.location.y)
        if not foot_off:
            base0 = _project_plane(arm.location, plane_o, nrm)
            for side in ("L", "R"):
                vert = _deepest(f"Foot_{side}", nrm)
                if vert is None:
                    continue
                foot_off[side] = _project_plane(vert, plane_o, nrm) - base0
            if "L" in foot_off and "R" in foot_off:
                gap_x = foot_off["L"].x - foot_off["R"].x
                if abs(gap_x) < 0.18:
                    mid = 0.5 * (foot_off["L"].x + foot_off["R"].x)
                    foot_off["L"].x = mid + 0.16
                    foot_off["R"].x = mid - 0.16
            wrists = []
            for side in ("L", "R"):
                w = _deepest(f"Hand_{side}", nrm)
                if w is not None:
                    wrists.append((nrm.dot(Vector(w) - plane_o), side, w))
            if wrists:
                wrists.sort(key=lambda item: item[0])
                hand_side = wrists[0][1]
                hand_off = _project_plane(wrists[0][2], plane_o, nrm) - base0
                # Keep the palm beside the hip so it is on the ramp, not under the chest.
                if abs(hand_off.x) < 0.30:
                    hand_off.x = 0.34 if hand_side == "L" else -0.34
        base = _project_plane(arm.location, plane_o, nrm)

        def plant(base_now, clear=False):
            for side, off in foot_off.items():
                # No sideways pole. A lateral knee swing yawed the thigh through the pelvis.
                pole = nrm * 1.0 + Vector((0.0, 0.05 if side == "L" else -0.05, 0.55))
                lock = base_now + off
                _pin_on_plane(arm, "foot", side, lock, nrm, pole)
                if clear:
                    _clear_thigh(arm, side, lock, nrm, pole)
            # The trail hand stays on the reference pose. Pulling the palm onto
            # the ramp folded the upper arm through the chest.

        for _cycle in range(3):
            plant(base)
            moved = _shift_plane(arm, plane_o, nrm, 0.0)
            base = _project_plane(arm.location, plane_o, nrm)
            if moved < 1.0e-4:
                break
        plant(base)
        _uncross(arm, guard=lambda: _min_plane(plane_o, nrm))
        plant(base, clear=True)
        _yaw_loose_thighs(arm, [(plane_o, nrm)])
        _uncross(arm, guard=lambda: _min_plane(plane_o, nrm))
        for side in ("L", "R"):
            _untwist_planted(arm, side, nrm, _deepest(f"Foot_{side}", nrm))
        body_pen = max(0.0, -_min_plane(plane_o, nrm))
        pen = max(pen, body_pen)
        for side, off in foot_off.items():
            lock = base + off
            vert = _deepest(f"Foot_{side}", nrm)
            if vert is None:
                continue
            err = Vector(vert) - lock
            tangential = (err - nrm * err.dot(nrm)).length
            skate = max(skate, tangential)
            pen = max(pen, max(0.0, -nrm.dot(Vector(vert) - plane_o)))
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
        if i % 10 == 0:
            print(
                f"  slide f={i} y={arm.location.y:.2f} z={arm.location.z:.2f} "
                f"skate={skate*100:.2f} pen={pen*100:.2f}",
                flush=True,
            )
    return {
        "keys": keys,
        "capsule": caps,
        "facing": facings,
        "times": times,
        "skate": skate,
        "pen": pen,
        "world": {"kind": "slide"},
        "note": (
            "Both soles stay on the 36° ramp and move with the capsule. "
            "Skate is extra foot travel along the ramp. The trail hand follows the reference pose."
        ),
    }


def bpy_update():
    import bpy
    bpy.context.view_layer.update()


def _hysteresis_plants(samples, hold=4):
    """Support leg is the one with the lower hip flex. Hold it so the lock does not chatter."""
    sides = []
    current = None
    pending = None
    pending_n = 0
    for ch in samples:
        fl = ch.get("hip_flex_L", 0.0)
        fr = ch.get("hip_flex_R", 0.0)
        want = "L" if fl <= fr else "R"
        if current is None:
            current = want
        if want != current:
            if pending == want:
                pending_n += 1
            else:
                pending = want
                pending_n = 1
            if pending_n >= hold:
                current = want
                pending = None
                pending_n = 0
        else:
            pending = None
            pending_n = 0
        sides.append(current)
    return sides


def _step_index(sides):
    steps = []
    k = 0
    prev = None
    for side in sides:
        if prev is not None and side != prev:
            k += 1
        steps.append(k)
        prev = side
    return steps


def _note_plant(vert, lock, normal, plant_at):
    """Tangential drift of a foot that is actually on the lock. Returns skate, pen, plant_at."""
    from mathutils import Vector
    if vert is None or lock is None:
        return 0.0, 0.0, plant_at
    err = Vector(vert) - Vector(lock)
    n = Vector(normal).normalized()
    gap = err.dot(n)
    if abs(gap) > 0.03:
        # The leg missed the lock. That is a reach failure, not a measured slide.
        return 0.0, max(0.0, -gap), plant_at
    if plant_at is None:
        plant_at = Vector(vert).copy()
    delta = Vector(vert) - plant_at
    along = delta - n * delta.dot(n)
    return along.length, max(0.0, -gap), plant_at


def nrm_out_pole(side, normal):
    """Knee bends off the wall. A wide lateral pole abducts the thigh into the pelvis."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    lateral = Vector((0.0, 0.12 if side == "L" else -0.12, 0.45))
    return n * 0.8 + lateral


def _brush_hands(arm, face, normal):
    """If a hand is inside the wall, seat the palm on the face. Elbow stays outside."""
    from mathutils import Vector
    nrm = Vector(normal).normalized()
    for side in ("L", "R"):
        vert = _deepest(f"Hand_{side}", nrm)
        if vert is None or vert.x >= face + 0.004:
            continue
        lock = Vector((face + 0.004, vert.y, max(0.06, vert.z)))
        pole = nrm * 0.9 + Vector((0.0, 0.25 if side == "L" else -0.25, 0.45))
        _pin_limb(arm, "hand", side, lock, nrm, pole)


def solve_wallrun(arm, samples, times):
    from mathutils import Vector, Euler
    face = 0.0
    normal = Vector((1.0, 0.0, 0.0))
    plants = _hysteresis_plants(samples, hold=4)
    steps = _step_index(plants)
    groups = {}
    for i, step in enumerate(steps):
        groups.setdefault(step, []).append(i)
    step_h = 0.30
    keys = []
    caps = []
    facings = []
    skate = 0.0
    pen = 0.0
    held = {}
    # Extra height so the sole, which extends below the contact, stays above the floor.
    step_boost = {}
    floor_o = Vector((0.0, 0.0, 0.0))
    floor_n = Vector((0.0, 0.0, 1.0))
    wall_o = Vector((face, 0.0, 0.0))
    for i, ch in enumerate(samples):
        side = plants[i]
        step = steps[i]
        members = groups[step]
        local = 0.0 if len(members) == 1 else members.index(i) / (len(members) - 1)
        lock_z = 0.16 + step * step_h + step_boost.get(step, 0.0)
        # The root sits near the soles, so the planted foot and the root share a height.
        # An 8 cm rise during the hold is a climb the leg can still reach.
        # Close enough that a nearly straight leg still plants. A deep fold puts the thigh through the pelvis.
        origin = Vector((0.30, 0.0, lock_z + local * 0.06))
        facing = Euler((math.radians(8.0), 0.0, math.radians(-90.0)), "XYZ")
        lock = Vector((face, 0.0, lock_z))
        _apply_sample(arm, ch, origin, facing)
        show_wall_world(face, max(2.4, origin.z + 1.4), 5.2)
        pole = nrm_out_pole(side, normal)
        other = "R" if side == "L" else "L"

        def place_contacts(clear=False):
            _pin_limb(arm, "foot", side, lock, normal, pole)
            if clear:
                _clear_thigh(arm, side, lock, normal, pole)
            swing = _deepest(f"Foot_{other}", normal)
            if swing is not None and swing.x < face + 0.03:
                park = Vector((face + 0.08, 0.0, max(0.20, lock_z + 0.18)))
                swing_pole = nrm_out_pole(other, normal)
                _pin_limb(arm, "foot", other, park, normal, swing_pole)
                if clear:
                    _clear_thigh(arm, other, park, normal, swing_pole)
            _brush_hands(arm, face, normal)

        place_contacts()
        if step not in step_boost:
            zs = [v.z for v in p5._verts(f"Foot_{side}")]
            step_boost[step] = (0.012 - min(zs)) if zs and min(zs) < 0.012 else 0.0
            if step_boost[step] > 0.0:
                lock.z = 0.16 + step * step_h + step_boost[step]
                origin.z = lock.z + local * 0.06
                arm.location.z = origin.z
                bpy_update()
                place_contacts()
        # Hands stay on the face. Everything else is pushed back out of the wall
        # and up off the floor, then the plant is put back on the same brick.
        for _cycle in range(3):
            place_contacts()
            into_wall = _shift_plane(arm, wall_o, normal, 0.0)
            into_floor = _shift_plane(arm, floor_o, floor_n, 0.0)
            if into_wall < 1.0e-4 and into_floor < 1.0e-4:
                break
        place_contacts()
        _uncross(
            arm,
            guard=lambda: min(_min_plane(wall_o, normal), _min_plane(floor_o, floor_n)),
        )
        place_contacts(clear=True)
        _yaw_loose_thighs(arm, [(wall_o, normal), (floor_o, floor_n)])
        _uncross(
            arm,
            guard=lambda: min(_min_plane(wall_o, normal), _min_plane(floor_o, floor_n)),
        )
        if step not in held:
            held[step] = None
        vert = _deepest(f"Foot_{side}", normal)
        got, gap, held[step] = _note_plant(vert, lock, normal, held[step])
        skate = max(skate, got)
        pen = max(pen, gap)
        pen = max(pen, max(0.0, -_min_plane(wall_o, normal)))
        pen = max(pen, max(0.0, -_min_plane(floor_o, floor_n)))
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
        if i % 10 == 0:
            print(
                f"  wall f={i} plant={side} lock_z={lock_z:.2f} skate={skate*100:.2f} pen={pen*100:.2f}",
                flush=True,
            )
    return {
        "keys": keys,
        "capsule": caps,
        "facing": facings,
        "times": times,
        "skate": skate,
        "pen": pen,
        "plants": plants,
        "world": {"kind": "wall"},
        "note": (
            "L/R swapped from the behind-camera take. Step timing follows clip 18. "
            "Each plant is a world lock on the vertical face, and the root only rises "
            "as far as that leg can still reach. Clip 19 is not posed."
        ),
    }


def _over_landing_xy(x, y, landing_y):
    return -0.20 <= x <= 0.90 and abs(y - landing_y) <= 0.38


def _shift_above_box(arm, landing_y, landing_top):
    """Lift so a vert over the low wall is not inside the box."""
    from mathutils import Vector
    worst = 0.0
    for v in _all_body_verts():
        if not _over_landing_xy(v.x, v.y, landing_y):
            continue
        if v.z < landing_top:
            worst = max(worst, landing_top - v.z)
    if worst <= 0.0:
        return 0.0
    arm.location = arm.location + Vector((0.0, 0.0, worst))
    bpy_update()
    return worst


def solve_turn(arm, samples, times):
    from mathutils import Vector, Euler
    face = 0.0
    wall_n = Vector((1.0, 0.0, 0.0))
    floor_n = Vector((0.0, 0.0, 1.0))
    landing_top = 0.42
    plants = _hysteresis_plants(samples, hold=4)
    # The run-in is only as long as the steps the legs can hold. Each plant
    # travels about 22 cm, which a stance leg can reach.
    run_ids = [i for i, t in enumerate(times) if t < 0.92]
    run_steps = _step_index([plants[i] for i in run_ids]) if run_ids else []
    step_of = {run_ids[k]: run_steps[k] for k in range(len(run_ids))}
    run_groups = {}
    for i in run_ids:
        run_groups.setdefault(step_of[i], []).append(i)
    n_run = (max(run_steps) + 1) if run_steps else 1
    stride = 0.22
    y_start = 0.55 + n_run * stride
    y_plant = y_start - n_run * stride
    # The low wall sits behind the run-in, with a gap so the runner is not inside it.
    landing_y = y_start + 0.95
    keys = []
    caps = []
    facings = []
    skate = 0.0
    pen = 0.0
    held = {}
    wall_lock = None
    land_lock = {}
    for i, ch in enumerate(samples):
        t = times[i]
        if t < 0.88:
            yaw = 0.0
        elif t < 1.38:
            yaw = math.pi * _smoothstep((t - 0.88) / 0.50)
        else:
            yaw = math.pi
        facing = Euler((math.radians(8.0), 0.0, yaw), "XYZ")
        if t < 0.92:
            phase = "run"
        elif t < 1.22:
            phase = "plant"
        elif t < 1.48:
            phase = "air"
        else:
            phase = "land"
        if phase == "run":
            step = step_of[i]
            members = run_groups[step]
            local = 0.0 if len(members) == 1 else members.index(i) / (len(members) - 1)
            y_lock = y_start - step * stride
            origin = Vector((0.58, y_lock - local * stride, 0.0))
            side = plants[i]
            lock = Vector((0.58 + (0.12 if side == "L" else -0.12), y_lock, 0.0))
        elif phase == "plant":
            # The root steps in toward the wall and then stays. The right foot
            # can keep one brick while the preview yaw starts.
            u = _clamp((t - 0.92) / 0.30, 0.0, 1.0)
            origin = Vector((0.58 - u * 0.20, y_plant, 0.0))
            if wall_lock is None:
                wall_lock = Vector((face, y_plant, 0.62))
            side = "R"
            lock = wall_lock
        elif phase == "air":
            u = _clamp((t - 1.22) / 0.26, 0.0, 1.0)
            y = y_plant + u * (landing_y - y_plant)
            # Hop clears the low wall, then the root arrives on its top.
            z = math.sin(u * math.pi) * 0.55 + u * landing_top
            origin = Vector((0.42, y, z))
            side = None
            lock = None
        else:
            # Root sits on the low wall, next to the soles.
            origin = Vector((0.42, landing_y, landing_top))
            side = None
            lock = None
        _apply_sample(arm, ch, origin, facing)
        show_turn_world(face, landing_top, landing_y)
        wall_o = Vector((face, 0.0, 0.0))
        floor_o = Vector((0.0, 0.0, 0.0))

        def push_clear():
            _shift_plane(arm, wall_o, wall_n, 0.0)
            _shift_plane(arm, floor_o, floor_n, 0.0)
            _shift_above_box(arm, landing_y, landing_top)

        if phase == "run":
            pole = _pole_knee(side, floor_n, Vector((1.0, 0.0, 0.0)))
            for _cycle in range(2):
                _pin_limb(arm, "foot", side, lock, floor_n, pole)
                _brush_hands(arm, face, wall_n)
                push_clear()
            _pin_limb(arm, "foot", side, lock, floor_n, pole)
            _clear_thigh(arm, side, lock, floor_n, pole)
            key = ("run", step_of[i], side)
            vert = _deepest(f"Foot_{side}", floor_n)
            got, gap, held[key] = _note_plant(vert, lock, floor_n, held.get(key))
            skate = max(skate, got)
            pen = max(pen, gap)
        elif phase == "plant":
            pole = Vector((0.9, 0.1, 0.45))
            other = _deepest("Foot_L", floor_n)
            left_lock = None
            if other is not None:
                left_lock = Vector((max(0.10, other.x), min(other.y, y_plant + 0.15), 0.0))
            for _cycle in range(2):
                _pin_limb(arm, "foot", "R", wall_lock, wall_n, pole)
                if left_lock is not None:
                    _pin_limb(
                        arm, "foot", "L", left_lock, floor_n,
                        _pole_knee("L", floor_n, Vector((1.0, 0.0, 0.0))),
                    )
                _brush_hands(arm, face, wall_n)
                push_clear()
            _pin_limb(arm, "foot", "R", wall_lock, wall_n, pole)
            _clear_thigh(arm, "R", wall_lock, wall_n, pole)
            if left_lock is not None:
                left_pole = _pole_knee("L", floor_n, Vector((1.0, 0.0, 0.0)))
                _pin_limb(arm, "foot", "L", left_lock, floor_n, left_pole)
                _clear_thigh(arm, "L", left_lock, floor_n, left_pole)
            vert = _deepest("Foot_R", wall_n)
            got, gap, held["wall"] = _note_plant(vert, wall_lock, wall_n, held.get("wall"))
            skate = max(skate, got)
            pen = max(pen, gap)
        elif phase == "air":
            for _cycle in range(2):
                over = _over_landing_xy(arm.location.x, arm.location.y, landing_y)
                foot_z = landing_top if over else max(0.02, arm.location.z)
            for s, dx in (("L", 0.16), ("R", -0.16)):
                _pin_limb(
                    arm, "foot", s,
                    Vector((arm.location.x + dx, arm.location.y, foot_z)),
                    floor_n,
                    _pole_knee(s, floor_n, Vector((1.0, 0.0, 0.0))),
                )
                push_clear()
            over = _over_landing_xy(arm.location.x, arm.location.y, landing_y)
            foot_z = landing_top if over else max(0.02, arm.location.z)
            for s, dx in (("L", 0.16), ("R", -0.16)):
                air_lock = Vector((arm.location.x + dx, arm.location.y, foot_z))
                air_pole = _pole_knee(s, floor_n, Vector((1.0, 0.0, 0.0)))
                _pin_limb(arm, "foot", s, air_lock, floor_n, air_pole)
                _clear_thigh(arm, s, air_lock, floor_n, air_pole)
        else:
            if not land_lock:
                for s, dx, dy in (("L", 0.18, 0.10), ("R", -0.18, -0.10)):
                    land_lock[s] = Vector((0.42 + dx, landing_y + dy, landing_top))
            for _cycle in range(2):
                for s in ("L", "R"):
                    _pin_limb(
                        arm, "foot", s, land_lock[s], floor_n,
                        _pole_knee(s, floor_n, Vector((1.0, 0.0, 0.0))),
                    )
                push_clear()
            for s in ("L", "R"):
                land_pole = _pole_knee(s, floor_n, Vector((1.0, 0.0, 0.0)))
                _pin_limb(arm, "foot", s, land_lock[s], floor_n, land_pole)
                _clear_thigh(arm, s, land_lock[s], floor_n, land_pole)
                vert = _deepest(f"Foot_{s}", floor_n)
                got, gap, held[("land", s)] = _note_plant(vert, land_lock[s], floor_n, held.get(("land", s)))
                skate = max(skate, got)
                pen = max(pen, gap)
        _yaw_loose_thighs(
            arm,
            [
                (wall_o, wall_n),
                (floor_o, floor_n),
                (Vector((0.0, landing_y, landing_top)), floor_n),
            ],
        )
        _uncross(
            arm,
            guard=lambda: min(_min_plane(wall_o, wall_n), _min_plane(floor_o, floor_n)),
        )
        pen = max(pen, max(0.0, -_min_plane(wall_o, wall_n)))
        pen = max(pen, max(0.0, -_min_plane(floor_o, floor_n)))
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
        if i % 10 == 0:
            print(
                f"  turn f={i} t={t:.2f} phase={phase} yaw={yaw:.2f} skate={skate*100:.2f} pen={pen*100:.2f}",
                flush=True,
            )
    return {
        "keys": keys,
        "capsule": caps,
        "facing": facings,
        "times": times,
        "skate": skate,
        "pen": pen,
        "world": {"kind": "turn", "landing_y": landing_y, "landing_top": landing_top, "face": face},
        "note": (
            "Yaw is a preview rotation of the armature, not a Root key. The run plants "
            "are short enough to reach, the right foot locks on the wall through the turn, "
            "and both feet lock on the low wall."
        ),
    }


SOLVERS = {
    "slide": solve_slide,
    "wallrun_vertical": solve_wallrun,
    "turn180": solve_turn,
}


# --- no-clip --------------------------------------------------------------

def _body_objects():
    import bpy
    out = []
    for ob in bpy.data.objects:
        if ob.type != "MESH":
            continue
        if not ob.name.startswith("Mesh_"):
            continue
        if ob.hide_render:
            continue
        out.append(ob)
    return out


def _world_objects():
    import bpy
    names = ("Ground", "ActionWall", "ActionSlope", "LandingWall")
    out = []
    for name in names:
        ob = bpy.data.objects.get(name)
        if ob is not None and ob.type == "MESH" and not ob.hide_render:
            out.append(ob)
    return out


def _eval_bvh(ob, deps):
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    ev = ob.evaluated_get(deps)
    me = ev.to_mesh()
    me.transform(ev.matrix_world)
    verts = [Vector(v.co) for v in me.vertices]
    polys = [tuple(p.vertices) for p in me.polygons]
    tris = []
    for poly in me.polygons:
        ids = list(poly.vertices)
        for k in range(1, len(ids) - 1):
            tris.append((verts[ids[0]], verts[ids[k]], verts[ids[k + 1]]))
    tree = BVHTree.FromPolygons(verts, polys) if polys else None
    ev.to_mesh_clear()
    return tree, verts, tris


def _tri_hit(tri_a, tri_b):
    """A point where an edge of one triangle crosses the other, or None.

    intersect_ray_tri(..., clip=True) does not limit the hit to the edge
    segment, so a hit past the segment is rejected here.
    """
    from mathutils.geometry import intersect_ray_tri
    for src, dst in ((tri_a, tri_b), (tri_b, tri_a)):
        for i in range(3):
            a = src[i]
            b = src[(i + 1) % 3]
            ray = b - a
            length_sq = ray.length_squared
            if length_sq < 1.0e-16:
                continue
            hit = intersect_ray_tri(dst[0], dst[1], dst[2], ray, a, True)
            if hit is None:
                continue
            t = ray.dot(hit - a) / length_sq
            if -1.0e-3 <= t <= 1.001:
                return hit
    return None


def _on_tri(point, tri, slack=0.004):
    """True when the point's projection onto the triangle plane lands inside it."""
    n = (tri[1] - tri[0]).cross(tri[2] - tri[0])
    if n.length_squared < 1.0e-16:
        return False
    n = n.normalized()
    proj = point - n * n.dot(point - tri[0])
    v0 = tri[1] - tri[0]
    v1 = tri[2] - tri[0]
    v2 = proj - tri[0]
    dot00 = v0.dot(v0)
    dot01 = v0.dot(v1)
    dot02 = v0.dot(v2)
    dot11 = v1.dot(v1)
    dot12 = v1.dot(v2)
    den = dot00 * dot11 - dot01 * dot01
    if abs(den) < 1.0e-12:
        return False
    u = (dot11 * dot02 - dot01 * dot12) / den
    v = (dot00 * dot12 - dot01 * dot02) / den
    return u >= -slack and v >= -slack and (u + v) <= 1.0 + slack


def _poke(tri, other, hit):
    """How far a vertex of tri has passed through the other triangle.

    The back side of the winding counts, and only when the vertex projects
    inside that triangle. A vertex merely behind the infinite plane is not
    inside the shell.
    """
    n = (other[1] - other[0]).cross(other[2] - other[0])
    if n.length < 1.0e-8:
        return 0.0
    n = n.normalized()
    worst = 0.0
    for v in tri:
        if (v - hit).length > 0.06:
            continue
        inside = -n.dot(v - other[0])
        if inside <= 0.0:
            continue
        if not _on_tri(v, other):
            continue
        if inside > worst:
            worst = inside
    return worst


def _cross_depth(tris_a, tris_b, tree_a, tree_b, joint):
    """Depth of real triangle crossings. Neighbour joints excuse a hit within 3 cm."""
    if tree_a is None or tree_b is None:
        return 0.0
    overlaps = tree_a.overlap(tree_b)
    worst = 0.0
    for ia, ib in overlaps:
        hit = _tri_hit(tris_a[ia], tris_b[ib])
        if hit is None:
            continue
        if joint is not None and (hit - joint).length <= JOINT_EXEMPT_M:
            continue
        worst = max(worst, _poke(tris_a[ia], tris_b[ib], hit), _poke(tris_b[ib], tris_a[ia], hit))
    return worst


def _box_pen(v, center, half):
    dx = abs(v.x - center[0]) - half[0]
    dy = abs(v.y - center[1]) - half[1]
    dz = abs(v.z - center[2]) - half[2]
    if dx > 0.0 or dy > 0.0 or dz > 0.0:
        return 0.0
    return min(-dx, -dy, -dz)


def _analytical_world(verts, spec, world):
    """Signed distance into the planar solids these clips stand on."""
    from mathutils import Vector
    pen = 0.0
    world = world or {}
    if spec["verb"] == "slide":
        _ang, y0, normal, _down = _slope_frame()
        origin = Vector((0.0, y0, 0.0))
        for v in verts:
            pen = max(pen, max(0.0, -normal.dot(Vector(v) - origin)))
        return pen
    landing_top = float(world.get("landing_top", 0.42))
    landing_y = float(world.get("landing_y", 1.55))
    for v in verts:
        pen = max(pen, max(0.0, -v.z))
        pen = max(pen, max(0.0, -v.x))
        if spec["verb"] == "turn180":
            # Same box as show_turn_world. Scale is the half-extent of a 2 m cube.
            pen = max(pen, _box_pen(v, (0.35, landing_y, landing_top * 0.5), (0.55, 0.38, landing_top * 0.5)))
    return pen


def _joint_world(arm, child_name):
    return p5._head_w(arm, child_name)


def _self_worst(arm, skip_prefixes=()):
    """Deepest shell crossing outside the 3 cm joint exemption. Metres, and the pair."""
    import bpy
    rh._ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    bodies = []
    for ob in _body_objects():
        tree, _verts, tris = _eval_bvh(ob, deps)
        bodies.append((ob.parent_bone, tree, tris))
    child_of = {child: parent for parent, child in NEIGHBORS}
    self_pen = 0.0
    worst_pair = ""
    for i, (bone_a, tree_a, tris_a) in enumerate(bodies):
        for bone_b, tree_b, tris_b in bodies[i + 1:]:
            if skip_prefixes and (
                bone_a.startswith(skip_prefixes) or bone_b.startswith(skip_prefixes)
            ):
                continue
            joint = None
            if child_of.get(bone_b) == bone_a:
                joint = _joint_world(arm, bone_b)
            elif child_of.get(bone_a) == bone_b:
                joint = _joint_world(arm, bone_a)
            depth = _cross_depth(tris_a, tris_b, tree_a, tree_b, joint)
            if depth > self_pen:
                self_pen = depth
                worst_pair = f"{bone_a}/{bone_b}"
    return self_pen, worst_pair


def _ease_bone(arm, name, amount):
    """Open a limb without undoing the bend that keeps a foot on the surface."""
    pb = arm.pose.bones.get(name)
    if pb is None:
        return
    pb.rotation_mode = "XYZ"
    e = pb.rotation_euler
    keep_bend = name.startswith(("UpperLeg", "LowerLeg", "Foot", "LowerArm"))
    if keep_bend:
        # Ease the twist, and a little of the bend. The bend is what reaches the contact.
        pb.rotation_euler = (e.x * (1.0 - amount * 0.35), e.y * (1.0 - amount), e.z * (1.0 - amount))
    else:
        pb.rotation_euler = tuple(e[i] * (1.0 - amount) for i in range(3))
    pb.location = (0.0, 0.0, 0.0)
    pb.scale = (1.0, 1.0, 1.0)
    bpy_update()


def _bone_to_ease(pair):
    if not pair or "/" not in pair:
        return None
    a, b = pair.split("/", 1)
    child_of = {child: parent for parent, child in NEIGHBORS}
    if child_of.get(b) == a:
        return b
    if child_of.get(a) == b:
        return a
    for name in (b, a):
        if name.startswith(("Hand", "LowerArm", "UpperArm", "LowerLeg", "Foot")):
            return name
    return b


def _snap_rots(arm):
    snap = {}
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        snap[pb.name] = (tuple(pb.rotation_euler), tuple(pb.location), tuple(pb.scale))
    return snap


def _restore_rots(arm, snap):
    for name, (rot, loc, scale) in snap.items():
        pb = arm.pose.bones.get(name)
        if pb is None:
            continue
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = rot
        pb.location = loc
        pb.scale = scale
    bpy_update()


def _uncross(arm, repin=None, guard=None, rounds=5, allow_legs=False):
    """Ease a crossing limb. Put that bone back if the contact sinks or nothing improves.

    Leg bones stay put unless allow_legs is set. Re-solving them puts the thigh
    back through the pelvis, and leaving them eased lifts the foot off the contact.
    """
    tried = set()
    for _ in range(rounds):
        depth, pair = _self_worst(arm)
        if depth <= 0.004 or not pair:
            return
        names = []
        primary = _bone_to_ease(pair)
        if primary:
            names.append(primary)
        a, b = pair.split("/", 1)
        for extra in (a, b):
            if extra not in names:
                names.append(extra)
        # A hand buried in the pelvis is the arm pose, not the wrist twist.
        if "Hand" in pair and "Hips" in pair:
            side = "L" if pair.endswith("_L") or "_L/" in pair or pair.startswith("Hand_L") else "R"
            if "Hand_L" in pair:
                side = "L"
            elif "Hand_R" in pair:
                side = "R"
            names = [f"UpperArm_{side}", f"LowerArm_{side}"] + names
        if not allow_legs:
            names = [n for n in names if not n.startswith(("UpperLeg", "LowerLeg", "Foot"))]
        name = next((n for n in names if n not in tried), None)
        if name is None and not allow_legs:
            # The deepest pair is a planted thigh. An arm can still be in the chest.
            depth, pair = _self_worst(arm, skip_prefixes=("UpperLeg", "LowerLeg", "Foot"))
            if depth <= 0.004 or not pair or "/" not in pair:
                return
            names = []
            primary = _bone_to_ease(pair)
            if primary and not primary.startswith(("UpperLeg", "LowerLeg", "Foot")):
                names.append(primary)
            a, b = pair.split("/", 1)
            for extra in (a, b):
                if extra not in names and not extra.startswith(("UpperLeg", "LowerLeg", "Foot")):
                    names.append(extra)
            name = next((n for n in names if n not in tried), None)
        if name is None:
            return
        tried.add(name)
        snap = _snap_rots(arm)
        loc = arm.location.copy()
        rot = arm.rotation_euler.copy()

        def _attempt(adjust):
            _restore_rots(arm, snap)
            arm.location = loc
            arm.rotation_euler = rot
            bpy_update()
            adjust()
            if repin is not None and name.startswith(("UpperLeg", "LowerLeg", "Foot")):
                repin()
            depth2, _pair2 = _self_worst(arm)
            solid = 0.0 if guard is None else guard()
            return depth2, solid

        depth2, solid = _attempt(lambda: _ease_bone(arm, name, 0.5))
        if depth2 > depth - 0.0004 or solid < -0.0015:
            # Arms that sit in the chest: swing the yaw outward instead of toward the bind.
            if name.startswith("UpperArm"):
                def _out():
                    pb = arm.pose.bones[name]
                    pb.rotation_mode = "XYZ"
                    e = pb.rotation_euler
                    sign = 1.0 if name.endswith("_L") else -1.0
                    pb.rotation_euler = (e.x * 0.75, e.y, e.z + sign * 0.35)
                    pb.scale = (1.0, 1.0, 1.0)
                    bpy_update()
                depth2, solid = _attempt(_out)
            if depth2 > depth - 0.0004 or solid < -0.0015:
                _restore_rots(arm, snap)
                arm.location = loc
                arm.rotation_euler = rot
                bpy_update()


def noclip_frame(arm, spec, world_info=None):
    """World plane depth plus real triangle crossings. Metres."""
    import bpy
    rh._ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    bodies = []
    all_verts = []
    for ob in _body_objects():
        tree, verts, tris = _eval_bvh(ob, deps)
        bodies.append((ob.parent_bone, tree, tris))
        all_verts.extend(verts)
    # These props are convex (ramp plane, wall, floor, landing box). Signed
    # distance of the shell vertices is the penetration. Triangle-edge hits
    # against a face wound the wrong way report the outside of a shoe as inside.
    world = _analytical_world(all_verts, spec, world_info)
    child_of = {child: parent for parent, child in NEIGHBORS}
    self_pen = 0.0
    worst_pair = ""
    for i, (bone_a, tree_a, tris_a) in enumerate(bodies):
        for bone_b, tree_b, tris_b in bodies[i + 1:]:
            joint = None
            if child_of.get(bone_b) == bone_a:
                joint = _joint_world(arm, bone_b)
            elif child_of.get(bone_a) == bone_b:
                joint = _joint_world(arm, bone_a)
            depth = _cross_depth(tris_a, tris_b, tree_a, tree_b, joint)
            if depth > self_pen:
                self_pen = depth
                worst_pair = f"{bone_a}/{bone_b}"
    return world, self_pen, worst_pair


def pose_from_key(arm, solved, i, spec):
    import bpy
    from mathutils import Euler, Vector
    eulers = {name: deg for name, deg in solved["keys"][i].items()}
    rh._apply_eulers(arm, eulers)
    _clear_pose_channels(arm)
    # _apply_eulers already wrote the rotations, then the clear kept them.
    # Write them again in case the clear ran against a stale channel.
    rh._apply_eulers(arm, eulers)
    cap = solved["capsule"][i]
    arm.location = Vector(cap)
    arm.rotation_euler = Euler(solved["facing"][i], "XYZ")
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    _show_for(spec, arm, solved)


def _show_for(spec, arm, solved=None):
    world = (solved or {}).get("world") or {}
    if spec["verb"] == "slide":
        show_slide_world(arm.location.y)
    elif spec["verb"] == "wallrun_vertical":
        show_wall_world(0.0, max(2.4, arm.location.z + 1.4), 5.2)
    else:
        show_turn_world(0.0, world.get("landing_top", 0.42), world.get("landing_y", 1.55))


def measure_noclip(arm, solved_all):
    frames = 0
    fails = 0
    world_max = 0.0
    self_max = 0.0
    worst = ""
    printed = {}
    for spec in SPECS:
        solved = solved_all[spec["id"]]
        for i in range(len(solved["keys"])):
            pose_from_key(arm, solved, i, spec)
            world, self_pen, pair = noclip_frame(arm, spec, solved.get("world"))
            frames += 1
            if world > world_max:
                world_max = world
                worst = f"{spec['id']} f={i} {pair} world"
            if self_pen > self_max:
                self_max = self_pen
                worst = f"{spec['id']} f={i} {pair}"
            if world > DEPTH_LIMIT_M or self_pen > DEPTH_LIMIT_M:
                fails += 1
                printed[spec["id"]] = printed.get(spec["id"], 0) + 1
                if printed[spec["id"]] <= 4:
                    print(
                        f"NOCLIP FAIL {spec['id']} f={i} world={world*100:.2f}cm self={self_pen*100:.2f}cm {pair}",
                        flush=True,
                    )
    line = (
        f"no-clip clips={len(SPECS)} frames={frames} "
        f"worldMax={world_max*100:.2f} selfMax={self_max*100:.2f} fails={fails}"
    )
    print(line, flush=True)
    if worst:
        print("no-clip worst", worst, flush=True)
    return line, fails, world_max, self_max


# --- stills ---------------------------------------------------------------

def _shot_for(spec, arm):
    cap = arm.location
    if spec["verb"] == "slide":
        focus = (cap.x, cap.y + 0.1, cap.z + 0.55)
        loc = (cap.x + 2.4, cap.y - 2.6, cap.z + 1.15)
        return loc, focus
    if spec["verb"] == "wallrun_vertical":
        focus = (0.15, cap.y, cap.z + 0.95)
        loc = (2.5, cap.y + 2.6, cap.z + 1.15)
        return loc, focus
    focus = (cap.x, cap.y, cap.z + 0.85)
    loc = (cap.x + 2.8, cap.y - 3.2, cap.z + 1.35)
    return loc, focus


def render_beats(arm, solved_all):
    import bpy
    os.makedirs(PREV, exist_ok=True)
    rh._prepare_scene()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    bpy.context.scene.render.resolution_x = 420
    bpy.context.scene.render.resolution_y = 560
    for spec in SPECS:
        solved = solved_all[spec["id"]]
        n = len(solved["keys"])
        for s in range(8):
            i = int(round(s * (n - 1) / 7.0))
            pose_from_key(arm, solved, i, spec)
            loc, look = _shot_for(spec, arm)
            rh._shot(os.path.join(PREV, f"{spec['verb']}_{s}.png"), loc, look)
            print(f"still {spec['id']} {s} frame={i} t={solved['times'][i]:.2f}", flush=True)


def _fit_under(path, limit=400_000):
    """Keep the sheet under 400 KB. Scale with Lanczos before giving up pixels."""
    from PIL import Image
    im = Image.open(path).convert("RGB")
    cur = im
    for _ in range(8):
        cur.save(path, optimize=True)
        if os.path.getsize(path) <= limit:
            return
        cur = cur.resize(
            (max(8, int(cur.width * 0.88)), max(8, int(cur.height * 0.88))),
            Image.Resampling.LANCZOS,
        )
    cur.save(path, optimize=True)


def composite(solved_all, noclip_line):
    import site
    site.addsitedir(site.getusersitepackages())
    from PIL import Image, ImageDraw, ImageFont
    os.makedirs(OUT, exist_ok=True)
    font = ImageFont.load_default()
    strip_dir = "/workspace/Docs/Storror/out/strips"
    for spec in SPECS:
        frames = []
        for s in range(8):
            frames.append(Image.open(os.path.join(PREV, f"{spec['verb']}_{s}.png")).convert("RGB"))
        fw, fh = frames[0].size
        gap = 6
        grid_w = fw * 4 + gap * 3
        grid_h = fh * 2 + gap
        grid = Image.new("RGB", (grid_w, grid_h), (48, 44, 40))
        solved = solved_all[spec["id"]]
        n = len(solved["keys"])
        draw = ImageDraw.Draw(grid)
        for s, fr in enumerate(frames):
            c = s % 4
            r = s // 4
            x = c * (fw + gap)
            y = r * (fh + gap)
            grid.paste(fr, (x, y))
            i = int(round(s * (n - 1) / 7.0))
            tag = f"{s + 1}  {solved['times'][i]:.2f}s"
            draw.rectangle((x + 8, y + 8, x + 8 + 7 * len(tag) + 10, y + 26), fill=(20, 18, 16))
            draw.text((x + 12, y + 10), tag, fill=(245, 236, 220), font=font)
        strip = Image.open(os.path.join(strip_dir, spec["strip"])).convert("RGB")
        # The stick strip is a wide timeline. Cap its width so the 4x2 poses stay large.
        max_sw = 1100
        sw = min(max_sw, int(round(strip.width * (grid_h / strip.height))))
        sh = int(round(strip.height * (sw / strip.width)))
        strip = strip.resize((sw, sh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGB", (sw + 10 + grid_w, grid_h + 36), (32, 30, 28))
        canvas.paste(strip, (0, 36 + max(0, (grid_h - sh) // 2)))
        canvas.paste(grid, (sw + 10, 36))
        d = ImageDraw.Draw(canvas)
        skate = solved_all[spec["id"]]["skate"] * 100.0
        pen = solved_all[spec["id"]]["pen"] * 100.0
        title = f"{spec['label']}   foot {skate:.2f}cm   pen {pen:.2f}cm   {noclip_line}"
        d.text((8, 8), title[:180], fill=(245, 236, 220), font=font)
        path = os.path.join(OUT, f"{spec['verb']}.png")
        canvas.save(path, optimize=True)
        _fit_under(path)
        print("wrote", path, os.path.getsize(path), flush=True)


def write_keys(solved_all, noclip_line):
    os.makedirs(OUT, exist_ok=True)
    doc = {
        "root_motion": False,
        "gameplayDelay": 0,
        "rig": "Hier v0.8.0",
        "fps": FPS,
        "note": (
            "Bone rotation keys only. Root stays at the origin. capsule_preview_m and "
            "facing_preview_rad are the stills camera path, not keys the game plays."
        ),
        "noclip": noclip_line,
        "clips": {},
    }
    for spec in SPECS:
        solved = solved_all[spec["id"]]
        keys_out = []
        for i, bones in enumerate(solved["keys"]):
            keys_out.append({
                "frame": i,
                "t": round(solved["times"][i], 4),
                "bones": {name: [round(a, 2) for a in deg] for name, deg in bones.items()},
            })
        doc["clips"][spec["id"]] = {
            "verb": spec["verb"],
            "fps": FPS,
            "frames": len(keys_out),
            "swap_lr": spec["swap_lr"],
            "window_s": list(WINDOWS[spec["id"]]),
            "foot_slide_cm": round(solved["skate"] * 100.0, 2),
            "penetration_cm": round(solved["pen"] * 100.0, 2),
            "root_location_keys": False,
            "root_rotation_keys": False,
            "capsule_preview_m": [[round(c, 4) for c in p] for p in solved["capsule"]],
            "facing_preview_rad": [[round(a, 4) for a in f] for f in solved["facing"]],
            "note": solved["note"],
            "keys": keys_out,
        }
    path = os.path.join(OUT, "keyed_clips.json")
    with open(path, "w") as f:
        json.dump(doc, f)
    with open(os.path.join(OUT, "noclip.txt"), "w") as f:
        f.write(noclip_line + "\n")
        for spec in SPECS:
            s = solved_all[spec["id"]]
            f.write(
                f"{spec['id']} foot_slide_cm={s['skate']*100:.2f} pen_cm={s['pen']*100:.2f} frames={len(s['keys'])}\n"
            )
    print("wrote", path, flush=True)


def main():
    import bpy
    args = _args()
    global PASS, OUT, PREV
    if "--pass" in args:
        PASS = int(args[args.index("--pass") + 1])
        OUT = f"/workspace/Docs/HierStills/v080/clips/pass{PASS}"
        PREV = f"/tmp/hier_v080_clips_pass{PASS}"
    only = None
    if "--only" in args:
        only = args[args.index("--only") + 1]
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    rh._prepare_scene()
    solved_all = {}
    for spec in SPECS:
        if only and only not in spec["id"] and only != spec["verb"]:
            continue
        samples, times, _data = load_window(spec)
        print(f"SOLVE {spec['id']} frames={len(samples)} t={times[0]:.2f}..{times[-1]:.2f}", flush=True)
        solved = SOLVERS[spec["verb"]](arm, samples, times)
        solved_all[spec["id"]] = solved
        print(
            f"SOLVED {spec['id']} foot_slide_cm={solved['skate']*100:.2f} pen_cm={solved['pen']*100:.2f}",
            flush=True,
        )
        rh._reset(arm)
    if len(solved_all) != len(SPECS):
        print("partial solve, skipping the combined no-clip line", flush=True)
        return
    line, _fails, _w, _s = measure_noclip(arm, solved_all)
    write_keys(solved_all, line)
    if "--metrics" in args:
        print(line, flush=True)
        return
    render_beats(arm, solved_all)
    composite(solved_all, line)
    print(line, flush=True)


if __name__ == "__main__":
    main()
