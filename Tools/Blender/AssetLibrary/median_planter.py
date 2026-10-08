"""Concrete median with a soil planter and a clipped shrub."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import TILE_L, Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Median_Planter",
        "Roads",
        "4.0 x 1.05 m median, 0.40 m concrete, planter recess with soil and a shrub.",
    )
    a.climb_note = "Too low to cling."
    a.vault_note = "0.40 m wall. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.005 if lod == 0 else 0
        # Walls of the median. The middle is a recess, not a solid block.
        g.box((0, 0.20, -0.42), (1.05, 0.40, 0.16), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0, 0.20, 0.42), (1.05, 0.40, 0.16), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((-0.44, 0.20, 0), (0.16, 0.40, TILE_L - 0.32), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0.44, 0.20, 0), (0.16, 0.40, TILE_L - 0.32), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0, 0.08, 0), (0.72, 0.16, 3.2), "Lib_Soil", uv_scale=1.0)
        g.sphere((0, 0.46, 0), lod_pick(lod, 0.22, 0.20), "Lib_FoliageDark", lod_pick(lod, 8, 6))
        g.sphere((0.12, 0.55, 0.1), 0.16, "Lib_Foliage", lod_pick(lod, 7, 5))
        g.sphere((-0.14, 0.50, -0.12), 0.15, "Lib_FoliageLite", lod_pick(lod, 7, 5))
        if lod == 0:
            g.box((0, 0.40, -0.42), (1.08, 0.04, 0.20), "Lib_Concrete")
            g.box((0, 0.40, 0.42), (1.08, 0.04, 0.20), "Lib_Concrete")
        a.end()
    a.box("Col_EndN", (0, 0.20, 0.42), (1.05, 0.40, 0.16))
    a.box("Col_EndS", (0, 0.20, -0.42), (1.05, 0.40, 0.16))
    a.box("Col_SideL", (-0.44, 0.20, 0), (0.16, 0.40, 3.68))
    a.box("Col_SideR", (0.44, 0.20, 0), (0.16, 0.40, 3.68))
    a.box("Col_Soil", (0, 0.08, 0), (0.72, 0.16, 3.2))
    a.sphere("Col_Shrub", (0, 0.48, 0), 0.20)
    return a
