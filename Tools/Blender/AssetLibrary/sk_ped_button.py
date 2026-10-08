"""Pedestrian pushbutton. Button center at 1.07 m.

The housing bites the pole. The button bites the housing.
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
        "PedButton_Post",
        "StreetFurniture",
        "Pushbutton pole. Button center at 1.07 m. Pole 90 mm. PUSH in Liberation Sans.",
    )
    a.climb_note = "90 mm pole. Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.02, 0), (0.22, 0.04, 0.22), "Lib_SteelDark", bevel=bev, segs=bs)
        g.cylinder((0, 0.68, 0), 0.045, 1.32, "Lib_Steel", seg)
        g.sphere((0, 1.36, 0), 0.052, "Lib_Steel", seg)
        g.box((0, 1.07, 0.055), (0.14, 0.20, 0.07), "Lib_PaintYellow", bevel=bev, segs=bs)
        g.cylinder((0, 1.05, 0.096), 0.028, 0.016, "Lib_Black", 10, axis="Z")
        g.box((0, 1.20, 0.052), (0.16, 0.11, 0.012), "Lib_PaintWhite")
        if lod == 0:
            g.text("PUSH", (0, 1.20, 0.058), 0.045, "Lib_Black", extrude=0.003, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.010, 0), (0.14, 0.012, 0.14))
    a.capsule("Col_Pole", (0, 0.48, 0), 0.028, 0.80)
    # Front of the housing, clear of the pole and the button.
    a.box("Col_Head", (0, 1.07, 0.062), (0.08, 0.12, 0.016))
    return a
