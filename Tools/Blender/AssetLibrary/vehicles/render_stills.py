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
    "sedan_midsize",
    "sedan_compact",
    "crossover_compact",
    "bus_city40",
    "bus_city40_blue",
    "bus_city40_red",
    "mannequin",
)


def _load():
    sys.path.insert(0, HERE)
    sys.path.insert(0, ROOT)
    found = {}
    for stem in MODULES:
        module = importlib.import_module(stem)
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


def _with_figure(found, prop, path, prop_pos, prop_yaw, fig_pos):
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
    r._frame(scene, objs, fill=0.78, elevation=12.0, azimuth=28.0)
    r._render(scene, path)
    _fit(path)


def _transit_set(found, name, folder, length, height, door_z):
    out = os.path.join(STILL_ROOT, folder)
    os.makedirs(out, exist_ok=True)
    _shot(found[name], os.path.join(out, "hero.png"), fill=0.86)
    dist = max(18.0, length * 0.92)
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
    found = _load()
    if only is None or "midsize" in only or "sedan_midsize" in only:
        print("SHOT", "sedan_midsize")
        _sedan_set(found, "Sedan_Midsize", "sedan_midsize")
    if only is None or "sedan_compact" in only or only == "compact":
        print("SHOT", "sedan_compact")
        _sedan_set(found, "Sedan_Compact", "sedan_compact")
    if only is None or "crossover" in only:
        print("SHOT", "crossover_compact")
        _sedan_set(found, "Crossover_Compact", "crossover_compact")
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
    print("VEHICLE_STILLS", STILL_ROOT)


if __name__ == "__main__":
    main()
