"""Sidewalk electrical cabinet.

Real size: about 30 x 16 x 52 in on a 4 in pad (0.76 x 0.42 x 1.38 m overall).
Door, louvers, hasp, and a conduit stub into the pad.
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
        "Cabinet_Electrical",
        "StreetFurniture",
        "Electrical cabinet 0.76 x 0.42 x 1.32 m on a 0.06 m pad. Door faces +Z.",
    )
    a.climb_note = "Smooth painted steel. Not a cling wall."
    a.vault_note = "Top is 1.38 m. Above the vault band, and the top is small."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.03, 0), (0.98, 0.06, 0.64), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.7)
        g.box((0, 0.72, 0), (0.72, 1.28, 0.36), "Lib_PaintGreen", bevel=bev, segs=bs)
        # Door overlaps the face.
        g.box((0.01, 0.74, 0.188), (0.62, 1.12, 0.022), "Lib_BoxGreen", bevel=bev, segs=bs)
        g.cylinder((-0.33, 0.74, 0.19), 0.012, 1.08, "Lib_Steel", 6)
        if lod == 0:
            for i in range(5):
                g.box((0.02, 0.42 + i * 0.09, 0.204), (0.36, 0.012, 0.010), "Lib_SteelDark")
            g.box((0.24, 0.78, 0.208), (0.028, 0.14, 0.016), "Lib_Steel")
            g.box((0.24, 0.70, 0.206), (0.05, 0.03, 0.012), "Lib_SteelDark")
            g.cylinder((-0.22, 0.12, -0.16), 0.028, 0.18, "Lib_Steel", seg)
            g.box((0, 0.10, 0.16), (0.40, 0.06, 0.02), "Lib_Rust")
            g.box((-0.08, 1.10, 0.206), (0.16, 0.08, 0.006), "Lib_PaintYellow")
            g.text("HV", (-0.08, 1.10, 0.214), 0.04, "Lib_Black", extrude=0.003, font=_FONT)
        g.box((0, 1.38, 0), (0.76, 0.04, 0.40), "Lib_SteelDark", bevel=bev, segs=bs)
        a.end()
    a.box("Col_Pad", (0, 0.03, 0), (0.86, 0.04, 0.52))
    a.box("Col_Box", (0, 0.72, 0), (0.60, 1.12, 0.28))
    # Crown is 1.40 m. Collider top is 0.7 cm under it.
    a.box("Col_Top", (0, 1.372, 0), (0.64, 0.028, 0.30))
    return a
