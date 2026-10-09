"""Playground entrance sign. Two posts and a board reading PARK."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("ParkSign", "Park", "Entrance board, 1.80 m wide. The board center is at 1.70 m. Posts to 2.15 m.")
    a.climb_note = "Posts are 8 cm. Not a cling wall."
    a.vault_note = "No rail. The board is overhead of a 1.8 m player."
    for lod in (0, 1):
        g = a.begin(lod)
        for x in (-0.75, 0.75):
            g.box((x, 1.05, 0), (0.08, 2.10, 0.08), "Lib_WoodDark", uv_scale=1.0)
        g.box((0, 1.70, 0.02), (1.70, 0.55, 0.06), "Lib_Wood", bevel=0.006 if lod == 0 else 0, segs=1, uv_scale=1.0)
        if lod == 0:
            g.text("PARK", (0, 1.72, 0.06), 0.18, "Lib_PaintWhite", extrude=0.006)
        else:
            g.box((0, 1.72, 0.06), (1.0, 0.22, 0.02), "Lib_PaintWhite")
        a.end()
    for i, x in enumerate((-0.75, 0.75)):
        a.box("Col_Post_%d" % i, (x, 1.05, 0), (0.08, 2.10, 0.08))
    a.box("Col_Board", (0, 1.70, 0.02), (1.70, 0.55, 0.08))
    return a
