"""Pass 7 stills. Harbor waterline, asphalt, playground, gazebo, and prop turntables.

  blender --background --python Tools/Blender/AssetLibrary/render_pass7.py
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass2 as r  # noqa: E402
import render_pass6 as p6  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass7")
# Hull runs about y=0.03 to y=1.04. Water at 0.40 is ~37% of that height.
WATER_Y = 0.40


def _harbor(found):
    scene = p6._begin(wide=True)
    quay_bollard = (2.4, 1.15, -0.42)
    boat_pos = (3.25, 0.0, -4.85)
    boat_yaw = -8.0
    dock_pos = (0.2, 0.0, -4.9)
    specs = [
        ("Quay_Edge", (0, 0, 0), 0),
        ("Container_20", (3.6, 0.90, 2.55), 90),
        ("HarborCrane", (-5.4, 0.90, 3.3), 180),
        ("Dock_Straight", dock_pos, 0),
        ("Piling", (-1.5, 0, -4.4), 0),
        ("Piling", (1.7, 0, -7.2), 0),
        ("Boat", boat_pos, boat_yaw),
        ("Buoy", (6.8, 0.0, -8.4), 0),
        ("Mannequin", (-1.4, 0.90, 1.6), 210),
    ]
    objs = p6._place(found, specs)
    for local, cleat in (
        ((-0.52, 0.97, -1.45), (1.35, 0.70, dock_pos[2] - 1.55)),
        ((-0.52, 0.97, 1.70), (1.35, 0.70, dock_pos[2] + 1.55)),
    ):
        p6._rope(p6._yaw_point(boat_pos, boat_yaw, local), cleat)
    p6._rope(p6._yaw_point(boat_pos, boat_yaw, (-0.52, 0.97, -1.45)), quay_bollard)
    # Under the sheet only. The visible foreground is the water, not this plane.
    p6._ground((0.03, 0.07, 0.09), y=-0.12)
    water = p6._mat("Pass7Water", (0.02, 0.32, 0.42), 0.05, metal=0.22, transmission=0.08)
    # From in front of the camera, across the quay face, to the far bank.
    p6._sheet(0.0, WATER_Y, -15.0, 180.0, 160.0, water)
    shore = p6._mat("Pass7Shore", (0.42, 0.44, 0.38), 0.94)
    p6._sheet(0.0, WATER_Y + 0.03, 64.0, 160.0, 52.0, shore)
    p6._skyline()
    r._frame(scene, objs, fill=0.76, elevation=16.0, azimuth=205.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_harbor.png"))


def _street(found):
    scene = p6._begin(wide=True)
    specs = list(p6._snap.street_hardscape())
    for name, pos, yaw in p6._snap.street_shops():
        specs.append((name, pos, yaw, 1.0))
    props = [
        ("LightPost_Single", (4.15, p6.WALK_Y, -10.0), -90),
        ("LightPost_Single", (4.15, p6.WALK_Y, 1.0), -90),
        ("LightPost_Single", (4.15, p6.WALK_Y, 11.5), -90),
        ("FireHydrant", (3.85, p6.WALK_Y, -4.2), 10),
        ("FireHydrant", (3.85, p6.WALK_Y, 6.8), -8),
        ("Bench_Wood", (4.35, p6.WALK_Y, -8.6), -90),
        ("Bench_Wood", (4.35, p6.WALK_Y, 5.2), -90),
        ("Bench_Wood", (4.35, p6.WALK_Y, 15.0), -90),
        ("TrashCan_Slat", (4.95, p6.WALK_Y, -14.2), 0),
        ("TrashCan_Lidded", (4.95, p6.WALK_Y, 9.4), 15),
        ("TrashCan_Slat", (4.95, p6.WALK_Y, 16.4), 0),
        ("Mannequin", (3.9, p6.WALK_Y, -1.5), 200),
        ("Tree", (-8.4, 0, -9.0), 0),
        ("Tree_Maple", (-8.2, 0, 7.5), 18),
    ]
    specs.extend((name, pos, yaw, 1.0) for name, pos, yaw in props)
    objs = p6._place(found, specs)
    p6._ground((0.40, 0.44, 0.38))
    p6._look(scene, (1.5, 3.4, -17.2), (7.2, 1.6, 9.0), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_street.png"))


def _road(found):
    scene = p6._begin(wide=True)
    specs = []
    for z in (-4.0, 0.0, 4.0):
        specs.append(("Road_Straight", (0.0, 0.0, z), 0.0))
        specs.append(("Gutter", (3.19, 0.0, z), 0.0))
        specs.append(("Sidewalk", (4.38, 0.0, z), 0.0))
        specs.append(("Gutter", (-3.19, 0.0, z), 180.0))
        specs.append(("Sidewalk", (-4.38, 0.0, z), 180.0))
    p6._place(found, specs)
    p6._ground((0.40, 0.44, 0.38))
    p6._look(scene, (0.6, 1.45, -4.8), (2.55, 0.10, 1.6), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "road_curb.png"))


def _turntable(found, name, path, yaw=28.0):
    scene = p6._begin(wide=True)
    obj = p6._spawn(found, name, (0, 0, 0), yaw)
    p6._ground((0.55, 0.56, 0.54))
    r._frame(scene, [obj], fill=0.72, elevation=18.0, azimuth=38.0)
    r._render(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
    found = r._catalog()
    shots = [
        ("harbor", lambda: _harbor(found)),
        ("street", lambda: _street(found)),
        ("road", lambda: _road(found)),
        ("playground", lambda: _turntable(found, "Playground", os.path.join(STILL_DIR, "playground.png"), 32)),
        ("gazebo", lambda: _turntable(found, "Gazebo", os.path.join(STILL_DIR, "gazebo.png"), -52)),
        ("seesaw", lambda: _turntable(found, "Seesaw", os.path.join(STILL_DIR, "seesaw.png"))),
        ("light_post_single", lambda: _turntable(found, "LightPost_Single", os.path.join(STILL_DIR, "light_post_single.png"), 40)),
        ("light_post_double", lambda: _turntable(found, "LightPost_Double", os.path.join(STILL_DIR, "light_post_double.png"), 40)),
        ("rooftop_ac", lambda: _turntable(found, "RooftopAC", os.path.join(STILL_DIR, "rooftop_ac.png"))),
        ("wall_ac", lambda: _turntable(found, "WallAC", os.path.join(STILL_DIR, "wall_ac.png"), 50)),
        ("trash_can_slat", lambda: _turntable(found, "TrashCan_Slat", os.path.join(STILL_DIR, "trash_can_slat.png"))),
        ("trash_can_lidded", lambda: _turntable(found, "TrashCan_Lidded", os.path.join(STILL_DIR, "trash_can_lidded.png"))),
        ("bench", lambda: _turntable(found, "Bench_Wood", os.path.join(STILL_DIR, "bench_wood.png"), 35)),
        ("median", lambda: _turntable(found, "Median_Planter", os.path.join(STILL_DIR, "median_planter.png"), 24)),
        ("cabin", lambda: _turntable(found, "Cabin", os.path.join(STILL_DIR, "cabin.png"), 28)),
        ("dock", lambda: _turntable(found, "Dock_Straight", os.path.join(STILL_DIR, "dock_straight.png"), 30)),
    ]
    for name, fn in shots:
        if only and only not in name:
            continue
        fn()
    print("PASS7_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
