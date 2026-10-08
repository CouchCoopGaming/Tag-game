"""Pass 4 stills. Closed buildings, FIBA court, harbor, and a horizon.

  blender --background --python Tools/Blender/AssetLibrary/render_pass4.py
"""

import os
import random
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
import render_pass3 as p3  # noqa: E402
from _common import _ensure_materials, unity_to_blender  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass4")


def _boost_night_windows():
    mat = bpy.data.materials.get("Lib_WindowLit")
    if mat is None or not mat.use_nodes:
        return
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and "Emission Strength" in bsdf.inputs:
        bsdf.inputs["Emission Strength"].default_value = 6.0


def _vignette(found, path, specs, dress, night=False, azimuth=42.0, elevation=16.0):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 20
    _ensure_materials()
    r._world(scene, night=night)
    if night:
        _boost_night_windows()
    objs = []
    for name, pos, yaw in specs:
        if name not in found:
            print("SKIP", name)
            continue
        objs.append(r._spawn(found[name](), pos, yaw))
    dress()
    _horizon(night)
    r._frame(scene, objs, fill=0.78, elevation=elevation, azimuth=azimuth)
    r._render(scene, path)


def _horizon(night):
    """A soft city edge past the pad so the frame is not a void."""
    color = (0.05, 0.07, 0.10) if night else (0.16, 0.20, 0.26)
    mat = r._principled("Skyline", color, 0.92)
    ground = r._principled("HorizonGround", (0.10, 0.12, 0.09) if not night else (0.03, 0.04, 0.05), 0.95)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(0.0, -0.2, 78.0))
    band = bpy.context.active_object
    band.scale = (180.0, 50.0, 0.15)
    band.data.materials.append(ground)
    rng = random.Random(4)
    for i in range(-14, 15):
        height = 4.0 + (rng.random() ** 1.3) * 20.0
        x = i * 8.0 + rng.uniform(-1.2, 1.2)
        z = 62.0 + rng.uniform(0.0, 8.0)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(x, height * 0.5, z))
        block = bpy.context.active_object
        block.scale = (3.2, 2.8, height)
        block.data.materials.append(mat)
    tree = r._principled("HorizonTree", (0.05, 0.10, 0.05), 0.95)
    for i in range(-10, 11):
        bpy.ops.mesh.primitive_cone_add(
            radius1=1.6, depth=7.0, vertices=6,
            location=unity_to_blender(-70.0, 3.5, i * 6.0),
        )
        cone = bpy.context.active_object
        cone.data.materials.append(tree)


def _street(found, night=False):
    # Front faces point at the street. Yaw -90 turns local +Z toward -X.
    specs = [
        ("Road_Straight", (0, 0, -4), 0),
        ("Road_Crosswalk", (0, 0, 0), 0),
        ("Road_Straight", (0, 0, 4), 0),
        ("Sidewalk", (4, 0, -4), 0),
        ("Sidewalk", (4, 0, 0), 0),
        ("Sidewalk", (4, 0, 4), 0),
        ("Store_Diner", (8.7, 0, -9.2), -90),
        ("Store_Corner", (8.8, 0, 0.2), -90),
        ("Store_Laundromat", (8.6, 0, 9.4), -90),
        ("LightPost_Single", (4.55, 0, -3.3), 0),
        ("FireHydrant", (3.55, 0, -1.15), 20),
        ("TrashCan_Slat", (4.55, 0, 1.6), 0),
        ("Bench_Wood", (4.35, 0, 3.3), 90),
        ("Mannequin", (2.4, 0, 0.4), 200),
        ("Tree", (-6.5, 0, -6.0), 0),
        ("Tree_Maple", (-7.2, 0, 5.5), 15),
    ]
    name = "vignette_street_night.png" if night else "vignette_street.png"
    _vignette(found, os.path.join(STILL_DIR, name), specs, p3._street_dress, night=night, azimuth=48, elevation=14)


def _park(found):
    # Rim is 0.86 m in front of the backboard. Baskets sit 1.575*22/28 inside the inner baseline.
    specs = [
        ("Court", (0, 0, 0), 0),
        ("Hoop", (0, 0, -10.57), 0),
        ("Hoop", (0, 0, 10.57), 180),
        ("CourtFence", (0, 0, 0), 0),
        ("Pavilion", (10.6, 0, 3.5), 15),
        ("Tree", (9.4, 0, -7.5), 0),
        ("Tree_Maple", (-9.6, 0, 6.5), 25),
        ("Tree_Pine", (8.8, 0, 8.5), 0),
        ("Tree_Palm", (-10.2, 0, -6.4), 10),
        ("Bench_Wood", (8.4, 0, 0.4), -90),
        ("PicnicTable", (-8.8, 0, -2.2), 12),
        ("Mannequin", (1.6, 0, -3.0), 150),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_park.png"), specs, p3._park_dress, azimuth=36, elevation=22)


def _suburb(found):
    specs = [
        ("House_Gable", (-4.6, 0, 0.2), 0),
        ("House_Hip", (5.4, 0, -0.4), 0),
        ("Garage", (11.2, 0, -1.2), 0),
        ("WoodFence", (-1.2, 0, -5.2), 0),
        ("WoodFence", (1.0, 0, -5.2), 0),
        ("Tree_Maple", (-8.4, 0, 5.4), 0),
        ("Tree", (2.2, 0, 6.4), 20),
        ("Mannequin", (0.4, 0, 4.6), 170),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_suburb.png"), specs, p3._suburb_dress, azimuth=46, elevation=15)


def _harbor(found):
    specs = [
        ("Quay_Edge", (0, 0, 0), 0),
        ("Container_20", (5.4, 0.90, 0.4), 90),
        ("HarborCrane", (-5.6, 0.90, 0.6), 20),
        ("Bollard", (-2.2, 0.90, -0.85), 0),
        ("Bollard", (2.4, 0.90, -0.85), 0),
        ("Dock_Straight", (0.4, 0, -6.2), 0),
        ("Piling", (-1.6, 0, -4.6), 0),
        ("Piling", (2.0, 0, -7.4), 0),
        ("Boat", (3.6, 0, -8.4), 16),
        ("Buoy", (6.4, 0, -9.6), 0),
        ("Mooring", (-8.2, 0, -6.2), 0),
        ("Mannequin", (-1.2, 0.90, 1.1), 200),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_harbor.png"), specs, p3._harbor_dress, azimuth=42, elevation=16)


def _corner(fn, path, azimuth):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 20
    _ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    obj = r._spawn(asset, (0, 0, 0))
    p3._rim(0, 0, -0.04, 16, 16)
    p3._slab(0, 0, 0.02, 14, 14, "Lib_Concrete")
    r._frame(scene, [obj], fill=0.78, elevation=16.0, azimuth=azimuth)
    r._render(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    import store_corner
    import house_gable
    _corner(store_corner.create, os.path.join(STILL_DIR, "store_corner_front.png"), 40)
    _corner(store_corner.create, os.path.join(STILL_DIR, "store_corner_back.png"), 220)
    _corner(house_gable.create, os.path.join(STILL_DIR, "house_gable_front.png"), 40)
    _corner(house_gable.create, os.path.join(STILL_DIR, "house_gable_back.png"), 220)
    found = r._catalog()
    _street(found, night=False)
    _street(found, night=True)
    _park(found)
    _suburb(found)
    _harbor(found)
    print("PASS4_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
