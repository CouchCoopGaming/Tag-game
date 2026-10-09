"""Pass 23 stills. JPEG quality 90, 1280x720, under 200 KB. No palette cut.

  blender --background --python Tools/Blender/AssetLibrary/render_pass23.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass22 as p22  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass23")


def _shots(found):
    shots = []
    yaw, cam, aim, lens, ground = p22._CABIN
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_cabin.jpg" % tag)
        shots.append((
            "%s_cabin" % tag,
            lambda p=path, yaw=yaw, cam=cam, aim=aim, lens=lens, ground=ground: p22._one(
                found, "Cabin", yaw, cam, aim, lens, p, ground
            ),
        ))
    shots.append((
        "cabin_runner",
        lambda g=ground: p22._runner(
            found, [("Cabin", (0.0, 0.0, 0.0), 18.0, 1.0)],
            (1.8, 0.0, 3.5), 200.0,
            (5.6, 2.3, 6.0), (0.3, 1.7, 0.8), 28.0,
            os.path.join(STILL_DIR, "cabin_runner.jpg"),
            g,
        ),
    ))
    cam, aim, lens, gnd = p22._DOCK
    for tag in ("before", "after"):
        path = os.path.join(STILL_DIR, "%s_dock.jpg" % tag)
        shots.append((
            "%s_dock" % tag,
            lambda p=path, c=cam, a=aim, ln=lens, g=gnd: p22._one(
                found, "Dock_Straight", 0.0, c, a, ln, p, g
            ),
        ))
    shots.append((
        "dock_runner",
        lambda g=gnd: p22._runner(
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
    found = p22.r._catalog()
    for name, fn in _shots(found):
        if only and not any(name.startswith(part) for part in only):
            continue
        print("SHOT", name, flush=True)
        fn()
        print("STILL", os.path.join(STILL_DIR, name + ".jpg"), flush=True)
    print("PASS23_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
