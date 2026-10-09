"""Hip, knee, and ankle joints for a new Hier mannequin.

Reads the shipped Dummy_Mannequin_Tan_Hier_Hi.fbx and writes a sibling FBX.
The shipped file is not modified and nothing is bound into the avatar prefab.

Each new joint is a rigid piece parented to its bone. A sphere sits on the
bone head. The neighbouring shell is pushed out to a socket, and the cuff is
pulled inside a cone so the swing stays outside the other mesh. No boolean,
so the panels stay closed islands instead of fanning into strips.
"""
import os
import sys

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_loco_stills as loco
import prove_pass4 as prove

ROOT = loco.ROOT
SRC = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT_FBX = os.path.join(
    ROOT, "Assets", "Art", "Characters", "HiPoly", "Candidate", "Dummy_Mannequin_Tan_Hier_Clearance.fbx"
)

HIP_BALL = 0.026
HIP_SOCKET = 0.036
KNEE_BALL = 0.026
KNEE_SOCKET = 0.034
KNEE_SLOPE = 0.20
ANKLE_BALL = 0.022
ANKLE_SOCKET = 0.030


def boundary_of(obj):
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    count = sum(1 for edge in bm.edges if edge.is_boundary)
    bm.free()
    return count


def head_axis(arm, bone):
    pb = arm.pose.bones[bone]
    head = arm.matrix_world @ pb.head
    tail = arm.matrix_world @ pb.tail
    return head, (tail - head).normalized()


def add_sphere(obj, center, radius):
    """Closed sphere island on this bone-parented piece. Parenting is restored."""
    parent = obj.parent
    bone = obj.parent_bone
    kept = obj.matrix_world.copy()
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=radius, location=center, segments=32, ring_count=16
    )
    sph = bpy.context.active_object
    for poly in sph.data.polygons:
        poly.use_smooth = True
    if obj.data.materials:
        sph.data.materials.append(obj.data.materials[0])
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    sph.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.join()
    obj.parent = parent
    obj.parent_type = "BONE"
    obj.parent_bone = bone
    bpy.context.view_layer.update()
    obj.matrix_world = kept
    bpy.context.view_layer.update()


def bool_islands(obj, pivot, radius):
    """Cut a spherical socket into only the islands the sphere touches.

    A boolean on the whole multi-island shell drops the islands it does not
    touch. Separating first keeps the panels, and each cut stays closed.
    """
    name = obj.name
    parent = obj.parent
    bone = obj.parent_bone
    kept = obj.matrix_world.copy()
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.separate(type="LOOSE")
    bpy.ops.object.mode_set(mode="OBJECT")
    parts = [piece for piece in bpy.context.selected_objects if piece.type == "MESH"]
    cut = 0
    for part in parts:
        mw = part.matrix_world
        if not any(((mw @ vert.co) - pivot).length < radius + 0.008 for vert in part.data.vertices):
            continue
        backup = part.data.copy()
        before = len(part.data.vertices)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, location=pivot, segments=28, ring_count=14)
        sph = bpy.context.active_object
        mod = part.modifiers.new("sock", "BOOLEAN")
        mod.operation = "DIFFERENCE"
        mod.object = sph
        mod.solver = "EXACT"
        bpy.context.view_layer.objects.active = part
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except RuntimeError as ex:
            print("BOOL FAIL", name, ex, flush=True)
            if mod.name in part.modifiers:
                part.modifiers.remove(mod)
        mesh = sph.data
        bpy.data.objects.remove(sph, do_unlink=True)
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
        after_b = boundary_of(part)
        if after_b > 0 or len(part.data.vertices) < 12:
            print("BOOL REVERT", name, before, "->", len(part.data.vertices), "boundary", after_b, flush=True)
            old = part.data
            part.data = backup
            if old.users == 0:
                bpy.data.meshes.remove(old)
            continue
        bpy.data.meshes.remove(backup)
        cut += 1
        for poly in part.data.polygons:
            poly.use_smooth = True
    bpy.ops.object.select_all(action="DESELECT")
    alive = [part for part in parts if part.name in bpy.data.objects]
    for part in alive:
        part.select_set(True)
    bpy.context.view_layer.objects.active = alive[0]
    if len(alive) > 1:
        bpy.ops.object.join()
    fused = bpy.context.view_layer.objects.active
    fused.name = name
    fused.parent = parent
    fused.parent_type = "BONE"
    fused.parent_bone = bone
    bpy.context.view_layer.update()
    fused.matrix_world = kept
    bpy.context.view_layer.update()
    print("socket", name, "islands", len(parts), "cut", cut, "boundary", boundary_of(fused), flush=True)
    return fused


