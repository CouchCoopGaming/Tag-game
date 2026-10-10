"""Two-lane street module. 6 m wide, 4 m long, surface at 0.12 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_street_road import LANE, TILE_L, asphalt, dashes, edge_lines, slab_collider

WIDTH = LANE * 2


@register
def create():
    a = Asset(
        "StreetRoad_TwoLane",
        "StreetFurniture",
        "Two 3 m lanes, 6 x 4 m, surface at 0.12 m. White edges, dashed yellow center. Next tile is 4 m along Z.",
    )
    a.climb_note = "Flat asphalt."
    a.vault_note = "No curb on this tile."
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, WIDTH, TILE_L)
        edge_lines(g, WIDTH, TILE_L)
        dashes(g, 0.0, TILE_L, "Lib_Lane")
        a.end()
    slab_collider(a, WIDTH, TILE_L)
    return a
