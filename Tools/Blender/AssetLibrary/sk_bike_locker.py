"""Single bike locker. A tall cabinet, not the wave rack."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "BikeLocker_Single",
        "StreetFurniture",
        "Bike locker, 0.78 m wide and 1.82 m tall. Plinth, door, vents, and a pull.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Roof is 1.82 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.04, 0), (0.78, 0.08, 0.90), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 0.90, 0), (0.70, 1.72, 0.80), "Lib_PaintBlue", bevel=bev, segs=bs)
        g.box((0, 1.78, 0), (0.76, 0.08, 0.86), "Lib_SteelDark", bevel=bev, segs=bs)
        # Door seam and a pull that stands off the door on two standoffs.
        g.box((0.32, 0.95, 0.406), (0.012, 1.40, 0.010), "Lib_Black")
        g.box((0.22, 0.95, 0.412), (0.016, 0.05, 0.016), "Lib_Steel")
        g.box((0.22, 1.15, 0.412), (0.016, 0.05, 0.016), "Lib_Steel")
        g.box((0.22, 1.05, 0.428), (0.018, 0.22, 0.016), "Lib_Steel")
        if lod == 0:
            for y in (1.35, 1.42, 1.49):
                g.box((-0.08, y, 0.406), (0.28, 0.012, 0.008), "Lib_Black")
        a.end()
    a.box("Col_Foot", (0, 0.018, 0), (0.52, 0.024, 0.60))
    a.box("Col_Body", (0, 0.90, 0), (0.50, 1.20, 0.56))
    a.box("Col_Cap", (0, 1.785, 0), (0.54, 0.03, 0.60))
    return a
