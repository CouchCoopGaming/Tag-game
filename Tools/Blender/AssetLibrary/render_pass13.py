"""Pass 13 stills. Gangway from the side, closed shed, rowboat, warehouse.

  blender --background --python Tools/Blender/AssetLibrary/render_pass13.py
"""

import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass9 as p9  # noqa: E402
import render_pass2 as r  # noqa: E402
from _common import Geo, _object_from_geo  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass13")


def _world(pos, yaw, local):
    a = math.radians(yaw)
    c, s = math.cos(a), math.sin(a)
    x, y, z = local
    return (pos[0] + x * c + z * s, pos[1] + y, pos[2] - x * s + z * c)


def _rope(a, b, sag=0.16):
    g = Geo(0)
    steps = 5
    pts = []
    for i in range(steps + 1):
        t = i / float(steps)
        x = a[0] + (b[0] - a[0]) * t
        y = a[1] + (b[1] - a[1]) * t - sag * math.sin(math.pi * t)
        z = a[2] + (b[2] - a[2]) * t
        pts.append((x, y, z))
    for i in range(steps):
        g.pipe(pts[i], pts[i + 1], 0.013, "Lib_Rust", 6)
    g.prepare()
    _object_from_geo(g, "MooringRope")


def _gangway(found):
    """Side elevation from water level. Hinge at the quay face, slope down to the float."""
    scene = p6._begin(wide=True)
    specs = [
        # Yaw 180 puts the water face on +Z and the apron inland.
        ("Quay_Edge", (0.0, 0.0, -1.47), 180.0, 1.0),
        # High end (local -Z) lands at world z = 0, just seaward of that face.
        ("Gangway", (4.6, 0.0, 1.30), 0.0, 1.0),
        ("Dock_Straight", (4.6, 0.0, 5.40), 0.0, 1.0),
        ("FishCrate", (5.4, 0.66, 6.6), 8.0, 1.0),
        ("Piling", (7.4, 0.0, 3.2), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.16, 0.20, 0.18), y=-0.35)
    p6._sheet(4.6, 0.34, 4.0, 22.0, 16.0, p9._water_mat())
    p6._look(scene, (12.6, 0.46, 1.30), (4.6, 0.78, 1.45), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_gangway.png"))


def _shed(found):
    scene = p6._begin(wide=True)
    specs = [
        ("HarborShed", (0.0, 0.0, 0.0), 28.0, 1.0),
        ("FishCrate", (2.5, 0.0, 1.5), 12.0, 1.0),
        ("LobsterTrap", (-2.3, 0.0, 1.7), -15.0, 1.0),
        ("RopeCoil", (2.2, 0.0, 0.2), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, (4.6, 1.7, 5.4), (0.2, 1.45, 0.15), lens=30.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_shed.png"))


def _boats(found):
    scene = p6._begin(wide=True)
    dock = (-2.05, 0.0, 0.10)
    fish = (2.35, 0.0, 0.15)
    fish_yaw = -8.0
    row = (1.45, 0.08, 2.45)
    row_yaw = 24.0
    specs = [
        ("Dock_Straight", dock, 0.0, 1.0),
        ("FishingBoat", fish, fish_yaw, 1.0),
        ("Rowboat", row, row_yaw, 1.0),
        ("Buoy", (3.8, 0.0, -1.35), 10.0, 1.0),
        ("Piling", (-3.6, 0.0, 2.4), 0.0, 1.0),
        ("FishCrate", (-1.4, 0.66, -1.2), 6.0, 1.0),
    ]
    p6._place(found, specs)
    # Dock cleats are at local x = ±1.20, z = ±1.55, horn near y = 0.78.
    cleat_bow = (dock[0] + 1.20, 0.78, dock[2] - 1.55)
    cleat_mid = (dock[0] + 1.20, 0.78, dock[2] + 1.55)
    _rope(cleat_mid, _world(fish, fish_yaw, (-0.28, 0.52, 1.35)), sag=0.14)
    _rope(cleat_bow, _world(fish, fish_yaw, (0.0, 0.92, -2.05)), sag=0.18)
    _rope(cleat_mid, _world(row, row_yaw, (-0.518, 0.626, -0.45)), sag=0.12)
    p6._ground((0.10, 0.16, 0.16), y=-0.45)
    p6._sheet(0.4, 0.34, 0.6, 16.0, 14.0, p9._water_mat())
    p6._look(scene, (5.6, 1.35, 4.2), (0.7, 0.55, 0.7), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_boats.png"))


def _warehouse(found):
    scene = p6._begin(wide=True)
    specs = [
        ("Quay_Edge", (0.0, 0.0, -3.6), 0.0, 1.0),
        ("HarborWarehouse", (-1.0, 0.91, -0.6), 180.0, 1.0),
        ("Container_20", (6.4, 0.91, -0.4), 0.0, 1.0),
        ("Container_20", (6.4, 3.55, -0.4), 0.0, 1.0),
        ("Container_20", (6.4, 6.19, -0.4), 0.0, 1.0),
        ("QuayDavit", (-7.0, 0.90, -4.35), 180.0, 1.0),
        ("FishCrate", (4.6, 0.91, -3.4), 20.0, 1.0),
        ("Pallet", (5.2, 0.91, -2.6), -8.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.30, 0.32, 0.30), y=-0.05)
    p6._sheet(0.0, 0.34, -10.0, 28.0, 14.0, p9._water_mat())
    p6._look(scene, (9.2, 3.4, -13.5), (0.4, 2.6, -1.0), lens=26.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_warehouse.png"))


def _harbor(found):
    scene = p6._begin(wide=True)
    dock = (0.2, 0.0, -4.8)
    fish = (3.6, 0.0, -6.2)
    fish_yaw = 12.0
    row = (-3.4, 0.08, -8.4)
    row_yaw = -20.0
    specs = [
        ("Quay_Edge", (0.0, 0.0, 0.0), 0.0, 1.0),
        ("HarborWarehouse", (0.3, 0.91, 3.55), 180.0, 1.0),
        ("Container_20", (7.5, 0.91, 3.1), 0.0, 1.0),
        ("Container_20", (7.5, 3.55, 3.1), 0.0, 1.0),
        ("Container_20", (7.5, 6.19, 3.1), 0.0, 1.0),
        ("QuayDavit", (-6.4, 0.90, -0.35), 180.0, 1.0),
        ("Lighthouse", (-8.2, 0.0, 1.8), 15.0, 1.0),
        ("HarborShed", (-7.2, 0.91, 5.9), 160.0, 1.0),
        ("Dock_Straight", dock, 0.0, 1.0),
        ("Dock_Straight", (0.2, 0.0, -10.8), 0.0, 1.0),
        ("Gangway", (0.2, 0.0, -1.9), 180.0, 1.0),
        ("FishingBoat", fish, fish_yaw, 1.0),
        ("Rowboat", row, row_yaw, 1.0),
        ("Boat", (6.5, 0.0, -13.0), 200.0, 1.0),
        ("Buoy", (8.4, 0.0, -7.5), 0.0, 1.0),
        ("Piling", (2.6, 0.0, -12.4), 0.0, 1.0),
        ("Piling", (-2.4, 0.0, -12.6), 0.0, 1.0),
        ("Crate", (0.45, 0.66, -5.0), 8.0, 1.0),
        ("Pallet", (-0.55, 0.66, -7.2), 15.0, 1.0),
        ("FuelDock", (0.75, 0.62, -6.5), 90.0, 1.0),
        ("FishCrate", (1.05, 0.66, -8.2), 8.0, 1.0),
        ("LobsterTrap", (-0.7, 0.66, -8.8), 20.0, 1.0),
        ("LifeRing", (4.6, 0.91, 1.2), 190.0, 1.0),
        ("LifeRing", (-1.05, 0.62, -9.6), 90.0, 1.0),
    ]
    for z in (-5.2, -7.2, -9.2, -11.2):
        specs.append(("HarborRail", (1.55, 0.62, z), 90.0, 1.0))
        specs.append(("HarborRail", (-1.25, 0.62, z), 90.0, 1.0))
    p6._place(found, specs)
    cleat_a = (dock[0] + 1.20, 0.78, dock[2] - 1.55)
    cleat_b = (dock[0] + 1.20, 0.78, dock[2] + 0.4)
    cleat_c = (dock[0] - 1.20, 0.78, dock[2] - 1.2)
    _rope(cleat_a, _world(fish, fish_yaw, (0.0, 0.92, -2.05)), sag=0.20)
    _rope(cleat_b, _world(fish, fish_yaw, (-0.28, 0.52, 1.35)), sag=0.16)
    _rope(cleat_c, _world(row, row_yaw, (0.518, 0.626, -0.45)), sag=0.22)
    p6._ground((0.22, 0.26, 0.20), y=-0.05)
    p6._sheet(0.0, 0.34, -8.0, 40.0, 30.0, p9._water_mat())
    p6._look(scene, (-16.0, 8.0, -18.0), (1.0, 2.2, 0.5), lens=20.0)
    r._render(scene, os.path.join(STILL_DIR, "vignette_harbor.png"))


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    shots = [
        ("gangway", lambda: _gangway(found)),
        ("shed", lambda: _shed(found)),
        ("boats", lambda: _boats(found)),
        ("warehouse", lambda: _warehouse(found)),
        ("harbor", lambda: _harbor(found)),
    ]
    for name, fn in shots:
        if only and not any(part in name for part in only):
            continue
        fn()
    print("PASS13_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
