#!/usr/bin/env python3
"""Pass 9 stills. Same Hier mesh on the runner and the ghosts, a low diagonal roll, a camera-facing tag burst.

Run:
  blender --background --python Tools/RenderPass9.py
"""
import math
import os
import sys

import bpy
from mathutils import Quaternion, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
import RenderPass7 as p7

OUT = os.environ.get("PASS9_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass9"))
SAMPLES = int(os.environ.get("PASS9_SAMPLES", "16"))
PLAYER = (0.93, 0.34, 0.40, 1)
TAGGER = (0.28, 0.46, 0.82, 1)

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
    scene.eevee.taa_render_samples = SAMPLES
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


def ground_park():
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


def ground_split():
    """Dirt on the left, concrete on the right, so the two sprints sit on different surfaces."""
    bpy.ops.mesh.primitive_plane_add(size=1, location=(-8, 0, 0))
    dirt = bpy.context.active_object
    dirt.name = "Dirt"
    dirt.scale = (16, 24, 1)
    dirt.data.materials.append(noisy(
        "Dirt", (0.42, 0.28, 0.14, 1), (0.62, 0.44, 0.24, 1), 9.0, 0.94))
    bpy.ops.mesh.primitive_plane_add(size=1, location=(8, 0, 0))
    pad = bpy.context.active_object
    pad.name = "ConcreteHalf"
    pad.scale = (16, 24, 1)
    pad.data.materials.append(noisy(
        "Concrete", (0.50, 0.50, 0.48, 1), (0.74, 0.74, 0.71, 1), 22.0, 0.7))


def setup(kind="park"):
    p7.clear()
    world()
    if kind == "split":
        ground_split()
    else:
        ground_park()
    os.makedirs(OUT, exist_ok=True)
    return p7.import_runner()


