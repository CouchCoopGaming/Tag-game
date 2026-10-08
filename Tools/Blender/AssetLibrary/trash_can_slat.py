"""Public trash can. Steel liner with vertical slats and an open top."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("TrashCan_Slat", "StreetFurniture", "Open-top public can, 0.92 m tall, 0.50 m across the slats.")
    a.climb_note = "Not a cling surface."
    a.vault_note = "0.92 m rim is narrow and round. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 10)
        n = lod_pick(lod, 16, 8)
        g.cylinder((0, 0.46, 0), 0.19, 0.84, "Lib_SteelDark", seg, cap_ends=False)
        g.cylinder((0, 0.045, 0), 0.19, 0.04, "Lib_SteelDark", seg)
        g.cylinder((0, 0.88, 0), 0.235, 0.045, "Lib_Steel", seg, bevel=0.004 if lod == 0 else 0, segs=1)
        g.cylinder((0, 0.84, 0), 0.198, 0.05, "Lib_Rubber", seg)
        g.cylinder((0, 0.06, 0), 0.215, 0.05, "Lib_Steel", seg)
        for i in range(n):
            ang = 2.0 * math.pi * i / n
            yaw = math.degrees(ang)
            x = math.sin(ang) * 0.218
            z = math.cos(ang) * 0.218
            g.box((x, 0.46, z), (0.055, 0.74, 0.012), "Lib_Steel", bevel=0.002 if lod == 0 else 0, segs=1, euler=(0, yaw, 0))
        a.end()
    a.capsule("Col_Liner", (0, 0.46, 0), 0.185, 0.88, 1)
    a.box("Col_Rim", (0, 0.88, 0), (0.30, 0.04, 0.30))
    return a
