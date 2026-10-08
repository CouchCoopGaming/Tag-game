"""Street-name blades. 9 in tall. MAIN faces ±Z. 5TH faces ±X.

Legends are Overpass Bold (OFL), a grotesque in the Highway Gothic family.
Each blade is a reflective green plate with a thin white border, on both faces.
The base is a slip-base anchor plate at the sidewalk.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "Overpass-Bold.ttf",
))


@register
def create():
    a = Asset(
        "Sign_StreetName",
        "StreetFurniture",
        "Street blades 0.91 x 0.23 m and 0.61 x 0.23 m. Bottoms at 2.74 m and 3.01 m. Slip base. MAIN and 5TH in Overpass.",
    )
    a.climb_note = "60 mm pole. Not a cling."
    a.vault_note = "Blades are thin and high."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.002, 0.0)
        bs = 1 if lod == 0 else 0
        # Slip base: anchor plate, four bolts, and a two-flange coupling.
        g.box((0, 0.006, 0), (0.22, 0.012, 0.22), "Lib_SteelDark", bevel=bev, segs=bs)
        for x in (-0.07, 0.07):
            for z in (-0.07, 0.07):
                g.cylinder((x, 0.016, z), 0.008, 0.012, "Lib_Steel", 6)
        g.cylinder((0, 0.028, 0), 0.055, 0.016, "Lib_Steel", seg)
        g.cylinder((0, 0.052, 0), 0.048, 0.020, "Lib_SteelDark", seg)
        g.cylinder((0, 1.64, 0), 0.030, 3.16, "Lib_Steel", seg)
        g.sphere((0, 3.22, 0), 0.038, "Lib_Steel", seg)
        g.cylinder((0, 2.98, 0), 0.046, 0.10, "Lib_SteelDark", seg)
        # MAIN, both faces. The plate is thick enough for an 8 mm collider inset.
        for zc, face in ((0.054, 1.0), (-0.054, -1.0)):
            g.box((0, 2.855, zc), (0.91, 0.229, 0.016), "Lib_SignGreen", bevel=bev, segs=bs)
            g.box((0, 2.855, zc + face * 0.009), (0.86, 0.198, 0.004), "Lib_PaintWhite")
            g.box((0, 2.855, zc + face * 0.012), (0.80, 0.150, 0.005), "Lib_SignGreen")
        for y in (2.78, 2.93):
            g.box((0, y, 0.038), (0.07, 0.028, 0.040), "Lib_Steel", bevel=bev, segs=bs)
            g.box((0, y, -0.038), (0.07, 0.028, 0.040), "Lib_Steel", bevel=bev, segs=bs)
        for xc, face in ((0.054, 1.0), (-0.054, -1.0)):
            g.box((xc, 3.125, 0), (0.016, 0.229, 0.61), "Lib_SignGreen", bevel=bev, segs=bs)
            g.box((xc + face * 0.009, 3.125, 0), (0.004, 0.198, 0.56), "Lib_PaintWhite")
            g.box((xc + face * 0.012, 3.125, 0), (0.005, 0.150, 0.50), "Lib_SignGreen")
        for y in (3.05, 3.20):
            g.box((0.038, y, 0), (0.040, 0.028, 0.07), "Lib_Steel", bevel=bev, segs=bs)
            g.box((-0.038, y, 0), (0.040, 0.028, 0.07), "Lib_Steel", bevel=bev, segs=bs)
        if lod == 0:
            g.text("MAIN", (0, 2.855, 0.070), 0.10, "Lib_PaintWhite", extrude=0.002, font=_FONT)
            g.text("MAIN", (0, 2.855, -0.070), 0.10, "Lib_PaintWhite", extrude=0.002, yaw=180.0, font=_FONT)
            g.text("5TH", (0.070, 3.125, 0), 0.09, "Lib_PaintWhite", extrude=0.002, yaw=90.0, font=_FONT)
            g.text("5TH", (-0.070, 3.125, 0), 0.09, "Lib_PaintWhite", extrude=0.002, yaw=-90.0, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.005, 0), (0.12, 0.006, 0.12))
    a.capsule("Col_Pole", (0, 1.30, 0), 0.018, 2.30)
    # Wings of each plate, clear of the pole, the saddles, the border, and the legends.
    a.box("Col_MainF", (0.30, 2.855, 0.054), (0.24, 0.14, 0.008))
    a.box("Col_MainB", (-0.30, 2.855, -0.054), (0.24, 0.14, 0.008))
    a.box("Col_FifthF", (0.054, 3.125, 0.18), (0.008, 0.14, 0.14))
    a.box("Col_FifthB", (-0.054, 3.125, -0.18), (0.008, 0.14, 0.14))
    return a
