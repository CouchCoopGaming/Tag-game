"""Street-name blade. 'MAPLE ST' at 2.55 m on a 3 m pole."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Sign_Street", "StreetFurniture", "Street blade 'MAPLE ST' at 2.55 m. Pole is 3.0 m, 6 cm tube.")
    a.climb_note = "Pole is a 6 cm tube. Not a cling wall."
    a.vault_note = "Blade is overhead. No vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.cylinder((0, 1.5, 0), 0.03, 3.0, "Lib_Steel", seg)
        g.box((0, 2.55, 0.02), (0.92, 0.22, 0.025), "Lib_PaintGreen", bevel=0.004 if lod == 0 else 0, segs=1)
        g.box((0, 2.55, 0.034), (0.86, 0.16, 0.006), "Lib_PaintWhite")
        if lod == 0:
            g.text("MAPLE ST", (0, 2.55, 0.042), 0.07, "Lib_Black", extrude=0.003)
        a.end()
    a.capsule("Col_Pole", (0, 1.5, 0), 0.03, 3.0, 1)
    a.box("Col_Blade", (0, 2.55, 0.02), (0.92, 0.22, 0.04))
    return a
