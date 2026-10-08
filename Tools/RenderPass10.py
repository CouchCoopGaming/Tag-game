#!/usr/bin/env python3
"""Pass 10 stills. Same Hier mesh on the solid and the ghosts, a surface tag, readable dust, and the verb FX.

Run:
  blender --background --python Tools/RenderPass10.py
"""
import math
import os
import sys

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
import RenderPass7 as p7
import RenderPass9 as p9

OUT = os.environ.get("PASS10_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass10"))
p9.OUT = OUT
p9.SAMPLES = int(os.environ.get("PASS10_SAMPLES", "16"))

# DustLook at sprint (t=1) and walk (t=0), times the surface multipliers.
# Dirt sprint size is 0.39. The still draws that puff as a soft cloud, up to about 0.7 m
# once it has billowed, which is the read the numbers were always aiming at.
DIRT = (0.76, 0.55, 0.30)
CONCRETE = (0.78, 0.78, 0.76)
GRASS_FLECK = (0.45, 0.55, 0.22)


def hier_names(arm):
    found = {}
    for ob in p7.meshes(arm):
        found[ob.name.split(".")[0]] = len(ob.data.vertices)
    return found


def assert_same_hier(solid, ghost):
    a = hier_names(solid)
    b = hier_names(ghost)
    if a != b:
        raise SystemExit("solid and ghost meshes differ: %s vs %s" % (a, b))
    if "Mesh_Head" not in a or a["Mesh_Head"] < 1000:
        raise SystemExit("head mesh is missing")
    print("hier-match head-verts", a["Mesh_Head"], "pieces", len(a))


def head_slot_colors(arm, label):
    for ob in p7.meshes(arm):
        if ob.name.split(".")[0] != "Mesh_Head":
            continue
        for slot in ob.material_slots:
            mat = slot.material
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            col = bsdf.inputs["Base Color"].default_value
            print(label, mat.name, tuple(round(c, 3) for c in col))


def still_ghosts():
    """Front three-quarter, close enough that the face sensors read on the solid and the ghosts."""
    arm = p9.setup()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
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
        p9.tint_hier(ghost, alphas[i], p9.PLAYER)
        p7.drop_to_floor(ghost)
        # Behind the facing, overlapping, so the nearest ghost's head sits beside the solid.
        p7.place(ghost, 0.08 * ((i % 2) * 2 - 1), 0.28 + i * 0.38)
        ghosts.append(ghost)
    assert_same_hier(arm, ghosts[0])
    head_slot_colors(arm, "solid")
    head_slot_colors(ghosts[0], "ghost")
    # The bolts and sensors face up from the crown. A high three-quarter sees them on the solid.
    p9.view(Vector((0.0, 0.35, 1.15)), 3.15, -0.62, 2.45, 48, 1.15)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    p9.shot("dash-ghosts")


def samples(arm, prefixes, step):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    pts = []
    for ob in p7.meshes(arm):
        key = ob.name.split(".")[0]
        if not any(key.startswith(p) for p in prefixes):
            continue
        ev = ob.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ob.matrix_world
        for i, v in enumerate(me.vertices):
            if i % step == 0:
                pts.append(mw @ v.co)
        ev.to_mesh_clear()
    return pts


def min_sep(a, b):
    best = 1e9
    for p in a:
        for q in b:
            d = (p - q).length
            if d < best:
                best = d
    return best


