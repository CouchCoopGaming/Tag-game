"""Pass 3 stills. Finite diorama bases, rebuilt trees, court, storefront, and quay.

  blender --background --python Tools/Blender/AssetLibrary/render_pass3.py
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
from _common import _ensure_materials, unity_to_blender  # noqa: E402

REPO = r._common.REPO
STILL_DIR = os.path.join(REPO, "Docs", "AssetStills", "pass3")
AFTER_DIR = os.path.join(STILL_DIR, "after")

UPGRADED = (
    "Tree", "Court", "Brick_Wall", "Brick_Window", "Brick_Door", "Brick_Corner",
    "ShopFront", "Brick_Parapet", "Median_Planter", "RooftopAC", "WallAC",
    "TrafficCone", "Bollard", "Mailbox", "WoodFence", "ChainFence", "PicnicTable",
    "Crate", "ParkLamp", "LightPost_Double", "Buoy", "Piling", "Dumpster",
)
NEW = (
    "Tree_Maple", "Tree_Pine", "Tree_Palm", "Storefront_Glass", "Roof_Parapet",
    "Quay_Edge", "CourtFence",
)


def _slab(cx, cz, y, w, d, kind):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(cx, y, cz))
    obj = bpy.context.active_object
    obj.scale = (w, d, 0.05)
    mat = bpy.data.materials.get(kind)
    if mat is None:
        mat = r._ground_material("studio")
    obj.data.materials.append(mat)
    return obj


def _rim(cx, cz, y, w, d):
    """Darker step under a pad so the base has an edge instead of running to the horizon."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(cx, y, cz))
    obj = bpy.context.active_object
    obj.scale = (w, d, 0.04)
    obj.data.materials.append(r._principled("DioramaRim", (0.05, 0.05, 0.045), 0.95))
    return obj


def _shot(fn, path, kind):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=False)
    _ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    obj = r._spawn(asset, (0, 0, 0))
    span = max(asset.lods[0].unity_bounds()[1])
    pad = max(3.2, span * 1.55)
    if kind == "water":
        _rim(0, 0, -0.04, pad * 1.18, pad * 1.18)
        _slab(0, 0, 0.02, pad, pad, "Lib_Water")
    elif kind == "grass":
        _rim(0, 0, -0.03, pad * 1.15, pad * 1.15)
        _slab(0, 0, 0.02, pad, pad, "Lib_FoliageDark")
    elif kind == "asphalt":
        _rim(0, 0, -0.03, pad * 1.15, pad * 1.15)
        _slab(0, 0, 0.02, pad, pad, "Lib_Asphalt")
    else:
        _rim(0, 0, -0.03, pad * 1.15, pad * 1.15)
        _slab(0, 0, 0.02, pad, pad, "Lib_Concrete")
    r._frame(scene, [obj], fill=0.70)
    r._render(scene, path)
    return asset


def _vignette(found, path, specs, dress, night=False, azimuth=42.0, elevation=16.0):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    _ensure_materials()
    r._world(scene, night=night)
    objs = []
    for name, pos, yaw in specs:
        if name not in found:
            print("SKIP", name)
            continue
        objs.append(r._spawn(found[name](), pos, yaw))
    dress()
    r._frame(scene, objs, fill=0.78, elevation=elevation, azimuth=azimuth)
    r._render(scene, path)


def _street_dress():
    _rim(2.2, 0.2, -0.05, 18.0, 16.0)
    _slab(0.0, 0.0, 0.0, 8.0, 14.0, "Lib_Asphalt")
    _slab(5.2, 0.0, 0.03, 3.2, 14.0, "Lib_Concrete")
    _slab(7.6, 0.0, 0.01, 2.4, 14.0, "Lib_FoliageDark")


def _street(found, night=False):
    specs = [
        ("Road_Straight", (0, 0, -4), 0),
        ("Road_Crosswalk", (0, 0, 0), 0),
        ("Road_Straight", (0, 0, 4), 0),
        ("Sidewalk", (4, 0, -4), 0),
        ("Sidewalk", (4, 0, 0), 0),
        ("Sidewalk", (4, 0, 4), 0),
        ("Brick_Wall", (6.55, 0, -4), -90),
        ("ShopFront", (6.55, 0, 0), -90),
        ("Brick_Door", (6.55, 0, 4), -90),
        ("Brick_Window", (6.55, 3.2, -4), -90),
        ("Brick_Window", (6.55, 3.2, 0), -90),
        ("Brick_Window", (6.55, 3.2, 4), -90),
        ("Roof_Parapet", (6.55, 6.4, -4), -90),
        ("Roof_Parapet", (6.55, 6.4, 0), -90),
        ("Roof_Parapet", (6.55, 6.4, 4), -90),
        ("LightPost_Single", (4.55, 0, -3.3), 0),
        ("FireHydrant", (3.55, 0, -1.15), 20),
        ("TrashCan_Slat", (4.55, 0, 1.6), 0),
        ("Bench_Wood", (4.35, 0, 3.3), 90),
        ("Mailbox", (4.7, 0, -1.9), 180),
        ("Mannequin", (2.6, 0, 0.4), 200),
    ]
    name = "vignette_street_night.png" if night else "vignette_street.png"
    _vignette(found, os.path.join(STILL_DIR, name), specs, _street_dress, night=night, azimuth=48, elevation=16)


