"""Traffic signal on a short mast arm. Head center at 4.6 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("TrafficLight", "StreetFurniture", "Three-section signal at 4.6 m on a mast arm over a 4.2 m pole.")
    a.climb_note = "Pole is round and 14 cm. Not a cling wall."
    a.vault_note = "No rail at vault height."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        g.box((0, 0.02, 0), (0.40, 0.04, 0.40), "Lib_SteelDark", bevel=0.004 if lod == 0 else 0, segs=1)
        g.cylinder((0, 2.1, 0), 0.07, 4.1, "Lib_Steel", seg)
        g.pipe((0, 4.15, 0.05), (0, 4.35, 1.3), 0.04, "Lib_SteelDark", seg)
        g.box((0, 4.55, 1.45), (0.28, 0.95, 0.28), "Lib_Black", bevel=0.006 if lod == 0 else 0, segs=1)
        g.cylinder((0, 4.82, 1.60), 0.09, 0.02, "Lib_PaintRed", 12, axis="Z")
        g.cylinder((0, 4.55, 1.60), 0.09, 0.02, "Lib_PaintYellow", 12, axis="Z")
        g.cylinder((0, 4.28, 1.60), 0.09, 0.02, "Lib_PaintGreen", 12, axis="Z")
        if lod == 0:
            for y in (4.82, 4.55, 4.28):
                g.cylinder((0, y, 1.59), 0.07, 0.01, "Lib_SteelDark", 12, axis="Z")
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.40, 0.04, 0.40))
    a.capsule("Col_Pole", (0, 2.1, 0), 0.07, 4.1, 1)
    a.box("Col_Arm", (0, 4.25, 0.68), (0.06, 0.06, 1.10), euler=(-9.0, 0, 0))
    a.box("Col_Head", (0, 4.55, 1.45), (0.26, 0.90, 0.26))
    return a
