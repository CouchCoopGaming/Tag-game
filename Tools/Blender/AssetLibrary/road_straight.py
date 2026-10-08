"""Two-lane straight. 6 m wide, 4 m long, driving surface at 0.12 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, TILE_L, Asset, register, lod_pick


def _worn_edge(g, x, y):
    """One stripe, broken into short worn pieces. Not a second line."""
    # (center z, length). Gaps stay inside the tile so the joint is still paint.
    pieces = ((-1.42, 0.92), (-0.28, 1.02), (0.78, 0.88), (1.62, 0.52))
    width = 0.08
    for z, length in pieces:
        g.box((x, y, z), (width, 0.008, length), "Lib_PaintWhite")


@register
def create():
    a = Asset(
        "Road_Straight",
        "Roads",
        "6 m wide (two 3 m lanes) by 4 m long. Top at 0.12 m. Worn edge lines and darkened wheel paths. Center the next tile 4 m along Z.",
    )
    a.climb_note = "Flat road."
    a.vault_note = "No curb on this tile. Sidewalks carry the curb."
    half = ROAD_W * 0.5
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L), "Lib_Asphalt", bevel=0.006 if lod == 0 else 0, segs=1, uv_scale=0.4)
        y = ROAD_TOP + 0.006
        # 4 mm above the slab so the wear does not share a face with the asphalt.
        wear_y = ROAD_TOP + 0.007
        for x in (-2.05, -0.72, 0.72, 2.05):
            g.box((x, wear_y, 0), (0.30, 0.005, TILE_L - 0.06), "Lib_AsphaltWear")
        if lod == 0:
            g.box((1.28, wear_y, 0.55), (0.48, 0.005, 0.36), "Lib_AsphaltPatch")
            g.box((-1.55, wear_y, -0.85), (0.62, 0.005, 0.28), "Lib_AsphaltPatch")
        # Edge line sits inboard of the road edge. The gutter, then the curb, are outside the tile.
        # One stripe, about 14 cm in from the road edge. The gutter and the curb are the next pieces.
        _worn_edge(g, -half + 0.18, y)
        _worn_edge(g, half - 0.18, y)
        dashes = lod_pick(lod, 3, 2)
        for i in range(dashes):
            z = -1.15 + i * (2.3 / max(1, dashes - 1))
            g.box((0, y, z), (0.10, 0.008, 0.38), "Lib_Lane")
        a.end()
    a.box("Col_Slab", (0, ROAD_TOP * 0.5, 0), (ROAD_W, ROAD_TOP, TILE_L))
    return a
