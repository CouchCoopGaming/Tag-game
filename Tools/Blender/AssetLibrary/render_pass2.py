"""Pass 2 stills. Level horizon, eye-level 3/4, context ground, contact shadow.

  blender --background --python Tools/Blender/AssetLibrary/render_pass2.py -- --phase before
  blender --background --python Tools/Blender/AssetLibrary/render_pass2.py -- --phase all

Before shots use whatever the scripts build at launch, so run --phase before
before editing hero scripts. After shots and vignettes are --phase all.
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _common  # noqa: E402
from _common import (  # noqa: E402
    _ensure_materials,
    _object_from_geo,
    _reset_scene,
    load_asset_modules,
    unity_to_blender,
)

REPO = _common.REPO
STILL_DIR = os.path.join(REPO, "Docs", "AssetStills", "pass2")
BEFORE_DIR = os.path.join(STILL_DIR, "before")
AFTER_DIR = os.path.join(STILL_DIR, "after")

HEROES = (
    "FireHydrant",
    "Hoop",
    "Court",
    "Cabin",
    "House",
    "Garage",
    "Brick_Wall",
    "Brick_Window",
    "Brick_Door",
    "Brick_Corner",
    "Bench_Wood",
    "LightPost_Single",
    "Dock_Straight",
    "Container_20",
    "TrashCan_Slat",
    "TrashCan_Lidded",
    "Tree",
)

# Context floor under a product shot. Vignettes bring their own ground.
GROUND_KIND = {
    "StreetFurniture": "asphalt",
    "Roads": "asphalt",
    "Buildings": "concrete",
    "Utility": "concrete",
    "Park": "grass",
    "Harbor": "wood",
    "Showcase": "studio",
}


def _phase():
    if "--phase" in sys.argv:
        return sys.argv[sys.argv.index("--phase") + 1]
    return "all"


def _only():
    if "--only" in sys.argv:
        return sys.argv[sys.argv.index("--only") + 1]
    return None


def _engine(scene, wide=False):
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 32
    scene.cycles.use_adaptive_sampling = True
    scene.cycles.adaptive_threshold = 0.08
    scene.cycles.max_bounces = 6
    scene.cycles.diffuse_bounces = 3
    scene.cycles.glossy_bounces = 2
    scene.cycles.transmission_bounces = 2
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 1280 if wide else 960
    scene.render.resolution_y = 720 if wide else 540
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.compression = 15
    # Day exposure sits under a brighter Nishita sky so asphalt stays mid-dark.
    # Night is lifted in _world so the emissive lenses still read.
    scene.view_settings.exposure = -0.40
    scene.view_settings.gamma = 1.0
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - Medium Contrast"
    except (TypeError, AttributeError):
        scene.view_settings.view_transform = "Filmic"
        scene.view_settings.look = "Medium Contrast"


def _world(scene, night=False):
    world = bpy.data.worlds.new("LibSky")
    scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    sky = nt.nodes.new("ShaderNodeTexSky")
    sky.sky_type = "NISHITA"
    sky.sun_disc = False
    sky.sun_size = math.radians(2.0)
    sky.sun_intensity = 0.04 if night else 0.20
    sky.sun_elevation = math.radians(7.0 if night else 32.0)
    sky.sun_rotation = math.radians(35.0)
    sky.altitude = 500
    sky.air_density = 1.0
    sky.dust_density = 0.2 if not night else 0.5
    sky.ozone_density = 1.4
    # Camera rays see the sky. Diffuse rays get a dim copy so the floor stays gray.
    bg_cam = nt.nodes.new("ShaderNodeBackground")
    bg_lit = nt.nodes.new("ShaderNodeBackground")
    nt.links.new(sky.outputs["Color"], bg_cam.inputs["Color"])
    nt.links.new(sky.outputs["Color"], bg_lit.inputs["Color"])
    bg_cam.inputs["Strength"].default_value = 0.012 if night else 0.045
    bg_lit.inputs["Strength"].default_value = 0.004 if night else 0.08
    mix = nt.nodes.new("ShaderNodeMixShader")
    light_path = nt.nodes.new("ShaderNodeLightPath")
    nt.links.new(light_path.outputs["Is Camera Ray"], mix.inputs[0])
    nt.links.new(bg_lit.outputs["Background"], mix.inputs[1])
    nt.links.new(bg_cam.outputs["Background"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    if night:
        scene.view_settings.exposure = 0.15
        _night_glow()

    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 0.04 if night else 3.4
    sun_data.angle = math.radians(2.4)
    sun_data.color = (1.0, 0.94, 0.84) if not night else (0.62, 0.70, 0.9)
    sun = bpy.data.objects.new("Sun", sun_data)
    scene.collection.objects.link(sun)
    elevation = math.radians(18 if night else 46)
    azimuth = math.radians(38)
    # Rays travel downhill toward the origin.
    direction = Vector((
        math.cos(elevation) * math.sin(azimuth),
        -math.cos(elevation) * math.cos(azimuth),
        -math.sin(elevation),
    ))
    sun.rotation_mode = "QUATERNION"
    sun.rotation_quaternion = direction.to_track_quat("-Z", "Z")

    fill_data = bpy.data.lights.new("Fill", "AREA")
    fill_data.energy = 2.0 if night else 18
    fill_data.size = 6
    fill_data.color = (0.75, 0.82, 0.95)
    fill = bpy.data.objects.new("Fill", fill_data)
    scene.collection.objects.link(fill)
    fill.location = Vector((-5.0, 4.0, 3.2))
    fill.rotation_mode = "QUATERNION"
    fill.rotation_quaternion = (Vector((0, 0, 0.4)) - fill.location).to_track_quat("-Z", "Z")


def _night_glow():
    """Windows and the lamp lens have to beat a dark sky. Day shots keep the authored strength."""
    boost = {"Lib_Lamp": 28.0, "Lib_Window": 8.0}
    for name, strength in boost.items():
        mat = bpy.data.materials.get(name)
        if mat is None or not mat.use_nodes:
            continue
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf and "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = strength


def _principled(name, color, rough, metal=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
        bsdf.inputs["Roughness"].default_value = rough
        bsdf.inputs["Metallic"].default_value = metal
    return mat


def _ground_material(kind):
    # Dedicated floors. Asset materials are too light under a Nishita sky and read as snow.
    if kind == "asphalt":
        return _principled("GroundAsphalt", (0.060, 0.059, 0.056), 0.93)
    if kind == "concrete":
        return _principled("GroundConcrete", (0.11, 0.108, 0.10), 0.88)
    if kind == "grass":
        return _principled("GroundGrass", (0.045, 0.085, 0.030), 0.96)
    if kind == "wood":
        return _principled("GroundWood", (0.13, 0.078, 0.042), 0.84)
    if kind == "water":
        mat = _principled("GroundWater", (0.03, 0.08, 0.10), 0.16)
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf and "Transmission Weight" in bsdf.inputs:
            bsdf.inputs["Transmission Weight"].default_value = 0.35
        return mat
    return _principled("GroundStudio", (0.090, 0.090, 0.090), 0.86)


def _ground(kind, size):
    bpy.ops.mesh.primitive_plane_add(size=size, location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.data.materials.append(_ground_material(kind))
    return obj


def _spawn(asset, pos, yaw=0.0):
    obj = _object_from_geo(asset.lods[0], asset.name)
    obj.location = Vector(unity_to_blender(pos[0], pos[1], pos[2]))
    # Blender Z is Unity Y, and a Z spin matches a Unity yaw.
    obj.rotation_euler = (0.0, 0.0, math.radians(yaw))
    return obj


def _bounds(objects):
    pts = []
    for obj in objects:
        for corner in obj.bound_box:
            pts.append(obj.matrix_world @ Vector(corner))
    return pts


def _frame(scene, objects, fill=0.70, elevation=16.0, azimuth=36.0):
    """Eye-level 3/4. Horizon stays level because camera up tracks world Z."""
    bpy.context.view_layer.update()
    pts = _bounds(objects)
    xs = [p.x for p in pts]
    ys = [p.y for p in pts]
    zs = [p.z for p in pts]
    mn = Vector((min(xs), min(ys), min(zs)))
    mx = Vector((max(xs), max(ys), max(zs)))
    center = (mn + mx) * 0.5
    # Aim a little below the bbox center so the contact shadow stays in frame.
    aim = Vector((center.x, center.y, mn.z + (mx.z - mn.z) * 0.42))
    az = math.radians(azimuth)
    el = math.radians(elevation)
    back = Vector((math.sin(az), -math.cos(az), 0.0))

    cam_data = bpy.data.cameras.new("Cam")
    # 32 mm at 16° down leaves a level sky band above a 70% subject.
    cam_data.lens = 32
    cam_data.sensor_width = 36
    cam_data.sensor_height = 24
    cam_data.sensor_fit = "AUTO"
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.rotation_mode = "QUATERNION"

    def place(dist):
        cam.location = aim + back * (dist * math.cos(el)) + Vector((0.0, 0.0, dist * math.sin(el)))
        direction = aim - cam.location
        # Camera up is +Y. The up axis here is the object's, not the world's.
        cam.rotation_quaternion = direction.to_track_quat("-Z", "Y")
        bpy.context.view_layer.update()

    def span_at(dist):
        place(dist)
        inv = cam.matrix_world.inverted()
        ax = cam.data.angle_x
        ay = cam.data.angle_y
        uvs = []
        for p in pts:
            co = inv @ p
            depth = -co.z
            if depth < 0.05:
                continue
            uvs.append((
                (co.x / depth) / math.tan(ax * 0.5),
                (co.y / depth) / math.tan(ay * 0.5),
            ))
        if len(uvs) < 2:
            return 0.0
        us = [u for u, _v in uvs]
        vs = [v for _u, v in uvs]
        return max(max(us) - min(us), max(vs) - min(vs))

    # NDC runs -1..1, so a 70% frame fill is a span of 1.40.
    target = fill * 2.0
    lo, hi = 0.35, 400.0
    for _ in range(22):
        mid = (lo + hi) * 0.5
        if span_at(mid) > target:
            lo = mid
        else:
            hi = mid
    place((lo + hi) * 0.5)
    right = cam.matrix_world.to_3x3() @ Vector((1.0, 0.0, 0.0))
    if abs(right.z) > 0.01:
        print("ROLL", right.z)
    return cam


def _render(scene, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("STILL", path, os.path.getsize(path))


def _catalog():
    load_asset_modules()
    found = {}
    for fn in _common.REGISTRY:
        _reset_scene()
        asset = fn()
        found[asset.name] = fn
        print("QUEUE", asset.category, asset.name)
    return found


def _shot(fn, path, kind, night=False, wide=False, fill=0.70):
    _reset_scene()
    scene = bpy.context.scene
    _engine(scene, wide=wide)
    _ensure_materials()
    _world(scene, night=night)
    asset = fn()
    obj = _spawn(asset, (0, 0, 0))
    span = max(asset.lods[0].unity_bounds()[1])
    _ground(kind, 400.0)
    _frame(scene, [obj], fill=fill)
    _render(scene, path)
    return asset


def _place_many(found, specs):
    objs = []
    for name, pos, yaw in specs:
        objs.append(_spawn(found[name](), pos, yaw))
    return objs


def _vignette(found, path, specs, kind, night=False, azimuth=42.0, elevation=16.0):
    _reset_scene()
    scene = bpy.context.scene
    _engine(scene, wide=True)
    _ensure_materials()
    _world(scene, night=night)
    objs = _place_many(found, specs)
    _ground(kind, 500.0)
    _frame(scene, objs, fill=0.72, elevation=elevation, azimuth=azimuth)
    _render(scene, path)


def _street(found, night=False):
    # Road runs along Z. Sidewalk curb faces -X, so the walk sits at x = +4.
    # Shop exterior is +Z; yaw -90 turns that face toward the street.
    specs = [
        ("Road_Straight", (0, 0, -4), 0),
        ("Road_Crosswalk", (0, 0, 0), 0),
        ("Road_Straight", (0, 0, 4), 0),
        ("Sidewalk", (4, 0, -4), 0),
        ("Sidewalk", (4, 0, 0), 0),
        ("Sidewalk", (4, 0, 4), 0),
        ("ShopFront", (6.15, 0, 0.2), -90),
        ("LightPost_Single", (4.55, 0, -3.3), 0),
        ("FireHydrant", (3.55, 0, -1.15), 20),
        ("TrashCan_Slat", (4.55, 0, 1.35), 0),
        ("Bench_Wood", (4.35, 0, 3.15), 90),
        ("Mailbox", (4.7, 0, -1.85), 180),
        ("Mannequin", (3.15, 0, 0.35), 200),
    ]
    name = "vignette_street_night.png" if night else "vignette_street.png"
    _vignette(found, os.path.join(STILL_DIR, name), specs, "asphalt", night=night, azimuth=48, elevation=16)


def _park(found):
    specs = [
        ("Court", (0, 0, 0), 0),
        ("Hoop", (0, 0, -7.35), 0),
        ("Hoop", (0, 0, 7.35), 180),
        ("CourtFence", (0, 0, 0), 0),
        ("Pavilion", (9.2, 0, 4.5), 20),
        ("Tree", (8.6, 0, -6.2), 0),
        ("Tree", (-8.8, 0, 6.0), 40),
        ("Bench_Wood", (7.8, 0, 1.2), -90),
        ("PicnicTable", (-8.2, 0, -1.6), 15),
        ("Mannequin", (1.2, 0, -1.5), 160),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_park.png"), specs, "grass", azimuth=38, elevation=18)


def _suburb(found):
    specs = [
        ("House", (0, 0, 0), 0),
        ("Garage", (7.2, 0, -0.4), 0),
        ("Cabin", (-7.4, 0, 1.2), 0),
        ("WoodFence", (-2.2, 0, -4.6), 0),
        ("WoodFence", (0.0, 0, -4.6), 0),
        ("WoodFence", (2.2, 0, -4.6), 0),
        ("ChainFence", (10.2, 0, 1.5), 90),
        ("ChainFence", (10.2, 0, 3.5), 90),
        ("UtilityPole", (11.5, 0, -5.5), 0),
        ("Mannequin", (2.2, 0, 5.4), 170),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_suburb.png"), specs, "grass", azimuth=46, elevation=15)


def _harbor(found):
    specs = [
        ("HarborWater", (2.5, 0, 1.0), 0),
        ("Dock_Straight", (0, 0, -2), 0),
        ("Dock_Straight", (0, 0, 2), 0),
        ("DockRamp", (0, 0, 6.0), 0),
        ("Piling", (-1.7, 0, -1.2), 0),
        ("Piling", (1.7, 0, 0.4), 0),
        ("Cleat", (-0.7, 0.62, 0.4), 0),
        ("Cleat", (0.7, 0.62, -0.8), 90),
        ("Crate", (-1.6, 0, 3.4), 12),
        ("Crate", (-1.5, 0.8, 3.5), -8),
        ("Container_20", (5.4, 0, -1.2), 90),
        ("HarborCrane", (8.6, 0, 3.5), -30),
        ("Boat", (2.6, 0, 1.6), 12),
        ("Buoy", (4.2, 0, 3.8), 0),
        ("HarborRail", (-0.95, 0.62, 0), 0),
        ("Mooring", (-4.2, 0, -1.5), 0),
        ("Mannequin", (0.35, 0.62, -0.4), 210),
    ]
    # Drop pieces the pass has not built yet so a partial render still frames.
    specs = [spec for spec in specs if spec[0] in found]
    _vignette(found, os.path.join(STILL_DIR, "vignette_harbor.png"), specs, "water", azimuth=40, elevation=16)


def main():
    phase = _phase()
    os.makedirs(STILL_DIR, exist_ok=True)
    found = _catalog()
    only = _only()
    if phase == "night":
        _street(found, night=True)
        print("NIGHT_DONE", STILL_DIR)
        return

    if phase == "before":
        os.makedirs(BEFORE_DIR, exist_ok=True)
        names = [only] if only else list(HEROES)
        for name in names:
            fn = found[name]
            _reset_scene()
            probe = fn()
            kind = GROUND_KIND.get(probe.category, "studio")
            _shot(fn, os.path.join(BEFORE_DIR, name + ".png"), kind)
        print("BEFORE_DONE", BEFORE_DIR)
        return

    for name, fn in found.items():
        _reset_scene()
        probe = fn()
        kind = GROUND_KIND.get(probe.category, "studio")
        _shot(fn, os.path.join(STILL_DIR, "%s_%s.png" % (probe.category, name)), kind)
        if name in HEROES:
            _shot(fn, os.path.join(AFTER_DIR, name + ".png"), kind)
    _street(found, night=False)
    _street(found, night=True)
    _park(found)
    _suburb(found)
    _harbor(found)
    print("RENDERS_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
