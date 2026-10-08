#!/usr/bin/env python3
"""Pass 5: capsule-space Storror clips.

The exported clip is bone rotations only. Root and the armature stay at the
origin, which is what the game plays. The preview moves the capsule (armature
location) from the scaled Storror root path and pins feet and palms with
two-bone IK. That location is not a root-motion key.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import storror_pose as sp
import render_hier_v080_stills as rh

OUT = os.path.join(rh.OUT, "pass5")
PREV = os.path.join(rh.PREV, "pass5")
HIER_LEG = 0.910  # hip joint to ankle, v0.8.0
HIP_Z = 0.93
BLEND_FRAMES = 3
PIN_BAND = 0.04
# Eight story frames per clip (source frame index). The hero contact is included.
BEATS = {
    "03_wall_drop_softland": [0, 9, 15, 21, 27, 30, 36, 48],
    "04_window_drop_softland": [0, 8, 14, 20, 25, 32, 40, 50],
    "10_wallrun_slanted": [7, 21, 35, 49, 63, 77, 88, 112],
    "12_tictac_slanted_wall": [8, 20, 28, 34, 42, 55, 80, 130],
    "15_cat_leap_wall": [16, 48, 64, 72, 79, 84, 96, 140],
}


def _args():
    if "--" in sys.argv:
        return sys.argv[sys.argv.index("--") + 1:]
    return sys.argv[1:]


def _smooth(vals, radius=4):
    n = len(vals)
    out = []
    for i in range(n):
        acc = 0.0
        c = 0
        for j in range(max(0, i - radius), min(n, i + radius + 1)):
            acc += vals[j]
            c += 1
        out.append(acc / c)
    return out


def _leg_scale(data):
    legs = []
    for w in data["world_xyz_m"]:
        if not w:
            continue
        hip = (
            0.5 * (w[23][0] + w[24][0]),
            0.5 * (w[23][1] + w[24][1]),
            0.5 * (w[23][2] + w[24][2]),
        )
        for a in (27, 28):
            d = math.dist(hip, w[a])
            if d > 0.2:
                legs.append(d)
    legs.sort()
    med = legs[len(legs) // 2]
    return HIER_LEG / med


def root_path(spec):
    """Forward travel and rise, in Hier metres, from the image hip velocity.

    root_vel_proxy is metres/second of the hip in the image, scaled by the
    MediaPipe torso. Multiplying by the leg-length ratio puts it on the Hier
    body. Image-right is forward. Image-down integrated and flipped is rise.
    """
    data = sp.load_clip(spec["id"])
    scale = _leg_scale(data)
    fps = float(data["fps"])
    vel = data["root_vel_proxy_mps"]
    fwd = 0.0
    rise = 0.0
    raw_f = []
    raw_r = []
    for v in vel:
        if v:
            fwd += float(v[0]) / fps * scale
            rise += -float(v[1]) / fps * scale
        raw_f.append(fwd)
        raw_r.append(rise)
    return {
        "scale": scale,
        "fps": fps,
        "fwd": _smooth(raw_f),
        "rise": _smooth(raw_r),
        "height": [None if h is None else float(h) * scale for h in data["root_height_above_feet_m"]],
        "frames": data["frame_count"],
    }


def _u(i, n):
    if n <= 1:
        return 0.0
    return i / (n - 1)


def _hero(spec, smooth):
    n = len(smooth["knee_flex_L"])
    if spec.get("hero_i") is not None and n:
        i = max(0, min(n - 1, int(spec["hero_i"])))
        return (0.0 if n <= 1 else i / (n - 1)), i
    u, idx = sp.hero_u(spec["id"], smooth)
    return u, idx


def _facing_at(facing, i):
    if callable(facing):
        return facing(i)
    return facing


def _facing_for(spec, smooth):
    u, idx = _hero(spec, smooth)
    raw = sp.load_clip(spec["id"])
    ang = raw["joint_angles_deg"][idx] or {}
    trunk = float(ang.get("trunk_lean_from_cam_vertical") or 0.0)
    return rh._facing_euler(spec, trunk)


def _pose_frame(arm, smooth, i, facing, capsule, spec=None):
    """Curve pose. Capsule location is preview-only; Root stays put."""
    import bpy
    n = len(smooth["knee_flex_L"])
    ch = sp.sample_raw(smooth, _u(i, n))
    rh._apply_eulers(arm, sp.blender_euler(sp.unity_pose(ch)))
    arm.location = capsule
    arm.rotation_euler = _facing_at(facing, i)
    # Optional whole-body yaw on Hips (local Y is up). Not a Root key.
    if spec is not None and spec.get("hip_yaw") is not None:
        hips = arm.pose.bones.get("Hips")
        if hips is not None:
            hips.rotation_mode = "XYZ"
            e = hips.rotation_euler
            extra = spec["hip_yaw"](i) if callable(spec["hip_yaw"]) else spec["hip_yaw"]
            hips.rotation_euler = (e.x, e.y + math.radians(float(extra)), e.z)
    root = arm.pose.bones.get("Root")
    if root is not None:
        root.location = (0.0, 0.0, 0.0)
        root.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    return sp.unity_pose(ch)


def _bone_len(arm, name):
    b = arm.data.bones[name]
    return (b.tail_local - b.head_local).length


def _head_w(arm, name):
    from mathutils import Vector
    pb = arm.pose.bones[name]
    return arm.matrix_world @ Vector(pb.head)


def _tail_w(arm, name):
    from mathutils import Vector
    pb = arm.pose.bones[name]
    return arm.matrix_world @ Vector(pb.tail)


def _point_bone(arm, name, target_world):
    """Minimal rotation that aims the bone's Y axis at a world point."""
    import bpy
    from mathutils import Vector, Matrix
    pb = arm.pose.bones[name]
    pb.rotation_mode = "XYZ"
    head = _head_w(arm, name)
    tail = _tail_w(arm, name)
    current = tail - head
    desired = Vector(target_world) - head
    if current.length < 1.0e-6 or desired.length < 1.0e-6:
        return
    rot = current.rotation_difference(desired)
    # Rotate about the bone head. A world-origin rotation walks the joint
    # off the body and the next iteration amplifies it.
    mw = arm.matrix_world
    T = Matrix.Translation(head)
    world = mw @ pb.matrix
    new_world = T @ rot.to_matrix().to_4x4() @ T.inverted() @ world
    pb.matrix = mw.inverted() @ new_world
    bpy.context.view_layer.update()

