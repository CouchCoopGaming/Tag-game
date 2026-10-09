#!/usr/bin/env python3
"""Pass 8 stills. Side trail of tinted ghosts, a readable shoulder roll, a letter-free comic burst.

Run:
  blender --background --python Tools/RenderPass8.py
"""
import math
import os
import sys

import bpy
from mathutils import Quaternion, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
import RenderPass7 as p7

OUT = os.path.join(ROOT, "Docs", "AnimStills", "pass8")
PLAYER = p7.PLAYER

TIPS = (1.16, 0.82, 1.24, 0.76, 1.08, 0.90, 1.20, 0.78, 1.04, 0.86, 1.14, 0.74)
VALLEYS = (0.46, 0.58, 0.40, 0.56, 0.44, 0.60, 0.42, 0.52, 0.48, 0.57, 0.41, 0.54)


def noisy(name, dark, light, scale, rough=0.86):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = 8.0
    noise.inputs["Roughness"].default_value = 0.55
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = dark
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = light
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = rough
    return mat


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
    nt = w.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    sky = nt.nodes.new("ShaderNodeTexSky")
    sky.sky_type = "HOSEK_WILKIE"
    sky.sun_elevation = math.radians(36)
    sky.sun_rotation = math.radians(-24)
    sky.altitude = 80
    sky.air_density = 0.9
    sky.dust_density = 1.6
    nt.links.new(sky.outputs["Color"], bg.inputs["Color"])
    bg.inputs["Strength"].default_value = 0.9
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(48), 0.05, math.radians(-36))
    sun.data.energy = 4.2
    sun.data.color = (1.0, 0.78, 0.52)
    sun.data.angle = math.radians(6)
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA"))
    bpy.context.collection.objects.link(fill)
    fill.location = (-3.2, -4.0, 3.4)
    fill.data.energy = 80
    fill.data.color = (1.0, 0.9, 0.78)
    fill.data.size = 5


def ground():
    bpy.ops.mesh.primitive_plane_add(size=48, location=(0, 0, 0))
    lawn = bpy.context.active_object
    lawn.name = "Lawn"
    lawn.data.materials.append(noisy(
        "Lawn", (0.18, 0.38, 0.14, 1), (0.46, 0.64, 0.22, 1), 14.0, 0.92))
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0.012))
    pad = bpy.context.active_object
    pad.name = "Pad"
    pad.scale = (7.5, 22, 1)
    pad.data.materials.append(noisy(
        "Concrete", (0.52, 0.48, 0.42, 1), (0.74, 0.68, 0.58, 1), 28.0, 0.74))


def setup():
    p7.clear()
    world()
    ground()
    os.makedirs(OUT, exist_ok=True)
    return p7.import_runner()


