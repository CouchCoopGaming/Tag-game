"""Wall-mounted air conditioner. 0.70 x 0.45 x 0.55 m, sleeve sticks through the wall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("WallAC", "Utility", "Through-wall unit. Sleeve is 0.32 m deep, the outdoor half projects on +Z. Bottom sits at the pivot.")
    a.climb_note = "Not a cling."
    a.vault_note = "Too small to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        seg = lod_pick(lod, 12, 8)
        g.box((0, 0.22, 0.0), (0.62, 0.40, 0.32), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 0.24, 0.28), (0.70, 0.48, 0.28), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((0, 0.16, 0.43), (0.55, 0.18, 0.02), "Lib_Black")
        if lod == 0:
            g.cylinder((0.22, 0.34, 0.43), 0.04, 0.02, "Lib_Steel", seg, axis="Z")
            for i in range(4):
                g.box((-0.18 + i * 0.1, 0.16, 0.45), (0.03, 0.12, 0.012), "Lib_SteelDark")
            g.box((0, 0.02, 0.36), (0.5, 0.02, 0.12), "Lib_SteelDark")
        a.end()
    a.loose_pivot = True
    a.box("Col_Sleeve", (0, 0.22, 0.0), (0.60, 0.38, 0.30))
    a.box("Col_Head", (0, 0.24, 0.28), (0.68, 0.46, 0.26))
    return a
