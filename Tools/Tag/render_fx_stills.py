"""FX kit stills. Hier mannequin, one pose per effect, screen flash on one pane.

No-clip measures evaluated meshes. Joined neighbours are exempt within 3 cm
of the child bone segment. Outside that tube, a joined pair may keep the
overlap it already has at rest (the rigid hip mesh sits about 4.8 cm inside
the thigh, and that does not go away when the bone turns). A pose fails when
it pushes a joined pair deeper than that rest depth by more than 0.5 cm.
Non-joined pairs and world solids fail above 0.5 cm with no rest credit.
"""
import math
import os
import sys

import bpy
from mathutils import Euler, Vector
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
GROUND = (0.48, 0.52, 0.44, 1.0)
SKY = (0.62, 0.74, 0.86, 1.0)
DUST = (0.62, 0.46, 0.28, 1.0)
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


def pose_wall(arm):
    leg(arm, "L", 12.0, -16.0, 6.0)
    leg(arm, "R", -8.0, -12.0, -4.0)
    arm_pose(arm, "L", 6.0, -18.0, -8.0, 0.0)
    arm_pose(arm, "R", -6.0, -12.0, -8.0, 0.0)
    torso(arm, 4.0, 2.0, -2.0)
    set_euler(arm, "Chest", 0.0, 0.0, 0.0)
    set_euler(arm, "Hips", 4.0, 0.0, 0.0)
    set_euler(arm, "Foot_L", 0.0, 0.0, 0.0)


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
    keep = {"PropGround", "PropSlab"}
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


def bone_segment(arm, a, b):
    child = b if arm.data.bones[b].parent and arm.data.bones[b].parent.name == a else a
    if child not in arm.pose.bones:
        return None
    bone = arm.pose.bones[child]
    head = arm.matrix_world @ bone.head
    tail = arm.matrix_world @ bone.tail
    return head, tail


def near_segment(point, seg):
    if seg is None:
        return False
    head, tail = seg
    span = tail - head
    denom = span.length_squared
    if denom < 1e-8:
        return (point - head).length <= JOINT_EXEMPT
    t = (point - head).dot(span) / denom
    if t < 0.0:
        t = 0.0
    if t > 1.0:
        t = 1.0
    closest = head + span * t
    return (point - closest).length <= JOINT_EXEMPT


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


def pair_depth(tree_a, verts_a, polys_a, tree_b, segment):
    """Deepest penetration. Points within 3 cm of the bone segment are exempt."""
    used = overlap_ids(tree_a, polys_a, tree_b)
    worst = 0.0
    for idx in used:
        if idx >= len(verts_a):
            continue
        point = verts_a[idx]
        if near_segment(point, segment):
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


def capture_socket(arm):
    """Deepest rest overlap of each joined pair, outside the 3 cm joint ball."""
    bpy.context.view_layer.update()
    clear_pose(arm)
    arm.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    packed = pack_body()
    joined = neighbours(arm)
    rest = {}
    socket = 0.0
    socket_pair = ""
    for i in range(len(packed)):
        obj_a, bone_a, tree_a, verts_a, polys_a = packed[i]
        for j in range(len(packed)):
            if i == j:
                continue
            obj_b, bone_b, tree_b, _vb, _pb = packed[j]
            if (bone_a, bone_b) not in joined:
                continue
            segment = bone_segment(arm, bone_a, bone_b)
            absolute = pair_depth(tree_a, verts_a, polys_a, tree_b, segment)
            rest[(obj_a.name, obj_b.name)] = absolute
            if absolute > socket:
                socket = absolute
                socket_pair = obj_a.name + "|" + obj_b.name
    return rest, socket, socket_pair


def measure(arm, solids, rest):
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
            segment = bone_segment(arm, bone_a, bone_b) if linked else None
            d_ab = pair_depth(tree_a, verts_a, polys_a, tree_b, segment)
            d_ba = pair_depth(tree_b, verts_b, polys_b, tree_a, segment)
            if linked:
                d_ab -= rest.get((obj_a.name, obj_b.name), 0.0)
                d_ba -= rest.get((obj_b.name, obj_a.name), 0.0)
                if d_ab < 0.0:
                    d_ab = 0.0
                if d_ba < 0.0:
                    d_ba = 0.0
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


