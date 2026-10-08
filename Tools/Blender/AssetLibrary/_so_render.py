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
    2: (
        ("trash_cart", "TrashCart_96", 200.0, (-1.15, 0.0, 0.75), 160.0),
        ("recycling_cart", "RecyclingCart_96", 200.0, (-1.15, 0.0, 0.75), 160.0),
        ("cabinet_electrical", "Cabinet_Electrical", 18.0, (-1.15, 0.0, 0.55), 200.0),
        ("barrier_jersey", "Barrier_Jersey", 14.0, (-2.2, 0.0, 1.15), 200.0),
        ("barrel_traffic", "Barrel_Traffic", 16.0, (-0.95, 0.0, 0.45), 200.0),
        ("bollard_fixed", "Bollard_Fixed", 12.0, (-0.85, 0.0, 0.35), 200.0),
        ("bollard_removable", "Bollard_Removable", 12.0, (-0.85, 0.0, 0.35), 200.0),
    ),
    3: (
        ("sign_stop", "Sign_Stop", 12.0, (-1.05, 0.0, 0.55), 200.0),
        ("sign_speed", "Sign_Speed_25", 14.0, (-1.05, 0.0, 0.55), 200.0),
        ("ac_small", "AC_Condenser_Small", 22.0, (-1.15, 0.0, 0.7), 200.0),
        ("ac_large", "AC_Condenser_Large", 20.0, (-1.35, 0.0, 0.85), 200.0),
        ("ac_roof_small", "AC_Roof_Small", 24.0, (-1.15, 0.0, 0.7), 200.0),
        ("ac_roof_large", "AC_Roof_Large", 18.0, (-1.55, 0.0, 0.95), 200.0),
        ("picnic_table", "PicnicTable_Wood", 16.0, (-1.7, 0.0, 1.05), 200.0),
    ),
    4: (
        ("road_two", "StreetRoad_TwoLane", 18.0, (-4.2, 0.0, 1.4), 200.0),
        ("road_four", "StreetRoad_FourLane", 16.0, (-7.6, 0.0, 1.6), 200.0),
        ("road_intersection", "StreetRoad_Intersection", 22.0, (-4.6, 0.0, 2.4), 200.0),
        ("road_crosswalk", "StreetRoad_Crosswalk", 18.0, (-4.2, 0.0, 1.4), 200.0),
        ("road_stop", "StreetRoad_StopBar", 18.0, (-4.2, 0.0, 1.8), 200.0),
        ("road_arrows", "StreetRoad_Arrows", 18.0, (-4.2, 0.0, 1.4), 200.0),
        ("curb_straight", "StreetCurb_Straight", 200.0, (-1.4, 0.0, 1.5), 160.0),
        ("median_planted", "StreetMedian_Planted", 20.0, (-4.4, 0.0, 1.6), 200.0),
        ("sidewalk_joint", "Sidewalk_Joint", 200.0, (-2.3, 0.0, 1.4), 160.0),
    ),
    5: (
        ("kiosk_atm", "Kiosk_ATM", 18.0, (-1.25, 0.0, 0.7), 200.0),
        ("kiosk_charge", "Kiosk_Charge", 16.0, (-1.15, 0.0, 0.55), 200.0),
        ("cafe_set", "CafeSet_Bistro", 24.0, (-1.55, 0.0, 0.2), 200.0),
        ("awning_door", "Awning_Door", 20.0, (-2.3, 0.0, 0.4), 200.0),
    ),
    6: (
        ("fence_chain", "Fence_ChainGate", 18.0, (-2.0, 0.0, 0.8), 200.0),
        ("fence_iron", "Fence_Iron", 16.0, (-1.8, 0.0, 0.7), 200.0),
        ("scaffold_bay", "Scaffold_Bay", 22.0, (-1.8, 0.0, 0.4), 200.0),
    ),
    7: (
        ("sign_street", "Sign_StreetName", 28.0, (-1.35, 0.0, 0.7), 200.0),
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
        "TrashCart_96": "sk_trash_cart",
        "RecyclingCart_96": "sk_recycling_cart",
        "Cabinet_Electrical": "sk_cabinet_electrical",
        "Barrier_Jersey": "sk_barrier_jersey",
        "Barrel_Traffic": "sk_barrel_traffic",
        "Bollard_Fixed": "sk_bollard_fixed",
        "Bollard_Removable": "sk_bollard_removable",
        "Sign_Stop": "sign_stop",
        "Sign_Speed_25": "sk_sign_speed",
        "AC_Condenser_Small": "sk_ac_condenser",
        "AC_Condenser_Large": "sk_ac_condenser_large",
        "AC_Roof_Small": "sk_ac_roof",
        "AC_Roof_Large": "sk_ac_roof_large",
        "PicnicTable_Wood": "sk_picnic_table",
        "StreetRoad_TwoLane": "sk_road_two",
        "StreetRoad_FourLane": "sk_road_four",
        "StreetRoad_Intersection": "sk_road_intersection",
        "StreetRoad_Crosswalk": "sk_road_crosswalk",
        "StreetRoad_StopBar": "sk_road_stop",
        "StreetRoad_Arrows": "sk_road_arrows",
        "StreetCurb_Straight": "sk_curb_straight",
        "StreetMedian_Planted": "sk_median_planted",
        "Sidewalk_Joint": "sk_sidewalk_joint",
        "Kiosk_ATM": "sk_kiosk_atm",
        "Kiosk_Charge": "sk_kiosk_charge",
        "CafeSet_Bistro": "sk_cafe_set",
        "Awning_Door": "sk_awning_door",
        "Fence_ChainGate": "sk_fence_chain",
        "Fence_Iron": "sk_fence_iron",
        "Scaffold_Bay": "sk_scaffold_bay",
        "Sign_StreetName": "sk_sign_street",
    }
    for name in names:
        module = importlib.import_module(stems[name])
        found[name] = module.create
    return found


def _fit(path):
    """Quantize with system Pillow. Blender's Python does not ship PIL."""
    helper = "/tmp/so_fit.py"
    if not os.path.isfile(helper):
        with open(helper, "w", encoding="utf-8") as handle:
            handle.write(
                "import sys\nfrom PIL import Image\n"
                "p = sys.argv[1]\n"
                "im = Image.open(p).convert('RGB')\n"
                "q = im.quantize(colors=96, method=Image.Quantize.MEDIANCUT)\n"
                "q.save(p, optimize=True)\n"
            )
    os.system("/usr/bin/python3 %s %s" % (helper, path))
    size = os.path.getsize(path)
    print("SIZE", size, path)
    if size > LIMIT:
        print("SIZE_OVER", size, path)
    else:
        print("SIZE_OK", size, path)


def _hero(fn, path, kind="concrete", yaw=18.0):
    r._reset_scene()
    scene = bpy.context.scene
    r._engine(scene, wide=True)
    scene.cycles.samples = 20
    r._ensure_materials()
    r._world(scene, night=False)
    asset = fn()
    obj = r._spawn(asset, (0, 0, 0), yaw)
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
        kind = "asphalt" if any(part in key for part in ("barrier", "road", "curb", "median")) else "concrete"
        print("SHOT", key)
        _hero(found[name], os.path.join(STILL_DIR, key + ".png"), kind=kind, yaw=yaw)
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
