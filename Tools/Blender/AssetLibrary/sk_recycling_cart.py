"""96-gallon recycling cart. Same shell as the trash cart, blue, with a lid slot.

RECYCLE is Liberation Sans (OFL).
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_trash_cart import _cart

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf",
))


@register
def create():
    a = Asset(
        "RecyclingCart_96",
        "StreetFurniture",
        "96-gallon recycling cart, 0.74 x 0.86 x 1.16 m. Lid slot. RECYCLE in Liberation Sans.",
    )
    a.climb_note = "Lid is not a cling."
    a.vault_note = "Lid is 1.12 m and small. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _cart(g, lod, "Lib_BoxBlue", "Lib_PaintBlue")
        g.box((0, 1.09, 0.08), (0.22, 0.008, 0.12), "Lib_Black")
        if lod == 0:
            g.text("RECYCLE", (0, 0.68, 0.368), 0.05, "Lib_PaintWhite", extrude=0.003, font=_FONT)
        a.end()
    a.box("Col_Body", (0, 0.54, 0.02), (0.50, 0.80, 0.52))
    a.box("Col_Lid", (0, 1.045, 0.02), (0.60, 0.04, 0.62))
    for i, x in enumerate((-0.34, 0.34)):
        a.box("Col_Wheel_%d" % i, (x, 0.125, -0.34), (0.03, 0.16, 0.16))
    return a
