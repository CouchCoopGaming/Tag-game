#!/usr/bin/env python3
"""Pass 12 stills. Lighter dust, a planted absorb, a readable POW, and the rope.

Run:
  blender --background --python Tools/RenderPass12.py
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
import RenderPass7 as p7
import RenderPass9 as p9
import RenderPass10 as p10
import RenderPass11 as p11

OUT = os.environ.get("PASS12_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass12"))
p9.OUT = OUT
p9.SAMPLES = int(os.environ.get("PASS12_SAMPLES", "12"))

# One step lighter than pass 11 (0.74, 0.50, 0.28), and a little less saturated.
DIRT = (0.86, 0.72, 0.56, 1)
SUN_RIM = (1.0, 0.93, 0.78, 1)
FONT = os.path.join(ROOT, "Assets", "Art", "FX", "Fonts", "Bangers-Regular.ttf")

p11.DIRT = DIRT


def cloud_mat(name, color, alpha_scale):
    """Filled cloud, same alpha as pass 11, with a warm sun rim on the edge."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.shadow_method = "NONE"
    mat.use_backface_culling = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = p11.cloud_image()
    tex.interpolation = "Linear"
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = alpha_scale
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], bsdf.inputs["Alpha"])
    layer = nt.nodes.new("ShaderNodeLayerWeight")
    layer.inputs["Blend"].default_value = 0.4
    rim = nt.nodes.new("ShaderNodeMath")
    rim.operation = "MULTIPLY"
    rim.inputs[1].default_value = 0.55
    nt.links.new(layer.outputs["Facing"], rim.inputs[0])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.inputs["A"].default_value = color
    mix.inputs["B"].default_value = SUN_RIM
    nt.links.new(rim.outputs["Value"], mix.inputs["Factor"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    emit = bsdf.inputs.get("Emission Color")
    if emit is not None:
        emit.default_value = SUN_RIM
    strength = bsdf.inputs.get("Emission Strength")
    if strength is not None:
        # The rim factor tops out near 0.55. Scale that down to a slight edge light.
        strength_scale = nt.nodes.new("ShaderNodeMath")
        strength_scale.operation = "MULTIPLY"
        strength_scale.inputs[1].default_value = 0.35
        nt.links.new(rim.outputs["Value"], strength_scale.inputs[0])
        nt.links.new(strength_scale.outputs["Value"], strength)
    bsdf.inputs["Roughness"].default_value = 1.0
    spec = bsdf.inputs.get("Specular IOR Level") or bsdf.inputs.get("Specular")
    if spec is not None:
        spec.default_value = 0.0
    return mat


p11.cloud_mat = cloud_mat


def wh(arm, name):
    return arm.matrix_world @ arm.pose.bones[name].head


def wt(arm, name):
    return arm.matrix_world @ arm.pose.bones[name].tail


def flex_of(a, b, c):
    u = (b - a).normalized()
    v = (c - b).normalized()
    d = max(-1.0, min(1.0, u.dot(v)))
    return math.degrees(math.acos(d))


def pose_squat(arm):
    """Feet under the hips. Knees fold forward, not up behind the body.

    Positive thigh X swings the knee backward and up on this rig, which is why
    the pass 11 absorb stood on its hands. Negative thigh X brings the knee forward.
    """
    p7.bone(arm, "Hips", 34, 0, 0)
    p7.bone(arm, "Spine", 26)
    p7.bone(arm, "Chest", 8)
    p7.bone(arm, "Head", -46)
    p7.bone(arm, "UpperLeg_L", -72)
    p7.bone(arm, "UpperLeg_R", -64)
    p7.bone(arm, "LowerLeg_L", 112)
    p7.bone(arm, "LowerLeg_R", 104)
    p7.bone(arm, "Foot_L", -70)
    p7.bone(arm, "Foot_R", -78)
    p7.bone(arm, "UpperArm_L", -140, 16, 0)
    p7.bone(arm, "UpperArm_R", -110, -14, 0)
    p7.bone(arm, "LowerArm_L", -20)
    p7.bone(arm, "LowerArm_R", -30)


def plant_hand(arm):
    foot = wh(arm, "Foot_L")
    best = None
    for pitch in range(-40, 80, 8):
        for yaw in range(-20, 50, 10):
            for elbow in (-20, -50, -80, -110):
                p7.bone(arm, "UpperArm_L", pitch, yaw, 10)
                p7.bone(arm, "LowerArm_L", elbow)
                bpy.context.view_layer.update()
                hand = wt(arm, "Hand_L")
                if hand.z < 0.08 or hand.z > 0.42:
                    continue
                dist = (hand - foot).length
                if dist > 0.42:
                    continue
                score = dist + abs(hand.z - 0.16) * 0.4
                if best is None or score < best[0]:
                    best = (score, pitch, yaw, elbow, dist, hand.z)
    if best is None:
        raise SystemExit("no hand pose near the foot")
    p7.bone(arm, "UpperArm_L", best[1], best[2], 10)
    p7.bone(arm, "LowerArm_L", best[3])
    bpy.context.view_layer.update()
    print("absorb-hand pitch", best[1], "yaw", best[2], "elbow", best[3],
          "dist", round(best[4], 3), "z", round(best[5], 3))


def lowest_mesh(arm):
    deps = bpy.context.evaluated_depsgraph_get()
    best = ("", 1e9)
    for ob in p7.meshes(arm):
        ev = ob.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ob.matrix_world
        for v in me.vertices:
            z = (mw @ v.co).z
            if z < best[1]:
                best = (ob.name.split(".")[0], z)
        ev.to_mesh_clear()
    return best


def report_land(arm):
    """World-space angles from the evaluated pose, at the capture frame."""
    bpy.context.view_layer.update()
    hip = wh(arm, "Hips")
    chest = wh(arm, "Chest")
    head = wh(arm, "Head")
    look = wt(arm, "Head") - head
    spine = chest - hip
    spine_pitch = math.degrees(math.atan2(math.sqrt(spine.x * spine.x + spine.y * spine.y), spine.z))
    lines = []
    for side in ("L", "R"):
        knee = wh(arm, "LowerLeg_" + side)
        ankle = wh(arm, "Foot_" + side)
        toe = wt(arm, "Foot_" + side)
        fl = flex_of(wh(arm, "UpperLeg_" + side), knee, ankle)
        offset = Vector((ankle.x - hip.x, ankle.y - hip.y, 0.0)).length
        print(
            "LAND-BONE", side,
            "flex", round(fl, 1),
            "interior", round(180.0 - fl, 1),
            "hip", tuple(round(v, 3) for v in hip),
            "knee", tuple(round(v, 3) for v in knee),
            "ankle", tuple(round(v, 3) for v in ankle),
            "toe", tuple(round(v, 3) for v in toe),
            "foot-hip-xz", round(offset, 3),
            "knee-fwd", round(knee.y - hip.y, 3),
        )
        lines.append((fl, offset, knee.y - hip.y, knee.z))
        if fl < 85 or fl > 120:
            raise SystemExit("knee flex is not a deep bend")
        if knee.y < hip.y + 0.12:
            raise SystemExit("knee folded backward")
        if knee.z > hip.z - 0.15:
            raise SystemExit("knee is not below the hip")
        if offset > 0.40:
            raise SystemExit("foot is not under the hip")
    if spine.y < 0.05:
        raise SystemExit("torso is not pitched forward")
    if look.z < 0.05 or look.y < 0.0:
        raise SystemExit("head is not looking ahead")
    if head.z < hip.z + 0.12:
        raise SystemExit("head is not above the hips")
    hand = wt(arm, "Hand_L")
    foot = wh(arm, "Foot_L")
    if (hand - foot).length > 0.45:
        raise SystemExit("hand is far from the foot")
    low = lowest_mesh(arm)
    print(
        "LAND-POSE",
        "flex", round(lines[0][0], 1), round(lines[1][0], 1),
        "hipz", round(hip.z, 3),
        "spine-from-up", round(spine_pitch, 1),
        "chest-fwd", round(chest.y - hip.y, 3),
        "head-look", tuple(round(v, 3) for v in look),
        "headz", round(head.z, 3),
        "hand-foot", round((hand - foot).length, 3),
        "lowest", low[0], round(low[1], 3),
    )
    if not low[0].startswith("Mesh_Foot"):
        raise SystemExit("body is not standing on its feet")


def still_land():
    arm = p11.begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_squat(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    plant_hand(arm)
    report_land(arm)
    ring = p10.torus((0.0, 0.05, 0.03), 1.30, 0.045, (0.85, 0.72, 0.48, 1), 1.1)
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
    p9.frame_yaw([arm], [ring] + bits, yaw=1.05, lens=42, fill=0.62, lift=0.35)
    p9.shot("landing-ring")


def word_rotation(tilt_deg):
    """Text width along +Y, height along +Z, normal toward the +X camera."""
    m = Matrix.Identity(3)
    m.col[0] = Vector((0.0, 1.0, 0.0))
    m.col[1] = Vector((0.0, 0.0, 1.0))
    m.col[2] = Vector((1.0, 0.0, 0.0))
    tilt = Matrix.Rotation(math.radians(tilt_deg), 3, "X")
    return (tilt @ m).to_euler()


def comic_word(text, loc, size, tilt):
    font = bpy.data.fonts.load(FONT)
    rot = word_rotation(tilt)
    made = []
    for scale, color, name in (
        (1.2, (0.04, 0.03, 0.03, 1), "Outline"),
        (1.0, (1.0, 0.84, 0.08, 1), "Fill"),
    ):
        curve = bpy.data.curves.new(name + text, "FONT")
        curve.body = text
        curve.font = font
        curve.align_x = "CENTER"
        curve.align_y = "CENTER"
        curve.size = size * scale
        curve.extrude = 0.004
        ob = bpy.data.objects.new(name, curve)
        bpy.context.collection.objects.link(ob)
        ob.location = loc
        ob.rotation_euler = rot
        ob.data.materials.append(p7.emissive(name + "Mat", color, 2.4))
        made.append(ob)
    return made


def word_height_fraction(objects):
    """Pixel fraction of the evaluated graphic. Font curves lie about their raw bound box."""
    from bpy_extras.object_utils import world_to_camera_view
    scene = bpy.context.scene
    cam = scene.camera
    deps = bpy.context.evaluated_depsgraph_get()
    ys = []
    for ob in objects:
        ev = ob.evaluated_get(deps)
        for corner in ev.bound_box:
            co = ev.matrix_world @ Vector(corner)
            p = world_to_camera_view(scene, cam, co)
            if p.z > 0.0:
                ys.append(p.y)
    if len(ys) < 2:
        raise SystemExit("POW is not in front of the camera")
    return max(ys) - min(ys)


def still_punch():
    arm = p11.begin()
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
    p7.bone(other, "Spine", 14, -8, 0)
    p7.bone(other, "Head", 6, -6, 0)
    p9.face_travel(other, base_q)
    p7.drop_to_floor(other)
    p7.place(other, 0.15, 0.95)
    p7.place(arm, 0.72, 0.25)
    gap = p10.seat_fist(arm, other)
    bpy.context.view_layer.update()
    fist = p7.bone_point(arm, "Hand_R")
    shoulder = p7.bone_point(other, "Shoulder_R")
    head = p7.bone_point(other, "Head")
    contact = (fist + shoulder) * 0.5
    # Same side camera as the tag burst. Push the word toward that camera so the chest does not hide it.
    contact = contact + Vector((0.22, 0.0, 0.0))
    if contact.z > shoulder.z + 0.02:
        contact.z = shoulder.z + 0.02
    if contact.z > head.z - 0.22:
        contact.z = head.z - 0.22
    print("punch-contact", tuple(round(v, 3) for v in contact), "gap", round(gap, 3), "headz", round(head.z, 3))
    pieces = []
    outline = p9.star_mesh("PowOut", 0.148, 0.0)
    outline.location = contact + Vector((-0.03, 0.0, 0.0))
    outline.data.materials.append(p7.emissive("PowOutMat", (0.05, 0.04, 0.03, 1), 1.3))
    pieces.append(outline)
    fill = p9.star_mesh("PowFill", 0.118, 0.0)
    fill.location = contact + Vector((-0.01, 0.0, 0.0))
    fill.data.materials.append(p7.emissive("PowFillMat", (1.0, 0.62, 0.08, 1), 1.8))
    pieces.append(fill)
    words = comic_word("POW!", contact + Vector((0.02, 0.0, 0.0)), 0.34, -8)
    pieces.extend(words)
    target = Vector((contact.x, contact.y, shoulder.z))
    p9.view(target, 4.5, math.pi / 2, shoulder.z, 48, shoulder.z)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    frac = word_height_fraction(pieces)
    print("pow-frame-height", round(frac, 4))
    p9.shot("punch-word")


def still_grapple():
    """Sagging grapple line. RopeSag(1) is 0.43 m. Two hook chips."""
    arm = p11.begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p7.bone(arm, "UpperArm_L", -158, 14, 10)
    p7.bone(arm, "LowerArm_L", -8)
    p7.bone(arm, "UpperArm_R", -36, -24, 0)
    p7.bone(arm, "LowerArm_R", -62)
    p7.bone(arm, "Hips", 16, 0, 8)
    p7.bone(arm, "Spine", 24, 0, 6)
    p7.bone(arm, "Head", -10)
    p7.bone(arm, "UpperLeg_L", 22)
    p7.bone(arm, "UpperLeg_R", -16)
    p7.bone(arm, "LowerLeg_L", -34)
    p7.bone(arm, "LowerLeg_R", -20)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 1.25
    bpy.context.view_layer.update()
    hand = p7.bone_point(arm, "Hand_L")
    hook = Vector((hand.x + 0.15, hand.y + 2.6, hand.z + 0.85))
    sag = 0.43
    pts = []
    for i in range(8):
        t = i / 7.0
        bell = 4.0 * t * (1.0 - t)
        lateral = 0.04 * bell * math.sin(t * 6.2)
        p = hand.lerp(hook, t)
        p.z -= sag * bell
        p.x += lateral
        pts.append(p)
    pieces = []
    for i in range(len(pts) - 1):
        pieces.append(p10.ribbon(pts[i], pts[i + 1], 0.045, (0.78, 0.62, 0.38, 1)))
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.09, minor_radius=0.02, location=hook, major_segments=20, minor_segments=8)
    hook_ob = bpy.context.active_object
    hook_ob.rotation_euler = (math.pi / 2, 0.0, 0.3)
    hook_ob.data.materials.append(p7.principled("Hook", (0.55, 0.56, 0.58, 1), 0.35))
    pieces.append(hook_ob)
    for i, off in enumerate((Vector((0.08, -0.05, -0.08)), Vector((-0.06, 0.08, -0.12)))):
        pieces.append(p10.chunk(hook + off, 0.045, (0.62, 0.42, 0.22, 1)))
    bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=hook.z + 0.2, location=(hook.x, hook.y, (hook.z + 0.2) * 0.5))
    post = bpy.context.active_object
    post.data.materials.append(p7.principled("HookPost", (0.35, 0.36, 0.38, 1), 0.55))
    pieces.append(post)
    mid = pts[3]
    print("grapple-sag", round(hand.lerp(hook, 0.5).z - mid.z, 3), "points", len(pts))
    p9.frame_yaw([arm], pieces, yaw=1.15, lens=40, fill=0.62, lift=0.15)
    p9.shot("grapple-rope")


if __name__ == "__main__":
    only = os.environ.get("PASS12_ONLY", "")
    shots = [
        ("dust", p11.still_dust_close),
        ("wide", p11.still_dust_wide),
        ("land", still_land),
        ("punch", still_punch),
        ("pad", p11.still_pad),
        ("zip", p11.still_zip),
        ("slide", p11.still_slide),
        ("grapple", still_grapple),
    ]
    os.makedirs(OUT, exist_ok=True)
    wanted = [s.strip() for s in only.split(",") if s.strip()] if only else None
    for name, fn in shots:
        if wanted and name not in wanted:
            continue
        fn()
