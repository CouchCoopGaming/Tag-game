#!/usr/bin/env python3
"""Pass 14 stills. Both hands on the zip, a low slide, a swinging grapple, and the next FX.

Run:
  blender --background --python Tools/RenderPass14.py
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
import RenderPass11 as p11
import RenderPass12 as p12
import RenderPass13 as p13

OUT = os.environ.get("PASS14_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass14"))
p9.OUT = OUT
p9.SAMPLES = int(os.environ.get("PASS14_SAMPLES", "12"))
p13.OUT = OUT

DIRT = (0.86, 0.72, 0.56, 1)
CONCRETE = (0.78, 0.78, 0.76, 1)
# Warm orange-white. The pass 13 zip sparks were magenta.
METAL = (1.0, 0.74, 0.42, 1)
HEADS = ("Mesh_Head", "Mesh_Neck")


def begin():
    return p13.begin()


def wh(arm, name):
    return p13.wh(arm, name)


def wt(arm, name):
    return p13.wt(arm, name)


def mesh_centroid(arm, prefix):
    pts = p10.samples(arm, (prefix,), 2)
    if not pts:
        raise SystemExit("missing mesh " + prefix)
    return p13.centroid(pts)


def ground(kind):
    """Park grass reads as dirt. Concrete hides the lawn and grays the pad."""
    lawn = bpy.data.objects["Lawn"]
    pad = bpy.data.objects["Pad"]
    if kind == "concrete":
        lawn.hide_render = True
        pad.hide_render = False
        pad.scale = (40, 40, 1)
        pad.data.materials.clear()
        pad.data.materials.append(p7.principled("ConcreteGround", (0.62, 0.63, 0.66, 1), 0.72))
    else:
        pad.hide_render = True
        lawn.hide_render = False


def speed_line(a, b, width, color, strength=0.4):
    """A flat streak facing the +X camera. A Y-long ribbon is edge-on from that side."""
    a = Vector(a)
    b = Vector(b)
    half = Vector((0.0, 0.0, width * 0.5))
    mesh = bpy.data.meshes.new("SpeedLine")
    mesh.from_pydata(
        [tuple(a + half), tuple(a - half), tuple(b - half), tuple(b + half)],
        [],
        [(0, 1, 2, 3)],
    )
    mesh.update()
    ob = bpy.data.objects.new("SpeedLine", mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(p7.emissive("SpeedLine", color, strength))
    return ob


def puff_mat(name, color):
    """Soft disc. The cloud texture stayed clear on these side cards, so the falloff is procedural."""
    mat = p7.emissive(name, (color[0], color[1], color[2], 1.0), 0.75)
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Generated"], sep.inputs["Vector"])

    def off(sock):
        node = nt.nodes.new("ShaderNodeMath")
        node.operation = "SUBTRACT"
        nt.links.new(sock, node.inputs[0])
        node.inputs[1].default_value = 0.5
        return node.outputs[0]

    def sq(sock):
        node = nt.nodes.new("ShaderNodeMath")
        node.operation = "MULTIPLY"
        nt.links.new(sock, node.inputs[0])
        nt.links.new(sock, node.inputs[1])
        return node.outputs[0]

    dist = nt.nodes.new("ShaderNodeMath")
    dist.operation = "ADD"
    nt.links.new(sq(off(sep.outputs["X"])), dist.inputs[0])
    nt.links.new(sq(off(sep.outputs["Y"])), dist.inputs[1])
    root = nt.nodes.new("ShaderNodeMath")
    root.operation = "SQRT"
    nt.links.new(dist.outputs[0], root.inputs[0])
    norm = nt.nodes.new("ShaderNodeMath")
    norm.operation = "DIVIDE"
    nt.links.new(root.outputs[0], norm.inputs[0])
    norm.inputs[1].default_value = 0.48
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(norm.outputs[0], inv.inputs[1])
    low = nt.nodes.new("ShaderNodeMath")
    low.operation = "MAXIMUM"
    nt.links.new(inv.outputs[0], low.inputs[0])
    low.inputs[1].default_value = 0.0
    high = nt.nodes.new("ShaderNodeMath")
    high.operation = "MINIMUM"
    nt.links.new(low.outputs[0], high.inputs[0])
    high.inputs[1].default_value = 1.0
    soft = nt.nodes.new("ShaderNodeMath")
    soft.operation = "POWER"
    nt.links.new(high.outputs[0], soft.inputs[0])
    soft.inputs[1].default_value = 1.35
    gain = nt.nodes.new("ShaderNodeMath")
    gain.operation = "MULTIPLY"
    nt.links.new(soft.outputs[0], gain.inputs[0])
    gain.inputs[1].default_value = 0.92
    nt.links.new(gain.outputs[0], bsdf.inputs["Alpha"])
    return mat


def dust_card(loc, diameter, color):
    """A cloud card facing the +X camera."""
    bpy.ops.mesh.primitive_plane_add(size=diameter, location=loc)
    ob = bpy.context.active_object
    ob.name = "Spray"
    # XY plane, normal +Z. A +90° yaw turns that normal to +X, toward the camera.
    ob.rotation_euler = (0.0, math.pi / 2.0, 0.0)
    ob.data.materials.append(puff_mat("Spray", color))
    ob.visible_shadow = False
    return ob


def report_frame(arms, effects, label, contacts=()):
    """Same gate as pass 13. A wall or box may touch the body if it stays under 10% and off the head."""
    runners = p13.runner_meshes(arms)
    rbox = p13.screen_box(runners, 4)
    print("BOX", label, "runner", p13.fmt_box(rbox))
    if rbox is None:
        raise SystemExit(label + " runner is off screen")
    if rbox[0] < -0.02 or rbox[1] < -0.02 or rbox[2] > 1.02 or rbox[3] > 1.02:
        raise SystemExit(label + " runner is clipped")
    heads = [ob for ob in runners if ob.name.split(".")[0] in HEADS]
    hbox = p13.screen_box(heads, 2) if heads else None
    keep = set(ob.name for ob in runners)
    contact_names = set(ob.name for ob in contacts)
    for ob in list(effects) + list(contacts):
        keep.add(ob.name)
        ebox = p13.screen_box([ob], 1)
        print("BOX", label, ob.name, p13.fmt_box(ebox))
        if ob.name in contact_names and ebox is not None:
            area = p13.box_area(ebox)
            if area > 0.10:
                raise SystemExit("%s contact %s covers the frame (area %.2f)" % (label, ob.name, area))
            if hbox is not None and p13.overlaps(ebox, hbox):
                raise SystemExit(label + " contact covers the head")
    for ob in list(bpy.data.objects):
        if ob.type != "MESH" or ob.name in keep:
            continue
        if ob.hide_render:
            continue
        if ob.name.split(".")[0] in p13.GROUND:
            continue
        pbox = p13.screen_box([ob], 3)
        if pbox is None:
            continue
        area = p13.box_area(pbox)
        hit = p13.overlaps(pbox, rbox)
        if area > 0.10 or hit:
            raise SystemExit("%s prop %s covers the frame (area %.2f overlap %s)" % (label, ob.name, area, hit))
    return rbox


def side(arms, pieces, lens, fill, lift):
    p9.frame_yaw(arms, pieces, yaw=math.pi / 2, lens=lens, fill=fill, lift=lift)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"


def still_punch():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    runner = p7.clone_arm(arm)
    p9.tint_hier(runner, None, p9.PLAYER)
    p9.tint_hier(arm, None, p9.TAGGER)
    p7.reset_arm(arm, base_q)
    p13.pose_jab(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    p13.assert_extended(arm)
    p7.reset_arm(runner, base_q)
    p7.pose_sprint(runner)
    # Two or three frames of hit-stop: the chest gives, the head snaps forward.
    p7.bone(runner, "Hips", 12, 0, 0)
    p7.bone(runner, "Spine", 20, -4, 0)
    p7.bone(runner, "Chest", 12)
    p7.bone(runner, "Head", 24, -6, 0)
    p9.face_travel(runner, base_q)
    p7.drop_to_floor(runner)
    lean = wh(runner, "Head").z - wh(runner, "Hips").z
    print("target-head-above-hip", round(lean, 3))
    if lean < 0.2:
        raise SystemExit("target folded over")
    p7.place(runner, 0.05, 0.55)
    p7.place(arm, 0.7, -0.15)
    touch = p13.seat_on_chest(arm, runner)
    p13.assert_extended(arm)
    fist = mesh_centroid(arm, "Mesh_Hand_R")
    contact = fist + Vector((0.04, 0.0, 0.0))
    print("punch-contact", tuple(round(v, 3) for v in contact), "touch", round(touch, 3),
          "target-spine", 20, "target-head", 24)
    pieces = []
    outline = p9.star_mesh("PowOut", 0.148, 0.0)
    outline.location = contact + Vector((-0.03, 0.0, 0.0))
    outline.data.materials.append(p7.emissive("PowOutMat", (0.05, 0.04, 0.03, 1), 1.3))
    pieces.append(outline)
    fill = p9.star_mesh("PowFill", 0.118, 0.0)
    fill.location = contact + Vector((-0.015, 0.0, 0.0))
    fill.data.materials.append(p7.emissive("PowFillMat", (1.0, 0.62, 0.08, 1), 1.8))
    pieces.append(fill)
    pieces.extend(p12.comic_word("POW!", contact + Vector((0.02, 0.0, 0.0)), 0.34, -8))
    # Short trail behind the fist. Travel is +Y, so the lines run back along -Y.
    for i, lat in enumerate((-0.05, 0.0, 0.05)):
        z = fist.z + lat
        pieces.append(speed_line(
            (fist.x, fist.y - 0.06, z),
            (fist.x, fist.y - 0.42, z + lat * 0.3),
            0.028,
            (0.93, 0.95, 1.0, 0.7),
            0.55,
        ))
    target = Vector((contact.x, contact.y, fist.z))
    p9.view(target, 4.5, math.pi / 2, fist.z, 48, fist.z)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    print("pow-frame-height", round(p12.word_height_fraction(pieces), 4))
    report_frame([arm, runner], pieces, "punch")
    p9.shot("punch-word")


def pose_slide_body(arm):
    """Hips sit on the folded trail leg. The pass 13 pitch left the pelvis at half a metre."""
    p7.bone(arm, "Hips", -28)
    p7.bone(arm, "Spine", -6)
    p7.bone(arm, "Chest", -4)
    p7.bone(arm, "Head", 16)
    p7.bone(arm, "UpperLeg_L", -60)
    p7.bone(arm, "LowerLeg_L", 0)
    p7.bone(arm, "Foot_L", -8)
    p7.bone(arm, "UpperLeg_R", 110)
    p7.bone(arm, "LowerLeg_R", -160)
    p7.bone(arm, "Foot_R", 12)
    p7.bone(arm, "UpperArm_L", -16, 8, 0)
    p7.bone(arm, "LowerArm_L", -14)


def plant_slide_hand(arm):
    """Pitch 160 and elbow -120 puts the hand behind the hip and on the ground."""
    hip = wh(arm, "Hips")
    p7.bone(arm, "UpperArm_R", 160, 14, 6)
    p7.bone(arm, "LowerArm_R", -120)
    bpy.context.view_layer.update()
    hand = wt(arm, "Hand_R")
    print("slide-hand z", round(hand.z, 3), "dy", round(hand.y - hip.y, 3))
    if hand.y > hip.y - 0.1 or not (0.02 < hand.z < 0.28):
        raise SystemExit("trailing hand left the ground behind the hip")


def still_slide(kind):
    arm = begin()
    ground(kind)
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_slide_body(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    plant_slide_hand(arm)
    hip = wh(arm, "Hips")
    head = wh(arm, "Head")
    lead = wh(arm, "Foot_L")
    tuck = wh(arm, "Foot_R")
    print("slide", kind, "hipz", round(hip.z, 3), "headz", round(head.z, 3),
          "head-dy", round(head.y - hip.y, 3),
          "lead-dy", round(lead.y - hip.y, 3), "leadz", round(lead.z, 3),
          "tuck-dy", round(tuck.y - hip.y, 3), "tuckz", round(tuck.z, 3))
    if hip.z > 0.38:
        raise SystemExit("hips are still up in a skate")
    if head.z < hip.z + 0.12 or head.y > hip.y:
        raise SystemExit("slide head is not back and above the hips")
    if lead.y < hip.y + 0.4:
        raise SystemExit("lead foot is not ahead")
    heel = lead
    bits = []
    if kind == "dirt":
        # A short spray kicked back from the heel, not a fog over the legs.
        for i in range(5):
            bits.append(dust_card(
                (heel.x + 0.14, heel.y - 0.10 - i * 0.06, 0.14 + (i % 3) * 0.06),
                0.28 + (i % 2) * 0.08,
                (0.94, 0.86, 0.72, 1),
            ))
    else:
        for i in range(7):
            ang = i / 7.0 * math.tau
            bits.append(p13.spark(
                (heel.x + math.cos(ang) * 0.07, heel.y - 0.06 - (i % 3) * 0.05, 0.07 + (i % 2) * 0.04),
                0.055,
                METAL,
                0.9,
            ))
    side([arm], bits, 48, 0.62, 0.4)
    report_frame([arm], bits, "slide-" + kind)
    p9.shot("slide-" + kind)


def pose_zip(arm):
    """Both arms meet on the handle. Knees tuck, and the legs trail back."""
    p7.bone(arm, "UpperArm_L", -155, 6, 8)
    p7.bone(arm, "UpperArm_R", -155, -6, -8)
    p7.bone(arm, "LowerArm_L", -12)
    p7.bone(arm, "LowerArm_R", -12)
    p7.bone(arm, "Hips", 22)
    p7.bone(arm, "Spine", 8)
    p7.bone(arm, "Head", 6)
    p7.bone(arm, "UpperLeg_L", 48)
    p7.bone(arm, "UpperLeg_R", 40)
    p7.bone(arm, "LowerLeg_L", -55)
    p7.bone(arm, "LowerLeg_R", -47)


def still_zip():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_zip(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 1.05
    bpy.context.view_layer.update()
    hip = wh(arm, "Hips")
    head = wh(arm, "Head")
    if head.z < hip.z:
        raise SystemExit("zip rider is inverted")
    left = mesh_centroid(arm, "Mesh_Hand_L")
    right = mesh_centroid(arm, "Mesh_Hand_R")
    grip = (left + right) * 0.5
    gap_l = (left - grip).length
    gap_r = (right - grip).length
    print("zip-hands", round(gap_l, 3), round(gap_r, 3), "gripz", round(grip.z, 3), "headz", round(head.z, 3))
    if gap_l > 0.08 or gap_r > 0.08:
        raise SystemExit("hands are not both on the handle")
    if left.z < head.z - 0.02 or right.z < head.z - 0.02:
        raise SystemExit("a hand is below the head")
    for foot_name in ("Foot_L", "Foot_R"):
        foot = wh(arm, foot_name)
        if foot.y > hip.y - 0.15:
            raise SystemExit("legs are not trailing")
    pieces = []
    y0, y1 = grip.y - 1.5, grip.y + 1.5
    pieces.append(p13.tube((grip.x, y0, grip.z + 0.03), (grip.x, y1, grip.z - 0.02), 0.026, (0.22, 0.23, 0.25, 1)))
    # A short bar through both fists, so the handle is what the hands close on.
    pieces.append(p13.tube(left, right, 0.02, (0.55, 0.56, 0.58, 1)))
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.065, minor_radius=0.016, location=grip, major_segments=16, minor_segments=8)
    trolley = bpy.context.active_object
    trolley.rotation_euler = (0.0, math.pi / 2, 0.0)
    trolley.data.materials.append(p7.principled("Trolley", (0.45, 0.46, 0.48, 1), 0.3))
    pieces.append(trolley)
    for i in range(8):
        ang = i / 8.0 * math.tau
        loc = grip + Vector((math.cos(ang) * 0.05, -0.03 - (i % 3) * 0.025, math.sin(ang) * 0.04))
        if (loc - grip).length > 0.22:
            raise SystemExit("spark left the trolley")
        pieces.append(p13.spark(loc, 0.04, METAL, 0.8))
    print("zip feet", round(wh(arm, "Foot_L").y - hip.y, 3), round(wh(arm, "Foot_R").y - hip.y, 3))
    side([arm], pieces, 46, 0.64, 0.15)
    report_frame([arm], pieces, "zip")
    p9.shot("zip-line")


def pose_swing(arm):
    """Arm out along the rope, torso on the arc, knees bent, legs trailing."""
    p7.bone(arm, "UpperArm_L", -118, 6, 4)
    p7.bone(arm, "LowerArm_L", -6)
    p7.bone(arm, "UpperArm_R", 18, -16, 0)
    p7.bone(arm, "LowerArm_R", -36)
    p7.bone(arm, "Hips", -34, 0, 6)
    p7.bone(arm, "Spine", -16)
    p7.bone(arm, "Chest", -8)
    p7.bone(arm, "Head", 6)
    p7.bone(arm, "UpperLeg_L", 72)
    p7.bone(arm, "UpperLeg_R", 86)
    p7.bone(arm, "LowerLeg_L", -48)
    p7.bone(arm, "LowerLeg_R", -58)


def still_grapple():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_swing(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 1.35
    bpy.context.view_layer.update()
    hip = wh(arm, "Hips")
    head = wh(arm, "Head")
    shoulder = wh(arm, "Shoulder_L")
    if head.z < hip.z:
        raise SystemExit("grapple rider is inverted")
    bend = p13.flex(arm, "Shoulder_L", "LowerArm_L", "Hand_L")
    print("grapple-elbow", round(bend, 1))
    if bend > 30:
        raise SystemExit("grapple arm is not extended")
    bone_head = wh(arm, "Hand_L")
    bone_tail = wt(arm, "Hand_L")
    mesh = p10.samples(arm, ("Mesh_Hand_L",), 1)
    seg = bone_tail - bone_head
    best = None
    for p in mesh:
        t = (p - bone_head).dot(seg) / max(seg.length_squared, 1e-8)
        t = max(0.0, min(1.0, t))
        on_bone = bone_head + seg * t
        dist = (p - on_bone).length
        if best is None or dist < best[0]:
            best = (dist, p, on_bone)
    gap, skin, start = best
    print("rope-bone", tuple(round(v, 3) for v in start), "mesh-gap", round(gap, 3))
    if gap > 0.12:
        raise SystemExit("left hand mesh is far from its bone")
    along = (start - shoulder)
    if along.length < 0.2:
        raise SystemExit("arm has no reach")
    hook = start + along.normalized() * 1.9 + Vector((0.0, 0.15, 0.15))
    sag = 0.43
    pts = []
    for i in range(8):
        t = i / 7.0
        bell = 4.0 * t * (1.0 - t)
        p = Vector(start).lerp(hook, t)
        p.z -= sag * bell
        pts.append(p)
    pts[0] = Vector(start)
    spine = (shoulder - hip)
    vertical = Vector((0.0, 0.0, 1.0))
    tilt = math.degrees(math.acos(max(-1.0, min(1.0, spine.normalized().dot(vertical)))))
    print("grapple-tilt", round(tilt, 1))
    if tilt < 20 or tilt > 60:
        raise SystemExit("body is not on the swing arc")
    for foot_name in ("Foot_L", "Foot_R"):
        foot = wh(arm, foot_name)
        if foot.y > hip.y - 0.1:
            raise SystemExit("legs are not trailing the swing")
    pieces = []
    if (skin - start).length > 0.008:
        pieces.append(p13.tube(skin, start, 0.016, (0.62, 0.48, 0.30, 1)))
    for i in range(len(pts) - 1):
        pieces.append(p13.tube(pts[i], pts[i + 1], 0.016, (0.62, 0.48, 0.30, 1)))
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.07, minor_radius=0.016, location=hook, major_segments=16, minor_segments=8)
    hook_ob = bpy.context.active_object
    hook_ob.rotation_euler = (math.pi / 2, 0.2, 0.0)
    hook_ob.data.materials.append(p7.principled("Hook", (0.55, 0.56, 0.58, 1), 0.35))
    pieces.append(hook_ob)
    for off in (Vector((0.05, -0.03, -0.05)), Vector((-0.04, 0.04, -0.06))):
        pieces.append(p10.chunk(hook + off, 0.035, (0.62, 0.42, 0.22, 1)))
    # Frame the body and the rope leaving the hand. The far hook stays in the shot if it fits.
    side([arm], pieces[:4], 50, 0.7, 0.12)
    report_frame([arm], pieces, "grapple")
    p9.shot("grapple-rope")


def add_box(name, center, size, color):
    """Unit cube, then dimensions. A hand-built box was wound inside-out and vanished."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.active_object
    ob.name = name
    ob.dimensions = size
    # Principled cubes were invisible from the side camera. A low emission keeps the face.
    mat = p7.emissive(name + "Mat", color, 0.45)
    mat.blend_method = "OPAQUE"
    mat.use_backface_culling = False
    ob.data.materials.append(mat)
    bpy.context.view_layer.update()
    return ob


