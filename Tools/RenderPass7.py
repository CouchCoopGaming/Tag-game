#!/usr/bin/env python3
"""Pass 7 stills. Blender EEVEE, Hier mannequin, real dust textures.

The roll keys match LandingRollPose. Run:
  blender --background --python Tools/RenderPass7.py
"""
import math
import os

import bpy
from mathutils import Quaternion, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
ART = os.path.join(ROOT, "Assets", "Resources", "FX")
OUT = os.path.join(ROOT, "Docs", "AnimStills", "pass7")
PLAYER = (0.93, 0.34, 0.40, 1)

# Unity Y-up axis (0.86, -0.30, 0.42) in this Z-up import.
ROLL_AXIS = Vector((0.86, 0.42, -0.30)).normalized()


def ease(u):
    if u <= 0:
        return 0.0
    if u >= 1:
        return 1.0
    return u * u * (3 - 2 * u)


def sample(**kw):
    s = {
        "Hip": 0, "HipYaw": 0, "HipRoll": 0,
        "Spine": 0, "SpineYaw": 0, "SpineRoll": 0,
        "Head": 0, "HeadYaw": 0,
        "ThighL": 0, "ThighR": 0, "KneeL": 0, "KneeR": 0,
        "ArmPitchL": 0, "ArmPitchR": 0, "ArmYawL": 0, "ArmYawR": 0,
        "ArmRollL": 0, "ArmRollR": 0, "ElbowL": 0, "ElbowR": 0,
    }
    s.update(kw)
    return s


ROLL_KEYS = [
    (0.00, sample(Hip=28, Spine=22, Head=-40, ThighL=62, ThighR=56, KneeL=-96, KneeR=-90,
                  ArmPitchL=-42, ArmPitchR=-55, ArmYawL=18, ArmYawR=-36, ArmRollR=22,
                  ElbowL=-108, ElbowR=-100, SpineRoll=8)),
    (0.18, sample(Hip=22, Spine=26, Head=-36, ThighL=72, ThighR=66, KneeL=-108, KneeR=-102,
                  ArmPitchL=-30, ArmPitchR=-78, ArmYawL=12, ArmYawR=-48, ArmRollR=36,
                  ElbowL=-96, ElbowR=-112, SpineRoll=14)),
    (0.36, sample(Hip=14, Spine=18, Head=-32, ThighL=90, ThighR=82, KneeL=-118, KneeR=-110,
                  ArmPitchL=-18, ArmPitchR=-64, ArmYawR=-30, ArmRollR=24,
                  ElbowL=-80, ElbowR=-104, SpineRoll=20)),
    (0.52, sample(Hip=8, Spine=6, Head=-38, ThighL=112, ThighR=104, KneeL=-120, KneeR=-112,
                  ArmPitchL=-12, ArmPitchR=-28, ArmYawR=-16, ElbowL=-70, ElbowR=-92,
                  SpineRoll=28, HipRoll=-10)),
    (0.68, sample(Hip=-4, Spine=-6, Head=-22, ThighL=118, ThighR=108, KneeL=-108, KneeR=-100,
                  ArmPitchL=-10, ArmPitchR=-8, ElbowL=-52, ElbowR=-64, SpineRoll=18)),
    (0.84, sample(Hip=12, Spine=8, Head=-8, ThighL=38, ThighR=22, KneeL=-32, KneeR=-18,
                  ArmPitchL=-20, ArmPitchR=-12, ElbowL=-36, ElbowR=-28, SpineRoll=8)),
    (1.00, sample(Hip=6, Spine=2, Head=-2, ThighL=24, ThighR=10, KneeL=-18, KneeR=-12,
                  ArmPitchL=-18, ArmPitchR=-10, ElbowL=-16, ElbowR=-14)),
]


def lerp_sample(a, b, t):
    return {k: a[k] * (1 - t) + b[k] * t for k in a}


