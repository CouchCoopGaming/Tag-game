"""Steel bollard, 0.95 m, rounded cap. Not a vault."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Bollard", "StreetFurniture", "Steel bollard, 0.95 m tall, 16 cm across the barrel, rounded cap.")
    a.climb_note = "Too narrow to cling."
    a.vault_note = "Round 0.95 m cap is not a vault rail. Treat it as a blocker."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        g.cylinder((0, 0.02, 0), 0.11, 0.04, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.46, 0), 0.075, 0.84, "Lib_Steel", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.sphere((0, 0.90, 0), 0.075, "Lib_Steel", seg)
        g.cylinder((0, 0.62, 0), 0.082, 0.025, "Lib_PaintYellow", seg)
        if lod == 0:
            g.cylinder((0, 0.78, 0), 0.078, 0.012, "Lib_SteelDark", seg)
            for i in range(4):
                ang = math.radians(45 + i * 90)
                g.cylinder((math.sin(ang) * 0.08, 0.045, math.cos(ang) * 0.08), 0.01, 0.012, "Lib_Steel", 5)
            g.torus((0, 0.55, 0), 0.078, 0.006, "Lib_SteelDark", 10, 4)
        a.end()
    a.capsule("Col_Bollard", (0, 0.48, 0), 0.075, 0.96, 1)
    a.box("Col_Base", (0, 0.02, 0), (0.16, 0.04, 0.16))
    return a
