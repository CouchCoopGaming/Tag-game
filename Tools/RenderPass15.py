#!/usr/bin/env python3
"""Pass 15 stills. Contacts are measured on the evaluated mesh, not the bone heads.

The mannequin is rigid pieces parented to bones. A bone 20 cm up can still leave
the pelvis mesh hovering, and a foot bone beside a box is not a plant.

Run:
  blender --background --python Tools/RenderPass15.py
"""
import math
import os
import sys

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
import RenderPass7 as p7
import RenderPass9 as p9
import RenderPass10 as p10
import RenderPass13 as p13
import RenderPass14 as p14

OUT = os.environ.get("PASS15_OUT", os.path.join(ROOT, "Docs", "AnimStills", "pass15"))
p9.OUT = OUT
p9.SAMPLES = int(os.environ.get("PASS15_SAMPLES", "12"))
p13.OUT = OUT
p14.OUT = OUT

DIRT_Z = 0.006
# Pelvis mesh sits 1.2 cm off the dirt after the lowest other mesh is cleared.
PELVIS_CLEAR = 0.012


def begin():
    return p14.begin()


def pts(arm, name):
    cloud = p10.samples(arm, (name,), 1)
    if not cloud:
        raise SystemExit("missing mesh " + name)
    return cloud


def min_z(arm, name):
    return min(p.z for p in pts(arm, name))


def extreme(cloud, key, mode, tol=0.015, cap=40):
    pivot = min(key(p) for p in cloud) if mode == "min" else max(key(p) for p in cloud)
    if mode == "min":
        chosen = [p for p in cloud if key(p) <= pivot + tol]
    else:
        chosen = [p for p in cloud if key(p) >= pivot - tol]
    chosen.sort(key=key, reverse=(mode == "max"))
    return chosen[:cap], pivot


def lowest_mesh(arm):
    best = ("", 1e9)
    for ob in p7.meshes(arm):
        name = ob.name.split(".")[0]
        z = min_z(arm, name)
        if z < best[1]:
            best = (name, z)
    return best


def seat_pelvis(arm, surface_z):
    """Put the pelvis mesh on the surface. Bone heads sit inside the piece."""
    bpy.context.view_layer.update()
    arm.location.z += (surface_z + PELVIS_CLEAR) - min_z(arm, "Mesh_Hips")
    bpy.context.view_layer.update()
    low_name, low_z = lowest_mesh(arm)
    if low_z < surface_z:
        arm.location.z += (surface_z + 0.002) - low_z
        bpy.context.view_layer.update()
    hip = min_z(arm, "Mesh_Hips") - surface_z
    low = lowest_mesh(arm)[1] - surface_z
    print("MESH pelvis", round(hip, 4), "lowest", low_name, round(low, 4), "surface", surface_z)
    if hip < 0.0 or hip > 0.02:
        raise SystemExit("pelvis mesh is not on the ground (%.3f m)" % hip)
    return hip


def pose_slide(arm):
    """Butt on the dirt, lead heel low, trail leg folded under, head back."""
    p7.bone(arm, "Hips", -18)
    p7.bone(arm, "Spine", -8)
    p7.bone(arm, "Chest", -4)
    p7.bone(arm, "Head", 12)
    p7.bone(arm, "UpperLeg_L", -75)
    p7.bone(arm, "LowerLeg_L", 15)
    p7.bone(arm, "Foot_L", 16)
    p7.bone(arm, "UpperLeg_R", 140)
    p7.bone(arm, "LowerLeg_R", -150)
    p7.bone(arm, "Foot_R", 8)
    p7.bone(arm, "UpperArm_L", -20, 8, 0)
    p7.bone(arm, "LowerArm_L", -16)
    p7.bone(arm, "UpperArm_R", 155, 12, 4)
    p7.bone(arm, "LowerArm_R", -115)


def lay_dirt(arm):
    """Brown path under the slider, above the lawn and the park pad."""
    bpy.data.objects["Pad"].hide_render = True
    hip = p14.wh(arm, "Hips")
    bpy.ops.mesh.primitive_plane_add(size=1, location=(hip.x, hip.y, DIRT_Z))
    dirt = bpy.context.active_object
    dirt.name = "Dirt"
    dirt.scale = (36, 36, 1)
    dirt.data.materials.append(p9.noisy(
        "DirtPath", (0.42, 0.28, 0.14, 1), (0.62, 0.44, 0.24, 1), 9.0, 0.94))
    return DIRT_Z


