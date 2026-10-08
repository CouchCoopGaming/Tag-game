"""Gas-station canopy over two pumps. FUEL sign on +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "GasCanopy",
        "Buildings",
        "Fuel canopy 9.0 x 6.8 m, roof at 3.50 m, two pumps on an asphalt pad. FUEL sign faces +Z.",
    )
    a.climb_note = "Columns are 22 cm. Not a cling wall."
    a.vault_note = "No rail. The roof is at 3.50 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.03, 0), (8.4, 0.06, 6.2), "Lib_Asphalt", uv_scale=0.5)
        for x in (-1.05, 1.05):
            g.box((x, 0.070, 0), (0.08, 0.008, 4.4), "Lib_PaintWhite")
        for x in (-3.4, 3.4):
            for z in (-2.35, 2.35):
                g.box((x, 1.72, z), (0.22, 3.28, 0.22), "Lib_PaintWhite", bevel=bev, segs=1)
                g.box((x + 0.126, 2.20, z), (0.016, 0.22, 0.16), "Lib_PaintRed")
                g.box((x - 0.126, 2.20, z), (0.016, 0.22, 0.16), "Lib_PaintRed")
                g.box((x, 2.20, z + 0.126), (0.16, 0.22, 0.016), "Lib_PaintRed")
                g.box((x, 2.20, z - 0.126), (0.16, 0.22, 0.016), "Lib_PaintRed")
        g.box((0, 3.46, 0), (9.0, 0.16, 6.8), "Lib_Steel", bevel=bev, segs=1, uv_scale=0.6)
        g.box((0, 3.58, 0), (8.55, 0.04, 6.35), "Lib_PaintWhite")
        g.box((0, 3.55, 3.48), (2.5, 0.52, 0.04), "Lib_PaintRed")
        if lod == 0:
            g.text("FUEL", (0, 3.55, 3.52), 0.32, "Lib_PaintWhite", extrude=0.008, yaw=0.0)
        for x in (-1.6, 1.6):
            g.box((x, 0.70, 0.10), (0.56, 1.24, 0.36), "Lib_PaintWhite", bevel=bev, segs=1)
            g.box((x, 1.02, 0.31), (0.50, 0.16, 0.02), "Lib_PaintRed")
            g.box((x, 0.72, 0.31), (0.24, 0.18, 0.016), "Lib_Black")
            if lod == 0:
                g.box((x + 0.34, 0.90, 0.10), (0.08, 0.24, 0.08), "Lib_SteelDark")
                g.cylinder((x, 1.355, 0.10), 0.07, 0.05, "Lib_Steel", seg)
        a.end()
    a.box("Col_Pad", (0, 0.03, 0), (8.2, 0.04, 6.0))
    for i, x in enumerate((-3.4, 3.4)):
        for j, z in enumerate((-2.35, 2.35)):
            a.box("Col_Post_%d%d" % (i, j), (x, 1.72, z), (0.16, 3.12, 0.16))
    a.box("Col_Roof", (0, 3.46, 0), (8.7, 0.12, 6.5))
    for i, x in enumerate((-1.6, 1.6)):
        a.box("Col_Pump_%d" % i, (x, 0.70, 0.10), (0.46, 1.12, 0.28))
    return a
