"""Pass 21 stills. JPEG at quality 90, no palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass21.py
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

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass21")
HIER_FBX = os.path.join(
    r._common.REPO, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"
)

# Pulled back so the chimney top stays in frame with the door.
_CABIN = (32.0, (6.4, 2.8, 6.8), (0.2, 1.9, 0.2), 26.0, (0.34, 0.36, 0.32))
# Whole crossing, one straight tile past each arm, sidewalks out to about ±16 m.
_JUNCTION_CAM = ((30.0, 22.0, 32.0), (0.0, 0.25, 0.0), 28.0)
_JUNCTION_GROUND = (0.16, 0.18, 0.14)
_ROAD_AT = 13.0

_DOCK = ((4.2, 1.45, 4.5), (0.0, 0.35, 0.0), 28.0, (0.22, 0.28, 0.26))
_MAIL = ((1.8, 1.15, 1.9), (0.0, 0.95, 0.0), 42.0, (0.32, 0.36, 0.28))
_PICKET = ((2.6, 1.35, 2.8), (0.0, 0.55, 0.0), 34.0, (0.32, 0.38, 0.26))
_HYDRANT = ((1.15, 0.85, 1.25), (0.0, 0.42, 0.0), 48.0, (0.30, 0.32, 0.28))


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


def _runner(found, specs, hier_pos, hier_yaw, cam, aim, lens, path, ground):
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    _place_hier(hier_pos, hier_yaw)
    if path.endswith("dock_runner.jpg"):
        _dock_water()
    else:
        p6._ground(ground)
    p6._look(scene, cam, aim, lens=lens)
    _jpg(scene, path)


def _dock_water():
    p6._ground((0.16, 0.14, 0.10), y=-1.35, size=40.0)
    p6._sheet(0.0, -0.02, 0.0, 40.0, 40.0, p6._mat("Pass21Water", (0.05, 0.10, 0.12), 0.18, transmission=0.35))


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
        _dock_still(found, yaw, cam, aim, lens, path)
        return
    _prop_many(found, [(name, (0.0, 0.0, 0.0), yaw, 1.0)], cam, aim, lens, path, ground)


def _dock_still(found, yaw, cam, aim, lens, path):
    """Mud under the piles, dark water at the surface, so the pilings read."""
    scene = p6._begin(wide=True)
    p6._place(found, [("Dock_Straight", (0.0, 0.0, 0.0), yaw, 1.0)])
    _dock_water()
    p6._look(scene, cam, aim, lens=lens)
    _jpg(scene, path)


def _shots(found):
    shots = []
    yaw, cam, aim, lens, ground = _CABIN
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_cabin.jpg" % tag)
        shots.append((
            "%s_cabin" % tag,
            lambda p=path: _one(found, "Cabin", yaw, cam, aim, lens, p, ground),
        ))
    shots.append((
        "cabin_runner",
        lambda: _runner(
            found, [("Cabin", (0.0, 0.0, 0.0), 18.0, 1.0)],
            (1.8, 0.0, 3.5), 200.0,
            (5.6, 2.3, 6.0), (0.3, 1.7, 0.8), 28.0,
            os.path.join(STILL_DIR, "cabin_runner.jpg"),
            ground,
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
            (4.4, 0.27, 9.2), 200.0,
            (7.5, 2.6, 7.0), (0.3, 0.35, 12.5), 32.0,
            os.path.join(STILL_DIR, "junction_runner.jpg"),
            _JUNCTION_GROUND,
        ),
    ))
    for key, name, spec in (
        ("dock", "Dock_Straight", _DOCK),
        ("mailbox", "Mailbox", _MAIL),
        ("picket", "PicketFence", _PICKET),
        ("hydrant", "FireHydrant", _HYDRANT),
    ):
        cam, aim, lens, gnd = spec
        for tag in ("before", "after"):
            path = os.path.join(STILL_DIR, "%s_%s.jpg" % (tag, key))
            shots.append((
                "%s_%s" % (tag, key),
                lambda n=name, c=cam, a=aim, ln=lens, p=path, g=gnd: _one(found, n, 28.0, c, a, ln, p, g),
            ))
        shots.append((
            "%s_runner" % key,
            lambda n=name, c=cam, a=aim, ln=lens, g=gnd, k=key: _runner(
                found, [(n, (0.0, 0.0, 0.0), 18.0, 1.0)],
                _runner_pos(k), _runner_yaw(k),
                _runner_cam(k), _runner_aim(k), _runner_lens(k),
                os.path.join(STILL_DIR, "%s_runner.jpg" % k),
                g,
            ),
        ))
    return shots


def _runner_pos(key):
    return {
        "dock": (0.3, 0.62, 0.4),
        "mailbox": (0.7, 0.0, 0.9),
        "picket": (0.2, 0.0, 1.4),
        "hydrant": (0.7, 0.0, 0.8),
    }[key]


def _runner_yaw(key):
    return {"dock": 30.0, "mailbox": 210.0, "picket": 200.0, "hydrant": 220.0}[key]


def _runner_cam(key):
    return {
        "dock": (3.6, 1.35, 3.8),
        "mailbox": (2.2, 1.35, 2.0),
        "picket": (2.8, 1.4, 2.2),
        "hydrant": (1.6, 1.05, 1.5),
    }[key]


def _runner_aim(key):
    return {
        "dock": (0.0, 0.40, 0.0),
        "mailbox": (0.0, 1.0, 0.0),
        "picket": (0.0, 0.6, 0.2),
        "hydrant": (0.0, 0.45, 0.0),
    }[key]


def _runner_lens(key):
    return {"dock": 32.0, "mailbox": 38.0, "picket": 32.0, "hydrant": 42.0}[key]


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
    print("PASS21_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
