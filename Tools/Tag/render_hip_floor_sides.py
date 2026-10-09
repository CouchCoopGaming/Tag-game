"""Side stills of loaded frames, posed exactly as the scanner poses them.

  KEYS=keys.tsv SHOTS="exit-Vault@0.133,exit-Slide@0.133" TAG=before OUT=dir \
    blender --background --python Tools/Tag/render_hip_floor_sides.py
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import render_evasion_pass4 as p4
import measure_evasion_hipsit as m

frames = g.load_keys(os.environ["KEYS"])
out = os.environ["OUT"]
tag = os.environ["TAG"]
os.makedirs(out, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=g.FBX)
arm = bpy.data.objects["DummyArmature"]
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
g.add_ground()
g.prepare(arm)
m.capture_stand(arm)
sc = bpy.context.scene
sc.render.engine = "CYCLES"  # CPU; the box has no EGL for Workbench/Eevee
sc.cycles.device = "CPU"
sc.cycles.samples = 16
world = bpy.data.worlds.new("w")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[1].default_value = 1.2
sc.world = world
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
sun.rotation_euler = (0.7, 0.2, 0.6)
sc.collection.objects.link(sun)


def mat(name, rgba):
    mt = bpy.data.materials.new(name)
    mt.use_nodes = True
    mt.node_tree.nodes["Principled BSDF"].inputs[0].default_value = rgba
    return mt


BODY = mat("body", (0.55, 0.62, 0.9, 1))
FLOOR = mat("floor", (0.82, 0.82, 0.8, 1))
sc.render.resolution_x = 900
sc.render.resolution_y = 900
for o in bpy.data.objects:
    if o.type == "MESH":
        o.data.materials.clear()
        o.data.materials.append(FLOOR if o.name == "Ground" else BODY)
cam_data = bpy.data.cameras.new("cam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = 2.0
cam = bpy.data.objects.new("cam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
for shot in os.environ["SHOTS"].split(","):
    clip, t = shot.split("@")
    fr = min((f for f in frames if f["clip"] == clip), key=lambda f: abs(f["t"] - float(t)))
    p4.apply_posed(arm, fr)
    face = m.facing_now(arm)
    side = Vector((face.y, -face.x, 0.0))  # 90 deg off the facing: true side view
    hips = arm.matrix_world @ arm.pose.bones["Hips"].head
    cam.location = Vector((hips.x, hips.y, 0.75)) + side * 6.0
    cam.rotation_euler = (-side).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(out, "%s-%s.png" % (clip, tag))
    bpy.ops.render.render(write_still=True)
    print("STILL", sc.render.filepath, flush=True)
