"""Four Hier idles, one camera. Same pose, same scale, seat color and chest shape.

Not a new clip. Knees at IdlePose.KneeRest (-4 deg). Feet cancel that pitch.
Soles land at 0.5 cm. Upper arms stay about 12 degrees off the torso.
A soft elbow brings the forearm in so the hang is not a straight bind line.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly")
OUT_DIR = os.environ.get("SEATLOAD_OUT", "/tmp/seatload")

# Seat fills. Circle, triangle, square, diamond.
SEATS = (
    ("Red_", os.path.join(ART, "Dummy_Mannequin_Red_Hier_Hi.fbx"), (0.90, 0.18, 0.20), "circle"),
    ("Blue_", os.path.join(ART, "Dummy_Mannequin_Blue_Hier_Hi.fbx"), (0.20, 0.48, 0.88), "triangle"),
    # The Orange Hier file is a different skinned mesh. Seat color is a tint on the shared rig.
    ("Orange_", os.path.join(ART, "Dummy_Mannequin_Red_Hier_Hi.fbx"), (1.00, 0.62, 0.18), "square"),
    ("Lavender_", os.path.join(ART, "Dummy_Mannequin_Lavender_Hier_Hi.fbx"), (0.80, 0.72, 0.92), "diamond"),
)
JOINT = (0.07, 0.07, 0.08)
PLANT = 0.005
KNEE = -4.0
YAW = 22.0


def wipe():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_prefixed(path, prefix):
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=path)
    fresh = [bpy.data.objects[name] for name in bpy.data.objects.keys() if name not in before]
    for obj in fresh:
        obj.name = prefix + obj.name
    return fresh


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
    return Vector((min(xs), min(ys), min(zs))), Vector((max(xs), max(ys), max(zs)))


def stand_up(roots, objs):
    lo, hi = world_bounds(objs)
    span = hi - lo
    if span.z >= span.y * 0.75 and span.z >= span.x * 0.75:
        return
    for root in roots:
        root.rotation_euler.x += math.radians(-90.0)
    bpy.context.view_layer.update()


def face_axis(objs):
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
    print("pose sign", sign, "sole tilt", round(sole_tilt(objs), 4))


def add_delta(obj, x, y, z):
    if obj is None:
        return
    from mathutils import Euler

    rest = obj.rotation_euler.to_quaternion()
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = rest @ Euler(
        (math.radians(x), math.radians(y), math.radians(z)), "ZXY"
    ).to_quaternion()


def pose_hang(objs):
    """Forearms in, upper arms left on the bind. About 12 degrees off the torso."""
    add_delta(find_named(objs, "LowerArm_L"), -14, -26, 0)
    add_delta(find_named(objs, "LowerArm_R"), -14, 26, 0)
    bpy.context.view_layer.update()


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
    print("sole", round(foot_min_z(objs), 4))


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


def shape_pts(kind, radius):
    if kind == "circle":
        n = 40
        return [(math.cos(i / n * math.pi * 2.0) * radius, math.sin(i / n * math.pi * 2.0) * radius) for i in range(n)]
    if kind == "triangle":
        return [
            (0.0, radius),
            (-radius * 0.90, -radius * 0.62),
            (radius * 0.90, -radius * 0.62),
        ]
    if kind == "square":
        s = radius * 0.78
        return [(-s, s), (s, s), (s, -s), (-s, -s)]
    s = radius
    return [(0.0, s), (s, 0.0), (0.0, -s), (-s, 0.0)]


def add_poly(name, center, normal, pts, depth, color, emit):
    n = normal.normalized()
    up = Vector((0.0, 0.0, 1.0)) - n * n.z
    if up.length < 1e-4:
        up = Vector((0.0, 1.0, 0.0))
    up.normalize()
    right = up.cross(n).normalized()
    mesh = bpy.data.meshes.new(name + "Mesh")
    import bmesh

    bm = bmesh.new()
    front = []
    back = []
    for x, y in pts:
        p = center + right * x + up * y
        front.append(bm.verts.new(p + n * (depth * 0.5)))
        back.append(bm.verts.new(p - n * (depth * 0.5)))
    bm.verts.ensure_lookup_table()
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    count = len(pts)
    for i in range(count):
        j = (i + 1) % count
        bm.faces.new((front[i], front[j], back[j], back[i]))
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(principled(name + "Mat", color, 0.45, emit=emit))
    return obj


def chest_mark(objs, kind, color):
    chest = find_named(meshes(objs), "Panel_Chest")
    if chest is None:
        chest = find_named(meshes(objs), "Chest")
    if chest is None:
        print("no chest", kind)
        return
    bpy.context.view_layer.update()
    normal = face_axis(objs)
    radius = 0.072 if kind == "circle" else 0.086
    center = chest.matrix_world.translation + normal * 0.014
    back_at = chest.matrix_world.translation + normal * 0.008
    add_poly("ChestWell", back_at, normal, shape_pts("circle", radius * 1.28), 0.004, (0.03, 0.03, 0.04), 0.0)
    add_poly("ChestDecal", center, normal, shape_pts(kind, radius), 0.006, color, 1.4)


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
    cam_data.lens = 42
    cam_data.clip_start = 0.05
    cam_data.clip_end = 40
    cam = bpy.data.objects.new("MenuCam", cam_data)
    scene.collection.objects.link(cam)
    side = Vector((-face.y, face.x, 0)).normalized()
    origin = Vector((0.0, 0.0, 0.88))
    cam.location = origin + face * 3.15 + side * 0.48 + Vector((0.0, 0.0, 0.42))
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
        scene.cycles.samples = 16
    elif hasattr(scene, "eevee"):
        scene.eevee.taa_render_samples = 16
    scene.render.resolution_x = 640
    scene.render.resolution_y = 960
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


def prepare(path, prefix, color, kind):
    fresh = import_prefixed(path, prefix)
    for obj in list(fresh):
        if obj.type in ("CAMERA", "LIGHT"):
            bpy.data.objects.remove(obj, do_unlink=True)
    objs = [obj for obj in bpy.data.objects if obj.name.startswith(prefix)]
    roots = [obj for obj in objs if obj.parent is None]
    print(prefix, "count", len(objs))
    stand_up(roots, objs)
    pose_idle(objs)
    pose_hang(objs)
    plant(roots, objs)
    tint(objs, color, prefix)
    lo, hi = world_bounds(objs)
    mid = (lo + hi) * 0.5
    move_root(roots, -mid.x, YAW)
    bpy.context.view_layer.update()
    chest_mark(objs, kind, color)
    feet = [obj for obj in meshes(objs) if "Foot" in obj.name]
    acc = Vector((0, 0, 0))
    n = 0
    for obj in feet:
        acc += obj.matrix_world.translation
        n += 1
    foot = acc / max(1, n)
    disc(foot.x, foot.y, color)
    lo, hi = world_bounds(objs)
    print(prefix, "bounds", tuple(round(v, 3) for v in lo), tuple(round(v, 3) for v in hi), "sole", round(foot_min_z(objs), 4))
    return face_axis(objs)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for index, (prefix, path, color, kind) in enumerate(SEATS):
        wipe()
        face = prepare(path, prefix, color, kind)
        lights()
        camera(face)
        render(os.path.join(OUT_DIR, "seat%d.png" % index))


if __name__ == "__main__":
    main()
