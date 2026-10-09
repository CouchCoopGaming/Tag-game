"""Lit 3/4 stills and one group shot per category.

Run:
  blender --background --python Tools/Blender/AssetLibrary/render_pass1.py
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import (  # noqa: E402
    STILL_DIR,
    _ensure_materials,
    _object_from_geo,
    _reset_scene,
    load_asset_modules,
    unity_to_blender,
)
import _common  # noqa: E402


def _world_and_light(scene):
    world = bpy.data.worlds.new("LibWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.62, 0.72, 0.82, 1.0)
        bg.inputs[1].default_value = 1.0
    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 3.2
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("Sun", sun_data)
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(52), math.radians(8), math.radians(38))
    fill_data = bpy.data.lights.new("Fill", "AREA")
    fill_data.energy = 250
    fill_data.size = 8
    fill = bpy.data.objects.new("Fill", fill_data)
    scene.collection.objects.link(fill)
    fill.location = unity_to_blender(-6, 4, -4)
    direction = Vector((0, 0, 0)) - fill.location
    fill.rotation_euler = direction.to_track_quat("-Z", "Z").to_euler()


def _engine(scene):
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 16
    scene.cycles.use_adaptive_sampling = True
    scene.cycles.adaptive_threshold = 0.1
    scene.cycles.max_bounces = 4
    scene.cycles.diffuse_bounces = 2
    scene.cycles.glossy_bounces = 2
    scene.cycles.transmission_bounces = 1
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 800
    scene.render.resolution_y = 450
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.compression = 15
    try:
        scene.view_settings.view_transform = "Filmic"
        scene.view_settings.look = "Medium High Contrast"
    except (TypeError, AttributeError):
        pass


def _ground(size):
    bpy.ops.mesh.primitive_plane_add(size=size, location=(0, 0, 0))
    obj = bpy.context.active_object
    mat = bpy.data.materials.get("Lib_Concrete")
    if mat is None:
        mat = bpy.data.materials.new("Ground")
        mat.diffuse_color = (0.72, 0.72, 0.70, 1)
    obj.data.materials.append(mat)
    return obj


def _spawn(asset, offset):
    obj = _object_from_geo(asset.lods[0], asset.name)
    obj.location = Vector(unity_to_blender(offset[0], offset[1], offset[2]))
    return obj


def _frame(scene, objects, margin=1.35):
    pts = []
    for obj in objects:
        for corner in obj.bound_box:
            pts.append(obj.matrix_world @ Vector(corner))
    if not pts:
        return
    xs = [p.x for p in pts]
    ys = [p.y for p in pts]
    zs = [p.z for p in pts]
    center = Vector(((min(xs) + max(xs)) * 0.5, (min(ys) + max(ys)) * 0.5, (min(zs) + max(zs)) * 0.5))
    radius = max(max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)) * 0.5
    dist = max(2.2, radius * 2.8) * margin
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 42
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = center + Vector((dist * 0.85, -dist * 0.95, dist * 0.48))
    direction = center - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Z").to_euler()
    scene.camera = cam


def _render(scene, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("STILL", path, os.path.getsize(path))


def _build_calls():
    load_asset_modules()
    return list(_common.REGISTRY)


def render_one(fn):
    _reset_scene()
    _engine(bpy.context.scene)
    _ensure_materials()
    _world_and_light(bpy.context.scene)
    asset = fn()
    obj = _spawn(asset, (0, 0, 0))
    _ground(max(12.0, max(asset.lods[0].unity_bounds()[1]) * 4))
    _frame(bpy.context.scene, [obj])
    path = os.path.join(STILL_DIR, "%s_%s.png" % (asset.category, asset.name))
    _render(bpy.context.scene, path)
    return asset.category


def render_group(category, fns):
    _reset_scene()
    scene = bpy.context.scene
    _engine(scene)
    if category in ("Buildings", "Harbor", "Park", "Roads"):
        scene.render.resolution_x = 960
        scene.render.resolution_y = 540
    _ensure_materials()
    _world_and_light(scene)
    cursor = 0.0
    objs = []
    depth = 4.0
    for fn in fns:
        asset = fn()
        mn, mx = asset.lods[0].unity_bounds()
        width = mx[0] - mn[0]
        depth = max(depth, mx[2] - mn[2], mx[1] - mn[1])
        px = cursor - mn[0]
        objs.append(_spawn(asset, (px, 0, 0)))
        cursor += width + 1.6
    _ground(max(40.0, cursor + depth))
    _frame(scene, objs, margin=1.15)
    path = os.path.join(STILL_DIR, "%s_group.png" % category)
    _render(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    calls = _build_calls()
    by_cat = {}
    order = []
    for fn in calls:
        # Probe the category without keeping the mesh. create() is cheap next to a render.
        _reset_scene()
        asset = fn()
        cat = asset.category
        by_cat.setdefault(cat, []).append(fn)
        if cat not in order:
            order.append(cat)
        print("QUEUE", cat, asset.name)
    for fn in calls:
        render_one(fn)
    for cat in order:
        render_group(cat, by_cat[cat])
    print("RENDERS_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
