"""Sidewalk valve box. Cover sits on a concrete pad.

The cover is the stand surface. The lift lug is on the rim, clear of the lid collider.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "ValveBox_Walk",
        "StreetFurniture",
        "Valve box. Pad 0.46 m square, cover 0.28 m, crown at 0.05 m.",
    )
    a.climb_note = "Cover is 5 cm. Not a climb."
    a.vault_note = "Too low to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = lod_pick(lod, 0.002, 0.0)
        g.box((0, 0.018, 0), (0.46, 0.036, 0.46), "Lib_Concrete", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=0.5)
        g.cylinder((0, 0.042, 0), 0.140, 0.024, "Lib_SteelDark", seg)
        if lod == 0:
            g.box((0.112, 0.050, 0), (0.024, 0.010, 0.014), "Lib_Steel")
        a.end()
    a.box("Col_Pad", (0, 0.012, 0), (0.34, 0.016, 0.34))
    # Cover crown is 0.054. Top sits about 8 mm under it, clear of the lug.
    a.box("Col_Cover", (0, 0.042, 0), (0.14, 0.012, 0.14))
    return a
