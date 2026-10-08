"""Pass 14 Hier stills. One camera, a ground horizon, and a prop for each action.

The fall brace is measured off the rendered bones: chest a little forward,
knees about 30 degrees under the hips, arms abducted about 40 degrees.
"""
import math
import os
import struct
import zlib
import bpy
from mathutils import Vector, Euler

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass14")

PLAYER = (0.235, 0.557, 0.847, 1.0)
ACCENT = (0.45, 0.82, 0.86, 1.0)
JOINT = (0.16, 0.17, 0.18, 1.0)
GROUND = (0.34, 0.36, 0.34, 1.0)
SKY = (0.52, 0.64, 0.76, 1.0)
WALL = (0.48, 0.46, 0.42, 1.0)
LIP = (0.62, 0.48, 0.36, 1.0)
BOX = (0.46, 0.38, 0.30, 1.0)
BAR = (0.16, 0.17, 0.18, 1.0)
CABLE = (0.12, 0.13, 0.14, 1.0)
ROPE = (0.42, 0.32, 0.20, 1.0)
PAD = (0.78, 0.62, 0.22, 1.0)


def rad(deg):
    return math.radians(deg)


def set_euler(arm, name, xdeg, ydeg, zdeg):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = Euler((rad(xdeg), rad(ydeg), rad(zdeg)), "XYZ")


def clear_pose(arm):
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")


def leg(arm, side, thigh, knee, yaw=0.0):
    yaw_b = yaw if side == "L" else -yaw
    set_euler(arm, "UpperLeg_" + side, -thigh, yaw_b, 0.0)
    set_euler(arm, "LowerLeg_" + side, -knee, 0.0, 0.0)


def arm_pose(arm, side, pitch, yaw, elbow, hand, roll=0.0):
    yaw_b = yaw if side == "L" else -yaw
    roll_b = roll if side == "L" else -roll
    set_euler(arm, "UpperArm_" + side, pitch, yaw_b, roll_b)
    set_euler(arm, "LowerArm_" + side, elbow, 0.0, 0.0)
    set_euler(arm, "Hand_" + side, hand, 0.0, 0.0)


def torso(arm, hip, spine, head):
    set_euler(arm, "Hips", hip, 0.0, 0.0)
    set_euler(arm, "Spine", spine, 0.0, 0.0)
    set_euler(arm, "Chest", 0.0, 0.0, 0.0)
    set_euler(arm, "Head", head, 0.0, 0.0)


def pose_stride(arm):
    leg(arm, "L", 42.0, -48.0)
    leg(arm, "R", -24.0, -10.0)
    arm_pose(arm, "L", 36.0, 8.0, -16.0, 0.0)
    arm_pose(arm, "R", -40.0, 8.0, -28.0, 0.0)
    torso(arm, 4.0, 6.5, 2.0)


def pose_stop(arm):
    leg(arm, "L", 16.0, -24.0)
    leg(arm, "R", -6.0, -12.0)
    arm_pose(arm, "L", -8.0, 6.0, -14.0, 0.0)
    arm_pose(arm, "R", -8.0, 6.0, -14.0, 0.0)
    torso(arm, -9.0, -3.5, -2.0)


def pose_turn(arm):
    leg(arm, "L", -8.0, -14.0)
    leg(arm, "R", 24.0, -36.0)
    arm_pose(arm, "L", 18.0, 10.0, -12.0, 0.0)
    arm_pose(arm, "R", -22.0, 10.0, -18.0, 0.0)
    torso(arm, 2.0, 3.0, 2.0)
    set_euler(arm, "Chest", 2.0, 0.0, 12.0)
    set_euler(arm, "Hips", 2.0, 0.0, 8.0)


def pose_apex(arm):
    leg(arm, "L", 76.0, -112.0)
    leg(arm, "R", 70.0, -106.0)
    arm_pose(arm, "L", -45.0, -35.0, -34.0, 0.0)
    arm_pose(arm, "R", -45.0, -35.0, -34.0, 0.0)
    torso(arm, 8.0, -4.0, 0.0)


