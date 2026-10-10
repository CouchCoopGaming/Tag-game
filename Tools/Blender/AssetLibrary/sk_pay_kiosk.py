"""Lot pay kiosk. A cabinet with a screen, not a parking meter."""

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
        "PayKiosk_Lot",
        "StreetFurniture",
        "Pay cabinet, 0.46 m wide and 1.32 m tall. Screen, card slot, and PAY in Liberation Sans.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.03, 0), (0.52, 0.06, 0.42), "Lib_SteelDark", bevel=bev, segs=bs)
        # Cabinet overlaps the foot so the joint is not a floating seam.
        g.box((0, 0.62, 0), (0.42, 1.20, 0.34), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 1.24, 0), (0.46, 0.10, 0.38), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 1.02, 0.176), (0.24, 0.16, 0.010), "Lib_Glass")
        g.box((0, 0.86, 0.176), (0.22, 0.012, 0.008), "Lib_Black")
        g.box((0, 0.78, 0.176), (0.16, 0.08, 0.010), "Lib_PaintCream")
        if lod == 0:
            g.text("PAY", (0, 1.18, 0.198), 0.07, "Lib_PaintWhite", extrude=0.004, font=_FONT)
            for row, y in enumerate((0.76, 0.80)):
                for col, x in enumerate((-0.04, 0.0, 0.04)):
                    g.box((x, y, 0.184), (0.022, 0.016, 0.006), "Lib_Steel")
        a.end()
    a.box("Col_Foot", (0, 0.012, 0), (0.34, 0.016, 0.26))
    a.box("Col_Body", (0, 0.62, 0), (0.28, 0.96, 0.22))
    a.box("Col_Hood", (0, 1.255, 0), (0.30, 0.04, 0.24))
    return a