def lay_concrete():
    lawn = bpy.data.objects["Lawn"]
    pad = bpy.data.objects["Pad"]
    lawn.hide_render = True
    pad.hide_render = False
    pad.scale = (40, 40, 1)
    pad.location.z = 0.0
    pad.data.materials.clear()
    pad.data.materials.append(p7.principled("ConcreteGround", (0.62, 0.63, 0.66, 1), 0.72))
    return 0.0


def nearest(arm, name, key):
    cloud = pts(arm, name)
    pick, _pivot = extreme(cloud, key, "min")
    return pick[0], pick


def body_min_x(arm, skip):
    best = 1e9
    for ob in p7.meshes(arm):
        name = ob.name.split(".")[0]
        if name in skip:
            continue
        best = min(best, min(p.x for p in pts(arm, name)))
    return best


def place_wall(foot_pt, length, height, name="Wall"):
    """Axis-aligned wall. The face toward +X (the camera) meets the foot."""
    thickness = 0.24
    face_x = foot_pt.x - 0.012
    center = (face_x - thickness * 0.5, foot_pt.y, height * 0.5)
    wall = p14.add_box(name, center, (thickness, length, height), (0.62, 0.63, 0.66, 1))
    print("PROP", name, "face", round(face_x, 4), "len", length, "h", height,
          "footz", round(foot_pt.z, 3))
    if height < 3.0 or length < 6.0:
        raise SystemExit("wall is undersized")
    if foot_pt.z < 0.35 or foot_pt.z > height - 0.35:
        raise SystemExit("foot is on the wall edge, not the face")
    return wall, face_x


def assert_foot_plant(arm, foot_name, face_x):
    foot = pts(arm, foot_name)
    near = min(p.x for p in foot)
    gap = near - face_x
    others = body_min_x(arm, {foot_name})
    nearest_rows = []
    for ob in p7.meshes(arm):
        mesh_name = ob.name.split(".")[0]
        if mesh_name == foot_name:
            continue
        nearest_rows.append((min(p.x for p in pts(arm, mesh_name)), mesh_name))
    nearest_rows.sort()
    print(
        "MESH", foot_name, "gap_cm", round(gap * 100.0, 2),
        "other_cm", round((others - face_x) * 100.0, 2),
        "nearest", [(mesh_name, round((x - face_x) * 100.0, 1)) for x, mesh_name in nearest_rows[:5]],
    )
    if gap < 0.0 or gap > 0.02:
        raise SystemExit("foot is not planted on the wall")
    if others < face_x - 0.002:
        raise SystemExit("the body crosses the wall ahead of the foot")
    return near


def pose_wall_run(arm):
    """20° into the wall, inner foot stepped onto the face. WallPose run, wall on the left."""
    p7.bone(arm, "Hips", 6, 0, 9)
    p7.bone(arm, "Spine", 12, 0, -20)
    p7.bone(arm, "Chest", 4, 0, -8)
    p7.bone(arm, "Head", -4, 0, 5)
    p7.bone(arm, "UpperLeg_L", 20, 20, -20)
    p7.bone(arm, "LowerLeg_L", -24)
    p7.bone(arm, "Foot_L", 8)
    p7.bone(arm, "UpperLeg_R", -18)
    p7.bone(arm, "LowerLeg_R", -16)
    p7.bone(arm, "Foot_R", 0)
    p7.bone(arm, "UpperArm_L", 8, 20, 0)
    p7.bone(arm, "LowerArm_L", -24)
    p7.bone(arm, "UpperArm_R", -70, -10, 0)
    p7.bone(arm, "LowerArm_R", -16)


def pose_wall_jump(arm):
    """Shove off the left foot. Arms swing away from the wall. WallJumpPose lean."""
    p7.bone(arm, "Hips", -10, 0, 8)
    p7.bone(arm, "Spine", -22, 0, -18)
    p7.bone(arm, "Head", -16, 0, 5)
    p7.bone(arm, "UpperLeg_L", 28, 24, -22)
    p7.bone(arm, "LowerLeg_L", -70)
    p7.bone(arm, "Foot_L", 8)
    p7.bone(arm, "UpperLeg_R", 78)
    p7.bone(arm, "LowerLeg_R", -100)
    p7.bone(arm, "UpperArm_L", 36, -22, 0)
    p7.bone(arm, "LowerArm_L", -18)
    p7.bone(arm, "UpperArm_R", 28, 16, 0)
    p7.bone(arm, "LowerArm_R", -20)


