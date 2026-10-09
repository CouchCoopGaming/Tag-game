"""T junction. Through traffic on X, stem toward +Z, curb closes -Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, WALK_TOP, Asset, register


@register
def create():
    a = Asset(
        "Road_T",
        "Roads",
        "6 x 6 m. Through road along X, stem opens on +Z, curb closes -Z. Do not attach a straight to -Z.",
    )
    a.climb_note = "Curb face on -Z is 0.15 m. Too short to cling."
    a.vault_note = "Curb is 0.15 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        # Drivable asphalt, closed 0.7 m short of the -Z edge.
        g.box((0, ROAD_TOP * 0.5, 0.35), (ROAD_W, ROAD_TOP, ROAD_W - 0.7), "Lib_Asphalt", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=0.4)
        g.box((0, WALK_TOP * 0.5, -2.65), (ROAD_W, WALK_TOP, 0.70), "Lib_Concrete", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=0.8)
        y = ROAD_TOP + 0.004
        g.box((0, y, 2.55), (ROAD_W - 0.5, 0.008, 0.28), "Lib_PaintWhite")
        g.box((2.55, y, 0.35), (0.28, 0.008, 4.6), "Lib_PaintWhite")
        g.box((-2.55, y, 0.35), (0.28, 0.008, 4.6), "Lib_PaintWhite")
        a.end()
    a.box("Col_Asphalt", (0, ROAD_TOP * 0.5, 0.35), (ROAD_W, ROAD_TOP, ROAD_W - 0.7))
    a.box("Col_Curb", (0, WALK_TOP * 0.5, -2.65), (ROAD_W, WALK_TOP, 0.70))
    return a