def seat_fist(tagger, runner):
    """Walk the fist onto the shoulder cap and stop before the forearm enters the chest."""
    gap = 0.0
    touch = 1.0
    for _ in range(36):
        armpts = samples(tagger, ("Mesh_LowerArm_R", "Mesh_Hand_R"), 2)
        body = samples(runner, ("Mesh_Chest", "Mesh_Spine"), 4)
        hand = samples(tagger, ("Mesh_Hand_R",), 2)
        cap = samples(runner, ("Mesh_Shoulder_R",), 2)
        gap = min_sep(armpts, body)
        touch = min_sep(hand, cap)
        print("arm-gap", round(gap, 3), "touch", round(touch, 3))
        if gap >= 0.04 and 0.02 <= touch <= 0.07:
            return gap
        hc = Vector((0, 0, 0))
        for p in hand:
            hc += p
        hc /= max(len(hand), 1)
        nearest = min(cap, key=lambda p: (Vector((p.x, p.y, 0)) - Vector((hc.x, hc.y, 0))).length)
        delta = Vector((nearest.x - hc.x, nearest.y - hc.y, 0.0))
        if delta.length < 1e-4:
            delta = Vector((0.2, -0.2, 0.0))
        d = delta.normalized()
        if gap < 0.04:
            tagger.location.x -= d.x * 0.02
            tagger.location.y -= d.y * 0.02
        else:
            step = min(0.10, max(0.015, delta.length * 0.4))
            tagger.location.x += d.x * step
            tagger.location.y += d.y * step
        bpy.context.view_layer.update()
    raise SystemExit("fist did not sit on the shoulder (gap %.3f touch %.3f)" % (gap, touch))


