"""Lidded public trash can. Domed lid, side door, 1.05 m tall."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("TrashCan_Lidded", "StreetFurniture", "Lidded park can, 1.05 m to the crown, domed lid and a side deposit door.")
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 10)
        g.cylinder((0, 0.40, 0), 0.22, 0.76, "Lib_SteelDark", seg, bevel=0.003 if lod == 0 else 0, segs=1)
        g.cylinder((0, 0.80, 0), 0.24, 0.05, "Lib_Steel", seg)
        g.cylinder((0, 0.77, 0), 0.205, 0.035, "Lib_Rubber", seg)
        g.cylinder((0, 0.04, 0), 0.24, 0.06, "Lib_Steel", seg)
        g.sphere((0, 0.90, 0), 0.20, "Lib_Steel", seg)
        g.cylinder((0, 1.02, 0), 0.03, 0.06, "Lib_SteelDark", 8)
        # Deposit door on +Z, flush in a recessed frame.
        g.box((0, 0.48, 0.21), (0.28, 0.18, 0.02), "Lib_Steel", bevel=0.003 if lod == 0 else 0, segs=1)
        g.box((0, 0.48, 0.222), (0.22, 0.10, 0.008), "Lib_Rubber")
        if lod == 0:
            g.torus((0, 0.55, 0.23), 0.045, 0.006, "Lib_Steel", 10, 6)
        a.end()
    a.capsule("Col_Body", (0, 0.50, 0), 0.21, 0.96, 1)
    a.box("Col_Door", (0, 0.48, 0.21), (0.28, 0.18, 0.03))
    return a