def pose_vault(arm):
    """Hands down on the box, hips clear of the top, both legs up and over."""
    p7.bone(arm, "Hips", -10)
    p7.bone(arm, "Spine", 6)
    p7.bone(arm, "Head", -4)
    p7.bone(arm, "UpperArm_L", 34, 16, 8)
    p7.bone(arm, "LowerArm_L", -14)
    p7.bone(arm, "UpperArm_R", 34, -14, -6)
    p7.bone(arm, "LowerArm_R", -16)
    p7.bone(arm, "UpperLeg_L", -40)
    p7.bone(arm, "LowerLeg_L", -80)
    p7.bone(arm, "UpperLeg_R", 20)
    p7.bone(arm, "LowerLeg_R", -140)


def project(cam, point):
    scene = bpy.context.scene
    res_x = scene.render.resolution_x
    res_y = scene.render.resolution_y
    ndc = world_to_camera_view(scene, cam, Vector(point))
    return ndc.x * res_x, (1.0 - ndc.y) * res_y


def fit_ortho(cam, cloud, view_dir):
    """Same fit as the lane ortho helper. A 10 cm vertical pair must read back as 10 cm."""
    scene = bpy.context.scene
    center = sum((Vector(p) for p in cloud), Vector()) / len(cloud)
    cam.data.type = "ORTHO"
    cam.data.sensor_fit = "VERTICAL"
    cam.data.clip_start = 0.01
    cam.data.clip_end = 80.0
    height_m = 3.0
    view = view_dir.normalized()
    res_x = scene.render.resolution_x
    res_y = scene.render.resolution_y
    for _ in range(6):
        cam.data.ortho_scale = height_m
        cam.location = center + view * 8.0
        p7.look_at(cam, center)
        bpy.context.view_layer.update()
        proj = [world_to_camera_view(scene, cam, Vector(p)) for p in cloud]
        minx = min(p.x for p in proj)
        maxx = max(p.x for p in proj)
        miny = min(p.y for p in proj)
        maxy = max(p.y for p in proj)
        cx = (minx + maxx) * 0.5
        cy = (miny + maxy) * 0.5
        view_h = cam.data.ortho_scale
        view_w = view_h * (res_x / float(res_y))
        right = cam.matrix_world.to_3x3() @ Vector((1.0, 0.0, 0.0))
        upv = cam.matrix_world.to_3x3() @ Vector((0.0, 1.0, 0.0))
        center = center + right * ((cx - 0.5) * view_w) + upv * ((cy - 0.5) * view_h)
        span_y = max(0.4, (maxy - miny) * view_h)
        span_x = max(0.4, (maxx - minx) * view_w)
        height_m = max(span_y, span_x * res_y / float(res_x)) * 1.28
    cam.data.ortho_scale = height_m
    cam.location = center + view * 8.0
    p7.look_at(cam, center)
    bpy.context.view_layer.update()
    base = Vector((center.x, center.y, center.z))
    raised = base + Vector((0.0, 0.0, 0.10))
    ax, ay = project(cam, base)
    bx, by = project(cam, raised)
    mpp = cam.data.ortho_scale / float(res_y)
    print("SCALE_CHECK", round(math.hypot(ax - bx, ay - by) * mpp * 100.0, 2))
    return mpp


def stamp(path, cam, dots):
    image = Image.open(path).convert("RGBA")
    draw = ImageDraw.Draw(image)
    for point in dots:
        x, y = project(cam, point)
        r = 4
        draw.ellipse((x - r, y - r, x + r, y + r), fill=(255, 0, 0, 255))
    image.save(path)


def render_ortho(cam, path, gaps, dots, view_dir):
    cloud = []
    for item in gaps:
        cloud.append(item["a"])
        cloud.append(item["b"])
    cloud.extend(dots)
    mpp = fit_ortho(cam, cloud, view_dir)
    scene = bpy.context.scene
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    stamp(path, cam, dots)
    label = os.path.basename(path).replace("-ortho.png", "")
    for item in gaps:
        ax, ay = project(cam, item["a"])
        bx, by = project(cam, item["b"])
        pixels = math.hypot(ax - bx, ay - by)
        print(
            "GAP", label, item["name"],
            "px", round(pixels, 1),
            "px_cm", round(pixels * mpp * 100.0, 1),
            "world_cm", round(item["cm"], 1),
        )