def shot(name, wide=False, res=None):
    scene = bpy.context.scene
    if res:
        scene.render.resolution_x, scene.render.resolution_y = res
    elif wide:
        scene.render.resolution_x = 1920
        scene.render.resolution_y = 640
    else:
        scene.render.resolution_x = 1280
        scene.render.resolution_y = 720
    path = os.path.join(OUT, name + ".png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("wrote", path)


def kind_of(name):
    n = name.split(".")[0]
    if n.startswith("Base"):
        return "base"
    if "Accent" in n:
        return "accent"
    return "dark"


def unlink(nt, socket):
    if socket is None or not socket.is_linked:
        return
    for link in list(socket.links):
        nt.links.remove(link)


def tint_material(src, alpha, base_color):
    mat = src.copy()
    mat.name = src.name.split(".")[0] + ("_ghost" if alpha is not None else "_body")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    kind = kind_of(src.name)
    color_in = bsdf.inputs.get("Base Color")
    unlink(nt, color_in)
    if kind == "base":
        color_in.default_value = base_color
    elif kind == "accent":
        color_in.default_value = (0.20, 0.75, 0.68, 1)
    if alpha is None:
        bsdf.inputs["Roughness"].default_value = 0.48
        return mat
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    mat.use_backface_culling = False
    alpha_in = bsdf.inputs.get("Alpha")
    unlink(nt, alpha_in)
    alpha_in.default_value = alpha
    emit = bsdf.inputs.get("Emission Color")
    unlink(nt, emit)
    emit.default_value = (1.0, 0.72, 0.70, 1)
    strength = bsdf.inputs.get("Emission Strength")
    unlink(nt, strength)
    bsdf.inputs["Roughness"].default_value = 0.32
    layer = nt.nodes.new("ShaderNodeLayerWeight")
    layer.inputs["Blend"].default_value = 0.28
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = 0.85
    nt.links.new(layer.outputs["Fresnel"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], strength)
    return mat


def tint_hier(arm, alpha=None, base_color=PLAYER):
    """Keep every Hier slot. Skin becomes the player color. Bolts, joints, and the teal accent stay."""
    cache = {}
    for ob in p7.meshes(arm):
        ob.data = ob.data.copy()
        for i, slot in enumerate(ob.material_slots):
            src = slot.material
            if src is None:
                continue
            key = src.name.split(".")[0]
            if key not in cache:
                cache[key] = tint_material(src, alpha, base_color)
            ob.data.materials[i] = cache[key]


def view(target, dist, yaw, height, lens, look_z):
    loc = Vector((
        target.x + math.sin(yaw) * dist,
        target.y - math.cos(yaw) * dist,
        height,
    ))
    p7.cam(loc, Vector((target.x, target.y, look_z)), lens)
    print("view", tuple(round(v, 2) for v in loc))
    return loc


def frame_yaw(arms, extras=(), yaw=math.pi / 2, lens=48, fill=0.5, lift=0.35):
    """Horizontal sensor fit, so a wide strip stays large instead of drowning in sky."""
    scene = bpy.context.scene
    mins, maxs = p7.object_bounds(list(arms) + list(extras))
    center = (mins + maxs) * 0.5
    size = maxs - mins
    span = max(size.y, size.x, 1.2)
    tall = max(size.z, 0.8)
    aspect = scene.render.resolution_x / float(scene.render.resolution_y)
    half_h = math.atan(18.0 / lens)
    half_v = math.atan(math.tan(half_h) / aspect)
    dist_h = (span * 0.5) / math.tan(half_h) / fill
    dist_v = (tall * 0.5) / math.tan(half_v) / fill
    dist = max(dist_h, dist_v, 3.2)
    look = Vector((center.x, center.y, center.z))
    loc = Vector((
        center.x + math.sin(yaw) * dist,
        center.y - math.cos(yaw) * dist,
        center.z + lift,
    ))
    cam = p7.cam(loc, look, lens)
    cam.data.sensor_fit = "HORIZONTAL"
    print("frame", tuple(round(v, 2) for v in mins), tuple(round(v, 2) for v in maxs), "dist", round(dist, 2))
    return loc


def face_travel(arm, base_q):
    arm.rotation_mode = "QUATERNION"
    arm.rotation_quaternion = Quaternion((0.0, 0.0, 1.0), math.pi) @ base_q


def measure(arm, label, allow_stand=False):
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    part = {}
    best = ("", 1e9)
    for ob in p7.meshes(arm):
        ev = ob.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ob.matrix_world
        mz = 1e9
        for v in me.vertices:
            z = (mw @ v.co).z
            if z < mz:
                mz = z
        ev.to_mesh_clear()
        part[ob.name] = mz
        if mz < best[1]:
            best = (ob.name, mz)
    hip = arm.matrix_world @ arm.pose.bones["Hips"].head
    head = arm.matrix_world @ arm.pose.bones["Head"].head
    d = hip - head
    horiz = math.sqrt(d.x * d.x + d.y * d.y)
    elev = math.degrees(math.atan2(d.z, max(horiz, 1e-4)))
    head_m = 9.0
    for name, z in part.items():
        if name.split(".")[0] == "Mesh_Head" and z < head_m:
            head_m = z
    print(
        "metric %s low=%s hip=%.2f head=%.2f elev=%.1f"
        % (label, best[0].replace("Mesh_", ""), hip.z, head_m, elev)
    )
    if hip.z > 0.90:
        raise SystemExit("hip too high on " + label)
    if not allow_stand:
        if head_m <= 0.05:
            raise SystemExit("head on the ground on " + label)
        if elev > 60.0:
            raise SystemExit("inverted on " + label)
        if best[0] == "Mesh_Head":
            raise SystemExit("head is the contact on " + label)
    return hip.z, head_m, elev


def pose_roll(arm, phase):
    """Low diagonal roll. No object spin. Root Z only banks onto the lead shoulder."""
    if phase == 0:
        # Hand plant. The right hand is the contact. Head stays high.
        p7.bone(arm, "Root", 32, 0, 14)
        p7.bone(arm, "Spine", 34, 0, 8)
        p7.bone(arm, "Head", 40, 16, -8)
        p7.bone(arm, "UpperArm_R", -70, 12, -22)
        p7.bone(arm, "LowerArm_R", -18)
        p7.bone(arm, "UpperArm_L", 30, -8, 16)
        p7.bone(arm, "LowerArm_L", -80)
        p7.bone(arm, "UpperLeg_L", 70)
        p7.bone(arm, "UpperLeg_R", 48)
        p7.bone(arm, "LowerLeg_L", -140)
        p7.bone(arm, "LowerLeg_R", -125)
    elif phase == 1:
        # Lead shoulder down. Knees in the chest. Head tucked to the side.
        p7.bone(arm, "Root", 8, 16, 85)
        p7.bone(arm, "Spine", 18, 8, 10)
        p7.bone(arm, "Head", 50, 42, -16)
        p7.bone(arm, "UpperArm_R", -70, 8, -35)
        p7.bone(arm, "LowerArm_R", -95)
        p7.bone(arm, "UpperArm_L", -20, 0, 12)
        p7.bone(arm, "LowerArm_L", -110)
        p7.bone(arm, "UpperLeg_L", -120)
        p7.bone(arm, "UpperLeg_R", -110)
        p7.bone(arm, "LowerLeg_L", -130)
        p7.bone(arm, "LowerLeg_R", -120)
    elif phase == 2:
        # Across the back. Tighter ball, still on the shoulder line, head off the ground.
        p7.bone(arm, "Root", -4, 22, 92)
        p7.bone(arm, "Spine", 16, 10, 18)
        p7.bone(arm, "Chest", 8, 0, 8)
        p7.bone(arm, "Head", 62, 52, -16)
        p7.bone(arm, "UpperArm_R", -25, 4, -12)
        p7.bone(arm, "LowerArm_R", -105)
        p7.bone(arm, "UpperArm_L", -16, 0, 10)
        p7.bone(arm, "LowerArm_L", -100)
        p7.bone(arm, "UpperLeg_L", -120)
        p7.bone(arm, "UpperLeg_R", -110)
        p7.bone(arm, "LowerLeg_L", -130)
        p7.bone(arm, "LowerLeg_R", -120)
    elif phase == 3:
        # Hip. The hip armor is down, one leg opens toward a plant, shoulder lifts off the stack.
        p7.bone(arm, "Root", 6, 8, 58)
        p7.bone(arm, "Spine", 12, 4, 6)
        p7.bone(arm, "Head", 36, 30, -10)
        p7.bone(arm, "UpperArm_R", -24, 0, -8)
        p7.bone(arm, "LowerArm_R", -70)
        p7.bone(arm, "UpperArm_L", -10, 0, 8)
        p7.bone(arm, "LowerArm_L", -60)
        p7.bone(arm, "UpperLeg_L", -70)
        p7.bone(arm, "UpperLeg_R", 15)
        p7.bone(arm, "LowerLeg_L", -100)
        p7.bone(arm, "LowerLeg_R", -35)
    else:
        p7.pose_sprint(arm)


def present_roll(arm, base_q, phase, station):
    p7.reset_arm(arm, base_q)
    pose_roll(arm, phase)
    face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    measure(arm, "roll%d" % phase, allow_stand=(phase == 4))
    # Sit on the travel line without lifting the contact.
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
    arm.location.y += station - best.y
    bpy.context.view_layer.update()


def still_ghosts():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    tint_hier(arm, None, PLAYER)
    p7.reset_arm(arm, base_q)
    p7.pose_sprint(arm)
    p7.drop_to_floor(arm)
    alphas = (0.60, 0.45, 0.30, 0.15)
    phases = (0.55, 0.05, -0.45, -0.9)
    for i in range(4):
        ghost = p7.clone_arm(arm)
        p7.reset_arm(ghost, base_q)
        p7.pose_run(ghost, phases[i])
        tint_hier(ghost, alphas[i], PLAYER)
        p7.drop_to_floor(ghost)
        p7.place(ghost, 0.04 * ((i % 2) * 2 - 1), 0.18 + i * 0.42)
    view(Vector((0.15, 0.7, 0.95)), 6.4, 0.95, 1.45, 50, 0.95)
    shot("dash-ghosts")


def still_roll(name, phase, swirl, yaw):
    arm = setup()
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 720
    base_q = arm.rotation_quaternion.copy()
    tint_hier(arm, None, PLAYER)
    present_roll(arm, base_q, phase, 0.0)
    extras = []
    if swirl:
        for i, frame_i in enumerate((1, 3, 5)):
            extras.append(p7.puff((0.05, -0.2 - i * 0.28, 0.08), frame_i, 0.28))
    frame_yaw([arm], extras, yaw=yaw, lens=50, fill=0.48, lift=0.28)
    shot(name)


def still_strip(name, yaw):
    arm = setup()
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 720
    base_q = arm.rotation_quaternion.copy()
    tint_hier(arm, None, PLAYER)
    arms = []
    for i in range(5):
        guy = arm if i == 0 else p7.clone_arm(arm)
        if i > 0:
            tint_hier(guy, None, PLAYER)
        present_roll(guy, base_q, i, -3.2 + i * 1.6)
        arms.append(guy)
    extras = [p7.puff((arms[2].location.x, arms[2].location.y - 0.15, 0.08), 4, 0.26)]
    frame_yaw(arms, extras, yaw=yaw, lens=42, fill=0.78, lift=0.22)
    shot(name, res=(1920, 720))


def disk(name, origin, radius, x, color, strength):
    bpy.ops.mesh.primitive_circle_add(vertices=12, radius=radius, fill_type="NGON", location=origin)
    ob = bpy.context.active_object
    ob.rotation_euler = (0.0, math.radians(90), 0.0)
    ob.location = (origin[0] + x, origin[1], origin[2])
    ob.data.materials.append(p7.emissive(name, color, strength))
    return ob


def star_mesh(name, scale, x):
    verts = [(x, 0.0, 0.0)]
    for i in range(12):
        ang_t = (i + 0.5) / 12.0 * math.tau
        ang_v = (i + 1.0) / 12.0 * math.tau
        rt = TIPS[i] * scale
        rv = VALLEYS[i] * scale
        verts.append((x, math.cos(ang_t) * rt, math.sin(ang_t) * rt))
        verts.append((x, math.cos(ang_v) * rv, math.sin(ang_v) * rv))
    faces = []
    n = 24
    for i in range(n):
        faces.append((0, 1 + i, 1 + ((i + 1) % n)))
        faces.append((0, 1 + ((i + 1) % n), 1 + i))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    return ob


def spike_mesh(origin, ang, length, width, x, color):
    c = math.cos(ang)
    s = math.sin(ang)
    along = Vector((0.0, c, s))
    side = Vector((0.0, -s, c))
    a = Vector((0.0, 0.0, 0.0))
    b = along * length
    pts = [
        tuple(a + side * (width * 0.5)),
        tuple(a - side * (width * 0.5)),
        tuple(b),
    ]
    mesh = bpy.data.meshes.new("Spike")
    mesh.from_pydata(pts, [], [(0, 1, 2), (0, 2, 1)])
    mesh.update()
    ob = bpy.data.objects.new("Spike", mesh)
    bpy.context.collection.objects.link(ob)
    ob.location = (origin[0] + x, origin[1], origin[2])
    ob.data.materials.append(p7.emissive("SpikeMat", color, 0.7))
    return ob


def still_tag():
    arm = setup()
    base_q = arm.rotation_quaternion.copy()
    tint_hier(arm, None, PLAYER)
    runner = p7.clone_arm(arm)
    tint_hier(runner, None, PLAYER)
    tint_hier(arm, None, TAGGER)
    p7.reset_arm(arm, base_q)
    p7.pose_punch(arm)
    face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    p7.reset_arm(runner, base_q)
    p7.pose_sprint(runner)
    p7.bone(runner, "Spine", 16, -12, 0)
    p7.bone(runner, "Head", 8, -10, 0)
    face_travel(runner, base_q)
    p7.drop_to_floor(runner)
    p7.place(runner, 0.0, 0.85)
    bpy.context.view_layer.update()
    fist = p7.bone_point(arm, "Hand_R")
    shoulder = p7.bone_point(runner, "Shoulder_R")
    # Slide the tagger on the ground so the fist meets the runner's shoulder.
    arm.location.x += shoulder.x - fist.x
    arm.location.y += shoulder.y - fist.y
    arm.location.z += shoulder.z - fist.z
    # Keep the feet on the floor if the fist was above the shoulder.
    if arm.location.z < 0:
        arm.location.z = 0
    bpy.context.view_layer.update()
    fist = p7.bone_point(arm, "Hand_R")
    shoulder = p7.bone_point(runner, "Shoulder_R")
    contact = (fist + shoulder) * 0.5
    print("contact", tuple(round(v, 3) for v in contact), "gap", round((fist - shoulder).length, 3))
    # Star in the YZ plane (normal +X) so the side camera sees the whole burst.
    # Outline radius is about 0.45 m. Framed to land near a quarter of the picture height.
    pieces = []
    pieces.append(star_mesh("Outline", 0.28, 0.0))
    pieces[-1].location = contact
    pieces[-1].data.materials.append(p7.emissive("OutlineMat", (0.05, 0.04, 0.04, 1), 1.3))
    fill = star_mesh("Fill", 0.23, 0.02)
    fill.location = contact
    fill.data.materials.append(p7.emissive("FillMat", (1.0, 0.55, 0.08, 1), 1.6))
    pieces.append(fill)
    flash = star_mesh("Flash", 0.11, 0.04)
    flash.location = contact
    flash.data.materials.append(p7.emissive("FlashMat", (1.0, 0.98, 0.92, 1), 1.8))
    pieces.append(flash)
    for i in range(8):
        ang = (i + 0.5) / 8.0 * math.tau
        pieces.append(spike_mesh(
            contact, ang, 0.34 + (i % 2) * 0.08, 0.028, 0.03, (0.10, 0.05, 0.04, 1)))
    for ix in range(-2, 3):
        for iz in range(-2, 3):
            if (ix + iz) % 2 != 0:
                continue
            if ix * ix + iz * iz > 5:
                continue
            pieces.append(disk(
                "Dot",
                (contact.x, contact.y + ix * 0.04, contact.z + iz * 0.04),
                0.010,
                0.05,
                (0.45, 0.10, 0.08, 1),
                0.8,
            ))
    # Side camera, tight enough that the burst is about a quarter of the frame.
    target = Vector((contact.x, contact.y, contact.z))
    view(target, 4.6, math.pi / 2, contact.z + 0.15, 48, contact.z)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    shot("tag-burst")


def tinted_puff(loc, frame, size, color, emit):
    ob = p7.puff(loc, frame, size)
    mat = ob.data.materials[0]
    for node in mat.node_tree.nodes:
        if node.type == "EMISSION":
            node.inputs["Color"].default_value = (color[0], color[1], color[2], 1)
            node.inputs["Strength"].default_value = emit
    return ob


def still_dust():
    """Sprint on dirt and on concrete. Dirt puffs are larger, browner, and more of them."""
    arm = setup("split")
    base_q = arm.rotation_quaternion.copy()
    tint_hier(arm, None, PLAYER)
    other = p7.clone_arm(arm)
    tint_hier(other, None, TAGGER)
    p7.reset_arm(arm, base_q)
    p7.pose_sprint(arm)
    p7.drop_to_floor(arm)
    p7.place(arm, -2.15, 0.0)
    p7.reset_arm(other, base_q)
    p7.pose_sprint(other)
    p7.bone(other, "UpperLeg_L", -42)
    p7.bone(other, "UpperLeg_R", 48)
    p7.drop_to_floor(other)
    p7.place(other, 2.15, 0.0)
    extras = []
    # Facing -Y, so the trail sits toward +Y, at the feet.
    dirt_n = 11
    for i in range(dirt_n):
        extras.append(tinted_puff(
            (-2.15 + ((i % 3) - 1) * 0.16, 0.35 + i * 0.22, 0.12 + (i % 2) * 0.05),
            i % 8,
            0.39,
            (0.76, 0.55, 0.30),
            1.35,
        ))
    for i in range(4):
        extras.append(tinted_puff(
            (2.15 + ((i % 2) * 2 - 1) * 0.08, 0.40 + i * 0.28, 0.08),
            i % 8,
            0.15,
            (0.82, 0.82, 0.80),
            0.55,
        ))
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 720
    frame_yaw([arm, other], extras, yaw=0.55, lens=40, fill=0.62, lift=0.4)
    shot("running-dust", res=(1600, 720))


if __name__ == "__main__":
    only = os.environ.get("PASS9_ONLY", "")
    shots = {
        "ghosts": still_ghosts,
        "swirl": lambda: still_roll("roll-swirl", 2, True, math.pi / 2),
        "strip": lambda: still_strip("roll-strip-side", math.pi / 2),
        "strip3": lambda: still_strip("roll-strip-three-quarter", 1.05),
        "tag": still_tag,
        "dust": still_dust,
    }
    if only:
        for name in only.split(","):
            shots[name.strip()]()
    else:
        for fn in shots.values():
            fn()
