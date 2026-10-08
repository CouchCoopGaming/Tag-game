"""Rooftop condenser. 1.30 x 0.90 x 0.85 m on rails, two fans."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("RooftopAC", "Buildings", "Rooftop condenser 1.30 x 0.90 m, 0.85 m tall, two fan grilles, sits on 8 cm rails.")
    a.climb_note = "Not a wall."
    a.vault_note = "Too bulky and low to vault. It is a rooftop obstacle."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.006 if lod == 0 else 0
        seg = lod_pick(lod, 12, 8, 6)
        g.box((-0.45, 0.04, 0), (0.08, 0.08, 0.90), "Lib_SteelDark")
        g.box((0.45, 0.04, 0), (0.08, 0.08, 0.90), "Lib_SteelDark")
        g.box((0, 0.48, 0), (1.30, 0.72, 0.90), "Lib_Steel", bevel=bev, segs=lod_pick(lod, 1, 1, 0))
        for x in (-0.32, 0.32):
            g.cylinder((x, 0.86, 0), 0.22, 0.04, "Lib_SteelDark", seg)
            g.cylinder((x, 0.88, 0), 0.08, 0.03, "Lib_Black", 8)
            if lod == 0:
                g.torus((x, 0.87, 0), 0.14, 0.008, "Lib_Steel", 12, 4)
        a.end()
    a.box("Col_Unit", (0, 0.48, 0), (1.26, 0.68, 0.86))
    a.box("Col_RailL", (-0.45, 0.04, 0), (0.08, 0.08, 0.90))
    a.box("Col_RailR", (0.45, 0.04, 0), (0.08, 0.08, 0.90))
    return a
