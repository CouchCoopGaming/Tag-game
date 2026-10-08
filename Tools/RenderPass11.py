#!/usr/bin/env python3
"""Pass 11 stills. Filled dust clouds, a deep absorb, a pad burst, and the park behind them.

Run:
  blender --background --python Tools/RenderPass11.py
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
import RenderPass10 as p10

OUT = os.environ.get("PASS11_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass11"))
p9.OUT = OUT
p9.SAMPLES = int(os.environ.get("PASS11_SAMPLES", "16"))

DIRT = (0.74, 0.50, 0.28, 1)
CONCRETE = (0.80, 0.80, 0.78, 1)
FONT = os.path.join(ROOT, "Assets", "Art", "FX", "Fonts", "Bangers-Regular.ttf")


def post():
    """Standard view, no glare stack. Alpha cards stay the color we give them."""
    scene = bpy.context.scene
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    scene.render.film_transparent = False
    scene.use_nodes = False
    if hasattr(scene.eevee, "use_bloom"):
        scene.eevee.use_bloom = False
    if hasattr(scene.eevee, "use_ssr"):
        scene.eevee.use_ssr = False


def hash2(ix, iy):
    n = (ix * 374761393 + iy * 668265263) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return (n & 65535) / 65535.0


def value_noise(u, v, scale):
    x = u * scale
    y = v * scale
    x0 = int(math.floor(x))
    y0 = int(math.floor(y))
    fx = x - x0
    fy = y - y0
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)

    def at(i, j):
        return hash2(i, j)

    return (
        at(x0, y0) * (1 - fx) * (1 - fy)
        + at(x0 + 1, y0) * fx * (1 - fy)
        + at(x0, y0 + 1) * (1 - fx) * fy
        + at(x0 + 1, y0 + 1) * fx * fy
    )


def smooth(a, b, x):
    if x <= a:
        return 0.0
    if x >= b:
        return 1.0
    t = (x - a) / (b - a)
    return t * t * (3 - 2 * t)


def build_cloud():
    """Filled soft blob. Center alpha stays near 0.75. The edge fades. It is not a ring."""
    w = 160
    img = bpy.data.images.new("PuffCloud", w, w, alpha=True, float_buffer=True)
    pix = [0.0] * (w * w * 4)
    center_a = 0.0
    ring_a = 0.0
    ring_n = 0
    for y in range(w):
        for x in range(w):
            u = x / (w - 1)
            v = y / (w - 1)
            dx = u - 0.5
            dy = v - 0.5
            d = math.sqrt(dx * dx + dy * dy) / 0.5
            n = value_noise(u, v, 5.0)
            n2 = value_noise(u + 3.1, v + 1.7, 9.0)
            # Radius wobble so the silhouette is a cloud, not a disc or a ring.
            edge = d * (1.08 - 0.22 * n2)
            fade = 1.0 - smooth(0.35, 1.02, edge)
            core = 1.0 - smooth(0.0, 0.85, d)
            a = (0.62 * core + 0.38 * fade) * (0.88 + 0.12 * n)
            if d < 0.28:
                a = max(a, 0.72)
            if a < 0:
                a = 0.0
            if a > 0.82:
                a = 0.82
            i = (y * w + x) * 4
            pix[i] = 1.0
            pix[i + 1] = 1.0
            pix[i + 2] = 1.0
            pix[i + 3] = a
            if d < 0.12:
                center_a += a
            if 0.55 < d < 0.75:
                ring_a += a
                ring_n += 1
    img.pixels.foreach_set(pix)
    img.pack()
    center_a /= max(1, int(3.1416 * (0.12 * w) ** 2 / 4))
    # The loop above counts every center pixel, so recompute properly.
    csum = nsum = 0
    rsum = rcount = 0
    for y in range(w):
        for x in range(w):
            d = math.sqrt((x / (w - 1) - 0.5) ** 2 + (y / (w - 1) - 0.5) ** 2) / 0.5
            a = pix[(y * w + x) * 4 + 3]
            if d < 0.15:
                csum += a
                nsum += 1
            elif 0.62 < d < 0.82:
                rsum += a
                rcount += 1
    center = csum / max(1, nsum)
    ring = rsum / max(1, rcount)
    print("cloud-alpha center", round(center, 3), "mid-edge", round(ring, 3))
    if center < 0.65 or center < ring + 0.25:
        raise SystemExit("cloud texture is hollow")
    return img


CLOUD = None


def cloud_image():
    global CLOUD
    try:
        if CLOUD is not None and CLOUD.name in bpy.data.images:
            return CLOUD
    except ReferenceError:
        pass
    CLOUD = build_cloud()
    return CLOUD


def cloud_mat(name, color, alpha_scale):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    mat.use_backface_culling = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = cloud_image()
    tex.interpolation = "Linear"
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = alpha_scale
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], bsdf.inputs["Alpha"])
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 1.0
    spec = bsdf.inputs.get("Specular IOR Level") or bsdf.inputs.get("Specular")
    if spec is not None:
        spec.default_value = 0.0
    return mat


def card(loc, diameter, mat):
    bpy.ops.mesh.primitive_plane_add(size=diameter, location=loc)
    ob = bpy.context.active_object
    ob.data.materials.append(mat)
    ob.visible_shadow = False
    return ob


def aim(cards):
    cam = bpy.context.scene.camera
    for ob in cards:
        direction = cam.location - ob.location
        ob.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()


def dress():
    """A few park shapes so the shot is an arena, not an empty plane."""
    spots = ((-4.6, 3.2), (5.2, -2.4), (-3.8, -4.6), (6.0, 4.4), (2.2, 5.5))
    for i, (x, y) in enumerate(spots):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=1.3, location=(x, y, 0.65))
        trunk = bpy.context.active_object
        trunk.data.materials.append(p7.principled("Trunk", (0.35, 0.24, 0.14, 1), 0.8))
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.85, location=(x, y, 1.7))
        leaf = bpy.context.active_object
        green = (0.16 + (i % 3) * 0.04, 0.42, 0.16, 1)
        leaf.data.materials.append(p7.principled("Leaf", green, 0.9))
    bpy.ops.mesh.primitive_cube_add(size=1, location=(6.5, 1.2, 0.45))
    block = bpy.context.active_object
    block.scale = (1.4, 2.2, 0.9)
    block.data.materials.append(p7.principled("Fort", (0.55, 0.42, 0.32, 1), 0.75))
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-5.5, 0.4, 0.28))
    ramp = bpy.context.active_object
    ramp.scale = (1.6, 2.4, 0.28)
    ramp.rotation_euler = (0.0, math.radians(-16), 0.2)
    ramp.data.materials.append(p7.principled("Ramp", (0.62, 0.48, 0.34, 1), 0.7))


def begin():
    arm = p9.setup("park")
    post()
    dress()
    return arm


def limb_bend(arm, parent, child):
    bpy.context.view_layer.update()
    a = arm.pose.bones[parent]
    b = arm.pose.bones[child]
    da = (arm.matrix_world @ a.tail - arm.matrix_world @ a.head).normalized()
    db = (arm.matrix_world @ b.tail - arm.matrix_world @ b.head).normalized()
    return math.degrees(math.acos(max(-1.0, min(1.0, da.dot(db)))))


def pose_absorb(arm):
    """Deep knee bend. Feet carry the body. One hand comes down near a foot afterward."""
    p7.bone(arm, "Hips", 32, 0, 0)
    p7.bone(arm, "Spine", 34)
    p7.bone(arm, "Chest", 10)
    p7.bone(arm, "Head", -36)
    p7.bone(arm, "UpperLeg_L", 78)
    p7.bone(arm, "UpperLeg_R", 72)
    p7.bone(arm, "LowerLeg_L", -102)
    p7.bone(arm, "LowerLeg_R", -98)
    p7.bone(arm, "UpperArm_R", -20, -16, 10)
    p7.bone(arm, "LowerArm_R", -48)
    p7.bone(arm, "UpperArm_L", -40, 12, 0)
    p7.bone(arm, "LowerArm_L", -20)


def plant_near_foot(arm):
    foot = p7.bone_point(arm, "Foot_L")
    best = None
    for pitch in range(-130, 90, 8):
        for elbow in (-10, -35, -60):
            p7.bone(arm, "UpperArm_L", pitch, 18, 8)
            p7.bone(arm, "LowerArm_L", elbow)
            bpy.context.view_layer.update()
            hand = p7.bone_point(arm, "Hand_L")
            if hand.z < 0.02 or hand.z > 0.35:
                continue
            d = (hand.x - foot.x) ** 2 + (hand.y - foot.y) ** 2 + (hand.z - 0.1) ** 2
            if best is None or d < best[0]:
                best = (d, pitch, elbow)
    if best is None:
        raise SystemExit("no hand pose near the foot")
    p7.bone(arm, "UpperArm_L", best[1], 18, 8)
    p7.bone(arm, "LowerArm_L", best[2])
    bpy.context.view_layer.update()
    hand = p7.bone_point(arm, "Hand_L")
    print("absorb-hand", round(hand.z, 3), "pitch", best[1], "elbow", best[2])


def still_ghosts():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p7.pose_sprint(arm)
    p7.drop_to_floor(arm)
    ghosts = []
    alphas = (0.60, 0.45, 0.30, 0.15)
    phases = (0.55, 0.05, -0.45, -0.9)
    for i in range(4):
        ghost = p7.clone_arm(arm)
        p7.reset_arm(ghost, base_q)
        p7.pose_run(ghost, phases[i])
        p9.tint_hier(ghost, alphas[i], p9.PLAYER)
        p7.drop_to_floor(ghost)
        p7.place(ghost, 0.08 * ((i % 2) * 2 - 1), 0.32 + i * 0.42)
        ghosts.append(ghost)
    p10.assert_same_hier(arm, ghosts[0])
    # High enough to see the crown bolts, far enough that the feet stay in frame.
    p9.frame_yaw([arm] + ghosts, yaw=-0.62, lens=46, fill=0.52, lift=1.15)
    p9.shot("dash-ghosts")


def foot_cards(guy, diameters, color, alpha, back):
    mat = cloud_mat("Puff", color, alpha)
    made = []
    foot_l = p7.bone_point(guy, "Foot_L")
    foot_r = p7.bone_point(guy, "Foot_R")
    for i, diameter in enumerate(diameters):
        foot = foot_l if i % 2 == 0 else foot_r
        y = foot.y + 0.15 + (i // 2) * back
        z = 0.12 + diameter * 0.28
        if z + diameter * 0.5 > 0.85:
            z = 0.85 - diameter * 0.5
        x = foot.x + ((i % 3) - 1) * 0.08
        made.append(card((x, y, z), diameter, mat))
    return made


def runner_on(x, sprint, swing, base_q, src, first):
    guy = src if first else p7.clone_arm(src)
    if not first:
        p9.tint_hier(guy, None, p9.PLAYER)
    p7.reset_arm(guy, base_q)
    if sprint:
        p7.pose_sprint(guy)
    else:
        p7.pose_run(guy, swing)
        p7.bone(guy, "Root", 6, 0, 0)
        p7.bone(guy, "Spine", 8)
    p7.drop_to_floor(guy)
    p7.place(guy, x, 0.0)
    return guy


def paint_ground(kind):
    """One surface under the close shot, so the puff color has something to match."""
    pad = bpy.data.objects.get("Pad")
    if pad is not None:
        bpy.data.objects.remove(pad, do_unlink=True)
    if kind == "concrete":
        dark = (0.40, 0.40, 0.38, 1)
        light = (0.58, 0.58, 0.56, 1)
        scale = 18.0
    else:
        dark = (0.36, 0.22, 0.11, 1)
        light = (0.55, 0.36, 0.18, 1)
        scale = 8.0
    lawn = bpy.data.objects["Lawn"]
    lawn.data.materials[0] = p9.noisy(kind + "Ground", dark, light, scale, 0.9)


def still_dust_one(name, kind, sprint, swing, diameters, alpha, back):
    arm = begin()
    paint_ground(kind)
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    guy = runner_on(0.0, sprint, swing, base_q, arm, True)
    color = DIRT if kind == "dirt" else CONCRETE
    cards = foot_cards(guy, diameters, color, alpha, back)
    # The runner fills about half the frame. Cards are aimed after the camera exists.
    p9.frame_yaw([guy], yaw=0.48, lens=48, fill=0.62, lift=0.22)
    aim(cards)
    p9.shot(name)


def still_dust_close():
    still_dust_one(
        "dust-dirt-sprint", "dirt", True, 0,
        (0.62, 0.74, 0.55, 0.68, 0.48, 0.70), 1.0, 0.38)
    still_dust_one(
        "dust-concrete-sprint", "concrete", True, 0,
        (0.22, 0.28, 0.18, 0.24), 0.85, 0.26)
    still_dust_one(
        "dust-dirt-walk", "dirt", False, 0.35,
        (0.16, 0.20, 0.14), 0.7, 0.22)


def still_dust_wide():
    arm = begin()
    pad = bpy.data.objects.get("Pad")
    if pad is not None:
        bpy.data.objects.remove(pad, do_unlink=True)
    # Three pads along X.
    for name, x, half, dark, light, scale in (
        ("DirtA", -3.6, 2.2, (0.36, 0.22, 0.11, 1), (0.55, 0.36, 0.18, 1), 8.0),
        ("Conc", 0.0, 2.0, (0.40, 0.40, 0.38, 1), (0.58, 0.58, 0.56, 1), 18.0),
        ("DirtB", 3.6, 2.2, (0.36, 0.22, 0.11, 1), (0.55, 0.36, 0.18, 1), 8.0),
    ):
        bpy.ops.mesh.primitive_plane_add(size=1, location=(x, 0.2, 0.02))
        ob = bpy.context.active_object
        ob.scale = (half, 6.0, 1)
        ob.data.materials.append(p9.noisy(name, dark, light, scale, 0.9))
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    a = runner_on(-3.6, True, 0, base_q, arm, True)
    b = runner_on(0.0, True, 0, base_q, arm, False)
    c = runner_on(3.6, False, 0.35, base_q, arm, False)
    cards = []
    cards += foot_cards(a, (0.62, 0.74, 0.55, 0.68, 0.48), DIRT, 1.0, 0.38)
    cards += foot_cards(b, (0.22, 0.28, 0.18, 0.24), CONCRETE, 0.85, 0.26)
    cards += foot_cards(c, (0.16, 0.20, 0.14), DIRT, 0.7, 0.22)
    bpy.context.scene.render.resolution_x = 2100
    bpy.context.scene.render.resolution_y = 780
    p9.frame_yaw([a, b, c], yaw=0.42, lens=34, fill=0.72, lift=0.35)
    aim(cards)
    p9.shot("running-dust", res=(2100, 780))


def still_land():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_absorb(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    plant_near_foot(arm)
    bend_l = limb_bend(arm, "UpperLeg_L", "LowerLeg_L")
    bend_r = limb_bend(arm, "UpperLeg_R", "LowerLeg_R")
    hip = p7.bone_point(arm, "Hips")
    head = p7.bone_point(arm, "Head")
    print("absorb bend", round(bend_l, 1), round(bend_r, 1), "hip", round(hip.z, 2), "head", round(head.z, 2))
    if bend_l < 80 or bend_l > 120 or bend_r < 80 or bend_r > 120:
        raise SystemExit("knees are not in the absorb range")
    if hip.z > 0.78:
        raise SystemExit("hips are too high for a hard absorb")
    if head.z < hip.z:
        raise SystemExit("head is below the hips")
    ring = p10.torus((0.0, 0.1, 0.03), 1.30, 0.045, (0.85, 0.72, 0.48, 1), 1.1)
    bits = []
    for i in range(12):
        ang = i / 12.0 * math.tau
        rad = 0.45 + (i % 3) * 0.32
        z = 0.06 + (i % 4) * 0.1
        bits.append(p10.chunk(
            (math.cos(ang) * rad, 0.05 + math.sin(ang) * rad * 0.6, z),
            0.04 + (i % 3) * 0.018,
            (0.62, 0.42, 0.22, 1) if i % 2 == 0 else (0.55, 0.50, 0.42, 1),
        ))
    p9.frame_yaw([arm], [ring] + bits, yaw=1.05, lens=42, fill=0.58, lift=0.4)
    p9.shot("landing-ring")


def still_slide():
    arm = begin()
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
    foot_l = p7.bone_point(arm, "Foot_L")
    foot_r = p7.bone_point(arm, "Foot_R")
    ribbons = []
    for foot in (foot_l, foot_r):
        ribbons.append(p10.ribbon(
            (foot.x, foot.y - 0.05, 0.04),
            (foot.x, foot.y - 1.35, 0.03),
            0.18,
            (0.72, 0.62, 0.48, 0.9),
        ))
    p9.frame_yaw([arm], ribbons, yaw=1.15, lens=46, fill=0.55, lift=0.35)
    p9.shot("slide-scrape")


def pose_tuck(arm):
    """Cannonball leap. Knees in the chest, arms swept back, not a running stride."""
    p7.bone(arm, "Spine", -18)
    p7.bone(arm, "Chest", -8)
    p7.bone(arm, "Head", 12)
    p7.bone(arm, "UpperLeg_L", 112)
    p7.bone(arm, "UpperLeg_R", 104)
    p7.bone(arm, "LowerLeg_L", -125)
    p7.bone(arm, "LowerLeg_R", -118)
    p7.bone(arm, "UpperArm_L", -125, 28, 20)
    p7.bone(arm, "UpperArm_R", -118, -24, -16)
    p7.bone(arm, "LowerArm_L", -22)
    p7.bone(arm, "LowerArm_R", -18)


def still_pad():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_tuck(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    # Clear of the burst, so the tuck reads above the pad instead of inside it.
    arm.location.y += 0.9
    arm.location.z += 1.35
    bpy.ops.mesh.primitive_cylinder_add(radius=1.25, depth=0.1, location=(0.0, 0.0, 0.05))
    pad = bpy.context.active_object
    pad.data.materials.append(p7.principled("PadBody", (0.22, 0.48, 0.55, 1), 0.45))
    rings = []
    # Short burst. The top ring is under knee height above the pad, not a pole into the body.
    for z, rad, strength in ((0.08, 0.7, 1.5), (0.22, 0.85, 1.0), (0.36, 0.55, 0.55), (0.48, 0.32, 0.28)):
        rings.append(p10.torus((0.0, 0.0, z), rad, 0.03, (0.45, 0.90, 1.0, 1), strength))
    streaks = []
    for i in range(6):
        ang = i / 6.0 * math.tau
        x = math.cos(ang) * 0.35
        y = math.sin(ang) * 0.35
        streaks.append(p10.ribbon((x, y, 0.06), (x * 0.6, y * 0.6, 0.42), 0.04, (0.65, 0.95, 1.0, 1)))
    bpy.context.view_layer.update()
    hip = p7.bone_point(arm, "Hips")
    # Brief speed lines behind the leap, along the travel, not up the torso.
    lines = []
    for i, z in enumerate((0.15, 0.35, 0.55, 0.2)):
        lines.append(p10.ribbon(
            (hip.x + 0.15 * (i - 1.5), hip.y - 0.3, hip.z * 0.15 + z),
            (hip.x + 0.12 * (i - 1.5), hip.y - 0.85, hip.z * 0.15 + z),
            0.03,
            (0.85, 0.95, 1.0, 1),
        ))
    p9.frame_yaw([arm], [pad] + rings + streaks + lines, yaw=0.85, lens=42, fill=0.52, lift=0.15)
    p9.shot("launch-pad")


def still_zip():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p10.pose_hang(arm)
    # Lean into the travel. Facing +Y after the turn, so a forward pitch is the lean.
    p7.bone(arm, "Hips", 16, 0, 8)
    p7.bone(arm, "Spine", 22, 0, 6)
    p7.bone(arm, "Head", -6)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 1.05
    bpy.context.view_layer.update()
    hand = p7.bone_point(arm, "Hand_R")
    y0, y1 = -2.4, 2.6
    z0, z1 = hand.z + 0.35, hand.z + 0.15
    sag = 0.43
    pts = []
    for i in range(9):
        t = i / 8.0
        y = y0 + (y1 - y0) * t
        z = z0 + (z1 - z0) * t - sag * 4 * t * (1 - t)
        pts.append(Vector((hand.x, y, z)))
    pieces = []
    for i in range(len(pts) - 1):
        pieces.append(p10.ribbon(pts[i], pts[i + 1], 0.045, (0.55, 0.32, 0.72, 1)))
    for i in range(8):
        t = (i + 0.5) / 8.0
        y = y0 + (y1 - y0) * t
        z = z0 + (z1 - z0) * t - sag * 4 * t * (1 - t)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.045, location=(hand.x + 0.06, y, z - 0.05))
        spark = bpy.context.active_object
        spark.data.materials.append(p7.emissive("Spark", (0.82, 0.45, 1.0, 1), 2.4))
        pieces.append(spark)
    # A short spark trail leaving the hand, opposite the travel.
    for i in range(5):
        bpy.ops.mesh.primitive_ico_sphere_add(
            subdivisions=1,
            radius=0.035 - i * 0.004,
            location=(hand.x, hand.y - 0.12 - i * 0.16, hand.z - 0.02 - i * 0.03),
        )
        bit = bpy.context.active_object
        bit.data.materials.append(p7.emissive("HandSpark", (0.9, 0.55, 1.0, 1), 2.0))
        pieces.append(bit)
    for y, z in ((y0, z0), (y1, z1)):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=z + 0.15, location=(hand.x, y, (z + 0.15) * 0.5))
        post = bpy.context.active_object
        post.data.materials.append(p7.principled("Post", (0.35, 0.36, 0.38, 1), 0.55))
        pieces.append(post)
    p9.frame_yaw([arm], pieces, yaw=1.15, lens=38, fill=0.58, lift=0.2)
    p9.shot("zip-line")


def comic_word(text, loc, size, tilt):
    font = bpy.data.fonts.load(FONT)
    made = []
    for scale, color, extrude, name in (
        (1.08, (0.05, 0.04, 0.04, 1), 0.012, "Outline"),
        (1.0, (1.0, 0.78, 0.12, 1), 0.02, "Fill"),
    ):
        curve = bpy.data.curves.new(name, "FONT")
        curve.body = text
        curve.font = font
        curve.align_x = "CENTER"
        curve.align_y = "CENTER"
        curve.size = size * scale
        curve.extrude = extrude
        ob = bpy.data.objects.new(name, curve)
        bpy.context.collection.objects.link(ob)
        ob.location = loc
        ob.rotation_euler = (math.pi / 2, 0.0, math.radians(tilt))
        ob.data.materials.append(p7.emissive(name + "Mat", color, 1.4))
        made.append(ob)
    return made


def still_punch():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    other = p7.clone_arm(arm)
    p9.tint_hier(other, None, p9.PLAYER)
    p9.tint_hier(arm, None, p9.TAGGER)
    p7.reset_arm(arm, base_q)
    p7.pose_punch(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    p7.reset_arm(other, base_q)
    p7.pose_sprint(other)
    p7.bone(other, "Spine", 12)
    p9.face_travel(other, base_q)
    p7.drop_to_floor(other)
    p7.place(other, 0.1, 0.9)
    p7.place(arm, 0.7, 0.2)
    p10.seat_fist(arm, other)
    bpy.context.view_layer.update()
    fist = p7.bone_point(arm, "Hand_R")
    head = p7.bone_point(other, "Head")
    # Small punch word. The tag burst was the large one. This one stays under the face.
    loc = Vector((fist.x + 0.08, fist.y, min(fist.z, head.z - 0.35)))
    # Punch word is the small size. The tag burst is the large one.
    words = comic_word("POW!", loc, 0.07, -8)
    p9.frame_yaw([arm, other], words, yaw=math.pi / 2, lens=48, fill=0.55, lift=0.2)
    p9.shot("punch-word")


if __name__ == "__main__":
    only = os.environ.get("PASS11_ONLY", "")
    shots = [
        ("ghosts", still_ghosts),
        ("dust", still_dust_close),
        ("wide", still_dust_wide),
        ("land", still_land),
        ("slide", still_slide),
        ("pad", still_pad),
        ("zip", still_zip),
        ("punch", still_punch),
    ]
    os.makedirs(OUT, exist_ok=True)
    wanted = [s.strip() for s in only.split(",") if s.strip()] if only else None
    for name, fn in shots:
        if wanted and name not in wanted:
            continue
        fn()
