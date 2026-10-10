"""Curb-opening storm inlet. 1.50 m of curb, gutter grate, open throat."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "StormDrain_Curb",
        "StreetFurniture",
        "Curb inlet 1.50 m long. Curb is 0.15 m, throat 0.56 m wide, gutter grate 0.55 x 0.32 m.",
    )
    a.climb_note = "Curb is 15 cm. Not a wall."
    a.vault_note = "Too low."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        # Sidewalk, curb, and gutter overlap a couple of centimetres so the joint is solid.
        g.box((0, 0.09, 0.26), (1.50, 0.18, 0.44), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.8)
        for x in (-0.52, 0.52):
            g.box((x, 0.075, 0.02), (0.44, 0.15, 0.16), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.8)
        g.box((0, 0.125, 0.02), (0.66, 0.05, 0.16), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.8)
        g.box((0, 0.055, 0.08), (0.52, 0.07, 0.06), "Lib_Black")
        g.box((0, 0.025, -0.25), (1.50, 0.05, 0.46), "Lib_Concrete", uv_scale=0.7)
        g.box((0, 0.058, -0.26), (0.58, 0.014, 0.32), "Lib_SteelDark", bevel=bev, segs=1)
        bars = 7 if lod == 0 else 4
        for i in range(bars):
            x = -0.22 + i * (0.44 / (bars - 1))
            g.box((x, 0.072, -0.26), (0.016, 0.012, 0.26), "Lib_Steel", bevel=0.002 if lod == 0 else 0, segs=1)
        if lod == 0:
            g.box((0, 0.074, -0.26), (0.46, 0.008, 0.014), "Lib_SteelDark")
        a.end()
    a.box("Col_Walk", (0, 0.09, 0.32), (1.42, 0.14, 0.28))
    a.box("Col_CurbL", (-0.52, 0.07, 0.01), (0.36, 0.10, 0.04))
    a.box("Col_CurbR", (0.52, 0.07, 0.01), (0.36, 0.10, 0.04))
    a.box("Col_Lintel", (0, 0.132, 0.00), (0.48, 0.024, 0.04))
    a.box("Col_Gutter", (0, 0.024, -0.32), (1.40, 0.032, 0.28))
    a.box("Col_Grate", (0, 0.056, -0.26), (0.50, 0.008, 0.26))
    return a