def gap(name, mesh_pt, surface_pt):
    delta = Vector(mesh_pt) - Vector(surface_pt)
    return {"name": name, "a": Vector(mesh_pt), "b": Vector(surface_pt), "cm": delta.length * 100.0}


def corners(ob):
    bpy.context.view_layer.update()
    return [ob.matrix_world @ Vector(c) for c in ob.bound_box]


def report_beauty(arms, effects, label, contacts=()):
    """Print screen boxes. A wall behind the runner may fill the frame.

    A contact aborts only when it is closer to the camera than the head and
    rises into the head. Dirt is ground.
    """
    runners = p13.runner_meshes(arms)
    rbox = p13.screen_box(runners, 4)
    print("BOX", label, "runner", p13.fmt_box(rbox))
    if rbox is None:
        raise SystemExit(label + " runner is off screen")
    if rbox[0] < -0.02 or rbox[1] < -0.02 or rbox[2] > 1.02 or rbox[3] > 1.02:
        raise SystemExit(label + " runner is clipped")
    heads = [ob for ob in runners if ob.name.split(".")[0] in p14.HEADS]
    hbox = p13.screen_box(heads, 2) if heads else None
    head_pts = []
    for ob in heads:
        head_pts.extend(pts(arms[0], ob.name.split(".")[0]))
    head_max_x = max((p.x for p in head_pts), default=0.0)
    head_min_z = min((p.z for p in head_pts), default=0.0)
    keep = set(ob.name for ob in runners)
    ground = set(p13.GROUND) | {"Dirt"}
    contact_ids = set(id(ob) for ob in contacts)
    for ob in list(effects) + list(contacts):
        keep.add(ob.name)
        ebox = p13.screen_box([ob], 1)
        print("BOX", label, ob.name, p13.fmt_box(ebox))
        if id(ob) in contact_ids and ebox is not None and hbox is not None and p13.overlaps(ebox, hbox):
            cs = corners(ob)
            front = max(p.x for p in cs)
            top = max(p.z for p in cs)
            print(
                "DEPTH", label, ob.name,
                "front_cm", round((front - head_max_x) * 100.0, 1),
                "top_vs_head_cm", round((top - head_min_z) * 100.0, 1),
            )
            if front > head_max_x + 0.02 and top > head_min_z - 0.02:
                raise SystemExit(label + " contact covers the head")
    for ob in list(bpy.data.objects):
        if ob.type != "MESH" or ob.name in keep:
            continue
        if ob.hide_render:
            continue
        if ob.name.split(".")[0] in ground:
            continue
        pbox = p13.screen_box([ob], 3)
        if pbox is None:
            continue
        area = p13.box_area(pbox)
        hit = p13.overlaps(pbox, rbox)
        if area > 0.10 or hit:
            raise SystemExit("%s prop %s covers the frame (area %.2f overlap %s)" % (label, ob.name, area, hit))
    return rbox


def shot_ortho_then_beauty(arm, effects, gaps, dots, ortho_name, beauty_name, lens=48, contacts=(), view=None):
    scene = bpy.context.scene
    cam = scene.camera or p7.cam(Vector((6.0, 0.0, 1.4)), Vector((0.0, 0.0, 1.0)), lens)
    # Side view looks along +X, so a vertical gap (slide, vault) is on screen.
    # A wall gap is along X, so that same view collapses it. Look along travel.
    render_ortho(cam, os.path.join(OUT, ortho_name + ".png"), gaps, dots, view or Vector((1.0, 0.0, 0.0)))
    # Frame the runner. A 6 m wall in the bounds would shrink the body to a speck.
    p14.side([arm], effects, lens, 0.62, 0.32)
    report_beauty([arm], effects, beauty_name, contacts)
    p9.shot(beauty_name)