def roll_at(u):
    if u < 0:
        u = 0
    if u > 1:
        u = 1
    for i in range(len(ROLL_KEYS) - 1):
        u0, a = ROLL_KEYS[i]
        u1, b = ROLL_KEYS[i + 1]
        if u <= u1 or i == len(ROLL_KEYS) - 2:
            span = u1 - u0
            t = 1.0 if span < 0.0001 else ease((u - u0) / span)
            return lerp_sample(a, b, t), 360.0 * u
    return ROLL_KEYS[-1][1], 360.0


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
    scene.eevee.taa_render_samples = 20
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
    sky.sun_elevation = math.radians(52)
    sky.sun_rotation = math.radians(18)
    sky.altitude = 200
    sky.air_density = 1.2
    sky.dust_density = 0.6
    nt.links.new(sky.outputs["Color"], bg.inputs["Color"])
    bg.inputs["Strength"].default_value = 0.55
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(52), 0.1, math.radians(24))
    sun.data.energy = 2.4
    sun.data.angle = math.radians(8)
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA"))
    bpy.context.collection.objects.link(fill)
    fill.location = (-2.4, -3.2, 3.2)
    fill.data.energy = 60
    fill.data.size = 4


def principled(name, color, rough=0.62):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = rough
    return mat


def ground():
    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, 0))
    lawn = bpy.context.active_object
    lawn.name = "Lawn"
    lawn.data.materials.append(principled("Lawn", (0.34, 0.52, 0.24, 1), 0.9))
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0.008))
    pad = bpy.context.active_object
    pad.name = "Pad"
    pad.scale = (22, 16, 1)
    pad.data.materials.append(principled("Concrete", (0.66, 0.65, 0.62, 1), 0.72))


def fresnel_ghost(fade):
    mat = bpy.data.materials.new("Ghost")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    rim = (1.0, 0.62, 0.64, 1)
    body = (PLAYER[0], PLAYER[1], PLAYER[2], 1)
    bsdf.inputs["Base Color"].default_value = body
    bsdf.inputs["Emission Color"].default_value = rim
    bsdf.inputs["Emission Strength"].default_value = 0.35 + 1.6 * fade
    bsdf.inputs["Alpha"].default_value = 0.16 + 0.62 * fade
    bsdf.inputs["Roughness"].default_value = 0.28
    layer = nt.nodes.new("ShaderNodeLayerWeight")
    layer.inputs["Blend"].default_value = 0.22
    mix = nt.nodes.new("ShaderNodeMixShader")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = rim
    em.inputs["Strength"].default_value = 2.4 * fade
    nt.links.new(layer.outputs["Fresnel"], mix.inputs["Fac"])
    nt.links.new(bsdf.outputs["BSDF"], mix.inputs[1])
    nt.links.new(em.outputs["Emission"], mix.inputs[2])
    out = nt.nodes.get("Material Output")
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def import_runner():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=FBX)
    created = [ob for ob in bpy.data.objects if ob not in before]
    arm = None
    for ob in created:
        if ob.type == "ARMATURE":
            arm = ob
            break
    if arm is None:
        raise RuntimeError("no armature in " + FBX)
    arm.rotation_mode = "QUATERNION"
    print("arm rot", tuple(round(v, 3) for v in arm.rotation_quaternion))
    return arm


def reset_arm(arm, base_q):
    arm.rotation_mode = "QUATERNION"
    arm.rotation_quaternion = base_q.copy()
    arm.location = (0.0, 0.0, 0.0)
    for b in arm.pose.bones:
        b.rotation_mode = "XYZ"
        b.rotation_euler = (0.0, 0.0, 0.0)


def clone_arm(src):
    bpy.ops.object.select_all(action="DESELECT")
    src.select_set(True)
    for child in src.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = src
    bpy.ops.object.duplicate()
    return bpy.context.view_layer.objects.active


def paint(arm, mat):
    for ob in meshes(arm):
        ob.data = ob.data.copy()
        count = len(ob.data.materials)
        if count == 0:
            ob.data.materials.append(mat)
            continue
        for i in range(count):
            ob.data.materials[i] = mat


def bone(arm, name, x=0.0, y=0.0, z=0.0):
    b = arm.pose.bones[name]
    b.rotation_mode = "XYZ"
    b.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))


def apply_sample(arm, s):
    bone(arm, "Hips", s["Hip"], s["HipYaw"], s["HipRoll"])
    bone(arm, "Spine", s["Spine"], s["SpineYaw"], s["SpineRoll"])
    bone(arm, "Head", s["Head"], s["HeadYaw"], 0)
    bone(arm, "UpperArm_L", s["ArmPitchL"], s["ArmYawL"], s["ArmRollL"])
    bone(arm, "UpperArm_R", s["ArmPitchR"], s["ArmYawR"], s["ArmRollR"])
    bone(arm, "LowerArm_L", s["ElbowL"], 0, 0)
    bone(arm, "LowerArm_R", s["ElbowR"], 0, 0)
    bone(arm, "UpperLeg_L", s["ThighL"], 0, 0)
    bone(arm, "UpperLeg_R", s["ThighR"], 0, 0)
    bone(arm, "LowerLeg_L", s["KneeL"], 0, 0)
    bone(arm, "LowerLeg_R", s["KneeR"], 0, 0)


