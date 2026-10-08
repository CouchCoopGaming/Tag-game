"""FX kit stills. Hier mannequin, one pose per effect, screen flash on one pane.

No-clip measures evaluated meshes. Joined neighbours are exempt only within
3 cm of their joint (the child bone head). Depth above 0.5 cm fails. There is
no rest-pose credit. The authored hip/thigh overlap past that ball is a rig
failure and is drawn in red.
"""
import math
import os
import sys

import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.environ.get("FX_STILL_OUT", os.path.join(ROOT, "Docs", "FxStills", "pass1"))
SAMPLES = int(os.environ.get("FX_SAMPLES", "12"))
RES_X = 800
RES_Y = 450
MEASURE_ONLY = os.environ.get("FX_MEASURE") == "1"

PLAYER = (0.86, 0.55, 0.42, 1.0)
JOINT = (0.16, 0.17, 0.18, 1.0)
ACCENT = (0.45, 0.82, 0.86, 1.0)
GROUND = (0.42, 0.43, 0.44, 1.0)
SKY = (0.55, 0.64, 0.74, 1.0)
DUST = (0.20, 0.17, 0.14, 1.0)
SHOCK = (0.93, 0.90, 0.82, 1.0)
SPARK = (1.0, 0.86, 0.38, 1.0)
CYAN = (0.45, 0.88, 1.0, 1.0)
STAR = (1.0, 0.86, 0.28, 1.0)
RIM = (0.95, 0.28, 0.32, 1.0)
TAG = (0.95, 0.28, 0.32, 1.0)
WALL = (0.55, 0.52, 0.48, 1.0)
PAD = (0.78, 0.55, 0.18, 1.0)
ROPE = (0.55, 0.40, 0.22, 1.0)
SCUFF = (0.18, 0.16, 0.14, 1.0)

DEPTH_LIMIT = 0.005
JOINT_EXEMPT = 0.03


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


def pose_land(arm):
    leg(arm, "L", 22.0, 24.0, 4.0)
    leg(arm, "R", 16.0, 18.0, -4.0)
    arm_pose(arm, "L", 12.0, -8.0, 0.0, 0.0)
    arm_pose(arm, "R", 6.0, -6.0, 0.0, 0.0)
    torso(arm, 12.0, 6.0, -4.0)


def pose_roll(arm):
    leg(arm, "L", 10.0, 18.0, 3.0)
    leg(arm, "R", 6.0, 12.0, -3.0)
    arm_pose(arm, "L", 12.0, -8.0, 0.0, 0.0)
    arm_pose(arm, "R", 6.0, -6.0, 0.0, 0.0)
    torso(arm, 8.0, 4.0, -2.0)
    set_euler(arm, "Chest", 0.0, 0.0, 0.0)
    set_euler(arm, "Hips", 8.0, 0.0, 0.0)


def pose_grapple(arm):
    leg(arm, "L", 16.0, -18.0)
    leg(arm, "R", 8.0, -12.0)
    arm_pose(arm, "L", -46.0, -32.0, -8.0, -4.0)
    arm_pose(arm, "R", 8.0, -16.0, -8.0, 0.0)
    torso(arm, -6.0, 4.0, -2.0)


def pose_run(arm):
    leg(arm, "L", 14.0, -18.0, 4.0)
    leg(arm, "R", -8.0, -6.0, -4.0)
    arm_pose(arm, "L", 8.0, -8.0, 0.0, 0.0)
    arm_pose(arm, "R", -8.0, -6.0, 0.0, 0.0)
    torso(arm, 4.0, 2.0, 2.0)


def pose_stagger(arm):
    leg(arm, "L", 14.0, -22.0, 4.0)
    leg(arm, "R", -6.0, -12.0, -4.0)
    arm_pose(arm, "L", 8.0, -8.0, -6.0, 2.0)
    arm_pose(arm, "R", -6.0, -8.0, -6.0, 0.0)
    torso(arm, 6.0, 2.0, 2.0)


def pose_launch(arm):
    # Arms up and out, elbows soft, so the forearms stay off the neck and the other arm.
    leg(arm, "L", 22.0, -20.0, 6.0)
    leg(arm, "R", 18.0, -16.0, -6.0)
    arm_pose(arm, "L", -46.0, -32.0, -8.0, 4.0)
    arm_pose(arm, "R", -46.0, -32.0, -8.0, 4.0)
    torso(arm, 2.0, -2.0, -2.0)


def wall_run_angles():
    """WallPose.Run(+1, wallLeft: true) at the locked 9.5 m/s gait.

    The lane branch has no wall-run animation clip. The pose the game plays
    is WallPose.RunCycle, phase 3π/2. LeanZ is the 20° roll toward the wall.
    """
    idle, sprint = 0.35, 12.0
    t = (9.5 - idle) / (sprint - idle)
    t = 0.0 if t < 0.0 else (1.0 if t > 1.0 else t)
    weight = 1.0 - (1.0 - t) * (1.0 - t)
    front = 26.0 + (48.0 - 26.0) * weight
    back = 14.0 + (30.0 - 14.0) * weight
    thigh_l = -back - 2.2
    thigh_r = front
    along = (thigh_r - (-22.0)) / (48.0 - (-22.0))
    along = 0.0 if along < 0.0 else (1.0 if along > 1.0 else along)
    return {
        "thigh_l": thigh_l,
        "knee_l": -5.0,
        "foot_l": 6.0,
        "thigh_r": thigh_r,
        "knee_r": -5.0,
        "foot_r": -(thigh_r + (-5.0)),
        "pitch_l": 4.0 + (-1.0) * 3.0,
        "yaw_l": 36.0,
        "elbow_l": -18.0,
        "pitch_r": -78.0 + (24.0 - (-78.0)) * along,
        "yaw_r": -12.0,
        "elbow_r": -12.0 + (-36.0 - (-12.0)) * along,
        "hip": 6.0,
        "spine": 12.0,
        "head": -4.0,
        "lean": -20.0,
    }


def pose_wall(arm):
    # Legs are WallPose.Run(+1, wallLeft). Unity's LeanZ does not roll this
    # FBX, so the 20° lean is a hip roll toward the wall and the arms stay
    # on the open side. The lane branch has no clip file to sample.
    a = wall_run_angles()
    leg(arm, "L", a["thigh_l"], -a["knee_l"])
    leg(arm, "R", a["thigh_r"], -a["knee_r"])
    set_euler(arm, "Foot_L", -a["foot_l"], 0.0, 0.0)
    set_euler(arm, "Foot_R", -a["foot_r"], 0.0, 0.0)
    arm_pose(arm, "L", 10.0, -20.0, -30.0, 0.0, 80.0)
    arm_pose(arm, "R", -12.0, -14.0, -20.0, 0.0)
    set_euler(arm, "Hips", a["hip"] + 2.0, 0.0, -a["lean"])
    set_euler(arm, "Spine", 6.0, 0.0, 0.0)
    set_euler(arm, "Chest", 0.0, 0.0, 0.0)
    set_euler(arm, "Head", a["head"], 0.0, 0.0)
    print(
        "WALL_CLIP",
        "thigh", round(a["thigh_l"], 1), round(a["thigh_r"], 1),
        "lean", a["lean"],
    )


