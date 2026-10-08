"""Pass 24 stills. JPEG quality 90, 1280x720, under 200 KB. No palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass24.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass22 as p22  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass24")

# Hero cabin, same framing as pass 23, so the ridge and the shakes can be compared.
_CABIN = p22._CABIN
# The oblique dock view the pass 23 still used.
_DOCK = p22._DOCK
# Deck-level side elevation, perpendicular to the long side, about 1 m above the water.
_DOCK_SIDE = ((-8.2, 0.98, 0.0), (0.0, 0.42, 0.0), 34.0)


def _shots(found):
    shots = []
    yaw, cam, aim, lens, ground = _CABIN
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_cabin.jpg" % tag)
        shots.append((
            "%s_cabin" % tag,
            lambda p=path, yaw=yaw, cam=cam, aim=aim, lens=lens, ground=ground: p22._one(
                found, "Cabin", yaw, cam, aim, lens, p, ground
            ),
        ))
    cam, aim, lens, gnd = _DOCK
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_dock.jpg" % tag)
        shots.append((
            "%s_dock" % tag,
            lambda p=path, c=cam, a=aim, ln=lens, g=gnd: p22._one(
                found, "Dock_Straight", 0.0, c, a, ln, p, g
            ),
        ))
    c, a, ln = _DOCK_SIDE
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_dock_side.jpg" % tag)
        shots.append((
            "%s_dock_side" % tag,
            lambda p=path, c=c, a=a, ln=ln: p22._one(
                found, "Dock_Straight", 0.0, c, a, ln, p, True
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
    print("PASS24_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
