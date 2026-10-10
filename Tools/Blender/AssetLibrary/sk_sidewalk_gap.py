"""Sidewalk tile with an open expansion joint. Same 2 x 4 m socket.

The joint is a 12 mm air gap. Control joints are score lines on each slab.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "Sidewalk_Gap",
        "StreetFurniture",
        "Sidewalk 2.00 x 4.00 m, top at 0.27 m. Open 12 mm expansion joint. Curb face is -X.",
    )
    a.climb_note = "Curb face is 0.27 m from the pivot. Not a cling wall."
    a.vault_note = "Top is 0.15 m above the road. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0.0
        bs = 1 if lod == 0 else 0
        # 12 mm gap at z=0. Each slab is its own solid.
        for zc in (-1.003, 1.003):
            g.box((0, 0.135, zc), (2.00, 0.27, 1.994), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.55)
        if lod == 0:
            for zc in (-1.003, 1.003):
                g.box((0.05, 0.272, zc), (1.70, 0.006, 0.012), "Lib_Mortar")
            for zc in (-1.003, 1.003):
                g.box((-1.004, 0.10, zc), (0.014, 0.16, 1.70), "Lib_AsphaltWear")
        a.end()
    # Crown is 0.27. Tops sit 8 mm under, and each box stays on its own slab.
    a.box("Col_SlabS", (0, 0.133, -1.003), (1.86, 0.258, 1.80))
    a.box("Col_SlabN", (0, 0.133, 1.003), (1.86, 0.258, 1.80))
    return a
