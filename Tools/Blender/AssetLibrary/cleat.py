"""Dock cleat. Horns 26 cm across, base bolted to a deck."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Cleat", "Harbor", "Cast cleat, 26 cm across the horns, 11 cm tall. Sits on a deck; pivot is the base.")
    a.climb_note = "Too small to cling."
    a.vault_note = "Not a rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = 0.002 if lod == 0 else 0
        g.box((0, 0.012, 0), (0.22, 0.024, 0.09), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 0.055, 0), (0.07, 0.055, 0.045), "Lib_Steel", bevel=bev, segs=1)
        g.box((0, 0.088, 0), (0.24, 0.028, 0.05), "Lib_Steel", bevel=bev, segs=1)
        g.cylinder((-0.115, 0.088, 0), 0.026, 0.04, "Lib_Steel", seg, axis="X")
        g.cylinder((0.115, 0.088, 0), 0.026, 0.04, "Lib_Steel", seg, axis="X")
        if lod == 0:
            for x in (-0.07, 0.07):
                for z in (-0.028, 0.028):
                    g.cylinder((x, 0.014, z), 0.008, 0.012, "Lib_SteelDark", 6)
        a.end()
    a.box("Col_Base", (0, 0.012, 0), (0.22, 0.024, 0.09))
    a.box("Col_Horns", (0, 0.082, 0), (0.26, 0.040, 0.052))
    a.box("Col_Waist", (0, 0.050, 0), (0.07, 0.040, 0.045))
    return a
