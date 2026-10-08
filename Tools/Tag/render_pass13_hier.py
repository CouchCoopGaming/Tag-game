"""Pass 13 Hier stills. One camera. The body fills about two thirds of the frame.

The fall brace bends the knees, opens the legs, and spreads the arms.
Airborne poses sit just above a contact shadow. Empty floor is cropped.
"""
import math
import os
import bpy
from mathutils import Vector, Euler
from bpy_extras.object_utils import world_to_camera_view

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass13")

PLAYER = (0.235, 0.557, 0.847, 1.0)
ACCENT = (0.45, 0.82, 0.86, 1.0)
JOINT = (0.16, 0.17, 0.18, 1.0)
AIR = 0.20


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


def arm_pose(arm, side, pitch, yaw, elbow, hand):
    yaw_b = yaw if side == "L" else -yaw
    set_euler(arm, "UpperArm_" + side, pitch, yaw_b, 0.0)
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
    # Knees about 30 degrees, a little apart, feet ahead. Arms about 38 degrees out.
    leg(arm, "L", 54.0, -32.0, -12.0)
    leg(arm, "R", 54.0, -32.0, -12.0)
    arm_pose(arm, "L", 12.0, 58.0, -34.0, -50.0)
    arm_pose(arm, "R", 12.0, 58.0, -34.0, -50.0)
    torso(arm, 2.0, -6.0, 16.0)


def pose_climb(arm):
    leg(arm, "L", 56.0, -60.0)
    leg(arm, "R", 76.0, -88.0)
    arm_pose(arm, "L", -120.0, -16.0, -16.0, 0.0)
    arm_pose(arm, "R", -70.0, 16.0, -48.0, 0.0)
    torso(arm, 8.0, 14.0, -26.0)


def pose_vault(arm):
    leg(arm, "L", 102.0, -124.0)
    leg(arm, "R", 10.0, -18.0)
    arm_pose(arm, "L", -112.0, 14.0, -96.0, 0.0)
    arm_pose(arm, "R", -104.0, 14.0, -90.0, 0.0)
    torso(arm, 22.0, 30.0, -8.0)


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
    leg(arm, "L", -30.0, -8.0)
    leg(arm, "R", 20.0, -32.0)
    arm_pose(arm, "L", -126.0, 8.0, -4.0, 0.0)
    arm_pose(arm, "R", -134.0, -8.0, -2.0, 0.0)
    torso(arm, 36.0, 56.0, -20.0)


def pose_pad(arm):
    leg(arm, "L", 42.0, -52.0)
    leg(arm, "R", 38.0, -46.0)
    arm_pose(arm, "L", -155.0, 16.0, -14.0, 0.0)
    arm_pose(arm, "R", -155.0, 16.0, -14.0, 0.0)
    torso(arm, 6.0, -4.0, -10.0)


