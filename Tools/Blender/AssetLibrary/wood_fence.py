"""Wood privacy fence bay. 2.0 m wide, 1.8 m tall, boards and two rails."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("WoodFence", "Buildings", "Privacy fence bay, 2.0 m wide, 1.8 m tall. Vertical boards, 6 mm gaps, posts at the ends.")
    a.climb_note = "The board faces are a cling panel. Gaps are 6 mm and are not a passage."
    a.vault_note = "Top is 1.80 m. Too high to vault from the ground."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        for x in (-0.96, 0.96):
            g.box((x, 0.95, 0), (0.10, 1.90, 0.10), "Lib_WoodDark", bevel=bev, segs=1, uv_scale=1.0)
        boards = lod_pick(lod, 12, 6)
        span = 1.72
        for i in range(boards):
            x = -span * 0.5 + (i + 0.5) * span / boards
            w = span / boards * 0.92
            g.box((x, 0.88, 0), (w, 1.68, 0.018), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.2)
        g.box((0, 0.40, -0.02), (1.7, 0.08, 0.04), "Lib_WoodDark")
        g.box((0, 1.35, -0.02), (1.7, 0.08, 0.04), "Lib_WoodDark")
        a.end()
    a.box("Climb_Boards", (0, 0.88, 0), (1.72, 1.68, 0.018))
    a.box("Col_PostL", (-0.96, 0.95, 0), (0.10, 1.90, 0.10))
    a.box("Col_PostR", (0.96, 0.95, 0), (0.10, 1.90, 0.10))
    return a
