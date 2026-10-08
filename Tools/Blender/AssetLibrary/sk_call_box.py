"""Pedestal emergency call box. Not a meter and not the pay kiosk."""

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
        "CallBox_Pedestal",
        "StreetFurniture",
        "Call box. Post to 1.05 m, head 1.05–1.45 m. HELP in Liberation Sans.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.015, 0), (0.22, 0.03, 0.22), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.54, 0), 0.035, 1.02, "Lib_Steel", seg)
        g.box((0, 1.24, 0), (0.24, 0.40, 0.16), "Lib_PaintBlue", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 1.30, 0.084), (0.14, 0.16, 0.008), "Lib_SteelDark")
        g.cylinder((0, 1.14, 0.092), 0.016, 0.012, "Lib_PaintRed", 8, axis="Z")
        if lod == 0:
            g.text("HELP", (0, 1.36, 0.092), 0.06, "Lib_PaintWhite", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.14, 0.02, 0.14))
    a.capsule("Col_Post", (0, 0.52, 0), 0.026, 0.88, 1)
    a.box("Col_Head", (0, 1.24, 0), (0.16, 0.28, 0.10))
    return a
