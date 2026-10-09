"""Pass 29 stills. JPEG quality 90, 1280x720, under 400 KB. No palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass29.py
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass22 as p22  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass29")

# Same ranch quarter as pass 28, so the rail, header, and roof can be compared.
_RANCH = (18.0, (11.6, 3.4, 13.2), (0.4, 1.7, 0.6), 28.0, (0.34, 0.36, 0.32))
_RANCH_FIGURE = (-1.6, 0.0, 6.6)
# Water side, slip and gable in frame.
_BOAT = (22.0, (8.6, 2.6, 12.2), (0.2, 1.7, 0.2), 28.0)
_BOAT_FIGURE = (1.5, 0.0, 6.4)
# Before still is the old 2 m bay, from the front. Do not retake it.
_FENCE = (16.0, (6.4, 1.7, 7.2), (1.4, 0.9, 0.2), 32.0, (0.34, 0.36, 0.32))
# After still looks at the back of the kit so the rails, brace, and dog-ears read.
_FENCE_AFTER = (32.0, (1.4, 1.85, -6.6), (1.6, 0.95, 0.5), 26.0, (0.34, 0.36, 0.32))
_FENCE_FIGURE = (1.15, 0.0, -2.05)
_GROUND = (0.34, 0.36, 0.32)


def _boat(found, yaw, path, figure):
    scene = p6._begin(wide=True)
    p6._place(found, [("Boathouse", (0.0, 0.0, 0.0), yaw, 1.0)])
    if figure:
        p22._place_hier(_BOAT_FIGURE, 200.0)
    p22._dock_water()
    p6._look(scene, _BOAT[1], _BOAT[2], lens=_BOAT[3])
    p22._jpg(scene, path)


def _shots(found):
    shots = []
    yaw, eye, aim, lens, ground = _RANCH
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_ranch_quarter.jpg" % tag)
        shots.append((
            "%s_ranch_quarter" % tag,
            lambda p=path, yaw=yaw, eye=eye, aim=aim, lens=lens, ground=ground: p22._one(
                found, "Ranch_House", yaw, eye, aim, lens, p, ground
            ),
        ))
    path = os.path.join(STILL_DIR, "ranch_scale.jpg")
    shots.append((
        "ranch_scale",
        lambda p=path, yaw=yaw, eye=eye, aim=aim, lens=lens, ground=ground: p22._runner(
            found, [("Ranch_House", (0.0, 0.0, 0.0), yaw, 1.0)],
            _RANCH_FIGURE, 160.0, eye, aim, lens, p, ground
        ),
    ))
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_boathouse.jpg" % tag)
        shots.append((
            "%s_boathouse" % tag,
            lambda p=path: _boat(found, _BOAT[0], p, False),
        ))
    path = os.path.join(STILL_DIR, "boathouse_scale.jpg")
    shots.append(("boathouse_scale", lambda p=path: _boat(found, _BOAT[0], p, True)))
    yaw, eye, aim, lens, ground = _FENCE
    path = os.path.join(STILL_DIR, "before_fence.jpg")
    shots.append((
        "before_fence",
        lambda p=path, yaw=yaw, eye=eye, aim=aim, lens=lens, ground=ground: p22._one(
            found, "WoodFence", yaw, eye, aim, lens, p, ground
        ),
    ))
    specs = [
        ("WoodFence_Corner", (-2.4, 0.0, 0.0), 0.0, 1.0),
        ("WoodFence", (0.0, 0.0, 0.0), 0.0, 1.0),
        ("WoodFence_Gate", (2.70, 0.0, 0.0), 0.0, 1.0),
        ("WoodFence_End", (4.30, 0.0, 0.0), 0.0, 1.0),
    ]
    yaw_a, eye_a, aim_a, lens_a, ground_a = _FENCE_AFTER
    path = os.path.join(STILL_DIR, "after_fence.jpg")
    shots.append((
        "after_fence",
        lambda p=path: p22._prop_many(
            found, specs, eye_a, aim_a, lens_a, p, ground_a
        ),
    ))
    path = os.path.join(STILL_DIR, "fence_scale.jpg")
    shots.append((
        "fence_scale",
        lambda p=path: p22._runner(
            found, specs, _FENCE_FIGURE, 200.0, eye_a, aim_a, lens_a, p, ground_a
        ),
    ))
    return shots


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = p22.r._catalog()
    for name, fn in _shots(found):
        if only and not any(name.startswith(part) for part in only):
            continue
        print("SHOT", name, flush=True)
        fn()
        print("STILL", os.path.join(STILL_DIR, name + ".jpg"), flush=True)
    print("PASS29_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
