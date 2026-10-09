"""Proposed Hier mannequin with hip, thigh, elbow, and knee clearance.

This does not replace Dummy_Mannequin_Tan_Hier_Hi.fbx. It writes a separate
candidate and before/after stills. The game does not reference the candidate.
"""
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_loco_stills as loco

ROOT = loco.ROOT
SRC = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT_FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Candidate", "Dummy_Mannequin_Tan_Hier_Clearance.fbx")
STILLS = os.path.join(ROOT, "Docs", "LocoStills", "pass3", "rig")

# Joined neighbours may overlap inside this radius. Anything deeper past it is pushed out.
EXEMPT = 0.03
# Leave the surface this far clear so the 0.5 cm test has margin.
MARGIN = 0.007
MAX_MOVE = 0.08
ORIGIN = {}

CAP_BONES = {
    "Mesh_ElbowCap_L": "LowerArm_L",
    "Mesh_ElbowCap_R": "LowerArm_R",
    "Mesh_KneeCap_L": "LowerLeg_L",
    "Mesh_KneeCap_R": "LowerLeg_R",
}
# A cap has to count as joined to both sides of the hinge or the 3 cm exemption misses one of them.
CAP_JOIN = {
    "ElbowCap_L": ("UpperArm_L", "LowerArm_L"),
    "ElbowCap_R": ("UpperArm_R", "LowerArm_R"),
    "KneeCap_L": ("UpperLeg_L", "LowerLeg_L"),
    "KneeCap_R": ("UpperLeg_R", "LowerLeg_R"),
}


def body_meshes():
    names = []
    for name in loco.MESHES:
        if name in bpy.data.objects:
            names.append(name)
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name in CAP_BONES and obj.name not in names:
            names.append(obj.name)
    return names


def bone_of(mesh_name):
    if mesh_name in CAP_BONES:
        return CAP_BONES[mesh_name]
    return loco.BONE_OF[mesh_name]


def joined(a, b):
    if loco.PARENT.get(a) == b or loco.PARENT.get(b) == a:
        return True
    if b in CAP_JOIN.get(a, ()) or a in CAP_JOIN.get(b, ()):
        return True
    return False


def joint_for(arm, mesh_a, mesh_b):
    bone_a = bone_of(mesh_a)
    bone_b = bone_of(mesh_b)
    # The hinge sits on the child bone head. Caps use that same head.
    if loco.PARENT.get(bone_b) == bone_a:
        child = bone_b
    elif loco.PARENT.get(bone_a) == bone_b:
        child = bone_a
    elif bone_a in CAP_BONES.values() or mesh_a in CAP_BONES:
        child = bone_of(mesh_a)
    else:
        child = bone_of(mesh_b)
    return arm.matrix_world @ arm.pose.bones[child].head


def world_co(obj):
    mw = obj.matrix_world
    return [mw @ v.co for v in obj.data.vertices], mw.inverted()


def hit(point, bvh):
    nearest, normal, _i, dist = bvh.find_nearest(point)
    if nearest is None or normal is None or dist is None:
        return None
    if dist <= 0.005 or dist > 0.04 or (point - nearest).dot(normal) >= 0.0:
        return None
    return nearest, normal.normalized(), dist


def relax_once(arm):
    """Move one mesh at a time so the two sides of a pair cannot swap through each other."""
    names = [n for n in loco.MESHES if n in bpy.data.objects]
    packed = {}
    for name in names:
        world, _inv = world_co(bpy.data.objects[name])
        polys = [tuple(p.vertices) for p in bpy.data.objects[name].data.polygons]
        packed[name] = (world, BVHTree.FromPolygons(world, polys))
    count = 0
    for src in names:
        obj = bpy.data.objects[src]
        _world, inv = world_co(obj)
        edits = {}
        near = [
            dst for dst in names
            if dst != src and loco.boxes_near(packed[src][0], packed[dst][0])
        ]
        for index, point in enumerate(packed[src][0]):
            best = None
            for dst in near:
                found = hit(point, packed[dst][1])
                if found is None:
                    continue
                if joined(bone_of(src), bone_of(dst)):
                    joint = joint_for(arm, src, dst)
                    if (point - joint).length <= EXEMPT:
                        continue
                _nearest, _normal, dist = found
                if best is None or dist > best[0]:
                    best = (dist, found, dst)
            if best is None:
                continue
            _dist, (nearest, normal, _d), _dst = best
            desired = nearest + normal * MARGIN
            delta = desired - point
            if delta.length < 1e-5:
                continue
            if delta.length > MAX_MOVE:
                desired = point + delta.normalized() * MAX_MOVE
            local = inv @ desired
            base = ORIGIN[src][index]
            swung = local - base
            if swung.length > MAX_MOVE:
                local = base + swung.normalized() * MAX_MOVE
            if (local - obj.data.vertices[index].co).length < 1e-4:
                continue
            edits[index] = local
        for index, local in edits.items():
            obj.data.vertices[index].co = local
            count += 1
        if edits:
            obj.data.update()
            world, _inv = world_co(obj)
            polys = [tuple(p.vertices) for p in obj.data.polygons]
            packed[src] = (world, BVHTree.FromPolygons(world, polys))
    return count


