"""Pass 15 Hier stills. Same stage and camera as the accepted fall.

Props are built from the posed bones. A contact point sits on the prop
surface, within 2 cm. The climb subject is turned so that camera sees the
wall face from the climber's side.
"""
import math
import os
import struct
import zlib
import bpy
from mathutils import Vector, Euler

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.environ.get("PASS15_OUT", os.path.join(ROOT, "Docs", "SmoothStills", "pass15"))
ONLY = set(filter(None, os.environ.get("PASS15_ONLY", "").split(",")))
SAMPLES = int(os.environ.get("PASS15_SAMPLES", "16"))

PLAYER = (0.235, 0.557, 0.847, 1.0)
ACCENT = (0.45, 0.82, 0.86, 1.0)
JOINT = (0.16, 0.17, 0.18, 1.0)
GROUND = (0.34, 0.36, 0.34, 1.0)
SKY = (0.52, 0.64, 0.76, 1.0)
WALL = (0.48, 0.46, 0.42, 1.0)
BOX = (0.46, 0.38, 0.30, 1.0)
BAR = (0.16, 0.17, 0.18, 1.0)
CABLE = (0.12, 0.13, 0.14, 1.0)
ROPE = (0.42, 0.32, 0.20, 1.0)
PAD = (0.78, 0.62, 0.22, 1.0)

# Locked to the pass 14 fall frame.
CAM_DIR = Vector((0.78, -1.42, 0.22)).normalized()
CAM_LOOK = Vector((0.0, 0.05, 1.15))
CAM_DIST = 5.28
CAM_LENS = 48
CONTACT = 0.02


def rad(deg):
    return math.radians(deg)


def set_euler(arm, name, xdeg, ydeg, zdeg):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = Euler((rad(xdeg), rad(ydeg), rad(zdeg)), "XYZ")


def clear_pose(arm):
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")


def leg(arm, side, thigh, knee, yaw=0.0):
    yaw_b = yaw if side == "L" else -yaw
    set_euler(arm, "UpperLeg_" + side, -thigh, yaw_b, 0.0)
    set_euler(arm, "LowerLeg_" + side, -knee, 0.0, 0.0)


def arm_pose(arm, side, pitch, yaw, elbow, hand, roll=0.0):
    yaw_b = yaw if side == "L" else -yaw
    roll_b = roll if side == "L" else -roll
    set_euler(arm, "UpperArm_" + side, pitch, yaw_b, roll_b)
    set_euler(arm, "LowerArm_" + side, elbow, 0.0, 0.0)
    set_euler(arm, "Hand_" + side, hand, 0.0, 0.0)


def torso(arm, hip, spine, head):
    set_euler(arm, "Hips", hip, 0.0, 0.0)
    set_euler(arm, "Spine", spine, 0.0, 0.0)
    set_euler(arm, "Chest", 0.0, 0.0, 0.0)
    set_euler(arm, "Head", head, 0.0, 0.0)


def pose_stride(arm):
    leg(arm, "L", 42.0, -48.0)
    leg(arm, "R", -24.0, -10.0)
    arm_pose(arm, "L", 36.0, 8.0, -16.0, 0.0)
    arm_pose(arm, "R", -40.0, 8.0, -28.0, 0.0)
    torso(arm, 4.0, 6.5, 2.0)


def pose_stop(arm):
    leg(arm, "L", 16.0, -24.0)
    leg(arm, "R", -6.0, -12.0)
    arm_pose(arm, "L", -8.0, 6.0, -14.0, 0.0)
    arm_pose(arm, "R", -8.0, 6.0, -14.0, 0.0)
    torso(arm, -9.0, -3.5, -2.0)


def pose_turn(arm):
    leg(arm, "L", -8.0, -14.0)
    leg(arm, "R", 24.0, -36.0)
    arm_pose(arm, "L", 18.0, 10.0, -12.0, 0.0)
    arm_pose(arm, "R", -22.0, 10.0, -18.0, 0.0)
    torso(arm, 2.0, 3.0, 2.0)
    set_euler(arm, "Chest", 2.0, 0.0, 12.0)
    set_euler(arm, "Hips", 2.0, 0.0, 8.0)


