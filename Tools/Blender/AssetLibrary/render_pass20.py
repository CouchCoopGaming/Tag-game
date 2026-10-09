"""Pass 20 stills. Court width, junction curb returns, cabin logs.

  blender --background --python Tools/Blender/AssetLibrary/render_pass20.py
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

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass20")
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)

# Same porch corner the last review used. The log and door changes stay in frame.
_CABIN = (32.0, (5.4, 1.9, 5.6), (0.0, 1.35, 0.6), 28.0, (0.34, 0.36, 0.32))

# Wide enough for the longer arms (roads out to ±15 m) and for the old 7 m junction.
_JUNCTION_CAM = ((34.0, 26.0, 36.0), (0.0, 0.25, 0.0), 28.0)
_JUNCTION_GROUND = (0.16, 0.18, 0.14)
# Pass 19 court camera still holds a 15 m slab, both hoop placements, and the fence.
_COURT_CAM = ((18.0, 16.0, 6.0), (0.0, 0.0, 0.0), 22.0)
_COURT_GROUND = (0.28, 0.34, 0.24)

# Current arms end at 7 m, so a 4 m tile butts them at ±9. After the curb
# return the arms end at 11 m and the same tile sits at ±13.
_ROAD_BEFORE = 9.0
_ROAD_AFTER = 13.0
# Current pole sits outside the baseline. After, the backboard face is 1.2 m
# inside it and the rim is 1.575 m inside it.
_HOOP_BEFORE = 12.2
_HOOP_AFTER = 10.235


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


def _roads(center):
    """One straight tile beyond each arm. Local +Z is the tile length."""
    return [
        ("Road_Straight", (0.0, 0.0, center), 0.0, 1.0),
        ("Road_Straight", (0.0, 0.0, -center), 0.0, 1.0),
        ("Road_Straight", (center, 0.0, 0.0), 90.0, 1.0),
        ("Road_Straight", (-center, 0.0, 0.0), 90.0, 1.0),
    ]


def _court(hoop_z, both, extras):
    specs = [("Court", (0.0, 0.0, 0.0), 0.0, 1.0), ("Hoop", (0.0, 0.0, -hoop_z), 0.0, 1.0)]
    if both:
        specs.append(("Hoop", (0.0, 0.0, hoop_z), 180.0, 1.0))
    if extras:
        specs.append(("CourtFence", (0.0, 0.0, 0.0), 0.0, 1.0))
        specs.append(("Bench_Wood", (9.3, 0.0, 3.0), 90.0, 1.0))
    return specs


def _shots(found):
    shots = []
    yaw, cam, aim, lens, ground = _CABIN
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_cabin.png" % tag)
        shots.append((
            "%s_cabin" % tag,
            lambda p=path: _prop(found, "Cabin", yaw, cam, aim, lens, p, ground),
        ))
    jcam, jaim, jlens = _JUNCTION_CAM
    shots.append((
        "before_junction",
        lambda: _prop_many(
            found,
            [("Road_Junction", (0.0, 0.0, 0.0), 0.0, 1.0)] + _roads(_ROAD_BEFORE),
            jcam, jaim, jlens,
            os.path.join(STILL_DIR, "before_junction.png"), _JUNCTION_GROUND,
        ),
    ))
    shots.append((
        "after_junction",
        lambda: _prop_many(
            found,
            [("Road_Junction", (0.0, 0.0, 0.0), 0.0, 1.0)] + _roads(_ROAD_AFTER),
            jcam, jaim, jlens,
            os.path.join(STILL_DIR, "after_junction.png"), _JUNCTION_GROUND,
        ),
    ))
    shots.append((
        "junction_runner",
        lambda: _runner(
            found,
            [("Road_Junction", (0.0, 0.0, 0.0), 0.0, 1.0)] + _roads(_ROAD_AFTER),
            (5.2, 0.27, 5.2), 230.0,
            (14.0, 4.0, 12.0), (4.0, 0.4, 5.5), 28.0,
            os.path.join(STILL_DIR, "junction_runner.png"),
            _JUNCTION_GROUND,
        ),
    ))
    ccam, caim, clens = _COURT_CAM
    shots.append((
        "before_basket",
        lambda: _prop_many(
            found, _court(_HOOP_BEFORE, False, False), ccam, caim, clens,
            os.path.join(STILL_DIR, "before_basket.png"), _COURT_GROUND,
        ),
    ))
    shots.append((
        "after_basket",
        lambda: _prop_many(
            found, _court(_HOOP_AFTER, True, True), ccam, caim, clens,
            os.path.join(STILL_DIR, "after_basket.png"), _COURT_GROUND,
        ),
    ))
    shots.append((
        "basket_runner",
        lambda: _runner(
            found, _court(_HOOP_AFTER, True, True),
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
    print("PASS20_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
