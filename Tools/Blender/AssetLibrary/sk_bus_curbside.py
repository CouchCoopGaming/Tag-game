"""Curbside bus stop. Two posts, a glass screen, a bench, and a roof. Not the city shelter."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "BusStop_Curbside",
        "StreetFurniture",
        "Curbside stop, 2.40 m wide. Two posts, glass screen, slat bench, roof at 2.28 m.",
    )
    a.climb_note = "Posts are 8 cm. The roof is a landing, not a cling."
    a.vault_note = "Roof edge is 2.28 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        for x in (-1.05, 1.05):
            g.box((x, 0.02, -0.34), (0.16, 0.04, 0.16), "Lib_SteelDark", bevel=bev, segs=bs)
            g.box((x, 1.12, -0.34), (0.08, 2.20, 0.08), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 2.24, -0.20), (2.40, 0.07, 1.15), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 2.29, -0.70), (2.40, 0.035, 0.08), "Lib_Steel")
        # Screen frame. Glass sits in the frame, not through the posts.
        g.box((0, 1.15, -0.40), (1.90, 0.045, 0.04), "Lib_Steel")
        g.box((0, 2.05, -0.40), (1.90, 0.045, 0.04), "Lib_Steel")
        g.box((-0.92, 1.60, -0.40), (0.04, 0.86, 0.04), "Lib_Steel")
        g.box((0.92, 1.60, -0.40), (0.04, 0.86, 0.04), "Lib_Steel")
        g.box((0, 1.60, -0.40), (0.035, 0.86, 0.035), "Lib_Steel")
        g.box((-0.46, 1.60, -0.388), (0.84, 0.78, 0.012), "Lib_Glass")
        g.box((0.46, 1.60, -0.388), (0.84, 0.78, 0.012), "Lib_Glass")
        # Bench. Legs reach the ground.
        for x in (-0.62, 0.62):
            g.box((x, 0.22, 0.13), (0.04, 0.44, 0.04), "Lib_Steel", bevel=bev, segs=bs)
            g.box((x, 0.22, 0.40), (0.04, 0.44, 0.04), "Lib_Steel", bevel=bev, segs=bs)
        g.box((0, 0.46, 0.26), (1.55, 0.045, 0.40), "Lib_Wood", uv_scale=1.3, bevel=bev, segs=bs)
        g.box((0, 0.72, 0.06), (1.55, 0.42, 0.04), "Lib_Wood", uv_scale=1.3, bevel=bev, segs=bs)
        if lod == 0:
            for i, x in enumerate((-0.48, -0.16, 0.16, 0.48)):
                g.box((x, 0.46, 0.26), (0.06, 0.012, 0.36), "Lib_WoodDark")
        a.end()
    a.box("Col_PostL", (-1.05, 1.05, -0.34), (0.05, 1.90, 0.05))
    a.box("Col_PostR", (1.05, 1.05, -0.34), (0.05, 1.90, 0.05))
    a.box("Col_Roof", (0, 2.24, -0.20), (2.10, 0.04, 0.90))
    a.box("Col_Seat", (0, 0.46, 0.26), (1.30, 0.03, 0.28))
    a.box("Col_Back", (0, 0.70, 0.06), (1.30, 0.28, 0.025))
    return a
