"""Sidewalk tile with an expansion joint. Matches the 2 x 4 m walk socket.

Top is 0.27 m. The -X face is the curb, 0.15 m above a 0.12 m road.
The expansion joint is 24 mm, filled, so the two slabs stay one walkable tile.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "Sidewalk_Joint",
        "StreetFurniture",
        "Sidewalk 2.00 x 4.00 m, top at 0.27 m. One 24 mm filled expansion joint and control joints. Curb face is -X.",
    )
    a.climb_note = "Curb face is 0.27 m from the pivot. Not a cling wall."
    a.vault_note = "Top is 0.15 m above the road. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0.0
        bs = 1 if lod == 0 else 0
        for zc in (-1.006, 1.006):
            g.box((0, 0.135, zc), (2.00, 0.27, 1.988), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.55)
        g.box((0, 0.25, 0), (1.84, 0.06, 0.032), "Lib_AsphaltPatch")
        if lod == 0:
            for z in (-1.55, -0.55, 0.55, 1.55):
                g.box((0.05, 0.272, z), (1.70, 0.006, 0.012), "Lib_Mortar")
            g.box((0.35, 0.272, 0), (0.012, 0.006, 3.70), "Lib_Mortar")
            g.box((-1.004, 0.10, 0), (0.014, 0.16, 3.70), "Lib_AsphaltWear")
        a.end()
    # Crown is 0.27. Each collider stays on its own slab, clear of the joint filler.
    a.box("Col_SlabS", (0, 0.133, -1.02), (1.88, 0.258, 1.84))
    a.box("Col_SlabN", (0, 0.133, 1.02), (1.88, 0.258, 1.84))
    return a
