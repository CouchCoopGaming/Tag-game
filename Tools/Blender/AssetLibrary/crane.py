"""Simplified harbor crane. Mast 7.2 m, boom reaching 7 m over the water."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "HarborCrane",
        "Harbor",
        "Simplified crane. Base 2.2 m square, mast to 7.2 m, boom rises toward +Z and reaches about 7 m out. The hook cable is visual only.",
    )
    a.climb_note = "The mast is a 0.45 m lattice, not a flat cling wall."
    a.vault_note = "No rail. The boom is overhead."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 4)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.15, 0), (2.2, 0.30, 2.2), "Lib_Concrete", bevel=bev, segs=1, uv_scale=1.0)
        g.box((0, 0.45, 0), (1.5, 0.28, 1.5), "Lib_SteelDark", bevel=bev, segs=1)
        # Four mast chords.
        for x in (-0.28, 0.28):
            for z in (-0.28, 0.28):
                g.cylinder((x, 3.7, z), 0.045, 6.4, "Lib_MetalWorn", seg)
        if lod < 2:
            for y in (1.4, 2.6, 3.8, 5.0, 6.2):
                g.box((0, y, 0), (0.56, 0.04, 0.04), "Lib_SteelDark")
                g.box((0, y, 0), (0.04, 0.04, 0.56), "Lib_SteelDark")
        g.box((0, 6.55, 0.15), (0.7, 0.55, 0.7), "Lib_CraneYellow", bevel=bev, segs=1)
        if lod == 0:
            g.box((0, 6.55, 0.52), (0.40, 0.22, 0.02), "Lib_Glass")
        # Boom from the cab toward +Z, rising.
        g.box((0, 7.55, 3.3), (0.16, 0.16, 6.4), "Lib_CraneYellow", euler=(-24, 0, 0), bevel=bev, segs=1)
        g.box((0, 7.15, 3.3), (0.10, 0.10, 6.2), "Lib_MetalWorn", euler=(-24, 0, 0))
        if lod < 2:
            g.cylinder((0, 6.9, 0.55), 0.08, 0.2, "Lib_SteelDark", seg, axis="X")
            # Pendant from mast head to boom tip area.
            g.pipe((0, 7.15, -0.15), (0, 8.85, 6.2), 0.02, "Lib_SteelDark", 6)
        if lod == 0:
            g.pipe((0, 8.7, 6.15), (0, 5.4, 6.15), 0.012, "Lib_Steel", 5)
            g.box((0, 5.25, 6.15), (0.18, 0.12, 0.10), "Lib_SteelDark", bevel=0.003, segs=1)
        a.end()
    a.box("Col_Base", (0, 0.15, 0), (2.2, 0.30, 2.2))
    a.box("Col_Slew", (0, 0.45, 0), (1.5, 0.28, 1.5))
    for i, (x, z) in enumerate(((-0.28, -0.28), (0.28, -0.28), (-0.28, 0.28), (0.28, 0.28))):
        a.capsule("Col_Chord_%d" % i, (x, 3.7, z), 0.045, 6.4, 1)
    a.box("Col_Cab", (0, 6.55, 0.15), (0.68, 0.52, 0.68))
    a.box("Col_Boom", (0, 7.55, 3.3), (0.14, 0.14, 6.0), euler=(-24, 0, 0))
    a.box("Col_BoomLow", (0, 7.15, 3.3), (0.08, 0.08, 5.8), euler=(-24, 0, 0))
    return a
