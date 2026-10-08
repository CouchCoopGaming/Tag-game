"""Pass 5 stills. Shopfronts, court dressing, harbor, houses, and a street.

  blender --background --python Tools/Blender/AssetLibrary/render_pass5.py
"""

import math
import os
import random
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
import render_pass3 as p3  # noqa: E402
from _common import _ensure_materials, unity_to_blender  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass5")


def _spawn(found, name, pos, yaw):
    return r._spawn(found[name](), pos, yaw)


def _centers(length, step=4.0):
    n = max(1, int(math.ceil((length + 0.2) / step)))
    start = -step * (n - 1) / 2.0
    return [start + i * step for i in range(n)]


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


def _block(x, z, w, d, h, mat, y0=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(x, y0 + h * 0.5, z))
    obj = bpy.context.active_object
    obj.scale = (w, d, h)
    obj.data.materials.append(mat)
    return obj


def _horizon(night):
    """Layered ridges and a stepped skyline. No tree cones."""
    sky = (0.05, 0.07, 0.10) if night else (0.22, 0.26, 0.32)
    ridge = (0.08, 0.10, 0.09) if night else (0.16, 0.20, 0.18)
    far = (0.10, 0.13, 0.16) if night else (0.45, 0.52, 0.58)
    mat = r._principled("Skyline", sky, 0.94)
    mid = r._principled("Ridge", ridge, 0.96)
    back = r._principled("FarRidge", far, 0.97)
    ground = r._principled("HorizonGround", (0.10, 0.12, 0.09) if not night else (0.03, 0.04, 0.05), 0.95)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=unity_to_blender(0.0, -0.4, 70.0))
    band = bpy.context.active_object
    band.scale = (220.0, 80.0, 0.2)
    band.data.materials.append(ground)
    _block(0.0, 96.0, 240.0, 28.0, 7.5, back)
    _block(18.0, 84.0, 200.0, 16.0, 4.2, back)
    _block(-30.0, 74.0, 170.0, 14.0, 3.4, mid)
    _block(40.0, 70.0, 120.0, 10.0, 2.6, mid)
    rng = random.Random(5)
    for i in range(-16, 17):
        height = 3.2 + (rng.random() ** 1.35) * 18.0
        width = 2.4 + rng.random() * 2.6
        depth = 2.2 + rng.random() * 1.8
        x = i * 7.2 + rng.uniform(-0.6, 0.6)
        z = 54.0 + rng.uniform(0.0, 5.0)
        _block(x, z, width, depth, height, mat)
        if height > 10.0:
            _block(x, z + 0.2, width * 0.58, depth * 0.58, height + 3.2, mat)
        if rng.random() > 0.84:
            _block(x + width * 0.15, z, 0.35, 0.35, height + 6.5, mat)


def _begin(wide=True):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=wide)
    scene.cycles.samples = 16
    _ensure_materials()
    r._world(scene, night=False)
    return scene


def _lot(found, sx, sz):
    """Sidewalk and gutter around a building whose front is +Z."""
    objs = []
    front = sz * 0.5
    back = -sz * 0.5
    z_walk = front + 1.45
    for x in _centers(sx + 2.4):
        objs.append(_spawn(found, "Sidewalk", (x, 0, z_walk), 90))
        objs.append(_spawn(found, "Gutter", (x, 0, z_walk + 1.20), 90))
    z_back = back - 1.45
    for x in _centers(sx + 2.4):
        objs.append(_spawn(found, "Sidewalk", (x, 0, z_back), -90))
    x_neg = -sx * 0.5 - 1.45
    x_pos = sx * 0.5 + 1.45
    for z in _centers(sz + 1.2):
        if z > front - 0.2 or z < back + 0.2:
            continue
        objs.append(_spawn(found, "Sidewalk", (x_neg, 0, z), 0))
        objs.append(_spawn(found, "Gutter", (x_neg - 1.20, 0, z), 0))
        objs.append(_spawn(found, "Sidewalk", (x_pos, 0, z), 180))
        objs.append(_spawn(found, "Gutter", (x_pos + 1.20, 0, z), 180))
    return objs


def _shop(found, name, sx, sz, path, azimuth):
    scene = _begin(wide=True)
    obj = _spawn(found, name, (0, 0, 0), 0)
    extra = _lot(found, sx, sz)
    pad = max(sx, sz) * 0.5 + 6.0
    p3._rim(0, 0, -0.05, pad, pad)
    p3._slab(0, 0, 0.01, pad * 0.92, pad * 0.92, "Lib_Concrete")
    r._frame(scene, [obj] + extra, fill=0.80, elevation=14.0, azimuth=azimuth)
    r._render(scene, path)