def _solve_chain(arm, upper, lower, target_world, pole_hint=None):
    """Two-bone IK. pole_hint is a world-space bend direction."""
    from mathutils import Vector
    S = _head_w(arm, upper)
    K = _tail_w(arm, upper)
    E = _tail_w(arm, lower)
    L1 = max((K - S).length, 1.0e-4)
    L2 = max((E - K).length, 1.0e-4)
    if pole_hint is not None:
        pole = Vector(pole_hint)
        prefer_out = pole.x > 0.0
    else:
        prefer_out = False
        axis = E - S
        if axis.length < 1.0e-5:
            pole = Vector((0.0, -1.0, 0.0))
        else:
            axis_n = axis.normalized()
            pole = (K - S) - axis_n * axis_n.dot(K - S)
            if pole.length < 1.0e-4:
                pole = Vector((0.0, -1.0, 0.2))
    if pole.length < 1.0e-6:
        pole = Vector((0.0, -1.0, 0.2))
    pole = pole.normalized()
    to = Vector(target_world) - S
    if to.length < 1.0e-5:
        return False
    # Keep the pole off the bone axis so the knee does not flip flat.
    axis_n = to.normalized()
    pole = pole - axis_n * axis_n.dot(pole)
    if prefer_out and pole.x < 0.15:
        # Push the knee or the elbow off the wall without flipping the bend
        # from up to down.
        pole = pole + Vector((0.85, 0.0, 0.0))
        pole = pole - axis_n * axis_n.dot(pole)
    if pole.length < 1.0e-4:
        pole = Vector((0.0, -1.0, 0.2))
        pole = pole - axis_n * axis_n.dot(pole)
    pole = pole.normalized()
    dist = min(max(to.length, abs(L1 - L2) + 1.0e-4), L1 + L2 - 1.0e-4)
    end = S + to.normalized() * dist
    cos_hip = (L1 * L1 + dist * dist - L2 * L2) / (2.0 * L1 * dist)
    cos_hip = max(-1.0, min(1.0, cos_hip))
    along = L1 * cos_hip
    height = math.sqrt(max(0.0, L1 * L1 - along * along))
    knee = S + (end - S).normalized() * along + pole * height
    _point_bone(arm, upper, knee)
    _point_bone(arm, lower, end)
    return True


def _chain(kind, side):
    if kind == "foot":
        return (f"UpperLeg_{side}", f"LowerLeg_{side}", f"Foot_{side}")
    return (f"UpperArm_{side}", f"LowerArm_{side}", f"Hand_{side}")


def _token(kind, side):
    return f"Foot_{side}" if kind == "foot" else f"Hand_{side}"


def _verts(token):
    return list(rh._iter_world_verts(lambda n, token=token: token in n))


def _foot_point(token, kind_surface):
    """Deepest foot vertex into the surface we are pinning against."""
    verts = _verts(token)
    if not verts:
        return None
    if kind_surface == "floor":
        return min(verts, key=lambda v: v.z)
    return min(verts, key=lambda v: v.x)


def _palm_point(arm, side, lock):
    cluster = rh._palm_cluster(arm, side)
    if not cluster:
        return None
    return min(cluster, key=lambda v: (v - lock).length)


def _pinned_vertex(arm, kind, side, surface, lock):
    """Vertex that is actually on the lock, so a new deepest corner is not slide."""
    from mathutils import Vector
    if kind == "hand":
        cluster = rh._palm_cluster(arm, side) or []
        return min(cluster, key=lambda v: (v - Vector(lock)).length) if cluster else None
    verts = _verts(_token(kind, side))
    if not verts:
        return None
    if surface == "floor":
        near = [v for v in verts if abs(v.z - lock.z) < 0.025] or verts
    else:
        near = [v for v in verts if abs(v.x - lock.x) < 0.025] or verts
    return min(near, key=lambda v: (v - Vector(lock)).length)


def _contact_point(arm, kind, side, surface, lock):
    if kind == "hand":
        return _palm_point(arm, side, lock)
    return _foot_point(_token(kind, side), surface)


def _save_eulers(arm, names):
    saved = {}
    for name in names:
        pb = arm.pose.bones[name]
        pb.rotation_mode = "XYZ"
        saved[name] = pb.rotation_euler.copy()
    return saved


def _blend_eulers(arm, saved_from, saved_to, names, w):
    """Blend local XYZ. w = 1 keeps the IK pose."""
    import bpy
    if w >= 0.999:
        return
    for name in names:
        pb = arm.pose.bones[name]
        a = saved_from[name]
        b = saved_to[name]
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = tuple((1.0 - w) * a[i] + w * b[i] for i in range(3))
    bpy.context.view_layer.update()


def _seat_end(arm, bone, point_fn, lock):
    """Rotate the end bone so `point_fn()` lands on the lock. One swing, capped."""
    import bpy
    from mathutils import Vector, Quaternion, Matrix
    joint = _head_w(arm, bone)
    vert = point_fn()
    if vert is None:
        return
    cur = vert - joint
    want = Vector(lock) - joint
    if cur.length < 1.0e-5 or want.length < 1.0e-5:
        return
    rot = cur.rotation_difference(want)
    ang = rot.angle
    if ang < 0.008:
        return
    cap = math.radians(75.0)
    if ang > cap:
        rot = Quaternion().slerp(rot, cap / ang)
    pb = arm.pose.bones[bone]
    pb.rotation_mode = "XYZ"
    mw = arm.matrix_world
    T = Matrix.Translation(joint)
    world = mw @ pb.matrix
    new_world = T @ rot.to_matrix().to_4x4() @ T.inverted() @ world
    pb.matrix = mw.inverted() @ new_world
    bpy.context.view_layer.update()


