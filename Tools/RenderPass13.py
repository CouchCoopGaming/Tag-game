#!/usr/bin/env python3
"""Pass 13 stills. The punch lands, the slide stays feet-first, the line and the rope are clear.

Run:
  blender --background --python Tools/RenderPass13.py
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

OUT = os.environ.get("PASS13_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass13"))
p9.OUT = OUT
p9.SAMPLES = int(os.environ.get("PASS13_SAMPLES", "12"))

GROUND = ("Lawn", "Pad")


def begin():
    """Park light and ground. No trees. The last pass put a tree over the rider."""
    arm = p9.setup("park")
    p11.post()
    return arm


def wh(arm, name):
    bpy.context.view_layer.update()
    return arm.matrix_world @ arm.pose.bones[name].head


def wt(arm, name):
    bpy.context.view_layer.update()
    return arm.matrix_world @ arm.pose.bones[name].tail


def flex(arm, parent, child, tip):
    a = wh(arm, parent)
    b = wh(arm, child)
    c = wh(arm, tip)
    u = (b - a).normalized()
    v = (c - b).normalized()
    d = max(-1.0, min(1.0, u.dot(v)))
    return math.degrees(math.acos(d))


def centroid(pts):
    c = Vector((0.0, 0.0, 0.0))
    for p in pts:
        c += p
    return c / max(len(pts), 1)


def screen_box(obs, step=5):
    """xmin, ymin, xmax, ymax. Y grows down the image, matching the still."""
    from bpy_extras.object_utils import world_to_camera_view
    scene = bpy.context.scene
    cam = scene.camera
    deps = bpy.context.evaluated_depsgraph_get()
    xs, ys = [], []
    for ob in obs:
        if ob is None or ob.type != "MESH":
            continue
        ev = ob.evaluated_get(deps)
        mw = ob.matrix_world
        if ob.type == "FONT":
            # Curve bodies do not to_mesh into the glyphs until they are evaluated as a bound.
            pts = [mw @ Vector(corner) for corner in ev.bound_box]
        else:
            me = ev.to_mesh()
            pts = []
            for i, v in enumerate(me.vertices):
                if i % step:
                    continue
                pts.append(mw @ v.co)
            ev.to_mesh_clear()
        for co in pts:
            p = world_to_camera_view(scene, cam, co)
            if p.z <= 0.0:
                continue
            xs.append(p.x)
            ys.append(1.0 - p.y)
    if not xs:
        return None
    return (min(xs), min(ys), max(xs), max(ys))


def box_area(box):
    return max(0.0, box[2] - box[0]) * max(0.0, box[3] - box[1])


def overlaps(a, b):
    return not (a[2] < b[0] or b[2] < a[0] or a[3] < b[1] or b[3] < a[1])


def fmt_box(box):
    if box is None:
        return "offscreen"
    return "x %.3f-%.3f y %.3f-%.3f area %.3f" % (box[0], box[2], box[1], box[3], box_area(box))


def runner_meshes(arms):
    found = []
    for arm in arms:
        found.extend(p7.meshes(arm))
    return found


def report_frame(arms, effects, label):
    """Fail if a prop covers the runner or more than 10% of the frame."""
    runners = runner_meshes(arms)
    rbox = screen_box(runners, 4)
    print("BOX", label, "runner", fmt_box(rbox))
    if rbox is None:
        raise SystemExit(label + " runner is off screen")
    if rbox[0] < -0.02 or rbox[1] < -0.02 or rbox[2] > 1.02 or rbox[3] > 1.02:
        raise SystemExit(label + " runner is clipped")
    keep = set(ob.name for ob in runners)
    for ob in effects:
        keep.add(ob.name)
        ebox = screen_box([ob], 2)
        print("BOX", label, ob.name, fmt_box(ebox))
    for ob in list(bpy.data.objects):
        if ob.type != "MESH" or ob.name in keep:
            continue
        if ob.name.split(".")[0] in GROUND:
            continue
        pbox = screen_box([ob], 3)
        if pbox is None:
            continue
        area = box_area(pbox)
        hit = rbox is not None and overlaps(pbox, rbox)
        if area > 0.10 or hit:
            raise SystemExit("%s prop %s covers the frame (area %.2f overlap %s)" % (label, ob.name, area, hit))
    return rbox


def tube(a, b, radius, color, strength=0.0):
    """A cylinder built along the segment. Tracking a Y-up camera flips a stock cylinder edge-on."""
    a = Vector(a)
    b = Vector(b)
    d = b - a
    if d.length < 1e-4:
        b = a + Vector((0.0, 0.0, 0.01))
        d = b - a
    z = d.normalized()
    up = Vector((0.0, 0.0, 1.0))
    if abs(z.dot(up)) > 0.85:
        up = Vector((1.0, 0.0, 0.0))
    x = up.cross(z).normalized()
    y = z.cross(x).normalized()
    n = 8
    coords = []
    for end in (a, b):
        for i in range(n):
            ang = i / n * math.tau
            coords.append(tuple(end + (x * math.cos(ang) + y * math.sin(ang)) * radius))
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    mesh = bpy.data.meshes.new("Tube")
    mesh.from_pydata(coords, [], faces)
    mesh.update()
    ob = bpy.data.objects.new("Tube", mesh)
    bpy.context.collection.objects.link(ob)
    if strength > 0.0:
        ob.data.materials.append(p7.emissive("Tube", color, strength))
    else:
        ob.data.materials.append(p7.principled("Tube", color, 0.45))
    return ob


def spark(loc, radius, color, strength):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=radius, location=loc)
    ob = bpy.context.active_object
    ob.data.materials.append(p7.emissive("Spark", color, strength))
    return ob


def pose_jab(arm):
    """Right arm extended forward. The old -75/-55 roll kept the fist up by the head."""
    p7.bone(arm, "Hips", 8, -12, 0)
    p7.bone(arm, "Spine", 14, -8, 0)
    p7.bone(arm, "Chest", 6)
    p7.bone(arm, "Head", -6)
    p7.bone(arm, "Shoulder_R", 0, 0, -6)
    p7.bone(arm, "UpperArm_R", -62, 22, -8)
    p7.bone(arm, "LowerArm_R", -16)
    p7.bone(arm, "UpperArm_L", 34, -22, 16)
    p7.bone(arm, "LowerArm_L", -70)
    p7.bone(arm, "UpperLeg_L", 24)
    p7.bone(arm, "UpperLeg_R", -14)
    p7.bone(arm, "LowerLeg_L", -30)
    p7.bone(arm, "LowerLeg_R", -18)


def assert_extended(arm):
    shoulder = wh(arm, "Shoulder_R")
    hand = wt(arm, "Hand_R")
    head = wh(arm, "Head")
    bend = flex(arm, "Shoulder_R", "LowerArm_R", "Hand_R")
    fwd = hand.y - shoulder.y
    print("jab fwd", round(fwd, 3), "up", round(hand.z - shoulder.z, 3),
          "flex", round(bend, 1), "hand-head", round((hand - head).length, 3))
    if fwd < 0.45:
        raise SystemExit("fist is not extended")
    if bend > 55:
        raise SystemExit("elbow is cocked")
    if (hand - head).length < 0.32:
        raise SystemExit("fist is still by the head")
    if wh(arm, "Head").z < wh(arm, "Hips").z:
        raise SystemExit("puncher is not upright")


def seat_on_chest(tagger, runner):
    """Slide the puncher in the ground plane until the fist meets the chest."""
    for _ in range(40):
        hand = p10.samples(tagger, ("Mesh_Hand_R",), 2)
        chest = p10.samples(runner, ("Mesh_Chest", "Mesh_Shoulder_R"), 4)
        if not hand or not chest:
            raise SystemExit("missing fist or chest")
        hc = centroid(hand)
        target = min(chest, key=lambda p: (Vector((p.x, p.y, hc.z)) - hc).length)
        delta = Vector((target.x - hc.x, target.y - hc.y, 0.0))
        touch = min((p - q).length for p in hand for q in chest)
        print("jab-touch", round(touch, 3))
        if 0.02 <= touch <= 0.08 and delta.length < 0.12:
            return touch
        step = min(0.12, max(0.02, delta.length * 0.55))
        if delta.length < 1e-4:
            break
        d = delta.normalized()
        if touch < 0.02:
            tagger.location.x -= d.x * 0.03
            tagger.location.y -= d.y * 0.03
        else:
            tagger.location.x += d.x * step
            tagger.location.y += d.y * step
        bpy.context.view_layer.update()
    raise SystemExit("fist did not reach the chest")


def still_punch():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    runner = p7.clone_arm(arm)
    p9.tint_hier(runner, None, p9.PLAYER)
    p9.tint_hier(arm, None, p9.TAGGER)
    p7.reset_arm(arm, base_q)
    pose_jab(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    assert_extended(arm)
    p7.reset_arm(runner, base_q)
    p7.pose_sprint(runner)
    p7.bone(runner, "Spine", 12, -6, 0)
    p7.bone(runner, "Head", 4, -4, 0)
    p9.face_travel(runner, base_q)
    p7.drop_to_floor(runner)
    if wh(runner, "Head").z < wh(runner, "Hips").z:
        raise SystemExit("runner is not upright")
    # Camera is on +X. The puncher stands on that side and reaches in.
    p7.place(runner, 0.05, 0.55)
    p7.place(arm, 0.7, -0.15)
    touch = seat_on_chest(arm, runner)
    assert_extended(arm)
    fist_pts = p10.samples(arm, ("Mesh_Hand_R",), 2)
    fist = centroid(fist_pts)
    # The word sits on the contact. A few centimetres toward the camera keeps it readable.
    contact = fist + Vector((0.04, 0.0, 0.0))
    print("punch-contact", tuple(round(v, 3) for v in contact), "touch", round(touch, 3))
    pieces = []
    outline = p9.star_mesh("PowOut", 0.148, 0.0)
    outline.location = contact + Vector((-0.03, 0.0, 0.0))
    outline.data.materials.append(p7.emissive("PowOutMat", (0.05, 0.04, 0.03, 1), 1.3))
    pieces.append(outline)
    fill = p9.star_mesh("PowFill", 0.118, 0.0)
    fill.location = contact + Vector((-0.015, 0.0, 0.0))
    fill.data.materials.append(p7.emissive("PowFillMat", (1.0, 0.62, 0.08, 1), 1.8))
    pieces.append(fill)
    words = p12.comic_word("POW!", contact + Vector((0.02, 0.0, 0.0)), 0.34, -8)
    pieces.extend(words)
    target = Vector((contact.x, contact.y, fist.z))
    p9.view(target, 4.5, math.pi / 2, fist.z, 48, fist.z)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    print("pow-frame-height", round(p12.word_height_fraction(pieces), 4))
    report_frame([arm, runner], pieces, "punch")
    p9.shot("punch-word")


def pose_slide_body(arm):
    """Feet lead, chest tips back, one leg folds under. Root 82 was the inversion."""
    p7.bone(arm, "Root", 0)
    p7.bone(arm, "Hips", -25)
    p7.bone(arm, "Spine", -12)
    p7.bone(arm, "Chest", -6)
    p7.bone(arm, "Head", 16)
    p7.bone(arm, "UpperLeg_L", -40)
    p7.bone(arm, "LowerLeg_L", 5)
    p7.bone(arm, "Foot_L", -20)
    p7.bone(arm, "UpperLeg_R", 70)
    p7.bone(arm, "LowerLeg_R", -120)
    p7.bone(arm, "Foot_R", 18)
    p7.bone(arm, "UpperArm_L", -28, 12, 0)
    p7.bone(arm, "LowerArm_L", -18)


def plant_trail_hand(arm):
    """Yaw -28 left the hand in the air. Pitch 120, yaw 20, elbow -60 trails it."""
    hip = wh(arm, "Hips")
    p7.bone(arm, "UpperArm_R", 120, 20, 12)
    p7.bone(arm, "LowerArm_R", -60)
    bpy.context.view_layer.update()
    hand = wt(arm, "Hand_R")
    print("slide-hand z", round(hand.z, 3), "dy", round(hand.y - hip.y, 3))
    if hand.y > hip.y - 0.15 or not (0.04 < hand.z < 0.36):
        raise SystemExit("trailing hand left the ground behind the hip")


def still_slide():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    pose_slide_body(arm)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    plant_trail_hand(arm)
    # The hand search can drop the mesh. Plant again, then keep the hand above the feet.
    p7.drop_to_floor(arm)
    hip = wh(arm, "Hips")
    head = wh(arm, "Head")
    lead = wh(arm, "Foot_L")
    tuck = wh(arm, "Foot_R")
    print("slide hipz", round(hip.z, 3), "headz", round(head.z, 3),
          "head-dy", round(head.y - hip.y, 3),
          "lead-dy", round(lead.y - hip.y, 3), "leadz", round(lead.z, 3),
          "tuck-dy", round(tuck.y - hip.y, 3), "tuckz", round(tuck.z, 3))
    if head.z < hip.z + 0.2 or head.z < 0.7:
        raise SystemExit("slide is still inverted")
    if lead.y < hip.y + 0.4:
        raise SystemExit("lead foot is not ahead")
    if head.y > hip.y:
        raise SystemExit("head is leading the slide")
    # The lead heel is the scrape. A side camera looks through a flat ribbon, so the
    # sparks and a short dust tube carry the trail.
    heel = lead
    bits = []
    for i in range(7):
        ang = i / 7.0 * math.tau
        bits.append(spark(
            (heel.x + math.cos(ang) * 0.07, heel.y - 0.06 - (i % 3) * 0.07, 0.07 + (i % 2) * 0.04),
            0.05,
            (1.0, 0.55, 0.12, 1),
            0.9,
        ))
    bits.append(tube(
        (heel.x, heel.y - 0.02, 0.06),
        (heel.x, heel.y - 0.9, 0.06),
        0.04,
        (0.86, 0.72, 0.56, 1),
    ))
    ribbon = p10.ribbon(
        (heel.x, heel.y - 0.02, 0.03),
        (heel.x, heel.y - 1.1, 0.03),
        0.18,
        (0.72, 0.62, 0.48, 0.9),
    )
    bits.append(ribbon)
    p9.frame_yaw([arm], bits, yaw=math.pi / 2, lens=48, fill=0.62, lift=0.55)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    report_frame([arm], bits, "slide")
    p9.shot("slide-scrape")


def still_zip():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p10.pose_hang(arm)
    p7.bone(arm, "Hips", 12, 0, 4)
    p7.bone(arm, "Spine", 10)
    p7.bone(arm, "Head", 6)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 0.85
    bpy.context.view_layer.update()
    if wh(arm, "Head").z < wh(arm, "Hips").z:
        raise SystemExit("zip rider is not upright")
    left = wt(arm, "Hand_L")
    right = wt(arm, "Hand_R")
    grip = (left + right) * 0.5
    if grip.z < wh(arm, "Head").z - 0.05:
        raise SystemExit("hands are not on a line above the body")
    # Cable through the hands. A tube, so the side camera sees it.
    y0, y1 = grip.y - 1.6, grip.y + 1.6
    pieces = []
    a = Vector((grip.x, y0, grip.z + 0.04))
    b = Vector((grip.x, y1, grip.z - 0.02))
    pieces.append(tube(a, b, 0.028, (0.22, 0.23, 0.25, 1)))
    trolley = Vector((grip.x, grip.y, grip.z))
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.07, minor_radius=0.018, location=trolley, major_segments=16, minor_segments=8)
    hook = bpy.context.active_object
    hook.rotation_euler = (0.0, math.pi / 2, 0.0)
    hook.data.materials.append(p7.principled("Trolley", (0.45, 0.46, 0.48, 1), 0.3))
    pieces.append(hook)
    # Eight sparks, all on the trolley. Not a string of lights down the cable.
    for i in range(8):
        ang = i / 8.0 * math.tau
        loc = trolley + Vector((math.cos(ang) * 0.06, -0.04 - (i % 3) * 0.03, math.sin(ang) * 0.05))
        if (loc - trolley).length > 0.28:
            raise SystemExit("spark left the trolley")
        pieces.append(spark(loc, 0.055, (0.72, 0.22, 1.0, 1), 1.1))
    print("zip-grip", tuple(round(v, 3) for v in grip), "headz", round(wh(arm, "Head").z, 3))
    p9.frame_yaw([arm], pieces, yaw=math.pi / 2, lens=46, fill=0.62, lift=0.1)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    report_frame([arm], pieces, "zip")
    p9.shot("zip-line")


def still_grapple():
    arm = begin()
    base_q = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base_q)
    p7.bone(arm, "UpperArm_L", -150, 10, 8)
    p7.bone(arm, "LowerArm_L", -10)
    p7.bone(arm, "UpperArm_R", -20, -24, 0)
    p7.bone(arm, "LowerArm_R", -48)
    p7.bone(arm, "Hips", 10, 0, 4)
    p7.bone(arm, "Spine", 14)
    p7.bone(arm, "Head", -4)
    p7.bone(arm, "UpperLeg_L", 16)
    p7.bone(arm, "UpperLeg_R", -12)
    p7.bone(arm, "LowerLeg_L", -28)
    p7.bone(arm, "LowerLeg_R", -16)
    p9.face_travel(arm, base_q)
    p7.drop_to_floor(arm)
    arm.location.z += 0.35
    bpy.context.view_layer.update()
    if wh(arm, "Head").z < wh(arm, "Hips").z:
        raise SystemExit("grapple rider is not upright")
    bone_head = wh(arm, "Hand_L")
    bone_tail = wt(arm, "Hand_L")
    mesh = p10.samples(arm, ("Mesh_Hand_L",), 1)
    if not mesh:
        raise SystemExit("no left hand mesh")
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
    hook = Vector((start.x + 0.05, start.y + 2.15, start.z + 0.55))
    sag = 0.43
    pts = []
    for i in range(8):
        t = i / 7.0
        bell = 4.0 * t * (1.0 - t)
        p = Vector(start).lerp(hook, t)
        p.z -= sag * bell
        pts.append(p)
    pts[0] = Vector(start)
    pieces = []
    if (skin - start).length > 0.008:
        pieces.append(tube(skin, start, 0.016, (0.62, 0.48, 0.30, 1)))
    for i in range(len(pts) - 1):
        pieces.append(tube(pts[i], pts[i + 1], 0.016, (0.62, 0.48, 0.30, 1)))
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.08, minor_radius=0.018, location=hook, major_segments=16, minor_segments=8)
    hook_ob = bpy.context.active_object
    hook_ob.rotation_euler = (math.pi / 2, 0.2, 0.0)
    hook_ob.data.materials.append(p7.principled("Hook", (0.55, 0.56, 0.58, 1), 0.35))
    pieces.append(hook_ob)
    for off in (Vector((0.06, -0.04, -0.06)), Vector((-0.05, 0.05, -0.08))):
        pieces.append(p10.chunk(hook + off, 0.04, (0.62, 0.42, 0.22, 1)))
    mid = pts[0].lerp(pts[-1], 0.5)
    # The sample nearest the middle carries the sag.
    deepest = min(pts, key=lambda p: p.z)
    print("grapple-sag", round(mid.z - deepest.z, 3), "points", len(pts))
    p9.frame_yaw([arm], pieces, yaw=math.pi / 2, lens=42, fill=0.7, lift=0.2)
    bpy.context.scene.camera.data.sensor_fit = "HORIZONTAL"
    report_frame([arm], pieces, "grapple")
    p9.shot("grapple-rope")


if __name__ == "__main__":
    only = os.environ.get("PASS13_ONLY", "")
    shots = [
        ("punch", still_punch),
        ("slide", still_slide),
        ("zip", still_zip),
        ("grapple", still_grapple),
    ]
    os.makedirs(OUT, exist_ok=True)
    wanted = [s.strip() for s in only.split(",") if s.strip()] if only else None
    for name, fn in shots:
        if wanted and name not in wanted:
            continue
        fn()
