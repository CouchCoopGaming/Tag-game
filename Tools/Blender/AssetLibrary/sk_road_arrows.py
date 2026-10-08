"""Two-lane tile with a through arrow and a right-turn arrow."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_street_road import LANE, TILE_L, arrow_right, arrow_straight, asphalt, edge_lines, slab_collider

WIDTH = LANE * 2


@register
def create():
    a = Asset(
        "StreetRoad_Arrows",
        "StreetFurniture",
        "6 x 4 m two-lane tile. Through arrow in the left lane, right-turn arrow in the right lane. Arrows are block paint, not a tapered stencil.",
    )
    a.climb_note = "Flat asphalt."
    a.vault_note = "No curb on this tile."
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, WIDTH, TILE_L)
        edge_lines(g, WIDTH, TILE_L)
        if lod == 0:
            arrow_straight(g, -LANE * 0.5, -0.15)
            arrow_right(g, LANE * 0.5, -0.05)
        else:
            arrow_straight(g, -LANE * 0.5, -0.15)
        a.end()
    slab_collider(a, WIDTH, TILE_L)
    return a