def _apply_pin(arm, kind, side, lock, surface, pole):
    """Two-bone IK plus an end-bone seat so the mesh, not the joint, hits lock."""
    from mathutils import Vector
    upper, lower, end = _chain(kind, side)
    normal = Vector((0.0, 0.0, 1.0)) if surface == "floor" else Vector((1.0, 0.0, 0.0))
    nudge = Vector((0.0, 0.0, 0.0))

    def point():
        return _contact_point(arm, kind, side, surface, lock)

    for _ in range(6):
        vert = point()
        if vert is None:
            return
        joint = _head_w(arm, end)
        _solve_chain(arm, upper, lower, lock + (joint - vert) + nudge, pole)
        _seat_end(arm, end, point, lock)
        seated = point()
        if seated is None:
            return
        err_vec = Vector(lock) - seated
        normal_err = err_vec.dot(normal)
        tangent = err_vec - normal * normal_err
        if abs(normal_err) < 0.004 and tangent.length < 0.012:
            break
        nudge += normal * normal_err
    # A short second pass pulls the contact along the surface. The step is
    # clamped so it cannot walk the limb through the plane.
    for _ in range(3):
        vert = point()
        if vert is None:
            return
        err_vec = Vector(lock) - vert
        normal_err = err_vec.dot(normal)
        tangent = err_vec - normal * normal_err
        if abs(normal_err) < 0.006 and tangent.length < 0.012:
            break
        if tangent.length > 0.04:
            tangent = tangent.normalized() * 0.04
        joint = _head_w(arm, end)
        _solve_chain(arm, upper, lower, lock + (joint - vert) + tangent + normal * normal_err, pole)
        _seat_end(arm, end, point, lock)


def _limb_keys(spec):
    keys = [("foot", "L"), ("foot", "R")]
    if spec["verb"] in ("cling", "slide"):
        keys.extend((("hand", "L"), ("hand", "R")))
    return keys


def _pole_for(spec, kind, side):
    from mathutils import Vector
    if spec["verb"] == "cling" and kind == "foot":
        # Knees up and out, off the wall, so the tuck clears the face.
        y = 0.25 if side == "L" else -0.25
        return Vector((0.35, y, 1.40))
    if spec["verb"] == "cling" and kind == "hand":
        # Elbows drop below the lip instead of spearing the face.
        y = 0.25 if side == "L" else -0.25
        return Vector((0.2, y, -0.9))
    if kind == "foot" and spec["verb"] in ("wallrun", "walljump", "vwall", "turn180"):
        y = 0.2 if side == "L" else -0.2
        return Vector((1.0, y, 0.35))
    return None


def _hero_locals(arm, spec, smooth, facing, hero_i):
    """Limb and torso positions of the hero pose, armature at the origin."""
    from mathutils import Vector
    _pose_frame(arm, smooth, hero_i, facing, Vector((0.0, 0.0, 0.0)), spec)
    pose = sp.unity_pose(sp.sample_raw(smooth, _u(hero_i, len(smooth["knee_flex_L"]))))
    loc = {"foot": {}, "foot_z": {}, "palm": {}, "palm_z": {}, "chest": None, "head": None, "pose": pose}
    for side in ("L", "R"):
        fx = _foot_point(f"Foot_{side}", "wall")
        fz = _foot_point(f"Foot_{side}", "floor")
        loc["foot"][side] = fx
        loc["foot_z"][side] = fz.z if fz is not None else 0.0
        cluster = rh._palm_cluster(arm, side)
        if cluster:
            palm = min(cluster, key=lambda v: v.x)
            loc["palm"][side] = palm
            loc["palm_z"][side] = palm.z
    cx, _ = rh._mesh_min_x(lambda n: "Chest" in n or "Spine" in n or "Hips" in n)
    hx, _ = rh._mesh_min_x(lambda n: "Head" in n)
    loc["chest"] = cx
    loc["head"] = hx
    loc["hip"] = _head_w(arm, "Hips").copy()
    return loc


def _fit_path(spec, path, hero_i, loc):
    """Preview capsule positions. Bone keys stay in the capsule; this does not.

    Forward travel is the scaled image-hip integral. Rise is the same integral
    flipped to up. Soft lands use the scaled hip-to-foot length as hip height,
    because that channel is the crouch and the image rise is mostly camera.
    """
    from mathutils import Vector
    custom = spec.get("fit")
    if custom is not None:
        return custom(spec, path, hero_i, loc)
    n = path["frames"]
    fwd = path["fwd"]
    rise = path["rise"]
    fx = [fwd[i] - fwd[hero_i] for i in range(n)]
    rz = [rise[i] - rise[hero_i] for i in range(n)]
    pose = loc["pose"]
    face = None
    lip_z = None
    capsule = []

    if spec["verb"] == "softland":
        heights = [HIP_Z if h is None else h for h in path["height"]]
        heights = _smooth(heights, 3)
        for i in range(n):
            hip = max(0.50, min(1.22, heights[i]))
            capsule.append(Vector((fx[i] * 0.35, 0.0, hip - HIP_Z)))
        return capsule, None, None, {}

    pre = [fx[i] for i in range(max(hero_i, 1))]
    # Body stays on the +X side of the wall. Flip a path that approaches from -X.
    if sum(pre) / len(pre) < 0.0:
        fx = [-v for v in fx]
    span = max(fx) - min(fx)
    if spec["verb"] in ("cling", "walljump") and 1.0e-3 < span < 0.85:
        gain = 0.85 / span
        fx = [v * gain for v in fx]

    face = 0.0
    if spec["verb"] == "wallrun":
        side = "L" if abs(pose["knee_l"]) <= abs(pose["knee_r"]) else "R"
        foot = loc["foot"][side]
        chest = loc["chest"] if loc["chest"] is not None else foot.x + 0.16
        # Hero plant on the face. Other frames share this normal offset; the
        # along-wall travel is the root integral. Chest clearance is a later push.
        base_x = face - foot.x
        foot_z = loc["foot_z"][side]
        base_z = (0.32 - foot_z) if foot_z < 0.32 else 0.0
        for i in range(n):
            capsule.append(Vector((base_x, fx[i], base_z + rz[i])))
        return capsule, face, None, {"plant": side}

    if spec["verb"] == "walljump":
        side = "L" if pose["thigh_l"] >= pose["thigh_r"] else "R"
        foot = loc["foot"][side]
        # Kick foot on the face. The chest stays back: this is a lean-away kick.
        base_x = face - foot.x
        foot_z = loc["foot_z"][side]
        base_z = HIP_Z - foot_z
        chest = loc["chest"] if loc["chest"] is not None else -0.2
        min_x = face + 0.04 - chest
        kick0 = max(0, hero_i - 4)
        kick1 = min(n - 1, hero_i + 4)
        for i in range(n):
            x = max(base_x + fx[i], min_x)
            z = base_z + rz[i]
            if kick0 <= i <= kick1:
                z = base_z
            capsule.append(Vector((x, 0.0, z)))
        return capsule, face, None, {"kick": side}

    # Cat leap. Palms define the lip. The hang holds the capsule on that lip
    # while the approach and the exit follow the root integral.
    palms = [p for p in loc["palm"].values() if p is not None]
    palm = min(palms, key=lambda v: v.x) if palms else loc["foot"]["L"]
    lip_z = sum(p.z for p in palms) / len(palms) if palms else palm.z
    chest = loc["chest"] if loc["chest"] is not None else palm.x + 0.3
    # Close enough for the palms to meet the lip, far enough that a 1 cm
    # chest margin is still possible. The later push enforces the margin.
    contact_x = max(face - palm.x, face + 0.10 - chest)
    hang0 = max(0, hero_i - 8)
    hang1 = min(n - 1, hero_i + 8)
    for i in range(n):
        x = contact_x + fx[i]
        if hang0 <= i <= hang1:
            x = contact_x
        else:
            x = max(x, contact_x)
        capsule.append(Vector((x, 0.0, rz[i])))
    return capsule, face, lip_z, {"hang": (hang0, hang1), "foot_z": 0.50}


