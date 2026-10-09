"""Pond coping. Stone lip, soil bank behind it. Water sits on the -Z side."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "PondEdge",
        "Park",
        "4 m pond coping. Stone lip faces -Z (the water). Soil bank is on +Z. Top of the lip is 0.22 m.",
    )
    a.loose_pivot = True
    a.climb_note = "The lip is 0.22 m. Not a cling wall."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.11, 0.21), (4.0, 0.22, 0.40), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0, 0.08, 0.80), (3.92, 0.16, 0.68), "Lib_Soil", uv_scale=0.8)
        if lod == 0:
            g.box((0, 0.232, 0.21), (3.7, 0.012, 0.08), "Lib_Mortar")
        rocks = lod_pick(lod, 4, 2)
        for i in range(rocks):
            x = -1.35 + i * 0.9
            g.box((x, 0.23, 0.76), (0.26, 0.12, 0.20), "Lib_Concrete", bevel=bev, segs=1)
        reeds = lod_pick(lod, 6, 0)
        for i in range(reeds):
            x = -1.6 + i * 0.64
            g.cylinder((x, 0.36, -0.08), 0.008, 0.72, "Lib_FoliageDark", 5)
            g.cone((x, 0.82, -0.08), 0.018, 0.004, 0.14, "Lib_Foliage", 5)
        a.end()
    a.box("Col_Lip", (0, 0.11, 0.21), (3.84, 0.16, 0.32))
    a.box("Col_Bank", (0, 0.08, 0.80), (3.72, 0.12, 0.56))
    return a
