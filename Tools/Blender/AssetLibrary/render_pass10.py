"""Pass 10 stills. Hoops over the keys, a longer street, suburb and park.

  blender --background --python Tools/Blender/AssetLibrary/render_pass10.py
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass9 as p9  # noqa: E402
import render_pass2 as r  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass10")


def _court(found):
    scene = p6._begin(wide=True)
    p6._spawn(found, "Court", (0, 0, 0), 0)
    # Pole is 1.2 m behind the slab end (z = ±11). Rim then lands on z = ±9.7125.
    p6._spawn(found, "Hoop", (0, 0, -12.2), 0)
    p6._spawn(found, "Hoop", (0, 0, 12.2), 180)
    p6._ground((0.15, 0.16, 0.15))
    bg = scene.world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.28, 0.38, 0.50, 1.0)
        bg.inputs["Strength"].default_value = 0.9
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 90.0
    # Raised three-quarter from inside the sideline, so both backboard faces read over the keys.
    p6._look(scene, (14.0, 8.0, 0.0), (0.0, 1.5, 0.0), lens=16.0)
    r._render(scene, os.path.join(STILL_DIR, "court.png"))


def _street_specs():
    specs = []
    for item in p6._snap.street_hardscape():
        name, pos, yaw, scale = item
        if name == "Road_Straight" and abs(pos[0]) < 0.01 and abs(pos[2] - 16.0) < 0.01:
            name = "Road_Crosswalk"
        specs.append((name, pos, yaw, scale))
    for name, pos, yaw in p6._snap.street_shops():
        specs.append((name, pos, yaw, 1.0))
    specs.append(("Road_Cross", (0.0, 0.0, 21.0), 0.0, 1.0))
    specs.append(("Road_Crosswalk", (5.0, 0.0, 21.0), 90.0, 1.0))
    specs.append(("Road_Straight", (9.0, 0.0, 21.0), 90.0, 1.0))
    specs.append(("Road_Crosswalk", (-5.0, 0.0, 21.0), 90.0, 1.0))
    specs.append(("Road_Straight", (-9.0, 0.0, 21.0), 90.0, 1.0))
    for z in (26.0, 30.0, 34.0, 38.0, 42.0, 46.0):
        road = "Road_Crosswalk" if abs(z - 26.0) < 0.01 else "Road_Straight"
        specs.append((road, (0.0, 0.0, z), 0.0, 1.0))
        specs.append(("Gutter", (3.19, 0.0, z), 0.0, 1.0))
        specs.append(("Sidewalk", (4.38, 0.0, z), 0.0, 1.0))
        specs.append(("Gutter", (-3.19, 0.0, z), 180.0, 1.0))
        specs.append(("Sidewalk", (-4.38, 0.0, z), 180.0, 1.0))
    for x in (7.38, 11.38, -7.38):
        specs.append(("Sidewalk", (x, 0.0, 25.0), -90.0, 1.0))
    y = p6.WALK_Y
    props = [
        ("LightPost_Single", (4.15, y, -10.0), -90),
        ("LightPost_Single", (4.15, y, 1.0), -90),
        ("LightPost_Single", (4.15, y, 11.5), -90),
        ("LightPost_Single", (4.15, y, 28.5), -90),
        ("FireHydrant", (3.85, y, -4.2), 10),
        ("FireHydrant", (3.85, y, 6.8), -8),
        ("Bench_Wood", (4.35, y, -8.6), -90),
        ("Bench_Wood", (4.35, y, 5.2), -90),
        ("Bench_Wood", (4.35, y, 15.0), -90),
        ("TrashCan_Slat", (4.95, y, -14.2), 0),
        ("TrashCan_Lidded", (4.95, y, 9.4), 15),
        ("RecyclingBin", (4.70, y, 3.1), 8),
        ("NewspaperBox", (4.85, y, -2.2), -90),
        ("ParkingMeter", (3.62, y, 2.2), 90),
        ("TrafficLight", (4.20, y, 13.6), -90),
        ("TrafficLight", (4.50, y, 25.2), -90),
        ("StopBar", (0.0, 0.12, 13.55), 0),
        ("Tree_Grate", (-4.38, 0.0, -11.0), 0),
        ("Tree_Grate", (-4.38, 0.0, 6.5), 18),
        ("Store_Corner", (9.38, 0.0, 29.6), 180),
        ("WalkUp", (-9.70, 0.0, 30.4), 90),
        ("GasCanopy", (11.2, 0.0, 42.0), 180),
    ]
    specs.extend((name, pos, yaw, 1.0) for name, pos, yaw in props)
    return specs


def _street(found):
    scene = p6._begin(wide=True)
    p6._place(found, _street_specs())
    p9._street_brick()
    p6._ground((0.40, 0.44, 0.38))
    p9._place_hier((3.9, p6.WALK_Y, -1.5), 200)
    # Foreground shops stay in frame. The aim is far enough to take in the next intersection.
    p6._look(scene, (2.0, 5.5, -20.0), (0.5, 1.2, 28.0), lens=20.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_street.png"))


def _suburb(found):
    scene = p6._begin(wide=True)
    specs = [
        ("House_Gable", (0.0, 0.0, 0.0), 28),
        ("Cabin", (-9.0, 0.0, -0.4), 18),
        ("Cabin", (8.8, 0.0, 0.6), -12),
        ("Tree_Maple", (-4.2, 0.0, -5.5), 15),
        ("Tree_Maple", (6.4, 0.0, -4.2), -8),
        ("Shrub", (-2.4, 0.0, 4.6), 10),
        ("Shrub", (3.1, 0.0, 4.2), -20),
    ]
    for i, x in enumerate((-8.0, -6.0, -4.0, -2.0, 0.0, 2.0, 4.0, 6.0)):
        specs.append(("PicketFence", (x, 0.0, 7.0), 0))
    for x in (-5.5, -3.2, -0.9, 1.4, 3.7):
        specs.append(("Mailbox", (x, 0.0, 8.6), 0))
    p6._place(found, [(n, p, y, 1.0) for n, p, y in specs])
    p6._ground((0.30, 0.38, 0.24))
    p6._look(scene, (11.0, 4.6, 16.5), (-0.4, 1.5, 1.0), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_suburb.png"))


def _park(found):
    scene = p6._begin(wide=True)
    specs = []
    for z in (-6.0, -2.0, 2.0, 6.0):
        specs.append(("Sidewalk", (0.0, 0.0, z), 0.0, 1.0))
    for pos in ((1.75, 0.0, -5.0), (-1.75, 0.0, -1.0), (1.75, 0.0, 3.0), (-1.75, 0.0, 7.2)):
        specs.append(("ParkLamp", pos, 0.0, 1.0))
    for x in (-4.0, 0.0, 4.0):
        specs.append(("PondEdge", (x, 0.0, 10.6), 180.0, 1.0))
    specs.append(("Bench_Wood", (2.7, 0.0, 0.4), 90.0, 1.0))
    specs.append(("Tree_Maple", (-4.6, 0.0, -2.2), 20.0, 1.0))
    specs.append(("Tree_Pine", (4.4, 0.0, 3.6), -10.0, 1.0))
    specs.append(("Shrub", (3.2, 0.0, 7.4), 0.0, 1.0))
    specs.append(("Shrub", (-3.4, 0.0, 6.2), 40.0, 1.0))
    p6._place(found, specs)
    water = p6._mat("Pass10Pond", (0.03, 0.08, 0.07), 0.35)
    p6._sheet(0.0, 0.02, 14.8, 14.0, 8.2, water)
    p6._ground((0.28, 0.36, 0.22))
    p6._look(scene, (6.0, 4.2, -14.0), (0.4, 0.7, 9.0), lens=20.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_park.png"))


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
    found = r._catalog()
    shots = [
        ("court", lambda: _court(found)),
        ("street", lambda: _street(found)),
        ("suburb", lambda: _suburb(found)),
        ("park", lambda: _park(found)),
        ("walkup", lambda: p9._turntable(found, "WalkUp", os.path.join(STILL_DIR, "walkup.png"), 36, 12, 0.78)),
        ("gas", lambda: p9._turntable(found, "GasCanopy", os.path.join(STILL_DIR, "gas_canopy.png"), 28, 16, 0.8)),
        ("picket", lambda: p9._turntable(found, "PicketFence", os.path.join(STILL_DIR, "picket_fence.png"), 18, 14, 0.75)),
        ("pond", lambda: p9._turntable(found, "PondEdge", os.path.join(STILL_DIR, "pond_edge.png"), 12, 22, 0.8)),
    ]
    for name, fn in shots:
        if only and only not in name:
            continue
        fn()
    print("PASS10_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
