"""Curb gutter. One concrete pan, 0.38 m wide, 4 m long. The +X face meets the sidewalk curb."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import TILE_L, Asset, register


@register
def create():
    a = Asset(
        "Gutter",
        "Roads",
        "0.38 m wide, 4 m long. One concrete pan. The +X face butts the sidewalk curb; the -X face sits at the road edge. Top is 0.09 m, below the 0.12 m asphalt.",
    )
    a.climb_note = "A shallow pan. Not a cling face."
    a.vault_note = "Under 0.15 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        # Outer faces stay at x = ±0.19 so the street snap (road edge, then curb) holds.
        g.box((0.0, 0.045, 0), (0.38, 0.09, TILE_L), "Lib_Concrete", uv_scale=0.8)
        a.end()
    a.box("Col_Pan", (0.0, 0.045, 0), (0.34, 0.07, TILE_L - 0.04))
    return a
