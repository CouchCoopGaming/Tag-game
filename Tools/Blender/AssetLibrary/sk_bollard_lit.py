"""Lit bollard. 1.05 m, steel barrel, glowing lens band."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Bollard_Lit",
        "StreetFurniture",
        "Lit bollard, 1.09 m tall, 18 cm across the barrel. Lens band at 0.80 m.",
    )
    a.climb_note = "Too narrow to cling."
    a.vault_note = "Round top at 1.05 m is not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.02, 0), 0.14, 0.04, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.40, 0), 0.09, 0.68, "Lib_Steel", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.80, 0), 0.10, 0.08, "Lib_Lamp", seg)
        g.cylinder((0, 0.92, 0), 0.09, 0.12, "Lib_Steel", seg)
        g.sphere((0, 1.00, 0), 0.09, "Lib_SteelDark", seg)
        if lod == 0:
            for i in range(4):
                ang = math.radians(i * 90 + 20)
                g.cylinder((math.sin(ang) * 0.11, 0.045, math.cos(ang) * 0.11), 0.012, 0.014, "Lib_Steel", 6)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.20, 0.032, 0.20))
    a.capsule("Col_Barrel", (0, 0.40, 0), 0.078, 0.62, 1)
    a.capsule("Col_Lens", (0, 0.80, 0), 0.088, 0.18, 1)
    a.capsule("Col_Neck", (0, 0.92, 0), 0.075, 0.10, 1)
    a.sphere("Col_Cap", (0, 1.045, 0), 0.040)
    return a
