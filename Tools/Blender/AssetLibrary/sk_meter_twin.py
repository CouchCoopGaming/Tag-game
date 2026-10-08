"""Twin-head parking meter. Heads sit side by side at 1.22–1.48 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import bolt_ring


@register
def create():
    a = Asset(
        "ParkingMeter_Twin",
        "StreetFurniture",
        "Dual-head meter. Post to 1.12 m, heads 1.22–1.48 m, 0.36 m across the pair.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.015, 0), (0.22, 0.03, 0.22), "Lib_SteelDark", bevel=bev, segs=1)
        if lod == 0:
            bolt_ring(g, (0, 0.034, 0), 0.08, 4, 0.01, 0.012)
        g.cylinder((0, 0.58, 0), 0.028, 1.08, "Lib_Steel", seg)
        g.box((0, 1.16, 0), (0.34, 0.06, 0.12), "Lib_SteelDark", bevel=bev, segs=1)
        for x in (-0.10, 0.10):
            g.box((x, 1.34, 0), (0.15, 0.26, 0.11), "Lib_SteelDark", bevel=bev, segs=1)
            g.cylinder((x, 1.50, 0), 0.07, 0.08, "Lib_Steel", seg, axis="Z")
            g.box((x, 1.36, 0.058), (0.08, 0.05, 0.008), "Lib_Glass")
            g.box((x, 1.26, 0.060), (0.07, 0.035, 0.01), "Lib_Black")
            if lod == 0:
                g.cylinder((x, 1.29, 0.068), 0.01, 0.01, "Lib_Brass", 6, axis="Z")
                g.box((x, 1.40, 0.06), (0.05, 0.012, 0.006), "Lib_PaintGreen")
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.20, 0.024, 0.20))
    a.capsule("Col_Post", (0, 0.58, 0), 0.024, 1.02, 1)
    for i, x in enumerate((-0.10, 0.10)):
        a.box("Col_Head_%d" % i, (x, 1.34, 0), (0.13, 0.22, 0.09))
    return a
