"""Four stills for each mesh repaired in the models grade.

Output is Docs/AssetStills/pass25/ so Harbor and Park count with the rest.
Names are {snake}_quarter.png, _side.png, _close.png, and _scale.png.
The scale shot includes the 1.8 m figure.

  blender --background --python Tools/Blender/AssetLibrary/_so_quartet.py
  blender --background --python Tools/Blender/AssetLibrary/_so_quartet.py -- --only hydrant
"""

import importlib
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _so_render as so
import render_pass2 as r
from _common import blender_to_unity

# key, module stem, prop yaw, ground kind, sidewalk span or 0
JOBS = (
    ("firehydrant", "fire_hydrant", 180.0, "concrete", 3.4),
    ("firehydrant_yellow", "sk_hydrant_yellow", 180.0, "concrete", 3.4),
    ("sign_aframe", "sk_sign_aframe", 180.0, "concrete", 3.2),
    ("sign_streetname", "sk_sign_street", 180.0, "concrete", 4.2),
    ("streetmedian_planted", "sk_median_planted", 20.0, "asphalt", 0),
    ("planter_street", "sk_planter_street", 180.0, "concrete", 4.0),
    ("newspaperrack", "sk_newspaper_rack", 200.0, "concrete", 6.4),
    ("parkingmeter_twin", "sk_meter_twin", 180.0, "concrete", 3.6),
    ("parkingmeter_single", "sk_meter_single", 180.0, "concrete", 3.4),
    ("powerpole_span", "sk_power_pole", 180.0, "concrete", 0),
    ("walkup", "walkup", 200.0, "concrete", 0),
    ("gascanopy", "gas_canopy", 200.0, "concrete", 0),
    ("dock_straight", "dock_straight", 24.0, "wood", 0),
    ("fishingboat", "fishing_boat", 28.0, "water", 0),
    ("bench_woodiron", "sk_bench_woodiron", 180.0, "concrete", 4.4),
    ("bikerack_hoop3", "sk_bike_wave", 180.0, "concrete", 4.6),
    ("fountain_walk", "sk_fountain", 180.0, "concrete", 3.4),
    ("lightpost_globe", "sk_light_globe", 180.0, "concrete", 4.4),
    ("newsstand_corner", "sk_newsstand", 180.0, "concrete", 5.6),
)

OUT = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass25")


def _only():
    if "--only" in sys.argv:
        return sys.argv[sys.argv.index("--only") + 1]
    return ""


def _upper(obj, frac):
    bpy.context.view_layer.update()
    raw = []
    for vert in obj.data.vertices:
        world = obj.matrix_world @ vert.co
        ux, uy, uz = blender_to_unity(world.x, world.y, world.z)
        raw.append((uy, world))
    if not raw:
        return None
    ys = [uy for uy, _w in raw]
    cut = min(ys) + (max(ys) - min(ys)) * frac
    pts = [world for uy, world in raw if uy >= cut]
    if len(pts) < 8:
        return None
    return pts


def _shoot(fn, key, yaw, kind, slab):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 12
    scene.cycles.adaptive_threshold = 0.12
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    obj = r._spawn(asset, (0, 0, 0), yaw)
    if slab:
        so._sidewalk(slab)
    else:
        r._ground(kind, 80.0)
    r._frame(scene, [obj], fill=0.70, elevation=14.0, azimuth=40.0, aim_frac=0.42)
    quarter = os.path.join(OUT, key + "_quarter.png")
    r._render(scene, quarter)
    so._fit(quarter)
    r._frame(scene, [obj], fill=0.74, elevation=8.0, azimuth=96.0, aim_frac=0.46)
    side = os.path.join(OUT, key + "_side.png")
    r._render(scene, side)
    so._fit(side)
    pts = _upper(obj, 0.62)
    r._frame(scene, [obj], fill=0.84, elevation=16.0, azimuth=48.0, points=pts, aim_frac=0.48)
    close = os.path.join(OUT, key + "_close.png")
    r._render(scene, close)
    so._fit(close)
    mn, mx = asset.lods[0].unity_bounds()
    hier = (mx[0] + 0.95, 0.0, 0.0)
    meshes = so._place_hier(hier, 12.0)
    r._frame(scene, [obj] + meshes, fill=0.64, elevation=12.0, azimuth=36.0, aim_frac=0.40)
    scale = os.path.join(OUT, key + "_scale.png")
    r._render(scene, scale)
    so._fit(scale)
    print("QUARTET", asset.name, key)


def main():
    os.makedirs(OUT, exist_ok=True)
    needle = _only()
    for key, stem, yaw, kind, slab in JOBS:
        if needle and needle not in key and needle not in stem:
            continue
        module = importlib.import_module(stem)
        print("SHOT", key)
        _shoot(module.create, key, yaw, kind, slab)
    print("QUARTET_DIR", OUT)


if __name__ == "__main__":
    main()
