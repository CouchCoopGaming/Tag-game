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


def _apply_sample(arm, ch, location, rotation):
    import bpy
    rh._apply_eulers(arm, sp.blender_euler(sp.unity_pose(ch)))
    arm.location = location
    arm.rotation_euler = rotation
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
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

    for _ in range(6):
        vert = point()
        if vert is None:
            return
        joint = p5._head_w(arm, end)
        p5._solve_chain(arm, upper, lower, lock + (joint - vert) + nudge, pole)
        p5._seat_end(arm, end, point, lock)
        seated = point()
        if seated is None:
            return
        err = lock - seated
        normal_err = err.dot(nrm)
        tangent = err - nrm * normal_err
        if abs(normal_err) < 0.004 and tangent.length < 0.010:
            break
        nudge += nrm * normal_err
        if tangent.length > 0.035:
            tangent = tangent.normalized() * 0.035
        nudge += tangent * 0.65
    bpy.context.view_layer.update()


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
        faces = [
            (0, 3, 2, 1),
            (4, 5, 6, 7),
            (0, 1, 5, 4),
            (1, 2, 6, 5),
            (2, 3, 7, 6),
            (3, 0, 4, 7),
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
    # Solid occupies x < face_x. The +X face is the contact. Scale is half-extents
    # because the primitive cube is 2 m on a side (scale 1 → 2 m).
    half_thick = 0.12
    _make_box(
        "ActionWall",
        (face_x - half_thick, 0.0, center_z),
        (half_thick, 1.1, length_z * 0.5),
        (0.42, 0.40, 0.38, 1),
    )
    _hide_named(("ActionSlope", "LandingWall"))
    ground = bpy.data.objects.get("Ground")
    if ground is not None:
        ground.hide_render = False
        ground.hide_set(False)


def show_turn_world(face_x, landing_top, landing_y):
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


def solve_slide(arm, samples, times):
    from mathutils import Vector, Euler
    ang, y0, normal, down = _slope_frame()
    n = len(samples)
    keys = []
    caps = []
    facings = []
    skate = 0.0
    pen = 0.0
    locals_lock = {"L": None, "R": None}
    hand_lock = None
    hand_side = None
    for i, ch in enumerate(samples):
        u = i / (n - 1)
        # Drop into the crouch, hold the slide, then the push-off opens up.
        crouch = _smoothstep(u / 0.28) * (1.0 - _smoothstep((u - 0.72) / 0.28))
        lean = math.radians(18.0 + 22.0 * crouch)
        # Negative X lays the chest back up the slope (head toward +Y).
        facing = Euler((-lean, 0.0, 0.0), "XYZ")
        y = 1.55 - u * 2.15
        origin = Vector((0.0, y, _slope_z(y, y0, ang)))
        _apply_sample(arm, ch, origin, facing)
        # Seat the lower sole on the ramp before the locks are taken.
        vert = _deepest("Foot_L", normal)
        vert_r = _deepest("Foot_R", normal)
        deepest = vert
        if vert_r is not None and (deepest is None or normal.dot(vert_r) < normal.dot(deepest)):
            deepest = vert_r
        if deepest is not None:
            gap = normal.dot(deepest - origin)
            arm.location = origin - normal * gap
            bpy_update()
            origin = arm.location.copy()
        if locals_lock["L"] is None:
            for side in ("L", "R"):
                locals_lock[side] = _foot_local_lock(arm, side, normal, origin)
            # Trail hand: the wrist that sits closer to the ramp.
            wrists = []
            for side in ("L", "R"):
                w = p5._head_w(arm, f"Hand_{side}")
                wrists.append((normal.dot(w), side))
            wrists.sort()
            hand_side = wrists[0][1]
            palm = _deepest(f"Hand_{hand_side}", normal)
            if palm is not None:
                on = _project_plane(palm, origin, normal)
                hand_lock = arm.matrix_world.inverted() @ on
        show_slide_world(origin.y)
        for side in ("L", "R"):
            if locals_lock[side] is None:
                continue
            world = arm.matrix_world @ locals_lock[side]
            world = _project_plane(world, origin, normal)
            pole = _pole_knee(side, normal, Vector((1.0, 0.0, 0.0)))
            _pin_limb(arm, "foot", side, world, normal, pole)
            # Skate is motion along the slope beyond the capsule lock.
            vert = _deepest(f"Foot_{side}", normal)
            if vert is not None:
                expected = _project_plane(arm.matrix_world @ locals_lock[side], origin, normal)
                err = vert - expected
                tangential = (err - normal * err.dot(normal)).length
                skate = max(skate, tangential)
                pen = max(pen, max(0.0, -normal.dot(vert - origin)))
        if hand_lock is not None and crouch > 0.45:
            world = arm.matrix_world @ hand_lock
            world = _project_plane(world, origin, normal)
            # Elbows up off the ramp, not speared into it.
            pole = normal * 0.2 + Vector((0.3 if hand_side == "L" else -0.3, 0.2, 0.8))
            _pin_limb(arm, "hand", hand_side, world, normal, pole)
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
    return {
        "keys": keys,
        "capsule": caps,
        "facing": facings,
        "times": times,
        "skate": skate,
        "pen": pen,
        "note": "Both soles stay on the 36° ramp and move with the capsule. Skate is extra foot travel along the ramp.",
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


def solve_wallrun(arm, samples, times):
    from mathutils import Vector, Euler
    face = 0.0
    normal = Vector((1.0, 0.0, 0.0))  # out of the wall, toward the body
    n = len(samples)
    plants = _hysteresis_plants(samples, hold=3)
    keys = []
    caps = []
    facings = []
    skate = 0.0
    pen = 0.0
    lock = None
    lock_side = None
    plant_at = None
    for i, ch in enumerate(samples):
        u = i / (n - 1)
        # Upright, facing the wall (-X), a small lean in so the chest is near the face.
        facing = Euler((math.radians(10.0), 0.0, math.radians(-90.0)), "XYZ")
        # Climb about 1.7 m. The origin is the root, near the soles at rest,
        # so this is the height of the lower body, not the head.
        origin = Vector((0.48, 0.0, 0.02 + u * 1.65))
        _apply_sample(arm, ch, origin, facing)
        side = plants[i]
        if lock is None or side != lock_side:
            vert = _deepest(f"Foot_{side}", normal)
            if vert is not None:
                z = vert.z
                if lock is not None:
                    z = max(z, lock.z + 0.22)
                z = max(0.04, z)
                lock = Vector((face, 0.0, z))
                lock_side = side
                plant_at = None
        # Keep the chest off the wall, then put the plant back on its lock.
        _apply_sample(arm, ch, origin, facing)
        show_wall_world(face, max(2.1, origin.z + 1.2))
        pole = _pole_knee(lock_side, normal, Vector((0.0, 1.0, 0.0)))
        _pin_limb(arm, "foot", lock_side, lock, normal, pole)
        # Swing foot must not stay inside the wall. Park it just off the face
        # without holding a world lock, so it is not a sliding plant.
        other = "R" if lock_side == "L" else "L"
        swing = _deepest(f"Foot_{other}", normal)
        if swing is not None and swing.x < face + 0.01:
            park = Vector((face + 0.03, swing.y, max(swing.z, lock.z + 0.08)))
            _pin_limb(arm, "foot", other, park, normal, _pole_knee(other, normal, Vector((0.0, 1.0, 0.0))))
        # Chest clearance: push the capsule out and re-pin the plant.
        chest = _deepest("Chest", normal)
        if chest is not None and chest.x < face + 0.06:
            origin = Vector((origin.x + (face + 0.06 - chest.x), origin.y, origin.z))
            arm.location = origin
            bpy_update()
            _pin_limb(arm, "foot", lock_side, lock, normal, pole)
        vert = _deepest(f"Foot_{lock_side}", normal)
        if vert is not None:
            if plant_at is None:
                plant_at = vert.copy()
            delta = vert - plant_at
            along = Vector((0.0, delta.y, delta.z))
            skate = max(skate, along.length)
            pen = max(pen, max(0.0, face - vert.x))
        # A foot under the floor is not part of the climb.
        for side_name in ("L", "R"):
            sole = _deepest(f"Foot_{side_name}", Vector((0.0, 0.0, 1.0)))
            if sole is not None and sole.z < -0.002:
                arm.location.z -= sole.z
                bpy_update()
                _pin_limb(arm, "foot", lock_side, lock, normal, pole)
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
    return {
        "keys": keys,
        "capsule": caps,
        "facing": facings,
        "times": times,
        "skate": skate,
        "pen": pen,
        "plants": plants,
        "note": "L/R swapped from the behind-camera take. Each plant is a world lock on the vertical face. Clip 19 was not posed; its step rhythm is about 0.4 s and this clip's plants follow clip 18.",
    }


def solve_turn(arm, samples, times):
    from mathutils import Vector, Euler
    face = 0.0
    wall_n = Vector((1.0, 0.0, 0.0))
    floor_n = Vector((0.0, 0.0, 1.0))
    landing_top = 0.42
    landing_y = 1.55
    n = len(samples)
    keys = []
    caps = []
    facings = []
    skate = 0.0
    pen = 0.0
    # Ground plants during the run-in, then the wall plant, then the landing.
    run_plants = _hysteresis_plants(samples, hold=3)
    ground_lock = {}
    wall_lock = None
    wall_plant_at = None
    land_lock = {}
    for i, ch in enumerate(samples):
        t = times[i]
        # Yaw is preview only. 0 faces -Y (the way he ran in). pi faces back.
        if t < 0.85:
            yaw = 0.0
        elif t < 1.40:
            yaw = math.pi * _smoothstep((t - 0.85) / 0.55)
        else:
            yaw = math.pi
        facing = Euler((math.radians(6.0), 0.0, yaw), "XYZ")
        if t < 0.90:
            u = (t - times[0]) / max(1.0e-4, 0.90 - times[0])
            origin = Vector((0.62, 2.05 - u * 1.25, 0.0))
        elif t < 1.28:
            u = (t - 0.90) / 0.38
            origin = Vector((0.62 - u * 0.30, 0.80 - u * 0.10, 0.0 + u * 0.12))
        else:
            u = _clamp((t - 1.28) / 0.42, 0.0, 1.0)
            origin = Vector((0.32 + u * 0.10, 0.70 + u * 0.85, landing_top * _smoothstep(u)))
        _apply_sample(arm, ch, origin, facing)
        phase = "run" if t < 0.95 else ("plant" if t < 1.30 else "land")
        if phase == "run":
            side = run_plants[i]
            if side not in ground_lock:
                vert = _deepest(f"Foot_{side}", floor_n)
                if vert is not None:
                    ground_lock[side] = Vector((vert.x, vert.y, 0.0))
            # Drop a lock that the body has left so the next stride can plant.
            for s, lk in list(ground_lock.items()):
                if s != side and abs(origin.y - lk.y) > 0.45:
                    del ground_lock[s]
            if side in ground_lock:
                pole = _pole_knee(side, floor_n, Vector((1.0, 0.0, 0.0)))
                _pin_limb(arm, "foot", side, ground_lock[side], floor_n, pole)
                vert = _deepest(f"Foot_{side}", floor_n)
                if vert is not None:
                    delta = vert - ground_lock[side]
                    skate = max(skate, Vector((delta.x, delta.y, 0.0)).length)
                    pen = max(pen, max(0.0, -vert.z))
        elif phase == "plant":
            # The right leg is the one driven up onto the wall in this take.
            if wall_lock is None:
                vert = _deepest("Foot_R", wall_n)
                z = 0.85 if vert is None else _clamp(vert.z, 0.55, 1.15)
                wall_lock = Vector((face, origin.y, z))
                wall_plant_at = None
            pole = Vector((0.9, 0.1, 0.45))
            _pin_limb(arm, "foot", "R", wall_lock, wall_n, pole)
            vert = _deepest("Foot_R", wall_n)
            if vert is not None:
                if wall_plant_at is None:
                    wall_plant_at = vert.copy()
                delta = vert - wall_plant_at
                skate = max(skate, Vector((0.0, delta.y, delta.z)).length)
                pen = max(pen, max(0.0, face - vert.x))
            # The other foot leaves the ground. Keep it out of the floor.
            other = _deepest("Foot_L", floor_n)
            if other is not None and other.z < 0.02:
                park = Vector((other.x, other.y, 0.08))
                _pin_limb(arm, "foot", "L", park, floor_n, _pole_knee("L", floor_n, Vector((1.0, 0.0, 0.0))))
        else:
            if not land_lock:
                for side in ("L", "R"):
                    vert = _deepest(f"Foot_{side}", floor_n)
                    x = origin.x + (0.12 if side == "L" else -0.12)
                    y = landing_y + (0.06 if side == "L" else -0.05)
                    if vert is not None:
                        x = vert.x
                        y = _clamp(vert.y, landing_y - 0.25, landing_y + 0.25)
                    land_lock[side] = Vector((x, y, landing_top))
            for side in ("L", "R"):
                pole = _pole_knee(side, floor_n, Vector((1.0, 0.0, 0.0)))
                _pin_limb(arm, "foot", side, land_lock[side], floor_n, pole)
                vert = _deepest(f"Foot_{side}", floor_n)
                if vert is not None:
                    delta = vert - land_lock[side]
                    skate = max(skate, Vector((delta.x, delta.y, 0.0)).length)
                    pen = max(pen, max(0.0, landing_top - vert.z))
        show_turn_world(face, landing_top, landing_y)
        keys.append(_save_pose(arm))
        caps.append((arm.location.x, arm.location.y, arm.location.z))
        facings.append((facing.x, facing.y, facing.z))
    return {
        "keys": keys,
        "capsule": caps,
        "facing": facings,
        "times": times,
        "skate": skate,
        "pen": pen,
        "note": "Yaw is a preview rotation of the armature, not a Root key. The right foot locks on the wall, then both feet lock on the low wall.",
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
    from mathutils.bvhtree import BVHTree
    ev = ob.evaluated_get(deps)
    me = ev.to_mesh()
    me.transform(ev.matrix_world)
    verts = [v.co.copy() for v in me.vertices]
    polys = [tuple(p.vertices) for p in me.polygons]
    tree = BVHTree.FromPolygons(verts, polys) if polys else None
    ev.to_mesh_clear()
    return tree, verts


def _inside_depth(point, tree):
    loc, normal, _idx, dist = tree.find_nearest(point)
    if loc is None or normal is None:
        return 0.0
    # Outward normal: a point inside sits against the back of the nearest face.
    if normal.dot(point - loc) < -1.0e-6:
        return dist
    return 0.0


def _joint_world(arm, child_name):
    return p5._head_w(arm, child_name)


def _pair_depth(verts_a, tree_b, joint, exempt):
    worst = 0.0
    if tree_b is None:
        return 0.0
    for v in verts_a:
        if exempt and joint is not None and (v - joint).length <= JOINT_EXEMPT_M:
            continue
        worst = max(worst, _inside_depth(v, tree_b))
    return worst


def noclip_frame(arm):
    """Max world and self penetration on the evaluated meshes, in metres."""
    import bpy
    rh._ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    bodies = []
    for ob in _body_objects():
        tree, verts = _eval_bvh(ob, deps)
        bone = ob.parent_bone
        bodies.append((bone, tree, verts))
    world = 0.0
    for ob in _world_objects():
        tree, _verts = _eval_bvh(ob, deps)
        if tree is None:
            continue
        for _bone, _t, verts in bodies:
            for v in verts:
                world = max(world, _inside_depth(v, tree))
    by_bone = {b[0]: b for b in bodies}
    neighbor_of = {}
    for parent, child in NEIGHBORS:
        neighbor_of.setdefault(parent, set()).add(child)
        neighbor_of.setdefault(child, set()).add(parent)
    joints = {}
    for _parent, child in NEIGHBORS:
        joints[(child,)] = _joint_world(arm, child)
        # Keyed by the child, since that head is the shared joint.
        joints[frozenset((_parent if False else child,))] = joints[(child,)]
    self_pen = 0.0
    worst_pair = ""
    for i, (bone_a, tree_a, verts_a) in enumerate(bodies):
        for bone_b, tree_b, verts_b in bodies[i + 1:]:
            if tree_a is None or tree_b is None:
                continue
            if not tree_a.overlap(tree_b):
                continue
            neighbors = bone_b in neighbor_of.get(bone_a, ())
            joint = None
            if neighbors:
                # The child is whichever bone lists the other as parent.
                child = bone_b if bone_a in {p for p, c in NEIGHBORS if c == bone_b} else bone_a
                # Simpler: the joint is the head of the bone that is not the ancestor.
                if (bone_a, bone_b) in ((p, c) for p, c in NEIGHBORS):
                    child = bone_b
                elif (bone_b, bone_a) in ((p, c) for p, c in NEIGHBORS):
                    child = bone_a
                else:
                    child = bone_b
                joint = _joint_world(arm, child)
            da = _pair_depth(verts_a, tree_b, joint, neighbors)
            db = _pair_depth(verts_b, tree_a, joint, neighbors)
            depth = max(da, db)
            if depth > self_pen:
                self_pen = depth
                worst_pair = f"{bone_a}/{bone_b}"
    return world, self_pen, worst_pair


def pose_from_key(arm, solved, i, spec):
    import bpy
    from mathutils import Euler, Vector
    eulers = {name: deg for name, deg in solved["keys"][i].items()}
    rh._apply_eulers(arm, eulers)
    cap = solved["capsule"][i]
    arm.location = Vector(cap)
    arm.rotation_euler = Euler(solved["facing"][i], "XYZ")
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    _show_for(spec, arm)


def _show_for(spec, arm):
    if spec["verb"] == "slide":
        show_slide_world(arm.location.y)
    elif spec["verb"] == "wallrun_vertical":
        show_wall_world(0.0, max(2.1, arm.location.z + 1.2))
    else:
        show_turn_world(0.0, 0.42, 1.55)


def measure_noclip(arm, solved_all):
    frames = 0
    fails = 0
    world_max = 0.0
    self_max = 0.0
    worst = ""
    for spec in SPECS:
        solved = solved_all[spec["id"]]
        for i in range(len(solved["keys"])):
            pose_from_key(arm, solved, i, spec)
            world, self_pen, pair = noclip_frame(arm)
            frames += 1
            if world > world_max:
                world_max = world
            if self_pen > self_max:
                self_max = self_pen
                worst = f"{spec['id']} f={i} {pair}"
            if world > DEPTH_LIMIT_M or self_pen > DEPTH_LIMIT_M:
                fails += 1
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
    """Keep the sheet under 400 KB without inventing pixels. Quantize, then scale."""
    from PIL import Image
    im = Image.open(path).convert("RGB")
    def save(image):
        image.save(path, optimize=True)
        return os.path.getsize(path)
    if save(im) <= limit:
        return
    # A flat backdrop and a narrow palette still show the pose.
    q = im.quantize(colors=48, method=Image.Quantize.MEDIANCUT).convert("RGB")
    if save(q) <= limit:
        return
    scale = 0.86
    cur = q
    while scale >= 0.45:
        w = max(8, int(im.width * scale))
        h = max(8, int(im.height * scale))
        cur = im.resize((w, h), Image.Resampling.LANCZOS)
        cur = cur.quantize(colors=40, method=Image.Quantize.MEDIANCUT).convert("RGB")
        if save(cur) <= limit:
            return
        scale *= 0.86
    save(cur)


def composite(solved_all, noclip_line):
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
        # Match the strip's height to the grid so the two sit side by side.
        sw = int(round(strip.width * (grid_h / strip.height)))
        strip = strip.resize((sw, grid_h), Image.Resampling.LANCZOS)
        canvas = Image.new("RGB", (sw + 10 + grid_w, grid_h + 36), (32, 30, 28))
        canvas.paste(strip, (0, 36))
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
