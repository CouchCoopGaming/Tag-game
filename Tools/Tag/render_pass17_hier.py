"""Pass 17 Hier stills and 30 fps strips.

Vault and slide stay on the pass 16 mesh contacts. The climb camera sits
on the runner's side of the wall. The pad still plants both shoes. Strips
sample the in-game blend clocks at 1/30 s. Gameplay timers are not written.
"""
import math
import os
import struct
import zlib

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Euler, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.environ.get("PASS17_OUT", os.path.join(ROOT, "Docs", "SmoothStills", "pass17"))
ONLY = set(filter(None, os.environ.get("PASS17_ONLY", "").split(",")))
SAMPLES = int(os.environ.get("PASS17_SAMPLES", "16"))
DO_RENDER = os.environ.get("PASS17_RENDER", "1") != "0"
DO_STRIPS = os.environ.get("PASS17_STRIPS", "1") != "0"

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

CAM_DIR = Vector((0.78, -1.42, 0.22)).normalized()
CAM_LOOK = Vector((0.0, 0.05, 1.15))
CAM_DIST = 5.28
CAM_LENS = 48
RES_X = 960
RES_Y = 720

MESH = {
    "handL": "Mesh_Hand_L",
    "handR": "Mesh_Hand_R",
    "footL": "Mesh_Foot_L",
    "footR": "Mesh_Foot_R",
    "head": "Mesh_Head",
    "hips": "Mesh_Hips",
    "chest": "Mesh_Chest",
}


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
    # Upright, hands down on a lip, chest on the face, lead foot on the face.
    torso(arm, 22.0, 6.0, -8.0)
    arm_pose(arm, "L", -140.0, 12.0, -60.0, -15.0)
    arm_pose(arm, "R", -140.0, 12.0, -60.0, -15.0)
    leg(arm, "L", 100.0, -120.0, 35.0)
    set_euler(arm, "Foot_L", -25.0, 0.0, 0.0)
    leg(arm, "R", 55.0, -70.0, 15.0)
    set_euler(arm, "Foot_R", 10.0, 0.0, 0.0)


def pose_vault(arm):
    # Diagonal over the box, both palms on one plane, knees up to the side.
    torso(arm, 36.0, 18.0, -4.0)
    set_euler(arm, "Chest", 6.0, 0.0, 0.0)
    arm_pose(arm, "L", 0.0, 18.0, -90.0, 20.0, -8.0)
    arm_pose(arm, "R", 0.0, 18.0, -90.0, 20.0, -8.0)
    leg(arm, "L", 112.0, -48.0, -88.0)
    leg(arm, "R", 120.0, -42.0, 78.0)


def pose_slide(arm):
    # Reclined, feet first, lead leg straight. Grounded on the lead shoe.
    torso(arm, -32.0, -22.0, 6.0)
    set_euler(arm, "Chest", -8.0, 0.0, 0.0)
    leg(arm, "L", 45.0, 0.0, 0.0)
    leg(arm, "R", 0.0, -150.0, -45.0)
    set_euler(arm, "UpperArm_L", 40.0, 10.0, 10.0)
    set_euler(arm, "LowerArm_L", -50.0, 0.0, 0.0)
    set_euler(arm, "Hand_L", -15.0, 0.0, 0.0)
    set_euler(arm, "UpperArm_R", 35.0, -15.0, 15.0)
    set_euler(arm, "LowerArm_R", -45.0, 0.0, 0.0)
    set_euler(arm, "Hand_R", -10.0, 0.0, 0.0)


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
    # Launch swing. Knees stay bent. The root is settled onto the plate.
    leg(arm, "L", 42.0, -52.0)
    leg(arm, "R", 38.0, -46.0)
    arm_pose(arm, "L", -155.0, 16.0, -14.0, 0.0)
    arm_pose(arm, "R", -155.0, 16.0, -14.0, 0.0)
    torso(arm, 6.0, -4.0, -10.0)


# Filled by fit_wall_plant. Pitch, yaw, elbow for the inner arm; yaw and roll for the plant thigh.
WALL_ARM = (20.0, 8.0, -28.0)
WALL_LEG = (10.0, -30.0)


def pose_wall_run(arm):
    # Inner hand and the plant foot on the same wall face.
    torso(arm, 8.0, 6.0, -4.0)
    set_euler(arm, "Hips", 8.0, 0.0, -16.0)
    set_euler(arm, "Chest", 2.0, 0.0, -8.0)
    arm_pose(arm, "L", WALL_ARM[0], WALL_ARM[1], WALL_ARM[2], 0.0)
    arm_pose(arm, "R", -32.0, 8.0, -22.0, 0.0)
    leg(arm, "L", 46.0, -58.0, WALL_LEG[0])
    set_euler(arm, "UpperLeg_L", -46.0, WALL_LEG[0], WALL_LEG[1])
    set_euler(arm, "LowerLeg_L", 58.0, 0.0, 0.0)
    leg(arm, "R", -8.0, -18.0, -6.0)
    set_euler(arm, "Foot_L", 8.0, 0.0, 0.0)


def fit_wall_plant(arm):
    """Put the inner shoe and the inner hand on one plane. Visual only."""
    global WALL_ARM, WALL_LEG
    ensure_pose(arm)
    best = None
    for pitch in (8.0, 18.0, 28.0):
        for arm_yaw in (0.0, 8.0, 16.0, 24.0):
            for elbow in (-36.0, -24.0, -12.0):
                for leg_yaw in (0.0, 12.0, 24.0, 36.0):
                    for roll in (0.0, -15.0, -30.0, -45.0):
                        WALL_ARM = (pitch, arm_yaw, elbow)
                        WALL_LEG = (leg_yaw, roll)
                        apply_pose(arm, pose_wall_run, 0.0, 0.0)
                        left = body_left(arm)
                        foot = max(mesh_world("Mesh_Foot_L"), key=lambda p: p.dot(left))
                        hand = max(mesh_world("Mesh_Hand_L"), key=lambda p: p.dot(left))
                        gap_m = hand.dot(left) - foot.dot(left)
                        err = abs(gap_m)
                        # Keep the shoe low. A foot at chest height is not a plant.
                        if foot.z > 0.35:
                            err += 0.4
                        if best is None or err < best[0]:
                            best = (err, pitch, arm_yaw, elbow, leg_yaw, roll, gap_m, foot.z)
    WALL_ARM = (best[1], best[2], best[3])
    WALL_LEG = (best[4], best[5])
    print(
        "WALL_PLANT",
        "gap_cm", round(best[6] * 100.0, 1),
        "arm", WALL_ARM,
        "leg", WALL_LEG,
        "footZ", round(best[7], 3),
    )


def pose_wall_push(arm):
    # Shove. Chest opens off the wall. The trail knee is already up.
    torso(arm, -10.0, -18.0, -12.0)
    set_euler(arm, "Hips", -10.0, 0.0, -14.0)
    set_euler(arm, "Chest", -6.0, 0.0, -6.0)
    # Inner arm stays on the plant through the shove. The jump carries it off.
    arm_pose(arm, "L", WALL_ARM[0], WALL_ARM[1], WALL_ARM[2], 0.0)
    arm_pose(arm, "R", 16.0, 12.0, -20.0, 0.0)
    leg(arm, "L", 36.0, -48.0, 6.0)
    # Trail knee lifts inside the 0.12s shove without a 25 degree step.
    leg(arm, "R", 32.0, -48.0, -6.0)


# Pass 16 framing. Re-searching these yaws moves the accepted stills.
VAULT_YAW = -45.0
SLIDE_YAW = -45.0


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


