"""Two-lane straight. 6 m wide, 4 m long, driving surface at 0.12 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, TILE_L, Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Road_Straight",
        "Roads",
        "6 m wide (two 3 m lanes) by 4 m long. Top at 0.12 m. Center the next tile 4 m along Z.",
    )
    a.climb_note = "Flat road."
    a.vault_note = "No curb on this tile. Sidewalks carry the curb."
    half = ROAD_W * 0.5
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L), "Lib_Asphalt", bevel=0.006 if lod == 0 else 0, segs=1, uv_scale=0.4)
        y = ROAD_TOP + 0.004
        g.box((-half + 0.16, y, 0), (0.10, 0.008, TILE_L - 0.08), "Lib_PaintWhite")
        g.box((half - 0.16, y, 0), (0.10, 0.008, TILE_L - 0.08), "Lib_PaintWhite")
        dashes = lod_pick(lod, 3, 2)
        for i in range(dashes):
            z = -1.2 + i * (2.4 / max(1, dashes - 1))
            g.box((0, y, z), (0.12, 0.008, 0.55), "Lib_Lane")
        a.end()
    a.box("Col_Slab", (0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L))
    return a
