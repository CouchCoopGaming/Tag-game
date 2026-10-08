"""Sidewalk ATM cabinet. About 28 x 18 x 60 in on a pad.

Screen, keypad, and card slot face +Z. Lettering is Liberation Sans.
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
        "Kiosk_ATM",
        "StreetFurniture",
        "ATM cabinet 0.72 x 0.46 x 1.40 m on a 60 mm pad. Screen faces +Z. ATM in Liberation Sans.",
    )
    a.climb_note = "Smooth cabinet. Not a cling wall."
    a.vault_note = "Top is 1.48 m. Above a comfortable vault, and the hood is small."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.03, 0), (0.96, 0.06, 0.70), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.6)
        g.box((0, 0.72, 0), (0.72, 1.36, 0.46), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 1.42, 0), (0.78, 0.12, 0.52), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 0.98, 0.236), (0.36, 0.28, 0.016), "Lib_Window")
        g.box((0, 0.74, 0.238), (0.22, 0.10, 0.014), "Lib_Black")
        if lod == 0:
            g.box((0, 1.22, 0.240), (0.28, 0.06, 0.012), "Lib_PaintBlue")
            g.text("ATM", (0, 1.22, 0.252), 0.045, "Lib_PaintWhite", extrude=0.003, font=_FONT)
            g.box((0.22, 0.78, 0.240), (0.06, 0.018, 0.012), "Lib_Brass")
            g.box((-0.16, 0.18, 0.236), (0.16, 0.05, 0.012), "Lib_Rust")
            for row in range(3):
                for col in range(3):
                    g.box((-0.05 + col * 0.05, 0.70 + row * 0.035, 0.246), (0.028, 0.018, 0.008), "Lib_Steel")
        a.end()
    a.box("Col_Pad", (0, 0.02, 0), (0.84, 0.028, 0.58))
    a.box("Col_Body", (0, 0.74, 0), (0.56, 1.10, 0.32))
    a.box("Col_Hood", (0, 1.448, 0), (0.60, 0.044, 0.38))
    return a
