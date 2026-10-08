"""Brick window bay. Same 4 x 3.2 m shell as Brick_Wall, with a real opening and glass."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W, H, T = 4.0, 3.2, 0.30


@register
def create():
    a = Asset(
        "Brick_Window",
        "Buildings",
        "4.00 x 3.20 m bay. Window opening 1.16 x 1.45 m, sill at 0.95 m. Glass is solid.",
    )
    a.climbable = True
    a.climb_note = "Piers, sill, and header are cling. The opening is glass, not a hole. Exterior is +Z."
    a.vault_note = "Sill is 0.95 m but only 0.30 m deep. Not a vault rail."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.006, 0.003, 0.0)
        bs = lod_pick(lod, 1, 1, 0)
        g.box((0, 0.09, 0), (W, 0.18, T + 0.06), "Lib_Brick", bevel=bev, segs=bs)
        g.box((0, 3.16, 0), (W, 0.08, T + 0.08), "Lib_Concrete", bevel=bev, segs=bs)
        # Piers and the wall under/over the opening.
        g.box((-1.29, 1.65, 0), (1.42, 2.94, T), "Lib_Brick", bevel=bev, segs=bs)
        g.box((1.29, 1.65, 0), (1.42, 2.94, T), "Lib_Brick", bevel=bev, segs=bs)
        g.box((0, 0.56, 0), (1.16, 0.76, T), "Lib_Brick", bevel=bev, segs=bs)
        g.box((0, 2.78, 0), (1.16, 0.68, T), "Lib_Brick", bevel=bev, segs=bs)
        if lod < 2:
            g.box((0, 1.68, 0), (1.08, 1.36, 0.012), "Lib_Glass")
            proud = T * 0.5 + 0.02
            g.box((0, 0.96, proud), (1.28, 0.06, 0.06), "Lib_Concrete", bevel=bev, segs=bs)
            g.box((0, 2.42, proud), (1.28, 0.05, 0.05), "Lib_SteelDark")
            g.box((-0.60, 1.68, proud), (0.05, 1.40, 0.05), "Lib_SteelDark")
            g.box((0.60, 1.68, proud), (0.05, 1.40, 0.05), "Lib_SteelDark")
        a.end()
    a.box("Climb_PierL", (-1.29, 1.65, 0), (1.42, 2.94, T))
    a.box("Climb_PierR", (1.29, 1.65, 0), (1.42, 2.94, T))
    a.box("Climb_Sill", (0, 0.56, 0), (1.16, 0.76, T))
    a.box("Climb_Header", (0, 2.78, 0), (1.16, 0.68, T))
    a.box("Col_Plinth", (0, 0.09, 0), (W, 0.18, T + 0.06))
    a.box("Col_Cornice", (0, 3.16, 0), (W, 0.08, T + 0.08))
    a.box("Col_Glass", (0, 1.68, 0), (1.08, 1.36, 0.012))
    a.box("Col_FrameSill", (0, 0.96, T * 0.5 + 0.02), (1.28, 0.06, 0.06))
    return a
