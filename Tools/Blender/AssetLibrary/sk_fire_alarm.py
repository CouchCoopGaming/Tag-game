"""Street fire-alarm box. Red pedestal, peaked cap, pull on the door. No legend.

The door center is about 1.15 m, a comfortable reach. Not the blue call box
and not the siamese connection.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

BOX_Y = 1.16
BOX_H = 0.46
BOX_W = 0.26
BOX_D = 0.16


def _cap(g, lod):
    """Cornice and a hipped cap. The peak is the high point."""
    bev = lod_pick(lod, 0.002, 0.0)
    bs = 1 if lod == 0 else 0
    y = BOX_Y + BOX_H * 0.5
    g.box((0, y + 0.02, 0), (BOX_W + 0.04, 0.04, BOX_D + 0.04), "Lib_PaintRed", bevel=bev, segs=bs)
    peak = y + 0.12
    base = y + 0.04
    hx, hz = (BOX_W + 0.02) * 0.5, (BOX_D + 0.02) * 0.5
    verts = [
        (-hx, base, -hz), (hx, base, -hz), (hx, base, hz), (-hx, base, hz),
        (0.0, peak, 0.0),
    ]
    g.mesh(verts, [
        (0, 1, 4),
        (1, 2, 4),
        (2, 3, 4),
        (3, 0, 4),
        (0, 3, 2, 1),
    ], "Lib_PaintRed")


def _door(g, lod):
    seg = lod_pick(lod, 8, 6)
    z = BOX_D * 0.5 + 0.006
    g.box((0, BOX_Y, z), (0.20, 0.34, 0.012), "Lib_BoxRed")
    # Hinge barrels on the left edge.
    for y in (BOX_Y - 0.12, BOX_Y + 0.12):
        g.cylinder((-0.09, y, z + 0.008), 0.008, 0.028, "Lib_Steel", seg)
    # T-handle. The stem comes out of the door, the bar is the pull.
    g.cylinder((0.0, BOX_Y, z + 0.016), 0.010, 0.016, "Lib_Steel", seg, axis="Z")
    g.cylinder((0.0, BOX_Y, z + 0.030), 0.008, 0.10, "Lib_Steel", seg, axis="X")
    if lod == 0:
        g.cylinder((0.06, BOX_Y - 0.10, z + 0.010), 0.006, 0.008, "Lib_Brass", 6, axis="Z")


@register
def create():
    a = Asset(
        "FireAlarm_Box",
        "StreetFurniture",
        "Fire alarm box. Red, 0.26 m wide, door at 1.16 m, peaked cap at 1.51 m. Pull handle, no legend.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.016, 0), (0.28, 0.032, 0.28), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            for x in (-0.09, 0.09):
                for z in (-0.09, 0.09):
                    g.cylinder((x, 0.036, z), 0.010, 0.010, "Lib_Steel", 6)
        g.cylinder((0, 0.50, 0), 0.045, 0.92, "Lib_Steel", seg)
        g.box((0, BOX_Y, 0), (BOX_W, BOX_H, BOX_D), "Lib_PaintRed", bevel=bev, segs=bs)
        _cap(g, lod)
        _door(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.014, 0), (0.18, 0.020, 0.18))
    a.capsule("Col_Post", (0, 0.48, 0), 0.030, 0.84, direction=1)
    # Interior of the red box, clear of the door plate and the post top.
    a.box("Col_Box", (0, BOX_Y, -0.01), (0.16, 0.32, 0.08))
    return a