def still_tag():
    arm = p9.setup()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    runner = p7.clone_arm(arm)
    p9.tint_hier(runner, None, p9.PLAYER)
    p9.tint_hier(arm, None, p9.TAGGER)
    p7.reset_arm(arm, base_q)
    p7.pose_punch(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    p7.reset_arm(runner, base_q)
    p7.pose_sprint(runner)
    p7.bone(runner, "Spine", 14, -8, 0)
    p7.bone(runner, "Head", 6, -6, 0)
    p9.face_travel(runner, base_q)
    p7.drop_to_floor(runner)
    # Runner ahead. Tagger behind and a little to the camera side, fist reaching the back.
    p7.place(runner, 0.15, 0.95)
    # Off to the camera side, so the punch reaches the shoulder from outside the chest.
    p7.place(arm, 0.72, 0.25)
    gap = seat_fist(arm, runner)
    if gap < 0.03:
        raise SystemExit("fist still inside the runner")
    bpy.context.view_layer.update()
    fist = p7.bone_point(arm, "Hand_R")
    shoulder = p7.bone_point(runner, "Shoulder_R")
    head = p7.bone_point(runner, "Head")
    # Burst on the contact, pushed off the face toward the shoulder and the camera.
    contact = (fist + shoulder) * 0.5
    away = Vector((fist.x - head.x, fist.y - head.y, 0.0))
    if away.length < 0.001:
        away = Vector((0.2, -0.2, 0.0))
    away.normalize()
    contact = contact + away * 0.16 + Vector((0.10, 0.0, -0.12))
    if contact.z > shoulder.z + 0.02:
        contact.z = shoulder.z + 0.02
    print("burst", tuple(round(v, 3) for v in contact), "head-z", round(head.z, 3))
    pieces = []
    outline = p9.star_mesh("Outline", 0.28, 0.0)
    outline.location = contact
    outline.data.materials.append(p7.emissive("OutlineMat", (0.05, 0.04, 0.04, 1), 1.3))
    pieces.append(outline)
    fill = p9.star_mesh("Fill", 0.23, 0.02)
    fill.location = contact
    fill.data.materials.append(p7.emissive("FillMat", (1.0, 0.55, 0.08, 1), 1.6))
    pieces.append(fill)
    flash = p9.star_mesh("Flash", 0.11, 0.04)
    flash.location = contact
    flash.data.materials.append(p7.emissive("FlashMat", (1.0, 0.98, 0.92, 1), 1.8))
    pieces.append(flash)
    for i in range(8):
        ang = (i + 0.5) / 8.0 * math.tau
        pieces.append(p9.spike_mesh(
            contact, ang, 0.34 + (i % 2) * 0.08, 0.028, 0.03, (0.10, 0.05, 0.04, 1)))
    for ix in range(-2, 3):
        for iz in range(-2, 3):
            if (ix + iz) % 2 != 0:
                continue
            if ix * ix + iz * iz > 5:
                continue
            pieces.append(p9.disk(
                "Dot",
                (contact.x, contact.y + ix * 0.04, contact.z + iz * 0.04),
                0.010,
                0.05,
                (0.45, 0.10, 0.08, 1),
                0.8,
            ))
    target = Vector((contact.x, contact.y, shoulder.z))
    p9.view(target, 4.5, math.pi / 2, shoulder.z, 48, shoulder.z)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    p9.shot("tag-burst")


def cloud_mat(name, color, alpha):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    mat.use_backface_culling = False
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    grad = nt.nodes.new("ShaderNodeTexGradient")
    grad.gradient_type = "SPHERICAL"
    mapping = nt.nodes.new("ShaderNodeMapping")
    coord = nt.nodes.new("ShaderNodeTexCoord")
    mapping.inputs["Scale"].default_value = (1.6, 1.6, 1.6)
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.15
    ramp.color_ramp.elements[0].color = (1, 1, 1, 1)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (0, 0, 0, 1)
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 4.5
    noise.inputs["Detail"].default_value = 4.0
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = 0.85
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (color[0], color[1], color[2], 1)
    em.inputs["Strength"].default_value = 1.15
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    fac = nt.nodes.new("ShaderNodeMath")
    fac.operation = "MULTIPLY"
    fac.inputs[1].default_value = alpha
    nt.links.new(coord.outputs["Object"], mapping.inputs["Vector"])
    nt.links.new(mapping.outputs["Vector"], grad.inputs["Vector"])
    nt.links.new(grad.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], mul.inputs[0])
    nt.links.new(noise.outputs["Fac"], mul.inputs[1])
    nt.links.new(mul.outputs["Value"], fac.inputs[0])
    nt.links.new(fac.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(em.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def soft_disc(loc, diameter, mat, tilt=0.4):
    bpy.ops.mesh.primitive_circle_add(
        vertices=20, radius=diameter * 0.5, fill_type="NGON", location=loc)
    ob = bpy.context.active_object
    ob.rotation_euler = (math.radians(72), 0.0, tilt)
    ob.data.materials.append(mat)
    return ob


def billow(origin, diameter, color, alpha, tilt):
    """A foot puff: a bright core and two softer lobes, so it reads as a cloud."""
    mat = cloud_mat("Cloud", color, alpha)
    soft_disc(origin, diameter, mat, tilt)
    soft_disc(
        (origin[0] + diameter * 0.18, origin[1] + 0.04, origin[2] + diameter * 0.12),
        diameter * 0.72,
        mat,
        tilt + 0.3,
    )
    soft_disc(
        (origin[0] - diameter * 0.14, origin[1] - 0.02, origin[2] + diameter * 0.05),
        diameter * 0.58,
        mat,
        tilt - 0.2,
    )


def fleck(loc, size, color):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=size, location=loc)
    ob = bpy.context.active_object
    ob.data.materials.append(p7.emissive("Fleck", (color[0], color[1], color[2], 1), 0.6))
    return ob


def still_dust():
    """Three panels, one camera. Sprint dirt, sprint concrete, then walk beside sprint on dirt."""
    arm = p9.setup("split")
    # Replace the half-and-half ground with three pads the camera can read.
    for ob in list(bpy.data.objects):
        if ob.name in ("Dirt", "ConcreteHalf"):
            bpy.data.objects.remove(ob, do_unlink=True)
    pads = (
        ("DirtA", -4.15, DIRT, 2.5),
        ("Concrete", 0.0, CONCRETE, 2.3),
        ("DirtB", 4.15, DIRT, 2.7),
    )
    for name, x, col, half in pads:
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, 0.2, 0.0))
        pad = bpy.context.active_object
        pad.name = name
        pad.scale = (half, 6.5, 1)
        dark = (col[0] * 0.55, col[1] * 0.5, col[2] * 0.45, 1)
        light = (min(col[0] * 1.15, 1), min(col[1] * 1.1, 1), min(col[2] * 1.05, 1), 1)
        if name == "Concrete":
            dark = (0.45, 0.45, 0.43, 1)
            light = (0.70, 0.70, 0.68, 1)
        else:
            dark = (0.38, 0.24, 0.12, 1)
            light = (0.58, 0.40, 0.20, 1)
        pad.data.materials.append(p9.noisy(name + "Mat", dark, light, 8.0, 0.9))

    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)

    runners = []

    def make(x, sprint, swing):
        guy = arm if not runners else p7.clone_arm(arm)
        if runners:
            p9.tint_hier(guy, None, p9.PLAYER)
        p7.reset_arm(guy, base_q)
        if sprint:
            p7.pose_sprint(guy)
        else:
            p7.pose_run(guy, swing)
            p7.bone(guy, "Root", 8, 0, 0)
            p7.bone(guy, "Spine", 10)
        p7.drop_to_floor(guy)
        p7.place(guy, x, 0.0)
        runners.append(guy)
        return guy

    dirt_sprint = make(-4.15, True, 0)
    conc_sprint = make(0.0, True, 0)
    p7.bone(conc_sprint, "UpperLeg_L", -40)
    p7.bone(conc_sprint, "UpperLeg_R", 46)
    walk = make(3.15, False, 0.45)
    dirt_fast = make(5.15, True, 0)

    def foot_puffs(guy, diameters, color, alphas, count, back):
        foot_l = p7.bone_point(guy, "Foot_L")
        foot_r = p7.bone_point(guy, "Foot_R")
        for i in range(count):
            foot = foot_l if i % 2 == 0 else foot_r
            # Trail behind the facing (-Y is forward, so the trail is +Y), aging as it goes.
            y = foot.y + 0.12 + (i // 2) * back
            z = 0.08 + diameters[i % len(diameters)] * 0.22
            x = foot.x + ((i % 3) - 1) * 0.06
            billow((x, y, z), diameters[i % len(diameters)], color, alphas[i % len(alphas)], 0.2 + i * 0.15)

    # Sprint dirt: thick warm clouds, about 0.45 to 0.75 m, several ages.
    foot_puffs(
        dirt_sprint,
        (0.48, 0.66, 0.74, 0.42),
        DIRT,
        (0.62, 0.40, 0.22, 0.50),
        8,
        0.42,
    )
    # Sprint concrete: small, pale, short.
    foot_puffs(
        conc_sprint,
        (0.16, 0.22, 0.14),
        CONCRETE,
        (0.28, 0.16, 0.10),
        4,
        0.28,
    )
    # Walk on dirt: a faint small puff. Sprint beside it is the same cloud as the first panel.
    foot_puffs(walk, (0.16, 0.22), DIRT, (0.22, 0.12), 3, 0.30)
    foot_puffs(
        dirt_fast,
        (0.48, 0.66, 0.74),
        DIRT,
        (0.62, 0.40, 0.22),
        6,
        0.40,
    )
    # A few grass flecks at the edge of the dirt, no cloud.
    for i in range(5):
        fleck((-5.2 + i * 0.18, 1.4, 0.04), 0.025, GRASS_FLECK)

    bpy.context.scene.render.resolution_x = 2100
    bpy.context.scene.render.resolution_y = 780
    p9.frame_yaw(runners, yaw=0.42, lens=32, fill=0.86, lift=0.55)
    p9.shot("running-dust", res=(2100, 780))


def torus(loc, major, minor, color, strength):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major, minor_radius=minor, location=loc, major_segments=48, minor_segments=8)
    ob = bpy.context.active_object
    ob.data.materials.append(p7.emissive("Ring", color, strength))
    return ob