def _park_dress():
    _rim(0, 0, -0.05, 32.0, 36.0)
    _slab(0, 0, 0.0, 28.0, 32.0, "Lib_FoliageDark")
    _slab(0, 0, 0.015, 16.0, 26.0, "Lib_Soil")


def _park(found):
    specs = [
        ("Court", (0, 0, 0), 0),
        ("Hoop", (0, 0, -10.7), 0),
        ("Hoop", (0, 0, 10.7), 180),
        ("CourtFence", (0, 0, 0), 0),
        ("Pavilion", (10.6, 0, 3.5), 15),
        ("Tree", (9.4, 0, -7.5), 0),
        ("Tree_Maple", (-9.6, 0, 6.5), 25),
        ("Tree_Pine", (8.8, 0, 8.5), 0),
        ("Bench_Wood", (8.4, 0, 0.4), -90),
        ("TrashCan_Slat", (8.8, 0, -1.8), 0),
        ("PicnicTable", (-8.8, 0, -2.2), 12),
        ("Mannequin", (1.6, 0, -3.0), 150),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_park.png"), specs, _park_dress, azimuth=36, elevation=17)


def _suburb_dress():
    _rim(1.0, 0.5, -0.04, 28.0, 20.0)
    _slab(1.0, 0.5, 0.0, 24.0, 16.0, "Lib_FoliageDark")
    _slab(1.5, 6.2, 0.02, 3.0, 4.0, "Lib_Concrete")


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
        ("Tree_Maple", (-3.5, 0, 6.2), 0),
        ("Mannequin", (2.2, 0, 5.4), 170),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_suburb.png"), specs, _suburb_dress, azimuth=46, elevation=15)


def _harbor_dress():
    # Dark water, finite, with a deeper rim. The quay and docks sit in this, not on a pale slab.
    _rim(0.0, -4.0, -0.08, 28.0, 24.0)
    _slab(0.0, -5.5, 0.0, 24.0, 18.0, "Lib_Water")


def _harbor(found):
    specs = [
        ("Quay_Edge", (-4.0, 0, 0), 0),
        ("Quay_Edge", (4.0, 0, 0), 0),
        ("Container_20", (2.2, 0.90, 0.55), 90),
        ("HarborCrane", (-3.4, 0.90, 0.7), 25),
        ("Bollard", (-1.0, 0.90, -0.85), 0),
        ("Bollard", (5.2, 0.90, -0.85), 0),
        ("Dock_Straight", (0.2, 0, -5.4), 0),
        ("Dock_Straight", (0.2, 0, -9.4), 0),
        ("Piling", (-1.2, 0, -4.4), 0),
        ("Piling", (1.5, 0, -6.6), 0),
        ("Piling", (-1.2, 0, -8.6), 0),
        ("Crate", (-0.5, 0.62, -5.8), 12),
        ("Crate", (0.6, 0.62, -7.6), -10),
        ("Cleat", (0.7, 0.62, -4.8), 0),
        ("Boat", (3.4, 0, -7.0), 18),
        ("Buoy", (5.8, 0, -9.2), 0),
        ("HarborRail", (0.9, 0.62, -6.4), 0),
        ("Mooring", (-5.6, 0, -6.5), 0),
        ("Mannequin", (0.3, 0.90, 1.0), 190),
    ]
    _vignette(found, os.path.join(STILL_DIR, "vignette_harbor.png"), specs, _harbor_dress, azimuth=42, elevation=16)


def main():
    phase = "all"
    if "--phase" in sys.argv:
        phase = sys.argv[sys.argv.index("--phase") + 1]
    os.makedirs(AFTER_DIR, exist_ok=True)
    found = r._catalog()
    if phase == "vignettes":
        _street(found, night=False)
        _street(found, night=True)
        _park(found)
        _suburb(found)
        _harbor(found)
        print("VIGNETTES_DONE", STILL_DIR)
        return
    show = list(UPGRADED) + list(NEW)
    for name in show:
        fn = found[name]
        r._reset_scene()
        probe = fn()
        kind = r.GROUND_KIND.get(probe.category, "studio")
        if name in ("Piling", "Buoy", "Quay_Edge", "Boat"):
            kind = "water"
        _shot(fn, os.path.join(STILL_DIR, "%s_%s.png" % (probe.category, name)), kind)
        if name in UPGRADED or name in NEW:
            _shot(fn, os.path.join(AFTER_DIR, name + ".png"), kind)
    _street(found, night=False)
    _street(found, night=True)
    _park(found)
    _suburb(found)
    _harbor(found)
    print("PASS3_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
