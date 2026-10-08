"""Mini-split condenser on a pad. Fan is on the face, unlike the rooftop unit."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "AC_MiniSplit",
        "StreetFurniture",
        "Outdoor mini-split, 0.80 x 0.55 x 0.32 m, on a 4 cm pad. Fan grille faces +Z. Overall height 0.62 m.",
    )
    a.climb_note = "Not a wall."
    a.vault_note = "Too low to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 14, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        g.box((0, 0.02, 0), (0.92, 0.04, 0.46), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.6)
        g.box((0, 0.32, 0), (0.80, 0.54, 0.30), "Lib_PaintWhite", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.36, 0.168), 0.18, 0.016, "Lib_SteelDark", seg, axis="Z")
        g.cylinder((0, 0.36, 0.180), 0.055, 0.012, "Lib_Black", 8, axis="Z")
        if lod == 0:
            for i in range(5):
                y = 0.22 + i * 0.07
                g.box((0, y, 0.178), (0.30, 0.008, 0.006), "Lib_Steel")
            g.box((0.43, 0.46, 0), (0.05, 0.14, 0.18), "Lib_SteelDark", bevel=bev, segs=1)
            g.cylinder((-0.36, 0.46, -0.22), 0.012, 0.24, "Lib_Steel", 6)
            g.pipe((-0.36, 0.56, -0.22), (-0.50, 0.56, -0.22), 0.012, "Lib_Steel", 6)
        a.end()
    a.box("Col_Pad", (0, 0.02, 0), (0.84, 0.03, 0.40))
    a.box("Col_Cabinet", (0, 0.32, 0), (0.64, 0.40, 0.22))
    return a
