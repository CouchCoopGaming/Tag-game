"""Pass 17 stills. Cast bench, dark can, lamp base, hedge, plus three upgrades.

  blender --background --python Tools/Blender/AssetLibrary/render_pass17.py
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass2 as r  # noqa: E402
from _common import unity_to_blender  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass17")
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)

# Same cameras for before and after. The lamp is framed base-to-lantern.
_PROPS = (
    ("bench", "Bench_Wood", 38.0, (2.35, 0.95, 1.85), (0.15, 0.42, 0.0), 38.0),
    ("trash", "TrashCan_Lidded", 32.0, (1.55, 0.70, 1.45), (0.0, 0.46, 0.0), 38.0),
    ("lamp", "ParkLamp", 18.0, (4.0, 1.55, 5.2), (0.0, 1.55, 0.0), 28.0),
    ("median", "Median_Planter", 20.0, (2.2, 1.25, 2.5), (0.0, 0.48, 0.0), 36.0),
    ("dock", "Dock_Straight", 28.0, (5.2, 2.6, 4.4), (0.0, 0.55, 0.0), 32.0),
    ("brick", "Brick_Wall", 24.0, (3.6, 1.8, 5.4), (0.0, 1.55, 0.0), 32.0),
    ("light", "LightPost_Single", 20.0, (4.6, 2.2, 6.4), (0.0, 2.4, 0.0), 26.0),
)


def _prop(found, name, yaw, cam, aim, lens, path):
    scene = p6._begin(wide=True)
    p6._place(found, [(name, (0.0, 0.0, 0.0), yaw, 1.0)])
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def _place_hier(pos, yaw):
    if not os.path.isfile(HIER_FBX):
        print("HIER_MISSING", HIER_FBX)
        return None
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
        return None
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
    print("HIER", round(height, 3), round(scale, 4))
    return root


def _runner(found, name, prop_pos, prop_yaw, hier_pos, hier_yaw, cam, aim, lens, path, ground_y=-0.04):
    scene = p6._begin(wide=True)
    p6._place(found, [(name, prop_pos, prop_yaw, 1.0)])
    _place_hier(hier_pos, hier_yaw)
    p6._ground((0.34, 0.36, 0.32), y=ground_y)
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    shots = []
    for key, name, yaw, cam, aim, lens in _PROPS:
        for tag in ("before", "after"):
            path = os.path.join(STILL_DIR, "%s_%s.png" % (tag, key))
            shots.append((
                "%s_%s" % (tag, key),
                lambda n=name, y=yaw, c=cam, a=aim, l=lens, p=path: _prop(found, n, y, c, a, l, p),
            ))
    shots.append((
        "dock_runner",
        lambda: _runner(
            found, "Dock_Straight", (0, 0, 0), 18.0,
            (1.15, 0.62, 0.4), 200.0,
            (3.4, 1.7, 2.8), (0.3, 1.15, 0.1), 34.0,
            os.path.join(STILL_DIR, "dock_runner.png"),
        ),
    ))
    shots.append((
        "brick_runner",
        lambda: _runner(
            found, "Brick_Wall", (0, 0, 0), 12.0,
            (1.35, 0.0, 1.7), 160.0,
            (2.8, 1.45, 3.6), (0.2, 1.15, 0.4), 34.0,
            os.path.join(STILL_DIR, "brick_runner.png"),
        ),
    ))
    shots.append((
        "light_runner",
        lambda: _runner(
            found, "LightPost_Single", (0, 0, 0), 16.0,
            (0.85, 0.0, 0.7), 150.0,
            (2.6, 1.45, 2.8), (0.25, 1.2, 0.15), 32.0,
            os.path.join(STILL_DIR, "light_runner.png"),
        ),
    ))
    for name, fn in shots:
        if only and not any(name.startswith(part) for part in only):
            continue
        fn()
    print("PASS17_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
