#!/usr/bin/env python3
"""Offline Blender stills of the owned verb FX on the Hier mannequin.

Run: blender --background --python Tools/RenderPass6.py
"""
import math
import os

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
ART = os.path.join(ROOT, "Assets", "Resources", "FX")
DIZZY = os.path.join(ROOT, "Assets", "Art", "FX", "ComicDizzy.png")
OUT = os.path.join(ROOT, "Docs", "AnimStills", "pass6")


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def world():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.film_transparent = False
    scene.eevee.taa_render_samples = 24
    scene.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("ParkSky")
    scene.world = w
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.62, 0.78, 0.92, 1)
    bg.inputs[1].default_value = 0.9
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(48), 0.15, math.radians(30))
    sun.data.energy = 3.2
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA"))
    bpy.context.collection.objects.link(fill)
    fill.location = (-3.2, -2.4, 2.6)
    fill.data.energy = 80
    fill.data.size = 3


def principled(name, color, rough=0.85):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = rough
    return mat


def ground():
    bpy.ops.mesh.primitive_plane_add(size=24, location=(0, 0, 0))
    g = bpy.context.active_object
    g.name = "Lawn"
    g.data.materials.append(principled("Lawn", (0.28, 0.46, 0.18, 1), 0.92))
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0.012))
    pad = bpy.context.active_object
    pad.name = "Pad"
    pad.scale = (3.2, 2.2, 1)
    pad.data.materials.append(principled("Concrete", (0.62, 0.61, 0.58, 1), 0.7))
    bpy.ops.mesh.primitive_cube_add(size=1, location=(2.4, 1.6, 0.7))
    wall = bpy.context.active_object
    wall.name = "Wall"
    wall.scale = (0.18, 1.6, 1.4)
    wall.data.materials.append(principled("Wall", (0.45, 0.62, 0.72, 1), 0.4))
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-3.2, 2.2, 0.35))
    box = bpy.context.active_object
    box.scale = (0.7, 0.5, 0.35)
    box.data.materials.append(principled("Crate", (0.55, 0.36, 0.18, 1), 0.8))


def import_runner():
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    arm.location.z = 0.04
    return arm


def bend(arm, name, x=0.0, y=0.0, z=0.0):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))


def pose_land(arm, kind):
    if kind == "light":
        bend(arm, "UpperLeg_L", 12, 0, 4)
        bend(arm, "UpperLeg_R", 8, 0, -4)
        bend(arm, "LowerLeg_L", -16)
        bend(arm, "LowerLeg_R", -10)
        bend(arm, "Spine", 6)
    elif kind == "medium":
        bend(arm, "UpperLeg_L", 32, 0, 6)
        bend(arm, "UpperLeg_R", 28, 0, -6)
        bend(arm, "LowerLeg_L", -40)
        bend(arm, "LowerLeg_R", -34)
        bend(arm, "Spine", 16)
        bend(arm, "Head", 8)
        bend(arm, "UpperArm_L", 20, 0, -16)
        bend(arm, "UpperArm_R", 18, 0, 16)
    else:
        bend(arm, "UpperLeg_L", 58, 0, 8)
        bend(arm, "UpperLeg_R", 54, 0, -8)
        bend(arm, "LowerLeg_L", -70)
        bend(arm, "LowerLeg_R", -64)
        bend(arm, "Spine", 28)
        bend(arm, "Head", 16)
        bend(arm, "UpperArm_L", 50, 0, -20)
        bend(arm, "UpperArm_R", 48, 0, 20)
        bend(arm, "LowerArm_L", -30)
        bend(arm, "LowerArm_R", -28)


def pose_roll(arm):
    bend(arm, "Root", 62, 12, -18)
    bend(arm, "Hips", 8)
    bend(arm, "Spine", 16)
    bend(arm, "Head", 20)
    bend(arm, "UpperArm_L", 64, 0, -28)
    bend(arm, "UpperArm_R", 36, 0, 22)
    bend(arm, "LowerArm_L", -24)
    bend(arm, "UpperLeg_L", 36)
    bend(arm, "UpperLeg_R", -16)
    bend(arm, "LowerLeg_L", -42)
    bend(arm, "LowerLeg_R", -24)


def pose_stagger(arm):
    bend(arm, "Spine", 22, 8, 0)
    bend(arm, "Chest", 10)
    bend(arm, "Head", 18, -12, 0)
    bend(arm, "UpperArm_L", -30, 0, -40)
    bend(arm, "UpperArm_R", -20, 0, 36)
    bend(arm, "LowerArm_L", -24)
    bend(arm, "LowerArm_R", -18)
    bend(arm, "UpperLeg_L", 10)
    bend(arm, "UpperLeg_R", 6)