def apply_pose(arm, spec):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, spec.get("z", 0.0))
    for name, eul in spec.get("bones", {}).items():
        loco.set_e(arm, name, eul[0], eul[1], eul[2])
    bpy.context.view_layer.update()


def poses():
    stand_arms = {
        "UpperArm_L": (0.0, -28.0, 0.0),
        "LowerArm_L": (-90.0, 0.0, 0.0),
        "UpperArm_R": (0.0, 28.0, 0.0),
        "LowerArm_R": (-90.0, 0.0, 0.0),
        "UpperLeg_L": (4.0, -8.0, 0.0),
        "LowerLeg_L": (18.0, 0.0, 0.0),
        "Foot_L": (-18.0, 0.0, 0.0),
        "UpperLeg_R": (4.0, 8.0, 0.0),
        "LowerLeg_R": (18.0, 0.0, 0.0),
        "Foot_R": (-18.0, 0.0, 0.0),
    }
    sprint = {
        "Hips": (18.0, 0.0, 0.0),
        "Spine": (4.0, 0.0, -4.0),
        "Chest": (2.0, 0.0, -6.0),
        "Head": (-10.0, 0.0, 0.0),
        "UpperLeg_L": (-85.0, -14.0, 0.0),
        "LowerLeg_L": (110.0, 0.0, 0.0),
        "Foot_L": (-8.0, 0.0, 0.0),
        "UpperLeg_R": (16.0, 14.0, 0.0),
        "LowerLeg_R": (124.0, 0.0, 0.0),
        "Foot_R": (6.0, 0.0, 0.0),
        "UpperArm_L": (-80.0, -28.0, 0.0),
        "LowerArm_L": (-90.0, 0.0, 0.0),
        "UpperArm_R": (58.0, 28.0, 0.0),
        "LowerArm_R": (-90.0, 0.0, 0.0),
    }
    crouch = {
        "Hips": (8.0, 0.0, 0.0),
        "Spine": (6.0, 0.0, 0.0),
        "UpperLeg_L": (-36.0, -8.0, 0.0),
        "LowerLeg_L": (78.0, 0.0, 0.0),
        "Foot_L": (-12.0, 0.0, 0.0),
        "UpperLeg_R": (-36.0, 8.0, 0.0),
        "LowerLeg_R": (78.0, 0.0, 0.0),
        "Foot_R": (-12.0, 0.0, 0.0),
        "UpperArm_L": (8.0, -20.0, 0.0),
        "LowerArm_L": (-40.0, 0.0, 0.0),
        "UpperArm_R": (8.0, 20.0, 0.0),
        "LowerArm_R": (-40.0, 0.0, 0.0),
    }
    return [
        ("rest", {"z": 0.0, "bones": {}}),
        ("arms", {"z": -0.02, "bones": stand_arms}),
        ("sprint", {"z": -0.04, "bones": sprint}),
        ("crouch", {"z": -0.08, "bones": crouch}),
    ]


def add_caps(arm):
    mat = bpy.data.materials.get("Joint_Tan")
    if mat is None:
        mat = bpy.data.materials.new("Joint_Tan")
    for mesh_name, bone_name in CAP_BONES.items():
        if mesh_name in bpy.data.objects:
            continue
        head = arm.matrix_world @ arm.pose.bones[bone_name].head
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.022, location=head)
        cap = bpy.context.active_object
        cap.name = mesh_name
        cap.data.materials.append(mat)
        bpy.context.view_layer.update()
        world = cap.matrix_world.copy()
        cap.parent = arm
        cap.parent_type = "BONE"
        cap.parent_bone = bone_name
        bpy.context.view_layer.update()
        cap.matrix_world = world
    bpy.context.view_layer.update()


