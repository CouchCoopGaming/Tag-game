"""Concrete quay. Bullnose on the water side (-Z), rubber fenders, deck at 0.90 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Quay_Edge",
        "Harbor",
        "Concrete quay module, 18 x 3.6 m, deck at 0.90 m. Bullnose and five fenders on -Z. The face runs into the water.",
    )
    a.climb_note = "The face is a wall. Too low and too thick to treat as a cling panel from the water."
    a.vault_note = "Deck is 0.90 m. The bullnose is rounded, not a rail."
    a.allow_below = True
    a.loose_pivot = True
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = 0.01 if lod == 0 else 0
        # Deck and face meet on a plane. They do not share a volume.
        g.box((0, 0.72, 0.45), (18.0, 0.36, 3.0), "Lib_Concrete", bevel=bev, segs=1, uv_scale=0.4)
        g.box((0, 0.16, -1.22), (18.0, 0.72, 0.28), "Lib_Concrete", uv_scale=0.4)
        g.cylinder((0, 0.84, -1.02), 0.10, 17.6, "Lib_Concrete", seg, axis="X")
        for x in (-7.0, -3.5, 0.0, 3.5, 7.0):
            g.cylinder((x, 0.32, -1.58), 0.14, 0.62, "Lib_Rubber", seg)
            if lod == 0:
                g.box((x, 0.62, -1.58), (0.16, 0.03, 0.16), "Lib_SteelDark")
                g.box((x, 0.95, -0.35), (0.28, 0.03, 0.14), "Lib_Steel")
        if lod == 0:
            g.box((0, 0.91, 0.5), (17.2, 0.012, 0.08), "Lib_Lane")
        a.end()
    a.box("Col_Deck", (0, 0.72, 0.55), (17.6, 0.30, 2.6))
    a.box("Col_Face", (0, 0.18, -1.22), (17.6, 0.64, 0.22))
    for i, x in enumerate((-7.0, -3.5, 0.0, 3.5, 7.0)):
        a.capsule("Col_Fender_%d" % i, (x, 0.32, -1.58), 0.11, 0.48, 1)
    return a
