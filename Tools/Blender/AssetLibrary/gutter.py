"""Curb gutter. 0.40 m wide, 4 m long. The +X lip meets the sidewalk curb."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import TILE_L, Asset, register


@register
def create():
    a = Asset(
        "Gutter",
        "Roads",
        "0.40 m wide, 4 m long. Channel along Z. The +X lip butts the sidewalk curb; the -X lip sits at the road edge.",
    )
    a.climb_note = "A shallow channel. Not a cling face."
    a.vault_note = "Under 0.15 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        # Full tile length so neighboring gutters meet. The lips are the curb and the road edge.
        g.box((-0.14, 0.07, 0), (0.10, 0.10, TILE_L), "Lib_Concrete", uv_scale=0.8)
        g.box((0.0, 0.045, 0), (0.16, 0.07, TILE_L), "Lib_Asphalt", uv_scale=0.8)
        g.box((0.15, 0.08, 0), (0.08, 0.12, TILE_L), "Lib_Concrete", uv_scale=0.8)
        a.end()
    a.box("Col_RoadLip", (-0.14, 0.07, 0), (0.07, 0.07, TILE_L - 0.04))
    a.box("Col_Channel", (0.0, 0.045, 0), (0.12, 0.05, TILE_L - 0.04))
    a.box("Col_CurbLip", (0.15, 0.08, 0), (0.05, 0.08, TILE_L - 0.04))
    return a
