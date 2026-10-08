"""Sidewalk A-frame. Boards 0.56 m wide, about 0.85 m tall.

The hinge bar ties the two boards. Each collider stays inside its own board.
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
        "Sign_AFrame",
        "StreetFurniture",
        "A-frame 0.56 m wide and 0.86 m tall. OPEN in Liberation Sans on the street face.",
    )
    a.climb_note = "Smooth board. Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.42, 0.16), (0.56, 0.80, 0.022), "Lib_PaintWhite", bevel=bev, segs=bs, euler=(14, 0, 0))
        g.box((0, 0.42, -0.16), (0.56, 0.80, 0.022), "Lib_PaintWhite", bevel=bev, segs=bs, euler=(-14, 0, 0))
        g.cylinder((0, 0.80, 0), 0.018, 0.58, "Lib_Steel", 8, axis="X")
        g.box((0.26, 0.02, 0.28), (0.06, 0.04, 0.08), "Lib_SteelDark")
        g.box((-0.26, 0.02, 0.28), (0.06, 0.04, 0.08), "Lib_SteelDark")
        g.box((0.26, 0.02, -0.28), (0.06, 0.04, 0.08), "Lib_SteelDark")
        g.box((-0.26, 0.02, -0.28), (0.06, 0.04, 0.08), "Lib_SteelDark")
        if lod == 0:
            g.text("OPEN", (0, 0.46, 0.22), 0.10, "Lib_Black", extrude=0.003, font=_FONT)
        a.end()
    a.box("Col_Front", (0, 0.42, 0.16), (0.40, 0.56, 0.010), euler=(14, 0, 0))
    a.box("Col_Back", (0, 0.42, -0.16), (0.40, 0.56, 0.010), euler=(-14, 0, 0))
    return a
