"""Pass 11 Hier stills. Visual poses only. The capsule is not in the shot.

Blender bone X is not Unity's X. Legs pitch forward on negative X.
Arms reach forward on negative X, which matches a negative Unity pitch.
Spine and hips pitch forward on positive X. Local Z rolls the chest.
"""
import math
import os
import bpy
from mathutils import Vector, Euler

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass11")

PLAYER = (0.235, 0.557, 0.847, 1.0)  # #3C8ED8
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


def arm_pose(arm, side, pitch, elbow):
    set_euler(arm, "UpperArm_" + side, pitch, 0.0, 0.0)
    set_euler(arm, "LowerArm_" + side, elbow, 0.0, 0.0)


def torso(arm, hip, spine, chest, roll):
    set_euler(arm, "Hips", hip, 0.0, roll * 0.65)
    set_euler(arm, "Spine", spine * 0.45, 0.0, roll * 0.2)
    set_euler(arm, "Chest", chest, 0.0, roll)


def pose_stride(arm):
    # Sprint reach. Left thigh forward, right arm forward.
    leg(arm, "L", 42.0, -48.0)
    leg(arm, "R", -24.0, -10.0)
    arm_pose(arm, "L", 36.0, -16.0)
    arm_pose(arm, "R", -40.0, -28.0)
    torso(arm, 4.0, 6.5, 2.0, 0.0)


def pose_stop(arm):
    # Lead plant, hips back. StopPlantPose.
    leg(arm, "L", 16.0, -24.0)
    leg(arm, "R", -6.0, -12.0)
    arm_pose(arm, "L", -8.0, -14.0)
    arm_pose(arm, "R", -8.0, -14.0)
    torso(arm, -9.0, -3.5, -1.0, 0.0)


def pose_turn(arm):
    # Lean into the turn. Outside foot plants.
    leg(arm, "L", -8.0, -14.0)
    leg(arm, "R", 24.0, -36.0)
    arm_pose(arm, "L", 18.0, -12.0)
    arm_pose(arm, "R", -22.0, -18.0)
    torso(arm, 2.0, 3.0, 2.0, 12.0)


def pose_apex(arm):
    # Straight-up tuck. Full tuck, arms out for balance.
    leg(arm, "L", 76.0, -112.0)
    leg(arm, "R", 70.0, -106.0)
    arm_pose(arm, "L", -126.0, -26.0)
    arm_pose(arm, "R", -126.0, -26.0)
    torso(arm, 14.0, -8.0, -4.0, 0.0)


def pose_fall(arm):
    # Brace as the drop nears terminal. Thighs and arms open toward the land.
    leg(arm, "L", 54.0, -78.0)
    leg(arm, "R", 54.0, -78.0)
    arm_pose(arm, "L", -24.0, -14.0)
    arm_pose(arm, "R", -24.0, -14.0)
    torso(arm, 18.0, 12.0, 6.0, 0.0)


POSES = (
    ("stride", pose_stride),
    ("stop", pose_stop),
    ("turn", pose_turn),
    ("apex", pose_apex),
    ("fall", pose_fall),
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


def scene_setup(arm):
    bpy.ops.mesh.primitive_plane_add(size=14, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    gmat = bpy.data.materials.new("Ground")
    gmat.use_nodes = True
    gmat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.76, 0.78, 0.80, 1)
    gmat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.85
    ground.data.materials.append(gmat)

    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.55, 0.74, 0.90, 1)
    bg.inputs["Strength"].default_value = 1.05

    bpy.ops.object.light_add(type="SUN", location=(1.5, -3.0, 6.0))
    sun = bpy.context.active_object
    sun.data.energy = 4.2
    look_at(sun, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.light_add(type="AREA", location=(-1.6, -1.2, 2.4))
    fill = bpy.context.active_object
    fill.data.energy = 250
    fill.data.size = 3.0
    look_at(fill, Vector((0.0, 0.0, 1.0)))

    bpy.ops.object.camera_add(location=(1.85, -2.55, 1.35))
    cam = bpy.context.active_object
    cam.data.lens = 48
    look_at(cam, Vector((0.0, -0.05, 0.92)))
    bpy.context.scene.camera = cam
    arm.hide_render = True

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = 24
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    return scene


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    tint()
    scene = scene_setup(arm)
    for name, fn in POSES:
        clear_pose(arm)
        fn(arm)
        bpy.context.view_layer.update()
        path = os.path.join(OUT, name + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        print("WROTE", path)


if __name__ == "__main__":
    main()
