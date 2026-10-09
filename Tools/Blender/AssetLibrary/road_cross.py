"""Four-way intersection. 6 x 6 m asphalt. Straights butt to each edge."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, Asset, register


@register
def create():
    a = Asset(
        "Road_Cross",
        "Roads",
        "6 x 6 m intersection. Attach Road_Straight to any edge, centered, 4 m further out.",
    )
    a.climb_note = "Flat intersection."
    a.vault_note = "No curb on this tile."
    half = ROAD_W * 0.5
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, ROAD_W), "Lib_Asphalt", bevel=0.006 if lod == 0 else 0, segs=1, uv_scale=0.4)
        y = ROAD_TOP + 0.004
        g.box((0, y, half - 0.35), (ROAD_W - 0.4, 0.008, 0.30), "Lib_PaintWhite")
        g.box((0, y, -half + 0.35), (ROAD_W - 0.4, 0.008, 0.30), "Lib_PaintWhite")
        g.box((half - 0.35, y, 0), (0.30, 0.008, ROAD_W - 0.4), "Lib_PaintWhite")
        g.box((-half + 0.35, y, 0), (0.30, 0.008, ROAD_W - 0.4), "Lib_PaintWhite")
        a.end()
    a.box("Col_Slab", (0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, ROAD_W))
    return a
