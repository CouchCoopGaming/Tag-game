"""Bike-lane tile. 1.80 m wide, 4 m long, surface at 0.12 m.

The symbol is block paint and two rings. It sits in the paint skin, above the slab collider.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
from sk_street_road import ROAD_TOP, TILE_L, asphalt, paint, slab_collider


@register
def create():
    a = Asset(
        "StreetRoad_Bike",
        "StreetFurniture",
        "Bike lane 1.80 x 4.00 m. Surface at 0.12 m. White edge line and a bike symbol.",
    )
    a.climb_note = "Flat asphalt. Not a climb."
    a.vault_note = "Surface is 0.12 m. Not a vault."
    width = 1.80
    for lod in (0, 1):
        g = a.begin(lod)
        asphalt(g, lod, width, TILE_L, wear=True)
        paint(g, width * 0.5 - 0.16, 0, 0.10, TILE_L)
        if lod == 0:
            # Wheels bite the asphalt and stay above the slab collider.
            for z in (-0.34, 0.36):
                g.torus((0.05, ROAD_TOP + 0.006, z), 0.16, 0.010, "Lib_PaintWhite", 14, 6)
            paint(g, 0.05, 0.02, 0.04, 0.62)
            paint(g, 0.16, 0.10, 0.28, 0.04)
            paint(g, -0.02, 0.22, 0.22, 0.04)
            g.box((0.22, ROAD_TOP + 0.002, 0.36), (0.28, 0.008, 0.04), "Lib_PaintWhite")
        a.end()
    slab_collider(a, width, TILE_L)
    return a