def hide_inside_ball(obj, pivot, radius, along_axis, along_max, skip):
    """Pull the old joint blob just inside the new ball so it does not z-fight."""
    mw = obj.matrix_world
    inv = mw.inverted()
    moved = 0
    inner = radius * 0.90
    for index, vert in enumerate(obj.data.vertices):
        if index in skip:
            continue
        world = mw @ vert.co
        vec = world - pivot
        dist = vec.length
        along = vec.dot(along_axis) if along_axis is not None else dist
        if dist < 1e-5 or dist > radius + 0.012 or along > along_max:
            continue
        vert.co = inv @ (pivot + vec.normalized() * inner)
        moved += 1
    obj.data.update()
    return moved


def cone_limit(obj, pivot, axis, d0, d1, r0, r1, skip):
    """Pull verts inside a linear cone. Faces stay connected; islands stay closed."""
    mw = obj.matrix_world
    inv = mw.inverted()
    moved = 0
    span = max(d1 - d0, 1e-4)
    for index, vert in enumerate(obj.data.vertices):
        if index in skip:
            continue
        world = mw @ vert.co
        vec = world - pivot
        along = vec.dot(axis)
        if along < d0 or along > d1:
            continue
        radial = vec - axis * along
        limit = r0 + (r1 - r0) * ((along - d0) / span)
        if radial.length <= limit or radial.length < 1e-6:
            continue
        vert.co = inv @ (pivot + axis * along + radial.normalized() * limit)
        moved += 1
    obj.data.update()
    return moved


def sole_ids(obj):
    mw = obj.matrix_world
    zs = [(mw @ vert.co).z for vert in obj.data.vertices]
    floor = min(zs)
    return {index for index, z in enumerate(zs) if z <= floor + 0.014}


def shade(obj):
    for poly in obj.data.polygons:
        poly.use_smooth = True
    obj.data.update()


def bvh_of(name):
    verts, polys = loco.world_verts(name)
    return BVHTree.FromPolygons(verts, polys)


def shrink_out(obj, other, pivot, axis, min_r, skip):
    """Pull this piece's verts toward the bone until they leave the other shell."""
    tree = bvh_of(other)
    mw = obj.matrix_world
    inv = mw.inverted()
    joint = pivot
    moved = 0
    for index, vert in enumerate(obj.data.vertices):
        if index in skip:
            continue
        world = mw @ vert.co
        if (world - joint).length <= 0.030:
            continue
        nearest, normal, _i, dist = tree.find_nearest(world)
        if nearest is None or dist is None or normal is None:
            continue
        if dist <= 0.005 or dist > 0.045:
            continue
        if (world - nearest).dot(normal) >= 0.0:
            continue
        vec = world - pivot
        along = vec.dot(axis)
        radial = vec - axis * along
        if radial.length <= min_r + 0.001:
            continue
        new_r = max(min_r, radial.length - dist - 0.003)
        vert.co = inv @ (pivot + axis * along + radial.normalized() * new_r)
        moved += 1
    if moved:
        obj.data.update()
    return moved


