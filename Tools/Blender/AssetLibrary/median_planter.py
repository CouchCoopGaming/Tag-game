"""Concrete median. Closed coping, soil, and a clipped shrub of overlapping crowns."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import TILE_L, Asset, register


@register
def create():
    a = Asset(
        "Median_Planter",
        "Roads",
        "4.0 x 1.05 m median, 0.40 m concrete coping closed on all four sides, soil and a clipped shrub.",
    )
    a.climb_note = "Too low to cling."
    a.vault_note = "0.40 m wall. Not a vault."
    half = TILE_L * 0.5
    end_z = half - 0.08
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.20, end_z), (1.05, 0.40, 0.16), "Lib_Concrete", uv_scale=0.8)
        g.box((0, 0.20, -end_z), (1.05, 0.40, 0.16), "Lib_Concrete", uv_scale=0.8)
        g.box((-0.44, 0.20, 0), (0.16, 0.40, TILE_L - 0.32), "Lib_Concrete", uv_scale=0.8)
        g.box((0.44, 0.20, 0), (0.16, 0.40, TILE_L - 0.32), "Lib_Concrete", uv_scale=0.8)
        # Coping sits on the wall top all the way around, including the long sides.
        g.box((0, 0.405, end_z), (1.12, 0.05, 0.22), "Lib_Concrete")
        g.box((0, 0.405, -end_z), (1.12, 0.05, 0.22), "Lib_Concrete")
        g.box((-0.44, 0.405, 0), (0.22, 0.05, TILE_L - 0.28), "Lib_Concrete")
        g.box((0.44, 0.405, 0), (0.22, 0.05, TILE_L - 0.28), "Lib_Concrete")
        g.box((0, 0.08, 0), (0.72, 0.16, 3.36), "Lib_Soil", uv_scale=1.0)
        # One crown holds the collider. The others sit off its sample points.
        g.sphere((0.0, 0.55, 0.0), 0.22, "Lib_FoliageDark", 12 if lod == 0 else 8)
        g.sphere((0.0, 0.64, 0.34), 0.13, "Lib_Foliage", 10 if lod == 0 else 6)
        g.sphere((-0.06, 0.62, -0.32), 0.12, "Lib_Foliage", 10 if lod == 0 else 6)
        if lod == 0:
            g.sphere((0.16, 0.66, 0.16), 0.09, "Lib_FoliageLite", 8)
            g.sphere((-0.14, 0.64, -0.12), 0.08, "Lib_FoliageDark", 8)
        a.end()
    a.box("Col_EndN", (0, 0.20, end_z), (1.00, 0.36, 0.12))
    a.box("Col_EndS", (0, 0.20, -end_z), (1.00, 0.36, 0.12))
    a.box("Col_SideL", (-0.44, 0.20, 0), (0.14, 0.36, 3.60))
    a.box("Col_SideR", (0.44, 0.20, 0), (0.14, 0.36, 3.60))
    a.box("Col_Soil", (0, 0.08, 0), (0.68, 0.14, 3.20))
    a.sphere("Col_Shrub", (0, 0.55, 0), 0.16)
    return a
