"""Two-lane tile with a stop bar at the +Z end."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_street_road import LANE, TILE_L, asphalt, dashes, edge_lines, paint, slab_collider

WIDTH = LANE * 2


@register
def create():
    a = Asset(
        "StreetRoad_StopBar",
        "StreetFurniture",
        "6 x 4 m two-lane tile. Stop bar is 0.40 m wide, 0.35 m in from the +Z end.",
    )
    a.climb_note = "Flat asphalt."
    a.vault_note = "No curb on this tile."
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, WIDTH, TILE_L)
        edge_lines(g, WIDTH, TILE_L)
        dashes(g, 0.0, TILE_L * 0.55, "Lib_Lane")
        paint(g, 0, TILE_L * 0.5 - 0.45, WIDTH - 0.70, 0.40)
        a.end()
    slab_collider(a, WIDTH, TILE_L)
    return a
