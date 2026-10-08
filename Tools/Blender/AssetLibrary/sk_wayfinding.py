"""Wayfinding pylon. A tall map board, not a street sign blade."""

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
        "Wayfinding_Pylon",
        "StreetFurniture",
        "Map pylon, 0.42 m wide and 2.15 m tall. MAP in Liberation Sans.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too smooth to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.03, 0), (0.50, 0.06, 0.28), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 1.06, 0), (0.32, 2.06, 0.10), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 2.12, 0), (0.38, 0.08, 0.14), "Lib_SteelDark")
        g.box((0, 1.35, 0.056), (0.24, 0.55, 0.008), "Lib_PaintCream")
        if lod == 0:
            g.text("MAP", (0, 1.52, 0.068), 0.08, "Lib_PaintWhite", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Foot", (0, 0.014, 0), (0.32, 0.018, 0.16))
    a.box("Col_Post", (0, 1.05, 0), (0.20, 1.70, 0.06))
    return a
