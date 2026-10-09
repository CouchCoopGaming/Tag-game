"""Outside brick corner. Each leg is 2.0 m. Exterior faces are -X and -Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Brick_Corner",
        "Buildings",
        "Outside corner. Legs run to +X and +Z and are 2.0 m. Exterior faces are -X and -Z. A Brick_Wall end meets a leg end.",
    )
    a.climbable = True
    a.climb_note = "Both exterior faces (-X and -Z) are cling. Interior corner is solid too."
    a.vault_note = "No rail."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.006, 0.003, 0.0)
        bs = lod_pick(lod, 1, 1, 0)
        # Leg along X, exterior on -Z. From x=-1 to 1, z=-1 to -0.7.
        g.box((0.0, 1.60, -0.85), (2.0, 3.2, 0.30), "Lib_Brick", bevel=bev, segs=bs)
        # Leg along Z, exterior on -X. Stop at the other leg so they don't occupy the same corner twice.
        g.box((-0.85, 1.60, 0.15), (0.30, 3.2, 1.70), "Lib_Brick", bevel=bev, segs=bs)
        g.box((0.0, 3.16, -0.85), (2.05, 0.08, 0.38), "Lib_Concrete", bevel=bev, segs=1)
        g.box((-0.85, 3.16, 0.15), (0.38, 0.08, 1.75), "Lib_Concrete", bevel=bev, segs=1)
        g.box((0.0, 2.42, -1.02), (1.9, 0.10, 0.04), "Lib_Concrete")
        g.box((-1.02, 2.42, 0.15), (0.04, 0.10, 1.6), "Lib_Concrete")
        a.end()
    a.box("Climb_LegX", (0.0, 1.60, -0.85), (2.0, 3.2, 0.30))
    a.box("Climb_LegZ", (-0.85, 1.60, 0.15), (0.30, 3.2, 1.70))
    return a