def still_wall_run():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p7.pose_wall_run(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 0.12
    bpy.context.view_layer.update()
    if wh(arm, "Head").z < wh(arm, "Hips").z:
        raise SystemExit("wall runner is not upright")
    foot = wh(arm, "Foot_L")
    height = max(1.05, foot.z + 0.55)
    wall = add_box(
        "Wall",
        (foot.x + 0.16, foot.y - 0.2, height * 0.5),
        (0.10, 1.15, height),
        (0.62, 0.63, 0.66, 1),
    )
    print("wall-foot", round(foot.z, 3), "wall-h", round(height, 3))
    bits = []
    # Four faint lines on the camera side of the wall, at the contact foot.
    # The wall center is foot.x+0.16 and half a 0.10 slab, so +0.22 is just in front.
    near = foot.x + 0.22
    for i in range(4):
        z = foot.z + 0.05 + i * 0.05
        bits.append(speed_line(
            (near, foot.y + 0.02, z),
            (near, foot.y - 0.53, z),
            0.04,
            (0.82, 0.88, 1.0, 1),
            0.9,
        ))
    side([arm], bits + [wall], 46, 0.62, 0.35)
    report_frame([arm], bits, "wall-run", [wall])
    p9.shot("wall-run")


def still_wall_jump():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    # WallJumpPose push, plant left. Degrees are the live shove.
    p7.bone(arm, "Hips", -10, 0, -18)
    p7.bone(arm, "Spine", -22)
    p7.bone(arm, "Head", -16)
    p7.bone(arm, "UpperLeg_L", 44)
    p7.bone(arm, "LowerLeg_L", -64)
    p7.bone(arm, "UpperLeg_R", 82)
    p7.bone(arm, "LowerLeg_R", -110)
    p7.bone(arm, "UpperArm_L", 46, 20, 0)
    p7.bone(arm, "LowerArm_L", -16)
    p7.bone(arm, "UpperArm_R", 30, -14, 0)
    p7.bone(arm, "LowerArm_R", -20)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 0.35
    bpy.context.view_layer.update()
    if wh(arm, "Head").z < wh(arm, "Hips").z:
        raise SystemExit("wall jump is inverted")
    foot = wh(arm, "Foot_L")
    height = max(1.15, foot.z + 0.45)
    wall = add_box(
        "Wall",
        (foot.x + 0.2, foot.y - 0.05, height * 0.5),
        (0.10, 0.85, height),
        (0.58, 0.59, 0.62, 1),
    )
    print("kick-foot", round(foot.y, 3), round(foot.z, 3))
    bits = []
    # Wall near face is foot.x+0.25. The puff sits on that face at the kicking foot.
    face = foot.x + 0.34
    for i in range(5):
        bits.append(dust_card(
            (face, foot.y + (i - 2) * 0.06, foot.z + (i - 2) * 0.05),
            0.38 + (i % 2) * 0.08,
            (0.92, 0.92, 0.90, 1),
        ))
    side([arm], bits + [wall], 46, 0.62, 0.3)
    report_frame([arm], bits, "wall-jump", [wall])
    p9.shot("wall-jump")


def still_vault():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p7.bone(arm, "Hips", -18)
    p7.bone(arm, "Spine", -10)
    p7.bone(arm, "Head", 4)
    p7.bone(arm, "UpperArm_L", 55, 18, 8)
    p7.bone(arm, "LowerArm_L", -18)
    p7.bone(arm, "UpperArm_R", 62, -16, -8)
    p7.bone(arm, "LowerArm_R", -22)
    p7.bone(arm, "UpperLeg_L", -36)
    p7.bone(arm, "LowerLeg_L", -28)
    p7.bone(arm, "UpperLeg_R", 48)
    p7.bone(arm, "LowerLeg_R", -64)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    bpy.context.view_layer.update()
    if wh(arm, "Head").z < wh(arm, "Hips").z:
        raise SystemExit("vaulter is inverted")
    left = mesh_centroid(arm, "Mesh_Hand_L")
    right = mesh_centroid(arm, "Mesh_Hand_R")
    plant = (left + right) * 0.5
    top = min(left.z, right.z) - 0.015
    if top < 0.45:
        raise SystemExit("vault hands are not on a box")
    box = add_box(
        "Box",
        (plant.x, plant.y + 0.02, top * 0.5),
        (0.40, 0.72, top),
        (0.48, 0.36, 0.24, 1),
    )
    bits = []
    # Box near face is plant.x+0.20. A puff on that edge, at the hands, reads from the side.
    puff = (0.92, 0.88, 0.80, 1)
    face = plant.x + 0.30
    for i, hand in enumerate((left, right)):
        bits.append(dust_card((face, hand.y, top + 0.10), 0.36, puff))
    bits.append(dust_card((face, plant.y, top + 0.14), 0.48, puff))
    print("vault-hands", round(left.z, 3), round(right.z, 3), "top", round(top, 3))
    side([arm], bits + [box], 44, 0.64, 0.3)
    report_frame([arm], bits, "vault", [box])
    p9.shot("vault-plant")


if __name__ == "__main__":
    only = os.environ.get("PASS14_ONLY", "")
    shots = [
        ("punch", still_punch),
        ("dirt", lambda: still_slide("dirt")),
        ("concrete", lambda: still_slide("concrete")),
        ("zip", still_zip),
        ("grapple", still_grapple),
        ("wallrun", still_wall_run),
        ("walljump", still_wall_jump),
        ("vault", still_vault),
    ]
    os.makedirs(OUT, exist_ok=True)
    wanted = [s.strip() for s in only.split(",") if s.strip()] if only else None
    for name, fn in shots:
        if wanted and name not in wanted:
            continue
        fn()