POSES = (
    ("stride", pose_stride, 0.0),
    ("stop", pose_stop, 0.0),
    ("turn", pose_turn, 0.0),
    ("apex", pose_apex, AIR),
    ("fall", pose_fall, AIR),
    ("climb", pose_climb, 0.08),
    ("vault", pose_vault, 0.22),
    ("slide", pose_slide, 0.0),
    ("zip", pose_zip, AIR),
    ("grapple", pose_grapple, AIR),
    ("pad", pose_pad, AIR),
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


def body_points(arm):
    pts = []
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        if obj.name in ("Ground", "Shadow", "GroundLine"):
            continue
        for corner in obj.bound_box:
            pts.append(obj.matrix_world @ Vector(corner))
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
    ndc = world_to_camera_view(scene, cam, point)
    return ndc.x, ndc.y, ndc.z


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


def scene_setup(arm):
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    gmat = bpy.data.materials.new("Ground")
    gmat.use_nodes = True
    gmat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.62, 0.64, 0.66, 1)
    gmat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
    ground.data.materials.append(gmat)

    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, location=(0.0, 0.05, 0.012))
    shadow = bpy.context.active_object
    shadow.name = "Shadow"
    shadow.scale = (0.55, 0.32, 0.02)
    smat = bpy.data.materials.new("Shadow")
    smat.use_nodes = True
    smat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.08, 0.09, 0.10, 1)
    smat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    shadow.data.materials.append(smat)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.05, 0.004))
    line = bpy.context.active_object
    line.name = "GroundLine"
    line.scale = (1.15, 0.012, 0.004)
    lmat = bpy.data.materials.new("GroundLine")
    lmat.use_nodes = True
    lmat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.22, 0.24, 0.26, 1)
    line.data.materials.append(lmat)

    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.55, 0.74, 0.90, 1)
    bg.inputs["Strength"].default_value = 1.05

    bpy.ops.object.light_add(type="SUN", location=(2.2, -3.2, 6.5))
    sun = bpy.context.active_object
    sun.data.energy = 2.4
    look_at(sun, Vector((0.0, 0.0, 1.1)))

    bpy.ops.object.light_add(type="AREA", location=(-2.0, -1.4, 2.6))
    fill = bpy.context.active_object
    fill.data.energy = 180
    fill.data.size = 3.2
    look_at(fill, Vector((0.0, 0.0, 1.1)))

    bpy.ops.object.camera_add(location=(2.2, -3.4, 1.35))
    cam = bpy.context.active_object
    cam.data.lens = 48
    look_at(cam, Vector((0.0, 0.0, 1.0)))
    bpy.context.scene.camera = cam
    arm.hide_render = True

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = 16
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    return scene, cam


def shadow_points():
    pts = []
    for x in (-0.5, 0.0, 0.5):
        for y in (-0.22, 0.05, 0.32):
            pts.append(Vector((x, y, 0.02)))
    return pts


def fit(scene, cam, cached):
    # More side-on than pass 12 so the fall brace's knee bend reads in frame.
    direction = Vector((0.792, -0.509, 0.337)).normalized()
    lens = 46
    dist = 5.16
    look_z = 0.94
    center = Vector((0.0, 0.0, look_z))
    cam.data.lens = lens
    cam.location = center + direction * dist
    look_at(cam, center)
    bpy.context.view_layer.update()
    fall_h = None
    stride_h = None
    min_margin = 1.0
    for name, pts in cached:
        box = project(scene, cam, pts)
        if box is None:
            raise SystemExit("pose behind camera " + name)
        x0, x1, y0, y1 = box
        margin = min(x0, 1.0 - x1, y0, 1.0 - y1)
        min_margin = min(min_margin, margin)
        if name == "fall":
            fall_h = y1 - y0
        if name == "stride":
            stride_h = y1 - y0
    shadow_box = project(scene, cam, shadow_points())
    print(
        "CAMERA", tuple(round(v, 3) for v in cam.location),
        "lens", lens, "look", look_z,
        "fall", round(fall_h, 3), "stride", round(stride_h, 3),
        "margin", round(min_margin, 3),
        "shadow", None if shadow_box is None else tuple(round(v, 3) for v in shadow_box),
    )
    return cam.location


def report_pose(scene, cam, arm, name):
    box = project(scene, cam, body_points(arm))
    if box is None:
        print(name, "BEHIND")
        return
    x0, x1, y0, y1 = box
    print(name, "body", round(y1 - y0, 3), "y", round(y0, 3), round(y1, 3), "x", round(x0, 3), round(x1, 3))


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    tint()
    scene, cam = scene_setup(arm)
    cached = []
    for name, fn, lift in POSES:
        apply_pose(arm, fn, lift)
        cached.append((name, [p for p in body_points(arm) if p.z > -0.02]))
    for name, pts in cached:
        zs = [p.z for p in pts]
        xs = [p.x for p in pts]
        print("SPAN", name, "n", len(pts), "x", round(min(xs), 2), round(max(xs), 2), "z", round(min(zs), 2), round(max(zs), 2))
    fit(scene, cam, cached)
    for name, fn, lift in POSES:
        apply_pose(arm, fn, lift)
        report_pose(scene, cam, arm, name)
        path = os.path.join(OUT, name + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        print("WROTE", path)


if __name__ == "__main__":
    main()