def pose_apex(arm):
    leg(arm, "L", 76.0, -112.0)
    leg(arm, "R", 70.0, -106.0)
    arm_pose(arm, "L", -45.0, -35.0, -34.0, 0.0)
    arm_pose(arm, "R", -45.0, -35.0, -34.0, 0.0)
    torso(arm, 8.0, -4.0, 0.0)


def pose_fall(arm):
    leg(arm, "L", 26.0, -32.0, -12.0)
    leg(arm, "R", 26.0, -32.0, -12.0)
    arm_pose(arm, "L", 12.0, 0.0, -34.0, -50.0, -14.0)
    arm_pose(arm, "R", 12.0, 0.0, -34.0, -50.0, -14.0)
    torso(arm, 8.0, -6.0, 16.0)


def pose_climb(arm):
    # Hips stay near the wall. Hands and toes share its face.
    torso(arm, 8.0, 6.0, -12.0)
    arm_pose(arm, "L", -145.0, 6.0, -20.0, 16.0)
    arm_pose(arm, "R", -145.0, 6.0, -20.0, 16.0)
    leg(arm, "L", 48.8, -85.0, -6.0)
    leg(arm, "R", 27.6, -50.0, -8.0)


def pose_vault(arm):
    # Chest pitched over the plant. Hands lie flat. Both knees up to one side.
    torso(arm, 65.0, 40.0, -8.0)
    arm_pose(arm, "L", -20.0, 22.0, -100.0, -25.0, -8.0)
    arm_pose(arm, "R", -20.0, 22.0, -100.0, -25.0, -8.0)
    leg(arm, "L", 100.0, -70.0, -90.0)
    leg(arm, "R", 92.0, -80.0, 75.0)


def pose_slide(arm):
    # Hips down, chest leaned back, lead leg out, trail leg folded, hand behind.
    torso(arm, -22.0, -14.0, 6.0)
    leg(arm, "L", 30.0, -2.0, 0.0)
    leg(arm, "R", -20.0, -150.0, -15.0)
    arm_pose(arm, "L", 16.0, 8.0, -12.0, 0.0)
    arm_pose(arm, "R", 105.0, -45.0, 0.0, 0.0, 20.0)


def pose_zip(arm):
    leg(arm, "L", 34.0, -18.0)
    leg(arm, "R", 30.0, -14.0)
    arm_pose(arm, "L", -158.0, 8.0, -22.0, -40.0)
    arm_pose(arm, "R", -158.0, 8.0, -22.0, -40.0)
    torso(arm, -8.0, -6.0, -4.0)


def pose_grapple(arm):
    leg(arm, "L", 8.0, -18.0)
    leg(arm, "R", -6.0, -14.0)
    arm_pose(arm, "L", 18.0, 28.0, -24.0, 0.0)
    arm_pose(arm, "R", -148.0, -6.0, -22.0, -45.0)
    torso(arm, -8.0, 6.0, -10.0)


def pose_pad(arm):
    leg(arm, "L", 42.0, -52.0)
    leg(arm, "R", 38.0, -46.0)
    arm_pose(arm, "L", -155.0, 16.0, -14.0, 0.0)
    arm_pose(arm, "R", -155.0, 16.0, -14.0, 0.0)
    torso(arm, 6.0, -4.0, -10.0)


POSES = (
    ("stride", pose_stride, 0.0, None),
    ("stop", pose_stop, 0.0, None),
    ("turn", pose_turn, 0.0, None),
    ("apex", pose_apex, 0.28, None),
    ("fall", pose_fall, 0.28, None),
    ("climb", pose_climb, 0.40, "climb"),
    ("vault", pose_vault, 0.14, "vault"),
    ("slide", pose_slide, 0.0, "slide"),
    ("zip", pose_zip, 0.35, "zip"),
    ("grapple", pose_grapple, 0.22, "grapple"),
    ("pad", pose_pad, 0.42, "pad"),
)


