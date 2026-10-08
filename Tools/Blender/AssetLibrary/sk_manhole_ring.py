"""Heavy manhole. Raised frame, ribbed cover, two pick slots. Sits on the road."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import bolt_ring


@register
def create():
    a = Asset(
        "Manhole_Ring",
        "StreetFurniture",
        "Manhole frame 0.86 m across, cover 0.66 m, 5 cm tall. Ribs and two pick slots.",
    )
    a.climb_note = "Flush with the road. Not a step."
    a.vault_note = "Flat cover."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 12)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.015, 0), 0.43, 0.030, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.042, 0), 0.33, 0.016, "Lib_MetalWorn", seg, uv_scale=0.6)
        g.torus((0, 0.028, 0), 0.39, 0.016, "Lib_Steel", seg, 6)
        if lod == 0:
            for i, rad in enumerate((0.10, 0.18, 0.26)):
                g.torus((0, 0.050, 0), rad, 0.006, "Lib_Rust", 18, 4)
            bolt_ring(g, (0, 0.052, 0), 0.30, 6, 0.010, 0.012, "Lib_Steel", phase=0.3)
            for sign in (-1, 1):
                g.box((sign * 0.12, 0.054, 0), (0.07, 0.010, 0.018), "Lib_Black")
            for i in range(8):
                ang = math.radians(i * 45 + 12)
                g.box(
                    (math.sin(ang) * 0.20, 0.052, math.cos(ang) * 0.20),
                    (0.05, 0.006, 0.012),
                    "Lib_SteelDark",
                    euler=(0, -math.degrees(ang), 0),
                )
        a.end()
    # Cover and frame are separate solids. Each box stays inside one of them.
    a.box("Col_Frame", (0, 0.014, 0), (0.52, 0.020, 0.52))
    a.box("Col_Cover", (0, 0.040, 0), (0.40, 0.006, 0.40))
    return a
