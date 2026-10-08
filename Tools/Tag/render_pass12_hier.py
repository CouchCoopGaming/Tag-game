"""Pass 12 Hier stills. One camera, full body, ten percent margin.

Airborne poses sit above a contact shadow so the height reads.
The apex tuck keeps the arms out and forward. The fall is a brace.
"""
import math
import os
import bpy
from mathutils import Vector, Euler
from bpy_extras.object_utils import world_to_camera_view

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass12")

PLAYER = (0.235, 0.557, 0.847, 1.0)
ACCENT = (0.45, 0.82, 0.86, 1.0)
JOINT = (0.16, 0.17, 0.18, 1.0)


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


def leg(arm, side, thigh, knee):
    set_euler(arm, "UpperLeg_" + side, -thigh, 0.0, 0.0)
    set_euler(arm, "LowerLeg_" + side, -knee, 0.0, 0.0)


def arm_pose(arm, side, pitch, yaw, elbow, hand):
    yaw_b = yaw if side == "L" else -yaw
    hand_z = 0.0 if side == "L" else 0.0
    set_euler(arm, "UpperArm_" + side, pitch, yaw_b, 0.0)
    set_euler(arm, "LowerArm_" + side, elbow, 0.0, 0.0)
    set_euler(arm, "Hand_" + side, hand, 0.0, hand_z)


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
    # Tuck. Elbows about 43 degrees, hands at chin height, out and forward.
    leg(arm, "L", 76.0, -112.0)
    leg(arm, "R", 70.0, -106.0)
    arm_pose(arm, "L", -45.0, -35.0, -34.0, 0.0)
    arm_pose(arm, "R", -45.0, -35.0, -34.0, 0.0)
    torso(arm, 8.0, -4.0, 0.0)


def pose_fall(arm):
    # Chest up, eyes down, arms about 30 degrees down and out, palms down.
    leg(arm, "L", 54.0, -36.0)
    leg(arm, "R", 54.0, -36.0)
    arm_pose(arm, "L", 12.0, -48.0, -10.0, -50.0)
    arm_pose(arm, "R", 12.0, -48.0, -10.0, -50.0)
    torso(arm, 2.0, -6.0, 16.0)


POSES = (
    ("stride", pose_stride, 0.0),
    ("stop", pose_stop, 0.0),
    ("turn", pose_turn, 0.0),
    ("apex", pose_apex, 0.72),
    ("fall", pose_fall, 0.72),
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


def margins(scene, cam, points):
    xs = []
    ys = []
    for p in points:
        x, y, z = frame_of(scene, cam, p)
        if z <= 0.0:
            return -1.0
        xs.append(x)
        ys.append(y)
    left = min(xs)
    right = 1.0 - max(xs)
    bottom = min(ys)
    top = 1.0 - max(ys)
    return min(left, right, bottom, top)


def scene_setup(arm):
    bpy.ops.mesh.primitive_plane_add(size=16, location=(0.0, 0.0, 0.0))
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
    shadow.scale = (0.62, 0.36, 0.02)
    smat = bpy.data.materials.new("Shadow")
    smat.use_nodes = True
    smat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.08, 0.09, 0.10, 1)
    smat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    shadow.data.materials.append(smat)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.05, 0.004))
    line = bpy.context.active_object
    line.name = "GroundLine"
    line.scale = (1.35, 0.012, 0.004)
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

    bpy.ops.object.camera_add(location=(2.4, -3.6, 1.35))
    cam = bpy.context.active_object
    cam.data.lens = 42
    look_at(cam, Vector((0.0, 0.0, 1.05)))
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


def fit(scene, cam, arm):
    points = []
    for name, fn, lift in POSES:
        apply_pose(arm, fn, lift)
        points.extend(body_points(arm))
    points.append(Vector((0.0, 0.05, 0.0)))
    points.append(Vector((0.55, 0.05, 0.0)))
    points.append(Vector((-0.55, 0.05, 0.0)))
    center = Vector((0.0, 0.0, 1.15))
    direction = Vector((1.05, -1.65, 0.22)).normalized()
    best = None
    for step in range(40):
        dist = 3.2 + step * 0.15
        cam.location = center + direction * dist
        look_at(cam, center)
        bpy.context.view_layer.update()
        margin = margins(scene, cam, points)
        if margin >= 0.10:
            best = (dist, margin, cam.location.copy())
            break
    if best is None:
        cam.location = center + direction * 6.0
        look_at(cam, center)
        bpy.context.view_layer.update()
        xs, ys = [], []
        behind = 0
        for p in points:
            x, y, z = frame_of(scene, cam, p)
            if z <= 0.0:
                behind += 1
                continue
            xs.append(x)
            ys.append(y)
        print("FAIL pts", len(points), "behind", behind)
        if xs:
            print("ndc x", round(min(xs), 3), round(max(xs), 3), "y", round(min(ys), 3), round(max(ys), 3))
        print("span", 
              round(min(p.x for p in points), 2), round(max(p.x for p in points), 2),
              round(min(p.y for p in points), 2), round(max(p.y for p in points), 2),
              round(min(p.z for p in points), 2), round(max(p.z for p in points), 2))
        raise SystemExit("camera never cleared a 10 percent margin")
    cam.location = best[2]
    look_at(cam, center)
    bpy.context.view_layer.update()
    print("CAMERA", tuple(round(v, 3) for v in cam.location), "margin", round(best[1], 3))
    return best[1]


def report_pose(arm, name):
    bpy.context.view_layer.update()
    def tail(n):
        return arm.matrix_world @ arm.pose.bones[n].tail
    hl = tail("Hand_L")
    hr = tail("Hand_R")
    hd = arm.matrix_world @ arm.pose.bones["Head"].head
    ht = tail("Head")
    print(
        name,
        "handL", tuple(round(v, 3) for v in hl),
        "handR", tuple(round(v, 3) for v in hr),
        "head", round(hd.z, 3), round(ht.z, 3),
        "root", round(arm.location.z, 3),
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
    margin = fit(scene, cam, arm)
    if margin < 0.10:
        raise SystemExit("margin " + str(margin))
    for name, fn, lift in POSES:
        apply_pose(arm, fn, lift)
        report_pose(arm, name)
        path = os.path.join(OUT, name + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        print("WROTE", path)


if __name__ == "__main__":
    main()
