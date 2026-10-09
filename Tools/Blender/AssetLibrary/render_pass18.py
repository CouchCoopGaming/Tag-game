"""Pass 18 stills. Bench arms, brick bay, trash flap, light base, court, junction, cabin.

  blender --background --python Tools/Blender/AssetLibrary/render_pass18.py
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

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass18")
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)

# Both bench arms and the slat ends are in frame. Brick camera is the door/window view.
_PROPS = (
    ("bench", "Bench_Wood", 50.0, (2.85, 1.20, 2.15), (0.0, 0.50, 0.0), 32.0),
    ("brick", "Brick_Wall", 16.0, (2.6, 1.50, 3.7), (-0.25, 1.35, 0.05), 32.0),
    ("trash", "TrashCan_Lidded", 50.0, (1.25, 0.68, 1.15), (0.0, 0.48, 0.08), 42.0),
    ("cabin", "Cabin", 32.0, (5.4, 1.9, 5.6), (0.0, 1.35, 0.6), 28.0),
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


def _runner(found, specs, hier_pos, hier_yaw, cam, aim, lens, path):
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    _place_hier(hier_pos, hier_yaw)
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def _shots(found):
    shots = []
    for key, name, yaw, cam, aim, lens in _PROPS:
        for tag in ("before", "after"):
            path = os.path.join(STILL_DIR, "%s_%s.png" % (tag, key))
            shots.append((
                "%s_%s" % (tag, key),
                lambda n=name, y=yaw, c=cam, a=aim, l=lens, p=path: _prop(found, n, y, c, a, l, p),
            ))
    shots.append((
        "light_base",
        lambda: _runner(
            found, [("LightPost_Single", (0, 0, 0), 22.0, 1.0)],
            (0.72, 0.0, 0.48), 150.0,
            (1.7, 0.95, 1.55), (0.15, 0.7, 0.1), 36.0,
            os.path.join(STILL_DIR, "light_base.png"),
        ),
    ))
    shots.append((
        "basket",
        lambda: _prop_many(
            found,
            [("Court", (0, 0, 0), 0.0, 1.0), ("Hoop", (0, 0, -12.2), 0.0, 1.0)],
            (7.2, 3.6, -16.4), (0.2, 1.4, -8.5), 26.0,
            os.path.join(STILL_DIR, "basket.png"),
        ),
    ))
    shots.append((
        "basket_runner",
        lambda: _runner(
            found,
            [("Court", (0, 0, 0), 0.0, 1.0), ("Hoop", (0, 0, -12.2), 0.0, 1.0)],
            (1.35, 0.0, -10.7), 210.0,
            (3.6, 1.7, -8.6), (0.3, 1.6, -11.4), 30.0,
            os.path.join(STILL_DIR, "basket_runner.png"),
        ),
    ))
    shots.append((
        "junction",
        lambda: _prop(found, "Road_Junction", 28.0, (9.5, 6.5, 9.5), (0.0, 0.15, 0.0), 26.0,
                      os.path.join(STILL_DIR, "junction.png")),
    ))
    shots.append((
        "junction_runner",
        lambda: _runner(
            found, [("Road_Junction", (0, 0, 0), 0.0, 1.0)],
            (3.7, 0.27, 3.7), 230.0,
            (6.4, 1.7, 6.2), (2.2, 0.8, 2.2), 32.0,
            os.path.join(STILL_DIR, "junction_runner.png"),
        ),
    ))
    shots.append((
        "cabin_runner",
        lambda: _runner(
            found, [("Cabin", (0, 0, 0), 18.0, 1.0)],
            (1.8, 0.0, 3.5), 200.0,
            (4.4, 1.55, 5.2), (0.3, 1.2, 1.4), 30.0,
            os.path.join(STILL_DIR, "cabin_runner.png"),
        ),
    ))
    return shots


def _prop_many(found, specs, cam, aim, lens, path):
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    for name, fn in _shots(found):
        if only and not any(name.startswith(part) for part in only):
            continue
        fn()
    print("PASS18_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
