"""Render the Hier mannequin pair for the menu. Pose sample 0, feet at 0.5 cm.

Blender 4, EEVEE, transparent film. Not a new clip: knees at IdlePose.KneeRest
(-4 deg) and the feet cancel that pitch so the sole stays level.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RED_FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Red_Hier_Hi.fbx")
BLUE_FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Blue_Hier_Hi.fbx")
OUT = os.path.join(ROOT, "Assets", "Resources", "UI", "Menu", "SeatIdle.png")

# Seat fills. P1 red circle, P2 blue triangle.
RED = (0.90, 0.18, 0.20)
BLUE = (0.20, 0.48, 0.88)
JOINT = (0.07, 0.07, 0.08)
PLANT = 0.005
KNEE = -4.0


def wipe():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_prefixed(path, prefix):
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=path)
    fresh = [bpy.data.objects[name] for name in bpy.data.objects.keys() if name not in before]
    for obj in fresh:
        obj.name = prefix + obj.name
    roots = [obj for obj in fresh if obj.parent is None]
    return fresh, roots


def meshes(objs):
    return [obj for obj in objs if obj.type == "MESH"]


def find_named(objs, token):
    hits = [obj for obj in objs if token in obj.name]
    hits.sort(key=lambda obj: (0 if obj.name.endswith(token) or obj.name.endswith(token + ".001") else 1, len(obj.name)))
    return hits[0] if hits else None


def world_bounds(objs):
    xs, ys, zs = [], [], []
    for obj in meshes(objs):
        for corner in obj.bound_box:
            w = obj.matrix_world @ Vector(corner)
            xs.append(w.x)
            ys.append(w.y)
            zs.append(w.z)
    if not xs:
        return Vector((0, 0, 0)), Vector((0, 0, 0))
    lo = Vector((min(xs), min(ys), min(zs)))
    hi = Vector((max(xs), max(ys), max(zs)))
    return lo, hi


def stand_up(roots, objs):
    """FBX comes back Z-up from the Blender export. If a round-trip left it
    lying down, pitch the root so the long axis is up."""
    lo, hi = world_bounds(objs)
    span = hi - lo
    if span.z >= span.y * 0.75 and span.z >= span.x * 0.75:
        return
    for root in roots:
        root.rotation_euler.x += math.radians(-90.0)
    bpy.context.view_layer.update()


def face_axis(objs):
    """Panel_Chest sits on the face. Return the horizontal axis that points out of the chest."""
    chest = find_named(objs, "Panel_Chest")
    hips = find_named(objs, "Hips")
    if chest is None:
        return Vector((0, -1, 0))
    origin = hips.matrix_world.translation if hips is not None else Vector((0, 0, 0))
    delta = chest.matrix_world.translation - origin
    flat = Vector((delta.x, delta.y, 0))
    if flat.length < 1e-4:
        flat = Vector((0, -1, 0))
    return flat.normalized()


def add_local_x(obj, degrees):
    if obj is None:
        return
    obj.rotation_mode = "XYZ"
    obj.rotation_euler.x += math.radians(degrees)
    bpy.context.view_layer.update()


def sole_tilt(objs):
    """Mean world-up difference between the front and back of each foot mesh.
    Near 0 means the sole is level."""
    face = face_axis(objs)
    tilts = []
    for obj in meshes(objs):
        if "Foot" not in obj.name:
            continue
        pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
        if len(pts) < 8:
            continue
        dots = [(p, (p - Vector((0, 0, p.z))).dot(face)) for p in pts]
        dots.sort(key=lambda item: item[1])
        n = max(1, len(dots) // 5)
        back = sum(p.z for p, _ in dots[:n]) / n
        front = sum(p.z for p, _ in dots[-n:]) / n
        tilts.append(front - back)
    if not tilts:
        return 0.0
    return sum(tilts) / len(tilts)


def pose_idle(objs):
    """Pose sample 0. Knee rest is -4 deg. Try both hinge signs and keep the
    one that leaves the sole nearer to level. Foot cancel is the opposite pitch."""
    knees = [find_named(objs, "LowerLeg_L"), find_named(objs, "LowerLeg_R")]
    feet = [find_named(objs, "Foot_L"), find_named(objs, "Foot_R")]
    chain = [obj for obj in knees + feet if obj is not None]
    for obj in chain:
        obj.rotation_mode = "XYZ"
    saved = {obj: obj.rotation_euler.copy() for obj in chain}
    best = None
    for sign in (1.0, -1.0):
        for obj, euler in saved.items():
            obj.rotation_euler = euler.copy()
        bpy.context.view_layer.update()
        for obj in knees:
            add_local_x(obj, KNEE * sign)
        for obj in feet:
            add_local_x(obj, -KNEE * sign)
        tilt = abs(sole_tilt(objs))
        if best is None or tilt < best[0]:
            best = (tilt, sign)
    for obj, euler in saved.items():
        obj.rotation_euler = euler.copy()
    bpy.context.view_layer.update()
    sign = best[1]
    for obj in knees:
        add_local_x(obj, KNEE * sign)
    for obj in feet:
        add_local_x(obj, -KNEE * sign)
    print("pose sign", sign, "sole tilt", round(sole_tilt(objs), 4), "knees", [obj.name if obj else None for obj in knees])


def foot_min_z(objs):
    zmin = 1e9
    found = False
    for obj in meshes(objs):
        if "Foot" not in obj.name:
            continue
        for v in obj.data.vertices:
            z = (obj.matrix_world @ v.co).z
            if z < zmin:
                zmin = z
            found = True
    return zmin if found else 0.0


def plant(roots, objs):
    bpy.context.view_layer.update()
    zmin = foot_min_z(objs)
    for root in roots:
        root.location.z += PLANT - zmin
    bpy.context.view_layer.update()
    print("planted", prefix_of(roots), "sole", round(foot_min_z(objs), 4))


def prefix_of(roots):
    if not roots:
        return "?"
    return roots[0].name.split("_")[0]


def principled(name, color, rough, emit=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.28
    elif "Specular" in bsdf.inputs:
        bsdf.inputs["Specular"].default_value = 0.28
    if emit > 0 and "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (color[0], color[1], color[2], 1.0)
        bsdf.inputs["Emission Strength"].default_value = emit
    elif emit > 0 and "Emission" in bsdf.inputs:
        bsdf.inputs["Emission"].default_value = (color[0], color[1], color[2], 1.0)
    mat.diffuse_color = (color[0], color[1], color[2], 1.0)
    return mat


def slot_names(obj):
    names = []
    for slot in obj.material_slots:
        if slot.material is not None:
            names.append(slot.material.name)
    return " ".join(names)


def tint(objs, color, tag):
    body = principled(tag + "Body", color, 0.42)
    panel = principled(tag + "Panel", tuple(c * 0.62 for c in color), 0.38)
    joint = principled(tag + "Joint", JOINT, 0.72)
    for obj in meshes(objs):
        blob = obj.name + " " + slot_names(obj)
        if any(key in blob for key in ("Joint", "Sensor", "Rubber", "Hand", "Foot", "Eye", "Socket")):
            use = joint
        elif "Panel" in blob:
            use = panel
        else:
            use = body
        obj.data.materials.clear()
        obj.data.materials.append(use)


def align_z(obj, normal):
    quat = Vector((0, 0, 1)).rotation_difference(normal.normalized())
    obj.rotation_euler = quat.to_euler()


def chest_mark(objs, kind, color):
    chest = find_named(meshes(objs), "Panel_Chest")
    if chest is None:
        chest = find_named(meshes(objs), "Chest")
    if chest is None:
        print("no chest", kind)
        return
    bpy.context.view_layer.update()
    normal = face_axis(objs)
    center = chest.matrix_world.translation + normal * 0.012
    if kind == "circle":
        radius, verts = 0.072, 48
    else:
        radius, verts = 0.086, 3
    back_at = chest.matrix_world.translation + normal * 0.008
    bpy.ops.mesh.primitive_cylinder_add(vertices=40, radius=radius * 1.28, depth=0.006, location=back_at)
    back = bpy.context.active_object
    back.name = "ChestWell"
    align_z(back, normal)
    back.data.materials.append(principled("Well" + kind, (0.03, 0.03, 0.04), 0.6))
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=0.008, location=center)
    mark = bpy.context.active_object
    mark.name = "ChestDecal"
    align_z(mark, normal)
    mark.data.materials.append(principled("Decal" + kind, color, 0.45, emit=1.4))


def move_root(roots, x, yaw_deg):
    for root in roots:
        root.location.x += x
        root.rotation_euler.z += math.radians(yaw_deg)
    bpy.context.view_layer.update()


def disc(x, y, color):
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=0.40, depth=0.045, location=(x, y, PLANT - 0.022))
    obj = bpy.context.active_object
    obj.name = "Disc"
    dark = tuple(min(1.0, c * 0.72 + 0.04) for c in color)
    obj.data.materials.append(principled(obj.name + "Mat", dark, 0.62))
    # Contact shadow sits on the disc, under the stance, not behind the heels.
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.16, depth=0.004, location=(x, y, PLANT + 0.004))
    shade = bpy.context.active_object
    shade.name = "Contact"
    shade.scale = (1.15, 0.72, 1.0)
    shade.data.materials.append(principled(shade.name + "Mat", (0.015, 0.016, 0.02), 1.0))


def lights():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("MenuWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.62, 0.70, 0.80, 1.0)
    bg.inputs[1].default_value = 0.55

    sun_data = bpy.data.lights.new("Key", "SUN")
    sun_data.energy = 2.4
    sun_data.angle = math.radians(28.0)
    sun_data.use_shadow = True
    sun = bpy.data.objects.new("Key", sun_data)
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(38), math.radians(6), math.radians(-28))

    fill_data = bpy.data.lights.new("Fill", "SUN")
    fill_data.energy = 1.1
    fill_data.use_shadow = False
    fill = bpy.data.objects.new("Fill", fill_data)
    scene.collection.objects.link(fill)
    fill.rotation_euler = (math.radians(64), math.radians(-12), math.radians(140))


def camera(face):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new("MenuCam")
    cam_data.lens = 38
    cam_data.clip_start = 0.05
    cam_data.clip_end = 40
    cam = bpy.data.objects.new("MenuCam", cam_data)
    scene.collection.objects.link(cam)
    # Sit off to the side of the face so the pair reads three-quarter,
    # far enough that the head and the discs stay inside the frame.
    side = Vector((-face.y, face.x, 0)).normalized()
    origin = Vector((0.0, 0.0, 0.70))
    # High enough to see the disc tops, still a front three-quarter.
    cam.location = origin + face * 4.35 + side * 0.82 + Vector((0.0, 0.0, 1.45))
    direction = origin - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam


def render(path):
    scene = bpy.context.scene
    picked = None
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "CYCLES"):
        try:
            scene.render.engine = eng
            picked = eng
            break
        except TypeError:
            continue
    print("engine", picked)
    if picked == "CYCLES":
        scene.cycles.samples = 32
    else:
        if hasattr(scene, "eevee"):
            scene.eevee.taa_render_samples = 24
            if hasattr(scene.eevee, "use_soft_shadows"):
                scene.eevee.use_soft_shadows = True
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.view_settings.view_transform = "Standard"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.compression = 15
    os.makedirs(os.path.dirname(path), exist_ok=True)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("wrote", path, os.path.getsize(path))


def prepare(path, prefix, color, kind, x, yaw):
    objs, roots = import_prefixed(path, prefix)
    # Drop cameras and lights the file may have carried.
    for obj in list(objs):
        if obj.type in ("CAMERA", "LIGHT"):
            bpy.data.objects.remove(obj, do_unlink=True)
    objs = [obj for obj in bpy.data.objects if obj.name.startswith(prefix)]
    roots = [obj for obj in objs if obj.parent is None]
    named = [obj.name for obj in objs if any(k in obj.name for k in ("Foot", "LowerLeg", "Panel_Chest", "Hips", "Head", "Root"))]
    print(prefix, "count", len(objs), "named", named[:24])
    lo, hi = world_bounds(objs)
    print(prefix, "span", tuple(round(v, 3) for v in (hi - lo)))
    stand_up(roots, objs)
    pose_idle(objs)
    plant(roots, objs)
    tint(objs, color, prefix)
    lo, hi = world_bounds(objs)
    mid = (lo + hi) * 0.5
    move_root(roots, x - mid.x, yaw)
    bpy.context.view_layer.update()
    chest_mark(objs, kind, color)
    lo, hi = world_bounds(objs)
    foot_y = (lo.y + hi.y) * 0.5
    # Discs sit under the feet, on the ground plane, not at the torso.
    feet = [obj for obj in meshes(objs) if "Foot" in obj.name]
    if feet:
        acc = Vector((0, 0, 0))
        n = 0
        for obj in feet:
            acc += obj.matrix_world.translation
            n += 1
        foot_y = acc.y / n
        x = acc.x / n
    disc(x, foot_y, color)
    print(prefix, "bounds", tuple(round(v, 3) for v in lo), tuple(round(v, 3) for v in hi))
    return face_axis(objs)


def main():
    wipe()
    face = prepare(RED_FBX, "Red_", RED, "circle", -0.62, 26.0)
    prepare(BLUE_FBX, "Blue_", BLUE, "triangle", 0.62, -26.0)
    lights()
    camera(face)
    render(OUT)


if __name__ == "__main__":
    main()