def _forced_target(spec, key, i, hero_i, pt, face, lip_z, meta):
    """World point a story contact has to hold, or None to use the 4 cm test."""
    from mathutils import Vector
    custom = spec.get("force")
    if custom is not None:
        return custom(spec, key, i, hero_i, pt, face, lip_z, meta)
    kind, side = key
    if spec["verb"] == "cling" and abs(i - hero_i) <= 8:
        if kind == "hand" and lip_z is not None:
            return Vector((face, pt.y, lip_z)), "lip"
        if kind == "foot":
            return Vector((face, pt.y, meta.get("foot_z", 0.50))), "wall"
    if spec["verb"] == "walljump" and kind == "foot" and side == meta.get("kick") and abs(i - hero_i) <= 4:
        return Vector((face, pt.y, HIP_Z)), "wall"
    return None, None


def _surface_of(spec, kind, face, lip_z):
    """Default surface when the limb is simply near something."""
    from mathutils import Vector
    if spec["verb"] == "softland" or face is None:
        return "floor", Vector((0.0, 0.0, 1.0)), Vector((0.0, 0.0, 0.0))
    if kind == "hand" and lip_z is not None:
        return "lip", Vector((1.0, 0.0, 0.0)), Vector((face, 0.0, lip_z))
    if kind == "foot" and spec["verb"] == "cling":
        return "wall", Vector((1.0, 0.0, 0.0)), Vector((face, 0.0, 1.0))
    if kind == "foot" and spec["verb"] in ("wallrun", "vwall"):
        return "wall", Vector((1.0, 0.0, 0.0)), Vector((face, 0.0, 1.0))
    # Tic-tac trail foot can meet the floor. The kick is forced onto the face.
    return "floor", Vector((0.0, 0.0, 1.0)), Vector((0.0, 0.0, 0.0))


def _project(point, surface, origin, normal, face, lip_z):
    from mathutils import Vector
    if surface == "lip":
        return Vector((face, point.y, lip_z))
    d = (point - origin).dot(normal)
    return point - normal * d


def _signed_dist(point, surface, origin, normal, face, lip_z):
    if surface == "floor":
        return point.z - origin.z
    if surface == "wall":
        return point.x - face
    return math.hypot(point.x - face, point.z - lip_z) * (1.0 if point.x >= face else -1.0)


def _blank_pin():
    return {"lock": None, "w": 0.0, "kind": None, "normal": None, "floor_lifts": 0, "plant_at": None}


def _reach(arm, kind, side, lock):
    upper, lower, end = _chain(kind, side)
    start = _head_w(arm, upper)
    L = _bone_len(arm, upper) + _bone_len(arm, lower)
    return (start - lock).length <= L - 0.02


def _scan(lip_z, face):
    """One depsgraph pass: floor pen, wall pen, torso and head clearance."""
    import bpy
    rh._ensure_object()
    deps = bpy.context.evaluated_depsgraph_get()
    floor = 0.0
    deepest = None
    torso = None
    head = None
    limb = None
    hand_x = None
    foot_deep = None
    for obj in bpy.data.objects:
        if not rh._body_mesh(obj):
            continue
        name = obj.name
        ev = obj.evaluated_get(deps)
        mw = ev.matrix_world
        is_hand = "Hand_" in name
        is_foot = "Foot_" in name
        is_torso = ("Chest" in name) or ("Hips" in name) or ("Spine" in name)
        is_head = "Head" in name
        is_limb = ("Leg_" in name) or ("Arm_" in name)
        for v in ev.data.vertices:
            w = mw @ v.co
            if -w.z > floor:
                floor = -w.z
            if is_torso and (torso is None or w.x < torso):
                torso = w.x
            if is_head and (head is None or w.x < head):
                head = w.x
            if is_limb and (limb is None or w.x < limb):
                limb = w.x
            if is_hand and (lip_z is None or w.z <= lip_z + 0.01):
                if hand_x is None or w.x < hand_x:
                    hand_x = w.x
            if face is None or is_hand:
                continue
            if lip_z is not None and w.z > lip_z + 0.01:
                continue
            if is_foot:
                if foot_deep is None or w.x < foot_deep:
                    foot_deep = w.x
                continue
            if deepest is None or w.x < deepest:
                deepest = w.x
    wall = 0.0
    if face is not None:
        for v in (deepest, foot_deep):
            if v is not None:
                wall = max(wall, face - v)
    return floor, wall, torso, head, limb, hand_x, foot_deep


