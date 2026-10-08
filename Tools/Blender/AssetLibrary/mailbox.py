"""Curbside mailbox on a post. Box center at 1.05 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Mailbox", "StreetFurniture", "Curbside mailbox. Post to 0.92 m, box 0.48 m long, flag on the +X side.")
    a.climb_note = "Post is a 5 cm tube. Not a cling."
    a.vault_note = "No vault edge."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.cylinder((0, 0.46, 0), 0.025, 0.92, "Lib_SteelDark", seg)
        g.box((0, 1.02, 0.02), (0.22, 0.22, 0.46), "Lib_Steel", bevel=0.006 if lod == 0 else 0, segs=1)
        g.cylinder((0, 1.13, 0.02), 0.11, 0.46, "Lib_Steel", seg, axis="Z", bevel=0.004 if lod == 0 else 0, segs=1)
        g.box((0, 1.02, 0.26), (0.16, 0.12, 0.012), "Lib_SteelDark")
        g.box((0.13, 1.08, 0.02), (0.012, 0.10, 0.16), "Lib_PaintRed")
        g.cylinder((0.145, 1.08, -0.06), 0.008, 0.02, "Lib_Steel", 6, axis="X")
        a.end()
    a.capsule("Col_Post", (0, 0.46, 0), 0.025, 0.92, 1)
    a.box("Col_Box", (0, 1.05, 0.02), (0.24, 0.28, 0.48))
    return a
