"""Straight with a zebra crossing across the lanes."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, TILE_L, Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Road_Crosswalk",
        "Roads",
        "6 x 4 m straight with zebra bars across the lanes. Same socket as Road_Straight.",
    )
    a.climb_note = "Flat road. Paint has no collider."
    a.vault_note = "No curb on this tile."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L), "Lib_Asphalt", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=0.4)
        bars = lod_pick(lod, 8, 5)
        span = ROAD_W - 0.8
        y = ROAD_TOP + 0.004
        for i in range(bars):
            x = -span * 0.5 + (i + 0.5) * span / bars
            g.box((x, y, 0), (span / bars * 0.55, 0.008, 2.4), "Lib_PaintWhite")
        a.end()
    a.box("Col_Slab", (0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L))
    return a
