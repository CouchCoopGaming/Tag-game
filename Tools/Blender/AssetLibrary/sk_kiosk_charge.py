"""Sidewalk phone and charging pillar. Narrower than a Link kiosk, 2.40 m tall.

Screen and a handset shelf face +Z. CHARGE is Liberation Sans.
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
        "Kiosk_Charge",
        "StreetFurniture",
        "Charging pillar 0.38 x 0.24 x 2.40 m. Screen and shelf face +Z. CHARGE in Liberation Sans.",
    )
    a.climb_note = "Narrow pillar. Not a cling wall."
    a.vault_note = "Top is 2.48 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        seg = lod_pick(lod, 10, 6)
        g.box((0, 0.03, 0), (0.56, 0.06, 0.42), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.6)
        g.box((0, 1.21, 0), (0.36, 2.34, 0.22), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 2.40, 0), (0.42, 0.10, 0.28), "Lib_Steel", bevel=bev, segs=bs)
        g.cylinder((0, 2.50, 0), 0.012, 0.12, "Lib_Steel", seg)
        g.box((0, 1.45, 0.116), (0.24, 0.42, 0.014), "Lib_WindowLit")
        g.box((0, 1.05, 0.130), (0.28, 0.025, 0.10), "Lib_Steel")
        if lod == 0:
            g.text("CHARGE", (0, 1.78, 0.132), 0.04, "Lib_PaintWhite", extrude=0.003, font=_FONT)
            g.box((0.08, 1.02, 0.150), (0.04, 0.03, 0.02), "Lib_Black")
            g.cylinder((0.08, 0.98, 0.145), 0.008, 0.08, "Lib_Steel", 6)
            g.box((-0.08, 0.40, 0.114), (0.12, 0.06, 0.010), "Lib_Rust")
        a.end()
    a.box("Col_Pad", (0, 0.02, 0), (0.44, 0.028, 0.30))
    a.box("Col_Body", (0, 1.16, 0), (0.24, 1.90, 0.12))
    # Cap crown is 2.45. Antenna occupies the center above 2.44, so the colliders sit to either side.
    a.box("Col_CapL", (-0.14, 2.428, 0), (0.14, 0.024, 0.14))
    a.box("Col_CapR", (0.14, 2.428, 0), (0.14, 0.024, 0.14))
    return a