def pose_run(arm, swing):
    bone(arm, "UpperLeg_L", 34 * swing)
    bone(arm, "UpperLeg_R", -30 * swing)
    bone(arm, "LowerLeg_L", -22 - 20 * max(swing, 0))
    bone(arm, "LowerLeg_R", -38 - 16 * max(-swing, 0))
    bone(arm, "UpperArm_L", -26 * swing)
    bone(arm, "UpperArm_R", 22 * swing)
    bone(arm, "LowerArm_L", -34)
    bone(arm, "LowerArm_R", -28)
    bone(arm, "Spine", 12)
    bone(arm, "Head", 12)


def pose_land(arm, kind):
    if kind == "light":
        apply_sample(arm, sample(
            Hip=6, Spine=4, Head=2, ThighL=20, ThighR=16, KneeL=-24, KneeR=-22,
            ArmPitchL=-18, ArmPitchR=14, ArmYawL=0, ElbowL=-10, ElbowR=-8, HipYaw=6))
    elif kind == "medium":
        apply_sample(arm, sample(
            Hip=20, Spine=18, Head=-10, ThighL=46, ThighR=40, KneeL=-64, KneeR=-58,
            ArmPitchL=-28, ArmPitchR=-22, ElbowL=-20, ElbowR=-18, HipYaw=-8))
    else:
        apply_sample(arm, sample(
            Hip=40, Spine=56, Head=-26, ThighL=86, ThighR=80, KneeL=-118, KneeR=-112,
            ArmPitchL=-64, ArmPitchR=-58, ElbowL=-16, ElbowR=-18, HipYaw=8))


def spin_roll(arm, degrees):
    arm.rotation_mode = "QUATERNION"
    base = arm.rotation_quaternion.copy()
    spin = Quaternion(ROLL_AXIS, math.radians(degrees))
    arm.rotation_quaternion = spin @ base


def meshes(arm):
    return [ob for ob in [arm] + list(arm.children_recursive) if ob.type == "MESH"]


def drop_to_floor(arm):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    minz = 1e9
    for ob in meshes(arm):
        ev = ob.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ob.matrix_world
        for v in me.vertices:
            z = (mw @ v.co).z
            if z < minz:
                minz = z
        ev.to_mesh_clear()
    if minz < 1e8:
        arm.location.z += -minz + 0.012
    bpy.context.view_layer.update()


