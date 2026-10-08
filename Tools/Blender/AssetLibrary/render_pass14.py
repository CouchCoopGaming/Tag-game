"""Pass 14 stills. Corrugated containers, aluminum gangway, shed, boats.

  blender --background --python Tools/Blender/AssetLibrary/render_pass14.py
"""

import os
import sys

import bpy  # noqa: F401

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass9 as p9  # noqa: E402
import render_pass2 as r  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass14")


def _containers(found):
    """Three enamels, long side, so the vertical ribs and the colors read."""
    scene = p6._begin(wide=True)
    specs = [
        ("Container_20", (0.0, 0.0, 0.0), 16.0, 1.0),
        ("Container_20_Blue", (4.8, 0.0, 0.15), 16.0, 1.0),
        ("Container_20_Green", (9.6, 0.0, 0.30), 16.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.34, 0.34, 0.32))
    p6._look(scene, (5.4, 2.5, 12.8), (4.8, 1.35, 0.3), lens=30.0)
    r._render(scene, os.path.join(STILL_DIR, "containers.png"))


def _container_door(found):
    """Close on the door end: four bars, handles, corner castings."""
    scene = p6._begin(wide=True)
    p6._place(found, [("Container_20", (0.0, 0.0, 0.0), 8.0, 1.0)])
    p6._ground((0.30, 0.30, 0.28))
    p6._look(scene, (1.55, 1.25, 5.55), (0.05, 1.30, 2.55), lens=42.0)
    r._render(scene, os.path.join(STILL_DIR, "container_door.png"))


def _gangway(found):
    """Side elevation. High end at the quay, roller on the float."""
    scene = p6._begin(wide=True)
    specs = [
        ("Quay_Edge", (0.0, 0.0, -1.47), 180.0, 1.0),
        ("Gangway", (4.6, 0.0, 1.30), 0.0, 1.0),
        ("Dock_Straight", (4.6, 0.0, 5.40), 0.0, 1.0),
        ("Piling", (7.4, 0.0, 3.2), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.16, 0.20, 0.18), y=-0.35)
    p6._sheet(4.6, 0.34, 4.0, 22.0, 16.0, p9._water_mat())
    p6._look(scene, (12.6, 0.46, 1.30), (4.6, 0.78, 1.45), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_gangway.png"))


def _gangway_above(found):
    """Three-quarter from above: plate, cleats, posts in the stringers, roller."""
    scene = p6._begin(wide=True)
    specs = [
        ("Quay_Edge", (0.0, 0.0, -1.47), 180.0, 1.0),
        ("Gangway", (4.6, 0.0, 1.30), 0.0, 1.0),
        ("Dock_Straight", (4.6, 0.0, 5.40), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.16, 0.20, 0.18), y=-0.35)
    p6._sheet(4.6, 0.34, 4.0, 22.0, 16.0, p9._water_mat())
    p6._look(scene, (8.6, 3.5, 3.8), (4.6, 0.78, 1.45), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_gangway_above.png"))


def _shed(found):
    scene = p6._begin(wide=True)
    specs = [
        ("HarborShed", (0.0, 0.0, 0.0), 28.0, 1.0),
        ("FishCrate", (2.5, 0.0, 1.5), 12.0, 1.0),
        ("LobsterTrap", (-2.3, 0.0, 1.7), -15.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, (4.6, 1.7, 5.4), (0.2, 1.45, 0.15), lens=30.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_shed.png"))


def _boats(found):
    """Low bow quarter so the skiff V and the rowboat strakes are in frame."""
    scene = p6._begin(wide=True)
    specs = [
        ("FishingBoat", (0.6, 0.0, -0.3), -28.0, 1.0),
        ("Rowboat", (-1.5, 0.08, 1.7), 30.0, 1.0),
        ("Dock_Straight", (-2.4, 0.0, 3.6), 0.0, 1.0),
        ("Piling", (3.2, 0.0, -2.4), 0.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.10, 0.16, 0.16), y=-0.45)
    p6._sheet(0.2, 0.34, 0.4, 16.0, 14.0, p9._water_mat())
    p6._look(scene, (4.6, 0.62, 2.4), (0.3, 0.40, 0.3), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_boats.png"))


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    shots = [
        ("containers", lambda: _containers(found)),
        ("container_door", lambda: _container_door(found)),
        ("gangway_above", lambda: _gangway_above(found)),
        ("gangway", lambda: _gangway(found)),
        ("shed", lambda: _shed(found)),
        ("boats", lambda: _boats(found)),
    ]
    for name, fn in shots:
        if only and not any(part in name for part in only):
            continue
        fn()
    print("PASS14_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
