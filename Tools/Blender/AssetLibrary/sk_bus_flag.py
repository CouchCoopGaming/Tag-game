"""Bus stop flag on its own pole. Not the shelter."""

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
        "BusFlag_Stop",
        "StreetFurniture",
        "Bus flag. Pole to 2.40 m, blade 0.46 m wide. BUS in Liberation Sans.",
    )
    a.climb_note = "Pole is 6 cm. Not a cling."
    a.vault_note = "Blade is 2.35 m. Too high to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.015, 0), (0.20, 0.03, 0.20), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 1.20, 0), 0.030, 2.34, "Lib_Steel", seg)
        g.box((0.28, 2.32, 0), (0.46, 0.22, 0.025), "Lib_PaintYellow", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0.08, 2.32, 0), (0.04, 0.08, 0.04), "Lib_Steel")
        if lod == 0:
            g.text("BUS", (0.30, 2.32, 0.020), 0.09, "Lib_Black", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.12, 0.02, 0.12))
    a.capsule("Col_Pole", (0, 1.10, 0), 0.022, 2.00, 1)
    a.box("Col_Blade", (0.30, 2.32, 0), (0.32, 0.14, 0.016))
    return a