def pose_roll_visual(arm, phase):
    """Blender XYZ. Faces -Y. Right shoulder is -X. Chin tuck is +X on the head."""
    if phase == 0:
        bone(arm, "Root", 12, 0, 18)
        bone(arm, "Spine", 34)
        bone(arm, "Head", 46)
        bone(arm, "UpperArm_R", -24, 8, -42)
        bone(arm, "LowerArm_R", -118)
        bone(arm, "UpperArm_L", -12, -6, 28)
        bone(arm, "LowerArm_L", -102)
        bone(arm, "UpperLeg_L", 48)
        bone(arm, "UpperLeg_R", 40)
        bone(arm, "LowerLeg_L", -108)
        bone(arm, "LowerLeg_R", -100)
    elif phase == 1:
        bone(arm, "Root", 28, 6, 58)
        bone(arm, "Spine", 30)
        bone(arm, "Head", 52)
        bone(arm, "UpperArm_R", -36, 4, -55)
        bone(arm, "LowerArm_R", -125)
        bone(arm, "UpperArm_L", -8, 0, 22)
        bone(arm, "LowerArm_L", -96)
        bone(arm, "UpperLeg_L", 62)
        bone(arm, "UpperLeg_R", 54)
        bone(arm, "LowerLeg_L", -118)
        bone(arm, "LowerLeg_R", -112)
    elif phase == 2:
        bone(arm, "Root", 24, 12, 76)
        bone(arm, "Spine", 26)
        bone(arm, "Head", 64)
        bone(arm, "UpperArm_R", -52, 8, -28)
        bone(arm, "LowerArm_R", -132)
        bone(arm, "UpperArm_L", 6, 0, 16)
        bone(arm, "LowerArm_L", -80)
        bone(arm, "UpperLeg_L", 70)
        bone(arm, "UpperLeg_R", 64)
        bone(arm, "LowerLeg_L", -124)
        bone(arm, "LowerLeg_R", -116)
    elif phase == 3:
        bone(arm, "Root", -8, 4, 168)
        bone(arm, "Spine", 8)
        bone(arm, "Head", 36)
        bone(arm, "UpperArm_R", 8, 0, -20)
        bone(arm, "LowerArm_R", -70)
        bone(arm, "UpperArm_L", 10, 0, 12)
        bone(arm, "LowerArm_L", -48)
        bone(arm, "UpperLeg_L", 78)
        bone(arm, "UpperLeg_R", 70)
        bone(arm, "LowerLeg_L", -110)
        bone(arm, "LowerLeg_R", -100)
    else:
        bone(arm, "Root", 4, 0, 8)
        bone(arm, "Spine", 14)
        bone(arm, "Head", 10)
        bone(arm, "UpperArm_L", -18)
        bone(arm, "UpperArm_R", 16)
        bone(arm, "LowerArm_L", -30)
        bone(arm, "LowerArm_R", -24)
        bone(arm, "UpperLeg_L", 28)
        bone(arm, "UpperLeg_R", -18)
        bone(arm, "LowerLeg_L", -22)
        bone(arm, "LowerLeg_R", -36)


def place(arm, x, y):
    arm.location.x += x
    arm.location.y += y


def card(image_path, loc, scale, uv, emit=1.2):
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
    ob.data.materials.append(mat)
    ob.rotation_euler = (math.radians(68), 0, 0)
    return ob


def puff(loc, frame, size):
    u = frame / 8.0
    return card(
        os.path.join(ART, "DustPuff.png"),
        loc,
        (size, size * 0.72, 1),
        (u, 0.0, u + 0.125, 1.0),
    )


def ring(loc, radius, cracked):
    col = 2 / 6.0
    row = 0.5 if cracked else 0.0
    ob = card(
        os.path.join(ART, "RingAtlas.png"),
        (loc[0], loc[1], 0.02),
        (radius * 2, radius * 2, 1),
        (col, row, col + 1.0 / 6.0, row + 0.5),
        emit=0.9,
    )
    ob.rotation_euler = (0, 0, 0)
    return ob


def cam(loc, target, lens=48):
    bpy.ops.object.camera_add(location=loc)
    c = bpy.context.active_object
    look_at(c, target)
    bpy.context.scene.camera = c
    c.data.lens = lens
    return c


def bounds(arms):
    bpy.context.view_layer.update()
    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for arm in arms:
        for ob in meshes(arm):
            for corner in ob.bound_box:
                w = ob.matrix_world @ Vector(corner)
                mins.x = min(mins.x, w.x)
                mins.y = min(mins.y, w.y)
                mins.z = min(mins.z, w.z)
                maxs.x = max(maxs.x, w.x)
                maxs.y = max(maxs.y, w.y)
                maxs.z = max(maxs.z, w.z)
    return mins, maxs


def frame(arms, lens=48, fill=0.72):
    frame_view(arms, lens=lens, fill=fill, yaw=0.0)


def object_bounds(objs):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for ob in objs:
        targets = meshes(ob) if ob.type == "ARMATURE" else [ob]
        for piece in targets:
            if piece.type != "MESH":
                continue
            ev = piece.evaluated_get(deps)
            me = ev.to_mesh()
            mw = piece.matrix_world
            for v in me.vertices:
                w = mw @ v.co
                mins.x = min(mins.x, w.x)
                mins.y = min(mins.y, w.y)
                mins.z = min(mins.z, w.z)
                maxs.x = max(maxs.x, w.x)
                maxs.y = max(maxs.y, w.y)
                maxs.z = max(maxs.z, w.z)
            ev.to_mesh_clear()
    # Pad the soles and the crown so a lean does not kiss the frame.
    mins.z -= 0.06
    maxs.z += 0.12
    return mins, maxs


