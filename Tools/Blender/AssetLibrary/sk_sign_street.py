"""Street-name blades with legends. 9 in tall, bottoms at 2.74 m and 3.01 m.

MAIN faces +Z. 5TH faces +X. Lettering is Liberation Sans, not a city wordmark.
Each legend is one plate in front of the pole. Colliders sit in the wings.
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
        "Sign_StreetName",
        "StreetFurniture",
        "Street blades 0.90 x 0.23 m. Bottoms at 2.74 m and 3.01 m. Pole 60 mm. MAIN and 5TH in Liberation Sans.",
    )
    a.climb_note = "60 mm pole. Not a cling."
    a.vault_note = "Blades are thin and high."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.02, 0), (0.20, 0.04, 0.20), "Lib_SteelDark", bevel=bev, segs=bs)
        # Shaft runs from the base into the collar. Blades clamp onto it.
        g.cylinder((0, 1.60, 0), 0.030, 3.16, "Lib_Steel", seg)
        # Lower blade, long in X. Front face is +Z.
        g.box((0, 2.855, 0.045), (0.90, 0.23, 0.018), "Lib_PaintGreen", bevel=bev, segs=bs)
        g.box((0, 2.855, 0.055), (0.84, 0.19, 0.006), "Lib_PaintWhite")
        g.box((0, 2.855, 0.059), (0.78, 0.145, 0.004), "Lib_PaintGreen")
        # Straps bite the pole and the back of the blade.
        for y in (2.755, 2.955):
            g.box((0, y, 0.028), (0.08, 0.030, 0.028), "Lib_Steel", bevel=bev, segs=bs)
        # Collar and cap. The upper blade sits on the +X side of the shaft.
        g.cylinder((0, 3.125, 0), 0.050, 0.22, "Lib_PaintGreen", seg)
        g.cylinder((0, 3.28, 0), 0.036, 0.08, "Lib_PaintGreen", seg)
        g.box((0.050, 3.125, 0), (0.016, 0.23, 0.90), "Lib_PaintGreen", bevel=bev, segs=bs)
        g.box((0.059, 3.125, 0), (0.006, 0.19, 0.84), "Lib_PaintWhite")
        g.box((0.063, 3.125, 0), (0.004, 0.145, 0.78), "Lib_PaintGreen")
        for y in (3.045, 3.205):
            g.box((0.028, y, 0), (0.036, 0.028, 0.07), "Lib_Steel", bevel=bev, segs=bs)
        if lod == 0:
            g.text("MAIN", (0, 2.855, 0.060), 0.11, "Lib_PaintWhite", extrude=0.003, font=_FONT)
            g.text("5TH", (0.062, 3.125, 0), 0.11, "Lib_PaintWhite", extrude=0.003, yaw=90.0, font=_FONT)
        a.end()
    # Below the pole, inside the base plate.
    a.box("Col_Base", (0, 0.010, 0), (0.12, 0.012, 0.12))
    # Bare shaft, under the straps and the blades.
    a.capsule("Col_Pole", (0, 1.20, 0), 0.018, 2.20)
    # Wings of the lower green plate. Clear of the pole, the straps, and the border.
    a.box("Col_MainL", (-0.26, 2.855, 0.044), (0.32, 0.15, 0.006))
    a.box("Col_MainR", (0.26, 2.855, 0.044), (0.32, 0.15, 0.006))
    # Wings of the upper green plate, clear of the straps and the collar.
    a.box("Col_FifthS", (0.048, 3.125, -0.24), (0.006, 0.15, 0.32))
    a.box("Col_FifthN", (0.048, 3.125, 0.24), (0.006, 0.15, 0.32))
    return a
