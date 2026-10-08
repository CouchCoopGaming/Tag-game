"""Traffic drum.

Real size: about 23 in across and 40 in tall (0.58 x 1.02 m), orange with
four white bands and a rubber base.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Barrel_Traffic",
        "StreetFurniture",
        "Traffic drum, 0.58 m across, 1.02 m tall. Four white bands, rubber foot.",
    )
    a.climb_note = "Round plastic. Not a cling."
    a.vault_note = "Top is 1.02 m and round. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        g.cylinder((0, 0.04, 0), 0.31, 0.08, "Lib_Rubber", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.52, 0), 0.27, 0.90, "Lib_Orange", seg)
        for y in (0.28, 0.46, 0.64, 0.82):
            g.cylinder((0, y, 0), 0.282, 0.07, "Lib_PaintWhite", seg)
        g.cylinder((0, 0.98, 0), 0.29, 0.05, "Lib_Orange", seg)
        g.cylinder((0, 1.01, 0), 0.08, 0.03, "Lib_Black", 8)
        if lod == 0:
            g.pipe((-0.16, 0.96, 0), (-0.16, 1.06, 0), 0.012, "Lib_Steel", 6)
            g.pipe((0.16, 0.96, 0), (0.16, 1.06, 0), 0.012, "Lib_Steel", 6)
            g.pipe((-0.16, 1.06, 0), (0.16, 1.06, 0), 0.012, "Lib_Steel", 6)
            g.box((0.20, 0.12, 0.26), (0.10, 0.06, 0.02), "Lib_Rust")
        a.end()
    # Square stays inside the round foot. Corners were the 2 cm stick-out.
    a.box("Col_Foot", (0, 0.035, 0), (0.36, 0.04, 0.36))
    a.capsule("Col_Drum", (0, 0.52, 0), 0.22, 0.78, 1)
    return a
