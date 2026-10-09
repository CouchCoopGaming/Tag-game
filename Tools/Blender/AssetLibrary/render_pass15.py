"""Pass 15 stills. Boats, shed door, containers, and five weak props.

  blender --background --python Tools/Blender/AssetLibrary/render_pass15.py
"""

import os
import sys

import bpy  # noqa: F401

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass9 as p9  # noqa: E402
import render_pass2 as r  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass15")

# Same cameras for the before and after close-ups.
_PROPS = (
    ("bench", "Bench_Wood", 28.0, (2.4, 1.15, 1.9), (0.0, 0.48, 0.0), 38.0),
    ("picnic", "PicnicTable", 24.0, (2.6, 1.45, 2.2), (0.0, 0.55, 0.0), 36.0),
    ("trash", "TrashCan_Lidded", 30.0, (1.35, 0.95, 1.25), (0.0, 0.55, 0.0), 42.0),
    ("median", "Median_Planter", 18.0, (2.6, 1.55, 3.4), (0.0, 0.45, 0.0), 32.0),
    ("lamp", "ParkLamp", 20.0, (1.9, 2.15, 2.1), (0.0, 1.7, 0.0), 30.0),
)


def _prop(found, name, yaw, cam, aim, lens, path):
    scene = p6._begin(wide=True)
    p6._place(found, [(name, (0.0, 0.0, 0.0), yaw, 1.0)])
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def _rowboat(found):
    """3/4 from about 2 m above the water. The whole hull stays in frame."""
    scene = p6._begin(wide=True)
    p6._place(found, [("Rowboat", (0.0, 0.08, 0.0), 35.0, 1.0)])
    p6._ground((0.10, 0.16, 0.16), y=-0.45)
    p6._sheet(0.0, 0.34, 0.0, 14.0, 12.0, p9._water_mat())
    p6._look(scene, (3.4, 2.34, 2.6), (0.1, 0.35, 0.0), lens=38.0)
    r._render(scene, os.path.join(STILL_DIR, "rowboat.png"))


def _skiff(found):
    scene = p6._begin(wide=True)
    p6._place(found, [("FishingBoat", (0.0, 0.0, 0.0), -32.0, 1.0)])
    p6._ground((0.10, 0.16, 0.16), y=-0.45)
    p6._sheet(0.0, 0.34, 0.0, 16.0, 14.0, p9._water_mat())
    p6._look(scene, (4.6, 2.34, 3.6), (0.0, 0.55, -0.2), lens=34.0)
    r._render(scene, os.path.join(STILL_DIR, "skiff.png"))


def _skiff_bow(found):
    """Bow-on, a little above the water, so the deadrise V is the subject."""
    scene = p6._begin(wide=True)
    p6._place(found, [("FishingBoat", (0.0, 0.0, 0.0), 0.0, 1.0)])
    p6._ground((0.10, 0.16, 0.16), y=-0.45)
    p6._sheet(0.0, 0.34, 0.0, 16.0, 16.0, p9._water_mat())
    p6._look(scene, (0.15, 1.15, -6.4), (0.0, 0.45, -1.2), lens=42.0)
    r._render(scene, os.path.join(STILL_DIR, "skiff_bow.png"))


def _shed(found):
    scene = p6._begin(wide=True)
    p6._place(found, [("HarborShed", (0.0, 0.0, 0.0), 18.0, 1.0)])
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, (2.4, 1.35, 3.6), (0.25, 1.15, 1.15), lens=38.0)
    r._render(scene, os.path.join(STILL_DIR, "harbor_shed.png"))


def _containers(found):
    scene = p6._begin(wide=True)
    specs = [
        ("Container_20", (0.0, 0.0, 0.0), 18.0, 1.0),
        ("Container_20_Blue", (4.8, 0.0, 0.2), 18.0, 1.0),
        ("Container_20_Green", (9.6, 0.0, 0.4), 18.0, 1.0),
    ]
    p6._place(found, specs)
    p6._ground((0.34, 0.34, 0.32))
    # High 3/4 so the long side and the roof waves are both in frame.
    p6._look(scene, (2.4, 3.6, 11.2), (4.6, 1.15, 0.6), lens=28.0)
    r._render(scene, os.path.join(STILL_DIR, "containers.png"))


def _container_door(found):
    scene = p6._begin(wide=True)
    p6._place(found, [("Container_20", (0.0, 0.0, 0.0), 12.0, 1.0)])
    p6._ground((0.30, 0.30, 0.28))
    p6._look(scene, (2.3, 1.7, 9.4), (0.0, 1.25, 2.5), lens=32.0)
    r._render(scene, os.path.join(STILL_DIR, "container_door.png"))


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    found = r._catalog()
    shots = []
    for key, name, yaw, cam, aim, lens in _PROPS:
        for tag in ("before", "after"):
            path = os.path.join(STILL_DIR, "%s_%s.png" % (tag, key))
            shots.append((tag + "_" + key, lambda n=name, y=yaw, c=cam, a=aim, l=lens, p=path: _prop(found, n, y, c, a, l, p)))
    shots.extend([
        ("rowboat", lambda: _rowboat(found)),
        ("skiff_bow", lambda: _skiff_bow(found)),
        ("skiff", lambda: _skiff(found)),
        ("shed", lambda: _shed(found)),
        ("container_door", lambda: _container_door(found)),
        ("containers", lambda: _containers(found)),
    ])
    for name, fn in shots:
        if only and not any(name.startswith(part) for part in only):
            continue
        fn()
    print("PASS15_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