def pose_fall(arm):
    # Blender X on the thigh is the opposite of the Unity pitch.
    # 26 degrees forward, 30 degrees of knee, feet just ahead.
    # Arm roll abducts. Yaw on this rig only twists.
    leg(arm, "L", 26.0, -32.0, -12.0)
    leg(arm, "R", 26.0, -32.0, -12.0)
    arm_pose(arm, "L", 12.0, 0.0, -34.0, -50.0, -14.0)
    arm_pose(arm, "R", 12.0, 0.0, -34.0, -50.0, -14.0)
    torso(arm, 8.0, -6.0, 16.0)


def pose_climb(arm):
    leg(arm, "L", 56.0, -60.0)
    leg(arm, "R", 76.0, -88.0)
    arm_pose(arm, "L", -120.0, -16.0, -16.0, 0.0)
    arm_pose(arm, "R", -70.0, 16.0, -48.0, 0.0)
    torso(arm, 8.0, 14.0, -26.0)


def pose_vault(arm):
    # Hands forward and down onto a box. One knee tucked out to the side.
    leg(arm, "L", 96.0, -110.0, -28.0)
    leg(arm, "R", 18.0, -36.0, 12.0)
    arm_pose(arm, "L", -50.0, 12.0, -72.0, 0.0)
    arm_pose(arm, "R", -50.0, 12.0, -72.0, 0.0)
    torso(arm, 34.0, 42.0, -6.0)


def pose_slide(arm):
    leg(arm, "L", 68.0, -10.0, 8.0)
    leg(arm, "R", 40.0, -130.0, 24.0)
    arm_pose(arm, "L", 48.0, 22.0, -36.0, 0.0)
    arm_pose(arm, "R", -36.0, 22.0, -28.0, 0.0)
    torso(arm, -22.0, -14.0, 50.0)


def pose_zip(arm):
    leg(arm, "L", 34.0, -18.0)
    leg(arm, "R", 30.0, -14.0)
    arm_pose(arm, "L", -158.0, 8.0, -8.0, 0.0)
    arm_pose(arm, "R", -158.0, 8.0, -8.0, 0.0)
    torso(arm, -8.0, -6.0, -4.0)


def pose_grapple(arm):
    leg(arm, "L", 8.0, -18.0)
    leg(arm, "R", -6.0, -14.0)
    arm_pose(arm, "L", 18.0, 28.0, -24.0, 0.0)
    arm_pose(arm, "R", -148.0, -6.0, -18.0, 0.0)
    torso(arm, -8.0, 6.0, -10.0)


def pose_pad(arm):
    leg(arm, "L", 42.0, -52.0)
    leg(arm, "R", 38.0, -46.0)
    arm_pose(arm, "L", -155.0, 16.0, -14.0, 0.0)
    arm_pose(arm, "R", -155.0, 16.0, -14.0, 0.0)
    torso(arm, 6.0, -4.0, -10.0)


POSES = (
    ("stride", pose_stride, 0.0, None),
    ("stop", pose_stop, 0.0, None),
    ("turn", pose_turn, 0.0, None),
    ("apex", pose_apex, 0.28, None),
    ("fall", pose_fall, 0.28, None),
    ("climb", pose_climb, 0.0, "climb"),
    ("vault", pose_vault, 0.05, "vault"),
    ("slide", pose_slide, 0.0, "slide"),
    ("zip", pose_zip, 0.35, "zip"),
    ("grapple", pose_grapple, 0.22, "grapple"),
    ("pad", pose_pad, 0.42, "pad"),
)


def tint():
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        node = mat.node_tree.nodes.get("Principled BSDF")
        if node is None:
            continue
        name = mat.name
        if name in ("Joint_Tan", "Sensor_Tan"):
            color = JOINT
        elif name == "Accent" or name.startswith("Cal"):
            color = ACCENT
        else:
            color = PLAYER
        node.inputs["Base Color"].default_value = color
        if "Roughness" in node.inputs:
            node.inputs["Roughness"].default_value = 0.45