def shot(name, wide=False):
    scene = bpy.context.scene
    if wide:
        scene.render.resolution_x = 1920
        scene.render.resolution_y = 640
    else:
        scene.render.resolution_x = 1280
        scene.render.resolution_y = 720
    path = os.path.join(OUT, name + ".png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("wrote", path)


def ghost_mat(alpha):
    """Translucent player tint. The fresnel is a rim, not a white shell."""
    mat = bpy.data.materials.new("Ghost")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    mat.use_backface_culling = False
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tint = (PLAYER[0], PLAYER[1], PLAYER[2], 1)
    rim = (1.0, 0.72, 0.7, 1)
    bsdf.inputs["Base Color"].default_value = tint
    bsdf.inputs["Emission Color"].default_value = rim
    bsdf.inputs["Alpha"].default_value = alpha
    bsdf.inputs["Roughness"].default_value = 0.32
    layer = nt.nodes.new("ShaderNodeLayerWeight")
    layer.inputs["Blend"].default_value = 0.28
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = 0.85
    nt.links.new(layer.outputs["Fresnel"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], bsdf.inputs["Emission Strength"])
    return mat


def view(target, dist, yaw, height, lens, look_z):
    loc = Vector((
        target.x + math.sin(yaw) * dist,
        target.y - math.cos(yaw) * dist,
        height,
    ))
    p7.cam(loc, Vector((target.x, target.y, look_z)), lens)
    print("view", tuple(round(v, 2) for v in loc), "at", round(target.x, 2), round(target.y, 2))
    return loc


def frame_side(arms, extras=(), lens=48, fill=0.55, height=1.4):
    """Camera on +X so the travel axis reads left to right, with margin around the whole body."""
    mins, maxs = p7.object_bounds(list(arms) + list(extras))
    center = (mins + maxs) * 0.5
    size = maxs - mins
    span = max(size.y, 1.4)
    tall = max(size.z, 1.5)
    hfov = 2.0 * math.atan(18.0 / lens)
    vfov = 2.0 * math.atan(12.0 / lens)
    dist_h = (span * 0.5) / math.tan(hfov * 0.5) / fill
    dist_v = (tall * 0.5) / math.tan(vfov * 0.5) / fill
    dist = max(dist_h, dist_v, 6.0)
    look = Vector((center.x, center.y, max(0.75, mins.z + tall * 0.42)))
    loc = Vector((center.x + dist, center.y, height + mins.z))
    p7.cam(loc, look, lens)
    print("frame", tuple(round(v, 2) for v in mins), tuple(round(v, 2) for v in maxs), "dist", round(dist, 2))


def face_travel(arm, base_q):
    """Turn the body so forward is +Y, which reads left to right from the +X camera."""
    arm.rotation_mode = "QUATERNION"
    turn = Quaternion((0.0, 0.0, 1.0), math.pi)
    arm.rotation_quaternion = turn @ base_q


# Unity's right-shoulder roll axis (x right, y up, z forward), rewritten for a
# Blender body that faces +Y: right +X, forward +Y, up +Z.
ROLL_AXIS = Vector((0.86, 0.42, -0.30))


def spin_world(arm, pivot, degrees):
    q = Quaternion(ROLL_AXIS.normalized(), math.radians(degrees))
    arm.rotation_mode = "QUATERNION"
    arm.location = q @ (Vector(arm.location) - pivot) + pivot
    arm.rotation_quaternion = q @ arm.rotation_quaternion


def park(arm, station_y):
    """Sit the lowest vertex on the floor and on the travel line."""
    p7.drop_to_floor(arm)
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    best = None
    for ob in p7.meshes(arm):
        ev = ob.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ob.matrix_world
        for v in me.vertices:
            w = mw @ v.co
            if best is None or w.z < best.z:
                best = w.copy()
        ev.to_mesh_clear()
    arm.location.x -= best.x
    arm.location.y += station_y - best.y
    bpy.context.view_layer.update()


def tuck_roll(arm):
    """Chin in, knees up, lead arm across the right shoulder."""
    p7.bone(arm, "Hips", 16, 0, -8)
    p7.bone(arm, "Spine", 32, 0, 12)
    p7.bone(arm, "Chest", 16, 0, 8)
    p7.bone(arm, "Head", 55, 25, -20)
    p7.bone(arm, "UpperArm_R", -95, 6, -18)
    p7.bone(arm, "LowerArm_R", -55)
    p7.bone(arm, "UpperArm_L", 30, -6, 20)
    p7.bone(arm, "LowerArm_L", -40)
    p7.bone(arm, "UpperLeg_L", 100)
    p7.bone(arm, "UpperLeg_R", 92)
    p7.bone(arm, "LowerLeg_L", -125)
    p7.bone(arm, "LowerLeg_R", -118)


def pose_roll_side(arm, phase):
    """Limb pose plus how far the body has spun over the right shoulder."""
    if phase == 0:
        p7.bone(arm, "Hips", 22, 0, -4)
        p7.bone(arm, "Spine", 34)
        p7.bone(arm, "Head", 26)
        p7.bone(arm, "UpperArm_R", -120, 8, -18)
        p7.bone(arm, "LowerArm_R", -12)
        p7.bone(arm, "UpperArm_L", 40, -18, 24)
        p7.bone(arm, "LowerArm_L", -16)
        p7.bone(arm, "UpperLeg_L", 48)
        p7.bone(arm, "UpperLeg_R", -22)
        p7.bone(arm, "LowerLeg_L", -55)
        p7.bone(arm, "LowerLeg_R", -20)
        return 32
    if phase == 1:
        tuck_roll(arm)
        return 200
    if phase == 2:
        tuck_roll(arm)
        return 230
    if phase == 3:
        tuck_roll(arm)
        return 275
    p7.pose_sprint(arm)
    p7.bone(arm, "Head", 6)
    return 0


def present_roll(arm, base_q, phase, station):
    p7.reset_arm(arm, base_q)
    spin = pose_roll_side(arm, phase)
    face_travel(arm, base_q)
    if spin:
        bpy.context.view_layer.update()
        spin_world(arm, p7.bone_point(arm, "Shoulder_R"), spin)
    park(arm, station)


def still_ghosts():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    p7.paint(arm, p7.principled("Player", PLAYER, 0.48))
    p7.reset_arm(arm, base_q)
    p7.pose_sprint(arm)
    p7.drop_to_floor(arm)
    alphas = (0.60, 0.45, 0.30, 0.15)
    phases = (0.55, 0.05, -0.45, -0.9)
    ghosts = []
    for i in range(4):
        ghost = p7.clone_arm(arm)
        p7.reset_arm(ghost, base_q)
        p7.pose_run(ghost, phases[i])
        p7.paint(ghost, ghost_mat(alphas[i]))
        p7.drop_to_floor(ghost)
        # Behind the facing (-Y is forward), overlapping the runner a little.
        p7.place(ghost, 0.04 * ((i % 2) * 2 - 1), 0.18 + i * 0.42)
        ghosts.append(ghost)
    target = Vector((0.15, 0.7, 0.95))
    view(target, 6.4, 0.95, 1.45, 50, 0.95)
    shot("dash-ghosts")


def still_roll(name, phase, swirl):
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    p7.paint(arm, p7.principled("Player", PLAYER, 0.5))
    present_roll(arm, base_q, phase, 0.0)
    extras = []
    if swirl:
        for i, frame_i in enumerate((1, 3, 5, 6)):
            extras.append(p7.puff((0.05, -0.15 - i * 0.32, 0.1), frame_i, 0.32))
    frame_side([arm], extras, lens=48, fill=0.42, height=1.35)
    shot(name)


def still_strip():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    p7.paint(arm, p7.principled("Player", PLAYER, 0.5))
    arms = []
    for i in range(5):
        guy = arm if i == 0 else p7.clone_arm(arm)
        present_roll(guy, base_q, i, -4.2 + i * 2.1)
        arms.append(guy)
    extras = [p7.puff((arms[2].location.x, arms[2].location.y - 0.2, 0.1), 4, 0.28)]
    frame_side(arms, extras, lens=38, fill=0.52, height=1.4)
    shot("roll-strip", wide=True)


def burst_poly(name, scale, color, y, count=12):
    verts = [(0.0, y, 0.0)]
    for i in range(count):
        ang_t = (i + 0.5) / count * math.tau - math.pi / 2
        ang_v = (i + 1.0) / count * math.tau - math.pi / 2
        rt = TIPS[i % len(TIPS)] * scale
        rv = VALLEYS[i % len(VALLEYS)] * scale
        verts.append((math.cos(ang_t) * rt, y, math.sin(ang_t) * rt))
        verts.append((math.cos(ang_v) * rv, y, math.sin(ang_v) * rv))
    faces = []
    n = count * 2
    for i in range(n):
        faces.append((0, 1 + i, 1 + ((i + 1) % n)))
        faces.append((0, 1 + ((i + 1) % n), 1 + i))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(p7.emissive(name + "Mat", color, 1.1))
    return ob


def spike(origin, ang, length, width, color, y):
    c = math.cos(ang)
    s = math.sin(ang)
    p = Vector((c, 0, s))
    side = Vector((-s, 0, c))
    a = Vector(origin)
    b = a + p * length
    verts = [
        (a + side * width).x, (a + side * width).y, (a + side * width).z,
    ]
    pts = [
        tuple(a + side * (width * 0.5)),
        tuple(a - side * (width * 0.5)),
        tuple(b),
    ]
    pts = [(p_[0], y, p_[2]) for p_ in pts]
    mesh = bpy.data.meshes.new("Spike")
    mesh.from_pydata(pts, [], [(0, 1, 2), (0, 2, 1)])
    mesh.update()
    ob = bpy.data.objects.new("Spike", mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(p7.emissive("SpikeMat", color, 0.6))
    return ob


def dot(loc, radius, color):
    bpy.ops.mesh.primitive_circle_add(vertices=10, radius=radius, fill_type="NGON", location=loc)
    ob = bpy.context.active_object
    ob.rotation_euler = (math.radians(90), 0, 0)
    ob.data.materials.append(p7.emissive("Dot", color, 0.4))
    return ob


def still_tag():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    p7.paint(arm, p7.principled("Player", PLAYER, 0.5))
    p7.reset_arm(arm, base_q)
    p7.pose_punch(arm)
    p7.drop_to_floor(arm)
    fist = p7.bone_point(arm, "Hand_R")
    origin = (fist.x, fist.y - 0.22, fist.z + 0.02)
    pieces = []
    for i in range(8):
        ang = i / 8.0 * math.tau
        pieces.append(spike(origin, ang, 0.55 + (i % 2) * 0.18, 0.035, (0.12, 0.08, 0.08, 1), origin[1] + 0.04))
    pieces.append(burst_poly("Outline", 0.62, (0.05, 0.04, 0.04, 1), origin[1] + 0.02))
    pieces.append(burst_poly("Fill", 0.50, (1.0, 0.62, 0.12, 1), origin[1]))
    pieces.append(burst_poly("Flash", 0.22, (1.0, 0.98, 0.92, 1), origin[1] - 0.02))
    for ix in range(-2, 3):
        for iz in range(-2, 3):
            if (ix + iz) % 2 != 0:
                continue
            if ix * ix + iz * iz > 5:
                continue
            pieces.append(dot(
                (origin[0] + ix * 0.07, origin[1] - 0.03, origin[2] + iz * 0.07),
                0.012,
                (0.55, 0.12, 0.1, 1),
            ))
    for ob in pieces:
        ob.location.x += 0
    frame_side([arm], pieces, lens=48, fill=0.5, height=1.6)
    shot("tag-burst")


if __name__ == "__main__":
    only = os.environ.get("PASS8_ONLY", "")
    shots = {
        "ghosts": still_ghosts,
        "roll": lambda: still_roll("roll-swirl", 2, True),
        "strip": still_strip,
        "tag": still_tag,
    }
    if only:
        for name in only.split(","):
            shots[name.strip()]()
    else:
        for fn in shots.values():
            fn()
