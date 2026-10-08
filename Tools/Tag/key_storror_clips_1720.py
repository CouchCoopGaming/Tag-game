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
# The vertical wall is a 4 m brick face. The top slab runs 1.5 m back so the
# mantle stands on a roof. The cube primitive is 2 m, so half_thick 0.75 puts
# the back face at x = -1.5. A palm on the top may sit at a small negative x.
# Below the top, x inside the slab is inside the brick.
WALL_TOP_Z = 4.0
WALL_FACE_X = 0.0
WALL_BACK_X = -1.50
WALL_HALF_THICK = 0.75
WALL_Y_HALF = 2.6
# Climb hip stays on this line, close to the face. A 0.72 m rail left the
# plant hip half a metre off the brick, so the shin read as a kick. Plants
# seat the sole and land near this x. The mantle then carries it onto the slab.
HIP_RAIL_X = 0.46
# Knee lands on the lip. Standing walks back onto the roof, not to the middle
# of the slab (that x is inside the brick if the thigh dips under the top).
KNEE_LIP_X = -0.10
ROOF_STAND_X = -0.55
# One world camera for every wall cell. Ortho height in metres. The face is
# at the same pixel x in all 8 cells.
WALL_CAM_FOCUS = (-0.40, 0.0, 3.05)
WALL_SIDE_SCALE = 6.60
# 35° off the side axis, from behind the runner (+X) on the same side as the
# side camera (-Y). A camera on -X sits behind the brick and looks at the
# chest through the wall. This one stays on the body's side of the face.
WALL_34_DIR = (0.57, -0.82, 0.06)

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
# 17's first 0.40 s is a small, noisy figure. 20's last frames are a close-up
# with feet leaving the frame.
# 18 is filmed from behind. In the image the hips stay at the bottom until
# about 0.60 s (the run-in), rise steadily until about 2.00 s (the wall run),
# then drop (the slide back down). The old 0.25–1.75 s window included the
# run-in and cut the climb before the top.
WINDOWS = {
    "17_slide_slope_crouch": (0.40, 1.40),
    "18_vertical_wallrun_back": (0.60, 2.00),
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
    """Pass the reference angles through.

    Knee and hip flexion are not clamped. Straightening them to spare the hip
    shells was what made the slide and the pop stand upright. Overlap from the
    real flexion is reported, not posed away.
    """
    return dict(ch)


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
        ch = _tame(ch)
        ch["trunk_lean_from_cam_vertical"] = _lookup_angle(data, t, "trunk_lean_from_cam_vertical")
        ch["root_height_m"] = _lookup_series(data, t, "root_height_above_feet_m")
        ch["side_height_m"] = _side_pelvis_m(data, t)
        samples.append(ch)
        times.append(t)
    return samples, times, data


def _lookup_series(data, t_rel, key):
    """Linear sample of a per-frame scalar. t_rel is seconds from the clip start."""
    times = data["times_s"]
    vals = data[key]
    t0 = float(times[0])
    target = t0 + t_rel
    if target <= times[0]:
        return float(vals[0] or 0.0)
    if target >= times[-1]:
        return float(vals[-1] or 0.0)
    for i in range(len(times) - 1):
        if times[i] <= target <= times[i + 1]:
            span = max(1.0e-6, times[i + 1] - times[i])
            w = (target - times[i]) / span
            a = vals[i] if vals[i] is not None else 0.0
            b = vals[i + 1] if vals[i + 1] is not None else a
            return float(a) * (1.0 - w) + float(b) * w
    return 0.0


def _side_pelvis_m(data, t_rel):
    """Hip height above the foot line, in a side elevation of the world landmarks.

    Camera-vertical root height collapses when the body is nearly horizontal in
    the frame. The distance from the hip to each ankle-toe line is the height a
    side view measures. Scaled onto the Hier leg.
    """
    times = data["times_s"]
    worlds = data["world_xyz_m"]
    t0 = float(times[0])
    target = t0 + t_rel

    def at(i):
        W = worlds[i]
        if not W or W[23] is None or W[31] is None:
            return None
        hip = (
            (W[23][0] + W[24][0]) * 0.5,
            (W[23][1] + W[24][1]) * 0.5,
            (W[23][2] + W[24][2]) * 0.5,
        )

        def dist_line(a, b):
            ab = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
            ap = (hip[0] - a[0], hip[1] - a[1], hip[2] - a[2])
            cx = ap[1] * ab[2] - ap[2] * ab[1]
            cy = ap[2] * ab[0] - ap[0] * ab[2]
            cz = ap[0] * ab[1] - ap[1] * ab[0]
            lab = math.sqrt(ab[0] ** 2 + ab[1] ** 2 + ab[2] ** 2) or 1.0
            return math.sqrt(cx * cx + cy * cy + cz * cz) / lab

        leg = (
            math.dist(W[23], W[25]) + math.dist(W[25], W[27])
        )
        height = max(dist_line(W[27], W[31]), dist_line(W[28], W[32]))
        return height * (0.91 / max(leg, 0.15))

    if target <= times[0]:
        return at(0) or 0.0
    if target >= times[-1]:
        return at(len(times) - 1) or 0.0
    for i in range(len(times) - 1):
        if times[i] <= target <= times[i + 1]:
            a = at(i)
            b = at(i + 1)
            if a is None and b is None:
                return 0.0
            if a is None:
                return b
            if b is None:
                return a
            span = max(1.0e-6, times[i + 1] - times[i])
            w = (target - times[i]) / span
            return a * (1.0 - w) + b * w
    return 0.0


def _lookup_angle(data, t_rel, key):
    times = data["times_s"]
    t0 = float(times[0])
    target = t0 + t_rel
    angs = data["joint_angles_deg"]
    if target <= times[0]:
        return float((angs[0] or {}).get(key) or 0.0)
    if target >= times[-1]:
        return float((angs[-1] or {}).get(key) or 0.0)
    for i in range(len(times) - 1):
        if times[i] <= target <= times[i + 1]:
            span = max(1.0e-6, times[i + 1] - times[i])
            w = (target - times[i]) / span
            a = float((angs[i] or {}).get(key) or 0.0)
            b = float((angs[i + 1] or {}).get(key) or 0.0)
            return a * (1.0 - w) + b * w
    return 0.0


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
# A real low wall, about 1.1 m. Scale values are half-extents of a 2 m cube.
LOW_WALL_TOP = 1.10
LOW_WALL_HALF = (0.75, 0.65, 0.55)
LOW_WALL_CX = 0.40


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
    _hide_named(("ActionWall", "LandingWall", "WallTopLip", "GroundLine", "HipGhost"))
    _set_wall_ticks(0.0, False)
    _set_wall_labels(False)
    ground = bpy.data.objects.get("Ground")
    if ground is not None:
        ground.hide_render = True
        ground.hide_set(True)


def _set_wall_ticks(face_x, enabled):
    """Horizontal height marks on the wall edge, every 0.5 m, longer on the metre."""
    import bpy
    z = 0.5
    while z <= 4.01:
        name = f"WallTick_{int(round(z * 100))}"
        if not enabled:
            ob = bpy.data.objects.get(name)
            if ob is not None:
                ob.hide_render = True
                ob.hide_set(True)
            z += 0.5
            continue
        metre = abs(z - round(z)) < 0.05
        # Cross the face in X so an edge-on camera sees a horizontal tick.
        _make_box(
            name,
            (face_x - 0.01, 0.0, z),
            (0.055 if metre else 0.028, 0.012, 0.008),
            (0.95, 0.86, 0.45, 1) if metre else (0.82, 0.78, 0.70, 1),
        )
        z += 0.5


def _set_wall_labels(enabled):
    """Metre labels beside the wall, readable by the edge-on camera (looking +Y)."""
    import bpy
    for metre in (1, 2, 3, 4):
        name = f"WallLabel_{metre}"
        ob = bpy.data.objects.get(name)
        if not enabled:
            if ob is not None:
                ob.hide_render = True
                ob.hide_set(True)
            continue
        if ob is None:
            bpy.ops.object.text_add(location=(0.0, 0.0, float(metre)))
            ob = bpy.context.active_object
            ob.name = name
            ob.data.body = f"{metre}m"
            ob.data.size = 0.28
            ob.data.align_x = "RIGHT"
            ob.data.align_y = "CENTER"
            ob.data.extrude = 0.003
            mat = bpy.data.materials.new(name + "Mat")
            mat.use_nodes = True
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            if bsdf:
                bsdf.inputs["Base Color"].default_value = (0.96, 0.90, 0.55, 1)
                bsdf.inputs["Roughness"].default_value = 0.6
            ob.data.materials.append(mat)
        # Text lies on XY facing +Z. +90° X turns that face toward -Y (the camera)
        # and turns text-up to world +Z. Sit on the camera side of the face so
        # the 1.5 m roof does not swallow the glyph, and so the mark stays at
        # the face x in the locked side view.
        ob.location = (0.10, -(WALL_Y_HALF + 0.08), float(metre))
        ob.rotation_euler = (math.radians(90.0), 0.0, 0.0)
        ob.hide_render = False
        ob.hide_set(False)


def _set_brick_courses(face_x, enabled):
    """Mortar lines on the face so the 3/4 reads as brick, not a brown block."""
    z = 0.28
    i = 0
    while z < WALL_TOP_Z - 0.08:
        name = f"BrickCourse_{i:02d}"
        if not enabled:
            _hide_named((name,))
        else:
            _make_box(
                name,
                (face_x + 0.012, 0.0, z),
                (0.010, 2.55, 0.012),
                (0.62, 0.48, 0.40, 1),
            )
        i += 1
        z += 0.34


def show_wall_world(face_x, center_z, length_z=4.2, hide_ground=False, ticks=False, half_thick=0.16):
    import bpy
    # Solid occupies x < face_x. The +X face is the contact. Scale is half-extents
    # because the primitive cube is 2 m on a side (scale 1 → 2 m).
    _make_box(
        "ActionWall",
        (face_x - half_thick, 0.0, center_z),
        (half_thick, 2.6, length_z * 0.5),
        (0.45, 0.32, 0.28, 1),
    )
    _set_wall_ticks(face_x, ticks)
    _set_wall_labels(ticks)
    top_z = center_z + length_z * 0.5
    if ticks:
        # A brighter lip so the top edge reads against the sky.
        _make_box(
            "WallTopLip",
            (face_x - half_thick, 0.0, top_z - 0.018),
            (half_thick, 2.6, 0.018),
            (0.78, 0.66, 0.50, 1),
        )
        # Ground stroke under the runner and out past the roof, not a short
        # dash behind a post.
        _make_box(
            "GroundLine",
            (-0.15, 0.0, 0.008),
            (1.55, 2.6, 0.006),
            (0.84, 0.78, 0.66, 1),
        )
        _set_brick_courses(face_x, True)
    else:
        _hide_named(("WallTopLip", "GroundLine"))
        _set_brick_courses(face_x, False)
    _hide_named(("ActionSlope", "LandingWall"))
    ground = bpy.data.objects.get("Ground")
    if ground is not None:
        ground.hide_render = hide_ground
        ground.hide_set(hide_ground)


def show_turn_world(face_x, landing_top, landing_y):
    import bpy
    show_wall_world(face_x, 1.6, 3.6)
    # Low wall he lands on. Height is LOW_WALL_TOP (about 1.1 m).
    _make_box(
        "LandingWall",
        (LOW_WALL_CX, landing_y, landing_top * 0.5),
        LOW_WALL_HALF,
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


# Geometric knee ≈ LowerLeg X − 2.9°. Geometric hip elev ≈ −UpperLeg X − 2.9°.
# The old bind offset (12°) left both angles about 15° short of the reference.
_BONE_BIAS = 2.9
# MediaPipe standing hip-above-feet on the wall-run is ~0.75 m. Hier hip is 0.90 m.
_PELVIS_SCALE = 0.90 / 0.75


def _faithful_eulers(ch):
    """Reference hip, knee and arm angles, calibrated so the bone measure matches."""
    def g(name, default=0.0):
        return float(ch.get(name, default))

    pose = sp.unity_pose(ch)
    eul = sp.blender_euler(pose)
    for side in ("L", "R"):
        elev = g(f"hip_elev_{side}")
        flex = g(f"hip_flex_{side}")
        signed = elev if flex >= 0.0 else -elev
        abd = g(f"hip_abd_lat_{side}")
        yaw = abd if side == "L" else -abd
        eul[f"UpperLeg_{side}"] = (-(signed + _BONE_BIAS), 0.0, yaw)
        eul[f"LowerLeg_{side}"] = (g(f"knee_flex_{side}") + _BONE_BIAS, 0.0, 0.0)
    return eul


def _apply_faithful(arm, ch, location, rotation):
    import bpy
    from mathutils import Euler, Vector
    eulers = _faithful_eulers(ch)
    rh._apply_eulers(arm, eulers)
    _clear_pose_channels(arm)
    rh._apply_eulers(arm, eulers)
    arm.location = Vector(location)
    arm.rotation_euler = Euler(rotation, "XYZ") if not hasattr(rotation, "x") else rotation
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
        root.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()


def _foot_verts(side):
    return p5._verts(f"Foot_{side}")


def _seat_feet(arm, origin, normal):
    """Translate along the normal so the nearer sole sits on the plane. Angles stay."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    o = Vector(origin)
    gap = 1.0e9
    for side in ("L", "R"):
        for v in _foot_verts(side):
            gap = min(gap, n.dot(Vector(v) - o))
    if gap > 1.0e8:
        return 0.0
    arm.location = arm.location - n * gap
    bpy_update()
    return gap


def _plant_on_slope(arm, origin, normal, toe_hint):
    """Lay both soles on the ramp and put the lower one within 1 cm.

    A toe vertex used to touch while the shoe bottom sat 10–30 cm off the concrete.
    """
    for side in ("L", "R"):
        _orient_sole_on_plane(arm, side, normal, toe_hint)
    best_side = None
    best_gap = 1.0e9
    for side in ("L", "R"):
        gap = _sole_gap(arm, side, origin, normal)
        if gap < best_gap:
            best_gap = gap
            best_side = side
    if best_side is None:
        return 1.0e9
    _seat_sole(arm, best_side, origin, normal, clearance=0.006)
    # Keep every shell outside the concrete. If that lifts the sole past 1 cm,
    # the sole seat wins: the lowest point has to read as contact.
    gap = _min_plane(origin, normal)
    sole_now = _sole_gap(arm, best_side, origin, normal)
    if gap < -0.004 and sole_now < 0.004:
        _shift_plane(arm, origin, normal, clearance=0.004)
    return _sole_gap(arm, best_side, origin, normal)


def _bone_world_axis(arm, name, local=(0.0, 0.0, 1.0)):
    from mathutils import Vector
    pb = arm.pose.bones[name]
    mw = arm.matrix_world @ pb.matrix
    return (mw.to_3x3() @ Vector(local)).normalized()


def _aim_bone_axis(arm, name, local, target):
    """Point a bone's local axis along a world direction. Other joints stay."""
    import bpy
    from mathutils import Vector
    pb = arm.pose.bones[name]
    bpy.context.view_layer.update()
    mw = arm.matrix_world @ pb.matrix
    axis = (mw.to_3x3() @ Vector(local)).normalized()
    tgt = Vector(target).normalized()
    if axis.dot(tgt) > 0.995:
        return
    rot = axis.rotation_difference(tgt).to_matrix().to_4x4()
    new_mw = rot @ mw
    new_mw.translation = mw.translation
    pb.matrix = arm.matrix_world.inverted() @ new_mw
    bpy.context.view_layer.update()
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = pb.matrix_basis.to_euler("XYZ")
    pb.location = (0.0, 0.0, 0.0)
    pb.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()


def _sole_patch(arm, side):
    """Vertices on the bottom of the shoe. Bone +Z points out through the sole."""
    from mathutils import Vector
    n = _bone_world_axis(arm, f"Foot_{side}", (0.0, 0.0, 1.0))
    verts = [Vector(v) for v in _foot_verts(side)]
    if not verts:
        return n, []
    scored = sorted(verts, key=lambda v: n.dot(v), reverse=True)
    count = max(16, len(scored) // 8)
    return n, scored[:count]


def _sole_gap(arm, side, origin, normal):
    """Signed distance of the sole patch to a plane. Positive is outside."""
    from mathutils import Vector
    _n, patch = _sole_patch(arm, side)
    if not patch:
        return 1.0e9
    n = Vector(normal).normalized()
    o = Vector(origin)
    return min(n.dot(v - o) for v in patch)


def _seat_sole(arm, side, origin, normal, clearance=0.006):
    """Translate so this sole sits just outside the plane. Hip and knee stay."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    gap = _sole_gap(arm, side, origin, normal)
    if gap > 1.0e8:
        return gap
    arm.location = arm.location + n * (clearance - gap)
    bpy_update()
    return _sole_gap(arm, side, origin, normal)


def _hands_on_plane(arm, origin, normal):
    """If a hand is inside the solid, put that hand on the face. Legs stay put."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    o = Vector(origin)
    for side in ("L", "R"):
        vert = _deepest(f"Hand_{side}", n)
        if vert is None:
            continue
        gap = n.dot(Vector(vert) - o)
        if gap >= -0.008:
            continue
        lock = Vector(vert) - n * gap
        pole = n * 0.85 + Vector((0.0, 0.35 if side == "L" else -0.35, 0.45))
        _pin_limb(arm, "hand", side, lock, n, pole)


def _lowest_foot_z():
    zs = []
    for side in ("L", "R"):
        zs.extend(v.z for v in _foot_verts(side))
    return min(zs) if zs else 0.0


def _measure_body(arm):
    """Geometric knee flex, hip elev, spine pitch from vertical, hip above lowest sole."""
    from mathutils import Vector
    hips = p5._head_w(arm, "Hips")
    chest = p5._head_w(arm, "Chest")
    torso = chest - hips
    if torso.length < 1.0e-6:
        torso = Vector((0.0, 0.0, 1.0))
    torso = torso.normalized()
    up = Vector((0.0, 0.0, 1.0))
    pitch = math.degrees(math.acos(max(-1.0, min(1.0, torso.dot(up)))))
    out = {"spine": pitch, "hip_z": hips.z}
    lowest = None
    for side in ("L", "R"):
        hip = p5._head_w(arm, f"UpperLeg_{side}")
        knee = p5._head_w(arm, f"LowerLeg_{side}")
        ankle = p5._tail_w(arm, f"LowerLeg_{side}")
        thigh = knee - hip
        shin = ankle - knee
        if thigh.length < 1.0e-6 or shin.length < 1.0e-6:
            out[f"knee_{side}"] = 0.0
            out[f"hip_{side}"] = 0.0
        else:
            thigh = thigh.normalized()
            shin = shin.normalized()
            out[f"knee_{side}"] = math.degrees(math.acos(max(-1.0, min(1.0, thigh.dot(shin)))))
            out[f"hip_{side}"] = math.degrees(math.acos(max(-1.0, min(1.0, thigh.dot(-torso)))))
        for v in _foot_verts(side):
            if lowest is None or v.z < lowest.z:
                lowest = v
    out["pelvis"] = hips.z - (lowest.z if lowest is not None else 0.0)
    return out


def _angle_row(ch, body):
    """Reference vs mannequin. Hip is elevation (0 = hanging, 90 = forward)."""
    def g(name):
        return float(ch.get(name, 0.0))

    ref_hip = {}
    for side in ("L", "R"):
        ref_hip[side] = g(f"hip_elev_{side}")
    cam_h = g("root_height_m") * _PELVIS_SCALE
    side_h = g("side_height_m")
    # Camera-vertical height collapses when the body lies across the frame.
    collapsed = side_h > 0.08 and cam_h < side_h * 0.75
    ref_pelvis = side_h if collapsed else cam_h
    row = {
        "hip_L_ref": ref_hip["L"],
        "hip_L": body["hip_L"],
        "hip_R_ref": ref_hip["R"],
        "hip_R": body["hip_R"],
        "knee_L_ref": g("knee_flex_L"),
        "knee_L": body["knee_L"],
        "knee_R_ref": g("knee_flex_R"),
        "knee_R": body["knee_R"],
        "spine_ref": g("trunk_lean_from_cam_vertical"),
        "spine": body["spine"],
        "pelvis_ref_cm": ref_pelvis * 100.0,
        "pelvis_cm": body["pelvis"] * 100.0,
        "pelvis_src": "side" if collapsed else "camera",
        "pelvis_cam_cm": cam_h * 100.0,
        "pelvis_side_cm": side_h * 100.0,
    }
    return row


def _pack_frame(arm, ch, facing, skate, pen, note_row):
    body = _measure_body(arm)
    row = _angle_row(ch, body)
    row["t"] = None
    return {
        "keys": _save_pose(arm),
        "cap": (arm.location.x, arm.location.y, arm.location.z),
        "facing": (facing.x, facing.y, facing.z),
        "row": row,
        "skate": skate,
        "pen": pen,
    }


def solve_slide(arm, samples, times):
    """Reference crouch on the ramp. Legs are not straightened to reach it."""
    from mathutils import Vector, Euler
    _ang, y0, normal, down = _slope_frame()
    plane_o = Vector((0.0, y0, 0.0))
    nrm = Vector(normal).normalized()
    start = _project_plane(Vector((0.0, 1.55, 1.2)), plane_o, nrm)
    travel = 2.15
    n = len(samples)
    keys, caps, facings, rows = [], [], [], []
    skate = 0.0
    pen = 0.0
    prev = {}
    for i, ch in enumerate(samples):
        u = 0.0 if n == 1 else i / (n - 1)
        trunk = float(ch.get("trunk_lean_from_cam_vertical") or 0.0)
        anchor = start + down * (u * travel)
        # Extra downhill pitch drops the chest toward the concrete so the trail
        # hand can reach. Hip and spine stay inside 10° of the reference.
        best = None
        tried = []
        for extra in (0.0, 3.0, 6.0, 10.0, -3.0, -6.0, -10.0):
            facing = Euler((math.radians(trunk + extra), 0.0, 0.0), "XYZ")
            _apply_faithful(arm, ch, anchor, facing)
            show_slide_world(arm.location.y)
            _plant_on_slope(arm, plane_o, nrm, down)
            _hand_on_ramp(arm, plane_o, nrm)
            _clear_pose_arms(arm)
            _hand_on_ramp(arm, plane_o, nrm)
            _plant_on_slope(arm, plane_o, nrm, down)
            body = _measure_body(arm)
            hip = p5._head_w(arm, "Hips")
            body["pelvis"] = nrm.dot(Vector(hip) - plane_o)
            herr = max(
                abs(body["hip_L"] - float(ch.get("hip_elev_L", 0.0))),
                abs(body["hip_R"] - float(ch.get("hip_elev_R", 0.0))),
            )
            serr = abs(body["spine"] - trunk)
            gap = _min_plane(plane_o, nrm)
            hands = []
            for s in ("L", "R"):
                vert = _deepest(f"Hand_{s}", nrm)
                if vert is not None:
                    hands.append(nrm.dot(Vector(vert) - plane_o) * 100.0)
            hand = min(hands) if hands else 999.0
            tried.append((extra, hand))
            if herr > 9.6 or serr > 9.6 or gap < -0.002:
                continue
            score = (hand <= 3.0, -hand, -abs(extra))
            if best is None or score > best[0]:
                best = (score, _save_pose(arm), arm.location.copy(), facing, hand, extra)
        if best is None:
            facing = Euler((math.radians(trunk), 0.0, 0.0), "XYZ")
            _apply_faithful(arm, ch, anchor, facing)
            show_slide_world(arm.location.y)
            _plant_on_slope(arm, plane_o, nrm, down)
            _hand_on_ramp(arm, plane_o, nrm)
            _clear_pose_arms(arm)
            _hand_on_ramp(arm, plane_o, nrm)
            _plant_on_slope(arm, plane_o, nrm, down)
            hand_extra = 0.0
        else:
            _score, pose, loc, facing, _hand, hand_extra = best
            rh._apply_eulers(arm, pose)
            _clear_pose_channels(arm)
            rh._apply_eulers(arm, pose)
            arm.location = loc
            arm.rotation_euler = facing
            bpy_update()
        gap = _min_plane(plane_o, nrm)
        pen = max(pen, max(0.0, -gap))
        for side in ("L", "R"):
            vert = _deepest(f"Foot_{side}", nrm)
            if vert is None:
                continue
            err = Vector(vert) - plane_o
            along = err.dot(nrm)
            if abs(along) > 0.04:
                continue
            tangent = err - nrm * along
            if side in prev:
                skate = max(skate, (tangent - prev[side]).length)
            prev[side] = tangent
        body = _measure_body(arm)
        hip = p5._head_w(arm, "Hips")
        body["pelvis"] = nrm.dot(Vector(hip) - plane_o)
        row = _angle_row(ch, body)
        row["t"] = times[i]
        row["world_pen_cm"] = max(0.0, -gap) * 100.0
        hand_gaps = []
        for s in ("L", "R"):
            vert = _deepest(f"Hand_{s}", nrm)
            if vert is not None:
                hand_gaps.append(nrm.dot(Vector(vert) - plane_o) * 100.0)
        row["hand_cm"] = min(hand_gaps) if hand_gaps else 999.0
        row["hand_extra_deg"] = hand_extra
        base_hand = next((h for e, h in tried if abs(e) < 0.1), row["hand_cm"])
        row["hand_base_cm"] = base_hand
        row["hand_short_cm"] = max(0.0, row["hand_cm"] - 3.0)
        # Linear guess only when the 10° pitch actually moved the hand.
        more = 0.0
        if row["hand_short_cm"] > 0.05:
            gain = base_hand - row["hand_cm"]
            if abs(hand_extra) > 0.5 and gain > 1.0:
                more = row["hand_short_cm"] / (gain / abs(hand_extra))
        row["hand_more_deg"] = more
        row["sole_L_cm"] = _sole_gap(arm, "L", plane_o, nrm) * 100.0
        row["sole_R_cm"] = _sole_gap(arm, "R", plane_o, nrm) * 100.0
        row["low_cm"] = _min_plane(plane_o, nrm) * 100.0
        rows.append(row)
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
        if i % 10 == 0:
            print(
                f"  slide f={i} hip={body['pelvis']*100:.1f}cm "
                f"hand={row['hand_cm']:.1f} extra={row['hand_extra_deg']:.0f} "
                f"short={row['hand_short_cm']:.1f}",
                flush=True,
            )
    over = [r for r in rows if r["hand_cm"] > 3.0]
    print(
        f"  slide hand>3cm {len(over)}/{len(rows)} "
        f"max={max(r['hand_cm'] for r in rows):.1f}",
        flush=True,
    )
    return {
        "keys": keys, "capsule": caps, "facing": facings, "times": times,
        "skate": skate, "pen": pen, "errors": rows, "world": {"kind": "slide"},
        "note": (
            "Feet-first crouched slide. Both soles are aimed at the concrete and "
            "the lower sole sits within 1 cm. The chest pitches up to 10° further "
            "into the ramp so the trail hand can reach. Frames that still miss "
            "the hand by more than 3 cm log the extra degrees. Legs are not "
            "straightened. The source is a crouched slide, not a thigh-down slide. "
            "A thigh-down slide reference (hip or thigh on the ground) is still needed. "
            "Clips 01-20 have no ground slide under a rail or bar. Clip 14 is "
            "rail precisions, not a slide. A pass-10 search of public Storror and "
            "other parkour footage found slope slides, rail grinds, slide vaults "
            "and underbars (feet through a rail, back off the ground). No clean "
            "side or 3/4 view of a feet-first or thigh-down ground slide under a "
            "bar, so this pose is unchanged."
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


def _knee_plants(samples, hold=3):
    """The stepping leg is the more bent knee. Hold it so the plant does not chatter."""
    sides = []
    current = None
    pending = None
    pending_n = 0
    for ch in samples:
        want = "L" if ch.get("knee_flex_L", 0.0) >= ch.get("knee_flex_R", 0.0) else "R"
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


def _swing_thigh(arm, side, deg):
    """Turn the thigh around the torso. Elevation and knee flex stay put."""
    import bpy
    from mathutils import Quaternion, Matrix
    if abs(deg) < 0.5:
        return
    pb = arm.pose.bones[f"UpperLeg_{side}"]
    bpy.context.view_layer.update()
    hips = p5._head_w(arm, "Hips")
    chest = p5._head_w(arm, "Chest")
    axis = chest - hips
    if axis.length < 1.0e-6:
        return
    axis = axis.normalized()
    mw = arm.matrix_world @ pb.matrix
    rot = Quaternion(axis, math.radians(deg)).to_matrix().to_4x4()
    spun = Matrix.Translation(mw.translation) @ rot @ Matrix.Translation(-mw.translation)
    pb.matrix = arm.matrix_world.inverted() @ (spun @ mw)
    bpy.context.view_layer.update()
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = pb.matrix_basis.to_euler("XYZ")
    pb.location = (0.0, 0.0, 0.0)
    pb.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()


def _retract_hands(arm, origin, normal):
    """Put a hand that has entered the solid back on the face. The sole stays."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    for side in ("L", "R"):
        verts = p5._verts(f"Hand_{side}")
        if not verts:
            continue
        vert = min(verts, key=lambda v: n.dot(Vector(v) - Vector(origin)))
        gap = n.dot(Vector(vert) - Vector(origin))
        if gap >= 0.004:
            continue
        lock = Vector(vert) - n * (gap - 0.008)
        pole = n * 0.9 + Vector((0.0, 0.35 if side == "L" else -0.35, 0.45))
        _pin_limb(arm, "hand", side, lock, n, pole)


def _swing_free_leg_out(arm, plant):
    """The trail leg must not pass through the wall the plant foot is on."""
    other = "R" if plant == "L" else "L"
    base = _save_pose(arm)
    loc = arm.location.copy()
    best = None
    for swing in (0, 30, -30, 60, -60, 90, -90, 120, -120):
        rh._apply_eulers(arm, base)
        _clear_pose_channels(arm)
        rh._apply_eulers(arm, base)
        arm.location = loc
        bpy_update()
        if swing:
            _swing_thigh(arm, other, swing)
        xs = [v.x for v in p5._verts(f"Foot_{other}")]
        xs += [v.x for v in p5._verts(f"LowerLeg_{other}")]
        xs += [v.x for v in p5._verts(f"UpperLeg_{other}")]
        if not xs:
            continue
        depth = min(xs)
        if best is None or depth > best[0]:
            best = (depth, _save_pose(arm), arm.location.copy())
        if depth >= -0.002:
            break
    if best is None:
        return
    rh._apply_eulers(arm, best[1])
    _clear_pose_channels(arm)
    rh._apply_eulers(arm, best[1])
    arm.location = best[2]
    bpy_update()


def _lift_shin_off_wall(arm, side, ch):
    """Straighten the plant knee a few degrees if the shin has crossed the face."""
    for _step in range(6):
        xs = [v.x for v in p5._verts(f"LowerLeg_{side}")]
        xs += [v.x for v in p5._verts(f"UpperLeg_{side}")]
        if not xs or min(xs) >= -0.002:
            return
        pb = arm.pose.bones[f"LowerLeg_{side}"]
        pb.rotation_mode = "XYZ"
        e = pb.rotation_euler.copy()
        pb.rotation_euler = (e.x - math.radians(2.0), e.y, e.z)
        bpy_update()
        _aim_bone_axis(arm, f"Foot_{side}", (0.0, 0.0, 1.0), (-1.0, 0.0, 0.0))
        _seat_sole(arm, side, (0.0, 0.0, 0.0), (1.0, 0.0, 0.0), clearance=0.005)
        body = _measure_body(arm)
        err = abs(body[f"knee_{side}"] - float(ch.get(f"knee_flex_{side}", 0.0)))
        if err > 9.5:
            pb.rotation_euler = e
            bpy_update()
            _aim_bone_axis(arm, f"Foot_{side}", (0.0, 0.0, 1.0), (-1.0, 0.0, 0.0))
            _seat_sole(arm, side, (0.0, 0.0, 0.0), (1.0, 0.0, 0.0), clearance=0.005)
            return


def _tilt_head_off_wall(arm):
    """Tip the head back if the shell crosses the face. The legs stay."""
    pb = arm.pose.bones.get("Head")
    if pb is None:
        return
    for _step in range(8):
        vs = p5._verts("Head")
        if not vs or min(v.x for v in vs) >= 0.0:
            return
        pb.rotation_mode = "XYZ"
        e = pb.rotation_euler.copy()
        pb.rotation_euler = (e.x - math.radians(3.0), e.y, e.z)
        bpy_update()


def _orient_sole(arm, side, toe, sole, bone=None):
    """Point bone +Z along sole and bone +Y along toe. Defaults to the foot."""
    import bpy
    from mathutils import Vector, Matrix
    pb = arm.pose.bones[bone or f"Foot_{side}"]
    bpy.context.view_layer.update()
    mw = arm.matrix_world @ pb.matrix
    toe_v = Vector(toe).normalized()
    sole_v = Vector(sole).normalized()
    side_axis = toe_v.cross(sole_v)
    if side_axis.length < 1.0e-6:
        return
    side_axis.normalize()
    rot = Matrix((
        (side_axis.x, toe_v.x, sole_v.x),
        (side_axis.y, toe_v.y, sole_v.y),
        (side_axis.z, toe_v.z, sole_v.z),
    )).to_4x4()
    rot.translation = mw.translation
    pb.matrix = arm.matrix_world.inverted() @ rot
    bpy.context.view_layer.update()
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = pb.matrix_basis.to_euler("XYZ")
    pb.location = (0.0, 0.0, 0.0)
    pb.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()


def _orient_sole_on_wall(arm, side):
    """Sole (bone +Z) into the brick, toes (bone +Y) up the face."""
    _orient_sole(arm, side, (0.0, 0.0, 1.0), (-1.0, 0.0, 0.0))


def _orient_sole_on_plane(arm, side, normal, toe_hint):
    """Lay the sole on a plane. Bone +Z points into the solid."""
    from mathutils import Vector
    sole = -Vector(normal).normalized()
    toe = Vector(toe_hint)
    toe = toe - sole * toe.dot(sole)
    if toe.length < 1.0e-6:
        toe = Vector((0.0, 0.0, 1.0))
    _orient_sole(arm, side, toe, sole)


def _place_sole(arm, side, foot_z, clearance=0.005):
    """Put this sole on the wall face at foot_z. The pose does not change."""
    from mathutils import Vector
    _n, patch = _sole_patch(arm, side)
    if not patch:
        return
    center = sum(patch, Vector()) / len(patch)
    arm.location.x += clearance - center.x
    arm.location.y += 0.0 - center.y
    arm.location.z += foot_z - center.z
    bpy_update()
    _seat_sole(arm, side, (0.0, 0.0, 0.0), (1.0, 0.0, 0.0), clearance=clearance)


def _open_chest(arm, ch):
    """Lean the spine so the chest stays just off the brick.

    Hip and knee stay inside 10°. The spine may sit up to 15° off the reference
    when that is what keeps the chest out of the wall.
    """
    ref = float(ch.get("trunk_lean_from_cam_vertical") or 0.0)
    pb = arm.pose.bones.get("Spine")
    if pb is None:
        return 0.0
    pb.rotation_mode = "XYZ"
    base = pb.rotation_euler.copy()
    best = None
    for deg in range(-15, 16, 3):
        pb.rotation_euler = (base.x + math.radians(deg), base.y, base.z)
        bpy_update()
        upper_x = []
        for token in ("Spine", "Chest", "Neck", "Head"):
            upper_x.extend(v.x for v in p5._verts(token))
        chest = min(upper_x) if upper_x else -1.0
        body = _measure_body(arm)
        herr = max(
            abs(body["hip_L"] - float(ch.get("hip_elev_L", 0.0))),
            abs(body["hip_R"] - float(ch.get("hip_elev_R", 0.0))),
        )
        kerr = max(
            abs(body["knee_L"] - float(ch.get("knee_flex_L", 0.0))),
            abs(body["knee_R"] - float(ch.get("knee_flex_R", 0.0))),
        )
        serr = abs(body["spine"] - ref)
        if herr > 9.6 or kerr > 9.6 or serr > 14.6:
            continue
        outside = chest >= 0.004
        # Just off the brick reads as chest-toward-wall. Further out looks like a stand.
        score = (outside, -abs(chest - 0.045) if outside else chest, -abs(deg))
        if best is None or score > best[0]:
            best = (score, deg, pb.rotation_euler.copy())
    if best is None:
        pb.rotation_euler = base
        bpy_update()
        return 0.0
    pb.rotation_euler = best[2]
    bpy_update()
    return best[1]


def _pitch_chest_off_wall(arm, ch, side, foot_z, facing):
    """Pitch the whole body so a spine shell in the brick comes back out.

    The foot is planted again after each try. Hip and knee stay inside 10°,
    and the spine stays inside 15° of the reference.
    """
    from mathutils import Euler
    ref = float(ch.get("trunk_lean_from_cam_vertical") or 0.0)
    base_pose = _save_pose(arm)
    base_loc = arm.location.copy()
    best = None
    for deg in (0, -4, -8, -12, -15, 4, 8, 12):
        rh._apply_eulers(arm, base_pose)
        _clear_pose_channels(arm)
        rh._apply_eulers(arm, base_pose)
        arm.location = base_loc
        arm.rotation_euler = Euler((facing.x + math.radians(deg), facing.y, facing.z), "XYZ")
        bpy_update()
        _orient_sole_on_wall(arm, side)
        _place_sole(arm, side, foot_z)
        body = _measure_body(arm)
        herr = max(
            abs(body["hip_L"] - float(ch.get("hip_elev_L", 0.0))),
            abs(body["hip_R"] - float(ch.get("hip_elev_R", 0.0))),
        )
        kerr = max(
            abs(body["knee_L"] - float(ch.get("knee_flex_L", 0.0))),
            abs(body["knee_R"] - float(ch.get("knee_flex_R", 0.0))),
        )
        serr = abs(body["spine"] - ref)
        if herr > 9.6 or kerr > 9.6 or serr > 14.6:
            continue
        gap = _min_plane((0.0, 0.0, 0.0), (1.0, 0.0, 0.0))
        score = (gap, -abs(deg))
        if best is None or score > best[0]:
            best = (score, _save_pose(arm), arm.location.copy(), arm.rotation_euler.copy())
    if best is None:
        rh._apply_eulers(arm, base_pose)
        _clear_pose_channels(arm)
        rh._apply_eulers(arm, base_pose)
        arm.location = base_loc
        arm.rotation_euler = facing
        bpy_update()
        return facing
    _score, pose, loc, rot = best
    rh._apply_eulers(arm, pose)
    _clear_pose_channels(arm)
    rh._apply_eulers(arm, pose)
    arm.location = loc
    arm.rotation_euler = rot
    bpy_update()
    return rot


def _arms_reach_up(arm, fore_x=-0.20, fore_r=None, upper_x=-0.52, upper_r=None, up_z=0.22):
    """Both hands up and in, palms toward the brick, for the hand-off at the top.

    The upper arm reaches in and the forearm stays mostly up, so the elbow
    keeps a bend. With the chest leaned into the wall the shoulder is closer,
    so the upper arm starts less deep than the old -0.86 aim. up_z drops the
    hands back to the 4.2 m reach when the lean has lifted them.
    """
    from mathutils import Vector
    if fore_r is None:
        fore_r = fore_x
    if upper_r is None:
        upper_r = upper_x
    for side, lateral, fx, ux in (
        ("L", 0.04, fore_x, upper_x),
        ("R", -0.04, fore_r, upper_r),
    ):
        _aim_bone_axis(arm, f"UpperArm_{side}", (0.0, 1.0, 0.0), Vector((ux, lateral, up_z)))
        _aim_bone_axis(arm, f"LowerArm_{side}", (0.0, 1.0, 0.0), Vector((fx, lateral * 0.2, 0.62 + up_z)))
        # Hand local -Z is turned toward the face. The mesh palm follows that axis.
        _aim_bone_axis(arm, f"Hand_{side}", (0.0, 0.0, -1.0), Vector((-0.92, lateral * 0.15, 0.22)))


def _wall_reach(arm, ch, side, facing):
    """Best foot-in-front-of-hip reach that stays inside 10° of hip and knee."""
    from mathutils import Vector
    best = None
    for extra in (0.0, 5.0, 10.0):
        for knee_extra in (0.0, -8.0, 8.0):
            for swing in range(-90, 91, 15):
                _apply_faithful(arm, ch, Vector((0.55, 0.0, 1.4)), facing)
                if extra:
                    pb = arm.pose.bones[f"UpperLeg_{side}"]
                    pb.rotation_mode = "XYZ"
                    e = pb.rotation_euler.copy()
                    pb.rotation_euler = (e.x - math.radians(extra), e.y, e.z)
                    bpy_update()
                if knee_extra:
                    pb = arm.pose.bones[f"LowerLeg_{side}"]
                    pb.rotation_mode = "XYZ"
                    e = pb.rotation_euler.copy()
                    pb.rotation_euler = (e.x + math.radians(knee_extra), e.y, e.z)
                    bpy_update()
                if swing:
                    _swing_thigh(arm, side, swing)
                _orient_sole_on_wall(arm, side)
                body = _measure_body(arm)
                herr = abs(body[f"hip_{side}"] - float(ch.get(f"hip_elev_{side}", 0.0)))
                kerr = abs(body[f"knee_{side}"] - float(ch.get(f"knee_flex_{side}", 0.0)))
                if herr > 9.6 or kerr > 9.6:
                    continue
                hip = p5._head_w(arm, "Hips")
                _n, patch = _sole_patch(arm, side)
                if not patch:
                    continue
                sole = sum(patch, Vector()) / len(patch)
                sole_x = min(v.x for v in patch)
                leg_xs = [v.x for v in p5._verts(f"UpperLeg_{side}")]
                leg_xs += [v.x for v in p5._verts(f"LowerLeg_{side}")]
                # Positive means the shin sits outside the sole, so seating the
                # sole on the face does not drive it through the brick.
                shin_clear = (min(leg_xs) - sole_x) if leg_xs else 0.0
                reach = hip.x - sole.x
                outside = shin_clear >= -0.008
                score = (outside, shin_clear, reach, -abs(knee_extra), -abs(swing))
                if best is None or score > best[0]:
                    best = (score, _save_pose(arm), arm.location.copy(), extra, swing)
    return best


def _shin_wall_deg(shin):
    """Angle between the shin and the vertical wall. 0 is straight up the face."""
    return math.degrees(math.acos(max(-1.0, min(1.0, abs(float(shin.z))))))


def _shin_world(arm, side):
    """Unit shin, knee to ankle, plus the sole-patch centre."""
    from mathutils import Vector
    knee = p5._head_w(arm, f"LowerLeg_{side}")
    ankle = p5._tail_w(arm, f"LowerLeg_{side}")
    shin = Vector(ankle) - Vector(knee)
    if shin.length < 1.0e-6:
        shin = Vector((-1.0, 0.0, 0.0))
    else:
        shin.normalize()
    _n, patch = _sole_patch(arm, side)
    sole = sum(patch, Vector()) / len(patch) if patch else Vector(ankle)
    return shin, sole


def _wall_cycle(i, n, n_steps=3):
    """Three equal steps. Each step swings, plants, then pushes."""
    u = 0.0 if n <= 1 else i / (n - 1)
    x = u * n_steps
    step = min(n_steps - 1, int(math.floor(x + 1.0e-6)))
    if u >= 1.0:
        step = n_steps - 1
        local = 1.0
    else:
        local = x - step
    side = "L" if step % 2 == 0 else "R"
    if local < 0.40:
        phase = "swing"
    elif local < 0.72:
        phase = "plant"
    else:
        phase = "push"
    return step, local, side, phase


def _arms_counter(arm, lead, local, both_up, brush=False):
    """Sprint arms: the arm opposite the plant leg swings forward.

    The free arm swings back and out, clear of the ribs. A hand left on the
    centre line sits inside the torso in the side view.
    """
    from mathutils import Vector
    if both_up:
        _arms_reach_up(arm)
        return
    t = _clamp(local, 0.0, 1.0)
    opp = "R" if lead == "L" else "L"
    for side, forward in ((opp, True), (lead, False)):
        if forward:
            # Toward the wall and a little wide, so the side view shows the
            # reach and the upper arm is not inside the leaned chest.
            # Upper and forearm stay near perpendicular: elbow about 90°.
            lat = 0.32 if side == "L" else -0.32
            upper = Vector((-0.92, lat, 0.10 + 0.06 * t))
            fore_x = -0.10 if brush else 0.02
            forearm = Vector((fore_x, lat * 0.20, 0.96))
        else:
            # Back, past the ribs, opposite the plant leg. The side camera
            # only sees the +X reach; a purely lateral swing stays hidden
            # inside the torso.
            lat = 0.28 if side == "L" else -0.28
            upper = Vector((0.94, lat, -0.06))
            forearm = Vector((0.18, lat * 0.25, -0.94))
        _aim_bone_axis(arm, f"UpperArm_{side}", (0.0, 1.0, 0.0), upper)
        _aim_bone_axis(arm, f"LowerArm_{side}", (0.0, 1.0, 0.0), forearm)


def _climb_hip(step, local):
    """Hip height of the three wall steps, before the run-in stride is joined on."""
    if local < 0.40:
        rise = 0.12 * (local / 0.40)
    elif local < 0.72:
        rise = 0.12 + 0.08 * ((local - 0.40) / 0.32)
    else:
        rise = 0.20 + 0.40 * ((local - 0.72) / 0.28)
    return 1.42 + step * 0.60 + rise


def _wall_hip_z(i, n):
    """Hip height that rises on every frame, from the grounded stride up to the top."""
    u = 0.0 if n <= 1 else i / (n - 1)
    x = u * 3.0
    step = min(2, int(math.floor(x + 1.0e-6)))
    local = 1.0 if u >= 1.0 else (x - step)
    climb = _climb_hip(step, local)
    # The first part of step 0 is the take-off. A straight leg with the sole
    # on the ground holds the hip near 0.90 m, so the grounded frames rise
    # through that band and the airborne frames meet the climb curve.
    if step == 0 and local < 0.34:
        t = local / 0.34
        end = _climb_hip(0, 0.34)
        if t <= 0.47:
            return 0.86 + 0.06 * (t / 0.47)
        s = (t - 0.47) / 0.53
        return 0.92 + (end - 0.92) * s
    return climb


def _signed_trunk_deg(arm):
    """Degrees the chest leans toward the wall. Positive is into the face.

    This is the angle the side picture shows. The armature euler is not that
    angle: the spine bones can bend the chest the other way.
    """
    from mathutils import Vector
    hips = Vector(p5._head_w(arm, "Hips"))
    chest = Vector(p5._head_w(arm, "Chest"))
    dz = chest.z - hips.z
    dx = hips.x - chest.x
    return math.degrees(math.atan2(dx, max(dz, 1.0e-4)))


def _zero_trunk(arm):
    """Drop the mocap spine bend so the chest follows the body pitch."""
    for name in ("Spine", "Chest", "Neck", "Head"):
        pb = arm.pose.bones.get(name)
        if pb is None:
            continue
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
    bpy_update()


def _facing_for_lean(arm, target_deg):
    """Armature pitch whose measured chest angle is target_deg, into the wall.

    Yaw -90 is the facing, not a camera trick. At yaw 0 the nose (head bone +Z)
    points world -Y, which is straight at the side camera. Yaw -90 turns that
    nose onto -X, into the wall, and the chest centre sits closer to the face
    than the hips. Pitch is searched around that yaw. Do not orbit the camera
    to fake a profile.
    """
    from mathutils import Euler
    yaw = math.radians(-90.0)

    def apply(deg):
        arm.rotation_euler = Euler((math.radians(deg), 0.0, yaw), "XYZ")
        bpy_update()
        return _signed_trunk_deg(arm)

    sign = 1.0 if apply(12.0) >= apply(-12.0) else -1.0
    lo, hi = -35.0, 35.0
    best = 0.0
    for _step in range(16):
        mid = 0.5 * (lo + hi)
        got = apply(mid)
        best = mid
        if (got < target_deg) == (sign > 0.0):
            lo = mid
        else:
            hi = mid
    apply(best)
    facing = Euler((math.radians(best), 0.0, yaw), "XYZ")
    return facing, _signed_trunk_deg(arm)


def _place_hip_at(arm, x, y, z):
    """Translate so the hip bone sits on this point. The pose is unchanged."""
    from mathutils import Vector
    hip = Vector(p5._head_w(arm, "Hips"))
    arm.location = arm.location + (Vector((x, y, z)) - hip)
    bpy_update()


def _ik_ankle_to(arm, side, tx, ty, tz):
    """Two-bone leg reach. The hip stays. The knee prefers to stay out of the brick."""
    from mathutils import Vector
    hip = Vector(p5._head_w(arm, f"UpperLeg_{side}"))
    L1 = arm.pose.bones[f"UpperLeg_{side}"].length
    L2 = arm.pose.bones[f"LowerLeg_{side}"].length
    target = Vector((tx, ty, tz))
    delta = target - hip
    d = delta.length
    if d < 1.0e-5:
        return
    reach = max(abs(L1 - L2) + 0.02, min(d, L1 + L2 - 0.02))
    toward = delta.normalized()
    if d > reach + 1.0e-4:
        target = hip + toward * reach
        delta = target - hip
        d = max(delta.length, 1.0e-6)
        toward = delta.normalized()
    # Bend the knee out of the wall (+X) and a little to the side.
    sign = 1.0 if side == "L" else -1.0
    pole = Vector((0.45, 0.35 * sign, 0.25))
    perp = toward.cross(pole)
    if perp.length < 1.0e-5:
        perp = Vector((0.0, sign, 0.0))
    perp.normalize()
    if perp.x < 0.0:
        perp = -perp
    cos_a = (L1 * L1 + d * d - L2 * L2) / max(1.0e-6, 2.0 * L1 * d)
    cos_a = max(-1.0, min(1.0, cos_a))
    sin_a = math.sqrt(max(0.0, 1.0 - cos_a * cos_a))
    knee_dir = toward * cos_a + perp * sin_a
    _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), knee_dir)
    knee = Vector(p5._head_w(arm, f"LowerLeg_{side}"))
    _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), target - knee)


def _seat_wall_foot(arm, side, sole_x=0.008, sole_z=1.0):
    """Put this sole on the face by rotating the leg. The hip is not moved."""
    from mathutils import Vector
    hip = Vector(p5._head_w(arm, "Hips"))
    sign = 1.0 if side == "L" else -1.0
    tx, ty, tz = sole_x + 0.05, hip.y + 0.05 * sign, sole_z + 0.04
    for _step in range(8):
        _ik_ankle_to(arm, side, tx, ty, tz)
        _orient_sole_on_wall(arm, side)
        _n, patch = _sole_patch(arm, side)
        if not patch:
            return
        cx = min(v.x for v in patch)
        cz = sum(v.z for v in patch) / len(patch)
        if abs(cx - sole_x) < 0.008 and abs(cz - sole_z) < 0.025:
            return
        tx += sole_x - cx
        tz += sole_z - cz
        tx = _clamp(tx, -0.02, 0.55)
        tz = _clamp(tz, 0.05, WALL_TOP_Z - 0.05)


def _seat_ground_foot(arm, side, sole_x=0.78):
    """Drive sole on the ground, clear of the wall. The hip is not moved."""
    from mathutils import Vector
    hip = Vector(p5._head_w(arm, "Hips"))
    tx, ty, tz = sole_x, hip.y, 0.06
    for _step in range(8):
        _ik_ankle_to(arm, side, tx, ty, tz)
        _orient_sole(arm, side, (-1.0, 0.0, 0.15), (0.0, 0.0, -1.0))
        _n, patch = _sole_patch(arm, side)
        if not patch:
            return
        cx = sum(v.x for v in patch) / len(patch)
        cz = min(v.z for v in patch)
        if abs(cx - sole_x) < 0.02 and abs(cz - 0.006) < 0.008:
            return
        tx += sole_x - cx
        tz += 0.006 - cz
        tx = _clamp(tx, 0.20, 1.15)
        tz = _clamp(tz, -0.02, 0.35)


def _chest_over_edge(arm):
    """True once the chest centre has cleared the lip: past the face and above it."""
    vs = list(p5._verts("Chest"))
    if not vs:
        return False, 0.0, 0.0
    cx = sum(v.x for v in vs) / len(vs)
    cz = sum(v.z for v in vs) / len(vs)
    return cx < 0.0 and cz > WALL_TOP_Z + 0.02, cx, cz


def _pose_wall_step(arm, ch, side, phase, local, lean_deg=-14.0, reach_top=False):
    """One step: swing the knee up, plant the sole on the face, push up.

    Thigh and shin are 0.456 m. The plant shin sits about 38° off the
    vertical face, toes up the brick, sole on the wall. The hip stays close
    enough for that angle. The push does not open into a horizontal kick.
    """
    from mathutils import Vector, Euler
    other = "R" if side == "L" else "L"
    plat = 0.07 if side == "L" else -0.07
    olat = -plat
    _apply_faithful(
        arm, ch, Vector((0.58, 0.0, 1.8)),
        Euler((0.0, 0.0, math.radians(-90.0)), "XYZ"),
    )
    # The source spine bends the chest away from the wall. Zero it, then pitch
    # until the measured chest angle matches the picture we want.
    _zero_trunk(arm)
    facing, _measured = _facing_for_lean(arm, lean_deg)
    # High knee. The trail leg finishes the step in this shape, so the next
    # lead starts here and the legs do not pop when they swap.
    sw_th = Vector((-0.45, plat, 0.82))
    sw_sh = Vector((-0.38, 0.0, -0.80))
    # Shin about 38° off the vertical face, toes up the brick. The Y
    # component keeps the calf shell out of the chest. It is depth to the
    # side camera, not the left-right split. The split was the trail foot
    # stuck out at +X, which the plant trail no longer does.
    plant_lat = 0.24 if side == "L" else -0.24
    pl_th = Vector((-0.38, plant_lat, 0.92))
    pl_sh = Vector((-0.62, 0.0, -0.78))
    # The push keeps that shin. Opening into a horizontal kick is what
    # shoved the last step off the face.
    pu_th = Vector((-0.32, plant_lat, 0.88))
    pu_sh = Vector((-0.60, 0.0, -0.80))
    hang_th = Vector((0.48, olat, -0.82))
    hang_sh = Vector((0.18, olat * 0.4, -0.94))
    pu_trail_th = Vector((-0.64, olat, -0.77))
    pu_trail_sh = Vector((-0.99, 0.0, -0.11))
    next_th = Vector((-0.45, olat, 0.82))
    next_sh = Vector((-0.38, olat * 0.3, -0.80))
    if phase == "takeoff":
        # Blend the grounded stride into the swing that feeds the first plant.
        t = _clamp(local / 0.34, 0.0, 1.0)
        sw_t = 0.80
        end_th = sw_th.lerp(pl_th, sw_t)
        end_sh = sw_sh.lerp(pl_sh, sw_t)
        # Lead knee lifts out of the stride, clear of the face. Drive leg
        # stays down so the sole can sit on the ground.
        thigh = Vector((-0.05, plat, 0.55)).lerp(end_th, t)
        shin = Vector((0.15, 0.0, -0.75)).lerp(end_sh, t)
        end_tr_th = pu_trail_th.lerp(hang_th, 0.40 + 0.60 * sw_t)
        end_tr_sh = pu_trail_sh.lerp(hang_sh, 0.40 + 0.60 * sw_t)
        if local < 0.16:
            # Stride contact: the knee opens as the hip rises. A bent leg
            # on frame 0 leaves room for the next frames to extend.
            g = _clamp(local / 0.16, 0.0, 1.0)
            trail_th = Vector((0.32, olat, -0.86)).lerp(Vector((0.08, olat, -0.98)), g)
            trail_sh = Vector((0.48, 0.0, -0.84)).lerp(Vector((0.04, 0.0, -0.99)), g)
        else:
            trail_th = Vector((0.12, olat, -0.96)).lerp(end_tr_th, t)
            trail_sh = Vector((0.05, 0.0, -0.98)).lerp(end_tr_sh, t)
    elif phase == "swing":
        t = _clamp(local / 0.40, 0.0, 1.0)
        thigh = sw_th.lerp(pl_th, t)
        shin = sw_sh.lerp(pl_sh, t)
        # The leg that just pushed is already leaving the face.
        trail_th = pu_trail_th.lerp(hang_th, 0.40 + 0.60 * t)
        trail_sh = pu_trail_sh.lerp(hang_sh, 0.40 + 0.60 * t)
    elif phase == "plant":
        thigh = pl_th
        shin = pl_sh
        # Free knee drives up in the camera plane, foot tucked toward the
        # wall under the knee. A shin that drops back to hip height at +X
        # is the other half of the frog split.
        trail_lat = 0.06 if other == "L" else -0.06
        trail_th = Vector((-0.16, trail_lat, 0.97))
        trail_sh = Vector((-0.40, trail_lat * 0.5, -0.80))
    else:
        # Stay on the plant's reach. A full extension shoved the hip out
        # and left the sole kicking air.
        t = _clamp((local - 0.72) / 0.28, 0.0, 1.0) * 0.35
        thigh = pl_th.lerp(pu_th, t)
        shin = pl_sh.lerp(pu_sh, t)
        trail_th = hang_th.lerp(next_th, t)
        trail_sh = hang_sh.lerp(next_sh, t)
        if reach_top:
            # The last push draws the hip in over the foot so the hands can
            # reach the face. The trail knee stays up and out, beside the
            # body: a knee driven into the brick would shove the plant off.
            thigh = thigh.lerp(Vector((-0.40, plat, -0.78)), t)
            shin = shin.lerp(Vector((-0.42, 0.0, -0.82)), t)
            trail_th = trail_th.lerp(Vector((0.50, olat, 0.78)), t)
            trail_sh = trail_sh.lerp(Vector((0.28, olat * 0.3, -0.90)), t)
    _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), thigh)
    _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), shin)
    _aim_bone_axis(arm, f"UpperLeg_{other}", (0.0, 1.0, 0.0), trail_th)
    _aim_bone_axis(arm, f"LowerLeg_{other}", (0.0, 1.0, 0.0), trail_sh)
    if phase == "takeoff" and local < 0.18:
        # Drive: toes toward the wall, sole into the ground.
        _orient_sole(arm, other, (-1.0, 0.0, 0.12), (0.0, 0.0, -1.0))
        # Lead foot is still in the stride. Toes up, sole away from the
        # brick, so a toe does not spear the face before the first plant.
        _orient_sole(arm, side, (0.05, 0.0, 0.99), (0.90, 0.0, -0.15))
    else:
        _orient_sole_on_wall(arm, side)
    # Seat the sole with this pitch. The replay applies the saved facing
    # without posing again, so the armature has to already be on it.
    arm.rotation_euler = facing
    bpy_update()
    return facing


def _wall_clearance(phase, local):
    """Sole on the face through the plant and the push. Off the face while swinging."""
    if phase in ("swing", "takeoff"):
        t = _clamp(local / 0.40, 0.0, 1.0)
        # Stay clearly off, then meet the face as the plant starts.
        return 0.24 + (0.012 - 0.24) * (t * t)
    return 0.005


def _leg_min_x(side):
    xs = []
    for token in (f"UpperLeg_{side}", f"LowerLeg_{side}", f"Foot_{side}"):
        xs.extend(v.x for v in p5._verts(token))
    return min(xs) if xs else 1.0


def _place_drive_on_ground(arm, side, sole_x=0.52):
    """Put the drive sole on the ground, clear of the wall."""
    from mathutils import Vector
    _n, patch = _sole_patch(arm, side)
    if not patch:
        return 1.0
    center = sum(patch, Vector()) / len(patch)
    arm.location.x += sole_x - center.x
    arm.location.y += 0.0 - center.y
    arm.location.z += 0.006 - center.z
    bpy_update()
    _seat_sole(arm, side, (0.0, 0.0, 0.0), (0.0, 0.0, 1.0), clearance=0.006)
    gap = _min_plane((0.0, 0.0, 0.0), (1.0, 0.0, 0.0))
    if gap < 0.004:
        arm.location.x += 0.004 - gap
        bpy_update()
        _seat_sole(arm, side, (0.0, 0.0, 0.0), (0.0, 0.0, 1.0), clearance=0.006)
    return _sole_gap(arm, side, (0.0, 0.0, 0.0), (0.0, 0.0, 1.0))


def _hand_face_gap(arm, side):
    """Min X of the hand (metres off the wall) and the highest hand vertex."""
    vs = p5._verts(f"Hand_{side}")
    if not vs:
        return 1.0, 0.0
    return min(v.x for v in vs), max(v.z for v in vs)


def _place_wall_hip(arm, side, other, hip_target, clearance, hold_plant=False):
    """Put the hip on hip_target and the lead sole at clearance.

    During a swing the trail leg can push the sole further out. A plant or
    the last push keeps the sole where it was asked to sit; the trail leg
    is moved off the face instead.
    """
    from mathutils import Vector
    hip = p5._head_w(arm, "Hips")
    _shin, sole = _shin_world(arm, side)
    other_min = _leg_min_x(other)
    if not hold_plant:
        clearance = max(clearance, 0.012 + sole.x - other_min)
    foot_z = sole.z + (hip_target - hip.z)
    _place_sole(arm, side, foot_z, clearance=clearance)
    return clearance


def _rise_drive_hip(arm, side, min_hip):
    """Extend the drive leg until the hip reaches min_hip. The sole stays down."""
    from mathutils import Vector
    hip_z = p5._head_w(arm, "Hips").z
    if min_hip is None or hip_z >= min_hip - 1.0e-4:
        return hip_z
    for _step in range(8):
        th = Vector(_bone_world_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0)))
        sh = Vector(_bone_world_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0)))
        before = p5._head_w(arm, "Hips").z
        # More downward, with a little +X so the foot stays ahead of the hip.
        th2 = Vector((max(0.04, th.x * 0.70), th.y * 0.85, th.z - 0.16))
        sh2 = Vector((max(0.02, sh.x * 0.60), sh.y * 0.50, sh.z - 0.20))
        _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), th2)
        _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), sh2)
        _orient_sole(arm, side, (-1.0, 0.0, 0.12), (0.0, 0.0, -1.0))
        _place_drive_on_ground(arm, side, sole_x=0.78)
        hip_z = p5._head_w(arm, "Hips").z
        if hip_z >= min_hip - 1.0e-4 or hip_z < before + 0.004:
            return hip_z
    return p5._head_w(arm, "Hips").z


def _keep_foot_off_wall(arm, side, margin=0.08):
    """Pull a free foot out of the brick without moving the body."""
    from mathutils import Vector
    for _step in range(6):
        xs = [v.x for v in p5._verts(f"Foot_{side}")]
        xs += [v.x for v in p5._verts(f"LowerLeg_{side}")]
        if not xs or min(xs) >= margin:
            return
        th = Vector(_bone_world_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0)))
        sh = Vector(_bone_world_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0)))
        _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), Vector((th.x + 0.22, th.y, th.z)))
        _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), Vector((sh.x + 0.28, sh.y, sh.z)))
        _orient_sole(arm, side, (0.05, 0.0, 0.99), (0.90, 0.0, -0.15))


def _push_trail_off_wall(arm, lead, other, margin=0.04):
    """Keep the trail leg outside the lead sole so a plant is not unseated."""
    from mathutils import Vector
    for _step in range(8):
        _n, patch = _sole_patch(arm, lead)
        sole_x = min((v.x for v in patch), default=0.0) if patch else 0.0
        if _leg_min_x(other) >= sole_x + margin:
            return
        th = Vector(_bone_world_axis(arm, f"UpperLeg_{other}", (0.0, 1.0, 0.0)))
        sh = Vector(_bone_world_axis(arm, f"LowerLeg_{other}", (0.0, 1.0, 0.0)))
        # Knee stays up. The limb moves out to +X, away from the face.
        _aim_bone_axis(
            arm, f"UpperLeg_{other}", (0.0, 1.0, 0.0),
            Vector((th.x + 0.30, th.y, max(0.40, th.z))),
        )
        _aim_bone_axis(
            arm, f"LowerLeg_{other}", (0.0, 1.0, 0.0),
            Vector((sh.x + 0.24, sh.y, min(-0.30, sh.z))),
        )


def _slab_depth(v):
    """Metres inside the 4 m wall slab. Above the top, or past the face, is outside."""
    if v.z >= WALL_TOP_Z or v.z <= 0.0:
        return 0.0
    if v.x >= WALL_FACE_X or v.x <= WALL_BACK_X:
        return 0.0
    if abs(v.y) > WALL_Y_HALF:
        return 0.0
    return min(WALL_FACE_X - v.x, v.x - WALL_BACK_X, WALL_TOP_Z - v.z, v.z)


def _body_world_pen():
    worst = 0.0
    for v in _all_body_verts():
        worst = max(worst, max(0.0, -v.z), _slab_depth(v))
    return worst


def _orient_palm_down(arm, side):
    """Palm (hand local -Z) faces the top. Fingers run along the wall, tipped up."""
    sign = 1.0 if side == "L" else -1.0
    _orient_sole(arm, side, (0.08, 0.55 * sign, 0.22), (0.0, 0.0, 1.0), bone=f"Hand_{side}")


def _one_hand_on_lip(arm, side, ux, uz, fx, fz, lat):
    """One arm toward the wall-top edge. The other arm is not moved."""
    from mathutils import Vector
    sign = 1.0 if side == "L" else -1.0
    lateral = lat * sign
    _aim_bone_axis(arm, f"UpperArm_{side}", (0.0, 1.0, 0.0), Vector((ux, lateral, uz)))
    _aim_bone_axis(arm, f"LowerArm_{side}", (0.0, 1.0, 0.0), Vector((fx, lateral * 0.2, fz)))
    _orient_palm_down(arm, side)


def _palm_band(side):
    """Mean of the lowest quarter of the hand: the palm once it faces down."""
    vs = list(p5._verts(f"Hand_{side}"))
    if not vs:
        return 1.0, 0.0, 1.0
    ordered = sorted(vs, key=lambda v: v.z)
    band = ordered[: max(1, len(ordered) // 4)]
    z = sum(v.z for v in band) / len(band)
    x = sum(v.x for v in band) / len(band)
    return z, x, min(v.z for v in vs)


def _ik_hand_to(arm, side, tx, tz, elbow_up=1.0, lat=0.36):
    """Two-bone reach. The elbow stays wide of the chest and above the lip."""
    from mathutils import Vector
    shoulder = Vector(p5._head_w(arm, f"UpperArm_{side}"))
    L1 = arm.pose.bones[f"UpperArm_{side}"].length
    L2 = arm.pose.bones[f"LowerArm_{side}"].length
    sign = 1.0 if side == "L" else -1.0
    # Hands plant out at shoulder width. A palm on the centre line folds the
    # upper arm through the chest once the trunk leans into the brick.
    target = Vector((tx, shoulder.y + lat * sign, tz))
    delta = Vector((target.x - shoulder.x, 0.0, target.z - shoulder.z))
    d = delta.length
    reach = max(abs(L1 - L2) + 0.02, min(d, L1 + L2 - 0.02))
    toward = delta.normalized() if delta.length > 1.0e-6 else Vector((-1.0, 0.0, 0.2))
    if d > 1.0e-6 and abs(d - reach) > 1.0e-4:
        target = shoulder + toward * reach
        target.y = shoulder.y + lat * sign
        delta = Vector((target.x - shoulder.x, 0.0, target.z - shoulder.z))
        d = max(delta.length, 1.0e-6)
        toward = delta.normalized()
    perp = Vector((-toward.z, 0.0, toward.x))
    if perp.z < 0.0:
        perp = -perp
    cos_a = (L1 * L1 + d * d - L2 * L2) / max(1.0e-6, 2.0 * L1 * d)
    cos_a = max(-1.0, min(1.0, cos_a))
    sin_a = math.sqrt(max(0.0, 1.0 - cos_a * cos_a)) * elbow_up
    elbow_dir = toward * cos_a + perp * sin_a
    if elbow_dir.length < 1.0e-6:
        elbow_dir = toward
    elbow = shoulder + elbow_dir.normalized() * L1
    fore = target - elbow
    side_off = Vector((0.0, lat * sign, 0.0))
    _aim_bone_axis(arm, f"UpperArm_{side}", (0.0, 1.0, 0.0), elbow_dir + side_off)
    _aim_bone_axis(arm, f"LowerArm_{side}", (0.0, 1.0, 0.0), fore + side_off * 0.35)
    _orient_palm_down(arm, side)


def _arm_chest_depth(arm):
    """How far an arm sits inside the chest. Rig-joint nests are ignored."""
    worst = 0.0
    for depth, pair in _pair_depths(arm):
        if depth <= DEPTH_LIMIT_M or _is_rig_pair(pair):
            continue
        if "Chest" in pair and ("Arm" in pair or "Hand" in pair):
            worst = max(worst, depth)
    return worst


def _seat_one_palm(arm, side):
    """Put this palm on the wall top. The other arm stays as it is.

    Wider plants are preferred when a narrow one drives the upper arm
    through the chest. The hip is not moved.
    """
    base = _snap_rots(arm)
    loc0 = arm.location.copy()
    rot0 = arm.rotation_euler.copy()
    best = None
    for lat in (0.10, 0.26, 0.42):
        tx, tz = -0.02, 4.04
        for _step in range(8):
            _restore_rots(arm, base)
            arm.location = loc0
            arm.rotation_euler = rot0
            bpy_update()
            _ik_hand_to(arm, side, tx, tz, elbow_up=1.15, lat=lat)
            z, x, zmin = _palm_band(side)
            pen = _body_world_pen()
            gap = zmin - WALL_TOP_Z
            on = (
                0.002 <= gap <= 0.010
                and -0.055 <= x <= 0.015
                and pen <= 0.004
            )
            err = abs(gap - 0.006) + abs(x + 0.015)
            # Chest depth is measured only on a candidate worth keeping.
            # A miss does not need the mesh test.
            if on or best is None:
                chest = _arm_chest_depth(arm) if on else 1.0
                score = (0 if on else 1, 0 if chest <= 0.005 else 1, round(chest, 4), round(pen, 4), err)
                if best is None or score < best[0]:
                    best = (
                        score, _snap_rots(arm), arm.location.copy(),
                        arm.rotation_euler.copy(), gap, x, pen,
                    )
            if on and _arm_chest_depth(arm) <= 0.005:
                best = (
                    (0, 0, 0.0, round(pen, 4), err),
                    _snap_rots(arm), arm.location.copy(), arm.rotation_euler.copy(),
                    gap, x, pen,
                )
                break
            if pen > 0.004 or gap < 0.0:
                tz += 0.012
                tx += 0.01
            else:
                tz += (WALL_TOP_Z + 0.006) - zmin
                tx += -0.015 - x
            tz = _clamp(tz, 3.92, 4.25)
            tx = _clamp(tx, -0.07, 0.05)
        if best is not None and best[0][0] == 0 and best[0][1] == 0:
            break
    _score, snap, loc, rot, gap, x, pen = best
    _restore_rots(arm, snap)
    arm.location = loc
    arm.rotation_euler = rot
    bpy_update()
    _z, x, zmin = _palm_band(side)
    return zmin - WALL_TOP_Z, x, _body_world_pen(), (
        0.002 <= (zmin - WALL_TOP_Z) <= 0.010
        and -0.055 <= x <= 0.015
        and _body_world_pen() <= 0.004
    )


def _place_palms_on_top(arm):
    """Search each arm until that palm sits on the wall top, within 1 cm.

    The feet and the hip stay where the caller put them. The hands are
    solved separately because the two shoulders are not at the same height
    once the chest leans into the brick.
    """
    gap_l, xl, pen_l, on_l = _seat_one_palm(arm, "L")
    gap_r, xr, pen_r, on_r = _seat_one_palm(arm, "R")
    pen = max(pen_l, pen_r, _body_world_pen())
    print(
        f"  catch palms L={gap_l*100:.2f}cm x={xl:.3f} R={gap_r*100:.2f}cm x={xr:.3f} "
        f"pen={pen*100:.2f}cm on={on_l and on_r}",
        flush=True,
    )
    return gap_l, gap_r, xl, xr


def _stand_legs(arm):
    """Both legs straight, soles aimed down onto the wall top."""
    for side, lat in (("L", 0.10), ("R", -0.10)):
        _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), (0.02, lat, -1.0))
        _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), (0.0, lat * 0.25, -1.0))
        _orient_sole(arm, side, (-1.0, 0.0, 0.0), (0.0, 0.0, -1.0))


def _hip_above_soles(arm):
    """How far the hip sits above the lower sole. Translation does not change it."""
    from mathutils import Vector
    hip = Vector(p5._head_w(arm, "Hips"))
    low = None
    for side in ("L", "R"):
        _n, patch = _sole_patch(arm, side)
        if not patch:
            continue
        z = min(v.z for v in patch)
        if low is None or z < low:
            low = z
    if low is None:
        return 0.95
    return hip.z - low


def _drive_mantle_knee(arm, side):
    """Near knee up toward the chest, foot tucked under it on the near side."""
    sign = 1.0 if side == "L" else -1.0
    _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), (-0.08, 0.22 * sign, 0.97))
    _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), (0.28, 0.10 * sign, -0.88))
    _orient_sole(arm, side, (0.10, 0.0, 0.96), (0.75, 0.0, -0.20))


def _hang_mantle_leg(arm, side, off_edge=False):
    """Other leg bent under the body, or dropped off the lip. Not an arabesque."""
    sign = 1.0 if side == "L" else -1.0
    if off_edge:
        # Hip is over the slab. The thigh drops off the near edge and the
        # shin folds so the foot hangs below the lip, in the open.
        _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), (0.42, 0.28 * sign, -0.82))
        _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), (0.05, 0.58 * sign, -0.62))
        _orient_sole(arm, side, (0.10, 0.0, -0.20), (0.25, 0.0, -0.92))
        return
    # Still in front of the wall. Knee tucks up under the hip. A thigh aimed
    # back along +X is the horizontal trail the mantle must not have.
    _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), (0.06, 0.34 * sign, -0.92))
    _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), (0.12, 0.42 * sign, 0.72))
    _orient_sole(arm, side, (0.05, 0.0, 0.85), (0.55, 0.0, -0.35))


def _keep_leg_bent_clear(arm, side, margin=0.03):
    """Nudge a leg out of the brick without locking the knee straight."""
    from mathutils import Vector
    for _step in range(5):
        xs = []
        for token in (f"UpperLeg_{side}", f"LowerLeg_{side}", f"Foot_{side}"):
            xs.extend(v.x for v in p5._verts(token))
        if not xs or min(xs) >= margin:
            return
        th = Vector(_bone_world_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0)))
        sh = Vector(_bone_world_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0)))
        _aim_bone_axis(
            arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0),
            Vector((th.x + 0.16, th.y, th.z)),
        )
        _aim_bone_axis(
            arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0),
            Vector((sh.x + 0.10, sh.y, sh.z)),
        )


def _hook_knee(arm, side, knee_z=None):
    """Lay one knee on the wall top. The foot stays above the slab."""
    from mathutils import Vector
    sign = 1.0 if side == "L" else -1.0
    hip = Vector(p5._head_w(arm, f"UpperLeg_{side}"))
    if knee_z is None:
        knee_z = WALL_TOP_Z + 0.012
    knee_at = Vector((
        KNEE_LIP_X,
        0.08 * sign,
        knee_z,
    ))
    aim = knee_at - hip
    if aim.length < 1.0e-4:
        aim = Vector((-0.2, sign, -0.3))
    _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), aim)
    # Shin up and out so the foot does not drop through the face.
    # Foot above the slab, on the near side. A shin that hangs down
    # puts the shoe through the top corner.
    _aim_bone_axis(
        arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0),
        Vector((0.70, 0.30 * sign, 0.55)),
    )
    _orient_sole(arm, side, (0.2, 0.0, 0.9), (0.55, 0.0, -0.15))


def _arms_wide_of_chest(arm):
    """Both arms out and back of the ribs. Used once the palms can no longer reach."""
    from mathutils import Vector
    for side, lat in (("L", 0.70), ("R", -0.70)):
        _aim_bone_axis(arm, f"UpperArm_{side}", (0.0, 1.0, 0.0), Vector((0.55, lat, 0.05)))
        _aim_bone_axis(arm, f"LowerArm_{side}", (0.0, 1.0, 0.0), Vector((0.35, lat * 0.55, -0.70)))
    for _step in range(3):
        if _arm_chest_depth(arm) <= 0.004 and _body_world_pen() <= 0.004:
            return
        for side, lat in (("L", 0.90 + 0.15 * _step), ("R", -(0.90 + 0.15 * _step))):
            _aim_bone_axis(arm, f"UpperArm_{side}", (0.0, 1.0, 0.0), Vector((0.40, lat, 0.20)))
            _aim_bone_axis(arm, f"LowerArm_{side}", (0.0, 1.0, 0.0), Vector((0.20, lat * 0.50, -0.55)))


def _reel_hip_toward_rail(arm, side, other, hip_z):
    """Bring a push hip back to the rail without lifting the sole off the face.

    The push used to shove the pelvis about 11 cm out past the rail, which
    is the lateral squiggle. Plants stay where the 90° knee puts them.
    Each step is kept only when the sole is still on the face and the body
    is clear, so a failed step does not throw away the ones that worked.
    """
    from mathutils import Vector
    origin = float(p5._head_w(arm, "Hips").x)
    limit = HIP_RAIL_X + 0.03
    if origin <= limit + 0.005:
        return
    wall_o = Vector((0.0, 0.0, 0.0))
    normal = Vector((1.0, 0.0, 0.0))
    x = origin
    snap = _snap_rots(arm)
    loc = arm.location.copy()
    rot = arm.rotation_euler.copy()
    while x > limit + 0.005:
        trial = max(limit, x - 0.02)
        held_snap = _snap_rots(arm)
        held_loc = arm.location.copy()
        held_rot = arm.rotation_euler.copy()
        _place_hip_at(arm, trial, 0.0, hip_z)
        for _step in range(6):
            gap = _sole_gap(arm, side, wall_o, normal)
            if 0.003 <= gap <= 0.011:
                break
            th = Vector(_bone_world_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0)))
            sh = Vector(_bone_world_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0)))
            if gap > 0.011:
                th = Vector((th.x - 0.12, th.y, th.z))
                sh = Vector((sh.x - 0.16, sh.y, sh.z))
            else:
                th = Vector((th.x + 0.14, th.y, th.z))
                sh = Vector((sh.x + 0.18, sh.y, sh.z))
            _aim_bone_axis(arm, f"UpperLeg_{side}", (0.0, 1.0, 0.0), th)
            _aim_bone_axis(arm, f"LowerLeg_{side}", (0.0, 1.0, 0.0), sh)
            _orient_sole_on_wall(arm, side)
            _place_hip_at(arm, trial, 0.0, hip_z)
        _push_trail_off_wall(arm, side, other)
        _keep_foot_off_wall(arm, other, margin=0.06)
        _place_hip_at(arm, trial, 0.0, hip_z)
        gap = _sole_gap(arm, side, wall_o, normal)
        if 0.003 <= gap <= 0.012 and _body_world_pen() <= 0.004:
            x = trial
            snap = _snap_rots(arm)
            loc = arm.location.copy()
            rot = arm.rotation_euler.copy()
            continue
        _restore_rots(arm, held_snap)
        arm.location = held_loc
        arm.rotation_euler = held_rot
        bpy_update()
        break
    _restore_rots(arm, snap)
    arm.location = loc
    arm.rotation_euler = rot
    bpy_update()


def _knee_on_slab(arm, side):
    """Lowest vertex near the knee, and its x. None if the leg is missing."""
    from mathutils import Vector
    knee = Vector(p5._head_w(arm, f"LowerLeg_{side}"))
    near = []
    for token in (f"UpperLeg_{side}", f"LowerLeg_{side}"):
        for v in p5._verts(token):
            if (Vector(v) - knee).length <= 0.09:
                near.append(v)
    if not near:
        return None, None
    lowest = min(near, key=lambda v: v.z)
    return lowest.z, lowest.x


def solve_wallrun(arm, samples, times):
    """Face the brick and take three alternating steps up it."""
    from mathutils import Vector
    face = 0.0
    normal = Vector((1.0, 0.0, 0.0))
    wall_o = Vector((face, 0.0, 0.0))
    # Nose (head bone +Z) points at the wall at yaw -90. The opposite yaw points
    # the chest away and the step misses the face.
    n = len(samples)
    plants = []
    keys, caps, facings, rows = [], [], [], []
    hip_path = []
    pen = 0.0
    prev_hip = None
    for i, ch in enumerate(samples):
        show_wall_world(face, WALL_TOP_Z * 0.5, WALL_TOP_Z, hide_ground=False, ticks=True, half_thick=WALL_HALF_THICK)
        step, local, side, phase = _wall_cycle(i, n, 3)
        if step == 0 and local < 0.34:
            phase = "takeoff"
        other = "R" if side == "L" else "L"
        plants.append(side)
        both_up = step == 2 and phase == "push"
        on_ground = phase == "takeoff" and local < 0.16
        hip_target = _wall_hip_z(i, n)
        if both_up:
            hip_target += 0.32 * _clamp((local - 0.72) / 0.28, 0.0, 1.0)
        # One line for the whole climb. Swing used to walk the body out and
        # back; that was the squiggle. The mantle, after this loop, is the
        # one move onto the slab.
        rail_x = HIP_RAIL_X
        if both_up:
            lean = 10.0
        elif phase == "takeoff":
            lean = 6.0 + 2.0 * _clamp(local / 0.34, 0.0, 1.0)
        else:
            lean = 8.0
        facing = _pose_wall_step(
            arm, ch, side, phase, local, lean, reach_top=False,
        )
        _place_hip_at(arm, rail_x, 0.0, hip_target)
        fore_x = fore_r = -0.15
        upper_x = upper_r = -0.40
        up_z = 0.30
        if both_up:
            for _try in range(12):
                _arms_reach_up(arm, fore_x, fore_r, upper_x, upper_r, up_z)
                _orient_sole_on_wall(arm, side)
                _push_trail_off_wall(arm, side, other)
                _place_wall_hip(arm, side, other, hip_target, 0.005, hold_plant=True)
                gap_l, zl = _hand_face_gap(arm, "L")
                gap_r, zr = _hand_face_gap(arm, "R")
                hand_z = max(zl, zr)
                if (
                    0.045 <= gap_l <= 0.12 and 0.045 <= gap_r <= 0.12
                    and 4.05 <= hand_z <= 4.40
                ):
                    break
                if gap_l > 0.12:
                    fore_x = max(-0.70, fore_x - 0.08)
                    upper_x = max(-0.80, upper_x - 0.06)
                elif gap_l < 0.045:
                    fore_x = min(0.25, fore_x + 0.10)
                    upper_x = min(-0.12, upper_x + 0.10)
                if gap_r > 0.12:
                    fore_r = max(-0.70, fore_r - 0.08)
                    upper_r = max(-0.80, upper_r - 0.06)
                elif gap_r < 0.045:
                    fore_r = min(0.25, fore_r + 0.10)
                    upper_r = min(-0.12, upper_r + 0.10)
                if hand_z > 4.40:
                    up_z = max(0.05, up_z - 0.05)
                elif hand_z < 4.05:
                    up_z = min(0.70, up_z + 0.05)
            _orient_sole_on_wall(arm, side)
            _push_trail_off_wall(arm, side, other)
            _place_wall_hip(arm, side, other, hip_target, 0.005, hold_plant=True)
            hip = p5._head_w(arm, "Hips")
            arm.location.y -= hip.y
            bpy_update()
            _reel_hip_toward_rail(arm, side, other, p5._head_w(arm, "Hips").z)
        elif phase in ("plant", "push"):
            # Keep the 90° plant. Seating the sole sets the hip x; the swing
            # frames are the ones locked to the rail so the path does not zigzag.
            _arms_counter(arm, side, local, False, brush=(phase == "plant"))
            _orient_sole_on_wall(arm, side)
            _push_trail_off_wall(arm, side, other)
            _place_wall_hip(arm, side, other, hip_target, 0.005, hold_plant=True)
            _arms_counter(arm, side, local, False, brush=(phase == "plant"))
            hip = p5._head_w(arm, "Hips")
            arm.location.y -= hip.y
            bpy_update()
            if phase == "push":
                _reel_hip_toward_rail(arm, side, other, p5._head_w(arm, "Hips").z)
        elif on_ground:
            _arms_counter(arm, side, local, False, brush=False)
            _orient_sole(arm, other, (-1.0, 0.0, 0.12), (0.0, 0.0, -1.0))
            _place_drive_on_ground(arm, other, sole_x=0.78)
            _rise_drive_hip(arm, other, None if prev_hip is None else prev_hip + 0.010)
            _keep_foot_off_wall(arm, side, margin=0.08)
            _orient_sole(arm, other, (-1.0, 0.0, 0.12), (0.0, 0.0, -1.0))
            _place_drive_on_ground(arm, other, sole_x=0.78)
            for kick in range(6):
                foot_vs = p5._verts(f"Foot_{other}")
                low = min((v.z for v in foot_vs), default=1.0)
                if low >= 0.002:
                    break
                _orient_sole(
                    arm, other,
                    (-0.80, 0.0, 0.22 + 0.10 * kick),
                    (0.10, 0.0, -0.99),
                )
                _seat_sole(arm, other, (0.0, 0.0, 0.0), (0.0, 0.0, 1.0), clearance=0.006)
            hip = p5._head_w(arm, "Hips")
            dx = HIP_RAIL_X - hip.x
            foot_vs = p5._verts(f"Foot_{other}")
            foot_min = min((v.x for v in foot_vs), default=1.0)
            # A closer rail must not drag the drive sole into the brick.
            if foot_min + dx < 0.04:
                dx = 0.04 - foot_min
            arm.location.x += dx
            arm.location.y -= hip.y
            bpy_update()
            _seat_sole(arm, other, (0.0, 0.0, 0.0), (0.0, 0.0, 1.0), clearance=0.006)
        else:
            _place_hip_at(arm, rail_x, 0.0, hip_target)
            _arms_counter(arm, side, local, False, brush=False)
            _keep_foot_off_wall(arm, side, margin=0.10)
            _keep_foot_off_wall(arm, other, margin=0.06)
        for _fix in range(4):
            if _body_world_pen() <= 0.004:
                break
            _push_trail_off_wall(arm, side, other)
            _keep_foot_off_wall(arm, side, margin=0.06)
            _keep_foot_off_wall(arm, other, margin=0.06)
        used = _body_world_pen()
        pen = max(pen, used)
        lean = _signed_trunk_deg(arm)
        body = _measure_body(arm)
        _n, patch = _sole_patch(arm, side)
        if patch:
            body["pelvis"] = body["hip_z"] - min(v.z for v in patch)
        row = _angle_row(ch, body)
        row["t"] = times[i]
        row["plant"] = side
        row["phase"] = phase
        row["step"] = step
        shin, sole_c = _shin_world(arm, side)
        row["hip_z"] = body["hip_z"]
        hip_now_early = p5._head_w(arm, "Hips")
        row["hip_x"] = float(hip_now_early.x)
        row["hip_y"] = float(hip_now_early.y)
        row["shin_deg"] = _shin_wall_deg(shin)
        row["foot_below_hip"] = body["hip_z"] - sole_c.z
        row["sole_L_cm"] = _sole_gap(arm, "L", wall_o, normal) * 100.0
        row["sole_R_cm"] = _sole_gap(arm, "R", wall_o, normal) * 100.0
        ground_n = Vector((0.0, 0.0, 1.0))
        ground_o = Vector((0.0, 0.0, 0.0))
        row["ground_L_cm"] = _sole_gap(arm, "L", ground_o, ground_n) * 100.0
        row["ground_R_cm"] = _sole_gap(arm, "R", ground_o, ground_n) * 100.0
        hl, zl = _hand_face_gap(arm, "L")
        hr, zr = _hand_face_gap(arm, "R")
        row["handL_cm"] = hl * 100.0
        row["handR_cm"] = hr * 100.0
        row["hand_z"] = max(zl, zr)
        row["foot_z"] = sole_c.z
        row["clear_cm"] = used * 100.0
        row["lean"] = lean
        face_z = _bone_world_axis(arm, "Head", (0.0, 0.0, 1.0))
        row["face_x"] = float(face_z.x)
        row["face_y"] = float(face_z.y)
        row["knee_y_L"] = float(p5._head_w(arm, "LowerLeg_L").y)
        row["knee_y_R"] = float(p5._head_w(arm, "LowerLeg_R").y)
        def _elbow_deg(side_name):
            upper_a = _bone_world_axis(arm, f"UpperArm_{side_name}", (0.0, 1.0, 0.0))
            fore_a = _bone_world_axis(arm, f"LowerArm_{side_name}", (0.0, 1.0, 0.0))
            dot = max(-1.0, min(1.0, upper_a.dot(fore_a)))
            return math.degrees(math.acos(dot))
        row["elbow_L"] = _elbow_deg("L")
        row["elbow_R"] = _elbow_deg("R")
        dipped = prev_hip is not None and body["hip_z"] < prev_hip - 1.0e-4
        prev_hip = body["hip_z"]
        hip_now = p5._head_w(arm, "Hips")
        hip_path.append((float(hip_now.x), float(hip_now.y), float(hip_now.z)))
        rows.append(row)
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
        on = phase != "swing"
        lead_cm = row["sole_L_cm"] if side == "L" else row["sole_R_cm"]
        trail_cm = row["sole_R_cm"] if side == "L" else row["sole_L_cm"]
        bad_sole = (on and lead_cm > 1.05) or ((not on) and lead_cm <= 1.05) or trail_cm <= 1.05
        if bad_sole or dipped or phase == "plant" or i % 4 == 0 or i == n - 1:
            chest_x = min((v.x for v in p5._verts("Chest")), default=0.0)
            deep_name, deep_x = "", 1.0
            for token in (
                "Hips", "Spine", "Chest", "Head",
                "UpperLeg_L", "LowerLeg_L", "Foot_L",
                "UpperLeg_R", "LowerLeg_R", "Foot_R",
                "UpperArm_L", "LowerArm_L", "Hand_L",
                "UpperArm_R", "LowerArm_R", "Hand_R",
            ):
                xs = [v.x for v in p5._verts(token)]
                if xs and min(xs) < deep_x:
                    deep_x = min(xs)
                    deep_name = token
            print(
                f"  wall f={i} step={step} {phase} plant={side} "
                f"soleL={row['sole_L_cm']:.2f} soleR={row['sole_R_cm']:.2f} "
                f"hipX={row['hip_x']:.3f} hipY={row['hip_y']:.3f} hipZ={body['hip_z']:.3f} "
                f"footZ={sole_c.z:.3f} "
                f"chestX={chest_x:.3f} shinWall={row['shin_deg']:.0f} "
                f"below={row['foot_below_hip']:.2f} "
                f"nose=({face_z.x:.2f},{face_z.y:.2f},{face_z.z:.2f}) "
                f"kneeY={row['knee_y_L']:.2f}/{row['knee_y_R']:.2f} "
                f"knee={body['knee_L']:.0f}/{body['knee_R']:.0f} "
                f"elb={row['elbow_L']:.0f}/{row['elbow_R']:.0f} "
                f"gnd={row['ground_L_cm']:.1f}/{row['ground_R_cm']:.1f} "
                f"hand={row['handL_cm']:.1f}/{row['handR_cm']:.1f} z={row['hand_z']:.2f} "
                f"lean={lean:+.1f} clear={used*100:.1f} "
                f"spine={body['spine']:.0f} deep={deep_name}:{deep_x*100:.1f}cm"
                f"{' DIP' if dipped else ''}",
                flush=True,
            )
    # Mantle after the reach. Same wall-run verb: palms plant, the chest
    # comes over the lip, one knee lands on the slab, then stand. The hip
    # moves one way, onto the top, and keeps rising.
    from mathutils import Vector
    n_climb = len(keys)
    t_climb_end = times[-1]
    last_ch = samples[-1]
    last_hip = rows[-1]["hip_z"]
    last_x = rows[-1].get("hip_x", HIP_RAIL_X)
    side = plants[-1] if plants else "L"
    other = "R" if side == "L" else "L"
    _stand_legs(arm)
    stand_z = WALL_TOP_Z + 0.010 + _hip_above_soles(arm)
    z_palms = max(last_hip + 0.06, 3.55)
    # Rise above the lip before the hips cross the face, so the pelvis does
    # not bury itself in the top corner.
    z_press = min(3.84, max(z_palms + 0.08, 3.72))
    z_knee = max(z_press + 0.36, 4.28)
    z_stand = max(z_knee + 0.20, stand_z)
    # Press stays in the band where the arms can still plant. The knee
    # phase is what carries the chest past the lip.
    segments = (
        ("palms", 4, last_x, 0.55, last_hip, z_palms, 10.0),
        ("press", 4, 0.55, 0.36, z_palms, z_press, 10.0),
        ("knee", 4, 0.36, -0.06, z_press, z_knee, 8.0),
        ("stand", 4, -0.06, ROOF_STAND_X, z_knee, z_stand, 6.0),
    )
    mantle_i = 0
    for phase, count, x0, x1, z0, z1, lean_tgt in segments:
        for j in range(1, count + 1):
            mantle_i += 1
            t = j / float(count)
            hx = x0 + (x1 - x0) * t
            hz = z0 + (z1 - z0) * t
            if prev_hip is not None:
                hz = max(hz, prev_hip + 0.012)
            if phase == "stand":
                hz = max(hz, z_stand)
            show_wall_world(
                face, WALL_TOP_Z * 0.5, WALL_TOP_Z,
                hide_ground=False, ticks=True, half_thick=WALL_HALF_THICK,
            )
            facing = _pose_wall_step(
                arm, last_ch, side, "push", 1.0, lean_tgt, reach_top=False,
            )
            _place_hip_at(arm, hx, 0.0, hz)
            gap_l = gap_r = xl = xr = 0.0
            if phase in ("palms", "press"):
                # Near knee drives up toward the chest. The other leg hangs
                # bent below the lip. Both palms stay on the slab.
                _drive_mantle_knee(arm, side)
                _hang_mantle_leg(arm, other)
                _place_hip_at(arm, hx, 0.0, hz)
                _keep_leg_bent_clear(arm, side, margin=0.04)
                _keep_leg_bent_clear(arm, other, margin=0.04)
                _place_hip_at(arm, hx, 0.0, hz)
                gap_l, gap_r, xl, xr = _place_palms_on_top(arm)
                _place_hip_at(arm, hx, 0.0, hz)
            elif phase == "knee":
                # One knee on the slab, chest past the lip. The shoulders are
                # too high to keep the palms down, so the arms stay wide
                # instead of folding back through the chest. Hooking while the
                # hips are still in front of the lip drives the thigh through
                # the top corner, so those frames only rise, knee still up.
                knee_z = WALL_TOP_Z + 0.012
                ready = hz >= 4.24 and hx <= 0.08
                if not ready:
                    hz = max(hz, (prev_hip or hz) + 0.14)
                    hx = max(hx, 0.16)
                    _drive_mantle_knee(arm, side)
                    _hang_mantle_leg(arm, other)
                    _place_hip_at(arm, hx, 0.0, hz)
                    _keep_leg_bent_clear(arm, side, margin=0.05)
                    _keep_leg_bent_clear(arm, other, margin=0.05)
                    _place_hip_at(arm, hx, 0.0, hz)
                    _arms_wide_of_chest(arm)
                    _place_hip_at(arm, hx, 0.0, hz)
                else:
                    _hook_knee(arm, side, knee_z=knee_z)
                    _hang_mantle_leg(arm, other, off_edge=True)
                    _place_hip_at(arm, hx, 0.0, hz)
                    for _nudge in range(6):
                        kz, kx = _knee_on_slab(arm, side)
                        if kz is None:
                            break
                        on_knee = (
                            4.002 <= kz <= 4.020
                            and -0.22 <= kx <= -0.004
                        )
                        if on_knee:
                            break
                        if kz < 4.002:
                            hz += min(0.03, 4.012 - kz)
                        elif kz > 4.020:
                            knee_z = max(WALL_TOP_Z + 0.004, knee_z - 0.008)
                        if kx is not None and kx > -0.004:
                            hx -= 0.015
                        elif kx is not None and kx < -0.22:
                            hx += 0.015
                        hx = _clamp(hx, -0.10, 0.22)
                        hz = max(hz, (prev_hip or hz) + 0.012)
                        _place_hip_at(arm, hx, 0.0, hz)
                        _hook_knee(arm, side, knee_z=knee_z)
                        _hang_mantle_leg(arm, other, off_edge=True)
                    for _over in range(12):
                        over_now, _cx, _cz = _chest_over_edge(arm)
                        if over_now and _body_world_pen() <= 0.004:
                            break
                        hx = max(-0.10, hx - 0.025)
                        hz = max(hz + 0.018, (prev_hip or hz) + 0.012)
                        _place_hip_at(arm, hx, 0.0, hz)
                        _hook_knee(arm, side, knee_z=knee_z)
                        _hang_mantle_leg(arm, other, off_edge=True)
                        _keep_leg_bent_clear(arm, other, margin=0.04)
                        if _body_world_pen() > 0.004:
                            hz += 0.03
                            _place_hip_at(arm, hx, 0.0, hz)
                            _hook_knee(arm, side, knee_z=knee_z)
                            _hang_mantle_leg(arm, other, off_edge=True)
                            if _body_world_pen() > 0.004:
                                hx = min(0.16, hx + 0.025)
                                _place_hip_at(arm, hx, 0.0, hz)
                                _hook_knee(arm, side, knee_z=knee_z)
                                _hang_mantle_leg(arm, other, off_edge=True)
                                break
                    _arms_wide_of_chest(arm)
                    _place_hip_at(arm, hx, 0.0, hz)
                    _hang_mantle_leg(arm, other, off_edge=True)
            else:
                _stand_legs(arm)
                hz = WALL_TOP_Z + 0.010 + _hip_above_soles(arm)
                hz = max(hz, prev_hip + 0.01)
                _place_hip_at(arm, hx, 0.0, hz)
                for s, lat in (("L", 0.62), ("R", -0.62)):
                    _aim_bone_axis(arm, f"UpperArm_{s}", (0.0, 1.0, 0.0), (0.10, lat, -0.45))
                    _aim_bone_axis(arm, f"LowerArm_{s}", (0.0, 1.0, 0.0), (0.05, lat * 0.45, -0.85))
            lean = _signed_trunk_deg(arm)
            over, chest_cx, chest_cz = _chest_over_edge(arm)
            pen = max(pen, _body_world_pen())
            body = _measure_body(arm)
            _n, patch = _sole_patch(arm, side)
            if patch:
                body["pelvis"] = body["hip_z"] - min(v.z for v in patch)
            row = _angle_row(last_ch, body)
            row["t"] = t_climb_end + mantle_i / FPS
            row["plant"] = side
            row["phase"] = phase
            row["step"] = 2
            shin, sole_c = _shin_world(arm, side)
            hip_now = p5._head_w(arm, "Hips")
            row["hip_z"] = body["hip_z"]
            row["hip_x"] = float(hip_now.x)
            row["hip_y"] = float(hip_now.y)
            row["shin_deg"] = _shin_wall_deg(shin)
            row["foot_below_hip"] = body["hip_z"] - sole_c.z
            row["sole_L_cm"] = _sole_gap(arm, "L", wall_o, normal) * 100.0
            row["sole_R_cm"] = _sole_gap(arm, "R", wall_o, normal) * 100.0
            ground_n = Vector((0.0, 0.0, 1.0))
            ground_o = Vector((0.0, 0.0, 0.0))
            row["ground_L_cm"] = _sole_gap(arm, "L", ground_o, ground_n) * 100.0
            row["ground_R_cm"] = _sole_gap(arm, "R", ground_o, ground_n) * 100.0
            hl, zl = _hand_face_gap(arm, "L")
            hr, zr = _hand_face_gap(arm, "R")
            row["handL_cm"] = hl * 100.0
            row["handR_cm"] = hr * 100.0
            row["hand_z"] = max(zl, zr)
            row["palmL_cm"] = gap_l * 100.0
            row["palmR_cm"] = gap_r * 100.0
            row["palm_x"] = (xl + xr) * 0.5
            row["chest_over"] = over
            row["chest_cx"] = chest_cx
            row["chest_cz"] = chest_cz
            kz, kx = _knee_on_slab(arm, side)
            if kz is not None:
                row["knee_z"] = kz
                row["knee_x"] = kx
            row["foot_z"] = sole_c.z
            row["clear_cm"] = _body_world_pen() * 100.0
            row["lean"] = lean
            face_z = _bone_world_axis(arm, "Head", (0.0, 0.0, 1.0))
            row["face_x"] = float(face_z.x)
            row["face_y"] = float(face_z.y)
            row["knee_y_L"] = float(p5._head_w(arm, "LowerLeg_L").y)
            row["knee_y_R"] = float(p5._head_w(arm, "LowerLeg_R").y)

            def _elbow_deg(side_name):
                upper_a = _bone_world_axis(arm, f"UpperArm_{side_name}", (0.0, 1.0, 0.0))
                fore_a = _bone_world_axis(arm, f"LowerArm_{side_name}", (0.0, 1.0, 0.0))
                dot = max(-1.0, min(1.0, upper_a.dot(fore_a)))
                return math.degrees(math.acos(dot))

            row["elbow_L"] = _elbow_deg("L")
            row["elbow_R"] = _elbow_deg("R")
            dipped = body["hip_z"] < prev_hip - 1.0e-4
            prev_hip = body["hip_z"]
            hip_path.append((float(hip_now.x), float(hip_now.y), float(hip_now.z)))
            rows.append(row)
            keys.append(_save_pose(arm))
            caps.append((arm.location.x, arm.location.y, arm.location.z))
            facings.append((facing.x, facing.y, facing.z))
            times.append(row["t"])
            print(
                f"  wall f={n_climb + mantle_i - 1} {phase} "
                f"hip=({row['hip_x']:.3f},{row['hip_y']:.3f},{body['hip_z']:.3f}) "
                f"lean={lean:+.1f} chest=({chest_cx:.3f},{chest_cz:.3f}) over={over} "
                f"kneeZ={kz if kz is not None else -1:.3f} "
                f"palm={gap_l*100:.2f}/{gap_r*100:.2f}cm "
                f"pen={_body_world_pen()*100:.2f}"
                f"{' DIP' if dipped else ''}",
                flush=True,
            )
    hips = [r["hip_z"] for r in rows]
    dips = [k for k in range(1, len(hips)) if hips[k] < hips[k - 1] - 1.0e-4]
    climb_x = [p[0] for p in hip_path[:n_climb]]
    climb_y = [p[1] for p in hip_path[:n_climb]]
    dev = 0.0
    if climb_x:
        dev = max(
            max(abs(x - HIP_RAIL_X) for x in climb_x),
            max(abs(y) for y in climb_y),
        )
    print(
        f"  wall hip {hips[0]:.3f}->{hips[-1]:.3f} dips={dips} "
        f"climbWobble={dev*100:.1f}cm",
        flush=True,
    )

    def _first(pred):
        for idx, r in enumerate(rows):
            if pred(r):
                return idx
        return 0

    def _last(pred):
        found = 0
        for idx, r in enumerate(rows):
            if pred(r):
                found = idx
        return found

    sheet = [
        0,
        _first(lambda r: r["phase"] == "plant" and r.get("step") == 1),
        _first(lambda r: r["phase"] == "plant" and r.get("step") == 2),
        n_climb - 1,
        _last(lambda r: r["phase"] == "palms"),
        _last(lambda r: r["phase"] == "press"),
        _last(lambda r: r["phase"] == "knee"),
        len(rows) - 1,
    ]
    sheet_lines = []
    for s, idx in enumerate(sheet):
        r = rows[idx]
        lead_sole = r.get("sole_L_cm", 0.0) if r.get("plant") == "L" else r.get("sole_R_cm", 0.0)
        sheet_lines.append(
            f"  cell {s + 1} f={idx} t={r['t']:.2f}s phase={r['phase']} "
            f"lean={r['lean']:+.1f} hip=({r.get('hip_x', 0):.2f},"
            f"{r.get('hip_y', 0):.2f},{r['hip_z']:.2f}) "
            f"shinWall={r.get('shin_deg', 0):.0f} sole={lead_sole:.2f} "
            f"nose=({r.get('face_x', 0):.2f},{r.get('face_y', 0):.2f}) "
            f"kneeY={r.get('knee_y_L', 0):.2f}/{r.get('knee_y_R', 0):.2f}"
        )
        print("SHEET" + sheet_lines[-1], flush=True)
    return {
        "keys": keys, "capsule": caps, "facing": facings, "times": times,
        "skate": 0.0, "pen": pen, "errors": rows, "plants": plants,
        "hip_path": hip_path, "sheet_frames": sheet,
        "sheet_note": "\n".join(sheet_lines),
        "world": {"kind": "wall", "top_z": WALL_TOP_Z},
        "note": (
            "The source window is the climb, 0.60–2.00 s, filmed from behind. "
            "Yaw -90 points the nose at the wall (head +Z on -X). The side "
            "camera stays edge-on along +Y; the facing is the key, not a camera "
            "orbit. Plant and drive legs stay in that camera plane, knee up, "
            "foot tucked, not splayed in Y. The hip rail sits close to the face "
            "so each plant shin is about 30–45° off the wall, sole at 0.5 cm, "
            "toes up the brick. One locked camera frames the 4 m face and the "
            "1.5 m roof. The drawn trail is a 2 px line behind the body, a "
            "3-frame average of the measured path, with no sample dots. Trunk "
            "lean is the measured chest angle toward the wall, positive into "
            "the face. After the reach the near knee drives up, both palms stay "
            "on the slab through the press, the other leg hangs bent below the "
            "lip, then one knee lands and the clip ends standing on the roof. "
            "No new verb. No side-on view of this vertical plant is in the "
            "downloaded set. Clip 19 is not posed. The slide was not re-posed."
        ),
    }


def _over_landing_xy(x, y, landing_y):
    return abs(x - LOW_WALL_CX) <= LOW_WALL_HALF[0] and abs(y - landing_y) <= LOW_WALL_HALF[1]


def _hang_off_low_wall(arm, landing_y, landing_top):
    """Slide along the top so a shin in front of the feet is not inside the wall."""
    from mathutils import Vector
    center = (LOW_WALL_CX, landing_y, landing_top * 0.5)
    base_y = arm.location.y
    top_o = Vector((0.0, 0.0, landing_top))
    floor_n = Vector((0.0, 0.0, 1.0))

    def pen_now():
        worst = 0.0
        for v in _all_body_verts():
            worst = max(worst, _box_pen(v, center, LOW_WALL_HALF))
            worst = max(worst, max(0.0, -v.z), max(0.0, -v.x))
        return worst

    best = None
    for dy in (0.0, -0.2, -0.35, -0.5, -0.65, 0.2, 0.35, 0.5):
        arm.location.y = base_y + dy
        bpy_update()
        # The contact sole has to stay over the top, not past the edge.
        on = False
        for side in ("L", "R"):
            _n, patch = _sole_patch(arm, side)
            if not patch:
                continue
            c = sum(patch, Vector()) / len(patch)
            if _over_landing_xy(c.x, c.y, landing_y) and abs(c.z - landing_top) < 0.04:
                on = True
        if not on:
            continue
        depth = pen_now()
        if best is None or depth < best[0]:
            best = (depth, arm.location.copy())
        if depth <= 0.004:
            break
    if best is not None:
        arm.location = best[1]
        bpy_update()
    gaps = [(_sole_gap(arm, s, top_o, floor_n), s) for s in ("L", "R")]
    _gap, lower = min(gaps)
    _seat_sole(arm, lower, top_o, floor_n, clearance=0.006)


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


def _settle_raised_sole(arm, side, origin, normal, budget=40):
    """Drop this sole onto the plane. Returns hip degrees, knee degrees, gap metres."""
    gap = _sole_gap(arm, side, origin, normal)
    if gap <= 0.01:
        return 0.0, 0.0, gap
    base = _snap_rots(arm)
    loc = arm.location.copy()

    def apply(hip_d, knee_d):
        _restore_rots(arm, base)
        arm.location = loc
        for bone, delta in ((f"UpperLeg_{side}", hip_d), (f"LowerLeg_{side}", knee_d)):
            pb = arm.pose.bones[bone]
            pb.rotation_mode = "XYZ"
            e = pb.rotation_euler.copy()
            pb.rotation_euler = (e.x + math.radians(delta), e.y, e.z)
            bpy_update()
        _aim_bone_axis(arm, f"Foot_{side}", (0.0, 0.0, 1.0), (0.0, 0.0, -1.0))
        return _sole_gap(arm, side, origin, normal)

    def rank(g):
        if g < -0.003:
            return 1.0 + abs(g)
        return abs(g - 0.006)

    best = (rank(gap), 0.0, 0.0, gap)
    for step in (8,):
        for hip_d in range(-budget, budget + 1, step):
            for knee_d in range(-budget, budget + 1, step):
                g = apply(hip_d, knee_d)
                key = (rank(g), abs(hip_d) + abs(knee_d))
                if key < (best[0], abs(best[1]) + abs(best[2])):
                    best = (rank(g), hip_d, knee_d, g)
    h0, k0 = best[1], best[2]
    for hip_d in range(int(h0) - 8, int(h0) + 9, 2):
        for knee_d in range(int(k0) - 8, int(k0) + 9, 2):
            if abs(hip_d) > budget or abs(knee_d) > budget:
                continue
            g = apply(hip_d, knee_d)
            key = (rank(g), abs(hip_d) + abs(knee_d))
            if key < (best[0], abs(best[1]) + abs(best[2])):
                best = (rank(g), float(hip_d), float(knee_d), g)
    apply(best[1], best[2])
    return best[1], best[2], best[3]


def solve_turn(arm, samples, times):
    """Reference crouch and pop. The 180 is a preview yaw, not a Root key."""
    from mathutils import Vector, Euler
    face = 0.0
    wall_n = Vector((1.0, 0.0, 0.0))
    floor_n = Vector((0.0, 0.0, 1.0))
    wall_o = Vector((face, 0.0, 0.0))
    floor_o = Vector((0.0, 0.0, 0.0))
    landing_top = LOW_WALL_TOP
    landing_y = 2.40
    n = len(samples)
    keys, caps, facings, rows = [], [], [], []
    skate = 0.0
    pen = 0.0
    prev = None
    for i, ch in enumerate(samples):
        t = times[i]
        u_all = 0.0 if n == 1 else i / (n - 1)
        # Face the wall through the plant. The 180 happens in the air, so the
        # body is not yawed through the brick while the foot is on it.
        if t < 1.22:
            yaw = 0.0
        elif t < 1.55:
            yaw = math.pi * _smoothstep((t - 1.22) / 0.33)
        else:
            yaw = math.pi
        trunk = float(ch.get("trunk_lean_from_cam_vertical") or 0.0)
        if t < 0.92:
            phase = "run"
        elif t < 1.22:
            phase = "plant"
        elif t < 1.48:
            phase = "air"
        else:
            phase = "land"
        facing = Euler((math.radians(trunk if phase in ("plant", "land", "air") else min(trunk, 18.0)), 0.0, yaw), "XYZ")
        if phase == "run":
            origin = Vector((0.70, 0.85 - u_all * 0.7, 0.4))
        elif phase == "plant":
            u = _clamp((t - 0.92) / 0.30, 0.0, 1.0)
            origin = Vector((0.55 - u * 0.25, 0.85, 0.35))
        elif phase == "air":
            u = _clamp((t - 1.22) / 0.26, 0.0, 1.0)
            z = 0.35 + math.sin(u * math.pi) * 0.85 + u * landing_top
            y = 0.85 + u * (landing_y - 0.85)
            origin = Vector((0.35, y, z))
        else:
            origin = Vector((LOW_WALL_CX, landing_y, landing_top + 0.35))
        _apply_faithful(arm, ch, origin, facing)
        show_turn_world(face, landing_top, landing_y)
        if phase == "run":
            _seat_feet(arm, floor_o, floor_n)
            _shift_plane(arm, wall_o, wall_n, 0.0)
            _hands_on_plane(arm, floor_o, floor_n)
            _hands_on_plane(arm, wall_o, wall_n)
        elif phase == "plant":
            # Right sole on the vertical face. The other foot keeps the reference pose.
            gap = 1.0e9
            from mathutils import Vector as _V
            for v in _foot_verts("R"):
                gap = min(gap, wall_n.dot(_V(v) - wall_o))
            if gap < 1.0e8:
                arm.location = arm.location - wall_n * gap
                bpy_update()
            if _lowest_foot_z() < 0.0:
                arm.location.z -= _lowest_foot_z()
                bpy_update()
            hip = p5._head_w(arm, "Hips")
            if hip.x < 0.16:
                arm.location.x += 0.16 - hip.x
                bpy_update()
            _hands_on_plane(arm, wall_o, wall_n)
            # The upper-arm shell can stick through the face after the hand is seated.
            gap = _min_plane(wall_o, wall_n)
            if gap < 0.0:
                arm.location.x -= gap
                bpy_update()
        elif phase == "air":
            # Clear the box without pinning the feet onto its top mid-hop.
            _shift_above_box(arm, landing_y, landing_top)
            _shift_plane(arm, wall_o, wall_n, 0.0)
        else:
            # Lay both soles on the horizontal top, over the wall, not past it.
            down = Vector((0.0, 0.0, -1.0))
            for s in ("L", "R"):
                _aim_bone_axis(arm, f"Foot_{s}", (0.0, 0.0, 1.0), down)
            _nL, patch_l = _sole_patch(arm, "L")
            _nR, patch_r = _sole_patch(arm, "R")
            patches = [p for p in (patch_l, patch_r) if p]
            if patches:
                centers = []
                for patch in patches:
                    c = sum(patch, Vector()) / len(patch)
                    centers.append(c)
                contact = min(centers, key=lambda c: c.z)
                arm.location.x += LOW_WALL_CX - contact.x
                arm.location.y += landing_y - contact.y
                arm.location.z += (landing_top + 0.005) - contact.z
                bpy_update()
            _shift_plane(arm, wall_o, wall_n, 0.0)
            # Keep the lower sole on the top after the wall push.
            top_o = Vector((0.0, 0.0, landing_top))
            gaps = [(_sole_gap(arm, s, top_o, floor_n), s) for s in ("L", "R")]
            _gap, lower = min(gaps)
            _seat_sole(arm, lower, top_o, floor_n, clearance=0.005)
            _clear_pose_arms(arm)
            _seat_sole(arm, lower, top_o, floor_n, clearance=0.005)
            _hang_off_low_wall(arm, landing_y, landing_top)
            # The first land frame's raised sole has to be on the top. Later
            # land frames get the same drop when they are still in the air.
            gaps = [(_sole_gap(arm, s, top_o, floor_n), s) for s in ("L", "R")]
            _glow, lower = min(gaps)
            _ghi, higher = max(gaps)
            first_land = not any(r.get("phase") == "land" for r in rows)
            if _ghi > 0.01:
                budget = 42 if first_land else 28
                hip_d, knee_d, _got = _settle_raised_sole(arm, higher, top_o, floor_n, budget)
                _hang_off_low_wall(arm, landing_y, landing_top)
                _seat_sole(arm, lower, top_o, floor_n, clearance=0.006)
                if _sole_gap(arm, higher, top_o, floor_n) > 0.01:
                    hip_d, knee_d, _got = _settle_raised_sole(
                        arm, higher, top_o, floor_n, budget
                    )
                    _seat_sole(arm, lower, top_o, floor_n, clearance=0.006)
            else:
                hip_d, knee_d = 0.0, 0.0
            # Stash for the contact row. Overwritten each land frame.
            arm["land_hip_deg"] = hip_d
            arm["land_knee_deg"] = knee_d
        pen = max(pen, max(0.0, -_min_plane(wall_o, wall_n)))
        pen = max(pen, max(0.0, -_min_plane(floor_o, floor_n)))
        pen = max(
            pen,
            max(0.0, -_min_plane(Vector((0.0, 0.0, landing_top)), floor_n))
            if phase == "land" else 0.0,
        )
        if phase in ("run", "land"):
            nrm = floor_n
            origin = floor_o if phase == "run" else Vector((0.0, 0.0, landing_top))
            vert = _deepest("Foot_R", nrm)
            if vert is not None and abs(nrm.dot(Vector(vert) - origin)) < 0.04:
                mark = Vector(vert) - nrm * nrm.dot(Vector(vert) - origin)
                if prev is not None and (mark - prev).length < 0.4:
                    skate = max(skate, (mark - prev).length)
                prev = mark
            else:
                prev = None
        if phase != "land":
            _clear_pose_arms(arm)
        body = _measure_body(arm)
        row = _angle_row(ch, body)
        row["t"] = t
        row["phase"] = phase
        top_o = Vector((0.0, 0.0, landing_top))
        row["sole_L_cm"] = _sole_gap(arm, "L", top_o, floor_n) * 100.0
        row["sole_R_cm"] = _sole_gap(arm, "R", top_o, floor_n) * 100.0
        if phase == "land":
            row["land_hip_deg"] = float(arm.get("land_hip_deg", 0.0))
            row["land_knee_deg"] = float(arm.get("land_knee_deg", 0.0))
        rows.append(row)
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
        if i % 10 == 0 or phase == "land":
            print(
                f"  turn f={i} t={t:.2f} {phase} "
                f"knee={body['knee_L']:.0f}/{body['knee_R']:.0f} "
                f"hipz={body['hip_z']:.2f} "
                f"sole={row['sole_L_cm']:.2f}/{row['sole_R_cm']:.2f}"
                + (
                    f" drop={row.get('land_hip_deg', 0):.0f}/{row.get('land_knee_deg', 0):.0f}"
                    if phase == "land" else ""
                ),
                flush=True,
            )
    return {
        "keys": keys, "capsule": caps, "facing": facings, "times": times,
        "skate": skate, "pen": pen, "errors": rows,
        "world": {"kind": "turn", "landing_y": landing_y, "landing_top": landing_top, "face": face},
        "note": (
            "Yaw is a preview of the armature, not a Root key. Hip and knee follow "
            "the reference through the crouch and the pop. On the landing the raised "
            "sole is brought down onto the low wall. The degrees that took are logged."
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
    if spec["verb"] == "wallrun_vertical":
        # Absolute depth into the 4 m slab. The old half-space (any x < 0)
        # counted a palm resting on the top as inside the brick.
        for v in verts:
            pen = max(pen, max(0.0, -v.z), _slab_depth(v))
        return pen
    landing_top = float(world.get("landing_top", 0.42))
    landing_y = float(world.get("landing_y", 1.55))
    for v in verts:
        pen = max(pen, max(0.0, -v.z))
        pen = max(pen, max(0.0, -v.x))
        if spec["verb"] == "turn180":
            # Same box as show_turn_world. Scale is the half-extent of a 2 m cube.
            pen = max(pen, _box_pen(
                v,
                (LOW_WALL_CX, landing_y, landing_top * 0.5),
                LOW_WALL_HALF,
            ))
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


def _is_rig_pair(pair):
    """Hip/thigh shell nesting is the rig joint, not a pose we straighten away."""
    parts = str(pair).split("/")
    if len(parts) != 2:
        return False
    a, b = parts
    hips = {"Hips", "Spine"}
    legs = {"UpperLeg_L", "UpperLeg_R"}
    return (a in hips and b in legs) or (b in hips and a in legs)


def _pair_depths(arm):
    """Self crossings outside the 3 cm joint exemption. List of (metres, pair)."""
    import bpy
    rh._ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    bodies = []
    for ob in _body_objects():
        tree, _verts, tris = _eval_bvh(ob, deps)
        bodies.append((ob.parent_bone, tree, tris))
    child_of = {child: parent for parent, child in NEIGHBORS}
    found = []
    for i, (bone_a, tree_a, tris_a) in enumerate(bodies):
        for bone_b, tree_b, tris_b in bodies[i + 1:]:
            joint = None
            if child_of.get(bone_b) == bone_a:
                joint = _joint_world(arm, bone_b)
            elif child_of.get(bone_a) == bone_b:
                joint = _joint_world(arm, bone_a)
            depth = _cross_depth(tris_a, tris_b, tree_a, tree_b, joint)
            if depth > 0.0:
                found.append((depth, f"{bone_a}/{bone_b}"))
    return found


def _clear_pose_arms(arm):
    """Yaw an arm out of the chest. Legs are not touched."""
    for _step in range(10):
        pose_hits = [
            (d, p) for d, p in _pair_depths(arm)
            if d > DEPTH_LIMIT_M and not _is_rig_pair(p)
        ]
        if not pose_hits:
            return
        _d, pair = max(pose_hits)
        bone = None
        for token in pair.split("/"):
            if token.startswith(("UpperArm", "LowerArm", "Hand", "Shoulder")):
                bone = token
                break
        if bone is None:
            return
        # A hand in the chest clears by swinging the upper arm, not the wrist.
        if bone.startswith(("Hand", "LowerArm")):
            bone = "UpperArm_" + bone[-1]
        if bone not in arm.pose.bones:
            return
        pb = arm.pose.bones[bone]
        pb.rotation_mode = "XYZ"
        e = pb.rotation_euler
        sign = 1.0 if bone.endswith("_L") else -1.0
        pb.rotation_euler = (e.x, e.y, e.z + sign * 0.10)
        pb.scale = (1.0, 1.0, 1.0)
        bpy_update()


def _separate_hand_from_chest(arm):
    """Push a hand out of the chest without putting it through the brick."""
    for _step in range(6):
        hits = [
            (d, p) for d, p in _pair_depths(arm)
            if d > DEPTH_LIMIT_M and not _is_rig_pair(p)
            and any(tok.startswith(("Hand", "LowerArm", "UpperArm")) for tok in p.split("/"))
        ]
        if not hits:
            return
        depth0, pair = max(hits)
        bone = None
        for token in pair.split("/"):
            if token.startswith(("UpperArm", "LowerArm", "Hand")):
                bone = token
                break
        if bone is None or bone not in arm.pose.bones:
            return
        if bone.startswith(("Hand", "LowerArm")):
            bone = "UpperArm_" + bone[-1]
        snap = _snap_rots(arm)
        loc = arm.location.copy()
        rot = arm.rotation_euler.copy()
        _restore_rots(arm, snap)
        e0 = arm.pose.bones[bone].rotation_euler.copy()
        side = bone[-1]
        elbow = arm.pose.bones.get(f"LowerArm_{side}")
        e_elbow = elbow.rotation_euler.copy() if elbow is not None else None
        best = None
        # Pitch or yaw the shoulder, and open the elbow. A folded hand sits in the chest.
        # UpperArm X +0.80 is the clearance that lifts Hand_R out of the chest
        # while the sole stays on the +X side of the brick.
        for dz in (0.0, -0.45, 0.45):
            for dx in (0.0, 0.45, 0.80, -0.45):
                for elbow_x in (0.0, 0.40):
                    if dz == 0.0 and dx == 0.0 and elbow_x == 0.0:
                        continue
                    _restore_rots(arm, snap)
                    arm.location = loc
                    arm.rotation_euler = rot
                    bpy_update()
                    pb = arm.pose.bones[bone]
                    pb.rotation_mode = "XYZ"
                    pb.rotation_euler = (e0.x + dx, e0.y, e0.z + dz)
                    pb.scale = (1.0, 1.0, 1.0)
                    if elbow is not None and e_elbow is not None and elbow_x:
                        elbow.rotation_mode = "XYZ"
                        elbow.rotation_euler = (e_elbow.x + elbow_x, e_elbow.y, e_elbow.z)
                    bpy_update()
                    if _min_plane((0.0, 0.0, 0.0), (1.0, 0.0, 0.0)) < -0.004:
                        continue
                    left = [
                        d for d, p in _pair_depths(arm)
                        if d > DEPTH_LIMIT_M and not _is_rig_pair(p)
                    ]
                    depth = max(left) if left else 0.0
                    if best is None or depth < best[0]:
                        best = (depth, _snap_rots(arm), arm.location.copy(), arm.rotation_euler.copy())
        if best is None or best[0] > depth0 - 0.0004:
            _restore_rots(arm, snap)
            arm.location = loc
            arm.rotation_euler = rot
            bpy_update()
            return
        _restore_rots(arm, best[1])
        arm.location = best[2]
        arm.rotation_euler = best[3]
        bpy_update()


def _hand_on_ramp(arm, origin, normal):
    """Put the nearer hand on the ramp. Reject a pin that enters the chest."""
    from mathutils import Vector
    n = Vector(normal).normalized()
    o = Vector(origin)
    best = None
    best_gap = 1.0e9
    for side in ("L", "R"):
        vert = _deepest(f"Hand_{side}", n)
        if vert is None:
            continue
        gap = n.dot(Vector(vert) - o)
        if gap < best_gap:
            best_gap = gap
            best = side
    if best is None:
        return None
    if abs(best_gap) <= 0.012:
        return best
    vert = _deepest(f"Hand_{best}", n)
    lock = Vector(vert) - n * (best_gap - 0.006)
    before = _save_pose(arm)
    loc = arm.location.copy()
    pole = n * 0.75 + Vector((0.4 if best == "L" else -0.4, 0.1, 0.35))
    _pin_limb(arm, "hand", best, lock, n, pole)
    pose_hits = [
        d for d, p in _pair_depths(arm)
        if d > DEPTH_LIMIT_M and not _is_rig_pair(p)
    ]
    if pose_hits:
        rh._apply_eulers(arm, before)
        _clear_pose_channels(arm)
        rh._apply_eulers(arm, before)
        arm.location = loc
        bpy_update()
        return None
    return best


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


def _moving_average(path, window=3):
    """Centered moving average. The stored path stays raw."""
    n = len(path)
    half = window // 2
    out = []
    for i in range(n):
        acc = [0.0, 0.0, 0.0]
        count = 0
        for j in range(max(0, i - half), min(n, i + half + 1)):
            acc[0] += path[j][0]
            acc[1] += path[j][1]
            acc[2] += path[j][2]
            count += 1
        out.append((acc[0] / count, acc[1] / count, acc[2] / count))
    return out


def _set_hip_ghost(path, show):
    """Thin hip line behind the figure. The stored path stays raw.

    The line is a 3-frame average so the climb reads. It is about 2 px at
    the locked side camera, and it sits on +Y so the body occludes it.
    No sample dots. Hidden for the slide and the 180. Workbench draws the
    viewport colour, not emission.
    """
    import bpy
    import bmesh
    name = "HipGhost"
    ob = bpy.data.objects.get(name)
    if not show or not path or len(path) < 2:
        if ob is not None:
            ob.hide_render = True
            ob.hide_set(True)
        return
    mesh = bpy.data.meshes.get(name + "Mesh")
    if ob is None or ob.type != "MESH" or mesh is None:
        if ob is not None:
            bpy.data.objects.remove(ob, do_unlink=True)
        mesh = bpy.data.meshes.new(name + "Mesh")
        ob = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(ob)
        mat = bpy.data.materials.new(name + "Mat")
        mat.use_nodes = True
        mat.diffuse_color = (0.05, 0.85, 1.0, 1.0)
        nodes = mat.node_tree.nodes
        bsdf = nodes.get("Principled BSDF")
        if bsdf is not None and "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = (0.05, 0.85, 1.0, 1.0)
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = (0.15, 0.95, 1.0, 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 4.0
        ob.data.materials.append(mat)
    smooth = _moving_average(path, 3)
    # 2 px full width at the locked side camera (ortho height / 500 px).
    half = WALL_SIDE_SCALE / 500.0
    y = 0.85
    bm = bmesh.new()
    verts = []
    for p in smooth:
        verts.append(bm.verts.new((p[0] - half, y, p[2])))
        verts.append(bm.verts.new((p[0] + half, y, p[2])))
    bm.verts.ensure_lookup_table()
    for i in range(len(smooth) - 1):
        bm.faces.new((verts[2 * i], verts[2 * i + 1], verts[2 * i + 3], verts[2 * i + 2]))
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()
    ob.hide_render = False
    ob.hide_set(False)


def _show_for(spec, arm, solved=None):
    world = (solved or {}).get("world") or {}
    if spec["verb"] == "slide":
        _set_hip_ghost(None, False)
        show_slide_world(arm.location.y)
    elif spec["verb"] == "wallrun_vertical":
        show_wall_world(
            0.0, WALL_TOP_Z * 0.5, WALL_TOP_Z,
            hide_ground=False, ticks=True, half_thick=WALL_HALF_THICK,
        )
        _set_hip_ghost((solved or {}).get("hip_path"), True)
    else:
        _set_hip_ghost(None, False)
        show_turn_world(0.0, world.get("landing_top", 0.42), world.get("landing_y", 1.55))


def measure_noclip(arm, solved_all):
    frames = 0
    rig_n = 0
    pose_n = 0
    world_max = 0.0
    rig_max = 0.0
    pose_max = 0.0
    for spec in SPECS:
        solved = solved_all[spec["id"]]
        rig_frames = []
        pose_frames = []
        worst_i = 0
        worst_depth = -1.0
        worst_pair = ""
        for i in range(len(solved["keys"])):
            pose_from_key(arm, solved, i, spec)
            world, _self_pen, _pair = noclip_frame(arm, spec, solved.get("world"))
            pairs = _pair_depths(arm)
            rig_d, rig_p = 0.0, ""
            pose_d, pose_p = 0.0, ""
            for depth, pair in pairs:
                if _is_rig_pair(pair):
                    if depth > rig_d:
                        rig_d, rig_p = depth, pair
                elif depth > pose_d:
                    pose_d, pose_p = depth, pair
            if world > pose_d:
                pose_d, pose_p = world, "world"
            frames += 1
            world_max = max(world_max, world)
            rig_max = max(rig_max, rig_d)
            pose_max = max(pose_max, pose_d)
            if rig_d > worst_depth:
                worst_depth = rig_d
                worst_i = i
                worst_pair = rig_p
            if pose_d > DEPTH_LIMIT_M and pose_d >= worst_depth:
                worst_depth = pose_d
                worst_i = i
                worst_pair = pose_p
            if rig_d > DEPTH_LIMIT_M:
                rig_n += 1
                rig_frames.append({
                    "frame": i,
                    "t": round(solved["times"][i], 3),
                    "cm": round(rig_d * 100.0, 2),
                    "pair": rig_p,
                    "kind": "rigJoint",
                })
            if pose_d > DEPTH_LIMIT_M:
                pose_n += 1
                pose_frames.append({
                    "frame": i,
                    "t": round(solved["times"][i], 3),
                    "cm": round(pose_d * 100.0, 2),
                    "pair": pose_p,
                    "kind": "pose",
                })
        solved["fail_frames"] = rig_frames + pose_frames
        solved["rig_frames"] = rig_frames
        solved["pose_frames"] = pose_frames
        solved["worst_i"] = worst_i
        solved["worst_pair"] = worst_pair or "world"
        print(
            f"NOCLIP {spec['id']} rigJoint={len(rig_frames)} pose={len(pose_frames)} "
            f"worst_f={worst_i} pair={solved['worst_pair']}",
            flush=True,
        )
    line = (
        f"no-clip clips={len(SPECS)} frames={frames} "
        f"worldMax={world_max*100:.2f} rigMax={rig_max*100:.2f} poseMax={pose_max*100:.2f} "
        f"rigJoint={rig_n} pose={pose_n}"
    )
    print(line, flush=True)
    return line, pose_n, world_max, pose_max


# --- stills ---------------------------------------------------------------

def _body_bounds():
    from mathutils import Vector
    pts = _all_body_verts()
    if not pts:
        return Vector((0.0, 0.0, 0.0)), Vector((1.0, 1.0, 1.0))
    return (
        Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))),
        Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts))),
    )


def _wall_body_frame(arm):
    """Focus and ortho scale so the figure is about 70% of a portrait cell.

    The wall face stays in frame. The lip is included once the body is near it.
    Returns focus xyz, ortho scale (world height), and figure/frame fraction.
    """
    from mathutils import Vector
    mn, mx = _body_bounds()
    head = Vector(p5._head_w(arm, "Head"))
    head_top = head.z + float(arm.pose.bones["Head"].length)
    foot_z = mx.z
    for side in ("L", "R"):
        vs = p5._verts(f"Foot_{side}")
        if vs:
            foot_z = min(foot_z, min(v.z for v in vs))
    top = max(head_top, mx.z)
    bot = min(foot_z, mn.z)
    figure_h = max(1.05, top - bot)
    scale = figure_h / 0.70
    aspect = 394.0 / 500.0
    z0 = bot - 0.08
    z1 = top + 0.10
    # The lip comes into frame once the body is up at it. A wide arm does
    # not zoom the figure out: the cell stays on the body.
    if top >= WALL_TOP_Z - 0.75:
        lip = WALL_TOP_Z + 0.14
        if (lip - (bot - 0.06)) <= figure_h / 0.64:
            z1 = max(z1, lip)
    if bot <= 0.35:
        z0 = min(z0, -0.02)
    need_h = z1 - z0
    if need_h > scale and need_h <= figure_h / 0.64:
        scale = need_h
    x0 = WALL_BACK_X - 0.05
    x1 = mx.x + 0.08
    half_h = scale * 0.5
    half_w = scale * aspect * 0.5
    cz = (bot + top) * 0.5
    cx = (mn.x + mx.x) * 0.5
    if cz - half_h > z0:
        cz = z0 + half_h
    if cz + half_h < z1:
        cz = z1 - half_h
    if cx - half_w > x0:
        cx = x0 + half_w
    if cx + half_w < x1:
        cx = x1 - half_w
    frac = figure_h / scale if scale else 0.0
    return (cx, 0.0, cz), scale, frac


def _plant_inset_camera(arm, main_scale):
    """Edge-on close-up of the planted sole and the wall, at 4× the cell.

    Displayed at 176 px inside a 500 px cell. Ortho scale is the world height
    of that inset, so a pixel there covers one quarter of a cell pixel.
    """
    from mathutils import Vector
    best = None
    for side in ("L", "R"):
        _n, patch = _sole_patch(arm, side)
        if not patch:
            continue
        cx = min(v.x for v in patch)
        cz = sum(v.z for v in patch) / len(patch)
        if best is None or cx < best[0]:
            best = (cx, cz)
    sole_x, sole_z = (0.02, 1.5) if best is None else best
    disp = 176.0
    world_h = disp * float(main_scale) / (4.0 * 500.0)
    world_h = max(world_h, 0.16)
    focus = ((sole_x + WALL_FACE_X) * 0.45, 0.0, sole_z + 0.035)
    loc = (focus[0], -6.0, focus[2])
    return loc, focus, world_h


def _shot_for(spec, arm):
    """Perspective framed on the pelvis, wide enough for the whole body and the prop."""
    from mathutils import Vector
    pelvis = p5._head_w(arm, "Hips")
    mn, mx = _body_bounds()
    center = (mn + mx) * 0.5
    focus = pelvis * 0.6 + center * 0.4
    rad = max(0.9, (mx - mn).length * 0.55)
    # 48 mm on a portrait frame. Stay back so nothing clips the cell edge.
    dist = rad / math.sin(math.radians(18.0))
    if spec["verb"] == "slide":
        direction = Vector((0.45, -0.8, 0.28)).normalized()
    elif spec["verb"] == "wallrun_vertical":
        # One locked three-quarter for every cell, from behind-side so the
        # wall is in front of the chest. It does not track the body, and it
        # does not sit on the far side of the brick.
        focus = WALL_CAM_FOCUS
        direction = Vector(WALL_34_DIR).normalized()
        dist = WALL_SIDE_SCALE * 2.05
        loc = Vector(focus) + direction * dist
        print(f"  cam3/4 locked dist={dist:.2f} focus={focus}", flush=True)
        return (loc.x, loc.y, loc.z), focus
    else:
        direction = Vector((0.7, -0.65, 0.25)).normalized()
    loc = Vector(focus) + direction * dist
    return (loc.x, loc.y, loc.z), (focus.x, focus.y, focus.z)


def _side_shot(spec, arm):
    """Orthographic profile. Returns loc, focus, ortho scale, and the camera up axis.

    The up axis is the camera axis that Blender aims at world up. Looking along
    X, that axis is Y. Looking along Y, it is Z. Either way image-up is world +Z
    and the prop is edge-on.
    """
    pelvis = p5._head_w(arm, "Hips")
    mn, mx = _body_bounds()
    if spec["verb"] == "slide":
        span_y = (mx.y - mn.y) + 0.9
        span_z = (mx.z - mn.z) + 0.7
        scale = max(span_y, span_z, 2.2)
        focus = ((mn.x + mx.x) * 0.5, (mn.y + mx.y) * 0.5, pelvis.z)
        loc = (focus[0] + 6.0, focus[1], focus[2])
        return loc, focus, scale, "Y"
    if spec["verb"] == "wallrun_vertical":
        # Look exactly along +Y. One scale and one focus for all 8 cells,
        # so the face and the 4 m marks do not move. Do not orbit this
        # camera to fake a profile; the yaw key is the facing.
        focus = WALL_CAM_FOCUS
        scale = WALL_SIDE_SCALE
        loc = (focus[0], -8.0, focus[2])
        print(
            f"  camSide locked scale={scale:.2f} focus=({focus[0]:.2f},{focus[2]:.2f})",
            flush=True,
        )
        return loc, focus, scale, "Z"
    span_y = (mx.y - mn.y) + 1.4
    span_z = (mx.z - mn.z) + 0.8
    scale = max(span_y, span_z, 2.6)
    focus = ((mn.x + mx.x) * 0.5, (mn.y + mx.y) * 0.5, pelvis.z)
    loc = (focus[0] + 6.5, focus[1], focus[2])
    return loc, focus, scale, "Y"


def _mark_overlap(pair):
    """Paint the crossing shells red. Returns a restore list."""
    import bpy
    saved = []
    if not pair or pair == "world":
        return saved
    mat = bpy.data.materials.get("OverlapRed")
    if mat is None:
        mat = bpy.data.materials.new("OverlapRed")
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (0.9, 0.04, 0.03, 1.0)
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = (1.0, 0.08, 0.04, 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 3.0
    for token in str(pair).split("/"):
        token = token.strip()
        if not token:
            continue
        ob = bpy.data.objects.get(f"Mesh_{token}")
        if ob is None or ob.type != "MESH":
            continue
        saved.append((ob, [slot.material for slot in ob.material_slots]))
        ob.data.materials.clear()
        ob.data.materials.append(mat)
    return saved


def _unmark_overlap(saved):
    for ob, mats in saved:
        ob.data.materials.clear()
        for mat in mats:
            if mat is not None:
                ob.data.materials.append(mat)


def _fix_right_thigh_tan():
    """The right thigh renders near-black. Flip inward normals and use the tan.

    This is a stills-only edit of the evaluated mesh. The blend file is not saved,
    and the rig proportions stay as PR #128 left them.
    """
    import bmesh
    import bpy
    from mathutils import Vector
    ob = bpy.data.objects.get("Mesh_UpperLeg_R")
    if ob is None or ob.type != "MESH":
        return
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    if bm.verts:
        center = sum((v.co.copy() for v in bm.verts), Vector()) / len(bm.verts)
        for face in bm.faces:
            if face.normal.dot(face.calc_center_median() - center) < 0.0:
                face.normal_flip()
    bm.to_mesh(me)
    bm.free()
    me.update()
    src = bpy.data.materials.get("Base")
    tan = bpy.data.materials.get("ThighTanPass6")
    if tan is None:
        tan = src.copy() if src is not None else bpy.data.materials.new("ThighTanPass6")
        tan.name = "ThighTanPass6"
    tan.use_nodes = True
    tan.use_backface_culling = False
    bsdf = tan.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None and "Base Color" in bsdf.inputs:
        bsdf.inputs["Base Color"].default_value = (0.95, 0.88, 0.76, 1.0)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = 0.55
    me.materials.clear()
    me.materials.append(tan)
    for poly in me.polygons:
        poly.material_index = 0


def _beat_index(solved, s):
    """Sheet cell s. The wall run pins cell 7 on the reach and cell 8 on the catch."""
    picks = solved.get("sheet_frames")
    if picks:
        return int(picks[s])
    n = len(solved["keys"])
    return int(round(s * (n - 1) / 7.0))


def render_beats(arm, solved_all):
    import bpy
    os.makedirs(PREV, exist_ok=True)
    rh._prepare_scene()
    _fix_right_thigh_tan()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    # 4 columns + 3 gaps of 8 px land on a 1600 px sheet.
    bpy.context.scene.render.resolution_x = 394
    bpy.context.scene.render.resolution_y = 500
    for spec in SPECS:
        if spec["id"] not in solved_all:
            continue
        solved = solved_all[spec["id"]]
        n = len(solved["keys"])
        fit = "VERTICAL" if spec["verb"] == "wallrun_vertical" else None
        for s in range(8):
            i = _beat_index(solved, s)
            pose_from_key(arm, solved, i, spec)
            loc, look = _shot_for(spec, arm)
            rh._shot(
                os.path.join(PREV, f"{spec['verb']}_{s}.png"),
                loc, look, sensor_fit=fit,
            )
            sloc, slook, scale, up = _side_shot(spec, arm)
            rh._shot(
                os.path.join(PREV, f"{spec['verb']}_side_{s}.png"),
                sloc, slook, ortho=scale, up=up, sensor_fit=fit,
            )
            errs = solved.get("errors") or []
            phase = errs[i].get("phase") if i < len(errs) else ""
            if spec["verb"] == "wallrun_vertical" and phase == "plant":
                iloc, ilook, iscale = _plant_inset_camera(arm, scale)
                scene = bpy.context.scene
                scene.render.resolution_x = 256
                scene.render.resolution_y = 256
                rh._shot(
                    os.path.join(PREV, f"{spec['verb']}_side_{s}_inset.png"),
                    iloc, ilook, ortho=iscale, up="Z", sensor_fit="VERTICAL",
                )
                scene.render.resolution_x = 394
                scene.render.resolution_y = 500
                print(
                    f"  inset {s} ortho={iscale:.3f} focus=({ilook[0]:.3f},{ilook[2]:.3f})",
                    flush=True,
                )
            print(f"still {spec['id']} {s} frame={i} t={solved['times'][i]:.2f}", flush=True)
        wi = int(solved.get("worst_i", n // 2))
        pose_from_key(arm, solved, wi, spec)
        marked = _mark_overlap(solved.get("worst_pair", ""))
        loc, look = _shot_for(spec, arm)
        rh._shot(os.path.join(PREV, f"{spec['verb']}_overlap_a.png"), loc, look)
        sloc, slook, scale, up = _side_shot(spec, arm)
        rh._shot(
            os.path.join(PREV, f"{spec['verb']}_overlap_b.png"),
            sloc, slook, ortho=scale, up=up, sensor_fit=fit,
        )
        _unmark_overlap(marked)
        print(f"overlap {spec['id']} frame={wi} pair={solved.get('worst_pair')}", flush=True)


def _save_sheet(path, image, limit=400_000):
    """Keep a 1600 px sheet under 400 KB without shrinking the cells."""
    image.save(path, optimize=True)
    if os.path.getsize(path) <= limit:
        return
    quant = image.quantize(colors=64, method=2).convert("RGB")
    quant.save(path, optimize=True)


def _grid(frames, solved, spec, title):
    from PIL import Image, ImageDraw, ImageFont
    font = ImageFont.load_default()
    gap = 8
    fw, fh = frames[0].size
    grid_w = fw * 4 + gap * 3
    grid_h = fh * 2 + gap
    grid = Image.new("RGB", (grid_w, grid_h), (48, 44, 40))
    draw = ImageDraw.Draw(grid)
    for s, fr in enumerate(frames):
        c = s % 4
        r = s // 4
        x = c * (fw + gap)
        y = r * (fh + gap)
        grid.paste(fr, (x, y))
        i = _beat_index(solved, s)
        errs = solved.get("errors") or []
        phase = errs[i].get("phase") if 0 <= i < len(errs) else ""
        if spec["verb"] == "wallrun_vertical" and phase == "plant":
            inset_path = os.path.join(PREV, f"{spec['verb']}_side_{s}_inset.png")
            if os.path.isfile(inset_path):
                inset = Image.open(inset_path).convert("RGB")
                box = 176
                inset = inset.resize((box, box), Image.Resampling.LANCZOS)
                ix = x + fw - box - 12
                iy = y + 30
                draw.rectangle((ix - 4, iy - 4, ix + box + 3, iy + box + 3), fill=(12, 18, 22))
                grid.paste(inset, (ix, iy))
                draw.text((ix + 6, iy + 4), "4x foot", fill=(220, 245, 255), font=font)
        tag = f"{s + 1}  {solved['times'][i]:.2f}s"
        errs = solved.get("errors") or []
        if spec["verb"] == "wallrun_vertical" and 0 <= i < len(errs) and "lean" in errs[i]:
            tag += f"  {errs[i]['lean']:+.0f}deg"
        draw.rectangle((x + 6, y + 6, x + 8 + 7 * len(tag), y + 22), fill=(20, 18, 16))
        draw.text((x + 10, y + 8), tag, fill=(245, 236, 220), font=font)
    strip = Image.open(os.path.join("/workspace/Docs/Storror/out/strips", spec["strip"])).convert("RGB")
    if spec["verb"] == "wallrun_vertical":
        # The strip is the whole 0–2.40 s clip, 12 cells. Cells before 0.60 s
        # are the run-in (a standing figure). Show the climb, 0.60–2.00 s.
        # The camera is behind the runner, so those cells stay upright too.
        ncells = 12
        cell = strip.width / float(ncells)
        c0 = int(round(0.60 / 2.40 * (ncells - 1)))
        c1 = int(round(2.00 / 2.40 * (ncells - 1)))
        strip = strip.crop((
            int(round(c0 * cell)), 0,
            int(round((c1 + 1) * cell)), strip.height,
        ))
    sh = 150
    sw = grid_w
    strip = strip.resize((sw, sh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (grid_w, sh + 8 + grid_h + 28), (32, 30, 28))
    d = ImageDraw.Draw(canvas)
    d.text((8, 6), title[:210], fill=(245, 236, 220), font=font)
    canvas.paste(strip, (0, 26))
    canvas.paste(grid, (0, 26 + sh + 8))
    return canvas


def _angle_brief(solved):
    rows = solved.get("errors") or []
    if not rows:
        return "no angle rows"
    def mx(key_a, key_b):
        return max(abs(r[key_a] - r[key_b]) for r in rows)
    hip = max(mx("hip_L", "hip_L_ref"), mx("hip_R", "hip_R_ref"))
    knee = max(mx("knee_L", "knee_L_ref"), mx("knee_R", "knee_R_ref"))
    spine = mx("spine", "spine_ref")
    pelvis = mx("pelvis_cm", "pelvis_ref_cm")
    return f"max |dhip| {hip:.1f}deg |dknee| {knee:.1f}deg |dspine| {spine:.1f}deg |dpelvis| {pelvis:.1f}cm"


def composite(solved_all, noclip_line):
    import site
    site.addsitedir(site.getusersitepackages())
    from PIL import Image
    os.makedirs(OUT, exist_ok=True)
    for spec in SPECS:
        if spec["id"] not in solved_all:
            continue
        solved = solved_all[spec["id"]]
        brief = _angle_brief(solved)
        rig_n = len(solved.get("rig_frames") or [])
        pose_n = len(solved.get("pose_frames") or [])
        title = f"{spec['label']}  {brief}  rigJoint {rig_n} pose {pose_n}"
        for kind, suffix in (("", ""), ("_side", " side")):
            frames = [
                Image.open(os.path.join(PREV, f"{spec['verb']}{kind}_{s}.png")).convert("RGB")
                for s in range(8)
            ]
            canvas = _grid(frames, solved, spec, title + suffix)
            path = os.path.join(OUT, f"{spec['verb']}{kind}.png")
            _save_sheet(path, canvas)
            print("wrote", path, canvas.size, os.path.getsize(path), flush=True)
        a = Image.open(os.path.join(PREV, f"{spec['verb']}_overlap_a.png")).convert("RGB")
        b = Image.open(os.path.join(PREV, f"{spec['verb']}_overlap_b.png")).convert("RGB")
        pair = solved.get("worst_pair") or "world"
        wi = solved.get("worst_i", 0)
        from PIL import ImageDraw, ImageFont
        font = ImageFont.load_default()
        gap = 8
        canvas = Image.new("RGB", (a.width + b.width + gap, a.height + 24), (32, 30, 28))
        canvas.paste(a, (0, 24))
        canvas.paste(b, (a.width + gap, 24))
        ImageDraw.Draw(canvas).text(
            (8, 6),
            f"{spec['label']} worst f={wi} t={solved['times'][wi]:.2f}s red={pair}",
            fill=(245, 236, 220),
            font=font,
        )
        path = os.path.join(OUT, f"{spec['verb']}_overlap.png")
        _save_sheet(path, canvas)
        print("wrote", path, canvas.size, os.path.getsize(path), flush=True)


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
            "angle_error": [
                {k: (round(v, 2) if isinstance(v, float) else v) for k, v in row.items()}
                for row in solved.get("errors") or []
            ],
            "noclip_fail_frames": solved.get("fail_frames") or [],
        }
    path = os.path.join(OUT, "keyed_clips.json")
    with open(path, "w") as f:
        json.dump(doc, f)
    _write_reports(solved_all, noclip_line)
    print("wrote", path, flush=True)


def _write_reports(solved_all, noclip_line):
    with open(os.path.join(OUT, "noclip.txt"), "w") as f:
        f.write(noclip_line + "\n")
        f.write(
            "rigJoint is the hip/thigh shell nesting (kept; legs are not straightened). "
            "pose is every other self crossing, plus world penetration, over 0.5 cm.\n"
        )
        for spec in SPECS:
            s = solved_all[spec["id"]]
            rig = s.get("rig_frames") or []
            pose = s.get("pose_frames") or []
            f.write(
                f"{spec['id']} frames={len(s['keys'])} "
                f"rigJoint={len(rig)} pose={len(pose)} "
                f"worst_f={s.get('worst_i')} worst_pair={s.get('worst_pair')}\n"
            )
            for row in rig + pose:
                f.write(
                    f"  {row['kind']} f={row['frame']} t={row['t']:.3f} "
                    f"cm={row['cm']:.2f} {row['pair']}\n"
                )
    with open(os.path.join(OUT, "contact.txt"), "w") as f:
        f.write(
            "Distances in cm, every frame. Wall-run soleL/soleR are the sole-to-wall "
            "gap (the plant is 0-1 cm, toes up). 180 soleL/soleR are the sole-to-top "
            "gap. Slide hand is the trail hand to the concrete.\n"
        )
        for spec in SPECS:
            rows = solved_all[spec["id"]].get("errors") or []
            f.write(spec["id"] + "\n")
            for i, r in enumerate(rows):
                extra = ""
                if "phase" in r:
                    extra += f" phase={r['phase']}"
                if "plant" in r:
                    extra += f" plant={r['plant']}"
                sl = r.get("sole_L_cm")
                sr = r.get("sole_R_cm")
                hand = r.get("hand_cm")
                if sl is None and hand is None:
                    continue
                sole = ""
                if sl is not None:
                    sole = f" soleL={sl:.2f} soleR={sr:.2f}"
                if hand is not None:
                    sole += f" hand={hand:.2f}"
                    short = r.get("hand_short_cm") or 0.0
                    if short > 0.05:
                        sole += (
                            f" extra_deg={r.get('hand_extra_deg', 0):.0f}"
                            f" base_cm={r.get('hand_base_cm', hand):.1f}"
                            f" short_cm={short:.1f}"
                        )
                        more = r.get("hand_more_deg") or 0.0
                        if more > 0.5:
                            sole += f" need_more_deg={more:.0f}"
                        else:
                            sole += " pitch_wont_close"
                if "low_cm" in r:
                    sole += f" low={r['low_cm']:.2f}"
                if "ground_L_cm" in r:
                    sole += f" gndL={r['ground_L_cm']:.2f} gndR={r['ground_R_cm']:.2f}"
                if "handL_cm" in r:
                    sole += (
                        f" handL={r['handL_cm']:.1f} handR={r['handR_cm']:.1f}"
                        f" handZ={r.get('hand_z', 0):.2f}"
                    )
                if "palmL_cm" in r:
                    sole += (
                        f" palmL={r['palmL_cm']:.2f} palmR={r['palmR_cm']:.2f}"
                        f" palmX={r.get('palm_x', 0):.3f}"
                    )
                if "hip_z" in r:
                    sole += f" hipZ={r['hip_z']:.2f} step={r.get('step', 0)}"
                if "hip_x" in r:
                    sole += f" hipX={r['hip_x']:.3f} hipY={r['hip_y']:.3f}"
                if "lean" in r:
                    sole += f" lean={r['lean']:+.1f}"
                if "shin_deg" in r:
                    sole += f" shinWall={r['shin_deg']:.0f} below={r['foot_below_hip']:.2f}"
                if "land_hip_deg" in r:
                    sole += (
                        f" drop_hip={r['land_hip_deg']:.0f} drop_knee={r['land_knee_deg']:.0f}"
                    )
                f.write(f"  f={i} t={r['t']:.3f}{sole}{extra}\n")
    header = (
        "t hipL_ref hipL dhipL hipR_ref hipR dhipR "
        "kneeL_ref kneeL dkneeL kneeR_ref kneeR dkneeR "
        "spine_ref spine dspine pelvis_ref_cm pelvis_cm dpelvis_cm"
    )
    lines = [
        "Hip is elevation from the downward torso axis (the thigh pitch). "
        "Knee is 0 when straight. Spine is the torso angle from world vertical, "
        "matched to trunk_lean_from_cam_vertical. Pelvis is hip height above the "
        "contact. The reference is root_height_above_feet scaled by 0.90/0.75, "
        "except on frames where that camera height collapses (under 75% of the "
        "side-view hip-to-foot-line height). Those frames use the side height.",
        "",
    ]
    for spec in SPECS:
        rows = solved_all[spec["id"]].get("errors") or []
        lines.append(spec["id"])
        lines.append(header)
        over = []
        for i, r in enumerate(rows):
            vals = [
                ("dhipL", r["hip_L"] - r["hip_L_ref"]),
                ("dhipR", r["hip_R"] - r["hip_R_ref"]),
                ("dkneeL", r["knee_L"] - r["knee_L_ref"]),
                ("dkneeR", r["knee_R"] - r["knee_R_ref"]),
                ("dspine", r["spine"] - r["spine_ref"]),
                ("dpelvis", r["pelvis_cm"] - r["pelvis_ref_cm"]),
            ]
            bits = [f"{r['t']:.3f}"]
            bits += [
                f"{r['hip_L_ref']:.1f}", f"{r['hip_L']:.1f}", f"{vals[0][1]:+.1f}",
                f"{r['hip_R_ref']:.1f}", f"{r['hip_R']:.1f}", f"{vals[1][1]:+.1f}",
                f"{r['knee_L_ref']:.1f}", f"{r['knee_L']:.1f}", f"{vals[2][1]:+.1f}",
                f"{r['knee_R_ref']:.1f}", f"{r['knee_R']:.1f}", f"{vals[3][1]:+.1f}",
                f"{r['spine_ref']:.1f}", f"{r['spine']:.1f}", f"{vals[4][1]:+.1f}",
                f"{r['pelvis_ref_cm']:.1f}", f"{r['pelvis_cm']:.1f}", f"{vals[5][1]:+.1f}",
            ]
            lines.append(" ".join(bits))
            bad = []
            for name, err in vals:
                if name == "dpelvis":
                    limit = 5.0
                elif name == "dspine" and spec["id"].startswith("18_"):
                    # The chest may lean up to 15° to stay out of the brick.
                    limit = 15.0
                else:
                    limit = 10.0
                if abs(err) > limit:
                    bad.append(f"{name}={err:+.1f}")
            if bad:
                over.append(f"  f={i} t={r['t']:.3f} " + " ".join(bad))
        lines.append(f"frames over 10 deg or 5 cm: {len(over)} / {len(rows)}")
        if spec["id"].startswith("18_"):
            lines.append(
                "Side or 3/4 vertical wall run: none in the downloaded set. "
                "Clip 19 is the same warped wall and is side-on only for the "
                "run-in (about 0.6 s, trunk about 1-10 deg); the camera is behind "
                "the runner once the knees drive up, and L/R flips near 1.6 s. "
                "Clips 10 and 11 are 3/4 views of Brighton's slanted sea wall, "
                "not a vertical face. Clip 10's high step peaks near knee 115 deg "
                "and trunk 14 deg. Clip 12 is a slanted tic-tac. No ground slide "
                "under a rail or bar is in clips 01-20. A pass-10 search of public "
                "Storror and other parkour footage found slope slides, rail grinds, "
                "slide vaults and underbars (feet through a rail, back off the "
                "ground). No clean side or 3/4 view of a feet-first or thigh-down "
                "ground slide under a bar, so the slide pose is unchanged."
            )
            note = solved_all[spec["id"]].get("sheet_note")
            if note:
                lines.append(
                    "Sheet cells, measured trunk lean (positive = chest toward the wall):"
                )
                lines.append(note)
        lines.extend(over)
        collapsed = [
            f"f={i} t={r['t']:.3f} cam={r['pelvis_cam_cm']:.1f} side={r['pelvis_side_cm']:.1f}"
            for i, r in enumerate(rows) if r.get("pelvis_src") == "side"
        ]
        lines.append(f"pelvis from side view (camera height collapsed): {len(collapsed)}")
        lines.extend("  " + c for c in collapsed)
        lines.append("")
    text = "\n".join(lines) + "\n"
    with open(os.path.join(OUT, "angle_error.txt"), "w") as f:
        f.write(text)
    print(text, flush=True)


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
        if "--metrics" not in args:
            render_beats(arm, solved_all)
            composite(solved_all, "partial")
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