def _keep_clear(arm, face, lip_z, cap):
    """Push the preview capsule +X until the torso and the head are outside."""
    import bpy
    from mathutils import Vector
    for _ in range(6):
        _floor, _wall, torso, head, limb, hand_x, foot_deep = _scan(lip_z, face)
        worst = None
        for v in (torso, head, limb, hand_x, foot_deep):
            if v is None:
                continue
            if worst is None or v < worst:
                worst = v
        if worst is None or worst >= face + 0.010:
            break
        cap = Vector((cap.x + (face + 0.012 - worst), cap.y, cap.z))
        arm.location = cap
        bpy.context.view_layer.update()
    return cap


def _lift_floor(arm, cap):
    """Keep every mesh on or above z = 0. Preview motion only."""
    import bpy
    from mathutils import Vector
    floor, _wall, _torso, _head, _limb, _hand, _foot = _scan(None, None)
    if floor > 0.001:
        cap = Vector((cap.x, cap.y, cap.z + floor))
        arm.location = cap
        bpy.context.view_layer.update()
    return cap


def _unstick_feet(arm, face, pins, spec, cap):
    """A pinned foot that is still through the face gets another solve, then a push."""
    import bpy
    from mathutils import Vector
    if face is None:
        return cap
    for _ in range(2):
        worst = None
        for side in ("L", "R"):
            pt = _foot_point(f"Foot_{side}", "wall")
            if pt is not None and (worst is None or pt.x < worst):
                worst = pt.x
        if worst is None or worst >= face - 0.008:
            return cap
        for key, st in pins.items():
            if key[0] != "foot" or st["w"] < 0.5 or st["lock"] is None:
                continue
            if st["kind"] != "wall":
                continue
            kind, side = key
            _apply_pin(arm, kind, side, st["lock"], st["kind"], _pole_for(spec, kind, side))
        worst = None
        for side in ("L", "R"):
            pt = _foot_point(f"Foot_{side}", "wall")
            if pt is not None and (worst is None or pt.x < worst):
                worst = pt.x
        if worst is None or worst >= face - 0.008:
            return cap
        cap = Vector((cap.x + (face + 0.002 - worst), cap.y, cap.z))
        arm.location = cap
        bpy.context.view_layer.update()
    return cap


def _settle_world(arm, face, lip_z, cap):
    """Push the preview capsule out of the ground and the wall.

    Bone rotations are not touched. The wall contact is the +X face. A vertex
    above a cat-leap lip is over the wall, not inside it.
    """
    import bpy
    from mathutils import Vector
    bpy.context.view_layer.update()
    for _ in range(4):
        floor, wall, _torso, _head, _limb, _hand, foot_deep = _scan(lip_z, face)
        moved = False
        if floor > 0.002:
            cap = Vector((cap.x, cap.y, cap.z + floor))
            moved = True
        if face is not None and wall > 0.003:
            # _scan's wall depth is how far a vertex sits past the face.
            cap = Vector((cap.x + wall + 0.001, cap.y, cap.z))
            moved = True
        if not moved:
            break
        arm.location = cap
        bpy.context.view_layer.update()
    return cap