def make_mat(name, color, rough=0.7, emit=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    node = mat.node_tree.nodes["Principled BSDF"]
    node.inputs["Base Color"].default_value = color
    node.inputs["Roughness"].default_value = rough
    if emit > 0.0:
        output = mat.node_tree.nodes.get("Material Output")
        emission = mat.node_tree.nodes.get("PropEmit")
        if emission is None:
            emission = mat.node_tree.nodes.new("ShaderNodeEmission")
            emission.name = "PropEmit"
        emission.inputs["Color"].default_value = color
        emission.inputs["Strength"].default_value = 1.0
        mat.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return mat


def add_box(name, center, size, color, rough=0.7, rot=None, emit=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    obj = bpy.context.active_object
    obj.name = name
    # size=1 already spans 1 m. Scale is the full size, not half of it.
    obj.scale = (size[0], size[1], size[2])
    if rot is not None:
        obj.rotation_euler = rot
    obj.data.materials.append(make_mat(name + "Mat", color, rough, emit))
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


def mesh_world(name):
    obj = bpy.data.objects[name]
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    pts = [mw @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    return pts


def body_forward(arm):
    return horiz(arm.matrix_world.to_3x3() @ Vector((0.0, -1.0, 0.0)))


def body_left(arm):
    return horiz(arm.matrix_world.to_3x3() @ Vector((1.0, 0.0, 0.0)))


def extreme(pts, key, mode, tol=0.015, cap=50):
    pivot = min(key(p) for p in pts) if mode == "min" else max(key(p) for p in pts)
    if mode == "min":
        chosen = [p for p in pts if key(p) <= pivot + tol]
    else:
        chosen = [p for p in pts if key(p) >= pivot - tol]
    chosen.sort(key=key, reverse=(mode == "max"))
    return chosen[:cap], pivot


def gap(name, mesh_pt, surface_pt):
    delta = mesh_pt - surface_pt
    return {"name": name, "a": mesh_pt, "b": surface_pt, "cm": cm(delta.length)}


def obj_corners(obj):
    bpy.context.view_layer.update()
    return [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]


def ang(a, b):
    d = max(-1.0, min(1.0, a.normalized().dot(b.normalized())))
    return math.degrees(math.acos(d))


def prove_pass15(arm):
    """Show why the pass 15 bone table does not match the picture."""
    clear_pose(arm)
    torso(arm, 65.0, 40.0, -8.0)
    arm_pose(arm, "L", -20.0, 22.0, -100.0, -25.0, -8.0)
    arm_pose(arm, "R", -20.0, 22.0, -100.0, -25.0, -8.0)
    leg(arm, "L", 100.0, -70.0, -90.0)
    leg(arm, "R", 92.0, -80.0, 75.0)
    arm.location = (0.0, 0.0, 0.14)
    bpy.context.view_layer.update()
    samples = []
    for side in ("L", "R"):
        samples.append(bone_head(arm, "Hand_" + side))
        samples.append(bone_tail(arm, "Hand_" + side))
    bone_top = sum(p.z for p in samples) / len(samples)
    half_top = bone_top * 0.75
    hands = mesh_world("Mesh_Hand_L") + mesh_world("Mesh_Hand_R")
    mesh_min = min(p.z for p in hands)
    foot = mesh_world("Mesh_Foot_L")
    foot_tail = bone_tail(arm, "Foot_L")
    foot_to_tail = min((p - foot_tail).length for p in foot)
    print(
        "SOURCE vault boneTop", round(bone_top, 3),
        "halfBoxTop", round(half_top, 3),
        "meshHandMin", round(mesh_min, 3),
        "meshToHalf_cm", cm(mesh_min - half_top),
        "meshToBone_cm", cm(mesh_min - bone_top),
        "footMeshToTail_cm", cm(foot_to_tail),
    )
    clear_pose(arm)
    torso(arm, -22.0, -14.0, 6.0)
    leg(arm, "L", 30.0, -2.0, 0.0)
    leg(arm, "R", -20.0, -150.0, -15.0)
    arm_pose(arm, "R", 105.0, -45.0, 0.0, 0.0, 20.0)
    bpy.context.view_layer.update()
    names = ("Foot_L", "Foot_R", "Hand_R")
    lowest = min(bone_tail(arm, name).z for name in names)
    arm.location.z = -lowest + 0.008
    bpy.context.view_layer.update()
    head_bone = bone_tail(arm, "Head").z
    head_mesh = max(p.z for p in mesh_world("Mesh_Head"))
    bar = head_bone + 0.07
    print(
        "SOURCE slide barAboveBone_cm", cm(0.07),
        "headMeshAboveBone_cm", cm(head_mesh - head_bone),
        "meshIntoBar_cm", cm(head_mesh - (bar - 0.035)),
    )


def settle(arm, kind):
    bpy.context.view_layer.update()
    if kind == "slide":
        foot = min(p.z for p in mesh_world("Mesh_Foot_L"))
        arm.location.z += 0.012 - foot
        bpy.context.view_layer.update()
    elif kind == "climb":
        foot = min(
            min(p.z for p in mesh_world("Mesh_Foot_L")),
            min(p.z for p in mesh_world("Mesh_Foot_R")),
        )
        arm.location.z += 0.012 - foot
        bpy.context.view_layer.update()
    elif kind == "pad":
        # Plate top is 0.08. Both soles land in 0–2 cm, knees still bent.
        z_l = min(p.z for p in mesh_world("Mesh_Foot_L"))
        z_r = min(p.z for p in mesh_world("Mesh_Foot_R"))
        low = min(z_l, z_r)
        arm.location.z += 0.086 - low
        bpy.context.view_layer.update()
        high_side = "L" if z_l >= z_r else "R"
        bone = arm.pose.bones["Foot_" + high_side]
        base = bone.rotation_euler.copy()
        best_z = min(p.z for p in mesh_world("Mesh_Foot_" + high_side))
        best_euler = base.copy()
        for deg in range(-20, 21, 2):
            bone.rotation_euler = Euler((base.x + rad(deg), base.y, base.z), "XYZ")
            bpy.context.view_layer.update()
            sole = min(p.z for p in mesh_world("Mesh_Foot_" + high_side))
            better = abs(sole - 0.090) < abs(best_z - 0.090)
            if better:
                best_z = sole
                best_euler = bone.rotation_euler.copy()
        bone.rotation_euler = best_euler
        bpy.context.view_layer.update()


def build_vault(arm):
    forward = body_forward(arm)
    tangent = horiz(Vector((0.0, 0.0, 1.0)).cross(forward))
    hands = mesh_world("Mesh_Hand_L") + mesh_world("Mesh_Hand_R")
    hips = mesh_world("Mesh_Hips")
    origin = bone_head(arm, "Hips")
    top = min(p.z for p in hands)

    def slot(point):
        delta = point - origin
        return delta.dot(forward), delta.dot(tangent)

    cover = hands + hips
    slots = [slot(p) for p in cover]
    s0, s1 = min(s for s, _t in slots) - 0.08, max(s for s, _t in slots) + 0.10
    t0, t1 = min(t for _s, t in slots) - 0.08, max(t for _s, t in slots) + 0.08
    length = max(0.7, s1 - s0)
    width = max(0.62, t1 - t0)
    center_s = (s0 + s1) * 0.5
    center_t = (t0 + t1) * 0.5
    s0, s1 = center_s - length * 0.5, center_s + length * 0.5
    t0, t1 = center_t - width * 0.5, center_t + width * 0.5
    center = origin + forward * center_s + tangent * center_t
    center.z = top * 0.5
    rot = forward.to_track_quat("Y", "Z").to_euler()
    box = add_box("PropBox", center, (width, length, top), BOX, 0.62, rot, emit=0.55)
    world_top = max(p.z for p in obj_corners(box))
    print("PROP vault worldTop", round(world_top, 4), "meshTop", round(top, 4), "size", round(length, 3), round(width, 3))
    gaps = []
    dots = []
    for label, name in (("handL", "Mesh_Hand_L"), ("handR", "Mesh_Hand_R")):
        pts = mesh_world(name)
        low, zmin = extreme(pts, lambda p: p.z, "min")
        dots.extend(low)
        surface = Vector((low[0].x, low[0].y, world_top))
        gaps.append(gap(label + "-boxTop", low[0], surface))
        print("MESH", label, "minz", round(zmin, 4), "nLow", len(low))
    hip_low = min(hips, key=lambda p: p.z)
    gaps.append(gap("hip-boxTop", hip_low, Vector((hip_low.x, hip_low.y, world_top))))
    for label, name in (("footL", "Mesh_Foot_L"), ("footR", "Mesh_Foot_R"), ("head", "Mesh_Head")):
        pts = mesh_world(name)
        pick, _pivot = extreme(pts, lambda p: p.z, "min" if label != "head" else "max")
        dots.extend(pick[:12])
    foot_l = min(p.z for p in mesh_world("Mesh_Foot_L"))
    foot_r = min(p.z for p in mesh_world("Mesh_Foot_R"))
    hand_s = [slot(p) for p in hands]
    hip_s = [slot(p) for p in hips]
    print(
        "POSE vault hipAbove_cm", cm(hip_low.z - world_top),
        "footL_cm", cm(foot_l - world_top),
        "footR_cm", cm(foot_r - world_top),
        "handS", round(min(s for s, _t in hand_s), 3), round(max(s for s, _t in hand_s), 3),
        "hipS", round(min(s for s, _t in hip_s), 3), round(max(s for s, _t in hip_s), 3),
        "boxS", round(s0, 3), round(s1, 3),
    )
    return {"dots": dots, "gaps": gaps, "side": body_left(arm), "focus": origin}


def build_climb(arm):
    forward = body_forward(arm)
    tangent = horiz(Vector((0.0, 0.0, 1.0)).cross(forward))
    chest = mesh_world("Mesh_Chest")
    hand_l = mesh_world("Mesh_Hand_L")
    hand_r = mesh_world("Mesh_Hand_R")
    foot_l = mesh_world("Mesh_Foot_L")
    foot_r = mesh_world("Mesh_Foot_R")
    head = mesh_world("Mesh_Head")
    face_pt = max(chest, key=lambda p: p.dot(forward))
    top = min(p.z for p in hand_l + hand_r)
    origin = Vector((face_pt.x, face_pt.y, 0.0))
    # Pull the plane back onto the chest front. origin is that point at z=0;
    # the plane normal is forward, so use face_pt itself.
    plane_pt = face_pt
    depth = 0.42
    hand_span = hand_l + hand_r
    lats = [(p - plane_pt).dot(tangent) for p in hand_span]
    lat = (min(lats) + max(lats)) * 0.5
    width = max(1.5, (max(lats) - min(lats)) + 0.8)
    center = plane_pt + forward * (depth * 0.5) + tangent * lat
    center.z = top * 0.5
    rot = forward.to_track_quat("Y", "Z").to_euler()
    wall = add_box("PropWall", center, (width, depth, top), WALL, 0.72, rot, emit=0.35)
    corners = obj_corners(wall)
    world_top = max(p.z for p in corners)
    # Near face is the side toward the body, opposite forward.
    face_corner = min(corners, key=lambda p: p.dot(forward))
    print(
        "PROP climb worldTop", round(world_top, 4),
        "meshTop", round(top, 4),
        "faceDot", round(face_corner.dot(forward), 4),
        "chestDot", round(plane_pt.dot(forward), 4),
    )
    gaps = []
    dots = []
    for label, pts in (("handL", hand_l), ("handR", hand_r)):
        low, _z = extreme(pts, lambda p: p.z, "min")
        dots.extend(low)
        surface = Vector((low[0].x, low[0].y, world_top))
        gaps.append(gap(label + "-lip", low[0], surface))
    chest_front, _ = extreme(chest, lambda p: p.dot(forward), "max")
    dots_chest = chest_front[0]
    chest_surface = dots_chest - forward * (dots_chest - plane_pt).dot(forward)
    # Plane through plane_pt. Distance is the gap; the surface point is the projection.
    chest_hit = dots_chest - forward * (dots_chest.dot(forward) - plane_pt.dot(forward))
    gaps.append(gap("chest-face", dots_chest, chest_hit))
    foot_front, _ = extreme(foot_l, lambda p: p.dot(forward), "max")
    dots.extend(foot_front)
    foot_hit = foot_front[0] - forward * (foot_front[0].dot(forward) - plane_pt.dot(forward))
    gaps.append(gap("footL-face", foot_front[0], foot_hit))
    head_top, _ = extreme(head, lambda p: p.z, "max")
    dots.extend(head_top[:12])
    def along(pts):
        return [(p.dot(forward) - plane_pt.dot(forward)) for p in pts]

    low_l = [p for p in hand_l if p.z <= min(q.z for q in hand_l) + 0.02]
    foot_r_front = max(foot_r, key=lambda p: p.dot(forward))
    print(
        "POSE climb chest_cm", cm((dots_chest.dot(forward) - plane_pt.dot(forward))),
        "footL_cm", cm((foot_front[0].dot(forward) - plane_pt.dot(forward))),
        "footR_cm", cm((foot_r_front.dot(forward) - plane_pt.dot(forward))),
        "footLz", round(min(p.z for p in foot_l), 3),
        "handOnto_cm", cm(min(along(low_l))),
        "nChest", len(chest_front),
        "nFoot", len(foot_front),
    )
    return {"dots": dots, "gaps": gaps, "side": body_left(arm), "focus": bone_head(arm, "Chest")}


def build_slide(arm):
    head = mesh_world("Mesh_Head")
    foot_l = mesh_world("Mesh_Foot_L")
    foot_r = mesh_world("Mesh_Foot_R")
    hand_l = mesh_world("Mesh_Hand_L")
    hand_r = mesh_world("Mesh_Hand_R")
    hips = mesh_world("Mesh_Hips")
    top_vert = max(head, key=lambda p: p.z)
    radius = 0.035
    clearance = 0.07
    bar_z = top_vert.z + clearance + radius
    travel = horiz(min(foot_l, key=lambda p: p.z) - top_vert)
    axis = horiz(Vector((0.0, 0.0, 1.0)).cross(travel))
    center = Vector((top_vert.x, top_vert.y, bar_z))
    half = 1.2
    a = center - axis * half
    b = center + axis * half
    add_cyl("PropBar", a, b, radius, BAR)
    add_cyl("PropPostL", Vector((a.x, a.y, 0.0)), a, 0.04, BAR)
    add_cyl("PropPostR", Vector((b.x, b.y, 0.0)), b, 0.04, BAR)
    underside = Vector((top_vert.x, top_vert.y, bar_z - radius))
    head_top, _ = extreme(head, lambda p: p.z, "max")
    dots = list(head_top)
    gaps = [gap("head-bar", top_vert, underside)]
    for label, pts in (("footL", foot_l), ("footR", foot_r), ("handL", hand_l), ("handR", hand_r)):
        low, zmin = extreme(pts, lambda p: p.z, "min")
        if label.startswith("foot"):
            dots.extend(low)
        ground = Vector((low[0].x, low[0].y, 0.0))
        gaps.append(gap(label + "-ground", low[0], ground))
        print("MESH", label, "minz", round(zmin, 4))
    hip_bone = bone_head(arm, "Hips").z
    thigh = bone_head(arm, "LowerLeg_L") - bone_head(arm, "UpperLeg_L")
    shin = bone_head(arm, "Foot_L") - bone_head(arm, "LowerLeg_L")
    print(
        "POSE slide kneeDeg", round(ang(thigh, shin), 1),
    )
    print(
        "POSE slide hipBone_cm", cm(hip_bone),
        "hipMesh_cm", cm(min(p.z for p in hips)),
        "headClear_cm", cm(clearance),
        "leadY", round(min(foot_l, key=lambda p: p.y).y, 3),
        "headY", round(max(head, key=lambda p: p.y).y, 3),
    )
    return {"dots": dots, "gaps": gaps, "side": body_left(arm), "focus": bone_head(arm, "Hips")}


def line_point(point, a, b):
    delta = b - a
    length = delta.length
    if length < 1e-6:
        return a
    t = (point - a).dot(delta) / (length * length)
    t = max(0.0, min(1.0, t))
    return a + delta * t


def build_zip(arm):
    hands = []
    dots = []
    tops = []
    for name in ("Mesh_Hand_L", "Mesh_Hand_R"):
        pts = mesh_world(name)
        top = max(pts, key=lambda p: p.z)
        tops.append(top)
        pick, _ = extreme(pts, lambda p: p.z, "max")
        dots.extend(pick)
        hands.append(pts)
    radius = 0.016
    a = Vector((tops[0].x, tops[0].y, tops[0].z + radius))
    b = Vector((tops[1].x, tops[1].y, tops[1].z + radius))
    direction = b - a
    if direction.length < 0.05:
        direction = Vector((1.0, 0.0, 0.0))
    direction = direction.normalized()
    a = a - direction * 1.6
    b = b + direction * 1.6
    # Lift until the cable surface meets the mesh instead of burying it.
    for _ in range(4):
        buried = 0.0
        for pts in hands:
            nearest = min(pts, key=lambda p: (p - line_point(p, a, b)).length)
            signed = (nearest - line_point(nearest, a, b)).length - radius
            if signed < buried:
                buried = signed
        if buried >= -0.001:
            break
        a.z -= buried
        b.z -= buried
    add_cyl("PropCable", a, b, radius, CABLE)
    gaps = []
    for label, pts in (("handL", hands[0]), ("handR", hands[1])):
        nearest = min(pts, key=lambda p: (p - line_point(p, a, b)).length)
        on_line = line_point(nearest, a, b)
        dist = (nearest - on_line).length
        surface = on_line + (nearest - on_line).normalized() * radius if dist > 1e-6 else on_line
        gaps.append(gap(label + "-cable", nearest, surface))
        print("MESH", label, "cableSigned_cm", cm(dist - radius))
    for name in ("Mesh_Foot_L", "Mesh_Foot_R", "Mesh_Head"):
        pts = mesh_world(name)
        pick, _ = extreme(pts, lambda p: p.z, "min" if "Foot" in name else "max")
        dots.extend(pick[:12])
    side = horiz(direction.cross(Vector((0.0, 0.0, 1.0))))
    focus = (tops[0] + tops[1]) * 0.5
    return {"dots": dots, "gaps": gaps, "side": side, "focus": focus}


def build_grapple(arm):
    pts = mesh_world("Mesh_Hand_R")
    center = sum(pts, Vector()) / len(pts)
    forearm = bone_tail(arm, "Hand_R") - bone_head(arm, "LowerArm_R")
    if forearm.length < 0.05:
        forearm = Vector((0.0, 0.0, 1.0))
    forearm = forearm.normalized()
    grip = center
    end = grip + forearm * 1.7
    radius = 0.012
    add_cyl("PropRope", grip, end, radius, ROPE)
    nearest = min(pts, key=lambda p: (p - line_point(p, grip, end)).length)
    on_line = line_point(nearest, grip, end)
    dist = (nearest - on_line).length
    surface = on_line + (nearest - on_line).normalized() * radius if dist > 1e-6 else on_line
    dots, _ = extreme(pts, lambda p: p.dot(forearm), "max")
    for name, mode in (("Mesh_Hand_L", "min"), ("Mesh_Foot_L", "min"), ("Mesh_Foot_R", "min"), ("Mesh_Head", "max")):
        extra, _ = extreme(mesh_world(name), lambda p: p.z, mode)
        dots.extend(extra[:12])
    print("MESH handR ropeSigned_cm", cm(dist - radius))
    side = body_left(arm)
    return {
        "dots": dots,
        "gaps": [gap("handR-rope", nearest, surface)],
        "side": side,
        "focus": grip,
    }


def build_pad(arm):
    feet = mesh_world("Mesh_Foot_L") + mesh_world("Mesh_Foot_R")
    hips = mesh_world("Mesh_Hips")
    pts = feet + hips
    x0, x1 = min(p.x for p in pts), max(p.x for p in pts)
    y0, y1 = min(p.y for p in pts), max(p.y for p in pts)
    margin = 0.35
    width = max(1.4, (x1 - x0) + margin * 2)
    depth = max(1.4, (y1 - y0) + margin * 2)
    cx = (x0 + x1) * 0.5
    cy = (y0 + y1) * 0.5
    top = 0.08
    pad = add_box("PropPad", (cx, cy, top * 0.5), (width, depth, top), PAD, 0.4)
    world_top = max(p.z for p in obj_corners(pad))
    gaps = []
    dots = []
    for label, name in (("footL", "Mesh_Foot_L"), ("footR", "Mesh_Foot_R")):
        pts = mesh_world(name)
        low, _ = extreme(pts, lambda p: p.z, "min")
        dots.extend(low)
        gaps.append(gap(label + "-pad", low[0], Vector((low[0].x, low[0].y, world_top))))
    head, _ = extreme(mesh_world("Mesh_Head"), lambda p: p.z, "max")
    dots.extend(head[:12])
    hip_low = min(hips, key=lambda p: p.z)
    gaps.append(gap("hip-pad", hip_low, Vector((hip_low.x, hip_low.y, world_top))))
    thigh = bone_head(arm, "LowerLeg_L") - bone_head(arm, "UpperLeg_L")
    shin = bone_head(arm, "Foot_L") - bone_head(arm, "LowerLeg_L")
    thigh_r = bone_head(arm, "LowerLeg_R") - bone_head(arm, "UpperLeg_R")
    shin_r = bone_head(arm, "Foot_R") - bone_head(arm, "LowerLeg_R")
    print(
        "PROP pad worldTop", round(world_top, 4),
        "kneeL", round(ang(thigh, shin), 1),
        "kneeR", round(ang(thigh_r, shin_r), 1),
        "footL_cm", cm(min(p.z for p in mesh_world("Mesh_Foot_L")) - world_top),
        "footR_cm", cm(min(p.z for p in mesh_world("Mesh_Foot_R")) - world_top),
    )
    return {"dots": dots, "gaps": gaps, "side": body_left(arm), "focus": Vector((cx, cy, 1.0))}


BUILD = {
    "vault": build_vault,
    "climb": build_climb,
    "slide": build_slide,
    "zip": build_zip,
    "grapple": build_grapple,
    "pad": build_pad,
}


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
    shadow.scale = (0.42, 0.26, 0.012)
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
    cam.name = "Camera"
    beauty_camera(cam)
    bpy.context.scene.camera = cam
    arm.hide_render = True

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = SAMPLES
    scene.render.resolution_x = RES_X
    scene.render.resolution_y = RES_Y
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.exposure = 0.0
    print("CAMERA", tuple(round(v, 3) for v in cam.location), "dist", CAM_DIST, "lens", CAM_LENS)
    return scene, cam, shadow


def beauty_camera(cam):
    cam.data.type = "PERSP"
    cam.data.lens = CAM_LENS
    cam.data.sensor_fit = "AUTO"
    cam.location = CAM_LOOK + CAM_DIR * CAM_DIST
    look_at(cam, CAM_LOOK)
    cam.data.clip_start = 0.1
    cam.data.clip_end = 200.0


def apply_pose(arm, fn, lift, yaw=0.0):
    clear_pose(arm)
    fn(arm)
    arm.rotation_mode = "XYZ"
    arm.rotation_euler = Euler((0.0, 0.0, rad(yaw)), "XYZ")
    arm.location = (0.0, 0.0, lift)
    bpy.context.view_layer.update()


def cam_z(cam, point):
    return (cam.matrix_world.inverted() @ point).z


def ray_name(scene, origin, target):
    direction = target - origin
    dist = direction.length
    if dist < 1e-5:
        return None
    deps = bpy.context.evaluated_depsgraph_get()
    hit, _loc, _normal, _idx, obj, _matrix = scene.ray_cast(
        deps, origin, direction.normalized(), distance=dist - 0.02
    )
    if not hit or obj is None:
        return None
    return obj.name


def choose_climb_yaw(arm, cam, scene):
    """Camera on the runner's side, looking at the face they climb.

    Pass 16 scored abs(view · forward) and kept the yaw that put the wall
    between the camera and the chest. The wall then hid the body.
    """
    beauty_camera(cam)
    bpy.context.view_layer.update()
    best = None
    view = horiz(CAM_LOOK - cam.location)
    for step in range(36):
        yaw = -180.0 + step * 10.0
        apply_pose(arm, pose_climb, 0.0, yaw)
        settle(arm, "climb")
        clear_props()
        build_climb(arm)
        forward = body_forward(arm)
        chest_pts = mesh_world("Mesh_Chest")
        chest = sum(chest_pts, Vector()) / len(chest_pts)
        hips = sum(mesh_world("Mesh_Hips"), Vector()) / len(mesh_world("Mesh_Hips"))
        wall = bpy.data.objects.get("PropWall")
        if wall is None:
            continue
        wall_c = wall.matrix_world.translation.copy()
        # Runner stands on the -forward side of the face. The camera must too.
        side = (cam.location - chest).dot(-forward)
        if side <= 0.35:
            continue
        # Chest and hips closer to the camera than the wall volume.
        if cam_z(cam, chest) < cam_z(cam, wall_c) + 0.02:
            continue
        if cam_z(cam, hips) < cam_z(cam, wall_c) + 0.02:
            continue
        blocked = False
        origin = cam.location + view * 0.15
        for target in (chest, hips):
            name = ray_name(scene, origin, target)
            if name == "PropWall":
                blocked = True
                break
        if blocked:
            continue
        facing = abs(view.dot(forward))
        # 3/4, not a pure side and not looking down the wall.
        if facing < 0.22 or facing > 0.72:
            continue
        score = abs(facing - 0.42) - min(side, 2.0) * 0.01
        if best is None or score < best[0]:
            best = (score, yaw, facing, side)
    if best is None:
        print("CLIMB_YAW fallback")
        return  -135.0, 0.0
    _score, yaw, facing, side = best
    print("CLIMB_YAW", round(yaw, 1), "facing", round(facing, 2), "runnerSide", round(side, 2))
    return yaw, facing


def choose_yaw(arm, cam, pose, lift, kind, prefer):
    best = None
    for step in range(24):
        yaw = -180.0 + step * 15.0
        apply_pose(arm, pose, lift, yaw)
        settle(arm, kind)
        forward = body_forward(arm)
        chest_pts = mesh_world("Mesh_Chest")
        chest = sum(chest_pts, Vector()) / len(chest_pts)
        ahead = chest + forward * 0.35
        if cam_z(cam, chest) > cam_z(cam, ahead) - 0.02:
            continue
        view = horiz(CAM_LOOK - cam.location)
        facing = abs(view.dot(forward))
        score = abs(facing - prefer)
        if best is None or score < best[0]:
            best = (score, yaw, facing)
    if best is None:
        return 90.0, 0.0
    _, yaw, facing = best
    return yaw, facing


def place_shadow(shadow, arm, kind, info):
    hip = bone_head(arm, "Hips")
    if kind == "vault" and info is not None:
        top = max(g["b"].z for g in info["gaps"] if g["name"].endswith("boxTop"))
        shadow.location = (hip.x, hip.y, top + 0.008)
        shadow.scale = (0.34, 0.22, 0.01)
    else:
        shadow.location = (hip.x, hip.y + 0.02, 0.01)
        shadow.scale = (0.42, 0.26, 0.012)


def project(scene, cam, point):
    ndc = world_to_camera_view(scene, cam, Vector(point))
    return ndc.x * RES_X, (1.0 - ndc.y) * RES_Y


def fit_ortho(scene, cam, pts, side):
    center = sum((Vector(p) for p in pts), Vector()) / len(pts)
    cam.data.type = "ORTHO"
    cam.data.sensor_fit = "VERTICAL"
    cam.data.clip_start = 0.01
    cam.data.clip_end = 80.0
    height_m = 3.0
    view_z = side.normalized()
    for _ in range(6):
        cam.data.ortho_scale = height_m
        cam.location = center + view_z * 7.0
        look_at(cam, center)
        bpy.context.view_layer.update()
        proj = [world_to_camera_view(scene, cam, Vector(p)) for p in pts]
        minx = min(p.x for p in proj)
        maxx = max(p.x for p in proj)
        miny = min(p.y for p in proj)
        maxy = max(p.y for p in proj)
        cx = (minx + maxx) * 0.5
        cy = (miny + maxy) * 0.5
        view_h = cam.data.ortho_scale
        view_w = view_h * (RES_X / RES_Y)
        right = cam.matrix_world.to_3x3() @ Vector((1.0, 0.0, 0.0))
        upv = cam.matrix_world.to_3x3() @ Vector((0.0, 1.0, 0.0))
        center = center + right * ((cx - 0.5) * view_w) + upv * ((cy - 0.5) * view_h)
        span_y = max(0.4, (maxy - miny) * view_h)
        span_x = max(0.4, (maxx - minx) * view_w)
        height_m = max(span_y, span_x * RES_Y / RES_X) * 1.22
    cam.data.ortho_scale = height_m
    cam.location = center + view_z * 7.0
    look_at(cam, center)
    bpy.context.view_layer.update()
    # A 10 cm vertical pair must come back as 10 cm from the pixel scale.
    base = Vector((center.x, center.y, center.z))
    raised = base + Vector((0.0, 0.0, 0.10))
    ax, ay = project(scene, cam, base)
    bx, by = project(scene, cam, raised)
    mpp = cam.data.ortho_scale / RES_Y
    print("SCALE_CHECK", round(math.hypot(ax - bx, ay - by) * mpp * 100.0, 2))
    return mpp


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


def write_png(path, rows):
    height = len(rows)
    width = len(rows[0]) // 4

    def chunk(tag, data):
        crc = zlib.crc32(tag + data) & 0xFFFFFFFF
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", crc)

    raw = b"".join(b"\x00" + bytes(row) for row in rows)
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    blob = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b"")
    with open(path, "wb") as handle:
        handle.write(blob)


def stamp(rows, x, y, radius=3):
    height = len(rows)
    width = len(rows[0]) // 4
    cx, cy = int(round(x)), int(round(y))
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            if dx * dx + dy * dy > radius * radius:
                continue
            px, py = cx + dx, cy + dy
            if 0 <= px < width and 0 <= py < height:
                i = px * 4
                rows[py][i] = 255
                rows[py][i + 1] = 0
                rows[py][i + 2] = 0
                rows[py][i + 3] = 255


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


def render_ortho(scene, cam, path, info):
    cloud = []
    for item in info["gaps"]:
        cloud.append(item["a"])
        cloud.append(item["b"])
    cloud.extend(info["dots"])
    # Keep the ground in frame on the action stills.
    focus = info["focus"]
    cloud.append(Vector((focus.x, focus.y, 0.0)))
    mpp = fit_ortho(scene, cam, cloud, info["side"])
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.light_add(type="SUN", location=cam.location)
    lamp = bpy.context.active_object
    lamp.name = "PropOrthoFill"
    lamp.data.energy = 1.4
    lamp.data.color = (1.0, 0.98, 0.94)
    look_at(lamp, focus)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    _w, _h, rows = png_rows(path)
    for point in info["dots"]:
        x, y = project(scene, cam, point)
        stamp(rows, x, y)
    write_png(path, rows)
    bpy.data.objects.remove(lamp, do_unlink=True)
    for item in info["gaps"]:
        ax, ay = project(scene, cam, item["a"])
        bx, by = project(scene, cam, item["b"])
        pixels = math.hypot(ax - bx, ay - by)
        px_cm = pixels * mpp * 100.0
        print(
            "GAP",
            os.path.basename(path).replace("-ortho.png", ""),
            item["name"],
            "px", round(pixels, 1),
            "px_cm", round(px_cm, 1),
            "world_cm", item["cm"],
        )


def pose_vault_from(arm):
    # Hands reaching the box. The kept vault pose is the over-box end of the blend.
    torso(arm, 16.0, 8.0, -6.0)
    set_euler(arm, "Chest", 2.0, 0.0, 0.0)
    arm_pose(arm, "L", -24.0, 14.0, -55.0, 8.0, -4.0)
    arm_pose(arm, "R", -24.0, 14.0, -55.0, 8.0, -4.0)
    leg(arm, "L", 55.0, -40.0, -24.0)
    leg(arm, "R", 48.0, -36.0, 20.0)


def pose_climb_from(arm):
    # Lower reach. The kept climb pose is the hands-on-lip end.
    torso(arm, 12.0, 4.0, -6.0)
    arm_pose(arm, "L", -90.0, 14.0, -36.0, -6.0)
    arm_pose(arm, "R", -90.0, 14.0, -36.0, -6.0)
    leg(arm, "L", 70.0, -80.0, 18.0)
    leg(arm, "R", 32.0, -40.0, 8.0)
    set_euler(arm, "Foot_L", -10.0, 0.0, 0.0)


def pose_land(arm):
    # Hard absorb the roll leaves and returns to. Matches LandPose / RollShot.
    leg(arm, "L", 74.0, -125.0)
    leg(arm, "R", 74.0, -125.0)
    arm_pose(arm, "L", -36.0, 8.0, -18.0, 0.0)
    arm_pose(arm, "R", 28.0, 8.0, -24.0, 0.0)
    torso(arm, 46.0, 28.0, 8.0)


def pose_roll_peak(arm):
    # HandoffFeel roll tuck, keyed so a 30 fps step stays inside 25 degrees.
    leg(arm, "L", 68.0, -116.0)
    leg(arm, "R", 62.0, -112.0)
    arm_pose(arm, "L", -28.0, 6.0, -40.0, 0.0)
    arm_pose(arm, "R", 10.0, 6.0, -40.0, 0.0)
    torso(arm, 34.0, 32.0, 12.0)
    set_euler(arm, "Hips", 34.0, 0.0, 6.0)


def build_wall_run(arm):
    left = body_left(arm)
    forward = body_forward(arm)
    foot = mesh_world("Mesh_Foot_L")
    hand = mesh_world("Mesh_Hand_L")
    cover = foot + hand
    face_pt = max(cover, key=lambda p: p.dot(left))
    top = max(max(p.z for p in hand) + 0.45, 2.05)
    depth = 0.42
    spans = [(p - face_pt).dot(forward) for p in cover]
    length = max(2.6, (max(spans) - min(spans)) + 1.8)
    along = (min(spans) + max(spans)) * 0.5
    center = face_pt + left * (depth * 0.5) + forward * along
    center.z = top * 0.5
    rot = left.to_track_quat("Y", "Z").to_euler()
    wall = add_box("PropWall", center, (length, depth, top), WALL, 0.72, rot, emit=0.35)
    corners = obj_corners(wall)
    plane = min(corners, key=lambda p: p.dot(left))
    gaps = []
    dots = []
    for label, pts in (("footL", foot), ("handL", hand)):
        front, _ = extreme(pts, lambda p: p.dot(left), "max")
        dots.extend(front)
        hit = front[0] - left * (front[0].dot(left) - plane.dot(left))
        gaps.append(gap(label + "-wall", front[0], hit))
    print(
        "PROP wall face", round(plane.dot(left), 4),
        "foot", round(max(foot, key=lambda p: p.dot(left)).dot(left), 4),
    )
    return {
        "dots": dots,
        "gaps": gaps,
        "side": forward,
        "focus": bone_head(arm, "Chest"),
        "left": left,
        "plane": plane,
        "forward": forward,
    }


def ensure_pose(arm):
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")


def capture_pose(arm):
    pose = {}
    for bone in arm.pose.bones:
        euler = bone.rotation_euler
        pose[bone.name] = (math.degrees(euler.x), math.degrees(euler.y), math.degrees(euler.z))
    return pose


def slerp_pose(a, b, weight):
    w = 0.0 if weight < 0.0 else (1.0 if weight > 1.0 else weight)
    out = {}
    for name in set(a) | set(b):
        ax, ay, az = a.get(name, (0.0, 0.0, 0.0))
        bx, by, bz = b.get(name, (0.0, 0.0, 0.0))
        qa = Euler((rad(ax), rad(ay), rad(az)), "XYZ").to_quaternion()
        qb = Euler((rad(bx), rad(by), rad(bz)), "XYZ").to_quaternion()
        euler = qa.slerp(qb, w).to_euler("XYZ")
        out[name] = (math.degrees(euler.x), math.degrees(euler.y), math.degrees(euler.z))
    return out


def apply_dict(arm, pose, yaw, lift):
    ensure_pose(arm)
    clear_pose(arm)
    for name, (xdeg, ydeg, zdeg) in pose.items():
        if name in arm.pose.bones:
            set_euler(arm, name, xdeg, ydeg, zdeg)
    arm.rotation_mode = "XYZ"
    arm.rotation_euler = Euler((0.0, 0.0, rad(yaw)), "XYZ")
    arm.location = (0.0, 0.0, lift)
    bpy.context.view_layer.update()


def grab(arm, fn, yaw, kind, lift):
    ensure_pose(arm)
    apply_pose(arm, fn, lift, yaw)
    settle(arm, kind)
    z = arm.location.z
    return capture_pose(arm), z


TRACK = (
    "Hips", "Spine", "Chest", "Head",
    "UpperArm_L", "LowerArm_L", "Hand_L",
    "UpperArm_R", "LowerArm_R", "Hand_R",
    "UpperLeg_L", "LowerLeg_L", "Foot_L",
    "UpperLeg_R", "LowerLeg_R", "Foot_R",
)


def bone_quats(arm):
    bpy.context.view_layer.update()
    quats = {}
    for name in TRACK:
        quats[name] = arm.pose.bones[name].matrix.to_quaternion()
    return quats


def quat_deg(a, b):
    angle = math.degrees(a.rotation_difference(b).angle)
    if angle < 0.0:
        angle = -angle
    if angle > 180.0:
        angle = 360.0 - angle
    return angle


def raised(u):
    if u <= 0.0:
        return 0.0
    if u >= 1.0:
        return 1.0
    return 0.5 * (1.0 - math.cos(u * math.pi))


def wall_arc(age):
    # BodyLine.WallArc. BeatSeconds 0.15 + EaseSeconds 0.12. Not rewritten.
    span = 0.27
    if age <= 0.0 or span <= 0.0:
        return 0.0
    u = age / span
    if u >= 1.0:
        return 1.0
    return 0.5 - 0.5 * math.cos(u * math.pi)


def roll_weight(u):
    if u <= 0.0 or u >= 1.0:
        return 0.0
    if u < 0.5:
        return raised(u / 0.5)
    return raised((1.0 - u) / 0.5)


def soften(game, linear, bias):
    # bias 0 keeps the in-game curve. bias 1 is the flat visual ease.
    b = 0.0 if bias < 0.0 else (1.0 if bias > 1.0 else bias)
    return (1.0 - b) * game + b * linear


def cluster(pts, key, mode, tol=0.02):
    pivot = min(key(p) for p in pts) if mode == "min" else max(key(p) for p in pts)
    if mode == "min":
        chosen = [p for p in pts if key(p) <= pivot + tol]
    else:
        chosen = [p for p in pts if key(p) >= pivot - tol]
    return sum(chosen, Vector()) / len(chosen), pivot


ANCHOR = {
    "handL": "Hand_L",
    "handR": "Hand_R",
    "footL": "Foot_L",
    "footR": "Foot_R",
    "chest": "Chest",
    "hips": "Hips",
}


def contact_cloud(arm, kind, surfaces):
    """Planted from the mesh gap. Slide is the bone-head move, which is where the part sits."""
    cloud = {}

    def put(label, planted):
        cloud[label] = (bone_head(arm, ANCHOR[label]), planted)

    if kind == "vault":
        top = surfaces["top"]
        for label, mesh in (("handL", "Mesh_Hand_L"), ("handR", "Mesh_Hand_R")):
            _point, zmin = cluster(mesh_world(mesh), lambda p: p.z, "min")
            put(label, abs(zmin - top) <= 0.02)
    elif kind == "climb":
        top = surfaces["top"]
        forward = surfaces["forward"]
        plane = surfaces["plane"]
        for label, mesh in (("handL", "Mesh_Hand_L"), ("handR", "Mesh_Hand_R")):
            _point, zmin = cluster(mesh_world(mesh), lambda p: p.z, "min")
            put(label, abs(zmin - top) <= 0.02)
        for label, mesh in (("chest", "Mesh_Chest"), ("footL", "Mesh_Foot_L")):
            point, _along = cluster(mesh_world(mesh), lambda p: p.dot(forward), "max")
            dist = (point - plane).dot(forward)
            put(label, abs(dist) <= 0.02)
    elif kind == "slide":
        for label, mesh in (("footL", "Mesh_Foot_L"), ("footR", "Mesh_Foot_R")):
            _point, zmin = cluster(mesh_world(mesh), lambda p: p.z, "min")
            put(label, zmin <= 0.02)
    elif kind == "wall":
        left = surfaces["left"]
        plane = surfaces["plane"]
        for label, mesh in (("footL", "Mesh_Foot_L"), ("handL", "Mesh_Hand_L")):
            point, _along = cluster(mesh_world(mesh), lambda p: p.dot(left), "max")
            dist = (point - plane).dot(left)
            put(label, abs(dist) <= 0.02)
    elif kind == "roll":
        for label, mesh in (("footL", "Mesh_Foot_L"), ("footR", "Mesh_Foot_R"), ("hips", "Mesh_Hips")):
            _point, zmin = cluster(mesh_world(mesh), lambda p: p.z, "min")
            put(label, zmin <= 0.02)
    return cloud


def slide_cm(prev, now):
    worst_name = ""
    worst = 0.0
    if prev is None:
        return worst_name, worst
    for name, (point, planted) in now.items():
        if not planted or name not in prev or not prev[name][1]:
            continue
        delta = (point - prev[name][0]).length * 100.0
        if delta > worst:
            worst = delta
            worst_name = name
    return worst_name, worst


def blue_count(path):
    _w, height, rows = png_rows(path)
    count = 0
    miny, maxy = height, -1
    minx, maxx = _w, -1
    for y, row in enumerate(rows):
        for x in range(_w):
            i = x * 4
            r, g, b = row[i], row[i + 1], row[i + 2]
            if b > r + 40 and g > r + 15 and b > 90 and r < 150 and b > g:
                count += 1
                if y < miny:
                    miny = y
                if y > maxy:
                    maxy = y
                if x < minx:
                    minx = x
                if x > maxx:
                    maxx = x
    span = (maxy - miny) if count else 0
    wide = (maxx - minx) if count else 0
    print("BLUE", os.path.basename(path), "px", count, "span", span, "wide", wide, "y", miny, maxy)
    return count, span


def choose_wall_yaw(arm, cam, scene):
    beauty_camera(cam)
    bpy.context.view_layer.update()
    best = None
    view = horiz(CAM_LOOK - cam.location)
    for step in range(36):
        yaw = -180.0 + step * 10.0
        ensure_pose(arm)
        apply_pose(arm, pose_wall_run, 0.0, yaw)
        settle(arm, "none")
        clear_props()
        info = build_wall_run(arm)
        left = info["left"]
        chest = sum(mesh_world("Mesh_Chest"), Vector()) / len(mesh_world("Mesh_Chest"))
        wall = bpy.data.objects.get("PropWall")
        if wall is None:
            continue
        side = (cam.location - chest).dot(-left)
        if side <= 0.8:
            continue
        if cam_z(cam, chest) < cam_z(cam, wall.matrix_world.translation) + 0.02:
            continue
        origin = cam.location + view * 0.15
        if ray_name(scene, origin, chest) == "PropWall":
            continue
        forward = body_forward(arm)
        facing = abs(view.dot(forward))
        if facing < 0.18 or facing > 0.75:
            continue
        score = abs(facing - 0.40)
        if best is None or score < best[0]:
            best = (score, yaw, facing, side)
    clear_props()
    if best is None:
        print("WALL_YAW fallback")
        return -60.0, 0.0
    _score, yaw, facing, side = best
    print("WALL_YAW", round(yaw, 1), "facing", round(facing, 2), "runnerSide", round(side, 2))
    return yaw, facing


def blend_weight(kind, age, bias):
    """In-game clock, then a visual ease if the posed step would pop.

    The seconds are the gameplay clocks. bias only flattens the visual curve.
    """
    if kind == "vault":
        # KneeEnd 0.55 of MantleWindow 0.40. VaultShown makes each beat linear.
        end = 0.55 * 0.40
        game = 0.0 if age <= 0.0 else (1.0 if age >= end else age / end)
        linear = game
        return soften(game, linear, bias), game
    if kind == "climb":
        game = raised(age / 0.18)
        linear = 0.0 if age <= 0.0 else (1.0 if age >= 0.18 else age / 0.18)
        return soften(game, linear, bias), game
    if kind == "slide":
        game = raised(age / 0.26)
        linear = 0.0 if age <= 0.0 else (1.0 if age >= 0.26 else age / 0.26)
        return soften(game, linear, bias), game
    if kind == "roll":
        span = 1.0 / 3.1
        u = 0.0 if age <= 0.0 else (1.0 if age >= span else age / span)
        game = roll_weight(u)
        if u <= 0.0 or u >= 1.0:
            linear = 0.0
        elif u < 0.5:
            linear = u / 0.5
        else:
            linear = (1.0 - u) / 0.5
        return soften(game, linear, bias), game
    # Wall run into the shove over WallSeconds 0.12, then WallArc across 0.27.
    if age <= 0.12:
        game = raised(age / 0.12)
        linear = age / 0.12
        return ("push", soften(game, linear, bias), game)
    arc_age = age - 0.12
    game = wall_arc(arc_age)
    linear = 0.0 if arc_age <= 0.0 else (1.0 if arc_age >= 0.27 else arc_age / 0.27)
    return ("air", soften(game, linear, bias), game)


def posed_frame(kind, age, bias, keys, yaws, lifts):
    if kind == "wall":
        phase, weight, _game = blend_weight(kind, age, bias)
        if phase == "push":
            pose = slerp_pose(keys["run"], keys["push"], weight)
            lift = lifts["run"] + (lifts["push"] - lifts["run"]) * weight
        else:
            pose = slerp_pose(keys["push"], keys["air"], weight)
            lift = lifts["push"] + (lifts["air"] - lifts["push"]) * weight
        return pose, lift
    weight, _game = blend_weight(kind, age, bias)
    pose = slerp_pose(keys["a"], keys["b"], weight)
    lift = lifts["a"] + (lifts["b"] - lifts["a"]) * weight
    return pose, lift


def contact_point(mesh, mode, axis):
    pts = mesh_world(mesh)
    if mode == "z":
        point, _pivot = cluster(pts, lambda p: p.z, "min")
        return point
    point, _pivot = cluster(pts, lambda p: p.dot(axis), "max")
    return point


def pin_contacts(arm, pins):
    """Hold a planted bone head on the hero contact. Far reaches are left alone."""
    for pin in pins:
        anchor = pin["anchor"]
        target = pin["point"]
        for _attempt in range(6):
            dist = (bone_head(arm, anchor) - target).length
            if dist < 0.008 or dist > 0.20:
                break
            moved = False
            for bone_name in pin["bones"]:
                bone = arm.pose.bones[bone_name]
                base = bone.rotation_euler.copy()
                best = (dist, base.copy())
                for axis in range(3):
                    for deg in (-5.0, 5.0):
                        trial = base.copy()
                        trial[axis] += rad(deg)
                        bone.rotation_euler = Euler((trial.x, trial.y, trial.z), "XYZ")
                        bpy.context.view_layer.update()
                        delta = (bone_head(arm, anchor) - target).length
                        if delta < best[0] - 0.0004:
                            best = (delta, trial.copy())
                            moved = True
                bone.rotation_euler = Euler((best[1].x, best[1].y, best[1].z), "XYZ")
                bpy.context.view_layer.update()
                dist = best[0]
            if not moved:
                break


def show_frame(arm, kind, age, bias, job, surfaces):
    pose, lift = posed_frame(kind, age, bias, job["keys"], None, job["lifts"])
    # The slide plant arrives with the ease. Early frames stay off the ground
    # so the opening knee is not a planted skate.
    apply_dict(arm, pose, job["yaw"], lift)
    if kind == "wall" and surfaces.get("left") is not None:
        phase, weight, _game = blend_weight(kind, age, bias)
        if phase == "air" and weight > 0.02:
            # The shove stays on the wall. The jump steps off, so the hand is not skating the face.
            arm.location = Vector(arm.location) + surfaces["left"] * (-0.18 * weight)
            bpy.context.view_layer.update()
    if kind == "slide":
        # Feet meet the ground on the last frames of the 0.26s open.
        # Earlier frames stay clear so the trail shoe is not skating a plant.
        weight, _game = blend_weight(kind, age, bias)
        sole = min(
            min(p.z for p in mesh_world("Mesh_Foot_L")),
            min(p.z for p in mesh_world("Mesh_Foot_R")),
        )
        target = 0.012 if weight >= 0.90 else 0.055
        arm.location.z += target - sole
        bpy.context.view_layer.update()
    pins = surfaces.get("pins") if surfaces else None
    if pins and not (kind == "wall" and age > 0.14):
        pin_contacts(arm, pins)
    return pose, lift


def measure_strip(arm, kind, bias, job, surfaces):
    prev_q = None
    prev_c = None
    worst_bone = ("", 0.0, -1)
    worst_slide = ("", 0.0, -1)
    for frame in range(8):
        age = frame / 30.0
        show_frame(arm, kind, age, bias, job, surfaces)
        quats_now = bone_quats(arm)
        contacts = contact_cloud(arm, kind, surfaces)
        if prev_q is not None:
            top_name = ""
            top_deg = 0.0
            for name, quat in quats_now.items():
                delta = quat_deg(prev_q[name], quat)
                if delta > top_deg:
                    top_deg = delta
                    top_name = name
            if top_deg > worst_bone[1]:
                worst_bone = (top_name, top_deg, frame - 1)
            cname, cslide = slide_cm(prev_c, contacts)
            if cslide > worst_slide[1]:
                worst_slide = (cname, cslide, frame - 1)
        prev_q = quats_now
        prev_c = contacts
    return worst_bone, worst_slide


def add_pins(arm, kind, surfaces):
    specs = ()
    if kind == "vault":
        specs = (
            ("Mesh_Hand_L", ("UpperArm_L", "LowerArm_L", "Hand_L"), "z", Vector((0.0, 0.0, 1.0))),
            ("Mesh_Hand_R", ("UpperArm_R", "LowerArm_R", "Hand_R"), "z", Vector((0.0, 0.0, 1.0))),
        )
    elif kind == "climb":
        specs = (
            ("Mesh_Hand_L", ("UpperArm_L", "LowerArm_L", "Hand_L"), "z", Vector((0.0, 0.0, 1.0))),
            ("Mesh_Hand_R", ("UpperArm_R", "LowerArm_R", "Hand_R"), "z", Vector((0.0, 0.0, 1.0))),
            ("Mesh_Foot_L", ("UpperLeg_L", "LowerLeg_L", "Foot_L"), "axis", surfaces["forward"]),
        )
    elif kind == "wall":
        specs = (
            ("Mesh_Foot_L", ("UpperLeg_L", "LowerLeg_L", "Foot_L"), "axis", surfaces["left"]),
            ("Mesh_Hand_L", ("UpperArm_L", "LowerArm_L", "Hand_L"), "axis", surfaces["left"]),
        )
    elif kind == "slide":
        specs = (
            ("Mesh_Foot_L", ("UpperLeg_L", "LowerLeg_L", "Foot_L"), "z", Vector((0.0, 0.0, 1.0))),
        )
    pins = []
    for mesh, bones, mode, axis in specs:
        anchor = {
            "Mesh_Hand_L": "Hand_L",
            "Mesh_Hand_R": "Hand_R",
            "Mesh_Foot_L": "Foot_L",
            "Mesh_Foot_R": "Foot_R",
        }[mesh]
        pins.append({
            "mesh": mesh,
            "bones": bones,
            "mode": mode,
            "axis": axis,
            "anchor": anchor,
            "point": bone_head(arm, anchor).copy(),
        })
    surfaces["pins"] = pins


def pick_bias(arm, kind, job, surfaces):
    chosen = 0.0
    bone = slide = None
    for bias in (0.0, 0.35, 0.7, 1.0):
        bone, slide = measure_strip(arm, kind, bias, job, surfaces)
        chosen = bias
        print(
            "POPTRY", kind, "bias", bias,
            "bone", bone[0], round(bone[1], 1), "pair", bone[2],
            "contact", slide[0] or "-", round(slide[1], 1), "cpair", slide[2],
        )
        if bone[1] <= 25.0 and slide[1] <= 3.0:
            break
    _name, _deg, pair = bone
    print(
        "POP", kind,
        "pair", str(pair) + "-" + str(pair + 1),
        "bone", bone[0], round(bone[1], 1),
        "contact", slide[0] or "-", round(slide[1], 1),
        "bias", round(chosen, 2),
    )
    return chosen, bone, slide


def render_beauty(scene, cam, shadow, arm, path, kind, info):
    place_shadow(shadow, arm, kind, info)
    beauty_camera(cam)
    bpy.context.view_layer.update()
    scene.eevee.taa_render_samples = SAMPLES
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    report_png(path)
    print("WROTE", path)


def main():
    global BUILD
    BUILD["pad-air"] = build_pad
    BUILD["wall"] = build_wall_run
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    ensure_pose(arm)
    tint()
    prove_pass15(arm)
    scene, cam, shadow = scene_setup(arm)
    ensure_pose(arm)
    beauty_camera(cam)

    climb_yaw, _facing = choose_climb_yaw(arm, cam, scene)
    clear_props()
    fit_wall_plant(arm)
    wall_yaw, _wf = choose_wall_yaw(arm, cam, scene)
    clear_props()
    ensure_pose(arm)
    pad_yaw, pad_facing = choose_yaw(arm, cam, pose_pad, 0.0, "pad", 0.40)
    print("PAD_YAW", round(pad_yaw, 1), "facing", round(pad_facing, 2))
    print("VAULT_YAW", VAULT_YAW, "SLIDE_YAW", SLIDE_YAW)

    stills = (
        ("vault", pose_vault, 0.0, "vault", VAULT_YAW),
        ("climb", pose_climb, 0.0, "climb", climb_yaw),
        ("slide", pose_slide, 0.0, "slide", SLIDE_YAW),
        ("pad", pose_pad, 0.0, "pad", pad_yaw),
        ("pad-air", pose_pad, 0.42, "pad-air", pad_yaw),
        ("wall", pose_wall_run, 0.0, "wall", wall_yaw),
    )

    if not DO_RENDER:
        for name, fn, lift, prop, yaw in stills:
            if ONLY and name not in ONLY:
                continue
            ensure_pose(arm)
            apply_pose(arm, fn, lift, yaw)
            settle(arm, prop)
            clear_props()
            info = BUILD[prop](arm)
            for item in info["gaps"]:
                print("GAP", name, item["name"], "world_cm", item["cm"])
    elif not ONLY or any(name in ONLY for name, *_rest in stills):
        for name, fn, lift, prop, yaw in stills:
            if ONLY and name not in ONLY:
                continue
            ensure_pose(arm)
            apply_pose(arm, fn, lift, yaw)
            settle(arm, prop)
            clear_props()
            info = BUILD[prop](arm)
            for item in info["gaps"]:
                print("GAP", name, item["name"], "world_cm", item["cm"])
            # The wall still is the planted contact for the strip. Beauty of the
            # run is frame 0 of the strip; the ortho is the contact check.
            if name != "wall":
                render_ortho(scene, cam, os.path.join(OUT, name + "-ortho.png"), info)
                render_beauty(scene, cam, shadow, arm, os.path.join(OUT, name + ".png"), prop, info)
                if name == "climb":
                    blue_count(os.path.join(OUT, name + ".png"))
            else:
                render_ortho(scene, cam, os.path.join(OUT, "wall-ortho.png"), info)

    if not DO_STRIPS:
        return
    if ONLY and not any(item.startswith("strip") for item in ONLY):
        return

    ensure_pose(arm)
    vault_a, vault_az = grab(arm, pose_vault_from, VAULT_YAW, "none", 0.0)
    vault_b, vault_bz = grab(arm, pose_vault, VAULT_YAW, "none", 0.0)
    # Hands and the chest stay on the box. The knees drive through the vault.
    for held in (
        "Hips", "Spine", "Chest", "Head",
        "UpperArm_L", "LowerArm_L", "Hand_L",
        "UpperArm_R", "LowerArm_R", "Hand_R",
    ):
        vault_a[held] = vault_b[held]
    climb_a, climb_az = grab(arm, pose_climb_from, climb_yaw, "climb", 0.0)
    climb_b, climb_bz = grab(arm, pose_climb, climb_yaw, "climb", 0.0)
    # Lead shoe stays on the face. The arms and the trail leg do the blend.
    for held in ("UpperLeg_L", "LowerLeg_L", "Foot_L"):
        climb_a[held] = climb_b[held]
    slide_a, slide_az = grab(arm, pose_stride, SLIDE_YAW, "none", 0.0)
    slide_b, slide_bz = grab(arm, pose_slide, SLIDE_YAW, "slide", 0.0)
    roll_a, roll_az = grab(arm, pose_land, SLIDE_YAW, "none", 0.0)
    # Ground the hard land on the shoes without a gameplay change.
    foot = min(min(p.z for p in mesh_world("Mesh_Foot_L")), min(p.z for p in mesh_world("Mesh_Foot_R")))
    roll_az = arm.location.z + (0.012 - foot)
    roll_b, _roll_bz = grab(arm, pose_roll_peak, SLIDE_YAW, "none", 0.0)
    foot = min(min(p.z for p in mesh_world("Mesh_Foot_L")), min(p.z for p in mesh_world("Mesh_Foot_R")))
    roll_bz = arm.location.z + (0.012 - foot)
    run_p, run_z = grab(arm, pose_wall_run, wall_yaw, "none", 0.0)
    push_p, push_z = grab(arm, pose_wall_push, wall_yaw, "none", 0.0)
    # Planted hand and shoe stay through the shove. The trail leg is the kick.
    for held in (
        "Hips", "Spine", "Chest", "Head",
        "UpperArm_L", "LowerArm_L", "Hand_L",
        "UpperLeg_L", "LowerLeg_L", "Foot_L",
    ):
        push_p[held] = run_p[held]
    air_p, _air_z = grab(arm, pose_apex, wall_yaw, "none", 0.28)

    jobs = {
        "vault": {
            "yaw": VAULT_YAW,
            "keys": {"a": vault_a, "b": vault_b},
            "lifts": {"a": vault_az, "b": vault_bz},
            "ref": pose_vault,
            "ref_kind": "vault",
            "ref_lift": 0.0,
        },
        "climb": {
            "yaw": climb_yaw,
            "keys": {"a": climb_a, "b": climb_b},
            "lifts": {"a": climb_az, "b": climb_bz},
            "ref": pose_climb,
            "ref_kind": "climb",
            "ref_lift": 0.0,
        },
        "slide": {
            "yaw": SLIDE_YAW,
            "keys": {"a": slide_a, "b": slide_b},
            "lifts": {"a": slide_az, "b": slide_bz},
            "ref": pose_slide,
            "ref_kind": "slide",
            "ref_lift": 0.0,
        },
        "wall": {
            "yaw": wall_yaw,
            "keys": {"run": run_p, "push": push_p, "air": air_p},
            "lifts": {"run": run_z, "push": push_z, "air": 0.28},
            "ref": pose_wall_run,
            "ref_kind": "wall",
            "ref_lift": 0.0,
        },
        "roll": {
            "yaw": SLIDE_YAW,
            "keys": {"a": roll_a, "b": roll_b},
            "lifts": {"a": roll_az, "b": roll_bz},
            "ref": pose_land,
            "ref_kind": "none",
            "ref_lift": 0.0,
        },
    }

    biases = {}
    for kind, job in jobs.items():
        if ONLY and ("strip-" + kind) not in ONLY and kind not in ONLY:
            continue
        ensure_pose(arm)
        apply_pose(arm, job["ref"], job["ref_lift"], job["yaw"])
        settle(arm, job["ref_kind"])
        clear_props()
        if kind == "roll":
            surfaces = {}
        elif kind == "wall":
            info = build_wall_run(arm)
            surfaces = {"left": info["left"], "plane": info["plane"]}
        elif kind == "vault":
            info = build_vault(arm)
            surfaces = {"top": max(item["b"].z for item in info["gaps"] if item["name"].endswith("boxTop"))}
        elif kind == "climb":
            info = build_climb(arm)
            forward = body_forward(arm)
            chest = mesh_world("Mesh_Chest")
            plane = max(chest, key=lambda p: p.dot(forward))
            surfaces = {
                "top": min(p.z for p in mesh_world("Mesh_Hand_L") + mesh_world("Mesh_Hand_R")),
                "forward": forward,
                "plane": plane,
            }
        else:
            build_slide(arm)
            surfaces = {}
        add_pins(arm, kind, surfaces)
        # Climb, slide, and the wall jump read from the keys. A hard pin
        # twists the wrist faster than the blend and shows up as a pop.
        if kind != "vault":
            surfaces["pins"] = []
        bias, _bone, _slide = pick_bias(arm, kind, job, surfaces)
        biases[kind] = (bias, surfaces, info if kind != "roll" and kind != "slide" else None)
        if kind == "slide":
            biases[kind] = (bias, surfaces, None)

    if not DO_RENDER:
        return

    strip_samples = int(os.environ.get("PASS17_STRIP_SAMPLES", "8"))
    for kind, job in jobs.items():
        if kind not in biases:
            continue
        if ONLY and ("strip-" + kind) not in ONLY and kind not in ONLY:
            continue
        bias, surfaces, _info = biases[kind]
        ensure_pose(arm)
        apply_pose(arm, job["ref"], job["ref_lift"], job["yaw"])
        settle(arm, job["ref_kind"])
        clear_props()
        info = None
        if kind == "vault":
            info = build_vault(arm)
        elif kind == "climb":
            info = build_climb(arm)
        elif kind == "slide":
            info = build_slide(arm)
        elif kind == "wall":
            info = build_wall_run(arm)
        for frame in range(8):
            age = frame / 30.0
            show_frame(arm, kind, age, bias, job, surfaces)
            path = os.path.join(OUT, kind + "-strip-" + str(frame) + ".png")
            place_shadow(shadow, arm, kind if kind != "roll" else "none", info)
            beauty_camera(cam)
            bpy.context.view_layer.update()
            scene.eevee.taa_render_samples = strip_samples
            scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            print("WROTE", path)


if __name__ == "__main__":
    main()
