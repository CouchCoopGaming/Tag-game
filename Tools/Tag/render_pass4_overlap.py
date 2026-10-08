"""Before/after red-overlap stills for the hip, neck, and knee at extreme flex.

Before is the shipped Hier mannequin. After is the unbound clearance candidate.
"""
import os
import sys

import bpy
import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_loco_stills as loco

ROOT = loco.ROOT
SHIP = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
CAND = os.path.join(
    ROOT, "Assets", "Art", "Characters", "HiPoly", "Candidate", "Dummy_Mannequin_Tan_Hier_Clearance.fbx"
)
OUT = os.path.join(ROOT, "Docs", "LocoStills", "pass5")


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arm = bpy.data.objects["DummyArmature"]
    loco.tint()
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    bpy.context.view_layer.update()
    return arm


def pose(arm, kind):
    loco.clear_pose(arm)
    loco.set_root(arm, 0.0, 0.0)
    if kind == "hip":
        # Negative thigh X is hip flexion. 110° is the clearance target.
        loco.set_e(arm, "UpperLeg_L", -110.0, 0.0, 0.0)
        loco.set_e(arm, "LowerLeg_L", 40.0, 0.0, 0.0)
        focus = "UpperLeg_L"
    elif kind == "knee":
        loco.set_e(arm, "UpperLeg_L", -15.0, 0.0, 0.0)
        loco.set_e(arm, "LowerLeg_L", 140.0, 0.0, 0.0)
        focus = "LowerLeg_L"
    else:
        loco.set_e(arm, "Head", 40.0, 0.0, 0.0)
        focus = "Head"
    bpy.context.view_layer.update()
    return arm.matrix_world @ arm.pose.bones[focus].head


def mark(arm, focus):
    old = bpy.data.objects.get("OverlapMarks")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    from mathutils.bvhtree import BVHTree
    packed = {}
    for name in loco.active_meshes():
        verts, polys = loco.world_verts(name)
        packed[name] = (verts, BVHTree.FromPolygons(verts, polys))
    names = list(packed)
    points = []
    for i in range(len(names)):
        for j in range(i + 1, len(names)):
            a, b = names[i], names[j]
            if not loco.boxes_near(packed[a][0], packed[b][0]):
                continue
            joined = loco.bones_joined(a, b)
            joint = None
            if joined:
                child = loco.hinge_bone(a, b)
                joint = arm.matrix_world @ arm.pose.bones[child].head
            for src, dst in ((a, b), (b, a)):
                for point in packed[src][0]:
                    if (point - focus).length > 0.22:
                        continue
                    nearest, normal, _i, dist = packed[dst][1].find_nearest(point)
                    if nearest is None or dist is None or normal is None:
                        continue
                    if dist <= 0.005 or dist > 0.04 or (point - nearest).dot(normal) >= 0.0:
                        continue
                    if joint is not None and (point - joint).length <= 0.03:
                        continue
                    points.append(point)
    mesh = bpy.data.meshes.new("OverlapMarksMesh")
    obj = bpy.data.objects.new("OverlapMarks", mesh)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()
    for point in points[:400]:
        bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.008, matrix=Matrix.Translation(point))
    bm.to_mesh(mesh)
    bm.free()
    mat = bpy.data.materials.new("OverlapRed")
    mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    if node:
        node.inputs["Base Color"].default_value = (0.9, 0.04, 0.03, 1.0)
        if "Emission Strength" in node.inputs:
            node.inputs["Emission Color"].default_value = (1.0, 0.05, 0.04, 1.0)
            node.inputs["Emission Strength"].default_value = 6.0
    obj.data.materials.append(mat)
    print("marks", len(points), flush=True)
    return len(points)


def close_cam(scene, cam, focus, kind):
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 0.42 if kind != "neck" else 0.36
    eye = focus + Vector((0.55, -0.35, 0.08))
    cam.location = eye
    cam.rotation_euler = (focus - eye).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    bpy.context.view_layer.update()


def save(scene, path):
    from PIL import Image
    os.makedirs(os.path.dirname(path), exist_ok=True)
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
    print("STILL", os.path.basename(path), os.path.getsize(path), flush=True)


def shoot(path, tag):
    arm = load(path)
    scene, cam = loco.scene_setup()
    for kind in ("hip", "knee", "neck"):
        focus = pose(arm, kind)
        mark(arm, focus)
        close_cam(scene, cam, focus, kind)
        save(scene, os.path.join(OUT, "overlap-{0}-{1}.png".format(kind, tag)))


def main():
    shoot(SHIP, "before")
    shoot(CAND, "after")


if __name__ == "__main__":
    main()