def overlap_points(arm, limit=700):
    names = body_meshes()
    packed = {}
    for name in names:
        world, _inv = world_co(bpy.data.objects[name])
        polys = [tuple(p.vertices) for p in bpy.data.objects[name].data.polygons]
        packed[name] = (world, BVHTree.FromPolygons(world, polys))
    points = []
    for i, a in enumerate(names):
        for b in names[i + 1:]:
            world_a, bvh_a = packed[a]
            world_b, bvh_b = packed[b]
            if not loco.boxes_near(world_a, world_b):
                continue
            pair_joined = joined(bone_of(a), bone_of(b))
            joint = joint_for(arm, a, b) if pair_joined else None
            for src, bvh in ((a, bvh_b), (b, bvh_a)):
                for point in packed[src][0]:
                    found = hit(point, bvh)
                    if found is None:
                        continue
                    if joint is not None and (point - joint).length <= EXEMPT:
                        continue
                    points.append(point)
    print("OVERLAP count", len(points))
    if len(points) > limit:
        points = points[:limit]
    return points


def mark_overlaps(arm):
    old = bpy.data.objects.get("OverlapMarks")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    points = overlap_points(arm)
    mesh = bpy.data.meshes.new("OverlapMarksMesh")
    obj = bpy.data.objects.new("OverlapMarks", mesh)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()
    for point in points:
        bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.012, matrix=Matrix.Translation(point))
    bm.to_mesh(mesh)
    bm.free()
    mat = bpy.data.materials.get("OverlapRed")
    if mat is None:
        mat = bpy.data.materials.new("OverlapRed")
        mat.use_nodes = True
        node = mat.node_tree.nodes.get("Principled BSDF")
        if node:
            node.inputs["Base Color"].default_value = (0.85, 0.05, 0.04, 1.0)
            if "Emission Color" in node.inputs:
                node.inputs["Emission Color"].default_value = (0.9, 0.02, 0.02, 1.0)
                node.inputs["Emission Strength"].default_value = 4.0
            elif "Emission" in node.inputs:
                node.inputs["Emission"].default_value = (0.9, 0.02, 0.02, 1.0)
    obj.data.materials.append(mat)
    print("OVERLAP dots", len(points))
    return len(points)


def hide_marks(hide):
    obj = bpy.data.objects.get("OverlapMarks")
    if obj is not None:
        obj.hide_render = hide


def place_front(cam, arm):
    cam.data.type = "PERSP"
    cam.data.lens = 48
    origin = arm.matrix_world @ Vector((0.0, 0.0, 0.96))
    eye = origin + Vector((0.0, -2.55, 0.04))
    look = origin + Vector((0.0, 0.0, -0.06))
    cam.location = eye
    cam.rotation_euler = (look - eye).to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()


def save_still(scene, filename):
    from PIL import Image
    os.makedirs(STILLS, exist_ok=True)
    path = os.path.join(STILLS, filename)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    if os.path.getsize(path) > 400 * 1024:
        img = Image.open(path)
        colors = 128
        while colors >= 32:
            img.quantize(colors=colors, method=Image.Quantize.MEDIANCUT).save(path, "PNG", optimize=True)
            if os.path.getsize(path) <= 400 * 1024:
                break
            colors -= 32
    print("STILL", filename, os.path.getsize(path))


def shoot(scene, cam, arm, tag):
    hide_marks(True)
    apply_pose(arm, {"z": 0.0, "bones": {}})
    place_front(cam, arm)
    save_still(scene, tag + "-front.png")
    loco.place_camera(cam, arm)
    save_still(scene, tag + "-threeq.png")
    hide_marks(False)
    mark_overlaps(arm)
    place_front(cam, arm)
    save_still(scene, tag + "-overlap-front.png")
    loco.place_camera(cam, arm)
    save_still(scene, tag + "-overlap-threeq.png")
    apply_pose(arm, poses()[2][1])
    mark_overlaps(arm)
    loco.place_camera(cam, arm)
    save_still(scene, tag + "-sprint-overlap-threeq.png")
    place_front(cam, arm)
    save_still(scene, tag + "-sprint-overlap-front.png")
    loco.place_camera_side(cam, arm)
    save_still(scene, tag + "-sprint-side.png")
    hide_marks(True)


