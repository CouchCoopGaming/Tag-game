"""Fixed steel ladder. 0.48 m wide, 3.60 m tall, rungs every 0.30 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Ladder_Fixed",
        "StreetFurniture",
        "Fixed ladder, 0.48 m outside the rails, 3.60 m tall, rungs at 0.30 m. Standoffs on the back.",
    )
    a.climb_note = "Round rails, 3 cm. Not a flat cling."
    a.vault_note = "Rails are vertical. Not a vault lip."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = lod_pick(lod, 0.002, 0.0)
        for x in (-0.22, 0.22):
            g.cylinder((x, 1.80, 0), 0.016, 3.56, "Lib_Steel", seg)
            g.box((x, 0.02, 0), (0.08, 0.04, 0.08), "Lib_SteelDark", bevel=bev, segs=1)
        rungs = range(1, 12) if lod == 0 else range(1, 12, 2)
        for i in rungs:
            y = 0.30 * i
            g.cylinder((0, y, 0), 0.011, 0.44, "Lib_SteelDark", 6, axis="X")
        for y in (0.90, 2.40):
            for x in (-0.22, 0.22):
                g.cylinder((x, y, 0.08), 0.012, 0.12, "Lib_Steel", 6, axis="Z")
                g.box((x, y, 0.15), (0.06, 0.08, 0.02), "Lib_SteelDark", bevel=bev, segs=1)
        a.end()
    for i, x in enumerate((-0.22, 0.22)):
        a.capsule("Col_Rail_%d" % i, (x, 1.80, 0), 0.012, 3.40, 1)
        a.box("Col_Foot_%d" % i, (x, 0.02, 0), (0.06, 0.03, 0.06))
    for i, y in enumerate((0.30, 0.90, 1.50, 2.10, 2.70, 3.30)):
        a.box("Col_Rung_%d" % i, (0, y, 0), (0.32, 0.014, 0.014))
    return a
