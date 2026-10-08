"""One-way sign. White blade, black field, OFL ONE WAY lettering and arrow."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import tri_plate

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf",
))


@register
def create():
    a = Asset(
        "Sign_OneWay",
        "StreetFurniture",
        "One-way blade 0.91 x 0.30 m at 2.45 m. ONE WAY and a black-field arrow, 6 cm pole.",
    )
    a.climb_note = "6 cm pole. Not a cling."
    a.vault_note = "Blade is a thin plate."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.025, 0), 0.08, 0.05, "Lib_SteelDark", seg, bevel=bev, segs=1)
        g.cylinder((0, 1.35, 0), 0.030, 2.65, "Lib_Steel", seg)
        g.box((0.10, 2.45, 0.055), (0.91, 0.30, 0.022), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((0.10, 2.45, 0.068), (0.84, 0.23, 0.006), "Lib_Black")
        g.box((-0.04, 2.395, 0.076), (0.42, 0.055, 0.006), "Lib_PaintWhite")
        tri_plate(g, (0.26, 2.395, 0.076), 0.16, 0.006, "Lib_PaintWhite", point="right")
        if lod == 0:
            g.text("ONE WAY", (0.10, 2.50, 0.086), 0.09, "Lib_PaintWhite", extrude=0.004, font=_FONT)
        g.pipe((0, 2.20, 0.02), (0.05, 2.32, 0.05), 0.014, "Lib_SteelDark", 6)
        if lod == 0:
            g.cylinder((0.10, 2.45, 0.03), 0.012, 0.03, "Lib_Steel", 6, axis="Z")
        a.end()
    a.box("Col_Base", (0, 0.025, 0), (0.12, 0.04, 0.12))
    a.capsule("Col_Pole", (0, 1.35, 0), 0.026, 2.58, 1)
    a.box("Col_Blade", (0.10, 2.45, 0.055), (0.86, 0.26, 0.018))
    return a