def push_out(obj, other, joint):
    """Place shell verts that sit inside the other piece just outside its surface."""
    tree = bvh_of(other)
    mw = obj.matrix_world
    inv = mw.inverted()
    moved = 0
    for vert in obj.data.vertices:
        world = mw @ vert.co
        if (world - joint).length <= 0.030:
            continue
        nearest, normal, _i, dist = tree.find_nearest(world)
        if nearest is None or dist is None or normal is None:
            continue
        if dist <= 0.005 or dist > 0.045:
            continue
        if (world - nearest).dot(normal) >= 0.0:
            continue
        if normal.length < 1e-8:
            continue
        step = nearest + normal.normalized() * 0.007 - world
        if step.length > 0.012:
            step = step.normalized() * 0.012
        vert.co = inv @ (world + step)
        moved += 1
    if moved:
        obj.data.update()
    return moved


def hip_euler(side, flex, abd, rot):
    """Flexion and extension are local X. Abduction is local Z. Twist is local Y."""
    x = -flex
    z = -abd if side == "L" else abd
    return x, rot, z


def pose_hip(arm, side, flex, abd, rot):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    loco.set_e(arm, "UpperLeg_" + side, *hip_euler(side, flex, abd, rot))
    bpy.context.view_layer.update()


def pose_knee(arm, side, bend):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    loco.set_e(arm, "LowerLeg_" + side, bend, 0.0, 0.0)
    bpy.context.view_layer.update()


def pose_ankle(arm, side, x_deg, y_deg):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    loco.set_e(arm, "Foot_" + side, x_deg, y_deg, 0.0)
    bpy.context.view_layer.update()


def pair_depth(arm, child_name, parent_name, bone):
    child_v, _cp = loco.world_verts(child_name)
    parent_v, parent_p = loco.world_verts(parent_name)
    _cv, child_p = loco.world_verts(child_name)
    joint = arm.matrix_world @ arm.pose.bones[bone].head
    into_parent = loco.side_depth(child_v, BVHTree.FromPolygons(parent_v, parent_p), True, joint, None)
    into_child = loco.side_depth(parent_v, BVHTree.FromPolygons(_cv, child_p), True, joint, None)
    return max(into_parent, into_child)


def hip_depth(arm, side):
    bone = "UpperLeg_" + side
    child = "Mesh_UpperLeg_" + side
    worst = 0.0
    for parent in ("Mesh_Hips", "Mesh_Spine", "Mesh_Chest"):
        worst = max(worst, pair_depth(arm, child, parent, bone))
    return worst


def clear_hips(arm):
    # Positive flex is flexion. -20 is the 20° extension sample.
    samples = [(flex, 0, 0) for flex in (0, 30, 60, 90, 120)]
    samples.append((-20, 0, 0))
    for flex in (0, 60, 120):
        for abd in (20, 35):
            for rot in (-25, 0, 25):
                samples.append((flex, abd, rot))
    for _round in range(3):
        total = 0
        for side in ("L", "R"):
            child = "Mesh_UpperLeg_" + side
            for flex, abd, rot in samples:
                pose_hip(arm, side, flex, abd, rot)
                pivot, axis = head_axis(arm, "UpperLeg_" + side)
                total += shrink_out(bpy.data.objects[child], "Mesh_Hips", pivot, axis, 0.016, ())
                total += shrink_out(bpy.data.objects[child], "Mesh_Spine", pivot, axis, 0.016, ())
                joint = arm.matrix_world @ arm.pose.bones["UpperLeg_" + side].head
                total += push_out(bpy.data.objects["Mesh_Hips"], child, joint)
                total += push_out(bpy.data.objects["Mesh_Spine"], child, joint)
        print("hip-clear", _round, total, flush=True)
        if total == 0:
            break
    loco.clear_pose(arm)
    bpy.context.view_layer.update()


