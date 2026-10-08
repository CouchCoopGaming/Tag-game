"""36-inch traffic cone. 0.91 m, square base, two collars."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "TrafficCone_Tall",
        "StreetFurniture",
        "36-inch cone, 0.91 m tall, 0.38 m square base, two white collars.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        g.box((0, 0.02, 0), (0.38, 0.04, 0.38), "Lib_Black", bevel=bev, segs=1)
        g.cone((0, 0.48, 0), 0.15, 0.018, 0.84, "Lib_Orange", seg)
        # Collars sized to the cone radius at that height.
        g.cylinder((0, 0.32, 0), 0.112, 0.05, "Lib_PaintWhite", seg)
        g.cylinder((0, 0.55, 0), 0.078, 0.045, "Lib_PaintWhite", seg)
        if lod == 0:
            g.cylinder((0, 0.90, 0), 0.022, 0.03, "Lib_Black", 8)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.34, 0.032, 0.34))
    a.capsule("Col_Low", (0, 0.28, 0), 0.06, 0.36, 1)
    a.capsule("Col_High", (0, 0.62, 0), 0.018, 0.40, 1)
    return a