def _court(found):
    scene = _begin(wide=True)
    specs = [
        ("Court", (0, 0, 0), 0),
        ("Hoop", (0, 0, -10.572), 0),
        ("Hoop", (0, 0, 10.572), 180),
        ("CourtFence", (0, 0, 0), 0),
        ("Bench_Wood", (8.35, 0, -1.4), -90),
        ("ParkLamp", (8.8, 0, -7.2), -90),
        ("ParkLamp", (-8.8, 0, 6.4), 90),
        ("LightPost_Single", (8.9, 0, 4.8), 0),
    ]
    objs = [_spawn(found, name, pos, yaw) for name, pos, yaw in specs]
    p3._rim(0, 0, -0.06, 28.0, 34.0)
    p3._slab(0, 0, 0.0, 24.0, 30.0, "Lib_FoliageDark")
    _horizon(False)
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
    objs = [_spawn(found, name, pos, yaw) for name, pos, yaw in specs]
    # Lines from the boat's inboard cleats to the dock cleats, and one up to a quay bollard.
    for local, cleat in (
        ((-0.52, 0.97, -1.45), (1.35, 0.70, dock_pos[2] - 1.55)),
        ((-0.52, 0.97, 1.70), (1.35, 0.70, dock_pos[2] + 1.55)),
    ):
        _rope(_yaw_point(boat_pos, boat_yaw, local), cleat)
    _rope(_yaw_point(boat_pos, boat_yaw, (-0.52, 0.97, -1.45)), quay_bollard)
    p3._rim(0.5, -3.5, -0.08, 32.0, 26.0)
    p3._slab(1.0, -6.0, 0.0, 28.0, 16.0, "Lib_Water")
    _horizon(False)
    # From the water, so the cabin boat is in front of the quay face instead of hidden behind the apron.
    r._frame(scene, objs, fill=0.78, elevation=18.0, azimuth=205.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_harbor.png"))


def _house(found, name, path, azimuth):
    scene = _begin(wide=True)
    obj = _spawn(found, name, (0, 0, 0), 0)
    p3._rim(0, 0.4, -0.04, 16.0, 16.0)
    p3._slab(0, 0.4, 0.01, 14.0, 14.0, "Lib_FoliageDark")
    p3._slab(0, 5.6, 0.02, 3.2, 2.4, "Lib_Concrete")
    _horizon(False)
    r._frame(scene, [obj], fill=0.78, elevation=15.0, azimuth=azimuth)
    r._render(scene, path)


def _street(found):
    scene = _begin(wide=True)
    specs = [
        ("Road_Straight", (0, 0, -12), 0),
        ("Road_Straight", (0, 0, -8), 0),
        ("Road_Straight", (0, 0, -4), 0),
        ("Road_Crosswalk", (0, 0, 0), 0),
        ("Road_Straight", (0, 0, 4), 0),
        ("Road_Straight", (0, 0, 8), 0),
        ("Road_Straight", (0, 0, 12), 0),
        ("Store_Diner", (9.25, 0, -12.0), -90),
        ("Store_Corner", (9.35, 0, 0.0), -90),
        ("Store_Laundromat", (9.15, 0, 12.2), -90),
        ("LightPost_Single", (5.05, 0, -8.2), 0),
        ("LightPost_Single", (5.05, 0, 1.5), 0),
        ("LightPost_Double", (5.05, 0, 10.5), 0),
        ("FireHydrant", (3.85, 0, -2.2), 15),
        ("FireHydrant", (3.85, 0, 6.4), -10),
        ("Bench_Wood", (5.15, 0, -4.6), 90),
        ("Bench_Wood", (5.15, 0, 4.2), 90),
        ("Bench_Wood", (5.15, 0, 14.0), 90),
        ("Mannequin", (2.2, 0, 0.3), 200),
        ("Tree", (-8.5, 0, -8.0), 0),
        ("Tree_Maple", (-8.2, 0, 8.5), 20),
    ]
    for z in (-16, -12, -8, -4, 0, 4, 8, 12, 16):
        specs.append(("Sidewalk", (4.55, 0, z), 0))
        specs.append(("Gutter", (3.25, 0, z), 0))
        specs.append(("Sidewalk", (-4.55, 0, z), 180))
        specs.append(("Gutter", (-3.28, 0, z), 180))
    objs = [_spawn(found, name, pos, yaw) for name, pos, yaw in specs]
    p3._rim(2.0, 0.0, -0.05, 26.0, 42.0)
    _horizon(False)
    r._frame(scene, objs, fill=0.78, elevation=14.0, azimuth=42.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_street.png"))


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
    found = r._catalog()
    shots = (
        ("store_corner", lambda: _shop(found, "Store_Corner", 8.0, 7.2, os.path.join(STILL_DIR, "store_corner.png"), 42)),
        ("store_diner", lambda: _shop(found, "Store_Diner", 10.0, 7.0, os.path.join(STILL_DIR, "store_diner.png"), 34)),
        ("store_laundromat", lambda: _shop(found, "Store_Laundromat", 9.2, 6.8, os.path.join(STILL_DIR, "store_laundromat.png"), -36)),
        ("court", _court),
        ("harbor", _harbor),
        ("house_gable_front", lambda: _house(found, "House_Gable", os.path.join(STILL_DIR, "house_gable_front.png"), 40)),
        ("house_gable_back", lambda: _house(found, "House_Gable", os.path.join(STILL_DIR, "house_gable_back.png"), 220)),
        ("house_hip_front", lambda: _house(found, "House_Hip", os.path.join(STILL_DIR, "house_hip_front.png"), 40)),
        ("house_hip_back", lambda: _house(found, "House_Hip", os.path.join(STILL_DIR, "house_hip_back.png"), 220)),
        ("street", _street),
    )
    for name, fn in shots:
        if only and only not in name:
            continue
        if name in ("court", "harbor", "street"):
            fn(found)
        else:
            fn()
    print("PASS5_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
