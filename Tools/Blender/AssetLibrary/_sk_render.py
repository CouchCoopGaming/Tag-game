"""Street-kit stills. Close-ups plus one lineup with the 1.8 m figure.

  blender --background --python Tools/Blender/AssetLibrary/_sk_render.py -- --pass 1
"""

import importlib
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
from _common import unity_to_blender  # noqa: E402

PASS = 1
if "--pass" in sys.argv:
    PASS = int(sys.argv[sys.argv.index("--pass") + 1])
ONLY = None
if "--only" in sys.argv:
    ONLY = sys.argv[sys.argv.index("--only") + 1]

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "street_kit", "pass%d" % PASS)
LIMIT = 400 * 1024

MODULES = (
    "sk_light_globe",
    "sk_light_mast",
    "sk_hydrant_yellow",
    "sk_hydrant_silver",
    "sk_bench_metal",
    "sk_bench_woodiron",
    "sk_trash_drum",
    "sk_recycling_dual",
    "sk_dumpster_rear",
    "mannequin",
)


def _load():
    found = {}
    for stem in MODULES:
        module = importlib.import_module(stem)
        asset = module.create()
        found[asset.name] = module.create
    return found


def _shot(fn, path, kind="asphalt", fill=0.72):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 28
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    obj = r._spawn(asset, (0, 0, 0))
    r._ground(kind, 80.0)
    r._frame(scene, [obj], fill=fill, elevation=14.0, azimuth=38.0)
    r._render(scene, path)
    _fit(path)


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


def _close(fn, path, eye, aim, lens=48.0):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    scene.cycles.samples = 32
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    r._spawn(asset, (0, 0, 0))
    r._ground("asphalt", 40.0)
    _look(scene, eye, aim, lens)
    r._render(scene, path)
    _fit(path)


def _lineup(found, path):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.cycles.samples = 24
    r._ensure_materials()
    r._world(scene, night=False)
    specs = [
        ("Mannequin", (0.0, 0.0, 0.4), 200),
        ("FireHydrant_Yellow", (1.15, 0.0, 0.0), 25),
        ("FireHydrant_Silver", (2.15, 0.0, 0.15), -15),
        ("TrashCan_Drum", (3.15, 0.0, 0.1), 20),
        ("RecyclingBin_Dual", (4.55, 0.0, 0.15), 8),
        ("Bench_Metal", (6.55, 0.0, 0.55), 90),
        ("Bench_WoodIron", (8.85, 0.0, 0.55), 90),
        ("Dumpster_Rear", (11.35, 0.0, 0.2), 18),
        ("LightPost_Globe", (13.4, 0.0, -0.4), 15),
        ("LightPost_Mast", (16.2, 0.0, -1.2), 0),
    ]
    objs = []
    for name, pos, yaw in specs:
        objs.append(r._spawn(found[name](), pos, yaw))
    r._ground("concrete", 80.0)
    r._frame(scene, objs, fill=0.86, elevation=11.0, azimuth=18.0)
    r._render(scene, path)
    _fit(path)


def _fit(path):
    size = os.path.getsize(path)
    if size <= LIMIT:
        print("SIZE_OK", size, path)
        return
    try:
        from PIL import Image
        img = Image.open(path)
        img.save(path, format="PNG", optimize=True, compress_level=9)
    except Exception as exc:
        print("PIL", exc)
    size = os.path.getsize(path)
    print("SIZE", size, path)
    if size > LIMIT:
        print("SIZE_OVER", size, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    found = _load()
    shots = [
        ("light_globe", lambda: _shot(found["LightPost_Globe"], os.path.join(STILL_DIR, "light_globe.png"))),
        ("light_globe_head", lambda: _close(
            found["LightPost_Globe"], os.path.join(STILL_DIR, "light_globe_head.png"),
            (0.85, 3.15, 0.95), (0.0, 3.32, 0.0), 55)),
        ("light_mast", lambda: _shot(found["LightPost_Mast"], os.path.join(STILL_DIR, "light_mast.png"), fill=0.8)),
        ("light_mast_head", lambda: _close(
            found["LightPost_Mast"], os.path.join(STILL_DIR, "light_mast_head.png"),
            (1.15, 9.35, 6.15), (0.0, 9.55, 4.7), 48)),
        ("hydrant_yellow", lambda: _shot(found["FireHydrant_Yellow"], os.path.join(STILL_DIR, "hydrant_yellow.png"))),
        ("hydrant_silver", lambda: _shot(found["FireHydrant_Silver"], os.path.join(STILL_DIR, "hydrant_silver.png"))),
        ("bench_metal", lambda: _shot(found["Bench_Metal"], os.path.join(STILL_DIR, "bench_metal.png"))),
        ("bench_woodiron", lambda: _shot(found["Bench_WoodIron"], os.path.join(STILL_DIR, "bench_woodiron.png"))),
        ("trash_drum", lambda: _shot(found["TrashCan_Drum"], os.path.join(STILL_DIR, "trash_drum.png"))),
        ("recycling_dual", lambda: _shot(found["RecyclingBin_Dual"], os.path.join(STILL_DIR, "recycling_dual.png"))),
        ("dumpster_rear", lambda: _shot(found["Dumpster_Rear"], os.path.join(STILL_DIR, "dumpster_rear.png"))),
        ("kit_lineup", lambda: _lineup(found, os.path.join(STILL_DIR, "kit_lineup.png"))),
    ]
    for name, fn in shots:
        if ONLY and ONLY not in name:
            continue
        print("SHOT", name)
        fn()
    print("STREET_KIT_STILLS", STILL_DIR)


if __name__ == "__main__":
    main()
