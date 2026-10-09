"""Close the clearance candidate's mid-flex hip window.

Swing channels are cut into the closed shipped pelvis and copied onto the
seated candidate. The thigh is rebuilt from the shipped panels, with a ball
at the hip bone head and a cuff inside the channel. The candidate is replaced
only when flexion, abduction, and twist stay under 0.5 cm and rest rigJoint
does not gain a pair.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_hip_ankle as hip
import prove_pass4 as prove
import render_loco_stills as loco

ROOT = loco.ROOT
SHIP = hip.SRC
CAND = os.path.join(
    ROOT, "Assets", "Art", "Characters", "HiPoly", "Candidate", "Dummy_Mannequin_Tan_Hier_Clearance.fbx"
)
SHELLS = "/tmp/loco/hip_shells.fbx"

# Proximal cuff. The knee head is ~0.54 m down the bone, so this stops short of it.
CUFF_D0 = 0.020
CUFF_D1 = 0.40
CUFF_R0 = 0.020
CUFF_R1 = 0.046
CHANNEL_D0 = 0.012
CHANNEL_D1 = 0.44
CHANNEL_R0 = 0.034
CHANNEL_R1 = 0.072


def flex_poses():
    poses = [(flex, 0) for flex in range(0, 121, 15)]
    poses.append((-20, 0))
    for flex in range(0, 121, 15):
        poses.append((flex, 35))
    return poses


def hip_depth(arm, side):
    """Same inside test as directed_fails. Joined pairs keep the 3 cm exemption."""
    from mathutils.bvhtree import BVHTree

    bone = "UpperLeg_" + side
    child = "Mesh_UpperLeg_" + side
    joint = arm.matrix_world @ arm.pose.bones[bone].head
    child_v, child_p = loco.world_verts(child)
    child_bvh = BVHTree.FromPolygons(child_v, child_p)
    worst = 0.0
    worst_name = ""
    for parent in ("Mesh_Hips", "Mesh_Spine", "Mesh_Chest"):
        if parent not in bpy.data.objects:
            continue
        parent_v, parent_p = loco.world_verts(parent)
        parent_bvh = BVHTree.FromPolygons(parent_v, parent_p)
        joined = loco.bones_joined(child, parent)
        hinge = joint if joined else None
        into_parent = loco.side_depth(child_v, parent_bvh, joined, hinge, None)
        into_child = loco.side_depth(parent_v, child_bvh, joined, hinge, None)
        if max(into_parent, into_child) > worst:
            worst = max(into_parent, into_child)
            worst_name = parent + " " + str(round(into_parent, 2)) + "/" + str(round(into_child, 2))
    if worst > 0.0 and os.environ.get("HIP_DEBUG") == "1":
        print("   pair", side, worst_name, flush=True)
    return worst


def carve_shells():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SHIP)
    arm = bpy.data.objects["DummyArmature"]
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    hips = bpy.data.objects["Mesh_Hips"]
    spine = bpy.data.objects["Mesh_Spine"]
    for side in ("L", "R"):
        pivot, _axis = hip.head_axis(arm, "UpperLeg_" + side)
        hips = hip.bool_islands(hips, pivot, hip.HIP_SOCKET)
        spine = hip.bool_islands(spine, pivot, hip.HIP_BALL + 0.008)
        for flex, abd in flex_poses():
            hip.pose_hip(arm, side, flex, abd, 0)
            pivot, axis = hip.head_axis(arm, "UpperLeg_" + side)
            cutter = hip.make_cone(
                pivot + axis * CHANNEL_D0,
                pivot + axis * CHANNEL_D1,
                CHANNEL_R0,
                CHANNEL_R1,
            )
            hips = hip.cut_solid(hips, cutter)
            spine = hip.cut_solid(spine, cutter)
            bpy.data.objects.remove(cutter, do_unlink=True)
            print("carved", side, flex, abd, "hips", len(hips.data.vertices), "b", hip.boundary_of(hips), flush=True)
        loco.clear_pose(arm)
        bpy.context.view_layer.update()
    os.makedirs("/tmp/loco", exist_ok=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for name in ("Mesh_Hips", "Mesh_Spine"):
        bpy.data.objects[name].select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=SHELLS,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        apply_scale_options="FBX_SCALE_NONE",
        path_mode="AUTO",
        axis_forward="-Z",
        axis_up="Y",
    )
    print("SHELLS", os.path.getsize(SHELLS), flush=True)


def copy_shell(src, dst_name):
    dst = bpy.data.objects[dst_name]
    data = src.data.copy()
    old = dst.data
    dst.data = data
    if old.users == 0:
        bpy.data.meshes.remove(old)


def imported_mesh(name, before):
    for obj in bpy.data.objects:
        if obj in before or obj.type != "MESH":
            continue
        if obj.name.split(".")[0] == name:
            return obj
    return None


def nudge_false_faces(src_name, dst_name, step, rounds):
    """Slide pelvis faces that fail the normal test but sit outside the volume.

    The ray test says these spine verts are not inside the pelvis. The nearest
    face is just inside the 4 cm window, so a small slide drops the false hit.
    """
    import bmesh
    from mathutils import Vector

    for _round in range(rounds):
        src_v, _sp = loco.world_verts(src_name)
        dst = bpy.data.objects[dst_name]
        dv, dp = loco.world_verts(dst_name)
        from mathutils.bvhtree import BVHTree
        tree = BVHTree.FromPolygons(dv, dp)
        mw = dst.matrix_world
        inv = mw.inverted()
        bm = bmesh.new()
        bm.from_mesh(dst.data)
        bm.faces.ensure_lookup_table()
        moved = set()
        hits = 0
        for point in src_v:
            nearest, normal, index, dist = tree.find_nearest(point)
            if nearest is None or index is None or dist is None or normal is None:
                continue
            if dist <= 0.005 or dist > 0.04 or (point - nearest).dot(normal) >= 0.0:
                continue
            if loco.inside(tree, point, Vector((1.0, 0.2, 0.05))):
                continue
            hits += 1
            away = nearest - point
            if away.length < 1e-6:
                continue
            away.normalize()
            for vert in bm.faces[index].verts:
                if vert.index in moved:
                    continue
                world = mw @ vert.co
                vert.co = inv @ (world + away * step)
                moved.add(vert.index)
        bm.to_mesh(dst.data)
        bm.free()
        dst.data.update()
        print("nudge", _round, "hits", hits, "verts", len(moved), flush=True)
        if hits < 4:
            break


def consistent_normals(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def fit_candidate():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=CAND)
    arm = bpy.data.objects["DummyArmature"]
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    before = set(bpy.data.objects)
    before_fails = prove.directed_fails(arm, prove.pack(arm))
    print("REST before", len(before_fails), flush=True)
    bpy.ops.import_scene.fbx(filepath=SHELLS)
    src = imported_mesh("Mesh_Hips", before)
    if src is None:
        raise RuntimeError("missing hip shell")
    copy_shell(src, "Mesh_Hips")
    print("copied hips", len(bpy.data.objects["Mesh_Hips"].data.vertices), "b", hip.boundary_of(bpy.data.objects["Mesh_Hips"]), flush=True)
    for obj in list(bpy.data.objects):
        if obj not in before:
            bpy.data.objects.remove(obj, do_unlink=True)
    shipped = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=SHIP)
    for side in ("L", "R"):
        src = imported_mesh("Mesh_UpperLeg_" + side, shipped)
        if src is None:
            raise RuntimeError("missing shipped thigh " + side)
        copy_shell(src, "Mesh_UpperLeg_" + side)
        print("copied thigh", side, len(bpy.data.objects["Mesh_UpperLeg_" + side].data.vertices), flush=True)
    for obj in list(bpy.data.objects):
        if obj not in shipped:
            bpy.data.objects.remove(obj, do_unlink=True)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    consistent_normals(bpy.data.objects["Mesh_Hips"])
    for side in ("L", "R"):
        thigh = bpy.data.objects["Mesh_UpperLeg_" + side]
        pivot, axis = hip.head_axis(arm, "UpperLeg_" + side)
        hip.subdivide(thigh, 2)
        print("snap", side, hip.snap_ball(thigh, pivot, hip.HIP_BALL * 0.92, 0.12, ()), flush=True)
        print("cuff", side, hip.cone_limit(thigh, pivot, axis, CUFF_D0, CUFF_D1, CUFF_R0, CUFF_R1, ()), flush=True)
        knee, shin_axis = hip.head_axis(arm, "LowerLeg_" + side)
        toward = (pivot - knee).normalized()
        hip.cone_limit(thigh, knee, toward, 0.025, 0.18, 0.014, hip.KNEE_SLOPE * 0.18, ())
        hip.add_sphere(thigh, pivot, hip.HIP_BALL)
        consistent_normals(thigh)
        hip.shade(thigh)
    nudge_false_faces("Mesh_Spine", "Mesh_Hips", 0.012, 12)
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    for _round in range(6):
        moved = 0
        for side in ("L", "R"):
            knee, _shin_axis = hip.head_axis(arm, "LowerLeg_" + side)
            moved += hip.push_out(bpy.data.objects["Mesh_LowerLeg_" + side], "Mesh_UpperLeg_" + side, knee)
            moved += hip.push_out(bpy.data.objects["Mesh_UpperLeg_" + side], "Mesh_LowerLeg_" + side, knee)
        print("knee push", _round, moved, flush=True)
        if moved == 0:
            break
    loco.clear_pose(arm)
    bpy.context.view_layer.update()
    after = prove.directed_fails(arm, prove.pack(arm))
    print("REST after", len(after), flush=True)
    for key in sorted(after):
        if key not in before_fails:
            print(" new", key, round(after[key], 2), flush=True)
    for key in sorted(before_fails):
        if key not in after:
            print(" gone", key, flush=True)
    ranges = {}
    clear = True
    for side in ("L", "R"):
        for flex in list(range(0, 121, 5)) + [-20]:
            hip.pose_hip(arm, side, flex, 0, 0)
            depth = hip_depth(arm, side)
            if depth > 0.0:
                clear = False
                print(" flex", side, flex, round(depth, 2), flush=True)
        for abd in (0, 15, 25, 35):
            hip.pose_hip(arm, side, 60, abd, 0)
            depth = hip_depth(arm, side)
            if depth > 0.0:
                clear = False
                print(" abd", side, abd, round(depth, 2), flush=True)
        for rot in (-25, 25):
            hip.pose_hip(arm, side, 60, 0, rot)
            depth = hip_depth(arm, side)
            if depth > 0.0:
                clear = False
                print(" rot", side, rot, round(depth, 2), flush=True)
        ranges[side] = clear
    new_pairs = [key for key in after if key not in before_fails]
    print("FLEX_CLEAR", clear, "rigJoint", len(after), "new", len(new_pairs), flush=True)
    hip.OUT_FBX = "/tmp/loco/hip_attempt.fbx"
    hip.export_fbx(arm)
    if not clear or new_pairs:
        print("KEEP candidate unchanged", flush=True)
        return
    hip.OUT_FBX = CAND
    hip.export_fbx(arm)


if __name__ == "__main__":
    if os.environ.get("SKIP_CARVE") == "1" and os.path.isfile(SHELLS):
        fit_candidate()
    else:
        carve_shells()
        fit_candidate()
