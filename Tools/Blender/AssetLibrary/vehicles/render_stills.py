"""Vehicle stills. Side profile, hero, and a 1.8 m figure.

  blender --background --python Tools/Blender/AssetLibrary/vehicles/render_stills.py -- --only sedan_midsize
"""

import importlib
import math
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, ROOT)
sys.path.insert(0, HERE)

import render_pass2 as r  # noqa: E402
from _common import unity_to_blender  # noqa: E402

STILL_ROOT = os.path.join(r._common.REPO, "Docs", "AssetStills", "vehicles")
LIMIT = 400 * 1024

MODULES = (
    "sedan_mid_a",
    "sedan_midsize",
    "sedan_compact",
    "crossover_compact",
    "hatch_compact",
    "bus_city40",
    "bus_city40_blue",
    "bus_city40_red",
    "bus_city60",
    "mannequin",
)

FONT = os.path.join(r._common.REPO, "Tools", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf")


def _wanted(stem, only):
    if not only:
        return True
    if stem == "mannequin":
        return True
    return only in stem or stem in only


def _load(only=None):
    sys.path.insert(0, HERE)
    sys.path.insert(0, ROOT)
    found = {}
    for stem in MODULES:
        if not _wanted(stem, only):
            continue
        module = importlib.import_module(stem)
        if hasattr(module, "create_variants"):
            for asset in module.create_variants():
                found[asset.name] = (lambda a=asset: a)
        else:
            asset = module.create()
            found[asset.name] = module.create
    return found


def _fit(path):
    size = os.path.getsize(path)
    print("SIZE", size, path)
    if size > LIMIT:
        print("SIZE_OVER", size, path)


def _look(scene, eye, aim, lens):
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


def _shot(fn, path, fill=0.84):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    obj = r._spawn(fn(), (0, 0, 0))
    r._ground("asphalt", 80.0)
    r._frame(scene, [obj], fill=fill, elevation=14.0, azimuth=38.0)
    r._render(scene, path)
    _fit(path)


def _close(fn, path, eye, aim, lens):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    r._spawn(fn(), (0, 0, 0))
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _with_figure(found, prop, path, prop_pos, prop_yaw, fig_pos, fill=0.78):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    objs = [
        r._spawn(found["Mannequin"](), fig_pos, 200),
        r._spawn(found[prop](), prop_pos, prop_yaw),
    ]
    r._ground("asphalt", 30.0)
    r._frame(scene, objs, fill=fill, elevation=12.0, azimuth=28.0)
    r._render(scene, path)
    _fit(path)


def _transit_set(found, name, folder, length, height, door_z):
    out = os.path.join(STILL_ROOT, folder)
    os.makedirs(out, exist_ok=True)
    _shot(found[name], os.path.join(out, "hero.png"), fill=0.86)
    dist = max(18.0, length * 1.55)
    _close(
        found[name], os.path.join(out, "side.png"),
        (dist, height * 0.46, length * 0.04), (0.0, height * 0.40, 0.0), 46.0,
    )
    # Figure at the curb door so the 1.8 m body reads against the step and the glass.
    _close_pair(
        found, name, os.path.join(out, "scale.png"),
        (0.0, 0.0, 0.0), 0.0,
        (1.85, 0.0, door_z), 90.0,
        (6.2, 1.55, door_z + 2.4), (1.15, 1.15, door_z), 32.0,
    )


def _close_pair(found, prop, path, prop_pos, prop_yaw, fig_pos, fig_yaw, eye, aim, lens):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    r._spawn(found["Mannequin"](), fig_pos, fig_yaw)
    r._spawn(found[prop](), prop_pos, prop_yaw)
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _shot_az(fn, path, azimuth, elevation=14.0, fill=0.84):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    obj = r._spawn(fn(), (0, 0, 0))
    r._ground("asphalt", 80.0)
    r._frame(scene, [obj], fill=fill, elevation=elevation, azimuth=azimuth)
    r._render(scene, path)
    _fit(path)


def _dimensions(length, height, wheelbase, z_front, z_rear):
    """Side-view ticks in the sky and beside the body. Numbers only, no names."""
    from _common import Asset

    asset = Asset("Dims", "Vehicles", "Dimension overlay.")
    g = asset.begin(0)
    # Camera sits on +X. Bars are thick enough to read at a full-car frame.
    x = 1.45
    y_len = height + 0.22
    g.box((x, y_len, 0.0), (0.04, 0.028, length), "Lib_Lane")
    g.box((x, y_len - 0.10, length * 0.5), (0.04, 0.20, 0.028), "Lib_Lane")
    g.box((x, y_len - 0.10, -length * 0.5), (0.04, 0.20, 0.028), "Lib_Lane")
    g.text("%d mm" % int(round(length * 1000)), (x, y_len + 0.14, 0.0), 0.16, "Lib_Lane", extrude=0.012, yaw=90.0, font=FONT)
    z = -length * 0.5 - 0.28
    g.box((x, height * 0.5, z), (0.04, height, 0.028), "Lib_Lane")
    g.box((x, 0.02, z), (0.04, 0.02, 0.22), "Lib_Lane")
    g.box((x, height, z), (0.04, 0.02, 0.22), "Lib_Lane")
    g.text("%d mm" % int(round(height * 1000)), (x, height * 0.45, z - 0.28), 0.12, "Lib_Lane", extrude=0.012, yaw=90.0, font=FONT)
    mid = (z_front + z_rear) * 0.5
    g.box((x, 0.42, mid), (0.03, 0.022, wheelbase), "Lib_Lane")
    g.box((x, 0.42, z_front), (0.03, 0.16, 0.022), "Lib_Lane")
    g.box((x, 0.42, z_rear), (0.03, 0.16, 0.022), "Lib_Lane")
    g.text("%d mm" % int(round(wheelbase * 1000)), (x, 0.62, mid), 0.12, "Lib_Lane", extrude=0.012, yaw=90.0, font=FONT)
    asset.end()
    return asset


def _side_dims(fn, dims, path, eye, aim, lens):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    r._spawn(fn(), (0, 0, 0))
    r._spawn(dims, (0, 0, 0))
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _lineup(found, names, path, azimuth=36.0, elevation=12.0, fill=0.90):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 16
    r._ensure_materials()
    r._world(scene, night=False)
    objs = []
    span = 3.55
    origin = (len(names) - 1) * span * 0.5
    for i, name in enumerate(names):
        objs.append(r._spawn(found[name](), (i * span - origin, 0.0, 0.0), 0.0))
    r._ground("asphalt", 80.0)
    r._frame(scene, objs, fill=fill, elevation=elevation, azimuth=azimuth)
    r._render(scene, path)
    _fit(path)


def _sedan_set(found, name, folder):
    out = os.path.join(STILL_ROOT, folder)
    os.makedirs(out, exist_ok=True)
    _shot(found[name], os.path.join(out, "hero.png"))
    # Whole side, long lens, so the hood, pillar, roof arc, and deck read as a profile.
    _close(
        found[name], os.path.join(out, "side.png"),
        (12.0, 0.90, 0.05), (0.0, 0.78, 0.0), 78.0,
    )
    _with_figure(
        found, name, os.path.join(out, "scale.png"),
        (0.4, 0.0, 0.0), 18, (-1.7, 0.0, 1.55),
    )


def main():
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
    shot = None
    if "--shot" in sys.argv:
        shot = sys.argv[sys.argv.index("--shot") + 1]
    found = _load(only)
    if only is not None and "sedan_mid_a" in only:
        print("SHOT", "sedan_mid_a", shot or "all")
        out = os.path.join(STILL_ROOT, "sedan_mid_a")
        os.makedirs(out, exist_ok=True)
        if shot in (None, "hero"):
            _shot_az(found["Sedan_Mid_A_25"], os.path.join(out, "hero.png"), 48.0, elevation=11.0, fill=0.90)
        # 2025 sheet: 193.5 x 56.9 in, wheelbase 111.2. Axles from the 39.0 in front overhang.
        length = 193.5 * 0.0254
        height = 56.9 * 0.0254
        wheelbase = 111.2 * 0.0254
        z_front = length * 0.5 - 39.0 * 0.0254
        z_rear = z_front - wheelbase
        if shot in (None, "side"):
            _side_dims(
                found["Sedan_Mid_A_25"],
                _dimensions(length, height, wheelbase, z_front, z_rear),
                os.path.join(out, "side.png"),
                (13.5, 1.15, 0.0), (0.0, 0.78, 0.0), 82.0,
            )
        if shot in (None, "front"):
            _shot_az(found["Sedan_Mid_A_25"], os.path.join(out, "front.png"), 4.0, elevation=3.0, fill=0.90)
        if shot in (None, "rear"):
            _shot_az(found["Sedan_Mid_A_25"], os.path.join(out, "rear.png"), 228.0, elevation=11.0, fill=0.90)
        if shot in (None, "scale"):
            _with_figure(
                found, "Sedan_Mid_A_25", os.path.join(out, "scale.png"),
                (0.4, 0.0, 0.0), 18, (-1.7, 0.0, 1.55), fill=0.88,
            )
        if shot in (None, "lineup"):
            _lineup(
                found,
                (
                    "Sedan_Mid_A_21",
                    "Sedan_Mid_A_22",
                    "Sedan_Mid_A_23",
                    "Sedan_Mid_A_24",
                    "Sedan_Mid_A_25",
                ),
                os.path.join(out, "lineup.png"),
                azimuth=34.0, elevation=8.0, fill=0.90,
            )
        return
    if only is None or "midsize" in only or "sedan_midsize" in only:
        print("SHOT", "sedan_midsize")
        _sedan_set(found, "Sedan_Midsize", "sedan_midsize")
    if only is None or "sedan_compact" in only or only == "compact":
        print("SHOT", "sedan_compact")
        _sedan_set(found, "Sedan_Compact", "sedan_compact")
    if only is None or "crossover" in only:
        print("SHOT", "crossover_compact")
        _sedan_set(found, "Crossover_Compact", "crossover_compact")
    if only is None or "hatch" in only:
        print("SHOT", "hatch_compact")
        _sedan_set(found, "Hatch_Compact", "hatch_compact")
    if only is None or "bus" in only:
        # Door center from the same overhang used by the shell.
        door_z = 5.10
        for name, folder in (
            ("Bus_City40", "bus_city40"),
            ("Bus_City40_Blue", "bus_city40_blue"),
            ("Bus_City40_Red", "bus_city40_red"),
        ):
            if only not in (None, "bus") and only not in folder and folder not in only:
                continue
            print("SHOT", folder)
            _transit_set(found, name, folder, 12.50, 3.20, door_z)
    if only is None or "city60" in only or only == "bus60":
        print("SHOT", "bus_city60")
        _transit_set(found, "Bus_City60", "bus_city60", 18.54, 3.20, 8.0)
    print("VEHICLE_STILLS", STILL_ROOT)


if __name__ == "__main__":
    main()
