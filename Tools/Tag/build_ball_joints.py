"""Artist-mannequin ball joints for the unbound clearance candidate.

The shipped Hier mannequin is not modified. Each hinge is rebuilt with
booleans: a sphere on the child pivot, a concave socket 0.3 cm larger in
the parent, and a tapered neck that stays inside a cleared cone.
"""
import os
import sys

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_loco_stills as loco

ROOT = loco.ROOT
SRC = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT_FBX = os.path.join(
    ROOT, "Assets", "Art", "Characters", "HiPoly", "Candidate", "Dummy_Mannequin_Tan_Hier_Clearance.fbx"
)
GAP = 0.0035

# bone, child mesh, parent meshes, ball radius, trim radius, cone length, fallback tube radius
JOINTS = [
    # 3.0 cm hip balls. The 4.5 cm balls held the lower spine inside the shell.
    ("UpperLeg_L", "Mesh_UpperLeg_L", ("Mesh_Hips",), 0.030, 0.155, 0.18, 0.078),
    ("UpperLeg_R", "Mesh_UpperLeg_R", ("Mesh_Hips",), 0.030, 0.155, 0.18, 0.078),
    ("LowerLeg_L", "Mesh_LowerLeg_L", ("Mesh_UpperLeg_L",), 0.036, 0.070, 0.12, 0.055),
    ("LowerLeg_R", "Mesh_LowerLeg_R", ("Mesh_UpperLeg_R",), 0.036, 0.070, 0.12, 0.055),
    ("Foot_L", "Mesh_Foot_L", ("Mesh_LowerLeg_L",), 0.030, 0.046, 0.07, 0.040),
    ("Foot_R", "Mesh_Foot_R", ("Mesh_LowerLeg_R",), 0.030, 0.046, 0.07, 0.040),
    ("UpperArm_L", "Mesh_UpperArm_L", ("Mesh_Shoulder_L", "Mesh_Chest"), 0.040, 0.115, 0.18, 0.055),
    ("UpperArm_R", "Mesh_UpperArm_R", ("Mesh_Shoulder_R", "Mesh_Chest"), 0.040, 0.115, 0.18, 0.055),
    ("LowerArm_L", "Mesh_LowerArm_L", ("Mesh_UpperArm_L",), 0.030, 0.062, 0.10, 0.042),
    ("LowerArm_R", "Mesh_LowerArm_R", ("Mesh_UpperArm_R",), 0.030, 0.062, 0.10, 0.042),
    ("Hand_L", "Mesh_Hand_L", ("Mesh_LowerArm_L",), 0.028, 0.058, 0.08, 0.034),
    ("Hand_R", "Mesh_Hand_R", ("Mesh_LowerArm_R",), 0.028, 0.058, 0.08, 0.034),
]


def world_head(arm, bone):
    pose = arm.pose.bones[bone]
    head = arm.matrix_world @ pose.head
    tail = arm.matrix_world @ pose.tail
    return head, (tail - head).normalized()


def new_sphere(name, loc, radius, seg=28):
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=radius, location=loc, segments=seg, ring_count=max(12, seg // 2)
    )
    obj = bpy.context.active_object
    obj.name = name
    return obj