def pose_run(arm):
    bend(arm, "UpperLeg_L", 28)
    bend(arm, "UpperLeg_R", -22)
    bend(arm, "LowerLeg_L", -18)
    bend(arm, "LowerLeg_R", -36)
    bend(arm, "UpperArm_L", -24, 0, -12)
    bend(arm, "UpperArm_R", 20, 0, 12)
    bend(arm, "Spine", 8)


def card(image_path, loc, scale, uv, emit=1.4, tint=(1, 1, 1, 1)):
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc)
    ob = bpy.context.active_object
    ob.scale = scale
    me = ob.data
    u0, v0, u1, v1 = uv
    me.uv_layers[0].data[0].uv = (u0, v0)
    me.uv_layers[0].data[1].uv = (u1, v0)
    me.uv_layers[0].data[2].uv = (u1, v1)
    me.uv_layers[0].data[3].uv = (u0, v1)
    mat = bpy.data.materials.new("FxCard")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(image_path)
    tex.interpolation = "Linear"
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Color"], em.inputs["Color"])
    nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(em.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    em.inputs["Strength"].default_value = emit
    em.inputs["Color"].default_value = tint
    # Emission color replaces the texture if linked after. Keep texture color.
    nt.links.new(tex.outputs["Color"], em.inputs["Color"])
    ob.data.materials.append(mat)
    return ob


def ring(loc, radius, surface, cracked):
    col = surface / 6.0
    row = 0.5 if cracked else 0.0
    ob = card(
        os.path.join(ART, "RingAtlas.png"),
        (loc[0], loc[1], 0.03),
        (radius * 2, radius * 2, 1),
        (col, row, col + 1.0 / 6.0, row + 0.5),
        emit=1.1,
    )
    ob.rotation_euler = (0, 0, 0)
    return ob


def puff(loc, frame, size, tint=(0.85, 0.75, 0.55, 1)):
    u = frame / 8.0
    ob = card(
        os.path.join(ART, "DustPuff.png"),
        loc,
        (size, size * 0.8, 1),
        (u, 0.0, u + 0.125, 1.0),
        emit=1.3,
        tint=tint,
    )
    ob.rotation_euler = (math.radians(78), 0, 0)
    return ob


def drip(loc, scale=0.22):
    ob = card(
        os.path.join(ART, "Drip.png"),
        loc,
        (scale * 0.45, scale, 1),
        (0, 0, 1, 1),
        emit=1.2,
    )
    ob.rotation_euler = (math.radians(80), 0, 0)
    return ob


def ghost_card(loc, tint, fade, sy=1.15):
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc)
    ob = bpy.context.active_object
    ob.scale = (0.48, sy, 1)
    ob.rotation_euler = (math.radians(90), 0, 0)
    mat = bpy.data.materials.new("Ghost")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = tint
    bsdf.inputs["Emission Color"].default_value = tint
    bsdf.inputs["Emission Strength"].default_value = 1.4 * fade
    bsdf.inputs["Alpha"].default_value = 0.28 + 0.5 * fade
    bsdf.inputs["Roughness"].default_value = 0.2
    ob.data.materials.append(mat)
    return ob


def cam(loc, target):
    bpy.ops.object.camera_add(location=loc)
    c = bpy.context.active_object
    look_at(c, target)
    bpy.context.scene.camera = c
    c.data.lens = 42


def shot(name):
    path = os.path.join(OUT, name + ".png")
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("wrote", path)


def setup():
    clear()
    world()
    ground()
    arm = import_runner()
    return arm


def still_lands():
    arm = setup()
    spots = ((-1.7, 0.2, "light", 0.55, False), (0.15, 0.15, "medium", 0.85, False), (1.9, 0.1, "heavy", 1.25, True))
    arm.location.x = spots[1][0]
    pose_land(arm, "medium")
    for x, y, kind, rad, crack in spots:
        if kind != "medium":
            bpy.ops.object.select_all(action="DESELECT")
            arm.select_set(True)
            for o in bpy.data.objects:
                if o.type == "MESH" and o.parent == arm:
                    o.select_set(True)
            bpy.ops.object.duplicate()
            dup_arm = None
            for o in bpy.context.selected_objects:
                o.location.x += x - spots[1][0]
                if o.type == "ARMATURE":
                    dup_arm = o
            if dup_arm:
                pose_land(dup_arm, kind)
        ring((x, y), rad, 2 if kind != "light" else 1, crack)
        for i in range(5 if crack else 3):
            ang = i / 5.0 * math.tau
            puff((x + math.cos(ang) * rad * 0.7, y + math.sin(ang) * rad * 0.35, 0.25 + i * 0.02), i % 8, 0.28 + (0.12 if crack else 0))
    cam((0.2, -5.4, 2.3), (0.2, 0.2, 0.8))
    shot("landing-tiers")


