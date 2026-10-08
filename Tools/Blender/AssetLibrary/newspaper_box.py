"""Newspaper honor box. Glass front, coin door, 0.95 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("NewspaperBox", "StreetFurniture", "Newspaper honor box, 0.55 m wide, 0.95 m tall, glass front.")
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.08, 0), (0.48, 0.16, 0.36), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 0.55, 0), (0.52, 0.78, 0.38), "Lib_PaintBlue", bevel=bev, segs=1)
        g.box((0, 0.52, 0.185), (0.40, 0.48, 0.012), "Lib_Glass")
        g.box((0, 0.22, 0.19), (0.22, 0.10, 0.02), "Lib_Steel")
        g.box((0, 0.96, 0), (0.56, 0.04, 0.42), "Lib_PaintBlue", bevel=bev, segs=1)
        if lod == 0:
            g.text("NEWS", (0, 0.82, 0.20), 0.06, "Lib_PaintWhite", extrude=0.003)
        a.end()
    a.box("Col_Body", (0, 0.50, 0), (0.52, 0.96, 0.38))
    a.box("Col_Cap", (0, 0.96, 0), (0.56, 0.04, 0.42))
    return a
