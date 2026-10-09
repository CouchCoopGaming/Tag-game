"""Parking meter. Head at 1.15 m on a 5 cm post."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("ParkingMeter", "StreetFurniture", "Single-space meter. Head 1.15–1.45 m, coin window, 5 cm post.")
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = 0.004 if lod == 0 else 0
        g.cylinder((0, 0.55, 0), 0.025, 1.10, "Lib_SteelDark", seg)
        g.box((0, 1.28, 0), (0.22, 0.32, 0.16), "Lib_Steel", bevel=bev, segs=1)
        g.cylinder((0, 1.44, 0), 0.09, 0.16, "Lib_SteelDark", seg, axis="Z")
        g.box((0, 1.30, 0.082), (0.12, 0.08, 0.008), "Lib_Glass")
        g.box((0, 1.18, 0.085), (0.10, 0.04, 0.012), "Lib_Black")
        if lod == 0:
            g.cylinder((0, 1.22, 0.09), 0.012, 0.01, "Lib_Brass", 6, axis="Z")
        a.end()
    a.capsule("Col_Post", (0, 0.55, 0), 0.025, 1.10, 1)
    a.box("Col_Head", (0, 1.30, 0), (0.22, 0.36, 0.18))
    return a