def look_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def bone_tail(arm, name):
    bone = arm.pose.bones[name]
    return arm.matrix_world @ bone.tail


def bone_head(arm, name):
    bone = arm.pose.bones[name]
    return arm.matrix_world @ bone.head


def make_mat(name, color, rough=0.7):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    node = mat.node_tree.nodes["Principled BSDF"]
    node.inputs["Base Color"].default_value = color
    node.inputs["Roughness"].default_value = rough
    return mat


def add_box(name, center, size, color, rough=0.7):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
    obj.data.materials.append(make_mat(name + "Mat", color, rough))
    return obj


def add_cyl(name, a, b, radius, color):
    delta = b - a
    length = delta.length
    if length < 0.001:
        return None
    mid = (a + b) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=length, location=mid)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = delta.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(make_mat(name + "Mat", color, 0.45))
    return obj


def clear_props():
    for obj in list(bpy.data.objects):
        if obj.name.startswith("Prop"):
            bpy.data.objects.remove(obj, do_unlink=True)


def build_prop(kind, arm):
    clear_props()
    bpy.ops.object.mode_set(mode="OBJECT")
    if kind == "climb":
        left = bone_tail(arm, "Hand_L")
        right = bone_tail(arm, "Hand_R")
        top = max(left.z, right.z) - 0.02
        face = min(left.y, right.y) - 0.08
        depth = 0.42
        add_box("PropWall", (0.0, face - depth * 0.5, top * 0.5), (2.6, depth, top), WALL)
        add_box("PropLip", (0.0, face - 0.06, top + 0.04), (2.7, 0.22, 0.08), LIP)
    elif kind == "vault":
        left = bone_tail(arm, "Hand_L")
        right = bone_tail(arm, "Hand_R")
        top = (left.z + right.z) * 0.5
        cy = (left.y + right.y) * 0.5
        height = max(0.72, top)
        add_box("PropBox", (0.0, cy, height * 0.5), (1.15, 0.52, height), BOX, 0.62)
        add_box("PropLip", (0.0, cy, height + 0.015), (1.2, 0.56, 0.03), LIP, 0.5)
    elif kind == "slide":
        head = bone_tail(arm, "Head")
        z = max(0.95, head.z + 0.16)
        add_cyl("PropBar", Vector((-1.15, 0.05, z)), Vector((1.15, 0.05, z)), 0.035, BAR)
        add_cyl("PropPostL", Vector((-1.05, 0.05, 0.0)), Vector((-1.05, 0.05, z)), 0.04, BAR)
        add_cyl("PropPostR", Vector((1.05, 0.05, 0.0)), Vector((1.05, 0.05, z)), 0.04, BAR)
    elif kind == "zip":
        left = bone_tail(arm, "Hand_L")
        right = bone_tail(arm, "Hand_R")
        z = (left.z + right.z) * 0.5
        y = (left.y + right.y) * 0.5
        add_cyl("PropCable", Vector((-2.4, y, z + 0.12)), Vector((2.4, y, z - 0.05)), 0.018, CABLE)
    elif kind == "grapple":
        hand = bone_tail(arm, "Hand_R")
        anchor = hand + Vector((0.15, -0.55, 1.7))
        add_cyl("PropRope", hand, anchor, 0.012, ROPE)
    elif kind == "pad":
        add_box("PropPad", (0.0, 0.0, 0.04), (1.5, 1.5, 0.08), PAD, 0.4)
        add_box("PropPadRim", (0.0, 0.0, 0.085), (1.62, 1.62, 0.02), (0.25, 0.22, 0.16, 1.0), 0.55)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")


def ang(a, b):
    d = max(-1.0, min(1.0, a.normalized().dot(b.normalized())))
    return math.degrees(math.acos(d))


