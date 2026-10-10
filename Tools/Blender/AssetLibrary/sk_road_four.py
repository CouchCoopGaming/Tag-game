"""Four-lane street module. 12 m wide, 4 m long, surface at 0.12 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_street_road import LANE, TILE_L, asphalt, dashes, double_yellow, edge_lines, slab_collider

WIDTH = LANE * 4


@register
def create():
    a = Asset(
        "StreetRoad_FourLane",
        "StreetFurniture",
        "Four 3 m lanes, 12 x 4 m, surface at 0.12 m. Double yellow center, white dashes between same-direction lanes.",
    )
    a.climb_note = "Flat asphalt."
    a.vault_note = "No curb on this tile."
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, WIDTH, TILE_L)
        edge_lines(g, WIDTH, TILE_L)
        double_yellow(g, TILE_L)
        dashes(g, -LANE, TILE_L, "Lib_PaintWhite")
        dashes(g, LANE, TILE_L, "Lib_PaintWhite")
        a.end()
    slab_collider(a, WIDTH, TILE_L)
    return a
