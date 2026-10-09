"""Double-headed residential street light. Two cobra heads, lamp at 5.45 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from light_post_single import _head
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "LightPost_Double",
        "StreetFurniture",
        "Two cobra heads on one tapered pole. Lamps at 5.45 m, arms along +Z and -Z.",
    )
    a.climb_note = "Round tapered pole. Not a cling wall."
    a.vault_note = "No vault edge. Both arms are overhead."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 10)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = lod_pick(lod, 1, 0)
        g.box((0, 0.012, 0), (0.38, 0.024, 0.38), "Lib_SteelDark", bevel=0.004 if lod == 0 else 0, segs=1)
        if lod == 0:
            for x in (-0.14, 0.14):
                for z in (-0.14, 0.14):
                    g.cylinder((x, 0.028, z), 0.012, 0.016, "Lib_Steel", 6)
        g.cylinder((0, 0.11, 0), 0.12, 0.16, "Lib_Steel", seg, bevel=bev, segs=bs)
        g.cone((0, 2.72, 0), 0.085, 0.048, 5.05, "Lib_Steel", seg)
        g.cylinder((0, 4.55, 0), 0.058, 0.04, "Lib_SteelDark", seg)
        g.cylinder((0, 5.20, 0), 0.055, 0.08, "Lib_SteelDark", seg)
        if lod == 0:
            g.box((0.07, 1.4, 0), (0.01, 0.28, 0.12), "Lib_SteelDark")
            g.cylinder((0.08, 1.4, 0), 0.012, 0.01, "Lib_Steel", 5, axis="X")
        _head(g, lod, 1, bev, bs, seg)
        _head(g, lod, -1, bev, bs, seg)
        a.end()
    a.box("Col_Base", (0, 0.012, 0), (0.38, 0.024, 0.38))
    a.capsule("Col_Pole", (0, 2.62, 0), 0.048, 5.15, 1)
    a.box("Col_Arm_N", (0, 5.25, 0.60), (0.05, 0.05, 0.95), euler=(-3.2, 0, 0))
    a.box("Col_Arm_S", (0, 5.25, -0.60), (0.05, 0.05, 0.95), euler=(3.2, 0, 0))
    a.box("Col_Head_N", (0, 5.36, 1.15), (0.58, 0.18, 0.34))
    a.box("Col_Head_S", (0, 5.36, -1.15), (0.58, 0.18, 0.34))
    return a
