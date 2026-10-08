"""Pass 26 stills. JPEG quality 90, 1280x720, under 400 KB. No palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass26.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass22 as p22  # noqa: E402
import render_pass24 as p24  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass26")

# Same market-corner framing as pass 25, so the seam and the returns can be compared.
_STORE = (0.0, (6.8, 2.6, 7.4), (0.4, 2.4, 0.2), 28.0, (0.34, 0.36, 0.32))
# Deck-level side elevation from pass 24. Do not move it.
_DOCK_SIDE = p24._DOCK_SIDE
# Far enough and high enough that the parapet and the fire escape both stay in frame.
_WALK = (-28.0, (-10.8, 6.6, 17.8), (-0.2, 4.6, 0.3), 32.0, (0.34, 0.36, 0.32))


def _shots(found):
    shots = []
    yaw, eye, aim, lens, ground = _STORE
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_store.jpg" % tag)
        shots.append((
            "%s_store" % tag,
            lambda p=path, yaw=yaw, eye=eye, aim=aim, lens=lens, ground=ground: p22._one(
                found, "Store_Corner", yaw, eye, aim, lens, p, ground
            ),
        ))
    eye, aim, lens = _DOCK_SIDE
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_dock_side.jpg" % tag)
        shots.append((
            "%s_dock_side" % tag,
            lambda p=path, eye=eye, aim=aim, lens=lens: p22._one(
                found, "Dock_Straight", 0.0, eye, aim, lens, p, (0.16, 0.14, 0.10)
            ),
        ))
    yaw, eye, aim, lens, ground = _WALK
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_walkup.jpg" % tag)
        shots.append((
            "%s_walkup" % tag,
            lambda p=path, yaw=yaw, eye=eye, aim=aim, lens=lens, ground=ground: p22._one(
                found, "WalkUp", yaw, eye, aim, lens, p, ground
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
    print("PASS26_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