def solve_clip(arm, spec, smooth, facing, path):
    """Pose every frame in the moving capsule and pin near contacts."""
    import bpy
    from mathutils import Vector
    n = path["frames"]
    _hero_u, hero_i = _hero(spec, smooth)
    loc = _hero_locals(arm, spec, smooth, facing, hero_i)
    capsule, face, lip_z, meta = _fit_path(spec, path, hero_i, loc)
    pins = {key: _blank_pin() for key in _limb_keys(spec)}
    slide = {key: 0.0 for key in pins}
    pen = 0.0
    torso_clear = 1.0e6
    head_clear = 1.0e6
    keys = []
    plants = []
    beats = set(_beats(spec, n, hero_i))
    for i in range(n):
        cap = capsule[i]
        _pose_frame(arm, smooth, i, facing, cap, spec)
        # Curve eulers, so a partial pin can blend back toward the clip.
        names = [pb.name for pb in arm.pose.bones if pb.name != "Root"]
        curve_e = _save_eulers(arm, names)
        for key in pins:
            kind, side = key
            st = pins[key]
            if kind == "hand" and lip_z is not None and face is not None:
                pt = _palm_point(arm, side, Vector((face, 0.0, lip_z)))
            else:
                on_floor = spec["verb"] in ("softland", "slide")
                pt = _foot_point(_token(kind, side), "floor" if on_floor else "wall")
            if pt is None:
                continue
            forced, fkind = _forced_target(spec, key, i, hero_i, pt, face, lip_z, meta)
            if forced is not None:
                surface = fkind
                proj = forced
                if surface == "floor":
                    normal = Vector((0.0, 0.0, 1.0))
                else:
                    normal = Vector((1.0, 0.0, 0.0))
                dist = _signed_dist(pt, "wall" if surface != "floor" else "floor", Vector((0, 0, 0)), normal, face or 0.0, lip_z or 0.0)
                if surface == "lip":
                    dist = -1.0  # the hang is the contact, even if the raw hand is short
            else:
                surface, normal, origin = _surface_of(spec, kind, face, lip_z)
                sample = pt
                # A tic-tac foot that is through the face has to pin there, not
                # only when it is the kick. The trail foot still prefers the floor.
                if spec["verb"] == "walljump" and kind == "foot" and face is not None:
                    sole = _foot_point(_token(kind, side), "floor") or pt
                    if (pt.x - face) < sole.z:
                        surface = "wall"
                        normal = Vector((1.0, 0.0, 0.0))
                        origin = Vector((face, 0.0, 1.0))
                        sample = pt
                if surface == "floor":
                    sample = _foot_point(_token(kind, side), "floor") or pt
                elif surface == "lip":
                    sample = _palm_point(arm, side, Vector((face, pt.y, lip_z))) or pt
                dist = _signed_dist(sample, surface, origin, normal, face or 0.0, lip_z or 0.0)
                proj = _project(sample, surface, origin, normal, face or 0.0, lip_z or 0.0)
            # A plant the stride has left is released. A plant the leg can no
            # longer reach is dropped so this frame can take a new lock.
            if st["lock"] is not None and forced is None:
                along_len = 0.0
                if dist >= 0.0:
                    delta = sample - st["lock"]
                    along = delta - normal * delta.dot(normal)
                    along_len = along.length
                if along_len > 0.08 or not _reach(arm, kind, side, st["lock"]):
                    st["lock"] = None
                    st["w"] = 0.0
            catching = forced is not None or dist < PIN_BAND
            if catching and st["lock"] is not None and not _reach(arm, kind, side, st["lock"]):
                st["lock"] = None
                st["w"] = 0.0
            if catching and (st["lock"] is None or (forced is not None and st["kind"] != surface)):
                # New contact, or the kick/hang takes over from a floor plant.
                if not _reach(arm, kind, side, proj) and forced is None:
                    catching = False
                else:
                    st["lock"] = proj.copy()
                    st["kind"] = surface
                    st["normal"] = normal.copy()
                    st["floor_lifts"] = 0
                    st["plant_at"] = None
                    # A story contact (forced) is on immediately. A near-miss still blends.
                    st["w"] = 1.0 if dist < 0.0 or forced is not None else 1.0 / BLEND_FRAMES
            elif forced is not None and st["lock"] is not None and (
                abs(st["lock"].x - proj.x) > 0.02 or abs(st["lock"].z - proj.z) > 0.05
            ):
                # The hang and the kick are a new plant, not a drag of the old one.
                st["lock"] = proj.copy()
                st["kind"] = surface
                st["normal"] = normal.copy()
                st["floor_lifts"] = 0
                st["plant_at"] = None
                st["w"] = 1.0
            elif catching:
                step = 1.0 if dist < 0.0 else 1.0 / BLEND_FRAMES
                st["w"] = min(1.0, st["w"] + step)
            elif st["lock"] is not None:
                st["w"] = st["w"] - 1.0 / BLEND_FRAMES
                if st["w"] <= 0.0:
                    st.update(_blank_pin())
            # A slide contact travels with the foot. Update the lock every
            # frame so the sole stays down without sticking to the ground.
            if spec.get("sliding_feet") and forced is not None and st.get("kind") == "floor" and st.get("lock") is not None:
                st["lock"] = proj.copy()
                st["normal"] = normal.copy()
                st["w"] = 1.0
            if st["w"] > 0.0 and st["lock"] is not None:
                _apply_pin(arm, kind, side, st["lock"], st["kind"], _pole_for(spec, kind, side))
                ik_e = _save_eulers(arm, names)
                _blend_eulers(arm, curve_e, ik_e, list(_chain(kind, side)), st["w"])
        if face is not None:
            moved = cap.copy()
            cap = _keep_clear(arm, face, lip_z, cap)
            if (cap - moved).length > 1.0e-4:
                for key, st in pins.items():
                    if st["w"] > 0.0 and st["lock"] is not None:
                        kind, side = key
                        _apply_pin(arm, kind, side, st["lock"], st["kind"], _pole_for(spec, kind, side))
                        ik_e = _save_eulers(arm, names)
                        _blend_eulers(arm, curve_e, ik_e, list(_chain(kind, side)), st["w"])
        # Anything still under the floor is lifted with the capsule. Wall and
        # lip locks are solved again so the plant stays put in the world.
        for _ in range(2):
            moved = cap.copy()
            cap = _lift_floor(arm, cap)
            if (cap - moved).length <= 1.0e-4:
                break
            for key, st in pins.items():
                if st["w"] > 0.0 and st["lock"] is not None and st["kind"] != "floor":
                    kind, side = key
                    _apply_pin(arm, kind, side, st["lock"], st["kind"], _pole_for(spec, kind, side))
                    ik_e = _save_eulers(arm, names)
                    _blend_eulers(arm, curve_e, ik_e, list(_chain(kind, side)), st["w"])
        capsule[i] = cap
        bpy.context.view_layer.update()
        floor_pen, wall_pen, torso, head, _limb, _hand, _foot = _scan(lip_z, face)
        for _ in range(3):
            if floor_pen <= 0.008:
                break
            lifted = False
            for key, st in pins.items():
                if st["w"] > 0.5 and st["lock"] is not None and st["kind"] == "wall":
                    st["lock"] = Vector((st["lock"].x, st["lock"].y, st["lock"].z + floor_pen))
                    st["plant_at"] = None
                    kind, side = key
                    _apply_pin(arm, kind, side, st["lock"], st["kind"], _pole_for(spec, kind, side))
                    lifted = True
            if not lifted:
                # Nothing planted on the wall is holding the body under the floor.
                cap = _lift_floor(arm, cap)
                break
            floor_pen, wall_pen, torso, head, _limb, _hand, _foot = _scan(lip_z, face)
        cap = _unstick_feet(arm, face, pins, spec, cap)
        capsule[i] = cap
        if spec.get("sliding_feet"):
            # The floor lift carries pinned feet with the capsule. Put them back
            # on the moving contact, then lift once more if the torso is still through.
            for _ in range(2):
                for key, st in pins.items():
                    if st.get("kind") != "floor" or st.get("w", 0) < 0.5 or st.get("lock") is None:
                        continue
                    kind, side = key
                    _apply_pin(arm, kind, side, st["lock"], st["kind"], _pole_for(spec, kind, side))
                    ik_e = _save_eulers(arm, names)
                    _blend_eulers(arm, curve_e, ik_e, list(_chain(kind, side)), st["w"])
                floor_now, _w, _t, _h, _l, _ha, _f = _scan(lip_z, face)
                if floor_now <= 0.008:
                    break
                cap = _lift_floor(arm, cap)
                capsule[i] = cap
        # World solids are cleared by moving the preview capsule. Bone angles
        # stay on the pose track. Straightening a limb to dodge overlap is
        # not allowed here.
        cap = _settle_world(arm, face, lip_z, cap)
        capsule[i] = cap
        floor_pen, wall_pen, torso, head, _limb, _hand, _foot = _scan(lip_z, face)
        if spec["verb"] in ("softland", "slide"):
            frame_pen = floor_pen
        else:
            frame_pen = max(floor_pen, wall_pen)
        if frame_pen > pen and frame_pen > 0.008:
            fl = _foot_point("Foot_L", "wall")
            fr = _foot_point("Foot_R", "wall")
            print(
                f"WORST {spec['id']} f={i} pen={frame_pen*100:.2f} floor={floor_pen*100:.2f} "
                f"wall={wall_pen*100:.2f} cap=({cap.x:.2f},{cap.y:.2f},{cap.z:.2f}) "
                f"footL={None if fl is None else round(fl.x,3)} footR={None if fr is None else round(fr.x,3)} "
                f"pins={ {k[0]+k[1]: (None if pins[k]['lock'] is None else round(pins[k]['w'],2)) for k in pins} }",
                flush=True,
            )
        pen = max(pen, frame_pen)
        if face is not None:
            if torso is not None:
                torso_clear = min(torso_clear, torso - face)
            if head is not None:
                head_clear = min(head_clear, head - face)
        for key, st in pins.items():
            if st["w"] < 0.95 or st["lock"] is None:
                continue
            kind, side = key
            vert = _pinned_vertex(arm, kind, side, st["kind"], st["lock"])
            if vert is None:
                continue
            normal = st["normal"]
            # A foot that missed the surface is not a plant, so it is not slide.
            if abs((vert - st["lock"]).dot(normal)) > 0.02:
                continue
            if spec.get("sliding_feet") and st["kind"] == "floor":
                # Skate against this frame's moving contact, not body travel.
                delta = vert - st["lock"]
                along = delta - normal * delta.dot(normal)
                slide[key] = max(slide[key], along.length)
                continue
            if st["plant_at"] is None:
                st["plant_at"] = vert.copy()
            delta = vert - st["plant_at"]
            along = delta - normal * delta.dot(normal)
            if along.length > 0.02 and along.length >= slide[key]:
                print(
                    f"SLIDE {spec['id']} f={i} {kind}{side} {along.length*100:.1f}cm "
                    f"lock=({st['lock'].x:.3f},{st['lock'].y:.3f},{st['lock'].z:.3f}) "
                    f"vert=({vert.x:.3f},{vert.y:.3f},{vert.z:.3f})",
                    flush=True,
                )
            slide[key] = max(slide[key], along.length)
        eulers = {}
        for pb in arm.pose.bones:
            if pb.name == "Root":
                continue
            pb.rotation_mode = "XYZ"
            e = pb.rotation_euler
            eulers[pb.name] = (math.degrees(e.x), math.degrees(e.y), math.degrees(e.z))
        keys.append(eulers)
        plants.append({
            key[0] + key[1]: None if pins[key]["lock"] is None else (
                round(pins[key]["w"], 2),
                pins[key]["kind"],
                [round(v, 3) for v in pins[key]["lock"]],
            )
            for key in pins
        })
        if i in beats or i == hero_i:
            print(
                f"BEAT {spec['id']} f={i} cap=({cap.x:.2f},{cap.y:.2f},{cap.z:.2f}) "
                f"pen={frame_pen*100:.1f} torso={None if torso is None or face is None else round((torso-face)*100,1)} "
                f"pins={plants[-1]}",
                flush=True,
            )
    return {
        "keys": keys,
        "capsule": capsule,
        "face": face,
        "lip_z": lip_z,
        "hero_i": hero_i,
        "pen": pen,
        "torso_clear": torso_clear,
        "head_clear": head_clear,
        "slide": slide,
        "fps": path["fps"],
        "plants": plants,
        "meta": meta,
    }


