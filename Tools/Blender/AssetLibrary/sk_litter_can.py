"""Street litter can. About 32 gal, with a rain lid.

Body, belts, and lid are separate solids. Each collider sits in one of them.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "LitterCan_Street",
        "StreetFurniture",
        "Litter can, 0.56 m across and 1.02 m to the lid. Steel body, two belts, rain cap.",
    )
    a.climb_note = "Smooth body. Not a cling."
    a.vault_note = "Lid is 1.02 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.02, 0), 0.30, 0.04, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.48, 0), 0.26, 0.90, "Lib_PaintGreen", seg)
        for y in (0.28, 0.62):
            g.cylinder((0, y, 0), 0.278, 0.028, "Lib_Steel", seg)
        g.cylinder((0, 0.935, 0), 0.29, 0.04, "Lib_Steel", seg)
        # Lid bites the rim. The crown is the stand surface.
        g.cylinder((0, 0.975, 0), 0.33, 0.09, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 1.03, 0), 0.04, 0.03, "Lib_Steel", 8)
        if lod == 0:
            g.box((0, 0.55, 0.262), (0.22, 0.10, 0.016), "Lib_SteelDark")
            g.box((0, 0.55, 0.272), (0.16, 0.012, 0.008), "Lib_Black")
        a.end()
    # Base only, under the body.
    a.box("Col_Base", (0, 0.012, 0), (0.32, 0.014, 0.32))
    # Body band between the two belts.
    a.box("Col_Body", (0, 0.45, 0), (0.32, 0.22, 0.32))
    # Lid above the rim, 1 cm under the crown.
    a.box("Col_Lid", (0, 1.004, 0), (0.40, 0.012, 0.40))
    return a