def chunk(loc, size, color):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=size, location=loc)
    ob = bpy.context.active_object
    ob.data.materials.append(p7.emissive("Bit", color, 0.7))
    return ob


def still_land():
    """Hard landing. Ring radius matches landRing 1.30. Twelve debris bits."""
    arm = p9.setup()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p7.pose_land(arm, "heavy")
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    ring = torus((0.0, 0.15, 0.03), 1.30, 0.045, (0.85, 0.72, 0.48, 1), 1.1)
    bits = []
    for i in range(12):
        ang = i / 12.0 * math.tau
        rad = 0.35 + (i % 3) * 0.28
        z = 0.08 + (i % 4) * 0.12
        bits.append(chunk(
            (math.cos(ang) * rad, 0.1 + math.sin(ang) * rad, z),
            0.045 + (i % 3) * 0.02,
            (0.62, 0.42, 0.22, 1) if i % 2 == 0 else (0.55, 0.5, 0.42, 1),
        ))
    p9.frame_yaw([arm], [ring] + bits, yaw=1.05, lens=42, fill=0.62, lift=0.45)
    p9.shot("landing-ring")


def ribbon(a, b, width, color):
    a = Vector(a)
    b = Vector(b)
    d = b - a
    d.z = 0
    if d.length < 0.001:
        d = Vector((0, 1, 0))
    side = Vector((-d.y, d.x, 0)).normalized() * (width * 0.5)
    verts = [
        tuple(a + side), tuple(a - side), tuple(b - side), tuple(b + side),
    ]
    mesh = bpy.data.meshes.new("Ribbon")
    mesh.from_pydata(verts, [], [(0, 1, 2), (0, 2, 3)])
    mesh.update()
    ob = bpy.data.objects.new("Ribbon", mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(p7.emissive("RibbonMat", color, 0.8))
    return ob


def still_slide():
    arm = p9.setup()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p7.bone(arm, "Root", 82, 0, 0)
    p7.bone(arm, "Hips", 36, 0, 0)
    p7.bone(arm, "Spine", 28)
    p7.bone(arm, "Head", 8)
    p7.bone(arm, "UpperLeg_L", -18)
    p7.bone(arm, "UpperLeg_R", 36)
    p7.bone(arm, "LowerLeg_L", -22)
    p7.bone(arm, "LowerLeg_R", -48)
    p7.bone(arm, "UpperArm_L", -36)
    p7.bone(arm, "UpperArm_R", 28)
    p7.bone(arm, "LowerArm_L", -20)
    p7.bone(arm, "LowerArm_R", -16)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    bpy.context.view_layer.update()
    hip = p7.bone_point(arm, "Hips")
    print("slide-hip", round(hip.z, 3))
    foot_l = p7.bone_point(arm, "Foot_L")
    foot_r = p7.bone_point(arm, "Foot_R")
    # Facing +Y after the turn, so the scrape trails toward -Y. Width is the live 0.18 m.
    ribbons = []
    for foot in (foot_l, foot_r):
        start = (foot.x, foot.y - 0.05, 0.04)
        end = (foot.x, foot.y - 1.35, 0.03)
        ribbons.append(ribbon(start, end, 0.18, (0.72, 0.62, 0.48, 0.9)))
    p9.frame_yaw([arm], ribbons, yaw=1.15, lens=46, fill=0.58, lift=0.35)
    p9.shot("slide-scrape")


def pose_rise(arm):
    p7.bone(arm, "Root", -8, 0, 0)
    p7.bone(arm, "Spine", -6)
    p7.bone(arm, "Head", 4)
    p7.bone(arm, "UpperLeg_L", 28)
    p7.bone(arm, "UpperLeg_R", -18)
    p7.bone(arm, "LowerLeg_L", -40)
    p7.bone(arm, "LowerLeg_R", -24)
    p7.bone(arm, "UpperArm_L", -20)
    p7.bone(arm, "UpperArm_R", 16)
    p7.bone(arm, "LowerArm_L", -16)
    p7.bone(arm, "LowerArm_R", -12)


def still_pad():
    arm = p9.setup()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_rise(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 1.35
    bpy.ops.mesh.primitive_cylinder_add(radius=1.35, depth=0.12, location=(0.0, 0.0, 0.06))
    pad = bpy.context.active_object
    pad.data.materials.append(p7.principled("PadBody", (0.25, 0.55, 0.62, 1), 0.4))
    ring = torus((0.0, 0.0, 0.14), 1.15, 0.035, (0.45, 0.90, 1.0, 1), 1.6)
    streak = ribbon((0.05, -0.15, 0.3), (0.05, -0.15, 2.4), 0.16, (0.55, 0.92, 1.0, 0.85))
    bpy.context.view_layer.update()
    p9.frame_yaw([arm], [pad, ring, streak], yaw=0.9, lens=40, fill=0.55, lift=0.2)
    p9.shot("launch-pad")


def pose_hang(arm):
    p7.bone(arm, "UpperArm_L", -165, 8, 10)
    p7.bone(arm, "UpperArm_R", -165, -8, -10)
    p7.bone(arm, "LowerArm_L", -18)
    p7.bone(arm, "LowerArm_R", -18)
    p7.bone(arm, "Spine", 8)
    p7.bone(arm, "Head", 12)
    p7.bone(arm, "UpperLeg_L", 12)
    p7.bone(arm, "UpperLeg_R", -8)
    p7.bone(arm, "LowerLeg_L", -16)
    p7.bone(arm, "LowerLeg_R", -12)


def still_zip():
    arm = p9.setup()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_hang(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    # Hands up to the cable. Sag at the middle is the live 0.43 m.
    arm.location.z += 1.05
    bpy.context.view_layer.update()
    hand = p7.bone_point(arm, "Hand_R")
    y0, y1 = -2.4, 2.6
    z0, z1 = hand.z + 0.15, hand.z + 0.05
    sag = 0.43
    pts = []
    for i in range(9):
        t = i / 8.0
        y = y0 + (y1 - y0) * t
        z = z0 + (z1 - z0) * t - sag * 4 * t * (1 - t)
        pts.append(Vector((0.0, y, z)))
    pieces = []
    for i in range(len(pts) - 1):
        pieces.append(ribbon(pts[i], pts[i + 1], 0.04, (0.55, 0.32, 0.72, 1)))
    # Eight sparks, the live count at zip pace.
    for i in range(8):
        t = (i + 0.5) / 8.0
        y = y0 + (y1 - y0) * t
        z = z0 + (z1 - z0) * t - sag * 4 * t * (1 - t)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.05, location=(0.08, y, z - 0.08))
        spark = bpy.context.active_object
        spark.data.materials.append(p7.emissive("Spark", (0.82, 0.45, 1.0, 1), 2.4))
        pieces.append(spark)
    for y, z in ((y0, z0), (y1, z1)):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=z + 0.2, location=(0.0, y, (z + 0.2) * 0.5))
        post = bpy.context.active_object
        post.data.materials.append(p7.principled("Post", (0.35, 0.36, 0.38, 1), 0.55))
        pieces.append(post)
    p9.frame_yaw([arm], pieces, yaw=1.2, lens=38, fill=0.62, lift=0.15)
    p9.shot("zip-line")


if __name__ == "__main__":
    only = os.environ.get("PASS10_ONLY", "")
    shots = {
        "ghosts": still_ghosts,
        "tag": still_tag,
        "dust": still_dust,
        "land": still_land,
        "slide": still_slide,
        "pad": still_pad,
        "zip": still_zip,
    }
    os.makedirs(OUT, exist_ok=True)
    if only:
        for name in only.split(","):
            shots[name.strip()]()
    else:
        for fn in shots.values():
            fn()
