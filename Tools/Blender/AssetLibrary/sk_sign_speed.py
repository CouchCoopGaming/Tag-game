"""SPEED LIMIT sign. White 24 x 30 in panel, bottom near 1.52 m.

Lettering is Liberation Sans (OFL). Not a regulatory clone of a trademarked face.
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
        "Sign_Speed_25",
        "StreetFurniture",
        "Speed sign 0.61 x 0.76 m, bottom at 1.52 m. Pole 60 mm OD. SPEED LIMIT 25 in Liberation Sans.",
    )
    a.climb_note = "Pole is 60 mm. Not a cling."
    a.vault_note = "Sign bottom is 1.52 m. No vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.015, 0), (0.22, 0.03, 0.22), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            for x in (-0.07, 0.07):
                for z in (-0.07, 0.07):
                    g.cylinder((x, 0.034, z), 0.008, 0.012, "Lib_Steel", 6)
        g.cylinder((0, 1.20, 0), 0.030, 2.34, "Lib_Steel", seg, bevel=bev, segs=bs)
        g.cylinder((0, 2.40, 0), 0.034, 0.03, "Lib_SteelDark", seg)
        g.box((0, 1.90, 0.02), (0.08, 0.10, 0.06), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 1.90, 0.055), (0.64, 0.80, 0.016), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, 1.90, 0.064), (0.58, 0.74, 0.006), "Lib_PaintWhite")
        if lod == 0:
            g.text("SPEED", (0, 2.12, 0.072), 0.07, "Lib_Black", extrude=0.003, font=_FONT)
            g.text("LIMIT", (0, 2.02, 0.072), 0.07, "Lib_Black", extrude=0.003, font=_FONT)
            g.text("25", (0, 1.78, 0.074), 0.22, "Lib_Black", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.16, 0.02, 0.16))
    a.capsule("Col_Pole", (0, 1.10, 0), 0.022, 2.00, 1)
    a.box("Col_Sign", (0, 1.90, 0.052), (0.50, 0.64, 0.010))
    return a
