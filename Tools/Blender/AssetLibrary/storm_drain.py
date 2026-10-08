"""Storm drain grate, 0.70 x 0.40 m. Slots are 3 cm. Walkable."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "StormDrain",
        "StreetFurniture",
        "Curb grate 0.70 x 0.40 m, 4 cm thick. Bars leave 3 cm slots. Walkable envelope.",
    )
    a.climb_note = "Flat grate."
    a.vault_note = "Flush. Not a vault."
    a.vault_height = 0.0
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        # Frame.
        g.box((0, 0.02, -0.17), (0.70, 0.04, 0.06), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 0.02, 0.17), (0.70, 0.04, 0.06), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((-0.32, 0.02, 0), (0.06, 0.04, 0.28), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0.32, 0.02, 0), (0.06, 0.04, 0.28), "Lib_SteelDark", bevel=bev, segs=1)
        bars = lod_pick(lod, 8, 4)
        span = 0.56
        for i in range(bars):
            x = -span * 0.5 + (i + 0.5) * span / bars
            g.box((x, 0.02, 0), (0.025, 0.03, 0.28), "Lib_Steel", bevel=bev, segs=1)
        a.end()
    # Envelope of the frame. Slots are 3 cm, smaller than a foot, so the grate is walkable.
    a.box("Col_Grate", (0, 0.02, 0), (0.70, 0.04, 0.40))
    return a
