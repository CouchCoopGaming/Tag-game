"""Pass 25 stills. JPEG quality 90, 1280x720, under 200 KB. No palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass25.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass22 as p22  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass25")

# Corner shop, both glazed faces, cornice and upper windows in frame.
_STORE = (0.0, (6.8, 2.6, 7.4), (0.4, 2.4, 0.2), 28.0, (0.34, 0.36, 0.32))
# Diner front, close enough that the storefront and the cornice read.
_DINER = (18.0, (7.6, 2.3, 8.6), (0.3, 2.1, 0.1), 28.0, (0.34, 0.36, 0.32))
# Walk-up street corner: reveals, stoop, and the projecting cornice.
_WALK = (16.0, (7.2, 3.6, 13.5), (0.2, 4.5, 0.4), 26.0, (0.34, 0.36, 0.32))
# Raised so the street corona and the window reveal are both in frame.
_BRICK = (20.0, (3.2, 2.15, 4.4), (0.0, 1.85, 0.05), 32.0, (0.34, 0.36, 0.32))


def _shots(found):
    shots = []
    specs = (
        ("store", "Store_Corner", _STORE),
        ("diner", "Store_Diner", _DINER),
        ("walkup", "WalkUp", _WALK),
        ("brick", "Brick_Wall", _BRICK),
    )
    for name, asset, cam in specs:
        yaw, eye, aim, lens, ground = cam
        for tag in ("before", "after"):
            path = os.path.join(STILL_DIR, "%s_%s.jpg" % (tag, name))
            shots.append((
                "%s_%s" % (tag, name),
                lambda p=path, asset=asset, yaw=yaw, eye=eye, aim=aim, lens=lens, ground=ground: p22._one(
                    found, asset, yaw, eye, aim, lens, p, ground
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
    print("PASS25_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
