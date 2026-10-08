"""Pass 22 stills. JPEG quality 90, 1280x720, under 400 KB. No palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass22.py
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

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass22")
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)

# Same cabin framing as pass 21, which still holds the gable chimney and the door.
_CABIN = (32.0, (8.2, 5.5, 8.6), (0.3, 2.0, 0.2), 32.0, (0.34, 0.36, 0.32))
# One approach: the 10 cm yellow gap and a curb-to-curb crosswalk in the same frame.
_JUNCTION_CAM = ((6.4, 3.9, 15.4), (0.0, 0.20, 8.6), 36.0)
_JUNCTION_GROUND = (0.16, 0.18, 0.14)
_ROAD_AT = 13.0
# Low on the lit side so the pile row, stringers, ladder, and rope all read.
_DOCK = ((-3.6, 0.38, 4.6), (0.2, 0.10, 0.0), 32.0, (0.22, 0.28, 0.26))


def _jpg(scene, path):
    scene.render.image_settings.file_format = "JPEG"
    scene.render.image_settings.quality = 90
    scene.render.image_settings.color_mode = "RGB"
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


def _prop_many(found, specs, cam, aim, lens, path, ground):
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    p6._ground(ground)
    p6._look(scene, cam, aim, lens=lens)
    _jpg(scene, path)


def _dock_water():
    p6._ground((0.16, 0.14, 0.10), y=-1.35, size=40.0)
    p6._sheet(0.0, -0.02, 0.0, 40.0, 40.0, p6._mat("Pass22Water", (0.07, 0.14, 0.16), 0.12, transmission=0.72))


def _runner(found, specs, hier_pos, hier_yaw, cam, aim, lens, path, ground, water=False):
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    _place_hier(hier_pos, hier_yaw)
    if water:
        _dock_water()
    else:
        p6._ground(ground)
    p6._look(scene, cam, aim, lens=lens)
    _jpg(scene, path)


def _roads():
    c = _ROAD_AT
    return [
        ("Road_Straight", (0.0, 0.0, c), 0.0, 1.0),
        ("Road_Straight", (0.0, 0.0, -c), 0.0, 1.0),
        ("Road_Straight", (c, 0.0, 0.0), 90.0, 1.0),
        ("Road_Straight", (-c, 0.0, 0.0), 90.0, 1.0),
    ]


def _junction_specs():
    return [("Road_Junction", (0.0, 0.0, 0.0), 0.0, 1.0)] + _roads()


def _one(found, name, yaw, cam, aim, lens, path, ground):
    if name == "Dock_Straight":
        scene = p6._begin(wide=True)
        p6._place(found, [("Dock_Straight", (0.0, 0.0, 0.0), yaw, 1.0)])
        _dock_water()
        p6._look(scene, cam, aim, lens=lens)
        _jpg(scene, path)
        return
    _prop_many(found, [(name, (0.0, 0.0, 0.0), yaw, 1.0)], cam, aim, lens, path, ground)


def _shots(found):
    shots = []
    yaw, cam, aim, lens, ground = _CABIN
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_cabin.jpg" % tag)
        shots.append((
            "%s_cabin" % tag,
            lambda p=path, yaw=yaw, cam=cam, aim=aim, lens=lens, ground=ground: _one(
                found, "Cabin", yaw, cam, aim, lens, p, ground
            ),
        ))
    shots.append((
        "cabin_runner",
        lambda g=ground: _runner(
            found, [("Cabin", (0.0, 0.0, 0.0), 18.0, 1.0)],
            (1.8, 0.0, 3.5), 200.0,
            (5.6, 2.3, 6.0), (0.3, 1.7, 0.8), 28.0,
            os.path.join(STILL_DIR, "cabin_runner.jpg"),
            g,
        ),
    ))
    jcam, jaim, jlens = _JUNCTION_CAM
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_junction.jpg" % tag)
        shots.append((
            "%s_junction" % tag,
            lambda p=path: _prop_many(found, _junction_specs(), jcam, jaim, jlens, p, _JUNCTION_GROUND),
        ))
    shots.append((
        "junction_runner",
        lambda: _runner(
            found, _junction_specs(),
            (3.6, 0.27, 10.4), 190.0,
            (4.4, 1.55, 12.4), (0.1, 0.16, 9.2), 40.0,
            os.path.join(STILL_DIR, "junction_runner.jpg"),
            _JUNCTION_GROUND,
        ),
    ))
    cam, aim, lens, gnd = _DOCK
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_dock.jpg" % tag)
        shots.append((
            "%s_dock" % tag,
            lambda p=path, c=cam, a=aim, ln=lens, g=gnd: _one(
                found, "Dock_Straight", 0.0, c, a, ln, p, g
            ),
        ))
    shots.append((
        "dock_runner",
        lambda c=cam, a=aim, g=gnd: _runner(
            found, [("Dock_Straight", (0.0, 0.0, 0.0), 0.0, 1.0)],
            (0.15, 0.62, 0.35), 40.0,
            (-3.4, 0.95, 4.2), (0.15, 0.28, 0.1), 30.0,
            os.path.join(STILL_DIR, "dock_runner.jpg"),
            g,
            water=True,
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
        print("SHOT", name, flush=True)
        fn()
    print("PASS22_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
