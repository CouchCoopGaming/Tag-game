"""Four couch seats. Two of them are on the existing rope.

Keyboard seat is RMB. The three pad seats are LT. The click window and
the pull speed are the solo numbers. This still does not write a pose key.
"""
import importlib.util
import math
import os

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
spec = importlib.util.spec_from_file_location(
    "pass17", os.path.join(ROOT, "Tools", "Tag", "render_pass17_hier.py")
)
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)

OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass19", "couch-grapple.png")
ROPE = (0.95, 0.85, 0.35, 1.0)
POST = (0.55, 0.56, 0.58, 1.0)

# P1 pull, P2 release, P3 pull, P4 idle. Two ropes.
SEATS = (
    ("P1  RMB  pull", (-2.15, 1.55, 0.0), True),
    ("P2  LT  release", (2.15, 1.55, 0.0), False),
    ("P3  LT  pull", (-2.15, -1.55, 0.0), True),
    ("P4  LT  idle", (2.15, -1.55, 0.0), False),
)


def duplicate(source_names):
    bpy.ops.object.select_all(action="DESELECT")
    for name in source_names:
        obj = bpy.data.objects.get(name)
        if obj is not None:
            obj.select_set(True)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.duplicate()
    copy = None
    for obj in bpy.context.selected_objects:
        if obj.type == "ARMATURE":
            copy = obj
    return copy


def stand(arm):
    p.clear_pose(arm)
    p.torso(arm, 0.0, 0.0, 0.0)
    p.arm_pose(arm, "L", 8.0, 6.0, -12.0, 0.0)
    p.arm_pose(arm, "R", 8.0, 6.0, -12.0, 0.0)


def rope_from(arm, name):
    bpy.context.view_layer.update()
    hand = p.bone_tail(arm, "Hand_R")
    forward = Vector((0.0, -1.0, 0.0))
    anchor = hand + forward * 1.35 + Vector((0.35, 0.0, 0.85))
    p.add_cyl(name + "Rope", hand, anchor, 0.035, ROPE)
    p.add_cyl(name + "Post", anchor, Vector((anchor.x, anchor.y, 0.0)), 0.06, POST)


def label(name, body, loc):
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.text_add(location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.body = body
    obj.data.size = 0.32
    obj.data.align_x = "CENTER"
    obj.data.extrude = 0.02
    obj.rotation_euler = (math.radians(68), 0.0, 0.0)
    mat = p.make_mat(name + "Mat", (0.96, 0.97, 0.98, 1.0), 0.35)
    obj.data.materials.append(mat)
    return obj


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=p.FBX)
    p.tint()
    source = [obj.name for obj in bpy.data.objects]
    arms = [bpy.data.objects["DummyArmature"]]
    for _ in range(3):
        arms.append(duplicate(source))

    for arm, (text, loc, pulling) in zip(arms, SEATS):
        p.ensure_pose(arm)
        if pulling:
            p.pose_grapple(arm)
        else:
            stand(arm)
        arm.location = Vector(loc)
        bpy.context.view_layer.update()
        mark = (1.0, 0.82, 0.05, 1.0) if pulling else (0.25, 0.28, 0.32, 1.0)
        if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        bpy.ops.mesh.primitive_cylinder_add(radius=0.85, depth=0.04, location=(loc[0], loc[1], 0.02))
        pad = bpy.context.active_object
        pad.name = arm.name + "Mark"
        pad.data.materials.append(p.make_mat(pad.name + "Mat", mark, 0.4, 3.0 if pulling else 0.0))
        if pulling:
            rope_from(arm, arm.name)
        label(arm.name + "Label", text, Vector((loc[0], loc[1] - 0.35, 2.15)))
        print("SEAT", text, "at", tuple(round(v, 2) for v in arm.matrix_world.translation), "rope", pulling)

    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")

    bpy.ops.mesh.primitive_plane_add(size=40, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    ground.data.materials.append(p.make_mat("GroundMat", p.GROUND, 0.92))

    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = p.SKY
    bg.inputs["Strength"].default_value = 0.85

    bpy.ops.object.light_add(type="SUN", location=(4.0, -6.0, 8.0))
    sun = bpy.context.active_object
    sun.data.energy = 3.2
    p.look_at(sun, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.data.type = "PERSP"
    cam.data.lens = 32
    cam.location = Vector((0.0, -10.6, 6.4))
    p.look_at(cam, Vector((0.0, 0.0, 0.9)))
    bpy.context.scene.camera = cam

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.film_transparent = False
    scene.eevee.taa_render_samples = 8
    scene.render.filepath = OUT
    bpy.ops.render.render(write_still=True)
    print("WROTE", OUT)


if __name__ == "__main__":
    main()