def _beats(spec, n, hero_i):
    frames = [i for i in spec.get("beats", BEATS.get(spec["id"], [])) if 0 <= i < n]
    if hero_i not in frames:
        frames.append(hero_i)
    frames = sorted(set(frames))
    while len(frames) > 8:
        mid = [i for i in frames[1:-1] if i != hero_i]
        if not mid:
            break
        mid.sort(key=lambda i: -abs(i - hero_i))
        frames.remove(mid[0])
    return frames


def _shot_at(arm, spec, ortho):
    """Camera locked to the capsule, framed on the hips so the head and feet fit."""
    cap = arm.location
    # The armature origin sits near the root, well below the chest. Aim at the hips.
    focus_z = cap.z + 0.90
    if spec["verb"] in ("softland", "slide"):
        focus_z = cap.z + (0.55 if spec["verb"] == "slide" else 0.70)
        if ortho:
            return (cap.x + 3.4, cap.y, focus_z), (cap.x, cap.y, focus_z), 2.35
        return (cap.x + 2.4, cap.y - 3.4, focus_z + 0.15), (cap.x, cap.y, focus_z - 0.05), None
    # Side elevation looks along the wall. 2.55 m covers a crown above the hips
    # and a foot on the face, including a cat-leap lip.
    if ortho:
        return (cap.x, cap.y - 3.5, focus_z), (cap.x, cap.y, focus_z), 2.55
    return (cap.x + 2.6, cap.y - 3.8, focus_z + 0.15), (cap.x - 0.05, cap.y, focus_z - 0.05), None


def _show_plants(solved, i, face):
    rh._hide_marks()
    if face is None:
        return
    info = solved["plants"][i]
    ys = []
    for side in ("L", "R"):
        rec = info.get("foot" + side)
        if not rec or rec[0] < 0.5:
            continue
        _w, kind, lock = rec
        if kind == "wall":
            rh._show_foot_mark(face, lock[1], lock[2], side)
            ys.append(lock[1])
        elif kind == "floor":
            rh._show_foot_mark(lock[0], lock[1], 0.0, side)
    if solved["lip_z"] is not None:
        hy = ys or [solved["capsule"][i].y]
        rh._show_lip_mark(face, solved["lip_z"], sum(hy) / len(hy), 0.7)


