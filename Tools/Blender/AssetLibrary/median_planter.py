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
        g.sphere((0, 0.48, 0), lod_pick(lod, 0.28, 0.26), "Lib_Foliage", lod_pick(lod, 12, 8))
        g.sphere((0.1, 0.58, 0.08), 0.18, "Lib_FoliageDark", lod_pick(lod, 10, 6))
        a.end()
    a.box("Col_EndN", (0, 0.20, 0.42), (1.05, 0.40, 0.16))
    a.box("Col_EndS", (0, 0.20, -0.42), (1.05, 0.40, 0.16))
    a.box("Col_SideL", (-0.44, 0.20, 0), (0.16, 0.40, 3.68))
    a.box("Col_SideR", (0.44, 0.20, 0), (0.16, 0.40, 3.68))
    a.box("Col_Soil", (0, 0.08, 0), (0.72, 0.16, 3.2))
    a.sphere("Col_Shrub", (0, 0.52, 0), 0.26)
    return a
