"""Parking time sign. 12 x 18 in, bottom at 2.13 m.

Lettering is Liberation Sans. Not a regulatory clone.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf",
))


@register
def create():
    a = Asset(
        "Sign_Parking_2H",
        "StreetFurniture",
        "Parking sign 0.30 x 0.46 m, bottom at 2.13 m. Pole 60 mm. 2 HR PARKING in Liberation Sans.",
    )
    a.climb_note = "60 mm pole. Not a cling."
    a.vault_note = "Sign bottom is 2.13 m. No vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.015, 0), (0.20, 0.03, 0.20), "Lib_SteelDark", bevel=bev, segs=bs)
        g.cylinder((0, 1.30, 0), 0.030, 2.56, "Lib_Steel", seg)
        g.cylinder((0, 2.56, 0), 0.038, 0.04, "Lib_SteelDark", seg)
        g.box((0, 2.36, 0.028), (0.06, 0.10, 0.040), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 2.36, 0.055), (0.32, 0.48, 0.016), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, 2.50, 0.064), (0.28, 0.14, 0.006), "Lib_PaintRed")
        if lod == 0:
            g.text("PARKING", (0, 2.50, 0.070), 0.055, "Lib_PaintWhite", extrude=0.003, font=_FONT)
            g.text("2 HR", (0, 2.28, 0.070), 0.12, "Lib_Black", extrude=0.003, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.012, 0), (0.14, 0.014, 0.14))
    a.capsule("Col_Pole", (0, 1.05, 0), 0.018, 1.80)
    # Back half of the white plate, clear of the pole, the bracket, and the legend.
    a.box("Col_Sign", (0, 2.36, 0.052), (0.22, 0.36, 0.006))
    return a
