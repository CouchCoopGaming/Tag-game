"""Quarter, side, joint close-up, and 1.8 m scale stills for the costume lab.

Renders into /tmp and does not save the blend. The pack script writes
Docs/Characters/pass3/.
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass1 as rp

RAW = os.environ.get("COSTUME_RAW", "/tmp/charlab/pass5")


def log(msg):
    print(msg, flush=True)


def solid(name, color, roughness=0.7):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def add_mesh(name, verts, faces, material):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    rp.link(obj)
    obj.data.materials.append(material)
    return obj


def box(name, center, size, material):
    hx, hy, hz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
    cx, cy, cz = center
    verts = [
        (cx - hx, cy - hy, cz - hz),
        (cx + hx, cy - hy, cz - hz),
        (cx + hx, cy + hy, cz - hz),
        (cx - hx, cy + hy, cz - hz),
        (cx - hx, cy - hy, cz + hz),
        (cx + hx, cy - hy, cz + hz),
        (cx + hx, cy + hy, cz + hz),
        (cx - hx, cy + hy, cz + hz),
    ]
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    return add_mesh(name, verts, faces, material)


def cylinder(name, origin, radius, height, material, sides=16):
    import math

    verts = []
    for ring, z in ((0.0, origin[2]), (1.0, origin[2] + height)):
        for i in range(sides):
            ang = math.tau * i / sides
            verts.append((origin[0] + math.cos(ang) * radius, origin[1] + math.sin(ang) * radius, z))
    # center caps
    bottom = len(verts)
    verts.append((origin[0], origin[1], origin[2]))
    top = len(verts)
    verts.append((origin[0], origin[1], origin[2] + height))
    faces = []
    for i in range(sides):
        j = (i + 1) % sides
        faces.append((i, j, j + sides, i + sides))
        faces.append((bottom, j, i))
        faces.append((top, i + sides, j + sides))
    return add_mesh(name, verts, faces, material)


def knee_world(arm):
    bone = arm.pose.bones["LowerLeg_R"]
    return arm.matrix_world @ bone.head


def shot_close(cam, clones, z_lift):
    chosen = [entry for entry in clones if entry[0]["id"] == "Reed_1_Hood"]
    rp.set_group(clones, False)
    rp.set_group(chosen, True)
    rp.place(chosen, "front", 1.2, z_lift)
    arm = chosen[0][1]
    bpy.context.view_layer.update()
    knee = knee_world(arm)
    log("KNEE %.3f %.3f %.3f" % (knee.x, knee.y, knee.z))
    span = 0.42
    mins = knee + Vector((-span, -span, -span * 0.85))
    maxs = knee + Vector((span, span, span * 0.7))
    rp.frame_ortho(cam, mins, maxs, 1280, 720, "side")
    rp.render_still(cam, os.path.join(RAW, "joint-close.png"), 1280, 720)


def shot_scale(cam, clones, z_lift, props):
    chosen = [entry for entry in clones if entry[0]["id"] == "Reed_1_Hood"]
    rp.set_group(clones, False)
    rp.set_group(chosen, True)
    rp.place(chosen, "front", 1.2, z_lift)
    props[0].location = (0.62, 0.12, 0.0)
    props[1].location = (0.62, 0.12, 0.0)
    for obj in props:
        obj.hide_render = False
        obj.hide_set(False)
    bpy.context.view_layer.update()
    mins, maxs = rp.world_bounds(rp.shown_meshes(chosen) + props)
    # The pole top is the 1.8 m mark. Keep it inside the frame.
    rp.frame_ortho(cam, mins, maxs, 1280, 720, "three")
    body = rp.world_bounds(rp.shown_meshes(chosen))
    height = body[1].z - body[0].z
    log("FIGURE height-m %.3f feet-z %.3f" % (height, body[0].z))
    rp.render_still(cam, os.path.join(RAW, "scale-figure.png"), 1280, 720)
    for obj in props:
        obj.hide_render = True
        obj.hide_set(True)


def main():
    os.makedirs(RAW, exist_ok=True)
    os.environ["COSTUME_ENGINE"] = "BLENDER_EEVEE"
    arm = bpy.data.objects["DummyArmature"]
    arm.rotation_euler = (0.0, 0.0, 0.0)
    arm.location = (0.0, 0.0, 0.0)
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    specs = __import__("json").load(open(rp.LOADOUTS, encoding="utf-8"))["sets"]
    clones = []
    for spec in specs:
        show_arm, meshes = rp.clone_rig(spec["id"])
        rp.apply_loadout(meshes, spec["pieces"], spec.get("hide"))
        rp.paint(meshes, spec["color"], spec["id"])
        clones.append((spec, show_arm, meshes))
    rp.hide_source()
    rp.set_group(clones[:1], True)
    rp.set_group(clones[1:], False)
    rp.place(clones[:1], "front", 1.2, 0.0)
    mins, _maxs = rp.world_bounds(rp.shown_meshes(clones[:1]))
    z_lift = 0.0 - mins.z
    log("Z-LIFT %.4f" % z_lift)
    rp.make_floor()
    rp.make_lights()
    rp.make_world()
    rp.configure_render()
    cam_data = bpy.data.cameras.new("LabCam")
    cam = bpy.data.objects.new("LabCam", cam_data)
    rp.link(cam)
    # 1.8 m staff and a plain bench. Solids only, no texture, no mark.
    wood = solid("ScaleBench", (0.45, 0.32, 0.22), 0.8)
    metal = solid("ScaleStaff", (0.25, 0.27, 0.30), 0.45)
    staff = cylinder("ScaleStaff", (0.0, 0.0, 0.0), 0.025, 1.80, metal)
    ring = cylinder("ScaleMark", (0.0, 0.0, 1.80), 0.055, 0.012, metal)
    bench = box("ScaleBench", (-1.15, 0.05, 0.225), (1.15, 0.42, 0.45), wood)
    props = [staff, ring, bench]
    for obj in props:
        obj.hide_render = True
        obj.hide_set(True)
    # Beside the row, level with Reed, so the staff does not stand in front of Bram.
    staff.location = (-3.90, -0.21, 0.0)
    ring.location = (-3.90, -0.21, 0.0)
    for obj in (staff, ring):
        obj.hide_render = False
        obj.hide_set(False)
    rp.shot_lineup(
        cam, clones, z_lift, "three",
        os.path.join(RAW, "lineup-three-quarter.png"), 1280, 720, 1.75,
        extras=[staff, ring],
    )
    for obj in (staff, ring):
        obj.hide_render = True
        obj.hide_set(True)
    rp.shot_lineup(
        cam, clones, z_lift, "side",
        os.path.join(RAW, "lineup-side.png"), 1280, 720, 1.75,
    )
    shot_close(cam, clones, z_lift)
    shot_scale(cam, clones, z_lift, props)
    # Same Reed loadout as the before still, three-quarter, alone.
    reed = [entry for entry in clones if entry[0]["id"] == "Reed_1_Hood"]
    rp.set_group(clones, False)
    rp.set_group(reed, True)
    rp.place(reed, "three", 1.75, z_lift)
    mins, maxs = rp.world_bounds(rp.shown_meshes(reed))
    rp.frame_ortho(cam, mins, maxs, 1280, 720, "three")
    rp.render_still(cam, os.path.join(RAW, "after-reed.png"), 1280, 720)
    log("QUARTET raw ready")


if __name__ == "__main__":
    main()
