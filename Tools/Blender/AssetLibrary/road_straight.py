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
        "6 m wide (two 3 m lanes) by 4 m long. Top at 0.12 m. Near-uniform asphalt, slight wheel-path darkening, one white edge line each side, dashed yellow center. Center the next tile 4 m along Z.",
    )
    a.climb_note = "Flat road."
    a.vault_note = "No curb on this tile. Sidewalks carry the curb."
    half = ROAD_W * 0.5
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L), "Lib_Asphalt", bevel=0, segs=1, uv_scale=0.4)
        # Full-length stains, only a shade darker, centered in each lane. Not patch blobs.
        wear_y = ROAD_TOP + 0.004
        for x in (-1.50, 1.50):
            g.box((x, wear_y, 0), (1.05, 0.003, TILE_L), "Lib_AsphaltWear")
        y = ROAD_TOP + 0.010
        # One continuous edge line per side, meeting the next tile. Then the gutter, then the curb.
        g.box((-half + 0.18, y, 0), (0.10, 0.006, TILE_L), "Lib_PaintWhite")
        g.box((half - 0.18, y, 0), (0.10, 0.006, TILE_L), "Lib_PaintWhite")
        dashes = lod_pick(lod, 3, 2)
        for i in range(dashes):
            z = -1.15 + i * (2.3 / max(1, dashes - 1))
            g.box((0, y, z), (0.10, 0.006, 0.50), "Lib_Lane")
        if lod == 0:
            seal_y = ROAD_TOP + 0.014
            g.box((0.55, seal_y, -0.35), (0.012, 0.003, 1.05), "Lib_AsphaltPatch")
            g.box((-1.35, seal_y, 0.85), (0.85, 0.003, 0.012), "Lib_AsphaltPatch")
        a.end()
    a.box("Col_Slab", (0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L))
    return a