def pose_punch(arm):
    leg(arm, "L", 16.0, -24.0, 4.0)
    leg(arm, "R", -8.0, -12.0, -4.0)
    arm_pose(arm, "L", 6.0, -10.0, -6.0, 0.0)
    arm_pose(arm, "R", -40.0, -18.0, -8.0, 0.0)
    torso(arm, 4.0, 2.0, 2.0)


def look_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


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
            node.inputs["Roughness"].default_value = 0.48


def make_mat(name, color, rough=0.55, alpha=1.0, emit=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = color
    if "Roughness" in node.inputs:
        node.inputs["Roughness"].default_value = rough
    if "Alpha" in node.inputs:
        node.inputs["Alpha"].default_value = alpha
    if emit > 0.0 and "Emission Strength" in node.inputs:
        node.inputs["Emission Strength"].default_value = emit
        key = "Emission Color" if "Emission Color" in node.inputs else None
        if key:
            node.inputs[key].default_value = color
    if alpha < 0.999:
        mat.blend_method = "BLEND"
        if hasattr(mat, "shadow_method"):
            mat.shadow_method = "NONE"
    return mat


def body_meshes():
    return [obj for obj in bpy.data.objects if obj.type == "MESH" and obj.name.startswith("Mesh_")]


def clear_fx():
    keep = {"PropGround", "PropSlab", "PropSeam"}
    for obj in list(bpy.data.objects):
        if obj.name in keep:
            continue
        if obj.name.startswith("Fx") or obj.name.startswith("Prop"):
            bpy.data.objects.remove(obj, do_unlink=True)


def mesh_world(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    verts = [mw @ v.co for v in me.vertices]
    polys = [tuple(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    return verts, polys


def bone_of(obj):
    if obj.parent_bone:
        return obj.parent_bone
    if obj.name.startswith("Mesh_"):
        return obj.name[5:]
    return obj.name


def neighbours(arm):
    pairs = set()
    for bone in arm.data.bones:
        if bone.parent is None:
            continue
        pairs.add((bone.name, bone.parent.name))
        pairs.add((bone.parent.name, bone.name))
    return pairs


def joint_world(arm, a, b):
    child = b if arm.data.bones[b].parent and arm.data.bones[b].parent.name == a else a
    if child not in arm.pose.bones:
        return None
    return arm.matrix_world @ arm.pose.bones[child].head


def inside_depth(tree, point):
    """Penetration of a closed mesh. The ray follows the nearest face so a
    turned character does not change the result."""
    loc, normal, index, dist = tree.find_nearest(point)
    if loc is None or normal is None or dist < 0.00005:
        return 0.0
    if (loc - point).dot(normal) <= 0.0:
        return 0.0
    inward = -normal.normalized()
    hit, hit_n, _idx, _dist = tree.ray_cast(point + inward * 0.0004, inward)
    if hit is None or hit_n is None:
        return 0.0
    if (hit - point).dot(hit_n) <= 0.0:
        return 0.0
    return dist


def overlap_ids(tree_a, polys_a, tree_b):
    overlap = tree_a.overlap(tree_b)
    used = set()
    if not overlap:
        return used
    for ia, _ib in overlap:
        if ia < len(polys_a):
            used.update(polys_a[ia])
    return used


def pair_depth(tree_a, verts_a, polys_a, tree_b, joint):
    """Deepest penetration. Joined verts within 3 cm of the joint are exempt."""
    used = overlap_ids(tree_a, polys_a, tree_b)
    worst = 0.0
    for idx in used:
        if idx >= len(verts_a):
            continue
        point = verts_a[idx]
        if joint is not None and (point - joint).length <= JOINT_EXEMPT:
            continue
        depth = inside_depth(tree_b, point)
        if depth > worst:
            worst = depth
    return worst


def pack_body():
    packed = []
    for obj in body_meshes():
        verts, polys = mesh_world(obj)
        if len(verts) < 4 or not polys:
            continue
        tree = BVHTree.FromPolygons(verts, polys, epsilon=0.0)
        packed.append((obj, bone_of(obj), tree, verts, polys))
    return packed


def each_pair(arm):
    """Joined pairs only. Yields (name, depth_m) using the 3 cm joint-head exemption."""
    packed = pack_body()
    joined = neighbours(arm)
    rows = []
    for i in range(len(packed)):
        obj_a, bone_a, tree_a, verts_a, polys_a = packed[i]
        for j in range(i + 1, len(packed)):
            obj_b, bone_b, tree_b, verts_b, polys_b = packed[j]
            if (bone_a, bone_b) not in joined:
                continue
            joint = joint_world(arm, bone_a, bone_b)
            d_ab = pair_depth(tree_a, verts_a, polys_a, tree_b, joint)
            d_ba = pair_depth(tree_b, verts_b, polys_b, tree_a, joint)
            depth = d_ab if d_ab > d_ba else d_ba
            rows.append((obj_a.name + "|" + obj_b.name, depth))
    rows.sort(key=lambda row: row[1], reverse=True)
    return rows


def measure(arm, solids):
    bpy.context.view_layer.update()
    packed = pack_body()
    joined = neighbours(arm)
    self_max = 0.0
    world_max = 0.0
    fails = 0
    worst_pair = ""
    for i in range(len(packed)):
        obj_a, bone_a, tree_a, verts_a, polys_a = packed[i]
        for obj_s, tree_s in solids:
            depth = pair_depth(tree_a, verts_a, polys_a, tree_s, None)
            if depth > world_max:
                world_max = depth
                worst_pair = obj_a.name + ">" + obj_s.name
            if depth > DEPTH_LIMIT:
                fails += 1
        for j in range(i + 1, len(packed)):
            obj_b, bone_b, tree_b, verts_b, polys_b = packed[j]
            linked = (bone_a, bone_b) in joined
            joint = joint_world(arm, bone_a, bone_b) if linked else None
            d_ab = pair_depth(tree_a, verts_a, polys_a, tree_b, joint)
            d_ba = pair_depth(tree_b, verts_b, polys_b, tree_a, joint)
            depth = d_ab if d_ab > d_ba else d_ba
            if depth > self_max:
                self_max = depth
                worst_pair = obj_a.name + "|" + obj_b.name
            if depth > DEPTH_LIMIT:
                fails += 1
    return self_max, world_max, fails, worst_pair


def lowest(arm):
    low = 99.0
    for obj in body_meshes():
        verts, _polys = mesh_world(obj)
        for p in verts:
            if p.z < low:
                low = p.z
    return low


def apply_pose(arm, fn, lift=0.0, yaw=20.0):
    clear_pose(arm)
    fn(arm)
    arm.rotation_mode = "XYZ"
    arm.rotation_euler = Euler((0.0, 0.0, rad(yaw)), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += -low + 0.006 + lift
    bpy.context.view_layer.update()


def bone_pos(arm, name, tail=False):
    bone = arm.pose.bones[name]
    return arm.matrix_world @ (bone.tail if tail else bone.head)


def extreme(obj, axis, mode):
    verts, _ = mesh_world(obj)
    vals = [getattr(v, axis) for v in verts]
    return min(vals) if mode == "min" else max(vals)


def add_plane(name, location, scale, color, alpha=1.0, emit=0.0, rot=None):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    if rot is not None:
        obj.rotation_euler = rot
    obj.data.materials.append(make_mat(name + "Mat", color, 0.6, alpha, emit))
    return obj


def add_ico(name, location, radius, color, alpha=0.8, emit=0.4):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=radius, subdivisions=1, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(make_mat(name + "Mat", color, 0.4, alpha, emit))
    return obj


def add_torus(name, location, major, minor, color, alpha=0.85, emit=0.3):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major, minor_radius=minor, location=location, major_segments=48, minor_segments=8
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(make_mat(name + "Mat", color, 0.35, alpha, emit))
    return obj


def add_curve(name, points, radius, color, alpha=0.7, emit=0.2):
    curve = bpy.data.curves.new(name, type="CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for i, p in enumerate(points):
        spline.points[i].co = (p.x, p.y, p.z, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(make_mat(name + "Mat", color, 0.4, alpha, emit))
    return obj


def soft_image():
    img = bpy.data.images.get("FxSoftDisc")
    if img is not None:
        return img
    n = 48
    img = bpy.data.images.new("FxSoftDisc", n, n, alpha=True, float_buffer=True)
    pix = [0.0] * (n * n * 4)
    for y in range(n):
        for x in range(n):
            dx = (x + 0.5) / n - 0.5
            dy = (y + 0.5) / n - 0.5
            r = math.sqrt(dx * dx + dy * dy) * 2.0
            a = max(0.0, 1.0 - r)
            a = a * a
            i = (y * n + x) * 4
            pix[i] = 1.0
            pix[i + 1] = 1.0
            pix[i + 2] = 1.0
            pix[i + 3] = a
    img.pixels.foreach_set(pix)
    img.pack()
    return img


def soft_mat(name, color, strength=0.35):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = soft_image()
    transparent = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    emit.inputs["Color"].default_value = color
    emit.inputs["Strength"].default_value = strength
    nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
    nt.links.new(transparent.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def add_puff(name, location, size, color, strength=0.4):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size, size, size)
    obj.data.materials.append(soft_mat(name + "Mat", color, strength))
    return obj


def dust_ring(origin, ring, count, color):
    # Matte clouds outside the shock ring, rising to about knee height.
    for i in range(count):
        ang = (i + 0.37 * (i % 4)) / count * math.tau
        radial = ring * (1.22 + (i % 4) * 0.12) + 0.08
        if radial > 1.42:
            radial = 1.42
        height = 0.22 + (i % 6) * 0.05
        size = 0.28 + (i % 3) * 0.07
        pos = origin + Vector((math.cos(ang) * radial, math.sin(ang) * radial, height))
        add_puff("FxPuff%d" % i, pos, size, color, 1.0)


def shockwave(origin, radius):
    add_torus("FxShock", origin + Vector((0, 0, 0.03)), radius, 0.022, SHOCK, 0.7, 0.55)
    add_torus("FxDustRing", origin + Vector((0, 0, 0.02)), radius * 0.72, 0.028, DUST, 0.55, 0.12)
    add_plane(
        "FxDisc",
        origin + Vector((0, 0, 0.012)),
        (radius * 1.15, radius * 1.15, 1),
        (DUST[0], DUST[1], DUST[2], 1),
        0.16,
        0.0,
    )


def star_object(name, location):
    mesh = bpy.data.meshes.new(name + "Mesh")
    verts = [(0.0, 0.0, 0.0)]
    for i in range(10):
        ang = i * math.pi / 5.0 - math.pi / 2.0
        radius = 0.10 if i % 2 == 0 else 0.04
        verts.append((math.cos(ang) * radius, math.sin(ang) * radius, 0.0))
    faces = [(0, 1 + i, 1 + (i + 1) % 10) for i in range(10)]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.data.materials.append(glow_mat(name + "Mat", (1.0, 0.78, 0.08, 1.0), 3.2))
    return obj


def glow_mat(name, color, strength):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    if hasattr(mat, "blend_method"):
        mat.blend_method = "BLEND"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = color
    emit.inputs["Strength"].default_value = strength
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    return mat


def stars(head, count=5):
    # Vertical ring, shifted toward the camera, so all five clear the skull.
    for i in range(count):
        ang = math.pi * 0.5 + i / count * math.tau
        pos = head + Vector((math.cos(ang) * 0.30, -0.18, math.sin(ang) * 0.26))
        star_object("FxStar%d" % i, pos)


def fresnel_shell():
    mat = bpy.data.materials.get("FxRimShell")
    if mat is not None:
        return mat
    mat = bpy.data.materials.new("FxRimShell")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    fres = nt.nodes.new("ShaderNodeFresnel")
    transparent = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    if hasattr(mat, "use_backface_culling"):
        mat.use_backface_culling = True
    fres.inputs["IOR"].default_value = 1.55
    emit.inputs["Color"].default_value = RIM
    emit.inputs["Strength"].default_value = 3.2
    nt.links.new(fres.outputs["Fac"], mix.inputs["Fac"])
    nt.links.new(transparent.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def add_shell():
    mat = fresnel_shell()
    for obj in body_meshes():
        shell = obj.copy()
        shell.data = obj.data.copy()
        shell.name = "FxShell" + obj.name
        bpy.context.collection.objects.link(shell)
        shell.parent = obj.parent
        shell.parent_type = obj.parent_type
        shell.parent_bone = obj.parent_bone
        shell.matrix_parent_inverse = obj.matrix_parent_inverse.copy()
        for vert in shell.data.vertices:
            vert.co += vert.normal * 0.014
        shell.data.materials.clear()
        shell.data.materials.append(mat)


def launch_fx(origin):
    add_torus("FxLaunch", origin + Vector((0, 0, 0.72)), 0.72, 0.03, CYAN, 0.85, 1.15)
    for i in range(6):
        ang = i / 6 * math.tau
        a = origin + Vector((math.cos(ang) * 0.28, math.sin(ang) * 0.28, 0.06))
        b = a + Vector((math.cos(ang) * 0.05, math.sin(ang) * 0.05, 1.15 + (i % 2) * 0.32))
        add_curve("FxStreak%d" % i, [a, b], 0.016, CYAN, 0.9, 0.85)


def rope_shimmer(hand, anchor):
    delta = anchor - hand
    dist = delta.length
    if dist < 0.05:
        return
    direction = delta / dist
    side = direction.cross(Vector((0, 0, 1)))
    if side.length < 0.001:
        side = Vector((1, 0, 0))
    side.normalize()
    pts = []
    for i in range(8):
        t = i / 7.0
        bell = 4 * t * (1 - t)
        wob = 0.028 * bell
        pts.append(hand + direction * (dist * t) + side * wob)
    # One line, 1.8 cm across. bevel_depth is the radius.
    add_curve("FxRope", pts, 0.009, ROPE, 1.0, 0.0)


def chip(name, location, size, color):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size, size * 0.45, size * 0.7)
    obj.rotation_euler = Euler((rad(20), rad(35), rad(15)), "XYZ")
    obj.data.materials.append(make_mat(name + "Mat", color, 0.55, 1.0, 0.05))
    return obj


def hook_fx(anchor, toward):
    # Burst sits in front of the post face, on the runner's side of the hit.
    away = toward.normalized() if toward.length > 0.001 else Vector((0.0, -1.0, 0.0))
    core = anchor + away * 0.08 + Vector((0.0, 0.0, 0.08))
    gold = (1.0, 0.62, 0.08, 1.0)
    add_puff("FxPuffSparkCore", core, 0.34, gold, 2.0)
    for i in range(10):
        ang = i / 10.0 * math.tau
        spread = 0.12 + (i % 3) * 0.07
        pos = core + away * (0.05 + (i % 3) * 0.04) + Vector((math.cos(ang) * spread, math.sin(ang) * spread * 0.45, 0.04 + (i % 4) * 0.06))
        add_puff("FxPuffSpark%d" % i, pos, 0.14 + (i % 3) * 0.05, gold, 1.6)
    for i in range(6):
        ang = i / 6.0 * math.tau + 0.4
        pos = core + away * 0.14 + Vector((math.cos(ang) * 0.22, math.sin(ang) * 0.12, (i % 3) * 0.05))
        chip("FxChip%d" % i, pos, 0.06 + (i % 3) * 0.02, (0.45, 0.32, 0.16, 1))


def scuff_mat():
    mat = bpy.data.materials.get("FxScuffDecal")
    if mat is not None:
        return mat
    mat = bpy.data.materials.new("FxScuffDecal")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = soft_image()
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs["Color"].default_value = (0.16, 0.11, 0.07, 1.0)
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(diff.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def wall_fx(foot, normal):
    # Soft marks on the wall, trailing back along the run, plus dust off the face.
    tint = (0.40, 0.30, 0.18, 1.0)
    trail = -WALL_TRAVEL if WALL_TRAVEL.length > 0.5 else Vector((-1.0, 0.0, 0.0))
    for i in range(5):
        base = foot + trail * (0.05 + i * 0.18) + Vector((0.0, 0.0, (i % 2) * 0.03)) - normal * 0.015
        for k, (along, dz, size, strength) in enumerate(
            ((0.0, 0.10, 0.10, 1.05), (0.06, 0.07, 0.07, 0.7), (-0.04, 0.055, 0.055, 0.45))
        ):
            pos = base + trail * along + Vector((0.0, 0.0, dz)) - normal * (0.012 * k)
            obj = add_puff("FxPuffScuff%d_%d" % (i, k), pos, size, tint, strength)
            obj.rotation_euler = normal.to_track_quat("-Z", "Y").to_euler()
    for i in range(8):
        puff = foot - normal * (0.05 + (i % 3) * 0.025) + trail * (0.04 + i * 0.09) + Vector((0.0, 0.0, 0.03 + (i % 3) * 0.02))
        add_puff("FxPuffFoot%d" % i, puff, 0.12 + (i % 3) * 0.04, (0.62, 0.46, 0.28, 1.0), 1.35)


def ground_mat():
    mat = bpy.data.materials.new("GroundMat")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 22.0
    noise.inputs["Detail"].default_value = 6.0
    noise.inputs["Roughness"].default_value = 0.65
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (0.30, 0.31, 0.32, 1.0)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (0.50, 0.51, 0.52, 1.0)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = 0.92
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def setup_world(arm):
    tint()
    bpy.ops.mesh.primitive_plane_add(size=16.0, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.name = "PropGround"
    ground.data.materials.append(ground_mat())
    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = SKY
    bg.inputs["Strength"].default_value = 0.42
    bpy.ops.object.light_add(type="SUN", location=(4.2, -3.2, 7.5))
    sun = bpy.context.active_object
    sun.data.energy = 1.35
    sun.data.color = (1.0, 0.97, 0.92)
    sun.data.use_shadow = True
    look_at(sun, Vector((0, 0, 0.8)))
    bpy.ops.object.light_add(type="AREA", location=(-2.6, -2.2, 3.4))
    fill = bpy.context.active_object
    fill.data.energy = 16
    fill.data.size = 3.2
    look_at(fill, Vector((0, 0, 1.1)))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, -0.25))
    slab = bpy.context.active_object
    slab.name = "PropSlab"
    slab.scale = (14, 14, 0.5)
    slab.hide_render = True
    bpy.ops.object.camera_add(location=(2.4, -3.6, 1.55))
    cam = bpy.context.active_object
    cam.data.lens = 48
    look_at(cam, Vector((0.0, 0.1, 1.05)))
    bpy.context.scene.camera = cam
    arm.hide_render = True
    scene = bpy.context.scene
    for i in range(-3, 4):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(i * 0.72, 0.0, 0.001))
        seam = bpy.context.active_object
        seam.name = "PropSeam"
        seam.scale = (0.012, 6.0, 0.002)
        seam.data.materials.append(make_mat("SeamMat", (0.18, 0.19, 0.16, 1), 0.9))
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = SAMPLES
    try:
        scene.eevee.use_bloom = False
    except AttributeError:
        pass
    scene.render.resolution_x = RES_X
    scene.render.resolution_y = RES_Y
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    try:
        scene.view_settings.view_transform = "Standard"
        scene.view_settings.look = "None"
        scene.view_settings.exposure = 0.0
    except (TypeError, AttributeError):
        pass
    return cam


def solid_of(obj):
    verts, polys = mesh_world(obj)
    tree = BVHTree.FromPolygons(verts, polys, epsilon=0.0)
    return obj, tree


def render_to(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    shrink(path)


def shrink(path, limit=400 * 1024):
    if os.path.getsize(path) <= limit:
        print("SIZE", os.path.basename(path), os.path.getsize(path))
        return
    from PIL import Image

    im = Image.open(path).convert("RGB")
    for colors in (220, 180, 140, 110):
        q = im.quantize(colors=colors, method=Image.Quantize.MEDIANCUT)
        q.save(path, optimize=True)
        if os.path.getsize(path) <= limit:
            print("SIZE", os.path.basename(path), os.path.getsize(path), "colors", colors)
            return
    im = im.resize((640, 360), Image.Resampling.LANCZOS)
    im.save(path, optimize=True)
    print("SIZE", os.path.basename(path), os.path.getsize(path), "resized")


def bounds_of():
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    found = False
    skip = {"PropGround", "PropSlab", "PropSeam"}
    for obj in bpy.data.objects:
        if obj.hide_render or obj.name in skip:
            continue
        take = obj.name.startswith("Mesh_") or obj.name.startswith("Fx") or obj.name.startswith("Prop")
        if not take:
            continue
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            found = True
            mn.x = min(mn.x, p.x)
            mn.y = min(mn.y, p.y)
            mn.z = min(mn.z, p.z)
            mx.x = max(mx.x, p.x)
            mx.y = max(mx.y, p.y)
            mx.z = max(mx.z, p.z)
    if not found:
        return Vector((0, 0, 0)), Vector((1, 1, 2))
    return mn, mx


def aim_billboards(origin):
    for obj in bpy.data.objects:
        if not (obj.name.startswith("FxPuff") or obj.name.startswith("FxStar")):
            continue
        if "Scuff" in obj.name:
            continue
        direction = origin - obj.location
        if direction.length < 0.001:
            continue
        obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()


def body_bounds():
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for obj in body_meshes():
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            mn.x = min(mn.x, p.x)
            mn.y = min(mn.y, p.y)
            mn.z = min(mn.z, p.z)
            mx.x = max(mx.x, p.x)
            mx.y = max(mx.y, p.y)
            mx.z = max(mx.z, p.z)
    return mn, mx


def body_frame_fraction(cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    ys = []
    for obj in body_meshes():
        for corner in obj.bound_box:
            co = world_to_camera_view(scene, cam, obj.matrix_world @ Vector(corner))
            if co.z > 0:
                ys.append(co.y)
    if len(ys) < 2:
        return 0.0
    return max(ys) - min(ys)


def point_in_frame(cam, point, margin=0.03):
    from bpy_extras.object_utils import world_to_camera_view

    co = world_to_camera_view(bpy.context.scene, cam, point)
    if co.z <= 0.05:
        return False
    return margin < co.x < 1.0 - margin and margin < co.y < 1.0 - margin


def fx_points(skip):
    pts = []
    for obj in bpy.data.objects:
        if obj.hide_render or obj.name in skip:
            continue
        if not (obj.name.startswith("Fx") or obj.name.startswith("Prop")):
            continue
        if obj.name in ("PropGround", "PropSlab", "PropSeam", "PropWall", "PropPost"):
            continue
        for corner in obj.bound_box:
            pts.append(obj.matrix_world @ Vector(corner))
    return pts


def frame_camera(cam, view=None, fraction=0.66):
    """Put the runner at about 65% of frame height, then back up only if an effect is clipped."""
    if view is None:
        view = Vector((0.55, -0.84, 0.24))
    view = view.normalized()
    cam.data.type = "PERSP"
    lens = 35.0
    cam.data.lens = lens
    aspect = RES_Y / float(RES_X)
    tan_v = (18.0 / lens) * aspect
    mn, mx = body_bounds()
    height = max(mx.z - mn.z, 1.45)
    center = (mn + mx) * 0.5
    dist = (height / fraction) * 0.5 / tan_v
    cam.data.clip_start = 0.04
    cam.data.clip_end = 80.0
    for _ in range(10):
        cam.location = center + view * dist
        look_at(cam, center)
        bpy.context.view_layer.update()
        aim_billboards(cam.location)
        outside = [p for p in fx_points(set()) if not point_in_frame(cam, p, 0.04)]
        frac = body_frame_fraction(cam)
        if not outside or frac <= 0.60:
            break
        dist *= 1.04
    bpy.context.view_layer.update()
    aim_billboards(cam.location)
    print("FRAME", round(body_frame_fraction(cam), 3), "dist", round(dist, 2))


def shot(arm, cam, name, pose, yaw, lift, build, frame=None):
    clear_fx()
    apply_pose(arm, pose, lift, yaw)
    slab = bpy.data.objects.get("PropSlab")
    bpy.context.view_layer.update()
    solids = [solid_of(slab)] if slab is not None else []
    build(arm, solids)
    self_max, world_max, fails, pair = measure(arm, solids)
    print(
        "NOCLIP",
        name,
        "self_cm", round(self_max * 100, 2),
        "world_cm", round(world_max * 100, 2),
        "fails", fails,
        "pair", pair,
    )
    if not MEASURE_ONLY:
        if frame is None:
            frame_camera(cam)
        else:
            frame(cam, arm)
            print("FRAME", name, round(body_frame_fraction(cam), 3))
        screen_box(cam)
        path = os.path.join(OUT, name + ".png")
        render_to(path)
    return self_max, world_max, fails


def ndc_box(cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    xs = []
    ys = []
    for obj in body_meshes():
        for corner in obj.bound_box:
            co = world_to_camera_view(scene, cam, obj.matrix_world @ Vector(corner))
            if co.z > 0:
                xs.append(co.x)
                ys.append(co.y)
    if not xs:
        return (0.0, 0.0, 0.0, 0.0)
    return (min(xs), max(xs), min(ys), max(ys))


def screen_box(cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    xs = []
    ys = []
    for obj in body_meshes():
        for corner in obj.bound_box:
            co = world_to_camera_view(scene, cam, obj.matrix_world @ Vector(corner))
            if co.z > 0:
                xs.append(co.x)
                ys.append(co.y)
    if not xs:
        return
    print(
        "SCREEN",
        "x", round(min(xs), 3), round(max(xs), 3),
        "y", round(min(ys), 3), round(max(ys), 3),
    )


def build_land(arm, solids):
    origin = Vector((arm.location.x, arm.location.y, 0.02))
    shockwave(origin, 0.72)
    dust_ring(origin, 0.72, 18, DUST)
    return solids


def build_roll(arm, solids):
    origin = Vector((arm.location.x, arm.location.y, 0.02))
    shockwave(origin, 1.05)
    add_torus("FxRollOuter", origin + Vector((0, 0, 0.03)), 1.28, 0.016, SHOCK, 0.55, 0.2)
    dust_ring(origin, 1.05, 22, DUST)
    return solids


def build_grapple(arm, solids):
    hand = bone_pos(arm, "Hand_L")
    shoulder = bone_pos(arm, "Shoulder_L")
    flat = Vector((hand.x - shoulder.x, hand.y - shoulder.y, 0.0))
    if flat.length < 0.15:
        flat = Vector((0.35, -0.94, 0.0))
    flat.normalize()
    # The hook face is 5 m out along the reach. A narrow post sits behind that face.
    hit = hand + flat * 5.0
    hit.z = hand.z
    if abs(flat.y) >= abs(flat.x):
        sign = 1.0 if flat.y >= 0.0 else -1.0
        center = Vector((hit.x, hit.y + sign * 0.17, 1.20))
        scale = (0.42, 0.34, 2.35)
    else:
        sign = 1.0 if flat.x >= 0.0 else -1.0
        center = Vector((hit.x + sign * 0.17, hit.y, 1.20))
        scale = (0.34, 0.42, 2.35)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    post = bpy.context.active_object
    post.name = "PropPost"
    post.scale = scale
    post.data.materials.append(make_mat("PostMat", (0.38, 0.40, 0.43, 1), 0.75))
    bpy.context.view_layer.update()
    solids.append(solid_of(post))
    empty = bpy.data.objects.new("PropHook", None)
    empty.location = hit
    empty.empty_display_size = 0.05
    bpy.context.collection.objects.link(empty)
    rope_shimmer(hand, hit)
    hook_fx(hit, -flat)
    print("GRAPPLE", "dist_m", round((hit - hand).length, 2))
    return solids


def build_immune(arm, solids):
    add_shell()
    return solids


def build_stagger(arm, solids):
    neck = bone_pos(arm, "Head", tail=False)
    crown = bone_pos(arm, "Head", tail=True)
    stars((neck + crown) * 0.5)
    return solids


def build_launch(arm, solids):
    pad_z = 0.04
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, pad_z * 0.5))
    pad = bpy.context.active_object
    pad.name = "PropPad"
    pad.scale = (1.1, 1.1, pad_z)
    pad.data.materials.append(make_mat("PadMat", PAD, 0.45, 1, 0.4))
    bpy.context.view_layer.update()
    solids.append(solid_of(pad))
    launch_fx(Vector((0, 0, pad_z)))
    return solids


def body_extent(axis, mode):
    val = None
    for obj in body_meshes():
        hit = extreme(obj, axis, mode)
        if val is None:
            val = hit
        elif mode == "max" and hit > val:
            val = hit
        elif mode == "min" and hit < val:
            val = hit
    return 0.0 if val is None else val


WALL_NORMAL = Vector((0.0, -1.0, 0.0))
WALL_TRAVEL = Vector((1.0, 0.0, 0.0))


def build_wall(arm, solids):
    # Upright wall run. The left foot is the plant. The wall sits 2 mm past it.
    global WALL_NORMAL, WALL_TRAVEL
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += 0.55 - low
    bpy.context.view_layer.update()
    foot = bpy.data.objects.get("Mesh_Foot_L")
    verts, _polys = mesh_world(foot)
    hips = bone_pos(arm, "Hips")
    foot_r = bone_pos(arm, "Foot_R")
    foot_c = Vector((
        sum(v.x for v in verts) / len(verts),
        sum(v.y for v in verts) / len(verts),
        sum(v.z for v in verts) / len(verts),
    ))
    # Stride runs along the wall. The wall is the character's left, from the left shoulder.
    travel = Vector((foot_r.x - foot_c.x, foot_r.y - foot_c.y, 0.0))
    if travel.length < 0.05:
        travel = Vector((1.0, 0.0, 0.0))
    travel.normalize()
    side = foot_c - hips
    side.z = 0.0
    side = side - travel * side.dot(travel)
    if side.length < 0.02:
        side = Vector((-travel.y, travel.x, 0.0))
    normal = side.normalized()
    face = max(v.x * normal.x + v.y * normal.y for v in verts) + 0.002
    center = normal * (face + 0.16)
    center.z = 1.35
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    wall = bpy.context.active_object
    wall.name = "PropWall"
    wall.rotation_euler = Vector((normal.x, normal.y, 0.0)).to_track_quat("Y", "Z").to_euler()
    wall.scale = (3.2, 0.32, 2.8)
    wall.data.materials.append(make_mat("WallMat", (0.62, 0.64, 0.68, 1), 0.78))
    bpy.context.view_layer.update()
    solids.append(solid_of(wall))
    WALL_NORMAL = normal
    WALL_TRAVEL = travel
    wall_fx(foot_c, normal)
    head = bone_pos(arm, "Head", tail=True)
    hips = bone_pos(arm, "Hips")
    foot_r = bone_pos(arm, "Foot_R")
    hand_l = bone_pos(arm, "Hand_L")
    up = head - hips
    print(
        "WALL_POSE",
        "head", round(head.x, 2), round(head.y, 2), round(head.z, 2),
        "hips", round(hips.x, 2), round(hips.y, 2), round(hips.z, 2),
        "footL", round(foot_c.x, 2), round(foot_c.y, 2), round(foot_c.z, 2),
        "footR", round(foot_r.x, 2), round(foot_r.y, 2), round(foot_r.z, 2),
        "handL", round(hand_l.x, 2), round(hand_l.y, 2), round(hand_l.z, 2),
        "up", round(up.x, 2), round(up.y, 2), round(up.z, 2),
        "normal", round(normal.x, 2), round(normal.y, 2),
    )
    return solids


def frame_wall_side(cam, arm):
    # Open side of the wall, so the upright body is in profile and the face is behind the plant.
    mn, mx = body_bounds()
    height = max(mx.z - mn.z, 1.55)
    center = (mn + mx) * 0.5
    lens = 36.0
    cam.data.type = "PERSP"
    cam.data.lens = lens
    cam.data.clip_start = 0.04
    cam.data.clip_end = 40.0
    aspect = RES_Y / float(RES_X)
    tan_v = (18.0 / lens) * aspect
    dist = (height / 0.64) * 0.5 / tan_v
    view = (-WALL_NORMAL * 0.78 + WALL_TRAVEL * 0.42 + Vector((0.0, 0.0, 0.28))).normalized()
    cam.location = center + view * dist
    look_at(cam, center)
    bpy.context.view_layer.update()
    aim_billboards(cam.location)


def frame_wall_top(cam, arm):
    # Plan view. Screen up is the wall normal, so the planted foot meets the face.
    mn, mx = body_bounds()
    normal = WALL_NORMAL
    travel = WALL_TRAVEL
    up = travel.cross(normal)
    if up.z < 0.0:
        travel = -travel
    center = (mn + mx) * 0.5
    span = max(mx.x - mn.x, mx.y - mn.y, 1.2) * 1.55
    cam.data.type = "ORTHO"
    cam.data.sensor_fit = "HORIZONTAL"
    cam.data.ortho_scale = max(span, span / (RES_Y / float(RES_X)) * 0.85)
    cam.data.clip_start = 0.01
    cam.data.clip_end = 40.0
    cam.matrix_world = Matrix((
        (travel.x, normal.x, 0.0, center.x),
        (travel.y, normal.y, 0.0, center.y),
        (0.0, 0.0, 1.0, center.z + 8.0),
        (0.0, 0.0, 0.0, 1.0),
    ))
    bpy.context.view_layer.update()


def frame_grapple_shoulder(cam, arm):
    # Behind the shoulder and off the rope axis, so the line leaves the
    # left hand and the post is not sitting on the chest. Feet stay in frame.
    hand = bone_pos(arm, "Hand_L")
    hip = bone_pos(arm, "Hips")
    hook = bpy.data.objects["PropHook"].location.copy()
    to_hook = Vector((hook.x - hip.x, hook.y - hip.y, 0.0))
    if to_hook.length < 0.2:
        to_hook = Vector((0.0, -1.0, 0.0))
    to_hook.normalize()
    side = Vector((to_hook.y, -to_hook.x, 0.0))
    # Stand off the rope, on the side away from the hand, so the line
    # crosses open ground instead of running through the chest.
    if (hand - hip).dot(side) > 0.0:
        side = -side
    mn, mx = body_bounds()
    height = max(mx.z - mn.z, 1.5)
    center = (mn + mx) * 0.5
    lens = 26.0
    cam.data.type = "PERSP"
    cam.data.lens = lens
    cam.data.clip_start = 0.05
    cam.data.clip_end = 50.0
    aspect = RES_Y / float(RES_X)
    tan_v = (18.0 / lens) * aspect
    dist = (height / 0.64) * 0.5 / tan_v
    view = (-to_hook * 0.48 + side * 0.78 + Vector((0.0, 0.0, 0.22))).normalized()
    aim = Vector((center.x * 0.7 + hook.x * 0.3, center.y * 0.7 + hook.y * 0.3, center.z - 0.05))
    for _ in range(8):
        cam.location = center + view * dist
        look_at(cam, aim)
        bpy.context.view_layer.update()
        aim_billboards(cam.location)
        box = ndc_box(cam)
        body_ok = box[0] > 0.03 and box[1] < 0.97 and box[2] > 0.04 and box[3] < 0.96
        hook_ok = point_in_frame(cam, hook, 0.05)
        hand_ok = point_in_frame(cam, hand, 0.03)
        if body_ok and hook_ok and hand_ok:
            break
        if box[2] < 0.04:
            aim.z -= 0.12
        elif not body_ok:
            dist *= 1.08
        elif not hook_ok or not hand_ok:
            aim.x = aim.x * 0.8 + hook.x * 0.2
            aim.y = aim.y * 0.8 + hook.y * 0.2
            dist *= 1.05
    from bpy_extras.object_utils import world_to_camera_view

    hook_ndc = world_to_camera_view(bpy.context.scene, cam, hook)
    hand_ndc = world_to_camera_view(bpy.context.scene, cam, hand)
    print(
        "HOOK_IN", "shoulder",
        point_in_frame(cam, hook, 0.04),
        point_in_frame(cam, hand, 0.03),
        "frac", round(body_frame_fraction(cam), 3),
        "box", [round(v, 3) for v in ndc_box(cam)],
        "hook", round(hook_ndc.x, 3), round(hook_ndc.y, 3),
        "hand", round(hand_ndc.x, 3), round(hand_ndc.y, 3),
    )


def frame_grapple_side(cam, arm):
    hand = bone_pos(arm, "Hand_L")
    hook = bpy.data.objects["PropHook"].location.copy()
    mn, mx = body_bounds()
    body_c = (mn + mx) * 0.5
    flat = Vector((hook.x - hand.x, hook.y - hand.y, 0.0))
    if flat.length < 0.2:
        flat = Vector((0.0, -1.0, 0.0))
    flat.normalize()
    side = Vector((flat.y, -flat.x, 0.0))
    span = hook - Vector((body_c.x, body_c.y, hook.z))
    length = max(span.length + 1.3, 5.5)
    center = (hook + body_c) * 0.5
    center.z = 1.1
    lens = 32.0
    cam.data.type = "PERSP"
    cam.data.lens = lens
    cam.data.clip_start = 0.05
    cam.data.clip_end = 50.0
    tan_h = 18.0 / lens
    dist = (length / 0.82) * 0.5 / tan_h
    for _ in range(6):
        cam.location = Vector((center.x, center.y, 0.0)) + side * dist + Vector((0.0, 0.0, 1.7))
        look_at(cam, center)
        bpy.context.view_layer.update()
        aim_billboards(cam.location)
        feet = Vector(((mn.x + mx.x) * 0.5, (mn.y + mx.y) * 0.5, mn.z))
        head = Vector(((mn.x + mx.x) * 0.5, (mn.y + mx.y) * 0.5, mx.z))
        if point_in_frame(cam, hook, 0.05) and point_in_frame(cam, hand, 0.04) and point_in_frame(cam, feet, 0.04) and point_in_frame(cam, head, 0.04):
            break
        dist *= 1.08
    print(
        "HOOK_IN", "side",
        point_in_frame(cam, hook, 0.04),
        point_in_frame(cam, hand, 0.04),
        "frac", round(body_frame_fraction(cam), 3),
    )


def build_punch(arm, solids):
    return solids


def vignette(path, color):
    from PIL import Image

    im = Image.open(path).convert("RGB")
    px = im.load()
    w, h = im.size
    cr, cg, cb = [int(c * 255) for c in color[:3]]
    for y in range(h):
        for x in range(w):
            edge = min(x / w, (w - 1 - x) / w, y / h, (h - 1 - y) / h)
            band = max(0.0, 0.11 - edge) / 0.11
            if band <= 0:
                continue
            r, g, b = px[x, y]
            a = band * band * 0.88
            px[x, y] = (
                int(r * (1 - a) + cr * a),
                int(g * (1 - a) + cg * a),
                int(b * (1 - a) + cb * a),
            )
    im.save(path, optimize=True)


def stitch(left, right, dest):
    from PIL import Image

    a = Image.open(left).convert("RGB")
    b = Image.open(right).convert("RGB")
    w = a.width + b.width
    h = max(a.height, b.height)
    out = Image.new("RGB", (w, h), (12, 12, 14))
    out.paste(a, (0, 0))
    out.paste(b, (a.width, 0))
    for y in range(h):
        for x in range(a.width - 2, a.width + 2):
            if 0 <= x < w:
                out.putpixel((x, y), (16, 16, 18))
    out.save(dest, optimize=True)
    shrink(dest)


def paint_rig_overlap(arm):
    """Color hip and thigh faces that penetrate past the 3 cm joint ball."""
    clear_pose(arm)
    arm.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += -low + 0.006
    bpy.context.view_layer.update()
    red = make_mat("OverlapRed", (0.92, 0.05, 0.04, 1), 0.35, 1.0, 1.6)
    packed = {row[0].name: row for row in pack_body()}
    joined = neighbours(arm)
    marked = []
    worst = 0.0
    worst_pair = ""
    for name_a, row_a in packed.items():
        obj_a, bone_a, tree_a, verts_a, polys_a = row_a
        for name_b, row_b in packed.items():
            if name_a >= name_b:
                continue
            obj_b, bone_b, tree_b, verts_b, polys_b = row_b
            if (bone_a, bone_b) not in joined:
                continue
            joint = joint_world(arm, bone_a, bone_b)
            for src, other_tree, other_name in (
                (row_a, tree_b, name_b),
                (row_b, tree_a, name_a),
            ):
                obj, bone, tree, verts, polys = src
                bad = set()
                for idx in overlap_ids(tree, polys, other_tree):
                    if idx >= len(verts):
                        continue
                    point = verts[idx]
                    if joint is not None and (point - joint).length <= JOINT_EXEMPT:
                        continue
                    depth = inside_depth(other_tree, point)
                    if depth <= DEPTH_LIMIT:
                        continue
                    bad.add(idx)
                    if depth > worst:
                        worst = depth
                        worst_pair = obj.name + "|" + other_name
                if not bad:
                    continue
                me = obj.data
                slot = -1
                for mi, existing in enumerate(me.materials):
                    if existing == red:
                        slot = mi
                        break
                if slot < 0:
                    me.materials.append(red)
                    slot = len(me.materials) - 1
                for poly in me.polygons:
                    if any(v in bad for v in poly.vertices):
                        poly.material_index = slot
                marked.append(obj.name)
    return worst, worst_pair, sorted(set(marked))


def ghost_body():
    """Let the buried red faces read. The rig still is the only caller."""
    for obj in body_meshes():
        for slot in obj.material_slots:
            mat = slot.material
            if mat is None or mat.name == "OverlapRed":
                continue
            ghost = mat.copy()
            ghost.blend_method = "BLEND"
            if hasattr(ghost, "shadow_method"):
                ghost.shadow_method = "NONE"
            node = ghost.node_tree.nodes.get("Principled BSDF") if ghost.use_nodes else None
            if node is not None and "Alpha" in node.inputs:
                node.inputs["Alpha"].default_value = 0.28
            slot.material = ghost


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    cam = setup_world(arm)
    shots = [
        ("landing-impact", pose_land, 24, 0.0, build_land, None),
        ("landing-roll", pose_roll, 28, 0.0, build_roll, None),
        ("grapple-shoulder", pose_grapple, -12, 0.0, build_grapple, frame_grapple_shoulder),
        ("grapple-side", pose_grapple, -12, 0.0, build_grapple, frame_grapple_side),
        ("immunity-glow", pose_run, 20, 0.0, build_immune, None),
        ("punch-stagger", pose_stagger, -14, 0.0, build_stagger, None),
        ("launch-pad", pose_launch, 18, 0.55, build_launch, None),
        ("wall-run", pose_wall, 90, 0.0, build_wall, frame_wall_side),
        ("wall-run-top", pose_wall, 90, 0.0, build_wall, frame_wall_top),
    ]
    only = os.environ.get("FX_ONLY", "")
    only_set = set(part.strip() for part in only.split(",") if part.strip())
    totals = []
    for name, pose, yaw, lift, build, frame in shots:
        if only_set and name not in only_set:
            continue
        totals.append((name,) + shot(arm, cam, name, pose, yaw, lift, build, frame))
    if only_set:
        self_max = max(row[1] for row in totals) if totals else 0.0
        world_max = max(row[2] for row in totals) if totals else 0.0
        fails = sum(row[3] for row in totals)
        print(
            "no-clip clips=%d frames=%d worldMax=%.2f selfMax=%.2f fails=%d"
            % (len(totals), len(totals), world_max * 100.0, self_max * 100.0, fails)
        )
        return

    clear_fx()
    punch_path = os.path.join(OUT, "_tagger.png")
    victim_path = os.path.join(OUT, "_runner.png")
    apply_pose(arm, pose_punch, 0.0, 22)
    slab = bpy.data.objects.get("PropSlab")
    ground_solids = [solid_of(slab)] if slab is not None else []
    s1, w1, f1, pair = measure(arm, ground_solids)
    print("NOCLIP", "tagger", "self_cm", round(s1 * 100, 2), "world_cm", round(w1 * 100, 2), "fails", f1, "pair", pair)
    if not MEASURE_ONLY:
        frame_camera(cam)
        bpy.context.scene.render.filepath = punch_path
        bpy.ops.render.render(write_still=True)
        vignette(punch_path, TAG)

    apply_pose(arm, pose_stagger, 0.0, -18)
    s2, w2, f2, pair2 = measure(arm, ground_solids)
    print("NOCLIP", "runner", "self_cm", round(s2 * 100, 2), "world_cm", round(w2 * 100, 2), "fails", f2, "pair", pair2)
    if not MEASURE_ONLY:
        frame_camera(cam)
        bpy.context.scene.render.filepath = victim_path
        bpy.ops.render.render(write_still=True)
        dest = os.path.join(OUT, "tagged-flash.png")
        stitch(punch_path, victim_path, dest)
        os.remove(punch_path)
        os.remove(victim_path)

    clear_fx()
    clear_pose(arm)
    arm.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += -low + 0.006
    bpy.context.view_layer.update()
    for pair_name, pair_depth in each_pair(arm):
        if pair_depth <= DEPTH_LIMIT:
            continue
        print("RIG-PAIR %s depth_cm %.2f" % (pair_name, pair_depth * 100.0))
    depth, rig_pair, pieces = paint_rig_overlap(arm)
    print(
        "RIG-FAIL depth_cm %.2f pair %s pieces %s"
        % (depth * 100.0, rig_pair, ",".join(pieces))
    )
    rig_self, rig_world, rig_fails, rig_worst = measure(arm, ground_solids)
    print(
        "NOCLIP",
        "rig-rest",
        "self_cm", round(rig_self * 100, 2),
        "world_cm", round(rig_world * 100, 2),
        "fails", rig_fails,
        "pair", rig_worst,
    )
    if not MEASURE_ONLY:
        ghost_body()
        hip = bpy.data.objects.get("Mesh_Hips")
        focus = hip.matrix_world.translation if hip is not None else Vector((0, 0, 1))
        cam.location = focus + Vector((0.72, -1.05, 0.18))
        look_at(cam, focus + Vector((0.0, 0.0, -0.05)))
        cam.data.lens = 62
        render_to(os.path.join(OUT, "rig-overlap-hips-thigh.png"))

    self_max = max([row[1] for row in totals] + [s1, s2, rig_self])
    world_max = max([row[2] for row in totals] + [w1, w2, rig_world])
    fails = sum(row[3] for row in totals) + f1 + f2 + rig_fails
    clips = len(totals) + 3
    print(
        "no-clip clips=%d frames=%d worldMax=%.2f selfMax=%.2f fails=%d"
        % (clips, clips, world_max * 100.0, self_max * 100.0, fails)
    )


if __name__ == "__main__":
    main()
