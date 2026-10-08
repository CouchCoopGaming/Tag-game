"""36-inch yield triangle. Red border, white field, OFL YIELD lettering."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import tri_plate

SIDE = 0.91
CY = 2.62
_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf",
))


@register
def create():
    a = Asset(
        "Sign_Yield",
        "StreetFurniture",
        "36-inch yield triangle. Point down, bottom of the sign at 2.10 m, pole 3.05 m.",
    )
    a.climb_note = "6 cm pole. Not a cling."
    a.vault_note = "Sign is a thin plate at 2.6 m."
    h = SIDE * math.sqrt(3.0) / 2.0
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.03, 0), 0.09, 0.06, "Lib_SteelDark", seg, bevel=bev, segs=1)
        g.cylinder((0, 1.52, 0), 0.030, 2.98, "Lib_Steel", seg)
        g.pipe((0, 2.35, 0.03), (0, 2.55, 0.05), 0.016, "Lib_SteelDark", 6)
        tri_plate(g, (0, CY, 0.055), SIDE, 0.022, "Lib_PaintRed", bevel=bev, segs=1 if lod == 0 else 0)
        tri_plate(g, (0, CY + 0.01, 0.070), SIDE * 0.72, 0.010, "Lib_PaintWhite")
        if lod == 0:
            g.cylinder((0, CY - 0.02, 0.04), 0.012, 0.04, "Lib_Steel", 6, axis="Z")
            g.text("YIELD", (0, 2.72, 0.086), 0.18, "Lib_PaintRed", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.03, 0), (0.14, 0.048, 0.14))
    a.capsule("Col_Pole", (0, 1.52, 0), 0.026, 2.90, 1)
    # Upper field of the triangle. The point is thinner than a useful box.
    a.box("Col_Sign", (0, CY + 0.06, 0.055), (0.42, 0.32, 0.016))
    a.box("Col_SignTip", (0, CY - 0.26, 0.055), (0.12, 0.18, 0.014))
    return a
