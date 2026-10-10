"""Planted median on a 6 x 4 m street module.

The tile matches StreetRoad_TwoLane's socket. A 1.50 m median replaces the
centerline, so each lane is about 2.25 m. Curb top is 0.28 m (0.16 m above
the 0.12 m asphalt). Shrubs are one remesh each.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_street_road import ROAD_TOP, TILE_L, asphalt, edge_lines, paint

WIDTH = 6.0


def _curb_wall(g, x, z, sx, sz, lod):
    bev = 0.003 if lod == 0 else 0.0
    g.box((x, 0.16, z), (sx, 0.24, sz), "Lib_Concrete", bevel=0.0, segs=0)


@register
def create():
    a = Asset(
        "StreetMedian_Planted",
        "StreetFurniture",
        "6 x 4 m module with a 1.50 m planted median. Asphalt at 0.12 m, curb top at 0.28 m. Lanes are about 2.25 m.",
    )
    a.climb_note = "Median curb is 0.16 m above the asphalt."
    a.vault_note = "Low curb. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, WIDTH, TILE_L, wear=False)
        if lod == 0:
            g.box((-1.9, ROAD_TOP + 0.001, 0.4), (0.7, 0.006, 0.5), "Lib_AsphaltPatch")
            g.box((2.0, ROAD_TOP + 0.001, -0.6), (0.6, 0.006, 0.4), "Lib_AsphaltWear")
        edge_lines(g, WIDTH, TILE_L)
        paint(g, -1.05, 0, 0.10, TILE_L, "Lib_Lane")
        paint(g, 1.05, 0, 0.10, TILE_L, "Lib_Lane")
        # Side walls stop short of the tile ends so they do not share a face with the asphalt.
        _curb_wall(g, -0.64, 0, 0.22, 3.70, lod)
        _curb_wall(g, 0.64, 0, 0.22, 3.70, lod)
        _curb_wall(g, 0, 1.78, 1.16, 0.28, lod)
        _curb_wall(g, 0, -1.78, 1.16, 0.28, lod)
        g.box((0, 0.145, 0), (1.08, 0.11, 3.55), "Lib_Soil", uv_scale=0.5)
        g.box((0, 0.205, 0), (0.90, 0.04, 3.40), "Lib_Mulch", uv_scale=0.5)
        for z in (-0.85, 0.85):
            g.blob(
                [((0.0, 0.50, z), 0.30), ((0.10, 0.58, z + 0.08), 0.16), ((-0.08, 0.42, z - 0.06), 0.14)],
                "Lib_Foliage",
                voxel=0.08 if lod == 0 else 0.12,
            )
        a.end()
    # Lanes stay off the curb. Curb colliders sit above the asphalt, in concrete only.
    a.box("Col_LaneL", (-1.88, 0.056, 0), (2.10, 0.112, 3.70))
    a.box("Col_LaneR", (1.88, 0.056, 0), (2.10, 0.112, 3.70))
    a.box("Col_CurbL", (-0.64, 0.206, 0), (0.10, 0.132, 3.00))
    a.box("Col_CurbR", (0.64, 0.206, 0), (0.10, 0.132, 3.00))
    # End curbs overlap the soil. Keep these in the concrete above the soil.
    a.box("Col_CurbN", (0, 0.246, 1.78), (0.70, 0.052, 0.08))
    a.box("Col_CurbS", (0, 0.246, -1.78), (0.70, 0.052, 0.08))
    a.box("Col_Mulch", (0, 0.208, 0), (0.50, 0.016, 0.46))
    a.sphere("Col_ShrubS", (0, 0.52, -0.85), 0.14)
    a.sphere("Col_ShrubN", (0, 0.52, 0.85), 0.14)
    return a
