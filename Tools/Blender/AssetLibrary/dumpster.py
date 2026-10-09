"""Commercial dumpster. 2.2 x 1.2 x 1.35 m, split lids, side pockets."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Dumpster", "Utility", "Front-load dumpster 2.20 x 1.20 m, 1.35 m tall. Lids closed. Fork pockets on the sides.")
    a.climb_note = "Side walls are short cling faces, 1.2 m. Lids are a landing."
    a.vault_note = "Lid top is 1.35 m. High for a ground vault; the side rail is not at 1.05."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.008 if lod == 0 else 0
        g.box((0, 0.58, 0), (2.20, 1.10, 1.20), "Lib_PaintGreen", bevel=bev, segs=lod_pick(lod, 1, 0))
        g.box((-0.55, 1.28, 0), (1.05, 0.06, 1.10), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0.55, 1.28, 0), (1.05, 0.06, 1.10), "Lib_SteelDark", bevel=bev, segs=1)
        for x in (-1.05, 1.05):
            g.box((x, 0.35, 0), (0.08, 0.22, 1.05), "Lib_Steel")
        if lod == 0:
            for i in range(5):
                g.box((-0.8 + i * 0.4, 0.7, 0.61), (0.04, 0.7, 0.015), "Lib_SteelDark")
            g.box((0, 1.32, 0.4), (0.2, 0.04, 0.08), "Lib_Steel")
            for z in (-0.35, 0.35):
                g.cylinder((-1.15, 0.18, z), 0.08, 0.06, "Lib_Rubber", 10, axis="X")
                g.cylinder((1.15, 0.18, z), 0.08, 0.06, "Lib_Rubber", 10, axis="X")
        a.end()
    a.box("Col_Body", (0, 0.58, 0), (2.20, 1.10, 1.20))
    a.box("Col_LidL", (-0.55, 1.28, 0), (1.05, 0.06, 1.10))
    a.box("Col_LidR", (0.55, 1.28, 0), (1.05, 0.06, 1.10))
    return a
