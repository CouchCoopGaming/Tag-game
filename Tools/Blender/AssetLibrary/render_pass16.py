"""Pass 16 stills. Lamp framing, trash can, bench frame, oars, hedge, trestle.

  blender --background --python Tools/Blender/AssetLibrary/render_pass16.py
"""

import os
import sys

import bpy  # noqa: F401

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass9 as p9  # noqa: E402
import render_pass2 as r  # noqa: E402

STILL_DIR = os.path.join(r._common.REPO, "Docs", "AssetStills", "pass16")

# Same cameras for before and after so the mesh change is the comparison.
# The lamp and the can are pulled back far enough that the whole object fits.
_PROPS = (
    ("bench", "Bench_Wood", 32.0, (2.7, 1.15, 2.15), (0.0, 0.48, 0.0), 36.0),
    ("picnic", "PicnicTable", 28.0, (2.9, 1.25, 2.35), (0.0, 0.42, 0.0), 34.0),
    ("trash", "TrashCan_Lidded", 28.0, (1.7, 0.72, 1.55), (0.0, 0.46, 0.0), 36.0),
    ("median", "Median_Planter", 16.0, (2.4, 1.35, 2.8), (0.0, 0.5, 0.0), 34.0),
    ("lamp", "ParkLamp", 16.0, (3.6, 1.75, 4.8), (0.0, 1.65, 0.0), 28.0),
)


def _prop(found, name, yaw, cam, aim, lens, path):
    scene = p6._begin(wide=True)
    p6._place(found, [(name, (0.0, 0.0, 0.0), yaw, 1.0)])
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, cam, aim, lens=lens)
    r._render(scene, path)


def _lamp_head(found, path):
    scene = p6._begin(wide=True)
    p6._place(found, [("ParkLamp", (0.0, 0.0, 0.0), 28.0, 1.0)])
    p6._ground((0.34, 0.36, 0.32))
    p6._look(scene, (0.95, 3.15, 1.15), (0.0, 3.08, 0.0), lens=42.0)
    r._render(scene, path)


def _rowboat(found, path):
    scene = p6._begin(wide=True)
    p6._place(found, [("Rowboat", (0.0, 0.08, 0.0), 50.0, 1.0)])
    p6._ground((0.10, 0.16, 0.16), y=-0.45)
    p6._sheet(0.0, 0.34, 0.0, 14.0, 12.0, p9._water_mat())
    p6._look(scene, (2.6, 2.15, 2.5), (0.15, 0.4, 0.15), lens=36.0)
    r._render(scene, path)


def _oar(found, path):
    """Close on the starboard oarlock. Yaw puts that lock toward the camera."""
    scene = p6._begin(wide=True)
    p6._place(found, [("Rowboat", (0.0, 0.08, 0.0), -40.0, 1.0)])
    p6._ground((0.10, 0.16, 0.16), y=-0.45)
    p6._sheet(0.0, 0.34, 0.0, 12.0, 10.0, p9._water_mat())
    p6._look(scene, (1.35, 1.05, 1.15), (0.35, 0.5, 0.05), lens=46.0)
    r._render(scene, path)


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
            shots.append((
                "%s_%s" % (tag, key),
                lambda n=name, y=yaw, c=cam, a=aim, l=lens, p=path: _prop(found, n, y, c, a, l, p),
            ))
    for tag in ("before", "after"):
        shots.append((
            "%s_rowboat" % tag,
            lambda t=tag: _rowboat(found, os.path.join(STILL_DIR, "%s_rowboat.png" % t)),
        ))
        shots.append((
            "%s_oar" % tag,
            lambda t=tag: _oar(found, os.path.join(STILL_DIR, "%s_oar.png" % t)),
        ))
    shots.append((
        "lamp_head",
        lambda: _lamp_head(found, os.path.join(STILL_DIR, "lamp_head.png")),
    ))
    for name, fn in shots:
        if only and not any(name.startswith(part) for part in only):
            continue
        fn()
    print("PASS16_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
