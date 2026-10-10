"""A-frame menu board. Two leaning panels that meet the ground."""

import math
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
        "MenuBoard_Aframe",
        "StreetFurniture",
        "A-frame board, 0.60 m wide and 0.86 m tall. MENU in Liberation Sans.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    lean = 19.0
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.002, 0.0)
        # Bottoms at z=±0.28 on the ground, tops meet near z=0.
        g.box((0, 0.41, 0.16), (0.56, 0.86, 0.020), "Lib_Wood", euler=(-lean, 0, 0), bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.41, -0.16), (0.56, 0.86, 0.020), "Lib_Wood", euler=(lean, 0, 0), bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.82, 0), (0.08, 0.03, 0.08), "Lib_SteelDark")
        for x in (-0.24, 0.24):
            g.box((x, 0.015, 0.30), (0.06, 0.03, 0.08), "Lib_SteelDark")
            g.box((x, 0.015, -0.30), (0.06, 0.03, 0.08), "Lib_SteelDark")
        if lod == 0:
            g.text("MENU", (0, 0.48, 0.20), 0.08, "Lib_PaintWhite", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Front", (0, 0.40, 0.16), (0.40, 0.64, 0.012), euler=(-lean, 0, 0))
    a.box("Col_Back", (0, 0.40, -0.16), (0.40, 0.64, 0.012), euler=(lean, 0, 0))
    return a
