"""Street-object stills. Hero 3/4 plus a shot beside the Hier mannequin.

  blender --background --python Tools/Blender/AssetLibrary/_so_render.py -- --pass 1

1280x720 PNG, kept under 400 KB. Output: Docs/AssetStills/street_objects/passN/.
"""

import importlib
import math
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

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "street_objects", "pass%d" % PASS)
LIMIT = 400 * 1024
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)

# pass -> list of (key, asset name, yaw, hier offset xyz, hier yaw)
PASSES = {
    1: (
        ("planter_street", "Planter_Street", 18.0, (-1.35, 0.0, 0.85), 200.0),
        ("fire_siamese", "FireSiamese_Post", 24.0, (-0.85, 0.0, 0.45), 195.0),
        ("dog_bag", "DogBag_Post", 16.0, (-0.85, 0.0, 0.35), 200.0),
        ("bus_flag", "BusFlag_Stop", 12.0, (-1.05, 0.0, 0.55), 195.0),
        ("barrier_water", "Barrier_Water", 16.0, (-1.55, 0.0, 0.95), 200.0),
    ),
}


def _load(names):
    found = {}
    stems = {
        "Planter_Street": "sk_planter_street",
        "FireSiamese_Post": "sk_fire_siamese",
        "DogBag_Post": "sk_dog_bag",
        "BusFlag_Stop": "sk_bus_flag",
        "Barrier_Water": "sk_barrier_water",
    }
    for name in names:
        module = importlib.import_module(stems[name])
        found[name] = module.create
    return found


def _fit(path):
    """Quantize with system Pillow. Blender's Python does not ship PIL."""
    script = (
        "import sys; from PIL import Image; p=sys.argv[1]; "
        "im=Image.open(p).convert('RGB'); "
        "q=im.quantize(colors=96, method=Image.Quantize.MEDIANCUT); "
        "q.save(p, optimize=True)"
    )
    os.system("/usr/bin/python3 -c '%s' '%s'" % (script, path))
    size = os.path.getsize(path)
    print("SIZE", size, path)
    if size > LIMIT:
        print("SIZE_OVER", size, path)
    else:
        print("SIZE_OK", size, path)


def _hero(fn, path, kind="concrete"):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 20
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    obj = r._spawn(asset, (0, 0, 0))
    r._ground(kind, 80.0)
    r._frame(scene, [obj], fill=0.78, elevation=16.0, azimuth=38.0)
    r._render(scene, path)
    _fit(path)


def _place_hier(pos, yaw):
    if not os.path.isfile(HIER_FBX):
        print("HIER_MISSING", HIER_FBX)
        return []
    before = {obj.name for obj in bpy.data.objects}
    bpy.ops.import_scene.fbx(filepath=HIER_FBX)
    fresh = [obj for obj in bpy.data.objects if obj.name not in before]
    meshes = [obj for obj in fresh if obj.type == "MESH"]
    bpy.context.view_layer.update()
    zs = []
    for obj in meshes:
        for corner in obj.bound_box:
            zs.append((obj.matrix_world @ Vector(corner)).z)
    if not zs:
        print("HIER_EMPTY")
        return []
    foot = min(zs)
    height = max(zs) - foot
    scale = 1.8 / height if height > 0.2 else 1.0
    root = bpy.data.objects.new("HierRoot", None)
    bpy.context.scene.collection.objects.link(root)
    root.location = (0.0, 0.0, foot)
    tops = [obj for obj in fresh if obj.parent is None or obj.parent not in fresh]
    for obj in tops:
        world = obj.matrix_world.copy()
        obj.parent = root
        obj.matrix_world = world
    root.scale = (scale, scale, scale)
    root.location = Vector(unity_to_blender(*pos))
    root.rotation_euler = (0.0, 0.0, math.radians(yaw))
    bpy.context.view_layer.update()
    print("HIER", round(height, 3), round(scale, 4))
    return meshes


def _scale(fn, path, prop_yaw, hier_pos, hier_yaw, kind="concrete"):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 16
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    prop = r._spawn(asset, (0, 0, 0), prop_yaw)
    meshes = _place_hier(hier_pos, hier_yaw)
    r._ground(kind, 40.0)
    r._frame(scene, [prop] + meshes, fill=0.80, elevation=12.0, azimuth=32.0)
    r._render(scene, path)
    _fit(path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    shots = PASSES.get(PASS)
    if not shots:
        print("NO_PASS", PASS)
        sys.exit(1)
    found = _load([item[1] for item in shots])
    for key, name, yaw, hier_pos, hier_yaw in shots:
        if ONLY and ONLY not in key:
            continue
        kind = "asphalt" if "barrier" in key else "concrete"
        print("SHOT", key)
        _hero(found[name], os.path.join(STILL_DIR, key + ".png"), kind=kind)
        print("SHOT", key + "_scale")
        _scale(
            found[name],
            os.path.join(STILL_DIR, key + "_scale.png"),
            yaw,
            hier_pos,
            hier_yaw,
            kind=kind,
        )
    print("STREET_OBJECTS_STILLS", STILL_DIR)


if __name__ == "__main__":
    main()
