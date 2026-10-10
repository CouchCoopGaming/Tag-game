"""Wood picnic table with attached benches.

Real size: 6 ft top (1.83 x 0.71 m) at 0.76 m. Benches at 0.43 m.
Legs are one frame, so nothing sits loose.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "PicnicTable_Wood",
        "StreetFurniture",
        "Picnic table, top 1.83 x 0.71 m at 0.76 m. Benches at 0.43 m.",
    )
    a.climb_note = "Table edge is 0.76 m. Not a cling wall."
    a.vault_note = "Top is 0.76 m, under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.74, 0), (1.83, 0.04, 0.71), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.2)
        if lod == 0:
            for z in (-0.22, 0.0, 0.22):
                g.box((0, 0.762, z), (1.72, 0.006, 0.008), "Lib_WoodDark")
        for z in (-0.55, 0.55):
            g.box((0, 0.43, z), (1.70, 0.032, 0.28), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.3)
        # A-frame legs, overlapping the top and the benches.
        for x in (-0.70, 0.70):
            g.box((x, 0.38, 0), (0.06, 0.72, 0.06), "Lib_WoodDark", bevel=bev, segs=bs)
            g.box((x, 0.38, -0.55), (0.05, 0.42, 0.05), "Lib_WoodDark")
            g.box((x, 0.38, 0.55), (0.05, 0.42, 0.05), "Lib_WoodDark")
            g.box((x, 0.18, 0), (0.05, 0.05, 1.05), "Lib_WoodDark")
            if lod == 0:
                g.box((x, 0.70, 0), (0.08, 0.04, 0.62), "Lib_WoodDark")
                g.box((x, 0.02, 0), (0.10, 0.04, 0.10), "Lib_Rust")
        g.box((0, 0.22, 0), (1.20, 0.04, 0.04), "Lib_SteelDark")
        a.end()
    # Slab crown is 0.76 m. Collider top is 0.5 cm under it, clear of the score lines.
    a.box("Col_Top", (0, 0.738, 0), (1.68, 0.024, 0.58))
    a.box("Col_BenchN", (0, 0.426, 0.55), (1.20, 0.016, 0.20))
    a.box("Col_BenchS", (0, 0.426, -0.55), (1.20, 0.016, 0.20))
    for i, x in enumerate((-0.70, 0.70)):
        a.box("Col_Leg_%d" % i, (x, 0.36, 0), (0.04, 0.60, 0.04))
    return a
