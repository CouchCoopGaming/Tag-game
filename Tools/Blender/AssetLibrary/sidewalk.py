"""Sidewalk with curb. Curb face is -X. Top is 0.27 m, 15 cm above the road."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import SIDE_W, TILE_L, WALK_TOP, Asset, register


@register
def create():
    a = Asset(
        "Sidewalk",
        "Roads",
        "2 m wide, 4 m long, top at 0.27 m. Curb face is -X. Butt that face to the road edge (x = ±3 m).",
    )
    a.climb_note = "Curb face is 0.27 m tall from the pivot, 0.15 m above the road. Not a cling wall."
    a.vault_note = "Curb is 0.15 m above the road. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, WALK_TOP * 0.5, 0), (SIDE_W, WALK_TOP, TILE_L), "Lib_Concrete", bevel=0.006 if lod == 0 else 0, segs=1, uv_scale=0.7)
        # Score joints.
        y = WALK_TOP + 0.003
        g.box((0, y, -1.0), (SIDE_W - 0.08, 0.004, 0.012), "Lib_Mortar")
        g.box((0, y, 1.0), (SIDE_W - 0.08, 0.004, 0.012), "Lib_Mortar")
        g.box((0.35, y, 0), (0.012, 0.004, TILE_L - 0.1), "Lib_Mortar")
        a.end()
    a.box("Col_Walk", (0, WALK_TOP * 0.5, 0), (SIDE_W, WALK_TOP, TILE_L))
    return a
