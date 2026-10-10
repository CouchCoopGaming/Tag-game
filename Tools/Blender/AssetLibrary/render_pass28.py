"""Pass 28 stills. JPEG quality 90, 1280x720, under 400 KB. No palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass28.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass22 as p22  # noqa: E402
import render_pass24 as p24  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass28")

# Same side elevation as pass 24, so the plank ends can be compared.
_DOCK_SIDE = p24._DOCK_SIDE
# Same three-quarter as pass 27, so the parapet cap can be compared.
_WALK_Q = (-12.0, (-7.4, 6.2, 17.4), (0.3, 4.6, 0.6), 26.0, (0.34, 0.36, 0.32))
# Front and garage side. The porch and the hip stay in frame.
_RANCH = (18.0, (11.6, 3.4, 13.2), (0.4, 1.7, 0.6), 28.0, (0.34, 0.36, 0.32))
# 1.8 m figure on the walk in front of the porch, clear of the door.
_RANCH_FIGURE = (-1.6, 0.0, 6.6)


def _shots(found):
    shots = []
    eye, aim, lens = _DOCK_SIDE
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_dock_side.jpg" % tag)
        shots.append((
            "%s_dock_side" % tag,
            lambda p=path, eye=eye, aim=aim, lens=lens: p22._one(
                found, "Dock_Straight", 0.0, eye, aim, lens, p, (0.16, 0.14, 0.10)
            ),
        ))
    yaw, eye, aim, lens, ground = _WALK_Q
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_walkup_quarter.jpg" % tag)
        shots.append((
            "%s_walkup_quarter" % tag,
            lambda p=path, yaw=yaw, eye=eye, aim=aim, lens=lens, ground=ground: p22._one(
                found, "WalkUp", yaw, eye, aim, lens, p, ground
            ),
        ))
    yaw, eye, aim, lens, ground = _RANCH
    path = os.path.join(STILL_DIR, "ranch.jpg")
    shots.append((
        "ranch",
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
    print("PASS28_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