def still_roll():
    arm = setup()
    pose_roll(arm)
    arm.location = (0.15, 0.05, 0.42)
    for i in range(7):
        puff((-1.1 + i * 0.38, -0.15 + math.sin(i) * 0.12, 0.18), min(7, i), 0.34 + i * 0.02, (0.72, 0.58, 0.38, 1))
    cam((2.4, -3.6, 1.7), (0.1, 0.0, 0.6))
    shot("roll-swirl")


def still_dash():
    arm = setup()
    pose_run(arm)
    arm.location = (0.8, 0, 0.04)
    tints = ((0.95, 0.28, 0.32, 1), (0.95, 0.28, 0.32, 1), (0.95, 0.28, 0.32, 1), (0.95, 0.28, 0.32, 1))
    for i, fade in enumerate((0.9, 0.68, 0.46, 0.28)):
        ghost_card((-0.35 - i * 0.48, 0.02, 0.95), tints[i], fade)
    cam((2.2, -4.2, 1.6), (0.1, 0, 0.9))
    shot("dash-ghosts")


def still_grapple():
    arm = setup()
    bend(arm, "UpperArm_L", -70, 10, -36)
    bend(arm, "LowerArm_L", -16)
    bend(arm, "Spine", -6)
    bpy.context.view_layer.update()
    hand = arm.matrix_world @ arm.pose.bones["Hand_L"].tail
    hand = (hand.x, hand.y, hand.z)
    hook = (2.30, 1.55, 1.15)
    curve = bpy.data.curves.new("Rope", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.018
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    pts = (hand, ((hand[0] + hook[0]) * 0.5, (hand[1] + hook[1]) * 0.5, 0.72), hook)
    for p, co in zip(spline.bezier_points, pts):
        p.co = co
        p.handle_left_type = "AUTO"
        p.handle_right_type = "AUTO"
    rope = bpy.data.objects.new("Rope", curve)
    bpy.context.collection.objects.link(rope)
    rope.data.materials.append(principled("Rope", (0.86, 0.78, 0.48, 1), 0.45))
    stamp = ring((hook[0], hook[1], hook[2]), 0.32, 2, True)
    stamp.rotation_euler = (0, math.radians(90), 0)
    stamp.location = (hook[0], hook[1], hook[2])
    puff((hook[0] - 0.08, hook[1], hook[2] - 0.05), 2, 0.2, (0.8, 0.8, 0.82, 1))
    cam((1.2, -3.8, 1.8), (1.1, 0.8, 1.1))
    shot("grapple")


def still_dizzy():
    arm = setup()
    pose_stagger(arm)
    for i, (x, z) in enumerate(((0.28, 1.95), (-0.05, 2.15), (-0.32, 1.9))):
        ob = card(DIZZY, (x, 0.15, z), (0.42, 0.42, 1), (0, 0, 1, 1), emit=1.6)
        ob.rotation_euler = (math.radians(80), 0, math.radians(-12 + i * 14))
    cam((1.8, -3.4, 1.7), (0, 0.1, 1.3))
    shot("dizzy")


def still_drips():
    arm = setup()
    arm.location = (1.85, 1.35, 0.04)
    arm.rotation_euler = (0, 0, math.radians(70))
    bend(arm, "UpperLeg_L", 20, 0, 18)
    bend(arm, "UpperLeg_R", -10, 0, 12)
    bend(arm, "Spine", 0, 0, 14)
    bend(arm, "UpperArm_L", 30, 0, -20)
    for i, z in enumerate((1.15, 0.92, 0.7, 0.5)):
        drip((2.22, 1.35 + (i - 1.5) * 0.08, z), 0.16 + i * 0.02)
    cam((3.4, -1.2, 1.5), (2.0, 1.4, 0.9))
    shot("wet-drips")


def main():
    os.makedirs(OUT, exist_ok=True)
    still_lands()
    still_roll()
    still_dash()
    still_grapple()
    still_dizzy()
    still_drips()


if __name__ == "__main__":
    main()