def still_slide(kind):
    arm = begin()
    base = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base)
    pose_slide(arm)
    p9.face_travel(arm, base)
    if kind == "dirt":
        surface_z = lay_dirt(arm)
    else:
        surface_z = lay_concrete()
    seat_pelvis(arm, surface_z)
    hip_pts = pts(arm, "Mesh_Hips")
    heel_pts = pts(arm, "Mesh_Foot_L")
    hip_low, _ = extreme(hip_pts, lambda p: p.z, "min")
    heel_low, heel_z = extreme(heel_pts, lambda p: p.z, "min")
    head = pts(arm, "Mesh_Head")
    head_back = min(head, key=lambda p: p.y)
    hip_back = min(hip_pts, key=lambda p: p.y)
    print(
        "POSE slide", kind,
        "pelvis_cm", round((min_z(arm, "Mesh_Hips") - surface_z) * 100.0, 2),
        "heel_cm", round((heel_z - surface_z) * 100.0, 2),
        "headBehind_cm", round((hip_back.y - head_back.y) * 100.0, 1),
    )
    if head_back.y > hip_back.y:
        raise SystemExit("slide head is not behind the pelvis")
    bits = []
    heel = heel_low[0]
    if kind == "dirt":
        for i in range(5):
            bits.append(p14.dust_card(
                (heel.x + 0.08, heel.y - 0.08 - i * 0.06, surface_z + 0.12 + (i % 3) * 0.05),
                0.28 + (i % 2) * 0.06,
                (0.94, 0.86, 0.72, 1),
            ))
    else:
        for i in range(7):
            ang = i / 7.0 * math.tau
            bits.append(p13.spark(
                (heel.x + math.cos(ang) * 0.06, heel.y - 0.04 - (i % 3) * 0.04, surface_z + 0.05),
                0.05,
                p14.METAL,
                0.9,
            ))
    gaps = [
        gap("pelvis-ground", hip_low[0], Vector((hip_low[0].x, hip_low[0].y, surface_z))),
        gap("heel-ground", heel_low[0], Vector((heel_low[0].x, heel_low[0].y, surface_z))),
    ]
    dots = list(hip_low) + list(heel_low)
    shot_ortho_then_beauty(arm, bits, gaps, dots, "slide-" + kind + "-ortho", "slide-" + kind)


def still_wall_run():
    arm = begin()
    base = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base)
    pose_wall_run(arm)
    p9.face_travel(arm, base)
    arm.location.z += 0.85
    bpy.context.view_layer.update()
    if p14.wh(arm, "Head").z < p14.wh(arm, "Hips").z:
        raise SystemExit("wall runner is not upright")
    foot_pt = min(pts(arm, "Mesh_Foot_L"), key=lambda p: p.x)
    wall, face_x = place_wall(foot_pt, 6.2, 3.2, "Wall")
    assert_foot_plant(arm, "Mesh_Foot_L", face_x)
    planted, _ = extreme(pts(arm, "Mesh_Foot_L"), lambda p: p.x, "min")
    hip_near, _ = extreme(pts(arm, "Mesh_Hips"), lambda p: p.x, "min")
    head_top = max(pts(arm, "Mesh_Head"), key=lambda p: p.z)
    foot = planted[0]
    bits = []
    for i in range(4):
        z = foot.z + 0.02 + i * 0.045
        bits.append(p14.speed_line(
            (face_x + 0.02, foot.y, z),
            (face_x + 0.02, foot.y - 0.55, z),
            0.035,
            (0.82, 0.88, 1.0, 1),
            0.85,
        ))
    gaps = [
        gap("foot-wall", foot, Vector((face_x, foot.y, foot.z))),
        gap("hip-wall", hip_near[0], Vector((face_x, hip_near[0].y, hip_near[0].z))),
    ]
    dots = list(planted) + list(hip_near[:12]) + [head_top]
    shot_ortho_then_beauty(
        arm, bits, gaps, dots, "wall-run-ortho", "wall-run", 46, [wall], Vector((0.0, 1.0, 0.0)))


def still_wall_jump():
    arm = begin()
    base = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base)
    pose_wall_jump(arm)
    p9.face_travel(arm, base)
    arm.location.z += 0.55
    bpy.context.view_layer.update()
    if p14.wh(arm, "Head").z < p14.wh(arm, "Hips").z:
        raise SystemExit("wall jump is inverted")
    foot_pt = min(pts(arm, "Mesh_Foot_L"), key=lambda p: p.x)
    wall, face_x = place_wall(foot_pt, 6.0, 3.2, "JumpWall")
    assert_foot_plant(arm, "Mesh_Foot_L", face_x)
    planted, _ = extreme(pts(arm, "Mesh_Foot_L"), lambda p: p.x, "min")
    hip_near, _ = extreme(pts(arm, "Mesh_Hips"), lambda p: p.x, "min")
    head_top = max(pts(arm, "Mesh_Head"), key=lambda p: p.z)
    foot = planted[0]
    bits = []
    for i in range(5):
        bits.append(p14.dust_card(
            (face_x + 0.05, foot.y + (i - 2) * 0.05, foot.z + (i - 2) * 0.04),
            0.32 + (i % 2) * 0.06,
            (0.92, 0.92, 0.90, 1),
        ))
    gaps = [
        gap("foot-wall", foot, Vector((face_x, foot.y, foot.z))),
        gap("hip-wall", hip_near[0], Vector((face_x, hip_near[0].y, hip_near[0].z))),
    ]
    dots = list(planted) + list(hip_near[:12]) + [head_top]
    shot_ortho_then_beauty(
        arm, bits, gaps, dots, "wall-jump-ortho", "wall-jump", 46, [wall], Vector((0.0, 1.0, 0.0)))


