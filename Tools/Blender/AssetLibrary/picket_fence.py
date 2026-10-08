"""Picket fence bay. 2.0 m wide, pointed pickets, two rails behind them."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "PicketFence",
        "Buildings",
        "Picket bay, 2.0 m wide. Posts to 1.10 m. Twelve pointed pickets about 7 cm wide with a gap about the same, two rails on -Z.",
    )
    a.climb_note = "Pickets are 6 cm boards with gaps. Not a cling wall."
    a.vault_note = "Post tops are 1.10 m. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.002 if lod == 0 else 0
        for x in (-0.96, 0.96):
            g.box((x, 0.55, 0), (0.09, 1.10, 0.09), "Lib_WoodDark", bevel=bev, segs=1, uv_scale=1.0)
            if lod == 0:
                g.box((x, 1.12, 0), (0.11, 0.03, 0.11), "Lib_Wood")
        count = lod_pick(lod, 12, 6)
        span = 1.72
        board = 0.070
        for i in range(count):
            x = -span * 0.5 + (i + 0.5) * span / count
            g.box((x, 0.44, 0.02), (board, 0.84, 0.024), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.2)
            g.cone((x, 0.924, 0.02), board * 0.5, 0.004, 0.12, "Lib_Wood", 6)
        g.box((0, 0.28, -0.028), (1.82, 0.06, 0.028), "Lib_WoodDark")
        g.box((0, 0.62, -0.028), (1.82, 0.06, 0.028), "Lib_WoodDark")
        a.end()
    a.box("Col_PostL", (-0.96, 0.55, 0), (0.07, 1.02, 0.07))
    a.box("Col_PostR", (0.96, 0.55, 0), (0.07, 1.02, 0.07))
    a.box("Col_RailLow", (0, 0.28, -0.028), (1.60, 0.04, 0.02))
    a.box("Col_RailHigh", (0, 0.62, -0.028), (1.60, 0.04, 0.02))
    span = 1.72
    for i in range(12):
        x = -span * 0.5 + (i + 0.5) * span / 12.0
        a.box("Col_Picket_%d" % i, (x, 0.44, 0.02), (0.050, 0.76, 0.016))
    return a