def dust_ring(origin, scale, count, color, lift=0.08):
    for i in range(count):
        ang = i / count * math.tau
        radial = scale * (0.42 + (i % 3) * 0.14)
        pos = origin + Vector((math.cos(ang) * radial, math.sin(ang) * radial, lift + (i % 2) * 0.12))
        add_ico("FxDust%d" % i, pos, 0.09 + (i % 3) * 0.03, color, 0.88, 0.55)


def shockwave(origin, radius):
    add_torus("FxShock", origin + Vector((0, 0, 0.03)), radius, 0.018, SHOCK, 0.9, 0.6)
    add_torus("FxDustRing", origin + Vector((0, 0, 0.025)), radius * 0.72, 0.045, DUST, 0.55, 0.05)
    add_plane(
        "FxDisc",
        origin + Vector((0, 0, 0.02)),
        (radius * 1.3, radius * 1.3, 1),
        (DUST[0], DUST[1], DUST[2], 1),
        0.28,
        0.0,
    )


def stars(head, count=5):
    add_torus("FxSwirl", head + Vector((0, 0, 0.28)), 0.20, 0.012, STAR, 0.9, 2.2)
    for i in range(count):
        ang = i / count * math.tau + 0.4
        pos = head + Vector((math.cos(ang) * 0.22, math.sin(ang) * 0.16, 0.30))
        add_ico("FxStar%d" % i, pos, 0.07, STAR, 0.98, 2.4)


def rim(origin, count=12):
    for i in range(count):
        ang = i / count * math.tau
        z = 0.42 + (i % 4) * 0.32
        rad = 0.46 + (i % 2) * 0.08
        pos = origin + Vector((math.cos(ang) * rad, math.sin(ang) * rad, z))
        add_ico("FxRim%d" % i, pos, 0.09, RIM, 0.62, 2.0)


def launch_fx(origin):
    add_torus("FxLaunch", origin + Vector((0, 0, 0.85)), 0.85, 0.02, CYAN, 0.85, 1.2)
    for i in range(6):
        ang = i / 6 * math.tau
        a = origin + Vector((math.cos(ang) * 0.32, math.sin(ang) * 0.32, 0.08))
        b = a + Vector((math.cos(ang) * 0.08, math.sin(ang) * 0.08, 1.35 + (i % 2) * 0.35))
        add_curve("FxStreak%d" % i, [a, b], 0.012, CYAN, 0.8, 1.0)


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
    add_curve("FxRope", pts, 0.008, (1.0, 0.9, 0.55, 1), 0.75, 0.8)
    add_curve("PropRope", [hand, anchor], 0.012, ROPE, 1.0, 0.0)


def hook_fx(anchor):
    for i in range(8):
        ang = i / 8 * math.tau
        pos = anchor + Vector((math.cos(ang) * 0.12, math.sin(ang) * 0.08, 0.05 + (i % 3) * 0.04))
        if i < 4:
            add_ico("FxSpark%d" % i, pos + Vector((0, 0, 0.08)), 0.035, SPARK, 0.95, 2.0)
        else:
            add_ico("FxDebris%d" % i, pos, 0.05, DUST, 0.8, 0.1)


def wall_fx(foot, along):
    for i in range(4):
        mark = foot - along * (0.08 + i * 0.1) + Vector((0, 0, 0.02 * (i % 2)))
        add_plane(
            "FxScuff%d" % i,
            mark,
            (0.09, 0.22, 1),
            (0.08, 0.07, 0.06, 1),
            0.95,
            0.15,
            Euler((rad(90), 0, 0), "XYZ"),
        )
    for i in range(4):
        puff = foot + Vector((0.06, -0.04 * i, 0.05 + i * 0.03))
        add_ico("FxFoot%d" % i, puff, 0.045 + i * 0.01, DUST, 0.7, 0.1)


