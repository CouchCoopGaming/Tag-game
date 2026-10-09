"""Pass 30 stills for the meshes this pass repairs.

Quarter, side, close-up, and a 1.8 m figure. JPEG quality 90, 1280x720.
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass22 as p22  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass30")
GROUND = (0.34, 0.36, 0.32)

# name, yaw, quarter eye, aim, lens, side eye, close eye, close aim, close lens,
# figure xyz, figure yaw, water
SHOTS = [
    ("WalkUp", 24.0, (12.0, 6.2, 16.0), (0.2, 4.2, 0.0), 26.0,
     (0.2, 4.6, 18.5), (2.4, 9.7, 1.6), (1.2, 9.5, -0.6), 48.0,
     (-3.4, 0.0, 6.8), 150.0, False),
    ("Cabin", 28.0, (7.2, 2.8, 8.4), (0.2, 1.6, 0.4), 32.0,
     (0.2, 1.8, 9.2), (1.6, 3.4, 2.4), (0.7, 3.6, 0.0), 45.0,
     (-2.2, 0.0, 4.6), 160.0, False),
    ("Gazebo", 18.0, (6.4, 2.4, 6.8), (0.0, 1.6, 0.0), 32.0,
     (0.0, 1.7, 7.6), (1.6, 2.2, 2.2), (0.4, 1.5, 0.6), 42.0,
     (-2.4, 0.0, 3.6), 140.0, False),
    ("Container_20", 32.0, (8.0, 2.4, 9.0), (0.0, 1.3, 0.4), 32.0,
     (0.0, 1.6, 10.5), (1.2, 1.6, 4.2), (0.4, 1.4, 2.9), 42.0,
     (-2.6, 0.0, 4.8), 150.0, False),
    ("Ranch_House", 18.0, (11.6, 3.4, 13.2), (0.4, 1.7, 0.6), 28.0,
     (0.4, 2.0, 16.0), (4.2, 1.6, 5.4), (3.5, 1.2, 3.6), 40.0,
     (-1.6, 0.0, 6.6), 160.0, False),
    ("Boathouse", 22.0, (8.6, 2.6, 12.2), (0.2, 1.7, 0.2), 28.0,
     (0.2, 2.0, 14.0), (2.2, 1.4, 6.2), (0.2, 1.2, 3.2), 36.0,
     (1.5, 0.0, 6.4), 200.0, True),
    ("Dock_Straight", 20.0, (8.0, 2.2, 8.5), (0.0, 0.5, 0.0), 32.0,
     (0.0, 1.4, 9.0), (1.6, 1.1, 2.2), (0.2, 0.62, 0.4), 40.0,
     (-2.2, 0.0, 4.2), 140.0, True),
    ("FishingBoat", 30.0, (6.5, 2.2, 7.5), (0.0, 0.8, 0.0), 34.0,
     (0.0, 1.3, 8.0), (1.8, 1.3, 1.2), (0.2, 1.1, -0.4), 40.0,
     (-2.4, 0.0, 3.8), 150.0, True),
    ("GasCanopy", 26.0, (14.0, 4.5, 16.0), (0.0, 2.0, -1.0), 24.0,
     (0.0, 2.4, 18.0), (2.4, 1.4, 2.2), (1.6, 0.9, 0.1), 40.0,
     (-4.6, 0.0, 5.5), 150.0, False),
    ("Road_Junction", 20.0, (18.0, 6.0, 22.0), (0.0, 0.2, 0.0), 24.0,
     (0.0, 3.0, 26.0), (4.0, 1.6, 6.0), (1.5, 0.15, 2.0), 36.0,
     (-3.5, 0.0, 8.0), 160.0, False),
    ("CourtFence", 24.0, (16.0, 4.0, 20.0), (0.0, 1.4, 0.0), 24.0,
     (0.0, 2.0, 24.0), (9.2, 1.4, 2.4), (7.95, 1.0, 0.0), 36.0,
     (-6.0, 0.0, 8.0), 140.0, False),
    ("HarborShed", 28.0, (7.0, 2.6, 8.0), (0.0, 1.4, 0.2), 32.0,
     (0.0, 1.6, 8.6), (1.4, 1.5, 3.2), (0.2, 1.2, 1.5), 40.0,
     (-2.4, 0.0, 4.2), 150.0, False),
    ("Rowboat", 35.0, (4.2, 1.8, 4.6), (0.0, 0.4, 0.0), 40.0,
     (0.0, 1.2, 5.2), (1.4, 0.9, 0.8), (0.3, 0.45, 0.1), 48.0,
     (-1.8, 0.0, 2.6), 140.0, True),
    ("WoodFence", 32.0, (3.4, 1.6, 4.6), (1.2, 0.9, 0.0), 32.0,
     (1.2, 1.2, 5.2), (1.4, 1.5, 1.6), (0.8, 1.6, 0.0), 42.0,
     (-1.2, 0.0, 2.4), 160.0, False),
    ("WoodFence_Gate", 28.0, (2.6, 1.5, 3.6), (0.6, 0.9, 0.0), 36.0,
     (0.6, 1.2, 4.0), (1.0, 1.3, 1.4), (0.6, 1.1, 0.0), 42.0,
     (-1.2, 0.0, 2.2), 150.0, False),
    ("WoodFence_Corner", 40.0, (3.2, 1.6, 3.4), (0.5, 0.9, 0.5), 34.0,
     (0.6, 1.2, 4.2), (1.2, 1.4, 1.2), (0.4, 1.5, 0.3), 42.0,
     (-1.4, 0.0, 2.2), 140.0, False),
    ("WoodFence_End", 24.0, (2.2, 1.5, 3.2), (0.3, 0.9, 0.0), 38.0,
     (0.3, 1.2, 3.6), (0.8, 1.4, 1.2), (0.3, 1.5, 0.0), 45.0,
     (-1.1, 0.0, 2.0), 150.0, False),
]


def _stem(name):
    return "_".join(part.lower() for part in name.split("_"))


def _shot(found, name, yaw, eye, aim, lens, path, ground, water, figure, figure_yaw):
    specs = [(name, (0.0, 0.0, 0.0), yaw, 1.0)]
    if figure is not None:
        p22._runner(found, specs, figure, figure_yaw, eye, aim, lens, path, ground, water=water)
        return
    scene = p6._begin(wide=True)
    p6._place(found, specs)
    if water:
        p22._dock_water()
    else:
        p6._ground(ground)
    p6._look(scene, eye, aim, lens=lens)
    p22._jpg(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = p22.r._catalog()
    for row in SHOTS:
        name = row[0]
        stem = _stem(name)
        if only and not any(stem.startswith(part) or name.startswith(part) for part in only):
            continue
        yaw, eye, aim, lens = row[1], row[2], row[3], row[4]
        side, close, close_aim, close_lens = row[5], row[6], row[7], row[8]
        figure, figure_yaw, water = row[9], row[10], row[11]
        jobs = (
            ("quarter", eye, aim, lens, None, 0.0),
            ("side", side, aim, lens, None, 0.0),
            ("close", close, close_aim, close_lens, None, 0.0),
            ("scale", eye, aim, lens, figure, figure_yaw),
        )
        for role, cam, look, use_lens, fig, fig_yaw in jobs:
            path = os.path.join(STILL_DIR, "%s_%s.jpg" % (stem, role))
            print("SHOT", stem, role, flush=True)
            _shot(found, name, yaw, cam, look, use_lens, path, GROUND, water, fig, fig_yaw)
            print("STILL", path, flush=True)
    print("PASS30_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
