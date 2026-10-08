"""Park seesaw. Fulcrum, plank, two seats, and handles. Static."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Seesaw",
        "Park",
        "Seesaw, plank 2.8 m, seats at 0.72 m, fulcrum in the middle. Handles at each seat. Pivot is the base center.",
    )
    a.climb_note = "The fulcrum is too small to cling."
    a.vault_note = "Seat is 0.72 m. Under the 0.90 m vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        g.box((0, 0.025, 0), (0.55, 0.05, 0.85), "Lib_SteelDark")
        # A-frame in the YZ plane. Legs stop short of each other.
        g.pipe((0, 0.08, -0.30), (0, 0.52, -0.06), 0.028, "Lib_Steel", seg)
        g.pipe((0, 0.08, 0.30), (0, 0.52, 0.06), 0.028, "Lib_Steel", seg)
        g.cylinder((0, 0.60, 0), 0.04, 0.22, "Lib_SteelDark", seg, axis="Z")
        g.box((0, 0.68, 0), (2.70, 0.06, 0.16), "Lib_Wood", uv_scale=1.2)
        for x in (-1.12, 1.12):
            g.box((x, 0.74, 0), (0.38, 0.04, 0.28), "Lib_PaintRed")
            if lod == 0:
                g.pipe((x, 0.76, -0.10), (x, 0.96, -0.10), 0.014, "Lib_Steel", 5)
                g.pipe((x, 0.96, -0.10), (x, 0.96, 0.10), 0.014, "Lib_Steel", 5)
                g.pipe((x, 0.96, 0.10), (x, 0.76, 0.10), 0.014, "Lib_Steel", 5)
        a.end()
    a.box("Col_Base", (0, 0.025, 0), (0.48, 0.04, 0.76))
    a.box("Col_Plank", (0, 0.68, 0), (2.50, 0.04, 0.12))
    for i, x in enumerate((-1.12, 1.12)):
        a.box("Col_Seat_%d" % i, (x, 0.74, 0), (0.30, 0.03, 0.22))
    return a