def frame_view(arms, extras=(), lens=48, fill=0.72, yaw=0.0):
    mins, maxs = object_bounds(list(arms) + list(extras))
    center = (mins + maxs) * 0.5
    size = maxs - mins
    hfov = 2.0 * math.atan(18.0 / lens)
    vfov = 2.0 * math.atan(12.0 / lens)
    span = max(size.x, size.y * 0.35, 0.7)
    dist_h = (span * 0.5) / math.tan(hfov * 0.5) / fill
    dist_v = (max(size.z, 1.2) * 0.5) / math.tan(vfov * 0.5) / (fill * 0.9)
    dist = max(dist_h, dist_v, 3.4)
    loc = Vector((
        center.x - math.sin(yaw) * dist,
        center.y - math.cos(yaw) * dist,
        max(1.15, center.z * 0.42 + 1.05),
    ))
    cam(loc, Vector((center.x, center.y, max(0.9, mins.z + size.z * 0.46))), lens)
    print("frame", tuple(round(v, 2) for v in mins), tuple(round(v, 2) for v in maxs), "dist", round(dist, 2))


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


def setup():
    clear()
    world()
    ground()
    os.makedirs(OUT, exist_ok=True)
    return import_runner()


def emissive(name, rgba, strength=6.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    mat.use_backface_culling = False
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Emission Color"].default_value = (rgba[0], rgba[1], rgba[2], 1)
    bsdf.inputs["Emission Strength"].default_value = strength
    # Stars and sparks should stay their color. A hard emission blows them white.
    bsdf.inputs["Alpha"].default_value = rgba[3]
    bsdf.inputs["Roughness"].default_value = 0.35
    return mat


def streak(a, b, rgba, w0, w1, name="Streak"):
    a = Vector(a)
    b = Vector(b)
    side = (b - a).cross(Vector((0.0, 1.0, 0.0)))
    if side.length < 1e-4:
        side = Vector((1.0, 0.0, 0.0))
    else:
        side.normalize()
    verts = [
        a + side * (w0 * 0.5),
        a - side * (w0 * 0.5),
        b - side * (w1 * 0.5),
        b + side * (w1 * 0.5),
    ]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], [(0, 1, 2, 3)])
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(emissive(name + "Mat", rgba, 3.2))
    return ob


def spark(loc, radius, rgba):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=radius, location=loc)
    ob = bpy.context.active_object
    ob.data.materials.append(emissive("Spark", rgba, 6.0))
    return ob