def setup_world(arm):
    tint()
    bpy.ops.mesh.primitive_plane_add(size=16.0, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.name = "PropGround"
    ground.data.materials.append(make_mat("GroundMat", GROUND, 0.9))
    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = SKY
    bg.inputs["Strength"].default_value = 1.05
    bpy.ops.object.light_add(type="SUN", location=(3.2, -2.4, 8.0))
    sun = bpy.context.active_object
    sun.data.energy = 3.6
    sun.data.color = (1.0, 0.97, 0.92)
    look_at(sun, Vector((0, 0, 1.0)))
    bpy.ops.object.light_add(type="AREA", location=(-2.4, -1.6, 3.2))
    fill = bpy.context.active_object
    fill.data.energy = 180
    fill.data.size = 4.0
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
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = SAMPLES
    scene.render.resolution_x = RES_X
    scene.render.resolution_y = RES_Y
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    try:
        scene.view_settings.view_transform = "Standard"
    except TypeError:
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


def shot(arm, cam, name, pose, yaw, lift, build, look, rest):
    clear_fx()
    apply_pose(arm, pose, lift, yaw)
    slab = bpy.data.objects.get("PropSlab")
    bpy.context.view_layer.update()
    solids = [solid_of(slab)] if slab is not None else []
    build(arm, solids)
    self_max, world_max, fails, pair = measure(arm, solids, rest)
    print(
        "NOCLIP",
        name,
        "self_cm", round(self_max * 100, 2),
        "world_cm", round(world_max * 100, 2),
        "fails", fails,
        "pair", pair,
    )
    if not MEASURE_ONLY:
        cam.location = look[0]
        look_at(cam, look[1])
        path = os.path.join(OUT, name + ".png")
        render_to(path)
    return self_max, world_max, fails


def build_land(arm, solids):
    origin = Vector((0, 0, 0.02))
    shockwave(origin, 0.95)
    dust_ring(origin, 0.9, 10, DUST)
    return solids


def build_roll(arm, solids):
    origin = Vector((0, 0, 0.02))
    shockwave(origin, 1.55)
    add_torus("FxRollOuter", origin + Vector((0, 0, 0.04)), 1.85, 0.016, SHOCK, 0.75, 0.8)
    dust_ring(origin, 1.55, 16, DUST, 0.14)
    return solids


def build_grapple(arm, solids):
    hand = bone_pos(arm, "Hand_L")
    anchor = hand + Vector((0.15, 0.85, 1.15))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(anchor.x, anchor.y, anchor.z + 0.18))
    beam = bpy.context.active_object
    beam.name = "PropBeam"
    beam.scale = (0.18, 1.4, 0.12)
    beam.data.materials.append(make_mat("BeamMat", (0.32, 0.30, 0.28, 1), 0.7))
    bpy.context.view_layer.update()
    # Keep the beam above the hand mesh.
    solids.append(solid_of(beam))
    surface = Vector((anchor.x, anchor.y, anchor.z))
    rope_shimmer(hand, surface)
    hook_fx(surface + Vector((0, 0, -0.02)))
    return solids


def build_immune(arm, solids):
    origin = Vector((arm.location.x, arm.location.y, 0.0))
    rim(origin)
    return solids


def build_stagger(arm, solids):
    head = bone_pos(arm, "Head", tail=True)
    stars(Vector((head.x, head.y, head.z)))
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


