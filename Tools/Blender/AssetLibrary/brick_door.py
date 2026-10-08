"""Brick door bay. Closed door, 1.00 x 2.15 m, in the 4 x 3.2 m shell."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W, T = 4.0, 0.30


@register
def create():
    a = Asset(
        "Brick_Door",
        "Buildings",
        "4.00 x 3.20 m bay with a closed 1.00 x 2.15 m door and a 0.12 m step. Exterior is +Z.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.climb_note = "Piers are cling. The door is closed and collides. Exterior is +Z."
    a.vault_note = "Step is 0.12 m. Not a vault."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.006, 0.003, 0.0)
        bs = lod_pick(lod, 1, 1, 0)
        g.box((0, 0.06, 0.22), (1.4, 0.12, 0.36), "Lib_Concrete", bevel=bev, segs=bs)
        g.box((0, 3.16, 0), (W, 0.08, T + 0.08), "Lib_Concrete", bevel=bev, segs=bs)
        g.box((-1.25, 1.60, 0), (1.50, 3.04, T), "Lib_Brick", bevel=bev, segs=bs)
        g.box((1.25, 1.60, 0), (1.50, 3.04, T), "Lib_Brick", bevel=bev, segs=bs)
        g.box((0, 2.68, 0), (1.00, 0.88, T), "Lib_Brick", bevel=bev, segs=bs)
        g.box((0, 1.12, 0.02), (0.92, 2.08, 0.05), "Lib_WoodDark", bevel=bev, segs=bs, uv_scale=1.2)
        if lod == 0:
            g.box((0.28, 1.05, 0.05), (0.04, 0.08, 0.03), "Lib_Brass")
            g.box((0, 2.20, 0.18), (1.15, 0.06, 0.06), "Lib_Concrete")
        a.end()
    a.box("Climb_PierL", (-1.25, 1.60, 0), (1.50, 3.04, T))
    a.box("Climb_PierR", (1.25, 1.60, 0), (1.50, 3.04, T))
    a.box("Climb_Header", (0, 2.68, 0), (1.00, 0.88, T))
    a.box("Col_Door", (0, 1.12, 0.02), (0.92, 2.08, 0.05))
    a.box("Col_Step", (0, 0.06, 0.22), (1.4, 0.12, 0.36))
    a.box("Col_Cornice", (0, 3.16, 0), (W, 0.08, T + 0.08))
    return a