def render_pass5(do_render=True):
    import bpy
    os.makedirs(PREV, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    arm = bpy.data.objects.get("DummyArmature")
    if arm is None:
        raise SystemExit("Tan blend has no DummyArmature")
    rh._prepare_scene()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 10
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 1200
    doc = {
        "root_motion": False,
        "note": (
            "Bone rotation keys are in capsule space. The armature and Root stay "
            "at the origin in the clip. capsule_preview_m is only the preview path "
            "used for these stills; the game does not play it as root motion."
        ),
        "clips": {},
    }
    summary = []
    solved_all = {}
    only = {s for s in os.environ.get("PASS5_ONLY", "").split(",") if s}
    for spec in sp.CLIPS:
        if only and spec["id"] not in only:
            continue
        curves = sp.clip_curves(spec["id"], keys=24)
        smooth = curves["raw_smooth"]
        facing = _facing_for(spec, smooth)
        path = root_path(spec)
        solved = solve_clip(arm, spec, smooth, facing, path)
        solved["facing"] = facing
        solved_all[spec["id"]] = solved
        slides = [v * 100.0 for v in solved["slide"].values()]
        slide_cm = max(slides) if slides else 0.0
        pen_cm = solved["pen"] * 100.0
        torso_cm = solved["torso_clear"] * 100.0 if solved["face"] is not None else None
        head_cm = solved["head_clear"] * 100.0 if solved["face"] is not None else None
        line = (
            f"PASS5 id={spec['id']} slide_cm={slide_cm:.2f} pen_cm={pen_cm:.2f} "
            f"torso_clear_cm={torso_cm if torso_cm is not None else float('nan'):.2f} "
            f"head_clear_cm={head_cm if head_cm is not None else float('nan'):.2f} "
            f"face={solved['face']} lip={solved['lip_z']} hero={solved['hero_i']}"
        )
        print(line, flush=True)
        summary.append(line)
        fps = solved["fps"]
        keys_out = []
        caps_out = []
        for i, eulers in enumerate(solved["keys"]):
            bones = {name: [round(a, 2) for a in deg] for name, deg in eulers.items()}
            keys_out.append({"frame": i, "t": round(i / fps, 4), "bones": bones})
            c = solved["capsule"][i]
            caps_out.append([round(c.x, 4), round(c.y, 4), round(c.z, 4)])
        doc["clips"][spec["id"]] = {
            "fps": fps,
            "frames": path["frames"],
            "hero_frame": solved["hero_i"],
            "foot_slide_cm": round(slide_cm, 2),
            "penetration_cm": round(pen_cm, 2),
            "torso_clearance_cm": None if torso_cm is None else round(torso_cm, 2),
            "head_clearance_cm": None if head_cm is None else round(head_cm, 2),
            "root_location_keys": False,
            "capsule_preview_m": caps_out,
            "keys": keys_out,
        }
        rh._reset(arm)
    with open(os.path.join(OUT, "keyed_clips.json"), "w") as f:
        json.dump(doc, f)
    print("wrote", os.path.join(OUT, "keyed_clips.json"), flush=True)
    if not do_render:
        for line in summary:
            print(line, flush=True)
        return solved_all
    for spec in sp.CLIPS:
        if spec["id"] not in solved_all:
            continue
        solved = solved_all[spec["id"]]
        beats = _beats(spec, len(solved["keys"]), solved["hero_i"])
        slug = rh.SLUGS[spec["id"]]
        facing = solved["facing"]
        for s, i in enumerate(beats):
            eulers = solved["keys"][i]
            rh._apply_eulers(arm, {k: v for k, v in eulers.items()})
            arm.location = solved["capsule"][i]
            arm.rotation_euler = facing
            root = arm.pose.bones.get("Root")
            if root is not None:
                root.location = (0.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            if solved["face"] is None:
                rh._hide_wall()
            else:
                rh._show_wall(solved["face"], solved["lip_z"], arm.location.y, 2.4)
            _show_plants(solved, i, solved["face"])
            loc, look, _ortho = _shot_at(arm, spec, False)
            rh._shot(os.path.join(PREV, f"{slug}_{s}.png"), loc, look)
            loc, look, ortho = _shot_at(arm, spec, True)
            rh._shot(os.path.join(PREV, f"{slug}_{s}_side.png"), loc, look, ortho=ortho)
            print(f"pass5 {slug} {s} frame={i}", flush=True)
        rh._reset(arm)
    for line in summary:
        print(line, flush=True)
    return solved_all


def composite_pass5():
    from PIL import Image, ImageDraw, ImageFont
    os.makedirs(OUT, exist_ok=True)
    font = ImageFont.load_default()
    labels = {
        "03_wall_drop_softland": "03 soft land",
        "04_window_drop_softland": "04 soft land",
        "10_wallrun_slanted": "10 wall run",
        "12_tictac_slanted_wall": "12 tic-tac",
        "15_cat_leap_wall": "15 cat leap",
    }
    for spec in sp.CLIPS:
        slug = rh.SLUGS[spec["id"]]
        for suffix, tag in (("", "3/4"), ("_side", "side")):
            frames = []
            for i in range(8):
                frames.append(Image.open(os.path.join(PREV, f"{slug}_{i}{suffix}.png")).convert("RGB"))
            w, h = frames[0].size
            gap = 8
            canvas = Image.new("RGB", (w * 8 + gap * 7, h), (48, 44, 40))
            for i, im in enumerate(frames):
                canvas.paste(im, (i * (w + gap), 0))
            d = ImageDraw.Draw(canvas)
            text = labels[spec["id"]] + "  " + tag
            d.rectangle((12, 12, 16 + 8 * len(text) + 12, 36), fill=(20, 18, 16))
            d.text((20, 16), text, fill=(245, 236, 220), font=font)
            path = os.path.join(OUT, f"strip_{slug}{suffix}.png")
            canvas.save(path)
            print("wrote", path)


if __name__ == "__main__":
    args = _args()
    if "--composite" in args and "bpy" not in sys.modules:
        composite_pass5()
    elif "--metrics" in args:
        render_pass5(do_render=False)
    else:
        render_pass5(do_render=True)