def star(loc, size, rgba):
    spikes = 8
    verts = [(0.0, 0.0, 0.0)]
    for i in range(spikes):
        ang = i / float(spikes) * math.tau
        rad = size * (0.5 if (i % 2) == 0 else 0.22)
        verts.append((math.cos(ang) * rad, 0.0, math.sin(ang) * rad))
    faces = []
    for i in range(spikes):
        a = 1 + i
        b = 1 + ((i + 1) % spikes)
        faces.append((0, a, b))
        faces.append((0, b, a))
    mesh = bpy.data.meshes.new("Star")
    mesh.from_pydata([(x + loc[0], y + loc[1], z + loc[2]) for x, y, z in verts], [], faces)
    mesh.update()
    ob = bpy.data.objects.new("Star", mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(emissive("StarMat", rgba, 1.8))
    return ob


def pose_sprint(arm):
    bone(arm, "Root", 18, 0, 0)
    bone(arm, "Spine", 22)
    bone(arm, "Head", 10)
    bone(arm, "UpperLeg_L", 48)
    bone(arm, "UpperLeg_R", -42)
    bone(arm, "LowerLeg_L", -38)
    bone(arm, "LowerLeg_R", -62)
    bone(arm, "UpperArm_L", -62)
    bone(arm, "UpperArm_R", 54)
    bone(arm, "LowerArm_L", -48)
    bone(arm, "LowerArm_R", -36)


def pose_wall_run(arm):
    bone(arm, "Root", 6, 0, -18)
    bone(arm, "Spine", 14, 0, -8)
    bone(arm, "Head", 6, 0, 6)
    bone(arm, "UpperArm_L", -78, 24, 12)
    bone(arm, "LowerArm_L", -36)
    bone(arm, "UpperArm_R", 18, -8, 0)
    bone(arm, "LowerArm_R", -42)
    bone(arm, "UpperLeg_L", 36)
    bone(arm, "UpperLeg_R", -24)
    bone(arm, "LowerLeg_L", -28)
    bone(arm, "LowerLeg_R", -46)


def pose_punch(arm):
    bone(arm, "Hips", 4, -18, 0)
    bone(arm, "Spine", 8, -22, 0)
    bone(arm, "Chest", 4, -10, 0)
    bone(arm, "Head", 2, -8, 0)
    bone(arm, "Shoulder_R", 0, 0, -15)
    bone(arm, "UpperArm_R", -75, 15, -55)
    bone(arm, "LowerArm_R", -8)
    bone(arm, "UpperArm_L", 50, -20, 35)
    bone(arm, "LowerArm_L", -85)
    bone(arm, "UpperLeg_L", 28)
    bone(arm, "UpperLeg_R", -18)
    bone(arm, "LowerLeg_L", -36)
    bone(arm, "LowerLeg_R", -22)


def bone_point(arm, name):
    bpy.context.view_layer.update()
    pb = arm.pose.bones[name]
    return arm.matrix_world @ pb.tail


def still_speed():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    paint(arm, principled("Player", PLAYER, 0.5))
    reset_arm(arm, base_q)
    pose_sprint(arm)
    drop_to_floor(arm)
    place(arm, 0.0, 0.0)
    # Four sprint lines, white-blue, trailing behind the facing (-Y is forward).
    drawn = []
    n = 4
    length = 1.15
    for i in range(n):
        lat = (-0.62, -0.28, 0.34, 0.7)[i]
        z = 1.15 - abs(lat) * 0.15
        back = 0.35 + (i % 2) * 0.08
        a = (arm.location.x + lat, arm.location.y + back, z)
        b = (a[0], a[1] + length, z + 0.04)
        drawn.append(streak(a, b, (0.93, 0.96, 1.0, 0.95), 0.055, 0.012))
    frame_view([arm], drawn, 46, fill=0.58, yaw=0.62)
    shot("speed-lines")


def still_scrape():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    paint(arm, principled("Player", PLAYER, 0.5))
    reset_arm(arm, base_q)
    pose_wall_run(arm)
    drop_to_floor(arm)
    place(arm, -0.2, 0.0)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(2.35, 0.35, 2.15))
    wall = bpy.context.active_object
    wall.name = "ScrapeWall"
    wall.scale = (3.7, 6.4, 4.3)
    wall.data.materials.append(principled("WallConcrete", (0.38, 0.37, 0.36, 1), 0.92))
    key = bpy.data.objects.new("ScrapeKey", bpy.data.lights.new("ScrapeKey", "AREA"))
    bpy.context.collection.objects.link(key)
    key.location = (-2.2, -1.8, 2.2)
    key.data.energy = 140
    key.data.size = 2.2
    face_x = 0.42
    hand = bone_point(arm, "Hand_L")
    chest = (face_x, arm.location.y + 0.02, 1.32)
    hand = (face_x, hand.y, max(0.95, hand.z))
    hot = (1.0, 0.72, 0.18, 1)
    warm = (1.0, 0.42, 0.08, 1)
    bits = []
    for i, origin in enumerate((chest, hand)):
        for k in range(5 if i == 0 else 3):
            ang = -0.7 + k * 0.32
            dist = 0.18 + 0.07 * k
            tip = (
                origin[0] - dist * 0.65,
                origin[1] + 0.05 * k,
                origin[2] + math.sin(ang) * 0.22,
            )
            bits.append(spark(origin, 0.07, hot))
            bits.append(streak(origin, tip, warm, 0.045, 0.014))
            bits.append(spark(tip, 0.045, hot))
    frame_view([arm], bits, 48, fill=0.6, yaw=0.5)
    shot("wall-scrape")


def still_tag():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    paint(arm, principled("Player", PLAYER, 0.5))
    reset_arm(arm, base_q)
    pose_punch(arm)
    drop_to_floor(arm)
    place(arm, -0.2, 0.1)
    fist = bone_point(arm, "Hand_R")
    contact = (fist.x, fist.y - 0.28, fist.z + 0.06)
    bits = 8
    gold = (1.0, 0.74, 0.12, 1)
    stars = []
    for n in range(bits):
        ang = n / float(bits) * math.tau
        at = (
            contact[0] + math.cos(ang) * 0.32,
            contact[1],
            contact[2] + math.sin(ang) * 0.26,
        )
        stars.append(star(at, 0.46 + (n % 2) * 0.1, gold))
    frame_view([arm], stars, 48, fill=0.6, yaw=0.15)
    shot("tag-burst")


