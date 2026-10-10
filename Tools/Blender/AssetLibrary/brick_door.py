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
        g.box((0, 1.12, -0.02), (0.90, 2.04, 0.04), "Lib_WoodDark", bevel=bev, segs=bs, uv_scale=1.2)
        face = T * 0.5 + 0.02
        g.box((-0.50, 1.12, face), (0.06, 2.16, 0.08), "Lib_Wood", bevel=bev, segs=bs)
        g.box((0.50, 1.12, face), (0.06, 2.16, 0.08), "Lib_Wood", bevel=bev, segs=bs)
        g.box((0, 2.16, face), (1.06, 0.08, 0.08), "Lib_Wood", bevel=bev, segs=bs)
        if lod == 0:
            g.box((0.32, 1.05, face + 0.02), (0.03, 0.12, 0.04), "Lib_Brass")
            g.cylinder((0.36, 1.05, face + 0.03), 0.012, 0.04, "Lib_Brass", 6, axis="Z")
            g.box((0, 2.28, face), (1.30, 0.12, 0.14), "Lib_Concrete", bevel=bev, segs=bs)
            g.box((0, 1.55, face), (0.70, 0.02, 0.015), "Lib_Wood")
            g.box((0, 0.45, face), (0.78, 0.16, 0.02), "Lib_SteelDark")
        a.end()
    a.box("Climb_PierL", (-1.25, 1.60, 0), (1.50, 3.04, T))
    a.box("Climb_PierR", (1.25, 1.60, 0), (1.50, 3.04, T))
    a.box("Climb_Header", (0, 2.68, 0), (1.00, 0.88, T))
    a.box("Col_Door", (0, 1.12, -0.02), (0.86, 2.00, 0.036))
    # Frame tops stop 2 cm under the header. The wood stops at 2.20; the lintel starts at 2.22.
    a.box("Col_FrameL", (-0.50, 1.145, 0.17), (0.05, 2.15, 0.06))
    a.box("Col_FrameR", (0.50, 1.145, 0.17), (0.05, 2.15, 0.06))
    a.box("Col_Step", (0, 0.06, 0.22), (1.4, 0.12, 0.36))
    a.box("Col_Cornice", (0, 3.16, 0), (W, 0.08, T + 0.08))
    return a
