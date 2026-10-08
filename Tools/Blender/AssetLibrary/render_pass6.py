"""Pass 6 stills. Flush sidewalks, shop glass, street, court, harbor, and new props.

  blender --background --python Tools/Blender/AssetLibrary/render_pass6.py
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
import _snap  # noqa: E402
from _common import _ensure_materials, unity_to_blender  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass6")
WALK_Y = 0.27


def _spawn(found, name, pos, yaw, scale=1.0):
    obj = r._spawn(found[name](), pos, yaw)
    if abs(scale - 1.0) > 1e-6:
        # Blender Y is the Unity Z tile length.
        obj.scale = (1.0, float(scale), 1.0)
    return obj


def _place(found, specs):
    objs = []
    for item in specs:
        if len(item) == 3:
            name, pos, yaw = item
            scale = 1.0
        else:
            name, pos, yaw, scale = item
        objs.append(_spawn(found, name, pos, yaw, scale))
    return objs


def _hardscape(found, rows):
    return _place(found, rows)


def _mat(name, color, rough, metal=0.0, transmission=0.0):
    mat = r._principled(name, color, rough, metal)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and transmission and "Transmission Weight" in bsdf.inputs:
        bsdf.inputs["Transmission Weight"].default_value = transmission
    return mat


def _sky(scene):
    """Pale neutral sky. The ground plane, not a dark gradient, closes the frame."""
    world = bpy.data.worlds.new("Pass6Sky")
    scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (0.62, 0.74, 0.86, 1.0)
    bg.inputs["Strength"].default_value = 1.0
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 2.6
    sun_data.angle = math.radians(1.6)
    sun_data.color = (1.0, 0.97, 0.92)
    sun = bpy.data.objects.new("Sun", sun_data)
    scene.collection.objects.link(sun)
    elevation = math.radians(52.0)
    azimuth = math.radians(36.0)
    direction = Vector((
        math.cos(elevation) * math.sin(azimuth),
        -math.cos(elevation) * math.cos(azimuth),
        -math.sin(elevation),
    ))
    sun.rotation_mode = "QUATERNION"
    sun.rotation_quaternion = direction.to_track_quat("-Z", "Y")

    fill_data = bpy.data.lights.new("Fill", "AREA")
    fill_data.energy = 30.0
    fill_data.size = 8.0
    fill_data.color = (0.82, 0.88, 0.98)
    fill = bpy.data.objects.new("Fill", fill_data)
    scene.collection.objects.link(fill)
    fill.location = Vector(unity_to_blender(-8.0, 6.0, 4.0))
    fill.rotation_mode = "QUATERNION"
    fill.rotation_quaternion = (Vector((0.0, 0.0, 0.5)) - fill.location).to_track_quat("-Z", "Y")


def _begin(wide=True):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=wide)
    scene.cycles.samples = 16
    scene.cycles.max_bounces = 8
    scene.cycles.transmission_bounces = 6
    scene.cycles.glossy_bounces = 3
    scene.view_settings.exposure = -0.08
    _ensure_materials()
    _sky(scene)
    return scene


def _ground(color, y=-0.04, size=4000.0):
    bpy.ops.mesh.primitive_plane_add(size=size, location=unity_to_blender(0.0, y, 0.0))
    obj = bpy.context.active_object
    obj.data.materials.append(_mat("Pass6Ground", color, 0.92))
    return obj


def _sheet(cx, y, cz, w, d, mat):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=unity_to_blender(cx, y, cz))
    obj = bpy.context.active_object
    obj.scale = (w, d, 1.0)
    obj.data.materials.append(mat)
    return obj


def _block(x, z, w, d, h, mat, y0=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(x, y0 + h * 0.5, z))
    obj = bpy.context.active_object
    obj.scale = (w, d, h)
    obj.data.materials.append(mat)
    return obj


def _look(scene, eye, aim, lens=32.0):
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = lens
    cam_data.sensor_width = 36.0
    cam_data.sensor_height = 24.0
    cam_data.sensor_fit = "AUTO"
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.rotation_mode = "QUATERNION"
    cam.location = Vector(unity_to_blender(*eye))
    direction = Vector(unity_to_blender(*aim)) - cam.location
    cam.rotation_quaternion = direction.to_track_quat("-Z", "Y")
    return cam


def _rope(a, b):
    pa = Vector(unity_to_blender(*a))
    pb = Vector(unity_to_blender(*b))
    delta = pb - pa
    length = delta.length
    if length < 0.05:
        return
    bpy.ops.mesh.primitive_cylinder_add(radius=0.012, depth=length, vertices=6, location=(pa + pb) * 0.5)
    obj = bpy.context.active_object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = delta.to_track_quat("Z", "Y")
    mat = bpy.data.materials.get("Lib_SteelDark")
    if mat is not None:
        obj.data.materials.append(mat)


def _yaw_point(pos, yaw, local):
    a = math.radians(yaw)
    x, y, z = local
    xr = x * math.cos(a) + z * math.sin(a)
    zr = -x * math.sin(a) + z * math.cos(a)
    return (pos[0] + xr, pos[1] + y, pos[2] + zr)


def _skyline():
    """Two-tone towers with window rows and roof caps, on the far shore."""
    dark = _mat("SkyBase", (0.34, 0.36, 0.38), 0.9)
    light = _mat("SkyShaft", (0.62, 0.64, 0.66), 0.86)
    brick = _mat("SkyBrick", (0.48, 0.40, 0.36), 0.88)
    cap = _mat("SkyCap", (0.24, 0.26, 0.28), 0.8)
    glass = _mat("SkyWin", (0.16, 0.22, 0.30), 0.25, metal=0.15)
    warm = _mat("SkyLit", (0.86, 0.72, 0.42), 0.4)
    towers = (
        (-28, 48, 7.5, 6.0, 16.0, light),
        (-16, 50, 5.5, 5.0, 22.0, brick),
        (-6, 47, 6.5, 5.5, 11.0, light),
        (4, 52, 8.0, 6.5, 28.0, light),
        (16, 49, 6.0, 5.5, 18.0, brick),
        (28, 51, 7.0, 6.0, 13.0, light),
        (-22, 58, 9.0, 7.0, 9.0, brick),
        (10, 60, 10.0, 7.0, 8.0, light),
    )
    for x, z, w, d, h, shaft in towers:
        base_h = max(2.4, h * 0.22)
        _block(x, z, w, d, base_h, dark, 0.0)
        _block(x, z, w * 0.96, d * 0.96, h - base_h, shaft, base_h + 0.05)
        _block(x, z, w * 1.04, d * 1.04, 0.45, cap, h + 0.08)
        if h > 14.0:
            _block(x, z, w * 0.55, d * 0.55, 2.2, cap, h + 0.55)
        face = z - d * 0.5 - 0.04
        cols = max(2, int(w / 1.35))
        rows = max(2, int((h - 2.0) / 1.6))
        for row in range(rows):
            for col in range(cols):
                wx = x - w * 0.5 + (col + 0.5) * (w / cols)
                wy = 1.6 + row * 1.55
                if wy > h - 1.2:
                    continue
                pane = warm if (row + col) % 5 == 0 else glass
                _block(wx, face, min(0.55, w / cols * 0.55), 0.06, 0.70, pane, wy)


def _shop(found, name, sx, sz, path, azimuth):
    scene = _begin(wide=True)
    obj = _spawn(found, name, (0, 0, 0), 0)
    extra = _hardscape(found, _snap.lot_placements(sx, sz))
    _ground((0.46, 0.47, 0.44))
    r._frame(scene, [obj] + extra, fill=0.78, elevation=16.0, azimuth=azimuth)
    r._render(scene, path)


def _court(found):
    scene = _begin(wide=True)
    specs = [
        ("Court", (0, 0, 0), 0),
        ("Hoop", (0, 0, -10.572), 0),
        ("Hoop", (0, 0, 10.572), 180),
        ("CourtFence", (0, 0, 0), 0),
        ("Bench_Wood", (8.6, 0, -1.4), -90),
        ("ParkLamp", (9.2, 0, -7.2), -90),
        ("ParkLamp", (-9.2, 0, 6.4), 90),
    ]
    objs = _place(found, specs)
    _ground((0.34, 0.46, 0.30))
    r._frame(scene, objs, fill=0.82, elevation=18.0, azimuth=38.0)
    r._render(scene, os.path.join(STILL_DIR, "court_three_quarter.png"))


def _harbor(found):
    scene = _begin(wide=True)
    quay_bollard = (2.4, 1.15, -0.42)
    boat_pos = (3.25, 0.0, -4.85)
    boat_yaw = -8.0
    dock_pos = (0.2, 0.0, -4.9)
    specs = [
        ("Quay_Edge", (0, 0, 0), 0),
        ("Container_20", (3.6, 0.90, 2.55), 90),
        ("HarborCrane", (-5.4, 0.90, 3.3), 180),
        ("Dock_Straight", dock_pos, 0),
        ("Piling", (-1.5, 0, -4.4), 0),
        ("Piling", (1.7, 0, -7.2), 0),
        ("Boat", boat_pos, boat_yaw),
        ("Buoy", (6.8, 0, -8.4), 0),
        ("Mannequin", (-1.4, 0.90, 1.6), 210),
    ]
    objs = _place(found, specs)
    for local, cleat in (
        ((-0.52, 0.97, -1.45), (1.35, 0.70, dock_pos[2] - 1.55)),
        ((-0.52, 0.97, 1.70), (1.35, 0.70, dock_pos[2] + 1.55)),
    ):
        _rope(_yaw_point(boat_pos, boat_yaw, local), cleat)
    _rope(_yaw_point(boat_pos, boat_yaw, (-0.52, 0.97, -1.45)), quay_bollard)
    _ground((0.38, 0.42, 0.36), y=-0.05)
    water = _mat("Pass6Water", (0.08, 0.20, 0.26), 0.12, metal=0.04, transmission=0.35)
    # From the quay face (z = -1.36) across the basin to the shore.
    _sheet(0.0, 0.0, 18.0, 90.0, 39.0, water)
    shore = _mat("Pass6Shore", (0.42, 0.44, 0.38), 0.94)
    _sheet(0.0, -0.01, 58.0, 120.0, 44.0, shore)
    _skyline()
    r._frame(scene, objs, fill=0.76, elevation=16.0, azimuth=205.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_harbor.png"))


def _house(found, name, path, azimuth):
    scene = _begin(wide=True)
    obj = _spawn(found, name, (0, 0, 0), 0)
    _ground((0.36, 0.48, 0.30))
    r._frame(scene, [obj], fill=0.76, elevation=15.0, azimuth=azimuth)
    r._render(scene, path)


def _street(found):
    scene = _begin(wide=True)
    specs = list(_snap.street_hardscape())
    for name, pos, yaw in _snap.street_shops():
        specs.append((name, pos, yaw, 1.0))
    props = [
        ("LightPost_Single", (4.15, WALK_Y, -10.0), -90),
        ("LightPost_Single", (4.15, WALK_Y, 1.0), -90),
        ("LightPost_Single", (4.15, WALK_Y, 11.5), -90),
        ("FireHydrant", (3.85, WALK_Y, -4.2), 10),
        ("FireHydrant", (3.85, WALK_Y, 6.8), -8),
        ("Bench_Wood", (4.35, WALK_Y, -8.6), -90),
        ("Bench_Wood", (4.35, WALK_Y, 5.2), -90),
        ("Bench_Wood", (4.35, WALK_Y, 15.0), -90),
        ("TrashCan_Slat", (4.95, WALK_Y, -14.2), 0),
        ("TrashCan_Lidded", (4.95, WALK_Y, 9.4), 15),
        ("TrashCan_Slat", (4.95, WALK_Y, 16.4), 0),
        ("Mannequin", (3.9, WALK_Y, -1.5), 200),
        ("Tree", (-8.4, 0, -9.0), 0),
        ("Tree_Maple", (-8.2, 0, 7.5), 18),
    ]
    specs.extend((name, pos, yaw, 1.0) for name, pos, yaw in props)
    objs = _place(found, specs)
    _ground((0.40, 0.44, 0.38))
    # Eye height 3.4 m, on the street, looking down the block at the shopfronts.
    _look(scene, (1.5, 3.4, -17.2), (7.2, 1.6, 9.0), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_street.png"))


def _turntable(found, name, path):
    scene = _begin(wide=True)
    obj = _spawn(found, name, (0, 0, 0), 25)
    _ground((0.55, 0.56, 0.54))
    r._frame(scene, [obj], fill=0.72, elevation=18.0, azimuth=38.0)
    r._render(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    gap, signed, label, _rows = _snap.measure()
    print("MAX_GAP %.6f m (%s %+.6f)" % (gap, label, signed))
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
    found = r._catalog()
    shots = (
        ("store_corner", lambda: _shop(found, "Store_Corner", 8.0, 7.2, os.path.join(STILL_DIR, "store_corner.png"), 42)),
        ("store_diner", lambda: _shop(found, "Store_Diner", 10.0, 7.0, os.path.join(STILL_DIR, "store_diner.png"), 34)),
        ("store_laundromat", lambda: _shop(found, "Store_Laundromat", 9.2, 6.8, os.path.join(STILL_DIR, "store_laundromat.png"), -36)),
        ("court", lambda: _court(found)),
        ("harbor", lambda: _harbor(found)),
        ("house_gable_front", lambda: _house(found, "House_Gable", os.path.join(STILL_DIR, "house_gable_front.png"), 40)),
        ("house_gable_back", lambda: _house(found, "House_Gable", os.path.join(STILL_DIR, "house_gable_back.png"), 220)),
        ("house_hip_front", lambda: _house(found, "House_Hip", os.path.join(STILL_DIR, "house_hip_front.png"), 40)),
        ("house_hip_back", lambda: _house(found, "House_Hip", os.path.join(STILL_DIR, "house_hip_back.png"), 220)),
        ("street", lambda: _street(found)),
        ("playground", lambda: _turntable(found, "Playground", os.path.join(STILL_DIR, "playground.png"))),
        ("gazebo", lambda: _turntable(found, "Gazebo", os.path.join(STILL_DIR, "gazebo.png"))),
    )
    for name, fn in shots:
        if only and only not in name:
            continue
        fn()
    print("PASS6_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