def still_flash():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    paint(arm, principled("Player", PLAYER, 0.5))
    reset_arm(arm, base_q)
    pose_run(arm, 0.35)
    drop_to_floor(arm)
    place(arm, 0, 0)
    chest = (arm.location.x, arm.location.y + 0.06, 1.12)
    span = 0.92
    bpy.ops.mesh.primitive_plane_add(size=span, location=chest, rotation=(math.radians(90), 0, 0))
    quad = bpy.context.active_object
    quad.name = "ItFlash"
    flash = (PLAYER[0], PLAYER[1], PLAYER[2], 0.42)
    quad.data.materials.append(emissive("Flash", flash, 2.2))
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.62, minor_radius=0.022, location=(chest[0], chest[1] - 0.04, chest[2]),
        rotation=(math.radians(90), 0, 0))
    ring_ob = bpy.context.active_object
    ring_ob.data.materials.append(emissive("FlashRing", (PLAYER[0], PLAYER[1], PLAYER[2], 0.95), 3.5))
    frame_view([arm], (quad, ring_ob), 48, fill=0.58, yaw=0.2)
    shot("handoff-flash")


def still_lands():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    body = principled("Player", PLAYER, 0.58)
    paint(arm, body)
    kinds = ("light", "medium", "heavy")
    radii = (0.55, 0.85, 1.25)
    arms = []
    for i, kind in enumerate(kinds):
        guy = arm if i == 0 else clone_arm(arm)
        reset_arm(guy, base_q)
        pose_land(guy, kind)
        drop_to_floor(guy)
        place(guy, (i - 1) * 2.15, 0)
        arms.append(guy)
        ring((guy.location.x, guy.location.y), radii[i], kind == "heavy")
        puff((guy.location.x + 0.42, guy.location.y + 0.2, 0.1), 2 + i, 0.26 + 0.06 * i)
    frame(arms, 42)
    shot("landing-tiers")


def still_roll():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    paint(arm, principled("Player", PLAYER, 0.58))
    reset_arm(arm, base_q)
    pose_roll_visual(arm, 2)
    drop_to_floor(arm)
    for i, frame_i in enumerate((1, 3, 5, 6)):
        puff((arm.location.x - 0.15 - i * 0.34, arm.location.y - 0.35, 0.08), frame_i, 0.3)
    frame([arm], 50)
    shot("roll-swirl")


def still_strip():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    paint(arm, principled("Player", PLAYER, 0.58))
    arms = []
    for i in range(5):
        guy = arm if i == 0 else clone_arm(arm)
        reset_arm(guy, base_q)
        pose_roll_visual(guy, i)
        drop_to_floor(guy)
        place(guy, (i - 2) * 1.9, 0)
        arms.append(guy)
    puff((arms[2].location.x + 0.2, arms[2].location.y - 0.35, 0.08), 4, 0.26)
    frame(arms, 40, fill=0.78)
    shot("roll-strip", wide=True)


def still_ghosts():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    paint(arm, principled("Player", PLAYER, 0.5))
    reset_arm(arm, base_q)
    pose_run(arm, 0.85)
    drop_to_floor(arm)
    place(arm, 1.85, 0.15)
    fades = (0.95, 0.68, 0.42, 0.2)
    phases = (1.0, 0.45, -0.05, -0.55)
    arms = [arm]
    for i in range(4):
        ghost = clone_arm(arm)
        reset_arm(ghost, base_q)
        pose_run(ghost, phases[i])
        paint(ghost, fresnel_ghost(fades[i]))
        drop_to_floor(ghost)
        place(ghost, 1.85 - (i + 1) * 0.98, 0.15 - (i + 1) * 0.08)
        arms.append(ghost)
    frame(arms, 48, fill=0.62)
    shot("dash-ghosts")


if __name__ == "__main__":
    only = os.environ.get("PASS7_ONLY", "")
    shots = {
        "lands": still_lands,
        "roll": still_roll,
        "ghosts": still_ghosts,
        "strip": still_strip,
        "speed": still_speed,
        "scrape": still_scrape,
        "tag": still_tag,
        "flash": still_flash,
    }
    if only:
        for name in only.split(","):
            shots[name.strip()]()
    else:
        for fn in shots.values():
            fn()