def separate_rest():
    """Open a gap at each overlap by moving both shells half the penetration.

    A one-sided push onto the other surface makes that shell swallow the
    neighbour. Half steps on the bind pose drop the official self depth to 0
    inside this Blender session, with about 4 cm of vertex travel. That 0 is
    the 4-hit rule: a few neck verts are still inside the head. Writing the
    FBX and reading it back puts those verts over the line again. Pushing the
    same shells on a swung thigh opens the bind hip pair back up, so this pass
    does not chase the clip poses.
    """
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.update()
    joints = {bone.name: arm.matrix_world @ bone.head for bone in arm.pose.bones}
    packed = {}
    for name in loco.MESHES:
        obj = bpy.data.objects[name]
        mw = obj.matrix_world.copy()
        verts = [mw @ v.co for v in obj.data.vertices]
        polys = [tuple(p.vertices) for p in obj.data.polygons]
        packed[name] = (verts, BVHTree.FromPolygons(verts, polys), mw)
    edits = {}
    names = list(packed)
    for i in range(len(names)):
        for j in range(i + 1, len(names)):
            a, b = names[i], names[j]
            if not loco.boxes_near(packed[a][0], packed[b][0]):
                continue
            joined_pair = loco.bones_joined(a, b)
            hinge = loco.hinge_bone(a, b) if joined_pair else None
            for src, dst in ((a, b), (b, a)):
                found = []
                bvh = packed[dst][1]
                mw = packed[src][2]
                joint = joints[hinge] if hinge else None
                for index, point in enumerate(packed[src][0]):
                    if joint is not None and (point - joint).length <= EXEMPT:
                        continue
                    nearest, normal, _idx, dist = bvh.find_nearest(point)
                    if nearest is None or normal is None or dist is None:
                        continue
                    if dist <= 0.005 or dist > 0.04 or (point - nearest).dot(normal) >= 0.0:
                        continue
                    found.append((index, point, normal.normalized(), dist))
                if len(found) < 4:
                    continue
                bucket = edits.setdefault(src, {})
                for index, point, normal, dist in found:
                    step = min(0.012, dist * 0.5 + 0.002)
                    local = mw.inverted() @ (point + normal * step)
                    bucket.setdefault(index, local)
    count = 0
    for name, bucket in edits.items():
        obj = bpy.data.objects[name]
        for index, local in bucket.items():
            obj.data.vertices[index].co = local
            count += 1
        obj.data.update()
    return count


def solve_clearance(arm):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    original = {name: [v.co.copy() for v in bpy.data.objects[name].data.vertices] for name in loco.MESHES}
    rest_mw = {name: bpy.data.objects[name].matrix_world.copy() for name in loco.MESHES}
    for i in range(12):
        moved = separate_rest()
        world, self_max, note, _pairs = loco.noclip(arm)
        print("REST round", i, "pushed", moved, "world", round(world, 2), "self", round(self_max, 2), note, flush=True)
        if moved == 0 or self_max <= 0.5:
            break
    worst = 0.0
    for name in loco.MESHES:
        mw = rest_mw[name]
        for index, vert in enumerate(bpy.data.objects[name].data.vertices):
            worst = max(worst, (mw @ vert.co - mw @ original[name][index]).length)
    print("MAX_MOVE_CM", round(worst * 100.0, 2))


def export_fbx(arm):
    os.makedirs(os.path.dirname(OUT_FBX), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for obj in bpy.data.objects:
        if obj.type == "MESH" and (obj.name in loco.MESHES or obj.name in CAP_BONES):
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
    os.makedirs(STILLS, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SRC)
    arm = bpy.data.objects["DummyArmature"]
    loco.tint()
    loco.CAP_BONES = CAP_BONES
    loco.CAP_JOIN = CAP_JOIN
    scene, cam = loco.scene_setup()
    scene.render.resolution_x = 720
    scene.render.resolution_y = 960
    print("PARENT sample", bpy.data.objects["Mesh_UpperLeg_L"].parent_type, bpy.data.objects["Mesh_UpperLeg_L"].parent_bone)
    if os.environ.get("LOCO_SKIP_BEFORE") != "1":
        shoot(scene, cam, arm, "before")
    solve_clearance(arm)
    add_caps(arm)
    apply_pose(arm, {"z": 0.0, "bones": {}})
    for mesh_name, bone_name in CAP_BONES.items():
        cap = bpy.data.objects[mesh_name]
        head = arm.matrix_world @ arm.pose.bones[bone_name].head
        gap = (cap.matrix_world.translation - head).length
        print("CAP", mesh_name, "parent", cap.parent_type, cap.parent_bone, "gap", round(gap, 4))
    world, self_max, note, _pairs = loco.noclip(arm)
    print("REST with caps", round(world, 2), round(self_max, 2), note)
    if os.environ.get("LOCO_SKIP_AFTER") != "1":
        shoot(scene, cam, arm, "after")
    apply_pose(arm, {"z": 0.0, "bones": {}})
    export_fbx(arm)


if __name__ == "__main__":
    main()


