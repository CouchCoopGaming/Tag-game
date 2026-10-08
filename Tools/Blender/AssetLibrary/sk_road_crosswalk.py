"""Two-lane tile with a continental crosswalk across both lanes."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_street_road import LANE, TILE_L, asphalt, dashes, edge_lines, slab_collider, zebra

WIDTH = LANE * 2


@register
def create():
    a = Asset(
        "StreetRoad_Crosswalk",
        "StreetFurniture",
        "6 x 4 m two-lane tile. Continental bars 0.45 m wide across both lanes. Same socket as StreetRoad_TwoLane.",
    )
    a.climb_note = "Flat asphalt. Paint is not a step."
    a.vault_note = "No curb on this tile."
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, WIDTH, TILE_L)
        edge_lines(g, WIDTH, TILE_L)
        dashes(g, 0.0, TILE_L, "Lib_Lane")
        zebra(g, "z", 0.15, lod_pick(lod, 8, 5), 2.20, WIDTH - 0.90)
        a.end()
    slab_collider(a, WIDTH, TILE_L)
    return a