def tint():
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        node = mat.node_tree.nodes.get("Principled BSDF")
        if node is None:
            continue
        name = mat.name
        if name in ("Joint_Tan", "Sensor_Tan"):
            color = JOINT
        elif name == "Accent" or name.startswith("Cal"):
            color = ACCENT
        else:
            color = PLAYER
        node.inputs["Base Color"].default_value = color
        if "Roughness" in node.inputs:
            node.inputs["Roughness"].default_value = 0.45


def look_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def bone_tail(arm, name):
    bone = arm.pose.bones[name]
    return arm.matrix_world @ bone.tail


def bone_head(arm, name):
    bone = arm.pose.bones[name]
    return arm.matrix_world @ bone.head


def bone_point(arm, name, along):
    return bone_head(arm, name).lerp(bone_tail(arm, name), along)


def make_mat(name, color, rough=0.7):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    node = mat.node_tree.nodes["Principled BSDF"]
    node.inputs["Base Color"].default_value = color
    node.inputs["Roughness"].default_value = rough
    return mat


def add_box(name, center, size, color, rough=0.7, rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
    if rot is not None:
        obj.rotation_euler = rot
    obj.data.materials.append(make_mat(name + "Mat", color, rough))
    return obj


def add_cyl(name, a, b, radius, color):
    delta = b - a
    length = delta.length
    if length < 0.001:
        return None
    mid = (a + b) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=length, location=mid)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = delta.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(make_mat(name + "Mat", color, 0.45))
    return obj


def clear_props():
    for obj in list(bpy.data.objects):
        if obj.name.startswith("Prop"):
            bpy.data.objects.remove(obj, do_unlink=True)


def horiz(v):
    flat = Vector((v.x, v.y, 0.0))
    if flat.length < 0.001:
        return Vector((0.0, -1.0, 0.0))
    return flat.normalized()


def cm(meters):
    return round(meters * 100.0, 1)


def plane_dist(point, origin, normal):
    return (point - origin).dot(normal)


def line_dist(point, a, b):
    delta = b - a
    length = delta.length
    if length < 1e-6:
        return (point - a).length
    t = (point - a).dot(delta) / (length * length)
    t = max(0.0, min(1.0, t))
    return (point - (a + delta * t)).length


def xy_inside(point, x0, x1, y0, y1):
    return x0 - 1e-4 <= point.x <= x1 + 1e-4 and y0 - 1e-4 <= point.y <= y1 + 1e-4


def report_contact(kind, touches, **notes):
    parts = [kind]
    worst = 0.0
    for key, value in touches.items():
        parts.append(key)
        parts.append(str(value))
        worst = max(worst, abs(value))
    for key, value in notes.items():
        parts.append(key)
        if isinstance(value, bool):
            parts.append("1" if value else "0")
        else:
            parts.append(str(value))
    flag = "OK" if worst <= 2.0 else "FAR"
    print("CONTACT", flag, " ".join(parts))
    return worst <= 2.0