def new_cone(name, start, end, r0, r1):
    direction = end - start
    length = direction.length
    if length < 1e-4:
        return None
    mid = (start + end) * 0.5
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.mesh.primitive_cone_add(
        radius1=max(r0, 0.004), radius2=max(r1, 0.004), depth=length, vertices=28, location=mid
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    bpy.context.view_layer.update()
    return obj


def outward_normals(obj):
    if obj.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def _bool_once(target, cutter, operation, solver):
    outward_normals(target)
    mod = target.modifiers.new("balljoint", "BOOLEAN")
    mod.operation = operation
    mod.object = cutter
    mod.solver = solver
    if hasattr(mod, "use_hole_tolerant"):
        mod.use_hole_tolerant = True
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.mode_set(mode="OBJECT")
    try:
        bpy.ops.object.modifier_apply(modifier=mod.name)
        return True
    except RuntimeError as ex:
        print("BOOL FAIL", target.name, operation, solver, ex)
        if mod.name in target.modifiers:
            target.modifiers.remove(mod)
        return False


def shell_count(obj, pivot, radius):
    """Verts sitting on a sphere. Used to notice a boolean that ate a ball or a cup."""
    if radius <= 0.0:
        return 0
    mw = obj.matrix_world
    count = 0
    for vert in obj.data.vertices:
        dist = ((mw @ vert.co) - pivot).length
        if abs(dist - radius) < 0.006:
            count += 1
    return count


def _drop_cutter(cutter):
    mesh = cutter.data
    bpy.data.objects.remove(cutter, do_unlink=True)
    if mesh.users == 0:
        bpy.data.meshes.remove(mesh)


def apply_bool(target, cutter, operation, protect=(), max_boundary=200):
    """Boolean with a revert when the solver eats a ball or the whole mesh.

    protect is (pivot, radius) spheres that must still be present afterwards.
    The child trim is allowed to drop a lot of verts; that is the fat joint blob.
    """
    if cutter is None:
        return False
    backup = target.data.copy()
    before = len(target.data.vertices)
    before_shells = [shell_count(target, pivot, radius) for pivot, radius in protect]

    def healthy():
        after = len(target.data.vertices)
        if operation == "DIFFERENCE" and before > 40 and after < 15:
            return False
        if boundary_edges(target) > max_boundary:
            return False
        for (pivot, radius), had in zip(protect, before_shells):
            if had > 60 and shell_count(target, pivot, radius) < had * 0.55:
                return False
        return True

    def restore():
        old = target.data
        target.data = backup.copy()
        if old.users == 0:
            bpy.data.meshes.remove(old)

    _bool_once(target, cutter, operation, "EXACT")
    if not healthy():
        print("RETRY FAST", target.name, before, "->", len(target.data.vertices), flush=True)
        restore()
        _bool_once(target, cutter, operation, "FAST")
    if not healthy():
        print("REVERT", target.name, before, "->", len(target.data.vertices), flush=True)
        old = target.data
        target.data = backup
        if old.users == 0:
            bpy.data.meshes.remove(old)
        _drop_cutter(cutter)
        return False
    bpy.data.meshes.remove(backup)
    _drop_cutter(cutter)
    return True


def voxel_remesh(obj, size):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if obj.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.remesh_voxel_size = size
    obj.data.remesh_voxel_adaptivity = 0.0
    try:
        bpy.ops.object.voxel_remesh()
        return True
    except RuntimeError as ex:
        print("VOXEL FAIL", obj.name, ex)
        return False


def push_out_of_cone(obj, pivot, axis, radius, length, tube_r):
    """Move parent verts out of the limb taper. The socket sphere is left alone."""
    socket = radius + GAP
    inv = obj.matrix_world.inverted()
    mw = obj.matrix_world
    moved = 0
    for vert in obj.data.vertices:
        world = mw @ vert.co
        vec = world - pivot
        along = vec.dot(axis)
        if along < socket * 0.35 or along > length:
            continue
        radial_vec = vec - axis * along
        radial = radial_vec.length
        t = along / length
        limit = socket * (1.0 - t) + (tube_r + GAP) * t
        if radial >= limit or vec.length <= socket:
            continue
        if radial < 1e-6:
            radial_vec = axis.cross(Vector((1.0, 0.0, 0.0)))
            if radial_vec.length < 1e-6:
                radial_vec = axis.cross(Vector((0.0, 1.0, 0.0)))
        radial_vec.normalize()
        vert.co = inv @ (pivot + axis * along + radial_vec * (limit + 0.002))
        moved += 1
    obj.data.update()
    return moved


def boundary_edges(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    count = sum(1 for edge in bm.edges if edge.is_boundary)
    bm.free()
    return count


def close_mesh(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.fill_holes(sides=0)
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def tube_radius(obj, pivot, axis, along_min, along_max):
    best = 0.0
    mw = obj.matrix_world
    for vert in obj.data.vertices:
        vec = (mw @ vert.co) - pivot
        along = vec.dot(axis)
        if along < along_min or along > along_max:
            continue
        radial = (vec - axis * along).length
        if radial > best:
            best = radial
    return best


def rebuild_child(obj, pivot, axis, radius, trim, tube_r):
    """Cut the fat proximal shell off and replace it with a ball plus a taper."""
    apply_bool(obj, new_sphere("trim", pivot, trim, 32), "DIFFERENCE")
    apply_bool(obj, new_sphere("ball", pivot, radius, 32), "UNION")
    start = pivot + axis * (radius * 0.35)
    end = pivot + axis * (trim + 0.025)
    apply_bool(obj, new_cone("taper", start, end, radius * 0.62, tube_r), "UNION")
    if boundary_edges(obj):
        close_mesh(obj)
    return boundary_edges(obj)


def make_cutter(pivot, axis, radius, length, tube_r, name):
    """Socket sphere plus the cone the taper travels through. Not yet applied."""
    socket = radius + GAP
    ball = new_sphere(name + "s", pivot, socket, 24)
    start = pivot + axis * (radius * 0.15)
    end = pivot + axis * length
    cone = new_cone(name + "c", start, end, socket, tube_r + GAP)
    if cone is None:
        return ball
    return join_objects([ball, cone])


def join_objects(objs):
    objs = [obj for obj in objs if obj is not None]
    if not objs:
        return None
    if len(objs) == 1:
        return objs[0]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    return bpy.context.view_layer.objects.active


def delete_inside(obj, pivot, radius):
    """Drop verts inside a socket sphere. Does not touch the rest of the limb."""
    mw = obj.matrix_world
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    kill = [vert for vert in bm.verts if ((mw @ vert.co) - pivot).length < radius]
    if kill:
        bmesh.ops.delete(bm, geom=kill, context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return len(kill)


def carve_parent(obj, cutters, protect=()):
    cutter = join_objects(cutters)
    if cutter is None:
        return False
    ok = apply_bool(obj, cutter, "DIFFERENCE", protect, max_boundary=40)
    if ok and boundary_edges(obj):
        close_mesh(obj)
    return ok


def _bisect_world(obj, plane_co, plane_no, clear_inner, clear_outer):
    """Bisect in local space. Operators need a 3D view, so this uses bmesh."""
    inv = obj.matrix_world.inverted()
    co = inv @ plane_co
    no = (inv.to_3x3() @ plane_no)
    if no.length < 1e-8:
        return
    no.normalize()
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(
        bm,
        geom=geom,
        plane_co=co,
        plane_no=no,
        clear_inner=clear_inner,
        clear_outer=clear_outer,
    )
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def stump_socket(obj, pivot, axis, radius):
    """Boolean the distal cap only, so a knee cut cannot delete the hip ball."""
    socket = radius + GAP
    plane_co = pivot - axis * (socket + 0.012)
    before = len(obj.data.vertices)
    backup = obj.data.copy()
    had = shell_count(obj, *protect_of(obj.name)[0]) if protect_of(obj.name) else 0
    ball_pivot = protect_of(obj.name)[0] if protect_of(obj.name) else None
    stump = obj.copy()
    stump.data = obj.data.copy()
    bpy.context.collection.objects.link(stump)
    # Body keeps the proximal side (negative). Stump keeps the joint cap.
    _bisect_world(obj, plane_co, axis, False, True)
    _bisect_world(stump, plane_co, axis, True, False)
    if ball_pivot is not None and had > 60 and shell_count(obj, ball_pivot[0], ball_pivot[1]) < had * 0.55:
        print("STUMP SKIP", obj.name, "ball cut off", before, "->", len(obj.data.vertices), flush=True)
        old = obj.data
        obj.data = backup
        if old.users == 0:
            bpy.data.meshes.remove(old)
        mesh = stump.data
        bpy.data.objects.remove(stump, do_unlink=True)
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
        return False
    bpy.data.meshes.remove(backup)
    ok = apply_bool(stump, new_sphere(obj.name + "stump", pivot, socket, 24), "DIFFERENCE", max_boundary=120)
    print(
        "stump-cap", obj.name, "body", len(obj.data.vertices),
        "cap", len(stump.data.vertices), "bool", ok, flush=True,
    )
    if len(stump.data.vertices) < 8:
        mesh = stump.data
        bpy.data.objects.remove(stump, do_unlink=True)
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
        return False
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    stump.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.join()
    if boundary_edges(obj):
        close_mesh(obj)
    print("stump", obj.name, before, "->", len(obj.data.vertices), "boundary", boundary_edges(obj), flush=True)
    return True


def _hemisphere(obj, pivot, axis, radius):
    sph = new_sphere(obj.name + "hemi", pivot, radius, 24)
    old = obj.data
    obj.data = sph.data
    bpy.data.objects.remove(sph, do_unlink=True)
    if old.users == 0:
        bpy.data.meshes.remove(old)
    mw = obj.matrix_world
    inv = mw.inverted()
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    kill = []
    for vert in bm.verts:
        world = mw @ vert.co
        if (world - pivot).dot(axis) > radius * 0.05:
            kill.append(vert)
    bmesh.ops.delete(bm, geom=kill, context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    outward_normals(obj)


def hip_axes(arm, bone):
    """Wide hip cone. Flexion is local X, abduction local Y."""
    axes = []
    for flex in (-25.0, 55.0, 110.0):
        for abd in (-15.0, 40.0):
            loco.clear_pose(arm)
            y = -abd if bone.endswith("_L") else abd
            loco.set_e(arm, bone, flex, y, 0.0)
            bpy.context.view_layer.update()
            _head, axis = world_head(arm, bone)
            axes.append(axis)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    return axes


def knee_axes(arm, bone):
    axes = []
    for knee in (0.0, 50.0, 100.0, 140.0):
        loco.clear_pose(arm)
        loco.set_e(arm, bone, knee, 0.0, 0.0)
        bpy.context.view_layer.update()
        _head, axis = world_head(arm, bone)
        axes.append(axis)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    return axes


def shoulder_axes(arm, bone):
    axes = []
    side = -1.0 if bone.endswith("_L") else 1.0
    for pitch in (-70.0, 20.0, 80.0):
        for out in (15.0, 55.0):
            loco.clear_pose(arm)
            loco.set_e(arm, bone, pitch, side * out, 0.0)
            bpy.context.view_layer.update()
            _head, axis = world_head(arm, bone)
            axes.append(axis)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    return axes


def elbow_axes(arm, bone):
    axes = []
    for elbow in (0.0, -50.0, -100.0, -145.0):
        loco.clear_pose(arm)
        loco.set_e(arm, bone, elbow, 0.0, 0.0)
        bpy.context.view_layer.update()
        _head, axis = world_head(arm, bone)
        axes.append(axis)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    return axes


def extra_axes(arm, bone):
    if bone.startswith("UpperLeg"):
        return hip_axes(arm, bone)
    if bone.startswith("LowerLeg"):
        return knee_axes(arm, bone)
    if bone.startswith("UpperArm"):
        return shoulder_axes(arm, bone)
    if bone.startswith("LowerArm"):
        return elbow_axes(arm, bone)
    return []


def neck_joint(arm):
    """Neck top becomes the ball. The head base is the socket."""
    head, axis = world_head(arm, "Head")
    radius = 0.038
    neck = bpy.data.objects["Mesh_Neck"]
    skull = bpy.data.objects["Mesh_Head"]
    chest = bpy.data.objects["Mesh_Chest"]
    # Cut the neck where it enters the head, then seat a ball on the pivot.
    apply_bool(neck, new_sphere("necktrim", head, 0.090, 28), "DIFFERENCE")
    apply_bool(neck, new_sphere("neckball", head, radius, 28), "UNION")
    start = head - axis * (radius * 0.3)
    end = head - axis * 0.105
    apply_bool(neck, new_cone("necktaper", start, end, radius * 0.7, 0.055), "UNION")
    apply_bool(skull, new_sphere("headsock", head, radius + GAP, 32), "DIFFERENCE")
    # Chest collar around the neck pivot (the neck bone head sits on the chest).
    nhead, naxis = world_head(arm, "Neck")
    apply_bool(chest, new_sphere("chestneck", nhead, 0.042, 24), "DIFFERENCE")
    for obj in (neck, skull, chest):
        if boundary_edges(obj):
            close_mesh(obj)
        print("neck-part", obj.name, "verts", len(obj.data.vertices), "boundary", boundary_edges(obj))


# 140° fold leaves a 40° wedge. tan(20°) is 0.364; 0.27 leaves a few millimetres
# so a float32 round trip does not close the gap. The cone is the allowed tube.
KNEE_SLOPE = 0.27


def fuse_solids(parts):
    """Boolean-union closed cutters. A mesh join leaves internal faces and INTERSECT fails."""
    parts = [obj for obj in parts if obj is not None]
    if not parts:
        return None
    base = parts[0]
    for extra in parts[1:]:
        if not apply_bool(base, extra, "UNION", max_boundary=4000):
            print("FUSE FAIL", base.name, flush=True)
            return None
    return base


def wrap_limb(obj, pivot, axis, slope, d_end, ball_r, own_ball, protect):
    """Replace the tube near a hinge with the clearance cone.

    own_ball keeps the joint sphere (the child cap). The parent starts outside
    that sphere so the cone cap does not fill the socket. A fat keeper past
    d_end leaves the rest of the limb, including the ball at the other end.
    """
    d0 = ball_r * 0.40 if own_ball else (ball_r + GAP)
    parts = []
    if own_ball:
        parts.append(new_sphere(obj.name + "keepball", pivot, ball_r + 0.002, 28))
    parts.append(new_cone(
        obj.name + "cone",
        pivot + axis * d0,
        pivot + axis * d_end,
        max(0.006, slope * d0),
        slope * d_end,
    ))
    parts.append(new_cone(
        obj.name + "keep",
        pivot + axis * (d_end - 0.02),
        pivot + axis * (d_end + 1.35),
        0.36,
        0.36,
    ))
    solid = fuse_solids(parts)
    if solid is None:
        for part in parts:
            if part is not None and part.name in bpy.data.objects:
                _drop_cutter(part)
        return False
    before = len(obj.data.vertices)
    ok = apply_bool(obj, solid, "INTERSECT", protect, max_boundary=400)
    print(
        "wrap", obj.name, before, "->", len(obj.data.vertices),
        "ok", ok, "boundary", boundary_edges(obj), flush=True,
    )
    return ok


def wrap_knees(arm):
    """Thigh and shin both wrap the knee ball. A 140° bend stays inside the wedge."""
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    for side in ("L", "R"):
        knee, shin_axis = world_head(arm, "LowerLeg_" + side)
        hip, _hip_axis = world_head(arm, "UpperLeg_" + side)
        toward_hip = hip - knee
        if toward_hip.dot(-shin_axis) < 0.9 * toward_hip.length:
            print("knee axis off", side, round(toward_hip.dot(-shin_axis), 3), flush=True)
        shin = bpy.data.objects["Mesh_LowerLeg_" + side]
        thigh = bpy.data.objects["Mesh_UpperLeg_" + side]
        # The thigh bone is not exactly opposite the shin. A cone on the shin
        # axis slices the upper thigh into the pelvis.
        toward_hip = (hip - knee).normalized()
        wrap_limb(shin, knee, shin_axis, KNEE_SLOPE, 0.28, 0.036, True, ((knee, 0.036),))
        wrap_limb(thigh, knee, toward_hip, KNEE_SLOPE, 0.32, 0.036, False, ((hip, 0.045),))


def slim_cone(obj, pivot, axis, slope, gap, d_min, d_max):
    """Pull a tube inside a cone. The knee uses wrap_limb; hip and neck still taper."""
    mw = obj.matrix_world
    inv = mw.inverted()
    moved = 0
    for vert in obj.data.vertices:
        world = mw @ vert.co
        vec = world - pivot
        along = vec.dot(axis)
        if along < d_min or along > d_max:
            continue
        radial_vec = vec - axis * along
        radial = radial_vec.length
        if radial < 1e-6:
            continue
        limit = max(0.012, slope * along - gap)
        if radial <= limit + 0.0008:
            continue
        vert.co = inv @ (pivot + axis * along + radial_vec * (limit / radial))
        moved += 1
    if moved:
        obj.data.update()
    return moved


def shrink_shell(obj, pivot, old_r, new_r):
    """Move a joint ball inward. Spanning verts between the two radii come with it."""
    mw = obj.matrix_world
    inv = mw.inverted()
    moved = 0
    for vert in obj.data.vertices:
        world = mw @ vert.co
        vec = world - pivot
        dist = vec.length
        if dist < 1e-6 or abs(dist - old_r) > 0.008:
            continue
        vert.co = inv @ (pivot + vec.normalized() * new_r)
        moved += 1
    if moved:
        obj.data.update()
    return moved


def _pose_axis(arm, bone, flex, abd):
    loco.clear_pose(arm)
    y = -abd if bone.endswith("_L") else abd
    loco.set_e(arm, bone, flex, y, 0.0)
    bpy.context.view_layer.update()
    return world_head(arm, bone)


def carve_flex_cones(arm, bone, parent_name, flexes, abds, slope, reach, start_d):
    """Hollow the parent along the swing. The cutter starts outside the joint ball."""
    parent = bpy.data.objects[parent_name]
    before = len(parent.data.vertices)
    cones = []
    for flex in flexes:
        for abd in abds:
            pivot, axis = _pose_axis(arm, bone, flex, abd)
            start = pivot + axis * start_d
            end = pivot + axis * reach
            cones.append(new_cone(
                bone + "flex", start, end,
                max(0.012, slope * start_d),
                max(0.02, slope * reach),
            ))
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    cutter = fuse_solids(cones)
    ok = False
    if cutter is not None:
        ok = apply_bool(parent, cutter, "DIFFERENCE", protect_of(parent_name), max_boundary=240)
        if ok and 0 < boundary_edges(parent) <= 80:
            close_mesh(parent)
    print(
        "flex-cone", parent_name, bone, before, "->", len(parent.data.vertices),
        "ok", ok, "boundary", boundary_edges(parent), flush=True,
    )
    return ok


def notch_flex(arm, bone, parent_name, flex, abd, gap=0.008):
    """Push parent verts that the posed limb contains out past that shell.

    A boolean fan tears the open pelvis. Moving the penetrated verts keeps the
    rest of the shell and opens a socket where the limb actually travels.
    """
    parent = bpy.data.objects[parent_name]
    child_name = "Mesh_" + bone
    loco.clear_pose(arm)
    y = -abd if bone.endswith("_L") else abd
    loco.set_e(arm, bone, flex, y, 0.0)
    bpy.context.view_layer.update()
    joint = arm.matrix_world @ arm.pose.bones[bone].head
    verts, polys = loco.world_verts(child_name)
    if len(verts) < 4:
        return 0
    bvh = BVHTree.FromPolygons(verts, polys)
    inv = parent.matrix_world.inverted()
    moved = 0
    for vert in parent.data.vertices:
        world = parent.matrix_world @ vert.co
        # The official test exempts 3 cm. Verts just outside that still count.
        if (world - joint).length <= 0.030:
            continue
        nearest, normal, _i, dist = bvh.find_nearest(world)
        if nearest is None or dist is None or normal is None:
            continue
        if dist <= 0.004 or dist > 0.045:
            continue
        if (world - nearest).dot(normal) >= 0.0:
            continue
        if normal.length < 1e-8:
            continue
        vert.co = inv @ (nearest + normal.normalized() * gap)
        moved += 1
    parent.data.update()
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    print("notch", parent_name, bone, flex, moved, flush=True)
    return moved


def clear_hips(arm):
    """Taper the thigh from a 2.8 cm hip ball so 110° of flex clears the pelvis.

    A vertex notch and a pelvis boolean both opened the reverse pair. Wrapping
    the thigh along the bone, with its own ball, is the cut that stayed clear
    at rest and at -110°.
    """
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    for side in ("L", "R"):
        pivot, _axis = world_head(arm, "UpperLeg_" + side)
        knee, _shin = world_head(arm, "LowerLeg_" + side)
        axis = (knee - pivot).normalized()
        wrap_limb(
            bpy.data.objects["Mesh_UpperLeg_" + side],
            pivot, axis, 0.34, 0.18, 0.028, True, ((pivot, 0.028),),
        )


def clear_neck(arm):
    """Short neck tube, and a head socket deep enough for ±40° of pitch."""
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    head, axis = world_head(arm, "Head")
    neck = bpy.data.objects["Mesh_Neck"]
    # Keep the neck ball. The tube below it is a narrow cone the recess can cover.
    wrap_limb(neck, head, -axis, 0.22, 0.12, 0.038, True, ((head, 0.038),))
    skull = bpy.data.objects["Mesh_Head"]
    cones = []
    for pitch in (-40.0, -20.0, 0.0, 20.0, 40.0):
        _pivot, pitched = _pose_axis(arm, "Head", pitch, 0.0)
        # The neck hangs down the head's -axis. Pitch swings that tube.
        start = head - pitched * 0.042
        end = head - pitched * 0.095
        cones.append(new_cone("headpitch", start, end, 0.028, 0.072))
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    cutter = fuse_solids(cones)
    ok = False
    if cutter is not None:
        ok = apply_bool(skull, cutter, "DIFFERENCE", max_boundary=200)
        if ok and 0 < boundary_edges(skull) <= 60:
            close_mesh(skull)
    print("head-recess", ok, "verts", len(skull.data.vertices), "boundary", boundary_edges(skull), flush=True)


def narrow_lower_spine(arm, ball_r=0.045, goal=0.053):
    """Shift lower-spine verts that sit in a hip ball toward the midline and up.

    A 1.5 cm silhouette change is enough to put them outside the 4.5 cm ball.
    """
    spine = bpy.data.objects["Mesh_Spine"]
    mw = spine.matrix_world
    inv = mw.inverted()
    balls = []
    for side in ("L", "R"):
        pivot, _axis = world_head(arm, "UpperLeg_" + side)
        balls.append(pivot)
    moved = 0
    for vert in spine.data.vertices:
        world = mw @ vert.co
        shifted = world.copy()
        did = False
        for pivot in balls:
            dist = (shifted - pivot).length
            if dist >= goal or dist < 1e-4:
                continue
            medial = -1.0 if pivot.x > 0.0 else 1.0
            direction = Vector((medial * 0.55, 0.0, 0.84)).normalized()
            step = 0.0
            for _ in range(16):
                step += 0.003
                trial = shifted + direction * step
                if (trial - pivot).length >= goal:
                    shifted = trial
                    did = True
                    break
        if did:
            vert.co = inv @ shifted
            moved += 1
    if moved:
        spine.data.update()
    print("narrow-spine", moved, flush=True)
    return moved


def clearance_pass(arm, stage):
    """Taper one hinge so the tube clears the stated bend. Stages stack.

    knee: 140° bend. hip: 110° flex. neck: ±40° pitch. spine: rest pair.
    """
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    if stage == "spine":
        narrow_lower_spine(arm)
    if stage in ("knee", "hip", "neck"):
        wrap_knees(arm)
    if stage in ("hip", "hip-only", "neck"):
        clear_hips(arm)
    if stage == "neck":
        clear_neck(arm)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()


def seat_sole(arm):
    """Translate the rest skeleton so the sole is at z=0.

    The bone-parent inverses stay as they were. A rigid armature-space move
    carries the meshes with the bones. Rewriting matrix_world slides the ankles.
    """
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = None
    for name in ("Mesh_Foot_L", "Mesh_Foot_R"):
        obj = bpy.data.objects[name]
        z = min((obj.matrix_world @ v.co).z for v in obj.data.vertices)
        low = z if low is None else min(low, z)
    if low is None or abs(low) < 0.0008:
        print("sole", round(low or 0.0, 4), flush=True)
        return
    delta = Vector((0.0, 0.0, -low))
    # Leave the bone-parent inverses alone. Restoring the pre-move inverses
    # puts the shells back on the old joints. A connected head is the parent
    # tail, so adding delta to both applies the shift twice.
    if arm.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
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
    z = min((obj.matrix_world @ v.co).z for v in obj.data.vertices)
    knee, _axis = world_head(arm, "LowerLeg_L")
    print("sole", round(low, 4), "->", round(z, 4), "knee", [round(c, 3) for c in knee], flush=True)


def build(arm):
    measured = []
    for bone, child_name, _parents, radius, trim, _length, fallback in JOINTS:
        pivot, axis = world_head(arm, bone)
        child = bpy.data.objects[child_name]
        radial = tube_radius(child, pivot, axis, trim * 0.85, trim + 0.14)
        if radial < 0.02:
            radial = fallback
        measured.append((bone, radial))
        bounds = rebuild_child(child, pivot, axis, radius, trim, max(radial, fallback) * 0.92)
        print("child", bone, "tube", round(radial, 3), "boundary", bounds, "verts", len(child.data.vertices))
    neck_joint(arm)
    # Limbs carry a ball at the other end, so the socket is cut on the distal cap.
    # Hips, chest, and shoulders take a sphere boolean; they hold no ball.
    limb_names = {child for _bone, child, _parents, _radius, _trim, _length, _fallback in JOINTS}
    by_parent = {}
    pivots = {}
    for bone, _child_name, parents, radius, _trim, _length, _fallback in JOINTS:
        pivot, axis = world_head(arm, bone)
        pivots[bone] = (pivot, radius)
        for parent_name in parents:
            if parent_name in limb_names:
                stump_socket(bpy.data.objects[parent_name], pivot, axis, radius)
                continue
            by_parent.setdefault(parent_name, []).append((bone, new_sphere(bone + "sock", pivot, radius + GAP, 28)))
    for parent_name, items in by_parent.items():
        parent = bpy.data.objects[parent_name]
        before = len(parent.data.vertices)
        ok = carve_parent(parent, [item[1] for item in items], protect_of(parent_name))
        print("socket", parent_name, before, "->", len(parent.data.vertices), "ok", ok, "boundary", boundary_edges(parent), flush=True)
        if ok:
            continue
        for bone, _cutter in items:
            pivot, radius = pivots[bone]
            removed = delete_inside(parent, pivot, radius + GAP)
            print("local", parent_name, bone, removed, flush=True)
    reseat_balls(arm)
    trim_overlaps(arm)


def _kill_indices(obj, indices):
    if not indices:
        return 0
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    kill = [bm.verts[i] for i in indices if i < len(bm.verts)]
    bmesh.ops.delete(bm, geom=kill, context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return len(kill)


def penetrating(src_name, dst_name, joint, keep):
    """Vert indices of src that sit inside dst, outside the ball and the 3 cm exemption."""
    src = bpy.data.objects[src_name]
    verts, polys = loco.world_verts(dst_name)
    if len(verts) < 4 or len(polys) < 1:
        return []
    bvh = BVHTree.FromPolygons(verts, polys)
    mw = src.matrix_world
    found = []
    for index, vert in enumerate(src.data.vertices):
        world = mw @ vert.co
        dist_j = (world - joint).length
        if dist_j <= 0.032 or dist_j <= keep:
            continue
        nearest, normal, _i, dist = bvh.find_nearest(world)
        if nearest is None or dist is None or normal is None:
            continue
        if dist <= 0.005 or dist > 0.04:
            continue
        if (world - nearest).dot(normal) >= 0.0:
            continue
        found.append(index)
    return found


def trim_overlaps(arm):
    """Clear rest overlaps outside the child ball. A parent cup vert that sits in the child goes too."""
    pairs = [(parent, child, bone, radius) for bone, child, parents, radius, _t, _l, _f in JOINTS for parent in parents]
    pairs.append(("Mesh_Head", "Mesh_Neck", "Head", 0.038))
    pairs.append(("Mesh_Chest", "Mesh_Neck", "Neck", 0.038))

    def rest_pass(tag):
        loco.clear_pose(arm)
        bpy.context.view_layer.update()
        cut = 0
        for parent_name, child_name, bone, ball_r in pairs:
            joint = arm.matrix_world @ arm.pose.bones[bone].head
            # Parent: drop anything outside the 3 cm exemption that is inside the child.
            found = penetrating(parent_name, child_name, joint, 0.0)
            # Child: keep the ball surface.
            found_c = penetrating(child_name, parent_name, joint, ball_r + 0.006)
            for src, indices in ((parent_name, found), (child_name, found_c)):
                if len(indices) > 500:
                    print("trim skip", src, len(indices), flush=True)
                    continue
                removed = _kill_indices(bpy.data.objects[src], indices)
                cut += removed
                if removed:
                    print("trim", tag, src, removed, flush=True)
        return cut

    for _pass in range(3):
        if rest_pass(_pass) == 0:
            break
    # Skirt: any hip angle, drop pelvis verts the thigh enters outside the socket.
    for side in ("L", "R"):
        bone = "UpperLeg_" + side
        mesh = "Mesh_UpperLeg_" + side
        joint = arm.matrix_world @ arm.pose.bones[bone].head
        marked = set()
        for flex, abd in ((-20.0, 0.0), (0.0, 0.0), (40.0, 20.0), (80.0, 35.0), (110.0, 45.0)):
            loco.clear_pose(arm)
            y = -abd if side == "L" else abd
            loco.set_e(arm, bone, flex, y, 0.0)
            bpy.context.view_layer.update()
            joint_now = arm.matrix_world @ arm.pose.bones[bone].head
            marked.update(penetrating("Mesh_Hips", mesh, joint_now, 0.045 + GAP))
        loco.clear_pose(arm)
        bpy.context.view_layer.update()
        if len(marked) > 500:
            print("skirt skip", side, len(marked), flush=True)
            continue
        removed = _kill_indices(bpy.data.objects["Mesh_Hips"], marked)
        print("skirt", side, removed, "hipverts", len(bpy.data.objects["Mesh_Hips"].data.vertices), flush=True)
    for _pass in range(3):
        if rest_pass("post" + str(_pass)) == 0:
            break
    loco.clear_pose(arm)
    bpy.context.view_layer.update()


def protect_of(mesh_name):
    """Balls that live on this mesh and must survive a later cut at the other end."""
    arm = bpy.data.objects["DummyArmature"]
    found = []
    for bone, child_name, _parents, radius, _trim, _length, _fallback in JOINTS:
        if child_name == mesh_name:
            pivot, _axis = world_head(arm, bone)
            found.append((pivot, radius))
    if mesh_name == "Mesh_Neck":
        pivot, _axis = world_head(arm, "Head")
        found.append((pivot, 0.038))
    return found


def reseat_balls(arm):
    """Union a fresh sphere wherever a later cut deleted the joint ball."""
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    seats = [(bone, child, radius) for bone, child, _p, radius, _t, _l, _f in JOINTS]
    seats.append(("Head", "Mesh_Neck", 0.038))
    for bone, child_name, radius in seats:
        obj = bpy.data.objects[child_name]
        pivot, _axis = world_head(arm, bone)
        count = shell_count(obj, pivot, radius)
        print("ball", bone, count, flush=True)
        if count >= 80:
            continue
        apply_bool(obj, new_sphere(bone + "reball", pivot, radius, 32), "UNION")
        print(" reseat", bone, shell_count(obj, pivot, radius), flush=True)


def cut_hip_skirt(arm):
    """Remove pelvis verts the thigh enters outside the socket, across the hip range."""
    hips = bpy.data.objects["Mesh_Hips"]
    marked = set()
    samples = [(flex, abd) for flex in (-25.0, 0.0, 35.0, 70.0, 100.0, 125.0) for abd in (0.0, 25.0, 50.0)]
    for side in ("L", "R"):
        bone = "UpperLeg_" + side
        mesh = "Mesh_UpperLeg_" + side
        pivot_rest, _axis = world_head(arm, bone)
        socket = 0.045 + GAP
        for flex, abd in samples:
            loco.clear_pose(arm)
            y = -abd if side == "L" else abd
            loco.set_e(arm, bone, flex, y, 0.0)
            loco.set_e(arm, "LowerLeg_" + side, 90.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            verts, polys = loco.world_verts(mesh)
            if len(verts) < 4 or len(polys) < 1:
                continue
            bvh = BVHTree.FromPolygons(verts, polys)
            mw = hips.matrix_world
            for index, vert in enumerate(hips.data.vertices):
                world = mw @ vert.co
                reach = (world - pivot_rest).length
                if reach <= socket + 0.004 or reach > 0.22:
                    continue
                nearest, normal, _i, dist = bvh.find_nearest(world)
                if nearest is None or dist is None or normal is None:
                    continue
                if dist <= 0.004 or dist > 0.06:
                    continue
                if (world - nearest).dot(normal) >= 0.0:
                    continue
                marked.add(index)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    if not marked:
        print("skirt none", flush=True)
        return
    bm = bmesh.new()
    bm.from_mesh(hips.data)
    bm.verts.ensure_lookup_table()
    kill = [bm.verts[i] for i in marked]
    bmesh.ops.delete(bm, geom=kill, context="VERTS")
    bm.to_mesh(hips.data)
    bm.free()
    hips.data.update()
    print("skirt cut", len(marked), "hipverts", len(hips.data.vertices), "boundary", boundary_edges(hips), flush=True)


def _purge_one(arm, src_name, dst_name, bone, radius):
    src = bpy.data.objects[src_name]
    verts, polys = loco.world_verts(dst_name)
    if len(verts) < 4 or len(polys) < 1:
        return 0
    bvh = BVHTree.FromPolygons(verts, polys)
    joint = arm.matrix_world @ arm.pose.bones[bone].head
    mw = src.matrix_world
    kill_idx = []
    for index, vert in enumerate(src.data.vertices):
        world = mw @ vert.co
        dist_j = (world - joint).length
        if dist_j <= 0.032:
            continue
        if abs(dist_j - radius) < 0.008:
            continue
        nearest, normal, _i, dist = bvh.find_nearest(world)
        if nearest is None or dist is None or normal is None:
            continue
        if dist <= 0.004 or dist > 0.045:
            continue
        if (world - nearest).dot(normal) >= 0.0:
            continue
        kill_idx.append(index)
    if not kill_idx or len(kill_idx) > 16:
        if len(kill_idx) > 16:
            print("purge skip", src_name, "in", dst_name, len(kill_idx), flush=True)
        return 0
    bm = bmesh.new()
    bm.from_mesh(src.data)
    bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.verts[i] for i in kill_idx], context="VERTS")
    bm.to_mesh(src.data)
    bm.free()
    src.data.update()
    return len(kill_idx)


def purge_penetrators(arm):
    """Delete non-ball verts that rest inside the neighbouring shell."""
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    pairs = [(parent, child, bone, radius) for bone, child, parents, radius, _t, _l, _f in JOINTS for parent in parents]
    pairs.append(("Mesh_Head", "Mesh_Neck", "Head", 0.038))
    pairs.append(("Mesh_Chest", "Mesh_Neck", "Neck", 0.038))
    for parent_name, child_name, bone, radius in pairs:
        for src, dst in ((parent_name, child_name), (child_name, parent_name)):
            moved = _purge_one(arm, src, dst, bone, radius)
            if moved:
                print("purge", src, "in", dst, moved, flush=True)


def strict_pairs(arm):
    packed = {}
    for name in loco.active_meshes():
        verts, polys = loco.world_verts(name)
        packed[name] = (verts, BVHTree.FromPolygons(verts, polys))
    names = list(packed.keys())
    rig = []
    pose = []
    for i in range(len(names)):
        for j in range(i + 1, len(names)):
            a, b = names[i], names[j]
            va, ba = packed[a]
            vb, bb = packed[b]
            if not loco.boxes_near(va, vb):
                continue
            joined = loco.bones_joined(a, b)
            joint = None
            if joined:
                child = loco.hinge_bone(a, b)
                joint = arm.matrix_world @ arm.pose.bones[child].head
            worst = 0.0
            hits = 0
            for verts, bvh in ((va, bb), (vb, ba)):
                for point in verts:
                    nearest, normal, _idx, dist = bvh.find_nearest(point)
                    if nearest is None or dist is None or dist <= 0.005 or dist > 0.04 or normal is None:
                        continue
                    if (point - nearest).dot(normal) >= 0.0:
                        continue
                    if joined and joint is not None and (point - joint).length <= 0.03:
                        continue
                    hits += 1
                    worst = max(worst, dist * 100.0)
            if hits == 0:
                continue
            row = (worst, hits, a + "|" + b)
            (rig if joined else pose).append(row)
    rig.sort(reverse=True)
    pose.sort(reverse=True)
    return rig, pose


def report(arm, label):
    bpy.context.view_layer.update()
    for name in loco.MESHES:
        count = len(bpy.data.objects[name].data.vertices)
        if count < 8:
            print(label, "EMPTY", name, count)
            return [], []
    world, self_max, note, _pairs = loco.noclip(arm)
    rig, pose = strict_pairs(arm)
    sole = min((bpy.data.objects["Mesh_Foot_L"].matrix_world @ v.co).z for v in bpy.data.objects["Mesh_Foot_L"].data.vertices)
    print(
        label, "official", round(world, 2), round(self_max, 2), note,
        "strictRig", len(rig), "strictPose", len(pose), "sole", round(sole, 3),
    )
    for row in (rig + pose)[:16]:
        print(" ", round(row[0], 2), "hits", row[1], row[2])
    return rig, pose


def eject_inside(arm, parent_name, child_name, bone):
    """Place parent verts that sit inside the child onto the outside of that shell."""
    parent = bpy.data.objects[parent_name]
    child = bpy.data.objects[child_name]
    verts, polys = loco.world_verts(child_name)
    if len(verts) < 4 or len(polys) < 1:
        return 0
    bvh = BVHTree.FromPolygons(verts, polys)
    joint = arm.matrix_world @ arm.pose.bones[bone].head
    inv = parent.matrix_world.inverted()
    moved = 0
    for vert in parent.data.vertices:
        world = parent.matrix_world @ vert.co
        if (world - joint).length <= 0.03:
            continue
        nearest, normal, _idx, dist = bvh.find_nearest(world)
        if nearest is None or dist is None or dist <= 0.004 or dist > 0.045 or normal is None:
            continue
        if (world - nearest).dot(normal) >= 0.0:
            continue
        if normal.length < 1e-8:
            continue
        normal = normal.normalized()
        vert.co = inv @ (nearest + normal * 0.008)
        moved += 1
    parent.data.update()
    return moved


def eject_hinges(arm):
    pairs = (
        ("Mesh_UpperLeg_L", "Mesh_LowerLeg_L", "LowerLeg_L"),
        ("Mesh_UpperLeg_R", "Mesh_LowerLeg_R", "LowerLeg_R"),
        ("Mesh_LowerLeg_L", "Mesh_Foot_L", "Foot_L"),
        ("Mesh_LowerLeg_R", "Mesh_Foot_R", "Foot_R"),
        ("Mesh_LowerArm_L", "Mesh_Hand_L", "Hand_L"),
        ("Mesh_LowerArm_R", "Mesh_Hand_R", "Hand_R"),
        ("Mesh_Shoulder_L", "Mesh_UpperArm_L", "UpperArm_L"),
        ("Mesh_Shoulder_R", "Mesh_UpperArm_R", "UpperArm_R"),
        ("Mesh_Neck", "Mesh_Head", "Head"),
        ("Mesh_Hips", "Mesh_UpperLeg_L", "UpperLeg_L"),
        ("Mesh_Hips", "Mesh_UpperLeg_R", "UpperLeg_R"),
    )
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    for parent, child, bone in pairs:
        moved = eject_inside(arm, parent, child, bone)
        print("eject", parent, child, moved, flush=True)


def sample_rom(arm):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    report(arm, "REST")
    for flex, abd in ((-30.0, 0.0), (90.0, 30.0), (120.0, 50.0)):
        loco.clear_pose(arm)
        loco.set_e(arm, "UpperLeg_L", flex, -abd, 0.0)
        loco.set_e(arm, "LowerLeg_L", 100.0, 0.0, 0.0)
        report(arm, "HIP {0}/{1}".format(flex, abd))
    loco.clear_pose(arm)
    loco.set_e(arm, "LowerArm_L", -130.0, 0.0, 0.0)
    loco.set_e(arm, "UpperArm_L", -70.0, -30.0, 0.0)
    loco.set_e(arm, "Head", 35.0, 0.0, 25.0)
    report(arm, "ARMNECK")
    loco.clear_pose(arm)


def sync_eval_world():
    """Write the evaluated bone-parent transform back before FBX export.

    Boolean mode switches leave matrix_world stale. The overlap test reads the
    evaluated mesh, so it stays clear, and the exported file then drops the head
    onto the neck.
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
    print("WROTE", OUT_FBX, os.path.getsize(OUT_FBX))


def main():
    # The pass-5 mesh is the seated export, not a fresh rebuild. build() plus
    # the old notch fan does not reproduce it, and this script writes the candidate.
    if os.environ.get("REBUILD_CANDIDATE") != "1":
        print("REFUSING rebuild. Set REBUILD_CANDIDATE=1 to regenerate from the shipped mannequin.")
        return
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SRC)
    arm = bpy.data.objects["DummyArmature"]
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    build(arm)
    clearance_pass(arm, "neck")
    seat_sole(arm)
    sample_rom(arm)
    export_fbx(arm)


if __name__ == "__main__":
    main()