def clear_knees(arm):
    for _round in range(3):
        total = 0
        for side in ("L", "R"):
            for bend in (0, 40, 80, 110, 140, 155):
                pose_knee(arm, side, bend)
                knee, shin_axis = head_axis(arm, "LowerLeg_" + side)
                hip, _up = head_axis(arm, "UpperLeg_" + side)
                toward = (hip - knee).normalized()
                total += shrink_out(
                    bpy.data.objects["Mesh_LowerLeg_" + side], "Mesh_UpperLeg_" + side,
                    knee, shin_axis, 0.012, (),
                )
                total += shrink_out(
                    bpy.data.objects["Mesh_UpperLeg_" + side], "Mesh_LowerLeg_" + side,
                    knee, toward, 0.012, (),
                )
                total += push_out(bpy.data.objects["Mesh_UpperLeg_" + side], "Mesh_LowerLeg_" + side, knee)
        print("knee-clear", _round, total, flush=True)
        if total == 0:
            break
    loco.clear_pose(arm)
    bpy.context.view_layer.update()


def clear_ankles(arm, soles):
    poses = [(0, 0), (-25, 0), (45, 0), (0, 15), (0, -15), (-25, 15), (-25, -15), (45, 15), (45, -15)]
    for _round in range(3):
        total = 0
        for side in ("L", "R"):
            foot = "Mesh_Foot_" + side
            shin = "Mesh_LowerLeg_" + side
            for x_deg, y_deg in poses:
                pose_ankle(arm, side, x_deg, y_deg)
                ankle, foot_axis = head_axis(arm, "Foot_" + side)
                _knee, shin_axis = head_axis(arm, "LowerLeg_" + side)
                total += shrink_out(
                    bpy.data.objects[foot], shin, ankle, foot_axis, 0.012, soles[side],
                )
                total += shrink_out(
                    bpy.data.objects[shin], foot, ankle, -shin_axis, 0.012, (),
                )
                total += push_out(bpy.data.objects[shin], foot, ankle)
        print("ankle-clear", _round, total, flush=True)
        if total == 0:
            break
    loco.clear_pose(arm)
    bpy.context.view_layer.update()


def rest_others(arm):
    """One rest pass on every hinge so rigJoint can reach 0 without a boolean."""
    pairs = []
    for bone, child, parents in (
        ("UpperArm_L", "Mesh_UpperArm_L", ("Mesh_Shoulder_L", "Mesh_Chest")),
        ("UpperArm_R", "Mesh_UpperArm_R", ("Mesh_Shoulder_R", "Mesh_Chest")),
        ("LowerArm_L", "Mesh_LowerArm_L", ("Mesh_UpperArm_L",)),
        ("LowerArm_R", "Mesh_LowerArm_R", ("Mesh_UpperArm_R",)),
        ("Hand_L", "Mesh_Hand_L", ("Mesh_LowerArm_L",)),
        ("Hand_R", "Mesh_Hand_R", ("Mesh_LowerArm_R",)),
        ("Head", "Mesh_Head", ("Mesh_Neck",)),
        ("Neck", "Mesh_Neck", ("Mesh_Chest",)),
    ):
        for parent in parents:
            pairs.append((bone, child, parent))
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    for _round in range(2):
        total = 0
        for bone, child, parent in pairs:
            pivot, axis = head_axis(arm, bone)
            total += shrink_out(bpy.data.objects[child], parent, pivot, axis, 0.012, ())
            joint = arm.matrix_world @ arm.pose.bones[bone].head
            total += push_out(bpy.data.objects[parent], child, joint)
        print("rest-other", _round, total, flush=True)
        if total == 0:
            break


