"""Square concrete planter with soil and a shrub."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Planter", "Park", "0.90 m square planter, 0.48 m tall, soil and a shrub. Walls are hollow.")
    a.climb_note = "Too low."
    a.vault_note = "0.48 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.006 if lod == 0 else 0
        g.box((0, 0.24, -0.40), (0.90, 0.48, 0.10), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0, 0.24, 0.40), (0.90, 0.48, 0.10), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((-0.40, 0.24, 0), (0.10, 0.48, 0.70), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0.40, 0.24, 0), (0.10, 0.48, 0.70), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        g.box((0, 0.10, 0), (0.70, 0.16, 0.70), "Lib_Soil", uv_scale=1.0)
        g.sphere((0, 0.46, 0), 0.28, "Lib_Foliage", lod_pick(lod, 12, 8))
        if lod == 0:
            g.sphere((0.08, 0.58, 0.05), 0.16, "Lib_FoliageDark", 8)
        a.end()
    for name, c, s in (
        ("Col_WallN", (0, 0.24, 0.40), (0.90, 0.48, 0.10)),
        ("Col_WallS", (0, 0.24, -0.40), (0.90, 0.48, 0.10)),
        ("Col_WallL", (-0.40, 0.24, 0), (0.10, 0.48, 0.70)),
        ("Col_WallR", (0.40, 0.24, 0), (0.10, 0.48, 0.70)),
    ):
        a.box(name, c, s)
    a.box("Col_Soil", (0, 0.10, 0), (0.70, 0.16, 0.70))
    # Shrub sits 2 cm above the soil. The crown stays inside the foliage spheres.
    a.sphere("Col_Shrub", (0, 0.46, 0), 0.26)
    return a
