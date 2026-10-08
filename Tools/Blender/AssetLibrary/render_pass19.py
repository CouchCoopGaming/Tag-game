"""Pass 19 stills. Junction, brick grain, cabin, court markings, street light.

  blender --background --python Tools/Blender/AssetLibrary/render_pass19.py
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

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass19")
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)

# Brick is the door and window the last review used. Cabin is the porch corner.
_PROPS = (
    ("brick", "Brick_Wall", 16.0, (2.6, 1.50, 3.7), (-0.25, 1.35, 0.05), 32.0, (0.34, 0.36, 0.32)),
    ("cabin", "Cabin", 32.0, (5.4, 1.9, 5.6), (0.0, 1.35, 0.6), 28.0, (0.34, 0.36, 0.32)),
)

_JUNCTION_CAM = ((18.0, 14.0, 20.0), (0.0, 0.25, 0.0), 28.0)
_JUNCTION_GROUND = (0.16, 0.18, 0.14)
_COURT_CAM = ((18.0, 16.0, 6.0), (0.0, 0.0, 0.0), 22.0)
_COURT_GROUND = (0.28, 0.34, 0.24)


def _prop(found, name, yaw, cam, aim, lens, path, ground=(0.34, 0.36, 0.32)):
    scene = p6._begin(wide=True)
    p6._place(found, [(name, (0.0, 0.0, 0.0), yaw, 1.0)])
    p6._ground(ground)
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


def _runner(found, specs, hier_pos, hier_yaw, cam, aim, lens, path, ground=(0.34, 0.36, 0.32)):
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    _place_hier(hier_pos, hier_yaw)
    p6._ground(ground)
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def _prop_many(found, specs, cam, aim, lens, path, ground=(0.34, 0.36, 0.32)):
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    p6._ground(ground)
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def _roads():
    """One straight tile beyond each junction arm. Local +Z is the tile length."""
    return [
        ("Road_Straight", (0.0, 0.0, 9.0), 0.0, 1.0),
        ("Road_Straight", (0.0, 0.0, -9.0), 0.0, 1.0),
        ("Road_Straight", (9.0, 0.0, 0.0), 90.0, 1.0),
        ("Road_Straight", (-9.0, 0.0, 0.0), 90.0, 1.0),
    ]


def _court(both, extras):
    specs = [("Court", (0.0, 0.0, 0.0), 0.0, 1.0), ("Hoop", (0.0, 0.0, -12.2), 0.0, 1.0)]
    if both:
        specs.append(("Hoop", (0.0, 0.0, 12.2), 180.0, 1.0))
    if extras:
        specs.append(("CourtFence", (0.0, 0.0, 0.0), 0.0, 1.0))
        specs.append(("Bench_Wood", (7.8, 0.0, 3.0), 90.0, 1.0))
    return specs


def _shots(found):
    shots = []
    for key, name, yaw, cam, aim, lens, ground in _PROPS:
        for tag in ("before", "after"):
            path = os.path.join(STILL_DIR, "%s_%s.png" % (tag, key))
            shots.append((
                "%s_%s" % (tag, key),
                lambda n=name, y=yaw, c=cam, a=aim, l=lens, p=path, g=ground: _prop(
                    found, n, y, c, a, l, p, g
                ),
            ))
    jcam, jaim, jlens = _JUNCTION_CAM
    shots.append((
        "before_junction",
        lambda: _prop(
            found, "Road_Junction", 0.0, jcam, jaim, jlens,
            os.path.join(STILL_DIR, "before_junction.png"), _JUNCTION_GROUND,
        ),
    ))
    shots.append((
        "after_junction",
        lambda: _prop_many(
            found,
            [("Road_Junction", (0.0, 0.0, 0.0), 0.0, 1.0)] + _roads(),
            jcam, jaim, jlens,
            os.path.join(STILL_DIR, "after_junction.png"), _JUNCTION_GROUND,
        ),
    ))
    shots.append((
        "junction_runner",
        lambda: _runner(
            found,
            [("Road_Junction", (0.0, 0.0, 0.0), 0.0, 1.0)] + _roads(),
            (4.2, 0.27, 4.2), 220.0,
            (7.4, 1.7, 6.6), (2.6, 0.6, 2.8), 30.0,
            os.path.join(STILL_DIR, "junction_runner.png"),
            _JUNCTION_GROUND,
        ),
    ))
    ccam, caim, clens = _COURT_CAM
    shots.append((
        "before_basket",
        lambda: _prop_many(
            found, _court(False, False), ccam, caim, clens,
            os.path.join(STILL_DIR, "before_basket.png"), _COURT_GROUND,
        ),
    ))
    shots.append((
        "after_basket",
        lambda: _prop_many(
            found, _court(True, True), ccam, caim, clens,
            os.path.join(STILL_DIR, "after_basket.png"), _COURT_GROUND,
        ),
    ))
    shots.append((
        "basket_runner",
        lambda: _runner(
            found, _court(True, True),
            (1.3, 0.0, -6.4), 210.0,
            (5.2, 1.75, -3.2), (0.2, 1.5, -8.6), 28.0,
            os.path.join(STILL_DIR, "basket_runner.png"),
            _COURT_GROUND,
        ),
    ))
    shots.append((
        "cabin_runner",
        lambda: _runner(
            found, [("Cabin", (0.0, 0.0, 0.0), 18.0, 1.0)],
            (1.8, 0.0, 3.5), 200.0,
            (4.4, 1.55, 5.2), (0.3, 1.2, 1.4), 30.0,
            os.path.join(STILL_DIR, "cabin_runner.png"),
        ),
    ))
    shots.append((
        "light_full",
        lambda: _runner(
            found, [("LightPost_Single", (0.0, 0.0, 0.0), 32.0, 1.0)],
            (1.55, 0.0, -0.15), 200.0,
            (7.2, 3.2, 8.4), (0.35, 2.55, 0.15), 24.0,
            os.path.join(STILL_DIR, "light_full.png"),
        ),
    ))
    return shots


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    for name, fn in _shots(found):
        if only and not any(name.startswith(part) for part in only):
            continue
        print("SHOT", name)
        fn()
    print("PASS19_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