def build_wall(arm, solids):
    foot = bone_pos(arm, "Foot_L")
    face_y = body_extent("y", "max")
    # Wall sits just past every body mesh, solid on the +Y side.
    gap = 0.01
    wall_y = face_y + gap + 0.08
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.1, wall_y, 1.2))
    wall = bpy.context.active_object
    wall.name = "PropWall"
    wall.scale = (2.2, 0.16, 2.4)
    wall.data.materials.append(make_mat("WallMat", WALL, 0.8))
    bpy.context.view_layer.update()
    solids.append(solid_of(wall))
    along = Vector((1, 0, 0))
    wall_fx(Vector((foot.x, face_y - 0.04, max(0.12, foot.z))), along)
    return solids


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


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    rest, socket, socket_pair = capture_socket(arm)
    print(
        "SOCKET restMax_cm %.2f pair %s"
        % (socket * 100.0, socket_pair)
    )
    cam = setup_world(arm)
    # Hide the measurement slab's twin; PropGround stays visible.
    shots = [
        ("landing-impact", pose_land, 28, 0.0, build_land, (Vector((1.7, -2.45, 0.95)), Vector((0.0, 0.05, 0.62)))),
        ("landing-roll", pose_roll, 36, 0.0, build_roll, (Vector((1.85, -2.55, 0.78)), Vector((0.0, 0.0, 0.48)))),
        ("grapple-hook", pose_grapple, -20, 0.15, build_grapple, (Vector((1.9, -1.85, 1.55)), Vector((0.15, 0.35, 1.25)))),
        ("immunity-glow", pose_run, 18, 0.0, build_immune, (Vector((1.75, -2.55, 1.15)), Vector((0.0, 0.0, 0.95)))),
        ("punch-stagger", pose_stagger, -12, 0.0, build_stagger, (Vector((1.35, -1.95, 1.45)), Vector((0.0, 0.0, 1.25)))),
        ("launch-pad", pose_launch, 16, 0.85, build_launch, (Vector((1.9, -2.7, 1.35)), Vector((0.0, 0.0, 1.05)))),
        ("wall-run", pose_wall, 8, 0.0, build_wall, (Vector((1.55, -1.35, 0.72)), Vector((0.05, 0.42, 0.42)))),
    ]
    totals = []
    for name, pose, yaw, lift, build, look in shots:
        totals.append((name,) + shot(arm, cam, name, pose, yaw, lift, build, look, rest))

    clear_fx()
    punch_path = os.path.join(OUT, "_tagger.png")
    victim_path = os.path.join(OUT, "_runner.png")
    apply_pose(arm, pose_punch, 0.0, 24)
    slab = bpy.data.objects.get("PropSlab")
    ground_solids = [solid_of(slab)] if slab is not None else []
    s1, w1, f1, pair = measure(arm, ground_solids, rest)
    print("NOCLIP", "tagger", "self_cm", round(s1 * 100, 2), "world_cm", round(w1 * 100, 2), "fails", f1, "pair", pair)
    if not MEASURE_ONLY:
        cam.location = Vector((1.65, -2.4, 1.2))
        look_at(cam, Vector((0.1, 0.0, 0.95)))
        bpy.context.scene.render.filepath = punch_path
        bpy.ops.render.render(write_still=True)
        vignette(punch_path, TAG)

    apply_pose(arm, pose_stagger, 0.0, -16)
    s2, w2, f2, pair2 = measure(arm, ground_solids, rest)
    print("NOCLIP", "runner", "self_cm", round(s2 * 100, 2), "world_cm", round(w2 * 100, 2), "fails", f2, "pair", pair2)
    if not MEASURE_ONLY:
        cam.location = Vector((1.65, -2.4, 1.2))
        look_at(cam, Vector((0.0, 0.0, 1.0)))
        bpy.context.scene.render.filepath = victim_path
        bpy.ops.render.render(write_still=True)
        dest = os.path.join(OUT, "tagged-flash.png")
        stitch(punch_path, victim_path, dest)
        os.remove(punch_path)
        os.remove(victim_path)

    self_max = max([row[1] for row in totals] + [s1, s2])
    world_max = max([row[2] for row in totals] + [w1, w2])
    fails = sum(row[3] for row in totals) + f1 + f2
    clips = len(totals) + 2
    frames = clips
    print(
        "no-clip clips=%d frames=%d worldMax=%.2f selfMax=%.2f socketRest=%.2f fails=%d"
        % (clips, frames, world_max * 100.0, self_max * 100.0, socket * 100.0, fails)
    )
    if fails:
        sys.exit(1)


if __name__ == "__main__":
    main()