def measure_fall(arm):
    def wd(name):
        direction = bone_tail(arm, name) - bone_head(arm, name)
        return direction.normalized(), bone_head(arm, name), bone_tail(arm, name)

    up = Vector((0.0, 0.0, 1.0))
    down = Vector((0.0, 0.0, -1.0))
    td, th, tt = wd("UpperLeg_L")
    sd, _, st = wd("LowerLeg_L")
    ad, _, _ = wd("UpperArm_L")
    rd, _, _ = wd("UpperArm_R")
    ed, _, _ = wd("LowerArm_L")
    cd, _, _ = wd("Chest")
    torso = ang(cd, up)
    torso_fwd = torso if cd.y < 0.0 else -torso
    print(
        "FALLBONE",
        "torsoFwd", round(torso_fwd, 1),
        "kneeFlex", round(ang(td, sd), 1),
        "thighDown", round(ang(td, down), 1),
        "footAhead", round(th.y - st.y, 3),
        "kneeAhead", round(th.y - tt.y, 3),
        "abdL", round(ang(ad, down), 1),
        "abdR", round(ang(rd, down), 1),
        "elbow", round(ang(ad, ed), 1),
    )


def scene_setup(arm):
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    ground.data.materials.append(make_mat("GroundMat", GROUND, 0.92))

    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, location=(0.0, 0.02, 0.012))
    shadow = bpy.context.active_object
    shadow.name = "Shadow"
    shadow.scale = (0.48, 0.28, 0.015)
    shadow.data.materials.append(make_mat("ShadowMat", (0.08, 0.09, 0.10, 1.0), 1.0))

    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = SKY
    bg.inputs["Strength"].default_value = 0.72

    bpy.ops.object.light_add(type="SUN", location=(3.0, -2.0, 8.0))
    sun = bpy.context.active_object
    sun.data.energy = 1.05
    sun.data.color = (1.0, 0.96, 0.90)
    look_at(sun, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.light_add(type="AREA", location=(-2.2, -2.6, 2.4))
    fill = bpy.context.active_object
    fill.data.energy = 28
    fill.data.size = 4.0
    look_at(fill, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.camera_add(location=(2.2, -4.2, 1.7))
    cam = bpy.context.active_object
    cam.data.lens = 48
    look_at(cam, Vector((0.0, 0.0, 0.98)))
    bpy.context.scene.camera = cam
    arm.hide_render = True

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = 16
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.exposure = 0.0
    return scene, cam


def body_points(arm):
    pts = []
    for bone in arm.pose.bones:
        pts.append(arm.matrix_world @ bone.head)
        pts.append(arm.matrix_world @ bone.tail)
    return pts


def apply_pose(arm, fn, lift):
    clear_pose(arm)
    fn(arm)
    arm.location = (0.0, 0.0, lift)
    bpy.context.view_layer.update()


def frame_of(scene, cam, point):
    ndc = world_to_camera_view_safe(scene, cam, point)
    return ndc.x, ndc.y, ndc.z


def world_to_camera_view_safe(scene, cam, point):
    from bpy_extras.object_utils import world_to_camera_view
    return world_to_camera_view(scene, cam, point)


def project(scene, cam, points):
    xs = []
    ys = []
    for p in points:
        x, y, z = frame_of(scene, cam, p)
        if z <= 0.0:
            return None
        xs.append(x)
        ys.append(y)
    return min(xs), max(xs), min(ys), max(ys)


def fit(scene, cam, arm):
    # Front three-quarter, eye a little above the chest, so the horizon stays in frame.
    direction = Vector((0.78, -1.42, 0.22)).normalized()
    lens = 48
    look = Vector((0.0, 0.05, 1.15))
    cam.data.lens = lens
    cached = []
    for name, fn, lift, _prop in POSES:
        apply_pose(arm, fn, lift)
        cached.append((name, body_points(arm)))
    best = None
    for step in range(16):
        dist = 4.4 + step * 0.12
        cam.location = look + direction * dist
        look_at(cam, look)
        bpy.context.view_layer.update()
        fall_h = None
        clear = True
        for name, pts in cached:
            box = project(scene, cam, pts)
            if box is None:
                clear = False
                break
            x0, x1, y0, y1 = box
            if x0 < 0.02 or x1 > 0.98 or y0 < 0.03 or y1 > 0.97:
                clear = False
                break
            if name == "fall":
                fall_h = y1 - y0
        if not clear or fall_h is None or fall_h < 0.58 or fall_h > 0.70:
            continue
        score = abs(fall_h - 0.63)
        if best is None or score < best[0]:
            best = (score, dist, fall_h, cam.location.copy())
    if best is None:
        # Bone tails on the zip and the pad sit outside a strict margin.
        # This distance keeps those hands in the picture and the upright body near 62%.
        dist = 5.28
        cam.location = look + direction * dist
        look_at(cam, look)
        bpy.context.view_layer.update()
        print("CAMERA", tuple(round(v, 3) for v in cam.location), "dist", dist)
        return
    _, dist, height, loc = best
    cam.location = loc
    look_at(cam, look)
    bpy.context.view_layer.update()
    print("CAMERA", tuple(round(v, 3) for v in loc), "lens", lens, "dist", round(dist, 2), "fallNdc", round(height, 3))


def paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    if pb <= pc:
        return b
    return c


def png_rows(path):
    with open(path, "rb") as handle:
        data = handle.read()
    pos = 8
    blob = b""
    width = height = None
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            width, height = struct.unpack(">II", chunk[:8])
        elif kind == b"IDAT":
            blob += chunk
        elif kind == b"IEND":
            break
        pos += 12 + length
    raw = zlib.decompress(blob)
    stride = width * 4
    rows = []
    prev = bytearray(stride)
    index = 0
    for _y in range(height):
        filt = raw[index]
        index += 1
        row = bytearray(raw[index:index + stride])
        index += stride
        if filt == 1:
            for x in range(stride):
                left = row[x - 4] if x >= 4 else 0
                row[x] = (row[x] + left) & 255
        elif filt == 2:
            for x in range(stride):
                row[x] = (row[x] + prev[x]) & 255
        elif filt == 3:
            for x in range(stride):
                left = row[x - 4] if x >= 4 else 0
                row[x] = (row[x] + ((left + prev[x]) // 2)) & 255
        elif filt == 4:
            for x in range(stride):
                left = row[x - 4] if x >= 4 else 0
                upv = prev[x]
                ul = prev[x - 4] if x >= 4 else 0
                row[x] = (row[x] + paeth(left, upv, ul)) & 255
        rows.append(row)
        prev = row
    return width, height, rows


def report_png(path):
    width, height, rows = png_rows(path)
    minx, maxx, miny, maxy = width, -1, height, -1
    white = 0
    for y, row in enumerate(rows):
        for x in range(width):
            i = x * 4
            r, g, b = row[i], row[i + 1], row[i + 2]
            if b > r + 40 and g > r + 15 and b > 90 and r < 150 and b > g:
                if x < minx:
                    minx = x
                if x > maxx:
                    maxx = x
                if y < miny:
                    miny = y
                if y > maxy:
                    maxy = y
            if r > 245 and g > 245 and b > 245:
                white += 1
    fill = (maxy - miny + 1) / height if maxy >= miny else 0
    top = rows[8]
    bot = rows[height - 12]
    mid = width // 2 * 4
    print(
        os.path.basename(path),
        "fill", round(fill * 100, 1),
        "y", miny, maxy,
        "x", minx, maxx,
        "edge", int(miny == 0), int(maxy == height - 1), int(minx == 0), int(maxx == width - 1),
        "white", round(100.0 * white / (width * height), 2),
        "sky", tuple(top[mid:mid + 3]),
        "ground", tuple(bot[mid:mid + 3]),
    )


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    tint()
    scene, cam = scene_setup(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    fit(scene, cam, arm)
    apply_pose(arm, pose_fall, 0.28)
    measure_fall(arm)
    for name, fn, lift, prop in POSES:
        apply_pose(arm, fn, lift)
        if prop:
            build_prop(prop, arm)
        else:
            clear_props()
            bpy.context.view_layer.objects.active = arm
            bpy.ops.object.mode_set(mode="POSE")
        path = os.path.join(OUT, name + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        report_png(path)
        print("WROTE", path)


if __name__ == "__main__":
    main()