def still_vault():
    arm = begin()
    base = arm.rotation_quaternion.copy()
    p9.tint_hier(arm, None, p9.PLAYER)
    p7.reset_arm(arm, base)
    pose_vault(arm)
    p9.face_travel(arm, base)
    bpy.context.view_layer.update()
    if p14.wh(arm, "Head").z < p14.wh(arm, "Hips").z:
        raise SystemExit("vaulter is inverted")
    left = pts(arm, "Mesh_Hand_L")
    right = pts(arm, "Mesh_Hand_R")
    hips = pts(arm, "Mesh_Hips")
    left_low, left_z = extreme(left, lambda p: p.z, "min")
    right_low, right_z = extreme(right, lambda p: p.z, "min")
    hip_low, hip_z = extreme(hips, lambda p: p.z, "min")
    top = min(left_z, right_z) - 0.012
    print(
        "MESH vault hands_cm", round((left_z - top) * 100.0, 2), round((right_z - top) * 100.0, 2),
        "hipAbove_cm", round((hip_z - top) * 100.0, 2),
        "footL_cm", round((min_z(arm, "Mesh_Foot_L") - top) * 100.0, 2),
        "footR_cm", round((min_z(arm, "Mesh_Foot_R") - top) * 100.0, 2),
    )
    if (left_z - top) > 0.02 or (right_z - top) > 0.02:
        raise SystemExit("a hand is off the box top")
    if hip_z - top < 0.08:
        raise SystemExit("hips are not clear of the box")
    if min_z(arm, "Mesh_Foot_L") < top - 0.02 or min_z(arm, "Mesh_Foot_R") < top - 0.02:
        raise SystemExit("a foot is inside the box")
    hands = left + right
    span_y0 = min(p.y for p in hands + hips) - 0.15
    span_y1 = max(p.y for p in hands + hips) + 0.15
    span_x0 = min(p.x for p in hands) - 0.05
    span_x1 = max(p.x for p in hands) + 0.28
    center = ((span_x0 + span_x1) * 0.5, (span_y0 + span_y1) * 0.5, top * 0.5)
    size = (span_x1 - span_x0, span_y1 - span_y0, top)
    box = p14.add_box("Box", center, size, (0.48, 0.36, 0.24, 1))
    # The cube's top is `top` only if the center and height match. Check the corners.
    bpy.context.view_layer.update()
    world_top = max((box.matrix_world @ Vector(c)).z for c in box.bound_box)
    if abs(world_top - top) > 0.02:
        raise SystemExit("box top drifted")
    face_x = max((box.matrix_world @ Vector(c)).x for c in box.bound_box)
    bits = []
    puff = (0.92, 0.88, 0.80, 1)
    for hand in (left_low[0], right_low[0]):
        bits.append(p14.dust_card((face_x + 0.04, hand.y, top + 0.08), 0.34, puff))
    gaps = [
        gap("handL-box", left_low[0], Vector((left_low[0].x, left_low[0].y, world_top))),
        gap("handR-box", right_low[0], Vector((right_low[0].x, right_low[0].y, world_top))),
        gap("hip-box", hip_low[0], Vector((hip_low[0].x, hip_low[0].y, world_top))),
    ]
    dots = list(left_low) + list(right_low) + list(hip_low[:8])
    shot_ortho_then_beauty(arm, bits, gaps, dots, "vault-plant-ortho", "vault-plant", 44, [box])


if __name__ == "__main__":
    only = os.environ.get("PASS15_ONLY", "")
    shots = [
        ("punch", p14.still_punch),
        ("zip", p14.still_zip),
        ("grapple", p14.still_grapple),
        ("dirt", lambda: still_slide("dirt")),
        ("concrete", lambda: still_slide("concrete")),
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