def build_prop(kind, arm):
    clear_props()
    bpy.ops.object.mode_set(mode="OBJECT")
    ok = True
    if kind == "climb":
        ok = build_climb(arm)
    elif kind == "vault":
        ok = build_vault(arm)
    elif kind == "slide":
        ok = build_slide(arm)
    elif kind == "zip":
        ok = build_zip(arm)
    elif kind == "grapple":
        ok = build_grapple(arm)
    elif kind == "pad":
        ok = build_pad(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    return ok


def build_climb(arm):
    left = bone_tail(arm, "Hand_L")
    right = bone_tail(arm, "Hand_R")
    foot_l = bone_tail(arm, "Foot_L")
    foot_r = bone_tail(arm, "Foot_R")
    hip = bone_head(arm, "Hips")
    hands = (left, right)
    top = (left.z + right.z) * 0.5
    normal = horiz((left + right) * 0.5 - hip)
    origin = (left + right) * 0.5
    origin.z = top
    tangent = Vector((-normal.y, normal.x, 0.0))
    lats = [((p - origin).dot(tangent)) for p in hands]
    lat = (min(lats) + max(lats)) * 0.5
    depth = 0.36
    width = max(1.6, (max(lats) - min(lats)) + 0.9)
    center = origin + normal * (depth * 0.5) + tangent * lat
    center.z = top * 0.5
    rot = normal.to_track_quat("Y", "Z").to_euler()
    add_box("PropWall", center, (width, depth, top), WALL, 0.72, rot)
    face = [plane_dist(p, origin, normal) for p in (left, right, foot_l, foot_r)]
    hand_top = [abs(p.z - top) for p in hands]
    return report_contact(
        "climb",
        {
            "handLcm": cm(math.hypot(face[0], hand_top[0])),
            "handRcm": cm(math.hypot(face[1], hand_top[1])),
            "footLcm": cm(abs(face[2])),
            "footRcm": cm(abs(face[3])),
        },
        hipFacecm=cm(abs(plane_dist(hip, origin, normal))),
    )


def build_vault(arm):
    points = []
    for side in ("L", "R"):
        points.append(bone_head(arm, "Hand_" + side))
        points.append(bone_tail(arm, "Hand_" + side))
    hip = bone_head(arm, "Hips")
    chest = bone_tail(arm, "Chest")
    top = sum(p.z for p in points) / len(points)
    forward = horiz(chest - hip)
    tangent = Vector((-forward.y, forward.x, 0.0))

    def slot(point):
        delta = point - hip
        return delta.dot(forward), delta.dot(tangent)

    slots = [slot(p) for p in points + [hip, chest]]
    s0, s1 = min(s for s, _t in slots), max(s for s, _t in slots)
    t0, t1 = min(t for _s, t in slots), max(t for _s, t in slots)
    s0 -= 0.10
    s1 += 0.12
    t0 -= 0.14
    t1 += 0.14
    length = max(0.62, s1 - s0)
    width = max(0.58, t1 - t0)
    center_s = (s0 + s1) * 0.5
    center_t = (t0 + t1) * 0.5
    # Keep the padded box centered on the same span.
    s0, s1 = center_s - length * 0.5, center_s + length * 0.5
    t0, t1 = center_t - width * 0.5, center_t + width * 0.5
    center = hip + forward * center_s + tangent * center_t
    center.z = top * 0.5
    rot = forward.to_track_quat("Y", "Z").to_euler()
    add_box("PropBox", center, (width, length, top), BOX, 0.62, rot)
    hand_cm = [cm(abs(p.z - top)) for p in points]
    inside = all(s0 <= slot(p)[0] <= s1 and t0 <= slot(p)[1] <= t1 for p in points)
    hip_s, hip_t = slot(hip)
    hip_inside = s0 <= hip_s <= s1 and t0 <= hip_t <= t1
    foot_l = bone_tail(arm, "Foot_L")
    foot_r = bone_tail(arm, "Foot_R")
    return report_contact(
        "vault",
        {"handcm": max(hand_cm)},
        handsOn=inside,
        hipOver=hip_inside,
        hipAbovecm=cm(hip.z - top),
        chestAbovecm=cm(chest.z - top),
        footLcm=cm(foot_l.z - top),
        footRcm=cm(foot_r.z - top),
    )


def build_slide(arm):
    hip = bone_head(arm, "Hips")
    lead = bone_tail(arm, "Foot_L")
    trail = bone_tail(arm, "Foot_R")
    hand = bone_tail(arm, "Hand_R")
    head = bone_tail(arm, "Head")
    chest = bone_tail(arm, "Chest")
    travel = horiz(lead - hip)
    axis = Vector((0.0, 0.0, 1.0)).cross(travel).normalized()
    clearance = 0.07
    bar_z = head.z + clearance
    center = Vector((head.x, head.y, bar_z))
    half = 1.15
    a = center - axis * half
    b = center + axis * half
    add_cyl("PropBar", a, b, 0.035, BAR)
    add_cyl("PropPostL", Vector((a.x, a.y, 0.0)), a, 0.04, BAR)
    add_cyl("PropPostR", Vector((b.x, b.y, 0.0)), b, 0.04, BAR)
    return report_contact(
        "slide",
        {
            "leadcm": cm(abs(lead.z)),
            "trailcm": cm(abs(trail.z)),
            "handcm": cm(abs(hand.z)),
        },
        hipcm=cm(hip.z),
        barClearcm=cm(clearance),
        chestClearcm=cm(bar_z - chest.z),
    )


def build_zip(arm):
    grips = [bone_point(arm, "Hand_" + side, 0.42) for side in ("L", "R")]
    delta = grips[1] - grips[0]
    if delta.length < 0.05:
        delta = Vector((1.0, 0.0, 0.0))
    direction = delta.normalized()
    a = grips[0] - direction * 1.7
    b = grips[1] + direction * 1.7
    add_cyl("PropCable", a, b, 0.018, CABLE)
    dists = [line_dist(g, a, b) for g in grips]
    return report_contact(
        "zip",
        {"handLcm": cm(dists[0]), "handRcm": cm(dists[1])},
    )


def build_grapple(arm):
    head = bone_head(arm, "Hand_R")
    tail = bone_tail(arm, "Hand_R")
    direction = tail - head
    if direction.length < 0.02:
        direction = Vector((0.0, 0.0, 1.0))
    direction = direction.normalized()
    grip = head.lerp(tail, 0.25)
    end = tail + direction * 1.55
    add_cyl("PropRope", grip, end, 0.012, ROPE)
    return report_contact(
        "grapple",
        {
            "palmc": cm(line_dist(grip, grip, end)),
            "handcm": cm(line_dist(tail, grip, end)),
        },
    )


def build_pad(arm):
    hip = bone_head(arm, "Hips")
    feet = [bone_tail(arm, "Foot_L"), bone_tail(arm, "Foot_R")]
    pts = [hip] + feet
    x0, x1 = min(p.x for p in pts), max(p.x for p in pts)
    y0, y1 = min(p.y for p in pts), max(p.y for p in pts)
    margin = 0.42
    x0 -= margin
    x1 += margin
    y0 -= margin
    y1 += margin
    width = max(1.35, x1 - x0)
    depth = max(1.35, y1 - y0)
    cx = (min(p.x for p in pts) + max(p.x for p in pts)) * 0.5
    cy = (min(p.y for p in pts) + max(p.y for p in pts)) * 0.5
    x0, x1 = cx - width * 0.5, cx + width * 0.5
    y0, y1 = cy - depth * 0.5, cy + depth * 0.5
    top = 0.07
    add_box("PropPad", (cx, cy, top * 0.5), (width, depth, top), PAD, 0.4)
    above = [cm(p.z - top) for p in feet]
    inside = all(xy_inside(p, x0, x1, y0, y1) for p in pts)
    # The pad air pose is off the plate. Horizontal cover is the contact check.
    report_contact(
        "pad",
        {},
        footAbovecm=max(above),
        hipAbovecm=cm(hip.z - top),
        over=inside,
    )
    return inside


def ang(a, b):
    d = max(-1.0, min(1.0, a.normalized().dot(b.normalized())))
    return math.degrees(math.acos(d))


def measure_fall(arm):
    def wd(name):
        direction = bone_tail(arm, name) - bone_head(arm, name)
        return direction.normalized(), bone_head(arm, name), bone_tail(arm, name)

    up = Vector((0.0, 0.0, 1.0))
    down = Vector((0.0, 0.0, -1.0))
    td, th, tt = wd("UpperLeg_L")
    sd, _, st = wd("LowerLeg_L")
    ad, _, _ = wd("UpperArm_L")
    rd, _, _ = wd("UpperArm_R")
    ed, _, _ = wd("LowerArm_L")
    cd, _, _ = wd("Chest")
    torso_ang = ang(cd, up)
    torso_fwd = torso_ang if cd.y < 0.0 else -torso_ang
    print(
        "FALLBONE",
        "torsoFwd", round(torso_fwd, 1),
        "kneeFlex", round(ang(td, sd), 1),
        "thighDown", round(ang(td, down), 1),
        "footAhead", round(th.y - st.y, 3),
        "kneeAhead", round(th.y - tt.y, 3),
        "abdL", round(ang(ad, down), 1),
        "abdR", round(ang(rd, down), 1),
        "elbow", round(ang(ad, ed), 1),
    )


def scene_setup(arm):
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    ground.data.materials.append(make_mat("GroundMat", GROUND, 0.92))

    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, location=(0.0, 0.02, 0.012))
    shadow = bpy.context.active_object
    shadow.name = "Shadow"
    shadow.scale = (0.48, 0.28, 0.015)
    shadow.data.materials.append(make_mat("ShadowMat", (0.08, 0.09, 0.10, 1.0), 1.0))

    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = SKY
    bg.inputs["Strength"].default_value = 0.72

    bpy.ops.object.light_add(type="SUN", location=(3.0, -2.0, 8.0))
    sun = bpy.context.active_object
    sun.data.energy = 1.05
    sun.data.color = (1.0, 0.96, 0.90)
    look_at(sun, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.light_add(type="AREA", location=(-2.2, -2.6, 2.4))
    fill = bpy.context.active_object
    fill.data.energy = 28
    fill.data.size = 4.0
    look_at(fill, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.camera_add(location=(2.2, -4.2, 1.7))
    cam = bpy.context.active_object
    cam.data.lens = CAM_LENS
    cam.location = CAM_LOOK + CAM_DIR * CAM_DIST
    look_at(cam, CAM_LOOK)
    bpy.context.scene.camera = cam
    arm.hide_render = True

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = SAMPLES
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.exposure = 0.0
    print("CAMERA", tuple(round(v, 3) for v in cam.location), "dist", CAM_DIST, "lens", CAM_LENS)
    return scene, cam, shadow


def apply_pose(arm, fn, lift, yaw=0.0):
    clear_pose(arm)
    fn(arm)
    arm.rotation_mode = "XYZ"
    arm.rotation_euler = Euler((0.0, 0.0, rad(yaw)), "XYZ")
    arm.location = (0.0, 0.0, lift)
    bpy.context.view_layer.update()


def ground_slide(arm):
    names = ("Foot_L", "Foot_R", "Hand_R")
    lowest = min(bone_tail(arm, name).z for name in names)
    arm.location.z = -lowest + 0.008
    bpy.context.view_layer.update()


def cam_z(cam, point):
    return (cam.matrix_world.inverted() @ point).z


def choose_action_yaw(arm, cam, pose, lift, prefer):
    best = None
    for step in range(24):
        yaw = -180.0 + step * 15.0
        apply_pose(arm, pose, lift, yaw)
        if pose is pose_slide:
            ground_slide(arm)
        hip = bone_head(arm, "Hips")
        chest = bone_tail(arm, "Chest")
        normal = horiz(chest - hip)
        ahead = chest + normal * 0.25
        z_chest = cam_z(cam, chest)
        z_ahead = cam_z(cam, ahead)
        if z_chest > z_ahead - 0.02:
            continue
        view = horiz(CAM_LOOK - cam.location)
        facing = abs(view.dot(normal))
        score = abs(facing - prefer)
        if best is None or score < best[0]:
            best = (score, yaw, facing)
    if best is None:
        return 90.0, 0.0
    _, yaw, facing = best
    return yaw, facing


def choose_climb_yaw(arm, cam):
    best = None
    for step in range(24):
        yaw = -180.0 + step * 15.0
        apply_pose(arm, pose_climb, 0.40, yaw)
        hip = bone_head(arm, "Hips")
        chest = bone_tail(arm, "Chest")
        hand = (bone_tail(arm, "Hand_L") + bone_tail(arm, "Hand_R")) * 0.5
        normal = horiz(hand - hip)
        wall = hand + normal * 0.3
        z_chest = cam_z(cam, chest)
        z_wall = cam_z(cam, wall)
        # Camera looks down -Z. More negative is closer.
        if z_chest > z_wall - 0.05:
            continue
        view = horiz(CAM_LOOK - cam.location)
        facing = abs(view.dot(normal))
        score = abs(facing - 0.42) - (z_wall - z_chest) * 0.05
        if best is None or score < best[0]:
            best = (score, yaw, facing, z_wall - z_chest)
    if best is None:
        print("CLIMB_YAW", 90.0, "fallback")
        return 90.0
    _, yaw, facing, gap = best
    print("CLIMB_YAW", round(yaw, 1), "facing", round(facing, 2), "inFront", round(gap, 2))
    return yaw


def place_shadow(shadow, arm):
    hip = bone_head(arm, "Hips")
    shadow.location = (hip.x, hip.y + 0.02, 0.012)


def paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    if pb <= pc:
        return b
    return c


def png_rows(path):
    with open(path, "rb") as handle:
        data = handle.read()
    pos = 8
    blob = b""
    width = height = None
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            width, height = struct.unpack(">II", chunk[:8])
        elif kind == b"IDAT":
            blob += chunk
        elif kind == b"IEND":
            break
        pos += 12 + length
    raw = zlib.decompress(blob)
    stride = width * 4
    rows = []
    prev = bytearray(stride)
    index = 0
    for _y in range(height):
        filt = raw[index]
        index += 1
        row = bytearray(raw[index:index + stride])
        index += stride
        if filt == 1:
            for x in range(stride):
                left = row[x - 4] if x >= 4 else 0
                row[x] = (row[x] + left) & 255
        elif filt == 2:
            for x in range(stride):
                row[x] = (row[x] + prev[x]) & 255
        elif filt == 3:
            for x in range(stride):
                left = row[x - 4] if x >= 4 else 0
                row[x] = (row[x] + ((left + prev[x]) // 2)) & 255
        elif filt == 4:
            for x in range(stride):
                left = row[x - 4] if x >= 4 else 0
                upv = prev[x]
                ul = prev[x - 4] if x >= 4 else 0
                row[x] = (row[x] + paeth(left, upv, ul)) & 255
        rows.append(row)
        prev = row
    return width, height, rows


def report_png(path):
    width, height, rows = png_rows(path)
    minx, maxx, miny, maxy = width, -1, height, -1
    white = 0
    for y, row in enumerate(rows):
        for x in range(width):
            i = x * 4
            r, g, b = row[i], row[i + 1], row[i + 2]
            if b > r + 40 and g > r + 15 and b > 90 and r < 150 and b > g:
                if x < minx:
                    minx = x
                if x > maxx:
                    maxx = x
                if y < miny:
                    miny = y
                if y > maxy:
                    maxy = y
            if r > 245 and g > 245 and b > 245:
                white += 1
    fill = (maxy - miny + 1) / height if maxy >= miny else 0
    top = rows[8]
    bot = rows[height - 12]
    mid = width // 2 * 4
    print(
        os.path.basename(path),
        "fill", round(fill * 100, 1),
        "y", miny, maxy,
        "x", minx, maxx,
        "edge", int(miny == 0), int(maxy == height - 1), int(minx == 0), int(maxx == width - 1),
        "white", round(100.0 * white / (width * height), 2),
        "sky", tuple(top[mid:mid + 3]),
        "ground", tuple(bot[mid:mid + 3]),
    )


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    tint()
    scene, cam, shadow = scene_setup(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    climb_yaw = choose_climb_yaw(arm, cam)
    vault_yaw, vault_face = choose_action_yaw(arm, cam, pose_vault, 0.14, 0.35)
    slide_yaw, slide_face = choose_action_yaw(arm, cam, pose_slide, 0.0, 0.28)
    print("VAULT_YAW", round(vault_yaw, 1), "facing", round(vault_face, 2))
    print("SLIDE_YAW", round(slide_yaw, 1), "facing", round(slide_face, 2))
    apply_pose(arm, pose_fall, 0.28)
    measure_fall(arm)
    yaws = {"climb": climb_yaw, "vault": vault_yaw, "slide": slide_yaw}
    for name, fn, lift, prop in POSES:
        if ONLY and name not in ONLY:
            continue
        yaw = yaws.get(prop, 0.0)
        apply_pose(arm, fn, lift, yaw)
        if prop == "slide":
            ground_slide(arm)
        place_shadow(shadow, arm)
        if prop:
            build_prop(prop, arm)
        else:
            clear_props()
            bpy.context.view_layer.objects.active = arm
            bpy.ops.object.mode_set(mode="POSE")
        path = os.path.join(OUT, name + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        report_png(path)
        print("WROTE", path)


if __name__ == "__main__":
    main()
