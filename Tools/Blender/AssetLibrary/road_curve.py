"""90-degree corner. Enter from -Z, leave toward +X. Sidewalk fills the inner corner."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, ROAD_W, WALK_TOP, Asset, register


@register
def create():
    a = Asset(
        "Road_Curve",
        "Roads",
        "6 x 6 m corner. Asphalt enters on -Z and exits on +X. Inner sidewalk is the -X/+Z wedge.",
    )
    a.climb_note = "Curb at the sidewalk wedge is 0.15 m."
    a.vault_note = "Curb is not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        # South leg, full width.
        g.box((0, ROAD_TOP * 0.5, -1.6), (ROAD_W, ROAD_TOP, 2.8), "Lib_Asphalt", bevel=bev, segs=1, uv_scale=0.4)
        # East leg meets the sidewalk at x = -0.45 and the south leg around z = -0.6.
        g.box((1.275, ROAD_TOP * 0.5, 1.1), (3.45, ROAD_TOP, 3.8), "Lib_Asphalt", bevel=bev, segs=1, uv_scale=0.4)
        # Inner sidewalk wedge.
        g.box((-1.725, WALK_TOP * 0.5, 1.4), (2.55, WALK_TOP, 3.2), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.8)
        y = ROAD_TOP + 0.004
        g.box((1.4, y, -2.2), (2.4, 0.008, 0.12), "Lib_Lane")
        g.box((2.2, y, 1.2), (0.12, 0.008, 2.6), "Lib_Lane")
        a.end()
    a.box("Col_South", (0, ROAD_TOP * 0.5, -1.6), (ROAD_W, ROAD_TOP, 2.8))
    a.box("Col_East", (1.275, ROAD_TOP * 0.5, 1.1), (3.45, ROAD_TOP, 3.8))
    a.box("Col_Walk", (-1.725, WALK_TOP * 0.5, 1.4), (2.55, WALK_TOP, 3.2))
    return a
