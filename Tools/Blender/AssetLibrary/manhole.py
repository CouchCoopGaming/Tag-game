"""Manhole cover, 0.72 m diameter, 3 cm thick. Walkable."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Manhole", "StreetFurniture", "Round cover, 0.72 m across, 3 cm thick. Lift it onto a road surface.")
    a.climb_note = "Flat ground cover."
    a.vault_note = "Flush. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 24, 12)
        g.cylinder((0, 0.015, 0), 0.36, 0.03, "Lib_SteelDark", seg, bevel=0.003 if lod == 0 else 0, segs=1)
        g.cylinder((0, 0.028, 0), 0.30, 0.008, "Lib_Steel", seg)
        rings = lod_pick(lod, 3, 1)
        for i in range(rings):
            g.torus((0, 0.034, 0), 0.12 + i * 0.07, 0.006, "Lib_Rust", 16 if lod == 0 else 8, 4)
        if lod == 0:
            for i in range(8):
                ang = math.radians(i * 45)
                g.box((math.sin(ang) * 0.18, 0.034, math.cos(ang) * 0.18), (0.04, 0.008, 0.012), "Lib_Steel", euler=(0, i * 45, 0))
        a.end()
    a.cylinder_collider = None
    a.box("Col_Cover", (0, 0.015, 0), (0.50, 0.03, 0.50))
    return a