def socket_normals(obj, pivots, radius):
    """Socket faces point at the ball. The rest of the shell is left as shipped."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    bm.normal_update()
    flipped = 0
    for face in bm.faces:
        center = face.calc_center_median()
        world = obj.matrix_world @ center
        normal = (obj.matrix_world.to_3x3() @ face.normal)
        if normal.length < 1e-8:
            continue
        normal.normalize()
        for pivot in pivots:
            vec = world - pivot
            dist = vec.length
            if dist < 1e-5 or abs(dist - radius) > 0.008:
                continue
            # Material is outside the socket, so the normal points back at the ball.
            if normal.dot(vec) > 0.0:
                face.normal_flip()
                flipped += 1
            break
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return flipped


def seat_sole(arm):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = None
    for name in ("Mesh_Foot_L", "Mesh_Foot_R"):
        obj = bpy.data.objects[name]
        z = min((obj.matrix_world @ vert.co).z for vert in obj.data.vertices)
        low = z if low is None else min(low, z)
    if low is None or abs(low) < 0.0008:
        print("sole", round(low or 0.0, 4), flush=True)
        return
    delta = Vector((0.0, 0.0, -low))
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in arm.data.edit_bones:
        if bone.parent is None or not bone.use_connect:
            bone.head += delta
        bone.tail += delta
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()
    obj = bpy.data.objects["Mesh_Foot_L"]
    z = min((obj.matrix_world @ vert.co).z for vert in obj.data.vertices)
    print("sole", round(low, 4), "->", round(z, 4), flush=True)


def last_clear(arm, pose_fn, depth_fn, angles):
    last = None
    for angle in angles:
        pose_fn(angle)
        depth = depth_fn()
        if depth > 0.0:
            print("  fail", angle, round(depth, 2), flush=True)
            break
        last = angle
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    return last


def measure(arm):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    fails = prove.directed_fails(arm, prove.pack(arm))
    print("REST rigJoint", len(fails), flush=True)
    buckets = {}
    for key, depth in fails.items():
        bucket = prove.joint_bucket(key)
        buckets.setdefault(bucket, []).append((depth, key))
    for bucket, rows in sorted(buckets.items()):
        rows.sort(reverse=True)
        print(" rest", bucket, len(rows), round(rows[0][0], 2), rows[0][1], flush=True)
    if not fails:
        print(" rest all-joints 0", flush=True)

    flex, ext, abd, rot = [], [], [], []
    for side in ("L", "R"):
        flex.append(last_clear(
            arm,
            lambda a, s=side: pose_hip(arm, s, a, 0, 0),
            lambda s=side: hip_depth(arm, s),
            list(range(0, 141, 5)),
        ))
        ext.append(last_clear(
            arm,
            lambda a, s=side: pose_hip(arm, s, -a, 0, 0),
            lambda s=side: hip_depth(arm, s),
            list(range(0, 41, 5)),
        ))
        abd.append(last_clear(
            arm,
            lambda a, s=side: pose_hip(arm, s, 0, a, 0),
            lambda s=side: hip_depth(arm, s),
            list(range(0, 51, 5)),
        ))
        pos = last_clear(
            arm,
            lambda a, s=side: pose_hip(arm, s, 0, 0, a),
            lambda s=side: hip_depth(arm, s),
            list(range(0, 46, 5)),
        )
        neg = last_clear(
            arm,
            lambda a, s=side: pose_hip(arm, s, 0, 0, -a),
            lambda s=side: hip_depth(arm, s),
            list(range(0, 46, 5)),
        )
        rot.append(min(pos or 0, neg or 0))
        print("hip", side, "flex", flex[-1], "ext", ext[-1], "abd", abd[-1], "rot", rot[-1], flush=True)

    knees = []
    for side in ("L", "R"):
        knees.append(last_clear(
            arm,
            lambda a, s=side: pose_knee(arm, s, a),
            lambda s=side: pair_depth(arm, "Mesh_LowerLeg_" + s, "Mesh_UpperLeg_" + s, "LowerLeg_" + s),
            list(range(0, 171, 5)),
        ))
        print("knee", side, knees[-1], flush=True)

    dorsi, plantar, inv, ev = [], [], [], []
    for side in ("L", "R"):
        dorsi.append(last_clear(
            arm,
            lambda a, s=side: pose_ankle(arm, s, -a, 0),
            lambda s=side: pair_depth(arm, "Mesh_Foot_" + s, "Mesh_LowerLeg_" + s, "Foot_" + s),
            list(range(0, 46, 5)),
        ))
        plantar.append(last_clear(
            arm,
            lambda a, s=side: pose_ankle(arm, s, a, 0),
            lambda s=side: pair_depth(arm, "Mesh_Foot_" + s, "Mesh_LowerLeg_" + s, "Foot_" + s),
            list(range(0, 61, 5)),
        ))
        inv.append(last_clear(
            arm,
            lambda a, s=side: pose_ankle(arm, s, 0, -a),
            lambda s=side: pair_depth(arm, "Mesh_Foot_" + s, "Mesh_LowerLeg_" + s, "Foot_" + s),
            list(range(0, 31, 5)),
        ))
        ev.append(last_clear(
            arm,
            lambda a, s=side: pose_ankle(arm, s, 0, a),
            lambda s=side: pair_depth(arm, "Mesh_Foot_" + s, "Mesh_LowerLeg_" + s, "Foot_" + s),
            list(range(0, 31, 5)),
        ))
        print(
            "ankle", side, "dorsi", dorsi[-1], "plantar", plantar[-1],
            "inv", inv[-1], "ev", ev[-1], flush=True,
        )

    def worst(values):
        nums = [v for v in values if v is not None]
        return min(nums) if nums else 0

    line_hip = "hip-range flex={0} ext={1} abd={2} rot={3}".format(
        worst(flex), worst(ext), worst(abd), worst(rot)
    )
    line_ankle = "ankle-range dorsi={0} plantar={1} inv={2} ev={3}".format(
        worst(dorsi), worst(plantar), worst(inv), worst(ev)
    )
    line_knee = "knee={0}".format(worst(knees))
    print(line_hip, flush=True)
    print(line_ankle, flush=True)
    print(line_knee, flush=True)
    return line_hip, line_ankle, line_knee, len(fails)


def sync_eval_world():
    """Write the evaluated bone-parent transform back before FBX export.

    Mode switches leave matrix_world stale. The overlap test reads the
    evaluated mesh, and the exported file would otherwise shift a piece.
    Parent inverse, scale, and bone roll are left as they are.
    """
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith("Mesh_"):
            continue
        ev = obj.evaluated_get(deps)
        obj.matrix_world = ev.matrix_world.copy()
    bpy.context.view_layer.update()


def export_fbx(arm):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    sync_eval_world()
    os.makedirs(os.path.dirname(OUT_FBX), exist_ok=True)
    if os.path.abspath(OUT_FBX) == os.path.abspath(SRC):
        raise RuntimeError("refusing to overwrite the shipped mannequin")
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name in loco.MESHES:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=OUT_FBX,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        apply_scale_options="FBX_SCALE_NONE",
        path_mode="AUTO",
        axis_forward="-Z",
        axis_up="Y",
    )
    print("WROTE", OUT_FBX, os.path.getsize(OUT_FBX), flush=True)


def subdivide(obj, cuts):
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.subdivide_edges(bm, edges=list(bm.edges), cuts=cuts)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def snap_ball(obj, pivot, radius, reach, skip):
    """Collapse the joint blob onto a sphere so long panels leave from the ball."""
    mw = obj.matrix_world
    inv = mw.inverted()
    moved = 0
    for index, vert in enumerate(obj.data.vertices):
        if index in skip:
            continue
        world = mw @ vert.co
        vec = world - pivot
        dist = vec.length
        if dist < 1e-5 or dist >= reach:
            continue
        vert.co = inv @ (pivot + vec.normalized() * radius)
        moved += 1
    obj.data.update()
    return moved


def make_cone(start, end, r0, r1):
    mid = (start + end) * 0.5
    bpy.ops.mesh.primitive_cone_add(
        radius1=max(r0, 0.004), radius2=max(r1, 0.004),
        depth=(end - start).length, vertices=24, location=mid,
    )
    obj = bpy.context.active_object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = (end - start).to_track_quat("Z", "Y")
    bpy.context.view_layer.update()
    return obj


def cut_solid(obj, cutter):
    """Boolean a closed cutter out of the islands it touches."""
    name = obj.name
    parent = obj.parent
    bone = obj.parent_bone
    kept = obj.matrix_world.copy()
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.separate(type="LOOSE")
    bpy.ops.object.mode_set(mode="OBJECT")
    parts = [piece for piece in bpy.context.selected_objects if piece.type == "MESH"]
    mw = cutter.matrix_world
    box = [mw @ Vector(corner) for corner in cutter.bound_box]
    lo = Vector((min(p.x for p in box), min(p.y for p in box), min(p.z for p in box)))
    hi = Vector((max(p.x for p in box), max(p.y for p in box), max(p.z for p in box)))
    cut = 0
    for part in parts:
        pm = part.matrix_world
        near = False
        for vert in part.data.vertices:
            world = pm @ vert.co
            if (lo.x - 0.01 <= world.x <= hi.x + 0.01 and lo.y - 0.01 <= world.y <= hi.y + 0.01
                    and lo.z - 0.01 <= world.z <= hi.z + 0.01):
                near = True
                break
        if not near:
            continue
        backup = part.data.copy()
        mod = part.modifiers.new("ch", "BOOLEAN")
        mod.operation = "DIFFERENCE"
        mod.object = cutter
        mod.solver = "EXACT"
        bpy.context.view_layer.objects.active = part
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except RuntimeError as ex:
            print("BOOL FAIL", name, ex, flush=True)
            if mod.name in part.modifiers:
                part.modifiers.remove(mod)
        if boundary_of(part) > 0 or len(part.data.vertices) < 12:
            old = part.data
            part.data = backup
            if old.users == 0:
                bpy.data.meshes.remove(old)
            continue
        bpy.data.meshes.remove(backup)
        cut += 1
        for poly in part.data.polygons:
            poly.use_smooth = True
    bpy.ops.object.select_all(action="DESELECT")
    alive = [part for part in parts if part.name in bpy.data.objects]
    for part in alive:
        part.select_set(True)
    bpy.context.view_layer.objects.active = alive[0]
    if len(alive) > 1:
        bpy.ops.object.join()
    fused = bpy.context.view_layer.objects.active
    fused.name = name
    fused.parent = parent
    fused.parent_type = "BONE"
    fused.parent_bone = bone
    bpy.context.view_layer.update()
    fused.matrix_world = kept
    bpy.context.view_layer.update()
    return fused


def build(arm):
    for side in ("L", "R"):
        for bone in ("Mesh_UpperLeg_" + side, "Mesh_LowerLeg_" + side, "Mesh_Foot_" + side):
            subdivide(bpy.data.objects[bone], 3)
    for side in ("L", "R"):
        sole = sole_ids(bpy.data.objects["Mesh_Foot_" + side])
        hip, thigh_axis = head_axis(arm, "UpperLeg_" + side)
        knee, shin_axis = head_axis(arm, "LowerLeg_" + side)
        ankle, foot_axis = head_axis(arm, "Foot_" + side)
        toward_hip = (hip - knee).normalized()
        thigh = bpy.data.objects["Mesh_UpperLeg_" + side]
        shin = bpy.data.objects["Mesh_LowerLeg_" + side]
        foot = bpy.data.objects["Mesh_Foot_" + side]
        print("snap hip", side, snap_ball(thigh, hip, HIP_BALL * 0.92, 0.12, ()), flush=True)
        cone_limit(thigh, hip, thigh_axis, 0.03, 0.42, 0.018, 0.050, ())
        add_sphere(thigh, hip, HIP_BALL)
        print("snap knee", side, snap_ball(shin, knee, KNEE_BALL * 0.92, 0.08, ()), flush=True)
        snap_ball(thigh, knee, KNEE_BALL * 0.92, 0.07, ())
        cone_limit(shin, knee, shin_axis, 0.030, 0.16, KNEE_SLOPE * 0.030, KNEE_SLOPE * 0.16, ())
        cone_limit(thigh, knee, toward_hip, 0.030, 0.14, KNEE_SLOPE * 0.030, KNEE_SLOPE * 0.14, ())
        add_sphere(shin, knee, KNEE_BALL)
        print("snap ankle", side, snap_ball(foot, ankle, ANKLE_BALL * 0.92, 0.05, sole), flush=True)
        cone_limit(foot, ankle, foot_axis, 0.018, 0.08, 0.014, 0.036, sole)
        cone_limit(shin, ankle, -shin_axis, 0.024, 0.09, 0.014, 0.032, ())
        add_sphere(foot, ankle, ANKLE_BALL)
        shade(thigh)
        shade(shin)
        shade(foot)

    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    hips = bpy.data.objects["Mesh_Hips"]
    spine = bpy.data.objects["Mesh_Spine"]
    poses = ((0, 0), (40, 0), (80, 0), (120, 0), (-20, 0), (0, 35), (60, 35), (120, 35))
    for side in ("L", "R"):
        hip, _axis = head_axis(arm, "UpperLeg_" + side)
        hips = bool_islands(hips, hip, HIP_SOCKET)
        spine = bool_islands(spine, hip, HIP_BALL + 0.008)
        for flex, abd in poses:
            pose_hip(arm, side, flex, abd, 0)
            pivot, axis = head_axis(arm, "UpperLeg_" + side)
            cutter = make_cone(pivot + axis * 0.02, pivot + axis * 0.24, 0.030, 0.060)
            hips = cut_solid(hips, cutter)
            spine = cut_solid(spine, cutter)
            bpy.data.objects.remove(cutter, do_unlink=True)
        loco.clear_pose(arm)
        bpy.context.view_layer.update()
        knee, shin_axis = head_axis(arm, "LowerLeg_" + side)
        thigh = bool_islands(bpy.data.objects["Mesh_UpperLeg_" + side], knee, KNEE_SOCKET)
        for bend in (0, 50, 100, 140, 155):
            pose_knee(arm, side, bend)
            knee, shin_axis = head_axis(arm, "LowerLeg_" + side)
            cutter = make_cone(knee + shin_axis * 0.02, knee + shin_axis * 0.12, 0.020, KNEE_SLOPE * 0.12 + 0.008)
            thigh = cut_solid(thigh, cutter)
            bpy.data.objects.remove(cutter, do_unlink=True)
        loco.clear_pose(arm)
        bpy.context.view_layer.update()
        ankle, _foot_axis = head_axis(arm, "Foot_" + side)
        shin = bpy.data.objects["Mesh_LowerLeg_" + side]
        bool_islands(shin, ankle, ANKLE_SOCKET)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    for name in ("Mesh_Hips", "Mesh_Spine", "Mesh_UpperLeg_L", "Mesh_LowerLeg_L", "Mesh_Foot_L"):
        obj = bpy.data.objects[name]
        print("health", name, "boundary", boundary_of(obj), "verts", len(obj.data.vertices), flush=True)


def main():
    if os.environ.get("REBUILD_CANDIDATE") != "1":
        raise SystemExit("refusing to rebuild the clearance rig; set REBUILD_CANDIDATE=1")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SRC)
    arm = bpy.data.objects["DummyArmature"]
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    build(arm)
    seat_sole(arm)
    lines = measure(arm)
    export_fbx(arm)
    out_dir = os.path.join(ROOT, "Docs", "LocoStills", "pass7")
    os.makedirs(out_dir, exist_ok=True)
    with open(os.path.join(out_dir, "ranges.txt"), "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines[:3]) + "\n")
        handle.write("rigJoint={0}\n".format(lines[3]))


if __name__ == "__main__":
    main()
